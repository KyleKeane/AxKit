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
}
