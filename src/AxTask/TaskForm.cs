using AxKit.Core;
using AxKit.Core.Events;
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
    /// <summary>
    /// What the detail pane's document is saved to: a file and the text it was read from (null when it does not exist
    /// yet, a list's first <c>list.json</c>), and what the document was made from: an item as read, or a list's
    /// configuration, whose details the document shows. <see cref="Locked"/>: why nothing of it can be changed
    /// (an item whose configuration is not there, AF-13.4; a configuration that does not read, AF-11.7), or null.
    /// </summary>
    private sealed record FileSource(string Path, string? Text, ItemFile? Item = null, ListConfig? List = null, string? Locked = null);

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
        // detail pane's: Context holds the one for where the focus is and what is selected (AX-9.11, B-19).
        var file = new ToolStripMenuItem("&Session");
        file.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Top &folder...", null, (_, _) => ChooseTopFolder()), CommandMark.Act));
        file.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), ItemKeys.Refresh, CommandMark.Read));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        // Navigation: the moves and the finds (AX-9.1).
        var navigation = new ToolStripMenuItem();
        Add(navigation, new Command("&All tasks", AxTaskKeys.AllTasks, ShowAllTasks) { When = NotRenaming, Mark = CommandMark.Open });
        // TK-7b.4: the search across every list, the user's own and every project's.
        Add(navigation, new Command("Find in &every list...", AxTaskKeys.FindEverywhere, FindEverywhere) { When = NotRenaming, Mark = CommandMark.Read });
        navigation.DropDownItems.Add(Item("Pre&vious section", () => JumpSection(-1), ItemKeys.PreviousSection, CommandMark.Open));
        navigation.DropDownItems.Add(Item("Ne&xt section", () => JumpSection(1), ItemKeys.NextSection, CommandMark.Open));

        // Actions: what acts on the lists as a whole, the same wherever the focus is (AX-9.1).
        var actions = new ToolStripMenuItem();
        actions.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New, CommandMark.Change));
        actions.DropDownItems.Add(Item("New &list...", NewList, ItemKeys.NewList, CommandMark.Change));
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("New &smart list...", null, (_, _) => NewSmartList()), CommandMark.Change));
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo, CommandMark.Change));
        actions.DropDownItems.Add(new ToolStripSeparator());
        // AX-8.16: the items of the level shown, selected or not; the keys are the item window's.
        actions.DropDownItems.Add(Item("Select &all", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("D&eselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Add &Done to every item", null, (_, _) => SwitchList(done: true)), CommandMark.Act));
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Remove Do&ne from every item", null, (_, _) => SwitchList(done: false)), CommandMark.Act));
        actions.DropDownItems.Add(new ToolStripSeparator());
        // The program an action of the list runs (forms-framework.md AF-7.6; TaskForm.Actions.cs). The actions
        // themselves are written in the list's configuration, Ctrl+J on the list, and listed in Context.
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Sto&p the running action", null, (_, _) => StopAction()), CommandMark.Act));

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open, CommandMark.Open));
        _listMenu.DropDownItems.Add(Item("&Rename", RenameSelected, ItemKeys.Rename, CommandMark.Change));
        _listMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act));
        Add(_listMenu, new Command("Move to s&ection...", AxTaskKeys.MoveToSection, MoveToSection) { When = OnListToMove, Mark = CommandMark.Change });
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        // AF-11.1: on a list, Ctrl+J is its configuration, the one file a list is configured in; on anything else it
        // opens the selection's file (the Item menu's "Open in axJSON" shows the same key).
        Add(_listMenu, new Command("Edit the list's confi&guration", AxTaskKeys.OpenInAxJson, OpenInAxJson) { When = NotRenaming, Mark = CommandMark.Open });
        Add(_listMenu, new Command("Show in ax&File", AxTaskKeys.ShowInAxFile, ShowInAxFile) { When = NotRenaming, Mark = CommandMark.Open });

        // AF-14.1: Enter on an item opens its form, one control for each field of its list.
        _itemMenu.DropDownItems.Add(Item("&Open its form", OpenFormOfSelected, ItemKeys.Open, CommandMark.Open));
        // TK-4.4 (2026-11-22): Ctrl+Enter, the bundle's main action, marks a task done or not done, the selected
        // tasks when items are selected; Space, which did until then, selects (bundle AX-8.16).
        Add(_itemMenu, new Command("&Done or not done", BundleKeys.MainAction, ToggleDone) { When = NotRenaming, Mark = CommandMark.Change });
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        _itemMenu.DropDownItems.Add(Item("Rena&me", RenameSelected, ItemKeys.Rename, CommandMark.Change));
        _itemMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act));
        // TK-4.7: Ctrl+M, the move key, on an item or with items selected: to a list chosen in a form, or a new one.
        // On a list with nothing selected the key is the List menu's Move to section (OnListToMove).
        Add(_itemMenu, new Command("Mo&ve to a list...", AxTaskKeys.MoveToSection, MoveToList) { When = OnItemsToMove, Mark = CommandMark.Change });
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        // The selection (AX-8.16, TK-4.7): the keys are the item window's commands; these items show them. Done,
        // Archive and the list's actions that set or move act on the selected items when there are any.
        _itemMenu.DropDownItems.Add(Item("&Select or deselect", SelectRow, SelectionKeys.Select, CommandMark.Change));
        _itemMenu.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        // The maintainer's approval of an outside report (docs/axKit/outside-reports.md OR-5; TaskForm.Approvals.cs).
        _itemMenu.DropDownItems.AddRange(CreateApprovalItems());
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        // TK-3a.2, TK-D14: Ctrl+1 and Ctrl+2 go by heading at a top level, where no list's action keys act; registered
        // before the action keys, so that the first command whose key and place match runs.
        AddKey(new Command("Next heading level 1", AxTaskKeys.NextHeading1, () => JumpHeading(1, 1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Next heading level 2", AxTaskKeys.NextHeading2, () => JumpHeading(2, 1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Previous heading level 1", AxTaskKeys.PreviousHeading1, () => JumpHeading(1, -1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Previous heading level 2", AxTaskKeys.PreviousHeading2, () => JumpHeading(2, -1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Next heading level 3", AxTaskKeys.NextHeading3, () => JumpHeading(3, 1)) { When = AtHeadings, Mark = CommandMark.Read });
        AddKey(new Command("Previous heading level 3", AxTaskKeys.PreviousHeading3, () => JumpHeading(3, -1)) { When = AtHeadings, Mark = CommandMark.Read });
        // The list's own actions, on Ctrl+1 to Ctrl+9 and with Shift, and by name in this submenu (forms-framework.md
        // AF-6; TaskForm.Actions.cs).
        PlaceActionMenu();
        _itemMenu.DropDownItems.Add(_actionsMenu);
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson, CommandMark.Open));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile, CommandMark.Open));

        // AF-14.2: Enter on a field of an item opens the item's form with the focus in that field.
        _fieldMenu.DropDownItems.Add(Item("&Change...", OpenFormAtField, ItemKeys.Open, CommandMark.Change));
        _fieldMenu.DropDownItems.Add(Item("&Done or not done", CheckField, ItemKeys.Check, CommandMark.Change));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        // AF-13.3: which fields a list's items have, their names and their order are the list's configuration's, so
        // the field commands that changed them in one item are gone (2026-11-23), and Context has the way to the
        // configuration.
        _fieldMenu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Edit the list's confi&guration", null, (_, _) => EditConfigurationOfShown()), CommandMark.Open));

        // Options: the app's own settings first (AX-9.1; Hide done tasks was the View menu's until 2026-11-11).
        var options = new ToolStripMenuItem();
        _hideDoneItem = Add(options, new Command("&Hide done tasks", AxTaskKeys.HideDone, ToggleHideDone) { When = NotRenaming, Mark = CommandMark.Change });
        _hideDoneItem.Checked = settings.HideDone;
        options.DropDownItems.Add(new ToolStripSeparator());
        options.DropDownItems.AddRange(CreateProjectFolderItems());
        options.DropDownItems.Add(CreateApprovalKeyItem());
        options.DropDownItems.Add(new ToolStripSeparator());
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Item &resources", [("&JSON", "https://www.json.org/json-en.html")]);

        PlaceMenuBar(_menu, file, actions, navigation, CreateContextMenu(_listMenu, _itemMenu, _fieldMenu), options, help);
        // TK-7b.4: in the search's level, Right and Enter open what was found where it lives, Escape goes back; before
        // the shared keys, which PlaceControls registers after these.
        AddKey(new Command("Open it where it lives", ItemKeys.Into, OpenFound) { When = InSearch, Mark = CommandMark.Open });
        AddKey(new Command("Open it where it lives", ItemKeys.Open, OpenFound) { When = InSearch, Mark = CommandMark.Open });
        // Escape gives up a selection first (AX-8.16), the item window's command; with none it leaves the search.
        AddKey(new Command("Back from the search", ItemKeys.Back, UpLevel) { When = () => InSearch() && Selection.Count == 0, Mark = CommandMark.Open });
        // TK-4.6: Enter on a file that does not read as an item opens it in axJSON; on the gathering lists' row of them it reads the row.
        AddKey(new Command("Open the file in axJSON", ItemKeys.Open, OpenUnreadable) { When = OnUnreadable, Mark = CommandMark.Open });
        // AF-14.1, AF-14.2: Enter on an item, and on a field of one in the detail pane, opens the item's form
        // (TaskForm.ItemForm.cs); everywhere else Enter is the shared window's.
        AddKey(new Command("Open the item's form", ItemKeys.Open, OpenFormOfSelected) { When = OnFormItem, Mark = CommandMark.Open });
        AddKey(new Command("Change the item in its form", ItemKeys.Open, OpenFormAtField) { When = OnFormField, Mark = CommandMark.Change });
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
            // Bundle AX-9.5a: the list of the last run is looked at only on a drive of this computer; elsewhere the top level shows.
            _folder = LocalPaths.AtStart(_settings.LastList is { } last ? Path.Combine(TopFolder, last) : null, Directory.Exists, out var notOpened, "Right on its list") ?? TopFolder;
            SayAtStart(notOpened);
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
            // TK-4.6: the files the gathering could not read, said in the name and as the last row.
            if (ItemFolder.UnreadableSummary(TopFolder, _unreadable) is { Length: > 0 } notRead)
            {
                rows.Add(new ItemRow(false, UnreadableKey, notRead, notRead, new UnreadableTag(null)));
                name += $", {_unreadable.Count} not readable";
            }

            // AF-13.6: the lists of the earlier format are not gathered until they are converted, and the level's
            // name says so; their own rows among the sections say it too, and Enter there converts.
            if (ItemFolder.EarlierSummary(ItemFolder.EarlierLists(TopFolder)) is { Length: > 0 } earlier)
            {
                name += ", " + earlier;
            }
        }
        else if (!AtTop && ItemFolder.Read(_folder) is { Earlier: false } level)
        {
            // A list's level: its items only (TK-D4).
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

            // TK-4.6: a JSON file that does not read as an item is a row of its list, after the items.
            foreach (var path in level.Unreadable)
            {
                rows.Add(new ItemRow(false, path, Path.GetFileName(path), ItemFolder.UnreadableRow(path), new UnreadableTag(path)));
            }

            if (level.Unreadable.Count > 0)
            {
                name += $", {level.Unreadable.Count} not readable";
            }

            if (level.NotShown > 0)
            {
                name += $", {Count(level.NotShown, "file")} not shown";
            }

            // AF-11.7: a configuration that does not read is said with the level, and where it is mended.
            if (level.ConfigProblem is { } problem)
            {
                name += $". {problem}. Ctrl+J opens it";
            }
        }
        else
        {
            // The top; or a list of the earlier format, which is not opened (AF-13.6; the list of the last run, at
            // the start): the sections, where its row says so and Enter asks to convert it.
            _folder = TopFolder;
            name = ReadSections(rows, today);
        }

        var title = _search is not null ? "Found" : _smart?.Name ?? (AtTop ? _projectName ?? "Lists" : ListTitle());
        return (rows, name, title, PlaceText);
    }

    /// <summary>"Home"; the Archive's own items are "Archived items".</summary>
    private string ListTitle() => IsArchiveFolder(_folder) ? ArchivedItemsName : ItemFolder.ListName(TopFolder, _folder);

    private const string ArchivedItemsName = "Archived items";

    /// <summary>
    /// A heading row of the top level (TK-3a.1): its name, whether it is a section of the user's own, its level, 1 to 3
    /// (TK-D13, TK-D20), and what its detail pane says when that is not the name's own line.
    /// </summary>
    private sealed record SectionTag(string Name, bool Custom, int Level, string? About = null);

    private const string SmartTags = "Tags";

    /// <summary>The Tag of the "Archived items" row, which opens the Archive's own items.</summary>
    private static readonly object ArchivedItemsTag = new();

    private const string YourLists = "Your lists";

    /// <summary>
    /// The items the smart lists and the date lists gather (TK-5.6): the folder shown as the top read now, and, at the
    /// user's own top, every project's as the last look read them off the UI thread (<see cref="_projectItems"/>).
    /// </summary>
    private IReadOnlyList<GatheredItem> GatheredItems()
    {
        var (items, unreadable) = ItemFolder.ReadAll(TopFolder);
        _unreadable = unreadable;
        return
        [
            .. items.Select(item => new GatheredItem(item, TopFolder, string.Empty)),
            .. _project is null ? _projectItems ?? [] : [],
        ];
    }

    /// <summary>The JSON files of the folder shown as the top that the last gathering could not read as items (TK-4.6); a project's own are not looked for from the user's top.</summary>
    private IReadOnlyList<string> _unreadable = [];

    /// <summary>The Tag of a row for a file that does not read as an item (TK-4.6); no path on the gathering lists' row that counts them.</summary>
    private sealed record UnreadableTag(string? Path);

    private const string UnreadableKey = "unreadable:";

    private bool OnUnreadable() => InNavigator() && NotRenaming() && SelectedRow() is { Tag: UnreadableTag };

    /// <summary>Enter on such a row: the file in axJSON, or the counting row read again.</summary>
    private void OpenUnreadable()
    {
        if (SelectedRow() is { Tag: UnreadableTag { Path: { } path } })
        {
            WindowMenu.Start(Dispatch.AxJSON, path);
        }
        else if (SelectedRow() is { } row)
        {
            Announce(row.Label, true);
        }
    }

    /// <summary>
    /// The top level under two levels of headings (TK-3a.1, asked 2026-11-11; TK-D13): Your lists, with a heading of
    /// level 2 for each section of the user's own; Projects, with one for each project folder (TK-7b.3); Smart lists,
    /// All tasks and the user's filters, those sharing a first word under a heading of level 2 of it (TK-5.2a), with Tags and a heading of level 3 per symbol (TK-5.5); Date lists (TK-5.4); Archived lists. The
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

        // TK-5.2a, TK-D17: All tasks, the user's smart lists whose first word no other shares, then a heading of level 2
        // for each first word two or more share ("App"), its lists under it by their full names.
        var filters = SmartList.InFolder(TopFolder);
        var (loose, groups) = SmartList.Grouped(filters);
        AddHeading(rows, "Smart lists", 1, Count(1 + filters.Count, "smart list"));
        rows.Add(SmartRow(SmartList.AllTasks, all, today));
        rows.AddRange(loose.Select(list => SmartRow(list, all, today)));
        foreach (var (word, lists) in groups)
        {
            rows.Add(Heading("filters:" + word, word, 2, Count(lists.Count, "smart list"),
                new SectionTag(word, false, 2, $"Your smart lists whose names start with {word}; each is a file of its own, Ctrl+J changes it in axJSON")));
            rows.AddRange(lists.Select(list => SmartRow(list, all, today)));
        }

        // TK-5.5, TK-D18: Tags, a heading of level 3 for each symbol found in the tasks not done, everywhere
        // gathered (TK-5.6), with a smart list for each tag; no heading when there are none.
        var bySymbol = Tags.InTasks(all.Select(gathered => gathered.Item));
        if (bySymbol.Count > 0)
        {
            AddHeading(rows, SmartTags, 2, Count(bySymbol.Count, "symbol"));
            foreach (var (symbol, tags) in bySymbol)
            {
                var spoken = Tags.Spoken(symbol);
                rows.Add(Heading("symbol:" + symbol, spoken, 3, Count(tags.Count, "smart list"),
                    new SectionTag(spoken, false, 3, symbol == Tags.Assignee
                        ? "A smart list for each person a task is assigned to, written @ and the name in its title or a text field"
                        : $"A smart list for each tag written {symbol} and a word in the title or a text field of a task not done")));
                rows.AddRange(tags.Select(tag => SmartRow(SmartList.Tagged(tag), all, today)));
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

    /// <summary>A heading row's level, 1 to 3; 0 for a row that is no heading.</summary>
    private static int LevelOf(ItemRow row) => row.Tag switch
    {
        SectionTag heading => heading.Level,
        ProjectSectionTag => 2,
        _ => 0,
    };

    /// <summary>"Home, list, 2 items"; "Home, list, earlier format: Enter converts it" (AF-13.6); "Home, list, 2 items, its configuration does not read" (AF-11.7).</summary>
    private static ItemRow ListRow(string list)
    {
        var listName = Path.GetFileName(list);
        var level = ItemFolder.Read(list);
        var state = level.Earlier ? "earlier format: Enter converts it"
            : Count(level.Items.Count, "item") + (level.ConfigProblem is null ? string.Empty : ", its configuration does not read");
        return new ItemRow(true, list, listName, $"{listName}, list, {state}");
    }

    /// <summary>"The list's configuration does not read: it is not JSON at line 12. Ctrl+J opens it" (AF-11.7); null for a list whose configuration reads or that has none.</summary>
    private static string? ConfigurationProblem(string folder) =>
        ItemFolder.ReadConfig(folder) is { Config: null, Text: not null, Problem: { } problem }
            ? $"The list's configuration {problem}. Ctrl+J opens it"
            : null;

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
    /// Ctrl+1 to Ctrl+3 at the top level (TK-3a.2, TK-D14, TK-D20): the next heading of that level, with Shift the one before;
    /// the edge sound when there is none. The selection's move is read as Ctrl+Down's is.
    /// </summary>
    private void JumpHeading(int level, int direction)
    {
        var rows = Rows;
        for (var i = Navigator.SelectedIndex + direction; i >= 0 && i < rows.Count; i += direction)
        {
            if (LevelOf(rows[i]) == level)
            {
                // The user's move from wherever the command came (AxKitList.SelectForCommand): the heading sound too.
                Navigator.SelectForCommand(i);
                return;
            }
        }

        EventDoor.Raise(EventKind.NothingFound, EventSound.Edge);
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
                return DetailOfItem(row.Name, item, []);
            case { Tag: FoundItem found }:
                return DetailOfItem(row.Name, found.Item, [new ItemDetailLine($"In {found.Hit.Place} / {found.Hit.List}; Enter in the navigator opens it there")]);
            case { Tag: UnreadableTag { Path: { } unreadable } }:
                return new ItemDetail("Details of " + row.Name, null, null,
                    [new ItemDetailLine(row.Label), new ItemDetailLine("The file is JSON that is not an item, or not JSON: Enter in the navigator or Ctrl+J opens it in axJSON to mend; Ctrl+E shows it in axFile"), new ItemDetailLine(unreadable)]);
            case { Tag: UnreadableTag }:
                return new ItemDetail("Details of files not readable", null, null,
                    [new ItemDetailLine(row.Label), .. _unreadable.Select(path => new ItemDetailLine(path)), new ItemDetailLine("Each is a row of its own list, where Enter opens it in axJSON")]);
            case { Tag: ProjectTag or ProjectSectionTag }:
                return ProjectDetail(row);
            case { Tag: SectionTag section }:
                return new ItemDetail("Details of " + section.Name, row.Key, null,
                [
                    new ItemDetailLine(row.Label),
                    new ItemDetailLine(section.About ?? (section.Custom ? string.Empty : section.Name) switch
                    {
                        "Smart lists" => "All tasks and your saved filters, those that share a first word under a heading of it, over your lists and every project's; List, New smart list makes one",
                        SmartTags => "A heading for each symbol that starts a word in your tasks not done, @ for the people they are assigned to, # and the rest for tags, with a smart list for each, over your lists and every project's",
                        "Date lists" => "The tasks not done by their Due date, in your lists and every project's",
                        "Projects" => "A heading for each project folder, with the folders of lists found under it; Options, Add a project folder adds and removes them",
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
                return DetailOfList(row);
        }
    }

    /// <summary>
    /// An item's detail: its fields as its list's configuration gives them, then what the caller says of it, why it
    /// cannot be changed when its configuration is not there (AF-13.4), and the line of the values it keeps that the
    /// configuration does not name (AF-11.4), on which Enter reads them.
    /// </summary>
    private ItemDetail DetailOfItem(string name, ItemFile item, IReadOnlyList<ItemDetailLine> first)
    {
        var lines = new List<ItemDetailLine>(first);
        if (item.ConfigProblem is { } problem)
        {
            lines.Add(new ItemDetailLine(problem + ": every value is shown as text, and nothing of the item is changed here"));
        }

        if (item.KeptLine is { } kept)
        {
            lines.Add(new ItemDetailLine(kept, () => ReadKept(item)));
        }

        return new ItemDetail("Fields of " + name, item.Path, item.Document, lines, new FileSource(item.Path, item.Text, Item: item, Locked: item.ConfigProblem));
    }

    /// <summary>Enter on "2 more fields kept: ...": the kept values, read-only (AF-11.4).</summary>
    private void ReadKept(ItemFile item) =>
        ShowNotice(Notice.Plain("Fields kept",
            string.Join("\r\n", item.Kept.Select(kept => $"{kept.Name}: {FieldTypes.PlainText(kept.Value)}"))
            + "\r\n\r\nThese values are in the item's file, and no field of the list's configuration has their names, so they are kept and not shown among the fields. A field of that name in the configuration shows its value again. Ctrl+J on the list opens the configuration in axJSON."));

    /// <summary>
    /// A list's detail: its details as fields (Section first, TK-3a.3), then the fields of its items, on which Enter
    /// says how they change, and what the configuration's reader passed over (AF-11.7). A list of the earlier format
    /// and one whose configuration does not read show why and nothing to change.
    /// </summary>
    private ItemDetail DetailOfList(ItemRow row)
    {
        var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
        if (ItemFolder.IsEarlier(row.Key))
        {
            return new ItemDetail("Details of " + row.Name, listFile, null,
                [new ItemDetailLine(row.Label), new ItemDetailLine("This list is written in axTask's earlier format. Enter in the navigator asks to convert it; a copy is kept among the Archived lists")]);
        }

        var read = ItemFolder.ReadConfig(row.Key);
        if (read is { Config: null, Text: not null })
        {
            return new ItemDetail("Details of " + row.Name, listFile, null,
            [
                new ItemDetailLine(row.Label),
                new ItemDetailLine(ConfigurationProblem(row.Key) ?? string.Empty),
                new ItemDetailLine("Until it reads again, the list's items are shown with every value as text, and axTask writes nothing into that file"),
            ]);
        }

        // A list without a file of its own shows what the file would say; it is written with the first change.
        var config = read.Config ?? ItemFolder.StarterConfig(row.Key);
        var lines = new List<ItemDetailLine>
        {
            new(config.ItemFields.Count == 0
                ? "Fields of its items: only a title, a plain list"
                : "Fields of its items: " + string.Join(", ", config.ItemFields.Select(field => field.Title)), ExplainFields),
        };
        lines.AddRange(config.Problems.Select(problem => new ItemDetailLine("In the list's configuration, " + problem)));
        return new ItemDetail("Details of " + row.Name, listFile, DetailsDocument(config), lines, new FileSource(listFile, read.Text, List: config));
    }

    /// <summary>
    /// A list's details as the detail pane's fields: Section first, empty when the list has none (TK-3a.3), then the
    /// others in the file's order; Notes a text of several lines, the rest by JSON's own kind (AF-11.2: a detail is a
    /// name and a value).
    /// </summary>
    private static ItemDocument DetailsDocument(ListConfig config)
    {
        var fields = new List<ItemField> { new(ItemFolder.SectionField, FieldTypes.Text, System.Text.Json.Nodes.JsonValue.Create(config.Detail(ItemFolder.SectionField)), []) };
        foreach (var (name, value) in config.Details)
        {
            if (!name.Equals(ItemFolder.SectionField, StringComparison.OrdinalIgnoreCase))
            {
                fields.Add(new ItemField(name, name.Equals(ItemFolder.NotesField, StringComparison.OrdinalIgnoreCase) ? FieldTypes.LongText : FieldTypes.OfKind(value), value, []));
            }
        }

        return ItemDocument.Of(fields);
    }

    /// <summary>Enter on a list's "Fields of its items" line: how they change.</summary>
    private void ExplainFields() =>
        ShowNotice(Notice.Plain("Fields of its items", (ShownDetail.Lines.FirstOrDefault()?.Label ?? string.Empty) + ".\r\n\r\nThe fields of a list's items are written once, in the list's configuration: Ctrl+J on the list opens it in axJSON, where \"item fields\" names each field with its title, its type and the value a new item starts with. A field added there shows in every item, empty until it is given a value. A field taken out is hidden and its values are kept in the items: put it back and they show again. Actions, Add Done to every item or Remove Done from every item makes the list a checklist or a plain list."));
}
