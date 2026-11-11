using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>axAutomate's pure code (docs/axAutomate/SPEC.md): a script and a workflow file read as steps, the rows and their words, the folder and the settings.</summary>
public sealed class AutomateTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axAutomate-tests-" + Guid.NewGuid().ToString("N"));

    public AutomateTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static readonly string[] Script =
    [
        "# axDown: the palette (plan section 2).",
        "# Second line of the header.",
        "file \"my notes.md\" # Notes\\nOne line",
        "",
        "# 2.1 The palette opens.",
        "start down {temp}\\my notes.md",
        "keys ctrl+shift+p down",
        "expect said Command palette",
        "try said trust",
        "iffound wait 1500",
        "iffound keys enter",
        "mcp call axkit_windows {}",
        "close",
    ];

    [Fact]
    public void A_script_reads_as_a_workflow_with_its_header_its_notes_and_its_try_blocks()
    {
        var workflow = ScriptReader.Parse(Script, "02-palette", @"C:\scripts\02-palette.txt");
        Assert.Null(workflow.Problem);
        Assert.Equal("axDown: the palette (plan section 2). Second line of the header.", workflow.Covers);
        Assert.Equal(["file", "", "start", "keys", "expect said", "expect said", "mcp call", "close"], workflow.Steps.Select(step => step.Do));
        Assert.Equal("my notes.md", workflow.Steps[0].Value("path"));
        Assert.Equal("# Notes\\nOne line", workflow.Steps[0].Value("text"));
        Assert.Equal("2.1 The palette opens.", workflow.Steps[1].Note);
        var trust = workflow.Steps[5];
        Assert.Equal(WorkflowStep.Continue, trust.OnFailure);
        Assert.Equal(["wait", "keys"], trust.Then.Select(step => step.Do));
        Assert.Equal("{}", workflow.Steps[6].Value("arguments"));
        Assert.Equal(9, workflow.StepCount);
    }

    [Fact]
    public void The_rows_read_each_step_as_a_sentence_with_the_nested_steps_after_their_step()
    {
        var rows = StepText.Rows(ScriptReader.Parse(Script, "02-palette", @"C:\scripts\02-palette.txt")).Select(row => row.Label).ToList();
        Assert.Equal(
        [
            "1. Make the file my notes.md",
            "Note: 2.1 The palette opens.",
            "2. Start down {temp}\\my notes.md",
            "3. Press Ctrl+Shift+P, Down",
            "4. Expect said: Command palette",
            "5. Expect said: trust, may not come, then 2 steps",
            "5.1 then: Wait 1.5 seconds",
            "5.2 then: Press Enter",
            "6. Call the tool axkit_windows",
            "7. Close the window",
        ], rows);
    }

    [Fact]
    public void A_steps_details_name_its_parts_and_its_line()
    {
        var workflow = ScriptReader.Parse(Script, "02-palette", @"C:\scripts\02-palette.txt");
        var trust = StepText.Rows(workflow).First(row => row.Number == "5");
        var details = StepText.Details(trust, workflow);
        Assert.Contains("Does: expect said", details);
        Assert.Contains("Text: trust", details);
        Assert.Contains("Then: 2 steps, run only when this one passed", details);
        Assert.Equal("Line 9 of 02-palette.txt", details[^1]);
    }

    [Fact]
    public void An_iffound_without_a_try_is_said_and_still_shown()
    {
        var workflow = ScriptReader.Parse(["iffound keys enter"], "odd", "odd.txt");
        Assert.NotNull(workflow.Problem);
        Assert.Equal(["keys"], workflow.Steps.Select(step => step.Do));
    }

    [Fact]
    public void A_workflow_file_reads_its_steps_blocks_and_reports()
    {
        var workflow = WorkflowFile.Parse("""
            { "format": "axkit-workflow 1", "title": "axDown: the palette", "covers": "plan section 2",
              "steps": [
                { "do": "keys", "keys": "ctrl+shift+p" },
                { "do": "expect said", "text": "trust", "on failure": "continue",
                  "then": [ { "do": "wait", "ms": 1500 } ], "otherwise": [ { "do": "close" } ] },
                { "do": "menu", "name": "Close all windows", "report": "The palette did not close them" },
                { "note": "The end." } ] }
            """, "palette", "palette.workflow.json");
        Assert.Null(workflow.Problem);
        Assert.Equal("axDown: the palette", workflow.Title);
        Assert.Equal("1500", workflow.Steps[1].Then[0].Value("ms"));
        Assert.Equal(
        [
            "1. Press Ctrl+Shift+P",
            "2. Expect said: trust, may not come, then 1 step, otherwise 1 step",
            "2.1 then: Wait 1.5 seconds",
            "2.1 otherwise: Close the window",
            "3. Choose the menu item Close all windows, reports",
            "Note: The end.",
        ], StepText.Rows(workflow).Select(row => row.Label));
    }

    [Fact]
    public void A_workflow_file_not_understood_says_why()
    {
        Assert.Contains("not JSON", WorkflowFile.Parse("{", "x", "x.workflow.json").Problem);
        Assert.Contains("format", WorkflowFile.Parse("""{ "steps": [] }""", "x", "x.workflow.json").Problem);
        Assert.Contains("on failure", WorkflowFile.Parse("""{ "format": "axkit-workflow 1", "steps": [ { "do": "close", "on failure": "retry" } ] }""", "x", "x.workflow.json").Problem);
    }

    [Fact]
    public void A_folder_lists_its_folders_then_its_workflows_in_the_natural_order()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "axDown"));
        Directory.CreateDirectory(Path.Combine(_folder, ".git"));
        File.WriteAllText(Path.Combine(_folder, "10-last.txt"), "close");
        File.WriteAllText(Path.Combine(_folder, "02-first.workflow.json"), "{}");
        File.WriteAllText(Path.Combine(_folder, "notes.md"), "not a workflow");
        File.WriteAllText(Path.Combine(_folder, "axDown", "01-start.txt"), "close");
        Assert.Equal(["axDown", "02-first", "10-last"], WorkflowFolder.Read(_folder).Select(entry => entry.Name));
        Assert.Equal(3, WorkflowFolder.CountWorkflows(_folder));
    }

    [Fact]
    public void Every_script_of_the_repository_reads_without_a_problem()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var scripts = Directory.GetFiles(Path.Combine(root, "tests", "scripts"), "*.txt", SearchOption.AllDirectories);
        Assert.NotEmpty(scripts);
        var problems = scripts.Select(ScriptReader.Read).Where(workflow => workflow.Problem is not null).Select(workflow => $"{workflow.Path}: {workflow.Problem}");
        Assert.Empty(problems);
    }

    [Fact]
    public void Seconds_and_keys_are_said_as_words()
    {
        Assert.Equal("1 second", StepText.Seconds("1000"));
        Assert.Equal("0.3 seconds", StepText.Seconds("300"));
        Assert.Equal("Alt+F4", StepText.Keys("alt+f4"));
    }

    [Fact]
    public void A_missing_settings_file_gives_the_defaults()
    {
        var settings = AutomateSettings.Load(Path.Combine(_folder, "axAutomate.json"), out var error);
        Assert.Null(error);
        Assert.Null(settings.Folder);
    }
}
