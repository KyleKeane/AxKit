using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>Set fields and Clear fields on several items (SPEC.md TK-4.7a): what starts as "leave" leaves, and what is filled in is set.</summary>
public sealed class ManyFieldsTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);

    private static ItemDocument Item() => ItemDocument.Of(
    [
        new("Title", FieldTypes.Text, JsonValue.Create("Fix the tap"), []),
        new("Done", FieldTypes.Check, JsonValue.Create(false), []),
        new("Due", FieldTypes.Date, JsonValue.Create("2026-11-30"), []),
        new("Priority", FieldTypes.Choice, JsonValue.Create("Normal"), ["High", "Normal", "Low"]),
        new("Notes", FieldTypes.LongText, JsonValue.Create("Under the sink."), []),
        new("Waits on", FieldTypes.Link, JsonValue.Create(string.Empty), []),
        new("Room", FieldTypes.Text, JsonValue.Create("Kitchen"), []),
    ]);

    private static (string? Path, string? Refused) Link(string title) =>
        title == "Washer" ? ("../Shopping/Washer.json", null) : (null, $"No item is called {title}");

    private static List<FormValue> Left(IReadOnlyList<FormControl> controls) =>
        [.. controls.Select(control => new FormValue(string.Empty, false, 0))];

    [Fact]
    public void Every_field_but_the_title_starts_as_leave_and_a_check_is_a_list_of_three()
    {
        var controls = ManyFields.Controls(Item());

        Assert.Equal(6, controls.Count);
        Assert.DoesNotContain(controls, control => control.Field == 0);
        Assert.Equal([ManyFields.Leave, ManyFields.Checked, ManyFields.NotChecked], controls[0].Options);
        Assert.Equal([ManyFields.Leave, "High", "Normal", "Low"], controls[2].Options);
        Assert.All(controls, control => Assert.Equal(string.Empty, control.Text));
        Assert.All(controls, control => Assert.Equal(0, control.Selected));
    }

    [Fact]
    public void A_form_left_as_it_started_sets_nothing()
    {
        var item = Item();
        var controls = ManyFields.Controls(item);

        var (set, _, refused) = ManyFields.Set(item, controls, Left(controls), Today, Link);

        Assert.Null(refused);
        Assert.Empty(set!);
    }

    [Fact]
    public void What_is_filled_in_is_set_by_the_fields_title()
    {
        var item = Item();
        var controls = ManyFields.Controls(item);
        var values = Left(controls);
        values[0] = new FormValue(string.Empty, false, 1);
        values[1] = new FormValue("tomorrow", false, 0);
        values[2] = new FormValue(string.Empty, false, 1);
        values[4] = new FormValue("Washer", false, 0);

        var (set, _, refused) = ManyFields.Set(item, controls, values, Today, Link);

        Assert.Null(refused);
        Assert.Equal(4, set!.Count);
        Assert.True(set["Done"]!.GetValue<bool>());
        Assert.Equal("2026-11-25", set["Due"]!.GetValue<string>());
        Assert.Equal("High", set["Priority"]!.GetValue<string>());
        Assert.Equal("../Shopping/Washer.json", set["Waits on"]!.GetValue<string>());
    }

    [Fact]
    public void Not_checked_sets_a_check_off_and_a_value_refused_names_its_control()
    {
        var item = Item();
        var controls = ManyFields.Controls(item);
        var values = Left(controls);
        values[0] = new FormValue(string.Empty, false, 2);
        Assert.False(ManyFields.Set(item, controls, values, Today, Link).Set!["Done"]!.GetValue<bool>());

        values[1] = new FormValue("30 Febuary", false, 0);
        var (set, control, refused) = ManyFields.Set(item, controls, values, Today, Link);
        Assert.Null(set);
        Assert.Equal(1, control);
        Assert.StartsWith("Due: ", refused, StringComparison.Ordinal);
    }

    [Fact]
    public void Clear_lists_the_fields_but_the_title_and_empties_each_as_its_kind_is_emptied()
    {
        var item = Item();

        Assert.Equal(["Done", "Due", "Priority", "Notes", "Waits on", "Room"], ManyFields.Clearable(item));
        var cleared = ManyFields.Cleared(item, ["Done", "Priority", "Nothing like it"]);
        Assert.Equal(2, cleared.Count);
        Assert.False(cleared["Done"]!.GetValue<bool>());
        Assert.Equal(string.Empty, cleared["Priority"]!.GetValue<string>());
    }
}
