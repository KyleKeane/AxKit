using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The marks on a list's rows (docs/axKit/SPEC.md AX-8.16; axFile's AF-3.7): each key's change and its words.</summary>
public sealed class MarksTests
{
    private static readonly IReadOnlyList<MarkRow> Shown = Entries("a.txt", "b.txt", "c.txt", "d.txt", "e.txt");

    private static IReadOnlyList<MarkRow> Entries(params string[] names) =>
        names.Select(name => new MarkRow("C:\\x\\" + name, name)).ToList();

    [Fact]
    public void Space_marks_and_unmarks_the_entry_with_the_count_when_more_are_marked()
    {
        var marks = new Marks();
        var first = marks.Toggle(Shown, 1);
        Assert.Equal([1], first.Changed);
        Assert.Equal("Selected", first.Said);
        Assert.True(marks.Contains("C:\\x\\b.txt"));

        var second = marks.Toggle(Shown, 3);
        Assert.Equal("Selected, 2 selected", second.Said);

        var off = marks.Toggle(Shown, 1);
        Assert.Equal("Deselected, 1 selected", off.Said);
        Assert.Equal(1, marks.Count);
        Assert.Equal("Deselected", marks.Toggle(Shown, 3).Said);
        Assert.Equal(0, marks.Count);
    }

    [Fact]
    public void Select_all_and_deselect_all_change_only_the_rows_whose_mark_flips()
    {
        var marks = new Marks();
        var all = marks.All(Shown);
        Assert.Equal([0, 1, 2, 3, 4], all.Changed);
        Assert.Equal("5 selected", all.Said);

        var again = marks.All(Shown);
        Assert.Empty(again.Changed);
        Assert.Equal("5 selected", again.Said);

        var none = marks.None(Shown);
        Assert.Equal([0, 1, 2, 3, 4], none.Changed);
        Assert.Equal("None selected", none.Said);
        Assert.Empty(marks.None(Shown).Changed);
    }

    [Fact]
    public void Shift_space_marks_from_the_nearest_marked_entry_and_is_space_without_one()
    {
        var marks = new Marks();
        marks.Toggle(Shown, 1);
        var toHere = marks.ToHere(Shown, 4);
        Assert.Equal([2, 3, 4], toHere.Changed);
        Assert.Equal("Selected, 4 selected", toHere.Said);

        // Both as near: the one above.
        var even = new Marks();
        even.Toggle(Shown, 0);
        even.Toggle(Shown, 4);
        Assert.Equal([1, 2], even.ToHere(Shown, 2).Changed);

        // No other entry marked: Space, so on the one marked entry it unmarks it again (decided 2026-10-23).
        var alone = new Marks();
        Assert.Equal("Selected", alone.ToHere(Shown, 2).Said);
        Assert.Equal("Deselected", alone.ToHere(Shown, 2).Said);
        Assert.Equal(0, alone.Count);
    }

    [Fact]
    public void A_shift_range_begins_at_the_current_entry_keeps_the_marks_before_it_and_shrinks_back()
    {
        var marks = new Marks();
        marks.Toggle(Shown, 0);
        var begin = marks.Extend(Shown, 2, 3);
        Assert.Equal([2, 3], begin.Changed);
        Assert.Equal(string.Empty, begin.Said);

        // The range goes on from where it reached.
        Assert.Equal([4], marks.Extend(Shown, 3, 4).Changed);

        // Back: it shrinks, and the mark made before it stays.
        Assert.Equal([3, 4], marks.Extend(Shown, 4, 2).Changed);
        Assert.Equal(2, marks.Count);
        Assert.True(marks.Contains("C:\\x\\a.txt"));

        // The current entry moved without Shift: the next Shift key begins a new range from there, over the marks there are.
        Assert.Equal([1], marks.Extend(Shown, 1, 0).Changed);
        Assert.Equal(3, marks.Count);

        // Space ends the range: the next Shift key, even from where the range had reached, begins a new one, which
        // keeps every mark there is. Had the range gone on, it would have dropped the mark Space made.
        marks.Toggle(Shown, 4);
        Assert.Empty(marks.Extend(Shown, 0, 1).Changed);
        Assert.Equal(4, marks.Count);
        Assert.True(marks.Contains("C:\\x\\e.txt"));
    }

    [Fact]
    public void The_marks_on_entries_the_list_no_longer_shows_are_gone_and_the_range_ends()
    {
        var marks = new Marks();
        marks.All(Shown);
        marks.Extend(Shown, 0, 1);
        marks.Keep(Entries("a.txt", "c.txt"));
        Assert.Equal(2, marks.Count);
        Assert.False(marks.Contains("C:\\x\\b.txt"));

        marks.Clear();
        Assert.Equal(0, marks.Count);
    }

    [Fact]
    public void A_row_that_cannot_be_marked_is_passed_over_by_the_keys_that_select_many()
    {
        // A heading between the items, as in an item window's navigator.
        IReadOnlyList<MarkRow> rows = [new("a", "A"), new("h", "Heading", CanMark: false), new("b", "B"), new("c", "C")];
        var marks = new Marks();
        var all = marks.All(rows);
        Assert.Equal([0, 2, 3], all.Changed);
        Assert.Equal("3 selected", all.Said);
        Assert.False(marks.Contains("h"));

        marks.Clear();
        var range = marks.Extend(rows, 0, 2);
        Assert.Equal([0, 2], range.Changed);
        Assert.False(marks.Contains("h"));

        // Onto the heading itself: the current row moves there, and nothing more is marked.
        marks.Clear();
        Assert.Equal([0], marks.Extend(rows, 0, 1).Changed);

        marks.Clear();
        marks.Toggle(rows, 0);
        var toHere = marks.ToHere(rows, 3);
        Assert.Equal([2, 3], toHere.Changed);
        Assert.Equal("Selected, 3 selected", toHere.Said);

        // A row that could be marked before and cannot now loses its mark with the next fill.
        marks.Keep([new("a", "A", CanMark: false), new("b", "B")]);
        Assert.Equal(1, marks.Count);
        Assert.True(marks.Contains("b"));
    }
}
