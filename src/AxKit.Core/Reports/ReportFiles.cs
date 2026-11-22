using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core.Conversion;

namespace AxKit.Core.Reports;

/// <summary>
/// The user's reports as an axTask list (docs/axKit/reports.md RP-5): the folder, its <c>list.json</c> with the
/// fields of a report and the actions for its kinds, and each report as one item file, written before anything is
/// sent and read again for the unsent ones. The files are of the lists' one shape (<see cref="ListConfig"/>,
/// <see cref="FlatItem"/>; docs/axTask/forms-framework.md section 13); a list an earlier version of axKit made is
/// converted when it is met (<see cref="EnsureList"/>), so that a report that waits in it is still sent.
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

    /// <summary>
    /// The name of the value that holds the report's id (RP-6.9): random, written when the report form saves the
    /// report, kept by axTask through every change and rename. No field of the list: it is kept in the file and not
    /// shown among the fields (AF-11.3).
    /// </summary>
    public const string IdKey = "reportId";

    /// <summary>The id an item carries, or null: a report an earlier version saved, or a file that is not the form's.</summary>
    public static string? IdOf(FlatItem item) => item.Text(IdKey) is { Length: > 0 } id ? id : null;

    private static readonly UTF8Encoding Utf8 = new(false);

    /// <summary>The fields of a report item (RP-5.2), in the order the detail pane shows them, each with the value a new one starts with.</summary>
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

    /// <summary>What the list says of itself, its Notes (RP-5.1).</summary>
    public const string ListNotes =
        "Your reports to axKit's maintainer, from F4 in any window: each one is saved here first, as you wrote it, then sent encrypted to a private repository on GitHub that only the maintainer can read; Status says whether it went, and Number is the report's number there, to quote to the maintainer. Delete removes a report from this list only.";

    /// <summary>The Notes of a list made by 5.1.0 to 5.8.0, when a report was a public issue; <see cref="EnsureList"/> replaces exactly these words.</summary>
    public const string ListNotesBefore =
        "Your reports to axKit's maintainer, from F4 in any window: each one is saved here first, then sent to GitHub as a public issue; Status says whether it went, and Issue is its address once it has. Delete removes a report from this list only.";

    /// <summary>The list's configuration (RP-5.1, RP-8.5): what the list is, the fields of a report with their first values (Feedback, Draft), and the three actions that set the kind.</summary>
    public static ListConfig ListConfiguration() =>
        ListConfig.New(Template)
            .WithDetail("Notes", JsonValue.Create(ListNotes))
            .WithExtra(ListConfig.ActionsKey, new JsonArray(
                Action("Make it a bug", 1, ReportKinds.Name(ReportKind.Bug)),
                Action("Make it a feature request", 2, ReportKinds.Name(ReportKind.Feature)),
                Action("Make it feedback", 3, ReportKinds.Name(ReportKind.Feedback))));

    /// <summary>The text of the list's <c>list.json</c> as it is first written.</summary>
    public static string ListText() => ListConfiguration().ToText();

    private static JsonObject Action(string name, int digit, string kind) =>
        new() { ["name"] = name, ["key"] = $"Ctrl+{digit}", ["does"] = "set", ["set"] = new JsonObject { [KindField] = kind } };

    /// <summary>A report as an item of the list, with its status.</summary>
    public static FlatItem ToItem(Report report, string status) =>
        FlatItem.New(report.Title, ListConfiguration())
            .With(KindField, JsonValue.Create(ReportKinds.Name(report.Kind)))
            .With(ReportField, JsonValue.Create(report.Words))
            .With(AppField, JsonValue.Create(report.App))
            .With(WhereField, JsonValue.Create(report.Where))
            .With(ActionsField, JsonValue.Create(string.Join("\r\n", report.Actions)))
            .With(LogField, JsonValue.Create(string.Join("\r\n", report.Log)))
            .With(CreatedField, JsonValue.Create(FieldTypes.DateTimeText(report.Created)))
            .With(StatusField, JsonValue.Create(status));

    /// <summary>The report an item holds, as the sender reads an unsent one; null when the item is not a report (no Report value).</summary>
    public static Report? FromItem(FlatItem item)
    {
        if (!item.Has(ReportField))
        {
            return null;
        }

        var created = FieldTypes.ReadDateTime(item.Value(CreatedField)) ?? DateTime.UtcNow;
        return new Report(
            item.Title.Trim().Length > 0 ? item.Title.Trim() : "Report",
            ReportKinds.Parse(item.Text(KindField)) ?? ReportKind.Feedback,
            item.Text(ReportField),
            item.Text(AppField),
            item.Text(WhereField),
            Lines(item.Text(ActionsField)),
            Lines(item.Text(LogField)),
            created);
    }

    public static string Status(FlatItem item) => item.Text(StatusField);

    /// <summary>The item with its status set; an item that holds no Status is left as it is.</summary>
    public static FlatItem WithStatus(FlatItem item, string status) => Set(item, StatusField, status);

    /// <summary>The item once its issue exists (RP-6.1): Sent, with the issue's number and address.</summary>
    public static FlatItem WithSent(FlatItem item, int number, string url)
    {
        item = Set(item, StatusField, ReportStatus.Sent);
        item = Set(item, IssueField, url);
        return Set(item, NumberField, number.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The folder and its <c>list.json</c>, made when missing (RP-5.1): before a report is written, and before axTask
    /// opens the list. A list an earlier version made keeps everything the user set in it: one of the earlier shape
    /// is converted first, a copy of it kept beside the folder (AF-13.6), so that the reports that wait in it are
    /// read and sent; and its Notes, when they are still the words of the versions that said "public issue", become
    /// <see cref="ListNotes"/>. A list that cannot be converted or read is left as it is, and the reason logged.
    /// </summary>
    public static void EnsureList(string folder)
    {
        Directory.CreateDirectory(folder);
        var list = Path.Combine(folder, ListConfig.FileName);
        try
        {
            if (ListConversion.IsEarlier(folder))
            {
                var plan = ListConversion.Plan(folder);
                ListConversion.KeepCopy(folder, Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(folder)) ?? folder);
                ListConversion.Apply(plan);
                Log.Info($"The reports list was converted from axKit's earlier format: {plan.Files.Count} files written, a copy kept beside it");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException)
        {
            Log.Error("The reports list is in axKit's earlier format and could not be converted", ex);
        }

        if (!File.Exists(list))
        {
            File.WriteAllText(list, ListText(), Utf8);
            return;
        }

        try
        {
            var config = ListConfig.Parse(File.ReadAllText(list));
            if (config.Detail("Notes") == ListNotesBefore)
            {
                SafeFile.WriteAllText(list, config.WithDetail("Notes", JsonValue.Create(ListNotes)).ToText(), Utf8);
            }
        }
        catch (InvalidDataException)
        {
            // A list.json the user changed into something else is theirs: left as it is.
        }
    }

    /// <summary>
    /// Writes a report into the folder (RP-5.1): the folder and its <c>list.json</c> made when missing, the item named
    /// after its title beside the taken names; the item's path. The file's name goes onto the list of the files the
    /// report form wrote first (RP-6.9, <see cref="ReportWritten"/>, <paramref name="writtenFile"/>), since the
    /// sender sends no other; when that list cannot be written the report is saved all the same, the words kept
    /// (RP-3.2), and waits unsent.
    /// </summary>
    public static string Save(Report report, string status, string folder, string writtenFile)
    {
        EnsureList(folder);
        var path = ItemFiles.FreePath(folder, ItemFiles.FileStem(report.Title), ".json");
        // RP-6.9: the report's own id, written into its file and onto the list, so that the report is still the
        // form's after axTask renamed its file with its title.
        var id = Guid.NewGuid().ToString("N");
        try
        {
            ReportWritten.Add(writtenFile, path, folder, id);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The list of the reports axKit's report form wrote could not be written, so this report waits unsent: " + ex.Message);
        }

        SafeFile.WriteAllText(path, ToItem(report, status).With(IdKey, JsonValue.Create(id)).ToText(), Utf8);
        return path;
    }

    /// <summary>
    /// The waiting items the report form wrote (RP-6.9), oldest first, and how many other waiting items the folder
    /// holds, which are never sent or changed. While the list is not kept yet, every waiting item is the form's.
    /// </summary>
    public static (IReadOnlyList<(string Path, FlatItem Item)> Ours, int Others) UnsentOfTheForm(string folder, string writtenFile)
    {
        var unsent = Unsent(folder);
        if (unsent.Count == 0)
        {
            return (unsent, 0);
        }

        var names = ReportWritten.Names(writtenFile);
        var ours = unsent.Where(entry => ReportWritten.Holds(names, entry.Path, IdOf(entry.Item))).ToList();
        return (ours, unsent.Count - ours.Count);
    }

    /// <summary>Writes an item back to its file (the status changed, the issue known).</summary>
    public static void Write(string path, FlatItem item) => SafeFile.WriteAllText(path, item.ToText(), Utf8);

    /// <summary>The items of the folder that wait to be sent (RP-6.7), oldest first by their Created value, each with its path; none when the folder is missing.</summary>
    public static IReadOnlyList<(string Path, FlatItem Item)> Unsent(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var unsent = new List<(string Path, FlatItem Item, DateTime Created)>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json"))
        {
            if (Path.GetFileName(path).Equals(ListConfig.FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FlatItem item;
            try
            {
                item = FlatItem.Parse(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                continue;
            }

            if (item.Has(ReportField) && ReportStatus.AwaitsSending(Status(item)))
            {
                unsent.Add((path, item, FieldTypes.ReadDateTime(item.Value(CreatedField)) ?? File.GetCreationTimeUtc(path)));
            }
        }

        return unsent.OrderBy(entry => entry.Created).Select(entry => (entry.Path, entry.Item)).ToList();
    }

    /// <summary>A value set where the item holds one of that name: an item that lacks the field is not given it.</summary>
    private static FlatItem Set(FlatItem item, string field, string value) =>
        item.Has(field) ? item.With(field, JsonValue.Create(value)) : item;

    private static IReadOnlyList<string> Lines(string text) =>
        text.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
