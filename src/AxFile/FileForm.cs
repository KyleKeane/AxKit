using AxFile.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>
/// axFile's window (SPEC.md AF-2): a menu bar, two panes, the folder list with a folder's entries and the Favorites
/// list always under it ("No favorites yet" until the first), and a status bar with the newest operation and the
/// folder's path. Enter is the verb (G2): a folder opens in the list, a file in the program Windows has for it;
/// Shift+Enter reads a file in axPreview; in the Favorites list, the favorite in the folder list. The folder reads
/// and the operations run on background tasks. The window is a navigator, the bundle's item window
/// (<see cref="ItemForm"/>; docs/axKit/framework.md FW-4.8, step 13, 2026-11-22; a window of its own until then): a
/// folder is a level and its entries the rows, and the Favorites list is the second pane, a list axFile fills
/// itself. The two lists, the split between them, the status bar, the selection's marks, the notices, the fonts and
/// the window's place are the item window's. In this first step the commands stay axFile's own, each on the item
/// window's condition for its place, so that the keys, the words and the order are what they were; they move onto
/// the type's hooks one by one after it. The window is in several files: this one (the menus, the hooks, the keys),
/// Browse (the folder, the list and the selection), Favorites (the Favorites list, the submenu and Ctrl+D),
/// Operations (the operations and the ways out), History (the operations' history, Recent operations and the
/// hand-overs to axMedia), Completion and Programs.
/// </summary>
public sealed partial class FileForm : ItemForm
{
    private readonly FileSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent folder");
    private readonly ToolStripMenuItem _favoritesItem = new("Fa&vorites");
    private readonly ToolStripMenuItem _drivesMenu = new("Driv&es");

    /// <summary>The folder list: the item window's navigator.</summary>
    private AxKitList _list => Navigator;

    /// <summary>The menu of the Favorites list's line, which Context holds while that list was the last of the two to have the focus (AX-9.11).</summary>
    private readonly ToolStripMenuItem _lineMenu = new("&Favorite");

    /// <param name="settingsPath">Where the settings are saved; the app's own file unless a probe says otherwise, so that a probe never writes the user's.</param>
    public FileForm(string? path, FileSettings settings, string? settingsError, string? settingsPath = null)
        : base(AxFileApp.Name, settings, settingsPath ?? FileSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _startPath = path;
        SetTitle(null);

        // ---- menus and keys (AF-6.1, AF-6.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables;
        // a key's When is where it acts: the folder list (InNavigator), the Favorites list (InDetail) or anywhere ----
        // Folder is the thing worked on, in the standard order of a first menu (bundle AX-6): Open and the recent
        // folders, the ways around, Show current folder in, Exit.
        var folder = new ToolStripMenuItem("&Session");
        Add(folder, new Command("&New window", AxFileKeys.NewWindow, NewWindow) { Mark = CommandMark.Open });
        Add(folder, new Command("&Open folder...", AxFileKeys.OpenFolder, OpenFolderDialog) { Mark = CommandMark.Open });
        CommandPalette.RefreshedOnOpening(_recentItem, FillRecent);
        FillRecent();
        folder.DropDownItems.Add(_recentItem);
        folder.DropDownItems.Add(new ToolStripSeparator());
        Add(folder, new Command("Re&fresh", AxFileKeys.Refresh, () => Do(() => RefreshAsync(announce: true))) { Mark = CommandMark.Read });
        folder.DropDownItems.Add(new ToolStripSeparator());

        // Navigation (AX-9.1): the ways around, and the Favorites list.
        var navigation = new ToolStripMenuItem();
        // AF-3.9: the pinned folders, refilled when the submenu opens, with the key's item under them; the same
        // folders as the Favorites list (AF-9.1).
        CommandPalette.RefreshedOnOpening(_favoritesItem, FillFavorites);
        FillFavorites();
        navigation.DropDownItems.Add(_favoritesItem);
        // Drives (AF-3.11): the drives to go to, This PC, and the drive's own Eject and Properties; refilled when it
        // opens, since a stick may have come or gone. A submenu here since 2026-10-22.
        CommandPalette.NoEntry(_drivesMenu).DropDownOpening += (_, _) => FillDrives();
        FillDrives();
        navigation.DropDownItems.Add(_drivesMenu);
        Add(navigation, new Command("&Go to folder...", AxFileKeys.GoToFolder, GoToFolder) { Mark = CommandMark.Open });
        Add(navigation, new Command("&Up one level", AxFileKeys.Up, GoUp) { When = InNavigator, Mark = CommandMark.Open });
        navigation.DropDownItems.Add(new ToolStripSeparator());
        Add(navigation, new Command("The Favorites &list", Keys.None, ShowFavorites) { Mark = CommandMark.Open });
        AddKey(new Command("Add to or remove from Favorites", AxFileKeys.Favorite, ToggleFavorite) { Mark = CommandMark.Change });

        // Actions (AX-9.1): what axFile does in the folder shown, the same wherever the focus is.
        var actions = new ToolStripMenuItem();
        // AF-4.14: one Undo, the same name whatever it would take back; what it undid is said after it runs ("Undone: ...").
        // Ctrl+Z in the rename box is the box's own undo of the typing (AF-6.2); everywhere else, in either list, the
        // newest operation.
        Add(actions, new Command("Un&do", AxFileKeys.Undo, Undo) { When = () => !_list.Renaming, Mark = CommandMark.Act });
        // AF-9.3: the window's operations, newest first, to read again and go to where one left things.
        Add(actions, new Command("Recent &operations...", Keys.None, ShowRecentOperations) { Mark = CommandMark.Read });
        actions.DropDownItems.Add(new ToolStripSeparator());
        Add(actions, new Command("&New folder...", AxFileKeys.NewFolder, NewFolder) { Mark = CommandMark.Act });
        Add(actions, new Command("New text &file", AxFileKeys.NewFile, NewFile) { Mark = CommandMark.Act });
        Add(actions, new Command("New &Markdown file", Keys.None, () => NewFile(".md")) { Mark = CommandMark.Act });
        Add(actions, new Command("New &JSON file", Keys.None, () => NewFile(".json")) { Mark = CommandMark.Act });
        Add(actions, new Command("&Paste", AxFileKeys.Paste, Paste) { When = InNavigator, Mark = CommandMark.Act });
        actions.DropDownItems.Add(new ToolStripSeparator());
        // AF-1.3: the way to the other windows, on the folder shown. The base makes the submenu around its File
        // Explorer item, open as every window's File Explorer item (docs/axKit/command-exceptions.json, 2026-11-25);
        // no key since 2026-11-26, when Ctrl+W left File Explorer in every window.
        folder.DropDownItems.Add(CreateShowInMenu("Show current folder &in", Item("File &Explorer", ShowFolderInExplorer, Keys.None, CommandMark.Open), () => TryFolder(out var shown) ? shown : null, FolderMissing));
        folder.DropDownItems.Add(new ToolStripSeparator());
        folder.DropDownItems.Add(CreateExitItem());

        // Context (AX-9.11, AF-4.6): the entry's commands, the folder list's, or the Favorites list's line's, by the list
        // that last had the focus; held in two menus off the bar. The bar's drop-down is the one WinForms makes
        // (AX-6.6): a hand-made one loses its owner item when it closes, and the bar then opens its neighbours' menus
        // on Left and Right.
        var item = new ToolStripMenuItem("&Entry");
        FillItemMenu(item);

        // Selection (AF-3.7, AF-D10, 2026-10-22): the marks, Select all and Deselect all in Actions, the rest a submenu
        // of the entry's Context: this entry's mark, then the operations on the marks alone, which say "None selected"
        // rather than fall back to the current entry.
        // The keys of the selection are the item window's commands (AX-8.16), the Shift ranges among them; these
        // items only show them.
        actions.DropDownItems.Add(Item("Select &all", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("D&eselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));
        var select = new ToolStripMenuItem("Selectio&n");
        item.DropDownItems.Add(new ToolStripSeparator());
        item.DropDownItems.Add(select);
        select.DropDownItems.Add(Item("Select or deselect this &entry", SelectRow, SelectionKeys.Select, CommandMark.Change));
        select.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));

        // Escape: the marks cleared (AF-3.7), and Ctrl+Delete the filter (AF-3.8); both are the item window's commands.
        select.DropDownItems.Add(new ToolStripSeparator());
        // The operations on the marks alone (AF-D10): their keys are the entry's commands, on the targets, which
        // are the marks while there are any; these items only show them.
        select.DropDownItems.Add(Item("Cu&t", () => OnMarks(() => CopyOrCut(cut: true)), AxFileKeys.Cut, CommandMark.Act));
        select.DropDownItems.Add(Item("&Copy", () => OnMarks(() => CopyOrCut(cut: false)), AxFileKeys.Copy, CommandMark.Act));
        select.DropDownItems.Add(Item("De&lete", () => OnMarks(Delete), AxFileKeys.Delete, CommandMark.Act));
        select.DropDownItems.Add(new ToolStripSeparator());
        Add(select, new Command("Mo&ve to folder...", Keys.None, () => OnMarks(MoveIntoNewFolder)) { Mark = CommandMark.Act });
        Add(select, new Command("Compress to &zip", Keys.None, () => OnMarks(Compress)) { Mark = CommandMark.Act });
        select.DropDownItems.Add(Item("Add to axMedia's &queue", () => OnMarks(AddToQueue), AxFileKeys.AddToQueue, CommandMark.Act));
        select.DropDownItems.Add(Item("Copy &paths", () => OnMarks(CopyPath), AxFileKeys.CopyPath, CommandMark.Act));

        // The keys without a menu item. Between the two places, the folder list and the Favorites list (AF-9.1), go
        // the item window's pane keys; Right and Left (AF-D12) are its Into and Up a level (EntryRefused, UpLevel),
        // and Context from the keyboard (AF-4.6, AF-D13, AX-9.11) its Context keys, in either list.
        AddKey(new Command("Find", BundleKeys.Find, ShowFindNotice) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(-1)) { Mark = CommandMark.Read });
        // In the Favorites list (AF-9.1): Enter shows the favorite in the folder list, Delete takes it out of the
        // favorites; Escape goes back to the folder list, the item window's key.
        AddKey(new Command("Open the favorite", AxFileKeys.Open, OpenFavorite) { When = InDetail, Mark = CommandMark.Open });
        AddKey(new Command("Remove from Favorites", AxFileKeys.Delete, RemoveSelectedFavorite) { When = InDetail, Mark = CommandMark.Change });

        // The Favorites list's line (AF-9.1): its keys, shown in Context while the Favorites list was the last of the
        // two lists to have the focus (the item window's LogLineMenu).
        _lineMenu.DropDownItems.Add(Item("&Open", OpenFavorite, AxFileKeys.Open, CommandMark.Open));
        _lineMenu.DropDownItems.Add(Item("&Remove from Favorites", RemoveSelectedFavorite, AxFileKeys.Delete, CommandMark.Change));

        // Options, as the other windows' (AX-6): the filter and the order (AF-3.8) first, which the item window puts
        // there as in every navigator, then every setting in the submenu for what it is about.
        var options = new ToolStripMenuItem();
        options.DropDownItems.AddRange(CreateProgramItems());
        options.DropDownItems.Add(new ToolStripSeparator());
        options.DropDownItems.Add(CreateTextMenu());
        // AF-2.2: what the list leaves out, two switches; the folder is read again on a flip.
        options.DropDownItems.Add(CreateGeneralMenu(
            Toggle("Show &hidden files and folders", _settings.ShowHidden, v =>
            {
                _settings.ShowHidden = v;
                Do(() => RefreshAsync(announce: false));
            }),
            Toggle("Show files and folders starting with a &period", _settings.ShowDotFiles, v =>
            {
                _settings.ShowDotFiles = v;
                Do(() => RefreshAsync(announce: false));
            })));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "File &resources",
            [("&Keyboard shortcuts in Windows", "https://support.microsoft.com/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec")]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        // Context holds the entry's menu on an entry, the line's menu while the Favorites list was the last to have the focus.
        PlaceMenuBar(_menu, folder, actions, navigation, CreateContextMenu(_ => item, item, _lineMenu), options, help);

        // ---- the list (AF-2.1, AF-2.2): the item window's navigator; its own keys stay native (AF-3.6) ----
        // Its type-ahead (AF-3.6) goes over the rows' names, which are the shown entries' (the item window's), and a
        // right click opens Context on the entry under it (AF-4.6, AF-D13; the item window's too).
        _list.DoubleClick += (_, _) => OpenEntry();

        // ---- the Favorites list (AF-9.1): the item window's second pane, under the folder list ----
        SetUpFavorites();

        // AF-3.4: coming back to the window reads the folder again when another program changed it; not when one of
        // the Windows dialogs closes, which activates the window too (the base's DialogShowing).
        Activated += (_, _) =>
        {
            if (!DialogShowing)
            {
                Do(RefreshIfChangedAsync);
            }
        };

        // The item window places the two lists, the split between them and the status bar, and adds its own keys
        // after axFile's: a key axFile has a command for in the same place stays axFile's.
        PlaceControls(_menu);
        Fill(select: null);
        FillFavoritesPane();
        UpdateStatusBar();
    }

    // ---- what the navigator asks for (the item window, docs/axKit/framework.md FW-4.8) ----

    /// <summary>The second pane is the Favorites list, a list of lines axFile fills itself (the item window's kind for it is a log).</summary>
    protected override SecondPane Pane => SecondPane.Log;

    protected override ToolStripMenuItem? LogLineMenu => _lineMenu;

    protected override string LogName => "Favorites list";

    /// <summary>Space selects entries (AF-3.7): the operations act on the selected ones.</summary>
    protected override bool HasSelection => true;

    protected override string RowNoun => "entry";

    /// <summary>The one line of an empty folder (AF-2.2); none before a folder is shown. The line of a folder whose entries all fail the filter is the item window's (AF-3.8).</summary>
    protected override string? EmptyLine => _folder is null ? null : EmptyFolderText;

    /// <summary>What the read left out (AF-2.2), as the last line, so that the count is there when wanted.</summary>
    protected override string? AfterRowsLine => _hidden > 0 || _dot > 0 ? FolderListing.NotShownText(_hidden, _dot) : null;

    /// <summary>The status bar's first part (AF-2.3): the newest file operation's line; how many are selected follows it, the item window's.</summary>
    protected override string? StatusState => Working ?? _actions.Latest?.Line ?? string.Empty;

    /// <summary>
    /// The folder shown as the navigator's level (AF-2.1, AF-2.2): a row for each entry, in the chosen order (a
    /// folder a list, a file an item, its key its path), the list named with its count, the title "folder" and the
    /// folder's full path after the app's name ("This PC" for This PC), the status bar's second part its path. The
    /// item window then shows the rows that pass its filter and says so in the list's name. Before a folder is shown there are no rows and the list is "No folder".
    /// </summary>
    protected override (IReadOnlyList<ItemRow> Rows, string Name, Opened? Title, string Place) ReadLevel() =>
        _folder is null
            ? ([], "No folder", null, string.Empty)
            : ([.. _sorted.Select(entry => new ItemRow(entry.IsFolder, entry.Path, entry.Name, entry.Label, entry))],
                FolderListing.ListName(_entries.Count), FolderListing.IsThisPC(_folder) ? new Opened(PathName.Of(_folder)) : Opened.At(_folder), _folder);

    /// <summary>
    /// The navigator's keys axFile has a command of its own for under another condition, or no use for: Ctrl+Z
    /// (Undo in either list), F7 (nothing to read in full), and the section keys, a folder
    /// having no sections. Of the rest, some are axFile's own commands in the same place, which the item window
    /// leaves to it (Enter, F2, Delete, Backspace), and the others are the item window's (the pane keys, Right
    /// and Left, the selection's, Context's, Escape).
    /// </summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse =>
        [ItemKeys.Undo, ItemKeys.ReadAll, ItemKeys.PreviousSection, ItemKeys.NextSection];

    /// <summary>The selection keys on a line that is no entry say that line (AF-7).</summary>
    protected override void NothingToSelect(ItemRow? row) => SayNoEntry();

    // What the item window asks every app for and axFile reaches through its own commands, which carry axFile's
    // marks (a rename or a delete of a file is an act, AK-2.5): F2 is Rename, whose box and steps are the item
    // window's (Operations), Delete is Delete, Ctrl+N is New window and Ctrl+Shift+N New folder.

    /// <summary>Nothing refuses a rename here: axFile's Rename has said "No folder" or "Choose a drive first" before the box (AF-3.10).</summary>
    protected override string? RenameRefused(ItemRow row) => null;

    protected override void DeleteRow(ItemRow row) => Delete();

    protected override void NewItem() => NewFile();

    protected override void NewList() => NewFolder();

    /// <summary>
    /// The entry's menu (AF-4.6, AF-6.1), filled for the menu bar and again for the list's context menu: one flat
    /// list in File Explorer's order and words, with two submenus for the ways to open (Open with: Windows and the
    /// user's programs, AF-4.7, AF-4.11; Open with axKit apps and Open with axKit access layers, AF-4.9), and the hand-overs to axPreview
    /// and axMedia (AF-3.2, AF-5). Filled once, so that each command is registered once; only Open with is refilled as it opens.
    /// </summary>
    private void FillItemMenu(ToolStripMenuItem menu)
    {
        Add(menu, new Command("&Open", AxFileKeys.Open, OpenEntry) { When = InNavigator, Mark = CommandMark.Act });
        var openWith = new ToolStripMenuItem("Open &with");
        CommandPalette.NoEntry(openWith).DropDownOpening += (_, _) => FillOpenWith(openWith);
        FillOpenWith(openWith);
        menu.DropDownItems.Add(openWith);
        // Every app that opens a path (AF-4.9), by group as in the Window menu, the standalone apps and the access layers
        // as two entries (the maintainer, 2026-11-23: "you can call open with axKit Apps and open with axKit Access
        // Layers as separate things").
        ToolStripMenuItem OpenItem(App app) => CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.AppLabel(app), null, (_, _) => OpenInAxKit(app)), WindowMenu.OpenMark(app.Verb));
        var apps = new ToolStripMenuItem("Open with axKit &apps");
        WindowMenu.AddApps(apps.DropDownItems, Dispatch.Apps.Where(app => app.Opens != Opens.Nothing && !app.IsAccessLayer), OpenItem);
        menu.DropDownItems.Add(apps);
        var layers = new ToolStripMenuItem("Open with axKit access layer&s");
        WindowMenu.AddApps(layers.DropDownItems, Dispatch.Apps.Where(app => app.Opens != Opens.Nothing && app.IsAccessLayer), OpenItem);
        menu.DropDownItems.Add(layers);
        Add(menu, new Command("Pr&eview", AxFileKeys.Preview, OpenInPreview) { When = InNavigator, Mark = CommandMark.Open });
        // Ctrl+Enter, the bundle's main action (AX-8.10), is a second key of Preview in the folder list (the maintainer,
        // 2026-11-26); Play folder in axMedia, its key until then, keeps its item here without a key.
        AddKey(new Command("Preview", BundleKeys.MainAction, OpenInPreview) { When = InNavigator, Mark = CommandMark.Open });
        Add(menu, new Command("Play folder in axMe&dia", Keys.None, PlayFolder) { When = InNavigator, Mark = CommandMark.Open });
        Add(menu, new Command("Add to axMedia's &queue", AxFileKeys.AddToQueue, AddToQueue) { When = InNavigator, Mark = CommandMark.Open });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Cu&t", AxFileKeys.Cut, () => CopyOrCut(cut: true)) { When = InNavigator, Mark = CommandMark.Act });
        Add(menu, new Command("&Copy", AxFileKeys.Copy, () => CopyOrCut(cut: false)) { When = InNavigator, Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        // Posted, so that a menu has closed and the list has the focus back before the box takes it; F2 is posted the same way.
        Add(menu, new Command("Rena&me", AxFileKeys.Rename, () => BeginInvoke(() => Rename())) { When = InNavigator, Mark = CommandMark.Act });
        Add(menu, new Command("&Delete", AxFileKeys.Delete, Delete) { When = InNavigator, Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Mo&ve to folder...", Keys.None, MoveIntoNewFolder) { Mark = CommandMark.Act });
        Add(menu, new Command("Compress to &zip", Keys.None, Compress) { Mark = CommandMark.Act });
        Add(menu, new Command("E&xtract here", Keys.None, Extract) { Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Copy pat&h", AxFileKeys.CopyPath, CopyPath) { Mark = CommandMark.Act });
        Add(menu, new Command("Show in File Exp&lorer", Keys.None, ShowItemInExplorer) { Mark = CommandMark.Open });
        // Ctrl+I in the Favorites list says the favorite's line again (AF-9.1).
        Add(menu, new Command("&Information", AxFileKeys.Information, SayInformation) { When = () => !_pane.Focused, Mark = CommandMark.Read });
        AddKey(new Command("The favorite's folder", AxFileKeys.Information, SayFavorite) { When = InDetail, Mark = CommandMark.Read });
        Add(menu, new Command("Prope&rties", AxFileKeys.Properties, ShowProperties) { Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        // AF-4.7: the shell's own menu, behind one item of ours (AF-D8).
        Add(menu, new Command("Windows men&u...", Keys.None, ShowWindowsMenu) { Mark = CommandMark.Act });
    }

    // Where a key acts (AF-6.2, AF-9.1) is the item window's condition for its place: InNavigator, the folder list
    // itself having the focus, not the rename box over it or the Favorites list; InDetail, the Favorites list.

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>A rename box left open is closed first; the two lists are hidden by the item window.</summary>
    protected override void HideForNotice()
    {
        EndRename();
        base.HideForNotice();
    }

    /// <summary>An announcement is raised on the list that has the focus (AF-7): the Favorites list while it does, the folder list otherwise.</summary>
    protected override Control AnnounceSource => _pane.Focused ? _pane : _list;

    protected override string UpdateRestartText => $"closes {AxFileApp.Name}, installs it and starts it again on the same folder";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Folder: {_folder ?? "(none)"}",
        $"Operations: {_actions.Count}, undoable {_actions.Entries.Count(entry => entry.CanUndo)}",
        $"Favorites: {_settings.Favorites.Count}",
    ];

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Do(() => StartAsync(_startPath));
    }

    /// <summary>The folder shown, for Recently closed (AX-9.4); nothing at This PC.</summary>
    protected override string? PathInHand => RealFolder;

    /// <summary>
    /// What an AI tool's act command would act on (docs/axKit/mcp.md MC-7.3): the selected entries, or the current one,
    /// in the folder shown, by their names ("report.pdf in files") and, for the Run's comparison, by their full paths, so
    /// that the same names in another folder of the same name are another target; the Favorites list's line when it
    /// has the focus.
    /// </summary>
    protected override AxKit.Core.Mcp.RunTarget McpTarget()
    {
        if (_pane.Focused)
        {
            return AxKit.Core.Mcp.RunTarget.Of(_pane.SelectedItem?.ToString() is { Length: > 0 } favorite ? $"{favorite}, in the Favorites list" : "the Favorites list");
        }

        var entries = SelectedEntries();
        if (entries.Count == 0 && CurrentEntry() is { } entry)
        {
            entries.Add(entry);
        }

        var folder = RealFolder is { } real ? PathName.Of(real) : "This PC";
        var names = AxKit.Core.Mcp.McpRun.Names([.. entries.Select(entry => entry.Name)]);
        var identity = string.Join("\n", [RealFolder ?? "This PC", .. entries.Select(entry => entry.Path)]);
        return new AxKit.Core.Mcp.RunTarget(names.Length > 0 ? $"{names} in {folder}" : $"the folder {folder}", identity);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (RealFolder is { } closing)
        {
            // AF-8.1: the last folder; the window base puts it in Recently closed (PathInHand).
            _settings.LastFolder = closing;
        }

        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        // AF-4.7: while Windows' own menu for an entry shows, the shell fills its submenus through the owner's messages.
        if (Shell.ForwardMenuMessage(ref m))
        {
            return;
        }

        base.WndProc(ref m);
    }



    /// <summary>The status bar (AF-2.3) shown again: the newest file operation's line on the left (<see cref="StatusState"/>; never a selection line), the folder's full path on the right.</summary>
    private void UpdateStatusBar()
    {
        SetPlace(_folder ?? string.Empty);
        ShowStatus();
    }
}
