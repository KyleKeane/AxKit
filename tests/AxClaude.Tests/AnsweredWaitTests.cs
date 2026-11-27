using AxClaude.Core.Transcript;

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
        Assert.True(AnsweredWait.Replaced(first.Signature, second.Signature, first.Core, second.Core));
        // The same question drawn again, also with another line between: the same question still.
        Assert.False(AnsweredWait.Replaced(first.Signature, first.Signature, first.Core, Create("alpha.txt").Core));
        // Another title or other answers: that is seen by the signature, at the settle, as ever.
        Assert.False(AnsweredWait.Replaced(first.Signature, "Claude's settings", first.Core, null));
        Assert.False(AnsweredWait.Replaced(first.Signature, null, first.Core, null));
        // Two questions that are no permission questions have no core, and are told apart by their signatures alone.
        Assert.False(AnsweredWait.Replaced("Colour\n1. Red", "Colour\n1. Red", null, null));
    }
}
