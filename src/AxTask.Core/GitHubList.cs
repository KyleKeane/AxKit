using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AxKit.Core;
using AxKit.Core.Reports;

namespace AxTask.Core;

/// <summary>What a GitHub list follows (docs/axkit/reports.md RP-7.1): the repository, the issues' state, and the code repository's folder for Open in axClaude.</summary>
public sealed record GitHubListSettings(string Repository, string State, string? Folder);

/// <summary>
/// A GitHub list (RP-7.1): an axTask list whose <c>list.json</c> has <c>"github": {"repository": "owner/name", "state":
/// "open"}</c>; its items are the repository's issues in that state, one file each, filled from GitHub when the list
/// opens and on F5. The user's Notes field is local and kept across refreshes by the issue's number; items whose issue
/// GitHub no longer returns go to the Archive. Pure: the issues come from <see cref="GitHubIssues"/>, the changes go
/// through <see cref="Changes"/>.
/// </summary>
public static partial class GitHubList
{
    public const string RootKey = "github";
    public const string NumberField = "Number";
    public const string KindField = "Kind";
    public const string StateField = "State";
    public const string OpenedField = "Opened";
    public const string ReporterField = "Reporter";
    public const string VersionField = "Version";
    public const string UpdatedField = "Updated";
    public const string IssueField = "Issue";
    public const string BodyField = "Body";
    public const string NotesField = "Notes";
    public const string Unlabelled = "unlabelled";
    public const string OpenState = "open";
    public const string ClosedState = "closed";

    /// <summary>Open in axClaude as an action key's target (RP-7.2, RP-8.1).</summary>
    public const string AxClaudeTarget = "axclaude";

    /// <summary>The kinds an issue's label gives, and the value for none.</summary>
    public static IReadOnlyList<string> KindNames { get; } = [.. ReportKinds.Names, Unlabelled];

    /// <summary>The template of an issue's item, in the order the detail pane shows the fields.</summary>
    public static IReadOnlyList<ItemField> Template { get; } =
    [
        new(NumberField, FieldTypes.Number, JsonValue.Create(string.Empty), []),
        new(KindField, FieldTypes.Choice, JsonValue.Create(Unlabelled), KindNames),
        new(StateField, FieldTypes.Text, JsonValue.Create(OpenState), []),
        new(OpenedField, FieldTypes.Date, JsonValue.Create(string.Empty), []),
        new(ReporterField, FieldTypes.Text, JsonValue.Create(string.Empty), []),
        new(VersionField, FieldTypes.Text, JsonValue.Create(string.Empty), []),
        new(UpdatedField, FieldTypes.DateTime, JsonValue.Create(string.Empty), []),
        new(IssueField, FieldTypes.Link, JsonValue.Create(string.Empty), []),
        new(BodyField, FieldTypes.LongText, JsonValue.Create(string.Empty), []),
        new(NotesField, FieldTypes.LongText, JsonValue.Create(string.Empty), []),
    ];

    /// <summary>The settings of a GitHub list, or null for an ordinary list (no <c>github</c> key, or no repository as owner/name).</summary>
    public static GitHubListSettings? Of(ItemDocument? list)
    {
        if (list?.Extra(RootKey) is not JsonObject github)
        {
            return null;
        }

        var repository = FieldTypes.TextOf(github["repository"]).Trim();
        if (!RepositoryRegex().IsMatch(repository))
        {
            return null;
        }

        var state = FieldTypes.TextOf(github["state"]).Trim();
        var folder = FieldTypes.TextOf(github["folder"]).Trim();
        return new GitHubListSettings(repository, state.Length == 0 ? OpenState : state, folder.Length == 0 ? null : folder);
    }

    /// <summary>Whether a repository name is owner/name.</summary>
    public static bool IsRepository(string text) => RepositoryRegex().IsMatch(text.Trim());

    /// <summary>
    /// A new GitHub list's <c>list.json</c> (RP-7.1, RP-8.5): the Notes saying what it is, the template, the github
    /// settings, and the action keys for the kinds, Close, Open on GitHub and Open in axClaude.
    /// </summary>
    public static ItemDocument NewList(string repository, string state, string? folder)
    {
        var list = ItemDocument.NewList(Template);
        list = list.WithValue(list.IndexOf(NotesField), JsonValue.Create(
            $"The {state} issues of {repository} on GitHub, read again when the list opens and on F5; Notes is yours and stays. "
            + "The Item menu's GitHub actions and the Ctrl+number keys change an issue as your GitHub account (List menu, GitHub account). docs/axkit/reports.md RP-7."));
        var github = new JsonObject { ["repository"] = repository, ["state"] = state };
        if (folder is not null)
        {
            github["folder"] = folder;
        }

        list = list.WithExtra(RootKey, github);
        list = ActionKeys.With(list, 1, new ListAction("Make it a bug", Set: SetKind(ReportKind.Bug)));
        list = ActionKeys.With(list, 2, new ListAction("Make it a feature request", Set: SetKind(ReportKind.Feature)));
        list = ActionKeys.With(list, 3, new ListAction("Make it feedback", Set: SetKind(ReportKind.Feedback)));
        list = ActionKeys.With(list, 4, new ListAction("Close", Set: new Dictionary<string, JsonNode?> { [StateField] = JsonValue.Create(ClosedState) }));
        list = ActionKeys.With(list, 5, new ListAction("Open on GitHub", Open: IssueField));
        return ActionKeys.With(list, 6, new ListAction("Open in axClaude", Open: AxClaudeTarget));
    }

    private static Dictionary<string, JsonNode?> SetKind(ReportKind kind) => new() { [KindField] = JsonValue.Create(ReportKinds.Name(kind)) };

    /// <summary>The kind an issue's labels give: the first of feedback, bug or feature, else "unlabelled".</summary>
    public static string KindName(IReadOnlyList<string> labels) =>
        labels.Select(ReportKinds.Parse).FirstOrDefault(kind => kind is not null) is { } found ? ReportKinds.Name(found) : Unlabelled;

    /// <summary>The label a Kind value means; null for "unlabelled" or anything else.</summary>
    public static string? LabelOf(string kindName) => ReportKinds.Parse(kindName) is { } kind ? ReportKinds.Label(kind) : null;

    /// <summary>An issue's item, with the user's notes kept (RP-7.1): the reporter id and the version from the body's last line, or the login of an issue filed in the browser.</summary>
    public static ItemDocument ToItem(IssueInfo issue, string notes)
    {
        var item = ItemDocument.NewItem(issue.Title.Trim().Length > 0 ? issue.Title.Trim() : $"Issue {issue.Number}", Template);
        var reporter = ReportBody.ParseReporterLine(issue.Body);
        item = Set(item, NumberField, JsonValue.Create(issue.Number));
        item = Set(item, KindField, JsonValue.Create(KindName(issue.Labels)));
        item = Set(item, StateField, JsonValue.Create(issue.State));
        item = Set(item, OpenedField, JsonValue.Create(issue.CreatedAt == DateTime.MinValue ? string.Empty : FieldTypes.DateText(DateOnly.FromDateTime(issue.CreatedAt))));
        item = Set(item, ReporterField, JsonValue.Create(reporter is { } r ? r.ReporterId.ToString("D") : issue.User));
        item = Set(item, VersionField, JsonValue.Create(reporter?.Version ?? string.Empty));
        item = Set(item, UpdatedField, JsonValue.Create(issue.UpdatedAt == DateTime.MinValue ? string.Empty : FieldTypes.DateTimeText(issue.UpdatedAt)));
        item = Set(item, IssueField, JsonValue.Create(issue.Url));
        item = Set(item, BodyField, JsonValue.Create(issue.Body.ReplaceLineEndings("\r\n")));
        return Set(item, NotesField, JsonValue.Create(notes));
    }

    /// <summary>The issue's number an item holds, or 0.</summary>
    public static int NumberOf(ItemDocument item)
    {
        var index = item.IndexOf(NumberField);
        if (index < 0 || item.Fields[index].Value is not JsonValue value)
        {
            return 0;
        }

        return value.TryGetValue<int>(out var number) ? number
            : value.TryGetValue<string>(out var text) && int.TryParse(text, out var parsed) ? parsed : 0;
    }

    /// <summary>The file name of an issue's item: the number first, so that a title that changes keeps its file.</summary>
    public static string FileStem(IssueInfo issue) => ItemFiles.FileStem($"{issue.Number} {issue.Title}");

    /// <summary>
    /// The changes that bring the folder up to the issues (RP-7.1): a new issue written, a changed one rewritten with
    /// its Notes kept, an item whose issue is gone moved to the archive; nothing for a folder already current.
    /// </summary>
    public static IReadOnlyList<FileChange> Refresh(string folder, IReadOnlyList<IssueInfo> issues, string archive, out int added)
    {
        added = 0;
        var changes = new List<FileChange>();
        var existing = Directory.Exists(folder) ? ItemFolder.Read(folder).Items : [];
        var byNumber = new Dictionary<int, ItemFile>();
        foreach (var item in existing)
        {
            var number = NumberOf(item.Document);
            if (number > 0)
            {
                byNumber.TryAdd(number, item);
            }
        }

        var seen = new HashSet<int>();
        foreach (var issue in issues)
        {
            seen.Add(issue.Number);
            if (byNumber.TryGetValue(issue.Number, out var known))
            {
                var notesIndex = known.Document.IndexOf(NotesField);
                var notes = notesIndex >= 0 ? FieldTypes.TextOf(known.Document.Fields[notesIndex].Value) : string.Empty;
                var text = ToItem(issue, notes).ToText();
                if (text != known.Text)
                {
                    changes.Add(FileChange.Write(known.Path, known.Text, text));
                }
            }
            else
            {
                var path = ItemFiles.FreePath(folder, FileStem(issue), ".json");
                changes.Add(FileChange.Write(path, null, ToItem(issue, string.Empty).ToText()));
                added++;
            }
        }

        foreach (var (number, item) in byNumber)
        {
            if (!seen.Contains(number))
            {
                changes.Add(FileChange.Move(item.Path, ItemFiles.FreePath(archive, Path.GetFileNameWithoutExtension(item.Path), ".json")));
            }
        }

        return changes;
    }

    /// <summary>"Issues, 14 items, 2 new".</summary>
    public static string SpokenRefresh(string name, int count, int added) =>
        $"{name}, {count} {(count == 1 ? "item" : "items")}" + (added > 0 ? $", {added} new" : string.Empty);

    private static ItemDocument Set(ItemDocument item, string field, JsonNode? value)
    {
        var index = item.IndexOf(field);
        return index < 0 ? item : item.WithValue(index, value);
    }


    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?/[A-Za-z0-9._-]+$")]
    private static partial Regex RepositoryRegex();
}
