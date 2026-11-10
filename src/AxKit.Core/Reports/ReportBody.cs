using System.Text;
using System.Text.RegularExpressions;

namespace AxKit.Core.Reports;

/// <summary>
/// The issue's body as Markdown (docs/axKit/reports.md RP-6.1): the user's words first, the context, the last actions,
/// the log lines folded away, and one last line the workflow and the maintainer filter on, with the version and the
/// install's reporter id.
/// </summary>
public static partial class ReportBody
{
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

    /// <summary>The version and the reporter id of a body's last line, or null for a body that has none (an issue filed in the browser).</summary>
    public static (string Version, Guid ReporterId)? ParseReporterLine(string body)
    {
        var last = body.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0) ?? string.Empty;
        var match = ReporterLineRegex().Match(last);
        return match.Success && Guid.TryParseExact(match.Groups[2].Value, "D", out var id) ? (match.Groups[1].Value, id) : null;
    }

    /// <summary>One line of a code block or a bullet: a backtick fence inside it would end the block early.</summary>
    private static string Line(string text) => text.ReplaceLineEndings(" ").Replace("```", "'''", StringComparison.Ordinal).Trim();

    [GeneratedRegex(@"^Sent from axKit (\S+) with F4; reporter ([0-9a-fA-F-]{36})$")]
    private static partial Regex ReporterLineRegex();
}
