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
    public void A_tag_is_a_symbol_after_white_space_and_before_a_letter()
    {
        Assert.Equal(["@Sam", "@team-2", "#ui", "$budget", "%done", "&team", "^up", "§law"],
            Tags.In("Ask @Sam, then @team-2 and #ui with $budget %done &team ^up §law"));
        // Case not minded, the first kept.
        Assert.Equal(["#garden"], Tags.In("#garden and #Garden"));
        // Not after a letter or a digit, not before a digit or a space, never an excluded symbol.
        Assert.Empty(Tags.In("sam@example.org x#no $5 @ alone #1 (note [link] {x} <tag> \"quote 'single `code *bold _it ~del -item /path \\win .dot ,c ;s :c =e +p |pipe"));
        Assert.True(Tags.Has("Call @sam today", "@Sam"));
        Assert.False(Tags.Has("Call @samuel today", "@Sam"));
    }

    [Fact]
    public void Exclamation_and_question_marks_are_symbols_before_a_word_only()
    {
        // "!urgent" and "?maybe" are tags; the marks that end a word or a sentence never start one.
        Assert.Equal(["!urgent", "?maybe"], Tags.In("Do it now! !urgent, or later? ?maybe !! ?? !5"));
        Assert.Equal("! exclamation mark", Tags.Spoken('!'));
        Assert.Equal("? question mark", Tags.Spoken('?'));
    }

    [Fact]
    public void A_symbol_is_spoken_by_its_name()
    {
        Assert.Equal("@ at sign", Tags.Spoken('@'));
        Assert.Equal("# number sign", Tags.Spoken('#'));
        Assert.Equal("§ section sign", Tags.Spoken('§'));
        Assert.Equal("※ U+203B", Tags.Spoken('※'));
        Assert.All(Tags.Excluded, symbol => Assert.False(Tags.IsSymbol(symbol)));
    }

    [Fact]
    public void Smart_tags_come_from_the_tasks_not_done_at_then_number_sign_first()
    {
        Item("Home", "Plan $budget with @Lee", done: false, due: "2026-11-30");
        var all = ItemFolder.AllItems(_top.Path);
        // The done, the plain and the Archive left out; @ and # first, then by first appearance; each symbol's tags in natural order.
        var bySymbol = Tags.InTasks(all);
        Assert.Equal(['@', '#', '$'], bySymbol.Select(group => group.Symbol));
        Assert.Equal(["@Lee", "@Sam"], bySymbol[0].Tags);
        Assert.Equal(["#garden"], bySymbol[1].Tags);
        Assert.Equal(2, SmartList.Tagged("@sam").Items(_top.Path, Today).Count);
        Assert.Equal(2, SmartList.Tagged("#GARDEN").Items(_top.Path, Today).Count);
        Assert.Empty(SmartList.Tagged("#done-tag").Items(_top.Path, Today));
    }

    [Fact]
    public void Smart_lists_are_grouped_by_a_shared_first_word()
    {
        foreach (var name in new[] { "App axDown", "App axClaude", "Tag ai", "tag bug", "Soon", "Appendix" })
        {
            File.WriteAllText(Path.Combine(_top.Path, name + ItemFolder.FilterExtension), SmartList.NewFileText());
        }

        var (loose, groups) = SmartList.Grouped(SmartList.InFolder(_top.Path));
        Assert.Equal(["Appendix", "Soon"], loose.Select(list => list.Name));
        Assert.Equal(["App", "Tag"], groups.Select(group => group.Word));
        Assert.Equal(["App axClaude", "App axDown"], groups[0].Lists.Select(list => list.Name));
        Assert.Equal(["Tag ai", "tag bug"], groups[1].Lists.Select(list => list.Name));
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
