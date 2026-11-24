using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>A list's own order and where its new items go (SPEC.md TK-4.9, TK-4.10).</summary>
public sealed class ListOrderTests
{
    private static void AssertMoved(string[] names, int at, (List<string> Names, int At)? moved)
    {
        Assert.NotNull(moved);
        Assert.Equal(names, moved.Value.Names);
        Assert.Equal(at, moved.Value.At);
    }

    private static ListConfig Config(string extra = "") =>
        ListConfig.Parse($$"""{"format": "axkit-list 2", "item fields": []{{extra}}}""");

    [Fact]
    public void A_list_without_an_order_keeps_the_items_as_given()
    {
        Assert.Empty(ListOrder.Of(Config()));
        Assert.Equal(["a", "b"], ListOrder.Arrange(["a", "b"], name => name, [], newAtTop: false));
    }

    [Fact]
    public void The_order_puts_named_items_first_and_the_others_where_new_items_go()
    {
        var config = Config(""", "order": ["c", "A", 3], "new items": "top" """);
        Assert.Equal(["c", "A"], ListOrder.Of(config));
        Assert.True(ListOrder.NewAtTop(config));
        Assert.Equal(["c", "a", "b", "d"], ListOrder.Arrange(["a", "b", "c", "d"], name => name, ["c", "a"], newAtTop: false));
        Assert.Equal(["b", "d", "c", "a"], ListOrder.Arrange(["a", "b", "c", "d"], name => name, ["c", "a"], newAtTop: true));
    }

    [Fact]
    public void A_move_goes_one_place_or_to_an_end_and_not_past_it()
    {
        AssertMoved(["b", "a", "c"], 0, ListOrder.Moved(["a", "b", "c"], 1, -1));
        AssertMoved(["b", "c", "a"], 2, ListOrder.Moved(["a", "b", "c"], 0, int.MaxValue));
        AssertMoved(["c", "a", "b"], 0, ListOrder.Moved(["a", "b", "c"], 2, int.MinValue));
        Assert.Null(ListOrder.Moved(["a", "b", "c"], 0, -1));
        Assert.Null(ListOrder.Moved(["a", "b", "c"], 2, 1));
        // Among the shown, done tasks hidden: x and y are hidden between and after.
        AssertMoved(["b", "a", "x", "c", "y"], 0, ListOrder.MovedAmong(["a", "x", "b", "c", "y"], ["a", "b", "c"], 1, -1));
        AssertMoved(["a", "x", "c", "b", "y"], 2, ListOrder.MovedAmong(["a", "x", "b", "c", "y"], ["a", "b", "c"], 1, 1));
        Assert.Equal("Moved up: Fix the tap, 3 of 12", ListOrder.Said(-1, "Fix the tap", 2, 12));
        Assert.Equal("Moved to the bottom: Fix the tap, 12 of 12", ListOrder.Said(int.MaxValue, "Fix the tap", 11, 12));
    }

    [Fact]
    public void A_renamed_item_keeps_its_place_and_a_new_one_goes_to_its_end()
    {
        Assert.Equal(["a", "x", "c"], ListOrder.Renamed(["a", "B", "c"], "b", "x"));
        Assert.Null(ListOrder.Renamed(["a"], "b", "x"));
        Assert.Null(ListOrder.Added([], ["a", "b"], "n", atTop: false));
        Assert.Equal(["n", "a", "b"], ListOrder.Added([], ["a", "b"], "n", atTop: true));
        Assert.Equal(["a", "n"], ListOrder.Added(["a"], ["a"], "n", atTop: false));
        Assert.Equal(["n", "a"], ListOrder.Added(["a"], ["a"], "n", atTop: true));
    }

    [Fact]
    public void The_order_and_the_setting_are_written_into_the_configuration()
    {
        var written = ListOrder.WithNewItems(ListOrder.WithOrder(Config(), ["b", "a"]), top: true);
        Assert.Equal(["b", "a"], ListOrder.Of(ListConfig.Parse(written.ToText())));
        Assert.True(ListOrder.NewAtTop(ListConfig.Parse(written.ToText())));
        Assert.False(ListOrder.NewAtTop(ListOrder.WithNewItems(written, top: false)));
    }
}
