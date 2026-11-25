using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// A line of an item app's navigator (docs/axKit/item-apps.md IA-2.2): a list (or group, or smart list) Right goes
/// into, or an item; its key for the selection (a path), its name for type-to-find, its words, and what the app keeps
/// with it. A section's header (IA-2.3a) has <see cref="IsSection"/>: Ctrl+Up and Ctrl+Down go from one to the next.
/// </summary>
public sealed record ItemRow(bool IsList, string Key, string Name, string Label, object? Tag = null, bool IsSection = false);

/// <summary>A line of the detail pane after the fields: its words, and what Enter on it does (nothing when null).</summary>
public sealed record ItemDetailLine(string Label, Action? Enter = null);

/// <summary>
/// What the detail pane shows for the navigator's selection (IA-2.3): its name ("Fields of Gmail"), the key of what it
/// shows (so that the field selected stays when the same thing is shown again), the fields' document (null for a line
/// that holds none, such as a smart list), the lines after the fields, and what the app keeps to save it.
/// </summary>
public sealed record ItemDetail(string Name, string? Key, ItemDocument? Document, IReadOnlyList<ItemDetailLine> Lines, object? Source = null);

/// <summary>
/// The window of an item app (docs/axKit/item-apps.md IA-2, IA-3, IA-5; from axTask on its second use, axPass,
/// 2026-10-24): the navigator on the left, the detail pane on the right, each the bundle's <see cref="AxKitList"/>, a
/// status bar, and the window's menu bar. This class holds what both apps do the same way: the panes, Right, Left and
/// Enter through the levels, Tab between the panes, the keys of <see cref="ItemKeys"/> on the window's command list
/// (docs/axKit/app-kit.md AK-2; registered after the app's own, so that the app's come first), the detail pane filled
/// silently for the selection, and every field command with its notices (<c>ItemForm.Fields.cs</c>). The app fills in
/// what a level holds (<see cref="ReadLevel"/>), how it goes in and out of a list, what the detail pane shows, and how
/// a change is saved (<see cref="SaveFields"/>): a file for axTask, the KeePassXC tool for axPass.
/// </summary>
public abstract partial class ItemForm : AxKitForm
{
    private readonly SplitContainer _split = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _nameLabel;
    private readonly ToolStripStatusLabel _placeLabel;
    private List<ItemRow> _rows = [];
    private List<(int Field, ItemDetailLine Line)> _lines = [];

    /// <summary>The detail pane's content now; its document is what the field commands change.</summary>
    private ItemDetail _shown = new("Details", null, null, []);

    /// <summary>A refill changed the selected row while the detail pane had the focus: the pane is made again when the focus reaches the navigator.</summary>
    private bool _detailStale;

    protected ItemForm(string appName, WindowSettings settings, string settingsPath, string? settingsError)
        : base(appName, settings, settingsPath, settingsError)
    {
        Navigator.Dock = DockStyle.Fill;
        Navigator.TabIndex = 0;
        Navigator.Names = () => _rows.Select(row => row.Name).ToList();
        Navigator.SelectedIndexChanged += (_, _) => FillDetail();
        // AX-7.9: the user's key landed on a heading row (a section): the heading sound before NVDA reads it.
        Navigator.UserMovedSelection += (_, _) =>
        {
            if (SelectedRow() is { IsSection: true })
            {
                SoundOnHeading();
            }
        };
        // AX-9.11: Context follows the pane that last had the focus.
        DetailPane.Enter += (_, _) => _detailLast = true;
        Navigator.Enter += (_, _) =>
        {
            _detailLast = false;
            if (_detailStale)
            {
                FillDetail();
            }
        };
        DetailPane.Dock = DockStyle.Fill;
        DetailPane.TabIndex = 1;
        DetailPane.Names = () => _lines.Select(line => line.Field >= 0 && _shown.Document is { } document && line.Field < document.Fields.Count ? document.Fields[line.Field].Title : line.Line.Label).ToList();
        Navigator.OpenMenuOnRightClick(OpenPaneMenu);
        DetailPane.OpenMenuOnRightClick(OpenPaneMenu);

        _split.Dock = DockStyle.Fill;
        _split.TabStop = false;
        _split.Panel1.Controls.Add(Navigator);
        _split.Panel2.Controls.Add(DetailPane);
        (_nameLabel, _placeLabel) = StatusLayout.Pair(_status);
    }

    /// <summary>The navigator, on the left (IA-2.2).</summary>
    protected AxKitList Navigator { get; } = new();

    /// <summary>The detail pane, on the right (IA-2.3).</summary>
    protected AxKitList DetailPane { get; } = new();

    /// <summary>The navigator's rows as last filled.</summary>
    protected IReadOnlyList<ItemRow> Rows => _rows;

    /// <summary>The detail pane's content now.</summary>
    protected ItemDetail ShownDetail => _shown;

    // ---- what the app fills in ----

    /// <summary>The level shown: its rows, the navigator's name with its count ("Home, 4 items"), the window title's first part, and the status bar's second label (the folder or the database).</summary>
    protected abstract (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel();

    /// <summary>The app's state set to the list's level, for Right and Enter on a list; nothing in an app whose rows are never lists (axMedia's queue).</summary>
    protected virtual void EnterList(ItemRow row)
    {
    }

    /// <summary>Why Right and Enter do not go into this list, said in place of the move; null (every list of most apps) when they do.</summary>
    protected virtual string? EntryRefused(ItemRow row) => null;

    /// <summary>The app's state set to the level above, and the key of the row to select there; null at the top, where nothing changes, which a navigator of one level always is.</summary>
    protected virtual string? LeaveList() => null;

    /// <summary>What the detail pane shows for a row, or for nothing selected; never asked in a window without the pane (<see cref="HasDetailPane"/>).</summary>
    protected virtual ItemDetail DetailOf(ItemRow? row) => new("Details", null, null, []);

    /// <summary>A change to the detail pane's document, saved; the app fills the panes again and calls <see cref="SelectField"/> with <paramref name="selectField"/> when it is done, and says <paramref name="said"/>.</summary>
    protected virtual void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
    }

    /// <summary>An item's Title changed, from the detail pane's Title field: the app renames it as F2 does.</summary>
    protected virtual void RenameItem(ItemRow row, string title)
    {
    }

    /// <summary>
    /// F2 in the navigator (AX-8.7): why this row cannot be renamed, said instead of the box; null when it can. Nothing
    /// is renamed by default: an app that renames says when it cannot, and one that never does leaves the key out
    /// (<see cref="KeysWithoutUse"/>), so that this default and the three below are said only where nothing else is.
    /// </summary>
    protected virtual string? RenameRefused(ItemRow row) => "Nothing is renamed here";

    /// <summary>How much of the row's name the rename box selects as it opens, from its start: all of it by default; axFile, a file's name before its extension.</summary>
    protected virtual int RenameSelectionLength(ItemRow row) => row.Name.Length;

    /// <summary>Why a typed name is refused for the row, said with the box kept open; null when it is fine.</summary>
    protected virtual string? RefusedName(ItemRow row, string name) => null;

    /// <summary>The row renamed to a name <see cref="RefusedName"/> took, once the box has closed; an item's rename by default, as its Title field's (axTask renames a list its own way).</summary>
    protected virtual void RenameTo(ItemRow row, string name) => RenameItem(row, name);

    /// <summary>Delete in the navigator (IA-D4).</summary>
    protected abstract void DeleteRow(ItemRow row);

    /// <summary>Ctrl+N in the navigator; nothing is made by default.</summary>
    protected virtual void NewItem() => Refuse("Nothing is made here");

    /// <summary>Ctrl+Shift+N; nothing is made by default.</summary>
    protected virtual void NewList() => Refuse("Nothing is made here");

    /// <summary>Ctrl+Z (IA-5.4); nothing to undo by default.</summary>
    protected virtual void Undo() => Refuse("Nothing to undo here");

    // ---- the bar's Context menu (AX-9.11, B-19, 2026-11-11; the three menus that acted on the selection until then) ----

    private ToolStripMenuItem? _listMenu;
    private ToolStripMenuItem? _rowMenu;
    private ToolStripMenuItem? _fieldMenu;
    private ContextBarMenu? _context;

    /// <summary>
    /// Whether the detail pane was the last of the two panes to have the focus: Context follows it while a notice, the
    /// palette or the menu bar has the focus, and while the window is behind another (an AI tool's run).
    /// </summary>
    private bool _detailLast;

    /// <summary>
    /// The bar's Context menu (AX-9.11) over the app's three menus of what acts on the selection, which are never placed
    /// on the bar: a list's (axTask's List, axPass's Group, axMCP's Server), an item's (Item, Entry) and a field's.
    /// Context holds the field's in the detail pane, the item's or the list's in the navigator; made once, with the bar.
    /// </summary>
    protected ToolStripMenuItem CreateContextMenu(ToolStripMenuItem list, ToolStripMenuItem item, ToolStripMenuItem field)
    {
        (_listMenu, _rowMenu, _fieldMenu) = (list, item, field);
        _context = new ContextBarMenu(ContextNow, NothingToActOn, said => Announce(said, true), list, item, field);
        return _context.Menu;
    }

    /// <summary>The menu of a row by its kind, in a window made with <see cref="CreateContextMenu(Func{ItemRow, ToolStripMenuItem}, ToolStripMenuItem[])"/>.</summary>
    private Func<ItemRow, ToolStripMenuItem?>? _menuOf;

    /// <summary>
    /// The bar's Context menu in a navigator without the second pane, whose rows are of the app's own kinds
    /// (axMedia's files, one menu; axProcess's lists, processes, services and startup programs, four): Context
    /// holds the menu <paramref name="menuOf"/> gives for the row you are on. <paramref name="menus"/> are all of
    /// them, kept off the bar.
    /// </summary>
    protected ToolStripMenuItem CreateContextMenu(Func<ItemRow, ToolStripMenuItem?> menuOf, params ToolStripMenuItem[] menus)
    {
        _menuOf = menuOf;
        _context = new ContextBarMenu(ContextNow, NothingToActOn, said => Announce(said, true), menus);
        return _context.Menu;
    }

    /// <summary>Whether the selection's commands act at all: axPass's only while the database is unlocked.</summary>
    protected virtual bool SelectionMenusAvailable => true;

    /// <summary>What Context says while <see cref="SelectionMenusAvailable"/> is false (axPass: locked).</summary>
    protected virtual string NothingWhileUnavailable => "Nothing to act on";

    /// <summary>The menu for where the focus is and what is selected: the field's in the detail pane, the item's or the list's in the navigator; null on a heading or with nothing selected.</summary>
    private ToolStripMenuItem? ContextNow()
    {
        if (!SelectionMenusAvailable)
        {
            return null;
        }

        if (_detailLast && Pane == SecondPane.Log)
        {
            // The log pane was the last of the two to have the focus: the menu of its line, which the app gives.
            return LogLineMenu;
        }

        if (_detailLast && _shown.Document is not null)
        {
            return _fieldMenu;
        }

        return SelectedRow() switch
        {
            null or { IsSection: true } => null,
            var row when _menuOf is not null => _menuOf(row),
            { IsList: true } => _listMenu,
            _ => _rowMenu,
        };
    }

    /// <summary>Context's one item when it has nothing else: why.</summary>
    private string NothingToActOn() =>
        !SelectionMenusAvailable ? NothingWhileUnavailable
        : SelectedRow() is { IsSection: true } ? "A heading: nothing to act on"
        : NoRowSaid();

    /// <summary>Context filled for where the focus is and what is selected now.</summary>
    private void UpdateMenus() => _context?.Fill();

    /// <summary>A field's value read out; the app's own types (a link, a password) here.</summary>
    protected virtual string SpokenValue(ItemRow? row, ItemField field) => FieldTypes.Spoken(field);

    /// <summary>A type's name read out.</summary>
    private static string SpokenType(string type) => FieldTypes.Spoken(type);

    /// <summary>Enter on a field of the app's own type (a link, a password); true when the app took it.</summary>
    protected virtual bool ChangeSpecial(ItemRow row, int index, ItemField field) => false;

    /// <summary>
    /// Ctrl+N, F2 and Delete in the detail pane of fields: keys with no command there, which play the sound of
    /// one (the maintainer's decision of 2026-11-23, "Remove the keys there"). Until 2026-11-21 they added,
    /// renamed and removed a field of the item; then every app refused with a sentence of its own, since which
    /// fields an item has is its list's configuration in axTask and the program's own in axPass, axMCP and
    /// axAutomate; the commands and the code behind them are gone. A key the app gave a command of its own there
    /// never reaches this: the commands run first.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (base.ProcessCmdKey(ref msg, keyData))
        {
            return true;
        }

        if (HasDetailPane && Pane == SecondPane.Fields && DetailPane.Focused && !DetailPane.Renaming
            && keyData is ItemKeys.New or ItemKeys.Rename or ItemKeys.Delete)
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Unassigned);
            return true;
        }

        return false;
    }

    // ---- the selection (docs/axKit/SPEC.md AX-8.16; 2026-11-22) ----

    /// <summary>
    /// Whether the app's navigator has a selection of several rows (AX-8.16): the keys of
    /// <see cref="SelectionKeys"/> are the window's commands then, and the app's commands that can act on several
    /// things ask <see cref="Selection"/> for what is selected. No by default: an app says yes once its commands
    /// act on a selection, so that nothing is ever selected that a command then passes by. axTask does.
    /// </summary>
    protected virtual bool HasSelection => false;

    /// <summary>Whether a row can be selected, in an app that <see cref="HasSelection"/>: a heading never can; the app says which of the others (axTask: an item, not a list).</summary>
    protected virtual bool CanSelectRow(ItemRow row) => true;

    /// <summary>Space on a row that cannot be selected, and the keys that select all in a level with nothing to select: the app's words for why; the edge sound alone by default.</summary>
    protected virtual void NothingToSelect(ItemRow? row) => EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);

    /// <summary>The app's word for a row, in the names of the commands that select as they move ("Select to the next item"); axMedia: file.</summary>
    protected virtual string RowNoun => "item";

    /// <summary>The navigator's rows as the marks see them, made with <see cref="_rows"/>.</summary>
    private IReadOnlyList<MarkRow> _markRows = [];

    /// <summary>The level the marks were made in (<see cref="LevelIdentity"/>): another level starts with none.</summary>
    private string? _selectionLevel;

    private ListSelection? _selectionMade;

    /// <summary>
    /// The navigator's selection (AX-8.16), the bundle's <see cref="ListSelection"/>: the marks on the rows of the
    /// level shown, kept while the same level shows. Empty for ever in an app without a selection.
    /// </summary>
    protected ListSelection Selection => _selectionMade ??= new ListSelection(Navigator, () => _markRows, row => DisplayText.Visible(RowText(_rows[row])), said => Announce(said, true)) { Changed = ShowName };

    /// <summary>A row's words as its item shows them: with ", selected" while it is.</summary>
    private string RowText(ItemRow row) => Selection.Label(row.Key, row.Label);

    /// <summary>The rows for the marks, and the marks kept for the same level or dropped for another; called as the navigator is filled, before its items are made.</summary>
    private void KeepSelection()
    {
        _markRows = [.. _rows.Select(row => new MarkRow(row.Key, row.Name, HasSelection && !row.IsSection && CanSelectRow(row)))];
        var level = LevelIdentity();
        if (level == _selectionLevel)
        {
            Selection.Keep();
        }
        else
        {
            Selection.Clear();
            _selectionLevel = level;
        }
    }

    /// <summary>The status bar's first part: the level's name with its count (or the app's <see cref="StatusState"/>), and ", 3 selected" while rows are.</summary>
    private void ShowName()
    {
        var name = StatusState ?? Navigator.AccessibleName ?? string.Empty;
        var text = Selection.Count > 0 ? (name.Length > 0 ? $"{name}, {Selection.Count} selected" : $"{Selection.Count} selected") : name;
        _nameLabel.Text = text;
        StatusLayout.Fit(_status, _nameLabel, _placeLabel);
    }

    /// <summary>Space in the navigator (AX-8.16): the row selected, or deselected again; on a row that cannot be selected, and in an app without a selection, the app's words or the edge sound.</summary>
    protected void SelectRow()
    {
        if (!Selection.Toggle())
        {
            NothingToSelect(SelectedRow());
        }
    }

    /// <summary>Shift+Space: every row from the nearest selected one to this one.</summary>
    protected void SelectToHere()
    {
        if (!Selection.ToHere())
        {
            NothingToSelect(SelectedRow());
        }
    }

    /// <summary>Ctrl+A: every row of the level that can be selected, "n selected".</summary>
    protected void SelectAllRows()
    {
        if (_markRows.Any(row => row.CanMark))
        {
            Selection.All();
        }
        else
        {
            NothingToSelect(null);
        }
    }

    /// <summary>Ctrl+Shift+A, and Escape while rows are selected: "None selected".</summary>
    protected void SelectNoRows() => Selection.None();

    /// <summary>The rows selected, in the navigator's order; none when nothing is selected.</summary>
    protected IReadOnlyList<ItemRow> SelectedRows() => Selection.Count == 0 ? [] : [.. _rows.Where(row => Selection.Contains(row.Key))];

    /// <summary>
    /// The row to select when rows leave the level (the selected ones archived, moved or hidden as done): the
    /// current row when it stays, otherwise the first after it that stays, otherwise the nearest before it.
    /// </summary>
    protected string? KeyAfter(IReadOnlyCollection<string> leaving)
    {
        bool Stays(ItemRow row) => !leaving.Contains(row.Key, StringComparer.OrdinalIgnoreCase);
        var at = Math.Max(Navigator.SelectedIndex, 0);
        for (var i = at; i < _rows.Count; i++)
        {
            if (Stays(_rows[i]))
            {
                return _rows[i].Key;
            }
        }

        for (var i = Math.Min(at, _rows.Count) - 1; i >= 0; i--)
        {
            if (Stays(_rows[i]))
            {
                return _rows[i].Key;
            }
        }

        return null;
    }

    /// <summary>F5, before the level is read again.</summary>
    protected virtual void BeforeRefresh()
    {
    }

    // ---- what the window base asks for ----

    protected override void HideForNotice() => _split.Visible = false;

    protected override void ShowAfterNotice() => _split.Visible = true;

    protected override Control FocusTarget => DetailPane.Focused ? DetailPane : TextPane.Focused ? TextPane : Navigator;

    /// <summary>
    /// What an AI tool's act command would act on (docs/axKit/mcp.md MC-7.3): the navigator's selected row by its name,
    /// told apart by the level it is in (<see cref="LevelIdentity"/>) and its key (an item's file), so that two items of
    /// one title in two lists are never one target. While rows are selected (AX-8.16) a command may act on them
    /// instead, so the target names both, "Fix the tap, and 3 selected", and changes when the selection does.
    /// </summary>
    protected override AxKit.Core.Mcp.RunTarget McpTarget()
    {
        if (SelectedRow() is not { } row)
        {
            return AxKit.Core.Mcp.RunTarget.None;
        }

        var identity = $"{LevelIdentity()}\n{row.Key}";
        return Selection.Count == 0
            ? new AxKit.Core.Mcp.RunTarget(row.Name, identity)
            : new AxKit.Core.Mcp.RunTarget($"{row.Name}, and {Selection.Count} selected", identity + "\n" + string.Join("\n", Selection.Keys.Order(StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// The level shown, for <see cref="McpTarget"/>: the status bar's place by default; an app whose place can change
    /// under a refill off the user's command (axTask's counts of found lists) names the level by its state instead, so
    /// that a Run is never refused for a selection that did not change.
    /// </summary>
    protected virtual string LevelIdentity() => _placeLabel.Text ?? "";

    protected override void ApplyFont(Font font)
    {
        Navigator.Font = font;
        DetailPane.Font = font;
        TextPane.Font = font;
    }

    /// <summary>Ctrl+S: every change is written at once.</summary>
    protected override void SaveCommand() => Announce("Nothing to save here: every change is written at once", true);

    protected override void OnShown(EventArgs e)
    {
        switch (Pane)
        {
            case SecondPane.Fields:
                _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Width * 45 / 100);
                break;
            case SecondPane.Text:
                // The text under the navigator, which has two fifths of the height (axGIT's layout, AG-2.1).
                _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Height * 2 / 5);
                break;
            case SecondPane.Log:
                // The app's list under the navigator, which has two thirds of the height (axFile's Favorites list, AF-9.1).
                _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Height * 2 / 3);
                break;
            default:
                // The navigator alone (framework step 4): the second panel is not there to see or to tab to.
                _split.Panel2Collapsed = true;
                break;
        }

        if (!NoticeOpen)
        {
            Navigator.Select();
        }

        base.OnShown(e);
    }

    /// <summary>
    /// The controls in their places with the app's menu bar, and the shared keys on the command list: called once at
    /// the end of the app's constructor, after its menus and its own commands are made, so that the app's keys come
    /// first (axPass's Enter unlocking a locked window before Enter opening).
    /// </summary>
    protected void PlaceControls(MenuStrip menu) => PlaceWith(menu);

    /// <summary>
    /// As <see cref="PlaceControls(MenuStrip)"/>, with further controls of the app's own docked between the panes
    /// and the status bar in the order given (axMedia's video area, at the top; framework step 4). The app hides
    /// and shows them itself around a notice (<see cref="HideForNotice"/>, <see cref="ShowAfterNotice"/>).
    /// </summary>
    protected void PlaceWith(MenuStrip menu, params Control[] between)
    {
        menu.MenuActivate += (_, _) => UpdateMenus();
        // Filter and Sort by begin the Options menu of every navigator (ItemForm.Arrange.cs).
        AddArrangeItems(menu);
        if (Pane == SecondPane.Text)
        {
            // The second pane as a text, under the navigator: a diff's lines are long, and a list of files is not.
            _split.Panel2.Controls.Clear();
            _split.Panel2.Controls.Add(TextPane);
            _split.Orientation = Orientation.Horizontal;
        }
        else if (Pane == SecondPane.Log)
        {
            // The second pane as a log, the detail pane's list filled by the app, under the navigator. Its
            // type-ahead goes over its own lines, there being no fields.
            _split.Orientation = Orientation.Horizontal;
            DetailPane.Names = null;
        }

        PlaceControls(menu, [_split, .. between, _status]);
        AddSharedKeys();
    }

    // ---- keys (IA-3): one command list (docs/axKit/app-kit.md AK-2), the app's own first, then ItemKeys here ----

    /// <summary>
    /// The rename box has its own keys (AX-8.7): Enter, Escape, Ctrl+Z on the typing. While it is open no command
    /// runs: a command that acts anywhere in the window has this as its <see cref="Command.When"/>, and one of a
    /// pane needs the pane focused, which it is not while the box has the focus.
    /// </summary>
    protected bool NotRenaming() => !Navigator.Renaming && !DetailPane.Renaming;

    /// <summary>Where a navigator command's key acts.</summary>
    protected bool InNavigator() => Navigator.Focused;

    /// <summary>Where a field command's key acts.</summary>
    protected bool InDetail() => DetailPane.Focused;

    /// <summary>The keys of <see cref="ItemKeys"/>; the apps' menu items for them only show them. In a pane, taking a key keeps the list box from its own move and its ding.</summary>
    private void AddSharedKeys()
    {
        // A shared key the app gave to a command of its own that acts anywhere is the app's: the shared command
        // would never run, and is not listed (framework step 4; the F5 case of axAutomate, made general).
        // A key the app has no use for (KeysWithoutUse) is no command of the window either. And a key the app gave
        // to a command of its own in the very place the shared command acts (the same condition: axFile's own
        // Enter, Delete and F2 with InNavigator) is the app's too (KeyTakenWhere).
        void Shared(Command command)
        {
            if (!KeyTakenWhere(command.Key, command.When) && !KeysWithoutUse.Contains(command.Key))
            {
                AddKey(command);
            }
        }

        // Find acts where the focus is (axGIT's AG-2.4): in the text pane's text while it has the focus, in the
        // navigator's rows otherwise. The text's three come first, so that they take the keys there.
        if (Pane == SecondPane.Text)
        {
            Shared(new Command("Find in the text", BundleKeys.Find, () => ShowFind(TextPane)) { When = InText, Mark = CommandMark.Read });
            Shared(new Command("Find next in the text", BundleKeys.FindNext, () => FindNext(TextPane, 1)) { When = InText, Mark = CommandMark.Read });
            Shared(new Command("Find previous in the text", BundleKeys.FindPrevious, () => FindNext(TextPane, -1)) { When = InText, Mark = CommandMark.Read });
        }

        Shared(new Command("Find in the list", BundleKeys.Find, () => ShowFindInList(Navigator, NavigatorLabels)) { When = NotRenaming, Mark = CommandMark.Read });
        Shared(new Command("Find next", BundleKeys.FindNext, () => FindInList(Navigator, NavigatorLabels, 1)) { When = NotRenaming, Mark = CommandMark.Read });
        Shared(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(Navigator, NavigatorLabels, -1)) { When = NotRenaming, Mark = CommandMark.Read });
        Shared(new Command("Undo", ItemKeys.Undo, Undo) { When = NotRenaming, Mark = CommandMark.Change });
        // Not where the app gave F5 to a command of its own (axAutomate runs with it, AU-D8): its menu's Refresh is the command there.
        if (!KeyTaken(ItemKeys.Refresh))
        {
            Shared(new Command("Refresh", ItemKeys.Refresh, () => Refresh(announce: true)) { When = NotRenaming, Mark = CommandMark.Read });
        }
        Shared(new Command("New list", ItemKeys.NewList, NewList) { When = NotRenaming, Mark = CommandMark.Change });
        Shared(new Command("Read in full", ItemKeys.ReadAll, ReadAll) { When = NotRenaming, Mark = CommandMark.Read });

        // The bundle's focus keys switch the panes from either one, as in every other window (AX-8.2; 2026-11-09, the
        // item windows had Tab and Shift+Tab alone, which stay each pane's own below).
        // In a navigator without a second pane (HasDetailPane, framework step 4) they come back to the list, the one
        // control, as in axMedia's window before it was a navigator.
        foreach (var key in new[] { BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            Shared(Pane switch
            {
                SecondPane.Fields => new Command("Switch between the navigator and the detail pane", key, () => (Navigator.Focused ? (Control)DetailPane : Navigator).Select()) { When = NotRenaming, Mark = CommandMark.Open },
                SecondPane.Text => new Command("Switch between the list and the text", key, () => (Navigator.Focused ? (Control)TextPane : Navigator).Select()) { When = NotRenaming, Mark = CommandMark.Open },
                SecondPane.Log => new Command($"Switch between the list and the {LogName}", key, () => (Navigator.Focused ? (Control)DetailPane : Navigator).Select()) { When = NotRenaming, Mark = CommandMark.Open },
                _ => new Command("Back to the list", key, () => Navigator.Select()) { When = NotRenaming, Mark = CommandMark.Open },
            });
        }

        // The navigator's keys.
        Shared(new Command("Into the list", ItemKeys.Into, Into) { When = InNavigator, Mark = CommandMark.Open });
        Shared(new Command("Up a level", ItemKeys.Up, UpLevel) { When = InNavigator, Mark = CommandMark.Open });
        Shared(new Command("Up a level", ItemKeys.UpAlso, UpLevel) { When = InNavigator, Mark = CommandMark.Open });
        Shared(new Command("Previous section", ItemKeys.PreviousSection, () => JumpSection(-1)) { When = InNavigator, Mark = CommandMark.Open });
        Shared(new Command("Next section", ItemKeys.NextSection, () => JumpSection(1)) { When = InNavigator, Mark = CommandMark.Open });
        // Enter opens the row's form where the app gives one (ItemForm.Form.cs), and does Enter's own work elsewhere.
        Shared(new Command("Open", ItemKeys.Open, OpenFormOfSelected) { When = InNavigator, Mark = CommandMark.Open });
        if (HasDetailPane)
        {
            Shared(new Command("To the detail pane", ItemKeys.OtherPane, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
            Shared(new Command("To the detail pane", ItemKeys.OtherPaneBack, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
        }
        else if (Pane == SecondPane.Text)
        {
            // The text pane's keys: Tab and Shift+Tab go to it and back, and Escape comes back from it.
            Shared(new Command("To the text", ItemKeys.OtherPane, () => TextPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
            Shared(new Command("To the text", ItemKeys.OtherPaneBack, () => TextPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.OtherPane, () => Navigator.Select()) { When = InText, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.OtherPaneBack, () => Navigator.Select()) { When = InText, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.Back, () => Navigator.Select()) { When = InText, Mark = CommandMark.Open });
        }
        else if (Pane == SecondPane.Log)
        {
            // The log pane's keys: Tab and Shift+Tab go to it and back, and Escape comes back from it. What Enter and
            // the other keys do on a line of the log is the app's, registered before these with InDetail.
            Shared(new Command("To the " + LogName, ItemKeys.OtherPane, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
            Shared(new Command("To the " + LogName, ItemKeys.OtherPaneBack, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.OtherPane, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.OtherPaneBack, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
            Shared(new Command("To the list", ItemKeys.Back, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
            Shared(new Command("The line's menu", ItemKeys.ItemMenu, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
            Shared(new Command("The line's menu", ItemKeys.ItemMenuAlso, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
        }

        // AX-8.16: Space selects, in an app whose navigator has a selection; the edge sound in one that has none.
        Shared(new Command("Select or deselect", SelectionKeys.Select, SelectRow) { When = InNavigator, Mark = CommandMark.Change });
        if (HasSelection)
        {
            Shared(new Command("Select to here", SelectionKeys.SelectTo, SelectToHere) { When = InNavigator, Mark = CommandMark.Change });
            foreach (var move in Selection.MoveCommands(RowNoun, InNavigator, () => NothingToSelect(null)))
            {
                Shared(move);
            }

            Shared(new Command("Select all", SelectionKeys.SelectAll, SelectAllRows) { When = InNavigator, Mark = CommandMark.Change });
            Shared(new Command("Deselect all", SelectionKeys.SelectNone, SelectNoRows) { When = InNavigator, Mark = CommandMark.Change });
            // Escape gives up a selection before anything else in the navigator; with none, the key is left alone.
            Shared(new Command("Deselect all", SelectionKeys.Clear, SelectNoRows) { When = () => InNavigator() && Selection.Count > 0, Mark = CommandMark.Change });
        }

        // Escape then gives up a filter (ItemForm.Arrange.cs).
        AddArrangeKeys(Shared);

        Shared(new Command("New item", ItemKeys.New, NewItem) { When = InNavigator, Mark = CommandMark.Change });
        Shared(new Command("Rename", ItemKeys.Rename, RenameSelected) { When = InNavigator, Mark = CommandMark.Change });
        Shared(new Command("Delete", ItemKeys.Delete, () => WithRow(DeleteRow)) { When = InNavigator, Mark = CommandMark.Act });
        Shared(new Command("The Context menu", ItemKeys.ItemMenu, OpenPaneMenu) { When = InNavigator, Mark = CommandMark.Open });
        Shared(new Command("The Context menu", ItemKeys.ItemMenuAlso, OpenPaneMenu) { When = InNavigator, Mark = CommandMark.Open });
        if (!HasDetailPane)
        {
            return;
        }

        // The detail pane's keys.
        Shared(new Command("Change the field", ItemKeys.Open, OpenFormAtField) { When = InDetail, Mark = CommandMark.Change });
        Shared(new Command("To the navigator", ItemKeys.OtherPane, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        Shared(new Command("To the navigator", ItemKeys.OtherPaneBack, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        Shared(new Command("To the navigator", ItemKeys.Back, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        Shared(new Command("Check the field", ItemKeys.Check, CheckField) { When = InDetail, Mark = CommandMark.Change });
        // Ctrl+N, F2 and Delete have no command in the detail pane since 2026-11-23 (NoFieldCommand): which fields an
        // item has is nowhere changed there any more.

        // Right and Left change the field in focus (IA-5.6, AX-8.10 rule 4): a choice's next and previous value, a
        // check checked and unchecked; the edge sound on every other field, never the list box's own move.
        Shared(new Command("The next value of the field", ItemKeys.Into, () => StepField(1)) { When = InDetail, Mark = CommandMark.Change });
        Shared(new Command("The previous value of the field", ItemKeys.Up, () => StepField(-1)) { When = InDetail, Mark = CommandMark.Change });
        Shared(new Command("The field's menu", ItemKeys.ItemMenu, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
        Shared(new Command("The field's menu", ItemKeys.ItemMenuAlso, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
    }

    /// <summary>A navigator command on the selected row; "Nothing selected" without one, or the one line of an empty level (<see cref="EmptyLine"/>).</summary>
    protected void WithRow(Action<ItemRow> command)
    {
        if (SelectedRow() is { } row)
        {
            command(row);
        }
        else
        {
            Announce(NoRowSaid(), true);
        }
    }

    /// <summary>
    /// What a command that needs a row says without one: the line you are on when it is no row (the one line of an
    /// empty level, <see cref="EmptyLine"/>; the line after the rows, <see cref="AfterRowsLine"/>), "Nothing
    /// selected" otherwise.
    /// </summary>
    protected string NoRowSaid()
    {
        var index = Navigator.SelectedIndex;
        return index >= _rows.Count && index < Navigator.Items.Count && Navigator.Items[index]?.ToString() is { Length: > 0 } line ? line : "Nothing selected";
    }

    // ---- what a navigator may do without (framework step 4, FW-4.7; 2026-11-22) ----

    /// <summary>The kinds of second pane a navigator has (docs/axKit/framework.md FW-4.7, point 2).</summary>
    protected enum SecondPane
    {
        /// <summary>The fields of the row you are on, a list (<see cref="DetailPane"/>): the item apps.</summary>
        Fields,

        /// <summary>A text to read, shown on the app's word (<see cref="TextPane"/>, <see cref="ShowText"/>): axGIT's diff and commit.</summary>
        Text,

        /// <summary>
        /// A list of lines the app keeps, under the navigator (axFile's Favorites list): the pane is
        /// <see cref="DetailPane"/>, a list the app fills itself and names; the pane keys go to it and back and
        /// Escape comes back; what a key does on a line is the app's command, registered with
        /// <see cref="InDetail"/> before the shared keys; Context holds <see cref="LogLineMenu"/> while the log was
        /// the last of the two to have the focus. No command of the fields is made.
        /// </summary>
        Log,

        /// <summary>None: the navigator fills the window (axMedia's queue, axProcess).</summary>
        None,
    }

    /// <summary>
    /// What the window's second pane is. The fields of the row you are on by default. With a text, the pane is a
    /// read-only text under the navigator that the app fills (<see cref="ShowText"/>): the pane keys go to it and
    /// back, Escape comes back from it, and find acts on it while it has the focus. With none, the navigator fills
    /// the window, and neither the pane keys nor the detail pane's commands are made.
    /// </summary>
    protected virtual SecondPane Pane => SecondPane.Fields;

    /// <summary>Whether the second pane is the fields of the row you are on.</summary>
    private bool HasDetailPane => Pane == SecondPane.Fields;

    /// <summary>The second pane as a text (<see cref="SecondPane.Text"/>): the bundle's read-only text, placed by <see cref="PlaceWith"/> in such a window alone.</summary>
    protected AxKitTextBox TextPane { get; } = new()
    {
        Multiline = true,
        ReadOnly = true,
        WordWrap = true,
        ScrollBars = ScrollBars.Vertical,
        HideSelection = false,
        MaxLength = 0,
        Dock = DockStyle.Fill,
        TabIndex = 1,
        AccessibleName = "Details",
    };

    /// <summary>Where a text pane's keys act.</summary>
    protected bool InText() => TextPane.Focused;

    /// <summary>
    /// A text into the text pane, named for what it is (from axGIT's details, AG-D5, on its move onto the type):
    /// the line ends made the control's, the caret on the first line. With <paramref name="focus"/> the focus goes
    /// there and the name and length are said after NVDA's own reading of the control, not interrupting it;
    /// without, they are said at once.
    /// </summary>
    protected void ShowText(string name, string text, bool focus)
    {
        var normalized = text.Replace("\r\n", "\n").TrimEnd('\n').Replace("\n", "\r\n");
        var lines = normalized.Length == 0 ? 0 : normalized.Split("\r\n").Length;
        TextPane.AccessibleName = name;
        TextPane.Text = normalized;
        TextPane.Select(0, 0);
        TextPane.ScrollToCaret();
        if (focus)
        {
            TextPane.Focus();
        }

        Announce($"{name}, {lines} {(lines == 1 ? "line" : "lines")}", !focus);
    }

    /// <summary>
    /// The navigator's keys the app has no use for, which are then no commands of its window: a key that could
    /// only say "not here" is left to do nothing (axMedia's queue has nothing to rename, to undo or to refresh,
    /// and no second list). None by default.
    /// </summary>
    protected virtual IReadOnlyCollection<Keys> KeysWithoutUse => [];

    /// <summary>
    /// The one line a level without rows shows, which is no row: nothing is selected on it, and the commands that
    /// need a row say it ("Queue is empty"). Null, the default, for an empty list, as the item apps have it.
    /// </summary>
    protected virtual string? EmptyLine => null;

    /// <summary>
    /// A line after the level's rows that is no row (axFile's "Not shown: 3 hidden", what a read left out): the
    /// arrows reach it, nothing is selected on it, and the commands that need a row say it. Null, the default, for
    /// none. It comes after the <see cref="EmptyLine"/> too.
    /// </summary>
    protected virtual string? AfterRowsLine => null;

    /// <summary>The menu of the log pane's line (<see cref="SecondPane.Log"/>), which Context holds while the log was the last of the two panes to have the focus; null for none.</summary>
    protected virtual ToolStripMenuItem? LogLineMenu => null;

    /// <summary>The app's words for its log pane (<see cref="SecondPane.Log"/>), in the names of the commands that go to it ("To the Favorites list"): "log" by default.</summary>
    protected virtual string LogName => "log";

    /// <summary>
    /// What the status bar's first part says in place of the level's name, when there is something of the moment
    /// to say there: by default the work that runs off the UI thread (<see cref="Working"/>: "Reading…"), and an app
    /// says more in its place (axMedia: what plays and where). Null for the level's name with its count.
    /// ", 3 selected" follows either. <see cref="ShowStatus"/> shows it again when it changed.
    /// </summary>
    protected virtual string? StatusState => Working;

    /// <summary>The status bar's first part shown again, for an app whose <see cref="StatusState"/> changed (a clock's tick).</summary>
    protected void ShowStatus() => ShowName();

    /// <summary>
    /// F2 in the navigator (AX-8.7): the bundle's rename box over the row with its name selected; Enter takes a name
    /// the app does not refuse, Escape keeps the old one.
    /// </summary>
    protected void RenameSelected() => WithRow(row =>
    {
        if (RenameRefused(row) is { } why)
        {
            Announce(why, true);
            return;
        }

        Navigator.BeginRename(row.Name, RenameSelectionLength(row), name =>
        {
            if (name == row.Name)
            {
                Navigator.EndRename();
                Announce("Name unchanged", true);
                return;
            }

            if (RefusedName(row, name) is { } refused)
            {
                Announce(refused, true);
                return;
            }

            Navigator.EndRename();
            RenameTo(row, name);
        });
    });

    /// <summary>
    /// A new item's title typed in the list itself (axTask TK-4.3; the maintainer's ask of 2026-11-22: "adding an item
    /// should just happen in line in the same way editing the title does with f2"): a row "New item" at the top or the
    /// bottom of the rows, the rename box over it, empty, named "Title of the new item". Enter with a title calls
    /// <paramref name="made"/>, which writes the item and fills the level; Escape, or the focus leaving the box, takes
    /// the row away again, and nothing was made.
    /// </summary>
    protected void NewItemInPlace(bool atTop, Action<string> made)
    {
        var before = Navigator.SelectedIndex;
        var index = atTop ? 0 : _rows.Count;
        Navigator.Items.Insert(index, "New item");
        Navigator.SelectedIndex = index;
        var taken = false;
        Navigator.BeginRename(string.Empty, 0, title =>
        {
            if (title.Length == 0)
            {
                Refuse("Type the item's title, or Escape");
                return;
            }

            taken = true;
            Navigator.EndRename();
            made(title);
        }, () =>
        {
            if (taken)
            {
                return;
            }

            if (index < Navigator.Items.Count && Navigator.Items[index] is "New item")
            {
                Navigator.Items.RemoveAt(index);
            }

            Navigator.SelectedIndex = Math.Min(before, Navigator.Items.Count - 1);
            Announce("No item made", true);
        }, "Title of the new item");
    }

    /// <summary>Shift+F10, the Applications key, a right click: the menu of the selection.</summary>
    protected void OpenPaneMenu()
    {
        UpdateMenus();
        OpenBarMenu(ContextBarMenu.Letter);
    }


    // ---- the levels (IA-2.2) ----

    /// <summary>Right: into a list; on an item, nothing but the edge sound (IA-3, axFile's Right on a file).</summary>
    protected void Into()
    {
        if (SelectedRow() is { IsSection: true })
        {
            OnSectionHeader();
        }
        else if (SelectedRow() is { IsList: true } row)
        {
            OpenList(row);
        }
        else
        {
            EventDoor.Raise(EventKind.Edge);
        }
    }

    /// <summary>Enter (IA-D2): a list opens, an item's fields take the focus.</summary>
    protected void OpenSelected()
    {
        switch (SelectedRow())
        {
            case null:
                Announce(_rows.Count == 0 ? EmptyLineNow ?? $"Nothing here: {BundleKeys.Text(ItemKeys.New)} adds an item" : "Nothing selected", true);
                return;
            case { IsSection: true }:
                OnSectionHeader();
                return;
            case { IsList: false } item:
                OpenItem(item);
                return;
            case var row:
                OpenList(row);
                return;
        }
    }

    /// <summary>
    /// Enter on an item, the one key the apps of the type differ in (docs/axKit/framework.md, section 9): its
    /// fields take the focus by default; an app without the second pane says what it does (axMedia plays the
    /// file), the edge sound otherwise.
    /// </summary>
    protected virtual void OpenItem(ItemRow row)
    {
        if (HasDetailPane)
        {
            DetailPane.Select();
        }
        else
        {
            EventDoor.Raise(EventKind.Edge);
        }
    }

    /// <summary>Right or Enter on a section's header (IA-2.3a): nothing to go into.</summary>
    private void OnSectionHeader()
    {
        EventDoor.Raise(EventKind.Edge);
        Announce("A section: Down goes into it", true);
    }

    /// <summary>
    /// Ctrl+Up, Ctrl+Down (IA-2.3a): the section header before or after the selection, as a screen reader's heading
    /// keys go; the edge sound past the first or the last. NVDA reads the header as the selection moves to it.
    /// </summary>
    protected void JumpSection(int direction)
    {
        var at = Navigator.SelectedIndex;
        for (var i = at + direction; i >= 0 && i < _rows.Count; i += direction)
        {
            if (_rows[i].IsSection)
            {
                // The user's move from wherever the command came (AxKitList.SelectForCommand): the heading sound too.
                Navigator.SelectForCommand(i);
                return;
            }
        }

        EventDoor.Raise(EventKind.NothingFound, EventSound.Edge);
    }

    private void OpenList(ItemRow row)
    {
        if (EntryRefused(row) is { } why)
        {
            // A list that cannot be gone into (axMCP's remote server, MP-1.4): the reason said, the level as it was.
            // An empty reason: the app has said or asked something itself (axTask's question before a list of the
            // earlier format is converted).
            if (why.Length > 0)
            {
                Announce(why, true);
            }

            return;
        }

        EnterList(row);
        Fill(select: null);
        AnnounceLevel();
    }

    /// <summary>
    /// Left, Backspace: up a level, the list left selected; at the top, the edge sound. An app whose level above
    /// is read off the UI thread makes it its own (axFile: the parent folder, read on a task).
    /// </summary>
    protected virtual void UpLevel()
    {
        if (LeaveList() is not { } select)
        {
            EventDoor.Raise(EventKind.Edge);
            return;
        }

        Fill(select);
        AnnounceLevel();
    }

    /// <summary>IA-2.4: "Home, 4 items, Fix the tap".</summary>
    protected void AnnounceLevel()
    {
        var name = Navigator.AccessibleName ?? string.Empty;
        Announce(SelectedRow() is { } row ? $"{name}, {row.Label}" : name, true);
    }

    /// <summary>F5: the level read again, the selection kept.</summary>
    protected void Refresh(bool announce)
    {
        BeforeRefresh();
        Fill(SelectedKey());
        if (announce)
        {
            Announce("Refreshed: " + Navigator.AccessibleName, true);
        }
    }

    /// <summary>
    /// The navigator filled for the level (<see cref="ReadLevel"/>), its name, the title and the status bar; the
    /// selection on the row whose key is <paramref name="select"/>, otherwise the first; then the detail pane.
    /// </summary>
    protected void Fill(string? select)
    {
        var (read, levelName, title, place) = ReadLevel();
        // The place first: it names the level for the marks and the filter where the app has no name of its own for it.
        _placeLabel.Text = place;
        // What of the level is shown, and in which order (ItemForm.Arrange.cs): the chosen order, then the filter.
        _rows = Arranged(read);
        var name = ArrangedName(levelName, _rows);
        KeepSelection();
        // S-42: titles from a shared folder or database with their hidden characters made visible, by Fill.
        // A level without rows shows the app's one line for it, when it has one (EmptyLine): no row, nothing selected.
        // After them, the app's line of what the rows leave out, when it has one (AfterRowsLine): no row either.
        Navigator.Fill(LevelLines(), name, select is null ? 0 : _rows.FindIndex(row => string.Equals(row.Key, select, StringComparison.OrdinalIgnoreCase)));
        SetLevelTitle(title);
        ShowName();
        FillDetail();
    }

    /// <summary>The navigator's lines for the level: its rows (or the one line of an empty level), then the line after the rows, each as its item shows it.</summary>
    private List<string> LevelLines()
    {
        List<string> lines = _rows.Count == 0 && EmptyLineNow is { } empty ? [empty] : [.. _rows.Select(RowText)];
        if (AfterRowsLine is { } after)
        {
            lines.Add(after);
        }

        return lines;
    }

    /// <summary>The window's title for the level; an empty one is the app's name alone (axMedia while nothing plays).</summary>
    private void SetLevelTitle(string title) => SetTitle(title.Length == 0 ? null : title);

    /// <summary>
    /// The status bar's second part alone, for a state that changes while the level stays (axTask's "Running" and
    /// an action's name, forms-framework.md AF-7.6): nothing read again, nothing said.
    /// </summary>
    protected void SetPlace(string place)
    {
        _placeLabel.Text = place;
        StatusLayout.Fit(_status, _nameLabel, _placeLabel);
    }

    /// <summary>
    /// The level read again for something that changed off the user's command (axTask's projects found, TK-7b.2): when
    /// the same rows are there by key, only the words of the rows that changed are rewritten in place and the selection
    /// stays, so that NVDA has nothing to read again; otherwise filled again on the row selected. Nothing said.
    /// </summary>
    protected void Refill()
    {
        var (read, levelName, title, place) = ReadLevel();
        // The rows as the list shows them, in the order and under the filter there are (ItemForm.Arrange.cs).
        var rows = Arranged(read);
        var name = ArrangedName(levelName, rows);
        if (!rows.Select(row => row.Key).SequenceEqual(_rows.Select(row => row.Key), StringComparer.OrdinalIgnoreCase))
        {
            Fill(SelectedKey());
            return;
        }

        if (rows.SequenceEqual(_rows) && name == Navigator.AccessibleName)
        {
            return;
        }

        if (rows.Count == 0 && EmptyLineNow is not null)
        {
            // Still without rows, under another name: the one line of the empty level stays (Fill makes it).
            Fill(select: null);
            return;
        }

        var selectedBefore = SelectedRow();
        _rows = [.. rows];
        _placeLabel.Text = place;
        // The same rows by key, so the same marks; a row that can no longer be selected loses its.
        KeepSelection();
        Navigator.SetLines(LevelLines().Select(DisplayText.Visible).ToList());
        if (Navigator.AccessibleName != name)
        {
            Navigator.AccessibleName = name;
        }

        SetLevelTitle(title);
        ShowName();
        // The detail pane is made again only for a selected row whose words changed, and never under the user's focus
        // in it: a refill of other rows leaves the field the user is on alone.
        if (SelectedRow() != selectedBefore)
        {
            if (DetailPane.Focused)
            {
                // Made again, silently, once the user is back in the navigator.
                _detailStale = true;
            }
            else
            {
                FillDetail();
            }
        }
    }

    /// <summary>The selected row's key, for <see cref="Fill"/>.</summary>
    protected string? SelectedKey() => SelectedRow()?.Key;

    protected ItemRow? SelectedRow() => Navigator.SelectedIndex is var i && i >= 0 && i < _rows.Count ? _rows[i] : null;

    /// <summary>The row to select when the selected one leaves the level: the next, or the one before at the end.</summary>
    protected string? NeighbourKey()
    {
        var i = Navigator.SelectedIndex;
        return i + 1 < _rows.Count ? _rows[i + 1].Key : i - 1 >= 0 ? _rows[i - 1].Key : null;
    }

    private IReadOnlyList<string> NavigatorLabels() => _rows.Select(row => row.Label).ToList();

    /// <summary>"1 item", "3 items".</summary>
    protected static string Count(int count, string noun) => MenuText.Count(count, noun);

    // ---- the detail pane (IA-2.3) ----

    /// <summary>The detail pane filled for the navigator's selection, silently; the field selected before kept when the same thing shows.</summary>
    protected void FillDetail()
    {
        if (!HasDetailPane)
        {
            return;
        }

        _detailStale = false;
        var keep = DetailPane.SelectedIndex;
        var detail = DetailOf(SelectedRow());
        var same = detail.Key is not null && string.Equals(detail.Key, _shown.Key, StringComparison.OrdinalIgnoreCase);
        _shown = detail;
        var lines = new List<(int Field, ItemDetailLine Line)>();
        if (detail.Document is { } document)
        {
            for (var i = 0; i < document.Fields.Count; i++)
            {
                lines.Add((i, new ItemDetailLine(FieldLabel(document.Fields[i]))));
            }
        }

        lines.AddRange(detail.Lines.Select(line => (-1, line)));
        _lines = lines;
        DetailPane.BeginUpdate();
        DetailPane.Items.Clear();
        DetailPane.Items.AddRange(lines.Select(line => (object)DisplayText.Visible(line.Line.Label)).ToArray());
        DetailPane.EndUpdate();
        DetailPane.AccessibleName = detail.Name;
        DetailPane.ResetTypeAhead();
        if (lines.Count > 0)
        {
            DetailPane.SelectedIndex = same ? Math.Clamp(keep, 0, lines.Count - 1) : 0;
        }
    }

    /// <summary>The detail pane's selection on field <paramref name="field"/>, after a change.</summary>
    protected void SelectField(int field)
    {
        var line = _lines.FindIndex(row => row.Field == field);
        if (line >= 0)
        {
            DetailPane.SelectedIndex = line;
        }
    }

    /// <summary>"Due, date, 30 October 2026, Friday" (IA-3.1).</summary>
    protected string FieldLabel(ItemField field) =>
        $"{(field.Title.Length == 0 ? "untitled" : field.Title)}, {SpokenType(field.Type)}, {SpokenValue(SelectedRow(), field)}";

    /// <summary>The field the detail pane's selection is on; null on another line.</summary>
    protected int? SelectedField() =>
        DetailPane.SelectedIndex is var i && i >= 0 && i < _lines.Count && _lines[i].Field >= 0 && _shown.Document is { } document && _lines[i].Field < document.Fields.Count
            ? _lines[i].Field
            : null;

    private ItemDetailLine? SelectedLine() => DetailPane.SelectedIndex is var i && i >= 0 && i < _lines.Count ? _lines[i].Line : null;
}
