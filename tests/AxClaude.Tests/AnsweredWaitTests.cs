using AxClaude.Core.Transcript;
using AxKit.Core.Pty;

namespace AxClaude.Tests;

/// <summary>
/// The window's rules around the user's own answer to what Claude Code's screen shows (<see cref="AnsweredWait"/>,
/// SPEC.md D33; the review of 2026-11-27): when the question answered is over, so that the next one is shown and said
/// though it reads the same, and when the settle waits. The window's part of it is played on the stand-in by
/// tests/scripts/axClaude/61-screen-same-question.txt.
/// </summary>
public sealed class AnsweredWaitTests
{
    private static Question Create(string file) => QuestionReader.Read(
    [
        "Permission Required: Create file",
        file,
        $"Do you want to create {file}?",
        "1. Yes",
        "2. Yes, allow all edits during this session",
        "3. No",
        "Select with numbers [1-3]. Then Enter to submit or Escape to cancel:",
    ], 6)!;

    [Fact]
    public void An_answered_question_is_over_when_anything_else_stands_in_its_place()
    {
        var first = Create("alpha.txt");
        var second = Create("beta.txt");

        // Still drawn as it was while Claude Code takes the keys: not over, or it would be asked twice.
        Assert.False(AnsweredWait.Over(first.Signature, first.Signature, first.Content, Create("alpha.txt").Content));
        // The second file's question, under the same title with the same answers: over.
        Assert.Equal(first.Signature, second.Signature);
        Assert.True(AnsweredWait.Over(first.Signature, second.Signature, first.Content, second.Content));
        // Nothing waits (the frame between the two), or something else does: over.
        Assert.True(AnsweredWait.Over(first.Signature, null, first.Content, null));
        Assert.True(AnsweredWait.Over(first.Signature, "Claude's settings", first.Content, null));
        // What was answered is no question of Claude's (a screen, the row for the user's own words): by its signature alone.
        Assert.False(AnsweredWait.Over("Enter text for option 4 (Other), or Escape for the list:", "Enter text for option 4 (Other), or Escape for the list:", null, null));
        Assert.False(AnsweredWait.Over(first.Signature, first.Signature, null, first.Content));
    }

    [Fact]
    public void The_settle_waits_while_the_users_keys_are_still_going_out()
    {
        // An answer of seventy characters is typed out over four seconds; a row of /feedback or a search the same way.
        // The settle of 400 ms came in the middle of it and showed the row half typed (until 2026-11-27).
        const int settle = 400;
        Assert.True(AnsweredWait.SettleWaits(now: 1_000, lastKeyAt: 5_300, settle, answeredStandsAsItStood: false));
        Assert.True(AnsweredWait.SettleWaits(now: 5_299, lastKeyAt: 5_300, settle, answeredStandsAsItStood: false));
        Assert.False(AnsweredWait.SettleWaits(now: 5_300, lastKeyAt: 5_300, settle, answeredStandsAsItStood: false));
        // Nothing of the user's on its way: never.
        Assert.False(AnsweredWait.SettleWaits(now: 9_000, lastKeyAt: 0, settle, answeredStandsAsItStood: false));
    }

    [Fact]
    public void An_answered_question_that_stands_as_it_stood_is_left_a_settles_time_after_the_last_key()
    {
        // Claude Code may draw the question again while it takes the answer (its rows are blank for a frame), which
        // reads as the question gone and come back: it is not shown again before Claude Code had its time.
        const int settle = 400;
        Assert.True(AnsweredWait.SettleWaits(now: 5_300, lastKeyAt: 5_300, settle, answeredStandsAsItStood: true));
        Assert.True(AnsweredWait.SettleWaits(now: 5_699, lastKeyAt: 5_300, settle, answeredStandsAsItStood: true));
        Assert.False(AnsweredWait.SettleWaits(now: 5_700, lastKeyAt: 5_300, settle, answeredStandsAsItStood: true));
    }

    /// <summary>
    /// The second review of 2026-11-27: with the screen, a "1" typed in the message field, a bare Enter, a screen's
    /// key or a list's Enter went to whatever Claude Code's screen showed in the 400 ms before the window had put it
    /// to the user. While this says true, the window sends none of them.
    /// </summary>
    [Fact]
    public void What_waits_is_unheard_until_the_window_has_shown_or_announced_it()
    {
        var first = Create("alpha.txt");
        // A question has come and the settle has yet to act on it: nothing was shown or said for it.
        Assert.True(AnsweredWait.Unheard(first.Signature, handled: null, over: false));
        // Shown in the area, or announced with "Claude asks" (Answer from a list off): heard, and answers go.
        Assert.False(AnsweredWait.Unheard(first.Signature, first.Signature, over: false));
        // Something else stands there now than what the window showed last (a screen replaced by a question).
        Assert.True(AnsweredWait.Unheard(first.Signature, "Status\nescape", over: false));
        // What was shown is over (answered, or replaced under the same title): the one that stands now was not said.
        Assert.True(AnsweredWait.Unheard(first.Signature, first.Signature, over: true));
        // Nothing waits: Claude Code's prompt takes the message field's Enter as ever.
        Assert.False(AnsweredWait.Unheard(null, handled: null, over: false));
        Assert.False(AnsweredWait.Unheard(null, first.Signature, over: true));
    }

    /// <summary>
    /// The second review of 2026-11-27: a wait marked answered stayed so for good. A key Claude Code did not take as
    /// an answer (its own mode key at a file question) left the question standing, answered in the window's record,
    /// and a single blank frame minutes later read as "the answered question has left": it was asked again.
    /// </summary>
    [Fact]
    public void A_key_stops_counting_as_an_answer_when_the_question_still_stands_three_seconds_later()
    {
        const int counts = 3000;
        // Within the three seconds Claude Code may still take it.
        Assert.False(AnsweredWait.AnswerLapsed(now: 12_999, lastKeyAt: 10_000, counts, over: false));
        // After them the question still stands as it stood: the key answered nothing.
        Assert.True(AnsweredWait.AnswerLapsed(now: 13_000, lastKeyAt: 10_000, counts, over: false));
        // An answer typed out a key at a time counts from its last key, which lies ahead while they go out.
        Assert.False(AnsweredWait.AnswerLapsed(now: 13_000, lastKeyAt: 14_200, counts, over: false));
        // What stands now is something else, or nothing: the answer was taken, however late, and the wait is over.
        Assert.False(AnsweredWait.AnswerLapsed(now: 60_000, lastKeyAt: 10_000, counts, over: true));
    }

    /// <summary>
    /// The second review of 2026-11-27: only an answered question was compared with what stands in its place. A file
    /// question Claude Code took back and replaced by the next file's, unanswered, kept its list.
    /// </summary>
    [Fact]
    public void An_unanswered_question_replaced_by_one_about_something_else_is_another_question()
    {
        var first = Create("alpha.txt");
        var second = Create("beta.txt");
        Assert.Equal(first.Signature, second.Signature);
        Assert.True(AnsweredWait.Replaced(first.Signature, second.Signature, first.Content, second.Content));
        // The same question drawn again: the same question still.
        Assert.False(AnsweredWait.Replaced(first.Signature, first.Signature, first.Content, Create("alpha.txt").Content));
        // Another title or other answers: that is seen by the signature, at the settle, as ever.
        Assert.False(AnsweredWait.Replaced(first.Signature, "Claude's settings", first.Content, null));
        Assert.False(AnsweredWait.Replaced(first.Signature, null, first.Content, null));
    }

    private static Question Command(params string[] rows) => QuestionReader.Read(
    [
        "Permission Required: Bash command",
        .. rows,
        "Do you want to proceed?",
        "1. Yes",
        "2. No",
        "Select with numbers [1-2]. Then Enter to submit or Escape to cancel:",
    ], rows.Length + 4)!;

    /// <summary>
    /// The third review of 2026-11-27: the two were compared by the three words of the label alone, so a command
    /// replaced by the same command with one word more was one question, and its list stayed over the other command.
    /// Everything the question shows between its title and its answers is compared now. A tip Claude Code puts
    /// inside a question is no such row (the reader passes it over), so it does not make the question another.
    /// </summary>
    [Fact]
    public void A_command_replaced_by_one_that_differs_past_its_third_word_is_another_question()
    {
        var push = Command("git push origin main");
        var force = Command("git push origin main --force");
        Assert.Equal(push.Signature, force.Signature);
        Assert.Equal(push.Core, force.Core);
        Assert.True(AnsweredWait.Replaced(push.Signature, force.Signature, push.Content, force.Content));
        // A second line under the same first line, too.
        Assert.True(AnsweredWait.Replaced(push.Signature, push.Signature, push.Content, Command("git push origin main", "rm -rf build").Content));
        var tipped = Command("Tip: auto mode handles these prompts for you", "git push origin main");
        Assert.False(AnsweredWait.Replaced(push.Signature, tipped.Signature, push.Content, tipped.Content));
    }

    /// <summary>
    /// The third review of 2026-11-27 (M3): a list or a field answers only while what it was shown for is what waits,
    /// and has been put to the user. "Nothing waits" is not "unheard": until that day the list of a question Claude
    /// Code had taken back still answered, and its key and Enter were typed to the prompt as a message.
    /// </summary>
    [Fact]
    public void A_control_answers_only_while_what_it_was_shown_for_still_waits()
    {
        var first = Create("alpha.txt");
        Assert.True(AnsweredWait.StillWaits(first.Signature, first.Signature, first.Signature, over: false));
        // Claude Code took the question back and waits on nothing: the list still stands for 400 ms, and answers nothing.
        Assert.False(AnsweredWait.StillWaits(first.Signature, waiting: null, first.Signature, over: false));
        // Something else waits, heard or not.
        Assert.False(AnsweredWait.StillWaits(first.Signature, "Status\nescape", first.Signature, over: false));
        Assert.False(AnsweredWait.StillWaits(first.Signature, "Status\nescape", "Status\nescape", over: false));
        // The same title and answers, and what was shown is over: the question that stands was not said yet.
        Assert.False(AnsweredWait.StillWaits(first.Signature, first.Signature, first.Signature, over: true));
        Assert.False(AnsweredWait.StillWaits(first.Signature, first.Signature, handled: null, over: false));
    }

    /// <summary>
    /// The third review of 2026-11-27 (M4): what is queued to be typed is asked about again when its turn comes. A
    /// message's Enter goes a tenth of a second after its text; a permission question drawn in between took it.
    /// </summary>
    [Fact]
    public void A_queued_message_is_over_when_Claude_has_begun_to_ask_since_it_was_queued()
    {
        var question = "question\n" + Create("alpha.txt").Content;
        var other = "question\n" + Create("beta.txt").Content;
        Assert.True(QueuedWrite.MessageGoes(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null));
        Assert.False(QueuedWrite.MessageGoes(askedThen: false, waitedFor: null, asksNow: true, waitingNow: question));
        // Sent into a question on purpose (the kept message sent again, "Enter again sends"): it goes into that one.
        Assert.True(QueuedWrite.MessageGoes(askedThen: true, waitedFor: question, asksNow: true, waitingNow: question));
        // Bound to it by its identity (the fourth review of 2026-11-27, N3): not into the question that replaced it,
        // nor once nothing is asked, when its Enter would go to the prompt as a message the user did not send so.
        Assert.False(QueuedWrite.MessageGoes(askedThen: true, waitedFor: question, asksNow: true, waitingNow: other));
        Assert.False(QueuedWrite.MessageGoes(askedThen: true, waitedFor: question, asksNow: false, waitingNow: null));
        // A question the window could not read is no other one (the fifth review of 2026-11-27, L4): until then two
        // such were taken for the same, and a kept message went into whatever unreadable question came next.
        Assert.False(QueuedWrite.MessageGoes(askedThen: true, waitedFor: null, asksNow: true, waitingNow: null));
        Assert.False(QueuedWrite.MessageGoes(askedThen: true, waitedFor: null, asksNow: true, waitingNow: question));
    }

    /// <summary>
    /// A message's Enter only on a positive check (the fourth review of 2026-11-27, N3, M2): the frame shows the
    /// message on Claude Code's prompt row and nothing is asked. Until then it went unless a question had been seen,
    /// and the window reads the screen only as of the last frame it took in. Not yet on the row: the Enter waits for
    /// the check of a dropped Enter, which asks the same. Since the fifth review of that day (M1) "not yet" is an
    /// answer of its own, never "over": the Enter stays first in the queue and what stands behind it waits; over only
    /// once it has waited its longest, or when Claude Code began to ask.
    /// </summary>
    [Fact]
    public void A_messages_Enter_goes_only_when_the_prompt_row_shows_it_and_nothing_is_asked()
    {
        const int longest = 3000;
        var question = "question\n" + Create("alpha.txt").Content;
        Assert.Equal(WriteCheck.Goes, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, promptHolds: true, waitedMs: 0, longest));
        // The old check let this go: nothing seen asked, and nothing seen on the row either. Not yet, until the longest wait.
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, promptHolds: false, waitedMs: 0, longest));
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, promptHolds: false, waitedMs: 2999, longest));
        Assert.Equal(WriteCheck.Over, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, promptHolds: false, waitedMs: 3000, longest));
        Assert.Equal(WriteCheck.Goes, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, promptHolds: true, waitedMs: 2999, longest));
        // Claude Code began to ask: over at once, whatever the row shows.
        Assert.Equal(WriteCheck.Over, QueuedWrite.MessageEnter(askedThen: false, waitedFor: null, asksNow: true, waitingNow: question, promptHolds: true, waitedMs: 0, longest));
        // A message sent into a question on purpose goes while that very question stands, whatever its row shows.
        Assert.Equal(WriteCheck.Goes, QueuedWrite.MessageEnter(askedThen: true, waitedFor: question, asksNow: true, waitingNow: question, promptHolds: false, waitedMs: 0, longest));
        Assert.Equal(WriteCheck.Over, QueuedWrite.MessageEnter(askedThen: true, waitedFor: null, asksNow: true, waitingNow: null, promptHolds: false, waitedMs: 0, longest));

    }

    /// <summary>
    /// The sixth review of 2026-11-27 (F1): a message is in flight until Claude Code has taken it. Its first write goes
    /// only onto a bare prompt row, and waits for one; the row is emptied of a message given back only while it holds
    /// exactly that; after its Enter the Enter goes once more while the row still holds it, and the queue waits until
    /// the row is bare again, then gives the message back.
    /// </summary>
    [Fact]
    public void A_message_is_in_flight_until_Claude_Code_has_taken_it()
    {
        const int longest = 3000;
        var question = "question\n" + Create("alpha.txt").Content;
        // The first write: a bare row goes; a row that holds another message or is not drawn waits, then is over.
        Assert.Equal(WriteCheck.Goes, QueuedWrite.MessageFirst(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, draft: string.Empty, waitedMs: 0, longest));
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.MessageFirst(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, draft: "first message", waitedMs: 0, longest));
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.MessageFirst(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, draft: null, waitedMs: 2999, longest));
        Assert.Equal(WriteCheck.Over, QueuedWrite.MessageFirst(askedThen: false, waitedFor: null, asksNow: false, waitingNow: null, draft: "first message", waitedMs: 3000, longest));
        Assert.Equal(WriteCheck.Over, QueuedWrite.MessageFirst(askedThen: false, waitedFor: null, asksNow: true, waitingNow: question, draft: string.Empty, waitedMs: 0, longest));
        // Sent into a question on purpose: the question's keys, whatever a row shows.
        Assert.Equal(WriteCheck.Goes, QueuedWrite.MessageFirst(askedThen: true, waitedFor: question, asksNow: true, waitingNow: question, draft: null, waitedMs: 0, longest));

        // The row emptied of a message given back: only while it holds exactly that text; else nothing is cleared.
        Assert.Equal(WriteCheck.Goes, QueuedWrite.ClearGivenBack(messageGoes: true, rowHoldsIt: true));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.ClearGivenBack(messageGoes: true, rowHoldsIt: false));
        Assert.Equal(WriteCheck.Over, QueuedWrite.ClearGivenBack(messageGoes: false, rowHoldsIt: true));

        // The Enter again: once, a second and a half after the Enter, while the row still holds the message.
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.EnterAgain(askedThen: false, asksNow: false, promptHolds: true, hides: false, sinceEnterMs: 1499, againMs: 1500));
        Assert.Equal(WriteCheck.Goes, QueuedWrite.EnterAgain(askedThen: false, asksNow: false, promptHolds: true, hides: false, sinceEnterMs: 1500, againMs: 1500));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.EnterAgain(askedThen: false, asksNow: false, promptHolds: false, hides: false, sinceEnterMs: 0, againMs: 1500));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.EnterAgain(askedThen: false, asksNow: true, promptHolds: true, hides: false, sinceEnterMs: 1500, againMs: 1500));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.EnterAgain(askedThen: true, asksNow: true, promptHolds: false, hides: false, sinceEnterMs: 1500, againMs: 1500));
        // K4 (the seventh review): never a second Enter for a text with a hidden character.
        Assert.Equal(WriteCheck.Skip, QueuedWrite.EnterAgain(askedThen: false, asksNow: false, promptHolds: true, hides: true, sinceEnterMs: 1500, againMs: 1500));

        // Taken: once the row is bare; until then everything behind it waits.
        Assert.Equal(WriteCheck.Skip, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: string.Empty, promptHolds: false, sinceEnterMs: 0, longest));
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: "first message", promptHolds: true, sinceEnterMs: 2999, longest));
        Assert.Equal(WriteCheck.NotYet, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: null, promptHolds: false, sinceEnterMs: 0, longest));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.Taken(askedThen: true, asksNow: true, heldBeforeAsk: true, draft: null, promptHolds: false, sinceEnterMs: 0, longest));
        // K1 (the seventh review): at the limit, given back only while the row still shows the message; a row not
        // drawn, or holding other text, counts as taken.
        Assert.Equal(WriteCheck.Over, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: "first message", promptHolds: true, sinceEnterMs: 3000, longest));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: null, promptHolds: false, sinceEnterMs: 3000, longest));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.Taken(askedThen: false, asksNow: false, heldBeforeAsk: false, draft: "something else", promptHolds: false, sinceEnterMs: 3000, longest));
        // K2: Claude Code asks before the row was seen without the message: the last reading before the question decides.
        Assert.Equal(WriteCheck.Over, QueuedWrite.Taken(askedThen: false, asksNow: true, heldBeforeAsk: true, draft: null, promptHolds: false, sinceEnterMs: 0, longest));
        Assert.Equal(WriteCheck.Skip, QueuedWrite.Taken(askedThen: false, asksNow: true, heldBeforeAsk: false, draft: null, promptHolds: false, sinceEnterMs: 0, longest));
    }

    /// <summary>
    /// The reviewer's sequence of the sixth review of 2026-11-27 (F1), played through the window's own queue
    /// (<see cref="WriteQueue"/>) with the checks the window gives a message, on a row that never takes an Enter: the
    /// first message's text and Enter go, the Enter once more, and three seconds after the Enter both messages are
    /// given back. Nothing of the second is written, nothing is cleared.
    /// </summary>
    [Fact]
    public void A_second_message_is_never_written_onto_a_row_that_holds_the_first()
    {
        var now = 0L;
        var row = string.Empty;
        var queue = new WriteQueue();
        var written = new List<string>();
        void Queue(string text)
        {
            long first = 0, enter = 0, entered = 0;
            queue.Add(text, 100, () =>
            {
                first = first == 0 ? Math.Max(now, 1) : first;
                return QueuedWrite.MessageFirst(false, null, false, null, row, now - first, 3000);
            });
            queue.Add("\r", 50, () =>
            {
                enter = enter == 0 ? Math.Max(now, 1) : enter;
                var check = QueuedWrite.MessageEnter(false, null, false, null, row == text, now - enter, 3000);
                entered = check == WriteCheck.Goes ? now : entered;
                return check;
            });
            queue.Add("\r", 50, () => QueuedWrite.EnterAgain(false, false, row == text, false, now - entered, 1500));
            queue.Add(string.Empty, 50, () => QueuedWrite.Taken(false, false, false, row, row == text, now - entered, 3000));
        }

        Queue("first message");
        Queue("second message");
        var dropped = false;
        while (!dropped && now < 10_000)
        {
            var step = queue.Next();
            switch (step.Kind)
            {
                case WriteStepKind.Write:
                    written.Add(step.Text);
                    // The row takes no Enter: what is typed stays, and an Enter leaves it there.
                    row = step.Text == "\r" ? row : row + step.Text;
                    break;
                case WriteStepKind.Dropped:
                    dropped = true;
                    break;
                case WriteStepKind.Idle:
                    Assert.Fail("the queue ran dry with a message not taken");
                    break;
            }

            now += Math.Max(step.WaitMs, 1);
        }

        Assert.True(dropped);
        Assert.Equal(["first message", "\r", "\r"], written);
        Assert.Equal("first message", row);
        Assert.InRange(now, 3000, 3700);
    }

    /// <summary>
    /// And an answer in the user's own words goes out a key at a time over seconds: the rest of it is over when
    /// something else waits than it was typed for, at once, and when nothing waits for as long as a settle; not for
    /// the frame in which Claude Code clears a question's rows to draw them again.
    /// </summary>
    [Fact]
    public void A_typed_answer_is_over_when_something_else_waits_or_nothing_does_for_a_settle()
    {
        const int settle = 400;
        var first = "question\n" + Create("alpha.txt").Content;
        var second = "question\n" + Create("beta.txt").Content;
        Assert.False(QueuedWrite.AnswerOver(first, first, goneForMs: 0, settle));
        // The next file's question under the same title, with the same answers: over, and no key more is written.
        Assert.True(QueuedWrite.AnswerOver(first, second, goneForMs: 0, settle));
        Assert.True(QueuedWrite.AnswerOver("words\nEnter text for option 4 (Other), or Escape for the list:", first, goneForMs: 0, settle));
        // Nothing waits: a redraw in passing for a frame or two, the question gone after a settle's time.
        Assert.False(QueuedWrite.AnswerOver(first, waiting: null, goneForMs: 0, settle));
        Assert.False(QueuedWrite.AnswerOver(first, waiting: null, goneForMs: 399, settle));
        Assert.True(QueuedWrite.AnswerOver(first, waiting: null, goneForMs: 400, settle));
        // Typed when nothing the window could read waited: bound to nothing, so nothing of it is written (the sixth
        // review of 2026-11-27, L2: one rule with a message sent into a question; until then it went whatever came).
        Assert.True(QueuedWrite.AnswerOver(typedFor: null, first, goneForMs: 0, settle));
        Assert.True(QueuedWrite.AnswerOver(typedFor: null, waiting: null, goneForMs: 0, settle));
        Assert.False(QueuedWrite.StillWaitsOn(null, null));
        Assert.True(QueuedWrite.StillWaitsOn(first, first));
    }
}
