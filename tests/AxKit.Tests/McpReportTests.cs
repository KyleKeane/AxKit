using AxKit.Core;
using AxKit.Core.Mcp;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>axkit_draft_report's pure rules (docs/axKit/mcp.md MC-9.11): what a draft may be, its kind, and the words that say an AI drafted it.</summary>
public sealed class McpReportTests
{
    [Fact]
    public void A_draft_must_have_words_and_stay_short()
    {
        Assert.NotNull(McpReport.Refusal(null));
        Assert.NotNull(McpReport.Refusal("   "));
        Assert.NotNull(McpReport.Refusal(new string('x', McpReport.MaxLength + 1)));
        Assert.Null(McpReport.Refusal("Find first misses a match on the last line."));
    }

    [Theory]
    [InlineData("bug", ReportKind.Bug)]
    [InlineData("Feature request", ReportKind.Feature)]
    [InlineData("feature", ReportKind.Feature)]
    [InlineData(null, ReportKind.Feedback)]
    [InlineData("anything", ReportKind.Feedback)]
    public void The_kind_is_read_from_a_word(string? word, ReportKind kind)
    {
        Assert.Equal(kind, McpReport.Kind(word));
    }

    [Fact]
    public void The_words_say_who_drafted_them_and_keep_the_lines()
    {
        var words = McpReport.Words("claude-code", "First line.\nSecond‮ line.");
        Assert.StartsWith("(Drafted by Claude, an AI tool; read by me before sending.)\r\n\r\n", words, StringComparison.Ordinal);
        Assert.Contains("First line.\r\nSecond", words, StringComparison.Ordinal);
        Assert.Contains("U+202E", words, StringComparison.Ordinal);
        Assert.StartsWith("Claude, an AI model, drafted this report", AiCaution.Drafted("Claude"), StringComparison.Ordinal);
    }

    [Fact]
    public void The_tool_needs_a_text_and_never_sends()
    {
        var tool = Assert.Single(McpTools.All, listed => listed.Name == McpTools.DraftReport);
        Assert.Equal(["text"], tool.InputSchema["required"]!.AsArray().Select(name => name!.GetValue<string>()));
        Assert.Contains("You never send it", tool.Description, StringComparison.Ordinal);
    }
}
