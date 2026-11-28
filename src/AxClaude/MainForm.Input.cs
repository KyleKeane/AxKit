using AxClaude.Core.Flow;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Core.Pty;
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

    protected override string EscapeGuardText => $"{BundleKeys.Text(AxClaudeKeys.Interrupt)} interrupts";

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
    protected override string? LineRefusedWhileAreaOpen => AnswerFirstWords + "; the message field is hidden while it asks";

    /// <summary>How every refusal starts that tells the user to answer what Claude asks before anything else goes to Claude Code.</summary>
    private const string AnswerFirstWords = "Answer Claude first";

    /// <summary>Said when the mode key is held back because Claude asks something: on some questions Claude Code takes that key as a yes.</summary>
    private const string ModeKeyWaitsWords = AnswerFirstWords + "; the permission mode stays as it is while it asks";

    /// <summary>Said when a key of Claude Code's own (Actions, Send key; Send queued messages now) is held back because the control area shows what Claude asks.</summary>
    private const string KeyWaitsWords = AnswerFirstWords + "; no key of Claude Code's own is sent while its question is in the list";

    /// <summary>
    /// Whether a key of Claude Code's own may go to its screen now, asked by every command that sends one (the items
    /// of Send key, Send queued messages now): not while what Claude Code waits on has not been put to the user
    /// (<see cref="WaitsUnheard"/>), and not while the control area shows it, since an answer then comes from the
    /// list, which holds its keys for a moment and binds them to the question it shows (bundle rule 11; the third
    /// review of 2026-11-27: until then such a key went to a question inside its hold). With Answer from a list off,
    /// or for a list a slash command leaves to the message field, "Claude asks" was said and the keys are how Claude
    /// Code's own list is worked: they go. When not, the window says why.
    /// </summary>
    private bool KeyMayGoToClaude()
    {
        if (WaitsUnheard)
        {
            Refuse(QuestionOpensWords);
            return false;
        }

        if (_model.PromptPending && _wait is { Shown: true })
        {
            Refuse(KeyWaitsWords);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Text that answers what Claude waits on: any text on an "Enter text for" row; else one of the question's keys,
    /// or several when it allows several. Never a key of a screen, and nothing for a question the window could not
    /// read, which the message field refuses before it asks here (the sixth review of 2026-11-27, L2: until then y, n
    /// or a number went into it, bound to nothing).
    /// </summary>
    private bool IsAnswer(string text) =>
        _model.AwaitsText || _model.PendingScreen is null && _model.PendingQuestion is { } question && question.Accepts(text);

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

        Announce(_hookVersionWait ? "Claude is starting" : ClaudeStopped, true);
        return true;
    }

    /// <summary>
    /// Whether the message field's Enter is held back because what Claude waits on has not been put to the user yet
    /// (bundle rule 11: nothing the user did not hear is answered by a key). Over the stream an answer is axKit's own
    /// to hold back until the question has been shown and heard (stream-mode.md SM-7.4; the 2026-11-21 audit): a "2"
    /// typed for something Claude wrote, in the moment before a tool call's question settles into the area, would
    /// allow that call with nothing of it heard; also while the area still shows what waited before (the review of
    /// 2026-11-27, <see cref="StillAsked"/>). With the screen the same moment is the settle's 400 ms, and longer
    /// while typed keys go out: a "1" typed then was sent as the answer to a question not yet said, and a bare Enter
    /// went to whatever the screen showed (<see cref="WaitsUnheard"/>, since 2026-11-27, second review). With
    /// Answer from a list off, the question counts as put to the user once "Claude asks" was said, and typed
    /// answers go as before.
    /// </summary>
    private bool FieldWaitsForQuestion(bool typed) =>
        Streams
            ? typed && _model.PromptPending && (_wait is not { Shown: true } shown || !_model.StillAsks(shown.Asked))
            : WaitsUnheard;

    private void SendInput()
    {
        // The text stays in the field: nothing was sent, and it is wanted again after a restart, or in a moment.
        if (StoppedSaid())
        {
            return;
        }

        var text = Input.Text.Replace("\r\n", "\n").Trim();
        if (FieldWaitsForQuestion(text.Length > 0))
        {
            // What Claude waits on has not been put to the user yet: nothing typed here goes to it. The text stays.
            Refuse(QuestionOpensWords);
            return;
        }

        if (!Streams && _model.PromptPending && WaitIdentity() is null)
        {
            // Claude Code asks something the window cannot read: nothing is bound to it, so nothing typed goes to it
            // (QueuedWrite.StillWaitsOn; the sixth review of 2026-11-27, L2). The text stays; Send key and Interrupt work it.
            Refuse(UnreadableWords);
            return;
        }

        if (AnswersInArea && text.Length > 0 && _model.PromptPending && !IsAnswer(text) && text != _keptMessage)
        {
            // D33: text sent into a question is lost (Claude answers "Please answer y or n." and drops it) or taken
            // as a key of the screen. It stays in the field; the same text sent again goes anyway (a typed answer).
            _keptMessage = text;
            var about = _model.PendingQuestion is { } waiting ? $": {waiting.Title}"
                : _model.PendingScreen is { } screen ? $": {screen.Title}" : string.Empty;
            Refuse($"Not sent, Claude asks{about}. Enter again sends");
            return;
        }

        if (Streams && text.Length > 0 && !_model.PromptPending && StreamHolds(text))
        {
            // Over the stream a typed text that would not do there what the user expects (a command of the screen alone,
            // a ! line) is said first, and stays in the field (stream-mode.md SM-5.8, SM-5.9).
            return;
        }

        _keptMessage = null;
        Input.Clear();
        if (text.Length == 0)
        {
            if (Streams)
            {
                // No prompt row takes a bare Enter over the stream (stream-mode.md SM-5.7).
                Refuse("Nothing to send");
                return;
            }

            EmptyFieldEnter();
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

        // Bundle AX-11.6: signing in goes to Anthropic through Claude Code, said once in the window's own words. Not over
        // the stream, which cannot sign in (stream-mode.md SM-6.2): there it would not be true.
        var signIn = !Streams && !answer && _lastCommand?.Name == ClaudeCodeTexts.LoginCommandName ? SignInReminder : null;
        if (signIn is not null)
        {
            _model.AddSystemLine(signIn);
        }

        // FR-7.3a: "Sent" and "Queued" can be switched off; the sign-in reminder is said whatever the switch. Said when
        // the message's Enter goes (QueueWrites), never before: a message whose writes are dropped says "Not sent"
        // alone (D33, the fourth review of 2026-11-27, L4; until then "Sent", then "Not sent").
        var sent = _settings.AnnounceSent ? (queued && !answer ? "Queued" : "Sent") : null;
        QueueWrites(text, fromField: true, () =>
        {
            if (string.Join(". ", new[] { sent, signIn }.OfType<string>()) is { Length: > 0 } said)
            {
                Announce(said, false);
            }

            if (_settings.SoundOnBell)
            {
                EventDoor.Raise(EventKind.Sent);
            }
        });
    }

    /// <summary>
    /// Queues a message or a command for Claude: the text and Enter as separate writes, since text plus CR in one
    /// chunk is treated as a paste (SPEC.md 4.4); the lines of a multi-line message are joined with a lone newline
    /// write, Claude's Ctrl+J. Every message and command passes here (the field's, an offered command's line, a
    /// command of the menus, the app's own /feedback), so here the core learns that one was sent (MainForm.Flow.cs).
    /// Over the stream a message is one line, written whole (stream-mode.md SM-5.1): nothing is typed or paced.
    /// Here, too, Claude Code's prompt row is emptied first of a message an interrupt put back there or one given back
    /// (<see cref="ClearRestoredMessage"/>, FR-2.3a), only while it holds exactly that text.
    /// A message is in flight until Claude Code has taken it (SPEC.md D33, "One message in flight"; the sixth review of
    /// 2026-11-27, F1), and every write of it asks again, right before it goes, whether it still goes where it was
    /// meant to (<see cref="MessageChecks"/>): its text goes only onto a bare prompt row; its Enter only once the frame
    /// shows the message whole on that row; then the Enter is sent once more if the row still holds it a second and a
    /// half later, and the queue waits until the row is bare again. Everything queued behind it (a second message, an
    /// offered command's line, a menu's command) waits, in order. <paramref name="fromField"/> for the message field's
    /// own message, which is put back into the field when it was not sent. The message counts as sent when its Enter
    /// goes, from inside that check and nowhere else: then the core learns of it (<see cref="FlowSent"/>) and
    /// <paramref name="sent"/> says so. A message Claude Code has not taken is given back (<see cref="WritesDropped"/>):
    /// out of the conversation (<see cref="ClaudeModel.TakeBack"/>), into the field or, when the field holds other
    /// text, into the conversation's line that says so, never lost.
    /// </summary>
    private void QueueWrites(string text, bool fromField = false, Action? sent = null)
    {
        if (Streams)
        {
            StreamMessage(text);
            FlowSent(text);
            sent?.Invoke();
            return;
        }

        var message = new Unsent(text, fromField);
        var checks = MessageChecks(message, () =>
        {
            FlowSent(text);
            sent?.Invoke();
        });
        ClearRestoredMessage(checks.Goes);
        _lastMessage = text;
        _unsent.Add(message);
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            Enqueue(lines[i], i == lines.Length - 1 ? 100 : 30, i == 0 ? checks.First : checks.Rest);
            if (i < lines.Length - 1)
            {
                Enqueue(ClaudeCodeKeys.NewLine, 30, checks.Rest);
            }
        }

        Enqueue(ClaudeCodeKeys.Enter, 50, checks.Enter);
        Enqueue(ClaudeCodeKeys.Enter, 50, checks.Again);
        // No write of its own: the point where the queue waits until Claude Code has taken the message.
        Enqueue(string.Empty, 50, checks.Taken);
        StartWrites();
    }

    /// <summary>A message queued and not yet taken by Claude Code: its text, whether it is the message field's own, which goes back into the field when it is not sent, and whether its first write went, so that Claude Code's row may hold it.</summary>
    private sealed class Unsent(string text, bool fromField)
    {
        public string Text { get; } = text;

        public bool FromField { get; } = fromField;

        public bool Typed { get; set; }
    }

    /// <summary>The messages queued that Claude Code has not taken yet, oldest first (one message in flight, D33).</summary>
    private readonly List<Unsent> _unsent = [];

    /// <summary>
    /// Why the messages not taken were given back, said in one sentence (<see cref="WritesDropped"/>): Claude Code began
    /// to ask; its row did not show the message; its row held other text, so nothing was typed; it did not take the
    /// message after its Enter; the user interrupted; Claude stopped.
    /// </summary>
    private enum GivenBack { Asked, NotShown, NotBare, NotTaken, Interrupted, Stopped }

    /// <summary>The reason a check that said over gave (<see cref="MessageChecks"/>), or that the user's interrupt or the stop gave; asked by default, as an answer's keys dropped in front of a message mean.</summary>
    private GivenBack _givenBack = GivenBack.Asked;

    /// <summary>The text of a message given back that was typed in part or whole, which Claude Code's prompt row may hold: the next message empties the row of it first, while the row holds exactly it (<see cref="ClearRestoredMessage"/>).</summary>
    private string? _rowHoldsUnsent;

    /// <summary>The conversation's line of the last messages given back when Claude stopped, written again after a stop that empties the conversation (<see cref="Relaunch"/>); null when none was written.</summary>
    private string? _givenBackOnStop;

    /// <summary>The checks of one message's writes, each asked right before its write goes, and whether the message still goes where it was meant to.</summary>
    private sealed record MessageCheck(Func<WriteCheck> First, Func<WriteCheck> Rest, Func<WriteCheck> Enter, Func<WriteCheck> Again, Func<WriteCheck> Taken, Func<bool> Goes);

    /// <summary>
    /// What every write of a queued message asks right before it is written (<see cref="ConsoleForm.Enqueue"/>; the
    /// rules are Core's, <see cref="QueuedWrite"/>). Its first write goes only onto a bare prompt row
    /// (<see cref="QueuedWrite.MessageFirst"/>), its other text and line breaks while it still goes where it was meant
    /// to (<see cref="QueuedWrite.MessageGoes"/>), its Enter once the frame shows it whole on the row as well
    /// (<see cref="QueuedWrite.MessageEnter"/>). After the Enter, the Enter again once while the row still holds it
    /// (<see cref="QueuedWrite.EnterAgain"/>, FR-2.1), and the wait until Claude Code has taken it
    /// (<see cref="QueuedWrite.Taken"/>), which ends its flight. When Claude Code has begun to ask something meanwhile,
    /// or asks something else than the question a message sent into it was for, a check says over: nothing more is
    /// typed, no Enter answers the question, and the message and those queued after it are given back
    /// (<see cref="WritesDropped"/>). The waits are <see cref="MessageWaitMs"/> each. The message counts as sent only
    /// when its Enter's check says it goes (<paramref name="sent"/>).
    /// </summary>
    private MessageCheck MessageChecks(Unsent message, Action sent)
    {
        var text = message.Text;
        var askedThen = _model.PromptPending;
        var waitedFor = WaitIdentity();
        long firstAsked = 0;
        long enterAsked = 0;
        long enteredAt = 0;
        bool Goes() => QueuedWrite.MessageGoes(askedThen, waitedFor, _model.PromptPending, WaitIdentity());

        WriteCheck Over(GivenBack why)
        {
            _givenBack = why;
            return WriteCheck.Over;
        }

        WriteCheck First()
        {
            var now = Environment.TickCount64;
            firstAsked = firstAsked == 0 ? now : firstAsked;
            var check = QueuedWrite.MessageFirst(askedThen, waitedFor, _model.PromptPending, WaitIdentity(), _model.PromptDraft, now - firstAsked, MessageWaitMs);
            message.Typed |= check == WriteCheck.Goes;
            return check == WriteCheck.Over ? Over(Goes() ? GivenBack.NotBare : GivenBack.Asked) : check;
        }

        WriteCheck Enter()
        {
            var now = Environment.TickCount64;
            enterAsked = enterAsked == 0 ? now : enterAsked;
            var check = QueuedWrite.MessageEnter(askedThen, waitedFor, _model.PromptPending, WaitIdentity(), _model.PromptHolds(text), now - enterAsked, MessageWaitMs);
            if (check == WriteCheck.Goes)
            {
                enteredAt = now;
                sent();
            }

            return check == WriteCheck.Over ? Over(Goes() ? GivenBack.NotShown : GivenBack.Asked) : check;
        }

        WriteCheck Again()
        {
            var check = QueuedWrite.EnterAgain(askedThen, _model.PromptPending, _model.PromptHolds(text), Environment.TickCount64 - enteredAt, EnterCheckMs);
            if (check == WriteCheck.Goes)
            {
                Log.Info("Enter sent again: the message was still on Claude's prompt row");
            }

            return check;
        }

        WriteCheck Taken()
        {
            var check = QueuedWrite.Taken(askedThen, _model.PromptPending, _model.PromptDraft, Environment.TickCount64 - enteredAt, MessageWaitMs);
            if (check == WriteCheck.Skip)
            {
                // Claude Code has taken it: its flight is over, and what waits behind it goes.
                _unsent.Remove(message);
            }

            return check == WriteCheck.Over ? Over(GivenBack.NotTaken) : check;
        }

        return new MessageCheck(First, () => Goes() ? WriteCheck.Goes : Over(GivenBack.Asked), Enter, Again, Taken, Goes);
    }

    /// <summary>How long a message's first write waits for a bare prompt row, its Enter for the row to show it, and its flight for the row to be bare again, each before the message is given back.</summary>
    private const int MessageWaitMs = 3000;

    /// <summary>
    /// The queue was dropped with writes in it (ConsoleForm), or Claude stopped: every message Claude Code had not taken
    /// is given back (D33), in one place whatever dropped it. Its block leaves the conversation, newest first so that
    /// each number is given again (<see cref="ClaudeModel.TakeBackAll"/>; the sixth review of 2026-11-27, L5), the
    /// message field's own messages come back into the field when it is empty, in order, a blank line between two, and
    /// every text that does not go into the field is written out in the conversation's line, on every path, a stop
    /// included (F2: until then a stop with other text in the field lost the message). The next message empties
    /// Claude Code's prompt row of what was typed of the first (<see cref="ClearRestoredMessage"/>), and one sentence
    /// says why, raised as <see cref="EventKind.NotSent"/>, an outcome that came by itself. Nothing is said when Claude
    /// stopped: the stop says its own. An answer's keys dropped with no message behind them leave this to their own
    /// check, which said so.
    /// </summary>
    protected override void WritesDropped()
    {
        var why = _givenBack;
        _givenBack = GivenBack.Asked;
        if (_unsent.Count == 0)
        {
            return;
        }

        var dropped = _unsent.ToList();
        _unsent.Clear();
        _model.TakeBackAll([.. dropped.Select(one => one.Text)]);
        _rowHoldsUnsent = dropped[0].Typed ? dropped[0].Text : null;
        _lastMessage = dropped[0].Text;
        var own = dropped.Where(one => one.FromField).Select(one => one.Text).ToList();
        var restored = own.Count > 0 && Input.TextLength == 0;
        if (restored)
        {
            Input.Text = string.Join("\n\n", own).Replace("\n", "\r\n");
            Input.Select(Input.TextLength, 0);
        }

        // Every text that is not back in the field (F2): written out here, so that nothing typed is lost.
        var elsewhere = dropped.Where(one => !(restored && one.FromField)).Select(one => one.Text.Replace("\n", " / ")).ToList();
        Log.Info(why switch
        {
            GivenBack.Asked => "The rest of a message was not written: Claude Code began to ask something while it was typed",
            GivenBack.NotShown => "A message's Enter was never written: Claude's prompt row did not show the message",
            GivenBack.NotBare => "A message was not typed: Claude's prompt row held other text",
            GivenBack.NotTaken => "A message was not taken: Claude's prompt row still held it after its Enter",
            GivenBack.Interrupted => "A message was not taken: interrupted before Claude took it",
            _ => "A message was not taken: Claude stopped",
        } + $"; {dropped.Count} message(s) given back, {(restored ? "into the message field" : "not into the field")}");

        var single = dropped.Count == 2;
        var after = dropped.Count > 1
            ? $" The {(single ? "message" : $"{dropped.Count - 1} messages")} sent after it {(single ? "was" : "were")} not typed either{(restored && own.Count > 1 ? (single ? " and is" : " and are") + " in the field after it" : string.Empty)}."
            : string.Empty;
        var field = !restored ? string.Empty : dropped[0].FromField ? " The message is back in the message field." : " Your message is back in the message field.";
        var kept = elsewhere.Count == 0 ? string.Empty
            : (own.Count > 0 && !restored ? " The message field held other text, so it is here: " : " Not put into the field: ") + string.Join(" / ", elsewhere) + ".";
        var what = dropped[0].FromField ? "your message" : "the line";
        var line = why switch
        {
            GivenBack.NotShown => $"Not sent yet: Claude's prompt row did not show {what}, so its Enter was not sent." + field + after + kept + (restored ? " Enter sends it again." : string.Empty),
            GivenBack.NotBare => $"Not sent: Claude's prompt row holds other text, so {what} was not typed. The Send key menu can empty the row." + field + after + kept,
            GivenBack.NotTaken => $"Not sent: Claude's prompt row still held {what} three seconds after its Enter." + field + after + kept,
            GivenBack.Interrupted => $"Not sent: you interrupted before Claude took {what}." + field + after + kept,
            GivenBack.Stopped => $"Not sent: Claude stopped before it took {what}." + field + after + kept,
            _ => $"Not sent: Claude asked something while {what} was being typed to it." + field + after + kept,
        };
        if (why == GivenBack.Stopped)
        {
            // Nothing is said, but nothing is lost: a text not back in the field is written out.
            if (elsewhere.Count > 0)
            {
                _model.AddSystemLine(line);
                _givenBackOnStop = line;
            }

            return;
        }

        _model.AddSystemLine(line);
        var firstOwn = dropped[0].FromField;
        var back = firstOwn ? "the message is back in the message field" : "your message is back in the message field";
        var said = why switch
        {
            GivenBack.NotShown => restored ? $"Not sent yet: {back}; Enter sends it again" : "Not sent yet: Claude's prompt row did not show it",
            GivenBack.NotBare => restored ? $"Not sent: Claude's prompt row holds other text; {back}" : "Not sent: Claude's prompt row holds other text",
            GivenBack.NotTaken => !restored ? $"Not sent: Claude did not take {(firstOwn ? "it" : "the line")}"
                : firstOwn ? "Not sent: Claude did not take the message; it is back in the message field"
                : $"Not sent: Claude did not take the line; {back}",
            GivenBack.Interrupted => restored ? "Not sent: the message is back in the message field" : "Not sent",
            _ => NotSentWords(),
        };
        Announce(EventKind.NotSent, dropped.Count > 1 ? $"{said}; the one after it was not typed either" : said, true);
    }

    /// <summary>The messages Claude Code had not taken, given back when Claude stops, with nothing said: the stop says its own. A text that cannot go back into the field is written out in the conversation (F2).</summary>
    private void GiveBackUnsentOnStop()
    {
        _givenBackOnStop = null;
        _givenBack = GivenBack.Stopped;
        WritesDropped();
        _givenBack = GivenBack.Asked;
    }

    /// <summary>
    /// Enter in the empty message field (FR-2.1): never a bare Enter checked against nothing (the fifth review of
    /// 2026-11-27, E2). While Claude Code asks (Answer from a list off, where the keys work Claude Code's own list),
    /// it goes only to that very question, asked right before it is written (<see cref="QueuedWrite.MessageGoes"/>).
    /// While nothing is asked, only when the frame shows a draft on Claude Code's prompt row, which the Enter then
    /// sends (one whose Enter Claude Code dropped); else "Nothing to send".
    /// </summary>
    private void EmptyFieldEnter()
    {
        var askedThen = _model.PromptPending;
        var waitedFor = WaitIdentity();
        if (!askedThen && _model.PromptDraft is not { Length: > 0 })
        {
            Refuse("Nothing to send");
            return;
        }

        Enqueue(ClaudeCodeKeys.Enter, 50, () =>
        {
            if (askedThen ? QueuedWrite.MessageGoes(true, waitedFor, _model.PromptPending, WaitIdentity()) : !_model.PromptPending && _model.PromptDraft is { Length: > 0 })
            {
                return WriteCheck.Goes;
            }

            Refuse(askedThen ? NotSentWords() : "Nothing to send");
            return WriteCheck.Over;
        });
        StartWrites();
    }

    /// <summary>
    /// FR-2.3a: since Claude Code 2.1.331 an interrupt can put the interrupted message back on Claude's prompt row,
    /// where the next message was typed after it and both went as one (the NVDA pass of 2026-11-24). After an
    /// interrupt, and after a message was given back that was typed in part or whole (<see cref="WritesDropped"/>),
    /// the row is emptied first: Ctrl+U for each of its lines, with Backspace between them, only while the row holds
    /// exactly that text, asked once as the first of them goes (<see cref="QueuedWrite.ClearGivenBack"/>, the same
    /// comparison as <see cref="ClaudeModel.PromptHolds"/>; the sixth review of 2026-11-27, F1: until then a given-back
    /// message's row was emptied whatever it showed, and a message counted as sent went with it). Otherwise nothing
    /// is cleared, and the next message waits for a bare row (<see cref="QueuedWrite.MessageFirst"/>), which says so
    /// when it does not come. Nothing is said here; the stream has no prompt row.
    /// </summary>
    private void ClearRestoredMessage(Func<bool> messageGoes)
    {
        var interrupted = _interruptedSinceSend;
        var given = _rowHoldsUnsent;
        _interruptedSinceSend = false;
        _rowHoldsUnsent = null;
        var last = given ?? (interrupted ? _lastMessage : null);
        if (Streams || last is null)
        {
            return;
        }

        bool? clears = null;
        WriteCheck Clear()
        {
            if (clears is null)
            {
                var check = QueuedWrite.ClearGivenBack(messageGoes(), _model.PromptHolds(last));
                if (check == WriteCheck.Over)
                {
                    _givenBack = GivenBack.Asked;
                    return check;
                }

                clears = check == WriteCheck.Goes;
                Log.Info(clears.Value
                    ? $"Claude's prompt held the message again ({last.Split('\n').Length} lines): emptied before the next one"
                    : "Claude's prompt row does not hold the message given back: nothing emptied");
            }

            return !messageGoes() ? Over() : clears.Value ? WriteCheck.Goes : WriteCheck.Skip;
        }

        WriteCheck Over()
        {
            _givenBack = GivenBack.Asked;
            return WriteCheck.Over;
        }

        var lines = last.Split('\n').Length;
        for (var i = 0; i < lines; i++)
        {
            if (i > 0)
            {
                Enqueue(ClaudeCodeKeys.Backspace, 30, Clear);
            }

            Enqueue(ClaudeCodeKeys.CtrlU, 30, Clear);
        }
    }

    /// <summary>How long after a message's Enter Claude's prompt row may still hold it before the Enter is sent once more (FR-2.1: Claude Code drops an Enter in its first seconds).</summary>
    private const int EnterCheckMs = 1500;

    /// <summary>
    /// Claude → Send queued messages now (<see cref="AxClaudeKeys.SendNow"/>): Claude's Ctrl+X Ctrl+S, which makes it take up a
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

        // The chord would go to what Claude Code asks, not to its prompt (KeyMayGoToClaude).
        if (!KeyMayGoToClaude())
        {
            return;
        }

        Enqueue(ClaudeCodeKeys.CtrlX, 30, still: null);
        Enqueue(ClaudeCodeKeys.CtrlS, 30, still: null);
        StartWrites();
        FlowActed(UserAction.SentNow);

        Announce("Sending now", false);
    }


    /// <summary>
    /// Escape to Claude (FR-2.3), and what is still queued to type goes: an answer typed out a key at a time must not
    /// land after it. Over the stream, the request that ends the running turn (stream-mode.md SM-5.3).
    /// The one command that sends Claude Code a key whatever it asks (<see cref="KeyMayGoToClaude"/> is not asked):
    /// it is the key that stops Claude, it must work at every moment, and its Escape is a no to whatever Claude Code
    /// asks, never a yes; at a question not yet heard it takes the question back, which the window then says.
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

        // A message Claude Code had not taken is given back with what was dropped (WritesDropped).
        _givenBack = GivenBack.Interrupted;
        ClearWrites();
        _givenBack = GivenBack.Asked;
        // The keys of an answer still being typed out were just dropped: the user's act ends now, not when its last
        // key would have gone (UserActedNow(typing) in SendAnswer). Until 2026-11-27 the settle went on waiting for
        // those keys (AnsweredWait.SettleWaits), and after an interrupted answer of seventy characters nothing Claude
        // Code showed was acted on for four seconds. The area holds its keys as after any key of the user's.
        UserActedNow();
        Area.Hold(UserActionMs);
        // A question the screen shows ends with this key as with an answer (MarkWaitAnswered).
        MarkWaitAnswered();
        Write(ClaudeCodeKeys.Escape, still: null);
        _interruptedSinceSend = true;
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

        // On plan approval Claude Code takes this key as an answer ("shift+tab to approve with this feedback"), and
        // at a file's question as "allow all edits": never to a question that has not been put to the user yet
        // (WaitsUnheard, as the message field's Enter), and never while Claude asks at all (the third review of
        // 2026-11-27: until then it went to a question inside its hold, a yes before a word of it was said). An
        // answer comes from the list, or with the list off from the message field.
        if (WaitsUnheard)
        {
            Refuse(QuestionOpensWords);
            return;
        }

        if (_model.PromptPending)
        {
            Refuse(ModeKeyWaitsWords);
            return;
        }

        Write(ClaudeCodeKeys.ShiftTab, still: null);
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

        // Not to what Claude asks while the list shows it, or before it was put to the user (KeyMayGoToClaude).
        if (!KeyMayGoToClaude())
        {
            return;
        }

        // With the list off, a key of Claude Code's own can answer the question its screen shows (MarkWaitAnswered).
        MarkWaitAnswered();
        Write(key, still: null);
        _interruptedSinceSend |= key == ClaudeCodeKeys.CtrlC;
    }
}
