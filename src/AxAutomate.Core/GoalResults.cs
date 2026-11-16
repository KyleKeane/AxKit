namespace AxAutomate.Core;

/// <summary>
/// One section's result in a run (docs/axKit/user-workflows.md WF-3.9): <see cref="GoalResults.Passed"/>,
/// <see cref="GoalResults.Failed"/> or <see cref="GoalResults.NotRun"/>; for a failed one the line that failed (0 when
/// it failed after the steps) and the words the report gives after its title: "at line 39", or "after the steps: "
/// and the first problem.
/// </summary>
public sealed record GoalResult(Goal Goal, string Outcome, int FailedLine, string Detail);

/// <summary>
/// Which sections of a script passed, failed and were not run (WF-3.9), and the report's lines of them (WF-3.10,
/// section 5.1 of docs/axKit/user-workflows.md). A failing step still stops its script, so the results follow from
/// where it stopped: pure, and tested in <c>GoalTests</c>.
/// </summary>
public static class GoalResults
{
    public const string Passed = "passed", Failed = "failed", NotRun = "not run";

    /// <summary>
    /// The result of every section. <paramref name="failedLine"/>: the line of the step that failed, 0 when none did:
    /// a section that ends before it passed, the one that holds it failed there, the ones after it were not run (a
    /// failure in the setup runs none). <paramref name="lastLinePlayed"/>, read when no step failed: every line up to it
    /// was played (<see cref="int.MaxValue"/> for a script played to its end); a section that ends after it was not
    /// run, since the script was stopped before its end. <paramref name="problemsAfter"/>: what failed after the steps
    /// (a window that ended by itself, an ERROR in a log), which belongs to no one section and fails every section that
    /// passed, with the first problem said.
    /// </summary>
    public static IReadOnlyList<GoalResult> Of(IReadOnlyList<Goal> goals, int failedLine, int lastLinePlayed, IReadOnlyList<string> problemsAfter)
    {
        var results = new List<GoalResult>();
        foreach (var goal in goals)
        {
            GoalResult result;
            if (failedLine > 0)
            {
                result = goal.LastLine < failedLine ? new GoalResult(goal, Passed, 0, string.Empty)
                    : goal.FirstLine <= failedLine ? new GoalResult(goal, Failed, failedLine, $"at line {failedLine}")
                    : new GoalResult(goal, NotRun, 0, string.Empty);
            }
            else
            {
                result = goal.LastLine <= lastLinePlayed ? new GoalResult(goal, Passed, 0, string.Empty) : new GoalResult(goal, NotRun, 0, string.Empty);
            }

            if (result.Outcome == Passed && problemsAfter.Count > 0)
            {
                result = new GoalResult(goal, Failed, 0, "after the steps: " + problemsAfter[0]);
            }

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// The results with one section failed after the steps (WF-3.9, WF-5.5: its trace differs from the kept one, or it
    /// has none): the first section of that title that had passed is failed, at no line, with
    /// <paramref name="detail"/> as the words the report gives after its title ("after the steps: ..."); every other
    /// section is as it was. Unlike a failure after the steps that is no one section's (<see cref="Of"/>), it fails
    /// this section alone.
    /// </summary>
    public static IReadOnlyList<GoalResult> WithFailed(IReadOnlyList<GoalResult> results, string title, string detail)
    {
        var changed = results.ToList();
        var index = changed.FindIndex(result => result.Outcome == Passed && result.Goal.Title == title);
        if (index >= 0)
        {
            changed[index] = changed[index] with { Outcome = Failed, FailedLine = 0, Detail = detail };
        }

        return changed;
    }

    /// <summary>
    /// The line the report gives before a section's first step (WF-3.10): <c>-- Setup</c> for the setup (null), else
    /// <c>-- Goal: &lt;title&gt;</c> or <c>-- Check: &lt;title&gt;</c>.
    /// </summary>
    public static string Heading(Goal? section) =>
        section is null ? "-- Setup" : (section.Check ? "-- Check: " : "-- Goal: ") + section.Title;

    /// <summary>
    /// The report's lines after the last step (section 5.1, from "Goals:" on): when the setup failed (at
    /// <paramref name="setupFailedLine"/>, 0 when it did not), "The setup failed at line n" first; then the counts; then
    /// one line per section in the script's order: two spaces, the result in a column of nine characters, the title (a
    /// check's after "check: "), and for a failed one its detail after a comma.
    /// </summary>
    public static IReadOnlyList<string> Lines(IReadOnlyList<GoalResult> results, int setupFailedLine)
    {
        var lines = new List<string>();
        if (setupFailedLine > 0)
        {
            lines.Add($"The setup failed at line {setupFailedLine}");
        }

        lines.Add("Goals: " + Counts(results));
        foreach (var result in results)
        {
            var outcome = result.Outcome == Failed ? "FAILED" : result.Outcome;
            var title = result.Goal.Check ? "check: " + result.Goal.Title : result.Goal.Title;
            var detail = result.Outcome == Failed && result.Detail.Length > 0 ? ", " + result.Detail : string.Empty;
            lines.Add("  " + outcome.PadRight(9) + title + detail);
        }

        return lines;
    }

    /// <summary>The counts, goals and checks together: "2 passed, 1 failed, 4 not run"; the failed and the not run only when there are any.</summary>
    public static string Counts(IEnumerable<GoalResult> results)
    {
        var all = results.ToList();
        var text = $"{all.Count(result => result.Outcome == Passed)} passed";
        var failed = all.Count(result => result.Outcome == Failed);
        var notRun = all.Count(result => result.Outcome == NotRun);
        if (failed > 0)
        {
            text += $", {failed} failed";
        }

        if (notRun > 0)
        {
            text += $", {notRun} not run";
        }

        return text;
    }
}
