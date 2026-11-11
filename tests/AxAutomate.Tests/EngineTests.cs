using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>The engine's pure parts (docs/axAutomate/SPEC.md AU-5.1, AU-5.2): a workflow as the lines the runner plays, and an expectation's marks.</summary>
public sealed class EngineTests
{
    private static readonly string[] Script =
    [
        "# axDown: the palette.",
        "",
        "start down",
        "keys ctrl+shift+p",
        "# The trust question may come.",
        "try said trust",
        "iffound keys enter",
        "  close  ",
    ];

    [Fact]
    public void A_script_plays_its_own_lines_numbered_by_their_line_each_with_its_step()
    {
        var workflow = ScriptReader.Parse(Script, "02-palette", @"C:\scripts\02-palette.txt");
        var lines = ScriptLines.FromScript(Script, workflow);
        Assert.Equal(["start down", "keys ctrl+shift+p", "try said trust", "iffound keys enter", "close"], lines.Select(line => line.Text));
        Assert.Equal([3, 4, 6, 7, 8], lines.Select(line => line.Number));
        Assert.True(workflow.Steps[2].IsNote);
        Assert.Same(workflow.Steps[3], lines[2].Step);
        Assert.Same(workflow.Steps[3].Then[0], lines[3].Step);
        Assert.Equal("keys", lines[3].Step!.Do);
    }

    [Fact]
    public void A_workflow_file_is_written_as_the_same_lines()
    {
        var workflow = WorkflowFile.Parse("""
            {"format": "axkit-workflow 1", "title": "Palette", "steps": [
              {"do": "file", "path": "my notes.md", "text": "# Notes\nOne line"},
              {"note": "Then the window."},
              {"do": "start", "app": "down", "path": "{temp}\\my notes.md"},
              {"do": "wait", "ms": 1500},
              {"do": "expect said", "text": "trust", "on failure": "continue", "then": [{"do": "keys", "keys": "enter"}]},
              {"do": "menu", "name": "Close all windows", "report": "Not closed", "then": [{"do": "expect closed"}]}
            ]}
            """, "palette", @"C:\flows\palette.workflow.json");
        var (lines, refusal) = ScriptLines.FromWorkflow(workflow);
        Assert.Null(refusal);
        Assert.Equal(
        [
            "file \"my notes.md\" # Notes\\nOne line",
            "start down {temp}\\my notes.md",
            "wait 1500",
            "try said trust",
            "iffound keys enter",
            "menu Close all windows",
            "expect closed",
        ], lines.Select(line => line.Text));
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], lines.Select(line => line.Number));
        Assert.Same(workflow.Steps[5], lines[5].Step);
    }

    [Fact]
    public void What_this_version_cannot_play_makes_the_workflow_unrunnable_with_the_step_said()
    {
        static string? Refusal(string steps) =>
            ScriptLines.Prepare(WorkflowFile.Parse($$"""{"format": "axkit-workflow 1", "steps": [{{steps}}]}""", "w", @"C:\w.workflow.json"), nvda: false).Refusal;

        Assert.Null(Refusal("""{"do": "keys", "keys": "f1"}"""));
        Assert.Contains("otherwise", Refusal("""{"do": "keys", "keys": "f1"}, {"do": "expect said", "text": "x", "otherwise": [{"do": "close"}]}"""));
        Assert.Contains("step 1 goes on when it fails", Refusal("""{"do": "keys", "keys": "f1", "on failure": "continue"}"""));
        Assert.Contains("fly is not one the runner knows", Refusal("""{"do": "fly"}"""));
        Assert.Contains("needs NVDA", Refusal("""{"do": "expect spoken", "text": "x"}"""));
        Assert.Contains("not all understood", ScriptLines.Prepare(WorkflowFile.Parse("{}", "w", @"C:\w.workflow.json"), nvda: false).Refusal);
    }

    [Fact]
    public void A_step_whose_number_is_not_one_makes_the_workflow_unrunnable()
    {
        static string? Script(params string[] lines) => ScriptLines.CannotRun(ScriptReader.Parse(lines, "w", @"C:\w.txt"), nvda: false);
        static string? Json(string steps) =>
            ScriptLines.CannotRun(WorkflowFile.Parse($$"""{"format": "axkit-workflow 1", "steps": [{{steps}}]}""", "w", @"C:\w.workflow.json"), nvda: false);

        Assert.Null(Script("start down", "wait 1500", "use 1", "silence a.wav 500", "expect quiet 2000", "close"));
        Assert.Contains("wait 2s", Script("start down", "wait 2s"));
        Assert.Contains("use first", Script("use first"));
        Assert.Contains("use 0", Script("use 0"));
        Assert.Contains("silence a.wav", Script("silence a.wav"));
        Assert.Contains("expect quiet soon", Script("expect quiet soon"));
        Assert.Contains("\"wait x\"", Script("try said x", "iffound wait x"));
        Assert.Null(Json("""{"do": "wait", "ms": 1500}"""));
        Assert.Contains("whole number of milliseconds", Json("""{"do": "wait"}"""));
        Assert.Contains("whole number of milliseconds", ScriptLines.Prepare(WorkflowFile.Parse("""{"format": "axkit-workflow 1", "steps": [{"do": "wait", "ms": "soon"}]}""", "w", @"C:\w.workflow.json"), nvda: false).Refusal);
        Assert.Null(ScriptLines.BadNumber("keys f5"));
    }

    [Fact]
    public void A_bad_number_in_a_played_line_is_an_error_with_the_line_said()
    {
        // The engine's own check, for a line no reader checked (the command line plays a script's lines as they are):
        // Run.Step fails the step on it, through ScriptRunner's catch, and never plays it.
        Assert.Equal(1500, Run.Number("1500", "wait 1500"));
        var error = Assert.Throws<ArgumentException>(() => Run.Number("2s", "wait 2s"));
        Assert.Contains("\"wait 2s\" needs a whole number", error.Message);
        Assert.Throws<ArgumentException>(() => Run.Number("-5", "wait -5"));
    }

    [Fact]
    public void From_the_window_a_file_step_must_stay_in_the_runs_folder()
    {
        static string? Script(params string[] lines) => ScriptLines.CannotRun(ScriptReader.Parse(lines, "w", @"C:\w.txt"), nvda: false, confined: true);

        Assert.Null(Script("mkdir flows", "file flows\\a.md text", "file {temp}\\b.md text", "silence s.wav 500", "file \"my notes.md\" x", "file a\\..\\b.md x"));
        Assert.Equal("step 2 writes outside the run's folder: C:\\Users\\x.md", Script("start down", "file C:\\Users\\x.md text"));
        Assert.Contains("step 1 writes outside", Script("file ..\\x.md text"));
        Assert.Contains("step 1 writes outside", Script("mkdir {temp}\\..\\elsewhere"));
        Assert.Contains("step 1 writes outside", Script("silence \\x.wav 500"));
        Assert.Contains("step 1 writes outside", Script("file \\u002e\\u002e\\x.md text"));
        Assert.Contains("step 1.1 writes outside", Script("try said x", "iffound file ..\\x.md y"));
        Assert.Contains("step 1 writes outside", ScriptLines.Prepare(WorkflowFile.Parse("""{"format": "axkit-workflow 1", "steps": [{"do": "mkdir", "path": "D:\\x"}]}""", "w", @"C:\w.workflow.json"), nvda: false, confined: true).Refusal);

        // The command line runs its scripts as they are.
        Assert.Null(ScriptLines.CannotRun(ScriptReader.Parse(["file C:\\x.md y"], "w", @"C:\w.txt"), nvda: false));
    }

    [Fact]
    public void The_run_steps_command_lines_are_named_each_once()
    {
        var lines = ScriptLines.FromScript(["run git init", "keys f5", "try said x", "iffound run git add .", "run git init"], null);
        Assert.Equal(["git init", "git add ."], ScriptLines.Commands(lines));
        Assert.Empty(ScriptLines.Commands(ScriptLines.FromScript(["start down", "close"], null)));
    }
    [Fact]
    public void Every_script_of_the_repository_can_run_from_the_window()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var scripts = Directory.GetFiles(Path.Combine(root, "tests", "scripts"), "*.txt", SearchOption.AllDirectories);
        Assert.NotEmpty(scripts);
        var refused = scripts.Select(ScriptReader.Read)
            .Select(workflow => (workflow.Path, ScriptLines.Prepare(workflow, nvda: false, confined: true).Refusal))
            .Where(pair => pair.Refusal is not null)
            .Select(pair => $"{pair.Path}: {pair.Refusal}");
        Assert.Empty(refused);
    }

    [Fact]
    public void A_run_is_said_as_a_failing_step_and_one_line_at_its_end()
    {
        var workflow = ScriptReader.Parse(Script, "02-palette", @"C:\scripts\02-palette.txt");
        var rows = StepText.Rows(workflow);
        var lines = ScriptLines.FromScript(Script, workflow);

        var enter = new LineResult(lines[3], false, "not within 10 s; seen: nothing");
        var row = RunSentences.RowOf(rows, enter.Line.Step);
        Assert.Equal("3.1", row!.Number);
        Assert.Equal("Step 3.1 failed: Press Enter. Not within 10 s; seen: nothing.", RunSentences.StepFailed(row, enter));

        var (said, outcome) = RunSentences.Ended(workflow, new ScriptResult(false, string.Empty, enter, [], false), row);
        Assert.Equal("02-palette failed at step 3.1", said);
        Assert.Equal(new RunOutcome("failed at step 3.1", "3.1", "Not within 10 s; seen: nothing."), outcome);

        Assert.Equal("02-palette passed, 5 steps", RunSentences.Ended(workflow, new ScriptResult(true, string.Empty, null, [], false), null).Said);
        Assert.Equal("02-palette stopped", RunSentences.Ended(workflow, new ScriptResult(true, string.Empty, null, [], true), null).Said);
        Assert.Equal("02-palette failed after its steps: AxDown.log: ERROR x.", RunSentences.Ended(workflow, new ScriptResult(false, string.Empty, null, ["AxDown.log: ERROR x"], false), null).Said);
        Assert.Equal("Run finished: 3 of 5 workflows passed, 1 failed, 1 could not run", RunSentences.FolderEnded(3, 1, 1, stopped: false));
        Assert.Equal("Run stopped at 02-x: 1 passed, 3 not run", RunSentences.FolderEnded(1, 0, 0, stopped: true, stoppedAt: "02-x", notRun: 3));
        Assert.Equal("Run stopped: 2 passed, 1 failed, 2 not run", RunSentences.FolderEnded(2, 1, 0, stopped: true, stoppedAt: null, notRun: 2));
        Assert.Equal("Run stopped at 05-x: 4 passed", RunSentences.FolderEnded(4, 0, 0, stopped: true, stoppedAt: "05-x", notRun: 0));
    }

    [Fact]
    public void A_step_with_its_own_report_says_it_and_a_long_detail_is_cut()
    {
        var step = new WorkflowStep("menu", [new StepValue("name", "Close all windows")], WorkflowStep.Stop, "The palette did not close the windows", string.Empty, [], [], 0);
        var line = new ScriptLine(4, "menu Close all windows", step);
        var said = RunSentences.StepFailed(null, new LineResult(line, false, new string('x', 400)));
        Assert.StartsWith("Line 4 failed: The palette did not close the windows. Xxx", said);
        Assert.EndsWith("...", said);
    }

    [Fact]
    public void An_expectations_mark_names_the_kind_of_event_it_takes()
    {
        var sound = new Event(0, "sound", "Edge", false);
        var said = new Event(0, "say", "Sent. Background music on", true);
        var focus = new Event(0, "focus", "Conversation (List)", false);
        Assert.Equal("♪ Edge", sound.Spoken);
        Assert.Equal("! Sent. Background music on", said.Spoken);
        Assert.Equal("→ Conversation (List)", focus.Spoken);

        var (fitsSound, soundWords) = Window.Wanted("♪ Edge");
        Assert.True(fitsSound(sound));
        Assert.False(fitsSound(said));
        Assert.Equal("Edge", soundWords);

        var (fitsInterrupt, words) = Window.Wanted("! Background music on");
        Assert.True(fitsInterrupt(said));
        Assert.Equal("Background music on", words);

        var (fitsAny, _) = Window.Wanted("Conversation");
        Assert.True(fitsAny(focus));
        Assert.False(fitsAny(sound));
    }
}
