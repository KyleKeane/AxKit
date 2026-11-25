using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// Choose the model and Choose the effort over the stream (docs/axClaude/stream-mode.md SM-5.5): the window's own
/// lists, made from what the answer to the first request listed (the fixture choices), and the slash command each
/// choice is sent with, which the stream takes mid-run (the same fixture).
/// </summary>
public sealed class StreamChoiceTests
{
    private static IReadOnlyList<StreamModelChoice> Recorded()
    {
        StreamReplay.Run("choices", out var model);
        return model.Models;
    }

    [Fact]
    public void The_models_are_listed_by_their_names_with_Claude_Codes_words_and_the_one_in_use_marked()
    {
        var models = Recorded();
        var choice = StreamChoice.Model("/model", models, "haiku")!;

        Assert.Equal("Choose the model", choice.Question.Title);
        Assert.Equal(models.Count, choice.Question.Options.Count);
        var haiku = choice.Question.Options[4];
        Assert.Equal("5", haiku.Key);
        Assert.Equal("Haiku 5.5 — Fastest for quick answers", haiku.Text);
        Assert.True(haiku.Current);
        Assert.Single(choice.Question.Options, option => option.Current);
        Assert.False(choice.Question.AllowsSeveral);

        Assert.Equal("/model haiku", choice.CommandFor("5"));
        Assert.Equal("/model default", choice.CommandFor(" 1 "));
        Assert.Null(choice.CommandFor("0"));
        Assert.Null(choice.CommandFor("15"));
        Assert.Null(choice.CommandFor("1,2"));
    }

    [Fact]
    public void The_efforts_are_every_level_the_models_take_once_in_their_order()
    {
        var choice = StreamChoice.Effort("/effort", Recorded())!;

        Assert.Equal("Choose the effort", choice.Question.Title);
        Assert.Equal(["1. low", "2. medium", "3. high", "4. xhigh", "5. max"], choice.Question.Options.Select(option => $"{option.Key}. {option.Text}"));
        Assert.DoesNotContain(choice.Question.Options, option => option.Current);
        Assert.Equal("/effort low", choice.CommandFor("1"));
    }

    [Fact]
    public void Nothing_is_listed_before_the_first_request_was_answered()
    {
        Assert.Null(StreamChoice.Model("/model", [], string.Empty));
        Assert.Null(StreamChoice.Effort("/effort", []));
        Assert.Null(StreamChoice.Effort("/effort", [new StreamModelChoice("old", "Old", string.Empty, [])]));
    }
}
