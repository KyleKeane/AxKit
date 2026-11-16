using AxClaude.Core.Flow;

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
/// number from what Claude Code shows in two places and, with events, the tasks the core keeps from the last stop
/// event (<see cref="SessionFlow.Tasks"/>, events-and-grounding.md EG-7), the words of the status bar, which sound a
/// change gets, the lines spoken when the user asks for them, and the answer to "What runs in the background"; when
/// the recurring chords play is <see cref="BackgroundChords"/>. Pure: the window only wires
/// it to the model, the core, the sounds and the settings. The texts read here are the model's own
/// (<see cref="SessionModel.Background"/>, made by <see cref="LineClassifier.BackgroundText"/>), with Claude Code's
/// two words for a kind from <see cref="ClaudeCodeTexts"/>, and the listed tasks' types are the core's words.
/// </summary>
public static class BackgroundWork
{
    /// <summary>
    /// The one number, from the places background work is shown in: two of Claude Code's screen, and the tasks its
    /// last stop event listed (events-and-grounding.md EG-7.2). The mode line counts shells ("1 shell") and may count
    /// agents and other kinds ("1 shell, 2 agents"); the live block shows one row per agent and one per dynamic
    /// workflow, and the waiting row says how many agents or workflows a finished turn still waits for; the stop
    /// event lists the shells, the agents and the tasks of other types that run when a turn ends. The rule:
    /// <list type="bullet">
    /// <item>shells: what the mode line counts as shells once a mode line was read after the last stop event; before
    /// that, the listed shells, which are newer than any mode line read before the event; nothing else knows them;</item>
    /// <item>agents: the largest of the rows shown, the waiting row's number, the mode line's own count of agents and
    /// the listed agents, never their sum, since all four count the same agents;</item>
    /// <item>workflows: the same rule, the largest of the workflows' rows, the waiting row's number for workflows and
    /// a mode line's own count of workflows;</item>
    /// <item>other: what the mode line counts under any other word, each added, since no other place shows them; or,
    /// when larger, <paramref name="unknownShown"/>, the progress rows of a kind axClaude does not know (each one thing),
    /// or <paramref name="listedOther"/>, the listed tasks of other types, which may be the same things the mode line
    /// counts, so never added to them;</item>
    /// <item>when all of that is zero and the mode line still shows background work (<paramref name="modeLineCounts"/>
    /// without a number, "see /tasks"), one thing, uncounted.</item>
    /// </list>
    /// Without events the listed numbers are zero and a mode line counts as read after the stop: the number is the
    /// screen's, exactly (EG-7.5).
    /// </summary>
    /// <param name="modeLineCounts"><see cref="SessionModel.Background"/>: "1 shell", "1 shell, 2 agents", "see /tasks" or null.</param>
    /// <param name="agentsShown">The rows of <see cref="SessionModel.BackgroundAgents"/>.</param>
    /// <param name="agentsAwaited"><see cref="SessionModel.BackgroundAgentsAwaited"/>.</param>
    /// <param name="workflowsShown">The rows of <see cref="SessionModel.BackgroundWorkflows"/>.</param>
    /// <param name="workflowsAwaited"><see cref="SessionModel.BackgroundWorkflowsAwaited"/>.</param>
    /// <param name="unknownShown"><see cref="SessionModel.BackgroundUnknown"/>.</param>
    /// <param name="listedAgents">The listed tasks that are agents (<see cref="ListedKinds"/> of <see cref="SessionFlow.Tasks"/>).</param>
    /// <param name="listedShells">The listed tasks that are shells.</param>
    /// <param name="listedOther">The listed tasks of any other type.</param>
    /// <param name="modeLineAfterStop">A mode line was read after the last stop event, or no stop event came.</param>
    public static BackgroundCount Count(string? modeLineCounts, int agentsShown, int agentsAwaited, int workflowsShown = 0, int workflowsAwaited = 0, int unknownShown = 0,
        int listedAgents = 0, int listedShells = 0, int listedOther = 0, bool modeLineAfterStop = true)
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

        if (!modeLineAfterStop)
        {
            shells = listedShells;
        }

        var agents = Math.Max(Math.Max(agentsShown, agentsAwaited), Math.Max(lineAgents, listedAgents));
        var workflows = Math.Max(Math.Max(workflowsShown, workflowsAwaited), lineWorkflows);
        other = Math.Max(Math.Max(other, unknownShown), listedOther);
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
    /// says them, then each listed shell by its description ("Shell: Run ping in background", EG-7.3), then each
    /// agent's type and description, then each listed agent the rows do not name, then the agents counted and not
    /// named, then each workflow with how far it is ("Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done,
    /// 20m18s"), then the workflows counted and not named, then each listed task of another type ("Other task: …").
    /// The listed tasks of a kind are named only while they and the rows of that kind are no more than its count:
    /// when more are listed, a mode line or the rows read after the stop event show that some have ended, and which
    /// ones is not known, so the number stands alone.
    /// </summary>
    /// <param name="listed"><see cref="SessionFlow.Tasks"/>: the tasks the last stop event listed; null or empty without events.</param>
    public static string Said(BackgroundCount count, IReadOnlyList<BackgroundAgent> agents, IReadOnlyList<BackgroundWorkflow>? workflows = null, IReadOnlyList<FlowTask>? listed = null)
    {
        workflows ??= [];
        listed ??= [];
        if (count.Total == 0)
        {
            return Nothing;
        }

        if (count.Uncounted > 0)
        {
            return "Claude Code shows work in the background and does not count it. The command /tasks lists it";
        }

        var parts = new List<string> { Status(count)! };
        var shells = NotOnTheRows(listed, ListedKind.Shell, [], count.Shells);
        foreach (var task in shells)
        {
            parts.Add(Named("Shell", task.Description));
        }

        More(parts, count.Shells, shells.Count, "1 more shell", "more shells");
        foreach (var agent in agents)
        {
            parts.Add(agent.Description.Length > 0 ? $"{agent.Type}: {agent.Description}" : agent.Type);
        }

        var listedAgents = NotOnTheRows(listed, ListedKind.Agent, agents, count.Agents);
        foreach (var task in listedAgents)
        {
            parts.Add(Named(string.IsNullOrEmpty(task.WorkerType) ? "Agent" : task.WorkerType, task.Description));
        }

        More(parts, count.Agents, agents.Count + listedAgents.Count, "1 more agent", "more agents");
        foreach (var workflow in workflows)
        {
            parts.Add($"Workflow {workflow.Name}: {workflow.Percent} percent, {workflow.Done} of {workflow.Total} done, {workflow.Elapsed}");
        }

        More(parts, count.Workflows, workflows.Count, "1 more workflow", "more workflows");
        var others = NotOnTheRows(listed, ListedKind.Other, [], count.Other);
        foreach (var task in others)
        {
            parts.Add(Named("Other task", task.Description));
        }

        More(parts, count.Other, others.Count, "1 more other task", "more other tasks");
        return string.Join(". ", parts);
    }

    /// <summary>"1 more agent, not named": said when some of a kind were named and fewer than its count.</summary>
    private static void More(List<string> parts, int counted, int named, string one, string many)
    {
        var unnamed = counted - named;
        if (unnamed > 0 && named > 0)
        {
            parts.Add(unnamed == 1 ? one + ", not named" : $"{unnamed} {many}, not named");
        }
    }

    /// <summary>"Shell: Run ping in background", or the word alone for a task without a description.</summary>
    private static string Named(string word, string description) => description.Length > 0 ? $"{word}: {description}" : word;

    /// <summary>What a listed task is, as the count and the answer group it.</summary>
    private enum ListedKind
    {
        Shell,
        Agent,
        Other,
    }

    private static ListedKind KindOf(FlowTask task) =>
        string.Equals(task.Type, SessionFlow.BackgroundShellType, StringComparison.Ordinal) ? ListedKind.Shell
        : string.Equals(task.Type, SessionFlow.BackgroundAgentType, StringComparison.Ordinal) ? ListedKind.Agent
        : ListedKind.Other;

    /// <summary>
    /// How many of the tasks the last stop event listed are agents, shells and of other types: the listed numbers of
    /// <see cref="Count"/>. The types are the core's words (<see cref="SessionFlow.BackgroundShellType"/>,
    /// <see cref="SessionFlow.BackgroundAgentType"/>); any other word is another type.
    /// </summary>
    public static (int Agents, int Shells, int Other) ListedKinds(IReadOnlyList<FlowTask> listed)
    {
        int agents = 0, shells = 0, other = 0;
        foreach (var task in listed)
        {
            switch (KindOf(task))
            {
                case ListedKind.Shell:
                    shells++;
                    break;
                case ListedKind.Agent:
                    agents++;
                    break;
                default:
                    other++;
                    break;
            }
        }

        return (agents, shells, other);
    }

    /// <summary>
    /// The listed tasks of one kind that no row names, in the list's order: an agent is named by a row of the same
    /// description, and of the same type when the task names its worker's type. Empty when they and the rows are
    /// more than <paramref name="counted"/>: the list is then older than what the screen shows of that kind.
    /// </summary>
    private static List<FlowTask> NotOnTheRows(IReadOnlyList<FlowTask> listed, ListedKind kind, IReadOnlyList<BackgroundAgent> rows, int counted)
    {
        var tasks = new List<FlowTask>();
        foreach (var task in listed)
        {
            if (KindOf(task) == kind && !OnTheRows(task, rows))
            {
                tasks.Add(task);
            }
        }

        if (rows.Count + tasks.Count > counted)
        {
            tasks.Clear();
        }

        return tasks;
    }

    private static bool OnTheRows(FlowTask task, IReadOnlyList<BackgroundAgent> rows)
    {
        foreach (var row in rows)
        {
            if (string.Equals(row.Description, task.Description, StringComparison.Ordinal)
                && (string.IsNullOrEmpty(task.WorkerType) || string.Equals(row.Type, task.WorkerType, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }
}
