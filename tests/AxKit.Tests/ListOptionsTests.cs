using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The options of a list's configuration (docs/axTask/forms-framework.md section 12): one table, held to what the
/// configuration's reader takes and to the file written from it for other programs. The actions' side of the table
/// is held to axTask's reader in <c>AxTask.Tests/ListActionsTests</c>.
/// </summary>
public sealed class ListOptionsTests
{
    [Fact]
    public void The_table_names_the_configurations_members_and_the_field_types_as_the_reader_takes_them()
    {
        var table = ListOptions.Table;
        Assert.Equal(ListConfig.Format, table.Format);
        Assert.Equal([ListConfig.FormatKey, ListConfig.DetailsKey, ListConfig.ItemFieldsKey, ListConfig.ActionsKey], table.Root.Members.Select(member => member.Name));
        Assert.Equal(FieldTypes.All, table.Kind("field")!.Member("type")!.Values);
        Assert.Equal(["title", "type", "value", "options"], table.Kind("field")!.Members.Select(member => member.Name));

        // A configuration made of every member's first value reads without a problem.
        var root = new JsonObject();
        foreach (var member in table.Root.Members)
        {
            root[member.Name] = JsonNode.Parse(member.First);
        }

        var field = new JsonObject();
        foreach (var member in table.Kind("field")!.Members)
        {
            field[member.Name] = JsonNode.Parse(member.First);
        }

        field["title"] = "Size";
        ((JsonArray)root[ListConfig.ItemFieldsKey]!).Add(field);
        var config = ListConfig.Parse(root.ToJsonString());
        Assert.Empty(config.Problems);
        Assert.Equal("Size", Assert.Single(config.ItemFields).Title);
    }

    [Fact]
    public void Every_member_says_what_it_is_and_starts_with_a_value_that_is_JSON()
    {
        foreach (var kind in ListOptions.Table.Kinds)
        {
            Assert.NotEmpty(kind.What);
            Assert.Equal(kind.Members.Count, kind.Members.Select(member => member.Name).Distinct().Count());
            foreach (var member in kind.Members)
            {
                Assert.True(member.What.Length > 10, $"{kind.Name}.{member.Name} does not say what it is");
                Assert.False(member.What.EndsWith('.'), $"{kind.Name}.{member.Name}: no full stop, it is read after the name");
                JsonNode.Parse(member.First);
                // A kind a member names is in the table.
                Assert.True(member.Kind is null || ListOptions.Table.Kind(member.Kind) is not null, $"{kind.Name}.{member.Name} names the kind {member.Kind}");
                Assert.True(member.Each is null || ListOptions.Table.Kind(member.Each) is not null, $"{kind.Name}.{member.Name} names the kind {member.Each}");
                // A first value that is one of the member's values, where it has them.
                if (member.Values.Count > 0 && member.Name != "folder")
                {
                    Assert.Contains(member.Quoted ? FieldTypes.TextOf(JsonNode.Parse(member.First)) : member.First, member.Values);
                }
            }
        }
    }

    [Fact]
    public void The_kind_of_an_object_is_found_by_the_way_to_it()
    {
        var table = ListOptions.Table;
        Assert.Equal("list", table.KindAt([])!.Name);
        Assert.Equal("details", table.KindAt(["details"])!.Name);
        Assert.Equal("field", table.KindAt(["item fields", null])!.Name);
        Assert.Equal("action", table.KindAt(["actions", null])!.Name);
        Assert.Equal("form field", table.KindAt(["actions", null, "fields", null])!.Name);
        Assert.Equal("then", table.KindAt(["actions", null, "then"])!.Name);
        // The list itself is no object of a kind, and neither is what the table does not name.
        Assert.Null(table.KindAt(["actions"]));
        Assert.Null(table.KindAt(["actions", null, "set"]));
        Assert.Null(table.KindAt(["github"]));
        Assert.Null(table.KindAt([null]));
    }

    [Fact]
    public void An_items_options_are_its_lists_fields()
    {
        var config = ListConfig.New(
        [
            new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), []),
            new ItemField("Priority", FieldTypes.Choice, JsonValue.Create("Normal"), ["High", "Normal", "Low"]),
            new ItemField("Due", FieldTypes.Date, JsonValue.Create(string.Empty), []),
        ]);

        var item = ListOptions.ForItem(config).Root;
        Assert.Equal([FlatItem.SchemaKey, ItemDocument.TitleField, "Done", "Priority", "Due"], item.Members.Select(member => member.Name));
        Assert.Equal(["true", "false"], item.Member("Done")!.Values);
        Assert.False(item.Member("Done")!.Quoted);
        Assert.Equal(["High", "Normal", "Low"], item.Member("Priority")!.Values);
        Assert.Equal("\"Normal\"", item.Member("Priority")!.First);
        Assert.Empty(item.Member("Due")!.Values);
        Assert.Equal("A field of the type date", item.Member("Due")!.What);
    }

    /// <summary>AF-12.2: the table as a file for a program that is not axKit, under docs/axKit, written from the table and never by hand.</summary>
    [Fact]
    public void The_options_file_is_what_the_table_writes()
    {
        var path = Path.Combine(LayoutTests.RepositoryRoot(), "docs", "axKit", ListOptions.FileName);
        var written = ListOptions.Table.ToJson();
        if (Environment.GetEnvironmentVariable("AXKIT_UPDATE_OPTIONS") == "1")
        {
            File.WriteAllText(path, written);
        }

        Assert.True(File.Exists(path), $"docs/axKit/{ListOptions.FileName} is not there: run this test once with AXKIT_UPDATE_OPTIONS=1");
        Assert.Equal(written.ReplaceLineEndings("\n"), File.ReadAllText(path).ReplaceLineEndings("\n"));
        var read = JsonNode.Parse(written)!;
        Assert.Equal("axkit-options 1", read["format"]!.GetValue<string>());
        Assert.Equal(ListConfig.Format, read["for"]!.GetValue<string>());
        Assert.Equal("set", read["kinds"]!["action"]!["members"]![2]!["values"]![0]!.GetValue<string>());
    }
}
