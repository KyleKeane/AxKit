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

            Write(ClaudeCodeKeys.Enter, still: null);
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
    /// Here, too, a message an interrupt put back on Claude Code's prompt row is emptied first, and the text becomes
    /// the last message (<see cref="ClearRestoredMessage"/>, FR-2.3a): until 2026-11-27 only the message field's own
    /// send did that, so an offered command's line, a menu's command or the app's own /feedback was typed after the
    /// restored message and both went as one, and the flag of the interrupt stayed up, which kept
    /// <see cref="ResendEnterWhileHeld"/> from ever sending a dropped Enter of theirs again.
    /// Every write of the message asks again, right before it goes, whether Claude Code has begun to ask something
    /// since the message was queued (<see cref="MessageStill"/>): its Enter goes a tenth of a second after its text,
    /// and a permission question drawn in between took that Enter as its answer until the third review of
    /// 2026-11-27. <paramref name="fromField"/> for the message field's own message, which is put back into the
    /// field when it was not sent. The Enter goes only on a positive check (<see cref="QueuedWrite.MessageEnterGoes"/>,
    /// the fourth review of the same day): the frame shows the message on Claude Code's prompt row and nothing is
    /// asked; when the row does not show it yet, the Enter is left to <see cref="ResendEnterWhileHeld"/>, which asks
    /// the same. The message counts as sent when its Enter goes, not before (L4): then the core learns of it
    /// (<see cref="FlowSent"/>) and <paramref name="sent"/> says so; a message whose writes are dropped is taken
    /// back from the conversation (<see cref="ClaudeModel.TakeBack"/>) and says "Not sent" alone.
    /// </summary>
    private void QueueWrites(string text, bool fromField = false, Action? sent = null)
    {
        void Sent()
        {
            FlowSent(text);
            sent?.Invoke();
        }

        if (Streams)
        {
            StreamMessage(text);
            Sent();
            return;
        }

        var (still, enter) = MessageStill(text, fromField, Sent);
        ClearRestoredMessage(still);
        _lastMessage = text;
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            Enqueue(lines[i], i == lines.Length - 1 ? 100 : 30, still);
            if (i < lines.Length - 1)
            {
                Enqueue(ClaudeCodeKeys.NewLine, 30, still);
            }
        }

        Enqueue(ClaudeCodeKeys.Enter, 50, enter);
        StartWrites();
    }

    /// <summary>
    /// What every write of a queued message asks right before it is written (<see cref="ConsoleForm.Enqueue"/>): its
    /// text and line breaks whether the message still goes where it was meant to (<see cref="QueuedWrite.MessageGoes"/>),
    /// its Enter whether the frame shows it on Claude Code's prompt row as well (<see cref="QueuedWrite.MessageEnterGoes"/>).
    /// When Claude Code has begun to ask something meanwhile, or asks something else than the question a message sent
    /// into it was for, the write and those behind it are dropped: nothing more is typed, no Enter answers the
    /// question, the message's block leaves the conversation, and the window says that it was not sent, with a line in
    /// the conversation. The message field's own message goes back into the field when the field is empty. What was
    /// typed so far stays on Claude Code's prompt row; the next message empties the row first when it still holds
    /// this one (<see cref="ClearRestoredMessage"/>, by the same flag as after an interrupt). An Enter whose message
    /// the frame does not show yet is not written then: the window looks again every tenth of a second
    /// (<see cref="EnterWaitMs"/>) and writes it, asked once more as it goes, as soon as the row shows the message, and
    /// counts the message as sent then (<paramref name="sent"/>); a question drawn meanwhile drops it as above; a row
    /// that never shows it within <see cref="EnterWaitTries"/> looks is said, with what sends it, since the message
    /// stands typed and unsent on Claude Code's prompt row.
    /// </summary>
    private (Func<bool> Text, Func<bool> Enter) MessageStill(string text, bool fromField, Action sent)
    {
        var askedThen = _model.PromptPending;
        var waitedFor = WaitIdentity();
        bool Goes() => QueuedWrite.MessageGoes(askedThen, waitedFor, _model.PromptPending, WaitIdentity());
        bool EnterGoes() => QueuedWrite.MessageEnterGoes(askedThen, waitedFor, _model.PromptPending, WaitIdentity(), _model.PromptHolds(text));

        void Entered()
        {
            sent();
            // Claude Code may still drop it in its first seconds (FR-2.1): looked at again in a second and a half.
            ResendEnterWhileHeld(text, attempt: 1);
        }

        void WaitForRow(int looks) => After(EnterWaitMs, () =>
        {
            if (IsDisposed || !ProcessRunning || _interruptedSinceSend)
            {
                return;
            }

            if (!Goes())
            {
                Dropped();
                return;
            }

            if (EnterGoes())
            {
                // Asked again right before it goes, as every Enter of a message (the queue may hold other writes first).
                Enqueue(ClaudeCodeKeys.Enter, 50, () => EnterGoes() || (!Goes() && Dropped()));
                StartWrites();
                Entered();
                return;
            }

            if (looks < EnterWaitTries)
            {
                WaitForRow(looks + 1);
                return;
            }

            Log.Info("A message's Enter was never written: Claude's prompt row did not show the message");
            _model.AddSystemLine("Not sent yet: Claude's prompt row did not show your message. Enter in the empty message field sends what the row holds.");
            Refuse("Not sent yet: Enter in the empty message field sends it");
        });

        bool Dropped()
        {
            Log.Info("The rest of a message was not written: Claude Code began to ask something while it was typed");
            _interruptedSinceSend = true;
            _model.TakeBack(text);
            var restored = fromField && Input.TextLength == 0;
            if (restored)
            {
                Input.Text = text.Replace("\n", "\r\n");
                Input.Select(Input.TextLength, 0);
            }

            _model.AddSystemLine(restored
                ? "Not sent: Claude asked something while your message was being typed to it. The message is back in the message field."
                : "Not sent: Claude asked something while the line above was being typed to it.");
            Refuse(QuestionOpensWords);
            return false;
        }

        return (
            () => Goes() || Dropped(),
            () =>
            {
                if (!Goes())
                {
                    return Dropped();
                }

                if (EnterGoes())
                {
                    Entered();
                    return true;
                }

                WaitForRow(1);
                return false;
            });
    }

    /// <summary>How often a message's Enter that waits for Claude Code's prompt row to show the message looks again, and how many times (three seconds in all).</summary>
    private const int EnterWaitMs = 100;

    private const int EnterWaitTries = 30;

    /// <summary>
    /// FR-2.3a: since Claude Code 2.1.331 an interrupt can put the interrupted message back on Claude's prompt row,
    /// where the next message was typed after it and both went as one (the NVDA pass of 2026-11-24). After an
    /// interrupt, while the row still holds the last message, it is emptied first: Ctrl+U for each of its lines, with
    /// Backspace between them. Only then: a message the row holds for another reason (an Enter Claude dropped) stays.
    /// Nothing is said; the window behaves as before that version. The stream has no prompt row.
    /// </summary>
    private void ClearRestoredMessage(Func<bool> still)
    {
        var interrupted = _interruptedSinceSend;
        _interruptedSinceSend = false;
        if (Streams || !interrupted || _lastMessage is not { } last || !_model.PromptHolds(last))
        {
            return;
        }

        var lines = last.Split('\n').Length;
        for (var i = 0; i < lines; i++)
        {
            if (i > 0)
            {
                Enqueue(ClaudeCodeKeys.Backspace, 30, still);
            }

            Enqueue(ClaudeCodeKeys.CtrlU, 30, still);
        }

        Log.Info($"Claude's prompt held the interrupted message again ({lines} lines): emptied before the next one");
    }

    /// <summary>How long a sent message may sit on Claude's prompt row before its Enter counts as dropped, and how often it is sent again.</summary>
    private const int EnterCheckMs = 1500;

    private const int EnterAttempts = 3;

    /// <summary>
    /// FR-2.1: Claude drops an Enter in its first seconds after the prompt shows, even a second after the text, and
    /// the message stays typed and unsent on its prompt row (found by tools/QaRun on 2026-10-23: four sessions of five).
    /// While the row still holds the text and Claude neither works nor asks, Enter goes again, three times at most,
    /// silently, with a log line. Not after an interrupt (since 2026-11-27): Claude Code 2.1.331 puts an interrupted
    /// message back on its prompt row (FR-2.3a), which reads here as an Enter that was dropped, and a message
    /// interrupted within the first second and a half was sent again. Since the fourth review of the same day it asks
    /// the same positive check as a message's own Enter (<see cref="QueuedWrite.EnterAgain"/>: the row shows the
    /// message, nothing is asked), again right before the Enter goes, and whether Claude works is not asked: a message
    /// queued behind Claude's work waits on the same row.
    /// </summary>
    private void ResendEnterWhileHeld(string text, int attempt)
    {
        After(EnterCheckMs, () =>
        {
            if (!ProcessRunning || !QueuedWrite.EnterAgain(_model.PromptHolds(text), _model.PromptPending, _interruptedSinceSend))
            {
                return;
            }

            Log.Info($"Enter sent again ({attempt}): the message was still on Claude's prompt row");
            // Asked again right before it goes, as every Enter of a message (the queue may hold other writes first).
            Enqueue(ClaudeCodeKeys.Enter, 50, () => QueuedWrite.EnterAgain(_model.PromptHolds(text), _model.PromptPending, _interruptedSinceSend));
            StartWrites();
            if (attempt < EnterAttempts)
            {
                ResendEnterWhileHeld(text, attempt + 1);
            }
        });
    }

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

        ClearWrites();
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
