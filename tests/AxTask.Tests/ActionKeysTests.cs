using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>The Ctrl+number action keys of a list (docs/axKit/reports.md RP-8).</summary>
public sealed class ActionKeysTests
{
    private const string ListText = """
        {
          "format": "axkit-list 1",
          "fields": [ { "title": "Notes", "type": "longtext", "value": "" } ],
          "new item": [ { "title": "Done", "type": "check", "value": false } ],
          "keys": {
            "1": { "name": "Make it a bug", "set": { "Kind": "Bug" } },
            "4": { "name": "Done", "set": { "Done": true } },
            "7": { "name": "Move to Later", "move": "../Later" },
            "8": { "name": "Open on GitHub", "open": "Issue" },
            "9": { "name": "", "open": "Issue" },
            "0": { "name": "Not a digit", "open": "Issue" }
          }
        }
        """;

    [Fact]
    public void The_keys_are_read_by_digit_and_a_nameless_one_is_not_one()
    {
        var keys = ActionKeys.Read(ItemDocument.Parse(ListText));
        Assert.Equal([1, 4, 7, 8], keys.Keys.Order());
        Assert.Equal("set", keys[1].Kind);
        Assert.Equal("sets Kind: Bug", keys[1].Described);
        Assert.Equal("sets Done: done", keys[4].Described);
        Assert.Equal("moves to ../Later", keys[7].Described);
        Assert.Equal("opens Issue", keys[8].Described);
        Assert.Equal("Ctrl+1. Make it a bug", ActionKeys.Spoken(1, keys[1]));
        Assert.Equal("Ctrl+3. not set", ActionKeys.Spoken(3, null));
        Assert.Empty(ActionKeys.Read(null));
        Assert.Empty(ActionKeys.Read(ItemDocument.NewList([])));
    }

    [Fact]
    public void A_key_is_set_and_removed_in_the_list_and_the_whole_key_goes_with_the_last()
    {
        var list = ItemDocument.NewList([]);
        list = ActionKeys.With(list, 2, new ListAction("Make it feedback", Set: new Dictionary<string, JsonNode?> { ["Kind"] = JsonValue.Create("Feedback") }));
        Assert.Contains("\"keys\"", list.ToText());
        var back = ActionKeys.Read(ItemDocument.Parse(list.ToText()));
        Assert.Equal("Make it feedback", back[2].Name);
        Assert.Equal("Feedback", back[2].Set!["kind"]!.GetValue<string>());

        list = ActionKeys.With(list, 2, null);
        Assert.DoesNotContain("\"keys\"", list.ToText());
        Assert.Single(list.Fields);
    }

    [Fact]
    public void Applying_a_set_changes_the_fields_and_adds_a_missing_one()
    {
        var item = ItemDocument.NewItem("Fix the tap", [new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), [])]);
        var set = ActionKeys.ParseSetLines("Done: done\r\nKind: Bug\r\nno colon here\r\nSize: 3");
        Assert.NotNull(set);
        var changed = ActionKeys.ApplySet(item, set);
        Assert.True(FieldTypes.IsTrue(changed.Fields[changed.IndexOf("Done")].Value));
        Assert.Equal("Bug", FieldTypes.TextOf(changed.Fields[changed.IndexOf("Kind")].Value));
        Assert.Equal(FieldTypes.Text, changed.Fields[changed.IndexOf("Kind")].Type);
        Assert.Equal(FieldTypes.Number, changed.Fields[changed.IndexOf("Size")].Type);
        Assert.Equal("Done: done, Kind: Bug, Size: 3", ActionKeys.SpokenSet(set));
        Assert.Null(ActionKeys.ParseSetLines("nothing"));
    }

    [Fact]
    public void An_open_action_never_starts_a_shell_or_Claude_Code_on_a_folder_off_this_computer()
    {
        // S-5's rule for the two apps that start a program where they open; the editors and viewers may open a share's file.
        Assert.StartsWith("Not started: axTerminal", ActionKeys.OpenRefusal(Dispatch.AxTerminal, @"\\server\share\lists\Home\Fix the tap.json"));
        Assert.StartsWith("Not started: axClaude", ActionKeys.OpenRefusal(Dispatch.AxClaude, @"\\server\share\code"));
        Assert.Null(ActionKeys.OpenRefusal(Dispatch.AxJSON, @"\\server\share\lists\Home\Fix the tap.json"));
        Assert.Null(ActionKeys.OpenRefusal(Dispatch.AxTerminal, Path.GetTempPath()));
        Assert.Null(ActionKeys.OpenRefusal(Dispatch.AxClaude, Path.Combine(Path.GetTempPath(), "code")));
    }
}
