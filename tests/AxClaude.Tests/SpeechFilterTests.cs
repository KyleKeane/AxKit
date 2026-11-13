using AxClaude.Core;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;
using AxKit.TestShared;

namespace AxClaude.Tests;

/// <summary>The words that keep a reply line silent (SPEC.md FR-7.4a, SpeechFilter, 2026-11-12).</summary>
public class SpeechFilterTests
{
    [Fact]
    public void The_field_is_words_between_semicolons_trimmed_without_empties_or_repeats()
    {
        Assert.Equal(["token", "Cost:", "api key"], SpeechFilter.Words(" token ;Cost:;; api key ; TOKEN"));
        Assert.Empty(SpeechFilter.Words(null));
        Assert.Empty(SpeechFilter.Words(" ; "));
        Assert.Equal("token; Cost:", SpeechFilter.Text(["token", "Cost:"]));
    }

    [Fact]
    public void A_line_holding_a_word_is_silenced_case_not_minded()
    {
        List<string> words = ["cost:", "Tip"];
        Assert.True(SpeechFilter.Silences("Total COST: $0.12", words));
        Assert.True(SpeechFilter.Silences("tip: use /compact", words));
        Assert.False(SpeechFilter.Silences("The tests pass.", words));
        Assert.False(SpeechFilter.Silences("Anything at all", []));
    }

    [Fact]
    public void A_wrapped_line_is_silenced_or_spoken_whole_with_the_rows_joined_to_it()
    {
        // docs/axKit/repairs.json R-20: one line over three rows, the word in the second; then a line of its own.
        var first = new Line(1, LineKind.ClaudeReply, "claude: The total");
        var second = new Line(2, LineKind.Plain, "cost: twelve cents for") { JoinedToPrevious = true };
        var third = new Line(3, LineKind.Plain, "this turn.") { JoinedToPrevious = true };
        var own = new Line(4, LineKind.Plain, "The tests pass.");
        Assert.Equal([own], SpeechFilter.Spoken([first, second, third, own], ["cost:"]));
        // No word: every row, in order.
        Assert.Equal([first, second, third, own], SpeechFilter.Spoken([first, second, third, own], []));
        // A word the wrap split over two rows still silences the line, with or without a space at the wrap.
        var head = new Line(5, LineKind.ClaudeReply, "claude: see the pel");
        var tail = new Line(6, LineKind.Plain, "ican there") { JoinedToPrevious = true };
        Assert.Empty(SpeechFilter.Spoken([head, tail], ["pelican"]));
        var spaced = new Line(7, LineKind.Plain, "feathers") { JoinedToPrevious = true };
        Assert.Empty(SpeechFilter.Spoken([new Line(8, LineKind.ClaudeReply, "claude: pelican"), spaced], ["pelican feathers"]));
    }

    [Fact]
    public void Every_announcement_is_on_and_surveys_are_asked_until_the_user_says_otherwise()
    {
        var settings = new AppSettings();
        Assert.True(settings.AnnounceReady && settings.AnnounceSent && settings.AnnounceResponding && settings.AnnounceQuestionClosed && settings.AnnounceClaudeNotices);
        Assert.False(settings.DismissSurveys);
        Assert.True(settings.OpenFeedbackDrafts);
        Assert.Empty(settings.SilentWords);
    }

    [Fact]
    public void The_silent_words_survive_the_settings_file_tidied()
    {
        using var temp = new TempFolder("axclaude-speechfilter");
        var path = temp.File("axClaude.json", """{ "SilentWords": [" Tip ", "", "tip", "Cost:"], "DismissSurveys": true }""");
        var settings = AppSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(["Tip", "Cost:"], settings.SilentWords);
        Assert.True(settings.DismissSurveys);
    }
}
