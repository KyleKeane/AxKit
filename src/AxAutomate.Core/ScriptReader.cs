namespace AxAutomate.Core;

/// <summary>
/// A script of <c>tools/AxAutomate</c> (one step per line, <c>#</c> for a comment; the runner's <c>Program.cs</c> header
/// lists the steps) read as a workflow (SPEC.md AU-5.2), so that the existing checks are viewed as workflows before
/// they are converted: the comment lines before the first step are what the workflow covers; later comment lines,
/// one run of them at a time, are notes; <c>try said</c> is <c>expect said</c> that goes on when it fails, and the
/// <c>iffound</c> lines after it are its <c>then</c> steps (AU-3.3.1). Beside the steps it reads the script's
/// sections, its goals and checks (<see cref="Goals"/>, SPEC.md AU-5.2b): a goal or check line ends the header and
/// stays a note's text, as every comment after the header is.
/// </summary>
public static class ScriptReader
{
    public const string Extension = ".txt";

    /// <summary>The first words that take a second word as part of the operation: <c>expect said</c>, <c>mcp call</c>.</summary>
    private static readonly HashSet<string> TwoWords = new(StringComparer.Ordinal) { "expect", "refuse", "try", "check", "allow", "mcp" };

    public static Workflow Read(string path)
    {
        var title = System.IO.Path.GetFileNameWithoutExtension(path);
        try
        {
            return Parse(File.ReadAllLines(path), title, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new Workflow(title, string.Empty, [], path, "the file could not be read");
        }
    }

    public static Workflow Parse(IReadOnlyList<string> lines, string title, string path)
    {
        var covers = new List<string>();
        var steps = new List<WorkflowStep>();
        var note = new List<string>();
        var noteLine = 0;
        string? problem = null;
        var needs = string.Empty;
        var headerLength = HeaderLength(lines);

        void EndNote()
        {
            if (note.Count > 0)
            {
                steps.Add(WorkflowStep.NoteOnly(string.Join(" ", note), noteLine));
                note.Clear();
            }
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                EndNote();
                continue;
            }

            if (line.StartsWith('#'))
            {
                var text = CommentText(line);
                if (i < headerLength && LabelOf(text) is { } label)
                {
                    // AU-5.2a: the workflow's label, a task in the user's words, in place of the file's name.
                    title = label;
                }
                else if (i < headerLength && IsStable(text))
                {
                    // WF-3.5: the mark of a script without sections, which is not what the workflow covers.
                }
                else if (i < headerLength)
                {
                    // The needs line stays among what the workflow covers, as the window shows it (WF-3.6).
                    needs = NeedsOf(text) ?? needs;
                    covers.Add(text);
                }
                else
                {
                    if (note.Count == 0)
                    {
                        noteLine = i + 1;
                    }

                    note.Add(text);
                }

                continue;
            }

            EndNote();
            var step = Step(line, i + 1);
            if (step.Do == "iffound")
            {
                var inner = Step(step.Value("step"), i + 1);
                var last = steps.FindLastIndex(candidate => !candidate.IsNote);
                if (last < 0 || steps[last].OnFailure != WorkflowStep.Continue)
                {
                    problem ??= $"line {i + 1}: iffound without a try before it";
                    steps.Add(inner);
                }
                else
                {
                    steps[last] = steps[last] with { Then = [.. steps[last].Then, inner] };
                }

                continue;
            }

            steps.Add(step);
        }

        EndNote();
        return new Workflow(title, string.Join(" ", covers), steps, path, problem) { Goals = ScriptReader.Goals(lines, title), Needs = needs };
    }

    /// <summary>
    /// How many lines the header takes (WF-3.6): the comment lines before the first step, ended early by a blank line
    /// once a line of what the workflow covers has come (the label and a <c># Stable</c> line are none), and by a goal
    /// or check line, which is never part of it.
    /// </summary>
    private static int HeaderLength(IReadOnlyList<string> lines)
    {
        var covers = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                if (covers)
                {
                    return i;
                }

                continue;
            }

            if (!line.StartsWith('#'))
            {
                return i;
            }

            var text = CommentText(line);
            if (GoalOf(text) is not null || CheckOf(text) is not null)
            {
                return i;
            }

            covers |= LabelOf(text) is null && !IsStable(text);
        }

        return lines.Count;
    }

    /// <summary>A comment line's text: the line, trimmed, without its <c>#</c> marks and the spaces after them.</summary>
    private static string CommentText(string trimmedLine) => trimmedLine.TrimStart('#').Trim();

    /// <summary>The start of the header line that names the workflow (AU-5.2a): <c># Workflow: Answer one of Claude's questions</c>.</summary>
    public const string LabelPrefix = "Workflow:";

    /// <summary>The label of a header comment's text, or null when the line is no label.</summary>
    public static string? LabelOf(string comment) => After(comment, LabelPrefix);

    /// <summary>The start of a goal line (WF-3.1): <c># Goal: Find a word in the text</c>, in any case, anywhere after the header.</summary>
    public const string GoalPrefix = "Goal:";

    /// <summary>The start of a check line (WF-3.1a): <c># Check: A word that is not in the text is said as not found</c>.</summary>
    public const string CheckPrefix = "Check:";

    /// <summary>The one word of a stable line (WF-3.5), in the comment lines that begin with a goal or check line, or in the header of a script with neither.</summary>
    public const string StableWord = "Stable";

    /// <summary>The start of the header line that says what the script depends on outside axKit (WF-3.6): <c># Needs: Git installed.</c>.</summary>
    public const string NeedsPrefix = "Needs:";

    /// <summary>The goal's title of a comment's text, or null when the line is no goal line ("Goal:" alone is a comment).</summary>
    public static string? GoalOf(string comment) => After(comment, GoalPrefix);

    /// <summary>The check's title of a comment's text, or null when the line is no check line ("Check:" alone is a comment).</summary>
    public static string? CheckOf(string comment) => After(comment, CheckPrefix);

    /// <summary>What a needs line says, or null when the comment is no needs line.</summary>
    public static string? NeedsOf(string comment) => After(comment, NeedsPrefix);

    /// <summary>
    /// What a needs line says of a live script (docs/axKit/automation.md): one that would start the real Claude
    /// Code on the user's account, or ask a server. The same three as <c>tools/window-run.ps1</c> holds, which
    /// <c>GoalTests</c> checks; a line that says "no Claude Code, no network" names none of them.
    /// </summary>
    public static readonly IReadOnlyList<string> LiveNeeds = ["Claude Code signed in", "the network", "GitHub"];

    /// <summary>Whether what a needs line says makes its script a live one, never run by a folder's run unless it is meant.</summary>
    public static bool IsLive(string needs) => LiveNeeds.Any(need => needs.Contains(need, StringComparison.OrdinalIgnoreCase));

    /// <summary>What the needs line of a script's file says, read from the comment lines at its head alone; empty without one.</summary>
    public static string NeedsOfFile(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (!line.StartsWith('#'))
            {
                // The head of a script is its comment lines; the first step ends it.
                break;
            }

            if (NeedsOf(CommentText(line)) is { } needs)
            {
                return needs;
            }
        }

        return string.Empty;
    }

    private static bool IsStable(string comment) => string.Equals(comment, StableWord, StringComparison.OrdinalIgnoreCase);

    /// <summary>The text after the prefix (any case), trimmed, or null when the comment does not start with it or holds nothing after it.</summary>
    private static string? After(string comment, string prefix) =>
        comment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && comment[prefix.Length..].Trim() is { Length: > 0 } rest ? rest : null;

    /// <summary>
    /// A script's sections (WF-3.1 to WF-3.5), lines numbered from 1: each goal line and check line starts one, which
    /// runs to the line before the next or to the script's end, and is stable when a <c># Stable</c> line stands in the
    /// comment lines that begin with it. A script with neither line is one goal, not named, titled
    /// <paramref name="title"/> (its label) and covering every line, stable when its header holds the stable line.
    /// </summary>
    public static IReadOnlyList<Goal> Goals(IReadOnlyList<string> lines, string title)
    {
        var starts = new List<(int Line, string Title, bool Check)>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('#'))
            {
                continue;
            }

            var text = CommentText(line);
            if (GoalOf(text) is { } goal)
            {
                starts.Add((i + 1, goal, false));
            }
            else if (CheckOf(text) is { } check)
            {
                starts.Add((i + 1, check, true));
            }
        }

        if (starts.Count == 0)
        {
            var header = HeaderLength(lines);
            var stable = lines.Take(header).Select(line => line.Trim()).Any(line => line.StartsWith('#') && IsStable(CommentText(line)));
            return [new Goal(title, 1, lines.Count, stable, Named: false, Check: false)];
        }

        var goals = new List<Goal>();
        for (var k = 0; k < starts.Count; k++)
        {
            var (first, name, isCheck) = starts[k];
            var last = k + 1 < starts.Count ? starts[k + 1].Line - 1 : lines.Count;
            goals.Add(new Goal(name, first, last, StableAfter(lines, first), Named: true, isCheck));
        }

        return goals;
    }

    /// <summary>Whether a stable line stands in the run of comment lines after the section's own line (<paramref name="first"/>, from 1); a blank line or a step ends the run.</summary>
    private static bool StableAfter(IReadOnlyList<string> lines, int first)
    {
        for (var i = first; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('#'))
            {
                return false;
            }

            if (IsStable(CommentText(line)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The section that holds the line (from 1), or null when the line is in the setup, before the first section.</summary>
    public static Goal? GoalAt(IReadOnlyList<Goal> goals, int line) =>
        goals.FirstOrDefault(goal => goal.FirstLine <= line && line <= goal.LastLine);

    /// <summary>The step words of an input step (section 3 of docs/axKit/user-workflows.md): what the user would do at the keyboard, and the steps that open, choose and close windows.</summary>
    private static readonly HashSet<string> InputWords = new(StringComparer.Ordinal) { "do", "keys", "type", "menu", "start", "launch", "adopt", "use", "close" };

    /// <summary>
    /// Whether a script's line is an input step: its step word, after any <c>iffound</c>, is one of
    /// <see cref="InputWords"/>, or the line is an <c>mcp call</c>. An expectation, a file, a wait, <c>mcp start</c>,
    /// a comment or a blank line is none (WF-3.4, the traces of WF-5).
    /// </summary>
    public static bool IsInput(string line)
    {
        var text = line.Trim();
        while (text.StartsWith("iffound ", StringComparison.Ordinal))
        {
            text = text[8..].TrimStart();
        }

        var (word, rest) = Split(text);
        return InputWords.Contains(word) || (word == "mcp" && Split(rest).Word == "call");
    }

    /// <summary>One line as a step: the operation's words, then its values by the operation's keys (AU-3.3).</summary>
    public static WorkflowStep Step(string line, int number)
    {
        var (first, afterFirst) = Split(line);
        var operation = first;
        var rest = afterFirst;
        if (TwoWords.Contains(first) && afterFirst.Length > 0)
        {
            var (second, afterSecond) = Split(afterFirst);
            operation = first + " " + second;
            rest = afterSecond;
        }

        var onFailure = WorkflowStep.Stop;
        if (operation == "try said")
        {
            operation = "expect said";
            onFailure = WorkflowStep.Continue;
        }

        return new WorkflowStep(operation, Values(operation, rest), onFailure, string.Empty, string.Empty, [], [], number);
    }

    private static IReadOnlyList<StepValue> Values(string operation, string rest)
    {
        if (rest.Length == 0)
        {
            return [];
        }

        switch (operation)
        {
            case "file":
            case "silence":
                var (path, after) = PathAndRest(rest);
                return after.Length == 0 ? [new("path", path)] : [new("path", path), new(operation == "file" ? "text" : "ms", after)];
            case "mcp call":
                var (tool, arguments) = Split(rest);
                return arguments.Length == 0 ? [new("tool", tool)] : [new("tool", tool), new("arguments", arguments)];
            case "iffound":
                return [new("step", rest)];
        }

        var key = operation switch
        {
            "keys" => "keys",
            "type" => "text",
            "menu" => "name",
            "start" or "launch" => "arguments",
            "use" => "window",
            // WF-4.7: the step do names a command of the window, by its canonical name.
            "run" or "do" => "command",
            "wait" or "expect quiet" => "ms",
            "mkdir" => "path",
            _ => "text",
        };
        return [new StepValue(key, rest)];
    }

    private static (string Word, string After) Split(string text)
    {
        var space = text.IndexOf(' ');
        return space < 0 ? (text, string.Empty) : (text[..space], text[(space + 1)..].Trim());
    }

    /// <summary>A path first, in quotes when it holds a space, then the rest.</summary>
    private static (string Path, string After) PathAndRest(string text)
    {
        if (text.StartsWith('"') && text.IndexOf('"', 1) is var close and > 0)
        {
            return (text[1..close], text[(close + 1)..].Trim());
        }

        return Split(text);
    }
}
