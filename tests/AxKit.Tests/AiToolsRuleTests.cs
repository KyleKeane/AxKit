using System.Text.RegularExpressions;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>
/// The rule file of axKit's AI tools in Claude's working folder (docs/axKit/ai-help.md AH-8): made from the one table
/// of tools, so that it names every tool and no tool that does not exist; its caution in <see cref="AiCaution"/>'s
/// words; short enough to be read at every start.
/// </summary>
public sealed class AiToolsRuleTests
{
    [Fact]
    public void Every_tool_is_in_the_file_with_its_own_description_and_its_guidance()
    {
        foreach (var tool in McpTools.All)
        {
            // A new tool of McpTools without its guidance in AiToolsRule.Tools fails here.
            Assert.True(AiToolsRule.Tools.ContainsKey(tool.Name), $"{tool.Name} has no guidance in AiToolsRule.Tools: when to use it, when not, what to say.");
            Assert.Contains($"### {tool.Name}\n{tool.Description}\n- Use it ", AiToolsRule.Text, StringComparison.Ordinal);
        }

        Assert.Equal(McpTools.All.Count, AiToolsRule.Tools.Count);
    }

    [Fact]
    public void No_tool_is_named_in_the_file_that_does_not_exist()
    {
        var known = McpTools.All.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        var named = Regex.Matches(AiToolsRule.Text, @"\baxkit_[a-z_]+").Select(match => match.Value).Distinct().ToList();
        Assert.All(named, name => Assert.True(known.Contains(name), $"The rule file names {name}, which is not a tool of McpTools."));
        // Every tool is named, the language rules' included.
        Assert.Equal(known.Count, named.Count);
        // A tool's guidance names no tool that is gone either (the same text, but said where the fault would be).
        Assert.All(AiToolsRule.Tools.Keys, name => Assert.Contains(name, known));
    }

    [Fact]
    public void The_file_says_what_the_user_is_likely_to_say_and_where_the_tools_stop()
    {
        var text = AiToolsRule.Text;
        Assert.Equal(Path.Combine(".claude", "rules", "axkit-tools.md"), AiToolsRule.RulePath);
        // No frontmatter: a rule with a paths field is loaded only for matching files, this one at every start.
        Assert.StartsWith("# axKit's AI tools\n", text, StringComparison.Ordinal);
        Assert.Contains("\"What did it just say\", \"I missed that\": axkit_window_said.", text, StringComparison.Ordinal);
        Assert.Contains("the guides in the folder documentation, not a tool", text, StringComparison.Ordinal);
        Assert.Contains("Never axClaude and axTerminal this way", text, StringComparison.Ordinal);
        Assert.Contains("axPass and axClip are never readable", text, StringComparison.Ordinal);
        Assert.Contains("Nothing answers a question a window asks the user", text, StringComparison.Ordinal);
        Assert.Contains("A refused call is not retried another way", text, StringComparison.Ordinal);
        Assert.Contains(AiToolsRule.WhereOn, text, StringComparison.Ordinal);
        Assert.Contains(AiToolsRule.WhereRun, text, StringComparison.Ordinal);
        Assert.Contains("mcp__axkit__axkit_windows", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_caution_of_an_offered_command_is_in_the_bundles_one_wording()
    {
        // Root rule 11: never written afresh.
        Assert.Contains(AiCaution.Short("Claude"), AiToolsRule.Text, StringComparison.Ordinal);
        Assert.Contains(AiCaution.Command("Claude"), AiToolsRule.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_file_is_short_and_its_own_lines_are_short()
    {
        var lines = AiToolsRule.Text.Split('\n');
        Assert.InRange(lines.Length, 60, 170);
        Assert.DoesNotContain('\r', AiToolsRule.Text);
        // The hand-written lines stay short; a tool's description and guidance and the caution are one line each.
        var descriptions = McpTools.All.Select(tool => tool.Description).ToHashSet(StringComparer.Ordinal);
        foreach (var line in lines.Where(line => !descriptions.Contains(line) && !line.StartsWith("- Use it ", StringComparison.Ordinal)
            && !line.StartsWith("- Not ", StringComparison.Ordinal) && !line.StartsWith("- Say ", StringComparison.Ordinal)
            && !line.StartsWith("Its explanation starts", StringComparison.Ordinal)))
        {
            Assert.True(line.Length <= 112, $"A line of the rule file is {line.Length} characters long: {line}");
        }
    }

    [Fact]
    public void The_file_is_written_and_kept_current_with_the_guides()
    {
        using var temp = new TempFolder("axkit-ai-tools-rule");
        Assert.Contains(AiToolsRule.RulePath, AiDocumentation.Write(temp.Path, "1.2.3"));
        var file = Path.Combine(temp.Path, AiToolsRule.RulePath);
        Assert.StartsWith("# axKit's AI tools\r\n", File.ReadAllText(file), StringComparison.Ordinal);

        // An axKit before this one wrote the guides without it: the folder counts as missing, so it is written before Claude starts.
        File.Delete(file);
        Assert.True(AiDocumentation.IsMissing(temp.Path));
        Assert.Equal([AiToolsRule.RulePath], AiDocumentation.Write(temp.Path, "1.2.3"));
        Assert.False(AiDocumentation.IsMissing(temp.Path));

        // Another version writes it again over a changed one; a rule of the user's beside it stays.
        File.WriteAllText(file, "changed");
        var own = temp.File(Path.Combine(".claude", "rules", "mine.md"), "my rule");
        AiDocumentation.Write(temp.Path, "1.3.0");
        Assert.StartsWith("# axKit's AI tools", File.ReadAllText(file), StringComparison.Ordinal);
        Assert.Equal("my rule", File.ReadAllText(own));
    }
}
