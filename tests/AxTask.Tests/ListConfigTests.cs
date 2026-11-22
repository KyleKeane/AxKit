using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// A list's configuration and an item read through it (docs/axTask/forms-framework.md sections 11 and 13): the file
/// a person edits, hide, keep and restore, and a file that does not read.
/// </summary>
public sealed class ListConfigTests
{
    private const string Config = """
        {
          "format": "axkit-list 2",
          "details": { "Notes": "What is open.", "Section": "Lists" },
          "item fields": [
            { "title": "Done", "type": "check", "value": false },
            { "title": "Priority", "type": "choice", "value": "Normal", "options": ["High", "Normal", "Low"] },
            { "title": "Due", "type": "date", "value": "" }
          ],
          "actions": [ { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true } } ],
          "something else": { "kept": true }
        }
        """;

    private const string Item = """
        {
          "schema": "list.json",
          "Title": "Fix the tap",
          "Done": false,
          "Priority": "High",
          "Sprint": "November",
          "Old priority": 3
        }
        """;

    [Fact]
    public void The_configuration_gives_its_details_its_item_fields_in_order_and_its_actions()
    {
        var config = ListConfig.Parse(Config);

        Assert.Empty(config.Problems);
        Assert.Equal("Lists", config.Detail("section"));
        Assert.Equal(["Done", "Priority", "Due"], config.ItemFields.Select(field => field.Title));
        Assert.Equal(FieldTypes.Choice, config.ItemFields[1].Type);
        Assert.Equal(["High", "Normal", "Low"], config.ItemFields[1].Options);
        Assert.Single(config.Actions!);
        Assert.Equal(1, config.IndexOf("priority"));
        Assert.Equal(-1, config.IndexOf("Sprint"));
    }

    [Fact]
    public void What_this_version_does_not_know_is_written_back_as_it_was_read()
    {
        var config = ListConfig.Parse(Config).WithDetail("Notes", JsonValue.Create("Changed."));

        var again = ListConfig.Parse(config.ToText());

        Assert.Equal("Changed.", again.Detail("Notes"));
        Assert.Equal("Lists", again.Detail("Section"));
        Assert.True(again.Extra("something else")!["kept"]!.GetValue<bool>());
        Assert.Equal(3, again.ItemFields.Count);
        Assert.EndsWith("\r\n", config.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_is_not_JSON_says_the_line_and_one_of_another_format_says_which()
    {
        var broken = Assert.Throws<InvalidDataException>(() => ListConfig.Parse("{\n  \"format\": \"axkit-list 2\",\n  \"details\": { \"Notes\": \"x\" \"y\" }\n}"));
        Assert.Equal("it is not JSON at line 3", broken.Message);

        var earlier = Assert.Throws<InvalidDataException>(() => ListConfig.Parse("""{ "format": "axkit-list 1", "fields": [] }"""));
        Assert.Equal("its format is axkit-list 1, not axkit-list 2", earlier.Message);
        Assert.Equal("axkit-list 1", ListConfig.FormatOf("""{ "format": "axkit-list 1", "fields": [] }"""));
        Assert.Null(ListConfig.FormatOf("not json"));
        Assert.Throws<InvalidDataException>(() => ListConfig.Parse("[1, 2]"));
    }

    [Fact]
    public void A_comma_too_many_and_a_comment_are_read_past()
    {
        var config = ListConfig.Parse("""
            {
              // the list of what is open
              "format": "axkit-list 2",
              "item fields": [ { "title": "Done", "type": "check", "value": false }, ],
            }
            """);

        Assert.Single(config.ItemFields);
    }

    [Fact]
    public void A_member_of_the_wrong_kind_is_passed_over_with_its_sentence_and_the_rest_counts()
    {
        var config = ListConfig.Parse("""
            {
              "format": "axkit-list 2",
              "details": "none",
              "actions": { "name": "x" },
              "item fields": [
                "Done",
                { "type": "check" },
                { "title": "Title", "type": "text" },
                { "title": "schema", "type": "text" },
                { "title": "Due", "type": "date" },
                { "title": "due", "type": "text" },
                { "title": "Weight", "type": "kilograms" },
                { "title": "Notes" }
              ]
            }
            """);

        Assert.Equal(["Due", "Weight", "Notes"], config.ItemFields.Select(field => field.Title));
        Assert.Equal(FieldTypes.Text, config.ItemFields[2].Type);
        Assert.Equal(
            [
                "\"details\" is not an object of names and values, and is passed over",
                "\"actions\" is not a list, and is passed over",
                "item field 1 is not an object, and is passed over",
                "item field 2 has no title, and is passed over",
                "an item field cannot be named Title, and is passed over",
                "an item field cannot be named schema, and is passed over",
                "the item field due is there twice; the first counts",
                "the item field Weight has the type kilograms, which this version does not know; it is read as text",
            ],
            config.Problems);
        Assert.Null(config.Actions);
    }

    [Fact]
    public void A_new_configuration_holds_every_member_to_be_filled_in()
    {
        var config = ListConfig.New([new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), []), new ItemField("Kind", FieldTypes.Choice, JsonValue.Create("Bug"), ["Bug", "Idea"])]);

        var text = config.ToText();

        Assert.Contains("\"format\": \"axkit-list 2\"", text, StringComparison.Ordinal);
        Assert.Contains("\"details\"", text, StringComparison.Ordinal);
        Assert.Contains("\"item fields\"", text, StringComparison.Ordinal);
        Assert.Contains("\"actions\": []", text, StringComparison.Ordinal);
        Assert.Equal(["Bug", "Idea"], ListConfig.Parse(text).ItemFields[1].Options);
    }

    [Fact]
    public void An_item_is_its_names_and_values_and_its_link()
    {
        var item = FlatItem.Parse(Item);

        Assert.Equal("list.json", item.Schema);
        Assert.Equal("Fix the tap", item.Title);
        Assert.Equal(["Title", "Done", "Priority", "Sprint", "Old priority"], item.Values.Select(pair => pair.Key));
        Assert.Equal("High", item.Text("priority"));
        Assert.True(item.Has("SPRINT"));
        Assert.False(item.Has("schema"));

        var missing = Assert.Throws<InvalidDataException>(() => FlatItem.Parse("""{ "Title": "No link" }"""));
        Assert.Equal("it names no configuration to be read through (\"schema\")", missing.Message);
        Assert.Throws<ArgumentException>(() => item.With("Schema", JsonValue.Create("other.json")));
    }

    [Fact]
    public void A_new_item_starts_with_the_configurations_values_one_member_a_line()
    {
        var item = FlatItem.New("Buy milk", ListConfig.Parse(Config));

        Assert.Equal(
            "{\r\n  \"schema\": \"list.json\",\r\n  \"Title\": \"Buy milk\",\r\n  \"Done\": false,\r\n  \"Priority\": \"Normal\",\r\n  \"Due\": \"\"\r\n}\r\n",
            item.ToText());
    }

    [Fact]
    public void Hide_keep_restore_a_field_the_configuration_no_longer_names_is_kept_and_comes_back()
    {
        var config = ListConfig.Parse(Config);
        var item = FlatItem.Parse(Item);

        // Read through the configuration: its fields in its order, the item's values; what it does not name is kept.
        var shown = ItemView.Of(item, config);
        Assert.Equal(["Done", "Priority", "Due"], shown.Fields.Select(field => field.Title));
        Assert.Equal("High", FieldTypes.TextOf(shown.Fields[1].Value));
        // A field the file does not hold is shown empty, and nothing is written for it.
        Assert.Equal(string.Empty, FieldTypes.TextOf(shown.Fields[2].Value));
        Assert.False(item.Has("Due"));
        Assert.Equal(["Sprint", "Old priority"], shown.Kept.Select(kept => kept.Name));
        Assert.Equal("2 more fields kept: Sprint, Old priority", shown.KeptLine);

        // Priority leaves the configuration: hidden, and its value still in the item.
        var without = config.WithItemFields([.. config.ItemFields.Where(field => field.Title != "Priority")]);
        var hidden = ItemView.Of(item, without);
        Assert.DoesNotContain(hidden.Fields, field => field.Title == "Priority");
        Assert.Contains(hidden.Kept, kept => kept.Name == "Priority" && FieldTypes.TextOf(kept.Value) == "High");
        Assert.Equal(Item.ReplaceLineEndings("\r\n") + "\r\n", item.ToText());

        // Sprint is named again: shown with the value it had.
        var with = without.WithItemFields([.. without.ItemFields, new ItemField("Sprint", FieldTypes.Text, JsonValue.Create(string.Empty), [])]);
        var restored = ItemView.Of(item, with);
        Assert.Equal("November", FieldTypes.TextOf(restored.Fields.Single(field => field.Title == "Sprint").Value));
        Assert.Equal("2 more fields kept: Priority, Old priority", restored.KeptLine);
        Assert.Null(ItemView.Of(FlatItem.New("x", config), config).KeptLine);
    }

    [Fact]
    public void A_value_given_for_the_first_time_is_put_where_the_configuration_has_it()
    {
        var config = ListConfig.Parse(Config);
        var item = FlatItem.Parse(Item);

        // Due is the configuration's third field: after Done and Priority, before what is kept.
        var due = item.With("due", JsonValue.Create("2026-11-30"), config);
        Assert.Equal(["Title", "Done", "Priority", "Due", "Sprint", "Old priority"], due.Values.Select(pair => pair.Key));

        // A name it holds keeps its place and its spelling; a name nobody knows goes last.
        Assert.Equal(["Title", "Done", "Priority", "Sprint", "Old priority"], item.With("SPRINT", JsonValue.Create("December"), config).Values.Select(pair => pair.Key));
        Assert.Equal("December", item.With("SPRINT", JsonValue.Create("December"), config).Text("Sprint"));
        Assert.Equal("Colour", item.With("Colour", JsonValue.Create("red"), config).Values[^1].Key);
        // The item read is as it was: every change is a new item.
        Assert.False(item.Has("Due"));
    }

    [Fact]
    public void An_item_whose_configuration_is_not_there_is_shown_as_plain_text_in_its_own_order()
    {
        var plain = ItemView.Plain(FlatItem.Parse(Item));

        Assert.Equal("Fix the tap", plain.Title);
        Assert.Equal(["Done", "Priority", "Sprint", "Old priority"], plain.Fields.Select(field => field.Title));
        Assert.All(plain.Fields, field => Assert.Equal(FieldTypes.Text, field.Type));
        Assert.Empty(plain.Kept);
    }
}
