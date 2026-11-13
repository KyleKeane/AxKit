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
    public void The_question_is_named_with_the_command_and_read_again_with_the_reason_on_one_line()
    {
        // The command first, then that an AI wrote it, every time the question is read (AX-11.12).
        Assert.Equal("Claude offers a command for you to run: gcloud auth login. Written by Claude, an AI: read the explanation before you run it", McpOffer.AskName("claude-code", " gcloud auth login "));
        Assert.EndsWith("before you run it. Its reason: It asks questions in a browser.",
            McpOffer.AskText("claude-code", "gcloud auth login", "It asks\nquestions in a browser."));
        Assert.DoesNotContain("Its reason", McpOffer.AskText("claude-code", "x", null));
    }

    [Fact]
    public void The_list_starts_on_the_answer_that_only_explains_and_running_is_never_first()
    {
        Assert.Equal(0, McpOffer.Explain);
        Assert.StartsWith("1. Explain", McpOffer.Answers[McpOffer.Explain]);
        Assert.StartsWith("2. Yes", McpOffer.Answers[McpOffer.Run]);
        Assert.StartsWith("3. No", McpOffer.Answers[McpOffer.No]);
        Assert.StartsWith("4. No, copy", McpOffer.Answers[McpOffer.Copy]);
        Assert.StartsWith("5. No, and report", McpOffer.Answers[McpOffer.Concern]);
        Assert.Equal(5, McpOffer.Answers.Count);
    }

    [Fact]
    public void A_concern_opens_Claude_Code_s_feedback_with_the_command_the_reason_and_the_words_on_one_line()
    {
        var line = McpOffer.ConcernLine("claude-code", "rm -rf build", "To clean up.", "It deletes\nmore than it says.");
        Assert.Equal("/feedback Concern about a command Claude offered me to run myself (axKit, axkit_offer_command). The command: rm -rf build The reason given: To clean up. My concern: It deletes more than it says.", line);
        Assert.EndsWith("The reason given: none My concern: not stated", McpOffer.ConcernLine("claude-code", "x", null, " "));
        Assert.True(McpOffer.ConcernLine("claude-code", new string('a', 1000), new string('b', 300), new string('c', 300)).Length <= "/feedback ".Length + 1500);
    }

    [Fact]
    public void The_explanation_has_the_command_the_reason_the_tool_s_breakdown_by_line_and_what_the_answers_do()
    {
        var text = McpOffer.ExplainText("claude-code", "git branch -D cleanup", "Deleting is yours.", "git branch: works on branches\r\n-D: deletes even when not merged\u001b[2J\nhttps://git-scm.com/docs/git-branch");
        // The caution comes first: who wrote it, that a refused command may be why it is offered, to read all of it, whose act running is.
        Assert.StartsWith("# Read this first\n\nClaude, an AI model, wrote this command and everything said about it below.", text);
        Assert.Contains("permission or safety checks refused it, so run it with care", text);
        Assert.Contains("Read all the details here", text);
        Assert.Contains("Running it is your own act", text);
        Assert.Contains("# The command\n\ngit branch -D cleanup\n\n", text);
        Assert.Contains("# Why Claude offers it\n\nDeleting is yours.\n\n", text);
        Assert.Contains("# What each part does, in Claude's words\n\ngit branch: works on branches\n-D: deletes even when not merged [2J\nhttps://git-scm.com/docs/git-branch\n\n", text);
        Assert.Contains("# What your answers do", text);
        var bare = McpOffer.ExplainText("claude-code", "x", null, null);
        Assert.Contains("No reason was given.", bare);
        Assert.DoesNotContain("What each part does", bare);
    }

    [Fact]
    public void The_tool_is_axClaude_s_and_needs_no_run_switch_since_the_user_is_asked_every_time()
    {
        Assert.Equal((Dispatch.AxClaude, false), McpTools.AppTools[McpTools.Offer]);
        var schema = McpTools.All.Single(tool => tool.Name == McpTools.Offer).InputSchema;
        Assert.Equal("command", schema["required"]![0]!.GetValue<string>());
    }
}
