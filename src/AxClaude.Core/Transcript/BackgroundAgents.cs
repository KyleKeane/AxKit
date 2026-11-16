namespace AxClaude.Core.Transcript;

/// <summary>
/// The kinds of work in the background a row of Claude Code's names: what a waiting row waits for
/// (<see cref="LineClassifier.BackgroundWaitOf"/>), <see cref="Other"/> for a kind of neither.
/// </summary>
public enum BackgroundKind
{
    None,
    Agent,
    Workflow,
    Other,
}

/// <summary>
/// One agent of Claude Code's panel of running agents, read off its row (<see cref="LineClassifier.AgentOf"/>):
/// the agent's type ("general-purpose", "Explore") and its description, without the mark in front, the word
/// "running" or the time and the tokens at the end. Two agents are the same when both texts are.
/// </summary>
public sealed record BackgroundAgent(string Type, string Description);

/// <summary>What changed between two readings of the panel: the agents newly shown and the agents no longer shown.</summary>
public sealed record BackgroundAgentChanges(IReadOnlyList<BackgroundAgent> Started, IReadOnlyList<BackgroundAgent> Finished)
{
    /// <summary>Nothing changed: the panel was redrawn as it was.</summary>
    public static readonly BackgroundAgentChanges None = new([], []);

    public bool Any => Started.Count > 0 || Finished.Count > 0;
}

/// <summary>
/// One dynamic workflow running in the background, read off its live row (<see cref="LineClassifier.WorkflowOf"/>):
/// its name, the percent done, the steps done of all of them, and the time it has run as Claude Code writes it
/// ("9m27s"). The row is redrawn as the workflow goes on, so two readings of one workflow differ in all but the name.
/// </summary>
public sealed record BackgroundWorkflow(string Name, int Percent, int Done, int Total, string Elapsed);

/// <summary>What changed between two readings of the workflows' rows: the workflows newly shown and those no longer shown.</summary>
public sealed record BackgroundWorkflowChanges(IReadOnlyList<BackgroundWorkflow> Started, IReadOnlyList<BackgroundWorkflow> Finished)
{
    /// <summary>Nothing changed: the rows were redrawn with the same workflows, at other percents and times.</summary>
    public static readonly BackgroundWorkflowChanges None = new([], []);

    public bool Any => Started.Count > 0 || Finished.Count > 0;
}

/// <summary>
/// The rule of what changed in the panel of running agents (<see cref="SessionModel.BackgroundAgents"/>) from one
/// reading to the next, for a window that says so only on a change: Claude Code redraws the panel often, and a
/// redraw with the same agents, in any order and with other times, is no change. The same for the workflows' rows
/// (<see cref="SessionModel.BackgroundWorkflows"/>), compared by name alone.
/// </summary>
public static class AgentPanel
{
    /// <summary>
    /// The workflows in <paramref name="after"/> whose name is not in <paramref name="before"/> (started) and those in
    /// <paramref name="before"/> whose name is not in <paramref name="after"/> (finished): a workflow's percent, steps
    /// and time change with every redraw, its name does not; two with the same name count as two.
    /// <see cref="BackgroundWorkflowChanges.None"/> when nothing changed.
    /// </summary>
    public static BackgroundWorkflowChanges WorkflowChanges(IReadOnlyList<BackgroundWorkflow> before, IReadOnlyList<BackgroundWorkflow> after)
    {
        if (ReferenceEquals(before, after) || (before.Count == 0 && after.Count == 0))
        {
            return BackgroundWorkflowChanges.None;
        }

        var started = new List<BackgroundWorkflow>(after);
        var finished = new List<BackgroundWorkflow>();
        foreach (var workflow in before)
        {
            var at = 0;
            while (at < started.Count && started[at].Name != workflow.Name)
            {
                at++;
            }

            if (at < started.Count)
            {
                started.RemoveAt(at);
            }
            else
            {
                finished.Add(workflow);
            }
        }

        return started.Count == 0 && finished.Count == 0 ? BackgroundWorkflowChanges.None : new BackgroundWorkflowChanges(started, finished);
    }

    /// <summary>
    /// The agents in <paramref name="after"/> and not in <paramref name="before"/> (started) and those in
    /// <paramref name="before"/> and not in <paramref name="after"/> (finished), compared by type and description;
    /// two agents with the same texts count as two. <see cref="BackgroundAgentChanges.None"/> when nothing changed.
    /// </summary>
    public static BackgroundAgentChanges Changes(IReadOnlyList<BackgroundAgent> before, IReadOnlyList<BackgroundAgent> after)
    {
        if (ReferenceEquals(before, after) || (before.Count == 0 && after.Count == 0))
        {
            return BackgroundAgentChanges.None;
        }

        var started = new List<BackgroundAgent>(after);
        var finished = new List<BackgroundAgent>();
        foreach (var agent in before)
        {
            if (!started.Remove(agent))
            {
                finished.Add(agent);
            }
        }

        return started.Count == 0 && finished.Count == 0 ? BackgroundAgentChanges.None : new BackgroundAgentChanges(started, finished);
    }
}
