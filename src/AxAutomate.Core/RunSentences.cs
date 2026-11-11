namespace AxAutomate.Core;

/// <summary>
/// How a workflow's run ended, for its row and its details (SPEC.md AU-4.2, AU-4.5): passed, failed at a step (its
/// row's number, empty when the failure came after the steps), stopped, or could not run; and what the runner saw.
/// </summary>
public sealed record RunOutcome(string Mark, string FailedStep, string Detail);

/// <summary>
/// The words of a run in axAutomate's window (SPEC.md AU-4.5): nothing for a step that passes; a failing step said as
/// it ends, "Step 7 failed: Expect said: Command palette. Not within 60 s; seen: ...", with the step's own report
/// first when it has one; one line at the end, "02-palette passed, 6 steps" or "02-palette failed at step 3"; and the
/// mark the rows carry after it.
/// </summary>
public static class RunSentences
{
    /// <summary>The most of what the runner saw that is said; the details hold the same, cut the same way.</summary>
    public const int DetailLength = 240;

    /// <summary>The row of the step a line stands for: the very record (a workflow may hold two equal steps), or null.</summary>
    public static StepRow? RowOf(IReadOnlyList<StepRow> rows, WorkflowStep? step) =>
        step is null ? null : rows.FirstOrDefault(row => ReferenceEquals(row.Step, step));

    /// <summary>"Step 3" by its row's number, or "Line 12" when the line stands for no step known.</summary>
    public static string StepName(StepRow? row, ScriptLine line) => row is null ? "Line " + line.Number : "Step " + row.Number;

    /// <summary>A failing step as it ends: its name, its report or its sentence, and what the runner saw.</summary>
    public static string StepFailed(StepRow? row, LineResult result)
    {
        var step = result.Line.Step;
        var what = step is null ? result.Line.Text : step.Report.Length > 0 ? step.Report : StepText.Sentence(step);
        return $"{StepName(row, result.Line)} failed: {Sentence(what)}{Saw(result.Detail)}";
    }

    /// <summary>The line at a workflow's end, and the mark its row and its failed step's row carry after it.</summary>
    public static (string Said, RunOutcome Outcome) Ended(Workflow workflow, ScriptResult result, StepRow? failedRow)
    {
        var title = workflow.Title;
        if (result.Failed is { } failed)
        {
            var number = failedRow?.Number ?? string.Empty;
            var at = failedRow is null ? "at line " + failed.Line.Number : "at step " + number;
            return ($"{title} failed {at}", new RunOutcome("failed " + at, number, Sentence(Cut(failed.Detail ?? string.Empty))));
        }

        if (!result.Ok)
        {
            var problem = Sentence(Cut(string.Join("; ", result.Problems)));
            return ($"{title} failed after its steps: {problem}", new RunOutcome("failed after its steps", string.Empty, problem));
        }

        if (result.Stopped)
        {
            return ($"{title} stopped", new RunOutcome("stopped", string.Empty, "Stopped with Escape before its end"));
        }

        var steps = StepText.Count(workflow.StepCount, "step");
        return ($"{title} passed, {steps}", new RunOutcome("passed", string.Empty, $"All {steps} passed"));
    }

    /// <summary>A workflow that cannot run (AU-3.3.2): the line said and its row's mark.</summary>
    public static (string Said, RunOutcome Outcome) CannotRun(Workflow workflow, string refusal) =>
        ($"{workflow.Title} cannot run: {refusal}", new RunOutcome("cannot run", string.Empty, refusal));

    /// <summary>
    /// The line after a folder's run: how many passed of how many, and the rest by kind, "Run finished: 3 of 5
    /// workflows passed, 1 failed, 1 could not run". A stopped run names the workflow it stopped in, when one was
    /// going on, and counts the workflows it never reached: "Run stopped at 02-x: 1 passed, 3 not run".
    /// </summary>
    public static string FolderEnded(int passed, int failed, int cannotRun, bool stopped, string? stoppedAt = null, int notRun = 0)
    {
        var total = passed + failed + cannotRun;
        var parts = new List<string> { stopped ? $"{passed} passed" : $"{passed} of {StepText.Count(total, "workflow")} passed" };
        if (failed > 0)
        {
            parts.Add($"{failed} failed");
        }

        if (cannotRun > 0)
        {
            parts.Add($"{cannotRun} could not run");
        }

        if (stopped && notRun > 0)
        {
            parts.Add($"{notRun} not run");
        }

        var head = !stopped ? "Run finished" : stoppedAt is null ? "Run stopped" : "Run stopped at " + stoppedAt;
        return head + ": " + string.Join(", ", parts);
    }

    /// <summary>Text cut to <see cref="DetailLength"/>, with "..." where it was cut.</summary>
    public static string Cut(string text)
    {
        var flat = text.Replace("\r", string.Empty).Replace('\n', ' ').Trim();
        return flat.Length <= DetailLength ? flat : flat[..DetailLength].TrimEnd() + "...";
    }

    private static string Saw(string? detail) => string.IsNullOrWhiteSpace(detail) ? string.Empty : " " + Sentence(Cut(detail));

    /// <summary>A part said as a sentence: a capital first, a full stop after it unless it ends in one already.</summary>
    private static string Sentence(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        trimmed = char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
        return trimmed.EndsWith('.') || trimmed.EndsWith("...", StringComparison.Ordinal) ? trimmed : trimmed + ".";
    }
}
