using AxKit.Core;

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
        // A handler's Enter may have closed the notice that held the field (the report's Enter sends it).
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
