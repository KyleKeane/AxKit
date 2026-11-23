using AxKit.Core.Events;
using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the command field and the sending (AT-3); the paced write queue is the console window's ----

    // The field's keys are the console window's (AxKit.Forms/ConsoleForm.Field.cs; framework step 11): Enter sends
    // and Shift+Enter is the field's own new line; Escape is the guard (AT-3.5: a reflex key, and the shell would
    // take it as an edit of its line); Ctrl+Enter sends as Enter does. What is axTerminal's own follows.

    /// <summary>Enter and Ctrl+Enter in the command field: the command goes to the shell, and into the recent commands (AT-3.1).</summary>
    protected override void SendField() => SendInput(record: true);

    protected override string SendFieldName => "Send the command";

    /// <summary>
    /// The history of commands on Ctrl+Up and Ctrl+Down (AT-3.3), before the field's Enter and Escape. The bundle's
    /// page and paragraph keys (AX-8.2) are the field's own (AxKitTextBox), after this: Ctrl+Up and Ctrl+Down are
    /// the history here, Shift with them still selects by paragraph.
    /// </summary>
    protected override bool OnFieldKey(Keys key)
    {
        switch (key)
        {
            case AxTerminalKeys.OlderCommand:
                Recall(_history.Older(FieldText()));
                return true;
            case AxTerminalKeys.NewerCommand:
                Recall(_history.Newer(FieldText()));
                return true;
            default:
                return false;
        }
    }

    private string FieldText() => Input.Text.Replace("\r\n", "\n");

    /// <summary>
    /// Ctrl+Up and Ctrl+Down (AT-3.9): a command of the recent list in the field, or the draft past the newest, with
    /// the recall earcon and the command said once, the caret at its end; the edge earcon when there is none further.
    /// Plain Up and Down stay the caret's.
    /// </summary>
    private void Recall(string? command)
    {
        if (command is null)
        {
            EventDoor.Raise(EventKind.Edge);
            return;
        }

        Input.Text = command.Replace("\n", "\r\n");
        Input.Select(Input.TextLength, 0);
        EventDoor.Raise(EventKind.Recalled);
        Announce(command.Length == 0 ? "blank" : command.Replace("\n", " "), true);
    }

    /// <summary>
    /// Enter, and Ctrl+Enter as Enter (the bundle's main action, AX-8.10 rule 3); without a record from the Actions
    /// menu's Send without recording (AT-3.4): the field's text goes to the shell line by line, each with its
    /// Enter (AT-3.1); the model prints the exchange block above the prompt row (AT-3.2) and the command joins the
    /// recent list; an empty field sends a bare Enter. Nothing is sent once the shell ended.
    /// </summary>
    private void SendInput(bool record)
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended. Ctrl+Shift+R starts it again", true);
            return;
        }

        var text = Input.Text.Replace("\r\n", "\n").TrimEnd('\n', ' ');
        Input.Clear();
        if (text.Trim().Length == 0)
        {
            Write(ShellTexts.Enter);
            return;
        }

        _model.Send(text, record, CurrentDirectory);
        if (record)
        {
            _history.Add(text);
        }

        QueueWrites(text);
        Announce(record ? "Sent" : "Sent, not recorded", false);
        if (_settings.SoundOnBell)
        {
            EventDoor.Raise(EventKind.Sent);
        }
    }

    /// <summary>
    /// Queues a command for the shell: each line as its own write and Enter as a separate write after it, with
    /// axClaude's pacing, so the shell reads a multi-line command as it would from the keyboard (AT-3.1). PowerShell's
    /// console host, which runs without PSReadLine while a screen reader is on, ends a block only on an empty line,
    /// so a command of several lines gets one more Enter; with PSReadLine on it is an empty Enter at the next prompt.
    /// </summary>
    private void QueueWrites(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            Enqueue(line, 60);
            Enqueue(ShellTexts.Enter, 120);
        }

        if (lines.Length > 1 && ShellTexts.EndsBlockOnEmptyLine(_shell))
        {
            Enqueue(ShellTexts.Enter, 120);
        }

        StartWrites();
    }

    /// <summary>Ctrl+Shift+C (AT-3.5): Ctrl+C to the shell, and what is still queued to type goes.</summary>
    private void Interrupt()
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended", true);
            return;
        }

        ClearWrites();
        Write(ShellTexts.Interrupt);
        Announce("Interrupt sent", true);
    }

    /// <summary>Shift+Escape (AT-3.5): Escape to the shell.</summary>
    private void SendEscape() => SendKey(ShellTexts.Escape, "Escape sent");

    /// <summary>Shell → Send key: a key the field cannot type, for a program that wants it.</summary>
    private void SendKey(string sequence, string said)
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended", true);
            return;
        }

        Write(sequence);
        Announce(said, true);
    }

    /// <summary>
    /// Enter on a terminal line (AT-3.7): the line's text goes into the field, after a new line when the field holds
    /// text; the console window puts the focus there with the caret at the end, to run or edit it, closing what
    /// stood in the field's place, and says "Line copied". No quote markers (AT-D4).
    /// </summary>
    protected override (string Field, string Said) WithLine(string existing, int number, int count, string line) =>
        (existing.Length == 0 ? line : existing + "\r\n" + line, "Line copied");

    /// <summary>
    /// Ctrl+R (AT-3.6): the window's recent commands as a list in the field's place, the newest first; Enter puts the
    /// chosen one in the field and focuses it, Escape brings the field back.
    /// </summary>
    private void ShowRecentCommands()
    {
        if (_history.Items.Count == 0)
        {
            Announce("No recent commands", true);
            return;
        }

        var items = _history.Items.ToList();
        Area.ShowList("Recent commands", items, 0, null, focus: true, hold: false, (index, _) =>
        {
            CloseArea();
            if (index >= 0 && index < items.Count)
            {
                Input.Text = items[index];
                Input.Select(Input.TextLength, 0);
            }

            Input.Focus();
        }, () =>
        {
            CloseArea();
            Input.Focus();
        });
        Input.Visible = false;
        Area.Visible = !NoticeOpen;
    }
}
