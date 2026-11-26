using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// What a tool printed, as a conversation shows it under the call (docs/axClaude/stream-mode.md SM-4.11): the screen's
/// preview, measured with Claude Code 2.1.332 (three lines, then "… +9 lines" for a command of twelve), and the whole
/// output kept.
/// </summary>
public sealed class ToolOutputTests
{
    [Fact]
    public void Three_lines_are_shown_and_the_rest_counted()
    {
        var output = new ToolOutput("1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n12\n");

        Assert.Equal(["1", "2", "3", "… +9 lines"], output.Shown);
        Assert.Equal(12, output.Lines.Count);
        Assert.Equal(9, output.Hidden);
        Assert.Equal("1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n12", output.Text);
    }

    [Theory]
    [InlineData("", new string[0])]
    [InlineData("one", new[] { "one" })]
    [InlineData("one\r\ntwo\r\nthree\r\n", new[] { "one", "two", "three" })]
    [InlineData("one\ntwo\nthree\nfour", new[] { "one", "two", "three", "… +1 line" })]
    [InlineData("a  \n\nc", new[] { "a", "", "c" })]
    public void An_output_shows_up_to_three_lines(string text, string[] shown)
    {
        Assert.Equal(shown, new ToolOutput(text).Shown);
    }

    [Fact]
    public void An_empty_output_shows_nothing_and_keeps_nothing()
    {
        var output = new ToolOutput("\r\n\n");

        Assert.Empty(output.Shown);
        Assert.Empty(output.Lines);
        Assert.Equal(string.Empty, output.Text);
    }
}
