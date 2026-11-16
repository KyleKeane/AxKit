namespace AxClaude.Core.Transcript;

/// <summary>
/// How many things run in the background now (SPEC.md FR-7.10): the shells, the agents and the dynamic workflows, the
/// other kinds (what the mode line counts under another word, and the progress rows of a kind axClaude does not know),
/// and <see cref="Uncounted"/>, one, when Claude Code shows background work and counts none of it.
/// </summary>
public sealed record BackgroundCount(int Shells, int Agents, int Other = 0, int Uncounted = 0, int Workflows = 0)
{
    public static readonly BackgroundCount None = new(0, 0);

    /// <summary>The one number: what the status bar says, what the sounds follow, how many tones the recurring chords add.</summary>
    public int Total => Shells + Agents + Workflows + Other + Uncounted;
}

/// <summary>Which sound a change of the background work gets: one for a change, however many things changed.</summary>
public enum BackgroundSound
{
    None,

    /// <summary>Something arrived: the number rose, or one thing took another's place.</summary>
    Start,

    /// <summary>Something left: the number fell.</summary>
    Finish,
}

/// <summary>
/// The rules of axClaude's signals for background work (SPEC.md FR-7.10, the user's decisions of 2026-11-15): the one
/// number from what Claude Code shows in two places, the words of the status bar, which sound a change gets, the lines
/// spoken when the user asks for them, and the answer to "What runs in the background"; when the recurring chords
/// play is <see cref="BackgroundChords"/>. Pure: the window only wires
/// it to the model, the sounds and the settings. The texts read here are the model's own
/// (<see cref="SessionModel.Background"/>, made by <see cref="LineClassifier.BackgroundText"/>), with Claude Code's
/// two words for a kind from <see cref="ClaudeCodeTexts"/>.
/// </summary>
public static class BackgroundWork
{
    /// <summary>
    /// The one number, from the two places Claude Code shows background work in. The mode line counts shells
    /// ("1 shell") and may count agents and other kinds ("1 shell, 2 agents"); the live block shows one row per agent
    /// and one per dynamic workflow, and the waiting row says how many agents or workflows a finished turn still waits
    /// for. The rule:
    /// <list type="bullet">
    /// <item>shells: what the mode line counts as shells; nothing else knows them;</item>
    /// <item>agents: the largest of the rows shown, the waiting row's number and the mode line's own count of agents,
    /// never their sum, since all three count the same agents;</item>
    /// <item>workflows: the same rule, the largest of the workflows' rows, the waiting row's number for workflows and
    /// a mode line's own count of workflows;</item>
    /// <item>other: what the mode line counts under any other word, each added, since no other place shows them; or,
    /// when larger, <paramref name="unknownShown"/>, the progress rows of a kind axClaude does not know (each one thing),
    /// which may be the same things the mode line counts, so never added to them;</item>
    /// <item>when all of that is zero and the mode line still shows background work (<paramref name="modeLineCounts"/>
    /// without a number, "see /tasks"), one thing, uncounted.</item>
    /// </list>
    /// </summary>
    /// <param name="modeLineCounts"><see cref="SessionModel.Background"/>: "1 shell", "1 shell, 2 agents", "see /tasks" or null.</param>
    /// <param name="agentsShown">The rows of <see cref="SessionModel.BackgroundAgents"/>.</param>
    /// <param name="agentsAwaited"><see cref="SessionModel.BackgroundAgentsAwaited"/>.</param>
    /// <param name="workflowsShown">The rows of <see cref="SessionModel.BackgroundWorkflows"/>.</param>
    /// <param name="workflowsAwaited"><see cref="SessionModel.BackgroundWorkflowsAwaited"/>.</param>
    /// <param name="unknownShown"><see cref="SessionModel.BackgroundUnknown"/>.</param>
    public static BackgroundCount Count(string? modeLineCounts, int agentsShown, int agentsAwaited, int workflowsShown = 0, int workflowsAwaited = 0, int unknownShown = 0)
    {
        var shells = 0;
        var lineAgents = 0;
        var lineWorkflows = 0;
        var other = 0;
        var text = modeLineCounts.AsSpan();
        var counted = false;
        // "1 shell, 2 agents": a number, then the kind's words up to the comma. No allocation: this runs for the status bar.
        while (text.Length > 0)
        {
            var comma = text.IndexOf(',');
            var part = (comma < 0 ? text : text[..comma]).Trim();
            text = comma < 0 ? default : text[(comma + 1)..];
            var digits = 0;
            while (digits < part.Length && char.IsAsciiDigit(part[digits]))
            {
                digits++;
            }

            if (digits == 0 || !int.TryParse(part[..digits], out var number))
            {
                continue;
            }

            counted = true;
            var kind = part[digits..];
            if (kind.Contains(ClaudeCodeTexts.CountedShell, StringComparison.Ordinal))
            {
                shells += number;
            }
            else if (kind.Contains(ClaudeCodeTexts.CountedAgent, StringComparison.Ordinal))
            {
                lineAgents += number;
            }
            else if (kind.Contains(ClaudeCodeTexts.CountedWorkflow, StringComparison.Ordinal))
            {
                lineWorkflows += number;
            }
            else
            {
                other += number;
            }
        }

        var agents = Math.Max(Math.Max(agentsShown, agentsAwaited), lineAgents);
        var workflows = Math.Max(Math.Max(workflowsShown, workflowsAwaited), lineWorkflows);
        other = Math.Max(other, unknownShown);
        var uncounted = modeLineCounts is not null && !counted && shells + agents + workflows + other == 0 ? 1 : 0;
        return shells == 0 && agents == 0 && workflows == 0 && other == 0 && uncounted == 0
            ? BackgroundCount.None
            : new BackgroundCount(shells, agents, other, uncounted, workflows);
    }

    /// <summary>
    /// The status bar's part (FR-10.2): "1 shell in the background" for one kind, "3 in the background: 1 shell,
    /// 2 agents" for several, "1 workflow in the background" for a dynamic workflow, "see /tasks in the background"
    /// when Claude Code shows work it does not count, null for none.
    /// </summary>
    public static string? Status(BackgroundCount count)
    {
        if (count.Total == 0)
        {
            return null;
        }

        if (count.Uncounted > 0)
        {
            return "see /tasks in the background";
        }

        var kinds = Kinds(count);
        return kinds.Count == 1 ? kinds[0] + " in the background" : $"{count.Total} in the background: {string.Join(", ", kinds)}";
    }

    /// <summary>"1 shell", "2 agents", "1 workflow", "1 other task": the kinds that run, in that order.</summary>
    private static List<string> Kinds(BackgroundCount count)
    {
        var kinds = new List<string>(4);
        if (count.Shells > 0)
        {
            kinds.Add(Shells(count.Shells));
        }

        if (count.Agents > 0)
        {
            kinds.Add(Agents(count.Agents));
        }

        if (count.Workflows > 0)
        {
            kinds.Add(Workflows(count.Workflows));
        }

        if (count.Other > 0)
        {
            kinds.Add(Others(count.Other));
        }

        return kinds;
    }

    private static string Shells(int number) => number == 1 ? "1 shell" : $"{number} shells";

    private static string Agents(int number) => number == 1 ? "1 agent" : $"{number} agents";

    private static string Workflows(int number) => number == 1 ? "1 workflow" : $"{number} workflows";

    private static string Others(int number) => number == 1 ? "1 other task" : $"{number} other tasks";

    /// <summary>
    /// The sound of a change (FR-7.10; the user's rule: what arrives rises, what leaves falls): the start sound when
    /// the number rose, the finish sound when it fell, one sound however much it changed; the start sound too when the
    /// number stayed and an agent or a workflow took another's place; none for a redraw.
    /// </summary>
    public static BackgroundSound Sound(BackgroundCount before, BackgroundCount after, BackgroundAgentChanges changes, BackgroundWorkflowChanges? workflows = null) =>
        after.Total > before.Total ? BackgroundSound.Start
        : after.Total < before.Total ? BackgroundSound.Finish
        : changes.Started.Count > 0 || workflows is { Started.Count: > 0 } ? BackgroundSound.Start
        : BackgroundSound.None;

    /// <summary>
    /// What is said of a change when the user turned Say background work as it starts and ends on: one short line for
    /// each agent ("general-purpose started: Checking callers", "Checking callers finished") and each workflow
    /// ("Workflow axkit-design-map-and-verify started", "… finished"), the agents and the workflows the rows did not
    /// name by their number ("1 agent started", "1 workflow finished"), then the shells and the other kinds by theirs
    /// ("1 shell started", "2 shells finished"). Empty for a redraw.
    /// </summary>
    public static IReadOnlyList<string> ChangeLines(BackgroundCount before, BackgroundCount after, BackgroundAgentChanges changes, BackgroundWorkflowChanges? workflows = null)
    {
        workflows ??= BackgroundWorkflowChanges.None;
        var lines = new List<string>();
        foreach (var agent in changes.Started)
        {
            lines.Add(agent.Description.Length > 0 ? $"{agent.Type} started: {agent.Description}" : $"{agent.Type} started");
        }

        foreach (var agent in changes.Finished)
        {
            lines.Add($"{(agent.Description.Length > 0 ? agent.Description : agent.Type)} finished");
        }

        foreach (var workflow in workflows.Started)
        {
            lines.Add($"Workflow {workflow.Name} started");
        }

        foreach (var workflow in workflows.Finished)
        {
            lines.Add($"Workflow {workflow.Name} finished");
        }

        // The agents counted and not named: the waiting row or the mode line knew of more or fewer than the rows showed.
        AddChange(lines, after.Agents - before.Agents - (changes.Started.Count - changes.Finished.Count), Agents);
        AddChange(lines, after.Workflows - before.Workflows - (workflows.Started.Count - workflows.Finished.Count), Workflows);
        AddChange(lines, after.Shells - before.Shells, Shells);
        AddChange(lines, after.Other - before.Other, Others);
        if (after.Uncounted != before.Uncounted && lines.Count == 0)
        {
            lines.Add(after.Uncounted > 0 ? "Background work started" : "Background work finished");
        }

        return lines;
    }

    private static void AddChange(List<string> lines, int difference, Func<int, string> words)
    {
        if (difference > 0)
        {
            lines.Add(words(difference) + " started");
        }
        else if (difference < 0)
        {
            lines.Add(words(-difference) + " finished");
        }
    }

    /// <summary>What says that nothing runs, for "What runs in the background".</summary>
    public const string Nothing = "Nothing runs in the background";

    /// <summary>
    /// The answer to "What runs in the background" (FR-7.10, on demand): the number and its kinds as the status bar
    /// says them, then each agent's type and description, then the agents counted and not named, then each workflow
    /// with how far it is ("Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s"), then the
    /// workflows counted and not named.
    /// </summary>
    public static string Said(BackgroundCount count, IReadOnlyList<BackgroundAgent> agents, IReadOnlyList<BackgroundWorkflow>? workflows = null)
    {
        workflows ??= [];
        if (count.Total == 0)
        {
            return Nothing;
        }

        if (count.Uncounted > 0)
        {
            return "Claude Code shows work in the background and does not count it. The command /tasks lists it";
        }

        var parts = new List<string> { Status(count)! };
        foreach (var agent in agents)
        {
            parts.Add(agent.Description.Length > 0 ? $"{agent.Type}: {agent.Description}" : agent.Type);
        }

        var unnamed = count.Agents - agents.Count;
        if (unnamed > 0 && agents.Count > 0)
        {
            parts.Add(unnamed == 1 ? "1 more agent, not named" : $"{unnamed} more agents, not named");
        }

        foreach (var workflow in workflows)
        {
            parts.Add($"Workflow {workflow.Name}: {workflow.Percent} percent, {workflow.Done} of {workflow.Total} done, {workflow.Elapsed}");
        }

        var unnamedWorkflows = count.Workflows - workflows.Count;
        if (unnamedWorkflows > 0 && workflows.Count > 0)
        {
            parts.Add(unnamedWorkflows == 1 ? "1 more workflow, not named" : $"{unnamedWorkflows} more workflows, not named");
        }

        return string.Join(". ", parts);
    }
}
