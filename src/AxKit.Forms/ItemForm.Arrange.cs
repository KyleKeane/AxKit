using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The item window's filter and sort (docs/axKit/framework.md FW-4.10a; the maintainer, 2026-11-22: "i want any
/// list to be able to be filtered or sorted"; from axFile's window, AF-3.8, and axProcess's Sort processes by). In
/// every navigator the Options menu begins with Filter and Sort by, which the item window puts there itself, so
/// that no app can forget them; neither has a key (the maintainer's choice of the same day), and both are in the
/// command palette. The filter shows only the rows whose name contains a text; the sort puts the rows in one of
/// the orders the app names. What the level's rows are is still the app's (<see cref="ReadLevel"/>); what of them
/// is shown, and in which order, is decided here, between the read and the list.
/// </summary>
public abstract partial class ItemForm
{
    /// <summary>The filter text, empty for none; it lives in one level and is dropped with it.</summary>
    private string _filter = string.Empty;

    /// <summary>The level the filter was set in (<see cref="LevelIdentity"/>).</summary>
    private string? _filterLevel;

    /// <summary>How many rows the level has before the filter, its headings not counted: with none, the one line of an empty level is the app's, not the filter's.</summary>
    private int _unfiltered;

    private ChoiceItems<int>? _sortItems;

    private int _sortChoice;

    /// <summary>
    /// The orders the list can be put in, by the words the menu and the announcement use ("name", "date"), the
    /// first being the one a window starts in. By default the level's own order, as the app gives its rows, and
    /// by name. An app with orders of its own names them (axFile: name, date, size) and gives its rows in
    /// the chosen one (<see cref="OrdersRowsItself"/>).
    /// </summary>
    protected virtual IReadOnlyList<string> SortNames => ["the list's own order", "name"];

    /// <summary>Which of <see cref="SortNames"/> is chosen. Kept for the window by default; an app with orders of its own keeps it in its settings.</summary>
    protected virtual int SortChoice
    {
        get => _sortChoice;
        set => _sortChoice = value;
    }

    /// <summary>Whether <see cref="ReadLevel"/> gives the rows in the chosen order itself, as an app with orders of its own does; otherwise the item window orders them.</summary>
    protected virtual bool OrdersRowsItself => false;

    /// <summary>Another order was chosen, before the list is filled again: an app that keeps its rows in order puts them in the new one here.</summary>
    protected virtual void SortChanged()
    {
    }

    /// <summary>The one line of a level whose rows all fail the filter (axFile's AF-3.8); null when the filter is off or the level has no rows of its own.</summary>
    private string? NoMatchLine => _filter.Length > 0 && _unfiltered > 0 ? ListArrange.NoMatch(_filter) : null;

    /// <summary>The one line a level without rows to show has: the filter's, or else the app's (<see cref="EmptyLine"/>).</summary>
    private string? EmptyLineNow => NoMatchLine ?? EmptyLine;

    /// <summary>
    /// The level's rows as the list shows them: in the chosen order, then those that pass the filter
    /// (<see cref="ListArrange"/>, which holds the rules: by name with lists first, a heading kept in its place
    /// and kept while a row under it passes). Another level than the one the filter was set in starts without
    /// it; called once the level's place is known.
    /// </summary>
    private List<ItemRow> Arranged(IReadOnlyList<ItemRow> rows)
    {
        var level = LevelIdentity();
        if (level != _filterLevel)
        {
            _filter = string.Empty;
            _filterLevel = level;
        }

        _unfiltered = rows.Count(row => !row.IsSection);
        List<ItemRow> ordered = !OrdersRowsItself && SortChoice == 1 ? Picked(rows, ListArrange.ByName(AsArrangeRows(rows))) : [.. rows];
        return _filter.Length == 0 ? ordered : Picked(ordered, ListArrange.Filter(AsArrangeRows(ordered), _filter));
    }

    private static List<ArrangeRow> AsArrangeRows(IReadOnlyList<ItemRow> rows) =>
        [.. rows.Select(row => new ArrangeRow(row.Name, row.IsList, row.IsSection))];

    private static List<ItemRow> Picked(IReadOnlyList<ItemRow> rows, IReadOnlyList<int> positions) =>
        [.. positions.Select(position => rows[position])];

    /// <summary>The list's name for what is shown: the level's, and under a filter the text and how many rows pass it, "Home, 4 items, filter tap, 1 shown".</summary>
    private string ArrangedName(string name, List<ItemRow> rows) =>
        ListArrange.Name(name, _filter, rows.Count(row => !row.IsSection));

    /// <summary>
    /// Filter and Sort by at the top of the window's Options menu, with a line under them when the menu has more:
    /// put there by the item window as the controls are placed, after the app has made its bar, so that every
    /// navigator has them in the same place under the same words.
    /// </summary>
    private void AddArrangeItems(MenuStrip bar)
    {
        if (bar.Items.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Text == BarMenus[4]) is not { } options)
        {
            Log.Error("The item window found no Options menu to put Filter and Sort by in: the bar is placed before the controls.");
            return;
        }

        var own = options.DropDownItems.Count;
        var filter = Add(options, new Command("&Filter...", Keys.None, ShowFilter) { Mark = CommandMark.Change });
        options.DropDownItems.Remove(filter);
        options.DropDownItems.Insert(0, filter);
        var placed = 1;
        // Sort by, where the list has two orders or more: a list with one order alone has no such menu.
        var names = SortNames;
        if (names.Count > 1)
        {
            var sort = new ToolStripMenuItem("&Sort by");
            _sortItems = new ChoiceItems<int>(sort, names.Select((name, index) => (index, "&" + char.ToUpperInvariant(name[0]) + name[1..])), SortChoice, SetSort);
            options.DropDownItems.Insert(placed++, sort);
        }

        if (own > 0)
        {
            options.DropDownItems.Insert(placed, new ToolStripSeparator());
        }
    }

    /// <summary>Escape in the navigator with a filter on and no row selected clears the filter (AF-3.8): after the selection, before anything else.</summary>
    private void AddArrangeKeys(Action<Command> shared) =>
        shared(new Command("Clear the filter", ItemKeys.Back, () => ApplyFilter(string.Empty)) { When = () => InNavigator() && Selection.Count == 0 && _filter.Length > 0, Mark = CommandMark.Change });

    /// <summary>Options → Filter…: the notice "Filter" with the field "Name contains"; an empty field clears the filter.</summary>
    private void ShowFilter() =>
        AskText("Filter", "Only what has the text in its name is listed; with * or ? the whole name is matched as a pattern, *.mp3 for instance; empty clears the filter.", "Name contains:", "Name contains", _filter, "&Filter",
            typed => ApplyFilter(typed.Trim()), required: false, emptyMessage: string.Empty);

    /// <summary>
    /// The filter set and the list made again, the row kept when it still shows; the list's new name and the row
    /// are said, "24 items, filter chap, 3 shown, Chapter 1.mp3", or "Filter cleared" when it is emptied.
    /// </summary>
    private void ApplyFilter(string text)
    {
        var had = _filter.Length > 0;
        _filter = text;
        _filterLevel = LevelIdentity();
        Fill(SelectedKey());
        if (text.Length == 0)
        {
            if (had)
            {
                Announce("Filter cleared", true);
            }

            return;
        }

        Announce($"{Navigator.AccessibleName}, {Navigator.SelectedItem}", true);
    }

    /// <summary>Options → Sort by → one order: the choice kept, the checked item, the list made again with the row kept; "Sorted by date".</summary>
    private void SetSort(int choice)
    {
        SortChoice = choice;
        _sortItems?.Check(choice);
        SortChanged();
        Fill(SelectedKey());
        Announce("Sorted by " + SortNames[choice], true);
    }
}
