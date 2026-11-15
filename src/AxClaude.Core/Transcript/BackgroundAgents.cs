namespace AxClaude.Core.Transcript;

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
/// The rule of what changed in the panel of running agents (<see cref="SessionModel.BackgroundAgents"/>) from one
/// reading to the next, for a window that says so only on a change: Claude Code redraws the panel often, and a
/// redraw with the same agents, in any order and with other times, is no change.
/// </summary>
public static class AgentPanel
{
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
