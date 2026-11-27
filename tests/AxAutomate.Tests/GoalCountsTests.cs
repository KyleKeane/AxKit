using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The bounds (docs/axKit/workflow-graph.md WG-3, <see cref="GoalCounts"/> and <see cref="GraphBounds"/>): a goal's own
/// actions counted by the rule of WG-3.3, the baseline written in one order and read back, what a write holds and what
/// the check names (WG-3.4), the waypoint bound by breadth-first search (WG-3.1), and the page. That the repository's
/// page and baseline are current is <c>tools/graph-check.ps1 -Check</c>'s, in <c>tools/pages.ps1 -Check</c>.
/// </summary>
public sealed class GoalCountsTests
{
    private const string Script = "tests/scripts/axDown/01-example.txt";

    private static ListedCommand Row(string label, string menu, string key, string keyText = "") =>
        new(label, menu, key, [], keyText, "read", Checked: null, KeyOnly: false, Ai: true, Now: string.Empty);

    /// <summary>axDown's Find with its key, Word wrap without one, and Next bookmark with a key text alone.</summary>
    private static readonly IReadOnlyList<InventoryApp> Inventory =
        [new("axDown", [Row("Find", "Navigation", "ctrl+f", "Ctrl+F"), Row("Word wrap", "Options", string.Empty), Row("Next bookmark", "Navigation", string.Empty, "K in the text")])];

    private static IReadOnlyList<CountedGoal> Count(params string[] lines) => GoalCounts.Of(Script, lines, Inventory);

    private static CountedGoal Only(params string[] lines) => Assert.Single(Count(lines));

    [Fact]
    public void A_goal_counts_its_inputs_and_not_its_setup_its_checks_or_what_only_looks()
    {
        var goals = Count(
            "# Workflow: Find text in axDown",
            "file a.md text",
            "start down {temp}\\a.md",
            "keys ctrl+end",
            "# Goal: Find a word",
            "do Find",
            "expect focus {\"role\":\"Text\"}",
            "type word",
            "keys enter",
            "wait 500",
            "keys escape",
            "# Check: Find says when nothing is found",
            "do Find",
            "type nothing",
            "keys enter");

        var goal = Assert.Single(goals);
        Assert.Equal("Find text in axDown", goal.Workflow);
        Assert.Equal("Find a word", goal.Goal);
        Assert.Equal("axDown", goal.App);
        // Find (its key), the word typed with its Enter, Escape: the file, the start, the setup's key and the check are not counted.
        Assert.Equal(3, goal.Actions);
        Assert.Equal(["Find (Ctrl+F)", "Type \"word\" and Enter", "Escape"], goal.Steps);
        Assert.Empty(goal.Example);
    }

    [Fact]
    public void Each_press_of_a_keys_step_is_one_and_a_run_of_moves_with_its_Enter_is_one_choice()
    {
        var goal = Only("# Workflow: W", "start down", "# Goal: G", "keys ctrl+s f2", "keys down down", "keys ctrl+shift+down", "keys enter", "keys tab");

        // Ctrl+S, F2, the moves with their Enter, Tab.
        Assert.Equal(4, goal.Actions);
        Assert.Equal("Down, Down, Ctrl+Shift+Down (one move), Enter", goal.Steps[2]);
    }

    [Fact]
    public void A_command_without_a_key_goes_through_the_palette_and_takes_two()
    {
        var goal = Only("# Workflow: W", "start down", "# Goal: G", "do Word wrap", "menu Find", "menu Next bookmark", "do Unknown name");

        // Word wrap through the palette, Find by its key, Next bookmark by its key text, a name the inventory lacks by the key the step presses.
        Assert.Equal(5, goal.Actions);
        Assert.Equal("Word wrap (F1, its name and Enter: 2)", goal.Steps[0]);
        Assert.Equal("Next bookmark (K in the text)", goal.Steps[2]);
    }

    [Fact]
    public void The_windows_steps_are_free_and_a_step_after_iffound_is_not_counted()
    {
        var goal = Only("# Workflow: W", "# Goal: G", "start down", "adopt", "use 1", "launch down b.md", "mcp call axkit_windows", "try said Saved", "iffound keys escape", "close");

        Assert.Equal(1, goal.Actions);
        Assert.Equal(["Close the window (Alt+F4)"], goal.Steps);
    }

    [Fact]
    public void The_text_a_goal_types_before_its_first_command_is_the_example_and_not_counted()
    {
        var example = Only("# Workflow: W", "start down", "# Goal: Undo a sentence", "type A sentence.", "keys backspace enter tab", "type Second line.", "do Find", "keys ctrl+z");

        Assert.Equal(2, example.Actions);
        Assert.Equal(["Type \"A sentence.\"", "Backspace", "Enter", "Tab", "Type \"Second line.\""], example.Example);

        // Typing with no command after it is the goal itself; so is typing that comes after the first action.
        Assert.Equal(1, Only("# Workflow: W", "start calc", "# Goal: Calculate", "type 2+2", "keys enter").Actions);
        Assert.Equal(3, Only("# Workflow: W", "start down", "# Goal: G", "keys home", "type x", "do Find").Actions);
    }

    [Fact]
    public void A_script_without_a_goal_line_is_one_goal_titled_by_its_label()
    {
        var goal = Only("# Workflow: Open a file", "file a.md x", "start down {temp}\\a.md", "keys ctrl+o");

        Assert.Equal("Open a file", goal.Goal);
        Assert.Equal(1, goal.Actions);
    }

    [Fact]
    public void A_baseline_is_written_in_one_order_and_read_back_as_it_was()
    {
        IReadOnlyList<GoalCount> goals =
        [
            new("W2", "Second", "tests/scripts/b.txt", 2),
            new("W1", "First", "tests/scripts/a.txt", 5),
            new("W1", "Then", "tests/scripts/a.txt", 1),
        ];

        var text = GoalCounts.Write(goals);
        var read = GoalCounts.Read(text);

        Assert.NotNull(read);
        Assert.Equal(["First", "Then", "Second"], read.Select(goal => goal.Goal));
        Assert.Equal(text, GoalCounts.Write(read));
        Assert.Null(GoalCounts.Read("{\"format\": \"axkit-workflow-graph 1\", \"goals\": []}"));
        Assert.Empty(GoalCounts.Read(GoalCounts.Write([]))!);
    }

    private static CountedGoal Counted(string goal, int actions) => new("W", goal, Script, actions, [], []);

    [Fact]
    public void A_goal_may_get_shorter_and_never_longer()
    {
        IReadOnlyList<GoalCount> baseline = [new("W", "Shorter", Script, 5), new("W", "Grew", Script, 5), new("W", "Over now", Script, 4), new("W", "Gone", Script, 1)];
        IReadOnlyList<CountedGoal> counted = [Counted("Shorter", 3), Counted("Grew", 6), Counted("Over now", 5), Counted("New over", 7), Counted("New within", 2)];

        // A write holds what grew at its old count and leaves out a new goal over four; a gone goal is dropped.
        var held = GoalCounts.Held(counted, baseline);
        Assert.Equal(["Shorter 3", "Grew 5", "Over now 4", "New within 2"], held.Select(goal => $"{goal.Goal} {goal.Actions}"));
        Assert.Equal(5, GoalCounts.Taken(counted).Count);

        var grown = GoalCounts.Grown(counted, baseline);
        Assert.Equal(3, grown.Count);
        Assert.Contains("Grew (" + Script + "): 6 actions; it took 5.", grown[0], StringComparison.Ordinal);
        Assert.Contains("5 actions, over four; it took 4.", grown[1], StringComparison.Ordinal);
        Assert.Contains("a new goal of 7 actions, over four.", grown[2], StringComparison.Ordinal);
        Assert.Empty(GoalCounts.Grown(counted, GoalCounts.Taken(counted).ToList()));
    }

    private static GraphNode Node(string id, bool waypoint, params string[] to)
    {
        var parts = id.Split('|');
        return new GraphNode(id, "axDown", parts[0], parts[1], [], waypoint, "the " + parts[0][7..], [.. to.Select(target => new GraphEdge("escape", string.Empty, target, string.Empty, string.Empty))]);
    }

    [Fact]
    public void A_node_whose_waypoint_is_more_than_two_actions_away_is_listed()
    {
        var graph = new WorkflowGraphFile([new GraphApp("axDown", "editor", "axDown.text|none")], [],
        [
            Node("axDown.text|none", true, "axDown.text|palette"),
            Node("axDown.text|palette", false, "axDown.text|form"),
            Node("axDown.text|form", false, "axDown.text|question"),
            Node("axDown.text|question", false, "axDown.text|form", "axDown.text|none"),
            // An edge out of the window is not followed: the app's key back would make every way one action.
            Node("axDown.details|none", false, WorkflowGraph.Outside),
        ]);

        var misses = GraphBounds.Misses(graph);

        Assert.Equal(["axDown.details|none no way", "axDown.text|palette 3"], misses.Select(miss => $"{miss.From.Id} {(miss.Actions is { } count ? count.ToString() : "no way")}"));
    }

    [Fact]
    public void The_page_gives_the_totals_and_by_app_the_goals_over_four_at_four_and_the_misses()
    {
        var graph = new WorkflowGraphFile([new GraphApp("axDown", "editor", "axDown.text|none")], [], [Node("axDown.text|none", true), Node("axDown.text|menu", false)]);
        IReadOnlyList<CountedGoal> goals =
        [
            new("W", "Long", Script, 6, ["Ctrl+F", "Escape"], ["Type \"x\""]),
            new("W", "Four", Script, 4, ["Tab"], []),
            new("W", "Short", Script, 2, ["Tab"], []),
            new("W", "Calc", "tests/scripts/axCalc/01.txt", 1, ["Tab"], []),
        ];

        var page = GraphBounds.Page(goals, GraphBounds.Misses(graph), graph);

        Assert.StartsWith("# Over the bounds\n", page, StringComparison.Ordinal);
        Assert.Contains("4 goals counted: 1 over four actions, 1 at four, 2 within three.", page, StringComparison.Ordinal);
        Assert.Contains("- **W: Long**, 6 actions (`" + Script + "`): Ctrl+F; Escape. Not counted, the example: Type \"x\".", page, StringComparison.Ordinal);
        Assert.Contains("### At four actions, over the ideal three\n\n- **W: Four**, 4 actions", page, StringComparison.Ordinal);
        Assert.Contains("- From the text with the menu over it (`axDown.text|menu`) to the text (`axDown.text|none`): no way found.", page, StringComparison.Ordinal);
        // An app with nothing over the bounds has no section.
        Assert.DoesNotContain("## axCalc", page, StringComparison.Ordinal);
        Assert.EndsWith(".\n", page, StringComparison.Ordinal);
    }
}
