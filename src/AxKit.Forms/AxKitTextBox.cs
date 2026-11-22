using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// The edit control of every app (docs/axKit/SPEC.md AX-8.9, 2026-11-09): a <see cref="TextBox"/> with what every
/// field of the bundle had been given one by one, so that a new field gets it by being made and nothing else.
/// No length cap (the control's own 32 767 characters cut a long paste short, axClaude FR-2.1); and in a multi-line
/// control the bundle's page and paragraph keys (<see cref="EditKeys"/>, AX-8.2), after the window's own handlers,
/// so that a window's key on the same stroke comes first (axTerminal's Ctrl+Up is its history) and the shared keys act
/// where nothing else did. Word wrap stays the control's (on); the editors alone take it from the theme file's
/// display settings (docs/axKit/theme.md TH-6). A line break a key puts into an editable field of several lines is
/// said, "new line", through the window's one announcement, unless Options, Text, Say new line is off (AX-7.8,
/// 2026-11-11): the text and selection kept as Enter goes down, compared once the key is handled
/// (<see cref="TypedLineBreak"/>). <c>TextBoxTests</c> fails on a plain <c>TextBox</c> made in an app
/// and on a second subscription of the keys.
/// </summary>
public class AxKitTextBox : TextBox
{
    private const int WM_CHAR = 0x0102;

    /// <summary>The text and the selection as Enter went down in an editable field of several lines; null otherwise (AX-7.8).</summary>
    private (string Text, int Start, int Length)? _beforeEnter;

    public AxKitTextBox()
    {
        MaxLength = 0;
    }

    /// <summary>
    /// The focus arrived in the text from outside it (AX-7.11, framework FW-3.8): it says so itself, through the
    /// window it is in: a text to read when it is read-only (a conversation, a notice's text, a file shown), a
    /// field to type in otherwise. Every text box of every window is this class.
    /// </summary>
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        if (ReadOnly)
        {
            (FindForm() as AxKitForm)?.SoundOnEntry(this, AxKit.Core.Events.EventKind.TextEntered);
        }
        else
        {
            (FindForm() as AxKitForm)?.SoundOnEntry(this, AxKit.Core.Events.EventKind.FieldEntered);
        }
    }

    // ---- edit mode and navigation mode (AX-8.13, 2026-11-15) ----

    /// <summary>
    /// Whether the text is in navigation mode: the plain letters jump (<see cref="NavigationJumps"/>), and no key, no
    /// character and no paste, cut or undo message changes the text. Off at every start: each text starts in edit mode.
    /// </summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool NavigationMode { get; private set; }

    /// <summary>The rule of AX-8.13: an editable text of several lines has the two modes; a one-line field and a read-only text have none.</summary>
    public bool HasModes => Multiline && !ReadOnly;

    /// <summary>The letters of navigation mode: Markdown's (h, 1 to 6, k, l, i, q, t), unless the window gives its own (an editor its outline's, <see cref="TextJump.Letters"/>).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TextJump> NavigationJumps { get; set; } = MarkdownLetters;

    private static readonly IReadOnlyList<TextJump> MarkdownLetters = TextJump.Letters(MarkdownKeys.Jumps);

    /// <summary>The mode changed: the window's status bar follows.</summary>
    public event EventHandler? ModeChanged;

    /// <summary>The mode key in this text (<see cref="BundleKeys.ToggleMode"/>): the other mode, and the words to say. Only where <see cref="HasModes"/>.</summary>
    public string ToggleMode()
    {
        NavigationMode = !NavigationMode;
        ModeChanged?.Invoke(this, EventArgs.Empty);
        return TextModes.Toggled(NavigationMode);
    }

    /// <summary>
    /// In navigation mode every key down goes first to the window base's <c>TakeNavigationKey</c> (AX-8.13): the jump
    /// letters, the paragraph keys and the refusal of a key that would change the text, ahead of the text box's own
    /// shortcuts and the window's commands.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (NavigationMode && FindForm() is AxKitForm window && window.TakeNavigationKey(this, keyData))
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>A message that would change the text in navigation mode: a character, or a paste, cut, clear or undo sent to the control (a menu's Paste included).</summary>
    private const int WM_CUT = 0x0300, WM_PASTE = 0x0302, WM_CLEAR = 0x0303, WM_UNDO = 0x0304, EM_UNDO = 0x00C7;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _beforeEnter = e.KeyCode == Keys.Enter && Multiline && !ReadOnly ? (Text, SelectionStart, SelectionLength) : null;
        base.OnKeyDown(e);
        if (_beforeEnter is not null && e.SuppressKeyPress)
        {
            // The window's own handler put the line break in itself (the report's Shift+Enter) or used Enter for
            // something else (axClaude's send): no character follows, so the check is now.
            SayIfLineBreakTyped();
        }

        if (!e.Handled && Multiline)
        {
            EditKeys.Handle(this, e);
        }
    }

    // ---- a move to another line by the user's own key (AX-7.9, 2026-11-11) ----

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    /// <summary>
    /// The caret went to another line on the user's key, the text unchanged and nothing selected: an arrow, a page
    /// key, Ctrl+Home or Ctrl+End, a paragraph key, or a window's command on the key (a heading jump). Never a move the
    /// window makes off a key (AX-7). The editors play the heading sound from it when the line is a heading (AX-7.9).
    /// </summary>
    public event EventHandler? UserMovedLine;

    /// <summary>Set while a key is being handled here, so that the window's commands (PreProcessMessage) and the control's own move (WndProc) are measured once each.</summary>
    private bool _measuring;

    private (int Line, int Length) Place() => (GetLineFromCharIndex(SelectionStart), TextLength);

    private bool Measures(int msg) => msg is WM_KEYDOWN or WM_SYSKEYDOWN && UserMovedLine is not null && !_measuring && IsHandleCreated;

    private void RaiseIfMoved((int Line, int Length) before)
    {
        if (IsDisposed || SelectionLength != 0)
        {
            return;
        }

        var after = Place();
        if (after.Line != before.Line && after.Length == before.Length)
        {
            UserMovedLine?.Invoke(this, EventArgs.Empty);
        }
    }

    public override bool PreProcessMessage(ref Message msg)
    {
        if (!Measures(msg.Msg))
        {
            return base.PreProcessMessage(ref msg);
        }

        var before = Place();
        _measuring = true;
        try
        {
            return base.PreProcessMessage(ref msg);
        }
        finally
        {
            _measuring = false;
            RaiseIfMoved(before);
        }
    }

    /// <summary>AX-7.8 (2026-11-11): Enter's character, or Ctrl+Enter's, handled by the control; then "new line" when it put a line break in. AX-7.9: a key's own move to another line measured.</summary>
    protected override void WndProc(ref Message m)
    {
        if (NavigationMode && (m.Msg is WM_CUT or WM_PASTE or WM_CLEAR or WM_UNDO or EM_UNDO
            || (m.Msg == WM_CHAR && !TextModes.CharacterAllowed((char)(m.WParam.ToInt64() & 0xFFFF)))))
        {
            // AX-8.13: nothing changes the text in navigation mode. The keys are refused before they get here (the window
            // base's ProcessCmdKey); this stops what comes another way: Tab in an editor, a character a program sends,
            // one that Ctrl+Alt makes on some keyboards, the menus' Cut and Paste.
            EventDoor.Raise(EventKind.Refused);
            return;
        }

        if (Measures(m.Msg))
        {
            var before = Place();
            _measuring = true;
            try
            {
                base.WndProc(ref m);
            }
            finally
            {
                _measuring = false;
            }

            RaiseIfMoved(before);
            return;
        }

        base.WndProc(ref m);
        if (m.Msg == WM_CHAR && (int)m.WParam is 13 or 10 && _beforeEnter is not null)
        {
            SayIfLineBreakTyped();
        }
    }

    private void SayIfLineBreakTyped()
    {
        if (_beforeEnter is not { } before)
        {
            return;
        }

        _beforeEnter = null;
        // A handler's Enter may have closed the notice that held the field (a one-field notice's Enter answers it).
        if (IsDisposed)
        {
            return;
        }

        if (TypedLineBreak.Inserted(before.Text, before.Start, before.Length, Text, SelectionStart) && FindForm() is AxKitForm window)
        {
            window.SayTypedLineBreak();
        }
    }
}
