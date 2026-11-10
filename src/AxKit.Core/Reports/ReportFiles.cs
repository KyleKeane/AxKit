using System.Text;
using System.Text.Json.Nodes;

namespace AxKit.Core.Reports;

/// <summary>
/// The user's reports as an axTask list (docs/axKit/reports.md RP-5): the folder, its <c>list.json</c> with the
/// template and the action keys, and each report as one item file, written before anything is sent and read again
/// for the unsent ones.
/// </summary>
public static class ReportFiles
{
    public const string KindField = "Kind";
    public const string ReportField = "Report";
    public const string AppField = "App";
    public const string WhereField = "Where";
    public const string ActionsField = "Actions";
    public const string LogField = "Log";
    public const string CreatedField = "Created";
    public const string StatusField = "Status";
    public const string IssueField = "Issue";
    public const string NumberField = "Number";

    private static readonly UTF8Encoding Utf8 = new(false);

    /// <summary>The template of a report item (RP-5.2), in the order the detail pane shows the fields.</summary>
    public static IReadOnlyList<ItemField> Template { get; } =
    [
        new(KindField, FieldTypes.Choice, JsonValue.Create(ReportKinds.Name(ReportKind.Feedback)), ReportKinds.Names),
        new(ReportField, FieldTypes.LongText, JsonValue.Create(string.Empty), []),
        new(AppField, FieldTypes.Text, JsonValue.Create(string.Empty), []),
        new(WhereField, FieldTypes.Text, JsonValue.Create(string.Empty), []),
        new(ActionsField, FieldTypes.LongText, JsonValue.Create(string.Empty), []),
        new(LogField, FieldTypes.LongText, JsonValue.Create(string.Empty), []),
        new(CreatedField, FieldTypes.DateTime, JsonValue.Create(string.Empty), []),
        new(StatusField, FieldTypes.Text, JsonValue.Create(ReportStatus.Draft), []),
        new(IssueField, FieldTypes.Link, JsonValue.Create(string.Empty), []),
        new(NumberField, FieldTypes.Number, JsonValue.Create(string.Empty), []),
    ];

    /// <summary>
    /// The list's <c>list.json</c> (RP-5.1, RP-8.5): what the list is, the template with its values (Feedback, Draft),
    /// and the action keys for the kinds and the issue; a list as <see cref="ItemDocument.NewList"/> writes one (until
    /// 2026-11-09 written by hand here, because the written template emptied its values).
    /// </summary>
    public static string ListText()
    {
        var list = ItemDocument.NewList(Template);
        list = list.WithValue(list.IndexOf("Notes"), JsonValue.Create(
            "Your reports to axKit's maintainer, from F4 in any window: each one is saved here first, then sent to GitHub as a public issue; Status says whether it went, and Issue is its address once it has. Delete removes a report from this list only."));
        return list.WithExtra("keys", new JsonObject
        {
            ["1"] = Key("Make it a bug", KindField, ReportKinds.Name(ReportKind.Bug)),
            ["2"] = Key("Make it a feature request", KindField, ReportKinds.Name(ReportKind.Feature)),
            ["3"] = Key("Make it feedback", KindField, ReportKinds.Name(ReportKind.Feedback)),
            ["4"] = new JsonObject { ["name"] = "Open on GitHub", ["open"] = IssueField },
        }).ToText();

        static JsonObject Key(string name, string field, string value) =>
            new() { ["name"] = name, ["set"] = new JsonObject { [field] = value } };
    }

    /// <summary>A report as an item (IA-4), with its status.</summary>
    public static ItemDocument ToItem(Report report, string status)
    {
        var item = ItemDocument.NewItem(report.Title, Template);
        item = Set(item, KindField, ReportKinds.Name(report.Kind));
        item = Set(item, ReportField, report.Words);
        item = Set(item, AppField, report.App);
        item = Set(item, WhereField, report.Where);
        item = Set(item, ActionsField, string.Join("\r\n", report.Actions));
        item = Set(item, LogField, string.Join("\r\n", report.Log));
        item = Set(item, CreatedField, FieldTypes.DateTimeText(report.Created));
        return Set(item, StatusField, status);
    }

    /// <summary>The report an item holds, as the sender reads an unsent one; null when the item is not a report (no Report field).</summary>
    public static Report? FromItem(ItemDocument item)
    {
        if (item.IndexOf(ReportField) < 0)
        {
            return null;
        }

        var created = FieldTypes.ReadDateTime(Value(item, CreatedField)) ?? DateTime.UtcNow;
        return new Report(
            item.Title.Trim().Length > 0 ? item.Title.Trim() : "Report",
            ReportKinds.Parse(Text(item, KindField)) ?? ReportKind.Feedback,
            Text(item, ReportField),
            Text(item, AppField),
            Text(item, WhereField),
            Lines(Text(item, ActionsField)),
            Lines(Text(item, LogField)),
            created);
    }

    public static string Status(ItemDocument item) => Text(item, StatusField);

    public static ItemDocument WithStatus(ItemDocument item, string status) => Set(item, StatusField, status);

    /// <summary>The item once its issue exists (RP-6.1): Sent, with the issue's number and address.</summary>
    public static ItemDocument WithSent(ItemDocument item, int number, string url)
    {
        item = Set(item, StatusField, ReportStatus.Sent);
        item = Set(item, IssueField, url);
        return Set(item, NumberField, number.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>The folder and its <c>list.json</c>, made when missing (RP-5.1): before a report is written, and before axTask opens the list.</summary>
    public static void EnsureList(string folder)
    {
        Directory.CreateDirectory(folder);
        var list = Path.Combine(folder, ItemDocument.ListFile);
        if (!File.Exists(list))
        {
            File.WriteAllText(list, ListText(), Utf8);
        }
    }

    /// <summary>
    /// Writes a report into the folder (RP-5.1): the folder and its <c>list.json</c> made when missing, the item named
    /// after its title beside the taken names; the item's path.
    /// </summary>
    public static string Save(Report report, string status, string folder)
    {
        EnsureList(folder);
        var path = ItemFiles.FreePath(folder, ItemFiles.FileStem(report.Title), ".json");
        File.WriteAllText(path, ToItem(report, status).ToText(), Utf8);
        return path;
    }

    /// <summary>Writes an item back to its file (the status changed, the issue known).</summary>
    public static void Write(string path, ItemDocument item) => File.WriteAllText(path, item.ToText(), Utf8);

    /// <summary>The items of the folder that wait to be sent (RP-6.7), oldest first by their Created field, each with its path; none when the folder is missing.</summary>
    public static IReadOnlyList<(string Path, ItemDocument Item)> Unsent(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var unsent = new List<(string Path, ItemDocument Item, DateTime Created)>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json"))
        {
            if (Path.GetFileName(path).Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ItemDocument item;
            try
            {
                item = ItemDocument.Parse(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                continue;
            }

            if (item.IndexOf(ReportField) >= 0 && ReportStatus.AwaitsSending(Status(item)))
            {
                unsent.Add((path, item, FieldTypes.ReadDateTime(Value(item, CreatedField)) ?? File.GetCreationTimeUtc(path)));
            }
        }

        return unsent.OrderBy(entry => entry.Created).Select(entry => (entry.Path, entry.Item)).ToList();
    }

    private static ItemDocument Set(ItemDocument item, string field, string value)
    {
        var index = item.IndexOf(field);
        return index < 0 ? item : item.WithValue(index, JsonValue.Create(value));
    }

    private static JsonNode? Value(ItemDocument item, string field) =>
        item.IndexOf(field) is var index && index >= 0 ? item.Fields[index].Value : null;

    private static string Text(ItemDocument item, string field) => FieldTypes.TextOf(Value(item, field));

    private static IReadOnlyList<string> Lines(string text) =>
        text.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
