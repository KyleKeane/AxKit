using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxKit.Tests;

public class FieldTypesTests
{
    // A Saturday.
    private static readonly DateOnly Today = new(2026, 10, 24);

    [Theory]
    [InlineData("2026-10-30", "2026-10-30")]
    [InlineData("today", "2026-10-24")]
    [InlineData("Tomorrow", "2026-10-25")]
    [InlineData("yesterday", "2026-10-23")]
    [InlineData("+3", "2026-10-27")]
    [InlineData("-1", "2026-10-23")]
    [InlineData("friday", "2026-10-30")]
    [InlineData("saturday", "2026-10-31")]
    [InlineData("30 October 2026", "2026-10-30")]
    [InlineData("3 Nov 2026", "2026-11-03")]
    [InlineData("1 December", "2026-12-01")]
    [InlineData("", "")]
    public void TakesADate(string typed, string stored)
    {
        var parsed = FieldTypes.Parse(FieldTypes.Date, typed, [], Today);
        Assert.Null(parsed.Refused);
        Assert.Equal(stored, FieldTypes.TextOf(parsed.Value));
    }

    /// <summary>IA-4.2a: a day and a month without a year is the next such day, today included; never a day that has passed.</summary>
    [Theory]
    [InlineData("2026-12-28", "5 January", "2027-01-05")]
    [InlineData("2026-12-28", "January 5", "2027-01-05")]
    [InlineData("2026-12-28", "28 December", "2026-12-28")]
    [InlineData("2026-12-28", "27 December", "2027-12-27")]
    [InlineData("2026-12-28", "31 Dec", "2026-12-31")]
    [InlineData("2026-10-24", "29 February", "2028-02-29")]
    [InlineData("2028-02-29", "29 February", "2028-02-29")]
    [InlineData("2028-03-01", "February 29", "2032-02-29")]
    public void A_day_and_month_without_a_year_is_the_next_such_day(string today, string typed, string stored)
    {
        var parsed = FieldTypes.Parse(FieldTypes.Date, typed, [], DateOnly.ParseExact(today, "yyyy-MM-dd"));
        Assert.Null(parsed.Refused);
        Assert.Equal(stored, FieldTypes.TextOf(parsed.Value));
    }

    [Theory]
    [InlineData("30 Febuary")]
    [InlineData("30 February")]
    [InlineData("31 April")]
    [InlineData("2026-02-30")]
    [InlineData("soon")]
    public void RefusesWhatIsNotADate(string typed) =>
        Assert.StartsWith("Not a date", FieldTypes.Parse(FieldTypes.Date, typed, [], Today).Refused);

    [Fact]
    public void TakesADateAndTime()
    {
        Assert.Equal("2026-10-25T14:00", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "tomorrow 14:00", [], Today).Value));
        Assert.Equal("2026-10-30T09:30", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "2026-10-30 9:30", [], Today).Value));
        Assert.Equal("2026-10-30T00:00", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.DateTime, "2026-10-30", [], Today).Value));
    }

    [Fact]
    public void TakesANumberAndAChoice()
    {
        Assert.Equal("3", FieldTypes.Parse(FieldTypes.Number, "3", [], Today).Value!.ToJsonString());
        Assert.Equal("2.5", FieldTypes.Parse(FieldTypes.Number, "2.5", [], Today).Value!.ToJsonString());
        Assert.NotNull(FieldTypes.Parse(FieldTypes.Number, "three", [], Today).Refused);
        Assert.Equal("Normal", FieldTypes.TextOf(FieldTypes.Parse(FieldTypes.Choice, "normal", ["High", "Normal"], Today).Value));
        Assert.Equal("Not one of the options: High, Normal", FieldTypes.Parse(FieldTypes.Choice, "low", ["High", "Normal"], Today).Refused);
    }

    [Fact]
    public void ReadsValuesOut()
    {
        Assert.Equal("30 October 2026, Friday", FieldTypes.Spoken(new ItemField("Due", FieldTypes.Date, JsonValue.Create("2026-10-30"), [])));
        Assert.Equal("30 October 2026, Friday, 14:00", FieldTypes.Spoken(new ItemField("At", FieldTypes.DateTime, JsonValue.Create("2026-10-30T14:00"), [])));
        Assert.Equal("done", FieldTypes.Spoken(new ItemField("Done", FieldTypes.Check, JsonValue.Create(true), [])));
        Assert.Equal("not done", FieldTypes.Spoken(new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), [])));
        Assert.Equal("empty", FieldTypes.Spoken(new ItemField("Notes", FieldTypes.LongText, JsonValue.Create(string.Empty), [])));
        Assert.Equal("First, and 2 more lines", FieldTypes.Spoken(new ItemField("Notes", FieldTypes.LongText, JsonValue.Create("First\nSecond\nThird"), [])));
        Assert.Equal("42", FieldTypes.Spoken(new ItemField("Count", FieldTypes.Number, JsonValue.Create(42), [])));
        Assert.Equal("Washer", FieldTypes.Spoken(new ItemField("Waits on", FieldTypes.Link, JsonValue.Create("../Shopping/Washer.json"), []), _ => "Washer"));
        Assert.Equal("long text", FieldTypes.Spoken(FieldTypes.LongText));
    }

    [Fact]
    public void APasswordIsNeverReadOut()
    {
        Assert.Equal("hidden", FieldTypes.Spoken(new ItemField("Password", FieldTypes.Password, JsonValue.Create("secret"), [])));
        Assert.Equal("hidden", FieldTypes.Spoken(new ItemField("Password", FieldTypes.Password, JsonValue.Create(string.Empty), [])));
        Assert.Equal("kyle", FieldTypes.Spoken(new ItemField("UserName", FieldTypes.UserName, JsonValue.Create("kyle"), [])));
        Assert.Equal("user name", FieldTypes.Spoken(FieldTypes.UserName));
        Assert.True(FieldTypes.Known(FieldTypes.Password));
        Assert.DoesNotContain(FieldTypes.Password, FieldTypes.All);
    }

    [Fact]
    public void ATextFieldHoldsOneLine() =>
        Assert.NotNull(FieldTypes.Parse(FieldTypes.Text, "one\ntwo", [], Today).Refused);

    [Fact]
    public void AValueHasATypeByItsOwnKind()
    {
        Assert.Equal(FieldTypes.Check, FieldTypes.OfKind(JsonValue.Create(false)));
        Assert.Equal(FieldTypes.Number, FieldTypes.OfKind(JsonValue.Create(3)));
        Assert.Equal(FieldTypes.Text, FieldTypes.OfKind(JsonValue.Create("one line")));
        Assert.Equal(FieldTypes.LongText, FieldTypes.OfKind(JsonValue.Create("two\nlines")));
        Assert.Equal(FieldTypes.Text, FieldTypes.OfKind(null));
        Assert.Equal(FieldTypes.Text, FieldTypes.OfKind(new JsonArray()));
    }

    [Fact]
    public void AValueOfTheTypeAFieldHadIsReadAsTextWithWhatTheFieldIsNow()
    {
        // docs/axTask/forms-framework.md AF-11.6: the field's type changed in its list's configuration.
        Assert.Equal("next week, kept as text: the field is now a date", FieldTypes.Spoken(new ItemField("Due", FieldTypes.Date, JsonValue.Create("next week"), [])));
        Assert.Equal("yes, kept as text: the field is now a check", FieldTypes.Spoken(new ItemField("Done", FieldTypes.Check, JsonValue.Create("yes"), [])));
        Assert.Equal("true, kept as text: the field is now a text", FieldTypes.Spoken(new ItemField("Kind", FieldTypes.Text, JsonValue.Create(true), [])));
        Assert.Equal("many, kept as text: the field is now a number", FieldTypes.Spoken(new ItemField("Size", FieldTypes.Number, JsonValue.Create("many"), [])));
        Assert.Equal("soon, kept as text: the field is now a date and time", FieldTypes.Spoken(new ItemField("At", FieldTypes.DateTime, JsonValue.Create("soon"), [])));

        // What the type holds is read as ever: nothing, a number in a text, a number written as text.
        Assert.Equal("empty", FieldTypes.Spoken(new ItemField("Due", FieldTypes.Date, JsonValue.Create(string.Empty), [])));
        Assert.Equal("empty", FieldTypes.Spoken(new ItemField("Due", FieldTypes.Date, null, [])));
        Assert.Equal("3", FieldTypes.Spoken(new ItemField("Kind", FieldTypes.Text, JsonValue.Create(3), [])));
        Assert.Equal("12", FieldTypes.Spoken(new ItemField("Number", FieldTypes.Number, JsonValue.Create("12"), [])));
        Assert.True(FieldTypes.Holds(FieldTypes.Choice, JsonValue.Create("not an option")));
        Assert.False(FieldTypes.Holds(FieldTypes.Text, new JsonObject()));
    }

    [Fact]
    public void AnyValueHasAPlainText()
    {
        Assert.Equal("words", FieldTypes.PlainText(JsonValue.Create("words")));
        Assert.Equal("true", FieldTypes.PlainText(JsonValue.Create(true)));
        Assert.Equal("3.5", FieldTypes.PlainText(JsonValue.Create(3.5)));
        Assert.Equal(string.Empty, FieldTypes.PlainText(null));
        Assert.Equal("[1,2]", FieldTypes.PlainText(new JsonArray(1, 2)));
    }
}
