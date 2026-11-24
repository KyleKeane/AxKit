using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The filter and the sort of a navigator's list (docs/axKit/framework.md FW-4.10a): which rows are shown, in which order, and the list's name under a filter.</summary>
public sealed class ListArrangeTests
{
    private static ArrangeRow Item(string name) => new(name);

    private static ArrangeRow Folder(string name) => new(name, IsList: true);

    private static ArrangeRow Heading(string name) => new(name, IsHeading: true);

    private static string[] Names(IReadOnlyList<ArrangeRow> rows, IReadOnlyList<int> order) => [.. order.Select(index => rows[index].Name)];

    [Fact]
    public void By_name_puts_lists_first_and_counts_as_people_do()
    {
        ArrangeRow[] rows = [Item("b 10.txt"), Item("b 2.txt"), Folder("zeta"), Item("Alpha.txt"), Folder("docs")];
        Assert.Equal(["docs", "zeta", "Alpha.txt", "b 2.txt", "b 10.txt"], Names(rows, ListArrange.ByName(rows)));
    }

    [Fact]
    public void By_name_leaves_each_heading_in_its_place_and_orders_the_rows_under_it()
    {
        ArrangeRow[] rows = [Item("loose"), Heading("Your lists"), Item("Work"), Item("Home"), Heading("Smart lists"), Item("Today"), Item("All tasks")];
        Assert.Equal(["loose", "Your lists", "Home", "Work", "Smart lists", "All tasks", "Today"], Names(rows, ListArrange.ByName(rows)));
    }

    [Fact]
    public void The_filter_keeps_the_names_that_contain_the_text_in_any_case_and_in_their_order()
    {
        ArrangeRow[] rows = [Item("Chapter 1.mp3"), Item("notes.md"), Folder("chap")];
        Assert.Equal(["Chapter 1.mp3", "chap"], Names(rows, ListArrange.Filter(rows, "CHAP")));
        Assert.Equal(3, ListArrange.Filter(rows, string.Empty).Count);
        Assert.Empty(ListArrange.Filter(rows, "zzz"));
    }

    [Fact]
    public void A_star_or_a_question_mark_makes_the_filter_a_pattern_over_the_whole_name()
    {
        ArrangeRow[] rows = [Item("Chapter 1.mp3"), Item("Chapter 12.mp3"), Item("notes.md"), Item("mp3 list.txt")];
        Assert.Equal(["Chapter 1.mp3", "Chapter 12.mp3"], Names(rows, ListArrange.Filter(rows, "*.MP3")));
        Assert.Equal(["Chapter 1.mp3"], Names(rows, ListArrange.Filter(rows, "chapter ?.mp3")));
        Assert.Equal(["Chapter 1.mp3", "Chapter 12.mp3", "mp3 list.txt"], Names(rows, ListArrange.Filter(rows, "mp3")));
    }

    [Fact]
    public void A_heading_stays_while_a_row_under_it_passes_and_its_own_name_is_not_looked_at()
    {
        ArrangeRow[] rows = [Heading("Your lists"), Item("Shopping"), Item("Home"), Heading("Shop lists"), Item("Today"), Heading("Date lists"), Item("Shop hours")];
        Assert.Equal(["Your lists", "Shopping", "Date lists", "Shop hours"], Names(rows, ListArrange.Filter(rows, "shop")));
    }

    [Fact]
    public void The_list_is_named_with_the_filter_and_how_many_rows_it_shows()
    {
        Assert.Equal("Home, 4 items", ListArrange.Name("Home, 4 items", string.Empty, 4));
        Assert.Equal("Home, 4 items, filter tap, 1 shown", ListArrange.Name("Home, 4 items", "tap", 1));
        Assert.Equal("24 items, filter zzz, 0 shown", ListArrange.Name("24 items", "zzz", 0));
        Assert.Equal("No names contain zzz", ListArrange.NoMatch("zzz"));
    }
}
