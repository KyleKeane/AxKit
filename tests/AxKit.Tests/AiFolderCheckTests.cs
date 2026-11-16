using AxKit.Core;
using AxKit.Core.Ai;

namespace AxKit.Tests;

/// <summary>
/// The files in Claude's working folder that Claude Code obeys and axKit did not write (docs/axKit/ai-help.md AH-7.8;
/// repairs.md R-54): which are named, which never are, and the accepted list under the settings' seal. Every folder
/// is the test's own; the accepted list keeps its key beside it.
/// </summary>
public sealed class AiFolderCheckTests : IDisposable
{
    private readonly TempFolder _temp = new("axkit-ai-folder");
    private readonly string _folder;
    private readonly string _accepted;

    /// <summary>A file the caller wrote, as axHelp's agent is: left out while it holds this text.</summary>
    private static readonly IReadOnlyList<(string Path, string Text)> Written = [(Path.Combine(".claude", "agents", "agent.md"), "the agent, as written\r\n")];

    private const string Hooks = "{\"hooks\":{\"SessionStart\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"something.cmd\"}]}]}}";

    public AiFolderCheckTests()
    {
        _folder = Directory.CreateDirectory(Path.Combine(_temp.Path, "working")).FullName;
        _accepted = Path.Combine(_temp.Path, "settings", "ai-folder-files.json");
    }

    public void Dispose() => _temp.Dispose();

    private string Put(string name, string text)
    {
        var path = Path.Combine(_folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private IReadOnlyList<string> Named() => AiFolderCheck.NewOrChanged(_folder, _accepted, Written).Select(file => file.Path).ToList();

    private void AcceptAll() => Assert.True(AiFolderCheck.Accept(_folder, _accepted, AiFolderCheck.NewOrChanged(_folder, _accepted, Written)));

    [Fact]
    public void A_folder_as_axKit_and_axHelp_leave_it_names_nothing()
    {
        Assert.Empty(Named());
        // axKit's guides and its two rule files, the agent as written, the user's CLAUDE.md files, a screenshot,
        // and a file outside .claude: none is asked about.
        AiDocumentation.Write(_folder, "1.2.3");
        Put(Written[0].Path, Written[0].Text);
        Put("CLAUDE.md", "my standing instructions");
        Put("CLAUDE.local.md", "more of mine");
        Put(Path.Combine("screenshots", "one.png"), "png");
        Put("notes.json", Hooks);
        Put(Path.Combine(".claude", "notes.md"), "a note");
        Assert.True(File.Exists(Path.Combine(_folder, AiDocumentation.RulePath)));
        Assert.True(File.Exists(Path.Combine(_folder, AiToolsRule.RulePath)));
        Assert.Empty(Named());
        Assert.False(File.Exists(_accepted));
    }

    [Fact]
    public void A_settings_file_with_hooks_is_named_and_one_with_only_permission_answers_is_not()
    {
        Put(Path.Combine(".claude", "settings.local.json"), "{\"permissions\":{\"allow\":[\"Bash(dir)\"]}}");
        Put(Path.Combine(".claude", "settings.json"), "{\"permissions\":{\"allow\":[]},\"hooks\":{}}");
        Assert.Empty(Named());

        Put(Path.Combine(".claude", "settings.json"), Hooks);
        Assert.Equal([Path.Combine(".claude", "settings.json")], Named());

        // No JSON at all: what Claude Code makes of it cannot be known, so it is named.
        Put(Path.Combine(".claude", "settings.local.json"), "{ not json");
        Assert.Equal([Path.Combine(".claude", "settings.json"), Path.Combine(".claude", "settings.local.json")], Named());
    }

    [Fact]
    public void An_accepted_file_is_silent_a_changed_hooks_member_is_named_again_and_a_changed_permissions_member_is_not()
    {
        var settings = Path.Combine(".claude", "settings.json");
        Put(settings, "{\"permissions\":{\"allow\":[]},\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"one.cmd\"}]}]}}");
        Assert.Equal([settings], Named());
        AcceptAll();
        Assert.Empty(Named());

        // An "always allow" answer Claude Code writes beside the hooks changes nothing.
        Put(settings, "{\"permissions\":{\"allow\":[\"Bash(dir)\",\"Read(*)\"]},\n  \"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"one.cmd\"}]}]}}");
        Assert.Empty(Named());

        // Another command in the hooks is asked about again.
        Put(settings, "{\"permissions\":{\"allow\":[]},\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"two.cmd\"}]}]}}");
        Assert.Equal([settings], Named());
        AcceptAll();
        Assert.Empty(Named());

        // A second hooks member in the file, which a reader that takes the last one would obey, is not hidden by the first.
        Put(settings, "{\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"two.cmd\"}]}]},\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"three.cmd\"}]}]}}");
        Assert.Equal([settings], Named());
    }

    [Fact]
    public void The_servers_file_a_third_rule_a_skill_a_command_and_a_changed_agent_are_named()
    {
        AiDocumentation.Write(_folder, "1.2.3");
        Put(Written[0].Path, Written[0].Text);
        Put(".mcp.json", "{\"mcpServers\":{\"x\":{\"command\":\"x.exe\"}}}");
        Put(Path.Combine(".claude", "rules", "third.md"), "do as this says");
        Put(Path.Combine(".claude", "skills", "helper", "SKILL.md"), "---\nname: helper\n---\n");
        Put(Path.Combine(".claude", "skills", "helper", "scripts", "run.ps1"), "something");
        Put(Path.Combine(".claude", "commands", "go.md"), "go");
        Put(Path.Combine(".claude", "output-styles", "style.md"), "style");
        Put(Path.Combine(".claude", "plugins", "p", "plugin.json"), "{}");
        Put(Path.Combine(".claude", "agents", "other.md"), "another agent");
        Assert.Equal(
        [
            Path.Combine(".claude", "agents", "other.md"),
            Path.Combine(".claude", "commands", "go.md"),
            Path.Combine(".claude", "output-styles", "style.md"),
            Path.Combine(".claude", "plugins", "p", "plugin.json"),
            Path.Combine(".claude", "rules", "third.md"),
            Path.Combine(".claude", "skills", "helper", "scripts", "run.ps1"),
            Path.Combine(".claude", "skills", "helper", "SKILL.md"),
            ".mcp.json",
        ], Named());

        AcceptAll();
        Assert.Empty(Named());

        // The agent no longer as axHelp wrote it, and an accepted file changed: both named, the others silent.
        Put(Written[0].Path, "the agent, changed");
        Put(".mcp.json", "{\"mcpServers\":{\"x\":{\"command\":\"y.exe\"}}}");
        Assert.Equal([Written[0].Path, ".mcp.json"], Named());
    }

    [Fact]
    public void The_accepted_list_is_for_one_folder_and_refuses_a_hand_edit()
    {
        var rule = Put(Path.Combine(".claude", "rules", "third.md"), "do as this says");
        AcceptAll();
        Assert.Empty(Named());

        // Another working folder with the same file has accepted nothing.
        var other = Directory.CreateDirectory(Path.Combine(_temp.Path, "other", ".claude", "rules")).Parent!.Parent!.FullName;
        File.WriteAllText(Path.Combine(other, ".claude", "rules", "third.md"), "do as this says");
        Assert.Single(AiFolderCheck.NewOrChanged(other, _accepted, Written));

        // The file changes; its new hash written into the list by hand does not count: the seal does not check, the
        // list is not used, and the file is named.
        File.WriteAllText(rule, "do as this other thing says");
        var changed = Assert.Single(AiFolderCheck.NewOrChanged(_folder, _accepted, Written));
        var text = File.ReadAllText(_accepted);
        var old = AiFolderCheck.Obeyed(_folder, Written).Single().Hash;
        Assert.DoesNotContain(old, text);
        var before = System.Text.RegularExpressions.Regex.Match(text, "\"hash\": \"([0-9A-F]{64})\"").Groups[1].Value;
        Assert.NotEqual(string.Empty, before);
        File.WriteAllText(_accepted, text.Replace(before, changed.Hash, StringComparison.Ordinal));
        Assert.Single(Named());

        // The seal taken out of the list is no way around it either: this install has sealed the file.
        File.WriteAllText(_accepted, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(_accepted), "\"seal\": \"[^\"]*\"", "\"seal\": \"\""));
        Assert.Single(Named());

        // Accepted through the notice, the list is written again with its seal.
        AcceptAll();
        Assert.Empty(Named());
    }

    [Fact]
    public void The_notice_names_the_files_one_a_line_and_says_whose_they_may_be()
    {
        Assert.Equal("Claude's working folder holds files that can run commands", AiFolderCheck.NoticeTitle);
        var one = AiFolderCheck.NoticeText([new ObeyedFile(".mcp.json", "A")]);
        Assert.Equal(
            "1 file in Claude's working folder can make Claude Code run commands by itself, and axKit did not write it:\n"
            + ".mcp.json\n"
            + "Claude Code would obey it from its start. It may be your own. "
            + "Open the folder shows the folder in axFile. Start anyway starts the conversation and does not ask again until one of these files changes or a new one comes. Cancel starts nothing.", one);

        var two = AiFolderCheck.NoticeText([new ObeyedFile(".mcp.json", "A"), new ObeyedFile(@".claude\settings.json", "B")]);
        Assert.StartsWith("2 files in Claude's working folder can make Claude Code run commands by themselves, and axKit did not write them:\n.mcp.json\n.claude\\settings.json\nClaude Code would obey them from its start. They may be your own. ", two, StringComparison.Ordinal);

        var many = AiFolderCheck.NoticeText(Enumerable.Range(1, 23).Select(n => new ObeyedFile($"file{n}.md", "A")).ToList());
        Assert.Contains("file20.md\nand 3 more.\n", many, StringComparison.Ordinal);
        Assert.DoesNotContain("file21.md", many, StringComparison.Ordinal);
    }
}
