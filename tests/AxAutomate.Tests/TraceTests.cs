using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// A run's trace without a window (docs/axKit/user-workflows.md WF-5.1 to WF-5.6, format 5.4): the lines of the input
/// steps and of the events, their cleaning, the file, which sections are kept, how they are compared and which scripts
/// may keep any.
/// </summary>
public sealed class TraceTests
{
    /// <summary>A profile folder, built from parts so that the repository's privacy test does not take it for a real one.</summary>
    private static readonly string Profile = "C:\\" + "Users\\Someone";

    [Fact]
    public void An_input_step_is_a_line_and_a_step_that_prepares_or_looks_is_not()
    {
        // The script's own line, trimmed, before {temp} is filled (WF-5.3).
        Assert.Equal("keys ctrl+f", Traces.InputLine("keys ctrl+f", null));
        Assert.Equal("keys enter", Traces.InputLine("  keys enter  ", null));
        Assert.Equal("type apple", Traces.InputLine("type apple", null));
        Assert.Equal("menu Larger text", Traces.InputLine("menu Larger text", null));
        Assert.Equal("start down {temp}\\find.md", Traces.InputLine("start down {temp}\\find.md", null));
        Assert.Equal("launch help", Traces.InputLine("launch help", null));
        Assert.Equal("adopt", Traces.InputLine("adopt", null));
        Assert.Equal("use 2", Traces.InputLine("use 2", null));
        Assert.Equal("close", Traces.InputLine("close", null));

        // WF-5.2: what prepares and what only looks has no line; what it waited for is on the timeline.
        string[] none =
        [
            "file find.md # Find", "mkdir a", "silence {temp}\\a.wav 500", "run git init", "wait 1500", "allow error x", "mcp start",
            "expect said Not found", "expect focus Text to find", "refuse said x", "try said trust", "said", "text", "list", "commands",
            "state", "focus", "check commands", "expect quiet 1000", "# Goal: Find a word in the text", "# Stable", "",
        ];
        Assert.All(none, line => Assert.Null(Traces.InputLine(line, "found: x")));
    }

    [Fact]
    public void Every_step_word_the_reader_calls_an_input_has_its_trace_line()
    {
        // Every operation the engine knows, do among them, as a line alone, with a value, and after an iffound that ran.
        var words = ScriptLines.Known.Append("do").Append("try said").Append("iffound").Distinct().ToList();
        foreach (var word in words)
        {
            foreach (var line in new[] { word, word + " x", "iffound " + word + " x" })
            {
                Assert.True(ScriptReader.IsInput(line) == (Traces.InputLine(line, null) is not null), line);
            }
        }

        // The input steps of 5.4, no more and no fewer: a new one gets its line here and its place in 5.4.
        var inputs = words.Where(word => Traces.InputLine(word + " x", null) is not null).Order(StringComparer.Ordinal);
        Assert.Equal(["adopt", "close", "do", "keys", "launch", "mcp call", "menu", "start", "template", "type", "use"], inputs);
    }

    [Fact]
    public void A_do_step_is_written_with_the_chord_it_pressed_or_as_a_menu_item()
    {
        Assert.Equal("do Find = ctrl+f", Traces.InputLine("do Find", "key ctrl+f"));
        Assert.Equal("do Find previous = shift+f3", Traces.InputLine("do Find previous", "key shift+f3"));
        Assert.Equal("do Go to line = ctrl+g", Traces.InputLine("  do Go to line ", "key ctrl+g"));
        Assert.Equal("do Word wrap = menu item", Traces.InputLine("do Word wrap", "menu item"));

        // Without the detail of a pass (a failed step, whose script is never compared), the line as written.
        Assert.Equal("do Find", Traces.InputLine("do Find", null));
    }

    [Fact]
    public void An_iffound_is_written_as_its_step_only_when_it_ran()
    {
        Assert.Equal("keys enter", Traces.InputLine("iffound keys enter", null));
        Assert.Equal("do Find = ctrl+f", Traces.InputLine("iffound do Find", "key ctrl+f"));
        Assert.Null(Traces.InputLine("iffound keys enter", "skipped"));
        Assert.Null(Traces.InputLine("iffound do Find", "skipped"));

        // An iffound of a step that is no input has no line, whether it ran or not.
        Assert.Null(Traces.InputLine("iffound wait 1500", null));
        Assert.Null(Traces.InputLine("iffound expect said x", "skipped"));
    }

    [Fact]
    public void An_mcp_call_is_written_without_its_arguments()
    {
        Assert.Equal("mcp call axkit_windows", Traces.InputLine("mcp call axkit_windows", "{\"windows\":[]}"));
        Assert.Equal("mcp call axkit_window_run",
            Traces.InputLine("mcp call axkit_window_run {\"window\":\"{window}\",\"command\":\"Find\"}", "{\"ran\":true}"));
        Assert.Equal("mcp call axkit_window_run", Traces.InputLine("iffound mcp call axkit_window_run {\"command\":\"Find\"}", null));
        Assert.Null(Traces.InputLine("mcp start", null));
    }

    [Fact]
    public void An_event_is_written_indented_as_the_timeline_writes_it()
    {
        // No time: the event's Ms is never part of its line (WF-5.1).
        Assert.Equal("  ! Line 5 of 7", Traces.EventLine(1, 1, new Event(1200, "say", "Line 5 of 7", true)));
        Assert.Equal("  Not found", Traces.EventLine(1, 1, new Event(5, "say", "Not found", false)));
        Assert.Equal("  ♪ edge", Traces.EventLine(1, 1, new Event(5, "sound", "edge", false)));
        Assert.Equal("  → find.md (Text): The first apple is on line 3.",
            Traces.EventLine(1, 1, new Event(5, "focus", "find.md (Text): The first apple is on line 3.", false)));
        Assert.Equal("  · apple", Traces.EventLine(1, 1, new Event(5, "value", "apple", false)));

        // A focus on a field with no value ends in a space on the timeline; the trace leaves it out, so that an editor that trims lines changes nothing.
        Assert.Equal("  → Text to find (Text):", Traces.EventLine(1, 1, new Event(5, "focus", "Text to find (Text): ", false)));

        // A focus move with its place and what is over it (docs/axKit/workflow-graph.md WG-1.2), after the trimmed text.
        Assert.Equal("  → Text to find (Text):  @ axDown.text|question", Traces.EventLine(1, 1, new Event(5, "focus", "Text to find (Text): ", false, "axDown.text|question")));

        // An event is one line of the file: its line breaks are written \n.
        Assert.Equal("  First line\\nSecond line\\nThird", Traces.EventLine(1, 1, new Event(5, "say", "First line\r\nSecond line\nThird", false)));
    }

    [Fact]
    public void With_several_windows_an_event_names_its_window()
    {
        var said = new Event(40, "say", "Line 5 of 7", true);
        Assert.Equal("  1: ! Line 5 of 7", Traces.EventLine(1, 2, said));
        Assert.Equal("  2: ! Line 5 of 7", Traces.EventLine(2, 2, said));
        Assert.Equal("  3: ♪ edge", Traces.EventLine(3, 3, new Event(40, "sound", "edge", false)));
        Assert.Equal("  ! Line 5 of 7", Traces.EventLine(1, 1, said));
    }

    [Fact]
    public void The_runs_folder_and_the_profile_are_written_as_placeholders_in_every_spelling()
    {
        var temp = Profile + "\\AppData\\Local\\Temp\\axrun-7f3a";
        var forward = temp.Replace('\\', '/');
        var doubled = temp.Replace("\\", "\\\\");

        Assert.Equal("  → {temp}\\find.md (Text):", Traces.Clean("  → " + temp + "\\find.md (Text):", temp, Profile));
        Assert.Equal("  · {temp}/find.md", Traces.Clean("  · " + forward + "/find.md", temp, Profile));
        Assert.Equal("  {\"path\":\"{temp}\\\\find.md\"}", Traces.Clean("  {\"path\":\"" + doubled + "\\\\find.md\"}", temp, Profile));
        Assert.Equal("  Opened {temp}\\FIND.MD", Traces.Clean("  Opened " + temp.ToUpperInvariant() + "\\FIND.MD", temp, Profile));

        // The profile after the run's folder, which lies under it; also given with forward slashes or a closing separator.
        Assert.Equal("  {home}\\Documents and {temp}", Traces.Clean("  " + Profile + "\\Documents and " + temp, temp, Profile));
        Assert.Equal("  {home}/Documents", Traces.Clean("  " + Profile.Replace('\\', '/') + "/Documents", temp + "\\", Profile.Replace('\\', '/') + "/"));
        Assert.Equal("  {home}\\\\Desktop", Traces.Clean("  " + Profile.ToLowerInvariant().Replace("\\", "\\\\") + "\\\\Desktop", temp, Profile));

        // Then the redaction, the second guard: another account's path, an e-mail address.
        var other = "D:\\" + "Users\\Other\\notes.md";
        Assert.Equal("  D:\\Users\\<user>\\notes.md for <email>", Traces.Clean("  " + other + " for someone" + "@example.com", temp, Profile));

        // An empty folder replaces nothing.
        Assert.Equal("keys enter", Traces.Clean("keys enter", "", ""));
    }

    [Fact]
    public void A_trace_file_reads_back_as_its_sections()
    {
        TraceSection[] sections =
        [
            new("Goal: Find a word in the text",
            [
                "do Find = ctrl+f", "  → Text to find (Text):", "type apple", "  · apple", "keys enter",
                "  → find.md (Text): The first apple is on line 3.", "do Find next = f3", "  · The second apple is on line 5.",
                "do Find previous = shift+f3", "  · The first apple is on line 3.",
            ]),
            new("Goal: Go to a line by its number",
            [
                "do Go to line = ctrl+g", "  → Go to line (now line 3, column 11) (Text):", "type 5", "  · 5", "keys enter",
                "  ! Line 5 of 7", "  → find.md (Text): The second apple is on line 5.",
            ]),
            new("Check: An empty section", []),
        ];

        var text = Traces.Write("tests\\scripts\\axDown\\04-find-and-go-to-line.txt", sections);
        Assert.StartsWith(
            "# Trace of tests/scripts/axDown/04-find-and-go-to-line.txt\n"
            + "# Written by tools/AxAutomate --traces write; never by hand (docs/axKit/user-workflows.md WF-5).\n"
            + "\n== Goal: Find a word in the text\ndo Find = ctrl+f\n  → Text to find (Text):\ntype apple\n",
            text);
        Assert.EndsWith("  → find.md (Text): The second apple is on line 5.\n\n== Check: An empty section\n", text);

        // As written, and as a checkout on Windows writes it, with carriage returns.
        foreach (var file in new[] { text, text.Replace("\n", "\r\n") })
        {
            var read = Traces.Read(file);
            Assert.Equal(sections.Select(section => section.Heading), read.Select(section => section.Heading));
            for (var i = 0; i < sections.Length; i++)
            {
                Assert.Equal(sections[i].Lines, read[i].Lines);
            }
        }

        Assert.Empty(Traces.Read("not a trace\n"));
        Assert.Empty(Traces.Read(""));
    }

    [Fact]
    public void Only_the_sections_marked_stable_are_kept_and_never_the_setup()
    {
        string[] lines =
        [
            "# Workflow: W",
            "start down {temp}\\a.md",
            "# Goal: First",
            "# Stable",
            "keys ctrl+f",
            "# Goal: Second",
            "keys ctrl+g",
            "# Check: Third",
            "# Stable",
            "keys f3",
        ];
        var goals = ScriptReader.Goals(lines, "W");
        Assert.Equal(["Goal: First", "Goal: Second", "Check: Third"], goals.Select(Traces.Heading));

        TraceSection[] sections =
        [
            new(Traces.Setup, ["start down {temp}\\a.md", "  → a.md (Text):"]),
            new("Goal: First", ["keys ctrl+f"]),
            new("Goal: Second", ["keys ctrl+g"]),
            new("Check: Third", ["keys f3"]),
        ];
        Assert.Equal(["Goal: First", "Check: Third"], Traces.Stable(sections, goals).Select(section => section.Heading));

        // The setup is never kept, even beside a script of one goal marked stable in its header.
        string[] whole = ["# Workflow: W", "# Stable", "keys ctrl+f"];
        var one = ScriptReader.Goals(whole, "W");
        Assert.Equal(["Goal: W"], Traces.Stable([new(Traces.Setup, ["keys x"]), new("Goal: W", ["keys ctrl+f"])], one).Select(section => section.Heading));

        // A goal and a check of one title are told apart by their headings.
        Assert.Empty(Traces.Stable([new("Goal: Third", ["keys f3"])], goals));
    }

    [Fact]
    public void Equal_sections_have_no_difference()
    {
        TraceSection[] kept = [new("Goal: First", ["keys ctrl+f", "  ! Found"]), new("Check: Third", ["keys f3"])];
        TraceSection[] now = [new("Goal: First", ["keys ctrl+f", "  ! Found"]), new("Check: Third", ["keys f3"])];
        Assert.Empty(Traces.Compare(kept, now));

        // A kept section that is not among now's is the cheap check's to find (WF-5.9), not a difference here.
        Assert.Empty(Traces.Compare(kept, [now[0]]));
    }

    [Fact]
    public void The_first_differing_line_is_named_with_both_texts()
    {
        TraceSection[] kept = [new("Goal: First", ["keys ctrl+f", "  ! Found", "  · apple", "keys enter"])];
        var differences = Traces.Compare(kept, [new("Goal: First", ["keys ctrl+f", "  ! Found it", "  · pear", "keys enter"])]);
        Assert.Equal([new TraceDifference("Goal: First", 2, "  ! Found", "  ! Found it")], differences);

        // A line one of them lacks reads as "(nothing)".
        Assert.Equal([new TraceDifference("Goal: First", 5, "(nothing)", "  ♪ edge")],
            Traces.Compare(kept, [new("Goal: First", [.. kept[0].Lines, "  ♪ edge"])]));
        Assert.Equal([new TraceDifference("Goal: First", 4, "keys enter", "(nothing)")],
            Traces.Compare(kept, [new("Goal: First", kept[0].Lines.Take(3).ToList())]));

        // Case and spaces count: there is no difference that is only a note (WF-5.5).
        Assert.Equal(1, Traces.Compare(kept, [new("Goal: First", ["keys Ctrl+F", "  ! Found", "  · apple", "keys enter"])])[0].Line);
    }

    [Fact]
    public void A_stable_section_without_a_kept_one_is_a_difference_of_line_zero()
    {
        TraceSection[] kept = [new("Goal: First", ["keys ctrl+f"])];
        TraceSection[] now = [new("Goal: First", ["keys ctrl+f"]), new("Check: First", ["keys ctrl+f"]), new("Goal: Second", ["keys ctrl+g"])];
        Assert.Equal(
            [new TraceDifference("Check: First", 0, "", ""), new TraceDifference("Goal: Second", 0, "", "")],
            Traces.Compare(kept, now));
        Assert.Equal([new TraceDifference("Goal: First", 0, "", "")], Traces.Compare([], [now[0]]));
    }

    [Fact]
    public void The_events_between_two_sections_belong_to_the_one_before()
    {
        string[] lines =
        [
            "# Workflow: W",              // 1
            "start down {temp}\\a.md",    // 2
            "expect focus a.md",          // 3
            "# Goal: First",              // 4
            "keys ctrl+f",                // 5
            "expect focus Text to find",  // 6
            "# Check: Second",            // 7
            "expect state Line 1",        // 8
            "keys f3",                    // 9
            "# Goal: Third",              // 10
            "keys ctrl+g",                // 11
        ];
        var goals = ScriptReader.Goals(lines, "W");

        // What a run played, line by line: the input line when the line is one, and what the windows had recorded
        // by the line's end. The announcement that came after the first goal's last step was taken with that step
        // (line 6), so it is the first goal's, not the check's, whose first line (8) took nothing.
        TracedLine[] played =
        [
            new(2, "start down {temp}\\a.md", []),
            new(3, null, ["  → a.md (Text):"]),
            new(5, "keys ctrl+f", ["  → Text to find (Text):"]),
            new(6, null, ["  Find"]),
            new(8, null, []),
            new(9, "keys f3", ["  ! Not found"]),
        ];
        var sections = Traces.Sections(played, goals);
        Assert.Equal([Traces.Setup, "Goal: First", "Check: Second"], sections.Select(section => section.Heading));
        Assert.Equal(["start down {temp}\\a.md", "  → a.md (Text):"], sections[0].Lines);
        Assert.Equal(["keys ctrl+f", "  → Text to find (Text):", "  Find"], sections[1].Lines);
        Assert.Equal(["keys f3", "  ! Not found"], sections[2].Lines);

        // A section whose lines gave nothing is there without lines; one no line of which was played is not there.
        Assert.Equal(
            [Traces.Setup, "Goal: First"],
            Traces.Sections([new(2, null, []), new(5, "keys ctrl+f", [])], goals).Select(section => section.Heading));
        Assert.Empty(Traces.Sections([new(3, null, [])], goals)[0].Lines);
        Assert.Empty(Traces.Sections([], goals));

        // A script without sections is its one goal, and a workflow file (no goals) is all setup.
        string[] whole = ["# Workflow: W", "keys ctrl+f", "expect focus Text to find"];
        Assert.Equal(["Goal: W"], Traces.Sections([new(2, "keys ctrl+f", []), new(3, null, ["  Find"])], ScriptReader.Goals(whole, "W")).Select(section => section.Heading));
        Assert.Equal([Traces.Setup], Traces.Sections([new(1, "keys ctrl+f", [])], []).Select(section => section.Heading));
    }

    [Fact]
    public void A_difference_is_said_with_its_section_its_line_and_both_texts()
    {
        // WF-5.5's two sentences, the words after "FAIL after the steps: "; "check" for a check.
        Assert.Equal(
            "the trace of the stable goal Find a word in the text differs at its line 2: kept → Text to find (Text):; now → Find (Text):",
            Traces.Problem(new TraceDifference("Goal: Find a word in the text", 2, "  → Text to find (Text):", "  → Find (Text):")));
        Assert.Equal(
            "the trace of the stable check A word is refused differs at its line 3: kept (nothing); now ! Not found",
            Traces.Problem(new TraceDifference("Check: A word is refused", 3, "(nothing)", "  ! Not found")));
        Assert.Equal(
            "the stable goal Find a word in the text has no kept trace: run with --traces write",
            Traces.Problem(new TraceDifference("Goal: Find a word in the text", 0, "", "")));
        Assert.Equal(
            "the stable check A word is refused has no kept trace: run with --traces write",
            Traces.Problem(new TraceDifference("Check: A word is refused", 0, "", "")));
    }

    [Fact]
    public void A_script_of_a_window_that_reads_the_computer_or_with_a_needs_line_may_keep_no_trace()
    {
        Assert.True(Traces.MayKeep("tests/scripts/axDown/04-x.txt", ""));
        Assert.True(Traces.MayKeep("tests\\scripts\\axFile\\01-x.txt", "  "));

        // WF-5.6: each folder whose window reads the computer or the user's session, in any case and either separator.
        foreach (var folder in new[] { "axClip", "axStart", "axProcess", "axTerminal", "axGIT", "axMedia", "axgit", "AXCLIP" })
        {
            Assert.False(Traces.MayKeep($"tests/scripts/{folder}/01-x.txt", ""), folder);
            Assert.False(Traces.MayKeep($"tests\\scripts\\{folder}\\01-x.txt", ""), folder);
        }

        // A needs line: a model's words, a program's output.
        Assert.False(Traces.MayKeep("tests/scripts/axDown/04-x.txt", "Claude Code installed and signed in."));

        // The NVDA tier's scripts, and a script played from outside tests/scripts.
        Assert.False(Traces.MayKeep("tests/nvda-scripts/axDown-notices.txt", ""));
        Assert.False(Traces.MayKeep("scratch/04-x.txt", ""));

        // A folder whose name only begins like one of them may.
        Assert.True(Traces.MayKeep("tests/scripts/axClipboardless/01-x.txt", ""));
    }

    [Fact]
    public void The_kept_trace_of_a_script_is_under_tests_traces_by_its_folder_and_name()
    {
        Assert.Equal("tests/traces/axDown/04-find-and-go-to-line.trace.txt", Traces.PathFor("tests/scripts/axDown/04-find-and-go-to-line.txt"));
        Assert.Equal("tests/traces/axFile/01-x.trace.txt", Traces.PathFor("tests\\scripts\\axFile\\01-x.txt"));
        Assert.Equal("tests/traces/mcp/02-run-commands.trace.txt", Traces.PathFor("Tests/Scripts/mcp/02-run-commands.TXT"));
    }
}
