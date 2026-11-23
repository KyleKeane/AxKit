using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// Goals and checks in a script (docs/axKit/user-workflows.md WF-3, docs/axAutomate/SPEC.md AU-5.2b): the reader's
/// sections, their results and the report's lines, the input steps and the app each line reaches; and over the
/// repository's scripts, the rules a script is written by (WF-3.4).
/// </summary>
public sealed class GoalTests
{
    /// <summary>
    /// tests/scripts/axDown/04-find-and-go-to-line.txt as section 5.1 of user-workflows.md gives it once it is marked
    /// and moved to <c>do</c>: two goals and five checks, line for line.
    /// </summary>
    private static readonly string[] Marked = """
        # Workflow: Find text and go to a line
        # axDown, test plan section 4 (Find and go to line), driven from inside the window (docs/axDown/nvda-test-plan.md).
        # The run's own settings: no update check, whose network call logs an ERROR when GitHub cannot be reached.
        file home\AxKit\axDown.json {"checkForUpdates": false}
        file find.md # Find and go to line\n\nThe first apple is on line 3.\nNothing here.\nThe second apple is on line 5.\nLine 6.\nThe last line, line 7.

        start down {temp}\find.md
        expect focus find.md

        # Goal: Find a word in the text
        # 4.1: Ctrl+F, the field "Text to find"; Enter lands on the match and the focus is back in the text there.
        do Find
        expect focus Text to find
        type apple
        keys enter
        expect focus {"name":"find.md","role":"Text","value":"The first apple is on line 3."}
        expect state Line 3, Column 11 \u00B7
        # F3 the next match; Shift+F3 back.
        do Find next
        expect focus The second apple is on line 5.
        do Find previous
        expect focus The first apple is on line 3.
        expect quiet 1000
        # Check: A word that is not in the text is said as not found
        # A word that is not there: "Not found".
        do Find
        expect focus Text to find
        type pear
        keys enter
        expect said Not found
        expect focus find.md

        # Goal: Go to a line by its number
        # 4.2: Ctrl+G, the field named with the line and column; a number, Enter: "Line n of m", the caret at that line's start.
        do Go to line
        expect focus Go to line (now line 3, column 11)
        type 5
        keys enter
        expect said ! Line 5 of 7
        expect focus {"name":"find.md","role":"Text","value":"The second apple is on line 5."}
        expect state Line 5, Column 1 \u00B7
        # Check: A number past the end lands on the last line
        # A number past the end lands on the last line.
        do Go to line
        expect focus Go to line (now line 5, column 1)
        type 99
        keys enter
        expect said ! Line 7 of 7
        expect focus The last line, line 7.
        # Check: A word in place of a number is refused and the caret stays
        # A word: "Not a line number", the caret where it was.
        do Go to line
        type five
        keys enter
        expect said ! Not a line number
        expect focus The last line, line 7.
        expect state Line 7, Column 1 \u00B7
        # Check: Escape in the field returns to the text where it was
        # Escape returns to the text where it was.
        do Go to line
        expect focus Go to line (now line 7, column 1)
        keys escape
        expect focus {"name":"find.md","role":"Text","value":"The last line, line 7."}

        # Check: The status bar holds the name, the line and column, the encoding and the line ending
        # 4.3: the status bar holds the name, "Line n, Column m", the encoding and the line ending (NVDA+End reads it).
        expect state "status":["find.md","Line 7, Column 1 \u00B7 UTF-8 \u00B7 CRLF"]
        """.ReplaceLineEndings("\n").Split('\n');

    private static IReadOnlyList<Goal> MarkedGoals() => ScriptReader.Goals(Marked, "Find text and go to a line");

    [Fact]
    public void A_goal_line_starts_a_goal_that_runs_to_the_next_goal_line()
    {
        string[] lines =
        [
            "# Workflow: Add, clear and read",
            "start calc",
            "# Goal: Add two numbers",
            "type 1+1",
            "keys enter",
            "",
            "# Goal: Clear the field",
            "keys escape",
            "# Goal: Read the last answer",
            "keys ctrl+q",
            "expect said 2",
        ];
        Assert.Equal(
            [
                new Goal("Add two numbers", 3, 6, false, true, false),
                new Goal("Clear the field", 7, 8, false, true, false),
                new Goal("Read the last answer", 9, 11, false, true, false),
            ],
            ScriptReader.Goals(lines, "Add, clear and read"));

        Assert.Equal(67, Marked.Length);
        Assert.Equal(
            [(10, 23, false), (24, 32, true), (33, 41, false), (42, 49, true), (50, 57, true), (58, 64, true), (65, 67, true)],
            MarkedGoals().Select(goal => (goal.FirstLine, goal.LastLine, goal.Check)));
    }

    [Fact]
    public void The_lines_before_the_first_goal_are_the_setup()
    {
        var goals = MarkedGoals();
        Assert.Null(ScriptReader.GoalAt(goals, 1));
        Assert.Null(ScriptReader.GoalAt(goals, 7));
        Assert.Null(ScriptReader.GoalAt(goals, 9));
        Assert.Equal("Find a word in the text", ScriptReader.GoalAt(goals, 10)?.Title);
        Assert.Equal("Find a word in the text", ScriptReader.GoalAt(goals, 23)?.Title);
        Assert.Equal("A word that is not in the text is said as not found", ScriptReader.GoalAt(goals, 24)?.Title);
        Assert.Equal("The status bar holds the name, the line and column, the encoding and the line ending", ScriptReader.GoalAt(goals, 67)?.Title);
    }

    [Fact]
    public void A_script_with_no_goal_line_is_one_goal_named_by_its_label()
    {
        string[] lines = ["# Workflow: Calculate", "# axCalc: the field.", "start calc", "type 1+1", "keys enter", ""];
        var workflow = ScriptReader.Parse(lines, "01-basics", @"C:\scripts\01-basics.txt");
        Assert.Equal([new Goal("Calculate", 1, 6, false, false, false)], workflow.Goals);
        Assert.Equal(workflow.Goals[0], ScriptReader.GoalAt(workflow.Goals, 1));
        Assert.Equal([new Goal("02-plain", 1, 1, false, false, false)], ScriptReader.Parse(["start calc"], "02-plain", @"C:\scripts\02-plain.txt").Goals);
    }

    [Fact]
    public void Goal_is_read_in_any_case_and_needs_a_title()
    {
        Assert.Equal("x", ScriptReader.GoalOf("goal: x"));
        Assert.Equal("Find a word", ScriptReader.GoalOf("GOAL:   Find a word  "));
        Assert.Null(ScriptReader.GoalOf("Goal:"));
        Assert.Null(ScriptReader.GoalOf("Goal:   "));
        Assert.Null(ScriptReader.GoalOf("Goals: x"));
        Assert.Equal(["x"], ScriptReader.Goals(["# Workflow: W", "start calc", "# goal: x", "keys enter"], "W").Select(goal => goal.Title));

        var plain = ScriptReader.Goals(["# Workflow: W", "start calc", "# Goal:", "keys enter"], "W");
        Assert.Equal([new Goal("W", 1, 4, false, false, false)], plain);
    }

    [Fact]
    public void A_check_line_starts_a_section_that_is_no_goal()
    {
        string[] lines =
        [
            "# Workflow: W",
            "start calc",
            "# Goal: Add two numbers",
            "type 1+1",
            "keys enter",
            "# Check: An empty field says empty",
            "keys escape",
            "# check: x",
            "keys escape",
            "# Check:",
            "keys escape",
        ];
        Assert.Equal(
            [
                new Goal("Add two numbers", 3, 5, false, true, false),
                new Goal("An empty field says empty", 6, 7, false, true, true),
                new Goal("x", 8, 11, false, true, true),
            ],
            ScriptReader.Goals(lines, "W"));
        Assert.Equal("x", ScriptReader.CheckOf("CHECK: x"));
        Assert.Null(ScriptReader.CheckOf("Check:"));
        Assert.Null(ScriptReader.CheckOf("check off (GitHub's limit)"));
        Assert.Null(ScriptReader.CheckOf("Checked, the box holds the fuller form"));
    }

    [Fact]
    public void A_stable_line_among_the_goals_own_comments_marks_it()
    {
        string[] lines = ["# Workflow: W", "start calc", "# Goal: Add two numbers", "# The plan's 2.1.", "# Stable", "type 1+1", "# Check: Empty", "# stable", "keys escape"];
        Assert.Equal([true, true], ScriptReader.Goals(lines, "W").Select(goal => goal.Stable));
    }

    [Fact]
    public void A_stable_line_after_a_step_marks_nothing()
    {
        string[] afterStep = ["# Workflow: W", "start calc", "# Goal: Add two numbers", "type 1+1", "# Stable", "keys enter"];
        Assert.False(ScriptReader.Goals(afterStep, "W")[0].Stable);
        string[] afterBlank = ["# Workflow: W", "start calc", "# Goal: Add two numbers", "", "# Stable", "type 1+1"];
        Assert.False(ScriptReader.Goals(afterBlank, "W")[0].Stable);
        string[] wordInLine = ["# Workflow: W", "start calc", "# Goal: Add two numbers", "# Stable since 2026", "type 1+1"];
        Assert.False(ScriptReader.Goals(wordInLine, "W")[0].Stable);
    }

    [Fact]
    public void A_stable_line_in_the_header_marks_the_one_goal_and_is_not_among_what_it_covers()
    {
        var workflow = ScriptReader.Parse(["# Workflow: Calculate", "# Stable", "# axCalc: the field.", "start calc", "# Stable", "keys enter"], "01-basics", @"C:\scripts\01-basics.txt");
        Assert.True(workflow.Goals[0].Stable);
        Assert.False(workflow.Goals[0].Named);
        Assert.Equal("axCalc: the field.", workflow.Covers);

        var later = ScriptReader.Parse(["# Workflow: Calculate", "start calc", "# Stable", "keys enter"], "01-basics", @"C:\scripts\01-basics.txt");
        Assert.False(later.Goals[0].Stable);
        Assert.Equal("Stable", later.Steps[1].Note);
    }

    [Fact]
    public void A_goal_line_ends_the_header_and_is_not_among_what_the_workflow_covers()
    {
        string[] lines = ["# Workflow: Calculate", "# What it covers.", "# Goal: Add two numbers", "# The plan's 2.1.", "start calc", "type 1+1"];
        var workflow = ScriptReader.Parse(lines, "01-basics", @"C:\scripts\01-basics.txt");
        Assert.Equal("Calculate", workflow.Title);
        Assert.Equal("What it covers.", workflow.Covers);
        Assert.Equal("Goal: Add two numbers The plan's 2.1.", workflow.Steps[0].Note);
        Assert.Equal(3, workflow.Steps[0].Line);
        Assert.Equal([new Goal("Add two numbers", 3, 6, false, true, false)], workflow.Goals);

        // The same script without the goal line: the comment after it is what the workflow covers, as before.
        var plain = ScriptReader.Parse(["# Workflow: Calculate", "# What it covers.", "# The plan's 2.1.", "start calc"], "01-basics", @"C:\scripts\01-basics.txt");
        Assert.Equal("What it covers. The plan's 2.1.", plain.Covers);
    }

    [Fact]
    public void The_needs_line_is_read_and_stays_in_what_the_workflow_covers()
    {
        var workflow = ScriptReader.Parse(["# Workflow: See the changes", "# Needs: Git installed.", "# axGIT: the changes.", "start git"], "03-changes", @"C:\scripts\03-changes.txt");
        Assert.Equal("Git installed.", workflow.Needs);
        Assert.Equal("Needs: Git installed. axGIT: the changes.", workflow.Covers);
        Assert.Equal("Git installed.", ScriptReader.NeedsOf("needs: Git installed."));
        Assert.Equal(string.Empty, ScriptReader.Parse(["# Workflow: W", "start calc", "# Needs: later", "keys enter"], "w", @"C:\w.txt").Needs);
    }

    [Fact]
    public void A_goal_line_reads_as_a_note_for_the_window()
    {
        var workflow = ScriptReader.Parse(Marked, "04-find-and-go-to-line", @"C:\scripts\04-find-and-go-to-line.txt");
        Assert.Null(workflow.Problem);
        var note = Assert.Single(workflow.Steps, step => step.IsNote && step.Line == 10);
        Assert.StartsWith("Goal: Find a word in the text 4.1: Ctrl+F", note.Note, StringComparison.Ordinal);
        var check = Assert.Single(workflow.Steps, step => step.IsNote && step.Line == 24);
        Assert.StartsWith("Check: A word that is not in the text is said as not found", check.Note, StringComparison.Ordinal);
        Assert.StartsWith("Note: Goal: Find a word in the text", StepText.Rows(workflow).First(row => ReferenceEquals(row.Step, note)).Label, StringComparison.Ordinal);
        Assert.Equal(7, workflow.Goals.Count);
    }

    [Fact]
    public void Goals_before_the_failed_line_passed_its_goal_failed_the_rest_were_not_run()
    {
        var results = GoalResults.Of(MarkedGoals(), 39, int.MaxValue, []);
        Assert.Equal(
            [GoalResults.Passed, GoalResults.Passed, GoalResults.Failed, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun],
            results.Select(result => result.Outcome));
        Assert.Equal(39, results[2].FailedLine);
        Assert.Equal("at line 39", results[2].Detail);
        Assert.Equal("2 passed, 1 failed, 4 not run", GoalResults.Counts(results));

        var passed = GoalResults.Of(MarkedGoals(), 0, int.MaxValue, []);
        Assert.All(passed, result => Assert.Equal(GoalResults.Passed, result.Outcome));
        Assert.Equal("7 passed", GoalResults.Counts(passed));
    }

    [Fact]
    public void A_run_stopped_before_its_end_did_not_run_the_sections_it_did_not_finish()
    {
        // Stopped before line 31: every line up to 30 was played, so the check of lines 24 to 32 was not run to its end.
        var results = GoalResults.Of(MarkedGoals(), 0, 30, []);
        Assert.Equal(
            [GoalResults.Passed, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun, GoalResults.NotRun],
            results.Select(result => result.Outcome));
    }

    [Fact]
    public void A_step_that_fails_in_the_setup_runs_no_goal()
    {
        var results = GoalResults.Of(MarkedGoals(), 7, int.MaxValue, ["AxDown.log: ERROR x"]);
        Assert.All(results, result => Assert.Equal(GoalResults.NotRun, result.Outcome));
        Assert.Equal(
            [
                "The setup failed at line 7",
                "Goals: 0 passed, 7 not run",
                "  not run  Find a word in the text",
            ],
            GoalResults.Lines(results, 7).Take(3));
    }

    [Fact]
    public void A_failure_after_the_steps_fails_every_goal_that_passed()
    {
        var all = GoalResults.Of(MarkedGoals(), 0, int.MaxValue, ["AxDown.log: ERROR x", "window 2 ended by itself"]);
        Assert.All(all, result => Assert.Equal(GoalResults.Failed, result.Outcome));
        Assert.All(all, result => Assert.Equal(0, result.FailedLine));
        Assert.Equal("  FAILED   Find a word in the text, after the steps: AxDown.log: ERROR x", GoalResults.Lines(all, 0)[1]);

        var some = GoalResults.Of(MarkedGoals(), 39, int.MaxValue, ["AxDown.log: ERROR x"]);
        Assert.Equal(
            ["after the steps: AxDown.log: ERROR x", "after the steps: AxDown.log: ERROR x", "at line 39", "", "", "", ""],
            some.Select(result => result.Detail));
        Assert.Equal("0 passed, 3 failed, 4 not run", GoalResults.Counts(some));
    }

    [Fact]
    public void A_trace_that_differs_fails_its_own_section_and_no_other()
    {
        // WF-3.9, WF-5.5: every step passed; the kept trace of one stable goal differs.
        var passed = GoalResults.Of(MarkedGoals(), 0, int.MaxValue, []);
        var title = passed[2].Goal.Title;
        var results = GoalResults.WithFailed(passed, title, "after the steps: its trace differs from the kept one at line 3");
        Assert.Equal(GoalResults.Failed, results[2].Outcome);
        Assert.Equal(0, results[2].FailedLine);
        Assert.Equal("after the steps: its trace differs from the kept one at line 3", results[2].Detail);
        Assert.Equal(passed.Where((_, index) => index != 2), results.Where((_, index) => index != 2));
        Assert.Equal($"{passed.Count - 1} passed, 1 failed", GoalResults.Counts(results));
        Assert.Equal($"  FAILED   {title}, after the steps: its trace differs from the kept one at line 3", GoalResults.Lines(results, 0)[3]);

        // The results given are not changed, and a title no section has changes nothing.
        Assert.All(passed, result => Assert.Equal(GoalResults.Passed, result.Outcome));
        Assert.Equal(passed, GoalResults.WithFailed(passed, "No such section", "after the steps: x"));

        // Only a section that had passed: one that failed at its own line keeps that, one not run stays not run.
        var stopped = GoalResults.Of(MarkedGoals(), 39, int.MaxValue, []);
        Assert.Equal(stopped, GoalResults.WithFailed(stopped, stopped[2].Goal.Title, "after the steps: x"));
        Assert.Equal(stopped, GoalResults.WithFailed(stopped, stopped[4].Goal.Title, "after the steps: x"));
    }

    [Fact]
    public void The_reports_lines_count_the_goals_and_name_each_with_its_result()
    {
        var goals = MarkedGoals();
        Assert.Equal("-- Setup", GoalResults.Heading(ScriptReader.GoalAt(goals, 4)));
        Assert.Equal("-- Goal: Find a word in the text", GoalResults.Heading(ScriptReader.GoalAt(goals, 12)));
        Assert.Equal("-- Check: A word that is not in the text is said as not found", GoalResults.Heading(ScriptReader.GoalAt(goals, 26)));
        Assert.Equal("-- Goal: Go to a line by its number", GoalResults.Heading(ScriptReader.GoalAt(goals, 35)));

        // The text of section 5.1, from "Goals:" on, for the run in which line 39 fails.
        Assert.Equal(
            [
                "Goals: 2 passed, 1 failed, 4 not run",
                "  passed   Find a word in the text",
                "  passed   check: A word that is not in the text is said as not found",
                "  FAILED   Go to a line by its number, at line 39",
                "  not run  check: A number past the end lands on the last line",
                "  not run  check: A word in place of a number is refused and the caret stays",
                "  not run  check: Escape in the field returns to the text where it was",
                "  not run  check: The status bar holds the name, the line and column, the encoding and the line ending",
            ],
            GoalResults.Lines(GoalResults.Of(goals, 39, int.MaxValue, []), 0));
    }

    [Fact]
    public void A_sections_line_says_how_many_inputs_went_through_the_tools()
    {
        // WF-9.5: in the AI tools mode a section's line ends with the count; a failed one after its line; a section
        // none of whose inputs was played (not run, or it only looks) says nothing of them.
        var results = GoalResults.Of(MarkedGoals(), 39, int.MaxValue, []).ToList();
        results[0] = results[0] with { Inputs = 5, ToolInputs = 3 };
        results[1] = results[1] with { Inputs = 3, ToolInputs = 1 };
        results[2] = results[2] with { Inputs = 3, ToolInputs = 1 };
        var lines = GoalResults.Lines(results, 0);
        Assert.Equal("  passed   Find a word in the text, through the AI tools: 3 of 5 inputs", lines[1]);
        Assert.Equal("  passed   check: A word that is not in the text is said as not found, through the AI tools: 1 of 3 inputs", lines[2]);
        Assert.Equal("  FAILED   Go to a line by its number, at line 39, through the AI tools: 1 of 3 inputs", lines[3]);
        Assert.Equal("  not run  check: A number past the end lands on the last line", lines[4]);

        // Inputs played and none through the tools: said, as 0.
        Assert.Equal("  passed   Find a word in the text, through the AI tools: 0 of 5 inputs", GoalResults.Lines([results[0] with { ToolInputs = 0 }], 0)[1]);

        // Failing a section after the steps keeps its counts.
        Assert.Equal(3, GoalResults.WithFailed(results, results[0].Goal.Title, "after the steps: x")[0].ToolInputs);
    }

    [Fact]
    public void A_line_is_an_input_step_by_its_step_word()
    {
        string[] inputs =
        [
            "do Find", "keys ctrl+f", "type apple", "menu Word wrap", "start down x", "launch help", "adopt", "use 1", "close",
            "mcp call axkit_windows", "mcp call axkit_window_run {\"window\":\"{window}\"}", "iffound keys enter", "iffound do Find",
            "iffound close", "  keys enter  ",
        ];
        Assert.All(inputs, line => Assert.True(ScriptReader.IsInput(line), line));

        string[] others =
        [
            "expect said Not found", "expect focus x", "file a.txt x", "wait 100", "mcp start", "# keys enter", "#", "", "said",
            "check commands", "try said trust", "run git init", "mkdir a", "refuse said x", "allow error x", "iffound wait 1500",
            "keyss enter", "mcp", "starting",
        ];
        Assert.All(others, line => Assert.False(ScriptReader.IsInput(line), line));
    }

    [Fact]
    public void A_start_names_the_app_of_the_lines_after_it()
    {
        string[] lines =
        [
            "# Workflow: W",
            "file a.txt x",
            "start down x",
            "keys ctrl+f",
            "start file {temp}",
            "use 1",
            "keys enter",
            "adopt",
            "keys enter",
            "use 2",
            "start {temp}\\x.txt",
            "use 4",
            "use 9",
            "iffound start Calc",
            "launch help",
            "# a comment",
        ];
        Assert.Equal(
            [null, null, "axDown", "axDown", "axFile", "axDown", "axDown", null, null, "axFile", null, null, null, "axCalc", "axCalc", "axCalc"],
            ScriptApps.ByLine(lines));
    }

    [Fact]
    public void No_script_of_the_repository_has_two_sections_of_one_title()
    {
        var repeated = RepositoryScripts()
            .SelectMany(path => ScriptReader.Read(path).Goals
                .GroupBy(goal => goal.Title, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => $"{path}: {group.Key}"));
        Assert.Empty(repeated);
    }

    [Fact]
    public void Every_goal_of_the_repository_holds_an_input_step()
    {
        // WF-3.4, WF-D-28: a section a goal line starts holds something the user does; a section that only looks is a check.
        var empty = new List<string>();
        foreach (var path in RepositoryScripts())
        {
            var lines = File.ReadAllLines(path);
            foreach (var goal in ScriptReader.Parse(lines, Path.GetFileNameWithoutExtension(path), path).Goals.Where(goal => goal.Named && !goal.Check))
            {
                if (!lines.Skip(goal.FirstLine - 1).Take(goal.LastLine - goal.FirstLine + 1).Any(ScriptReader.IsInput))
                {
                    empty.Add($"{path}, line {goal.FirstLine}: {goal.Title}");
                }
            }
        }

        Assert.Empty(empty);
    }

    /// <summary>
    /// A workflow's, a goal's and a check's title goes into the workflow record as it is, and the record holds no
    /// backslash (WF-6.4, <c>RepositoryWorkflowTests.The_record_is_well_formed</c>): a double quote in a title is
    /// written there with one. Found only after an NVDA pass had recorded such a title (2026-11-23), which failed the
    /// record's test on main; held here, where the script is written. Name a word in a title without quotes.
    /// </summary>
    [Fact]
    public void No_title_of_the_repository_holds_a_double_quote_or_a_backslash()
    {
        var marked = new List<string>();
        foreach (var path in RepositoryScripts())
        {
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if ((line.StartsWith("# Workflow:", StringComparison.Ordinal) || line.StartsWith("# Goal:", StringComparison.Ordinal) || line.StartsWith("# Check:", StringComparison.Ordinal))
                    && (line.Contains('"') || line.Contains('\\')))
                {
                    marked.Add($"{path}, line {i + 1}: {line}");
                }
            }
        }

        Assert.Empty(marked);
    }

    [Fact]
    public void A_goal_without_an_input_step_is_found()
    {
        // The check above, on a script that breaks it: the goal of line 3 only looks.
        string[] lines = ["# Workflow: W", "start calc", "# Goal: See the field", "expect focus Expression", "# Goal: Add", "type 1+1"];
        var goals = ScriptReader.Goals(lines, "W");
        Assert.Equal(
            [false, true],
            goals.Select(goal => lines.Skip(goal.FirstLine - 1).Take(goal.LastLine - goal.FirstLine + 1).Any(ScriptReader.IsInput)));
    }

    /// <summary>
    /// A live script (docs/axKit/automation.md): its needs line names Claude Code signed in, the network or GitHub.
    /// A run of a folder leaves such a script out unless --live says it is meant (2026-11-22: a folder's run, meant
    /// as a check of the windows, started the real Claude Code for every live script of axClaude).
    /// </summary>
    [Theory]
    [InlineData("Claude Code signed in, and the network.", true)]
    [InlineData("Claude Code signed in and a live model.", true)]
    [InlineData("The network: GitHub's API.", true)]
    [InlineData("GitHub.", true)]
    [InlineData("no Claude Code, no network: the stand-in plays the turn.", false)]
    [InlineData("Git installed.", false)]
    [InlineData("KeePassXC installed in C:\\Program Files\\KeePassXC.", false)]
    [InlineData("", false)]
    public void A_needs_line_says_whether_its_script_is_live(string needs, bool live) =>
        Assert.Equal(live, ScriptReader.IsLive(needs));

    /// <summary>The runner and the PowerShell helper that leaves live scripts out of the recorded passes hold the same three needs.</summary>
    [Fact]
    public void The_runner_and_the_window_passes_agree_on_what_is_live()
    {
        var helper = File.ReadAllText(Path.Combine(RepositoryRoot(), "tools", "window-run.ps1"));
        Assert.Contains("$script:LiveNeed = '" + string.Join("|", ScriptReader.LiveNeeds) + "'", helper);
    }

    /// <summary>The needs line is read from the comment lines at a script's head alone, and the repository has live scripts for the guard to hold back.</summary>
    [Fact]
    public void The_live_scripts_of_the_repository_are_found_by_their_needs_line()
    {
        var live = RepositoryScripts().Where(path => ScriptReader.IsLive(ScriptReader.NeedsOfFile(path))).Select(Path.GetFileName).ToList();
        Assert.Contains("31-hooks-live.txt", live);
        Assert.Contains("48-stream-live.txt", live);
        Assert.DoesNotContain("29-stand-in-turn.txt", live);
        Assert.DoesNotContain("01-queue.txt", live);
    }

    private static string RepositoryRoot()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        return root;
    }

    /// <summary>Every script of tests/scripts and tests/nvda-scripts.</summary>
    private static List<string> RepositoryScripts()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var scripts = Directory.GetFiles(Path.Combine(root, "tests", "scripts"), "*.txt", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests", "nvda-scripts"), "*.txt", SearchOption.AllDirectories))
            .ToList();
        Assert.NotEmpty(scripts);
        return scripts;
    }
}
