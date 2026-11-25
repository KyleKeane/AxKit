using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>A date and a !priority at the end of a new item's title fill Due and Priority (SPEC.md TK-4.3c).</summary>
public class TitleWordsTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    private static readonly IReadOnlyList<ItemField> Fields =
    [
        new(TaskRules.DoneField, FieldTypes.Check, JsonValue.Create(false), []),
        new(TaskRules.DueField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
        new(TitleWords.PriorityField, FieldTypes.Choice, JsonValue.Create(string.Empty), ["High", "Normal", "Low"]),
    ];

    private static string Value(TitleWords.Taken taken, string field) =>
        taken.Values.Where(pair => pair.Key == field).Select(pair => FieldTypes.TextOf(pair.Value)).SingleOrDefault() ?? string.Empty;

    [Fact]
    public void A_date_and_a_priority_at_the_end_fill_the_fields_and_leave_the_title()
    {
        var taken = TitleWords.Read("Buy milk tomorrow !high", Fields, Today);
        Assert.Equal("Buy milk", taken.Title);
        Assert.Equal("2026-11-26", Value(taken, "Due"));
        Assert.Equal("High", Value(taken, "Priority"));
        Assert.Equal(", due 26 November, priority High", taken.Said);
    }

    [Theory]
    [InlineData("Call the bank +3", "Call the bank", "2026-11-28", "")]
    [InlineData("Pay rent 2026-12-01", "Pay rent", "2026-12-01", "")]
    [InlineData("Send it !l", "Send it", "", "Low")]
    [InlineData("Send it !LOW friday", "Send it", "2026-11-27", "Low")]
    public void Each_kind_is_read_in_any_order(string typed, string title, string due, string priority)
    {
        var taken = TitleWords.Read(typed, Fields, Today);
        Assert.Equal(title, taken.Title);
        Assert.Equal(due, Value(taken, "Due"));
        Assert.Equal(priority, Value(taken, "Priority"));
    }

    [Theory]
    [InlineData("Read chapter 2")]
    [InlineData("Tomorrow")]
    [InlineData("Shout !urgent")]
    [InlineData("Plan tomorrow tomorrow")]
    public void Words_that_name_nothing_or_would_empty_the_title_stay(string typed)
    {
        var taken = TitleWords.Read(typed, Fields, Today);
        Assert.StartsWith(typed.Split(' ')[0], taken.Title);
        if (typed == "Plan tomorrow tomorrow")
        {
            // One date only: the second "tomorrow" stays in the title.
            Assert.Equal("Plan tomorrow", taken.Title);
        }
        else
        {
            Assert.Equal(typed, taken.Title);
            Assert.Empty(taken.Values);
        }
    }

    [Fact]
    public void A_list_without_the_fields_takes_nothing()
    {
        var taken = TitleWords.Read("Buy milk tomorrow !high", [new(TaskRules.DoneField, FieldTypes.Check, JsonValue.Create(false), [])], Today);
        Assert.Equal("Buy milk tomorrow !high", taken.Title);
        Assert.Empty(taken.Values);
        Assert.Equal(string.Empty, taken.Said);
    }
}
