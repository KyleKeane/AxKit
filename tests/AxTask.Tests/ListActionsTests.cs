using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// The actions of a list and their values (docs/axTask/forms-framework.md sections 3, 4, 6 and 7): what the file
/// may hold, what is never run, the eighteen keys, and a value that stays one argument whatever it holds.
/// </summary>
public sealed class ListActionsTests
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    private static IReadOnlyList<ConfiguredAction> Read(string actions) =>
        ListActions.Read(ListConfig.Parse($$"""{ "format": "axkit-list 2", "item fields": [ { "title": "Number", "type": "number" }, { "title": "Answered", "type": "date" } ], "actions": {{actions}} }"""));

    private static ConfiguredAction One(string action) => Assert.Single(Read($"[ {action} ]"));

    private const string Answer = """
        { "name": "Answer the report on GitHub",
          "key": "Ctrl+Shift+1",
          "fields": [ { "title": "Answer", "type": "longtext" } ],
          "does": "run",
          "program": "gh",
          "arguments": ["issue", "comment", "{Number}", "--repo", "KyleKeane/AxKit-reports", "--body-file", "-"],
          "input": "{form:Answer}",
          "then": { "set": { "Answered": "{today}" } } }
        """;

    private static ActionContext Context(string item, IReadOnlyDictionary<string, string>? form = null) =>
        new(FlatItem.Parse(item),
            ListConfig.Parse("""{ "format": "axkit-list 2", "item fields": [ { "title": "Number", "type": "number" }, { "title": "Notes", "type": "longtext" }, { "title": "Sprint", "type": "text" } ] }"""),
            @"C:\lists\Reports\It froze.json", @"C:\lists\Reports", form ?? new Dictionary<string, string>(), Today);

    private const string Item = """{ "schema": "list.json", "Title": "It froze; rm -rf \"x\" & calc", "Number": 15, "Done": false, "Notes": "one\ntwo", "name": "a field called name" }""";

    // ---- what the file may hold ----

    [Fact]
    public void The_specifications_example_reads_whole_and_says_what_it_does()
    {
        var action = One(Answer);

        Assert.Null(action.NotUnderstood);
        Assert.Null(action.KeyProblem);
        Assert.Equal(("Answer the report on GitHub", ListActions.RunKind, 10), (action.Name, action.Does, action.Key));
        Assert.Equal("Ctrl+Shift+1", ListActions.KeyText(action.Key!.Value));
        Assert.Equal("gh", action.Program);
        Assert.Equal(7, action.Arguments.Count);
        Assert.Equal("{form:Answer}", action.Input);
        Assert.Null(action.Folder);
        Assert.Equal(60, action.Seconds);
        Assert.False(action.HasHiddenField);
        var field = Assert.Single(action.Fields);
        Assert.Equal(("Answer", FieldTypes.LongText, true, false), (field.Title, field.Type, field.Required, field.Hidden));
        Assert.Equal(
            "runs gh with 7 arguments: issue, comment, {Number}, --repo, KyleKeane/AxKit-reports, --body-file, -; gives it {form:Answer} as input; then sets Answered: {today}",
            action.Described);
        Assert.Equal("Answer the report on GitHub", action.Label);
    }

    [Fact]
    public void The_three_kinds_the_action_keys_had_read_as_they_were()
    {
        var actions = Read("""
            [ { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true, "Closed": "{today}" } },
              { "name": "Later", "key": "ctrl + 2", "does": "move", "to": "../Later" },
              { "name": "Web", "does": "open", "open": "Issue" },
              { "name": "Look", "key": "Ctrl+Shift+9", "does": "terminal", "command": ["gh", "issue", "view", "{Number}"], "folder": "..\\code" } ]
            """);

        Assert.All(actions, action => Assert.Null(action.NotUnderstood));
        Assert.Equal([1, 2, null, 18], actions.Select(action => action.Key));
        Assert.Equal("sets Done: done, Closed: {today}", actions[0].Described);
        Assert.Equal("moves to ../Later", actions[1].Described);
        Assert.Equal("opens Issue", actions[2].Described);
        Assert.Equal("puts a command of 4 parts into axTerminal's command field, for you to send: gh, issue, view, {Number}; in the folder ..\\code", actions[3].Described);
        Assert.Empty(ListActions.Read(null));
        Assert.Empty(ListActions.Read(ListConfig.New([])));
    }

    /// <summary>
    /// AF-12.1: the options table axJSON offers a configuration from (<c>AxKit.Core/ListOptions</c>) names what this
    /// reader takes: the kinds, the keys, a form's field types, and members that read as an action when each holds
    /// its first value.
    /// </summary>
    [Fact]
    public void The_options_table_names_what_the_reader_takes()
    {
        Assert.Equal([ListActions.SetKind, ListActions.MoveKind, ListActions.OpenKind, ListActions.RunKind, ListActions.TerminalKind], ListOptions.ActionKinds);
        Assert.Equal(Enumerable.Range(1, ListActions.KeyCount).Select(ListActions.KeyText), ListOptions.ActionKeys);
        Assert.All(ListOptions.ActionKeys, key => Assert.NotNull(ListActions.ParseKey(key)));
        Assert.Equal(ListActions.FieldTypesOfAForm, ListOptions.FormFieldTypes);
        Assert.Equal([ListActions.ListFolder], ListOptions.Table.Kind("action")!.Member("folder")!.Values);
        Assert.Equal(ListActions.DefaultSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture), ListOptions.Table.Kind("action")!.Member("seconds")!.First);

        // Each kind of action, written with the table's members alone, is understood.
        var members = ListOptions.Table.Kind("action")!.Members.Select(member => member.Name).ToHashSet();
        foreach (var action in new[]
        {
            """{ "name": "A", "key": "Ctrl+1", "does": "set", "set": { "Done": true } }""",
            """{ "name": "B", "does": "move", "to": "../Later" }""",
            """{ "name": "C", "does": "open", "open": "Address" }""",
            """{ "name": "D", "does": "run", "program": "gh", "arguments": ["x"], "input": "{form:Say}", "folder": "list", "seconds": 60, "then": { "set": { "Done": true } }, "fields": [ { "title": "Say", "type": "text", "value": "", "options": [], "required": true, "hidden": false } ] }""",
            """{ "name": "E", "does": "terminal", "command": ["gh", "x"], "folder": "list" }""",
        })
        {
            var read = Assert.Single(Read($"[ {action} ]"));
            Assert.Null(read.NotUnderstood);
            Assert.All(((System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(action)!).Select(pair => pair.Key), name => Assert.Contains(name, members));
        }

        Assert.Equal(["title", "type", "value", "options", "required", "hidden"], ListOptions.Table.Kind("form field")!.Members.Select(member => member.Name));
        Assert.Equal(["set"], ListOptions.Table.Kind("then")!.Members.Select(member => member.Name));
    }

    /// <summary>TK-7a.3 (bundle repairs R-72): a list's file may come from elsewhere, so what leaves the list is said before it is done.</summary>
    [Fact]
    public void An_action_that_moves_or_opens_asks_first_and_one_that_sets_fields_does_not()
    {
        var actions = Read("""
            [ { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true } },
              { "name": "Later", "key": "Ctrl+2", "does": "move", "to": "../Later" },
              { "name": "Web", "does": "open", "open": "Issue" },
              { "name": "Answer", "does": "run", "program": "gh", "arguments": ["issue", "view"] } ]
            """);

        Assert.False(actions[0].AsksFirst);
        Assert.True(actions[1].AsksFirst);
        Assert.True(actions[2].AsksFirst);
        // A program has its own question, with everything that would run (AF-7.2).
        Assert.False(actions[3].AsksFirst);
    }

    [Theory]
    [InlineData("Ctrl+1", 1)]
    [InlineData("ctrl+9", 9)]
    [InlineData("Ctrl+Shift+1", 10)]
    [InlineData(" CTRL + SHIFT + 9 ", 18)]
    [InlineData("Ctrl+0", null)]
    [InlineData("Ctrl+Alt+1", null)]
    [InlineData("Alt+1", null)]
    [InlineData("Ctrl+J", null)]
    [InlineData("Ctrl+10", null)]
    [InlineData("1", null)]
    public void Eighteen_keys_and_no_other(string text, int? key)
    {
        Assert.Equal(key, ListActions.ParseKey(text));
        if (key is { } known)
        {
            Assert.Equal(known, ListActions.ParseKey(ListActions.KeyText(known)));
        }
    }

    [Fact]
    public void The_first_action_of_a_key_has_it_and_a_second_still_runs_from_the_menu()
    {
        var actions = Read("""
            [ { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true } },
              { "name": "Also", "key": "Ctrl+1", "does": "set", "set": { "Done": false } },
              { "name": "Odd key", "key": "Ctrl+J", "does": "set", "set": { "Done": false } } ]
            """);

        Assert.Equal(1, actions[0].Key);
        Assert.Null(actions[1].Key);
        Assert.Null(actions[1].NotUnderstood);
        Assert.Equal("Also, not understood: Ctrl+1 is Done's", actions[1].Label);
        Assert.Null(actions[2].Key);
        Assert.Null(actions[2].NotUnderstood);
        Assert.StartsWith("Odd key, not understood: its key is none of Ctrl+1 to Ctrl+9", actions[2].Label);
    }

    [Theory]
    [InlineData("""{ "does": "set", "set": { "Done": true } }""", "it has no name")]
    [InlineData("""{ "name": "X" }""", "it does not say what it does")]
    [InlineData("""{ "name": "X", "does": "ask" }""", "it does ask, which this version does not know")]
    [InlineData("""{ "name": "X", "does": "set" }""", "it sets nothing")]
    [InlineData("""{ "name": "X", "does": "set", "set": { "schema": "other.json" } }""", "its \"set\" names a field that cannot be set, or a value that is a list or an object")]
    [InlineData("""{ "name": "X", "does": "move" }""", "it does not say which list it moves to (\"to\")")]
    [InlineData("""{ "name": "X", "does": "move", "to": "../{Kind}" }""", "the list it moves to holds a placeholder")]
    [InlineData("""{ "name": "X", "does": "open", "open": 3 }""", "it does not say what it opens (\"open\")")]
    [InlineData("""{ "name": "X", "does": "run" }""", "it does not name its program (\"program\")")]
    [InlineData("""{ "name": "X", "does": "run", "program": "{Tool}" }""", "its program holds a placeholder or a control character: the program is fixed in the file")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "arguments": "issue view" }""", "its arguments are not a list of texts")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "arguments": ["issue", 15] }""", "its arguments are not a list of texts")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "arguments": ["one\ntwo"] }""", "one of its arguments holds a line break or another control character")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "arguments": ["{Number"] }""", "one of its texts holds a brace without its pair")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "arguments": ["{form:Answer}"] }""", "it names {form:Answer}, and its form has no field Answer")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "seconds": 601 }""", "its seconds are not a number from 1 to 600")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "folder": "{folder}\\x" }""", "its folder is not a path written in the file")]
    [InlineData("""{ "name": "X", "does": "run", "program": "gh", "then": { "set": [1] } }""", "its \"then, set\" is not an object of fields and values")]
    [InlineData("""{ "name": "X", "does": "terminal", "command": [] }""", "its command names no program")]
    [InlineData("""{ "name": "X", "does": "terminal", "command": ["{Tool}", "x"] }""", "the program of its command holds a placeholder: the program is fixed in the file")]
    [InlineData("""{ "name": "X", "does": "set", "set": { "A": "b" }, "fields": [ { "type": "text" } ] }""", "a field of its form has no title")]
    [InlineData("""{ "name": "X", "does": "set", "set": { "A": "b" }, "fields": [ { "title": "Kind", "type": "check" } ] }""", "the field Kind of its form has the type check; a form takes text, longtext, choice and date")]
    [InlineData("""{ "name": "X", "does": "set", "set": { "A": "b" }, "fields": [ { "title": "Kind", "type": "choice" } ] }""", "the choice Kind of its form has no options")]
    [InlineData("""{ "name": "X", "does": "set", "set": { "A": "b" }, "fields": [ { "title": "Kind" }, { "title": "kind" } ] }""", "its form has two fields named kind")]
    public void What_is_not_understood_is_listed_with_its_reason_and_never_run(string action, string why)
    {
        var read = One(action);

        Assert.Equal(why, read.NotUnderstood);
        Assert.Null(read.Key);
        Assert.EndsWith(", not understood: " + why, read.Label);
    }

    [Fact]
    public void One_bad_action_leaves_the_others_working_and_a_second_of_a_name_is_not_run()
    {
        var actions = Read("""
            [ "not an object",
              { "name": "Done", "does": "set", "set": { "Done": true } },
              { "name": "done", "does": "set", "set": { "Done": false } },
              { "name": "A name that is far too long to be said every time the menu is read, over sixty", "does": "set", "set": { "Done": true } } ]
            """);

        Assert.Equal("it is not an object of names and values", actions[0].NotUnderstood);
        Assert.Null(actions[1].NotUnderstood);
        Assert.Equal("another action of this list has this name", actions[2].NotUnderstood);
        Assert.Equal("its name is longer than 60 characters", actions[3].NotUnderstood);
    }

    [Fact]
    public void A_hidden_field_is_for_a_run_actions_input_alone()
    {
        var good = One("""{ "name": "Sign in", "does": "run", "program": "tool", "arguments": ["login", "--stdin"], "input": "{form:Token}", "fields": [ { "title": "Token", "hidden": true } ] }""");
        Assert.Null(good.NotUnderstood);
        Assert.True(good.HasHiddenField);

        Assert.Equal("the hidden field Token may fill the input alone",
            One("""{ "name": "Sign in", "does": "run", "program": "tool", "arguments": ["login", "{form:Token}"], "fields": [ { "title": "Token", "hidden": true } ] }""").NotUnderstood);
        Assert.Equal("the hidden field Token may fill the input alone",
            One("""{ "name": "Sign in", "does": "run", "program": "tool", "input": "x", "then": { "set": { "Key": "{form:Token}" } }, "fields": [ { "title": "Token", "hidden": true } ] }""").NotUnderstood);
        Assert.Equal("the hidden field Token is for a run action's input alone, as a text",
            One("""{ "name": "Show", "does": "terminal", "command": ["tool"], "fields": [ { "title": "Token", "hidden": true } ] }""").NotUnderstood);
    }

    [Fact]
    public void An_acceptance_is_for_one_list_and_the_whole_action_as_the_file_holds_it()
    {
        var action = One(Answer);
        var mark = ListActions.MarkText(@"C:\Lists\Reports", action);

        Assert.Equal(mark, ListActions.MarkText(@"c:\lists\reports\", One(Answer)));
        Assert.NotEqual(mark, ListActions.MarkText(@"C:\Lists\Other", action));
        // Any change to the action is another text: an argument, the program, the input, the name.
        Assert.NotEqual(mark, ListActions.MarkText(@"C:\Lists\Reports", One(Answer.Replace("AxKit-reports", "AxKit", StringComparison.Ordinal))));
        Assert.NotEqual(mark, ListActions.MarkText(@"C:\Lists\Reports", One(Answer.Replace("\"gh\"", "\"gh2\"", StringComparison.Ordinal))));
        Assert.NotEqual(mark, ListActions.MarkText(@"C:\Lists\Reports", One(Answer.Replace("--body-file", "--body", StringComparison.Ordinal))));
    }

    // ---- the values ----

    [Fact]
    public void Names_in_braces_are_found_and_doubled_braces_are_braces()
    {
        Assert.Equal(["Number", "form:Answer", "today"], ActionValues.Names("#{Number}: {form:Answer} on {today}"));
        Assert.Empty(ActionValues.Names("{{literal}} and plain")!);
        Assert.Null(ActionValues.Names("{Number"));
        Assert.Null(ActionValues.Names("Number}"));
        Assert.Null(ActionValues.Names("{}"));
        Assert.Null(ActionValues.Names("{a{b}}"));
        Assert.True(ActionValues.IsFixed("gh"));
        Assert.False(ActionValues.IsFixed("{Tool}"));
        Assert.Equal("Answer", ActionValues.FormFieldOf("form:Answer"));
        Assert.Null(ActionValues.FormFieldOf("Answer"));
    }

    [Fact]
    public void A_value_fills_the_one_text_it_stands_in_whatever_it_holds()
    {
        // The Title holds quotes, a semicolon and an ampersand: it is that text, as one argument (AF-4.2).
        var context = Context(Item, new Dictionary<string, string> { ["Answer"] = "Thank you.\nFixed in the next version." });
        var action = One("""
            { "name": "Answer", "does": "run", "program": "gh",
              "arguments": ["issue", "comment", "{Number}", "--title", "Re: {Title}", "{{braces}}", "{file}", "{name}", "{folder}", "{today}", "{Sprint}", "{Done}", "{field:name}"],
              "input": "{form:Answer}", "fields": [ { "title": "Answer", "type": "longtext" } ],
              "then": { "set": { "Answered": "{today}", "Done": true } } }
            """);

        var (run, refused) = ActionValues.Run(action, context);

        Assert.Null(refused);
        Assert.Equal("gh", run!.Program);
        Assert.Equal(
            ["issue", "comment", "15", "--title", "Re: It froze; rm -rf \"x\" & calc", "{braces}", @"C:\lists\Reports\It froze.json", "It froze", @"C:\lists\Reports", "2026-11-21", string.Empty, "false", "a field called name"],
            run.Arguments);
        Assert.Equal("Thank you.\nFixed in the next version.", run.Input);
        Assert.Equal(@"C:\lists\Reports", run.Folder);
        Assert.Equal("2026-11-21", FieldTypes.TextOf(run.Then["Answered"]));
        Assert.True(FieldTypes.IsTrue(run.Then["done"]));
    }

    [Fact]
    public void A_name_the_item_or_the_form_does_not_have_refuses_the_action_before_anything_happens()
    {
        var context = Context(Item);

        Assert.Equal("Look names {Version}, which this item does not have",
            ActionValues.Terminal(One("""{ "name": "Look", "does": "terminal", "command": ["gh", "{Version}"] }"""), context).Refused);
        // The form's values are not there (the form was not filled): the name is refused, never left as it is.
        Assert.Equal("Answer the report on GitHub names {form:Answer}, which its form does not have", ActionValues.Run(One(Answer), context).Refused);
    }

    [Fact]
    public void A_line_break_is_refused_in_an_argument_and_taken_by_the_input()
    {
        var context = Context(Item, new Dictionary<string, string> { ["Words"] = "typed\nover two lines" });

        Assert.Equal("The field Notes holds a line break, which an argument cannot; an action gives such a text as its input",
            ActionValues.Run(One("""{ "name": "Note", "does": "run", "program": "tool", "arguments": ["{Notes}"] }"""), context).Refused);
        Assert.Equal("What you typed for Words holds a line break, which an argument cannot; an action gives such a text as its input",
            ActionValues.Terminal(One("""{ "name": "Say", "does": "terminal", "command": ["tool", "{form:Words}"], "fields": [ { "title": "Words", "type": "longtext" } ] }"""), context).Refused);
        Assert.Equal("one\ntwo", ActionValues.Run(One("""{ "name": "Note", "does": "run", "program": "tool", "input": "{Notes}" }"""), context).Run!.Input);
    }

    [Fact]
    public void An_action_runs_in_the_lists_folder_or_in_the_one_it_names()
    {
        var context = Context(Item);

        Assert.Equal(@"C:\lists\Reports", ActionValues.Terminal(One("""{ "name": "A", "does": "terminal", "command": ["gh"], "folder": "list" }"""), context).Terminal!.Folder);
        Assert.Equal(@"C:\lists\code", ActionValues.Terminal(One("""{ "name": "A", "does": "terminal", "command": ["gh"], "folder": "..\\code" }"""), context).Terminal!.Folder);
        Assert.Equal(@"D:\work", ActionValues.Terminal(One("""{ "name": "A", "does": "terminal", "command": ["gh"], "folder": "D:\\work" }"""), context).Terminal!.Folder);
    }

    [Fact]
    public void A_set_actions_texts_are_filled_and_its_other_values_stay()
    {
        var (set, refused) = ActionValues.Set(One("""{ "name": "Close", "does": "set", "set": { "Closed": "{today}", "Done": true, "By": "{{me}}" } }""").Set, Context(Item), "Close");

        Assert.Null(refused);
        Assert.Equal("2026-11-21", FieldTypes.TextOf(set!["Closed"]));
        Assert.True(FieldTypes.IsTrue(set["Done"]));
        Assert.Equal("{me}", FieldTypes.TextOf(set["By"]));
    }
}
