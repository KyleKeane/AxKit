namespace AxAutomate.Core;

/// <summary>
/// A workflow as the lines the engine plays (SPEC.md AU-5.2, AU-D7): the script interpreter of the runner is the
/// engine, so a <c>.txt</c> script runs its own lines exactly as the runner always read them, each tied to the step
/// <see cref="ScriptReader"/> made of it, and a <c>.workflow.json</c> file is written as the same lines, step by step:
/// an <c>expect said</c> that goes on when it fails is <c>try said</c>, its <c>then</c> steps <c>iffound</c> lines; the
/// <c>then</c> steps of a step that stops follow it as they are. What this version cannot play (an <c>otherwise</c>
/// block, another step that goes on when it fails, a block in a block, an operation the engine does not know, AU-3.3.2)
/// makes the workflow unrunnable, with the step said.
/// </summary>
public static class ScriptLines
{
    /// <summary>The operations the engine knows (the header of <c>tools/AxAutomate/Program.cs</c>), as <see cref="ScriptReader"/> names them.</summary>
    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        "mkdir", "file", "silence", "run", "start", "launch", "adopt", "use", "keys", "type", "menu", "close", "wait",
        "said", "text", "list", "commands", "state", "focus", "check commands", "allow error", "mcp start", "mcp call",
        "expect said", "expect focus", "expect state", "expect list", "expect text", "expect commands", "expect closed",
        "expect quiet", "expect mcp", "refuse said", "refuse focus", "refuse state", "refuse list", "refuse text",
        "refuse commands", "refuse mcp", "spoken", "expect spoken", "refuse spoken",
    };

    /// <summary>The operations of the NVDA tier alone, which need NVDA listening: never run from the window.</summary>
    public static readonly IReadOnlySet<string> NvdaOnly = new HashSet<string>(StringComparer.Ordinal) { "spoken", "expect spoken", "refuse spoken" };

    /// <summary>
    /// The lines of the workflow, or why it cannot run (the lines then empty). <paramref name="nvda"/>: the NVDA
    /// tier's steps may run (the command line's <c>--nvda</c>). A script's steps are matched by their line, so the
    /// steps of the lines are the very records of <paramref name="workflow"/>.
    /// </summary>
    public static (IReadOnlyList<ScriptLine> Lines, string? Refusal) Prepare(Workflow workflow, bool nvda)
    {
        if (CannotRun(workflow, nvda) is { } refusal)
        {
            return ([], refusal);
        }

        if (workflow.Path.EndsWith(ScriptReader.Extension, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return (FromScript(File.ReadAllLines(workflow.Path), workflow), null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ([], "the file could not be read");
            }
        }

        return FromWorkflow(workflow);
    }

    /// <summary>
    /// Why the workflow cannot run, from what was read alone (no file is read, so the window may ask it on its own
    /// thread for every row): not all understood, an operation the engine does not know, a number that is not one, a
    /// block the lines cannot say; null when it can run.
    /// </summary>
    public static string? CannotRun(Workflow workflow, bool nvda)
    {
        if (workflow.Problem is { } problem)
        {
            return "not all understood: " + problem;
        }

        if (Unknown(workflow.Steps, nvda) is { } unknown)
        {
            return unknown;
        }

        if (Flattened(workflow.Steps).Select(step => BadNumber(Line(step))).FirstOrDefault(bad => bad is not null) is { } bad)
        {
            return bad;
        }

        return workflow.Path.EndsWith(ScriptReader.Extension, StringComparison.OrdinalIgnoreCase) ? null : FromWorkflow(workflow).Refusal;
    }

    /// <summary>
    /// A line whose operation takes a number (<c>wait</c>, <c>expect quiet</c>, <c>use</c>, <c>silence</c>'s length)
    /// and does not hold one, said; null for every other line. The engine would otherwise fail on it mid-run.
    /// </summary>
    public static string? BadNumber(string line)
    {
        while (line.StartsWith("iffound ", StringComparison.Ordinal))
        {
            line = line[8..];
        }

        var space = line.IndexOf(' ');
        var verb = space < 0 ? line : line[..space];
        var rest = space < 0 ? string.Empty : line[(space + 1)..];
        return verb switch
        {
            "wait" when !Whole(rest) => $"the step \"{line}\" needs a whole number of milliseconds",
            "expect" when rest.StartsWith("quiet", StringComparison.Ordinal) && !Whole(rest.Length > 6 ? rest[6..] : string.Empty) => $"the step \"{line}\" needs a whole number of milliseconds",
            "use" when !(Whole(rest) && int.Parse(rest, System.Globalization.CultureInfo.InvariantCulture) > 0) => $"the step \"{line}\" needs the window's number, 1 for the first",
            "silence" when !(rest.Split(' ', 2) is [_, var ms] && Whole(ms)) => $"the step \"{line}\" needs a file name and a length in milliseconds",
            _ => null,
        };

        static bool Whole(string text) => int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    /// <summary>A script's lines as the runner reads them: every line but the blank ones and the comments, trimmed, numbered by its line.</summary>
    public static IReadOnlyList<ScriptLine> FromScript(IReadOnlyList<string> fileLines, Workflow? workflow)
    {
        var steps = workflow is null ? [] : Flattened(workflow.Steps);
        var lines = new List<ScriptLine>();
        for (var i = 0; i < fileLines.Count; i++)
        {
            var line = fileLines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            lines.Add(new ScriptLine(i + 1, line, steps.FirstOrDefault(step => step.Line == i + 1)));
        }

        return lines;
    }

    /// <summary>A workflow file's steps as lines (the class's summary), or why they cannot be.</summary>
    public static (IReadOnlyList<ScriptLine> Lines, string? Refusal) FromWorkflow(Workflow workflow)
    {
        var lines = new List<ScriptLine>();
        var number = 0;
        foreach (var step in workflow.Steps)
        {
            if (step.IsNote)
            {
                continue;
            }

            number++;
            if (step.Otherwise.Count > 0)
            {
                return ([], $"step {number} has otherwise steps, which a later version runs");
            }

            var then = step.Then.Where(inner => !inner.IsNote).ToList();
            if (then.Any(inner => inner.Then.Count > 0 || inner.Otherwise.Count > 0 || inner.OnFailure == WorkflowStep.Continue))
            {
                return ([], $"a step of step {number}'s then block has its own blocks or goes on when it fails, which a later version runs");
            }

            if (step.OnFailure == WorkflowStep.Continue)
            {
                if (step.Do != "expect said")
                {
                    return ([], $"step {number} goes on when it fails; in this version only an expect said may");
                }

                lines.Add(new ScriptLine(lines.Count + 1, "try said " + step.Value("text"), step));
                lines.AddRange(then.Select(inner => new ScriptLine(lines.Count + 1, "iffound " + Line(inner), inner)));
                continue;
            }

            lines.Add(new ScriptLine(lines.Count + 1, Line(step), step));
            foreach (var inner in then)
            {
                // A step that stops when it fails: its then steps run only after it passed, which is the next line.
                lines.Add(new ScriptLine(lines.Count + 1, Line(inner), inner));
            }
        }

        return (lines, null);
    }

    /// <summary>
    /// A step as the runner's line: its operation and its values in order; a file's text with its line breaks as
    /// <c>\n</c>, and its path in quotes when it holds a space, as a script writes them.
    /// </summary>
    public static string Line(WorkflowStep step)
    {
        switch (step.Do)
        {
            case "file":
                var path = step.Value("path");
                var text = step.Value("text").Replace("\r\n", "\n").Replace("\n", "\\n");
                var quoted = path.Contains(' ') ? $"\"{path}\"" : path;
                return text.Length == 0 ? "file " + quoted : $"file {quoted} {text}";
            default:
                return (step.Do + " " + string.Join(" ", step.Values.Select(value => value.Value))).Trim();
        }
    }

    /// <summary>The first step whose operation the engine does not know (or, without NVDA, needs it), said; null when every one is known.</summary>
    private static string? Unknown(IReadOnlyList<WorkflowStep> steps, bool nvda)
    {
        foreach (var step in Flattened(steps))
        {
            if (!Known.Contains(step.Do))
            {
                return $"the operation {step.Do} is not one the runner knows";
            }

            if (!nvda && NvdaOnly.Contains(step.Do))
            {
                return $"the operation {step.Do} needs NVDA listening, which only the NVDA pass has";
            }
        }

        return null;
    }

    /// <summary>Every step that runs, its blocks' included, in the order of the file; notes left out.</summary>
    private static List<WorkflowStep> Flattened(IReadOnlyList<WorkflowStep> steps)
    {
        var all = new List<WorkflowStep>();
        foreach (var step in steps.Where(step => !step.IsNote))
        {
            all.Add(step);
            all.AddRange(Flattened(step.Then));
            all.AddRange(Flattened(step.Otherwise));
        }

        return all;
    }
}
