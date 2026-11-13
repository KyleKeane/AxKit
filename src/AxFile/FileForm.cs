using AxFile.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>
/// axFile's window (SPEC.md AF-2): a menu bar, one list with a folder's entries, the Actions list always under it ("No
/// actions yet" until the first operation), and a status bar with the newest action and the folder's path. Enter is the verb (G2): a
/// folder opens in the list, a file in the program Windows has for it; Shift+Enter reads a file in axPreview. The
/// folder reads and the operations run on background tasks. The notices, the fonts, the window's place, the update
/// flow and the announcements are the bundle's window base (<see cref="AxKitForm"/>). The window is in four files:
/// this one (the controls, the menus, the hooks, the keys), Browse (the folder, the list and the selection),
/// Operations (the operations and the ways out) and Actions (the Actions list and the hand-overs to axMedia).
/// </summary>
public sealed partial class FileForm : AxKitForm
{
    private readonly FileSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent folder");
    private readonly ToolStripMenuItem _favoritesItem = new("Fa&vorites");
    private readonly ToolStripMenuItem _drivesMenu = new("Driv&es");
    private readonly ChoiceItems<SortBy> _sortItems;
    private readonly AxKitList _list = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly ToolStripStatusLabel _pathLabel;
    private readonly ToolStripMenuItem _undoItem;

    /// <summary>The bar's Context menu (AX-9.11): the entry's commands, or the Actions list's line's.</summary>
    private readonly ContextBarMenu _context;

    /// <summary>Whether the Actions list was the last of the two lists to have the focus, which Context follows.</summary>
    private bool _paneLast;

    /// <param name="settingsPath">Where the settings are saved; the app's own file unless a probe says otherwise, so that a probe never writes the user's.</param>
    public FileForm(string? path, FileSettings settings, string? settingsError, string? settingsPath = null)
        : base(AxFileApp.Name, settings, settingsPath ?? FileSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _startPath = path;
        SetTitle(null);

        // ---- menus and keys (AF-6.1, AF-6.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables;
        // a key's When is where it acts: the folder list (InList), the Actions list (InPane) or anywhere ----
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

        // Navigation (AX-9.1): the ways around, and the Actions list.
        var navigation = new ToolStripMenuItem();
        // AF-3.9: the pinned folders, refilled when the submenu opens, with the key's item under them.
        CommandPalette.RefreshedOnOpening(_favoritesItem, FillFavorites);
        FillFavorites();
        navigation.DropDownItems.Add(_favoritesItem);
        // Drives (AF-3.11): the drives to go to, This PC, and the drive's own Eject and Properties; refilled when it
        // opens, since a stick may have come or gone. A submenu here since 2026-10-22, freeing Alt+D for Go to folder.
        CommandPalette.NoEntry(_drivesMenu).DropDownOpening += (_, _) => FillDrives();
        FillDrives();
        navigation.DropDownItems.Add(_drivesMenu);
        Add(navigation, new Command("&Go to folder...", AxFileKeys.GoToFolder, GoToFolder) { Mark = CommandMark.Open });
        AddKey(new Command("Go to folder", AxFileKeys.GoToFolderAlso, GoToFolder) { Mark = CommandMark.Open });
        Add(navigation, new Command("&Up one level", AxFileKeys.Up, GoUp) { When = InList, Mark = CommandMark.Open });
        AddKey(new Command("Up one level", AxFileKeys.UpAlso, GoUp) { Mark = CommandMark.Open });
        navigation.DropDownItems.Add(new ToolStripSeparator());
        Add(navigation, new Command("The &Actions list", Keys.None, ShowActions) { Mark = CommandMark.Open });
        AddKey(new Command("Add to or remove from Favorites", AxFileKeys.Favorite, ToggleFavorite) { Mark = CommandMark.Change });

        // Actions (AX-9.1): what axFile does in the folder shown, the same wherever the focus is.
        var actions = new ToolStripMenuItem();
        // AF-4.14: named after what it would undo each time the menu opens, "Undo rename"; the palette refreshes it the same way (AX-10.2).
        // Ctrl+Z in the rename box is the box's own undo of the typing (AF-6.2); in the Actions list, the line under
        // the selection (AF-9.3), whatever its place.
        _undoItem = Add(actions, new Command(_actions.MenuText, AxFileKeys.Undo, Undo) { When = () => !_list.Renaming && !_pane.Focused, Mark = CommandMark.Act });
        AddKey(new Command("Undo the action", AxFileKeys.Undo, UndoSelectedAction) { When = () => !_list.Renaming && _pane.Focused, Mark = CommandMark.Act });
        CommandPalette.RefreshedOnOpening(actions, () => _undoItem.Text = _actions.MenuText);
        actions.DropDownItems.Add(new ToolStripSeparator());
        Add(actions, new Command("&New folder...", AxFileKeys.NewFolder, NewFolder) { Mark = CommandMark.Act });
        Add(actions, new Command("New text &file", AxFileKeys.NewFile, NewFile) { Mark = CommandMark.Act });
        Add(actions, new Command("&Paste", AxFileKeys.Paste, Paste) { When = InList, Mark = CommandMark.Act });
        actions.DropDownItems.Add(new ToolStripSeparator());
        // AF-1.3: the way to the other windows, on the folder shown. The base makes the submenu around its File
        // Explorer item, so the item only shows the key and the key is a command of its own.
        folder.DropDownItems.Add(CreateShowInMenu("Show current folder &in", Item("File &Explorer", ShowFolderInExplorer, AxFileKeys.ShowInExplorer), () => TryFolder(out var shown) ? shown : null, FolderMissing));
        AddKey(new Command("Show the folder in File Explorer", AxFileKeys.ShowInExplorer, ShowFolderInExplorer) { Mark = CommandMark.Open });
        folder.DropDownItems.Add(new ToolStripSeparator());
        folder.DropDownItems.Add(CreateExitItem());

        // Context (AX-9.11, AF-4.6): the entry's commands, the folder list's, or the Actions list's line's, by the list
        // that last had the focus; held in two menus off the bar. The bar's drop-down is the one WinForms makes
        // (AX-6.6): a hand-made one loses its owner item when it closes, and the bar then opens its neighbours' menus
        // on Left and Right.
        var item = new ToolStripMenuItem("&Entry");
        FillItemMenu(item);

        // Selection (AF-3.7, AF-D10, 2026-10-22): the marks, Select all and Deselect all in Actions, the rest a submenu
        // of the entry's Context: this entry's mark, then the operations on the marks alone, which say "None selected"
        // rather than fall back to the current entry.
        Add(actions, new Command("Select &all", AxFileKeys.SelectAll, SelectAll) { When = InList, Mark = CommandMark.Change });
        Add(actions, new Command("D&eselect all", AxFileKeys.SelectNone, SelectNone) { When = InList, Mark = CommandMark.Change });
        var select = new ToolStripMenuItem("Selectio&n");
        item.DropDownItems.Add(new ToolStripSeparator());
        item.DropDownItems.Add(select);
        Add(select, new Command("Select or deselect this &entry", AxFileKeys.Select, ToggleSelection) { When = InList, Mark = CommandMark.Change });
        Add(select, new Command("Select to &here", AxFileKeys.SelectTo, SelectToHere) { When = InList, Mark = CommandMark.Change });
        // The Shift ranges (AF-3.7), keys of the list with no menu item.
        AddKey(new Command("Select to the next entry", AxFileKeys.SelectDown, () => ExtendSelection(_list.SelectedIndex + 1)) { When = InList, Mark = CommandMark.Change });
        AddKey(new Command("Select to the previous entry", AxFileKeys.SelectUp, () => ExtendSelection(_list.SelectedIndex - 1)) { When = InList, Mark = CommandMark.Change });
        AddKey(new Command("Select to the first entry", AxFileKeys.SelectToStart, () => ExtendSelection(0)) { When = InList, Mark = CommandMark.Change });
        AddKey(new Command("Select to the last entry", AxFileKeys.SelectToEnd, () => ExtendSelection(int.MaxValue)) { When = InList, Mark = CommandMark.Change });
        // Escape: the marks cleared, else the filter (AF-3.7, AF-3.8); with neither, the key is left alone.
        AddKey(new Command("Clear the selection or the filter", AxFileKeys.Clear, () => ClearSelectionOrFilter()) { When = () => InList() && (_marks.Count > 0 || _filter.Length > 0), Mark = CommandMark.Change });
        select.DropDownItems.Add(new ToolStripSeparator());
        // The operations on the marks alone (AF-D10): their keys are the entry's commands, on the targets, which
        // are the marks while there are any; these items only show them.
        select.DropDownItems.Add(Item("Cu&t", () => OnMarks(() => CopyOrCut(cut: true)), AxFileKeys.Cut));
        select.DropDownItems.Add(Item("&Copy", () => OnMarks(() => CopyOrCut(cut: false)), AxFileKeys.Copy));
        select.DropDownItems.Add(Item("De&lete", () => OnMarks(Delete), AxFileKeys.Delete));
        select.DropDownItems.Add(new ToolStripSeparator());
        Add(select, new Command("Mo&ve to folder...", Keys.None, () => OnMarks(MoveIntoNewFolder)) { Mark = CommandMark.Act });
        Add(select, new Command("Compress to &zip", Keys.None, () => OnMarks(Compress)) { Mark = CommandMark.Act });
        select.DropDownItems.Add(Item("Add to axMedia's &queue", () => OnMarks(AddToQueue), AxFileKeys.AddToQueue));
        select.DropDownItems.Add(Item("Copy &paths", () => OnMarks(CopyPath), AxFileKeys.CopyPath));

        // The keys without a menu item. Two places at most, the folder list and the Actions list (AF-9.1): every one
        // of the control keys moves between them.
        foreach (var key in new[] { Keys.Tab, Keys.Shift | Keys.Tab, BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            AddKey(new Command("Switch between the folder list and the Actions list", key, SwitchPane) { Mark = CommandMark.Open });
        }

        AddKey(new Command("Find", BundleKeys.Find, ShowFindNotice) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(-1)) { Mark = CommandMark.Read });
        // In the folder list: Right and Left (AF-D12), and Context from the keyboard (AF-4.6, AF-D13, AX-9.11).
        AddKey(new Command("Into the folder", AxFileKeys.IntoFolder, IntoFolder) { When = InList, Mark = CommandMark.Open });
        AddKey(new Command("Up one level", AxFileKeys.OutOfFolder, OutOfFolder) { When = InList, Mark = CommandMark.Open });
        AddKey(new Command("Context menu", AxFileKeys.ContextMenu, ShowItemMenu) { When = InList, Mark = CommandMark.Open });
        AddKey(new Command("Context menu", AxFileKeys.ContextMenuAlso, ShowItemMenu) { When = InList, Mark = CommandMark.Open });
        // In the Actions list (AF-9.3): Enter goes where the action left things, Escape goes back to the folder list.
        AddKey(new Command("Go to the action's place", AxFileKeys.Open, GoToAction) { When = InPane, Mark = CommandMark.Open });
        AddKey(new Command("Back to the folder list", AxFileKeys.Clear, () => _list.Focus()) { When = InPane, Mark = CommandMark.Open });

        // The Actions list's line (AF-9.3): its keys, shown in Context while the Actions list has the focus.
        var line = new ToolStripMenuItem("&Line");
        line.DropDownItems.Add(Item("&Go to where it happened", GoToAction, AxFileKeys.Open));
        line.DropDownItems.Add(Item("&Undo this action", UndoSelectedAction, AxFileKeys.Undo));
        line.DropDownItems.Add(Item("Its number and &time", SayActionInformation, AxFileKeys.Information));
        _list.Enter += (_, _) => _paneLast = false;
        _pane.Enter += (_, _) => _paneLast = true;
        _context = new ContextBarMenu(() => _paneLast ? line : item, () => "Nothing selected", said => Announce(said, true), item, line);

        // Options, as the other windows' (AX-6): the order and the filter (AF-3.8) first, then every setting in the
        // submenu for what it is about.
        var options = new ToolStripMenuItem();
        var sort = new ToolStripMenuItem("&Sort by");
        _sortItems = new ChoiceItems<SortBy>(sort, Enum.GetValues<SortBy>().Select(by => (by, "&" + char.ToUpperInvariant(by.ToString()[0]) + by.ToString()[1..])), _settings.SortBy, SetSort);
        options.DropDownItems.Add(sort);
        Add(options, new Command("&Filter...", AxFileKeys.Filter, ShowFilterNotice) { Mark = CommandMark.Change });
        options.DropDownItems.Add(new ToolStripSeparator());
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
        PlaceMenuBar(_menu, folder, actions, navigation, _context.Menu, options, help);
        _menu.MenuActivate += (_, _) => _context.Fill();

        // ---- the list (AF-2.1, AF-2.2): the main control; its own keys stay native (AF-3.6) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = "No folder";
        _list.DoubleClick += (_, _) => OpenSelected();
        // AF-3.6: File Explorer's type-ahead, several letters into one prefix, in place of the list's one-letter jump.
        // The bundle's list (AX-8.6) types over the shown entries' names, not the empty folder's or the Not shown line.
        _list.Names = () => _shown.Select(entry => entry.Name).ToList();
        // AF-4.6, AF-D13: a right click opens the bar's Context menu on the entry under it, selected first as File
        // Explorer does; Shift+F10 and the Applications key open the same menu from their commands.
        _list.OpenMenuOnRightClick(ShowItemMenu);

        // ---- the Actions list (AF-9): always under the folder list, "No actions yet" until the first operation ----
        SetUpActions();

        // ---- the status bar (AF-2.3): the bundle's pair, no AccessibleName, no Spring ----
        (_stateLabel, _pathLabel) = StatusLayout.Pair(_status);

        SizeChanged += (_, _) => FitPane();
        // AF-3.4: coming back to the window reads the folder again when another program changed it; not when one of
        // the Windows dialogs closes, which activates the window too (the base's DialogShowing).
        Activated += (_, _) =>
        {
            if (!DialogShowing)
            {
                Do(RefreshIfChangedAsync);
            }
        };

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the Actions
        // list the strip above the status bar, and the folder list (or the notice over both) what is left.
        PlaceControls(_menu, _list, _pane, _status);
        FitPane();
        FillPane();
    }

    /// <summary>
    /// The entry's menu (AF-4.6, AF-6.1), filled for the menu bar and again for the list's context menu: one flat
    /// list in File Explorer's order and words, with two submenus for the ways to open (Open with: Windows and the
    /// user's programs, AF-4.7, AF-4.11; Open in axKit: the bundle's apps, AF-4.9), and the hand-overs to axPreview
    /// and axMedia (AF-3.2, AF-5). Filled once, so that each command is registered once; only Open with is refilled as it opens.
    /// </summary>
    private void FillItemMenu(ToolStripMenuItem menu)
    {
        Add(menu, new Command("&Open", AxFileKeys.Open, OpenSelected) { When = InList, Mark = CommandMark.Act });
        var openWith = new ToolStripMenuItem("Open &with");
        CommandPalette.NoEntry(openWith).DropDownOpening += (_, _) => FillOpenWith(openWith);
        FillOpenWith(openWith);
        menu.DropDownItems.Add(openWith);
        var axkit = new ToolStripMenuItem("Open in &axKit");
        Add(axkit, new Command("&Editor", AxFileKeys.OpenInEditor, OpenInEditor) { Mark = CommandMark.Open });
        axkit.DropDownItems.Add(new ToolStripSeparator());
        // Every app that opens a path (AF-4.9), by group as in the Window menu.
        WindowMenu.AddApps(axkit.DropDownItems, Dispatch.Apps.Where(app => app.Opens != Opens.Nothing), app => new ToolStripMenuItem(AxKitWindows.AppLabel(app), null, (_, _) => OpenInAxKit(app)));

        menu.DropDownItems.Add(axkit);
        Add(menu, new Command("Pr&eview", AxFileKeys.Preview, OpenInPreview) { When = InList, Mark = CommandMark.Open });
        Add(menu, new Command("Play folder in AxMe&dia", AxFileKeys.PlayFolder, PlayFolder) { When = InList, Mark = CommandMark.Open });
        Add(menu, new Command("Add to axMedia's &queue", AxFileKeys.AddToQueue, AddToQueue) { When = InList, Mark = CommandMark.Open });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Cu&t", AxFileKeys.Cut, () => CopyOrCut(cut: true)) { When = InList, Mark = CommandMark.Act });
        Add(menu, new Command("&Copy", AxFileKeys.Copy, () => CopyOrCut(cut: false)) { When = InList, Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        // Posted, so that a menu has closed and the list has the focus back before the box takes it; F2 is posted the same way.
        Add(menu, new Command("Rena&me", AxFileKeys.Rename, () => BeginInvoke(() => Rename())) { When = InList, Mark = CommandMark.Act });
        Add(menu, new Command("&Delete", AxFileKeys.Delete, Delete) { When = InList, Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Mo&ve to folder...", Keys.None, MoveIntoNewFolder) { Mark = CommandMark.Act });
        Add(menu, new Command("Compress to &zip", Keys.None, Compress) { Mark = CommandMark.Act });
        Add(menu, new Command("E&xtract here", Keys.None, Extract) { Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        Add(menu, new Command("Copy pat&h", AxFileKeys.CopyPath, CopyPath) { Mark = CommandMark.Act });
        Add(menu, new Command("Show in File Exp&lorer", AxFileKeys.ShowItemInExplorer, ShowItemInExplorer) { Mark = CommandMark.Open });
        // Ctrl+I in the Actions list is the line's number and time (AF-9.3).
        Add(menu, new Command("&Information", AxFileKeys.Information, SayInformation) { When = () => !_pane.Focused, Mark = CommandMark.Read });
        AddKey(new Command("The action's number and time", AxFileKeys.Information, SayActionInformation) { When = InPane, Mark = CommandMark.Read });
        Add(menu, new Command("Prope&rties", AxFileKeys.Properties, ShowProperties) { Mark = CommandMark.Act });
        menu.DropDownItems.Add(new ToolStripSeparator());
        // AF-4.7: the shell's own menu, behind one item of ours (AF-D8).
        Add(menu, new Command("Windows men&u...", Keys.None, ShowWindowsMenu) { Mark = CommandMark.Act });
    }

    /// <summary>Where a key of the folder list acts: the list itself has the focus, not the rename box over it or the Actions list (AF-6.2).</summary>
    private bool InList() => _list.Focused;

    /// <summary>Where a key of the Actions list acts (AF-9.3).</summary>
    private bool InPane() => _pane.Focused;

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        EndRename();
        _list.Visible = false;
        _pane.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _list.Visible = true;
        FitPane();
        _pane.Visible = true;
    }

    /// <summary>The folder list, when the control a notice opened from is gone (the rename box); the Actions list keeps the focus across a notice by the base's rule (AF-9.1).</summary>
    protected override Control FocusTarget => _list;

    /// <summary>An announcement is raised on the list that has the focus (AF-7): the Actions list while it does, the folder list otherwise.</summary>
    protected override Control AnnounceSource => _pane.Focused ? _pane : _list;

    protected override void ApplyFont(Font font)
    {
        _list.Font = font;
        _pane.Font = font;
        FitPane();
    }

    protected override string UpdateRestartText => $"closes {AxFileApp.Name}, installs it and starts it again on the same folder";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Folder: {_folder ?? "(none)"}",
        $"Actions: {_actions.Count}, undoable {_actions.Entries.Count(entry => entry.CanUndo)}",
    ];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
        Do(() => StartAsync(_startPath));
    }

    /// <summary>The folder shown, for Recently closed (AX-9.4); nothing at This PC.</summary>
    protected override string? PathInHand => RealFolder;

    /// <summary>
    /// What an AI tool's act command would act on (docs/axKit/mcp.md MC-7.3): the selected entries, or the current one,
    /// in the folder shown, by their names ("report.pdf in files") and, for the Run's comparison, by their full paths, so
    /// that the same names in another folder of the same name are another target; the Actions list's line when it has
    /// the focus.
    /// </summary>
    protected override AxKit.Core.Mcp.RunTarget McpTarget()
    {
        if (_pane.Focused)
        {
            return AxKit.Core.Mcp.RunTarget.Of(_pane.SelectedItem?.ToString() is { Length: > 0 } action ? $"{action}, in the Actions list" : "the Actions list");
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



    /// <summary>The status bar (AF-2.3): the newest file operation's line on the left (never a selection line), the folder's full path on the right.</summary>
    private void UpdateStatusBar()
    {
        _stateLabel.Text = _actions.LatestOperation?.Line ?? string.Empty;
        _pathLabel.Text = _folder ?? string.Empty;
        StatusLayout.Fit(_status, _stateLabel, _pathLabel);
    }
}
