using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>axTask's AI tools, their pure half (docs/axTask/SPEC.md TK-8, docs/axKit/mcp.md MC-8): the lists reached, a list found by its name, an item as the tools give it, a new item from a template.</summary>
public sealed class TaskToolsTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    private readonly TempFolder _temp = new("axtask-tools");

    private string Top => Path.Combine(_temp.Path, "Tasks");

    private string Project => Path.Combine(_temp.Path, "projects", "alpha");

    public TaskToolsTests()
    {
        List(Path.Combine(Top, "Personal"));
        List(Path.Combine(Top, "Work"));
        List(Path.Combine(Top, "Archive", "Old"));
        List(Path.Combine(Project, "Home"));
        List(Path.Combine(Project, "Work"));
    }

    public void Dispose() => _temp.Dispose();

    private IReadOnlyList<ReachableList> Reached() => TaskTools.Lists(Top, [new TaskProject(Project, "alpha", 2)]);

    [Fact]
    public void The_top_folders_lists_come_first_then_each_projects_by_project_and_list_never_the_Archive()
    {
        var lists = Reached();
        Assert.Equal(["Personal", "Work", "alpha / Home", "alpha / Work"], lists.Select(list => list.Name));
        Assert.Equal([TaskTools.TopPlace, TaskTools.TopPlace, "alpha", "alpha"], lists.Select(list => list.Place));
    }

    [Theory]
    [InlineData("personal", "Personal")]
    [InlineData("  Personal ", "Personal")]
    [InlineData("alpha / home", "alpha / Home")]
    [InlineData("Home", "alpha / Home")]
    [InlineData("Work", "Work")]
    public void A_list_is_found_by_its_name_or_by_its_own_name_when_one_list_has_it(string named, string found)
    {
        var (list, refused) = TaskTools.Find(Reached(), named);
        Assert.Null(refused);
        Assert.Equal(found, list!.Name);
    }

    [Fact]
    public void A_name_that_names_none_several_or_nothing_is_refused_with_how_to_name_one()
    {
        var lists = Reached();
        Assert.Contains("No list is named Garden", TaskTools.Find(lists, "Garden").Refused);
        Assert.Contains("Name the list", TaskTools.Find(lists, " ").Refused);
        Assert.Contains("Name the list", TaskTools.Find(lists, null).Refused);
        var two = TaskTools.Find([new("one / Home", "one", @"D:\one\Home"), new("two / Home", "two", @"D:\two\Home")], "Home");
        Assert.Equal("Several lists are named Home: give one of one / Home; two / Home.", two.Refused);
    }

    [Fact]
    public void A_path_is_taken_only_when_it_is_one_of_the_lists_reached()
    {
        var lists = Reached();
        Assert.Equal("Personal", TaskTools.Find(lists, Path.Combine(Top, "Personal") + Path.DirectorySeparatorChar).List!.Name);
        Assert.Contains("is not one of the axTask window's lists", TaskTools.Find(lists, Path.Combine(Top, "Archive", "Old")).Refused);
        Assert.Contains("is not one of the axTask window's lists", TaskTools.Find(lists, @"D:\Elsewhere\Personal").Refused);
        Assert.Contains("is not one of the axTask window's lists", TaskTools.Find(lists, @"\\server\share\Personal").Refused);
    }

    [Fact]
    public void A_new_item_is_the_templates_with_the_values_typed_as_the_fields_take_them()
    {
        List<ItemField> template =
        [
            new(TaskRules.DoneField, FieldTypes.Check, JsonValue.Create(false), []),
            new(TaskRules.DueField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
            new("Priority", FieldTypes.Choice, JsonValue.Create("Normal"), ["High", "Normal", "Low"]),
        ];
        template.Add(new("Notes", FieldTypes.LongText, JsonValue.Create(string.Empty), []));
        var (item, refused) = TaskTools.NewItem(" Call the dentist ", template, [new("due", "+7"), new("Priority", "high"), new("Notes", "Ask about Tuesday")], Today);
        Assert.Null(refused);
        Assert.Equal("Call the dentist", item!.Title);
        Assert.Equal(["Title", "Done", "Due", "Priority", "Notes"], item.Fields.Select(field => field.Title));
        Assert.Equal("2026-11-18", FieldTypes.TextOf(item.Fields[2].Value));
        Assert.Equal("High", FieldTypes.TextOf(item.Fields[3].Value));
        Assert.Equal(FieldTypes.LongText, item.Fields[4].Type);
        Assert.Equal("Ask about Tuesday", FieldTypes.TextOf(item.Fields[4].Value));
        Assert.False(FieldTypes.IsTrue(item.Fields[1].Value));

        // Its file: the link, the Title and the list's fields in the list's order, one member a line.
        Assert.Equal(
            "{\r\n  \"schema\": \"list.json\",\r\n  \"Title\": \"Call the dentist\",\r\n  \"Done\": false,\r\n  \"Due\": \"2026-11-18\",\r\n  \"Priority\": \"High\",\r\n  \"Notes\": \"Ask about Tuesday\"\r\n}\r\n",
            ItemFolder.NewItemFile(item, ListConfig.New(template)).ToText());
    }

    [Fact]
    public void A_field_the_list_does_not_have_is_refused_with_the_fields_it_has()
    {
        // A value the list's configuration does not name would be kept in the file and shown to no one (AF-11.3).
        var (item, refused) = TaskTools.NewItem("Call the dentist", TaskRules.DefaultTemplate, [new("Notes", "Ask about Tuesday")], Today);
        Assert.Null(item);
        Assert.Equal("Notes is not a field of this list. Its fields: Done, Start, Due.", refused);
        Assert.Equal("Notes is not a field of this list: its items have only a title.", TaskTools.NewItem("Milk", [], [new("Notes", "x")], Today).Refused);
        Assert.StartsWith("schema is not a field of this list", TaskTools.NewItem("Milk", [], [new("schema", "other.json")], Today).Refused);
    }

    [Fact]
    public void The_same_item_as_New_item_makes_when_no_value_is_given()
    {
        var (item, _) = TaskTools.NewItem("Milk", TaskRules.DefaultTemplate, [], Today);
        var config = ListConfig.New(TaskRules.DefaultTemplate);
        Assert.Equal(FlatItem.New("Milk", config).ToText(), ItemFolder.NewItemFile(item!, config).ToText());
    }

    [Fact]
    public void A_title_longer_than_the_limit_is_refused_and_one_at_the_limit_taken()
    {
        var (longer, refused) = TaskTools.NewItem(new string('a', TaskTools.MaxTitle + 1), TaskRules.DefaultTemplate, [], Today);
        Assert.Null(longer);
        Assert.Equal("A title is at most 200 characters: put the rest in Notes.", refused);
        Assert.NotNull(TaskTools.NewItem(new string('a', TaskTools.MaxTitle), TaskRules.DefaultTemplate, [], Today).Item);
    }

    [Theory]
    [InlineData("", "Due", "+1", "Give the item's title.")]
    [InlineData("Two\nlines", "Due", "+1", "A title is one line.")]
    [InlineData("Tab\there", "Due", "+1", "A title holds no control characters.")]
    [InlineData("Bell\u0007", "Due", "+1", "A title holds no control characters.")]
    [InlineData("Milk", "Due\u001b", "+1", "A field's title holds no control characters.")]
    [InlineData("Milk", "Due", "next week", "Due: Not a date: next week.")]
    [InlineData("Milk", "Priority", "Urgent", "Priority: Not one of the options: High, Normal, Low")]
    [InlineData("Milk", "Done", "maybe", "Done: a check is true or false")]
    [InlineData("Milk", "title", "Bread", "The title is given as title")]
    [InlineData("Milk", "See also", "x", "See also: a link is made by the user")]
    public void A_value_the_field_does_not_take_is_refused_with_its_reason(string title, string field, string value, string reason)
    {
        List<ItemField> template =
        [
            new(TaskRules.DoneField, FieldTypes.Check, JsonValue.Create(false), []),
            new(TaskRules.DueField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
            new("Priority", FieldTypes.Choice, JsonValue.Create("Normal"), ["High", "Normal", "Low"]),
            new("See also", FieldTypes.Link, JsonValue.Create(string.Empty), []),
        ];
        var (item, refused) = TaskTools.NewItem(title, template, [new(field, value)], Today);
        Assert.Null(item);
        Assert.StartsWith(reason, refused);
    }

    [Fact]
    public void An_item_is_given_with_its_title_whether_it_is_done_and_its_fields_a_password_never()
    {
        // A password field (a type of axPass's, should a list's configuration name one) is never given.
        var config = ListConfig.New([.. TaskRules.DefaultTemplate, new("Secret", FieldTypes.Password, JsonValue.Create(string.Empty), [])]);
        var flat = FlatItem.New("Fix the tap", config).With("Secret", JsonValue.Create("hunter2")).With("Sprint", JsonValue.Create("November"));
        var item = TaskTools.ItemOf(new ItemFile(@"D:\Tasks\Home\Fix the tap.json", flat, config, flat.ToText()));
        Assert.Equal("Fix the tap", item["title"]!.GetValue<string>());
        Assert.False(item["done"]!.GetValue<bool>());
        Assert.Equal("protected", item["fields"]![4]!["value"]!.GetValue<string>());
        Assert.DoesNotContain("hunter2", item.ToJsonString(), StringComparison.Ordinal);
        // A value the list does not name is kept in the file and is no field given.
        Assert.DoesNotContain("Sprint", item.ToJsonString(), StringComparison.Ordinal);
        var plain = ListConfig.New([]);
        var milk = FlatItem.New("Milk", plain);
        Assert.Null(TaskTools.ItemOf(new ItemFile(@"D:\Tasks\Shopping\Milk.json", milk, plain, milk.ToText()))["done"]);
    }

    private static void List(string folder) => TestLists.List(folder, fields: []);
}
