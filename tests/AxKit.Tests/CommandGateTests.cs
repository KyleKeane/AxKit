using System.Text.RegularExpressions;
using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>
/// The table of docs/axKit/command-language.md section 3, cell by cell (CL-3.2): who may run a command of which mark,
/// in which state of the window, as <see cref="CommandGate.Decide"/> answers it. One test per cell of an origin's
/// marks and one per condition the document lists under the origin, each named after its cell: the origin, then the
/// mark or <c>condition</c> and its number, so that the table and this file are read side by side. The last tests
/// read the document and fail when it names a cell that has no test here.
///
/// Only the AI tool's path asks the gate in code (<c>AxKitForm.McpRunCommand</c>); for the other origins these tests
/// hold the description, and the enforcement is where CL-4.8 says.
/// </summary>
public sealed partial class CommandGateTests
{
    private static readonly CommandState Calm = new();

    private static readonly CommandMark[] Marks = [CommandMark.Read, CommandMark.Open, CommandMark.Change, CommandMark.Act];

    private static CommandVerdict Decide(CommandOrigin origin, CommandMark mark, CommandState? state = null) =>
        CommandGate.Decide(origin, mark, state ?? Calm);

    private static void Never(CommandRefusal why, CommandOrigin origin, CommandState state)
    {
        foreach (var mark in Marks)
        {
            Assert.Equal(CommandVerdict.Never(why), Decide(origin, mark, state));
        }
    }

    private static void RunsWhateverTheMark(CommandOrigin origin, CommandState state)
    {
        foreach (var mark in Marks)
        {
            Assert.Equal(CommandVerdict.Runs, Decide(origin, mark, state));
        }
    }

    // ---- the key (the user) ----

    [Fact]
    public void Key_read_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Key, CommandMark.Read));

    [Fact]
    public void Key_open_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Key, CommandMark.Open));

    [Fact]
    public void Key_change_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Key, CommandMark.Change));

    [Fact]
    public void Key_act_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Key, CommandMark.Act));

    [Fact]
    public void Key_condition_1_a_notice_holds_every_key_but_the_ones_that_are_information_alone()
    {
        Never(CommandRefusal.Notice, CommandOrigin.Key, Calm with { NoticeOpen = true });
        // Larger and smaller text, Say the status, the mode key: handled ahead of the notice.
        RunsWhateverTheMark(CommandOrigin.Key, Calm with { NoticeOpen = true, InformationKey = true });
        // A Windows dialog has the user's keys itself; the gate asks about it for the AI tool alone.
        RunsWhateverTheMark(CommandOrigin.Key, Calm with { DialogShowing = true });
    }

    [Fact]
    public void Key_condition_2_the_key_test_names_every_key_and_carries_none_out()
    {
        Never(CommandRefusal.KeyTest, CommandOrigin.Key, Calm with { KeyTestRunning = true });
        Never(CommandRefusal.KeyTest, CommandOrigin.Key, Calm with { KeyTestRunning = true, InformationKey = true });
        // The key test's filter takes the key before the window's key handling: its reason comes first.
        Never(CommandRefusal.KeyTest, CommandOrigin.Key, Calm with { KeyTestRunning = true, NoticeOpen = true });
    }

    [Fact]
    public void Key_condition_3_the_key_acts_only_where_its_command_says()
    {
        Never(CommandRefusal.KeyNotHere, CommandOrigin.Key, Calm with { KeyDoesNotActHere = true });
        // A key with no menu item runs by its key, and a key acts whether or not its menu item is offered.
        RunsWhateverTheMark(CommandOrigin.Key, Calm with { KeyOnly = true });
        RunsWhateverTheMark(CommandOrigin.Key, Calm with { NotOffered = true });
    }

    // ---- the menu (the user) ----

    [Fact]
    public void Menu_read_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Menu, CommandMark.Read));

    [Fact]
    public void Menu_open_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Menu, CommandMark.Open));

    [Fact]
    public void Menu_change_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Menu, CommandMark.Change));

    [Fact]
    public void Menu_act_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Menu, CommandMark.Act));

    [Fact]
    public void Menu_condition_1_a_notice_disables_the_menu_bar()
    {
        Never(CommandRefusal.Notice, CommandOrigin.Menu, Calm with { NoticeOpen = true });
        // The menu item always runs, whatever the command's key would do where the focus is (condition 3).
        RunsWhateverTheMark(CommandOrigin.Menu, Calm with { KeyDoesNotActHere = true });
        // What is no menu item, or not offered, cannot be chosen.
        Never(CommandRefusal.NoMenuItem, CommandOrigin.Menu, Calm with { KeyOnly = true });
        Never(CommandRefusal.NotOffered, CommandOrigin.Menu, Calm with { NotOffered = true });
    }

    [Fact]
    public void Menu_condition_2_the_key_test_lets_no_key_reach_the_menu() =>
        Never(CommandRefusal.KeyTest, CommandOrigin.Menu, Calm with { KeyTestRunning = true });

    // ---- the command palette (the user) ----

    [Fact]
    public void Palette_read_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Palette, CommandMark.Read));

    [Fact]
    public void Palette_open_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Palette, CommandMark.Open));

    [Fact]
    public void Palette_change_runs_and_a_check_item_says_its_new_state()
    {
        Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Palette, CommandMark.Change));
        // The sentence no menu shows, the one place for its words (MenuText.CheckSaid).
        Assert.Equal("Word wrap, checked", MenuText.CheckSaid("Word wrap", true));
        Assert.Equal("Word wrap, not checked", MenuText.CheckSaid("Word wrap", false));
    }

    [Fact]
    public void Palette_act_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Palette, CommandMark.Act));

    [Fact]
    public void Palette_condition_1_the_palette_is_a_notice_and_opens_over_none() =>
        Never(CommandRefusal.Notice, CommandOrigin.Palette, Calm with { NoticeOpen = true });

    [Fact]
    public void Palette_condition_2_the_palette_does_not_open_in_the_key_test() =>
        Never(CommandRefusal.KeyTest, CommandOrigin.Palette, Calm with { KeyTestRunning = true });

    [Fact]
    public void Palette_condition_4_the_palette_offers_what_the_menus_offer_at_that_moment()
    {
        Never(CommandRefusal.NotOffered, CommandOrigin.Palette, Calm with { NotOffered = true });
        Never(CommandRefusal.NoMenuItem, CommandOrigin.Palette, Calm with { KeyOnly = true });
        RunsWhateverTheMark(CommandOrigin.Palette, Calm with { KeyDoesNotActHere = true });
    }

    // ---- a script's do step (the automated checks) ----

    [Fact]
    public void Script_read_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Script, CommandMark.Read));

    [Fact]
    public void Script_open_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Script, CommandMark.Open));

    [Fact]
    public void Script_change_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Script, CommandMark.Change));

    [Fact]
    public void Script_act_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Script, CommandMark.Act));

    [Fact]
    public void Script_condition_1_a_notice_fails_the_step_with_nothing_pressed() =>
        Never(CommandRefusal.Notice, CommandOrigin.Script, Calm with { NoticeOpen = true });

    [Fact]
    public void Script_condition_2_the_key_test_fails_the_step_with_nothing_pressed()
    {
        Never(CommandRefusal.KeyTest, CommandOrigin.Script, Calm with { KeyTestRunning = true });
        // The listing names the key test where a notice's title stands.
        Never(CommandRefusal.KeyTest, CommandOrigin.Script, Calm with { KeyTestRunning = true, NoticeOpen = true });
    }

    [Fact]
    public void Script_condition_3_a_key_that_does_not_act_where_the_focus_is_fails_the_step()
    {
        Never(CommandRefusal.KeyNotHere, CommandOrigin.Script, Calm with { KeyDoesNotActHere = true });
        // A key with no menu item is pressed as the user presses it.
        RunsWhateverTheMark(CommandOrigin.Script, Calm with { KeyOnly = true });
    }

    [Fact]
    public void Script_condition_5_only_in_a_test_run() =>
        Never(CommandRefusal.NotATestRun, CommandOrigin.Script, Calm with { NoTestRun = true });

    [Fact]
    public void Script_condition_6_in_the_AI_tools_mode_a_step_the_tools_may_run_unasked_is_the_AI_tool_s_and_an_act_stays_on_the_keyboard()
    {
        // The runner is then the paired client: the step is decided in the AI tool's row and is said.
        foreach (var mark in new[] { CommandMark.Read, CommandMark.Open, CommandMark.Change })
        {
            Assert.Equal(CommandVerdict.RunsAndIsSaid, Decide(CommandOrigin.AiTool, mark));
        }

        // Nothing answers the question for the user, so an act is pressed: this row, which asks nothing.
        Assert.Equal(CommandVerdict.AsksFirst, Decide(CommandOrigin.AiTool, CommandMark.Act));
        Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Script, CommandMark.Act));
    }

    // ---- the channel's choose (the automated checks) ----

    [Fact]
    public void Choose_read_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Choose, CommandMark.Read));

    [Fact]
    public void Choose_open_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Choose, CommandMark.Open));

    [Fact]
    public void Choose_change_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Choose, CommandMark.Change));

    [Fact]
    public void Choose_act_runs() => Assert.Equal(CommandVerdict.Runs, Decide(CommandOrigin.Choose, CommandMark.Act));

    [Fact]
    public void Choose_condition_1_a_notice_leaves_no_menu_item_to_choose()
    {
        Never(CommandRefusal.Notice, CommandOrigin.Choose, Calm with { NoticeOpen = true });
        Never(CommandRefusal.NotOffered, CommandOrigin.Choose, Calm with { NotOffered = true });
        Never(CommandRefusal.NoMenuItem, CommandOrigin.Choose, Calm with { KeyOnly = true });
    }

    [Fact]
    public void Choose_condition_2_the_key_test_holds_it()
    {
        // CL-4.8 (1), decided 2026-11-25: held as every other origin; and the key test before a notice, as for do.
        Never(CommandRefusal.KeyTest, CommandOrigin.Choose, Calm with { KeyTestRunning = true });
        Never(CommandRefusal.KeyTest, CommandOrigin.Choose, Calm with { KeyTestRunning = true, NoticeOpen = true });
    }

    [Fact]
    public void Choose_condition_5_only_in_a_test_run() =>
        Never(CommandRefusal.NotATestRun, CommandOrigin.Choose, Calm with { NoTestRun = true });

    // ---- an AI tool, axkit_window_run ----

    [Fact]
    public void AiTool_read_runs_and_is_said() => Assert.Equal(CommandVerdict.RunsAndIsSaid, Decide(CommandOrigin.AiTool, CommandMark.Read));

    [Fact]
    public void AiTool_open_runs_and_is_said() => Assert.Equal(CommandVerdict.RunsAndIsSaid, Decide(CommandOrigin.AiTool, CommandMark.Open));

    [Fact]
    public void AiTool_change_runs_and_is_said() => Assert.Equal(CommandVerdict.RunsAndIsSaid, Decide(CommandOrigin.AiTool, CommandMark.Change));

    [Fact]
    public void AiTool_act_asks_first() => Assert.Equal(CommandVerdict.AsksFirst, Decide(CommandOrigin.AiTool, CommandMark.Act));

    [Fact]
    public void AiTool_condition_1_a_notice_or_a_Windows_dialog_refuses_it()
    {
        Never(CommandRefusal.Notice, CommandOrigin.AiTool, Calm with { NoticeOpen = true });
        Never(CommandRefusal.Dialog, CommandOrigin.AiTool, Calm with { DialogShowing = true });
        Assert.Equal("The window shows the user a notice, Save changes?: commands run once the user has answered it, and AI tools never answer it.",
            McpRun.Refusal(CommandRefusal.Notice, Dispatch.AxDown, "Save changes?"));
        Assert.Equal("The window shows the user a Windows dialog: commands run once the user has closed it.",
            McpRun.Refusal(CommandRefusal.Dialog, Dispatch.AxDown, null));
    }

    [Fact]
    public void AiTool_condition_2_the_key_test_refuses_it()
    {
        Never(CommandRefusal.KeyTest, CommandOrigin.AiTool, Calm with { KeyTestRunning = true });
        Assert.Equal(KeyTest.AiToolsRefusal, McpRun.Refusal(CommandRefusal.KeyTest, Dispatch.AxDown, null));
    }

    [Fact]
    public void AiTool_condition_7_axPass_and_axClip_answer_no_AI_tool()
    {
        Never(CommandRefusal.AppNeverRun, CommandOrigin.AiTool, Calm with { AppServesNoAiTool = true });
        Assert.Equal(McpRun.AppRefusal(Dispatch.AxPass), McpRun.Refusal(CommandRefusal.AppNeverRun, Dispatch.AxPass, null));
        Assert.Equal(McpRun.AppRefusal(Dispatch.AxClip), McpRun.Refusal(CommandRefusal.AppNeverRun, Dispatch.AxClip, null));
    }

    [Fact]
    public void AiTool_condition_8_the_switches_that_decide_what_an_AI_may_do_are_never_for_AI_tools()
    {
        Never(CommandRefusal.Withheld, CommandOrigin.AiTool, Calm with { NeverForAiTools = true });
        // The user's own hands reach them as any item.
        RunsWhateverTheMark(CommandOrigin.Menu, Calm with { NeverForAiTools = true });
        RunsWhateverTheMark(CommandOrigin.Palette, Calm with { NeverForAiTools = true });
    }

    [Fact]
    public void AiTool_condition_9_a_key_with_no_menu_item_is_never_for_AI_tools() =>
        Never(CommandRefusal.NoMenuItem, CommandOrigin.AiTool, Calm with { KeyOnly = true });

    [Fact]
    public void AiTool_condition_10_the_two_switches_must_both_be_on()
    {
        Never(CommandRefusal.AiToolsOff, CommandOrigin.AiTool, Calm with { AiToolsOff = true });
        Never(CommandRefusal.RunCommandsOff, CommandOrigin.AiTool, Calm with { RunCommandsOff = true });
        Assert.Equal(McpRun.SwitchOff, McpRun.Refusal(CommandRefusal.RunCommandsOff, Dispatch.AxDown, null));
        // The switches are about AI tools alone.
        RunsWhateverTheMark(CommandOrigin.Key, Calm with { AiToolsOff = true, RunCommandsOff = true });
    }

    [Fact]
    public void AiTool_condition_11_never_under_the_user_s_hands()
    {
        Never(CommandRefusal.UserBusy, CommandOrigin.AiTool, Calm with { UserBusy = true });
        Assert.Equal(McpRun.UserBusy, McpRun.Refusal(CommandRefusal.UserBusy, Dispatch.AxDown, null));
    }

    [Fact]
    public void AiTool_condition_12_the_name_must_name_one_command_the_menus_offer_now() =>
        Never(CommandRefusal.NotOffered, CommandOrigin.AiTool, Calm with { NotOffered = true });

    [Fact]
    public void AiTool_condition_13_after_a_No_the_window_asks_no_more_for_a_minute()
    {
        Assert.Equal(CommandVerdict.Never(CommandRefusal.DeniedLately), Decide(CommandOrigin.AiTool, CommandMark.Act, Calm with { DeniedLately = true }));
        Assert.Equal("The user denied running a command a moment ago; this window asks no more within a minute.",
            McpRun.Refusal(CommandRefusal.DeniedLately, Dispatch.AxDown, null));
        // Only what would be asked is held: the other marks run as before.
        foreach (var mark in new[] { CommandMark.Read, CommandMark.Open, CommandMark.Change })
        {
            Assert.Equal(CommandVerdict.RunsAndIsSaid, Decide(CommandOrigin.AiTool, mark, Calm with { DeniedLately = true }));
        }
    }

    [Fact]
    public void AiTool_condition_14_a_Yes_runs_only_what_was_asked()
    {
        // The gate only asks: an act never runs on the gate's word, and what the window checks again at the user's
        // Yes is its own (AxKitForm.WhyNotRunForMcp, and the selection's identity, below).
        Assert.Equal(CommandAnswer.AsksFirst, Decide(CommandOrigin.AiTool, CommandMark.Act).Answer);
        Assert.Equal(McpRun.SelectionChanged, McpRun.TargetRefusal(new RunTarget("a.txt in files", @"D:\a\files\a.txt"), new RunTarget("a.txt in files", @"D:\b\files\a.txt")));
    }

    [Fact]
    public void AiTool_condition_15_every_run_is_heard()
    {
        // No cell of the row is a plain run: what runs is said, and an act is asked first.
        foreach (var mark in Marks)
        {
            Assert.NotEqual(CommandAnswer.Runs, Decide(CommandOrigin.AiTool, mark).Answer);
        }

        Assert.Equal("Claude ran Word wrap", McpRun.Said("claude-code", "&Word wrap"));
    }

    // ---- the order of the AI tool's refusals (which words a client gets when two conditions hold at once) ----

    /// <summary>The AI tool's conditions in the order the code asks them (<c>McpChannel</c>, then <c>AxKitForm.McpRunCommand</c>), each with the fact that sets it.</summary>
    private static readonly (CommandRefusal Why, Func<CommandState, CommandState> Set)[] AiToolOrder =
    [
        (CommandRefusal.AiToolsOff, state => state with { AiToolsOff = true }),
        (CommandRefusal.AppNeverRun, state => state with { AppServesNoAiTool = true }),
        (CommandRefusal.RunCommandsOff, state => state with { RunCommandsOff = true }),
        (CommandRefusal.KeyTest, state => state with { KeyTestRunning = true }),
        (CommandRefusal.Notice, state => state with { NoticeOpen = true }),
        (CommandRefusal.Dialog, state => state with { DialogShowing = true }),
        (CommandRefusal.UserBusy, state => state with { UserBusy = true }),
        (CommandRefusal.NoMenuItem, state => state with { KeyOnly = true }),
        (CommandRefusal.Withheld, state => state with { NeverForAiTools = true }),
        (CommandRefusal.NotOffered, state => state with { NotOffered = true }),
        (CommandRefusal.DeniedLately, state => state with { DeniedLately = true }),
    ];

    [Fact]
    public void Of_two_conditions_that_hold_at_once_the_AI_tool_gets_the_earlier_one_s_refusal()
    {
        for (var first = 0; first < AiToolOrder.Length; first++)
        {
            for (var second = first + 1; second < AiToolOrder.Length; second++)
            {
                var state = AiToolOrder[second].Set(AiToolOrder[first].Set(Calm));
                var verdict = Decide(CommandOrigin.AiTool, CommandMark.Act, state);
                Assert.True(verdict == CommandVerdict.Never(AiToolOrder[first].Why),
                    $"{AiToolOrder[first].Why} and {AiToolOrder[second].Why} together: expected {AiToolOrder[first].Why}, got {verdict.Why}");
            }
        }
    }

    [Fact]
    public void The_window_s_part_of_the_gate_is_the_start_of_the_whole_and_asks_nothing_of_the_command()
    {
        // AxKitForm.McpRunCommand asks Window before it reads the menus, then Decide with the same facts.
        for (var index = 0; index < AiToolOrder.Length; index++)
        {
            var state = AiToolOrder[index].Set(Calm);
            var ofWindow = AiToolOrder[index].Why is CommandRefusal.NoMenuItem or CommandRefusal.Withheld or CommandRefusal.NotOffered or CommandRefusal.DeniedLately
                ? (CommandRefusal?)null
                : AiToolOrder[index].Why;
            Assert.Equal(ofWindow, CommandGate.Window(CommandOrigin.AiTool, state));
        }

        Assert.Null(CommandGate.Window(CommandOrigin.AiTool, Calm));
    }

    [Fact]
    public void Every_refusal_the_AI_tool_s_path_can_give_has_its_words_and_the_others_have_none_there()
    {
        // The name's refusals are McpRun.Find's, which sees no withheld item, no key alone and no item not offered;
        // the first switch is the pipe's own answer.
        foreach (var why in new[] { CommandRefusal.None, CommandRefusal.AiToolsOff, CommandRefusal.NoMenuItem, CommandRefusal.Withheld, CommandRefusal.NotOffered, CommandRefusal.KeyNotHere, CommandRefusal.NotATestRun })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => McpRun.Refusal(why, Dispatch.AxDown, null));
        }

        foreach (var why in new[] { CommandRefusal.AppNeverRun, CommandRefusal.RunCommandsOff, CommandRefusal.KeyTest, CommandRefusal.Notice, CommandRefusal.Dialog, CommandRefusal.UserBusy, CommandRefusal.DeniedLately })
        {
            Assert.False(string.IsNullOrWhiteSpace(McpRun.Refusal(why, Dispatch.AxPass, "Title")));
        }
    }

    // ---- the document's table against this file ----

    /// <summary>The origins as section 3 names them, in bold at the start of each one's lines.</summary>
    private static readonly Dictionary<string, string> OriginsOfTheDocument = new()
    {
        ["The key"] = nameof(CommandOrigin.Key),
        ["The menu"] = nameof(CommandOrigin.Menu),
        ["The command palette"] = nameof(CommandOrigin.Palette),
        ["A script's `do` step"] = nameof(CommandOrigin.Script),
        ["The channel's `choose`"] = nameof(CommandOrigin.Choose),
        ["An AI tool, `axkit_window_run`"] = nameof(CommandOrigin.AiTool),
    };

    /// <summary>An origin the document names as not built: it has no cells yet.</summary>
    private const string NotBuilt = "Speech";

    [Fact]
    public void Every_cell_the_document_s_table_names_has_a_test_named_after_it()
    {
        var cells = CellsOfTheDocument();
        var tests = TestName().Matches(File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "tests", "AxKit.Tests", "CommandGateTests.cs")))
            .Select(match => match.Groups[1].Value).ToList();
        var missing = cells.Where(cell => !tests.Any(test => test.StartsWith(cell + "_", StringComparison.Ordinal))).ToList();
        Assert.True(missing.Count == 0, "docs/axKit/command-language.md section 3 names cells that no test of CommandGateTests is named after: " + string.Join(", ", missing));
    }

    [Fact]
    public void The_document_s_table_has_every_origin_of_the_gate_with_its_four_marks_and_its_conditions()
    {
        var cells = CellsOfTheDocument();
        foreach (var origin in Enum.GetNames<CommandOrigin>())
        {
            foreach (var mark in Marks)
            {
                Assert.Contains($"{origin}_{mark.ToString().ToLowerInvariant()}", cells);
            }

            Assert.Contains(cells, cell => cell.StartsWith($"{origin}_condition_", StringComparison.Ordinal));
        }

        // The cells as they stood when the gate was written; a change of the table changes this line and its tests.
        Assert.Equal(6 * 4 + 3 + 2 + 3 + 5 + 3 + 11, cells.Count);
    }

    /// <summary>
    /// The cells of section 3 by their ids: for each origin its four marks ("Key_read") and the conditions its
    /// Conditions line names ("Key_condition_1"; "7 to 15" is every number between; texts in quotes and in brackets
    /// are no part of the list). An origin this file does not know fails the reading, so a new origin cannot be
    /// written into the table without its tests.
    /// </summary>
    private static List<string> CellsOfTheDocument()
    {
        var lines = File.ReadAllLines(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "command-language.md"));
        var start = Array.FindIndex(lines, line => line.StartsWith("## 3.", StringComparison.Ordinal));
        var end = Array.FindIndex(lines, start + 1, line => line.StartsWith("The conditions:", StringComparison.Ordinal));
        Assert.True(start >= 0 && end > start, "command-language.md: section 3 or its line The conditions: was not found");

        var cells = new List<string>();
        string? origin = null;
        var conditions = new List<string>();
        var inConditions = false;

        void EndOrigin()
        {
            if (origin is null)
            {
                return;
            }

            var text = InBrackets().Replace(InQuotes().Replace(string.Join(" ", conditions), string.Empty), string.Empty);
            foreach (Match match in Numbers().Matches(text))
            {
                var from = int.Parse(match.Groups[1].Value);
                var to = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : from;
                for (var number = from; number <= to; number++)
                {
                    var cell = $"{origin}_condition_{number}";
                    if (!cells.Contains(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            conditions.Clear();
            inConditions = false;
        }

        for (var index = start + 1; index < end; index++)
        {
            var line = lines[index];
            if (OriginLine().Match(line) is { Success: true } named)
            {
                EndOrigin();
                var name = named.Groups[1].Value;
                origin = name == NotBuilt ? null : OriginsOfTheDocument.TryGetValue(name, out var known) ? known : null;
                Assert.True(origin is not null || name == NotBuilt, $"command-language.md section 3 names an origin CommandGateTests does not know: {name}");
                continue;
            }

            if (origin is null)
            {
                continue;
            }

            if (line.StartsWith("  - Conditions:", StringComparison.Ordinal))
            {
                inConditions = true;
                conditions.Add(line["  - Conditions:".Length..]);
            }
            else if (line.StartsWith("  - ", StringComparison.Ordinal))
            {
                inConditions = false;
                if (MarkLine().Match(line) is { Success: true } marks)
                {
                    cells.AddRange(marks.Groups[1].Value.Split(", ").Select(mark => $"{origin}_{mark}"));
                }
            }
            else if (inConditions && line.StartsWith("    ", StringComparison.Ordinal))
            {
                conditions.Add(line.Trim());
            }
            else if (line.Trim().Length == 0 || !line.StartsWith(' '))
            {
                EndOrigin();
                origin = null;
            }
        }

        EndOrigin();
        return cells;
    }

    [GeneratedRegex(@"public void (\w+)\(")]
    private static partial Regex TestName();

    [GeneratedRegex(@"^- \*\*(.+?)\*\*")]
    private static partial Regex OriginLine();

    [GeneratedRegex(@"^  - ((?:read|open|change|act)(?:, (?:read|open|change|act))*):")]
    private static partial Regex MarkLine();

    [GeneratedRegex("\"[^\"]*\"")]
    private static partial Regex InQuotes();

    [GeneratedRegex(@"\([^()]*\)")]
    private static partial Regex InBrackets();

    [GeneratedRegex(@"\b(\d+)(?: to (\d+))?\b")]
    private static partial Regex Numbers();
}
