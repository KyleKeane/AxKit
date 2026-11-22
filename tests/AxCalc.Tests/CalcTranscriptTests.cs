using AxCalc.Core;
using AxKit.Core.Transcript;

namespace AxCalc.Tests;

/// <summary>axCalc's lines for the conversation window (SPEC.md AC-2.1): answers in order, system lines and bookmarks among them.</summary>
public sealed class CalcTranscriptTests
{
    [Fact]
    public void An_answer_is_a_plain_line_at_the_end_and_the_window_is_told()
    {
        var lines = new CalcTranscript();
        var changes = 0;
        lines.Changed += () => changes++;

        lines.AddAnswer("2 + 3 = 5");
        lines.AddAnswer("10*3 = 30");

        Assert.Equal(["2 + 3 = 5", "10*3 = 30"], lines.Lines.Select(line => line.Text));
        Assert.All(lines.Lines, line => Assert.Equal(LineKind.Plain, line.Kind));
        Assert.Equal(2, changes);
    }

    [Fact]
    public void The_answers_kept_from_the_last_run_come_in_order_with_one_change()
    {
        var lines = new CalcTranscript();
        var changes = 0;
        lines.Changed += () => changes++;

        lines.Load(["1 = 1", "2 = 2", "3 = 3"]);

        Assert.Equal(["1 = 1", "2 = 2", "3 = 3"], lines.Answers);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void A_system_line_is_marked_and_is_no_answer()
    {
        var lines = new CalcTranscript();
        lines.AddAnswer("6*7 = 42");
        lines.AddSystemLine("The settings file could not be read.");

        Assert.Equal("System: The settings file could not be read.", lines.Lines[1].Text);
        Assert.Equal(LineKind.System, lines.Lines[1].Kind);
        Assert.Equal(["6*7 = 42"], lines.Answers);
    }

    [Fact]
    public void The_oldest_answer_goes_with_the_bookmark_in_front_of_it()
    {
        var lines = new CalcTranscript();
        lines.AddAnswer("1 = 1");
        lines.AddAnswer("2 = 2");
        lines.ToggleBookmark(lines.Lines[0]);
        Assert.Equal(LineKind.Bookmark, lines.Lines[0].Kind);

        lines.RemoveOldestAnswer();

        Assert.Equal(["2 = 2"], lines.Lines.Select(line => line.Text));
    }

    [Fact]
    public void Clearing_takes_the_answers_and_the_bookmarks_and_keeps_the_system_lines()
    {
        var lines = new CalcTranscript();
        lines.AddSystemLine("A note of the window's.");
        lines.AddAnswer("1 = 1");
        lines.AddAnswer("2 = 2");
        lines.ToggleBookmark(lines.Lines[2]);

        lines.ClearAnswers();

        Assert.Equal(["System: A note of the window's."], lines.Lines.Select(line => line.Text));
        Assert.Empty(lines.Answers);

        // The bookmarks count from 1 again.
        lines.AddAnswer("3 = 3");
        var bookmark = lines.ToggleBookmark(lines.Lines[1]);
        Assert.Equal("Bookmark 1", bookmark!.Value.Bookmark.Text);
    }

    [Fact]
    public void Removing_the_oldest_of_none_changes_nothing()
    {
        var lines = new CalcTranscript();
        lines.AddSystemLine("Only this.");
        lines.RemoveOldestAnswer();
        Assert.Single(lines.Lines);
    }
}
