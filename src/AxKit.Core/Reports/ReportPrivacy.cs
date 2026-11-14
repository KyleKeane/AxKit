using System.Globalization;
using System.Text.RegularExpressions;

namespace AxKit.Core.Reports;

/// <summary>
/// What a report carries of the window when the user only types and presses Enter (docs/axKit/reports.md RP-4.2a;
/// the user's question of 2026-11-14, after his own first reports from a second computer showed a file's name and a
/// folder's in a public issue). A report is public, so its plain form holds nothing that can name the user's files,
/// folders, lists or items: the app and the versions without the window's title, the kind of control in focus and its
/// place without its name or the item's, the commands run without what the window said or which notices it showed,
/// and of the log only today's errors, with every path and every quoted text masked. The fuller form, with the title,
/// the names, the spoken lines and the log's last lines as they are, is the user's own choice in the form, said to be
/// able to name their files; it goes through <see cref="Redaction"/> as before. Pure; the window gathers the pieces.
/// </summary>
/// <summary>What the window adds to a report, in one of its two forms: the app line, where the focus was, the last actions, the log lines.</summary>
public sealed record ReportContext(string App, string Where, IReadOnlyList<string> Actions, IReadOnlyList<string> Log);

public static partial class ReportPrivacy
{
    /// <summary>How many log lines a report holds at most.</summary>
    public const int LogLines = 20;

    /// <summary>How many of the log's last lines are read to find them.</summary>
    public const int LogRead = 400;

    public const string DetailBox = "Add detail that may name your files: the window's title, the focused item, what the window said, the log as it is";

    // A drive path, a network path, a path under the home sign, or a Unix-like path of two parts or more, up to a
    // character no file name holds or the line's end; spaces inside a name go with it only between quotes, which
    // QuotedRegex masks whole first.
    [GeneratedRegex(@"(?i)(?:[A-Z]:[\\/]|\\\\|~[\\/]|(?<![\w.<>])/(?=[\w.~-]+/))[^\s""'<>|*?,;]*")]
    private static partial Regex PathRegex();

    // Text between double quotes, or between single quotes that are not apostrophes inside a word.
    [GeneratedRegex(@"""[^""\r\n]*""|(?<![A-Za-z0-9])'[^'\r\n]+'(?![A-Za-z0-9])")]
    private static partial Regex QuotedRegex();

    // "2026-11-14 10:59:21.681 ERROR [18156] ..." (Log.Write).
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\.\d{3} (INFO |ERROR|WARN ) ")]
    private static partial Regex LogLineRegex();

    /// <summary>
    /// A line with what <see cref="Redaction"/> replaces and, beyond it, every quoted text as <c>&lt;text&gt;</c> and
    /// every path as <c>&lt;path&gt;</c>: an exception's message names the file it failed on in one of the two.
    /// </summary>
    public static string Mask(string line, IEnumerable<string>? names = null)
    {
        line = QuotedRegex().Replace(line, "<text>");
        line = PathRegex().Replace(line, "<path>");
        return Redaction.Redact(line, names);
    }

    /// <summary>The plain form's app line: without the window's title, which names the folder or the file in hand.</summary>
    public static string AppLine(string app, string version, string windows, string nvda) =>
        $"{app} {version}, {windows}, {nvda}";

    /// <summary>The fuller form's app line: with the window's title.</summary>
    public static string AppLine(string app, string version, string windows, string nvda, string title) =>
        $"{AppLine(app, version, windows, nvda)}; window: {title}";

    /// <summary>The plain form's actions: the commands alone, never what the window said or the notices' titles, which can hold a name.</summary>
    public static IReadOnlyList<string> Commands(IEnumerable<(DateTime At, RecentActionKind Kind, string Text)> entries, DateTime now) =>
        entries.Where(entry => entry.Kind == RecentActionKind.Command)
            .Select(entry => RecentActions.Line(entry.At, entry.Kind, entry.Text, now))
            .ToList();

    /// <summary>
    /// The plain form's log lines: the errors of the last day with the lines that belong to them (an exception's
    /// stack), each masked, the last <see cref="LogLines"/> of them. The lines of a start or an end, which name the
    /// folder or file an app was started on, and everything older are left out.
    /// </summary>
    public static IReadOnlyList<string> Errors(IEnumerable<string> log, DateTime now, IEnumerable<string>? names = null)
    {
        var kept = new List<string>();
        var keeping = false;
        foreach (var line in log)
        {
            if (LogLineRegex().Match(line) is { Success: true } start)
            {
                keeping = start.Groups[2].Value != "INFO "
                    && DateTime.TryParseExact(start.Groups[1].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                    && now - at < TimeSpan.FromDays(1) && at - now < TimeSpan.FromMinutes(5);
            }

            if (keeping && line.Trim().Length > 0)
            {
                kept.Add(Mask(line.Trim(), names));
            }
        }

        return kept.Skip(Math.Max(0, kept.Count - LogLines)).ToList();
    }
}
