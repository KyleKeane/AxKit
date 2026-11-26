using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// What the actions of a list share with the action keys they grew from (docs/axKit/reports.md RP-8, SPEC.md
/// TK-7a): a set applied and said, the open rule, the acceptance under the seal, and the program an action names.
/// The actions themselves are <see cref="ListActionsTests"/>'.
/// </summary>
public sealed class ActionKeysTests
{
    [Fact]
    public void Applying_a_set_changes_the_fields_and_adds_a_missing_one()
    {
        var item = ItemDocument.NewItem("Fix the tap", [new ItemField("Done", FieldTypes.Check, JsonValue.Create(false), [])]);
        var set = new Dictionary<string, JsonNode?> { ["Done"] = JsonValue.Create(true), ["Kind"] = JsonValue.Create("Bug"), ["Size"] = JsonValue.Create(3) };

        var changed = ActionKeys.ApplySet(item, set);

        Assert.True(FieldTypes.IsTrue(changed.Fields[changed.IndexOf("Done")].Value));
        Assert.Equal("Bug", FieldTypes.TextOf(changed.Fields[changed.IndexOf("Kind")].Value));
        Assert.Equal(FieldTypes.Text, changed.Fields[changed.IndexOf("Kind")].Type);
        Assert.Equal(FieldTypes.Number, changed.Fields[changed.IndexOf("Size")].Type);
        Assert.Equal("Done: done, Kind: Bug, Size: 3", ActionKeys.SpokenSet(set));
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

    /// <summary>
    /// The action keys of the earlier format come through the conversion as the same keys and the same actions, and
    /// so with the acceptances given then (TK-7a.3): the mark the window makes of a converted action on Ctrl+digit is
    /// the mark that was sealed before. A list made as a GitHub list before 2026-11-14 (TK-7a.2) keeps its
    /// <c>github</c> member, which means nothing.
    /// </summary>
    [Fact]
    public void An_acceptance_of_a_key_of_the_earlier_format_stands_after_the_conversion()
    {
        const string text = """
            {
              "format": "axkit-list 1",
              "fields": [ { "title": "Notes", "type": "longtext", "value": "" } ],
              "new item": [ { "title": "Kind", "type": "choice", "value": "unlabelled", "options": ["Bug", "unlabelled"] } ],
              "github": { "repository": "owner/repo", "state": "open" },
              "keys": { "1": { "name": "Make it a bug", "set": { "Kind": "Bug" } }, "5": { "name": "Open on GitHub", "open": "Issue" }, "7": { "name": "Later", "move": "../Later" } }
            }
            """;
        var converted = AxKit.Core.Conversion.EarlierFormat.Convert(text, []).Config;
        var list = ListConfig.Parse(converted);
        var actions = ListActions.Read(list);

        Assert.Equal([1, 5, 7], actions.Select(action => action.Key));
        Assert.All(actions, action => Assert.Null(action.NotUnderstood));
        Assert.Equal("Kind", Assert.Single(TaskRules.TemplateOf(list)).Title);
        Assert.Contains("owner/repo", converted, StringComparison.Ordinal);

        // What the window seals over for a move and an open on Ctrl+digit is what the earlier file held, to the letter.
        var later = actions[2];
        var open = actions[1];
        Assert.Equal("{\"name\":\"Later\",\"move\":\"../Later\"}", new ListAction(later.Name, Move: later.To).ToJson().ToJsonString());
        Assert.Equal("{\"name\":\"Open on GitHub\",\"open\":\"Issue\"}", new ListAction(open.Name, Open: open.Open).ToJson().ToJsonString());
    }

    // ---- an acceptance is kept under the seal (TK-7a.3, AF-7.2; bundle repairs.md R-72) ----

    [Fact]
    public void An_acceptance_is_for_one_list_one_key_and_one_action_and_is_kept_under_the_seal()
    {
        using var temp = new TempFolder("axtask-keys");
        var path = Path.Combine(temp.Path, "axTask.json");
        var home = Path.Combine(temp.Path, "tasks", "Home");
        var move = new ListAction("Later", Move: "../Later");
        var settings = TaskSettings.Load(path, out _);
        Assert.False(settings.AcceptsActionKey(path, home, 2, move));

        // An acceptance of a Ctrl+digit key as it was kept before 2026-11-21.
        settings.AcceptedActionKeys.Add(TaskSettings.ActionKeyMark(path, home, 2, move));
        settings.Save(path);
        var back = TaskSettings.Load(path, out _);
        Assert.True(back.AcceptsActionKey(path, home.ToUpperInvariant() + Path.DirectorySeparatorChar, 2, move));

        // Another list, another key, another target, another kind and another name are each not accepted.
        Assert.False(back.AcceptsActionKey(path, Path.Combine(temp.Path, "tasks", "Work"), 2, move));
        Assert.False(back.AcceptsActionKey(path, home, 3, move));
        Assert.False(back.AcceptsActionKey(path, home, 2, move with { Move = "../Archive" }));
        Assert.False(back.AcceptsActionKey(path, home, 2, new ListAction("Later", Open: "../Later")));
        Assert.False(back.AcceptsActionKey(path, home, 2, move with { Name = "Sooner" }));

        // Nothing of the list is readable in the file, and a line written into it by hand accepts nothing.
        Assert.DoesNotContain("Later", File.ReadAllText(path), StringComparison.Ordinal);
        var open = new ListAction("Shell", Open: "terminal");
        back.AcceptedActionKeys.Add(ActionKeys.MarkText(home, 5, open));
        back.AcceptedActionKeys.Add(Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ActionKeys.MarkText(home, 5, open)))));
        Assert.False(back.AcceptsActionKey(path, home, 5, open));

        // The settings of another install (another key) accept nothing of this file's.
        using var other = new TempFolder("axtask-keys");
        var otherPath = Path.Combine(other.Path, "axTask.json");
        File.Copy(path, otherPath);
        Assert.False(TaskSettings.Load(otherPath, out _).AcceptsActionKey(otherPath, home, 2, move));
    }

    [Fact]
    public void An_action_that_runs_a_program_is_accepted_whole_and_any_change_to_it_is_not()
    {
        using var temp = new TempFolder("axtask-keys");
        var path = Path.Combine(temp.Path, "axTask.json");
        var home = Path.Combine(temp.Path, "tasks", "Home");
        static ConfiguredAction Action(string arguments) => ListActions.Read(ListConfig.Parse(
            $$"""{ "format": "axkit-list 2", "actions": [ { "name": "Answer", "does": "run", "program": "gh", "arguments": {{arguments}} } ] }"""))[0];
        var action = Action("""["issue", "view", "{Number}"]""");
        var settings = TaskSettings.Load(path, out _);
        Assert.False(settings.AcceptsAction(path, ListActions.MarkText(home, action)));

        Assert.True(settings.AcceptAction(path, ListActions.MarkText(home, action)));
        Assert.False(settings.AcceptAction(path, ListActions.MarkText(home, action)));
        settings.Save(path);
        var back = TaskSettings.Load(path, out _);
        Assert.True(back.AcceptsAction(path, ListActions.MarkText(home, Action("""["issue", "view", "{Number}"]"""))));
        // An argument changed in the file asks again; so does the same action in another list.
        Assert.False(back.AcceptsAction(path, ListActions.MarkText(home, Action("""["issue", "close", "{Number}"]"""))));
        Assert.False(back.AcceptsAction(path, ListActions.MarkText(Path.Combine(temp.Path, "tasks", "Work"), action)));
        // Nothing of the action is readable in the settings, and its mark written in by hand accepts nothing.
        Assert.DoesNotContain("issue", File.ReadAllText(path), StringComparison.Ordinal);
        var fresh = TaskSettings.Load(Path.Combine(temp.Path, "other.json"), out _);
        fresh.AcceptedActionKeys.Add(ListActions.MarkText(home, action));
        Assert.False(fresh.AcceptsAction(Path.Combine(temp.Path, "other.json"), ListActions.MarkText(home, action)));
    }

    [Fact]
    public void The_acceptances_are_kept_to_a_limit_the_oldest_going_first()
    {
        using var temp = new TempFolder("axtask-keys");
        var path = Path.Combine(temp.Path, "axTask.json");
        var settings = TaskSettings.Load(path, out _);
        var first = new ListAction("First", Move: "../Later");
        settings.AcceptedActionKeys.Add(TaskSettings.ActionKeyMark(path, temp.Path, 1, first));
        settings.AcceptedActionKeys.AddRange(Enumerable.Range(0, TaskSettings.MaxAcceptedActionKeys - 1).Select(i => "mark " + i));
        Assert.True(settings.AcceptsActionKey(path, temp.Path, 1, first));
        Assert.True(settings.AcceptAction(path, "the newest action"));
        Assert.Equal(TaskSettings.MaxAcceptedActionKeys, settings.AcceptedActionKeys.Count);
        Assert.False(settings.AcceptsActionKey(path, temp.Path, 1, first));
        Assert.True(settings.AcceptsAction(path, "the newest action"));
    }

    // ---- the program a run action names (forms-framework.md AF-3.2, AF-4.3) ----

    [Fact]
    public void A_program_is_found_on_the_search_path_or_by_a_full_path_and_never_in_the_lists_folder()
    {
        using var temp = new TempFolder("axtask-program");
        var tools = Directory.CreateDirectory(Path.Combine(temp.Path, "tools")).FullName;
        var list = Directory.CreateDirectory(Path.Combine(temp.Path, "lists", "Home")).FullName;
        File.WriteAllText(Path.Combine(tools, "gh.exe"), string.Empty);
        File.WriteAllText(Path.Combine(tools, "build.cmd"), string.Empty);
        File.WriteAllText(Path.Combine(tools, "notes.txt"), string.Empty);
        File.WriteAllText(Path.Combine(list, "evil.exe"), string.Empty);
        // The list's own folder is on the search path here, and still nothing is taken from it.
        var path = $"{list};relative\\folder;{tools}";
        static bool Local(string found) => true;

        Assert.Equal(Path.Combine(tools, "gh.exe"), ActionProgram.Find("gh", list, path, Local).Path);
        Assert.Equal(Path.Combine(tools, "gh.exe"), ActionProgram.Find(" gh.exe ", list, path, Local).Path);
        Assert.Equal(Path.Combine(tools, "build.cmd"), ActionProgram.Find("build", list, path, Local).Path);
        Assert.Equal(Path.Combine(tools, "gh.exe"), ActionProgram.Find(Path.Combine(tools, "gh.exe"), list, path, Local).Path);

        Assert.Contains("which is not there", ActionProgram.Find("no-such-tool", list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("inside the list's own folder", ActionProgram.Find("evil", list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("inside the list's own folder", ActionProgram.Find(Path.Combine(list, "evil.exe"), list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("a path that is not a full one", ActionProgram.Find(@"tools\gh.exe", list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("a path that is not a full one", ActionProgram.Find(@"..\tools\gh.exe", list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("is no program file", ActionProgram.Find(Path.Combine(tools, "notes.txt"), list, path, Local).Refused, StringComparison.Ordinal);
        Assert.Contains("not on a drive of this computer", ActionProgram.Find("gh", list, path, _ => false).Refused, StringComparison.Ordinal);
    }
}
