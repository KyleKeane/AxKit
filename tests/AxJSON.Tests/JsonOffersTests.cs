using AxJSON.Core;
using AxKit.Core;

namespace AxJSON.Tests;

/// <summary>
/// What axJSON offers from an options table (docs/axJSON/SPEC.md AJ-5; docs/axTask/forms-framework.md section 12):
/// the values of a member that is one of a few, and the members an object can have and lacks; for a list's
/// configuration by its format, for an item by its link to that configuration.
/// </summary>
public sealed class JsonOffersTests
{
    private const string Config = """
        {
          "format": "axkit-list 2",
          "details": { "Notes": "" },
          "item fields": [
            { "title": "Done", "type": "check", "value": false },
            { "title": "Priority", "type": "choice", "value": "Normal", "options": ["High", "Normal", "Low"] }
          ],
          "actions": [
            {
              "name": "Later",
              "does": "move",
              "to": "../Later"
            }
          ]
        }
        """;

    private const string Item = """
        {
          "schema": "list.json",
          "Title": "Fix the tap",
          "Done": false,
          "Priority": "Normal",
          "Sprint": "old"
        }
        """;

    private static string? NoFile(string name) => null;

    private static string? Beside(string name) => name == "list.json" ? Config : null;

    private static string Apply(string text, JsonEdit edit) => text[..edit.Start] + edit.Text + text[(edit.Start + edit.Length)..];

    private static int On(string text, string words) => text.IndexOf(words, StringComparison.Ordinal) + 1;

    [Fact]
    public void A_value_that_is_one_of_a_few_lists_them_and_writes_the_one_chosen()
    {
        var (offer, said) = JsonOffers.Values(Config, On(Config, "\"check\""), NoFile);
        Assert.Null(said);
        Assert.Equal("type: What the field holds", offer!.Name);
        Assert.Equal(FieldTypes.All, offer.Lines);
        // The list starts on the value that stands there.
        Assert.Equal(FieldTypes.All.ToList().IndexOf("check"), offer.Selected);

        var edit = offer.Choose(FieldTypes.All.ToList().IndexOf("date"));
        var changed = Apply(Config, edit);
        Assert.Contains("{ \"title\": \"Done\", \"type\": \"date\", \"value\": false }", changed, StringComparison.Ordinal);
        Assert.Equal("type: date", edit.Said);
        Assert.Equal('"', changed[edit.Caret]);
        // Nothing else of the text changed.
        Assert.Equal(Config.Length - 1, changed.Length);
    }

    [Fact]
    public void The_caret_on_the_members_key_or_just_after_its_value_is_on_the_member()
    {
        Assert.Equal("does: What the action does", JsonOffers.Values(Config, On(Config, "\"does\""), NoFile).Offer!.Name);
        var afterValue = Config.IndexOf("\"move\"", StringComparison.Ordinal) + "\"move\"".Length;
        var offer = JsonOffers.Values(Config, afterValue, NoFile).Offer!;
        Assert.Equal(["set", "move", "open", "run", "terminal"], offer.Lines);
        Assert.Equal(1, offer.Selected);
    }

    [Fact]
    public void A_place_without_a_list_of_values_says_so_and_offers_nothing()
    {
        Assert.Equal("name takes any value. The action's name, as the menu shows it", JsonOffers.Values(Config, On(Config, "\"Later\""), NoFile).Said);
        Assert.StartsWith("No list of values here", JsonOffers.Values(Config, On(Config, "\"actions\""), NoFile).Said);
        Assert.StartsWith("No list of values here", JsonOffers.Values(Config, 0, NoFile).Said);
        Assert.StartsWith("axJSON has no options for this file", JsonOffers.Values("{ \"model\": \"opus\" }", 5, NoFile).Said);
        Assert.StartsWith("axJSON has no options for this file", JsonOffers.Values("{ \"format\": \"axkit-list 1\" }", 5, NoFile).Said);
        Assert.StartsWith("Not valid JSON: line 1", JsonOffers.Values("{ \"format\": ", 5, NoFile).Said);
        Assert.StartsWith("axJSON has no options for this file", JsonOffers.Members(string.Empty, 0, NoFile).Said);
    }

    [Fact]
    public void An_object_lists_the_members_it_lacks_and_adds_the_one_chosen_as_its_last()
    {
        var (offer, said) = JsonOffers.Members(Config, On(Config, "\"Later\""), NoFile);
        Assert.Null(said);
        Assert.Equal("Add a member to an action of the list", offer!.Name);
        Assert.Equal("key: The key that runs it; without one it runs from the menu", offer.Lines[0]);
        Assert.DoesNotContain(offer.Lines, line => line.StartsWith("name:", StringComparison.Ordinal) || line.StartsWith("does:", StringComparison.Ordinal) || line.StartsWith("to:", StringComparison.Ordinal));

        var edit = offer.Choose(0);
        var changed = Apply(Config, edit).ReplaceLineEndings("\n");
        Assert.Contains("      \"to\": \"../Later\",\n      \"key\": \"Ctrl+1\"\n    }", changed, StringComparison.Ordinal);
        Assert.Equal("Added key: The key that runs it; without one it runs from the menu", edit.Said);
        // The caret is on the new value, where the next command can list its values.
        Assert.Equal("\"Ctrl+1\"", Apply(Config, edit).Substring(edit.Caret, 8));
        Assert.Null(JsonParser.Parse(Apply(Config, edit)).Problem);
    }

    [Fact]
    public void An_object_on_one_line_stays_on_one_line_and_an_empty_one_opens()
    {
        // The field's object, on one line.
        var (offer, _) = JsonOffers.Members(Config, On(Config, "\"Done\""), NoFile);
        Assert.Equal(["options: The values to choose from, for the type choice"], offer!.Lines);
        Assert.Contains("{ \"title\": \"Done\", \"type\": \"check\", \"value\": false, \"options\": [] }", Apply(Config, offer.Choose(0)), StringComparison.Ordinal);

        const string empty = "{\n  \"format\": \"axkit-list 2\",\n  \"details\": {}\n}";
        var details = JsonOffers.Members(empty, empty.IndexOf("{}", StringComparison.Ordinal) + 1, NoFile).Offer!;
        Assert.Equal(["Notes: Notes about the list", "Section: The section of the top level the list stands in"], details.Lines);
        Assert.Equal("{\n  \"format\": \"axkit-list 2\",\n  \"details\": {\n    \"Section\": \"\"\n  }\n}", Apply(empty, details.Choose(1)));

        // The root, from a place in no member: what the configuration can still have.
        var root = JsonOffers.Members(empty, 1, NoFile).Offer!;
        Assert.Equal("Add a member to the configuration of a list", root.Name);
        Assert.Equal(2, root.Lines.Count);
        Assert.Null(JsonParser.Parse(Apply(empty, root.Choose(0))).Problem);
    }

    [Fact]
    public void A_trailing_comma_is_not_doubled_and_a_comment_before_the_brace_is_left_alone()
    {
        const string trailing = "{\n  \"format\": \"axkit-list 2\",\n  \"details\": {},\n}";
        var added = Apply(trailing, JsonOffers.Members(trailing, 1, NoFile).Offer!.Choose(0));
        Assert.Equal("{\n  \"format\": \"axkit-list 2\",\n  \"details\": {},\n  \"item fields\": []\n}", added);

        const string comment = "{\n  \"format\": \"axkit-list 2\" // the version\n}";
        var edit = JsonOffers.Members(comment, 1, NoFile).Offer!.Choose(0);
        Assert.Equal(comment, Apply(comment, edit));
        Assert.StartsWith("details was not added: a comment stands before the object's closing brace", edit.Said);
    }

    [Fact]
    public void An_item_gets_its_options_through_its_link_to_the_lists_configuration()
    {
        var (offer, said) = JsonOffers.Values(Item, On(Item, "\"Normal\""), Beside);
        Assert.Null(said);
        Assert.Equal("Priority: A choice of the list's options", offer!.Name);
        Assert.Equal(["High", "Normal", "Low"], offer.Lines);
        Assert.Equal(1, offer.Selected);
        Assert.Contains("\"Priority\": \"High\"", Apply(Item, offer.Choose(0)), StringComparison.Ordinal);

        // A check is true or false, written as the words.
        var done = JsonOffers.Values(Item, On(Item, "false"), Beside).Offer!;
        Assert.Equal(["true", "false"], done.Lines);
        Assert.Contains("\"Done\": true,", Apply(Item, done.Choose(0)), StringComparison.Ordinal);

        // A value the list does not name (kept, AF-11.3) has no options; the link not found, none at all.
        Assert.Equal("Sprint is not a member axJSON has options for", JsonOffers.Values(Item, On(Item, "\"old\""), Beside).Said);
        Assert.StartsWith("axJSON has no options for this file", JsonOffers.Values(Item, On(Item, "\"Normal\""), NoFile).Said);
        // The item has every field of its list: nothing to add.
        Assert.Equal("An item of a list has every member it can have", JsonOffers.Members(Item, 1, Beside).Said);
    }

    [Fact]
    public void A_link_is_followed_only_as_a_plain_file_name()
    {
        var asked = new List<string>();
        string? Record(string name)
        {
            asked.Add(name);
            return Config;
        }

        foreach (var link in new[] { @"..\\list.json", "sub/list.json", @"C:\\lists\\list.json", "" })
        {
            var text = $$"""{ "schema": "{{link}}", "Title": "x" }""";
            Assert.StartsWith("axJSON has no options for this file", JsonOffers.Values(text, 5, Record).Said);
        }

        Assert.Empty(asked);
    }

    [Fact]
    public void The_place_of_the_caret_is_the_innermost_value_with_the_way_to_it()
    {
        var root = JsonParser.Parse(Config).Root;
        var place = JsonOffers.At(root, On(Config, "\"High\""))!;
        Assert.Equal("High", place.Node.Value);
        Assert.Equal(["item fields", null, "options", null], place.Path);

        var action = JsonOffers.At(root, On(Config, "\"../Later\""))!;
        Assert.Equal(["actions", null, "to"], action.Path);
        Assert.Equal(TokenKind.OpenObject, action.Parent!.Kind);
        Assert.Empty(JsonOffers.At(root, 0)!.Path);
        Assert.Null(JsonOffers.At(null, 0));
    }
}
