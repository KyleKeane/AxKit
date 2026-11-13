namespace AxAutomate.Core;

/// <summary>
/// A script of <c>tools/AxAutomate</c> (one step per line, <c>#</c> for a comment; the runner's <c>Program.cs</c> header
/// lists the steps) read as a workflow (SPEC.md AU-5.2), so that the existing checks are viewed as workflows before
/// they are converted: the comment lines before the first step are what the workflow covers; later comment lines,
/// one run of them at a time, are notes; <c>try said</c> is <c>expect said</c> that goes on when it fails, and the
/// <c>iffound</c> lines after it are its <c>then</c> steps (AU-3.3.1).
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
        var inHeader = true;

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
                inHeader = covers.Count == 0 && inHeader;
                EndNote();
                continue;
            }

            if (line.StartsWith('#'))
            {
                var text = line.TrimStart('#').Trim();
                if (inHeader && LabelOf(text) is { } label)
                {
                    // AU-5.2a: the workflow's label, a task in the user's words, in place of the file's name.
                    title = label;
                }
                else if (inHeader)
                {
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

            inHeader = false;
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
        return new Workflow(title, string.Join(" ", covers), steps, path, problem);
    }

    /// <summary>The start of the header line that names the workflow (AU-5.2a): <c># Workflow: Answer one of Claude's questions</c>.</summary>
    public const string LabelPrefix = "Workflow:";

    /// <summary>The label of a header comment's text, or null when the line is no label.</summary>
    public static string? LabelOf(string comment) =>
        comment.StartsWith(LabelPrefix, StringComparison.OrdinalIgnoreCase) && comment[LabelPrefix.Length..].Trim() is { Length: > 0 } label ? label : null;

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
            "run" => "command",
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
