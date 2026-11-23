using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>The day an item was made (SPEC.md TK-4.1a): a list that names a field Created gets it filled as an item is added.</summary>
public sealed class CreatedTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 14, 5, 0);

    [Fact]
    public void A_date_field_named_Created_gets_today_and_a_date_and_time_field_the_moment()
    {
        var dated = ListConfig.New([new(TaskRules.DoneField, FieldTypes.Check, null, []), new("created", FieldTypes.Date, null, [])]);
        var item = TaskRules.WithCreated(FlatItem.New("Fix the tap", dated), dated, Now);
        Assert.Equal("2026-11-23", item.Text("Created"));
        Assert.Equal(["Title", "Done", "created"], item.Values.Select(value => value.Key));

        var timed = ListConfig.New([new(TaskRules.CreatedField, FieldTypes.DateTime, null, [])]);
        Assert.Equal("2026-11-23T14:05", TaskRules.WithCreated(FlatItem.New("Fix the tap", timed), timed, Now).Text("Created"));
    }

    [Fact]
    public void A_list_without_it_a_field_of_another_type_and_a_value_given_are_left_as_they_are()
    {
        var plain = ListConfig.New([new(TaskRules.DueField, FieldTypes.Date, null, [])]);
        var item = FlatItem.New("Fix the tap", plain);
        Assert.Equal(item.ToText(), TaskRules.WithCreated(item, plain, Now).ToText());

        var text = ListConfig.New([new(TaskRules.CreatedField, FieldTypes.Text, null, [])]);
        Assert.Equal(string.Empty, TaskRules.WithCreated(FlatItem.New("Fix the tap", text), text, Now).Text("Created"));

        var dated = ListConfig.New([new(TaskRules.CreatedField, FieldTypes.Date, null, [])]);
        var given = FlatItem.New("Fix the tap", dated).With(TaskRules.CreatedField, System.Text.Json.Nodes.JsonValue.Create("2026-01-02"), dated);
        Assert.Equal("2026-01-02", TaskRules.WithCreated(given, dated, Now).Text("Created"));
    }
}
