using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>The built-in smart lists by date (SPEC.md TK-5.4) and the mentions and hashtags (TK-5.5), over a temporary folder.</summary>
public sealed class SmartListTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    private readonly TempFolder _top = new("axtask-smart");

    public SmartListTests()
    {
        Directory.CreateDirectory(Path.Combine(_top.Path, "Home"));
        Directory.CreateDirectory(Path.Combine(_top.Path, "Archive", "Old"));
        Item("Home", "Pay rent", done: false, due: "2026-11-01");
        Item("Home", "Call @Sam about #garden", done: false, due: "2026-11-11");
        Item("Home", "Book train", done: false, due: "2026-11-18");
        Item("Home", "Plan trip", done: false, due: "2026-11-19");
        Item("Home", "Read #Garden notes with @sam", done: false, due: null);
        Item("Home", "Write to #done-tag", done: true, due: "2026-11-01");
        Item(Path.Combine("Archive", "Old"), "Old #archived", done: false, due: null);
        File.WriteAllText(Path.Combine(_top.Path, "Home", "Plain.json"), ItemDocument.NewItem("Plain #plain", []).ToText());
    }

    public void Dispose() => _top.Dispose();

    private string[] Names(SmartList list) => list.Items(_top.Path, Today).Select(ItemFolder.NameOf).ToArray();

    [Fact]
    public void The_four_by_date_split_the_tasks_not_done()
    {
        Assert.Equal(["Pay rent"], Names(SmartList.Overdue));
        Assert.Equal(["Call @Sam about #garden"], Names(SmartList.DueToday));
        // Today through seven days on: the 18th is in, the 19th is not.
        Assert.Equal(["Call @Sam about #garden", "Book train"], Names(SmartList.DueInSevenDays));
        Assert.Equal(["Read #Garden notes with @sam"], Names(SmartList.NoDueDate));
    }

    [Fact]
    public void Built_ins_are_told_apart_by_name()
    {
        Assert.Equal(["All tasks", "Overdue", "Due today", "Due in the next 7 days", "No due date"], SmartList.BuiltIns.Select(list => list.Name));
        Assert.All(SmartList.BuiltIns, list => Assert.True(list.BuiltIn));
        Assert.Equal(SmartList.BuiltIns.Count, SmartList.BuiltIns.Select(list => list.Key).Distinct().Count());
        Assert.All(SmartList.BuiltIns, list => Assert.NotNull(list.Described));
    }

    [Fact]
    public void Tokens_start_after_no_letter_or_digit()
    {
        Assert.Equal(["@Sam", "@team-2"], Tags.In("Ask @Sam, then @team-2 and sam@example.org; @ alone", Tags.Mention));
        Assert.Equal(["#garden"], Tags.In("#garden and #Garden and x#no", Tags.Hashtag));
        Assert.True(Tags.Has("Call @sam today", "@Sam"));
        Assert.False(Tags.Has("Call @samuel today", "@Sam"));
    }

    [Fact]
    public void Mentions_and_hashtags_come_from_the_tasks_not_done()
    {
        var all = ItemFolder.AllItems(_top.Path);
        // Case not minded, shown as first seen; the done, the plain and the Archive left out.
        Assert.Single(Tags.InTasks(all, Tags.Mention));
        Assert.Single(Tags.InTasks(all, Tags.Hashtag));
        Assert.Equal(2, SmartList.Tagged("@sam").Items(_top.Path, Today).Count);
        Assert.Equal(2, SmartList.Tagged("#GARDEN").Items(_top.Path, Today).Count);
        Assert.Empty(SmartList.Tagged("#done-tag").Items(_top.Path, Today));
    }

    private void Item(string list, string title, bool done, string? due)
    {
        var document = ItemDocument.NewItem(title, TaskRules.DefaultTemplate);
        document = document.WithValue(document.IndexOf(TaskRules.DoneField), System.Text.Json.Nodes.JsonValue.Create(done));
        if (due is not null)
        {
            document = document.WithValue(document.IndexOf(TaskRules.DueField), System.Text.Json.Nodes.JsonValue.Create(due));
        }

        File.WriteAllText(Path.Combine(_top.Path, list, title + ".json"), document.ToText());
    }
}
