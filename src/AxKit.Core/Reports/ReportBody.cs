using System.Text;

namespace AxKit.Core.Reports;

/// <summary>
/// A report as text (docs/axKit/reports.md RP-6.1): <see cref="Markdown"/>, the plain report, the user's words first,
/// the context, the last actions, the log lines folded away, and one last line with the version and the install's
/// reporter id; and what goes to GitHub since 2026-11-14, <see cref="IssueTitle"/> and <see cref="Sealed"/>: an issue
/// that holds nothing of the user's in the clear, its body the JSON envelope of <see cref="ReportSeal"/> with the
/// plain report sealed for the maintainer's key and the reporter id, the version and the kind beside it, which the
/// reports repository's workflow reads.
/// </summary>
public static class ReportBody
{
    /// <summary>The issue's title: the kind and the version, never the user's words. "Bug report, axKit 5.9.0".</summary>
    public static string IssueTitle(ReportKind kind, string version) =>
        $"{(kind == ReportKind.Bug ? "Bug report" : ReportKinds.Name(kind))}, axKit {version.Trim()}";

    /// <summary>What is sealed: the report's own title and kind, then the plain report as <see cref="Markdown"/> makes it.</summary>
    public static string Plain(Report report, string version, Guid reporterId) =>
        $"Title: {Line(report.Title)}\nKind: {ReportKinds.Name(report.Kind)}\n\n{Markdown(report, version, reporterId)}";

    /// <summary>
    /// The issue's body: the envelope alone, one JSON object in a <c>json</c> code fence, so that GitHub shows it as
    /// it is; the fence's two lines are the only thing in the body that is not the object. Throws
    /// <see cref="ReportSealException"/> for a key that is none.
    /// </summary>
    public static string Sealed(Report report, string version, Guid reporterId, string publicKey) =>
        $"```json\n{ReportSeal.Seal(Plain(report, version, reporterId), reporterId, version, ReportKinds.Label(report.Kind), publicKey)}```\n";

    public static string Markdown(Report report, string version, Guid reporterId)
    {
        var sb = new StringBuilder();
        sb.Append(report.Words.Trim().ReplaceLineEndings("\n")).Append("\n\n");
        sb.Append("### Context\n\n");
        sb.Append("- App and version: ").Append(Line(report.App)).Append('\n');
        if (report.Where.Trim().Length > 0)
        {
            sb.Append("- Where the focus was: ").Append(Line(report.Where)).Append('\n');
        }

        if (report.Actions.Count > 0)
        {
            sb.Append("\n### Last actions\n\n```text\n");
            foreach (var action in report.Actions)
            {
                sb.Append(Line(action)).Append('\n');
            }

            sb.Append("```\n");
        }

        if (report.Log.Count > 0)
        {
            sb.Append("\n<details>\n<summary>Log lines</summary>\n\n```text\n");
            foreach (var line in report.Log)
            {
                sb.Append(Line(line)).Append('\n');
            }

            sb.Append("```\n\n</details>\n");
        }

        sb.Append('\n').Append(ReporterLine(version, reporterId)).Append('\n');
        return sb.ToString();
    }

    /// <summary>"Sent from axKit 5.1.0 with F4; reporter 6f1c…": the body's last line.</summary>
    private static string ReporterLine(string version, Guid reporterId) => $"Sent from axKit {version.Trim()} with F4; reporter {reporterId:D}";

    /// <summary>One line of a code block or a bullet: a backtick fence inside it would end the block early.</summary>
    private static string Line(string text) => text.ReplaceLineEndings(" ").Replace("```", "'''", StringComparison.Ordinal).Trim();
}
