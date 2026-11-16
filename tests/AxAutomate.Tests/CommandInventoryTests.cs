using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The command inventory (CommandInventory, docs/axKit/user-workflows.md WF-2.6 to WF-2.11, format 5.3): the file
/// written in its one order and read back, what never enters it, a snapshot merged in and a run's rewrite, what a live
/// window lists differently named, and a command found in it by its name, its chord or a menu step's text. The rows are
/// axDown's as section 5.2 gives them.
/// </summary>
public sealed class CommandInventoryTests
{
    // The run's folder and the profile folder; neither is an account of this computer.
    private const string Temp = @"D:\Runs\axkit-automate\4242-101500";
    private const string Profile = @"C:\Users\<user>";

    /// <summary>Section 5.3's example, the file written from the rows of <see cref="AxDown"/>.</summary>
    private static readonly string Example = """
        {
          "format": "axkit-window-commands 1",
          "apps": [
            {
              "app": "axDown",
              "commands": [
                {"label": "Redo", "menu": "Actions", "key": "ctrl+y", "also": ["ctrl+shift+z"], "keyText": "Ctrl+Y", "mark": "change", "keyOnly": false, "ai": true},
                {"label": "Undo", "menu": "Actions", "key": "ctrl+z", "also": [], "keyText": "Ctrl+Z", "mark": "change", "keyOnly": false, "ai": true},
                {"label": "Keyboard shortcuts", "menu": "Help", "key": "f1", "also": [], "keyText": "F1", "mark": "read", "keyOnly": false, "ai": true},
                {"label": "Find", "menu": "Navigation", "key": "ctrl+f", "also": [], "keyText": "Ctrl+F", "mark": "read", "keyOnly": false, "ai": true},
                {"label": "Go to line", "menu": "Navigation", "key": "ctrl+g", "also": [], "keyText": "Ctrl+G", "mark": "read", "keyOnly": false, "ai": true},
                {"label": "Heading", "menu": "Navigation, Next", "key": "ctrl+h", "also": [], "keyText": "Ctrl+H", "mark": "read", "keyOnly": false, "ai": true},
                {"label": "Heading", "menu": "Navigation, Previous", "key": "ctrl+shift+h", "also": [], "keyText": "Ctrl+Shift+H", "mark": "read", "keyOnly": false, "ai": true},
                {"label": "Word wrap", "menu": "Options, Text", "key": "ctrl+shift+w", "also": [], "keyText": "Ctrl+Shift+W", "mark": "change", "keyOnly": false, "ai": true},
                {"label": "Between the text and the details", "menu": "", "key": "ctrl+tab", "also": ["ctrl+shift+tab", "f6"], "keyText": "Ctrl+Tab", "mark": "open", "keyOnly": true, "ai": false}
              ]
            }
          ]
        }
        """.ReplaceLineEndings("\n") + "\n";

    private static ListedCommand Row(string label, string menu, string key = "", string keyText = "", string mark = "read", IReadOnlyList<string>? also = null,
        bool? isChecked = null, bool ai = true, string now = "") =>
        new(label, menu, key, also ?? [], keyText, mark, isChecked, KeyOnly: false, ai, now);

    /// <summary>A row of keys with no menu item, as the window lists it: no menus, never for the AI tools.</summary>
    private static ListedCommand KeyAlone(string label, string key, string keyText, params string[] also) =>
        new(label, string.Empty, key, also, keyText, "open", Checked: null, KeyOnly: true, Ai: false, key);

    /// <summary>axDown's window with a file open and the focus in the text, as section 5.2 lists it (its Heading level 2 aside).</summary>
    private static WindowCommands AxDown(string? notice = null) => new("axDown", notice,
    [
        Row("Undo", "Actions", "ctrl+z", "Ctrl+Z", "change", now: "ctrl+z"),
        Row("Redo", "Actions", "ctrl+y", "Ctrl+Y", "change", ["ctrl+shift+z"], now: "ctrl+y"),
        Row("Find", "Navigation", "ctrl+f", "Ctrl+F", now: "ctrl+f"),
        Row("Go to line", "Navigation", "ctrl+g", "Ctrl+G", now: "ctrl+g"),
        Row("Heading", "Navigation, Next", "ctrl+h", "Ctrl+H", now: "ctrl+h"),
        Row("Heading", "Navigation, Previous", "ctrl+shift+h", "Ctrl+Shift+H", now: "ctrl+shift+h"),
        Row("Word wrap", "Options, Text", "ctrl+shift+w", "Ctrl+Shift+W", "change", isChecked: true, now: "ctrl+shift+w"),
        Row("Keyboard shortcuts", "Help", "f1", "F1", now: "f1"),
        KeyAlone("Between the text and the details", "ctrl+tab", "Ctrl+Tab", "ctrl+shift+tab", "f6"),
    ]);

    /// <summary>A snapshot of axDown with one row put in place of its row of that label and menus, or added.</summary>
    private static WindowCommands AxDownWith(ListedCommand changed)
    {
        var rows = AxDown().Commands.ToList();
        var at = rows.FindIndex(row => row.Label.Equals(changed.Label, StringComparison.OrdinalIgnoreCase) && row.Menu == changed.Menu);
        if (at >= 0)
        {
            rows[at] = changed;
        }
        else
        {
            rows.Add(changed);
        }

        return new WindowCommands("axDown", null, rows);
    }

    private static IReadOnlyList<InventoryApp> Inventory(params WindowCommands[] snapshots)
    {
        IReadOnlyList<InventoryApp> apps = [];
        foreach (var snapshot in snapshots)
        {
            apps = CommandInventory.Merge(apps, snapshot, Temp, Profile);
        }

        return apps;
    }

    private static List<string> Labels(IReadOnlyList<InventoryApp> apps, string app) => [.. CommandInventory.Of(apps, app).Select(row => row.Label)];

    [Fact]
    public void The_inventory_reads_back_what_it_wrote_one_row_a_line()
    {
        var text = CommandInventory.Write(Inventory(AxDown()));
        Assert.Equal(Example, text);
        Assert.Equal(9, text.Split('\n').Count(line => line.StartsWith("        {\"label\": ", StringComparison.Ordinal)));
        Assert.Equal(text, CommandInventory.Write(CommandInventory.Read(text)));
        Assert.Equal(text, CommandInventory.Write(CommandInventory.Read(text.Replace("\n", "\r\n", StringComparison.Ordinal))));

        var read = Assert.Single(CommandInventory.Read(text));
        Assert.Equal("axDown", read.App);
        var between = read.Commands[^1];
        Assert.Equal(("ctrl+tab", "Ctrl+Tab", "open", true, false), (between.Key, between.KeyText, between.Mark, between.KeyOnly, between.Ai));
        Assert.Equal(["ctrl+shift+tab", "f6"], between.Also);

        // Two apps, and a label with a quotation mark, a backslash and a character beyond ASCII, written as itself.
        var two = Inventory(AxDown(), new WindowCommands("axJSON", null, [Row("Copy the \"path\" \\ \u00B7", "Edit", "ctrl+shift+c", "Ctrl+Shift+C")]));
        var written = CommandInventory.Write(two);
        Assert.Contains("{\"label\": \"Copy the \\\"path\\\" \\\\ \u00B7\", \"menu\": \"Edit\"", written, StringComparison.Ordinal);
        Assert.Contains("    },\n    {\n      \"app\": \"axJSON\",\n", written, StringComparison.Ordinal);
        Assert.Equal(written, CommandInventory.Write(CommandInventory.Read(written)));
        Assert.Equal("Copy the \"path\" \\ \u00B7", CommandInventory.Of(CommandInventory.Read(written), "axJSON")[0].Label);

        // An empty inventory, and what is no inventory.
        Assert.Equal("{\n  \"format\": \"axkit-window-commands 1\",\n  \"apps\": []\n}\n", CommandInventory.Write([]));
        Assert.Empty(CommandInventory.Read(CommandInventory.Write([])));
        Assert.Empty(CommandInventory.Read(string.Empty));
        Assert.Empty(CommandInventory.Read("not json"));
        Assert.Empty(CommandInventory.Read("[]"));
        Assert.Empty(CommandInventory.Read(text.Replace(CommandInventory.Format, "axkit-window-commands 2", StringComparison.Ordinal)));
    }

    [Fact]
    public void The_apps_and_their_rows_are_in_the_one_order()
    {
        // The apps by name without regard to case: "axfile" is written small here to show it.
        var apps = Inventory(
            new WindowCommands("axGIT", null, [Row("Commit", "Actions", "ctrl+enter", "Ctrl+Enter", "change")]),
            new WindowCommands("axfile", null, [Row("Copy", "Edit", "ctrl+c", "Ctrl+C", "change")]),
            AxDown());
        Assert.Equal(["axDown", "axfile", "axGIT"], apps.Select(app => app.App));

        // An app's rows: the menu rows by menus and then label, without regard to case, rows of one label and menus in the
        // order the window listed them; then the keys with no menu item, by label.
        var task = Inventory(new WindowCommands("axTask", null,
        [
            KeyAlone("Between the list and the details", "f6", "F6"),
            Row("Not set", "Context, Action keys", keyText: "Ctrl+2", mark: "act"),
            Row("Rename", "Context", "f2", "F2", "change"),
            Row("Not set", "Context, Action keys", keyText: "Ctrl+1", mark: "act"),
            KeyAlone("Back", "alt+left", "Alt+Left"),
            Row("copy path", "Context", "ctrl+shift+c", "Ctrl+Shift+C"),
            Row("Add item", "Actions", "ctrl+n", "Ctrl+N", "change"),
        ]));
        var rows = CommandInventory.Of(task, "axTask");
        Assert.Equal(["Add item", "copy path", "Rename", "Not set", "Not set", "Back", "Between the list and the details"], rows.Select(row => row.Label));
        Assert.Equal(["Ctrl+2", "Ctrl+1"], rows.Where(row => row.Label == "Not set").Select(row => row.KeyText));

        // The writer keeps the one order whatever order it is given.
        Assert.Equal(CommandInventory.Write(apps), CommandInventory.Write([.. apps.Reverse()]));
    }

    [Fact]
    public void A_numbered_entry_of_a_list_is_left_out()
    {
        Assert.False(CommandInventory.Kept(Row("1. notes.md - axDown", "File, Recent files", mark: "open"), Temp, Profile));
        Assert.False(CommandInventory.Kept(Row("9. find.md - axDown", "Window, Open windows", mark: "open"), Temp, Profile));
        Assert.False(CommandInventory.Kept(Row("a. tenth.md - axDown", "Window, Open windows", mark: "open"), Temp, Profile));
        Assert.False(CommandInventory.Kept(Row("z. last.md - axDown", "File, Recently closed", mark: "open"), Temp, Profile));
        // As a menu item's text shows it, with its mnemonic.
        Assert.False(CommandInventory.Kept(Row("&2. notes.md - axDown", "File, Recent files", mark: "open"), Temp, Profile));

        // Not of the form: no number, two digits, a decimal, a capital, nothing after the space.
        Assert.True(CommandInventory.Kept(Row("Find", "Navigation"), Temp, Profile));
        Assert.True(CommandInventory.Kept(Row("10. notes.md", "File"), Temp, Profile));
        Assert.True(CommandInventory.Kept(Row("1.5 times", "Playback, Speed"), Temp, Profile));
        Assert.True(CommandInventory.Kept(Row("A. Capital", "Help"), Temp, Profile));
        Assert.True(CommandInventory.Kept(Row("1. ", "Help"), Temp, Profile));

        var apps = Inventory(new WindowCommands("axDown", null,
        [
            Row("Find", "Navigation", "ctrl+f", "Ctrl+F"),
            Row("1. notes.md - axDown", "File, Recent files", mark: "open"),
            Row("b. find.md - axDown", "Window, Open windows", mark: "open"),
        ]));
        Assert.Equal(["Find"], Labels(apps, "axDown"));
        // Nor is one named as missing.
        Assert.Empty(CommandInventory.Stale(apps, new WindowCommands("axDown", null, [Row("2. other.md - axDown", "File, Recent files", mark: "open")]), Temp, Profile));
    }

    [Fact]
    public void A_row_that_names_the_runs_folder_or_the_profile_is_left_out()
    {
        // In the label, with the folder's own separators.
        Assert.False(CommandInventory.Kept(Row(@"Open D:\Runs\axkit-automate\4242-101500\find-0\notes.md", "File"), Temp, Profile));
        // With the other separator and another case.
        Assert.False(CommandInventory.Kept(Row("Open d:/runs/AXKIT-automate/4242-101500/find-0", "File"), Temp, Profile));
        // In the menus, and in the key text.
        Assert.False(CommandInventory.Kept(Row("Documents", @"Go, C:\Users\<user>\Documents"), Temp, Profile));
        Assert.False(CommandInventory.Kept(Row("Shell", "Options", keyText: "c:/users/<USER>/bin"), Temp, Profile));
        // A folder given with a separator at its end.
        Assert.False(CommandInventory.Kept(Row(@"Open D:\Runs\axkit-automate\4242-101500", "File"), Temp + @"\", Profile));

        // Another folder is kept; a folder that only starts like the run's is no such folder's part.
        Assert.True(CommandInventory.Kept(Row(@"Open C:\Windows", "File"), Temp, Profile));
        Assert.True(CommandInventory.Kept(Row(@"Open D:\Runs\axkit", "File"), Temp, Profile));
        // An empty folder leaves out nothing.
        Assert.True(CommandInventory.Kept(Row(@"Open D:\Runs\axkit-automate\4242-101500", "File"), string.Empty, string.Empty));

        var apps = Inventory(new WindowCommands("axFile", null,
        [
            Row("Copy", "Edit", "ctrl+c", "Ctrl+C", "change"),
            Row(@"D:\Runs\axkit-automate\4242-101500\find-0", "Go, Recent folders", mark: "open"),
        ]));
        Assert.Equal(["Copy"], Labels(apps, "axFile"));
    }

    [Fact]
    public void What_is_state_is_not_stored()
    {
        var stored = CommandInventory.Stored(Row("Word wrap", "Options, Text", "ctrl+shift+w", "Ctrl+Shift+W", "change", isChecked: true, now: "ctrl+shift+w"));
        Assert.Null(stored.Checked);
        Assert.Equal(string.Empty, stored.Now);
        Assert.Equal(("Word wrap", "Options, Text", "ctrl+shift+w", "Ctrl+Shift+W", "change", false, true),
            (stored.Label, stored.Menu, stored.Key, stored.KeyText, stored.Mark, stored.KeyOnly, stored.Ai));

        var apps = Inventory(AxDown());
        Assert.All(CommandInventory.Of(apps, "axDown"), row => Assert.True(row.Checked is null && row.Now.Length == 0));
        var text = CommandInventory.Write(apps);
        Assert.DoesNotContain("\"checked\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"now\"", text, StringComparison.Ordinal);

        // A window whose rows differ in their state alone (Word wrap off, no key acting now) is not stale.
        var later = new WindowCommands("axDown", null, [.. AxDown().Commands.Select(row => row with { Checked = row.Checked is null ? null : false, Now = string.Empty })]);
        Assert.Empty(CommandInventory.Stale(apps, later, Temp, Profile));
    }

    [Fact]
    public void A_snapshot_adds_the_rows_the_inventory_lacks()
    {
        var apps = Inventory(new WindowCommands("axDown", null, [Row("Find", "Navigation", "ctrl+f", "Ctrl+F")]));
        apps = CommandInventory.Merge(apps, AxDown(), Temp, Profile);
        Assert.Equal(9, CommandInventory.Of(apps, "axDown").Count);
        Assert.Single(CommandInventory.Of(apps, "axDown"), row => row.Label == "Find");

        // An app not yet in the inventory is added.
        apps = CommandInventory.Merge(apps, new WindowCommands("axJSON", null, [Row("Format the document", "Actions", "ctrl+shift+f", "Ctrl+Shift+F", "change")]), Temp, Profile);
        Assert.Equal(["axDown", "axJSON"], apps.Select(app => app.App));
        Assert.Equal(["Format the document"], Labels(apps, "axJSON"));
    }

    [Fact]
    public void A_changed_key_replaces_its_row()
    {
        var apps = Inventory(AxDown());
        apps = CommandInventory.Merge(apps, AxDownWith(Row("Go to line", "Navigation", "ctrl+shift+g", "Ctrl+Shift+G")), Temp, Profile);
        var rows = CommandInventory.Of(apps, "axDown");
        Assert.Equal(9, rows.Count);
        var go = Assert.Single(rows, row => row.Label == "Go to line");
        Assert.Equal(("ctrl+shift+g", "Ctrl+Shift+G"), (go.Key, go.KeyText));

        // A label whose case changed is the same label and menus: the row is replaced, not doubled.
        apps = CommandInventory.Merge(apps, AxDownWith(Row("Go to Line", "Navigation", "ctrl+shift+g", "Ctrl+Shift+G")), Temp, Profile);
        Assert.Equal("Go to Line", Assert.Single(CommandInventory.Of(apps, "axDown"), row => row.Label.StartsWith("Go to", StringComparison.Ordinal)).Label);
    }

    [Fact]
    public void Rows_of_one_label_in_one_menu_stay_side_by_side()
    {
        // axTask's action keys: one label, one menus, a key each (WF-1.5), shown as text alone (WF-2.1).
        var actionKeys = new WindowCommands("axTask", null,
        [
            Row("Not set", "Context, Action keys", keyText: "Ctrl+1", mark: "act"),
            Row("Not set", "Context, Action keys", keyText: "Ctrl+2", mark: "act"),
            Row("Not set", "Context, Action keys", keyText: "Ctrl+3", mark: "act"),
            Row("Rename", "Context", "f2", "F2", "change"),
        ]);
        var apps = Inventory(actionKeys, actionKeys);
        Assert.Equal(["Ctrl+1", "Ctrl+2", "Ctrl+3"], CommandInventory.Of(apps, "axTask").Where(row => row.Label == "Not set").Select(row => row.KeyText));
        Assert.Empty(CommandInventory.Stale(apps, actionKeys, Temp, Profile));

        // The name cannot tell them apart, and the refusal says so of the rows the indexes count in.
        var resolution = CommandInventory.Resolve(apps, "axTask", "Not set", out var row);
        Assert.Equal(CommandMatch.Ambiguous, resolution.Match);
        Assert.Null(row);
        var names = CommandInventory.Of(apps, "axTask").Select(known => new CommandName(known.Label, known.Menu)).ToList();
        Assert.Equal(["Not set", "Not set", "Not set"], resolution.Candidates.Select(i => names[i].Label));
        Assert.Contains("cannot tell them apart", CommandNames.Refusal(resolution, names), StringComparison.Ordinal);

        // A list that names its third key: the group of that label and menus is the snapshot's, the new label is added.
        apps = CommandInventory.Merge(apps, new WindowCommands("axTask", null,
        [
            Row("Not set", "Context, Action keys", keyText: "Ctrl+1", mark: "act"),
            Row("Not set", "Context, Action keys", keyText: "Ctrl+2", mark: "act"),
            Row("Open the site", "Context, Action keys", keyText: "Ctrl+3", mark: "act"),
        ]), Temp, Profile);
        Assert.Equal(["Rename", "Not set", "Not set", "Open the site"], Labels(apps, "axTask"));
        Assert.Equal(["Ctrl+1", "Ctrl+2", "Ctrl+3"], CommandInventory.Of(apps, "axTask").Skip(1).Select(row => row.KeyText));
    }

    [Fact]
    public void A_row_the_snapshot_does_not_hold_stays_in_a_merge_and_goes_in_a_rewrite()
    {
        var task = new WindowCommands("axTask", null, [Row("Rename", "Context", "f2", "F2", "change")]);
        var apps = Inventory(AxDown(), task);
        // The window no longer has Heading in Previous.
        var seen = new WindowCommands("axDown", null, [.. AxDown().Commands.Where(row => row.Menu != "Navigation, Previous")]);

        var merged = CommandInventory.Merge(apps, seen, Temp, Profile);
        Assert.Single(CommandInventory.Of(merged, "axDown"), row => row.Menu == "Navigation, Previous");

        var rewritten = CommandInventory.Rewritten(apps, [seen], new HashSet<string> { "axDown" }, Temp, Profile);
        Assert.DoesNotContain(CommandInventory.Of(rewritten, "axDown"), row => row.Menu == "Navigation, Previous");
        Assert.Equal(8, CommandInventory.Of(rewritten, "axDown").Count);
        // An app that is not whole keeps its rows; what the run saw of it is merged.
        Assert.Equal(["Rename"], Labels(rewritten, "axTask"));
        var notWhole = CommandInventory.Rewritten(apps, [seen], new HashSet<string>(), Temp, Profile);
        Assert.Equal(CommandInventory.Write(merged), CommandInventory.Write(notWhole));

        // A whole app's rows are those of every snapshot the run took of it, together.
        var context = new WindowCommands("axDown", null, [Row("Copy", "Context", "ctrl+c", "Ctrl+C", "change")]);
        var both = CommandInventory.Rewritten(apps, [seen, context], new HashSet<string> { "axDown" }, Temp, Profile);
        Assert.Equal(9, CommandInventory.Of(both, "axDown").Count);
        Assert.Single(CommandInventory.Of(both, "axDown"), row => row.Label == "Copy");
    }

    [Fact]
    public void A_live_row_the_inventory_lacks_is_named()
    {
        var apps = Inventory(AxDown());
        var level = Row("Heading level 2", "Navigation, Next", "ctrl+2", "Ctrl+2", now: "ctrl+2");
        Assert.Equal(["axDown: Heading level 2, Navigation, Next is not in the inventory"],
            CommandInventory.Stale(apps, AxDownWith(level), Temp, Profile));

        // A key with no menu item is named by its label; a row listed twice is named once.
        var twice = new WindowCommands("axDown", null, [.. AxDown().Commands, KeyAlone("Next pane", "f6", "F6"), KeyAlone("Next pane", "f6", "F6")]);
        Assert.Equal(["axDown: Next pane is not in the inventory"], CommandInventory.Stale(apps, twice, Temp, Profile));

        Assert.Empty(CommandInventory.Stale(apps, AxDown(), Temp, Profile));
    }

    [Fact]
    public void A_live_row_the_inventory_holds_differently_is_named_with_the_field()
    {
        var apps = Inventory(AxDown());
        string Stale(ListedCommand changed) => Assert.Single(CommandInventory.Stale(apps, AxDownWith(changed), Temp, Profile));

        // The first field that differs, and that one alone: here the key, though the key text differs too.
        Assert.Equal("axDown: Go to line, Navigation has key ctrl+shift+g; the inventory has ctrl+g",
            Stale(Row("Go to line", "Navigation", "ctrl+shift+g", "Ctrl+Shift+G")));
        Assert.Equal("axDown: Go to line, Navigation has key none; the inventory has ctrl+g", Stale(Row("Go to line", "Navigation", keyText: "Ctrl+G")));
        Assert.Equal("axDown: Redo, Actions has also none; the inventory has ctrl+shift+z", Stale(Row("Redo", "Actions", "ctrl+y", "Ctrl+Y", "change")));
        Assert.Equal("axDown: Go to line, Navigation has keyText \"Ctrl+Shift+G\"; the inventory has \"Ctrl+G\"",
            Stale(Row("Go to line", "Navigation", "ctrl+g", "Ctrl+Shift+G")));
        Assert.Equal("axDown: Find, Navigation has mark \"change\"; the inventory has \"read\"", Stale(Row("Find", "Navigation", "ctrl+f", "Ctrl+F", "change")));
        Assert.Equal("axDown: Find, Navigation has ai false; the inventory has true", Stale(Row("Find", "Navigation", "ctrl+f", "Ctrl+F", ai: false)));
        Assert.Equal("axDown: Go to Line, Navigation has label \"Go to Line\"; the inventory has \"Go to line\"",
            Stale(Row("Go to Line", "Navigation", "ctrl+g", "Ctrl+G")));
        Assert.Equal("axDown: Between the text and the details has also ctrl+shift+tab; the inventory has ctrl+shift+tab, f6",
            Stale(KeyAlone("Between the text and the details", "ctrl+tab", "Ctrl+Tab", "ctrl+shift+tab")));
    }

    [Fact]
    public void A_name_resolves_among_an_apps_rows_and_in_some_app_for_an_unknown_app()
    {
        var apps = Inventory(
            AxDown(),
            new WindowCommands("axJSON", null,
            [
                Row("Find", "Navigation", "ctrl+f", "Ctrl+F"),
                Row("Format the document", "Actions", "ctrl+shift+f", "Ctrl+Shift+F", "change"),
            ]),
            new WindowCommands("axFile", null, [Row("Find", "Navigation", "ctrl+f", "Ctrl+F")]));

        var resolution = CommandInventory.Resolve(apps, "axDown", "find", out var row);
        Assert.Equal(CommandMatch.Found, resolution.Match);
        Assert.Equal(("Find", "Navigation"), (row!.Label, row.Menu));
        Assert.Same(row, CommandInventory.Of(apps, "axDown")[resolution.Index]);

        // Two commands of the name in the app: ambiguous, the whole line found.
        resolution = CommandInventory.Resolve(apps, "axDown", "Heading", out row);
        Assert.Equal(CommandMatch.Ambiguous, resolution.Match);
        Assert.Null(row);
        Assert.Equal(["Navigation, Next", "Navigation, Previous"], resolution.Candidates.Select(i => CommandInventory.Of(apps, "axDown")[i].Menu));
        CommandInventory.Resolve(apps, "axDown", "Heading, Navigation, Next", out row);
        Assert.Equal("ctrl+h", row!.Key);
        Assert.Equal(CommandMatch.NoName, CommandInventory.Resolve(apps, "axDown", " ", out _).Match);
        // A command of another app, and an app without rows.
        Assert.Equal(CommandMatch.Unknown, CommandInventory.Resolve(apps, "axDown", "Format the document", out _).Match);
        Assert.Equal(CommandMatch.Unknown, CommandInventory.Resolve(apps, "axCalc", "Find", out _).Match);

        // For an unknown app, app by app: Find, which three apps have, is found, though among the union of their rows it is ambiguous.
        Assert.Equal(CommandMatch.Found, CommandInventory.Resolve(apps, null, "Find", out row).Match);
        Assert.Equal("Find", row!.Label);
        var union = apps.SelectMany(app => app.Commands).Select(known => new CommandName(known.Label, known.Menu)).ToList();
        Assert.Equal(CommandMatch.Ambiguous, CommandNames.Resolve(union, "Find").Match);
        CommandInventory.Resolve(apps, null, "Format the document", out row);
        Assert.Equal("ctrl+shift+f", row!.Key);
        // A name no app finds exactly once is not found, and names no candidates.
        resolution = CommandInventory.Resolve(apps, null, "Heading", out row);
        Assert.Equal((CommandMatch.Unknown, -1), (resolution.Match, resolution.Index));
        Assert.Empty(resolution.Candidates);
        Assert.Null(row);
        Assert.Equal(CommandMatch.Unknown, CommandInventory.Resolve(apps, null, "Nothing of the kind", out _).Match);
        Assert.Equal(CommandMatch.NoName, CommandInventory.Resolve(apps, null, string.Empty, out _).Match);
    }

    [Fact]
    public void A_snapshot_under_a_notice_merges_its_rows_of_keys_alone()
    {
        // While a notice shows, the bar is disabled: what is listed is the keys, each with no menu row to fold into.
        var underNotice = new WindowCommands("axDown", "Turn on AI tools?",
        [
            Row("Find", "Navigation", "ctrl+shift+f", "Ctrl+Shift+F"),
            KeyAlone("Redo", "ctrl+shift+z", "Ctrl+Shift+Z"),
            KeyAlone("Between the text and the details", "ctrl+tab", "Ctrl+Tab", "ctrl+shift+tab", "f6"),
        ]);
        var fresh = Inventory(underNotice);
        Assert.Equal(["Between the text and the details", "Redo"], Labels(fresh, "axDown"));
        Assert.All(CommandInventory.Of(fresh, "axDown"), row => Assert.True(row.KeyOnly));

        var apps = CommandInventory.Merge(Inventory(AxDown()), underNotice, Temp, Profile);
        Assert.Equal("ctrl+f", Assert.Single(CommandInventory.Of(apps, "axDown"), row => row.Label == "Find").Key);
        Assert.Equal(2, CommandInventory.Of(apps, "axDown").Count(row => row.Label == "Redo"));

        // Checked the same way: the menu row is not, a key alone that differs is named.
        var changed = underNotice with { Commands = [.. underNotice.Commands.Take(2), KeyAlone("Between the text and the details", "ctrl+tab", "Ctrl+Tab", "f6")] };
        Assert.Equal(["axDown: Between the text and the details has also f6; the inventory has ctrl+shift+tab, f6"],
            CommandInventory.Stale(apps, changed, Temp, Profile));
    }

    [Fact]
    public void An_app_without_rows_is_not_held_and_is_never_stale()
    {
        var apps = Inventory(AxDown());
        Assert.True(CommandInventory.Holds(apps, "axDown"));
        Assert.True(CommandInventory.Holds(apps, "AXDOWN"));
        Assert.False(CommandInventory.Holds(apps, "axClaude"));
        Assert.Empty(CommandInventory.Of(apps, "axClaude"));
        Assert.Empty(CommandInventory.Stale(apps, new WindowCommands("axClaude", null, [Row("Send message", "Actions", mark: "act")]), Temp, Profile));
        Assert.Empty(CommandInventory.Stale([], AxDown(), Temp, Profile));

        // A snapshot of which nothing is kept adds no app.
        var none = CommandInventory.Merge(apps, new WindowCommands("axHelp", null, [Row("1. notes.md - axDown", "Window, Open windows", mark: "open")]), Temp, Profile);
        Assert.Equal(["axDown"], none.Select(app => app.App));
        Assert.False(CommandInventory.Holds(none, "axHelp"));

        // An app given without rows is not written, and so not read back.
        Assert.Equal(CommandInventory.Write([]), CommandInventory.Write([new InventoryApp("axClaude", [])]));
    }

    [Fact]
    public void A_menu_steps_text_names_its_row_only_when_one_row_answers_it()
    {
        var apps = Inventory(new WindowCommands("axDown", null,
        [
            .. AxDown().Commands,
            Row("Find next", "Navigation", "f3", "F3"),
            Row("Find previous", "Navigation", "shift+f3", "Shift+F3"),
            Row("Larger text", "Options, Text", keyText: "Ctrl+Plus", mark: "change"),
        ]));

        Assert.Equal("Larger text", CommandInventory.OfMenuText(apps, "axDown", "Larger")?.Label);
        Assert.Equal("Larger text", CommandInventory.OfMenuText(apps, "axDown", " larger text ")?.Label);
        Assert.Equal("Larger text", CommandInventory.OfMenuText(apps, "axDown", "Larger text, Options, T")?.Label);
        Assert.Equal("Navigation, Next", CommandInventory.OfMenuText(apps, "axDown", "Heading, Navigation, N")?.Menu);
        Assert.Equal("Go to line", CommandInventory.OfMenuText(apps, "axDown", "Go to")?.Label);
        Assert.Equal("Find next", CommandInventory.OfMenuText(apps, "axDown", "Find n")?.Label);

        // Several rows answer: Find, Find next and Find previous; the two Headings.
        Assert.Null(CommandInventory.OfMenuText(apps, "axDown", "Find"));
        Assert.Null(CommandInventory.OfMenuText(apps, "axDown", "Heading"));
        // A key with no menu item is no menu item; no row; no app known; no text.
        Assert.Null(CommandInventory.OfMenuText(apps, "axDown", "Between"));
        Assert.Null(CommandInventory.OfMenuText(apps, "axDown", "Nothing of the kind"));
        Assert.Null(CommandInventory.OfMenuText(apps, null, "Larger"));
        Assert.Null(CommandInventory.OfMenuText(apps, "axDown", " "));
    }

    [Fact]
    public void A_row_of_keys_alone_is_passed_over_when_a_menu_row_has_its_label()
    {
        // Redo's second key, listed as a key alone while a notice showed and the menu row was not offered.
        var apps = CommandInventory.Merge(Inventory(AxDown()), new WindowCommands("axDown", "Turn on AI tools?", [KeyAlone("Redo", "ctrl+shift+z", "Ctrl+Shift+Z")]), Temp, Profile);
        Assert.Equal(2, CommandInventory.Of(apps, "axDown").Count(row => row.Label == "Redo"));

        var resolution = CommandInventory.Resolve(apps, "axDown", "Redo", out var row);
        Assert.Equal(CommandMatch.Found, resolution.Match);
        Assert.False(row!.KeyOnly);
        Assert.Same(row, CommandInventory.Of(apps, "axDown")[resolution.Index]);
        CommandInventory.Resolve(apps, null, "Redo", out row);
        Assert.Equal("Actions", row!.Menu);
        Assert.Equal("Actions", CommandInventory.OfChord(apps, "axDown", "ctrl+shift+z")?.Menu);

        // A key alone that no menu row shares its label with is found as itself.
        CommandInventory.Resolve(apps, "axDown", "Between the text and the details", out row);
        Assert.True(row!.KeyOnly);
    }

    [Fact]
    public void A_chord_names_its_command_only_when_one_row_holds_it()
    {
        var apps = Inventory(AxDown(), new WindowCommands("axFile", null,
        [
            Row("Copy", "Edit", "ctrl+c", "Ctrl+C", "change"),
            Row("Copy", "Context", "ctrl+c", "Ctrl+C", "change"),
        ]));

        Assert.Equal("Go to line", CommandInventory.OfChord(apps, "axDown", "ctrl+g")?.Label);
        // Among the further chords, of a menu row and of a key alone.
        Assert.Equal("Redo", CommandInventory.OfChord(apps, "axDown", "ctrl+shift+z")?.Label);
        Assert.Equal("Between the text and the details", CommandInventory.OfChord(apps, "axDown", "f6")?.Label);
        // Written another way, put in the one order and case first.
        Assert.Equal("Go to line", CommandInventory.OfChord(apps, "axDown", "Ctrl+G")?.Label);
        Assert.Equal("Navigation, Previous", CommandInventory.OfChord(apps, "axDown", "shift+ctrl+h")?.Menu);

        // Two rows hold it; no row; another app's; no app known; no chord.
        Assert.Null(CommandInventory.OfChord(apps, "axFile", "ctrl+c"));
        Assert.Null(CommandInventory.OfChord(apps, "axDown", "ctrl+q"));
        Assert.Null(CommandInventory.OfChord(apps, "axFile", "ctrl+g"));
        Assert.Null(CommandInventory.OfChord(apps, null, "ctrl+g"));
        Assert.Null(CommandInventory.OfChord(apps, "axDown", string.Empty));
    }
}
