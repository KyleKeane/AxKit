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

    /// <summary>
    /// A list made as a GitHub list before 2026-11-14 (TK-7a.2): its <c>github</c> member means nothing now and is kept
    /// as any member axTask does not know; its keys are read and its template is the list's, as in every list.
    /// </summary>
    [Fact]
    public void A_list_that_was_a_GitHub_list_is_an_ordinary_list_and_keeps_its_member()
    {
        const string text = """
            {
              "format": "axkit-list 1",
              "fields": [ { "title": "Notes", "type": "longtext", "value": "" } ],
              "new item": [ { "title": "Kind", "type": "choice", "value": "unlabelled", "options": ["Bug", "unlabelled"] } ],
              "github": { "repository": "owner/repo", "state": "open" },
              "keys": { "1": { "name": "Make it a bug", "set": { "Kind": "Bug" } }, "5": { "name": "Open on GitHub", "open": "Issue" } }
            }
            """;
        var list = ItemDocument.Parse(text);
        Assert.Equal([1, 5], ActionKeys.Read(list).Keys.Order());
        Assert.Equal("Kind", Assert.Single(TaskRules.TemplateOf(list)).Title);
        var changed = ActionKeys.With(list, 5, null);
        Assert.Contains("\"github\"", changed.ToText());
        Assert.Contains("owner/repo", changed.ToText());
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

    // ---- the form of one key (RP-8.2, bundle AX-8.10) ----

    private static bool Later(string list) => list == "Later";

    [Fact]
    public void The_form_shows_a_keys_action_by_its_kind_and_its_value()
    {
        var keys = ActionKeys.Read(ItemDocument.Parse(ListText));
        Assert.Equal(3, ActionKeys.KindNames.Count);
        Assert.Equal((0, "Kind: Bug"), (ActionKeys.KindIndex(keys[1]), ActionKeys.ValueText(keys[1])));
        // A check is written as it is typed, done or not done.
        Assert.Equal((0, "Done: done"), (ActionKeys.KindIndex(keys[4]), ActionKeys.ValueText(keys[4])));
        Assert.Equal((1, "Later"), (ActionKeys.KindIndex(keys[7]), ActionKeys.ValueText(keys[7])));
        Assert.Equal((2, "Issue"), (ActionKeys.KindIndex(keys[8]), ActionKeys.ValueText(keys[8])));
        // A key not set: the first kind, nothing typed.
        Assert.Equal((0, string.Empty), (ActionKeys.KindIndex(null), ActionKeys.ValueText(null)));
    }

    [Fact]
    public void The_forms_fields_make_the_action_of_the_kind_chosen()
    {
        var set = ActionKeys.FromForm(" Finish ", 0, "Done: done\r\nKind: Bug", Later);
        Assert.Null(set.Refused);
        Assert.Equal("Finish", set.Action!.Name);
        Assert.Equal("sets Done: done, Kind: Bug", set.Action.Described);

        var move = ActionKeys.FromForm("Park", 1, " Later ", Later);
        Assert.Equal("../Later", move.Action!.Move);

        var open = ActionKeys.FromForm("Web", 2, "Issue", Later);
        Assert.Equal("Issue", open.Action!.Open);
    }

    [Fact]
    public void What_a_key_shows_comes_back_as_the_same_action()
    {
        var keys = ActionKeys.Read(ItemDocument.Parse(ListText));
        foreach (var action in keys.Values)
        {
            var again = ActionKeys.FromForm(action.Name, ActionKeys.KindIndex(action), ActionKeys.ValueText(action), Later);
            Assert.Null(again.Refused);
            Assert.Equal(action.ToJson().ToJsonString(), again.Action!.ToJson().ToJsonString());
        }
    }

    [Fact]
    public void An_empty_name_removes_the_key_whatever_else_is_typed()
    {
        Assert.Equal((null, null, false), ActionKeys.FromForm("  ", 1, "Nowhere", Later));
    }

    [Theory]
    [InlineData(0, "no colon here", "Type at least one line as Field: value")]
    [InlineData(0, "", "Type at least one line as Field: value")]
    [InlineData(1, "", "Type the name of a list in the top folder")]
    [InlineData(1, "Nowhere", "No list called Nowhere in the top folder")]
    [InlineData(1, "Later\nSooner", "No list called Later Sooner in the top folder")]
    [InlineData(2, " ", "Type one field's title or one app")]
    [InlineData(2, "Issue\njson", "Type one field's title or one app")]
    public void A_value_the_kind_cannot_take_is_refused_in_the_values_field(int kind, string value, string refused)
    {
        var typed = ActionKeys.FromForm("Name", kind, value, Later);
        Assert.Null(typed.Action);
        Assert.Equal(refused, typed.Refused);
        Assert.True(typed.InValue);
    }
}
