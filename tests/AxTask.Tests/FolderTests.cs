using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>The folder of lists, the changes with their undo, the tasks and the smart lists, over a temporary folder.</summary>
public sealed class FolderTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 24);

    private readonly TempFolder _top = new("axtask-tests");

    public FolderTests()
    {
        Directory.CreateDirectory(Path.Combine(_top.Path, "Home"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Work", "Nested"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Course plan"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Archive", "Old list"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Archive"));
        Directory.CreateDirectory(Path.Combine(_top.Path, ".hidden"));
        TestLists.List(Path.Combine(_top.Path, "Course plan"), section: " Work ");
        // A plain list: its items have a title and nothing else.
        TestLists.List(Path.Combine(_top.Path, "Plain"), fields: []);
        Item("Home", "Fix the tap", done: false, due: "2026-10-30");
        Item("Home", "Water plants", done: true, due: "2026-10-20");
        Item("Work", "Report", done: false, due: "2026-10-22");
        Item("Course plan", "Week one", done: false, due: null);
        Item(Path.Combine("Work", "Nested"), "Never shown", done: false, due: null);
        Item(Path.Combine("Archive", "Old list"), "Old task", done: false, due: null);
        File.WriteAllText(Path.Combine(_top.Path, "Loose.json"), """{ "schema": "list.json", "Title": "Loose" }""");
        // An item the Archive holds of its own: the Archive has no configuration, and the item is read all the same.
        File.WriteAllText(Path.Combine(_top.Path, "Archive", "Old thing.json"), """{ "schema": "list.json", "Title": "Old thing", "Done": false }""");
        File.WriteAllText(Path.Combine(_top.Path, "Home", "notes.txt"), "not an item");
        File.WriteAllText(Path.Combine(_top.Path, "Home", "broken.json"), "{ nope");
        TestLists.Item(Path.Combine(_top.Path, "Plain"), "Milk");
    }

    public void Dispose() => _top.Dispose();

    [Fact]
    public void ReadsALevel()
    {
        var top = ItemFolder.ReadTop(_top.Path);
        Assert.Equal(["Course plan", "Home", "Plain", "Work"], top.Lists.Select(Path.GetFileName));
        Assert.Equal(["Old list"], top.ArchivedLists.Select(Path.GetFileName));
        Assert.Equal(1, top.ArchivedItems);
        Assert.Equal(1, top.NotShown);
        var home = ItemFolder.Read(Path.Combine(_top.Path, "Home"));
        Assert.Equal(["Fix the tap", "Water plants"], home.Items.Select(ItemFolder.NameOf));
        Assert.False(home.Earlier);
        Assert.Null(home.ConfigProblem);
        // An item is read through its list's configuration: its Title, then the list's fields in the list's order.
        Assert.Equal(["Title", "Done", "Start", "Due"], home.Items[0].Document.Fields.Select(field => field.Title));
        Assert.Equal(FieldTypes.Date, home.Items[0].Document.Fields[3].Type);
        Assert.Null(home.Items[0].ConfigProblem);
        Assert.Empty(home.Items[0].Kept);
        // notes.txt is no item and is counted; broken.json is a damaged item and is named (TK-4.6).
        Assert.Equal(1, home.NotShown);
        Assert.Equal(["broken.json"], home.Unreadable.Select(Path.GetFileName));
        // TK-D4: a list holds items, not lists; the folder inside it is counted, not shown.
        Assert.Equal(1, ItemFolder.Read(Path.Combine(_top.Path, "Work")).NotShown);
        Assert.Equal("Work", ItemFolder.SectionOf(Path.Combine(_top.Path, "Course plan")));
        Assert.Equal(string.Empty, ItemFolder.SectionOf(Path.Combine(_top.Path, "Home")));
    }

    [Fact]
    public void A_damaged_item_is_named_in_its_list_and_counted_with_its_lists_by_what_gathers_items()
    {
        // R-66 (TK-4.6): a due task whose file broke no longer vanishes without a word.
        File.WriteAllText(Path.Combine(_top.Path, "Work", "Report.json"), "{ \"fields\": 5 }");
        File.WriteAllText(Path.Combine(_top.Path, "Work", "Also.json"), "");
        File.WriteAllText(Path.Combine(_top.Path, "Archive", "Old broken.json"), "{");

        var (items, unreadable) = ItemFolder.ReadAll(_top.Path);
        Assert.Equal(ItemFolder.AllItems(_top.Path).Select(item => item.Path), items.Select(item => item.Path));
        Assert.DoesNotContain(items, item => ItemFolder.NameOf(item) == "Report");
        // The Archive is not gathered; the lists come in their order, each file by name.
        Assert.Equal(["broken.json", "Also.json", "Report.json"], unreadable.Select(Path.GetFileName));

        var row = ItemFolder.UnreadableRow(unreadable[0]);
        Assert.StartsWith("broken.json, ", row);
        Assert.Contains("axJSON", row);

        var summary = ItemFolder.UnreadableSummary(_top.Path, unreadable);
        Assert.StartsWith("3 items ", summary);
        Assert.EndsWith(", in Home, Work", summary);
        Assert.StartsWith("1 item ", ItemFolder.UnreadableSummary(_top.Path, [unreadable[0]]));
        Assert.EndsWith(", in Home", ItemFolder.UnreadableSummary(_top.Path, [unreadable[0]]));
        Assert.Equal(string.Empty, ItemFolder.UnreadableSummary(_top.Path, []));
    }

    [Fact]
    public void AllTasksLeavesOutTheDoneThePlainAndTheArchive()
    {
        var tasks = SmartList.AllTasks.Items(_top.Path, Today);
        Assert.Equal(["Report", "Fix the tap", "Week one"], tasks.Select(ItemFolder.NameOf));
    }

    [Fact]
    public void ASmartListFilters()
    {
        File.WriteAllText(Path.Combine(_top.Path, "Work soon.filter.json"), """
            { "format": "axkit-filter 1", "match": "all",
              "conditions": [ { "field": "Done", "is": false }, { "list": "Work" }, { "field": "Due", "before": "+7" } ] }
            """);
        File.WriteAllText(Path.Combine(_top.Path, "Odd.filter.json"), """{ "conditions": [ { "field": "Due", "soon": true } ] }""");
        var lists = SmartList.InFolder(_top.Path);
        Assert.Equal(["Odd", "Work soon"], lists.Select(list => list.Name));
        Assert.NotNull(lists[0].NotUnderstood);
        Assert.Empty(lists[0].Items(_top.Path, Today));
        Assert.Equal(["Report"], lists[1].Items(_top.Path, Today).Select(ItemFolder.NameOf));
        Assert.Equal(1, ItemFolder.ReadTop(_top.Path).NotShown);
    }

    [Fact]
    public void ANewSmartListIsAllTasks()
    {
        var path = Path.Combine(_top.Path, "Mine.filter.json");
        File.WriteAllText(path, SmartList.NewFileText());
        Assert.Equal(SmartList.AllTasks.Items(_top.Path, Today).Select(item => item.Path), SmartList.Read(path).Items(_top.Path, Today).Select(item => item.Path));
    }

    [Fact]
    public void SummariesSayDoneDueAndOverdue()
    {
        var home = ItemFolder.Read(Path.Combine(_top.Path, "Home")).Items;
        Assert.Equal(", due 30 October", TaskRules.Summary(home[0].Document, Today));
        Assert.Equal(", done, due 20 October", TaskRules.Summary(home[1].Document, Today));
        // An item of a plain list is no task and has no summary.
        var milk = ItemFolder.Read(Path.Combine(_top.Path, "Plain")).Items[0];
        Assert.False(TaskRules.IsTask(milk.Document));
        Assert.Equal(string.Empty, TaskRules.Summary(milk.Document, Today));
        var report = ItemFolder.Read(Path.Combine(_top.Path, "Work")).Items[0];
        Assert.Equal(", overdue since 22 October", TaskRules.Summary(report.Document, Today));
    }

    [Theory]
    [InlineData("Fix the tap", "Fix the tap")]
    [InlineData("a/b: c?", "a b c")]
    [InlineData("  ", "Untitled")]
    [InlineData("Ends with.", "Ends with")]
    [InlineData("CON", "_CON")]
    [InlineData("com1", "_com1")]
    [InlineData("nul.txt", "_nul.txt")]
    [InlineData("console", "console")]
    [InlineData(".gitignore rules", "_.gitignore rules")]
    // The one name an item's file never has: its list's configuration's.
    [InlineData("list", "_list")]
    [InlineData("List", "_List")]
    [InlineData("list of things", "list of things")]
    [InlineData("...", "Untitled")]
    public void FileNamesComeFromTheTitle(string title, string stem) => Assert.Equal(stem, ItemFiles.FileStem(title));

    /// <summary>A file whose name starts with a full stop is not shown (IA-4.6): a Title that starts with one got such a file, and the item was gone from its list.</summary>
    [Fact]
    public void An_item_whose_title_starts_with_a_full_stop_is_still_shown()
    {
        var home = Path.Combine(_top.Path, "Home");
        File.WriteAllText(Path.Combine(home, ItemFiles.FileStem(".NET upgrade") + ".json"), """{ "schema": "list.json", "Title": ".NET upgrade" }""");
        Assert.Contains(".NET upgrade", ItemFolder.Read(home).Items.Select(ItemFolder.NameOf));
    }

    /// <summary>
    /// AF-13.2: an item's own folder's <c>list.json</c> is its configuration wherever the folder is, a share or a
    /// mapped drive included (the folder was read to get the item); a link that leaves the folder is followed only
    /// on this computer. Paths alone: nothing is asked of a network here.
    /// </summary>
    [Fact]
    public void An_items_own_folders_configuration_is_read_on_a_share_too_and_a_link_that_leaves_it_is_not()
    {
        const string item = @"\\server\share\Tasks\Home\Fix the tap.json";
        Assert.Equal(@"\\server\share\Tasks\Home\list.json", ItemFolder.ConfigurationFile(item, "list.json"));
        Assert.Equal(@"\\server\share\Tasks\Home\list.json", ItemFolder.ConfigurationFile(item, "./list.json"));
        Assert.Null(ItemFolder.ConfigurationFile(item, "../Shared/list.json"));
        Assert.Null(ItemFolder.ConfigurationFile(item, "//other/share/list.json"));
        Assert.Null(ItemFolder.ConfigurationFile(item, @"C:\elsewhere\list.json"));
        Assert.Null(ItemFolder.ConfigurationFile(item, string.Empty));

        // On this computer a link to another folder's file is followed, as before.
        var local = Path.Combine(_top.Path, "Home", "Fix the tap.json");
        Assert.Equal(Path.Combine(_top.Path, "Course plan", "list.json"), ItemFolder.ConfigurationFile(local, "../Course plan/list.json"));
        // A link between items keeps its rule: never off this computer, the own folder included.
        Assert.Null(ItemFolder.Resolve(item, "Other.json"));
    }

    /// <summary>TK-4.11: a list's history file is the list's own, like its configuration: no item, and not counted as a file not shown.</summary>
    [Fact]
    public void A_lists_history_file_is_not_counted_as_a_file_not_shown()
    {
        var home = Path.Combine(_top.Path, "Home");
        var before = ItemFolder.Read(home).NotShown;
        File.WriteAllText(Path.Combine(home, ItemHistory.FileName), "{}\n");
        Assert.Equal(before, ItemFolder.Read(home).NotShown);
    }

    /// <summary>A name cut at its longest never ends in half of a character of two units (an emoji), which Windows refuses to write.</summary>
    [Fact]
    public void A_long_title_is_never_cut_inside_a_character_of_two_units()
    {
        var stem = ItemFiles.FileStem(new string('a', 119) + "\U0001F600 and more");
        Assert.Equal(new string('a', 119), stem);
        Assert.Equal(120, ItemFiles.FileStem(new string('a', 118) + "\U0001F600 and more").Length);
    }

    [Fact]
    public void ATakenNameGetsANumber()
    {
        var home = Path.Combine(_top.Path, "Home");
        Assert.Equal(Path.Combine(home, "Fix the tap - 2.json"), ItemFiles.FreePath(home, "Fix the tap", ".json"));
        var own = Path.Combine(home, "Fix the tap.json");
        Assert.Equal(own, ItemFiles.FreePath(home, "Fix the tap", ".json", own));
        Assert.Equal("There is a list called home here already", ItemFolder.RefusedListName(_top.Path, "home"));
        Assert.NotNull(ItemFolder.RefusedListName(_top.Path, "a/b"));
        Assert.Null(ItemFolder.RefusedListName(_top.Path, "Garden"));
    }

    [Fact]
    public void LinksArePathsFromTheItem()
    {
        var from = Path.Combine(_top.Path, "Home", "Fix the tap.json");
        var to = Path.Combine(_top.Path, "Work", "Report.json");
        Assert.Equal("../Work/Report.json", ItemFolder.LinkTo(from, to));
        Assert.Equal(to, ItemFolder.Resolve(from, "../Work/Report.json"));
    }

    [Fact]
    public void AStepIsUndoneOnlyWhenNothingChangedIt()
    {
        var path = Path.Combine(_top.Path, "Home", "Fix the tap.json");
        var before = File.ReadAllText(path).ReplaceLineEndings("\r\n");
        var after = TestLists.TextOf(path, document => document.WithTitle("Fix the sink"));
        var moved = Path.Combine(_top.Path, "Home", "Fix the sink.json");
        var step = new Step([FileChange.Write(path, before, after), FileChange.Move(path, moved)], "Renamed");
        Changes.Apply(step.Parts);
        Assert.False(File.Exists(path));
        Assert.Equal("Fix the sink", ItemFolder.ReadItem(moved)!.Document.Title);
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.False(File.Exists(moved));
        Assert.Equal("Fix the tap", ItemFolder.ReadItem(path)!.Document.Title);

        Changes.Apply(step.Parts);
        File.WriteAllText(moved, """{ "schema": "list.json", "Title": "Changed elsewhere" }""");
        Assert.Equal("Fix the sink changed on disk", Changes.CheckUndo(step));
    }

    /// <summary>IA-5.4: a step is whole or not at all. A part that fails takes the parts before it back, so that nothing is left on disk that Ctrl+Z does not know of.</summary>
    [Fact]
    public void AStepThatFailsMidwayLeavesNothingOfItself()
    {
        var path = Path.Combine(_top.Path, "Home", "Fix the tap.json");
        var before = File.ReadAllText(path).ReplaceLineEndings("\r\n");
        var after = TestLists.TextOf(path, document => document.WithTitle("Fix the sink"));
        var made = Path.Combine(_top.Path, "Garden", "list.json");
        var parts = new List<FileChange>
        {
            FileChange.WriteInNewFolder(made, ListConfig.New([]).ToText()),
            FileChange.Write(path, before, after),
            FileChange.Move(path, Path.Combine(_top.Path, "Home", "Fix the sink.json")),
            // Nothing is there to move: the step fails at its last part.
            FileChange.Move(Path.Combine(_top.Path, "Home", "Not there.json"), Path.Combine(_top.Path, "Home", "Elsewhere.json")),
        };

        Assert.ThrowsAny<IOException>(() => Changes.Apply(parts));

        Assert.Equal(before, File.ReadAllText(path).ReplaceLineEndings("\r\n"));
        Assert.False(File.Exists(Path.Combine(_top.Path, "Home", "Fix the sink.json")));
        Assert.False(Directory.Exists(Path.Combine(_top.Path, "Garden")));
    }

    /// <summary>A list renamed in its capitals only is one folder under two spellings: the rename is undone like any other.</summary>
    [Fact]
    public void ARenameThatChangesCapitalsOnlyIsUndone()
    {
        var from = Path.Combine(_top.Path, "Home");
        var to = Path.Combine(_top.Path, "HOME");
        var step = new Step([FileChange.Move(from, to)], "Renamed Home to HOME");
        Changes.Apply(step.Parts);
        Assert.Contains("HOME", Directory.GetDirectories(_top.Path).Select(Path.GetFileName));
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.Contains("Home", Directory.GetDirectories(_top.Path).Select(Path.GetFileName));

        var file = Path.Combine(from, "Fix the tap.json");
        var renamed = new Step([FileChange.Move(file, Path.Combine(from, "fix the tap.json"))], "Renamed");
        Changes.Apply(renamed.Parts);
        Assert.Null(Changes.CheckUndo(renamed));
        Changes.Undo(renamed);
        Assert.Contains("Fix the tap.json", Directory.GetFiles(from).Select(Path.GetFileName));
    }

    /// <summary>
    /// AD-3.3a: an item's file another program wrote in a Windows code page is written as UTF-8 once a value holds a
    /// character that page lacks, never with a question mark in the character's place.
    /// </summary>
    [Fact]
    public void AFileInAWindowsCodePageIsWrittenAsUtf8WhenItCannotHoldTheText()
    {
        var path = Path.Combine(_top.Path, "Home", "Old.json");
        // "Café" with its é as the single byte of the Windows code pages: not UTF-8, so read in the system's page.
        File.WriteAllBytes(path, [.. "{ \"schema\": \"list.json\", \"Title\": \"Caf"u8, 0xE9, .. "\" }"u8]);
        var before = TextDocument.Read(path);
        Assert.NotEqual(65001, before.Encoding.CodePage);

        // What the page holds is written in the page still.
        var same = before.Text.Replace("list.json", "list.json ");
        Changes.Apply([FileChange.Write(path, before.Text, same)]);
        Assert.Equal(before.Encoding.CodePage, TextDocument.Read(path).Encoding.CodePage);

        var with = same.Replace("Caf", "\U0001F600 中 Caf");
        Changes.Apply([FileChange.Write(path, same, with)]);
        var read = TextDocument.Read(path);
        Assert.Equal(65001, read.Encoding.CodePage);
        Assert.Equal(with, read.Text);
    }

    [Fact]
    public void AMadeFileIsRemovedByUndo()
    {
        var path = Path.Combine(_top.Path, "Garden", "Dig.json");
        var step = new Step([FileChange.Write(path, null, FlatItem.New("Dig", ListConfig.New([])).ToText())], "Added Dig");
        Changes.Apply(step.Parts);
        Assert.True(File.Exists(path));
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void AFolderMovesAndComesBack()
    {
        var from = Path.Combine(_top.Path, "Work");
        var to = Path.Combine(_top.Path, "Archive", "Work");
        var step = new Step([FileChange.Move(from, to)], "Archived Work");
        Changes.Apply(step.Parts);
        Assert.True(Directory.Exists(to));
        Assert.Null(Changes.CheckUndo(step));
        Changes.Undo(step);
        Assert.True(Directory.Exists(from));
    }

    [Fact]
    public void TheArchiveIsKnown()
    {
        Assert.True(ItemFolder.IsArchive(_top.Path, Path.Combine(_top.Path, "Archive")));
        Assert.True(ItemFolder.IsArchive(_top.Path, Path.Combine(_top.Path, "archive", "x.json")));
        Assert.False(ItemFolder.IsArchive(_top.Path, Path.Combine(_top.Path, "Archived")));
        Assert.Equal("Work, Course plan", ItemFolder.ListName(_top.Path, Path.Combine(_top.Path, "Work", "Course plan")));
    }

    [Fact]
    public void AChoiceSortsByItsOptionsUnlessTheOrderSaysByText()
    {
        // The options and their order are the list's configuration's; an item holds the option's text alone.
        var ranked = Path.Combine(_top.Path, "Ranked");
        TestLists.List(ranked, [new ItemField("Priority", FieldTypes.Choice, System.Text.Json.Nodes.JsonValue.Create(string.Empty), ["High", "Normal", "Low"])]);
        foreach (var (title, priority) in new[] { ("A", "Low"), ("B", "High"), ("C", ""), ("D", "Normal") })
        {
            TestLists.Item(ranked, title, ("Priority", System.Text.Json.Nodes.JsonValue.Create(priority)));
        }

        string[] Sorted(string order)
        {
            var path = Path.Combine(_top.Path, "Ranked.filter.json");
            File.WriteAllText(path, $$"""{ "conditions": [ { "list": "Ranked" } ], "order": [ {{order}} ] }""");
            return SmartList.Read(path).Items(_top.Path, Today).Select(ItemFolder.NameOf).ToArray();
        }

        Assert.Equal(["B", "D", "A", "C"], Sorted("\"Priority\""));
        Assert.Equal(["B", "D", "A", "C"], Sorted("""{ "field": "Priority", "by": "options" }"""));
        Assert.Equal(["B", "A", "D", "C"], Sorted("""{ "field": "Priority", "by": "text" }"""));
        Assert.Equal(["A", "D", "B", "C"], Sorted("""{ "field": "Priority", "descending": true }"""));
        Assert.Null(SmartList.Read(Path.Combine(_top.Path, "Ranked.filter.json")).NotUnderstood);
        File.WriteAllText(Path.Combine(_top.Path, "Odd.filter.json"), """{ "conditions": [], "order": [ { "field": "Priority", "by": "colour" } ] }""");
        Assert.NotNull(SmartList.Read(Path.Combine(_top.Path, "Odd.filter.json")).NotUnderstood);
    }

    [Fact]
    public void An_item_is_read_through_the_configuration_its_link_names()
    {
        var home = Path.Combine(_top.Path, "Home");
        var tap = Path.Combine(home, "Fix the tap.json");

        // A value the list's configuration does not name is kept, and named in one line (AF-11.3, AF-11.4).
        File.WriteAllText(tap, """{ "schema": "list.json", "Title": "Fix the tap", "Done": false, "Sprint": "November" }""");
        var item = ItemFolder.ReadItem(tap)!;
        Assert.Equal(["Title", "Done", "Start", "Due"], item.Document.Fields.Select(field => field.Title));
        Assert.Equal("1 more field kept: Sprint", item.KeptLine);

        // A change writes what changed and leaves what is kept alone.
        var changed = FlatItem.Parse(item.TextWith(item.Document.WithValue(1, System.Text.Json.Nodes.JsonValue.Create(true))));
        Assert.True(FieldTypes.IsTrue(changed.Value("Done")));
        Assert.Equal("November", changed.Text("Sprint"));
        Assert.False(changed.Has("Due"));

        // The configuration gone: every value as plain text, and why (AF-13.4).
        File.Delete(Path.Combine(home, "list.json"));
        var plain = ItemFolder.ReadItem(tap)!;
        Assert.Equal("Its configuration, list.json, is not there", plain.ConfigProblem);
        Assert.Equal(["Title", "Done", "Sprint"], plain.Document.Fields.Select(field => field.Title));
        Assert.All(plain.Document.Fields, field => Assert.Equal(FieldTypes.Text, field.Type));

        // One that does not read: the same, with the line (AF-11.7); the list says it too.
        File.WriteAllText(Path.Combine(home, "list.json"), "{\n  \"format\": \"axkit-list 2\",\n  \"item fields\": [ }\n}");
        Assert.Equal("Its configuration, list.json, does not read: it is not JSON at line 3", ItemFolder.ReadItem(tap)!.ConfigProblem);
        var level = ItemFolder.Read(home);
        Assert.Equal("The list's configuration does not read: it is not JSON at line 3", level.ConfigProblem);
        Assert.Equal(2, level.Items.Count);
        Assert.Null(ItemFolder.ReadList(home));

        // A link that leads out of the item's reach is followed nowhere.
        File.WriteAllText(tap, """{ "schema": "C:\\elsewhere\\list.json", "Title": "Fix the tap" }""");
        Assert.Equal("Its configuration, C:\\elsewhere\\list.json, is not a file beside it", ItemFolder.ReadItem(tap)!.ConfigProblem);
    }

    [Fact]
    public void A_list_of_the_earlier_format_is_not_read_and_is_named()
    {
        var old = Path.Combine(_top.Path, "Old");
        Directory.CreateDirectory(old);
        File.WriteAllText(Path.Combine(old, "list.json"), """{ "format": "axkit-list 1", "fields": [], "new item": [] }""");
        File.WriteAllText(Path.Combine(old, "Thing.json"), """{ "format": "axkit-item 1", "fields": [ { "title": "Title", "type": "text", "value": "Thing" }, { "title": "Done", "type": "check", "value": false } ] }""");

        var level = ItemFolder.Read(old);
        Assert.True(level.Earlier);
        Assert.Empty(level.Items);
        Assert.Empty(level.Unreadable);
        Assert.True(ItemFolder.IsEarlier(old));
        Assert.Equal([old], ItemFolder.EarlierLists(_top.Path));
        Assert.Equal("1 list in the earlier format is not read: Old. Open one to convert it", ItemFolder.EarlierSummary([old]));
        Assert.StartsWith("2 lists in the earlier format are not read: Old, Home.", ItemFolder.EarlierSummary([old, Path.Combine(_top.Path, "Home")]));
        Assert.Equal(string.Empty, ItemFolder.EarlierSummary([]));
        // Nothing of it is gathered: no task of it, and no word of a file not readable.
        Assert.DoesNotContain(ItemFolder.ReadAll(_top.Path).Items, item => ItemFolder.NameOf(item) == "Thing");
        Assert.DoesNotContain(ItemFolder.ReadAll(_top.Path).Unreadable, path => path.StartsWith(old, StringComparison.OrdinalIgnoreCase));

        // A converted list an earlier version of axKit wrote one item into is of the earlier format again, for that item.
        var home = Path.Combine(_top.Path, "Home");
        File.Copy(Path.Combine(old, "Thing.json"), Path.Combine(home, "Thing.json"));
        Assert.True(ItemFolder.Read(home).Earlier);
    }

    [Fact]
    public void A_list_without_a_configuration_gets_one_from_what_its_items_hold()
    {
        var folder = Path.Combine(_top.Path, "Made elsewhere");
        Directory.CreateDirectory(folder);
        // No items yet: the fields of a new list.
        Assert.Equal(["Done", "Start", "Due"], ItemFolder.StarterConfig(folder).ItemFields.Select(field => field.Title));

        File.WriteAllText(Path.Combine(folder, "A.json"), """{ "schema": "list.json", "Title": "A", "Done": true, "Size": 3 }""");
        File.WriteAllText(Path.Combine(folder, "B.json"), """{ "schema": "list.json", "Title": "B", "Notes": "one\ntwo", "done": false }""");
        File.WriteAllText(Path.Combine(folder, "not an item.json"), "[1]");
        var starter = ItemFolder.StarterConfig(folder);
        Assert.Equal(["Done", "Size", "Notes"], starter.ItemFields.Select(field => field.Title));
        Assert.Equal([FieldTypes.Check, FieldTypes.Number, FieldTypes.LongText], starter.ItemFields.Select(field => field.Type));
        // A new item starts empty, whatever the items held.
        Assert.False(FieldTypes.IsTrue(starter.ItemFields[0].Value));
        Assert.Empty(starter.Actions!);
    }

    private void Item(string list, string title, bool done, string? due)
    {
        var folder = Path.Combine(_top.Path, list);
        if (due is null)
        {
            TestLists.Item(folder, title, ("Done", System.Text.Json.Nodes.JsonValue.Create(done)));
        }
        else
        {
            TestLists.Item(folder, title, ("Done", System.Text.Json.Nodes.JsonValue.Create(done)), ("Due", System.Text.Json.Nodes.JsonValue.Create(due)));
        }
    }
}
