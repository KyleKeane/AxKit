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
            .Select(workflow => (workflow.Path, ScriptLines.Prepare(workflow, nvda: false).Refusal))
            .Where(pair => pair.Refusal is not null)
            .Select(pair => $"{pair.Path}: {pair.Refusal}");
        Assert.Empty(refused);
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
