using System.Globalization;

namespace AxAutomate.Core;

/// <summary>One row of a workflow as the navigator shows it (SPEC.md AU-4.2): its number, where it sits, and the step.</summary>
public sealed record StepRow(string Number, string Block, WorkflowStep Step, int Depth)
{
    /// <summary>The row's line: "5. Expect said: trust, may not come, then 2 steps", "5.1 then: Wait 1.5 seconds", "Note: ...".</summary>
    public string Label => Step.IsNote
        ? "Note: " + Step.Note
        : $"{Number}{(Number.Contains('.') ? string.Empty : ".")} {(Block.Length > 0 ? Block + ": " : string.Empty)}{StepText.Sentence(Step)}{StepText.Marks(Step)}";
}

/// <summary>
/// The words of a workflow (SPEC.md AU-4.2, AU-D5): each step as a sentence, the small marks that say it may fail or
/// holds steps, and the steps flattened into rows, the steps of a <c>then</c> or <c>otherwise</c> block right after
/// the step that holds them, numbered under it (5.1, 5.2) and named by their block, so that the arrows go through a
/// workflow in the order it runs, with no level to go into.
/// </summary>
public static class StepText
{
    public static string Sentence(WorkflowStep step)
    {
        var text = step.Value("text");
        return step.Do switch
        {
            "keys" => "Press " + Keys(step.Value("keys")),
            "type" => "Type " + text,
            "menu" => "Choose the menu item " + step.Value("name"),
            "start" => "Start " + step.Value("arguments"),
            "launch" => "Launch " + step.Value("arguments"),
            "adopt" => "Take the window the app opened",
            "use" => "Use window " + step.Value("window"),
            "close" => "Close the window",
            "run" => "Run " + step.Value("command"),
            "wait" => "Wait " + Seconds(step.Value("ms")),
            "mkdir" => "Make the folder " + step.Value("path"),
            "file" => "Make the file " + step.Value("path"),
            "silence" => $"Make a silent sound {step.Value("path")}, {Seconds(step.Value("ms"))}",
            "said" => "Print what was said",
            "spoken" => "Print what NVDA spoke",
            "text" or "list" or "commands" or "state" or "focus" => "Print the " + step.Do,
            "check commands" => "Check the commands",
            "allow error" => "Allow a logged error: " + text,
            "mcp start" => "Start the AI tools",
            "mcp call" => "Call the tool " + step.Value("tool"),
            "expect said" => "Expect said: " + text,
            "expect spoken" => "Expect NVDA to say: " + text,
            "expect closed" => "Expect the window closed",
            "expect quiet" => "Expect nothing said for " + Seconds(step.Value("ms")),
            "expect mcp" => "Expect the tool's answer: " + text,
            "refuse said" => "Must not say: " + text,
            "refuse spoken" => "NVDA must not say: " + text,
            "refuse mcp" => "The tool's answer must not hold: " + text,
            _ when step.Do.StartsWith("expect ", StringComparison.Ordinal) => $"Expect the {step.Do[7..]}: {text}",
            _ when step.Do.StartsWith("refuse ", StringComparison.Ordinal) => $"The {step.Do[7..]} must not hold: {text}",
            _ => (step.Do + " " + string.Join(" ", step.Values.Select(value => value.Value))).Trim(),
        };
    }

    /// <summary>The marks after a step's sentence: it may fail, it reports, it holds steps.</summary>
    public static string Marks(WorkflowStep step)
    {
        var marks = new List<string>();
        if (step.OnFailure == WorkflowStep.Continue)
        {
            marks.Add(step.Do.StartsWith("expect ", StringComparison.Ordinal) ? "may not come" : "goes on if it fails");
        }

        if (step.Report.Length > 0)
        {
            marks.Add("reports");
        }

        if (step.Then.Count > 0)
        {
            marks.Add("then " + Count(step.Then.Count(inner => !inner.IsNote), "step"));
        }

        if (step.Otherwise.Count > 0)
        {
            marks.Add("otherwise " + Count(step.Otherwise.Count(inner => !inner.IsNote), "step"));
        }

        return marks.Count == 0 ? string.Empty : ", " + string.Join(", ", marks);
    }

    /// <summary>The workflow's rows in the order they run, nested steps after the step that holds them.</summary>
    public static IReadOnlyList<StepRow> Rows(Workflow workflow)
    {
        var rows = new List<StepRow>();
        Add(rows, workflow.Steps, prefix: string.Empty, block: string.Empty, depth: 0);
        return rows;
    }

    private static void Add(List<StepRow> rows, IReadOnlyList<WorkflowStep> steps, string prefix, string block, int depth)
    {
        var number = 0;
        foreach (var step in steps)
        {
            if (step.IsNote)
            {
                rows.Add(new StepRow(string.Empty, block, step, depth));
                continue;
            }

            number++;
            var own = prefix + number.ToString(CultureInfo.InvariantCulture);
            rows.Add(new StepRow(own, block, step, depth));
            Add(rows, step.Then, own + ".", "then", depth + 1);
            Add(rows, step.Otherwise, own + ".", "otherwise", depth + 1);
        }
    }

    /// <summary>The detail pane's lines for a step (SPEC.md AU-4.3): what it does, each value, and the rest.</summary>
    public static IReadOnlyList<string> Details(StepRow row, Workflow workflow)
    {
        var step = row.Step;
        if (step.IsNote)
        {
            return ["Note: " + step.Note, Where(step, workflow)];
        }

        var lines = new List<string> { $"Step {row.Number}{(row.Block.Length > 0 ? ", in the " + row.Block + " block" : string.Empty)}", "Does: " + step.Do };
        lines.AddRange(step.Values.Select(value => $"{Capital(value.Key)}: {value.Value}"));
        lines.Add(step.OnFailure == WorkflowStep.Continue
            ? "On failure: continue, the run goes on" + (step.Then.Count > 0 ? " and the then steps are skipped" : string.Empty)
            : "On failure: stop, the workflow fails here");
        lines.Add(step.Report.Length > 0 ? "Report: " + step.Report : "Report: the runner's own words");
        if (step.Then.Count > 0)
        {
            lines.Add($"Then: {Count(step.Then.Count(inner => !inner.IsNote), "step")}, run only when this one passed");
        }

        if (step.Otherwise.Count > 0)
        {
            lines.Add($"Otherwise: {Count(step.Otherwise.Count(inner => !inner.IsNote), "step")}, run only when this one failed");
        }

        if (step.Note.Length > 0)
        {
            lines.Add("Note: " + step.Note);
        }

        lines.Add(Where(step, workflow));
        return lines;
    }

    private static string Where(WorkflowStep step, Workflow workflow) =>
        step.Line > 0 ? $"Line {step.Line} of {Path.GetFileName(workflow.Path)}" : "In " + Path.GetFileName(workflow.Path);

    /// <summary>Keys as they are named aloud: "ctrl+shift+p down" is "Ctrl+Shift+P, Down".</summary>
    public static string Keys(string keys) =>
        string.Join(", ", keys.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(key => string.Join("+", key.Split('+').Select(Capital))));

    /// <summary>Milliseconds as seconds: "1500" is "1.5 seconds", "1000" is "1 second".</summary>
    public static string Seconds(string ms)
    {
        if (!double.TryParse(ms, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return ms + " milliseconds";
        }

        var seconds = value / 1000;
        return seconds == 1 ? "1 second" : seconds.ToString("0.###", CultureInfo.InvariantCulture) + " seconds";
    }

    public static string Count(int count, string noun) => $"{count} {(count == 1 ? noun : noun + "s")}";

    /// <summary>A key or a word with a capital: "ctrl" is "Ctrl", "p" is "P", "f1" is "F1".</summary>
    private static string Capital(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
}
