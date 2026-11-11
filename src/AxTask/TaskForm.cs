using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// axTask's window (docs/axKit/item-apps.md IA-2), on the item apps' shared window <see cref="ItemForm"/>, which holds
/// the panes, the moves between levels, the shared keys and every field command. This class holds what is axTask's:
/// the levels of a folder of lists (the top with All tasks and the smart lists, a list, a smart list's items), what
/// the detail pane shows for each, its menus and its own keys. The commands and the one way a file changes are in
/// <c>TaskForm.Commands.cs</c>.
/// </summary>
public sealed partial class TaskForm : ItemForm
{
    /// <summary>What the detail pane's document is saved to: a file and the text it was read from (null when it does not exist yet, a list's first <c>list.json</c>).</summary>
    private sealed record FileSource(string Path, string? Text);

    private readonly TaskSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _itemMenu = new("&Item");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly ToolStripMenuItem _hideDoneItem;

    /// <summary>The level shown: a folder (the top or a list), or a smart list's items.</summary>
    private string _folder = string.Empty;

    private SmartList? _smart;

    public TaskForm(string? path, TaskSettings settings, string? settingsError)
        : base(AxTaskApp.Name, settings, TaskSettings.DefaultPath, settingsError)
    {
        _settings = settings;

        // ---- menus and keys: one command list (docs/axKit/app-kit.md AK-2), each key from the tables (TK-7) ----
        // The shared keys (ItemKeys) are ItemForm's commands, registered after these by PlaceControls; the items here
        // for them only show them. A key in a second menu is an item showing it too: its command is the first menu's.
        // The first menu holds what every pane has; List and Item act on the navigator's selection, Field on the
        // detail pane's, and only the menu of where the focus is and what is selected is enabled (UpdateRowMenus).
        var file = new ToolStripMenuItem("&Session");
        file.DropDownItems.Add(new ToolStripMenuItem("Top &folder...", null, (_, _) => ChooseTopFolder()));
        Add(file, new Command("&All tasks", AxTaskKeys.AllTasks, ShowAllTasks) { When = NotRenaming, Mark = CommandMark.Open });
        // TK-7b.4: the search across every list, the user's own and every project's.
        Add(file, new Command("Find in &every list...", AxTaskKeys.FindEverywhere, FindEverywhere) { When = NotRenaming, Mark = CommandMark.Read });
        file.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), ItemKeys.Refresh));
        file.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open));
        _listMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _listMenu.DropDownItems.Add(Item("New &list...", NewList, ItemKeys.NewList));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("New &smart list...", null, (_, _) => NewSmartList()));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("New Git&Hub list...", null, (_, _) => NewGitHubList()));
        _listMenu.DropDownItems.Add(Item("&Rename", RenameSelected, ItemKeys.Rename));
        _listMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        Add(_listMenu, new Command("Move to s&ection...", AxTaskKeys.MoveToSection, MoveToSection) { When = InNavigator, Mark = CommandMark.Change });
        _listMenu.DropDownItems.Add(Item("Pre&vious section", () => JumpSection(-1), ItemKeys.PreviousSection));
        _listMenu.DropDownItems.Add(Item("Ne&xt section", () => JumpSection(1), ItemKeys.NextSection));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Add &Done to every item", null, (_, _) => SwitchList(done: true)));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Remove Do&ne from every item", null, (_, _) => SwitchList(done: false)));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        // The action keys and the GitHub account (docs/axKit/reports.md RP-7.2, RP-8.2; TaskForm.GitHub.cs).
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Action &keys...", null, (_, _) => EditActionKeys()));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("GitHub &account...", null, (_, _) => SetGitHubAccount()));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Forget GitHub acco&unt", null, (_, _) => ForgetGitHubAccount()));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        Add(_listMenu, new Command("Open in ax&JSON", AxTaskKeys.OpenInAxJson, OpenInAxJson) { When = NotRenaming, Mark = CommandMark.Open });
        Add(_listMenu, new Command("Show in ax&File", AxTaskKeys.ShowInAxFile, ShowInAxFile) { When = NotRenaming, Mark = CommandMark.Open });

        _itemMenu.DropDownItems.Add(Item("&Open its fields", OpenSelected, ItemKeys.Open));
        _itemMenu.DropDownItems.Add(Item("&Done or not done", () => CheckInNavigator(SelectedRow()), ItemKeys.Check));
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _itemMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _itemMenu.DropDownItems.Add(Item("Rena&me", RenameSelected, ItemKeys.Rename));
        _itemMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        _itemMenu.DropDownItems.Add(new ToolStripMenuItem("Use these fields for &new items", null, (_, _) => UseAsTemplate()));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        // TK-3a.2, TK-D14: Ctrl+1 and Ctrl+2 go by heading at a top level, where no list's action keys act; registered
        // before the action keys, so that the first command whose key and place match runs.
        AddKey(new Command("Next heading of level 1", AxTaskKeys.NextHeading1, () => JumpHeading(1, 1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Next heading of level 2", AxTaskKeys.NextHeading2, () => JumpHeading(2, 1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Previous heading of level 1", AxTaskKeys.PreviousHeading1, () => JumpHeading(1, -1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Previous heading of level 2", AxTaskKeys.PreviousHeading2, () => JumpHeading(2, -1)) { When = AtHeadings, Mark = CommandMark.Read });
        // Ctrl+1 to Ctrl+9, the list's own actions, and the GitHub actions of a GitHub list (RP-7.2, RP-8; TaskForm.GitHub.cs).
        PlaceGitHubMenus();
        _itemMenu.DropDownItems.Add(_actionKeysMenu);
        _itemMenu.DropDownItems.Add(_gitHubMenu);
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, ItemKeys.Open));
        _fieldMenu.DropDownItems.Add(Item("&Done or not done", CheckField, ItemKeys.Check));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _fieldMenu.DropDownItems.Add(Item("&New field...", NewField, ItemKeys.New));
        _fieldMenu.DropDownItems.Add(Item("Ren&ame field", RenameField, ItemKeys.Rename));
        _fieldMenu.DropDownItems.Add(Item("Re&move field", RemoveField, ItemKeys.Delete));
        _fieldMenu.DropDownItems.Add(Item("Move &up", () => MoveField(-1), ItemKeys.MoveFieldUp));
        _fieldMenu.DropDownItems.Add(Item("Move do&wn", () => MoveField(1), ItemKeys.MoveFieldDown));

        var view = new ToolStripMenuItem("&View");
        _hideDoneItem = Add(view, new Command("&Hide done tasks", AxTaskKeys.HideDone, ToggleHideDone) { When = NotRenaming, Mark = CommandMark.Change });
        _hideDoneItem.Checked = settings.HideDone;

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateProjectFoldersMenu());
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Item &resources", [("&JSON", "https://www.json.org/json-en.html")]);

        _menu.Items.AddRange([file, _listMenu, _itemMenu, _fieldMenu, view, options, CreateWindowMenu(), CreateAxKitMenu(), help]);
        SelectionMenus(_listMenu, _itemMenu, _fieldMenu);
        // TK-7b.4: in the search's level, Right and Enter open what was found where it lives, Escape goes back; before
        // the shared keys, which PlaceControls registers after these.
        AddKey(new Command("Open it where it lives", ItemKeys.Into, OpenFound) { When = InSearch, Mark = CommandMark.Open });
        AddKey(new Command("Open it where it lives", ItemKeys.Open, OpenFound) { When = InSearch, Mark = CommandMark.Open });
        AddKey(new Command("Back from the search", ItemKeys.Back, UpLevel) { When = InSearch, Mark = CommandMark.Open });
        PlaceControls(_menu);
        Start(path);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;


    protected override IEnumerable<string> DiagnosticsLines =>
        [$"Level: {(_search is not null ? "search" : _smart is not null ? "smart list" : AtTop ? "top" : "list")}{(_project is null ? string.Empty : ", in a project")}, {Rows.Count} rows, {_settings.ProjectFolders.Count} project folders"];

    /// <summary>The folder of lists, for Recently closed (AX-9.4): the user's own, never a project's.</summary>
    protected override string? PathInHand => _settings.TopFolder;


    /// <summary>A link reads the item it points to, or "missing: name" (IA-D3).</summary>
    protected override string SpokenValue(ItemRow? row, ItemField field) =>
        FieldTypes.Spoken(field, link => ShownDetail.Source is FileSource source ? Links.Spoken(source.Path, link) : link);

    protected override void BeforeRefresh()
    {
        if (_smart?.Path is { } filter && File.Exists(filter))
        {
            _smart = SmartList.Read(filter);
        }

        // F5 in a GitHub list reads GitHub again (TK-7a.2); the panes fill from the files meanwhile.
        RefreshGitHubAgain();

        // F5 on the top level looks for the projects again (TK-7b.2).
        if (_search is null && _project is null && AtTop)
        {
            FindProjects();
        }
    }

    // ---- the levels (IA-2.2) ----

    /// <summary>The folder of lists shown: the user's own, or the project entered (TK-7b.3), which is a top folder as the user's is.</summary>
    private string TopFolder => _project ?? _settings.TopFolder;

    private bool AtTop => _smart is null && _folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>A folder opens as the top; an item's file opens its list with the item selected; nothing opens the last run's list.</summary>
    private void Start(string? path)
    {
        string? select = null;
        if (path is not null && Directory.Exists(path))
        {
            _settings.Folder = Path.GetFullPath(path);
            _folder = TopFolder;
        }
        else if (path is not null && File.Exists(path))
        {
            var full = Path.GetFullPath(path);
            var folder = Path.GetDirectoryName(full)!;
            if (!folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) && !folder.StartsWith(TopFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Folder = folder;
            }

            _folder = folder;
            select = full;
        }
        else
        {
            _folder = _settings.LastList is { } last && Directory.Exists(Path.Combine(TopFolder, last)) ? Path.Combine(TopFolder, last) : TopFolder;
        }

        SaveSettings();
        Fill(select);
        FindProjects();
    }

    protected override void EnterList(ItemRow row)
    {
        if (row.Tag is ProjectTag { Project: var project })
        {
            // TK-7b.3: a project is a top folder of its own while it is shown.
            _project = project.Path;
            _projectName = project.Name;
            _smart = null;
            _folder = project.Path;
            return;
        }

        if (row.Tag is SmartList smart)
        {
            _smart = smart;
        }
        else
        {
            _smart = null;
            _folder = row.Key;
        }

        RememberLevel();
        // A GitHub list is read from GitHub as it opens (TK-7a.2); its files show at once, the issues follow.
        RefreshGitHubOnEnter();
    }

    protected override string? LeaveList()
    {
        if (_search is { } search)
        {
            return LeaveSearch(search);
        }

        if (AtTop)
        {
            if (_project is not { } project)
            {
                return null;
            }

            // TK-7b.3: out of a project's top, back to the user's own top level on the project's row; looked at again,
            // since what was changed inside it counts in the smart lists here (TK-5.6).
            _project = null;
            _projectName = null;
            _folder = TopFolder;
            FindProjects();
            return project;
        }

        string select;
        if (_smart is not null)
        {
            select = _smart.Key;
            _smart = null;
            _folder = TopFolder;
        }
        else
        {
            // One level of lists (TK-D4): out of any list straight to the sections.
            select = _folder;
            _folder = TopFolder;
        }

        RememberLevel();
        return select;
    }

    /// <summary>Ctrl+1: All tasks from anywhere.</summary>
    private void ShowAllTasks()
    {
        // In a project, its own All tasks (TK-7b.3); from the search, the user's own.
        _search = null;
        _smart = SmartList.AllTasks;
        _folder = TopFolder;
        Fill(select: null);
        Navigator.Select();
        AnnounceLevel();
    }

    /// <summary>
    /// The list open last, kept for the next start: a project's level is never remembered, and while a project is
    /// shown the user's own place stays as it was (TK-7b.3).
    /// </summary>
    private void RememberLevel()
    {
        if (_project is not null)
        {
            return;
        }

        _settings.LastList = _project is null && _smart is null && !AtTop && _folder.StartsWith(TopFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(TopFolder, _folder)
            : null;
        SaveSettings();
    }

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        var today = Today;
        var rows = new List<ItemRow>();
        string name;
        if (_project is not null && !Directory.Exists(_project))
        {
            // A project gone from disk: back to the user's own top level.
            _project = null;
            _projectName = null;
            _folder = TopFolder;
            _smart = null;
        }

        if (!Directory.Exists(_folder) && !_folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase))
        {
            // A list gone from disk: back to the top.
            _folder = TopFolder;
            _smart = null;
        }

        if (_search is { } search)
        {
            name = ReadSearch(search, rows);
        }
        else if (_smart is { } smart)
        {
            // TK-5.6: the user's own items and every project's together, each saying where it lives; a change is made
            // to its own file, as a found item's is.
            var items = smart.Items(GatheredItems(), today);
            foreach (var gathered in items)
            {
                var item = gathered.Item;
                var itemName = ItemFolder.NameOf(item);
                rows.Add(new ItemRow(false, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today) + ", in " + gathered.Where, item));
            }

            name = smart.NotUnderstood is { } why ? $"{smart.Name}, a condition not understood: {why}" : $"{smart.Name}, {Count(items.Count, "item")}";
        }
        else if (AtTop)
        {
            name = ReadSections(rows, today);
        }
        else
        {
            // A list's level: its items only (TK-D4).
            var level = ItemFolder.Read(_folder);
            var hidden = 0;
            foreach (var item in level.Items)
            {
                if (_settings.HideDone && TaskRules.IsDone(item.Document))
                {
                    hidden++;
                    continue;
                }

                var itemName = ItemFolder.NameOf(item);
                rows.Add(new ItemRow(false, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today), item));
            }

            name = ListTitle() + ", " + (rows.Count == 0 && hidden == 0 ? "empty: Ctrl+N adds an item" : Count(rows.Count, "item"));
            if (hidden > 0)
            {
                name += $", {hidden} done hidden";
            }

            if (level.NotShown > 0)
            {
                name += $", {Count(level.NotShown, "file")} not shown";
            }
        }

        var title = _search is not null ? "Found" : _smart?.Name ?? (AtTop ? _projectName ?? "Lists" : ListTitle());
        return (rows, name, title, TopFolder);
    }

    /// <summary>"Home"; the Archive's own items are "Archived items".</summary>
    private string ListTitle() => IsArchiveFolder(_folder) ? ArchivedItemsName : ItemFolder.ListName(TopFolder, _folder);

    private const string ArchivedItemsName = "Archived items";

    /// <summary>
    /// A heading row of the top level (TK-3a.1): its name, whether it is a section of the user's own, and its level, 1
    /// or 2 (TK-D13).
    /// </summary>
    private sealed record SectionTag(string Name, bool Custom, int Level);

    /// <summary>The Tag of the "Archived items" row, which opens the Archive's own items.</summary>
    private static readonly object ArchivedItemsTag = new();

    private const string YourLists = "Your lists";

    /// <summary>
    /// The items the smart lists and the date lists gather (TK-5.6): the folder shown as the top read now, and, at the
    /// user's own top, every project's as the last look read them off the UI thread (<see cref="_projectItems"/>).
    /// </summary>
    private IReadOnlyList<GatheredItem> GatheredItems() =>
    [
        .. ItemFolder.AllItems(TopFolder).Select(item => new GatheredItem(item, TopFolder, string.Empty)),
        .. _project is null ? _projectItems ?? [] : [],
    ];

    /// <summary>
    /// The top level under two levels of headings (TK-3a.1, asked 2026-11-11; TK-D13): Your lists, with a heading of
    /// level 2 for each section of the user's own; Projects, with one for each project folder (TK-7b.3); Smart lists,
    /// All tasks and the user's filters, with Mentions and Hashtags (TK-5.5); Date lists (TK-5.4); Archived lists. The
    /// navigator's name is returned.
    /// </summary>
    private string ReadSections(List<ItemRow> rows, DateOnly today)
    {
        var top = ItemFolder.ReadTop(TopFolder);
        var all = GatheredItems();
        AddListSections(rows, top);
        if (_project is null && _settings.ProjectFolders.Count > 0)
        {
            // TK-7b.3: the projects gathered, a heading of level 2 for each project folder, after the user's own lists.
            AddHeading(rows, "Projects", 1, Count(_settings.ProjectFolders.Count, "project folder"));
            AddProjectSections(rows);
        }

        List<SmartList> smart = [SmartList.AllTasks, .. SmartList.InFolder(TopFolder)];
        AddHeading(rows, "Smart lists", 1, Count(smart.Count, "smart list"));
        rows.AddRange(smart.Select(list => SmartRow(list, all, today)));

        // TK-5.5: a smart list for each mention and each hashtag in the tasks not done; no heading when there are none.
        var tasks = all.Select(gathered => gathered.Item).ToList();
        foreach (var (heading, mark) in new[] { ("Mentions", Tags.Mention), ("Hashtags", Tags.Hashtag) })
        {
            var tokens = Tags.InTasks(tasks, mark);
            if (tokens.Count > 0)
            {
                AddHeading(rows, heading, 2, Count(tokens.Count, "smart list"));
                rows.AddRange(tokens.Select(token => SmartRow(SmartList.Tagged(token), all, today)));
            }
        }

        AddHeading(rows, "Date lists", 1, Count(SmartList.DateLists.Count, "date list"));
        rows.AddRange(SmartList.DateLists.Select(list => SmartRow(list, all, today)));

        if (top.ArchivedLists.Count > 0 || top.ArchivedItems > 0)
        {
            var archived = top.ArchivedLists.Select(ListRow).ToList();
            if (top.ArchivedItems > 0)
            {
                archived.Add(new ItemRow(true, ItemFolder.ArchiveFolder(TopFolder), ArchivedItemsName, $"{ArchivedItemsName}, {top.ArchivedItems}", ArchivedItemsTag));
            }

            AddHeading(rows, "Archived lists", 1, Count(archived.Count, "list"));
            rows.AddRange(archived);
        }

        var name = $"{_projectName ?? "Lists"}, {Count(top.Lists.Count, "list")}";
        return top.NotShown > 0 ? name + $", {Count(top.NotShown, "file")} not shown" : name;
    }

    /// <summary>Your lists, the lists in no section, then a heading of level 2 for each section of the user's own (TK-3a.1).</summary>
    private static void AddListSections(List<ItemRow> rows, TopLevel top)
    {
        var bySection = top.Lists.GroupBy(ItemFolder.SectionOf, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var plain = bySection.GetValueOrDefault(string.Empty) ?? [];
        AddHeading(rows, YourLists, 1, top.Lists.Count == 0 ? "none: Ctrl+Shift+N makes one" : Count(plain.Count, "list"));
        rows.AddRange(plain.Select(ListRow));
        foreach (var section in bySection.Keys.Where(key => key.Length > 0).OrderBy(key => key, AxKit.Core.NaturalOrder.Instance))
        {
            rows.Add(Heading("section:" + section, section, 2, Count(bySection[section].Count, "list"), new SectionTag(section, true, 2)));
            rows.AddRange(bySection[section].Select(ListRow));
        }
    }

    /// <summary>A heading of axTask's own, keyed apart from the user's sections of the same name.</summary>
    private static void AddHeading(List<ItemRow> rows, string name, int level, string state) =>
        rows.Add(Heading("heading:" + name, name, level, state, new SectionTag(name, false, level)));

    /// <summary>The one shape of a heading row (TK-D13): "Smart lists, heading level 1, 3 smart lists".</summary>
    private static ItemRow Heading(string key, string name, int level, string state, object tag) =>
        new(false, key, name, $"{name}, heading level {level}, {state}", tag, IsSection: true);

    /// <summary>A heading row's level, 1 or 2; 0 for a row that is no heading.</summary>
    private static int LevelOf(ItemRow row) => row.Tag switch
    {
        SectionTag heading => heading.Level,
        ProjectSectionTag => 2,
        _ => 0,
    };

    /// <summary>"Home, list, 2 items".</summary>
    private static ItemRow ListRow(string list)
    {
        var listName = Path.GetFileName(list);
        return new ItemRow(true, list, listName, $"{listName}, list, {Count(ItemFolder.Read(list).Items.Count, "item")}");
    }

    private static ItemRow SmartRow(SmartList smart, IReadOnlyList<GatheredItem> all, DateOnly today)
    {
        var count = smart.Count(all, today);
        // "All tasks, 26 not done", "Overdue, 2", "@Sam, 3" (TK-5.4, TK-5.5); a filter of the user's says it is one.
        var label = ReferenceEquals(smart, SmartList.AllTasks) ? $"{smart.Name}, {count} not done"
            : smart.BuiltIn ? $"{smart.Name}, {count}"
            : smart.NotUnderstood is null ? $"{smart.Name}, smart list, {count}" : $"{smart.Name}, smart list, a condition not understood";
        return new ItemRow(true, smart.Key, smart.Name, label, smart);
    }

    /// <summary>
    /// Ctrl+1 and Ctrl+2 at the top level (TK-3a.2, TK-D14): the next heading of that level, with Shift the one before;
    /// the edge sound when there is none. The selection's move is read as Ctrl+Down's is.
    /// </summary>
    private void JumpHeading(int level, int direction)
    {
        var rows = Rows;
        for (var i = Navigator.SelectedIndex + direction; i >= 0 && i < rows.Count; i += direction)
        {
            if (LevelOf(rows[i]) == level)
            {
                Navigator.SelectedIndex = i;
                return;
            }
        }

        Sounds.Edge();
    }

    /// <summary>Where the heading keys act: the navigator at a top level, not renaming (TK-D14); elsewhere Ctrl+1 and Ctrl+2 are the list's action keys.</summary>
    private bool AtHeadings() => InNavigator() && NotRenaming() && AtTop && _search is null;

    // ---- the detail pane (IA-2.3) ----

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row)
        {
            case null:
                return new ItemDetail("Details, nothing selected", null, null,
                    [new ItemDetailLine(Rows.Count == 0 ? "Nothing in this list: Ctrl+N in the navigator adds an item" : "Nothing selected")]);
            case { Tag: ItemFile item }:
                return new ItemDetail("Fields of " + row.Name, item.Path, item.Document, [], new FileSource(item.Path, item.Text));
            case { Tag: FoundItem found }:
                return new ItemDetail("Fields of " + row.Name, found.Item.Path, found.Item.Document,
                    [new ItemDetailLine($"In {found.Hit.Place} / {found.Hit.List}; Enter in the navigator opens it there")], new FileSource(found.Item.Path, found.Item.Text));
            case { Tag: ProjectTag or ProjectSectionTag }:
                return ProjectDetail(row);
            case { Tag: SectionTag section }:
                return new ItemDetail("Details of " + section.Name, row.Key, null,
                [
                    new ItemDetailLine(row.Label),
                    new ItemDetailLine((section.Custom ? string.Empty : section.Name) switch
                    {
                        "Smart lists" => "All tasks and your saved filters, over your lists and every project's; List, New smart list makes one",
                        "Mentions" => "A smart list for each @name in the tasks not done, in their titles and text fields, in your lists and every project's",
                        "Hashtags" => "A smart list for each #tag in the tasks not done, in their titles and text fields, in your lists and every project's",
                        "Date lists" => "The tasks not done by their Due date, in your lists and every project's",
                        "Projects" => "A heading for each project folder, with the folders of lists found under it; Options, Project folders adds and removes them",
                        "Archived lists" => "What Delete moved away; Ctrl+Z brings back the last, axFile deletes for good",
                        YourLists => "Lists in no section of your own, then your sections; Ctrl+M on a list moves it to a section",
                        _ => "A section of your own: a list joins it by its Section field, or Ctrl+M",
                    }),
                ]);
            case { Tag: var tag } when ReferenceEquals(tag, ArchivedItemsTag):
                return new ItemDetail("Details of " + ArchivedItemsName, row.Key, null,
                    [new ItemDetailLine(row.Label), new ItemDetailLine("Items Delete moved out of their lists; Right shows them")]);
            case { Tag: SmartList smart }:
                var lines = new List<ItemDetailLine>
                {
                    new(smart.Described ?? "A smart list: the items its conditions hold for"),
                };
                if (smart.NotUnderstood is { } why)
                {
                    lines.Add(new ItemDetailLine("A condition not understood: " + why));
                }

                lines.Add(new ItemDetailLine(smart.BuiltIn ? "Built in; a smart list of your own is List, New smart list" : $"Its file: {Path.GetFileName(smart.Path)}; Ctrl+J changes it in axJSON"));
                return new ItemDetail("Details of " + smart.Name, null, null, lines);
            default:
                var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
                var read = ItemFolder.ReadItem(listFile);
                var template = TaskRules.TemplateOf(read?.Document);
                var templateLine = new ItemDetailLine(template.Count == 0
                    ? "Fields of a new item: only its title, a plain list"
                    : "Fields of a new item: " + string.Join(", ", template.Select(field => field.Title)), ExplainTemplate);
                return new ItemDetail("Details of " + row.Name, listFile, WithSectionField(read?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate)), [templateLine], new FileSource(listFile, read?.Text));
        }
    }

    /// <summary>TK-3a.3: a list's details with the Section field first, made empty when the file has none.</summary>
    private static ItemDocument WithSectionField(ItemDocument details)
    {
        if (details.IndexOf(ItemFolder.SectionField) >= 0)
        {
            return details;
        }

        var withField = details.WithField(-1, ItemFolder.SectionField, FieldTypes.Text, []);
        for (var i = withField.Fields.Count - 1; i > 0; i--)
        {
            withField = withField.WithFieldMoved(i, -1)!;
        }

        return withField;
    }

    /// <summary>Enter on a list's "Fields of a new item" line: how they change.</summary>
    private void ExplainTemplate() =>
        ShowNotice(Notice.Plain("Fields of a new item", (ShownDetail.Lines.FirstOrDefault()?.Label ?? string.Empty) + ".\r\n\r\nTo change them: select an item with the fields new items should get, then Item, Use these fields for new items. List, Add Done to every item or Remove Done from every item makes the list a checklist or a plain list. Ctrl+J opens the list's details in axJSON."));
}
