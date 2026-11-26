using System.Text;
using AxClaude.Core.Flow;
using AxClaude.Core.Transcript;
using AxKit.Core.Events;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the control area: what Claude waits on, in the message field's place (D33) ----
    //
    // The state is one record, _wait: what the window did about what Claude waits on. OnFrameSettled
    // (MainForm.Model.cs), 400 ms after the screen last changed, compares it with what the model says Claude waits
    // on and does one thing: shows the wait in the area, replaces what the area shows, closes the area, or announces
    // "Claude asks". One stamp, _userActedAt, says whether what Claude shows is the user's doing. The timings of the
    // flow are the constants here. (Sixteen fields and three timers until 2026-10-23.)

    /// <summary>What Claude's screen must settle for before what it waits on is acted on (FR-7.2): a question's rows can be blank for a frame while Claude redraws it.</summary>
    private const int SettleMs = 400;

    /// <summary>How long reply speech waits for Claude's prompt row, which comes after a question's rows (D33).</summary>
    private const int SpeechHoldMs = 250;

    /// <summary>
    /// How long after the bell reply lines still held wait for "Done" (FR-7.2a): Done is said first, then the lines.
    /// Longer than the core's own cap for Done (600 ms after the bell) and the settle's 400; when no Done comes in
    /// that time (the bell was a question's, the turn was cut), the lines go as they would have.
    /// </summary>
    private const int DoneFirstMs = 800;

    /// <summary>FR-2.11: the interrupting speech of an information screen's notice waits this long after the focus moved, so that NVDA's reading of the new control does not cut it off.</summary>
    private const int FocusSettleMs = 300;

    /// <summary>
    /// The word that interrupts what NVDA is reading just before the focus moves to a question (D33, FR-7.4b): the
    /// focus then says the question, so that it is heard once. NVDA's reading of the focus may cut the word itself off.
    /// </summary>
    private const string QuestionWord = "Question";

    /// <summary>The moment before a question takes the focus from the conversation without the interrupting kind (D33).</summary>
    private const int QuestionFocusMs = 1000;

    /// <summary>
    /// How long after the user's answer or key what Claude shows next counts as the user's doing: no chime, no
    /// "Question closed", and the control area holds its keys that long so that a second Escape does not reach Claude.
    /// </summary>
    private const int UserActionMs = 3000;

    /// <summary>The settle before /feedback is opened for a draft Claude queued (D33).</summary>
    private const int DraftSettleMs = 500;

    /// <summary>
    /// What the window did about what Claude waits on (D33): the signature of the question, screen or text prompt;
    /// whether the control area shows it (false: "Claude asks" was announced, or the app answered it itself); the
    /// text Ctrl+Q speaks while it is shown; and <see cref="Stale"/> once a key of the user's went to a screen, so
    /// that the screen shown next replaces it even when it looks the same (a moved selection, another tab).
    /// </summary>
    private sealed record Wait(string Signature, bool Shown, string? Text)
    {
        public bool Stale { get; init; }
    }

    /// <summary>Where the app is in bringing up a feedback draft Claude queued (D33): about to send /feedback, waiting for Claude's list of drafts to answer it with the draft, or reviewing, until the list Claude shows after the review is closed for the user.</summary>
    private sealed record Draft(DraftStep Step, string? Title, long At);

    private enum DraftStep
    {
        Opening,
        Listing,
        Reviewing,
    }

    private Wait? _wait;
    private Draft? _draft;

    /// <summary>When the user last answered, pressed a screen key or dismissed a question, or the app did so for them: what Claude shows within <see cref="UserActionMs"/> is the user's doing.</summary>
    private long _userActedAt;

    private bool UserActed() => Environment.TickCount64 - _userActedAt < UserActionMs;

    private void UserActedNow() => _userActedAt = Environment.TickCount64;

    /// <summary>About when NVDA has read the reply text spoken last (<see cref="LeftoverSpeech"/>, FR-7.4b); 0 while none is counted as running.</summary>
    private long _replySpeechEnds;

    /// <summary>
    /// Claude waits on something the control area reads, so its rows are not read as reply lines too. Not a feedback
    /// survey: it comes after a turn, and the end of the reply still held when it arrives is spoken, where a question
    /// of Claude's drops it (docs/axKit/repairs.json R-19).
    /// </summary>
    private bool QuestionShown => AnswersInArea && _model.PromptPending && _model.PendingQuestion is not { KeyOnly: true };

    /// <summary>What Claude waits on now, as a signature: the question, the screen, or the row for the user's own words; null when it waits on nothing.</summary>
    private string? WaitSignature => _model.PendingQuestion?.Signature ?? _model.PendingScreen?.Signature ?? _model.TextPrompt;

    /// <summary>Whether the control area still shows the wait with this signature.</summary>
    private bool AreaShows(string signature) => _wait is { Shown: true } wait && wait.Signature == signature;

    /// <summary>
    /// What the control area shows is over, whatever waits next: the next thing waited on is shown and said afresh
    /// even when it reads the same. For an answer that went back over the stream (docs/axClaude/stream-mode.md
    /// SM-7.4): with a screen a question ends when Claude Code redraws it, over the stream it ends with the answer.
    /// </summary>
    private void MarkWaitOver()
    {
        if (_wait is { } wait)
        {
            _wait = wait with { Stale = true };
        }
    }

    /// <summary>
    /// What Claude waits on is answered from the control area: by the user's option, and always in a window the
    /// stream feeds, where a question has no rows in the conversation and the area is the only place that shows its
    /// answers and its caution (stream-mode.md SM-7.2, SM-7.6).
    /// </summary>
    private bool AnswersInArea => _settings.QuestionNotices || Streams;

    /// <summary>
    /// Shows what Claude waits on in the control area, in place of the message field (D33). <paramref name="show"/>
    /// puts the control there, given whether it takes the focus and whether it holds its answering keys for a moment
    /// (when the user did not cause it, since they may be typing). Such a control also plays the notice chime, unless
    /// the window is in the background with the louder alert on, which the signal then plays in its place (FR-7.3);
    /// or it plays <paramref name="chime"/>, an offered command's, wherever the window is.
    /// With Go to them at once, interrupting speech it takes the focus wherever it was, and the word
    /// <see cref="QuestionWord"/> is raised as an interrupting notification just before the focus moves: it cuts off
    /// what NVDA was reading, a reply included, and NVDA's reading of the focus then says the question, once (until
    /// 2026-11-26 the question itself was raised 300 ms after the focus, and was heard twice). Without the option, the
    /// focus moves at once from the bottom of the window and after a moment from the conversation, and nothing
    /// interrupts but leftover reply speech (FR-7.4b): NVDA reads the question when the focus reaches it, after what
    /// it was reading (a focus change alone does not cut NVDA off).
    /// </summary>
    private void ShowInArea(string signature, string text, bool unprompted, Action<bool, bool> show, Action? chime = null)
    {
        // The next question of a set, after the user's answer, interrupts too: the focus is in the area, or in the
        // message field when the area closed between two questions, and what NVDA was still reading (the answer just
        // sent) is cut off by the question now under the user's fingers (the user's rule of 2026-10-23).
        // Whatever Go to them at once says, since the focus is at the bottom of the window already and goes to the
        // question without a jump.
        var interrupt = !NoticeOpen && (unprompted ? _settings.InterruptForQuestions : Area.ContainsFocus || Input.ContainsFocus);
        var focus = !NoticeOpen && (interrupt || Input.ContainsFocus || Area.ContainsFocus);
        // FR-7.4b: a question of Claude's that opens while NVDA may still be reading reply text the window spoke cuts
        // that speech off (the word below, just before the focus), whatever Go to them at once says: the reply is in the conversation,
        // and read to its end it ran into the question (2026-11-14). The option still decides alone whether the focus
        // moves at once (interrupt and focus above are untouched). Reply text still held, not yet spoken, is dropped
        // rather than spoken after the question. Not a feedback survey, which leaves the reply's end be (R-19), and
        // not an offered command: QuestionShown is false for both.
        var cut = false;
        if (QuestionShown)
        {
            cut = !NoticeOpen && LeftoverSpeech.Running(Environment.TickCount64, _replySpeechEnds);
            _replySpeechEnds = 0;
            _speechHold.Stop();
            _heldLines.Clear();
        }

        // In the background with the louder alert on, the signal plays the alert in the notice chime's place (FR-7.3).
        // An offered command has a chime of its own (FR-2.12), so that it is told from a question of Claude's, and no
        // signal: its chime plays wherever the window is, since what asks to run code must be known at once anywhere
        // (until 2026-11-16 an offer in the background with the alert on made no sound at all).
        if (AreaChime.Plays(unprompted, _settings.SoundOnBell, _settings.BackgroundAlert, ContainsFocus, offer: chime is not null))
        {
            if (chime is not null)
            {
                chime();
            }
            else
            {
                // What Claude waits on, shown here as a list or a field: the kind has two sounds still (EV-3.3), and
                // this place plays the notice sound; the two notes are the signal's, for what cannot be shown so.
                EventDoor.Raise(EventKind.ProgramAsks, EventSound.Notice);
            }
        }

        // The question is said once, by the focus that moves to it (FR-7.4b, the maintainer's rule of 2026-11-26).
        // What NVDA is reading is cut off first, by a word that interrupts, raised just before the focus moves, so
        // that NVDA's reading of the focus comes after it and says the question; until 2026-11-26 the question itself
        // was raised 300 ms after the focus, and the user heard it twice, by the announcement and by the list.
        if (!NoticeOpen && focus && (interrupt || cut))
        {
            Announce(QuestionWord, true);
        }

        show(focus, unprompted);
        Input.Visible = false;
        Area.Visible = !NoticeOpen;
        _wait = new Wait(signature, Shown: true, text);
        // Something new is under the user's fingers: the core learns of it (MainForm.Flow.cs).
        FlowQuestionShown();
        var actedBefore = _userActedAt;
        if (!focus && !NoticeOpen && (Transcript.ContainsFocus || !ContainsFocus))
        {
            // From the conversation, or with the window not the active one, without the interrupting kind, or within
            // three seconds of the user's own answer: the focus goes to the question after a moment, so that it is
            // not found by Tab, and NVDA reads it there after what it is reading (asked 2026-10-21; the three-second
            // case was found by the off-screen replay). Not from an open menu, which the move would close (2026-10-23).
            After(QuestionFocusMs, () =>
            {
                if (AreaShows(signature) && _userActedAt == actedBefore && !NoticeOpen && Area.Current is { } current && !current.ContainsFocus)
                {
                    if (cut)
                    {
                        // Reply speech to cut off (FR-7.4b): the word that interrupts goes just before the focus, and
                        // the focus says the question, once.
                        Announce(QuestionWord, true);
                    }

                    current.Focus();
                }
            });
        }
    }

    /// <summary>The message field comes back in the control area's place (the console window's); what was shown stays handled, so that it is not shown again while Claude waits on it.</summary>
    protected override void CloseArea()
    {
        base.CloseArea();
        // An information screen's notice (FR-2.11) goes the same way as the area.
        CloseScreenNotice();
        if (_wait is { Shown: true } wait)
        {
            _wait = wait with { Shown = false, Text = null };
        }
    }

    /// <summary>
    /// Ctrl+Q (D33): the question or screen the control area shows, spoken again on demand from anywhere, the title
    /// and all its lines and then the answer the focus is on, as when it opened; the focus stays. NVDA+Tab repeats a
    /// question too, since its list is named with it, but a screen's lines are items and its name only the title. Also
    /// every character typed in the list of Claude's question (<see cref="ControlArea.ShowList"/>'s typed). The words
    /// go through the door as their own kind, <see cref="EventKind.QuestionReadAgain"/>, which has no sound until the
    /// maintainer has chosen one by ear (bundle rule 13).
    /// </summary>
    private void ReadQuestionAgain()
    {
        if (_wait is not { Shown: true, Text: { } text })
        {
            Announce("Claude is not asking anything", true);
            return;
        }

        Announce(EventKind.QuestionReadAgain, (text + Stop(text) + Area.CurrentItem).TrimEnd(' ', '.'), true);
    }

    /// <summary>
    /// A question as a list: its answers as items ("2. Green"), each with a permission question's core after it ("1.
    /// No, Write permission-test.txt", <see cref="Question.Core"/>: only the label, the key sent is the answer's own),
    /// the current one marked and the start, or check boxes when it takes several, ticked where Claude marks them.
    /// Enter hands <paramref name="choose"/> the chosen answer, or the ticked ones; <paramref name="startKey"/> starts
    /// on another answer (the setting just changed). <paramref name="typed"/>, when given, is what every typed
    /// character but Space does in place of the list's jump (<see cref="ControlArea.ShowList"/>).
    /// </summary>
    private void ShowQuestionList(string signature, string name, string text, Question question, string? startKey, bool unprompted,
        Action<IReadOnlyList<QuestionOption>> choose, Action escape, Action? typed = null)
    {
        var options = question.Options;
        var several = question.AllowsSeveral;
        var core = question.Core is { Length: > 0 } words ? ", " + words : string.Empty;
        var items = options.Select(option => $"{option.Key}. {option.Text}{core}" + (option.Current && !several ? " (current)" : string.Empty)).ToList();
        var start = Math.Max(0, options.ToList().FindIndex(option => startKey is null ? option.Current : option.Key == startKey));
        IReadOnlyList<int>? ticked = several ? Enumerable.Range(0, options.Count).Where(i => options[i].Current).ToList() : null;
        ShowInArea(signature, text, unprompted, (focus, hold) => Area.ShowList(name, items, start, ticked, focus, hold, (focused, ticks) =>
        {
            if (several && ticks.Count == 0)
            {
                Announce("Tick one with Space", true);
                return;
            }

            choose(several ? ticks.Select(i => options[i]).ToList() : [options[focused]]);
        }, escape, typed: typed));
    }

    /// <summary>
    /// What the list of a question is called, read each time the focus comes to it: the title (with its place among
    /// several) and the question's lines while they stay short, so that the review of several answers says the
    /// answers and "Ready to submit your answers?", and a long plan is not read out whole. With no
    /// <paramref name="limit"/>, the whole question, which Ctrl+Q speaks (<see cref="ReadQuestionAgain"/>).
    /// </summary>
    private static string QuestionName(Question question, int limit = 1000)
    {
        var title = Unquoted(question.Title);
        var name = new StringBuilder(question.Step is (int number, int count) ? $"Question {number} of {count}: {title}" : title);
        foreach (var line in question.Text.Select(Unquoted))
        {
            if (name.Length + line.Length > limit)
            {
                break;
            }

            name.Append(Stop(name.ToString())).Append(line);
        }

        return name.ToString();
    }

    /// <summary>A line without the quote marker Claude Code draws in front of a question's text ("> Which colour …"), which NVDA read as "greater" (found by the speech capture, 2026-10-23).</summary>
    private static string Unquoted(string line) => line.StartsWith(ClaudeCodeTexts.QuoteMark, StringComparison.Ordinal) ? line[ClaudeCodeTexts.QuoteMark.Length..] : line;

    /// <summary>What goes between two sentences: a space after a text that ends in its own punctuation, else a full stop and a space; nothing after an empty text.</summary>
    private static string Stop(string before) => before.Length == 0 ? string.Empty : before[^1] is '.' or '?' or '!' or ':' ? " " : ". ";

    /// <summary>
    /// One of Claude's questions (D33): the answers as a list in the control area. Enter answers, Escape cancels the
    /// question. Other, like any answer, is sent as its key; Claude then asks for the words, and
    /// <see cref="ShowOwnAnswer"/> follows. False when the app answered the question itself (a review it submits).
    /// </summary>
    private bool ShowClaudeQuestion(Question question)
    {
        if (question.IsSettings)
        {
            ShowSettings(question);
            return true;
        }

        if (SubmitsReview(question))
        {
            // The last answer's "Sent" said nothing about a review; y goes as the user's own answer would.
            SendAnswer(ClaudeCodeTexts.Yes);
            return false;
        }

        if (question.KeyOnly && _settings.DismissSurveys && question.EscapeKey is { } dismiss && !WritesPending)
        {
            // Options, Questions, Dismiss feedback surveys (2026-11-12): the app gives the survey's Dismiss, or No to
            // the transcript question after it, as the user's Escape would, and nothing is heard. Not while a write of
            // the user's is on its way to Claude: the key would land in the middle of the message (R-18); the survey is
            // then shown like any question.
            TypeAnswer(dismiss, enter: false);
            return false;
        }

        _askedQuestion = question;
        // Every character typed in the list but Space reads the whole question again, as the read-again key does, and
        // nothing jumps to an answer by typing, its number, y or n included (the maintainer's word of 2026-11-26:
        // "every key reads the question and it should also emit a distinct earcon").
        ShowQuestionList(question.Signature, QuestionName(question), QuestionName(question, int.MaxValue), question, null, !UserActed(), AnswerQuestion, () =>
        {
            // A survey's Escape is its Dismiss: an Escape at Claude's prompt is no answer, and a second one rewinds.
            if (question.EscapeKey is { } key && question.OptionFor(key) is { } leave)
            {
                AnswerQuestion([leave]);
                return;
            }

            SendEscape();
            Announce("Cancelled", false);
        }, ReadQuestionAgain);
        return true;
    }

    /// <summary>
    /// /config's list of settings (docs/axClaude/claude-screens.md), read off Claude's screen each time it shows. Enter on a
    /// true-or-false setting asks for its value (<see cref="ShowSwitch"/>), since Claude flips it when its number is
    /// sent; on any other setting it sends the number and Claude's list of its values follows. Either way the list comes
    /// back on the setting just chosen. Escape saves and closes.
    /// </summary>
    private void ShowSettings(Question settings, bool back = false) =>
        ShowQuestionList(settings.Signature, "Claude's settings", "Claude's settings", settings, _settingKey, !back && !UserActed(), chosen =>
        {
            var setting = chosen[0];
            _settingKey = setting.Key;
            if (setting.Switch is { } on)
            {
                ShowSwitch(settings, setting, on);
            }
            else
            {
                SendAnswer(setting.Key);
            }
        }, () =>
        {
            _settingKey = null;
            SendEscape();
            Announce("Saved", false);
        });

    /// <summary>A true-or-false setting of /config: True and False, the current one first in focus. Only a change sends the setting's number.</summary>
    private void ShowSwitch(Question settings, QuestionOption setting, bool on)
    {
        var name = setting.Setting?.Name ?? setting.Text;
        var values = new Question(name, [], [new QuestionOption("1", "True", on), new QuestionOption("2", "False", !on)]);
        ShowQuestionList(settings.Signature, name, name, values, null, false, chosen =>
        {
            if (chosen[0].Current)
            {
                // Unchanged: nothing to send, and the settings come back on this one.
                ShowSettings(settings, back: true);
            }
            else
            {
                SendAnswer(setting.Key);
            }
        }, () => ShowSettings(settings, back: true));
    }

    /// <summary>Sends the chosen answer's key, or the ticked keys joined with commas, and says what comes next.</summary>
    private void AnswerQuestion(IReadOnlyList<QuestionOption> chosen)
    {
        // Over the stream, the answer of a tool call's question that only explains opens its text; the question
        // stays and nothing is sent (docs/axClaude/stream-mode.md SM-7.4).
        if (Streams && chosen.Count == 1 && StreamExplained(chosen[0].Key))
        {
            return;
        }

        // Other is no answer yet: Claude asks for the words next (ShowOwnAnswer), and the sound of sending comes once,
        // with them. Until 2026-11-21 it played here too, and one answer in the user's own words sounded like two
        // (the maintainer: "submitting my answers ... caused multiple submission noises to play").
        var wordsFollow = chosen.Any(option => option.Text.Equals(ClaudeCodeTexts.OtherAnswer, StringComparison.OrdinalIgnoreCase));
        SendAnswer(string.Join(",", chosen.Select(option => option.Key)), sound: !wordsFollow);
        // Each answer's name without Claude's explanation after the dash.
        var names = string.Join(", ", chosen.Select(option =>
        {
            var dash = option.Text.IndexOf(ClaudeCodeTexts.AnswerDash, StringComparison.Ordinal);
            return $"{option.Key}, {(dash > 0 ? option.Text[..dash] : option.Text)}";
        }));
        // The words are as they were ("Sent: 4, Other. Your words next"); only the sound waits for the words.
        Announce("Sent: " + names + (wordsFollow ? ". Your words next" : NextStep()), false);
    }

    /// <summary>After an answer to one of several questions, or to a question whose review follows, what comes next, so that the gap before it does not sound like the end.</summary>
    private string NextStep() => _askedQuestion switch
    {
        { Step: (int number, int count) } when number < count => ". Next question",
        // The review is a screen of Claude Code's own: the stream has none.
        { ReviewFollows: true } when _settings.ReviewAnswers && !Streams => ". Review next",
        _ => string.Empty,
    };

    /// <summary>
    /// Claude's "Review your answers" right after the user answered the question it follows (the last of several, or
    /// one alone with several answers: <see cref="Question.ReviewFollows"/>), which the app answers y itself unless
    /// Review answers is on (D33). Not when it warns that a question has no answer, and not a review the user came
    /// back to by some other way.
    /// </summary>
    private bool SubmitsReview(Question question) =>
        !_settings.ReviewAnswers
        && question.Title == ClaudeCodeTexts.ReviewTitle
        && question.OptionFor(ClaudeCodeTexts.Yes) is not null
        && !question.Text.Any(line => line.StartsWith(ClaudeCodeTexts.WarningLabel, StringComparison.Ordinal))
        && _askedQuestion is { ReviewFollows: true }
        && UserActed();

    /// <summary>
    /// The follow-up of a question when Claude asks for the user's own words ("Enter text for option 4 (Other), or
    /// Escape for the list:", also plan approval's "No, keep planning"): a field named with the question and the
    /// answer chosen. Enter sends the words, a keystroke each, and Escape goes back to Claude's list.
    /// </summary>
    private void ShowOwnAnswer(string prompt)
    {
        var chosen = LineClassifier.TextPromptAnswer(prompt) ?? "an answer that asks for your own words";
        var asked = _askedQuestion is { } question ? QuestionName(question) : null;
        var name = (asked is null ? string.Empty : asked + Stop(asked)) + $"You chose {chosen}. Your answer";
        ShowInArea(prompt, name, !UserActed(), (focus, hold) => Area.ShowField(name, focus, hold, typed =>
        {
            var words = typed.Trim();
            if (words.Length == 0)
            {
                Announce("Type an answer", true);
                return;
            }

            SendAnswer(words);
            Announce("Sent: " + words + NextStep(), false);
        }, SendEscape));
    }

    /// <summary>
    /// A screen of Claude's that waits on its hint row (/status, /tasks, /help …; D33) as a list: its lines, which the
    /// conversation does not show for a tab screen, then an item for every key the hint row names ("View (Enter)",
    /// "Close (Escape)"). Enter on a key's item sends the key, Escape sends Escape; the screen Claude shows next takes
    /// the list's place, without the chime since the user caused it. What the keys do is <see cref="ScreenActions"/>,
    /// tested: the screen's keys work from anywhere in the list, as in Claude, where only its highlight counts; Enter
    /// on one of the screen's lines does what the hint row says Enter does, or leaves a screen whose Escape closes
    /// it, or names the actions; a letter the hint row names (d to discard) goes to Claude from any item.
    /// </summary>
    private void ShowClaudeScreen(ClaudeScreen screen)
    {
        var items = ScreenActions.Items(screen);
        var first = screen.Lines.Count;
        var leave = ScreenActions.LeaveKey(screen);
        void Leave()
        {
            SendScreenKey(leave.Send);
            if (ScreenActions.Closes(leave))
            {
                Announce("Closed", false);
            }
        }

        void EnterOnLine()
        {
            var key = ScreenActions.EnterOnLine(screen);
            if (key is null)
            {
                Announce(ScreenActions.ActionsText(screen), true);
            }
            else if (ReferenceEquals(key, leave))
            {
                Leave();
            }
            else
            {
                SendScreenKey(key.Send);
            }
        }

        bool Letter(char letter)
        {
            if (ScreenActions.LetterKey(screen, letter) is not { } key)
            {
                return false;
            }

            SendScreenKey(key.Send);
            return true;
        }

        // A search is named with the window's words, its first line the search row ("Search: git").
        var name = screen.Searching ? ScreenActions.SearchName : screen.Title;
        var text = new StringBuilder(name);
        foreach (var line in screen.Lines)
        {
            text.Append(Stop(text.ToString())).Append(line);
        }

        if (screen.Editing)
        {
            ShowEditedRow(screen, text.ToString());
            return;
        }

        if (screen.Searching && !screen.SearchTyped)
        {
            ShowSearchField(screen, text.ToString());
            return;
        }

        if (ScreenActions.IsInformation(screen))
        {
            ShowScreenNotice(screen, text.ToString());
            return;
        }

        ShowInArea(screen.Signature, text.ToString(), !UserActed(), (focus, hold) =>
            Area.ShowList(name, items, 0, null, focus, hold, (index, _) =>
            {
                if (index < first)
                {
                    EnterOnLine();
                    return;
                }

                SendScreenKey(screen.Keys[index - first].Send);
            }, Leave, Letter));
    }

    /// <summary>
    /// A text row of a draft's review that Claude has opened for editing (D33, <see cref="ClaudeScreen.Editing"/>):
    /// a field in the control area named with the row. Enter sends what was typed to the end of Claude's text, a
    /// keystroke at a time, then Enter, which keeps it and moves the highlight on; an empty field only moves on;
    /// Escape cancels the edit. Ctrl+Q reads the review with the row's text as it stands. Nothing is deleted from
    /// here: a draft is reworded by discarding it and asking Claude again.
    /// </summary>
    private void ShowEditedRow(ClaudeScreen screen, string text)
    {
        var row = screen.Highlighted?.TrimEnd(ClaudeCodeTexts.FieldNameEnd) ?? "Text";
        var name = $"{screen.Title}. {row}, add at the end";
        ShowInArea(screen.Signature, text, !UserActed(), (focus, hold) => Area.ShowField(name, focus, hold, typed =>
        {
            ScreenKeySent();
            TypeAnswer(typed);
        }, () => SendScreenKey(ClaudeCodeKeys.Escape)));
    }

    /// <summary>
    /// The permissions screen's search row with Claude Code's cursor on it, nothing typed yet (<see cref="ClaudeScreen.Searching"/>,
    /// the item Search the rules sends "/"): a field in the control area, "Search the rules, type and press Enter". Enter
    /// types the text into Claude Code's search a keystroke at a time, and the rules that match come back as the
    /// screen's list (<see cref="ClaudeScreen.SearchTyped"/>); an empty field goes into the whole list (Down, where
    /// Claude Code asks for a rule by its number); Escape goes back. Without this field the message field came back
    /// over Claude Code's search, which took the next message as its text.
    /// </summary>
    private void ShowSearchField(ClaudeScreen screen, string text)
    {
        var name = ScreenActions.SearchFieldName;
        ShowInArea(screen.Signature, text, !UserActed(), (focus, hold) => Area.ShowField(name, focus, hold, typed =>
        {
            if (typed.Length > 0)
            {
                ScreenKeySent();
                TypeAnswer(typed, enter: false);
            }
            else if (ScreenActions.SearchDownKey(screen) is { } down)
            {
                SendScreenKey(down.Send);
            }
            else
            {
                Announce("Type the words to search for", true);
            }
        }, () => SendScreenKey(ClaudeCodeKeys.Escape)));
    }

    // ---- a screen that only informs, as read-only text in a notice (FR-2.11) ----

    /// <summary>The notice that shows an information screen; null while none shows.</summary>
    private Notice? _screenNotice;


    /// <summary>The notice of an information screen is the one showing (an error or an update offer may have replaced it).</summary>
    private bool ScreenNoticeOpen => _screenNotice is not null && NoticeOpen && ReferenceEquals(Overlay.Shown, _screenNotice);

    /// <summary>The information screen's notice shows text that Claude's screen no longer has (a scroll, the answer arriving): the settle refreshes it.</summary>
    private bool ScreenNoticeOutdated => ScreenNoticeShows(_model.PendingScreen) && ScreenNoticeText(_model.PendingScreen!) != _screenNotice!.Text;

    /// <summary>The lines the information screen's notice shows: the screen's rows joined with what was shown before when they overlap, so that a long answer keeps the rows that went off the top (<see cref="ScreenActions.Merge"/>).</summary>
    private IReadOnlyList<string> _screenLines = [];

    private IReadOnlyList<string> ScreenNoticeLines(ClaudeScreen screen) => ScreenActions.Merge(_screenLines, screen.Lines);

    private string ScreenNoticeText(ClaudeScreen screen) => string.Join("\n", ScreenNoticeLines(screen));

    /// <summary>
    /// The notice showing is this information screen's: the same title. Not the whole signature, since the keys change
    /// as the user browses (a second /btw offers ⇧← only at its oldest answer): the notice keeps the buttons it opened
    /// with, and only its text follows the screen.
    /// </summary>
    private bool ScreenNoticeShows(ClaudeScreen? screen) =>
        ScreenNoticeOpen && screen is not null && ScreenActions.IsInformation(screen) && screen.Title == _screenNotice!.Title;

    /// <summary>
    /// A screen of Claude's that only informs (/btw's side answer, /goal; FR-2.11, <see cref="ScreenActions.IsInformation"/>):
    /// a read-only notice, the screen's title and lines, with Close (Enter and Escape) and a button per other key of
    /// its hint row. The same screen again (after a key, or with new lines) replaces the notice's text in place. The
    /// notice opens by the control area's rules (<see cref="ShowInArea"/>): at once from the bottom of the window,
    /// interrupting under Go to them at once; after a moment from the conversation or in the background; once
    /// another notice or a menu has closed.
    /// </summary>
    private void ShowScreenNotice(ClaudeScreen screen, string summary)
    {
        var signature = screen.Signature;
        if (ScreenNoticeShows(screen))
        {
            var lines = ScreenNoticeLines(screen);
            var text = string.Join("\n", lines);
            if (text != _screenNotice!.Text)
            {
                _screenLines = lines;
                Overlay.SetBody(text);
                _screenNotice = Overlay.Shown;
            }

            _wait = new Wait(signature, Shown: true, summary);
            return;
        }

        // Another information screen in place of the one shown: that one's notice goes first.
        CloseScreenNotice();
        var unprompted = !UserActed();
        var bottom = Area.ContainsFocus || Input.ContainsFocus;
        var interrupt = !NoticeOpen && (unprompted ? _settings.InterruptForQuestions : bottom);
        if (Area.Current is not null)
        {
            // What the area showed before is over: the field comes back under the notice.
            CloseArea();
        }

        if (unprompted && _settings.SoundOnBell && !(_settings.BackgroundAlert && !ContainsFocus))
        {
            EventDoor.Raise(EventKind.ProgramScreen);
        }

        if (!NoticeOpen && (interrupt || bottom))
        {
            OpenScreenNotice(screen, summary, interrupt, unprompted);
            return;
        }

        // From the conversation, in the background, or behind another notice or a menu: after a moment, and again
        // until nothing is in the way, while Claude still shows the screen.
        void TryOpen()
        {
            if (_model.PendingScreen is not { } now || now.Signature != signature || _wait is not { Shown: false } wait || wait.Signature != signature)
            {
                return;
            }

            if (NoticeOpen || !(Transcript.ContainsFocus || Input.ContainsFocus || Area.ContainsFocus || !ContainsFocus))
            {
                After(QuestionFocusMs, TryOpen);
                return;
            }

            OpenScreenNotice(now, summary, interrupt: false, unprompted);
        }

        After(QuestionFocusMs, TryOpen);
    }

    /// <summary>Opens the information screen's notice: Close, a button per other key, the scroll buttons when the screen may hold more than the console shows.</summary>
    private void OpenScreenNotice(ClaudeScreen screen, string summary, bool interrupt, bool unprompted)
    {
        var signature = screen.Signature;
        var leave = ScreenActions.LeaveKey(screen);
        var choices = new List<OverlayChoice>
        {
            new(ScreenActions.LeaveButton(leave), () =>
            {
                _screenNotice = null;
                if (_wait is { } wait)
                {
                    _wait = wait with { Shown = false, Text = null };
                }

                SendScreenKey(leave.Send);
                if (ScreenActions.Closes(leave))
                {
                    Announce("Closed", false);
                }
            }, IsDefault: true, IsCancel: true),
        };
        // A key of the screen's own (Copy, Fork, Browse) may change the answer shown: the text starts afresh from the
        // screen's rows rather than being joined with the answer before (ScreenActions.Merge).
        var keys = ScreenActions.NoticeKeys(screen);
        void SendNoticeKey(ScreenKey key)
        {
            _screenLines = [];
            SendScreenKey(key.Send);
        }

        choices.AddRange(keys.Select(key => new OverlayChoice(ScreenActions.KeyItem(key), () => SendNoticeKey(key), StaysOpen: true)));
        if (ScreenActions.Scrolls(screen))
        {
            choices.Add(new OverlayChoice("Scroll &down", () => SendScreenKey(ClaudeCodeKeys.Down), StaysOpen: true));
            choices.Add(new OverlayChoice("Scroll &up", () => SendScreenKey(ClaudeCodeKeys.Up), StaysOpen: true));
        }

        _screenLines = screen.Lines;
        var notice = new Notice(screen.Title, ScreenActions.NoticeText(screen), choices)
        {
            Unprompted = unprompted,
            // The chime is the area's rule, not the notice's (FR-2.11).
            Silent = true,
            // The hint row's letters typed in the text, as the buttons (c to copy, f to fork; FR-2.11).
            Letter = letter =>
            {
                if (ScreenActions.LetterKey(screen, letter) is not { } key || !keys.Contains(key))
                {
                    return false;
                }

                SendNoticeKey(key);
                return true;
            },
        };
        ShowNotice(notice);

        _screenNotice = Overlay.Shown;
        _wait = new Wait(signature, Shown: true, summary);
        // A screen that only informs is shown as this notice, where another would be shown in the control area.
        FlowQuestionShown();
        if (interrupt)
        {
            // As a question's: after NVDA has taken the focus, so that it comes last and cuts off what was read.
            var spoken = (screen.Lines.Count == 0 ? screen.Title : screen.Title + Stop(screen.Title) + screen.Lines[0]).TrimEnd(' ', '.');
            var shown = _screenNotice;
            var actedBefore = _userActedAt;
            After(FocusSettleMs, () =>
            {
                if (ScreenNoticeOpen && ReferenceEquals(_screenNotice, shown) && _userActedAt == actedBefore)
                {
                    Announce(spoken, true);
                }
            });
        }
    }

    /// <summary>The information screen's notice closes, when Claude closed the screen or showed something else; nothing when it is not the notice showing.</summary>
    private void CloseScreenNotice()
    {
        if (ScreenNoticeOpen)
        {
            CloseNotice(null);
        }

        _screenNotice = null;
        _screenLines = [];
    }

    /// <summary>Sends a screen's key. The screen that shows next opens again even when its title and keys are the same (a moved selection, another tab).</summary>
    private void SendScreenKey(string key)
    {
        ScreenKeySent();
        Write(key);
    }

    /// <summary>The bookkeeping of a key sent from a screen, or of text typed into one of its rows: what Claude shows next is the user's doing, the area holds its keys meanwhile, and the screen shown next replaces this one even when it looks the same.</summary>
    private void ScreenKeySent()
    {
        UserActedNow();
        if (_wait is { } wait)
        {
            _wait = wait with { Stale = true };
        }

        Area.Hold(UserActionMs);
        // A key that changes nothing draws no frame: the timer brings the screen back all the same.
        _attention.Stop();
        _attention.Start();
        FlowActed(UserAction.ScreenKey);
    }

    /// <summary>
    /// Escape from the control area: the question closes, or goes back a step. What Claude shows next is the user's
    /// doing, and the area holds its keys until then, so that a second Escape does not reach Claude as an interrupt.
    /// </summary>
    private void SendEscape()
    {
        UserActedNow();
        Area.Hold(UserActionMs);
        if (Streams)
        {
            // Over the stream Escape is no to what Claude Code asks, or the way back from the user's own words to the
            // question's list (docs/axClaude/stream-mode.md SM-5.3, SM-7.5).
            StreamEscape();
            return;
        }

        Write(ClaudeCodeKeys.Escape);
        FlowActed(UserAction.Escaped);
    }

    /// <summary>
    /// An answer through the send queue: each character as a keystroke of its own, then Enter, except for a question
    /// answered by its key alone (a feedback survey, <see cref="Question.KeyOnly"/>), where Claude takes the key after
    /// a pause and an Enter would go to its prompt. The control area holds its keys meanwhile, so that a second Enter
    /// does not send it again. <paramref name="sound"/> false for a choice that is not the answer yet (Other, whose
    /// words follow): the sound of sending plays once for one answer.
    /// </summary>
    private void SendAnswer(string key, bool sound = true)
    {
        if (!ProcessRunning)
        {
            return;
        }

        if (Streams)
        {
            // Over the stream an answer goes back through the relay (docs/axClaude/stream-mode.md SM-5.2): nothing is typed.
            StreamAnswer(key);
        }
        else
        {
            TypeAnswer(key, enter: _model.PendingQuestion is not { KeyOnly: true });
        }

        UserActedNow();
        FlowActed(UserAction.Answered);
        Area.Hold(UserActionMs);
        if (sound && _settings.SoundOnBell)
        {
            EventDoor.Raise(EventKind.Sent);
        }
    }

    /// <summary>
    /// Types an answer: one keystroke per character (per rune, so an emoji stays whole), since Claude's answer field
    /// takes typed keys and several characters in one write are a paste, which it ignores ("12" in a long list, "1,3"
    /// for several answers). Then Enter. Without the sound and the hold of <see cref="SendAnswer"/>: an answer the app
    /// gives on the user's behalf, which what follows announces as new.
    /// </summary>
    private void TypeAnswer(string key, bool enter = true)
    {
        var keys = key.EnumerateRunes().Select(rune => rune.ToString()).ToList();
        for (var i = 0; i < keys.Count; i++)
        {
            Enqueue(keys[i], i == keys.Count - 1 ? 100 : 60);
        }

        if (enter)
        {
            Enqueue(ClaudeCodeKeys.Enter, 50);
        }

        StartWrites();
    }

    // ---- a feedback draft Claude queued (D33) ----

    /// <summary>
    /// A feedback draft Claude has queued in the turn that just ended, counted on its mode line ("1 feedback draft",
    /// D33, <see cref="FeedbackDraftWatch"/>): /feedback opens for it once Claude is idle, after a short settle, and
    /// Claude's list of drafts is answered with the draft's title (<see cref="AnswerDraftsList"/>), so that its
    /// review comes straight up as a screen in the control area, like a question. The card Claude prints for the
    /// draft has keys that do nothing in screen reader mode; its rows are chrome. Drafts counted at startup or left
    /// by other sessions, and a draft put off with Later, wait for /feedback by hand. Not with Answer from a list off,
    /// when the card stays in the conversation and /feedback is typed.
    /// </summary>
    private void OpenDraftsWhenIdle()
    {
        if (!_readyAnnounced || !ProcessRunning)
        {
            return;
        }

        var idle = ClaudeIdle;
        if (idle && !UserActed() && _draft is { Step: not DraftStep.Opening } draft && Environment.TickCount64 - draft.At >= UserActionMs)
        {
            // The list never came, or the review ended without the list: nothing of the draft's is on its way.
            _draft = null;
        }

        if (!_drafts.Drafted(ClaudeWorks, idle, _model.FeedbackDrafts))
        {
            return;
        }

        if (!_settings.QuestionNotices || !_settings.OpenFeedbackDrafts)
        {
            _drafts.Opened(_model.FeedbackDrafts);
        }
        else if (_draft is null)
        {
            _draft = new Draft(DraftStep.Opening, null, Environment.TickCount64);
            After(DraftSettleMs, OpenFeedbackDrafts);
        }
    }

    /// <summary>Claude runs, waits on nothing, and no write of the user's is on its way to it.</summary>
    private bool ClaudeIdle => ProcessRunning && !ClaudeWorks && !_model.PromptPending && !WritesPending;

    /// <summary>Sends /feedback as the user's own command would go, without "Sent": the review of the draft announces itself. Skipped when Claude is no longer idle.</summary>
    private void OpenFeedbackDrafts()
    {
        if (!ClaudeIdle || Area.Current is not null || !_drafts.Drafted(ClaudeWorks, idle: true, _model.FeedbackDrafts))
        {
            _draft = null;
            return;
        }

        _drafts.Opened(_model.FeedbackDrafts);
        _draft = new Draft(DraftStep.Listing, _model.DraftTitle, Environment.TickCount64);
        _lastCommand = SlashCommands.Find(ClaudeCodeTexts.FeedbackCommand);
        QueueWrites(ClaudeCodeTexts.FeedbackCommand);
    }

    /// <summary>
    /// Claude's list of drafts when the app opened /feedback itself (D33): answered with the draft's number, so that
    /// the review comes straight up, or, when the review is over (Send, Discard or Later brings the list back),
    /// closed with Escape, back to the message field; neither flashes the taskbar nor is followed by "Question
    /// closed", being the app's doing. False when the list is the user's own, or the draft is not on it: the list is shown.
    /// </summary>
    private bool AnswerDraftsList(Question question)
    {
        if (question.Title != FeedbackDraftWatch.ListTitle || _draft is null)
        {
            return false;
        }

        if (_draft.Step == DraftStep.Reviewing)
        {
            _draft = null;
            Handled(question.Signature, mark: false);
            UserActedNow();
            Write(ClaudeCodeKeys.Escape);
            FlowActed(UserAction.Escaped);
            return true;
        }

        if (_draft.Step != DraftStep.Listing)
        {
            return false;
        }

        var title = _draft.Title;
        _draft = null;
        if (title is null || FeedbackDraftWatch.Find(question, title) is not { } draft)
        {
            return false;
        }

        // The review that follows is new to the user: it comes with the trill, so the stamp is not touched here.
        _draft = new Draft(DraftStep.Reviewing, title, Environment.TickCount64);
        Handled(question.Signature, mark: false);
        TypeAnswer(draft.Key);
        return true;
    }
}
