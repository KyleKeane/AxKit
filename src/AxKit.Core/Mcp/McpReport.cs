using AxKit.Core.Reports;

namespace AxKit.Core.Mcp;

/// <summary>
/// <c>axkit_draft_report</c> (docs/axKit/mcp.md MC-9.11; the maintainer's request through axHelp: "an AI tool may draft a
/// Send a report submission and show it for review before anything is sent"): the pure rules. The window opens its
/// report form with the draft in the words' box, the form's text starting with the caution; only the user's own Send
/// sends. Tested in <c>tests/AxKit.Tests/McpReportTests</c>.
/// </summary>
public static class McpReport
{
    /// <summary>The longest draft: a report is a few paragraphs, and the user reads every word of it.</summary>
    public const int MaxLength = 4000;

    /// <summary>Why a draft is refused before any window is asked, or null.</summary>
    public static string? Refusal(string? text) => text switch
    {
        null => "Give the report's text: what happened, what was expected, and the steps, in the user's words where you can.",
        _ when text.Trim().Length == 0 => "The report's text is empty.",
        { Length: > MaxLength } => $"The draft is longer than {MaxLength} characters: a report is a few paragraphs the user reads whole.",
        _ => null,
    };

    /// <summary>The kind a draft names ("bug", "feature" or "feature request", "feedback"), Feedback for none or any other word.</summary>
    public static ReportKind Kind(string? kind) => (kind ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "bug" => ReportKind.Bug,
        "feature" or "feature request" => ReportKind.Feature,
        _ => ReportKind.Feedback,
    };

    /// <summary>
    /// The words as the form shows them: a first line that says who drafted them, which the report keeps unless the
    /// user takes it out, so that the maintainer knows; then the draft, its hidden characters made visible.
    /// </summary>
    public static string Words(string client, string text) =>
        AiCaution.ReportMark(McpRun.Speaker(client)) + "\r\n\r\n"
        + string.Join("\r\n", text.Trim().ReplaceLineEndings("\n").Split('\n').Select(DisplayText.Visible));

    /// <summary>What the tool answers once the form is open: nothing is sent, and the user decides.</summary>
    public const string Opened = "The report form is open in the axKit window with your draft. Nothing is sent: the user reads it, changes it if they like, and sends it with Send, or cancels. Say that it waits there, and end your turn.";
}
