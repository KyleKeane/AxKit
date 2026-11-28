
using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// A button on a notice. Unless <see cref="StaysOpen"/> is set, the notice closes before the action runs. On a notice
/// with a check box (<see cref="Notice.Consent"/>), the default button, unless it is also the cancel button, and any
/// button with <paramref name="NeedsConsent"/> act only once the box is checked.
/// </summary>
public sealed record OverlayChoice(string Text, Action? Action = null, bool IsDefault = false, bool IsCancel = false, bool StaysOpen = false, bool NeedsConsent = false)
{
    /// <summary>The single button of a plain notice: Enter and Escape both close it.</summary>
    public static readonly OverlayChoice Close = new("&Close", IsDefault: true, IsCancel: true);
}

/// <summary>
/// A text field on a notice (Find): its label, its accessible name, the initial
/// text, and, when <paramref name="Required"/>, what to say when the default button is pressed while the field is
/// empty (the notice then stays open). <paramref name="KeyHook"/>, when given, is asked about each key in the field
/// first and takes it by returning true (axFile's path completion on Down, Up and Right). <paramref name="Changed"/> is
/// called on each change of the text, and <paramref name="ListName"/>, when given, shows a list under the field that
/// never takes the focus, filled by the window with <see cref="OverlayPanel.SetMatches"/> (the command palette, AX-10).
/// <paramref name="Hidden"/> hides what is typed, as a password field does (axPass's master password, docs/axPass/SPEC.md
/// PW-4.2): the screen shows dots and NVDA reads the field as protected.
/// </summary>
public sealed record OverlayInput(string Label, string AccessibleName, string Initial, string EmptyMessage, bool Required = true, Func<Keys, bool>? KeyHook = null, Action? Changed = null, string? ListName = null, bool Hidden = false);

/// <summary>
/// What a notice says and offers, and nothing about how it is shown (D23): the title, the text, the buttons, at most
/// one of the text field and a control of the window's own, and whether the user opened it. Every notice goes
/// through the window base's <c>AxKitForm.ShowNotice</c>, which adds what all notices share: the key line made from
/// the buttons, the chime and the hold of a notice the user did not open, the focus, and the blocked window.
/// </summary>
public sealed record Notice(string Title, string Text, IReadOnlyList<OverlayChoice> Choices)
{
    public OverlayInput? Input { get; init; }

    /// <summary>
    /// A control of the window's own under the text, which the window makes, reads and keeps (the New session
    /// controls, <see cref="SessionControls"/>); the panel shows it in the text's place below a strip of a few lines.
    /// </summary>
    public Control? Custom { get; init; }

    /// <summary>Asked before the default button acts, when given: false keeps the notice open, the window having said why (no folder chosen yet).</summary>
    public Func<bool>? Accepts { get; init; }

    /// <summary>
    /// False for a notice whose text names its keys itself, because Enter is not the default button in the control
    /// that has the focus: a form whose first field takes several lines (the report, AX-8.10), a long text's box. The
    /// line made from the buttons, "Enter: Send. Escape: Cancel.", would be untrue there. True, the default, adds it.
    /// </summary>
    public bool KeyLine { get; init; } = true;

    /// <summary>
    /// A notice that arrived by itself and brings no chime of its own: the window sounds its own rule for it
    /// (axClaude's information screens), it follows the user's own command and was heard before (the notice shown
    /// again after a find in its text), or it is the second of its kind (axGIT's missing git). The chime alone:
    /// whether the notice holds its keys is <see cref="OpenedByUser"/>'s to say, and nothing here changes it.
    /// </summary>
    public bool Silent { get; init; }

    /// <summary>
    /// Its cancel sends a key to the program the window runs (axClaude's notice of an information screen, whose Close
    /// sends Claude Code the screen's own key): only the user's own Escape or button may run it, never a notice that
    /// comes by itself in its place (<see cref="ReplacesUnanswered"/>; the seventh review of 2026-11-27, L4: nothing
    /// is sent outside the window that the user did not ask for). Such a notice is replaced as before, its cancel
    /// unrun, and what it stood for still waits in the program.
    /// </summary>
    public bool CancelReachesProgram { get; init; }

    /// <summary>A first line of the text that the window changes while the notice shows (the folder of New session, <see cref="OverlayPanel.SetHeadLine"/>); null for none.</summary>
    public string? HeadLine { get; init; }

    /// <summary>
    /// Opened by something other than the user's own command a moment ago, and said so by its maker: an update offer,
    /// an error, a screen a program shows, the result of a job or a timer. It plays the notice chime unless it is
    /// <see cref="Silent"/>. Set by <see cref="ByItself"/> alone. The chime alone since the fourth review of
    /// 2026-11-27: every notice holds its answering keys unless it is <see cref="OpenedByUser"/>, whether its maker
    /// said ByItself or nothing.
    /// </summary>
    public bool Unprompted { get; private init; }

    /// <summary>
    /// Opened at once by the user's own key or command, and read as that command's first result: a help text, the
    /// details of what is selected, the Find notice, the command palette, an explanation the user chose from a list,
    /// a field, a form or a confirmation the command opens.
    /// Only such a notice takes its answering keys the moment it is shown. Every other notice holds them for a
    /// moment after it is shown and after the focus comes back into it (<see cref="KeyHold"/>, the one rule; the
    /// fourth review of 2026-11-27, N1: the default turned around, as the question area's <c>hold:</c> was, since a
    /// notice a job, a flow, a timer or a command run for an AI tool opened had no hold unless its maker remembered
    /// to say <see cref="ByItself"/>, and five places did not). Set by <see cref="ByUser"/> alone, whose callers
    /// <c>NoticeHoldTests</c> lists; and the window base takes it away again where the notice cannot have been the
    /// user's own command's: opened from a notice's button or after one closed (what follows an answer arrives by
    /// itself), or inside a command an AI tool ran.
    /// </summary>
    public bool OpenedByUser { get; private init; }

    /// <summary>
    /// The notice as one that arrives by itself (<see cref="Unprompted"/>): everything a program, a job or a timer
    /// shows, whatever the user did before it, with the chime. It holds as every notice does that is not
    /// <see cref="ByUser"/>.
    /// </summary>
    public Notice ByItself() => this with { Unprompted = true, OpenedByUser = false };

    /// <summary>
    /// The notice as the first result of the user's own key or command, shown in the same breath (<see cref="OpenedByUser"/>):
    /// no hold, no chime. A help text, the details of what is selected, a field or a form the command opens, the
    /// command's own confirmation. Never for a notice shown after an await, by a timer, a job or a flow, nor for what
    /// a program or an AI tool asks. <c>NoticeHoldTests</c> lists every caller.
    /// </summary>
    public Notice ByUser() => this with { OpenedByUser = true, Unprompted = false };

    /// <summary>
    /// The notice as the window base shows it (<c>AxKitForm.ShowNotice</c>, the one place). A notice made
    /// <see cref="ByUser"/> takes its keys at once only when it is, at this moment, the first result of a key of the
    /// user's that can answer no notice (the fifth review of 2026-11-27, E3: a fact of the moment, not of the source):
    /// <paramref name="userKey"/> is the key whose handling runs now, null when none does (a click, a menu item chosen
    /// in a drop-down, a timer, a post, after an await), and it must be one that <see cref="AnswerKeys.CannotAnswer"/>
    /// (a key with Ctrl or Alt, a function key). Held after all, whatever its maker said, when the key could answer
    /// (Enter, Space, Ctrl+Enter, a letter: a doubled press would answer what it opened), when no key of the user's
    /// is being handled, when <paramref name="followsAnswer"/> (shown by a notice's button or by the action after one
    /// closed, <see cref="KeyHold"/> rule 1), and when <paramref name="forAiTool"/> (inside a command clicked for an
    /// AI tool, docs/axKit/mcp.md MC-7.9). A notice not made ByUser is never the user's.
    /// </summary>
    public static Notice AsShown(Notice notice, bool followsAnswer, bool forAiTool, Keys? userKey) =>
        notice.OpenedByUser && (followsAnswer || forAiTool || userKey is not { } key || !AnswerKeys.CannotAnswer(key))
            ? notice with { OpenedByUser = false }
            : notice;

    /// <summary>
    /// Whether a notice that comes by itself (<paramref name="byItself"/>: no key of the user's handled, not from a
    /// notice's button or the action after one) in place of <paramref name="shown"/> takes the place of a question
    /// the user has not answered: a notice that holds its keys and asks, a cancel and a second choice, whatever its
    /// default (the sixth review of 2026-11-27, L1: an AI tool's request to run a command and the pairing request,
    /// whose default is their No, were left out, and the pairing's call waited its two minutes). Then that question's
    /// cancel runs first, as Escape would, so that what waited on it hears a no at once, and the new notice says which
    /// one it replaced (<see cref="Replacing"/>). The fifth review of 2026-11-27 (L7): until then the question was gone
    /// without an answer and without a word. A notice that only informs or shows progress (a Close or a Cancel and
    /// nothing else) is the flow's own, which replaces it with its result. Never one whose cancel sends a key to the
    /// program the window runs (<see cref="CancelReachesProgram"/>, the seventh review of that day, L4).
    /// </summary>
    public static bool ReplacesUnanswered(Notice? shown, bool byItself) =>
        byItself && shown is { OpenedByUser: false, CancelReachesProgram: false } held
        && held.Choices.Any(choice => choice.IsCancel) && held.Choices.Any(choice => !choice.IsCancel);

    /// <summary>
    /// The notice an AI tool's run may type its text into (docs/axKit/mcp.md MC-7.9): <paramref name="shown"/>, only when
    /// it is the very notice the run's click showed (<paramref name="shownInClick"/>, recorded as it was shown); null
    /// for any other, one that came by itself after the click above all (the sixth review of 2026-11-27, L3).
    /// </summary>
    public static Notice? ShownForRun(Notice? shown, Notice? shownInClick) =>
        shown is not null && ReferenceEquals(shown, shownInClick) ? shown : null;

    /// <summary>
    /// <paramref name="incoming"/> as it is shown in place of <paramref name="shown"/>: when it replaces a question the
    /// user has not answered (<see cref="ReplacesUnanswered"/>), that question's cancel runs first, as Escape would, so
    /// that what waits on it hears a no at once (an AI tool's pairing call ends then, not after its two minutes), and
    /// the incoming notice says which one it replaced (<see cref="Replacing"/>); else it is shown as it is. The window
    /// base calls it while it marks itself as answering a notice, so that what the cancel opens follows an answer.
    /// </summary>
    public static Notice InPlaceOf(Notice? shown, Notice incoming, bool byItself)
    {
        if (!ReplacesUnanswered(shown, byItself))
        {
            return incoming;
        }

        shown!.Choices.First(choice => choice.IsCancel).Action?.Invoke();
        return incoming.Replacing(shown);
    }

    /// <summary>The notice that replaces the unanswered question <paramref name="replaced"/>: its text begins with the question's title, so that its opening says it ("Replaces: Clear the history?").</summary>
    public Notice Replacing(Notice replaced)
    {
        var title = replaced.Title.Trim();
        return this with { Text = $"Replaces: {title}{(title.EndsWith('?') || title.EndsWith('.') || title.EndsWith('!') ? string.Empty : ".")}\r\n{Text}" };
    }

    /// <summary>
    /// Which kind of event the notice is when it opens by itself (docs/axKit/events.md EV-3), for the sound the
    /// window base raises with it: a notice no command of the user's asked for, unless the place that makes it says
    /// that a command failed (<see cref="EventKind.Failed"/>) or that an AI client asks to use axKit
    /// (<see cref="EventKind.Pairing"/>). It changes nothing a notice shows or says.
    /// </summary>
    public EventKind Kind { get; init; } = EventKind.NoticeByItself;

    /// <summary>
    /// A progress bar under the text, for a download (bundle AX-3.2). The window moves it with
    /// <see cref="OverlayPanel.SetProgress"/>; NVDA reports a progress bar the way it reports every other one, by its
    /// own settings (beeps or percentages), so the notice says nothing of its own about the progress.
    /// </summary>
    public bool Progress { get; init; }

    /// <summary>
    /// A plain letter typed in the text, after the text's jump keys have had it: true when the window acted on it
    /// (axClaude's side answer: c copies, f forks, as the screen's hint row names them; 2026-10-25). While the
    /// answering keys are held a letter is not asked about and is heard as held back; a repeat of a letter is never
    /// asked about.
    /// </summary>
    public Func<char, bool>? Letter { get; init; }

    /// <summary>
    /// A check box under the text with this as its text and name (AX-11.4, the consent before an access layer first
    /// connects): unchecked each time the notice shows, after the text and before the buttons in the Tab order, Space
    /// toggles it; the default button says "Check the box first" and does nothing else while it is unchecked. Null for none.
    /// </summary>
    public string? Consent { get; init; }

    /// <summary>
    /// The focus starts in the text, not in the field, when the notice has both (an answer to read first, with a field
    /// for a follow-up under it, docs/axKit/ai-help.md AH-6.3); Tab reaches the field. False, the default, puts it in the field.
    /// </summary>
    public bool FocusText { get; init; }

    /// <summary>A read-only text with a Close button: help texts, About and errors.</summary>
    public static Notice Plain(string title, string text) => new(title, text, [OverlayChoice.Close]);

    /// <summary>
    /// A question with one verb and a way out: the verb's button runs <paramref name="act"/> and is the default, the
    /// cancel button (Escape too) closes the notice and runs <paramref name="cancelled"/> when given. With
    /// <paramref name="cancelIsDefault"/> Enter is the way out instead (axPass's deletions, PW-4.6).
    /// </summary>
    public static Notice Ask(string title, string text, string verb, Action act, string cancel = "&Cancel", Action? cancelled = null, bool cancelIsDefault = false) =>
        new(title, text,
        [
            new OverlayChoice(verb, act, IsDefault: !cancelIsDefault),
            new OverlayChoice(cancel, cancelled, IsDefault: cancelIsDefault, IsCancel: true),
        ]);
}

/// <summary>
/// The window's own dialogs, drawn inside the window (D23). A notice or a help text takes the place of the
/// conversation and the bottom of the window until it is answered, so there is never a second window to lose. (What
/// Claude waits on goes to the control area instead, D33.) The panel holds a title, a read-only text, an optional
/// text field or an optional control of the window's own, and a row of buttons; it shows a <see cref="Notice"/>,
/// which says what, and the window decides the rest. The window hides the controls underneath, blocks its own
/// shortcuts and the menu, and routes Enter and Escape to the default and cancel buttons through its AcceptButton
/// and CancelButton; Tab moves between the text, the field or the window's control, and the buttons.
/// </summary>
public sealed class OverlayPanel : Panel
{
    /// <summary>
    /// A notice that is not <see cref="Notice.OpenedByUser"/> takes no answer for a moment (docs/axKit/SPEC.md AX-6.4; <see cref="KeyHold"/>,
    /// the one rule): it can open while the user is typing a message, and the next keys were meant for the message
    /// field. The moment starts when the notice is shown (<see cref="Populate"/>) and again when the focus comes back
    /// into it (<see cref="FocusCameBack"/>). A hold that runs is never shortened: a notice shown while one runs
    /// inherits what is left of it. Two places ask it. The keys that answer are read in <see cref="ProcessCmdKey"/>:
    /// Enter, Escape, Ctrl+Enter, each also with Shift, Space on a button or a check box, and a letter the notice
    /// gives a meaning; held, a press is heard and does nothing, and a repeat never does anything. And every button
    /// runs through <see cref="Choose"/>, which asks the hold again whatever pressed the button: a key not read
    /// above, a letter with Alt, a click, UI Automation's Invoke.
    /// </summary>
    private readonly KeyHold _hold = new();

    /// <summary>The clock of the hold, in milliseconds: the system's, or a test's own.</summary>
    private readonly Func<long> _clock;

    /// <summary>What makes a held key heard: the refusal raised at the door, or a test's own count of it.</summary>
    private readonly Action _heldKeyHeard;

    /// <summary>Whether the key message read last is a repeat (<see cref="ProcessCmdKey"/>), for the key's own event in the text.</summary>
    private bool _repeat;

    /// <summary>Whether the answering keys are held now.</summary>
    private bool Holding => _hold.Holding(_clock());

    private readonly Label _title = new();
    private readonly AxKitTextBox _text = new();
    private readonly Panel _inputRow = new();
    private readonly Label _inputLabel = new();
    private readonly AxKitTextBox _input = new();
    private readonly FlowLayoutPanel _buttons = new();
    private readonly ProgressBar _progress = new();
    private readonly AxKitList _matches = new();
    private readonly AxKitCheckBox _consent = new();

    /// <summary>The consent sentence whose box the user checked, kept for the same notice shown again after Ctrl+F (a new record each time, so by its text); cleared when the notice is answered.</summary>
    private string? _consentedText;

    /// <summary>Inside Populate, where the box is set by the panel, not the user.</summary>
    private bool _populating;

    private OverlayInput? _inputSpec;
    private Control? _custom;
    private string? _headLine;
    private string _body = string.Empty;

    public OverlayPanel()
        : this(() => Environment.TickCount64, AnswerKeys.HeldBack)
    {
    }

    /// <summary>
    /// A panel whose hold runs by <paramref name="clock"/> (milliseconds) and whose held keys call
    /// <paramref name="heldKeyHeard"/>, for a test of the rules with a clock and a count of its own, and no sound.
    /// </summary>
    public OverlayPanel(Func<long> clock, Action heldKeyHeard)
    {
        _clock = clock;
        _heldKeyHeard = heldKeyHeard;
        Visible = false;
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Control;
        Padding = new Padding(8);
        TabStop = false;

        _title.AutoSize = true;
        _title.Dock = DockStyle.Top;
        _title.Font = new Font(SystemFonts.MessageBoxFont ?? DefaultFont, FontStyle.Bold);
        _title.Padding = new Padding(0, 0, 0, 6);
        _title.UseMnemonic = false;

        // Enter is not taken by the text (AcceptsReturn is off), so it reaches the default button.
        _text.Multiline = true;
        _text.ReadOnly = true;
        _text.WordWrap = true;
        _text.ScrollBars = ScrollBars.Vertical;
        _text.AcceptsTab = false;
        _text.BackColor = SystemColors.Window;
        _text.ForeColor = SystemColors.WindowText;
        _text.Dock = DockStyle.Fill;
        _text.TabIndex = 2;
        _text.KeyDown += OnTextKeyDown;

        _inputLabel.AutoSize = true;
        _inputLabel.Location = new Point(0, 6);
        _input.Location = new Point(0, 2);
        _inputRow.Dock = DockStyle.Top;
        _inputRow.Height = 32;
        _inputRow.TabIndex = 1;
        _inputRow.Controls.Add(_inputLabel);
        _inputRow.Controls.Add(_input);
        _inputRow.SizeChanged += (_, _) => LayoutInput();
        _input.KeyDown += OnInputKeyDown;
        _input.TextChanged += (_, _) => _inputSpec?.Changed?.Invoke();

        // The matches under the field (OverlayInput.ListName): for the eyes and NVDA's object navigation, never the
        // focus, as axStart's list (AS-D2); the field's keys move the selection: the bundle's list in its mode
        // under a field that keeps the caret (framework FW-4.12).
        _matches.Dock = DockStyle.Fill;
        _matches.Field = _input;
        _matches.Visible = false;

        _buttons.Dock = DockStyle.Bottom;
        _buttons.FlowDirection = FlowDirection.LeftToRight;
        _buttons.WrapContents = true;
        _buttons.AutoSize = true;
        _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttons.Padding = new Padding(0, 6, 0, 0);
        _buttons.TabIndex = 5;

        // A download's progress bar (Notice.Progress), above the buttons; hidden otherwise.
        _progress.Dock = DockStyle.Bottom;
        _progress.Height = 22;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.AccessibleName = "Progress";
        _progress.Visible = false;

        // The consent's check box (Notice.Consent), above the progress bar and the buttons; hidden otherwise. Its text
        // wraps over a few lines, so it does not size itself.
        _consent.Dock = DockStyle.Bottom;
        _consent.AutoSize = false;
        _consent.CheckAlign = ContentAlignment.TopLeft;
        _consent.TextAlign = ContentAlignment.TopLeft;
        _consent.UseMnemonic = false;
        _consent.Padding = new Padding(0, 6, 0, 0);
        _consent.TabIndex = 4;
        _consent.Visible = false;
        // The user's check remembered by the sentence, so that the same notice shown again (after Ctrl+F in it) keeps it.
        _consent.CheckedChanged += (_, _) =>
        {
            if (!_populating)
            {
                _consentedText = _consent.Checked ? Shown?.Consent : null;
            }
        };

        // Docked controls are laid out from the last in the collection to the first: the buttons take the bottom,
        // the progress bar and the check box the strips above them, the title the top, the field the strip under the
        // title, then the text takes what is left, or, with a control of the window's own, a strip of a few lines with
        // that control filling the rest under it (it is put first in the collection when a notice brings one).
        Controls.AddRange([_matches, _text, _inputRow, _title, _consent, _progress, _buttons]);
    }

    /// <summary>A button was pressed. The window closes the notice (unless the choice stays open) and runs its action.</summary>
    public event Action<OverlayChoice>? Chosen;

    /// <summary>
    /// The keys for moving through the text by Markdown structure (AX-7.2): the heading letters every window's notices
    /// take (<see cref="TextJump.HeadingLetters"/>) unless the window sets pairs of its own (an editor adds its
    /// Ctrl+letter pairs, so that its convention holds in its notices too).
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TextJump> Jumps { get; set; } = TextJump.HeadingLetters;

    /// <summary>The notice shown, for a flow that shows it again (find in its text, AX-7.2); null before the first.</summary>
    public Notice? Shown { get; private set; }

    /// <summary>The notice's read-only text, for the window's find in it (AX-7.2): the caret kept and the match selected there.</summary>
    public TextBox Body => _text;

    /// <summary>The text as the bundle's edit control, for the heading sound the window follows it with (AX-7.9).</summary>
    internal AxKitTextBox BodyText => _text;

    /// <summary>Whether the text can be searched: a notice with text and neither a field nor a control of the window's own, since the text holds the focus then.</summary>
    public bool HasSearchableText => _text.Visible && _inputSpec is null && _custom is null;

    /// <summary>The button Enter presses: the window's AcceptButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? DefaultButton { get; private set; }

    /// <summary>The button Escape presses: the window's CancelButton while the notice shows. Null when there is none.</summary>
    public IButtonControl? CancelButton { get; private set; }

    /// <summary>The text field's current text.</summary>
    public string InputText => _input.Text;

    /// <summary>
    /// A password typed into a notice's hidden field goes with the notice, whichever way it closed, Cancel and Escape
    /// included (security audit S-17): the notice's action has read it by then, and the field held it until the next
    /// notice before.
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible && _input.UseSystemPasswordChar)
        {
            _input.Text = string.Empty;
        }

        base.OnVisibleChanged(e);
    }

    /// <summary>Where the caret is in the text field.</summary>
    public int InputCaret => _input.SelectionStart;

    /// <summary>The text field's text replaced, the caret after it: a completion the window chose.</summary>
    public void SetInputText(string text)
    {
        _input.Text = text;
        _input.Select(text.Length, 0);
    }

    /// <summary>The list under the field (OverlayInput.ListName) filled anew, named "Commands, 3", the first line selected.</summary>
    public void SetMatches(IReadOnlyList<string> lines, string name) => _matches.Fill(lines, name, 0);

    /// <summary>The selected line of the list under the field.</summary>
    public int MatchIndex => _matches.SelectedIndex;

    /// <summary>The selection of the list under the field moved to a line, kept within the <paramref name="count"/> matches: the line it is on, the edge sound at an end, and -1 with the sound of nothing found when there is no match (<see cref="AxKitList.MoveFromField"/>).</summary>
    public int MoveMatch(int index, int count) => _matches.MoveFromField(index, count);

    /// <summary>The lines the list shows at once, for Page Up and Page Down.</summary>
    public int MatchPage => _matches.PageRows;

    /// <summary>The first line of the text changed while the notice shows (<see cref="Notice.HeadLine"/>): the folder of New session.</summary>
    public void SetHeadLine(string? line)
    {
        _headLine = line;
        SetText();
    }

    /// <summary>
    /// The notice's text replaced while it shows, its buttons, the focus and, as far as the new text reaches, the
    /// caret kept: axClaude's information screen as Claude redraws it (docs/axClaude/SPEC.md FR-2.11). <see cref="Shown"/>
    /// then holds the new text.
    /// </summary>
    public void SetBody(string text)
    {
        if (Shown is not { } shown)
        {
            return;
        }

        Shown = shown with { Text = text };
        _body = shown.KeyLine ? WithKeyLine(text, shown.Choices) : text;
        var caret = _text.SelectionStart;
        SetText();
        _text.Select(Math.Min(caret, _text.TextLength), 0);
    }

    /// <summary>The default button pressed, as Enter presses it: a control of the window's own asks for it (Enter in the session controls' free field).</summary>
    public void PressDefault() => DefaultButton?.PerformClick();

    /// <summary>
    /// Ctrl+Enter while the notice shows (AX-8.10): the notice's default button, from whichever control has the
    /// focus: a form's Send from any of its fields, a long text saved from inside its box, where plain Enter makes a
    /// new line; the edge sound when the notice has no default button. Nothing while the answering keys of an
    /// unprompted notice are held, but the sound of a key that was held back.
    /// </summary>
    public void PressMain()
    {
        if (Holding)
        {
            _heldKeyHeard();
            return;
        }

        if (DefaultButton is { } button)
        {
            button.PerformClick();
        }
        else
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
        }
    }

    /// <summary>The font of the text, kept in step with the conversation's by the window.</summary>
    public void SetTextFont(Font font)
    {
        _text.Font = font;
        _matches.Font = font;
        if (_custom is not null)
        {
            _text.Height = TextStripHeight();
        }
    }

    /// <summary>
    /// Fills the panel with a notice, whose text the window has completed with the key line. The text box carries the
    /// title as its accessible name, so a screen reader hears the title and then the first line when the focus lands
    /// on it. A notice the user did not open holds its answering keys for a moment.
    /// </summary>
    public void Populate(Notice notice)
    {
        Shown = notice;
        var (title, text, choices) = (notice.Title, notice.Text, notice.Choices);
        var input = notice.Input;
        if (!notice.OpenedByUser)
        {
            _hold.Start(_clock());
        }

        _title.Text = title;
        _text.AccessibleName = title;
        _body = notice.KeyLine ? WithKeyLine(text, choices) : text;
        _headLine = notice.HeadLine;
        // The field's spec goes in after its text is set, so that Changed does not fire for the filling (the palette
        // filled its list twice), nor the previous notice's Changed for the new text.
        _inputSpec = null;
        ShowCustom(notice.Custom);
        if (_custom is not null)
        {
            // The text in a strip of a few lines, the window's control filling the rest under it.
            _text.Dock = DockStyle.Top;
            _text.Height = TextStripHeight();
        }
        else if (input?.ListName is not null)
        {
            // The key line in a strip under the field, the list filling the rest.
            _text.Dock = DockStyle.Top;
            _text.Height = _text.Font.Height * 2 + 10;
        }
        else
        {
            _text.Dock = DockStyle.Fill;
        }

        _matches.Visible = input?.ListName is not null;
        _matches.AccessibleName = input?.ListName ?? string.Empty;
        _matches.Items.Clear();
        SetText();
        _progress.Visible = notice.Progress;
        if (notice.Progress)
        {
            _progress.Style = ProgressBarStyle.Marquee;
        }

        _populating = true;
        _consent.Checked = notice.Consent is not null && notice.Consent == _consentedText;
        _populating = false;
        _consent.Visible = notice.Consent is not null;
        _consent.Text = notice.Consent ?? string.Empty;
        _consent.AccessibleName = notice.Consent ?? string.Empty;
        _consent.Height = ConsentHeight();

        _inputRow.Visible = input is not null;
        // The field before the text in the Tab order, as the Find and one-field notices read; after it when the notice
        // is read first and the field asks more (Notice.FocusText): text, field, buttons.
        _inputRow.TabIndex = notice.FocusText ? 3 : 1;
        if (input is not null)
        {
            _inputLabel.Text = input.Label;
            _input.AccessibleName = input.AccessibleName;
            _input.UseSystemPasswordChar = input.Hidden;
            _input.Text = input.Initial;
            _input.SelectAll();
            _inputSpec = input;
            LayoutInput();
        }
        else
        {
            // A notice without the field must not see what was last typed in it (a Find text, a password).
            _input.Text = string.Empty;
            _input.UseSystemPasswordChar = false;
        }

        var old = _buttons.Controls.Cast<Control>().ToList();
        _buttons.Controls.Clear();
        foreach (var control in old)
        {
            control.Dispose();
        }

        DefaultButton = null;
        CancelButton = null;
        _defaultChoice = null;
        foreach (var choice in choices)
        {
            var button = new Button
            {
                Text = choice.Text,
                AutoSize = true,
                Height = 32,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = true,
            };
            button.Click += (_, _) => Choose(choice);
            _buttons.Controls.Add(button);
            // The first default and the first cancel, the ones the key line names (WithKeyLine): a second default (a Close
            // after Install it now) would make Enter press a button the spoken line did not name (2026-10-24 review).
            if (choice.IsDefault && DefaultButton is null)
            {
                DefaultButton = button;
                _defaultChoice = choice;
            }

            if (choice.IsCancel && CancelButton is null)
            {
                CancelButton = button;
            }
        }
    }

    /// <summary>Three lines of the consent's text, which wraps, in the panel's font; again when the text size changes while the notice shows.</summary>
    private int ConsentHeight() => (Font.Height * 3) + 12;

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _consent.Height = ConsentHeight();
    }

    /// <summary>The progress bar of a <see cref="Notice.Progress"/> notice: 0 to 100, or a marquee while the total is unknown (below 0).</summary>
    public void SetProgress(int percent)
    {
        if (percent < 0)
        {
            _progress.Style = ProgressBarStyle.Marquee;
            return;
        }

        _progress.Style = ProgressBarStyle.Blocks;
        _progress.Value = Math.Clamp(percent, 0, 100);
    }

    /// <summary>
    /// Puts the focus where reading starts: in the text field when there is one (unless the notice says the text first,
    /// <see cref="Notice.FocusText"/>), otherwise at the top of the text. A notice with a field that follows another
    /// with one (a passphrase typed again) finds the focus already in the field: nothing moves, so NVDA would say
    /// nothing, and the notice says its title and the field's name itself (2026-11-21, the confirmation went unheard).
    /// </summary>
    public void FocusStart()
    {
        if (Shown?.Custom is FormControls form)
        {
            // A form (AX-8.10, AX-7.11): the focus starts in its first control and never on its text, so that the
            // explanation is not read at opening; the window says the title and "form", and F7 reads the text.
            form.FocusFirst();
            return;
        }

        if (_inputSpec is not null && !(Shown is { FocusText: true } && _text.Visible))
        {
            var stays = _input.Focused;
            _input.Select();
            if (stays)
            {
                EventDoor.SayOn(EventKind.Said, Source, $"{_title.Text}. {_inputSpec.AccessibleName}", true);
            }

            return;
        }

        _text.Select(0, 0);
        _text.Select();
    }

    /// <summary>
    /// The key that reads the notice's text on demand (AX-7.11): the bundle's read in full, F7 (Ctrl+Q until
    /// 2026-11-26), which reads what you are on in every window (an item in full, a question again, what is playing).
    /// The window base takes it while a notice shows, before any command of the window.
    /// </summary>
    public const Keys ReadText = BundleKeys.ReadInFull;

    /// <summary>The notice's text said again, whole: its explanation, and the line of its keys when it has one.</summary>
    public void SayText() => EventDoor.SayOn(EventKind.Said, Source, _text.Visible ? _text.Text : _title.Text, true);

    /// <summary>The control the notice speaks on: its field when it has one, its text otherwise, which holds the focus while it shows.</summary>
    private Control Source => _inputSpec is not null ? _input : _text;

    /// <summary>Speaks the window's words, raised at the door already (<see cref="AxKitForm.Announce(string, bool)"/>), through a UI Automation notification raised on the notice.</summary>
    internal bool Speak(string text, bool interrupt) => EventDoor.Speak(Source, text, interrupt);

    /// <summary>
    /// The focus came back into the notice from outside it, or the window became the active one again while the
    /// notice shows: a notice that holds (any that is not <see cref="Notice.OpenedByUser"/>) starts its moment again (<see cref="KeyHold"/>, rule 3), since
    /// NVDA reads it now and the key that follows at once was meant for where the user came from. The window base
    /// calls it on its activation; the panel itself when the focus enters it.
    /// </summary>
    internal void FocusCameBack()
    {
        if (Visible && Shown is { OpenedByUser: false })
        {
            _hold.Start(_clock());
        }
    }

    protected override void OnEnter(EventArgs e)
    {
        FocusCameBack();
        base.OnEnter(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var repeat = AnswerKeys.IsRepeat(msg);
        // For the key's own event in the text, which comes after this (a letter the notice gives a meaning).
        _repeat = repeat;
        if (Answers(msg, keyData))
        {
            // A repeat is not a press (docs/axKit/SPEC.md AX-6.4): a key still down from the press that answered or
            // opened something never answers the notice that stands here now, held or not, and says nothing.
            if (repeat)
            {
                return true;
            }

            // Keys typed for the message field just before a notice the user did not open must not answer it. A
            // fresh press that is taken so is heard.
            if (Holding)
            {
                _heldKeyHeard();
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Whether a key answers the notice from where the focus is: Escape, which presses its cancel button; Enter,
    /// which presses its default button, but in a box where Enter makes a new line; either with Shift as well, which
    /// the window takes for the same press (it looks only for Ctrl and Alt; until the third review of 2026-11-27
    /// Shift+Enter and Shift+Escape answered a held notice, and as repeats too); Ctrl+Enter, the main action from any
    /// control (<see cref="BundleKeys.MainAction"/>); Space on a button or a check box, which it presses; and a
    /// letter or a digit with Alt that is the letter of one of the notice's buttons, which it presses. Anywhere else
    /// Space is typed text or a list's own key, and repeats as it always did. A plain letter the notice gives a
    /// meaning is read in the text's own event (<see cref="OnTextKeyDown"/>).
    /// </summary>
    private bool Answers(in Message msg, Keys keyData)
    {
        if (keyData == BundleKeys.MainAction)
        {
            return true;
        }

        if ((keyData & Keys.Modifiers) == Keys.Alt)
        {
            // A letter's and a digit's key code is its character.
            var letter = (char)(keyData & Keys.KeyCode);
            return (keyData & Keys.KeyCode) is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9
                && _buttons.Controls.Cast<Control>().Any(button => IsMnemonic(letter, button.Text));
        }

        // Shift changes nothing about which button the key presses.
        return (keyData & ~Keys.Shift) switch
        {
            Keys.Escape => true,
            Keys.Enter => Control.FromChildHandle(msg.HWnd) is not TextBox { Multiline: true, AcceptsReturn: true },
            Keys.Space => Control.FromChildHandle(msg.HWnd) is ButtonBase,
            _ => false,
        };
    }

    /// <summary>The window's control shown under the text in place of the one before, which the window keeps (it reads the choice from it); none for a plain notice.</summary>
    private void ShowCustom(Control? custom)
    {
        if (ReferenceEquals(_custom, custom))
        {
            return;
        }

        if (_custom is not null)
        {
            Controls.Remove(_custom);
        }

        _custom = custom;
        if (custom is not null)
        {
            custom.Dock = DockStyle.Fill;
            Controls.Add(custom);
            // First in the collection: laid out last, so it takes what the others leave.
            Controls.SetChildIndex(custom, 0);
        }
    }

    /// <summary>
    /// The one place a button of the notice runs, whatever pressed it: a key, a click, UI Automation's Invoke, the
    /// window's own press of the default button. So the hold is asked here once more (<see cref="KeyHold"/>, rule 4):
    /// while it runs the press is heard and nothing happens, also for a way of pressing that the reading of the keys
    /// does not know.
    /// </summary>
    private void Choose(OverlayChoice choice)
    {
        if (Holding)
        {
            _heldKeyHeard();
            return;
        }

        Run(choice);
    }

    /// <summary>
    /// The field typed into and the default button run, as the user's Enter would (docs/axKit/mcp.md MC-7.9: the
    /// text an AI tool's run of a command carries, for the notice that command opened). No key of the user's and no
    /// answer of theirs, so it passes no hold, as <see cref="ControlArea.Fill"/> does not: the notice holds against
    /// the user's keys, which are what the hold is for, and the run that opened it fills it in the same breath. The
    /// checks of the button all apply: an empty required field, the consent's box, the window's own.
    /// </summary>
    internal void Fill(string text)
    {
        SetInputText(text);
        if (_defaultChoice is { } choice)
        {
            Run(choice);
        }
    }

    /// <summary>The choice of the default button, for <see cref="Fill"/>; null when there is none.</summary>
    private OverlayChoice? _defaultChoice;

    /// <summary>What a button does once the hold has let it through (<see cref="Choose"/>), or for a fill (<see cref="Fill"/>).</summary>
    private void Run(OverlayChoice choice)
    {
        if (choice.IsDefault && _inputSpec is { Required: true } spec && _input.Text.Length == 0)
        {
            // Nothing to act on yet (Find with an empty field): say so and stay. Refused as everywhere (AX-7.3c): the
            // sound, then the words.
            EventDoor.Raise(EventKind.Refused);
            EventDoor.SayOn(EventKind.Refused, Source, spec.EmptyMessage, true);
            _input.Select();
            return;
        }

        if ((choice.NeedsConsent || (choice.IsDefault && !choice.IsCancel)) && Shown?.Consent is not null && !_consent.Checked)
        {
            // The consent not given yet (AX-11.4): say so and stay, the focus where it was; refused as everywhere (AX-7.3c).
            EventDoor.Raise(EventKind.Refused);
            EventDoor.SayOn(EventKind.Refused, Source, "Check the box first", true);
            return;
        }

        if (choice.IsDefault && Shown?.Accepts is { } accepts && !accepts())
        {
            // The window's control is not ready (no folder chosen yet): it said so and took the focus.
            return;
        }

        // The consent notice answered, either way: the next one of the same words starts unchecked.
        if (Shown?.Consent is not null)
        {
            _consentedText = null;
        }

        Chosen?.Invoke(choice);
    }

    /// <summary>
    /// The last line of every notice but the ones that say their keys themselves (<see cref="Notice.KeyLine"/> false:
    /// Enter is a new line where their focus starts), made from its buttons so that no other notice words its keys: "Enter: Close
    /// anyway. Escape: Keep working.", or "Enter or Escape: Close." for a single button; the whole text of a notice
    /// with no words of its own (axDown's save question), so that there is always text to land on.
    /// </summary>
    private static string WithKeyLine(string text, IReadOnlyList<OverlayChoice> choices)
    {
        var enter = choices.FirstOrDefault(choice => choice.IsDefault);
        var escape = choices.FirstOrDefault(choice => choice.IsCancel);
        // The button's text as spoken: without its mnemonic marker or a trailing ellipsis.
        static string Label(OverlayChoice choice) => MenuText.Plain(choice.Text);
        var keys = enter is not null && ReferenceEquals(enter, escape) ? $"Enter or Escape: {Label(enter)}."
            : string.Join(" ", new[] { enter is null ? null : $"Enter: {Label(enter)}.", escape is null ? null : $"Escape: {Label(escape)}." }.OfType<string>());
        return keys.Length == 0 ? text : text.Length == 0 ? keys : text.TrimEnd('\n') + "\n" + keys;
    }

    private void SetText()
    {
        var body = _body.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _text.Text = _headLine is null
            ? body
            : _headLine + (body.Length > 0 ? "\r\n" + body : string.Empty);
        _text.Visible = _text.Text.Length > 0;
    }

    /// <summary>A navigation key in the text (AX-7.2): the shared jump with the window's pairs.</summary>
    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        // What a jump says (the heading it landed on, or that there is none) is raised as said: the jump tells which.
        if (TextJump.Handle(_text, Jumps, e.KeyData, (said, interrupt) => EventDoor.SayOn(EventKind.Said, Source, said, interrupt)))
        {
            e.Handled = e.SuppressKeyPress = true;
            return;
        }

        // A letter the notice gives a meaning (Notice.Letter), never one typed into the notice by accident: a repeat
        // of it does nothing, and while the notice holds its keys a press is heard and the letter is not asked about.
        if (e.Modifiers == Keys.None && e.KeyCode is >= Keys.A and <= Keys.Z && Shown?.Letter is { } letter)
        {
            var repeat = _repeat;
            _repeat = false;
            if (repeat)
            {
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (Holding)
            {
                e.Handled = e.SuppressKeyPress = true;
                _heldKeyHeard();
            }
            else if (letter((char)('a' + (e.KeyCode - Keys.A))))
            {
                e.Handled = e.SuppressKeyPress = true;
            }
        }
    }

    /// <summary>A key in the field that the window asked for (<see cref="OverlayInput.KeyHook"/>), such as axFile's path completion.</summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (_inputSpec?.KeyHook is { } hook && hook(e.KeyData))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private int TextStripHeight() => _text.Font.Height * 4 + 10;

    private void LayoutInput()
    {
        _input.Left = _inputLabel.Right + 6;
        _input.Width = Math.Max(100, _inputRow.ClientSize.Width - _input.Left);
    }
}
