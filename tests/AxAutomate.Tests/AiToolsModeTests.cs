using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The AI tools mode's pure half (docs/axKit/user-workflows.md WF-9.2 to WF-9.4): which scripts the mode plays, the
/// steps that turn the access on, which <c>do</c> step goes through the tools, the call's line and the refusal that is
/// waited out.
/// </summary>
public sealed class AiToolsModeTests
{
    private static ListedCommand Row(string label, string menu, string mark = "read", bool keyOnly = false, bool ai = true) =>
        new(label, menu, "ctrl+f", [], string.Empty, mark, null, keyOnly, ai, "ctrl+f");

    private static readonly WindowCommands InTheText = new("axDown", null, [Row("Find", "Navigation")]);

    [Fact]
    public void The_preamble_is_the_scripts_own_lines()
    {
        // WF-9.3: every line of the preamble is a line of the script that turns the access on, or the start of one,
        // in the script's order: a notice changed there shows here at once.
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var script = File.ReadAllLines(Path.Combine(root, "tests", "scripts", "mcp", "02-run-commands.txt"));
        Assert.Equal(20, AiToolsMode.Preamble.Count);
        var at = 0;
        foreach (var line in AiToolsMode.Preamble)
        {
            while (at < script.Length && !script[at].StartsWith(line, StringComparison.Ordinal))
            {
                at++;
            }

            Assert.True(at < script.Length, $"\"{line}\" is no line of tests/scripts/mcp/02-run-commands.txt after the preamble's lines before it.");
            at++;
        }
    }

    [Fact]
    public void The_mode_covers_a_script_of_one_window_with_a_do_step()
    {
        Assert.True(AiToolsMode.Covers(["# Workflow: W", "file find.md x", "start down {temp}\\find.md", "do Find", "keys enter"]));

        // An iffound do counts, and the verb is read without regard to case.
        Assert.True(AiToolsMode.Covers(["start Down x", "try said Trust", "  iffound do Find"]));
        Assert.True(AiToolsMode.Covers(["start file {temp}", "keys down", "do Rename", "close"]));
    }

    [Fact]
    public void A_script_with_an_mcp_step_a_second_window_or_no_do_step_is_passed_over()
    {
        Assert.False(AiToolsMode.Covers(["start down x", "keys ctrl+f"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "mcp start"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "mcp call axkit_windows"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "iffound mcp call axkit_windows"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "start down y"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "launch down y"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do New window", "adopt"]));
        Assert.False(AiToolsMode.Covers(["start down x", "do Find", "use 1"]));
        Assert.False(AiToolsMode.Covers(["do Find"]));
        Assert.False(AiToolsMode.Covers([]));

        // A start that names no app (a file alone): which window it opens cannot be read.
        Assert.False(AiToolsMode.Covers(["start {temp}\\x.md", "do Find"]));

        // A comment is no step.
        Assert.False(AiToolsMode.Covers(["start down x", "# do Find"]));
        Assert.True(AiToolsMode.Covers(["start down x", "do Find", "# mcp start", "# use 1"]));
    }

    [Fact]
    public void A_script_of_axPass_or_axClip_is_passed_over()
    {
        Assert.False(AiToolsMode.Covers(["start pass {temp}\\x.kdbx", "do Lock"]));
        Assert.False(AiToolsMode.Covers(["start clip", "do Clear"]));
        Assert.False(AiToolsMode.Covers(["start CLIP", "do Clear"]));
    }

    [Fact]
    public void The_tools_may_run_a_menu_command_that_is_for_them_and_does_not_act()
    {
        Assert.True(AiToolsMode.MayRun(InTheText, Row("Find", "Navigation")));
        Assert.True(AiToolsMode.MayRun(InTheText, Row("Word wrap", "View", mark: "change")));

        // A menu item with no key at all is theirs too.
        Assert.True(AiToolsMode.MayRun(InTheText, Row("Say new line", "Options") with { Key = string.Empty, Now = string.Empty }));
    }

    [Fact]
    public void A_key_alone_an_act_command_a_row_not_for_the_tools_and_a_window_with_a_notice_go_by_the_keyboard()
    {
        Assert.False(AiToolsMode.MayRun(InTheText, Row("Between the text and the details", string.Empty, keyOnly: true, ai: false)));
        Assert.False(AiToolsMode.MayRun(InTheText, Row("Between the text and the details", string.Empty, keyOnly: true)));
        Assert.False(AiToolsMode.MayRun(InTheText, Row("Delete", "File", mark: "act")));
        Assert.False(AiToolsMode.MayRun(InTheText, Row("Delete", "File", mark: "Act")));
        Assert.False(AiToolsMode.MayRun(InTheText, Row("Recent file", "File", ai: false)));
        Assert.False(AiToolsMode.MayRun(InTheText with { Notice = "Find" }, Row("Find", "Navigation")));
        Assert.False(AiToolsMode.MayRun(InTheText with { Notice = string.Empty }, Row("Find", "Navigation")));
    }

    [Fact]
    public void The_call_names_the_window_the_label_and_the_menus()
    {
        Assert.Equal(
            "axkit_window_run {\"window\":\"{window}\",\"command\":\"Heading\",\"menu\":\"Navigation, Next\"}",
            AiToolsMode.Call(Row("Heading", "Navigation, Next")));
        Assert.Equal(
            "axkit_window_run {\"window\":\"{window}\",\"command\":\"Go to line...\",\"menu\":\"Navigation\"}",
            AiToolsMode.Call(Row("Go to line...", "Navigation")));

        // A row with no menus: no menu member. A quote in a label is escaped, an apostrophe stays.
        Assert.Equal(
            "axkit_window_run {\"window\":\"{window}\",\"command\":\"Say \\\"it's\\\"\"}",
            AiToolsMode.Call(Row("Say \"it's\"", string.Empty)));
    }

    [Fact]
    public void The_refusal_for_a_user_at_work_is_told_apart()
    {
        // The words of AxKit.Core's McpRun.UserBusy, as the broker gives a refusal to a script.
        Assert.True(AiToolsMode.IsBusyRefusal("error: " + AxKit.Core.Mcp.McpRun.UserBusy));
        Assert.False(AiToolsMode.IsBusyRefusal("error: " + AxKit.Core.Mcp.McpRun.SwitchOff));
        Assert.False(AiToolsMode.IsBusyRefusal("error: The window shows the user a notice, Find: commands run once the user has answered it, and AI tools never answer it."));
        Assert.False(AiToolsMode.IsBusyRefusal("{\"ran\": \"Find\"}"));
        Assert.False(AiToolsMode.IsBusyRefusal(string.Empty));
    }
}
