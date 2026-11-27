using AxKit.Core;
using AxKit.Core.Conversion;

namespace AxKit.Tests;

/// <summary>
/// The conversion of a list from the earlier shape (docs/axTask/forms-framework.md AF-13.6): nothing is lost, nothing
/// an item shows is hidden by it, and what could not be converted is said and left as it was.
/// </summary>
public sealed class EarlierFormatTests
{
    private const string List = """
        {
          "format": "axkit-list 1",
          "fields": [
            { "title": "Notes", "type": "longtext", "value": "What is open." },
            { "title": "Section", "type": "text", "value": "Lists" }
          ],
          "new item": [
            { "title": "Done", "type": "check", "value": false },
            { "title": "Priority", "type": "choice", "value": "Normal", "options": ["High", "Normal", "Low"] }
          ],
          "keys": {
            "1": { "name": "Done", "set": { "Done": true } },
            "2": { "name": "Later", "move": "../Later" },
            "4": { "name": "Issue", "open": "Issue" },
            "5": { "name": "Nothing" }
          },
          "github": { "repository": "owner/name" }
        }
        """;

    private const string Tap = """
        {
          "format": "axkit-item 1",
          "fields": [
            { "title": "Title", "type": "text", "value": "Fix the tap" },
            { "title": "Due", "type": "date", "value": "2026-11-30" },
            { "title": "Done", "type": "check", "value": true },
            { "title": "Priority", "type": "choice", "value": "High", "options": ["High", "Normal", "Low"] },
            { "title": "Notes", "type": "longtext", "value": "Under the sink.\nBring the wrench." }
          ]
        }
        """;

    private const string Milk = """
        {
          "format": "axkit-item 1",
          "fields": [
            { "title": "Title", "type": "text", "value": "Buy milk" },
            { "title": "Done", "type": "check", "value": false }
          ]
        }
        """;

    [Fact]
    public void The_earlier_shape_is_recognised_and_the_new_one_is_not()
    {
        Assert.True(EarlierFormat.IsEarlierList(List));
        Assert.True(EarlierFormat.IsEarlierItem(Tap));
        Assert.False(EarlierFormat.IsEarlierList(ListConfig.New([]).ToText()));
        Assert.False(EarlierFormat.IsEarlierItem("""{ "schema": "list.json", "Title": "x" }"""));
        Assert.False(EarlierFormat.IsEarlierItem("not json"));
    }

    [Fact]
    public void A_list_becomes_its_configuration_and_its_items_their_values()
    {
        var converted = EarlierFormat.Convert(List, [("Fix the tap.json", Tap), ("Buy milk.json", Milk)]);

        Assert.Empty(converted.Notes);
        var config = ListConfig.Parse(converted.Config);
        Assert.Empty(config.Problems);
        // The list's own fields are its details.
        Assert.Equal("What is open.", config.Detail("Notes"));
        Assert.Equal("Lists", config.Detail("Section"));
        // The template's fields first, in its order; then what only items hold, so that nothing is hidden.
        Assert.Equal(["Done", "Priority", "Due", "Notes"], config.ItemFields.Select(field => field.Title));
        Assert.Equal("Normal", FieldTypes.TextOf(config.ItemFields[1].Value));
        Assert.Equal(["High", "Normal", "Low"], config.ItemFields[1].Options);
        // A field only an item held starts empty in a new item.
        Assert.Equal(FieldTypes.Date, config.ItemFields[2].Type);
        Assert.Equal(string.Empty, FieldTypes.TextOf(config.ItemFields[2].Value));
        // What the file held beside is kept.
        Assert.Equal("owner/name", FieldTypes.TextOf(config.Extra("github")!["repository"]));

        // An item is the link, its Title, the configuration's fields it holds in the configuration's order.
        Assert.Equal(
            "{\r\n  \"schema\": \"list.json\",\r\n  \"Title\": \"Fix the tap\",\r\n  \"Done\": true,\r\n  \"Priority\": \"High\",\r\n  \"Due\": \"2026-11-30\",\r\n  \"Notes\": \"Under the sink.\\nBring the wrench.\"\r\n}\r\n",
            converted.Items[0].Text);
        Assert.Equal("{\r\n  \"schema\": \"list.json\",\r\n  \"Title\": \"Buy milk\",\r\n  \"Done\": false\r\n}\r\n", converted.Items[1].Text);
        Assert.Equal(["Fix the tap.json", "Buy milk.json"], converted.Items.Select(item => item.FileName));

        // Read through the new configuration, the item shows what it showed, and keeps nothing hidden.
        var shown = ItemView.Of(FlatItem.Parse(converted.Items[0].Text), config);
        Assert.Equal(["Done", "Priority", "Due", "Notes"], shown.Fields.Select(field => field.Title));
        Assert.Empty(shown.Kept);
    }

    [Fact]
    public void The_action_keys_become_actions_with_their_keys()
    {
        var text = EarlierFormat.Convert(List, []).Config;
        var actions = ListConfig.Parse(text).Actions!;

        // The key is written as it is read aloud, the plus sign itself: the file is one a person edits.
        Assert.Contains("\"key\": \"Ctrl+1\"", text, StringComparison.Ordinal);
        Assert.Equal(3, actions.Count);
        Assert.Equal(["name", "key", "does", "set"], actions[0]!.AsObject().Select(member => member.Key));
        Assert.Equal(("Done", "Ctrl+1", "set"), (Text(actions[0], "name"), Text(actions[0], "key"), Text(actions[0], "does")));
        Assert.True(actions[0]!["set"]!["Done"]!.GetValue<bool>());
        Assert.Equal(("Later", "Ctrl+2", "move", "../Later"), (Text(actions[1], "name"), Text(actions[1], "key"), Text(actions[1], "does"), Text(actions[1], "to")));
        Assert.Equal(("Issue", "Ctrl+4", "open", "Issue"), (Text(actions[2], "name"), Text(actions[2], "key"), Text(actions[2], "does"), Text(actions[2], "open")));

        static string Text(System.Text.Json.Nodes.JsonNode? action, string member) => FieldTypes.TextOf(action![member]);
    }

    [Fact]
    public void A_folder_without_a_list_file_is_converted_from_its_items_alone()
    {
        var converted = EarlierFormat.Convert(null, [("Fix the tap.json", Tap)]);

        var config = ListConfig.Parse(converted.Config);
        Assert.Equal(["Due", "Done", "Priority", "Notes"], config.ItemFields.Select(field => field.Title));
        Assert.Empty(config.Actions!);
        Assert.Empty(converted.Notes);
    }

    [Fact]
    public void What_is_not_of_the_earlier_shape_is_left_as_it_is_and_said()
    {
        var converted = EarlierFormat.Convert(List,
        [
            ("Fix the tap.json", Tap),
            ("broken.json", "{ not json"),
            ("already.json", """{ "schema": "list.json", "Title": "Already new" }"""),
            ("Later.filter.json", """{ "where": [] }"""),
        ]);

        // An item of the shape now is left without a word: there is nothing to know about it.
        Assert.Single(converted.Items);
        Assert.Equal(
            [
                "broken.json is not an item of the earlier format and is left as it is.",
                "Later.filter.json is not an item of the earlier format and is left as it is.",
            ],
            converted.Notes);
    }

    [Fact]
    public void A_list_of_the_shape_now_keeps_its_file_and_gains_only_what_an_earlier_item_brings()
    {
        // A converted list an earlier version of axKit wrote an item into: the item is converted, the list's file
        // kept to the letter when the item brings no new field.
        const string now = "{\r\n  // mine\r\n  \"format\": \"axkit-list 2\",\r\n  \"item fields\": [ { \"title\": \"Done\", \"type\": \"check\", \"value\": false, \"note\": \"kept\" } ],\r\n  \"actions\": []\r\n}\r\n";

        var same = EarlierFormat.Convert(now, [("Buy milk.json", Milk)]);
        Assert.Equal(now, same.Config);
        Assert.Equal("{\r\n  \"schema\": \"list.json\",\r\n  \"Title\": \"Buy milk\",\r\n  \"Done\": false\r\n}\r\n", same.Items.Single().Text);

        // An item that brings fields: they are added after the list's own, whose objects stay as the file wrote them.
        var more = EarlierFormat.Convert(now, [("Fix the tap.json", Tap)]);
        var config = ListConfig.Parse(more.Config);
        Assert.Equal(["Done", "Due", "Priority", "Notes"], config.ItemFields.Select(field => field.Title));
        Assert.Contains("\"note\": \"kept\"", more.Config, StringComparison.Ordinal);
        Assert.Empty(more.Notes);
    }

    [Fact]
    public void A_folder_is_recognised_planned_copied_and_written()
    {
        var above = Path.Combine(Path.GetTempPath(), "axkit-tests", "conversion-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(above, "Home");
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "list.json"), List);
            File.WriteAllText(Path.Combine(folder, "Fix the tap.json"), Tap);
            File.WriteAllText(Path.Combine(folder, "Buy milk.json"), Milk);
            File.WriteAllText(Path.Combine(folder, "notes.md"), "not a list's file");
            File.WriteAllText(Path.Combine(folder, "Later.filter.json"), """{ "where": [] }""");
            Assert.True(ListConversion.IsEarlier(folder));
            Assert.False(ListConversion.IsEarlier(Path.Combine(above, "missing")));

            var plan = ListConversion.Plan(folder);
            // The list's file first, then the items by name; a smart list's filter and other files are no part of it.
            Assert.Equal(["list.json", "Buy milk.json", "Fix the tap.json"], plan.Files.Select(file => Path.GetFileName(file.Path)));
            Assert.All(plan.Files, file => Assert.NotNull(file.Before));
            Assert.Empty(plan.Notes);
            // Nothing is written by the plan.
            Assert.Contains("axkit-list 1", File.ReadAllText(Path.Combine(folder, "list.json")));

            var copy = ListConversion.KeepCopy(folder, above);
            Assert.Equal(Path.Combine(above, "Home, before conversion"), copy);
            Assert.Equal(5, Directory.GetFiles(copy).Length);
            Assert.Equal(Path.Combine(above, "Home, before conversion 2"), ListConversion.KeepCopy(folder, above));

            ListConversion.Apply(plan);
            Assert.False(ListConversion.IsEarlier(folder));
            Assert.Equal("Fix the tap", FlatItem.Parse(File.ReadAllText(Path.Combine(folder, "Fix the tap.json"))).Title);
            Assert.Empty(ListConfig.Parse(File.ReadAllText(Path.Combine(folder, "list.json"))).Problems);
            // Converted, there is nothing left to plan.
            Assert.Empty(ListConversion.Plan(folder).Files);

            // A list without a file of its own is one of the earlier format when an item of it is.
            File.Delete(Path.Combine(folder, "list.json"));
            Assert.False(ListConversion.IsEarlier(folder));
            File.WriteAllText(Path.Combine(folder, "Old.json"), Milk);
            Assert.True(ListConversion.IsEarlier(folder));

            // A file that cannot be read as text (a NUL character) is no item of the earlier format, and hides none
            // that is: until 2026-11-27 one such file made the whole list read as not earlier.
            File.WriteAllBytes(Path.Combine(folder, "A broken file.json"), [(byte)'{', 0, (byte)'}']);
            Assert.True(ListConversion.IsEarlier(folder));
        }
        finally
        {
            Directory.Delete(above, recursive: true);
        }
    }

    /// <summary>AF-13.6: several lists are planned, every one, before the first copy is made: a list that cannot be planned leaves no copy of another in the Archive.</summary>
    [Fact]
    public void A_conversion_of_several_lists_that_cannot_be_prepared_leaves_no_copy()
    {
        using var above = new AxKit.TestShared.TempFolder("conversion-prepare");
        var good = Path.Combine(above.Path, "Home");
        var bad = Path.Combine(above.Path, "Work");
        var archive = Path.Combine(above.Path, "Archive");
        Directory.CreateDirectory(good);
        Directory.CreateDirectory(bad);
        File.WriteAllText(Path.Combine(good, "list.json"), List);
        File.WriteAllText(Path.Combine(good, "Fix the tap.json"), Tap);
        File.WriteAllText(Path.Combine(bad, "list.json"), List);
        // No text: the plan of this list cannot be made.
        File.WriteAllBytes(Path.Combine(bad, "Broken.json"), [(byte)'{', 0, (byte)'}']);

        Assert.Throws<InvalidOperationException>(() => ListConversion.Prepare([good, bad], _ => archive));
        Assert.False(Directory.Exists(archive) && Directory.EnumerateFileSystemEntries(archive).Any());

        File.Delete(Path.Combine(bad, "Broken.json"));
        var (plans, copies) = ListConversion.Prepare([good, bad], _ => archive);
        Assert.Equal([good, bad], plans.Select(plan => plan.Folder));
        Assert.Equal(["Home, before conversion", "Work, before conversion"], copies.Select(Path.GetFileName));
        ListConversion.RemoveCopies(copies);
        Assert.Empty(Directory.EnumerateFileSystemEntries(archive));
    }

    [Fact]
    public void Nothing_is_dropped_a_name_that_cannot_stay_is_kept_under_another_and_said()
    {
        const string odd = """
            {
              "format": "axkit-item 1",
              "fields": [
                { "title": "Title", "type": "text", "value": "Odd" },
                { "title": "schema", "type": "text", "value": "a field of that name" },
                { "title": "Size", "type": "text", "value": "first" },
                { "title": "size", "type": "text", "value": "second" }
              ],
              "source": "imported",
              "Size": "the file's own"
            }
            """;

        var converted = EarlierFormat.Convert(null, [("Odd.json", odd)]);

        var item = FlatItem.Parse(converted.Items[0].Text);
        Assert.Equal("list.json", item.Schema);
        Assert.Equal("a field of that name", item.Text("schema (field)"));
        Assert.Equal("first", item.Text("Size"));
        Assert.Equal("second", item.Text("size 2"));
        Assert.Equal("imported", item.Text("source"));
        Assert.Equal("the file's own", item.Text("Size (file)"));
        Assert.Equal(
            [
                "Odd.json: the field named schema is kept as \"schema (field)\", since that name is the item's link now.",
                "Odd.json: a second field named size is kept as \"size 2\".",
                "Odd.json: the file's own member Size is kept as \"Size (file)\".",
            ],
            converted.Notes);
    }

    [Fact]
    public void A_list_file_that_does_not_read_is_said_and_the_items_are_still_converted()
    {
        var converted = EarlierFormat.Convert("{ not json", [("Buy milk.json", Milk)]);

        Assert.Equal(["The list's own file did not read as JSON; the new one is made from the items alone."], converted.Notes);
        Assert.Single(converted.Items);
        Assert.Equal(["Done"], ListConfig.Parse(converted.Config).ItemFields.Select(field => field.Title));
    }
}
