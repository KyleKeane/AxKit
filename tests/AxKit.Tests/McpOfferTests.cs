using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>The pure rules of <c>axkit_offer_command</c> (docs/axKit/mcp.md MC-9).</summary>
public sealed class McpOfferTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_command_is_refused(string? command) =>
        Assert.Contains("No command", McpOffer.Refusal(command));

    [Theory]
    [InlineData("git status\nrm -rf x")]
    [InlineData("git status\r")]
    [InlineData("echo a\u001b[2K")]
    [InlineData("echo\ta")]
    public void A_line_break_or_a_control_character_is_refused_since_the_user_hears_one_line(string command) =>
        Assert.Contains("one line", McpOffer.Refusal(command));

    [Fact]
    public void A_command_too_long_to_judge_by_ear_is_refused()
    {
        Assert.Null(McpOffer.Refusal(new string('a', McpOffer.MaxLength)));
        Assert.Contains("longer than", McpOffer.Refusal(new string('a', McpOffer.MaxLength + 1)));
    }

    [Fact]
    public void A_plain_command_is_offered_and_sent_after_the_shell_mark()
    {
        Assert.Null(McpOffer.Refusal("git branch -d automate"));
        Assert.Equal("! git branch -d automate", McpOffer.Line("  git branch -d automate "));
    }

    [Fact]
    public void The_question_holds_the_command_on_a_line_of_its_own_then_the_reason_on_one_line()
    {
        var text = McpOffer.AskText("claude-code", "gcloud auth login", "It asks\nquestions in a browser.");
        Assert.Contains("\n\ngcloud auth login\n\n", text);
        Assert.Contains("Its reason: It asks questions in a browser.", text);
        Assert.StartsWith("Claude asks you to run", text);
        Assert.DoesNotContain("Its reason", McpOffer.AskText("claude-code", "x", null));
    }

    [Fact]
    public void The_tool_is_axClaude_s_and_needs_no_run_switch_since_the_user_is_asked_every_time()
    {
        Assert.Equal((Dispatch.AxClaude, false), McpTools.AppTools[McpTools.Offer]);
        var schema = McpTools.All.Single(tool => tool.Name == McpTools.Offer).InputSchema;
        Assert.Equal("command", schema["required"]![0]!.GetValue<string>());
    }
}
