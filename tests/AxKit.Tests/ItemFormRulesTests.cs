using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The item's form (docs/axTask/forms-framework.md AF-14.1): one standard control per field, and what the controls'
/// values make of the item when the form is saved.
/// </summary>
public sealed class ItemFormRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    private static ItemDocument Item() => ItemDocument.Of(
    [
        new("Title", FieldTypes.Text, JsonValue.Create("Fix the tap"), []),
        new("Done", FieldTypes.Check, JsonValue.Create(false), []),
        new("Due", FieldTypes.Date, JsonValue.Create("2026-11-30"), []),
        new("At", FieldTypes.DateTime, JsonValue.Create("2026-11-30T14:00"), []),
        new("Priority", FieldTypes.Choice, JsonValue.Create("Normal"), ["High", "Normal", "Low"]),
        new("Kind", FieldTypes.Choice, JsonValue.Create(string.Empty), ["Bug", "Idea"]),
        new("Notes", FieldTypes.LongText, JsonValue.Create("Under the sink.\nBring the wrench."), []),
        new("Size", FieldTypes.Number, JsonValue.Create(3), []),
        new("Waits on", FieldTypes.Link, JsonValue.Create("../Shopping/Washer.json"), []),
        new("Room", FieldTypes.Text, JsonValue.Create("Kitchen"), []),
    ]);

    private static string LinkTitle(string path) => path == "../Shopping/Washer.json" ? "Washer" : string.Empty;

    private static (string? Path, string? Refused) Link(string title) => title switch
    {
        "Washer" => ("../Shopping/Washer.json", null),
        "Hose" => ("../Shopping/Hose.json", null),
        "Twice" => (null, "2 items are called Twice: rename one first"),
        _ => (null, $"No item is called {title}"),
    };

    /// <summary>The controls' values as the form gives them back untouched.</summary>
    private static List<FormValue> Untouched(IReadOnlyList<FormControl> controls) =>
        [.. controls.Select(control => new FormValue(control.Text.ReplaceLineEndings("\r\n"), control.Checked, control.Selected))];

    [Fact]
    public void Each_field_gets_one_standard_control_named_with_an_instruction()
    {
        var controls = ItemFormRules.Controls(Item(), LinkTitle);

        Assert.Equal(Item().Fields.Count, controls.Count);
        Assert.Equal(Enumerable.Range(0, 10), controls.Select(control => control.Field));
        Assert.Equal(
            [
                ItemFormRules.TextKind, ItemFormRules.CheckKind, ItemFormRules.TextKind, ItemFormRules.TextKind, ItemFormRules.ListKind,
                ItemFormRules.ListKind, ItemFormRules.LinesKind, ItemFormRules.TextKind, ItemFormRules.TextKind, ItemFormRules.TextKind,
            ],
            controls.Select(control => control.Kind));
        Assert.Equal("Type the title", controls[0].Name);
        Assert.Equal("Fix the tap", controls[0].Text);
        // A check is a check box whose text is the field's title.
        Assert.Equal(("Done", false), (controls[1].Name, controls[1].Checked));
        Assert.StartsWith("Type Due: a date such as 2026-11-30, today", controls[2].Name);
        Assert.Equal("2026-11-30", controls[2].Text);
        // A date and time is typed with a space where the file has a T.
        Assert.Equal("2026-11-30 14:00", controls[3].Text);
        // A choice is a list of its options, the one it holds selected; never radio buttons.
        Assert.Equal(("Choose Priority", 1), (controls[4].Name, controls[4].Selected));
        Assert.Equal(["High", "Normal", "Low"], controls[4].Options);
        // A choice that holds none of its options starts on a line that chooses nothing.
        Assert.Equal([ItemFormRules.NotChosen, "Bug", "Idea"], controls[5].Options);
        Assert.Equal(0, controls[5].Selected);
        Assert.Equal("Type Notes; Enter makes a new line", controls[6].Name);
        Assert.Equal("3", controls[7].Text);
        // A link is typed as the title of the item it points to.
        Assert.Equal("Washer", controls[8].Text);
        Assert.Equal("Type Room", controls[9].Name);
    }

    [Fact]
    public void A_form_saved_untouched_changes_nothing()
    {
        var item = Item();
        var controls = ItemFormRules.Controls(item, LinkTitle);

        var (changed, control, refused) = ItemFormRules.Apply(item, controls, Untouched(controls), Today, Link);

        Assert.Same(item, changed);
        Assert.Equal(-1, control);
        Assert.Null(refused);
        Assert.Equal("Nothing changed", ItemFormRules.Said(item, changed!));
    }

    [Fact]
    public void Each_control_changed_gives_its_field_a_value_as_the_field_takes_it()
    {
        var item = Item();
        var controls = ItemFormRules.Controls(item, LinkTitle);
        var values = Untouched(controls);
        values[0] = values[0] with { Text = "  Fix the sink " };
        values[1] = values[1] with { Checked = true };
        values[2] = values[2] with { Text = "+3" };
        values[3] = values[3] with { Text = "tomorrow 09:30" };
        values[4] = values[4] with { Selected = 0 };
        values[5] = values[5] with { Selected = 2 };
        values[6] = values[6] with { Text = "One line.\r\nAnother.\r\n" };
        values[7] = values[7] with { Text = "4.5" };
        values[8] = values[8] with { Text = "Hose" };
        values[9] = values[9] with { Text = string.Empty };

        var (changed, _, refused) = ItemFormRules.Apply(item, controls, values, Today, Link);

        Assert.Null(refused);
        Assert.Equal("Fix the sink", changed!.Title);
        Assert.True(FieldTypes.IsTrue(changed.Fields[1].Value));
        Assert.Equal("2026-11-24", FieldTypes.TextOf(changed.Fields[2].Value));
        Assert.Equal("2026-11-22T09:30", FieldTypes.TextOf(changed.Fields[3].Value));
        Assert.Equal("High", FieldTypes.TextOf(changed.Fields[4].Value));
        Assert.Equal("Idea", FieldTypes.TextOf(changed.Fields[5].Value));
        Assert.Equal("One line.\nAnother.", FieldTypes.TextOf(changed.Fields[6].Value));
        Assert.Equal(4.5, changed.Fields[7].Value!.GetValue<double>());
        Assert.Equal("../Shopping/Hose.json", FieldTypes.TextOf(changed.Fields[8].Value));
        Assert.Equal(string.Empty, FieldTypes.TextOf(changed.Fields[9].Value));
        // The item handed in is as it was.
        Assert.Equal("Fix the tap", item.Title);

        Assert.Equal("Saved", ItemFormRules.Said(item, changed));
        Assert.Equal("Nothing changed", ItemFormRules.Said(item, item));
    }

    [Theory]
    [InlineData(0, "   ", "Type the item's title")]
    [InlineData(2, "30 Febuary", "Due: Not a date: 30 Febuary. Type 2026-10-30, today, tomorrow, +3 or a weekday")]
    [InlineData(7, "many", "Size: Not a number: many")]
    [InlineData(8, "Nothing like it", "Waits on: No item is called Nothing like it")]
    [InlineData(8, "Twice", "Waits on: 2 items are called Twice: rename one first")]
    public void The_first_value_a_field_does_not_take_is_named_with_its_control_and_nothing_changes(int index, string typed, string refusal)
    {
        var item = Item();
        var controls = ItemFormRules.Controls(item, LinkTitle);
        var values = Untouched(controls);
        values[1] = values[1] with { Checked = true };
        values[index] = values[index] with { Text = typed };

        var (changed, control, refused) = ItemFormRules.Apply(item, controls, values, Today, Link);

        Assert.Null(changed);
        Assert.Equal(index, control);
        Assert.Equal(refusal, refused);
    }

    [Fact]
    public void A_link_emptied_is_no_link_and_the_choice_left_on_not_chosen_stays_empty()
    {
        var item = Item();
        var controls = ItemFormRules.Controls(item, LinkTitle);
        var values = Untouched(controls);
        values[8] = values[8] with { Text = " " };

        var (changed, _, _) = ItemFormRules.Apply(item, controls, values, Today, Link);

        Assert.Equal(string.Empty, FieldTypes.TextOf(changed!.Fields[8].Value));
        Assert.Equal(string.Empty, FieldTypes.TextOf(changed.Fields[5].Value));
    }

    [Fact]
    public void A_value_the_form_cannot_show_as_it_is_stays_untouched_unless_it_is_changed()
    {
        // A value of the type the field had before (AF-11.6), a link whose item is gone, a type this version does
        // not know: each is a text box; saved untouched they stay to the letter, and one that is changed is taken
        // as its field takes a value now.
        var item = ItemDocument.Of(
        [
            new("Title", FieldTypes.Text, JsonValue.Create("Odd"), []),
            new("Due", FieldTypes.Date, JsonValue.Create("next week"), []),
            new("Done", FieldTypes.Check, JsonValue.Create("yes"), []),
            new("Waits on", FieldTypes.Link, JsonValue.Create("../Gone/Hose.json"), []),
            new("Weight", "kilograms", JsonValue.Create("12"), []),
        ]);
        var controls = ItemFormRules.Controls(item, _ => string.Empty);

        Assert.All(controls, control => Assert.Equal(ItemFormRules.TextKind, control.Kind));
        Assert.Equal("Type Due, kept as text: the field is now a date", controls[1].Name);
        Assert.Equal("next week", controls[1].Text);
        Assert.Equal("Type Done, kept as text: the field is now a check", controls[2].Name);
        Assert.Same(item, ItemFormRules.Apply(item, controls, Untouched(controls), Today, Link).Changed);

        var values = Untouched(controls);
        values[1] = values[1] with { Text = "today" };
        values[4] = values[4] with { Text = "13" };
        var changed = ItemFormRules.Apply(item, controls, values, Today, Link).Changed!;
        Assert.Equal("2026-11-21", FieldTypes.TextOf(changed.Fields[1].Value));
        Assert.Equal("13", FieldTypes.TextOf(changed.Fields[4].Value));
        // The link whose item is gone was not typed over: it is the path it was.
        Assert.Equal("../Gone/Hose.json", FieldTypes.TextOf(changed.Fields[3].Value));
    }
}
