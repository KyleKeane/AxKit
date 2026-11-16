using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// One section of a run's trace (docs/axKit/user-workflows.md WF-5.1, format 5.4): its heading, <see cref="Traces.Setup"/>,
/// "Goal: &lt;title&gt;" or "Check: &lt;title&gt;", and its lines, each input step and the events under it.
/// </summary>
public sealed record TraceSection(string Heading, IReadOnlyList<string> Lines);

/// <summary>
/// A stable section whose trace is not the kept one (WF-5.5): the section's heading, the first line that differs
/// (from 1) with the kept text and the text of now, "(nothing)" for a line one of them lacks. <see cref="Line"/> 0: the
/// section has no kept trace, and both texts are empty.
/// </summary>
public sealed record TraceDifference(string Heading, int Line, string Kept, string Now);

/// <summary>
/// One line of a script as a traced run played it (WF-5.1): its number in the script, its input line
/// (<see cref="Traces.InputLine"/>; null for a step that is no input) and what every window of the run recorded up to
/// the line's end and after the line before it, as trace lines (<see cref="Traces.EventLine"/>), all cleaned
/// (<see cref="Traces.Clean"/>).
/// </summary>
public sealed record TracedLine(int Number, string? Input, IReadOnlyList<string> Events);

/// <summary>
/// A run's trace, pure (docs/axKit/user-workflows.md WF-5, format 5.4): the line of an input step and of an event, the
/// cleaning of every line, a run's lines as its sections, the file of a script's kept sections, which sections are kept
/// and how a kept one is compared, and which scripts may keep any. The engine collects the lines
/// (<see cref="ScriptRunner.Play"/> with <see cref="PlayOptions.Trace"/>) and the command line reads and writes the
/// files (<c>tools/AxAutomate/TraceRun.cs</c>); tested in <c>TraceTests</c>.
/// </summary>
public static class Traces
{
    /// <summary>The heading of the section before a script's first goal or check line, which is never kept (WF-5.4).</summary>
    public const string Setup = "Setup";

    private const string Header = "# Trace of ";
    private const string WrittenBy = "# Written by tools/AxAutomate --traces write; never by hand (docs/axKit/user-workflows.md WF-5).";
    private const string SectionMark = "== ";
    private const string Nothing = "(nothing)";
    private const string ScriptsFolder = "tests/scripts/";
    private const string TracesFolder = "tests/traces/";
    private const string TraceExtension = ".trace.txt";

    // The details a played step gives (Run.Step): a do step that pressed a chord or chose a menu item (WF-4.6), an iffound that did not run its step.
    private const string KeyDetail = "key ";
    private const string MenuItem = "menu item";
    private const string Skipped = "skipped";

    // WF-5.6: the folders of tests/scripts whose windows read the computer or the user's session; no script of them may keep a trace.
    private static readonly string[] NoTraceFolders =
    [
        "axClip",     // the test desktop shares the clipboard with the user's session
        "axStart",    // the programs installed
        "axProcess",  // the programs running
        "axTerminal", // a shell's output
        "axGIT",      // git's output
        "axMedia",    // the sound devices
    ];

    private const string GoalHeading = "Goal: ";
    private const string CheckHeading = "Check: ";

    /// <summary>A section's heading (5.4): "Goal: &lt;title&gt;" or "Check: &lt;title&gt;".</summary>
    public static string Heading(Goal goal) => (goal.Check ? CheckHeading : GoalHeading) + goal.Title;

    /// <summary>
    /// Whether a script may keep a trace in the repository (WF-5.6), by its path from the repository's root and its
    /// needs line: never with a needs line (a model's words, a program's output), never one of tests/nvda-scripts or
    /// anywhere else outside tests/scripts, and never one of the folders of tests/scripts whose windows read the
    /// computer or the user's session (<see cref="NoTraceFolders"/>), without regard to case.
    /// </summary>
    public static bool MayKeep(string scriptFromRepository, string needs)
    {
        if (!string.IsNullOrWhiteSpace(needs))
        {
            return false;
        }

        // tests/nvda-scripts, and a script played from elsewhere, are not under tests/scripts.
        var path = scriptFromRepository.Replace('\\', '/');
        if (!path.StartsWith(ScriptsFolder, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = path[ScriptsFolder.Length..];
        var slash = rest.IndexOf('/');
        var folder = slash < 0 ? string.Empty : rest[..slash];
        return !NoTraceFolders.Contains(folder, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The trace line of a script's line once it was played (5.4), with the detail the step gave: the line as written
    /// (trimmed, before <c>{temp}</c> is filled) for an input step (<see cref="ScriptReader.IsInput"/>); a <c>do</c> step
    /// with what it pressed, "do &lt;name&gt; = &lt;chord&gt;" for the detail "key &lt;chord&gt;" and
    /// "do &lt;name&gt; = menu item" for "menu item"; an <c>mcp call</c> as "mcp call &lt;tool&gt;" without its
    /// arguments; an <c>iffound</c> as its inner step, or null when the detail is "skipped". Null for every step that
    /// prepares or only looks, a comment and a blank line (WF-5.2).
    /// </summary>
    public static string? InputLine(string scriptLine, string? detail)
    {
        var text = scriptLine.Trim();
        if (text.StartsWith("iffound ", StringComparison.Ordinal))
        {
            return detail == Skipped ? null : InputLine(text[8..], detail);
        }

        if (!ScriptReader.IsInput(text))
        {
            return null;
        }

        var (word, rest) = Split(text);
        if (word == "do")
        {
            if (detail is not null && detail.StartsWith(KeyDetail, StringComparison.Ordinal))
            {
                return $"do {rest} = {detail[KeyDetail.Length..].Trim()}";
            }

            return detail == MenuItem ? $"do {rest} = {MenuItem}" : text;
        }

        if (word == "mcp")
        {
            var tool = Split(Split(rest).After).Word;
            return tool.Length == 0 ? "mcp call" : "mcp call " + tool;
        }

        return text;
    }

    /// <summary>
    /// The trace line of an event of window <paramref name="window"/> (from 1) of a run of <paramref name="windows"/>
    /// windows (5.4): two spaces, the window's number and ": " only when the run has several, and the event as the
    /// report's timeline writes it (<see cref="Event.Spoken"/>), with no time. A line break in its text is written
    /// "\n" and trailing spaces are left out, so that an event is one line of a file and an editor that trims lines
    /// changes nothing.
    /// </summary>
    public static string EventLine(int window, int windows, Event e)
    {
        var number = windows > 1 ? window + ": " : string.Empty;
        return "  " + (number + e.Spoken.ReplaceLineEndings("\\n")).TrimEnd();
    }

    /// <summary>
    /// A line of a trace without the computer's names (WF-5.3): the run's folder as <c>{temp}</c>, then the profile
    /// folder as <c>{home}</c>, each with back slashes, with forward slashes and with doubled back slashes, without
    /// regard to case; then <see cref="Redaction.Redact"/>, which knows an account path, an account name and an e-mail
    /// address. An empty folder is not replaced.
    /// </summary>
    public static string Clean(string text, string temp, string profile)
    {
        text = Placeholder(text, temp, "{temp}");
        text = Placeholder(text, profile, "{home}");
        return Redaction.Redact(text);
    }

    private static string Placeholder(string text, string folder, string placeholder)
    {
        var plain = folder.Trim().Replace('/', '\\').TrimEnd('\\');
        if (plain.Length == 0)
        {
            return text;
        }

        // The doubled spelling first: the plain one is never part of it, and JSON writes a path so.
        text = text.Replace(plain.Replace("\\", "\\\\"), placeholder, StringComparison.OrdinalIgnoreCase);
        text = text.Replace(plain, placeholder, StringComparison.OrdinalIgnoreCase);
        return text.Replace(plain.Replace('\\', '/'), placeholder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A run's trace as its sections (WF-5.1), from the lines it played, in their order: a line belongs to the section
    /// that holds its number (<see cref="ScriptReader.GoalAt"/>; the setup before the first), and a section's lines are,
    /// for each of its played lines, the input line when it is one and then the line's events. So what the windows
    /// said after a section's last step and before the next section's first is the earlier section's: it was taken
    /// with that last step. A section no line of which was played has none; one whose lines gave nothing has no lines.
    /// </summary>
    public static IReadOnlyList<TraceSection> Sections(IReadOnlyList<TracedLine> played, IReadOnlyList<Goal> goals)
    {
        var sections = new List<TraceSection>();
        Goal? current = null;
        List<string>? lines = null;
        foreach (var line in played)
        {
            var goal = ScriptReader.GoalAt(goals, line.Number);
            if (lines is null || goal != current)
            {
                current = goal;
                lines = [];
                sections.Add(new TraceSection(goal is null ? Setup : Heading(goal), lines));
            }

            if (line.Input is not null)
            {
                lines.Add(line.Input);
            }

            lines.AddRange(line.Events);
        }

        return sections;
    }

    /// <summary>
    /// What a difference is called after the steps (WF-5.5), the words after "FAIL after the steps: ": "the trace of
    /// the stable goal &lt;title&gt; differs at its line &lt;n&gt;: kept &lt;line&gt;; now &lt;line&gt;", or, for a
    /// section with no kept trace (line 0), "the stable goal &lt;title&gt; has no kept trace: run with --traces
    /// write"; "check" in place of "goal" for a check. The two lines are given without the spaces they begin with.
    /// </summary>
    public static string Problem(TraceDifference difference)
    {
        var check = difference.Heading.StartsWith(CheckHeading, StringComparison.Ordinal);
        var title = difference.Heading[(check ? CheckHeading : GoalHeading).Length..];
        var section = check ? "check" : "goal";
        return difference.Line == 0
            ? $"the stable {section} {title} has no kept trace: run with --traces write"
            : $"the trace of the stable {section} {title} differs at its line {difference.Line}: kept {difference.Kept.Trim()}; now {difference.Now.Trim()}";
    }

    /// <summary>
    /// The sections that may be kept (WF-5.4): those of the goals and checks marked stable, in the script's order,
    /// never the setup. Each goal takes the first section of its heading not taken by a goal before it.
    /// </summary>
    public static IReadOnlyList<TraceSection> Stable(IReadOnlyList<TraceSection> sections, IReadOnlyList<Goal> goals)
    {
        var left = sections.Where(section => section.Heading != Setup).ToList();
        var kept = new List<TraceSection>();
        foreach (var goal in goals)
        {
            var heading = Heading(goal);
            var index = left.FindIndex(section => section.Heading == heading);
            if (index < 0)
            {
                continue;
            }

            if (goal.Stable)
            {
                kept.Add(left[index]);
            }

            left.RemoveAt(index);
        }

        return kept;
    }

    /// <summary>
    /// The file of a script's kept trace (5.4): the header, with the script's path from the repository's root in
    /// forward slashes, then each section after a blank line, its heading after "== ", then its lines; line feeds,
    /// one at the end.
    /// </summary>
    public static string Write(string script, IReadOnlyList<TraceSection> sections)
    {
        var lines = new List<string> { Header + script.Replace('\\', '/'), WrittenBy };
        foreach (var section in sections)
        {
            lines.Add(string.Empty);
            lines.Add(SectionMark + section.Heading);
            lines.AddRange(section.Lines);
        }

        return string.Join("\n", lines) + "\n";
    }

    /// <summary>
    /// The sections of a trace file (5.4), in the file's order: each "== " line starts one, and the lines after it up
    /// to the next are its own; what comes before the first (the header) and the blank lines between sections are
    /// none. Carriage returns are left out, as a checkout on Windows writes them.
    /// </summary>
    public static IReadOnlyList<TraceSection> Read(string text)
    {
        var sections = new List<TraceSection>();
        string? heading = null;
        var lines = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith(SectionMark, StringComparison.Ordinal))
            {
                if (heading is not null)
                {
                    sections.Add(new TraceSection(heading, lines));
                }

                heading = line[SectionMark.Length..];
                lines = [];
            }
            else if (heading is not null && line.Length > 0)
            {
                lines.Add(line);
            }
        }

        if (heading is not null)
        {
            sections.Add(new TraceSection(heading, lines));
        }

        return sections;
    }

    /// <summary>
    /// What differs between the kept sections and the stable sections of now (WF-5.5), at most one difference for each
    /// section of now, in its order: a section with no kept one of its heading gives line 0; else the first line that
    /// differs gives its number with both texts, "(nothing)" for a line one of them lacks; equal sections give none. A
    /// kept section with no section of now is not compared here (the cheap check of WF-5.9 finds it).
    /// </summary>
    public static IReadOnlyList<TraceDifference> Compare(IReadOnlyList<TraceSection> kept, IReadOnlyList<TraceSection> now)
    {
        var left = kept.ToList();
        var differences = new List<TraceDifference>();
        foreach (var section in now)
        {
            var index = left.FindIndex(candidate => candidate.Heading == section.Heading);
            if (index < 0)
            {
                differences.Add(new TraceDifference(section.Heading, 0, string.Empty, string.Empty));
                continue;
            }

            var was = left[index].Lines;
            left.RemoveAt(index);
            var count = Math.Max(was.Count, section.Lines.Count);
            for (var i = 0; i < count; i++)
            {
                if (i >= was.Count || i >= section.Lines.Count || !string.Equals(was[i], section.Lines[i], StringComparison.Ordinal))
                {
                    var keptLine = i < was.Count ? was[i] : Nothing;
                    var nowLine = i < section.Lines.Count ? section.Lines[i] : Nothing;
                    differences.Add(new TraceDifference(section.Heading, i + 1, keptLine, nowLine));
                    break;
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Where a script's kept trace is (WF-5.4): <c>tests/scripts/axDown/04-x.txt</c> as
    /// <c>tests/traces/axDown/04-x.trace.txt</c>, forward slashes; a script outside tests/scripts, which keeps none,
    /// by its file's name alone.
    /// </summary>
    public static string PathFor(string scriptFromRepository)
    {
        var path = scriptFromRepository.Replace('\\', '/');
        var rest = path.StartsWith(ScriptsFolder, StringComparison.OrdinalIgnoreCase) ? path[ScriptsFolder.Length..] : path[(path.LastIndexOf('/') + 1)..];
        if (rest.EndsWith(ScriptReader.Extension, StringComparison.OrdinalIgnoreCase))
        {
            rest = rest[..^ScriptReader.Extension.Length];
        }

        return TracesFolder + rest + TraceExtension;
    }

    private static (string Word, string After) Split(string text)
    {
        var space = text.IndexOf(' ');
        return space < 0 ? (text, string.Empty) : (text[..space], text[(space + 1)..].Trim());
    }
}
