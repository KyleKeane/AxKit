using AxKit.Core;
using AxKit.Core.Reports;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>A GitHub list of axTask, the repository's issues as items (docs/axKit/reports.md RP-7.1).</summary>
public sealed class GitHubListTests : IDisposable
{
    private readonly string _top = Path.Combine(Path.GetTempPath(), "axkit-tests", "github-" + Guid.NewGuid().ToString("N"));
    private readonly string _list;
    private readonly string _archive;

    public GitHubListTests()
    {
        _list = Path.Combine(_top, "Issues");
        _archive = Path.Combine(_top, "Archive");
        Directory.CreateDirectory(_list);
        Directory.CreateDirectory(_archive);
    }

    public void Dispose() => Directory.Delete(_top, recursive: true);

    private static IssueInfo Issue(int number, string title, string label, string body = "words", string state = "open") =>
        new(number, title, body, state, label.Length == 0 ? [] : [label], "axkit-reporter", new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 7, 11, 0, 0, DateTimeKind.Utc), $"https://github.com/KyleKeane/AxKit-reports/issues/{number}");

    [Fact]
    public void A_new_list_carries_the_repository_the_template_and_the_default_keys()
    {
        var list = GitHubList.NewList("KyleKeane/AxKit-reports", "open", @"C:\src\axit");
        var written = ItemDocument.Parse(list.ToText());
        var settings = GitHubList.Of(written);
        Assert.NotNull(settings);
        // The written template keeps its values (2026-11-09): a Ctrl+N item in the list starts as unlabelled and open.
        Assert.Equal(GitHubList.Unlabelled, FieldTypes.TextOf(written.Template.First(f => f.Title == GitHubList.KindField).Value));
        Assert.Equal(GitHubList.OpenState, FieldTypes.TextOf(written.Template.First(f => f.Title == GitHubList.StateField).Value));
        Assert.Equal("KyleKeane/AxKit-reports", settings.Repository);
        Assert.Equal("open", settings.State);
        Assert.Equal(@"C:\src\axit", settings.Folder);
        Assert.Equal(GitHubList.Template.Select(f => f.Title), list.Template.Select(f => f.Title));
        var keys = ActionKeys.Read(list);
        Assert.Equal([1, 2, 3, 4, 5, 6], keys.Keys.Order());
        Assert.Equal("Close", keys[4].Name);
        Assert.Equal("Issue", keys[5].Open);
        Assert.Equal(GitHubList.AxClaudeTarget, keys[6].Open);

        Assert.Null(GitHubList.Of(ItemDocument.NewList([])));
        Assert.Null(GitHubList.Of(ItemDocument.Parse("{\"format\": \"axkit-list 1\", \"fields\": [], \"github\": {\"repository\": \"not a repo\"}}")));
        Assert.True(GitHubList.IsRepository("KyleKeane/AxKit-reports"));
        Assert.False(GitHubList.IsRepository("KyleKeane"));
    }

    [Fact]
    public void Refresh_writes_new_issues_keeps_notes_and_archives_the_gone()
    {
        var body = "The list loses my place.\n\n### Context\n\nSent from axKit 5.1.0 with F4; reporter 6f1c2d3e-4a5b-4c6d-8e7f-901234567890\n";
        var first = GitHubList.Refresh(_list, [Issue(12, "The list loses my place", "bug", body), Issue(13, "An idea", "feature")], _archive, out var added);
        Assert.Equal(2, added);
        Assert.Equal(2, first.Count);
        Changes.Apply(first);
        var twelve = ItemDocument.Parse(File.ReadAllText(Path.Combine(_list, "12 The list loses my place.json")));
        Assert.Equal("Bug", FieldTypes.TextOf(twelve.Fields[twelve.IndexOf(GitHubList.KindField)].Value));
        Assert.Equal("6f1c2d3e-4a5b-4c6d-8e7f-901234567890", FieldTypes.TextOf(twelve.Fields[twelve.IndexOf(GitHubList.ReporterField)].Value));
        Assert.Equal("5.1.0", FieldTypes.TextOf(twelve.Fields[twelve.IndexOf(GitHubList.VersionField)].Value));
        Assert.Equal(12, GitHubList.NumberOf(twelve));
        var thirteen = ItemDocument.Parse(File.ReadAllText(Path.Combine(_list, "13 An idea.json")));
        Assert.Equal("axkit-reporter", FieldTypes.TextOf(thirteen.Fields[thirteen.IndexOf(GitHubList.ReporterField)].Value));
        Assert.Equal("Feature request", FieldTypes.TextOf(thirteen.Fields[thirteen.IndexOf(GitHubList.KindField)].Value));

        // The user's notes, then the issue's title changes and 13 is closed: the notes stay, the file name stays, 13 goes to the archive.
        var notes = twelve.WithValue(twelve.IndexOf(GitHubList.NotesField), System.Text.Json.Nodes.JsonValue.Create("Seen it too"));
        File.WriteAllText(Path.Combine(_list, "12 The list loses my place.json"), notes.ToText());
        Assert.DoesNotContain(GitHubList.Refresh(_list, [Issue(12, "The list loses my place", "bug", body), Issue(13, "An idea", "feature")], _archive, out _), change => change.IsMove);
        var second = GitHubList.Refresh(_list, [Issue(12, "The list keeps losing my place", "feedback", body)], _archive, out added);
        Assert.Equal(0, added);
        Assert.Equal(2, second.Count);
        Changes.Apply(second);
        var again = ItemDocument.Parse(File.ReadAllText(Path.Combine(_list, "12 The list loses my place.json")));
        Assert.Equal("The list keeps losing my place", again.Title);
        Assert.Equal("Seen it too", FieldTypes.TextOf(again.Fields[again.IndexOf(GitHubList.NotesField)].Value));
        Assert.Equal("Feedback", FieldTypes.TextOf(again.Fields[again.IndexOf(GitHubList.KindField)].Value));
        Assert.False(File.Exists(Path.Combine(_list, "13 An idea.json")));
        Assert.True(File.Exists(Path.Combine(_archive, "13 An idea.json")));

        // Nothing to do when nothing changed.
        Assert.Empty(GitHubList.Refresh(_list, [Issue(12, "The list keeps losing my place", "feedback", body)], _archive, out _));
    }

    [Fact]
    public void Kinds_come_from_the_labels()
    {
        Assert.Equal("Bug", GitHubList.KindName(["enhancement", "bug"]));
        Assert.Equal(GitHubList.Unlabelled, GitHubList.KindName(["question"]));
        Assert.Equal("feature", GitHubList.LabelOf("Feature request"));
        Assert.Null(GitHubList.LabelOf(GitHubList.Unlabelled));
        Assert.Equal("Issues, 1 item", GitHubList.SpokenRefresh("Issues", 1, 0));
        Assert.Equal("Issues, 14 items, 2 new", GitHubList.SpokenRefresh("Issues", 14, 2));
    }
}
