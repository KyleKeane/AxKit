using System.Diagnostics.CodeAnalysis;
using AxFile.Core;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;

namespace AxFile;

/// <summary>The folder and the list (SPEC.md AF-3): the reads, the navigation, the ways to another folder, find, information, the selection.</summary>
public sealed partial class FileForm
{
    private string? _folder;

    /// <summary>Every entry of the folder as read (AF-3.8).</summary>
    private IReadOnlyList<Entry> _entries = [];

    /// <summary>
    /// The folder's entries in the chosen order (AF-3.8), which the level's rows are made from. The item window
    /// then shows those that pass its filter: what the list holds is its rows (<see cref="ShownEntries"/>), and
    /// an index into the list is an index into them.
    /// </summary>
    private IReadOnlyList<Entry> _sorted = [];

    /// <summary>The entries the list shows, in its order: the item window's rows, each row's entry.</summary>
    private List<Entry> ShownEntries() => [.. Rows.Select(row => (Entry)row.Tag!)];

    /// <summary>How many entries the read left out (AF-2.2): hidden ones, and ones starting with a period; the list's last line says so.</summary>
    private int _hidden;
    private int _dot;

    // The marks on the shown entries (AF-3.7), which the operations act on, are the item window's selection
    // (AX-8.16, its Selection): each entry by its path, kept while the same folder shows, a marked item reading
    // ", selected".

    /// <summary>The folder as the level the marks belong to: the same folder under another spelling keeps them.</summary>
    protected override string LevelIdentity() => _folder?.ToUpperInvariant() ?? string.Empty;

    /// <summary>The folder's last write time at the last read, so that coming back to the window knows whether to read again (AF-3.4).</summary>
    private DateTime _folderStamp;

    /// <summary>The folder shown when it is one on disk; null before one is shown and at This PC, which is no path a window can start on (AF-3.10).</summary>
    private string? RealFolder => _folder is { } folder && !FolderListing.IsThisPC(folder) ? folder : null;

    // ---- the guards of a command (AF-7) ----

    /// <summary>The folder a command needs, or why there is none said: "No folder" before any is shown, "Choose a drive first" at This PC (AF-3.10).</summary>
    private bool TryFolder([NotNullWhen(true)] out string? folder)
    {
        folder = RealFolder;
        if (folder is null)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
        }

        return folder is not null;
    }

    /// <summary>Whether a command that needs a real folder has none, which is said (<see cref="TryFolder"/>).</summary>
    private bool NoFolderHere() => !TryFolder(out _);

    /// <summary>The entry under the selection, or the line the selection is on said: an empty folder's, the filter's, the not-shown line, "Nothing selected", or "No folder".</summary>
    private bool TryEntry([NotNullWhen(true)] out Entry? entry)
    {
        entry = CurrentEntry();
        if (entry is null)
        {
            SayNoEntry();
        }

        return entry is not null;
    }

    /// <summary>The guard of every command on an entry (AF-7): "No folder", the line the selection is on when it is not an entry (the empty folder's, the filter's, the not-shown line), or "Nothing selected".</summary>
    private void SayNoEntry()
    {
        var index = _list.SelectedIndex;
        var line = index >= Rows.Count && index < _list.Items.Count ? _list.Items[index].ToString() : null;
        Announce(_folder is null ? "No folder" : line ?? "Nothing selected", true);
    }

    /// <summary>The entry under the selection; null on the lines after the entries (an empty list's, the not-shown line), which are not entries.</summary>
    private Entry? CurrentEntry() => SelectedRow()?.Tag as Entry;

    // ---- the folder (AF-2.2, AF-3.1) ----

    /// <summary>The start (AF-1.1): the path given, or the last folder, or the user's profile folder; a file selects itself in its folder.</summary>
    private async Task StartAsync(string? path)
    {
        string? folder = null;
        string? select = null;
        if (path is not null)
        {
            // R-64: a path of the command line is the user's own ask, looked at off the window's thread (a share can take the network's time limit).
            var (isFolder, isFile) = await Task.Run(() => (Directory.Exists(path), File.Exists(path)));
            if (isFolder)
            {
                folder = path;
            }
            else if (isFile)
            {
                folder = Path.GetDirectoryName(path);
                select = Path.GetFileName(path);
            }
            else
            {
                ShowNotice(Notice.Plain("Folder not found", $"{path} does not exist.") with { Unprompted = true });
            }
        }

        if (folder is null)
        {
            // AX-9.5a: the remembered folder is looked at only on a drive of this computer; one on the network is said and left for Open recent folder.
            folder = LocalPaths.AtStart(_settings.LastFolder, Directory.Exists, out var notOpened) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            SayAtStart(notOpened);
        }

        // NVDA reads the window, the list and its entry as the window opens; nothing is added (AF-3.1).
        await ShowFolderAsync(folder, select, remember: true, announce: false);
    }

    /// <summary>
    /// Shows a folder (AF-2.2): read on a background task, the list refilled with <paramref name="select"/> selected
    /// by name (or the first entry), the title and the status bar following. A folder that is gone gives the "Folder
    /// not found" notice and the nearest parent that exists (AF-3.5); any other failure is one announcement and the
    /// list stays. With <paramref name="remember"/> the folder joins the recent list (AF-3.3). Another folder than the
    /// one shown is announced with its count and the entry landed on, "docs, 5 items, plan.md" (AF-3.1), unless
    /// <paramref name="announce"/> is off: the start, which NVDA reads itself, an undo, which says what it undid, and
    /// the Actions list's Enter, which moves the focus first.
    /// </summary>
    private async Task<bool> ShowFolderAsync(string folder, string? select, bool remember, bool announce = true)
    {
        // A rename box left open over the list that is about to change renames the wrong entry (AF-4.2).
        EndRename();
        var read = BeginRead();
        var changed = !string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase);
        Listing listing;
        try
        {
            // AF-3.10: This PC is the drives, read the same way.
            listing = await Task.Run(() => FolderListing.IsThisPC(folder) ? new Listing(FolderListing.Drives(), 0, 0) : FolderListing.Read(folder, _settings.ShowHidden, _settings.ShowDotFiles));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (!EndRead(read))
            {
                return false;
            }

            if (!Directory.Exists(folder))
            {
                ShowNotice(Notice.Plain("Folder not found", $"{folder} does not exist any more.") with { Unprompted = true });
                if (NearestExisting(folder) is { } parent)
                {
                    await ShowFolderAsync(parent, null, remember);
                }

                return false;
            }

            Failed("Folder not read", FileOperations.Message(ex));
            return false;
        }

        if (!EndRead(read))
        {
            return false;
        }

        // AF-3.7, AF-3.8: the marks and the filter live in one folder: another folder starts with none, and the
        // same one keeps the filter and the marks on the entries still shown. Both are the item window's, which
        // drops them as the level changes (LevelIdentity).
        _folder = folder;
        _entries = listing.Entries;
        _hidden = listing.Hidden;
        _dot = listing.Dot;
        _folderStamp = Stamp(folder);
        Rearrange(select);
        if (FolderListing.IsThisPC(folder))
        {
            // Not a folder to come back to: the last folder and the recent list keep the one before.
        }
        else if (remember)
        {
            _settings.RememberFolder(folder);
            SaveSettings();
        }
        else
        {
            _settings.LastFolder = folder;
        }

        UpdateStatusBar();
        if (announce && changed)
        {
            Announce(FolderAnnouncement(), true);
        }

        return true;
    }

    /// <summary>
    /// What a change of folder says (AF-3.1): the folder's name, the list's count and the entry landed on, "docs, 5
    /// items, plan.md"; "temp, No items in this folder" for an empty one. NVDA alone would read the new entry only,
    /// which does not tell the folder landed in from the folder under the focus.
    /// </summary>
    private string FolderAnnouncement()
    {
        var name = FolderListing.IsThisPC(_folder) ? FolderListing.ThisPC : PathName.Of(_folder ?? string.Empty);
        var item = _list.SelectedItem?.ToString() ?? string.Empty;
        return Rows.Count == 0 ? $"{name}, {item}" : $"{name}, {_list.AccessibleName}, {item}";
    }

    /// <summary>
    /// An operation's end (AF-4.5): in the folder it ran in, when that is still the one shown, the list read again with
    /// the result selected and <paramref name="said"/> announced (unless <paramref name="sayInView"/> is off: a caller
    /// that goes on with the result, New text file's rename box, says its own); when the user has gone to another
    /// folder meanwhile (the window never waits for an operation), the result is said and the list is left where the
    /// user is, since a list that jumps back is a move the user did not make (AX-7.3; 2026-10-23). True when the
    /// result is in view.
    /// </summary>
    private async Task<bool> ShowResultAsync(string folder, string? select, string said, bool sayInView = true)
    {
        if (!string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase))
        {
            Announce(said, true);
            return false;
        }

        if (!await ShowFolderAsync(folder, select, remember: false))
        {
            return false;
        }

        if (sayInView)
        {
            Announce(said, true);
        }

        return true;
    }

    /// <summary>The one item of an empty folder (AF-2.2), so that the list is never empty and the reason is read (AX-9.5).</summary>
    private const string EmptyFolderText = "No items in this folder";

    /// <summary>
    /// The list made again from the folder's entries (AF-3.8): the entries put in the chosen order, and the list
    /// filled by the item window (<see cref="ReadLevel"/>: the rows, the list's name with the count, the title,
    /// the lines after the entries; then its filter), <paramref name="select"/> selected by name or path, or the
    /// first entry; the marks on entries no longer shown go with them (AF-3.7), and a Shift range ends. A new
    /// list starts the type-ahead afresh (AF-3.6).
    /// </summary>
    private void Rearrange(string? select)
    {
        PutInOrder();
        // A row's key is its entry's path; a name that is not here, or that the filter hides, selects nothing.
        var selected = select is null ? null : _sorted.FirstOrDefault(entry => Named(entry, select));
        Fill(select is null ? null : selected?.Path ?? select);
    }

    /// <summary>The folder's entries in the chosen order; the drives of This PC stay in letter order (AF-3.10).</summary>
    private void PutInOrder() =>
        _sorted = FolderListing.IsThisPC(_folder) ? _entries : FolderListing.Sort(_entries, _settings.SortBy);

    /// <summary>Whether the entry has that name, or that path (a drive of This PC is selected by its root).</summary>
    private static bool Named(Entry entry, string name) =>
        string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Path, name, StringComparison.OrdinalIgnoreCase);

    // ---- the order and the filter (AF-3.8): the item window's Sort by and Filter (docs/axKit/framework.md FW-4.10a) ----
    // Options → Filter… and Sort by are the item window's, the same in every navigator. axFile names its orders
    // (name, date, size), keeps the chosen one in its settings, and gives its rows in that order.

    protected override IReadOnlyList<string> SortNames => [.. Enum.GetValues<SortBy>().Select(FolderListing.SortName)];

    protected override int SortChoice
    {
        get => (int)_settings.SortBy;
        set
        {
            _settings.SortBy = (SortBy)value;
            SaveSettings();
        }
    }

    protected override bool OrdersRowsItself => true;

    protected override void SortChanged() => PutInOrder();

    // Escape in the list: the selection cleared, as Ctrl+Shift+A, while entries are selected (AF-3.7); otherwise
    // the filter cleared; the key left alone when there is neither. Both are the item window's commands.

    // ---- the selection (AF-3.7): the item window's (AX-8.16, ListSelection over Marks) ----
    // The shared piece makes each item (an entry's label, with ", selected" while it is, S-42's hidden characters
    // made visible), rewrites the items whose mark flipped and says what the key says; its keys are the item
    // window's commands (Space, Shift+Space, Ctrl+A, Ctrl+Shift+A, Shift with the arrows, Home and End), which on
    // a line that is no entry say that line (NothingToSelect). A change of the marks is no line of the Actions
    // list (it was one until 2026-11-22, AF-9.2): Space and Escape take a selection back, and the status bar says
    // how many are selected.

    /// <summary>A Selection menu operation (AF-3.7): on the marked entries alone, "None selected" when there are none, never the current entry.</summary>
    private void OnMarks(Action operation)
    {
        if (Selection.Count == 0)
        {
            Announce("None selected", true);
            return;
        }

        operation();
    }

    // Shift with Up, Down, Home or End in the list (AF-3.7) are the shared piece's four commands
    // (ListSelection.MoveCommands, registered by the item window): the current entry moves and every entry between
    // where the range began and there is marked, as in File Explorer. Each entry passed reads ", selected" on its
    // own; nothing else is said. On a line after the entries (an empty folder's, the not-shown line) the keys say
    // that line (SayNoEntry).

    /// <summary>The selected entries among those shown, in the list's order; none when nothing is selected.</summary>
    private List<Entry> SelectedEntries() => [.. SelectedRows().Select(row => (Entry)row.Tag!)];

    /// <summary>What an operation acts on (AF-3.7): the selected entries, or the current entry alone when none is selected; empty, and said, when there is no entry.</summary>
    private List<Entry> Targets()
    {
        var targets = SelectedEntries();
        if (targets.Count == 0 && CurrentEntry() is { } entry)
        {
            targets.Add(entry);
        }

        if (targets.Count == 0)
        {
            SayNoEntry();
        }

        return targets;
    }

    /// <summary>The shown entry with that name, or that path (a drive of This PC is selected by its root); -1 when none.</summary>
    private int IndexOfName(string name)
    {
        for (var i = 0; i < Rows.Count; i++)
        {
            if (Named((Entry)Rows[i].Tag!, name))
            {
                return i;
            }
        }

        return -1;
    }

    // ---- the Drives menu (AF-3.11) ----

    /// <summary>Drives ▸: the ready drives as "1. Local Disk (C:)", This PC, then Eject and Properties for the drive in hand.</summary>
    private void FillDrives()
    {
        var items = _drivesMenu.DropDownItems;
        items.Clear();
        var drives = FolderListing.Drives();
        for (var i = 0; i < drives.Count; i++)
        {
            var drive = drives[i];
            items.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, drive.Name), null, (_, _) => Do(() => ShowFolderAsync(drive.Path, null, remember: true))), CommandMark.Open));
        }

        items.Add(new ToolStripSeparator());
        items.Add(CommandPalette.Marked(new ToolStripMenuItem("This &PC", null, (_, _) => Do(() => ShowFolderAsync(FolderListing.ThisPC, CurrentDrive()?.Path, remember: false))), CommandMark.Open));
        items.Add(new ToolStripSeparator());
        items.Add(CommandPalette.Marked(new ToolStripMenuItem("&Eject this drive", null, (_, _) => EjectDrive()), CommandMark.Act));
        items.Add(CommandPalette.Marked(new ToolStripMenuItem("Drive p&roperties", null, (_, _) => ShowDriveProperties()), CommandMark.Act));
    }

    /// <summary>The drive in hand: the entry at This PC, the folder's own drive otherwise; null without a folder.</summary>
    private Entry? CurrentDrive()
    {
        if (_folder is null)
        {
            return null;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            return CurrentEntry();
        }

        var root = Path.GetPathRoot(_folder);
        return FolderListing.Drives().FirstOrDefault(drive => string.Equals(drive.Path, root, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Drives → Eject this drive: the shell's own Eject for a stick or a disc, "Ejecting <name>", and This PC when the folder was on it; "Failed: …" when the drive has no Eject or Windows refuses.</summary>
    private void EjectDrive()
    {
        if (CurrentDrive() is not { } drive)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
            return;
        }

        if (Shell.Eject(Handle, drive.Path) is { } reason)
        {
            Failed("Eject failed", reason);
            return;
        }

        Announce($"Ejecting {drive.Name}", true);
        if (RealFolder is not null)
        {
            Do(() => ShowFolderAsync(FolderListing.ThisPC, null, remember: false));
        }
    }

    /// <summary>Drives → Drive properties: the Windows Properties sheet of the drive in hand, with its space and tools.</summary>
    private void ShowDriveProperties()
    {
        if (CurrentDrive() is not { } drive)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
            return;
        }

        if (Shell.Properties(Handle, drive.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    // ---- favorites (AF-3.9) ----

    /// <summary>Folder → Favorites ▸: the pinned folders that still exist as "1. name (path)", or "No favorites yet", then the key's item.</summary>
    private void FillFavorites()
    {
        RecentMenu.FillFolders(_favoritesItem, _settings.Favorites, "&No favorites yet", CommandMark.Open, target => Do(() => ShowFolderAsync(target, null, remember: true)));
        _favoritesItem.DropDownItems.Add(new ToolStripSeparator());
        _favoritesItem.DropDownItems.Add(Item("&Add or remove this folder", ToggleFavorite, AxFileKeys.Favorite, CommandMark.Change));
    }

    /// <summary>Ctrl+D: the folder shown pinned, "Added to Favorites", or unpinned again, "Removed from Favorites".</summary>
    private void ToggleFavorite()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        var added = _settings.ToggleFavorite(folder);
        SaveSettings();
        Announce(added ? "Added to Favorites" : "Removed from Favorites", true);
    }

    /// <summary>The folder above a folder that is gone, the first one that exists; null when none is.</summary>
    private static string? NearestExisting(string folder)
    {
        for (var parent = FolderListing.Parent(folder); parent is not null; parent = FolderListing.Parent(parent))
        {
            if (Directory.Exists(parent))
            {
                return parent;
            }
        }

        return null;
    }

    private static DateTime Stamp(string folder)
    {
        try
        {
            return Directory.GetLastWriteTimeUtc(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }

    // ---- browsing (AF-3.1) ----

    /// <summary>Enter in the list (G2): a folder opens here, a file in the program the user has made its default in Windows, as in File Explorer (AF-3.2, AF-D2).</summary>
    private void OpenEntry()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (entry.IsFolder)
        {
            Do(() => ShowFolderAsync(entry.Path, null, remember: false));
        }
        else
        {
            OpenWithWindows(entry);
        }
    }

    /// <summary>Shift+Enter in the list, Item → Preview (AF-3.2): a file read in axPreview, which hands it to its open window itself; a folder in a new axFile window, File Explorer's Shift+Enter.</summary>
    private void OpenInPreview()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        WindowMenu.Start(entry.IsFolder ? Dispatch.AxFile : Dispatch.AxPreview, entry.Path);
    }

    /// <summary>Backspace, Alt+Up, Folder → Up one level: the parent with the folder just left selected; This PC with the drive selected from a drive's root (AF-3.10); "Top" at This PC.</summary>
    private void GoUp()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            Announce("Top", true);
            return;
        }

        var from = _folder;
        if (FolderListing.Parent(from) is not { } parent)
        {
            Do(() => ShowFolderAsync(FolderListing.ThisPC, from, remember: false));
            return;
        }

        var left = PathName.Of(from);
        Do(() => ShowFolderAsync(parent, left, remember: false));
    }

    /// <summary>
    /// Right Arrow in the list (AF-3.1, AF-D12), the item window's Into: a folder or a drive opened, as Enter; the
    /// edge earcon on a file or a line that is not an entry (the item window's), so that an arrow key never starts
    /// a program. The folder is read on a background task and shown when the read is in, so the item window is told
    /// that the level stays for now, with nothing to say.
    /// </summary>
    protected override string? EntryRefused(ItemRow row)
    {
        Do(() => ShowFolderAsync(row.Key, null, remember: false));
        return string.Empty;
    }

    /// <summary>The item window's Enter on an item, should anything of it open one: as Enter here (AF-3.2).</summary>
    protected override void OpenItem(ItemRow row) => OpenEntry();

    /// <summary>Left Arrow in the list (AF-3.1, AF-D12), the item window's Up a level: up one level, as Backspace; the edge earcon at This PC.</summary>
    protected override void UpLevel()
    {
        if (FolderListing.IsThisPC(_folder))
        {
            EventDoor.Raise(EventKind.Edge);
            return;
        }

        GoUp();
    }

    /// <summary>Folder → Open folder… (Ctrl+O): the bundle's folder picker, one of the system's own windows, on the folder shown.</summary>
    private void OpenFolderDialog()
    {
        var chosen = ChooseFolder("Choose the folder to show", _folder);
        if (chosen is not null)
        {
            Do(() => ShowFolderAsync(chosen, null, remember: true));
        }
    }

    /// <summary>Folder → Open recent folder (AF-3.3): the last ten opened by command, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.FillFolders(_recentItem, _settings.RecentFolders, "&No recent folders", CommandMark.Open, target => Do(() => ShowFolderAsync(target, null, remember: true)));

    // ---- find (AF-3.6) and information (AF-2.4) ----

    /// <summary>Ctrl+F: the bundle's find in a list over the shown entries' names (AF-3.6); the lines after the entries are not searched.</summary>
    private void ShowFindNotice() => ShowFindInList(_list, EntryNames);

    /// <summary>F3 and Shift+F3: the next (or previous) entry whose name contains the text, wrapping once; "Not found" otherwise.</summary>
    private void FindInList(int direction) => FindInList(_list, EntryNames, direction);

    private IReadOnlyList<string> EntryNames() => Rows.Select(row => row.Name).ToList();

    /// <summary>Item → Information (Ctrl+I): the entry's size, or that it is a folder, and its date, on demand (AF-D7); and how many entries are selected, when any are (AF-3.7).</summary>
    private void SayInformation()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        var count = Selection.Count;
        Announce(FolderListing.Information(entry) + (count > 0 ? $", {count} selected" : string.Empty), true);
    }
}
