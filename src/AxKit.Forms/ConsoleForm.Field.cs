using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The console window's field and a line taken into it (docs/axKit/framework.md step 11, first part, 2026-11-22;
/// from axClaude's <c>MainForm</c> and axTerminal's <c>TerminalForm</c>, which each had the same handler, the same
/// command and the same steps under other names). Enter in the field sends, Escape there is a guard that only
/// speaks, Ctrl+Enter sends as Enter does, and Enter on a line of the view takes the line into the field. What is
/// sent, what the guard says and what a taken line looks like in the field are the app's.
/// </summary>
public abstract partial class ConsoleForm
{
    /// <summary>Enter in the field, and Ctrl+Enter there: what the field holds goes to the program.</summary>
    protected abstract void SendField();

    /// <summary>The name of the command on Ctrl+Enter in the field: "Send the message", "Send the command".</summary>
    protected abstract string SendFieldName { get; }

    /// <summary>
    /// What Escape in the field says. Escape is a reflex key for screen reader users, and the program under the
    /// window would take it as its own (Claude as an interrupt, a shell as an edit of its line): in the field it
    /// is a guard that sends nothing and says which key does send it.
    /// </summary>
    protected abstract string EscapeGuardText { get; }

    /// <summary>A key of the field that is the app's own, asked before Enter and Escape (axTerminal's history on Ctrl+Up and Ctrl+Down); true when the app took it.</summary>
    protected virtual bool OnFieldKey(Keys key) => false;

    /// <summary>
    /// The field's text with a line of the view taken into it, and what is said: the app's shape of it (axClaude
    /// quotes the line between its markers after a blank line and says its number; axTerminal puts the line on a
    /// line of its own, to run or edit). <paramref name="existing"/> is the field's text without its last line ends.
    /// </summary>
    protected abstract (string Field, string Said) WithLine(string existing, int number, int count, string line);

    /// <summary>
    /// Why a line cannot be taken into the field while something stands in the field's place, said instead (axClaude:
    /// Claude's question, behind which the line would land unseen); null, the default, when what stands there is
    /// closed for the line (axTerminal).
    /// </summary>
    protected virtual string? LineRefusedWhileAreaOpen => null;

    /// <summary>The field's keys and the view's Enter wired, and Ctrl+Enter as a command; called once as the window is made.</summary>
    private void WireField()
    {
        Input.KeyDown += OnFieldKeyDown;
        Transcript.LineChosen += TakeLine;
        // Ctrl+Enter, the bundle's main action (AX-8.10 rule 3): in the field it sends, exactly as Enter does.
        AddKey(new Command(SendFieldName, BundleKeys.MainAction, SendField) { When = () => Input.Focused, Mark = CommandMark.Act });
    }

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (OnFieldKey(e.KeyData))
        {
            e.Handled = e.SuppressKeyPress = true;
            return;
        }

        switch (e.KeyData)
        {
            case ConsoleKeys.Send:
                // Enter sends; Shift+Enter is the field's own new line.
                e.Handled = e.SuppressKeyPress = true;
                SendField();
                break;
            case ConsoleKeys.Guard:
                e.SuppressKeyPress = true;
                Announce(EscapeGuardText, true);
                break;
                // Plain Up and Down only move the caret: text appearing in the field on an arrow key confused the reading.
        }
    }

    /// <summary>
    /// Enter on a line of the view, and the Context menu's item for it: the line goes into the field as the app
    /// shapes it (<see cref="WithLine"/>), and the focus goes there with the caret at the end. The view's caret
    /// stays where it is, so that the key to the view comes back to the same line.
    /// </summary>
    private void TakeLine(int number, int count, string text)
    {
        if (Area.Current is not null)
        {
            if (LineRefusedWhileAreaOpen is { } refused)
            {
                Announce(refused, true);
                return;
            }

            CloseArea();
        }

        var (field, said) = WithLine(Input.Text.TrimEnd('\r', '\n'), number, count, text);
        Input.Text = field;
        Input.Select(Input.TextLength, 0);
        Announce(said, true);
        GoTo(BottomControl);
    }

    /// <summary>
    /// The places as the first items of the window's Navigation menu (AX-8.14): the field on Alt+1, the view on
    /// Alt+2 and the latest of what the program said on Alt+3, each with its older Ctrl key as a further key of
    /// the same command. The app gives the three labels, which name its field and its view ("&amp;Message field",
    /// "&amp;Conversation", "&amp;Latest response").
    /// </summary>
    protected void AddPlaceItems(ToolStripMenuItem menu, string field, string view, string latest)
    {
        Place(new Command(field, ConsoleKeys.GoToField, () => GoTo(BottomControl)) { Mark = CommandMark.Open }, ConsoleKeys.GoToFieldAlso);
        Place(new Command(view, ConsoleKeys.GoToView, () => GoTo(Transcript)) { Mark = CommandMark.Open }, ConsoleKeys.GoToViewAlso);
        Place(new Command(latest, ConsoleKeys.Latest, Transcript.JumpToLatestResponse) { Mark = CommandMark.Open }, ConsoleKeys.LatestAlso);
        void Place(Command command, Keys also)
        {
            Add(menu, command);
            AddKey(command with { Key = also });
        }
    }
}
