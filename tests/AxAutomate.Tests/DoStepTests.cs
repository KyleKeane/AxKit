using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The step <c>do</c> (docs/axKit/user-workflows.md WF-4): what a name comes to against a window's listing
/// (<see cref="DoStep.Decide"/>), which both tiers then press or choose, and how a <c>do</c> line reads as a step.
/// The rows are axDown's as section 5.2 of that document gives them.
/// </summary>
public sealed class DoStepTests
{
    private static ListedCommand Row(string label, string menu, string key, string now, string keyText, bool keyOnly = false, string[]? also = null) =>
        new(label, menu, key, also ?? [], keyText, "read", null, keyOnly, !keyOnly, now);

    /// <summary>axDown's window with a file open and the focus in the text: every key acts.</summary>
    private static readonly WindowCommands InTheText = new("axDown", null,
    [
        Row("Undo", "Actions", "ctrl+z", "ctrl+z", "Ctrl+Z"),
        Row("Redo", "Actions", "ctrl+y", "ctrl+y", "Ctrl+Y", also: ["ctrl+shift+z"]),
        Row("Cut", "Actions", string.Empty, string.Empty, "Ctrl+X"),
        Row("Find", "Navigation", "ctrl+f", "ctrl+f", "Ctrl+F"),
        Row("Find next", "Navigation", "f3", "f3", "F3"),
        Row("Find previous", "Navigation", "shift+f3", "shift+f3", "Shift+F3"),
        Row("Go to line", "Navigation", "ctrl+g", "ctrl+g", "Ctrl+G"),
        Row("Heading", "Navigation, Next", "ctrl+h", "ctrl+h", "Ctrl+H"),
        Row("Heading", "Navigation, Previous", "ctrl+shift+h", "ctrl+shift+h", "Ctrl+Shift+H"),
        Row("Heading level 2", "Navigation, Next", "ctrl+2", "ctrl+2", "Ctrl+2"),
        Row("Open settings file", "Options, Developer", string.Empty, string.Empty, string.Empty),
        Row("Between the text and the details", string.Empty, "ctrl+tab", "ctrl+tab", "Ctrl+Tab", keyOnly: true, also: ["ctrl+shift+tab", "f6"]),
    ]);

    /// <summary>The same rows, each with another chord that acts now, or none.</summary>
    private static WindowCommands With(string label, string now) =>
        InTheText with { Commands = [.. InTheText.Commands.Select(row => row.Label == label ? row with { Now = now } : row)] };

    [Fact]
    public void A_name_gives_the_chord_that_acts_now()
    {
        var find = DoStep.Decide(InTheText, "Find");
        Assert.Equal(DoKind.Press, find.Kind);
        Assert.Equal("ctrl+f", find.Chord);
        Assert.Equal("key ctrl+f", find.Detail);
        Assert.Equal(("Find", "Navigation"), (find.Row!.Label, find.Row.Menu));

        // Whole text against whole text, without regard to case, the menu's dots aside: never the start of a name.
        Assert.Equal("f3", DoStep.Decide(InTheText, "find NEXT").Chord);
        Assert.Equal("ctrl+g", DoStep.Decide(InTheText, "Go to line...").Chord);

        // Where two commands share a label, the name is the label with its menus (WF-1.1).
        var previous = DoStep.Decide(InTheText, "Heading, Navigation, Previous");
        Assert.Equal(DoKind.Press, previous.Kind);
        Assert.Equal("key ctrl+shift+h", previous.Detail);

        // The key is the window's at this moment (WF-4.2): another key in the listing is another press, the name unchanged.
        var moved = InTheText with { Commands = [.. InTheText.Commands.Select(row => row.Label == "Find" ? row with { Key = "ctrl+shift+f", Now = "ctrl+shift+f" } : row)] };
        Assert.Equal("key ctrl+shift+f", DoStep.Decide(moved, "Find").Detail);
    }

    [Fact]
    public void A_second_chord_is_pressed_when_the_first_does_not_act()
    {
        // The window says which chord runs the command now (WF-2.5): the row's own, or a further one.
        var redo = DoStep.Decide(With("Redo", "ctrl+shift+z"), "Redo");
        Assert.Equal(DoKind.Press, redo.Kind);
        Assert.Equal("ctrl+shift+z", redo.Chord);
        Assert.Equal("key ctrl+shift+z", redo.Detail);

        // A row of keys alone, by its label.
        var panes = DoStep.Decide(With("Between the text and the details", "f6"), "Between the text and the details");
        Assert.Equal(DoKind.Press, panes.Kind);
        Assert.Equal("f6", panes.Chord);
        Assert.True(panes.Row!.KeyOnly);
    }

    [Fact]
    public void A_command_without_a_chord_runs_by_its_menu_item()
    {
        // No key at all.
        var settings = DoStep.Decide(InTheText, "Open settings file");
        Assert.Equal(DoKind.Choose, settings.Kind);
        Assert.Equal("menu item", settings.Detail);
        Assert.Equal(string.Empty, settings.Chord);
        // What the channel's choose is sent: exactly the row's label and its menus.
        Assert.Equal(("Open settings file", "Options, Developer"), (settings.Row!.Label, settings.Row.Menu));

        // A key text and no key as data (WF-2.1): the edit control's own Ctrl+X is never pressed for the menu's Cut.
        var cut = DoStep.Decide(InTheText, "Cut");
        Assert.Equal(DoKind.Choose, cut.Kind);
        Assert.Equal("menu item", cut.Detail);
    }

    [Fact]
    public void A_chord_that_does_not_act_where_the_focus_is_fails_with_the_command_and_its_key_said()
    {
        var next = DoStep.Decide(With("Find next", string.Empty), "Find next");
        Assert.Equal(DoKind.NoAct, next.Kind);
        Assert.Equal("the key of Find next (F3) does not act where the focus is", next.Detail);
        Assert.Equal(string.Empty, next.Chord);
        Assert.Equal("Find next", next.Row!.Label);

        // Neither of its chords acts: the key is said as the menu shows it. Never its menu item in the key's place:
        // what is checked is the keyboard path (WF-4.1).
        var redo = DoStep.Decide(With("Redo", string.Empty), "Redo");
        Assert.Equal(DoKind.NoAct, redo.Kind);
        Assert.Equal("the key of Redo (Ctrl+Y) does not act where the focus is", redo.Detail);

        // A row of keys alone has no menu item to run instead.
        var panes = DoStep.Decide(With("Between the text and the details", string.Empty), "Between the text and the details");
        Assert.Equal(DoKind.NoAct, panes.Kind);
        Assert.Equal("the key of Between the text and the details (Ctrl+Tab) does not act where the focus is", panes.Detail);

        // A menu row whose only chord came from a key with no menu item of the same label: the chord stands for the key text.
        var folded = new WindowCommands("axDown", null, [Row("Refresh", "Actions", string.Empty, string.Empty, string.Empty, also: ["f5"])]);
        Assert.Equal("the key of Refresh (f5) does not act where the focus is", DoStep.Decide(folded, "Refresh").Detail);

        // A key with no menu item and no name in the chord vocabulary: nothing a script can press, nothing to choose.
        var nameless = new WindowCommands("axDown", null, [Row("Odd key", string.Empty, string.Empty, string.Empty, "Ctrl+`", keyOnly: true)]);
        var odd = DoStep.Decide(nameless, "Odd key");
        Assert.Equal(DoKind.NoAct, odd.Kind);
        Assert.StartsWith("the key of Odd key (Ctrl+`) has no name a script can press, and the command has no menu item", odd.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_name_fails_with_the_nearest_named()
    {
        // The resolver never searches (WF-1.2): the start of a label is no name. The refusal names what is near.
        var near = DoStep.Decide(InTheText, "Find text");
        Assert.Equal(DoKind.Name, near.Kind);
        Assert.Equal("no command \"Find text\"; nearest: Find, Navigation", near.Detail);
        Assert.Null(near.Row);
        Assert.Equal(string.Empty, near.Chord);

        Assert.Equal("no command \"Go\"; nearest: Go to line, Navigation", DoStep.Decide(InTheText, "Go").Detail);
        Assert.Equal("no command \"Zoom\"", DoStep.Decide(InTheText, "Zoom").Detail);

        var none = DoStep.Decide(InTheText, " ");
        Assert.Equal(DoKind.Name, none.Kind);
        Assert.Equal("no command is named", none.Detail);
    }

    [Fact]
    public void An_ambiguous_name_fails_with_what_to_write()
    {
        var heading = DoStep.Decide(InTheText, "Heading");
        Assert.Equal(DoKind.Name, heading.Kind);
        Assert.Equal("several commands are named \"Heading\": write one of: Heading, Navigation, Next; Heading, Navigation, Previous", heading.Detail);
        Assert.Null(heading.Row);

        // Commands no name tells apart (WF-1.5): nothing runs.
        var same = new WindowCommands("axTask", null,
        [
            Row("Not set", "Context, Action keys", "ctrl+1", "ctrl+1", "Ctrl+1"),
            Row("Not set", "Context, Action keys", "ctrl+2", "ctrl+2", "Ctrl+2"),
        ]);
        var notSet = DoStep.Decide(same, "Not set");
        Assert.Equal(DoKind.Name, notSet.Kind);
        Assert.Equal("several commands are named \"Not set, Context, Action keys\" and the name cannot tell them apart", notSet.Detail);
    }

    [Fact]
    public void A_notice_fails_with_its_title()
    {
        // A window that shows a notice lists no menu row, and none of its keys acts (WF-4.5).
        var notice = new WindowCommands("axDown", "Find",
            [Row("Between the text and the details", string.Empty, "ctrl+tab", string.Empty, "Ctrl+Tab", keyOnly: true)]);
        var find = DoStep.Decide(notice, "Find");
        Assert.Equal(DoKind.Notice, find.Kind);
        Assert.Equal("the window shows the notice Find: its commands wait", find.Detail);
        Assert.Null(find.Row);

        // Whatever the name: one of its rows, one of no row, none.
        Assert.Equal(find.Detail, DoStep.Decide(notice, "Between the text and the details").Detail);
        Assert.Equal(DoKind.Notice, DoStep.Decide(notice, "Zoom").Kind);
        Assert.Equal(DoKind.Notice, DoStep.Decide(notice, string.Empty).Kind);

        // Also with every row of the window before it: a listing that names a notice runs nothing.
        Assert.Equal(DoKind.Notice, DoStep.Decide(InTheText with { Notice = "Keyboard shortcuts" }, "Find").Kind);
        Assert.Equal("the window shows a notice: its commands wait", DoStep.Decide(InTheText with { Notice = string.Empty }, "Find").Detail);
    }

    [Fact]
    public void A_window_without_the_listing_fails_with_that_said()
    {
        var none = DoStep.Decide(null, "Find");
        Assert.Equal(DoKind.NoData, none.Kind);
        Assert.Equal("this AxKit.exe does not list its commands as data", none.Detail);
        Assert.Null(none.Row);

        // What an AxKit.exe from before the request answers "commands data" with, the palette's lines, is no listing
        // (WF-4.9); nor is an error, or a window that closed.
        Assert.Equal(DoKind.NoData, DoStep.Decide(CommandListing.Read("[\"Find, Navigation, Ctrl+F\",\"Go to line, Navigation, Ctrl+G\"]"), "Find").Kind);
        Assert.Equal(DoKind.NoData, DoStep.Decide(CommandListing.Read("{\"error\":\"the window closed\"}"), "Find").Kind);
    }

    [Fact]
    public void The_answer_of_a_window_is_decided_as_it_is_read()
    {
        // The one line of section 5.2, as CommandListing writes and reads it: the listing a run has in hand.
        var answer = CommandListing.Write(With("Redo", "ctrl+shift+z"));
        var listing = CommandListing.Read(answer);
        Assert.NotNull(listing);
        Assert.Equal("key ctrl+f", DoStep.Decide(listing, "Find").Detail);
        Assert.Equal("key ctrl+shift+z", DoStep.Decide(listing, "Redo").Detail);
        Assert.Equal("menu item", DoStep.Decide(listing, "Cut").Detail);
        Assert.Equal(DoKind.Name, DoStep.Decide(listing, "Heading").Kind);
    }

    [Fact]
    public void A_do_line_reads_as_a_step_with_its_command_and_its_sentence()
    {
        var step = ScriptReader.Step("do Find next", 12);
        Assert.Equal("do", step.Do);
        Assert.Equal("command", Assert.Single(step.Values).Key);
        Assert.Equal("Find next", step.Value("command"));
        Assert.Equal("Do the command Find next", StepText.Sentence(step));
        Assert.Equal(12, step.Line);

        // The whole line of a command that shares its label is one value, commas and all.
        var heading = ScriptReader.Step("do Heading, Navigation, Next", 3);
        Assert.Equal("Heading, Navigation, Next", heading.Value("command"));
        Assert.Equal("Do the command Heading, Navigation, Next", StepText.Sentence(heading));

        // The engine knows the word, so axAutomate's window runs a workflow that uses it (WF-4.7), after a try too.
        Assert.Contains("do", ScriptLines.Known);
        var workflow = ScriptReader.Parse(["start down", "do Find", "try said Text to find", "iffound do Find next"], "w", @"C:\w.txt");
        Assert.Null(workflow.Problem);
        Assert.Null(ScriptLines.CannotRun(workflow, nvda: false, confined: true));
        Assert.Equal("do", Assert.Single(workflow.Steps[2].Then).Do);
        Assert.Equal(["2. Do the command Find", "3.1 then: Do the command Find next"],
            StepText.Rows(workflow).Where(row => row.Step.Do == "do").Select(row => row.Label));

        // It is an input step: what the user does at the keyboard (a goal must hold one).
        Assert.True(ScriptReader.IsInput("do Find"));
        Assert.True(ScriptReader.IsInput("iffound do Find next"));

        // In a workflow file it is {"do": "do", "command": "Find"}, played as the same line.
        var file = WorkflowFile.Parse("""{"format": "axkit-workflow 1", "steps": [{"do": "start", "arguments": "down"}, {"do": "do", "command": "Go to line"}]}""", "w", @"C:\w.workflow.json");
        var (lines, refusal) = ScriptLines.Prepare(file, nvda: false);
        Assert.Null(refusal);
        Assert.Equal(["start down", "do Go to line"], lines.Select(line => line.Text));
    }

    [Fact]
    public void The_NVDA_tier_takes_a_keys_code_from_the_one_vocabulary()
    {
        // WF-2.2: a chord a window lists must be one both tiers can press, so neither keeps a table of key names of
        // its own. The NVDA tier's key path is P/Invoke that no test can run; its source is read instead.
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var native = File.ReadAllText(Path.Combine(root, "src", "AxAutomate.Core", "Native.cs"));
        Assert.Contains("KeyChord.Code(name)", native, StringComparison.Ordinal);
        Assert.DoesNotContain("\"enter\" =>", native, StringComparison.Ordinal);
        Assert.DoesNotContain("\"pagedown\" =>", native, StringComparison.Ordinal);

        // Every chord the window can write for a key (KeyChord.Text) names a key the vocabulary reads back: the
        // codes that have a name, with and without modifiers.
        foreach (var code in Enumerable.Range(0, 256).Where(code => KeyChord.Name(code) is not null))
        {
            var chord = KeyChord.Text(code, ctrl: true, shift: true, alt: false);
            Assert.Equal(code, KeyChord.Code(chord.Split('+')[^1]));
            Assert.Equal(chord, KeyChord.Normal(chord));
        }
    }
}
