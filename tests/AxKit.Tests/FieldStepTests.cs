using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Right and Left on a field in a list of fields (docs/axKit/SPEC.md AX-8.10 rule 4, item-apps IA-5.6).</summary>
public sealed class FieldStepTests
{
    private static readonly string[] Priorities = ["High", "Normal", "Low"];

    private static ItemField Choice(string value) => new("Priority", FieldTypes.Choice, JsonValue.Create(value), Priorities);

    private static ItemField Check(bool value) => new("Done", FieldTypes.Check, JsonValue.Create(value), []);

    private static string? Text(JsonNode? value) => value is null ? null : FieldTypes.TextOf(value);

    [Fact]
    public void Right_on_a_choice_is_the_next_value_and_Left_the_previous()
    {
        Assert.Equal("Normal", Text(FieldStep.Step(Choice("High"), 1)));
        Assert.Equal("Low", Text(FieldStep.Step(Choice("Normal"), 1)));
        Assert.Equal("Normal", Text(FieldStep.Step(Choice("Low"), -1)));
        Assert.Equal("High", Text(FieldStep.Step(Choice("Normal"), -1)));
    }

    [Fact]
    public void A_choice_stops_at_its_ends_and_never_wraps()
    {
        Assert.Null(FieldStep.Step(Choice("Low"), 1));
        Assert.Null(FieldStep.Step(Choice("High"), -1));
    }

    [Fact]
    public void A_choice_is_matched_without_case()
    {
        Assert.Equal("Normal", Text(FieldStep.Step(Choice("high"), 1)));
    }

    [Fact]
    public void An_empty_choice_takes_the_first_value_on_Right_and_nothing_on_Left()
    {
        Assert.Equal("High", Text(FieldStep.Step(Choice(string.Empty), 1)));
        Assert.Null(FieldStep.Step(Choice(string.Empty), -1));
        Assert.Equal("High", Text(FieldStep.Step(Choice("Something else"), 1)));
    }

    [Fact]
    public void A_choice_without_options_changes_nothing()
    {
        var field = new ItemField("Priority", FieldTypes.Choice, JsonValue.Create("High"), []);
        Assert.Null(FieldStep.Step(field, 1));
        Assert.Null(FieldStep.Step(field, -1));
    }

    [Fact]
    public void Right_checks_and_Left_unchecks_a_direction_not_a_toggle()
    {
        Assert.True(FieldTypes.IsTrue(FieldStep.Step(Check(false), 1)));
        var unchecked_ = FieldStep.Step(Check(true), -1);
        Assert.NotNull(unchecked_);
        Assert.False(FieldTypes.IsTrue(unchecked_));
        // Already in that state: nothing, the edge sound.
        Assert.Null(FieldStep.Step(Check(true), 1));
        Assert.Null(FieldStep.Step(Check(false), -1));
    }

    [Theory]
    [InlineData(FieldTypes.Text)]
    [InlineData(FieldTypes.LongText)]
    [InlineData(FieldTypes.Number)]
    [InlineData(FieldTypes.Date)]
    [InlineData(FieldTypes.DateTime)]
    [InlineData(FieldTypes.Link)]
    [InlineData("a type of later")]
    public void Any_other_kind_of_field_changes_nothing(string type)
    {
        var field = new ItemField("Field", type, JsonValue.Create("5"), Priorities);
        Assert.Null(FieldStep.Step(field, 1));
        Assert.Null(FieldStep.Step(field, -1));
    }

    [Theory]
    [InlineData(FieldTypes.UserName)]
    [InlineData(FieldTypes.Password)]
    public void A_protected_field_never_changes(string type)
    {
        Assert.True(FieldStep.Protected(type));
        // Whatever the value and the options hold.
        var field = new ItemField("Password", type, JsonValue.Create("High"), Priorities);
        Assert.Null(FieldStep.Step(field, 1));
        Assert.Null(FieldStep.Step(field, -1));
        Assert.False(FieldStep.Protected(FieldTypes.Choice));
        Assert.False(FieldStep.Protected(FieldTypes.Check));
    }

    [Fact]
    public void The_change_is_said_as_the_name_and_the_new_value()
    {
        Assert.Equal("Priority: Low", FieldStep.Said("Priority", "Low"));
        Assert.Equal("untitled: Low", FieldStep.Said(string.Empty, "Low"));
    }
}
