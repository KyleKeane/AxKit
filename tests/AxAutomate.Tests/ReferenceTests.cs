using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The reference for people (Reference, docs/axKit/user-workflows.md WF-7, formats 5.6): the index, a group's page and
/// the commands page as texts, from scripts and a command inventory given as values. What is pinned here is what a
/// user has read to him and what Claude in axHelp answers from: the sentences word for word, and that nothing of the
/// checks stands on a page.
/// </summary>
public sealed class ReferenceTests
{
    private const string FindPath = "tests/scripts/axDown/04-find-and-go-to-line.txt";

    /// <summary>
    /// tests/scripts/axDown/04-find-and-go-to-line.txt as section 5.1 of user-workflows.md gives it: a setup, two goals
    /// and five checks.
    /// </summary>
    private static readonly string[] Find = """
        # Workflow: Find text and go to a line
        # axDown, test plan section 4 (Find and go to line), driven from inside the window (docs/axDown/nvda-test-plan.md).
        # Needs: nothing outside axKit.
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
        expect state Line 3, Column 11 ·
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
        expect said Not found here
        expect focus find.md

        # Goal: Go to a line by its number
        # 4.2: Ctrl+G, the field named with the line and column; a number, Enter: "Line n of m", the caret at that line's start.
        do Go to line
        expect focus Go to line (now line 3, column 11)
        type 5
        keys enter
        expect said ! Line 5 of 7
        expect focus {"name":"find.md","role":"Text","value":"The second apple is on line 5."}
        expect state Line 5, Column 1 ·
        # Check: A number past the end lands on the last line
        do Go to line
        type 99
        keys enter
        expect said ! Line 7 of 7
        # Check: A word in place of a number is refused and the caret stays
        do Go to line
        type five
        keys enter
        expect said ! Not a line number
        # Check: Escape in the field returns to the text where it was
        do Go to line
        keys escape
        expect focus {"name":"find.md","role":"Text","value":"The last line, line 7."}

        # Check: The status bar holds the name, the line and column, the encoding and the line ending
        expect state "status":["find.md","Line 7, Column 1 · UTF-8 · CRLF"]
        """.ReplaceLineEndings("\n").Split('\n');

    /// <summary>A script with no goal line and no check line: the whole of it is one goal, titled by its label.</summary>
    private static readonly string[] Open = """
        # Workflow: Open a text file in axDown
        # axDown, test plan section 1.
        file notes.md Hello
        start down {temp}\notes.md
        expect said notes.md, line 1
        keys ctrl+end
        expect said ! Line 3 of 3
        """.ReplaceLineEndings("\n").Split('\n');

    private const string GroupPreamble = """
        Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
        is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
        line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
        cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
        the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
        command's in this version of axKit.
        """;

    private static ListedCommand Row(string label, string menu, string key = "", string keyText = "", string mark = "read", IReadOnlyList<string>? also = null,
        bool ai = true) =>
        new(label, menu, key, also ?? [], keyText, mark, Checked: null, KeyOnly: false, ai, Now: string.Empty);

    /// <summary>A row of keys with no menu item, as the inventory stores it: no menus, never for the AI tools.</summary>
    private static ListedCommand KeyAlone(string label, string key, string keyText, params string[] also) =>
        new(label, string.Empty, key, also, keyText, "open", Checked: null, KeyOnly: true, Ai: false, Now: string.Empty);

    /// <summary>
    /// A command inventory: axDown's rows as sections 5.2 and 5.3 give them, with Larger text, whose menu spells its key
    /// "Ctrl++"; axClaude's Send message, whose key the menu only shows as text; axFile's rows whose keys are plain keys.
    /// </summary>
    private static readonly IReadOnlyList<InventoryApp> Inventory =
    [
        new("axClaude",
        [
            Row("Send message", "Actions", keyText: "Enter in the message field", mark: "act"),
        ]),
        new("axDown",
        [
            Row("Redo", "Actions", "ctrl+y", "Ctrl+Y", "change", ["ctrl+shift+z"]),
            Row("Undo", "Actions", "ctrl+z", "Ctrl+Z", "change"),
            Row("Find", "Navigation", "ctrl+f", "Ctrl+F"),
            Row("Find next", "Navigation", "f3", "F3"),
            Row("Find previous", "Navigation", "shift+f3", "Shift+F3"),
            Row("Go to line", "Navigation", "ctrl+g", "Ctrl+G"),
            Row("Heading", "Navigation, Next", "ctrl+h", "Ctrl+H"),
            Row("Heading", "Navigation, Previous", "ctrl+shift+h", "Ctrl+Shift+H"),
            Row("Larger text", "Options, Text", "ctrl+plus", "Ctrl++", "change"),
            Row("Word wrap", "Options, Text", "ctrl+shift+w", "Ctrl+Shift+W", "change"),
            KeyAlone("Between the text and the details", "ctrl+tab", "Ctrl+Tab", "ctrl+shift+tab", "f6"),
        ]),
        new("axFile",
        [
            Row("Move to folder", "Context", mark: "change"),
            Row("Open", "Context", "enter", "Enter", "open"),
            Row("Up one level", "Navigation", "backspace", "Backspace", "open", ["alt+up", "left"]),
            Row("New folder", "Session", "ctrl+shift+n", "Ctrl+Shift+N", "change"),
        ]),
    ];

    /// <summary>The lines of a goal made of the given script lines: read as the engine reads them, with the app each line reaches.</summary>
    private static IReadOnlyList<string> Lines(IReadOnlyList<InventoryApp> inventory, params string[] script) =>
        Reference.GoalLines(ScriptReader.Parse(script, "W", "w.txt").Steps, ScriptApps.ByLine(script), inventory);

    private static IReadOnlyList<string> Lines(params string[] script) => Lines(Inventory, script);

    private static string Page(IReadOnlyList<ReferencePage> pages, string path) => pages.Single(page => page.Path == path).Text;

    private static string Text(string raw) => raw.ReplaceLineEndings("\n") + "\n";

    [Fact]
    public void The_index_lists_each_group_its_workflows_and_under_each_its_goals()
    {
        var pages = Reference.Pages(
            [
                new(FindPath, Find),
                new("tests/scripts/axDown/01-start.txt", Open),
                new("tests/scripts/mcp/01-read.txt", ["# Workflow: Let an AI tool read a window", "start down", "# Goal: Turn the AI tools on", "menu Let AI tools read and open axKit"]),
                new("tests/nvda-scripts/axDown-notices.txt", ["# Workflow: Hear a notice with NVDA", "start down", "keys f1"]),
            ],
            Inventory);

        // 5.6: the preamble word for word; a script with no goal line has no line under its label, and a check has none.
        Assert.Equal(
            Text("""
                # Workflows

                Written by `tools/workflows.ps1` from the scripts of `tests/scripts` and `tests/nvda-scripts` and the command
                inventory (`docs/axKit/window-commands.json`); never edit it by hand. What a user can do in axKit, by app: each
                workflow is one script of the automated checks, found by its label, a task in the user's words, and under it are its
                goals. The steps of every goal, with the keys as they are now, are in the app's page. The checks run on the
                developer's computer (`tools/AxAutomate`, `nvda-pass.ps1`), never in CI (`docs/axKit/automation.md`,
                `docs/axKit/user-workflows.md`).

                ## axDown

                Steps and keys: `docs/axKit/workflows/axDown.md` (in Claude's working folder: `workflows-axDown.md`).

                1. **Open a text file in axDown**
                2. **Find text and go to a line**
                    - Find a word in the text
                    - Go to a line by its number

                ## AI tools

                Steps and keys: `docs/axKit/workflows/ai-tools.md` (in Claude's working folder: `workflows-ai-tools.md`).

                1. **Let an AI tool read a window**
                    - Turn the AI tools on

                ## NVDA only

                Steps and keys: `docs/axKit/workflows/nvda-only.md` (in Claude's working folder: `workflows-nvda-only.md`).

                1. **Hear a notice with NVDA**
                """),
            Page(pages, Reference.IndexPath));
    }

    [Fact]
    public void The_groups_are_the_apps_by_name_then_AI_tools_then_NVDA_only()
    {
        string[] script = ["# Workflow: W", "start calc", "type 1"];
        var pages = Reference.Pages(
            [
                new("tests/nvda-scripts/axDown-notices.txt", script),
                new("tests/scripts/mcp/01-read.txt", script),
                new("tests/scripts/axTask/01-lists.txt", script),
                new("tests/scripts/AxFile/01-browse.txt", script),
                new(@"tests\scripts\axDown\B-second.txt", ["# Workflow: Second by its path", "start down"]),
                new("tests/scripts/axDown/a-first.txt", ["# Workflow: First by its path", "start down"]),
                new("tests/scripts/axCalc/01-basics.txt", script),
            ],
            []);

        // WF-7.8: the index, the apps without regard to case, the two other groups, the commands page.
        Assert.Equal(
            [
                "docs/axKit/workflows.md",
                "docs/axKit/workflows/axCalc.md",
                "docs/axKit/workflows/axDown.md",
                "docs/axKit/workflows/AxFile.md",
                "docs/axKit/workflows/axTask.md",
                "docs/axKit/workflows/ai-tools.md",
                "docs/axKit/workflows/nvda-only.md",
                "docs/axKit/window-commands.md",
            ],
            pages.Select(page => page.Path));
        Assert.Equal(
            ["## axCalc", "## axDown", "## AxFile", "## axTask", "## AI tools", "## NVDA only"],
            Page(pages, Reference.IndexPath).Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)));

        // In a group the scripts by their path, ordinal, lower case: "a-first" before "B-second".
        Assert.Equal(
            ["## First by its path", "## Second by its path"],
            Page(pages, "docs/axKit/workflows/axDown.md").Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)));

        Assert.Equal("axDown", Reference.Group("tests/scripts/axDown/04-find-and-go-to-line.txt"));
        Assert.Equal("AI tools", Reference.Group("tests/scripts/mcp/01-read-and-pair.txt"));
        Assert.Equal("NVDA only", Reference.Group(@"tests\nvda-scripts\axDown-notices.txt"));
        Assert.Equal("axDown", Reference.PageName("axDown"));
        Assert.Equal("ai-tools", Reference.PageName("AI tools"));
        Assert.Equal("nvda-only", Reference.PageName("NVDA only"));
    }

    [Fact]
    public void A_groups_page_is_the_documents_example_to_the_letter()
    {
        var pages = Reference.Pages([new(FindPath, Find)], Inventory);

        // 5.6: the preamble, how the workflow starts, and each goal's steps with what is heard; nothing else.
        Assert.Equal(
            Text($"""
                # axDown: workflows

                {GroupPreamble}

                ## Find text and go to a line

                Starts with: Open axDown with the file find.md.

                ### Find a word in the text

                1. Press Ctrl+F (Find; Navigation menu).
                2. Type: apple
                3. Press Enter.
                4. Press F3 (Find next; Navigation menu).
                5. Press Shift+F3 (Find previous; Navigation menu).

                ### Go to a line by its number

                1. Press Ctrl+G (Go to line; Navigation menu).
                2. Type: 5
                3. Press Enter.
                    You hear: Line 5 of 7.
                """),
            Page(pages, "docs/axKit/workflows/axDown.md"));
    }

    [Fact]
    public void A_script_with_no_goal_line_has_its_steps_under_its_workflow()
    {
        var pages = Reference.Pages([new("tests/scripts/axDown/01-start.txt", Open)], Inventory);

        // The window steps before the first other input are how it starts; the steps follow with no goal's heading.
        // What is heard before step 1 has no spaces before it (four would make it a code block in Markdown); under a
        // step it has four, so that it stays inside an item numbered 10 or more.
        Assert.Equal(
            Text($"""
                # axDown: workflows

                {GroupPreamble}

                ## Open a text file in axDown

                Starts with: Open axDown with the file notes.md.

                You hear: notes.md, line 1.
                1. Press Ctrl+End.
                    You hear: Line 3 of 3.
                """),
            Page(pages, "docs/axKit/workflows/axDown.md"));
        Assert.DoesNotContain("   - ", Page(pages, Reference.IndexPath));
    }

    [Fact]
    public void A_script_whose_only_inputs_open_windows_has_them_as_its_steps()
    {
        var pages = Reference.Pages(
            [new("tests/scripts/axDown/01-start.txt", ["# Workflow: Open axDown with an empty document", "start down", "expect said Untitled, empty document"])],
            Inventory);

        var page = Page(pages, "docs/axKit/workflows/axDown.md");
        Assert.EndsWith("## Open axDown with an empty document\n\n1. Open axDown.\n    You hear: Untitled, empty document.\n", page);
        Assert.DoesNotContain("Starts with:", page);
    }

    [Fact]
    public void A_workflow_starts_with_the_window_steps_of_its_setup()
    {
        var pages = Reference.Pages(
            [
                new("tests/scripts/axDown/03-two-windows.txt",
                [
                    "# Workflow: Work in two windows",
                    "file a.md A",
                    "start down {temp}\\a.md",
                    "keys ctrl+end",
                    "start file {temp}",
                    "use 1",
                    "# Goal: Find a word",
                    "do Find",
                ]),
            ],
            Inventory);

        // The setup's window steps as sentences, each with its full stop; its key press is no part of how it starts.
        Assert.Contains(
            "## Work in two windows\n\nStarts with: Open axDown with the file a.md. Open axFile with the test folder. Go to the axDown window.\n\n### Find a word\n\n1. Press Ctrl+F (Find; Navigation menu).\n",
            Page(pages, "docs/axKit/workflows/axDown.md"));
    }

    [Fact]
    public void A_check_is_in_no_index_and_on_no_page()
    {
        var pages = Reference.Pages([new(FindPath, Find)], Inventory);

        foreach (var page in pages)
        {
            Assert.DoesNotContain("A word that is not in the text is said as not found", page.Text);
            Assert.DoesNotContain("The status bar holds", page.Text);
            // The checks' own steps and words.
            Assert.DoesNotContain("Type: pear", page.Text);
            Assert.DoesNotContain("Not found here", page.Text);
            Assert.DoesNotContain("Not a line number", page.Text);
            Assert.DoesNotContain("Check:", page.Text);
        }
    }

    [Fact]
    public void A_section_without_an_input_step_is_left_out()
    {
        var pages = Reference.Pages(
            [
                new("tests/scripts/axCalc/01-basics.txt",
                [
                    "# Workflow: Calculate",
                    "start calc",
                    "# Goal: See the field",
                    "expect focus Expression",
                    "expect said Expression, edit",
                    "# Goal: Add two numbers",
                    "type 1+1",
                    "keys enter",
                ]),
            ],
            []);

        // WF-D-28: a how-to with nothing to do is no how-to; the generator leaves it out of the index and of the page.
        Assert.DoesNotContain("See the field", Page(pages, Reference.IndexPath));
        Assert.Contains("1. **Calculate**\n    - Add two numbers\n", Page(pages, Reference.IndexPath));
        var page = Page(pages, "docs/axKit/workflows/axCalc.md");
        Assert.DoesNotContain("See the field", page);
        Assert.DoesNotContain("Expression", page);
        Assert.EndsWith("### Add two numbers\n\n1. Type: 1+1\n2. Press Enter.\n", page);

        Assert.Empty(Lines("expect focus Expression", "expect said Expression, edit"));
    }

    [Fact]
    public void A_goals_inputs_are_numbered_sentences_and_what_is_heard_stands_under_them()
    {
        Assert.Equal(
            [
                "1. Open axDown with the file find.md.",
                "2. Press Ctrl+G (Go to line; Navigation menu).",
                "3. Type: 5",
                "4. Press Enter.",
                "    You hear: Line 5 of 7.",
                "    You hear the sound Edge.",
                "5. A new window opens: go to it.",
                "6. Go to the axDown window.",
                "7. Call the tool axkit_windows.",
                "    The tool answers with app: axDown.",
                "8. Start axDown again with the file other.md.",
                "9. Close the window.",
            ],
            Lines(
                "start down {temp}\\find.md",
                "do Go to line",
                "type 5",
                "keys enter",
                "expect said ! Line 5 of 7",
                "expect said ♪ Edge",
                "adopt",
                "use 1",
                "mcp call axkit_windows",
                "expect mcp \"app\": \"axDown\"",
                "launch down {temp}\\other.md",
                "close"));
    }

    [Fact]
    public void An_expectation_before_the_first_input_is_a_line_before_step_one()
    {
        Assert.Equal(
            ["You hear: Go to line, edit.", "1. Type: 5", "    You hear: Line 5 of 7."],
            Lines("expect said Go to line, edit", "type 5", "expect said ! Line 5 of 7"));
    }

    [Fact]
    public void A_do_step_reads_with_its_key_and_its_menus_from_the_inventory()
    {
        Assert.Equal(
            ["1. Open axDown.", "2. Press Ctrl+F (Find; Navigation menu).", "3. Press Ctrl+Y (Redo; Actions menu)."],
            Lines("start down", "do Find", "do redo"));

        // A window the script's lines do not name (adopt): every app is asked, and the key is written when all that have
        // the command read the same, never the key of another app's command of that name.
        Assert.Equal(["1. A new window opens: go to it.", "2. Press Ctrl+G (Go to line; Navigation menu)."], Lines("adopt", "do Go to line"));
        IReadOnlyList<InventoryApp> same = [new("axDown", [Row("Find", "Navigation", "ctrl+f", "Ctrl+F")]), new("axJSON", [Row("Find", "Navigation", "ctrl+f", "Ctrl+F")])];
        Assert.Equal(["1. A new window opens: go to it.", "2. Press Ctrl+F (Find; Navigation menu)."], Lines(same, "adopt", "do Find"));
        IReadOnlyList<InventoryApp> other = [new("axCalc", [Row("Find", "History", "f3", "F3")]), new("axDown", [Row("Find", "Navigation", "ctrl+f", "Ctrl+F")])];
        Assert.Equal(["1. A new window opens: go to it.", "2. Do the command Find."], Lines(other, "adopt", "do Find"));
        Assert.Equal(["1. Open axDown.", "2. Press Ctrl+F (Find; Navigation menu)."], Lines(other, "start down", "do Find"));
    }

    [Fact]
    public void A_command_two_menus_deep_reads_with_its_menus_from_the_bar_down()
    {
        Assert.Equal(
            ["1. Open axDown.", "2. Press Ctrl+H (Heading; Navigation menu, Next).", "3. Press Ctrl+Shift+H (Heading; Navigation menu, Previous)."],
            Lines("start down", "do Heading, Navigation, Next", "do Heading, Navigation, Previous"));

        // The same command without a key is chosen in its menus.
        IReadOnlyList<InventoryApp> keyless = [new("axDown", [Row("Heading", "Navigation, Next"), Row("Heading", "Navigation, Previous")])];
        Assert.Equal(["1. Open axDown.", "2. Choose Heading in the Navigation menu, Next."], Lines(keyless, "start down", "do Heading, Navigation, Next"));
    }

    [Fact]
    public void A_do_step_of_a_key_alone_reads_without_a_menu()
    {
        Assert.Equal(
            ["1. Open axDown.", "2. Press Ctrl+Tab (Between the text and the details)."],
            Lines("start down", "do Between the text and the details"));
    }

    [Fact]
    public void A_do_step_without_a_chord_reads_as_a_menu_choice()
    {
        // The menu shows "Enter in the message field", which is no chord a script presses: the step chooses the item.
        Assert.Equal(["1. Open axClaude.", "2. Choose Send message in the Actions menu."], Lines("start claude", "do Send message"));
        Assert.Equal(["1. Open axFile.", "2. Choose Move to folder in the Context menu."], Lines("start file", "do Move to folder"));
    }

    [Fact]
    public void A_do_step_the_inventory_does_not_know_reads_as_the_command_alone()
    {
        Assert.Equal(["1. Open axDown.", "2. Do the command No such command."], Lines("start down", "do No such command"));
        // Several commands of the name, and an app the inventory has no rows for.
        Assert.Equal(["1. Open axDown.", "2. Do the command Heading."], Lines("start down", "do Heading"));
        Assert.Equal(["1. Open axCalc.", "2. Do the command Find."], Lines("start calc", "do Find"));
        Assert.Equal(["1. Open axDown.", "2. Do the command Find."], Lines([], "start down", "do Find"));
    }

    [Fact]
    public void A_rows_key_text_is_printed_where_it_differs_from_the_chords_words()
    {
        // The window's own spelling is what its menu shows: "Ctrl++" for the chord ctrl+plus. A keys line of that chord
        // reads the same way, so that one key has one spelling on a page.
        Assert.Equal(
            ["1. Open axDown.", "2. Press Ctrl++ (Larger text; Options menu, Text).", "3. Type: x", "4. Press Ctrl++ (Larger text; Options menu, Text)."],
            Lines("start down", "do Larger text", "type x", "keys ctrl+plus"));

        // A row whose only chord was folded in from a key with no menu item: that chord, in key words.
        IReadOnlyList<InventoryApp> folded = [new("axDown", [Row("Redo", "Actions", also: ["ctrl+shift+z"])])];
        Assert.Equal(["1. Open axDown.", "2. Press Ctrl+Shift+Z (Redo; Actions menu)."], Lines(folded, "start down", "do Redo"));
    }

    [Fact]
    public void A_single_chord_names_its_command_when_exactly_one_row_holds_it()
    {
        Assert.Equal(
            [
                "1. Open axDown.",
                // The command is named as a do step names it: its label, then its menus from the bar down.
                "2. Press Ctrl+G (Go to line; Navigation menu).",
                // Among a row's further chords, and written in another order and case: the key is the one pressed.
                "3. Press Ctrl+Shift+Z (Redo; Actions menu).",
                "4. Press Shift+Ctrl+Z (Redo; Actions menu).",
                "5. Press F6 (Between the text and the details).",
                // A label two menu items share: the menus tell them apart, as after a do step.
                "6. Press Ctrl+H (Heading; Navigation menu, Next).",
                // No row holds it.
                "7. Press Ctrl+Q.",
            ],
            Lines("start down", "keys ctrl+g", "keys ctrl+shift+z", "keys Shift+Ctrl+Z", "keys f6", "keys ctrl+h", "keys ctrl+q"));

        // Two rows hold the chord, and a window the lines do not name: no command is named.
        IReadOnlyList<InventoryApp> twice = [new("axDown", [Row("Find", "Navigation", "ctrl+f", "Ctrl+F"), Row("Filter", "Navigation", "ctrl+f", "Ctrl+F")])];
        Assert.Equal(["1. Open axDown.", "2. Press Ctrl+F."], Lines(twice, "start down", "keys ctrl+f"));
        Assert.Equal(["1. A new window opens: go to it.", "2. Press Ctrl+G."], Lines("adopt", "keys ctrl+g"));
    }

    [Fact]
    public void A_plain_key_names_no_command()
    {
        // axFile has Enter for Open and Backspace for Up one level; in the field of a notice they are the field's keys,
        // and the page cannot know where the focus is: only a chord with Ctrl or Alt, or a function key, is named.
        Assert.Equal(
            ["1. Open axFile.", "2. Press Ctrl+Shift+N (New folder; Session menu).", "3. Type: notes", "4. Press Backspace.", "5. Press Enter.", "6. Press Alt+Up (Up one level; Navigation menu)."],
            Lines("start file", "keys ctrl+shift+n", "type notes", "keys backspace", "keys enter", "keys alt+up"));
    }

    [Fact]
    public void A_line_of_several_chords_names_none()
    {
        Assert.Equal(
            ["1. Open axDown.", "2. Press Ctrl+G, Ctrl+F.", "3. Press Down, Down, Enter."],
            Lines("start down", "keys ctrl+g ctrl+f", "keys down down enter"));
    }

    [Fact]
    public void A_menu_step_names_its_menus_and_its_key_when_one_row_answers_it_and_reads_plainly_otherwise()
    {
        Assert.Equal(
            [
                "1. Open axDown.",
                // Written as the choice it is: the check chose the item and did not press the key.
                "2. Choose Larger text in the Options menu, Text (key: Ctrl++).",
                "3. Choose Heading in the Navigation menu, Previous (key: Ctrl+Shift+H).",
                // Several rows answer "Find" (Find, Find next, Find previous); none answers the last.
                "4. Choose the menu item Find.",
                "5. Choose the menu item Let AI tools read and open axKit.",
            ],
            Lines("start down", "menu Larger text", "menu Heading, Navigation, Previous", "menu Find", "menu Let AI tools read and open axKit"));

        // An item without a key, and one whose key the menu only shows as text.
        Assert.Equal(["1. Open axFile.", "2. Choose Move to folder in the Context menu."], Lines("start file", "menu Move to folder, Context"));
        Assert.Equal(
            ["1. Open axClaude.", "2. Choose Send message in the Actions menu (key: Enter in the message field)."],
            Lines("start claude", "menu Send message"));
    }

    [Fact]
    public void A_start_reads_as_opening_the_app_by_its_name()
    {
        Assert.Equal(
            ["1. Open axDown with the file find.md.", "2. Open axDown.", "3. Open the file settings.json.", "4. Open axFile with the test folder.", "5. Open axTask with the folder tasks."],
            Lines("start down {temp}\\find.md", "start down", "start {temp}\\settings.json", "start file {temp}", "start Task {temp}\\tasks"));

        // A path in the run's folder is named by what it is, a name with an extension a file; a path of the user's own
        // stands as it is, and a name in quotes is one name.
        Assert.Equal(
            ["1. Open axMedia with the file ch\\alpha.wav.", "2. Open axDown with C:\\notes\\a.md.", "3. Open axTerminal with the folder work\\my project."],
            Lines("start media {temp}\\ch\\alpha.wav", "start down C:\\notes\\a.md", "start terminal \"{temp}\\work\\my project\""));
    }

    [Fact]
    public void A_starts_options_that_only_serve_the_check_are_left_out()
    {
        Assert.Equal(
            [
                // What follows " -- " in a start of axClaude is Claude Code's own options: the check's model.
                "1. Open axClaude with the folder fresh.",
                "2. Open axClaude with the folder proj.",
                // --record with its file, --model with its value, and --claude with the check's stand-in in the run's folder.
                "3. Open axClaude with the folder opts.",
                "4. Open axClaude with the folder st.",
                // A program of the user's own stays, and so does an option a person gives.
                "5. Open axClaude with the folder gone --claude C:\\nowhere\\claude.exe.",
                "6. Open axMedia with --add the folder ch.",
                "7. Open axPreview with --new the file plain.txt.",
                // In another app's start a bare -- is no end of what the user gives.
                "8. Open axDown with -- the file a.md.",
            ],
            Lines(
                "start claude {temp}\\fresh -- --model haiku",
                "start claude {temp}\\proj -- --model haiku --continue",
                "start claude {temp}\\opts --record {temp}\\raw.vt -- --model haiku",
                "start claude {temp}\\st --claude {temp}\\claude.cmd -- --model haiku",
                "start claude {temp}\\gone --claude C:\\nowhere\\claude.exe -- --model haiku",
                "start media --add {temp}\\ch",
                "start preview --new {temp}\\plain.txt",
                "start down -- {temp}\\a.md"));
    }

    [Fact]
    public void Window_steps_read_as_what_a_person_does()
    {
        Assert.Equal(
            [
                "1. Open axDown with the file a.md.",
                "2. Open axFile with the test folder.",
                // use: the window is named by its app, read off the script's own start lines.
                "3. Go to the axDown window.",
                "4. Go to the axFile window.",
                // adopt: the app opened a window of its own.
                "5. A new window opens: go to it.",
                // The window adopted has no app in the script's lines.
                "6. Go to the axDown window.",
                "7. Go to the other window.",
                // launch hands over to an open window: for a person, starting the app again.
                "8. Start axClip again.",
                "9. Start axMedia again with --add the folder ch.",
                "10. Start axPreview again with the file plain.txt.",
                "11. Open the file b.md again.",
                "12. Close the window.",
            ],
            Lines(
                "start down {temp}\\a.md",
                "start file {temp}",
                "use 1",
                "use 2",
                "adopt",
                "use 1",
                "use 3",
                "launch clip",
                "launch media --add {temp}\\ch",
                "launch preview {temp}\\plain.txt",
                "launch {temp}\\b.md",
                "close"));

        // Two windows of one app: the one gone to is the other one. A number no window has: the other window.
        Assert.Equal(
            ["1. Open axDown with the file one.md.", "2. Open axDown with the file two.md.", "3. Go to the other axDown window.", "4. Go to the other window."],
            Lines("start down {temp}\\one.md", "start down {temp}\\two.md", "use 1", "use 7"));
    }

    [Fact]
    public void Keys_are_named_as_a_keyboard_names_them()
    {
        Assert.Equal(
            ["1. Press Page Down.", "2. Press Ctrl+Page Up.", "3. Press NVDA+A.", "4. Press Escape, Shift+Page Down.", "5. Press Ctrl+Shift+P, Down."],
            Lines("keys pagedown", "keys ctrl+pageup", "keys nvda+a", "keys esc shift+pagedown", "keys ctrl+shift+p down"));

        // The commands page's further chords are in the same key words; a row's own key text is the window's, as it is.
        IReadOnlyList<InventoryApp> paging = [new("axClip", [KeyAlone("Page down", "pagedown", "Next", "ctrl+pagedown")])];
        Assert.Contains("- Page down. Menu: none, a key alone. Key: Next. Also: Ctrl+Page Down. Mark: open. Never run by AI tools.\n", Page(Reference.Pages([], paging), Reference.CommandsPath));
    }

    [Fact]
    public void Equal_presses_in_a_row_are_one_line()
    {
        Assert.Equal(
            [
                "1. Open axTerminal.",
                // Seven steps, with checks of state between them, which are on no page.
                "2. Press Tab 7 times.",
                // One line of one key several times, and the same key again after it.
                "3. Press Down 5 times.",
                "4. Press Enter.",
                "    You hear: Opened.",
                // What is heard between two presses keeps them apart.
                "5. Press Enter.",
                // Another key, and a line of several keys, stay as they are.
                "6. Press Down, Enter.",
                "7. Press Down, Enter.",
            ],
            Lines(
                "start terminal",
                "keys tab",
                "expect focus Shell",
                "keys tab",
                "keys tab",
                "wait 200",
                "keys tab",
                "keys tab",
                "keys tab",
                "keys tab",
                "keys down down down",
                "keys down",
                "keys down",
                "keys enter",
                "expect said Opened",
                "keys enter",
                "keys down enter",
                "keys down enter"));

        // A command named after its key keeps its name after the count; typed text is never counted.
        Assert.Equal(
            ["1. Open axDown.", "2. Press F3 3 times (Find next; Navigation menu).", "3. Type: a", "4. Type: a"],
            Lines("start down", "do Find next", "keys f3", "do Find next", "type a", "type a"));

        // A step that runs only when words came is a line of its own.
        Assert.Equal(
            ["1. Press Enter.", "    You may hear: trust.", "2. If that came: Press Enter.", "3. Press Enter."],
            Lines("keys enter", "try said trust", "iffound keys enter", "keys enter"));
    }

    [Fact]
    public void Typed_text_ends_its_line_with_nothing_added()
    {
        // A listener must know that no full stop is typed: the line ends with the text, also after "If that came".
        Assert.Equal(
            ["1. Type: if ($true) {", "2. Type: }", "3. Type: cmd.exe /k", "4. Type: cd ..", "    You may hear: sure.", "5. If that came: Type: y"],
            Lines("type if ($true) {", "type }", "type cmd.exe /k", "type cd ..", "try said sure", "iffound type y"));
    }

    [Fact]
    public void A_menu_text_cut_short_reads_whole_from_the_inventory_or_without_its_open_bracket()
    {
        // The script names the item by its first words, since the rest changes with every release.
        IReadOnlyList<InventoryApp> one = [new("axDown", [Row("Check for updates (axKit 5.9.1)", "Help", mark: "act"), Row("Check for updates at start", "Options, General", mark: "act")])];
        Assert.Equal(
            ["1. Open axDown.", "2. Choose Check for updates (axKit 5.9.1) in the Help menu."],
            Lines(one, "start down", "menu Check for updates (axKit"));

        // No row, or several, begin with it: as the script has it, without the bracket that never closes.
        Assert.Equal(["1. Open axDown.", "2. Choose the menu item Check for updates."], Lines("start down", "menu Check for updates (axKit"));
        Assert.Equal(["1. Open axDown.", "2. Choose the menu item Check for updates."], Lines(one, "start down", "menu Check for updates"));
        Assert.Equal(["1. Open axDown.", "2. Choose the menu item Save as (Ctrl+Shift+S)."], Lines("start down", "menu Save as (Ctrl+Shift+S)"));
    }

    [Fact]
    public void The_marks_of_an_expectation_read_as_words()
    {
        Assert.Equal(
            [
                "1. Press F1.",
                "    You hear: Line 5 of 7.",
                "    You hear the sound Edge.",
                "    You hear the sound BackgroundChord 3.",
                "    The focus moves to Message to Claude (Text).",
                "    The focus moves to Terminal.",
                "    It reads beta.wav.",
                "    You may hear: Not in a test run.",
                "    You may hear the sound Notice.",
                "    NVDA says: Yes, run it.",
                "    The tool answers with offered: true.",
            ],
            Lines(
                "keys f1",
                "expect said ! Line 5 of 7",
                "expect said ♪ Edge",
                "expect said ♪ BackgroundChord 3",
                "expect said → Message to Claude (Text)",
                "expect said → Terminal",
                "expect said · beta.wav",
                "try said Not in a test run",
                "try said ♪ Notice",
                "expect spoken Yes, run it",
                "expect mcp \"offered\": true"));
    }

    [Fact]
    public void A_text_that_reads_whole_is_heard_and_one_cut_short_is_words_such_as()
    {
        Assert.Equal(
            [
                "1. Press Down.",
                // Whole: written without a comma or colon at either end, every bracket closed, no joining word last.
                "    You hear: Sent: 1.",
                "    You hear: bundle, 2 items, New file.txt.",
                "    You hear: Turn on AI tools?",
                "    You hear: Input 11 (from Claude).",
                "    You hear: AI tools on.",
                // Cut short at a comma or a colon, at either end.
                "    You hear words such as: 13 items.",
                "    You hear words such as: nav, 13 items.",
                "    NVDA says words such as: Save as.",
                // A bracket that opens and never closes goes, with what follows it.
                "    You hear words such as: 2. orchard.",
                "    You hear words such as: Output 11 Reply from Claude.",
                // A joining word at the end goes when two words stay without it.
                "    You hear words such as: Before axTerminal connects.",
                "    You hear words such as: New window, Session, Ctrl+Shift+N, 2.",
                "    You hear words such as: Paused at.",
                "    You hear words such as: 2 of.",
                "    You may hear words such as: Line 1.",
                // A bar at the end; a text with no letter and no digit, and a joining word alone, are left out.
                "    You hear words such as: Claude: ready.",
                "    You hear words such as: tasks.",
                "    You hear: Working…",
            ],
            Lines(
                "keys down",
                "expect said Sent: 1.",
                "expect said ! bundle, 2 items, New file.txt",
                "expect said Turn on AI tools?",
                "expect said Input 11 (from Claude)",
                "expect said AI tools on",
                "expect said , 13 items",
                "expect said nav, 13 items,",
                "expect spoken Save as:",
                "expect said 2. orchard (",
                "expect said Output 11 Reply from Claude (",
                "expect said Before axTerminal connects to",
                "expect said New window, Session, Ctrl+Shift+N, 2 of",
                "expect said Paused at",
                "expect said , 2 of",
                "try said Line 1 of",
                "expect said Claude: ready |",
                "expect said · @@",
                "expect said of",
                "expect said !",
                // A backslash first is the middle of a path; an ellipsis is an end of its own.
                "expect said \\tasks",
                "expect said Working\\u2026"));

        Assert.Equal("2 of", Reference.Heard(", 2 of"));
        Assert.Equal("Line 5 of 7", Reference.Heard("! Line 5 of 7."));
        Assert.Equal("Before axTerminal connects", Reference.Heard("Before axTerminal connects to"));
        Assert.Equal("2. orchard", Reference.Heard("2. orchard ("));
        Assert.Equal("trust", Reference.Heard("trust"));
        Assert.Equal("Edge", Reference.Heard("♪ Edge"));
        Assert.Equal(string.Empty, Reference.Heard("!"));
        Assert.Equal(string.Empty, Reference.Heard("· @@"));
        Assert.True(Reference.IsWhole("! Line 5 of 7."));
        Assert.False(Reference.IsWhole(", 2 of"));
        Assert.False(Reference.IsWhole("2. orchard ("));
        Assert.False(Reference.IsWhole("Line 1 of"));
    }

    [Fact]
    public void One_word_heard_is_kept_unless_the_line_before_it_already_holds_it()
    {
        Assert.Equal(
            [
                "1. Type: 1920*1080*4",
                "2. Press Enter.",
                // One number is the whole point after a sum; one word after a key says what the key did.
                "    You hear: 8294400.",
                "3. Press Escape.",
                "    You hear: Cleared.",
                "    You may hear: Empty.",
                // NVDA saying the same word again only repeats the line before it; another word stays.
                "    The tool answers: notes.md.",
                // The word typed, said back, and the key's own name tell nothing new.
                "4. Type: apple",
                "5. Press Enter.",
                // The file's name under the step that opens it, from the window and from a tool: both only repeat it.
                "6. Open axDown with the file notes.md.",
                "    You hear: Ready.",
            ],
            Lines(
                "type 1920*1080*4",
                "keys enter",
                "expect said ! 8294400",
                "keys escape",
                "expect said ! Cleared",
                "try said Empty",
                "expect spoken Empty",
                "expect mcp notes.md",
                "type apple",
                "expect said apple",
                "keys enter",
                "expect said Enter",
                "start down {temp}\\notes.md",
                "expect said notes.md",
                "expect mcp notes.md",
                "expect said Ready",
                "expect spoken ready"));
    }

    [Fact]
    public void A_tools_answer_written_as_data_reads_as_a_sentence()
    {
        Assert.Equal(
            [
                "1. Call the tool axkit_windows.",
                "    The tool answers with app: axDown.",
                "    The tool answers with title: notes.md - axDown.",
                "    The tool answers with offered: true.",
                // A name of data in plain words, and a value the check cut short.
                "    The tool answers with not done: 1.",
                "    The tool answers with done left out: 1.",
                "    The tool answers with notice: Find.",
                // Any other text as it is.
                "    The tool answers: error: The user did not allow axAutomate.",
                "    The tool answers: First line of the notes.",
            ],
            Lines(
                "mcp call axkit_windows",
                "expect mcp \"app\": \"axDown\"",
                "expect mcp \"title\": \"notes.md - axDown\"",
                "expect mcp \"offered\": true",
                "expect mcp \"notDone\": 1",
                "expect mcp \"doneLeftOut\": 1",
                "expect mcp \"notice\": \"Find",
                "expect mcp error: The user did not allow axAutomate",
                "expect mcp First line of the notes."));
    }

    [Fact]
    public void A_step_that_runs_only_when_words_came_reads_after_if_that_came()
    {
        // The try's one word stays, since the step under it says "If that came"; the waits are left out.
        Assert.Equal(
            [
                "1. Open axClaude with the folder fresh.",
                "    You may hear: trust.",
                "2. If that came: Press Enter.",
                "3. Type: hello",
                "    You may hear the sound Notice.",
                "4. If that came: Press 1, Enter.",
                "    You hear: Sent: 1.",
            ],
            Lines(
                "start claude {temp}\\fresh",
                "try said trust",
                "iffound wait 1500",
                "iffound keys enter",
                "type hello",
                "try said ♪ Notice",
                "iffound wait 1500",
                "iffound keys 1 enter",
                "iffound expect said Sent: 1"));

        // Words that only come with the try's, before any step of theirs: they may come or not.
        Assert.Equal(
            ["1. Press F1.", "    You may hear: Claude asks.", "    You may hear: Closed the list."],
            Lines("keys f1", "try said Claude asks", "iffound expect said Closed the list"));
    }

    [Fact]
    public void Set_up_steps_waits_and_checks_of_state_are_left_out()
    {
        Assert.Equal(
            ["1. Open axDown with the file a.md.", "2. Press Ctrl+S."],
            Lines(
                "file a.md Hello",
                "mkdir sub",
                "run echo x> b.txt",
                "silence c.wav 500",
                "mcp start",
                "allow error GitHub could not be reached",
                "start down {temp}\\a.md",
                "wait 500",
                "expect focus a.md",
                "expect state Line 1, Column 1",
                "expect text Hello",
                "expect list \"a.md\"",
                "expect commands Find",
                "check commands",
                "refuse said Not found",
                "refuse state \"notice\"",
                "refuse mcp error",
                "expect quiet 1000",
                "said",
                "state",
                "keys ctrl+s",
                "expect closed"));
    }

    [Fact]
    public void The_runs_folder_is_never_written_and_escapes_are_filled()
    {
        Assert.Equal(
            [
                "1. Open axFile with the test folder.",
                "2. Type: sub\\notes.md",
                "    You hear: Saved · notes.md in the test folder.",
                "    You hear the sound Edge.",
                "3. Type: café",
            ],
            Lines(
                "start file {temp}",
                "type {temp}\\sub\\notes.md",
                "expect said Saved \\u00B7 {temp}\\notes.md in {temp}",
                "expect said \\u266A Edge",
                "type caf\\u00E9"));

        var pages = Reference.Pages([new(FindPath, Find), new("tests/scripts/axDown/01-start.txt", Open)], Inventory);
        Assert.All(pages, page => Assert.DoesNotContain("{temp}", page.Text));
        Assert.All(pages, page => Assert.DoesNotContain("\\u00", page.Text));
    }

    [Fact]
    public void A_sentence_that_ends_itself_gets_no_second_full_stop()
    {
        Assert.Equal(
            ["1. Type: Reply with the single word second.", "    You hear: Turn on AI tools?", "2. Type: What is 2+2?", "3. Type: a?b", "4. Choose the menu item Save as..."],
            Lines("type Reply with the single word second.", "expect said Turn on AI tools?", "type What is 2+2?", "type a?b", "menu Save as..."));
    }

    [Fact]
    public void A_page_holds_no_script_path_no_needs_line_and_nothing_of_a_run()
    {
        var pages = Reference.Pages([new(FindPath, Find), new("tests/scripts/axDown/01-start.txt", Open)], Inventory);

        foreach (var page in pages)
        {
            Assert.DoesNotContain(page.Text.Split('\n'), line =>
                line.TrimStart().StartsWith("Script:", StringComparison.Ordinal) || line.TrimStart().StartsWith("Needs:", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("Checked:", StringComparison.Ordinal) || line.TrimStart().StartsWith("Through the AI tools:", StringComparison.Ordinal));
            // WF-7.5: no script path, nothing of the header (what the script covers, what it needs), no file the setup made.
            Assert.DoesNotContain("04-find-and-go-to-line", page.Text);
            Assert.DoesNotContain("01-start", page.Text);
            Assert.DoesNotContain("nothing outside axKit", page.Text);
            Assert.DoesNotContain("test plan", page.Text);
            Assert.DoesNotContain("checkForUpdates", page.Text);
            Assert.DoesNotContain("passed", page.Text);
        }
    }

    [Fact]
    public void Every_line_of_a_page_ends_with_a_line_feed_alone()
    {
        var pages = Reference.Pages([new(FindPath, Find), new("tests/scripts/axDown/01-start.txt", Open)], Inventory);

        Assert.All(pages, page =>
        {
            Assert.EndsWith("\n", page.Text);
            Assert.False(page.Text.EndsWith("\n\n", StringComparison.Ordinal), page.Path + " ends with a blank line");
            Assert.DoesNotContain("\r", page.Text);
            Assert.DoesNotContain("\n\n\n", page.Text);
        });
    }

    [Fact]
    public void The_commands_page_has_one_line_a_command_under_its_app()
    {
        // The apps by name, whatever the order given; an app without rows has no heading.
        IReadOnlyList<InventoryApp> inventory = [Inventory[2], new("axCalc", []), Inventory[1], Inventory[0]];

        Assert.Equal(
            Text("""
                # The commands of every window

                Written by `tools/workflows.ps1` from `docs/axKit/window-commands.json`, which the automated checks write from the
                windows themselves; never edit it by hand. One line a command: its name, its menu, its key, and its mark, what it
                does beyond reading (read, open, change, act). A command that all or nearly all windows have stands once, under "In
                every window" or under a heading that names the windows without it; under an app's name are the commands of its own.

                ## axClaude

                - Send message. Menu: Actions. Key: Enter in the message field. Mark: act.

                ## axDown

                - Redo. Menu: Actions. Key: Ctrl+Y. Also: Ctrl+Shift+Z. Mark: change.
                - Undo. Menu: Actions. Key: Ctrl+Z. Mark: change.
                - Find. Menu: Navigation. Key: Ctrl+F. Mark: read.
                - Find next. Menu: Navigation. Key: F3. Mark: read.
                - Find previous. Menu: Navigation. Key: Shift+F3. Mark: read.
                - Go to line. Menu: Navigation. Key: Ctrl+G. Mark: read.
                - Heading. Menu: Navigation, Next. Key: Ctrl+H. Mark: read.
                - Heading. Menu: Navigation, Previous. Key: Ctrl+Shift+H. Mark: read.
                - Larger text. Menu: Options, Text. Key: Ctrl++. Mark: change.
                - Word wrap. Menu: Options, Text. Key: Ctrl+Shift+W. Mark: change.
                - Between the text and the details. Menu: none, a key alone. Key: Ctrl+Tab. Also: Ctrl+Shift+Tab, F6. Mark: open. Never run by AI tools.

                ## axFile

                - Move to folder. Menu: Context. Key: none. Mark: change.
                - Open. Menu: Context. Key: Enter. Mark: open.
                - Up one level. Menu: Navigation. Key: Backspace. Also: Alt+Up, Left. Mark: open.
                - New folder. Menu: Session. Key: Ctrl+Shift+N. Mark: change.
                """),
            Page(Reference.Pages([], inventory), Reference.CommandsPath));

        // An empty inventory, as before the first run that writes it: the preamble alone.
        Assert.EndsWith("under an app's name are the commands of its own.\n", Page(Reference.Pages([], []), Reference.CommandsPath));
    }

    [Fact]
    public void The_commands_every_window_has_are_written_once()
    {
        // Four apps: the two rows of the axKit menu in all of them, Exit in three of the four (three in four is enough),
        // Refresh in two (too few), and a row whose key differs in one app, which is that app's own there.
        var forget = Row("Forget AI tools", "axKit, AI tools", mark: "act", ai: false);
        var theme = Row("Reload theme", "axKit, Theme", mark: "change");
        var exit = Row("Exit", "Session", "alt+f4", "Alt+F4", "act");
        var refresh = Row("Refresh", "Session", "f5", "F5");
        var status = Row("Say the status", "Window", "f8", "F8");
        IReadOnlyList<InventoryApp> inventory =
        [
            new("axFile", [forget, theme, exit, refresh, status, Row("New folder", "Session", "ctrl+shift+n", "Ctrl+Shift+N", "change")]),
            new("axClip", [forget, theme, status, Row("Paste", "Actions", "enter", "Enter", "act")]),
            new("axDown", [forget, theme, exit, status, Row("Find", "Navigation", "ctrl+f", "Ctrl+F")]),
            new("axGIT", [forget, theme, exit, refresh, Row("Say the status", "Window", "f9", "F9")]),
        ];

        Assert.Equal(
            Text("""
                ## In every window

                - Forget AI tools. Menu: axKit, AI tools. Key: none. Mark: act. Never run by AI tools.
                - Reload theme. Menu: axKit, Theme. Key: none. Mark: change.

                ## In every window but axClip

                - Exit. Menu: Session. Key: Alt+F4. Mark: act.

                ## In every window but axGIT

                - Say the status. Menu: Window. Key: F8. Mark: read.

                ## axClip

                - Paste. Menu: Actions. Key: Enter. Mark: act.

                ## axDown

                - Find. Menu: Navigation. Key: Ctrl+F. Mark: read.

                ## axFile

                - Refresh. Menu: Session. Key: F5. Mark: read.
                - New folder. Menu: Session. Key: Ctrl+Shift+N. Mark: change.

                ## axGIT

                - Refresh. Menu: Session. Key: F5. Mark: read.
                - Say the status. Menu: Window. Key: F9. Mark: read.
                """),
            Page(Reference.Pages([], inventory), Reference.CommandsPath).Split("commands of its own.\n\n")[1]);

        // Two apps share only what both have; one app shares nothing; every row of the inventory is on the page once.
        var two = Page(Reference.Pages([], [inventory[0], inventory[2]]), Reference.CommandsPath);
        Assert.Contains("## In every window\n\n- Forget AI tools.", two);
        Assert.DoesNotContain("## In every window but", two);
        Assert.Equal(1, two.Split('\n').Count(line => line.StartsWith("- Exit.", StringComparison.Ordinal)));
        Assert.DoesNotContain(Reference.EveryWindow, Page(Reference.Pages([], [inventory[0]]), Reference.CommandsPath));
    }

    [Fact]
    public void A_script_without_a_label_is_refused_by_name()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => Reference.Pages(
            [
                new(FindPath, Find),
                new("tests/scripts/axDown/09-no-label.txt", ["# axDown, something it covers.", "start down"]),
                new(@"tests\scripts\axFile\02-empty-label.txt", ["# Workflow:", "start file"]),
            ],
            Inventory));

        Assert.Equal("No '# Workflow:' line: tests/scripts/axDown/09-no-label.txt, tests/scripts/axFile/02-empty-label.txt", refused.Message);
        Assert.Throws<InvalidOperationException>(() => Reference.Counts([new("tests/scripts/axDown/09-no-label.txt", ["start down"])]));
    }

    [Fact]
    public void The_counts_are_the_workflows_and_the_how_tos_on_the_pages()
    {
        // Two goals of the first script (its five checks are none), the steps of the second, and nothing of a script of checks alone.
        Assert.Equal(
            (3, 3),
            Reference.Counts(
            [
                new(FindPath, Find),
                new("tests/scripts/axDown/01-start.txt", Open),
                new("tests/scripts/axDown/07-checks.txt", ["# Workflow: Keep the text in order", "start down", "# Check: A line stays where it was", "keys ctrl+end"]),
            ]));
    }

    [Fact]
    public void The_repositorys_scripts_make_their_pages()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        var scripts = Directory.GetFiles(Path.Combine(root, "tests", "scripts"), "*.txt", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests", "nvda-scripts"), "*.txt", SearchOption.AllDirectories))
            .Select(path => new ReferenceScript(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllLines(path)))
            .ToList();
        Assert.NotEmpty(scripts);

        var pages = Reference.Pages(scripts, []);

        // The index, a page for each folder of scripts, the commands page; and every workflow's label in the index.
        Assert.Equal(scripts.Select(script => Reference.Group(script.Path)).Distinct().Count() + 2, pages.Count);
        Assert.Equal(scripts.Count, Reference.Counts(scripts).Workflows);
        Assert.Equal(scripts.Count, Page(pages, Reference.IndexPath).Split('\n').Count(line => line.Contains(". **", StringComparison.Ordinal)));
        foreach (var page in pages)
        {
            Assert.EndsWith("\n", page.Text);
            Assert.DoesNotContain("\r", page.Text);
            Assert.DoesNotContain("{temp}", page.Text);
            // WF-7.5: nothing of the checks' bookkeeping, and no check.
            Assert.DoesNotContain(page.Text.Split('\n'), line =>
                line.StartsWith("Script:", StringComparison.Ordinal) || line.StartsWith("Needs:", StringComparison.Ordinal)
                || line.StartsWith("Checked:", StringComparison.Ordinal) || line.StartsWith("Through the AI tools:", StringComparison.Ordinal)
                || line.StartsWith("### Check:", StringComparison.Ordinal));
        }

        // Every step and every line of what is heard is a sentence with its end, but typed text, which ends its line.
        var groupPages = pages.Where(page => page.Path.StartsWith(Reference.PagesFolder + "/", StringComparison.Ordinal)).ToList();
        var steps = groupPages
            .SelectMany(page => page.Text.Split('\n'))
            .Where(line => line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith("You ", StringComparison.Ordinal) || (line.Length > 0 && char.IsAsciiDigit(line[0])))
            .Where(line => !line.Contains(" Type: ", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(steps);
        Assert.All(steps, line => Assert.True(line[^1] is '.' or '?' or '!' or '…', line));

        // WF-7.9: nothing of the runner's mechanics, no option of the check, no key by the script's spelling, and no
        // line under a step with fewer than four spaces before it.
        foreach (var page in groupPages)
        {
            Assert.DoesNotContain("Use window", page.Text);
            Assert.DoesNotContain("Take the window", page.Text);
            Assert.DoesNotContain("Launch ", page.Text);
            Assert.DoesNotContain("--model", page.Text.Split('\n').Where(line => line.Contains("Open ax", StringComparison.Ordinal)).DefaultIfEmpty(string.Empty).Aggregate((one, other) => one + "\n" + other));
            Assert.DoesNotContain("Pagedown", page.Text);
            Assert.DoesNotContain("Pageup", page.Text);
            Assert.DoesNotContain("Nvda+", page.Text);
            Assert.DoesNotContain("\n   You", page.Text);
            Assert.DoesNotContain("\n   The", page.Text);
            Assert.DoesNotContain("The tool answers: \"", page.Text);
        }
    }
}
