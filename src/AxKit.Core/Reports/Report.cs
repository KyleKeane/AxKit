namespace AxKit.Core.Reports;

/// <summary>The three kinds of report (docs/axkit/reports.md RP-3 item 2), recategorised later on GitHub by their label.</summary>
public enum ReportKind
{
    Feedback,
    Bug,
    Feature,
}

/// <summary>
/// A report as the form holds it (RP-3): the user's words and title, the kind, and the context the window filled in
/// and the user may have edited: the app and version line, where the focus was, the last actions and the log lines.
/// </summary>
public sealed record Report(
    string Title,
    ReportKind Kind,
    string Words,
    string App,
    string Where,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> Log,
    DateTime Created)
{
    public const int TitleLength = 80;

    /// <summary>The title a report takes from its words (RP-3 item 7): the first line with text, eighty characters at most; "Report" for none.</summary>
    public static string TitleFrom(string words)
    {
        var line = words.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? string.Empty;
        if (line.Length > TitleLength)
        {
            var cut = line.LastIndexOf(' ', TitleLength);
            line = (cut > TitleLength / 2 ? line[..cut] : line[..TitleLength]).TrimEnd() + "…";
        }

        return line.Length == 0 ? "Report" : line;
    }
}

/// <summary>The kinds' names as the form and the list show them, and their GitHub labels (RP-6.1).</summary>
public static class ReportKinds
{
    public static readonly IReadOnlyList<ReportKind> All = [ReportKind.Feedback, ReportKind.Bug, ReportKind.Feature];

    public static string Name(ReportKind kind) => kind switch
    {
        ReportKind.Bug => "Bug",
        ReportKind.Feature => "Feature request",
        _ => "Feedback",
    };

    public static string Label(ReportKind kind) => kind switch
    {
        ReportKind.Bug => "bug",
        ReportKind.Feature => "feature",
        _ => "feedback",
    };

    /// <summary>The kind a name or a label means, case ignored; null for anything else.</summary>
    public static ReportKind? Parse(string text) => text.Trim().ToLowerInvariant() switch
    {
        "bug" => ReportKind.Bug,
        "feature" or "feature request" => ReportKind.Feature,
        "feedback" => ReportKind.Feedback,
        _ => null,
    };

    public static IReadOnlyList<string> Names => All.Select(Name).ToList();
}

/// <summary>The Status field's values (RP-5.2).</summary>
public static class ReportStatus
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    private const string NotSentPrefix = "Not sent yet";

    /// <summary>"Not sent yet: no network"; the reason in a few words, or none.</summary>
    public static string NotSent(string? reason) => string.IsNullOrWhiteSpace(reason) ? NotSentPrefix : $"{NotSentPrefix}: {reason.Trim()}";

    /// <summary>Whether the sender should try this item: a draft never, a sent one never, an unsent one yes.</summary>
    public static bool AwaitsSending(string status) => status.StartsWith(NotSentPrefix, StringComparison.Ordinal);
}
