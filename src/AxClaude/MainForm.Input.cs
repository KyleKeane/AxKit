using AxClaude.Core.Flow;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the message field and the sending (FR-2); the paced write queue is the console window's ----

    // The field's keys are the console window's (AxKit.Forms/ConsoleForm.Field.cs; framework step 11): Enter sends
    // and Shift+Enter is the field's own new line (FR-2.1); Escape is a guard that only speaks (FR-2.3, D20:
    // Escape is a reflex key for screen reader users, and Claude takes it as an interrupt); Ctrl+Enter sends as
    // Enter does. Plain Up and Down only move the caret (D13). What is axClaude's own follows.

    /// <summary>Enter and Ctrl+Enter in the message field: the message goes to Claude (FR-2.1).</summary>
    protected override void SendField() => SendInput();

    protected override string SendFieldName => "Send the message";

    protected override string EscapeGuardText => "Shift+Escape interrupts";

    /// <summary>The literal lines around a quoted conversation line in the message, so Claude can tell it from the user's words (FR-2.9).</summary>
    private const string QuoteMarker = "_ start of copied line from conversation history _";
    private const string CommentMarker = "_ start of comment on the copied line _";

    /// <summary>
    /// Enter on a conversation line (FR-2.9): the line is appended to the message under the marker and a line
    /// naming its number, separated from what is there by a blank line, then the comment marker; the console
    /// window puts the focus into the field with the caret on the empty line under it, ready for the comment, and
    /// says "Line 12 copied". The conversation caret stays where it is, so Alt+2 comes back to the same line.
    /// </summary>
    protected override (string Field, string Said) WithLine(string existing, int number, int count, string line)
    {
        var quote = $"{QuoteMarker}\r\nLine {number} of {count}:\r\n{line}\r\n{CommentMarker}\r\n";
        return (existing.Length == 0 ? quote : existing + "\r\n\r\n" + quote, $"Line {number} copied");
    }

    /// <summary>The field is hidden behind Claude's question: the quote would land unseen and the comment in the list.</summary>
    protected override string? LineRefusedWhileAreaOpen => "Answer Claude first; the message field is hidden while it asks";

    /// <summary>
    /// Text that answers what Claude waits on: any text on an "Enter text for" row; else one of the question's keys,
    /// or several when it allows several; or, when the question could not be read, y, n or a number. Never a key of a
    /// screen.
    /// </summary>
    private bool IsAnswer(string text) =>
        _model.AwaitsText
        || _model.PendingScreen is null
        && (_model.PendingQuestion is { } question
            ? question.Accepts(text)
            : text is ClaudeCodeTexts.Yes or ClaudeCodeTexts.No or ClaudeCodeTexts.YesCapital or ClaudeCodeTexts.NoCapital || text.All(char.IsAsciiDigit));

    /// <summary>
    /// Nothing runs to take a message or a key: said, with the key that starts Claude again, or "Claude is starting"
    /// while a start still waits for Claude Code's version (events-and-grounding.md EG-2.4). True when it was said,
    /// and the caller sends nothing. Never silence on a key (FR-2.3, FR-2.4, FR-11; until 2026-11-22 the interrupt,
    /// a key of Claude Code's own and the next permission mode did nothing and said nothing while Claude was stopped).
    /// </summary>
    private bool StoppedSaid()
    {
        if (ProcessRunning)
        {
            return false;
        }

        Announce(_hookVersionWait ? "Claude is starting" : "Claude stopped. Ctrl+Shift+R starts it", true);
        return true;
    }

    private void SendInput()
    {
        // The text stays in the field: nothing was sent, and it is wanted again after a restart, or in a moment.
        if (StoppedSaid())
        {
            return;
        }

        var text = Input.Text.Replace("\r\n", "\n").Trim();
        if (AnswersInArea && text.Length > 0 && _model.PromptPending && !IsAnswer(text) && text != _keptMessage)
        {
            // D33: text sent into a question is lost (Claude answers "Please answer y or n." and drops it) or taken
            // as a key of the screen. It stays in the field; the same text sent again goes anyway (a typed answer).
            _keptMessage = text;
            var about = _model.PendingQuestion is { } waiting ? $": {waiting.Title}"
                : _model.PendingScreen is { } screen ? $": {screen.Title}" : string.Empty;
            Announce($"Not sent, Claude asks{about}. Enter again sends", true);
            return;
        }

        if (Streams && text.Length > 0 && _model.PromptPending && _wait is not { Shown: true })
        {
            // Over the stream an answer is axKit's own to hold back until the question has been shown and heard
            // (stream-mode.md SM-7.4; the 2026-11-21 audit): a "2" typed for something Claude wrote, in the moment
            // before a tool call's question settles into the area, would allow that call with nothing of it heard.
            // The text stays in the field.
            Announce("Not sent: Claude asks something, and its question opens in a moment", true);
            return;
        }

        _keptMessage = null;
        Input.Clear();
        if (text.Length == 0)
        {
            if (Streams)
            {
                // No prompt row takes a bare Enter over the stream (stream-mode.md SM-5.7).
                Announce("Nothing to send", true);
                return;
            }

            Write(ClaudeCodeKeys.Enter);
            return;
        }

        var answer = _model.PromptPending;
        if (answer && IsAnswer(text))
        {
            // Over the stream, the typed key of the answer that only explains opens its text (stream-mode.md SM-7.4).
            if (Streams && StreamExplained(text))
            {
                return;
            }

            // Typed like the control area's answers, one keystroke each ("1,3" for several answers, the words of an
            // answer of the user's own on one line).
            SendAnswer(text.Replace('\n', ' '));
            if (_settings.AnnounceSent)
            {
                Announce("Sent", false);
            }

            return;
        }

        // A question that follows belongs to the slash command sent last (QuestionRouter); an answer keeps it for the
        // next step of the same screen, a message ends it.
        if (!answer)
        {
            _lastCommand = SlashCommands.Find(text);
        }

        // A message sent into a question on purpose (the kept message sent again, or the guard off) gets its block too (FR-2.10).
        // "Queued" also for a turn that has ended and is not yet over (events-and-grounding.md EG-6.4): its reply's rows
        // may still be arriving, and the model holds the message's block back until Claude takes the message up.
        var queued = ClaudeInTurn;
        _model.Send(text, evenWhilePrompting: true);
        _messageSent = true;
        QueueWrites(text);
        if (!queued)
        {
            ResendEnterWhileHeld(text, attempt: 1);
        }

        // Bundle AX-11.6: signing in goes to Anthropic through Claude Code, said once in the window's own words.
        var signIn = !answer && _lastCommand?.Name == ClaudeCodeTexts.LoginCommandName ? SignInReminder : null;
        if (signIn is not null)
        {
            _model.AddSystemLine(signIn);
        }

        // FR-7.3a: "Sent" and "Queued" can be switched off; the sign-in reminder is said whatever the switch.
        var sent = _settings.AnnounceSent ? (queued && !answer ? "Queued" : "Sent") : null;
        if (string.Join(". ", new[] { sent, signIn }.OfType<string>()) is { Length: > 0 } said)
        {
            Announce(said, false);
        }
        if (_settings.SoundOnBell)
        {
            EventDoor.Raise(EventKind.Sent);
        }
    }

    /// <summary>
    /// Queues a message or a command for Claude: the text and Enter as separate writes, since text plus CR in one
    /// chunk is treated as a paste (SPEC.md 4.4); the lines of a multi-line message are joined with a lone newline
    /// write, Claude's Ctrl+J. Every message and command passes here (the field's, an offered command's line, a
    /// command of the menus, the app's own /feedback), so here the core learns that one was sent (MainForm.Flow.cs).
    /// Over the stream a message is one line, written whole (stream-mode.md SM-5.1): nothing is typed or paced.
    /// </summary>
    private void QueueWrites(string text)
    {
        if (Streams)
        {
            StreamMessage(text);
            return;
        }

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            Enqueue(lines[i], i == lines.Length - 1 ? 100 : 30);
            if (i < lines.Length - 1)
            {
                Enqueue(ClaudeCodeKeys.NewLine, 30);
            }
        }

        Enqueue(ClaudeCodeKeys.Enter, 50);
        StartWrites();
        FlowSent(text);
    }

    /// <summary>How long a sent message may sit on Claude's prompt row before its Enter counts as dropped, and how often it is sent again.</summary>
    private const int EnterCheckMs = 1500;

    private const int EnterAttempts = 3;

    /// <summary>
    /// FR-2.1: Claude drops an Enter in its first seconds after the prompt shows, even a second after the text, and
    /// the message stays typed and unsent on its prompt row (found by tools/QaRun on 2026-10-23: four sessions of five).
    /// While the row still holds the text and Claude neither works nor asks, Enter goes again, three times at most,
    /// silently, with a log line.
    /// </summary>
    private void ResendEnterWhileHeld(string text, int attempt)
    {
        After(EnterCheckMs, () =>
        {
            if (!ProcessRunning || ClaudeWorks || _model.PromptPending || !_model.PromptHolds(text))
            {
                return;
            }

            Log.Info($"Enter sent again ({attempt}): the message was still on Claude's prompt row");
            Enqueue(ClaudeCodeKeys.Enter, 50);
            StartWrites();
            if (attempt < EnterAttempts)
            {
                ResendEnterWhileHeld(text, attempt + 1);
            }
        });
    }

    /// <summary>
    /// Claude → Send queued messages now (Ctrl+Shift+S): Claude's Ctrl+X Ctrl+S, which makes it take up a
    /// message it holds without finishing the current step first (FR-2.8). The chord goes as two writes.
    /// </summary>
    private void SendNow()
    {
        if (Streams)
        {
            // stream-mode.md SM-5.7: the stream has no key for it; Claude Code takes a waiting message up by itself.
            SayNotWithStream();
            return;
        }

        if (_model.QueuedMessages == 0)
        {
            Announce("Nothing queued", false);
            return;
        }

        Enqueue(ClaudeCodeKeys.CtrlX, 30);
        Enqueue(ClaudeCodeKeys.CtrlS, 30);
        StartWrites();
        FlowActed(UserAction.SentNow);

        Announce("Sending now", false);
    }


    /// <summary>
    /// Escape to Claude (FR-2.3), and what is still queued to type goes: an answer typed out a key at a time must not
    /// land after it. Over the stream, the request that ends the running turn (stream-mode.md SM-5.3).
    /// </summary>
    private void Interrupt()
    {
        if (Streams)
        {
            StreamInterrupt();
            return;
        }

        if (StoppedSaid())
        {
            return;
        }

        ClearWrites();
        Write(ClaudeCodeKeys.Escape);
        FlowActed(UserAction.Escaped);
    }

    /// <summary>Claude Code's next permission mode (FR-11): its own Shift+Tab, or over the stream the request that sets the next mode (stream-mode.md SM-5.4).</summary>
    private void NextPermissionMode()
    {
        if (Streams)
        {
            StreamNextMode();
            return;
        }

        if (StoppedSaid())
        {
            return;
        }

        Write(ClaudeCodeKeys.ShiftTab);
    }

    /// <summary>
    /// A key of Claude Code's own prompt and menus, as the terminal's bytes (FR-2.4). The stream has no prompt and no
    /// menu to take it: the one sentence (stream-mode.md SM-5.7).
    /// </summary>
    private void SendKey(string key)
    {
        if (Streams)
        {
            SayNotWithStream();
            return;
        }

        if (StoppedSaid())
        {
            return;
        }

        Write(key);
    }
}
