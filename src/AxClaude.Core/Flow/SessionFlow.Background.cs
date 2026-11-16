namespace AxClaude.Core.Flow;

/// <summary>
/// The tasks the last stop event listed as running in the background (events-and-grounding.md EG-7.1): the evidence
/// of background work that the events give, one more place beside the rows and the mode line, which stay
/// (<c>BackgroundWork.Count</c>, EG-7.2). A stop event's list takes the place of the one before; a worker's stop takes
/// its id off the list; a worker's start is counted and adds nothing by itself (EG-D-12: the inner agents of a
/// dynamic workflow were not measured with events, and fifty of them must not ping fifty times). Whenever the list
/// changes the core says so with <see cref="BackgroundListed"/>, and only then: a stop that lists what is listed
/// already decides nothing. <c>SessionFlow.cs</c> calls these methods at a stop event, at a worker's start and stop,
/// and, before the generation rises, when the conversation or the run starts afresh. Every member added here starts
/// with the file's word, as the gate's start with Gate.
/// </summary>
public sealed partial class SessionFlow
{
    /// <summary>
    /// A listed task's type for a shell started in the background, as the core's records carry it (EG-7.1). A program
    /// under the window maps its own word to this one; Claude Code's Stop event uses the same word.
    /// </summary>
    public const string BackgroundShellType = "shell";

    /// <summary>A listed task's type for an agent (a worker) running in the background, as <see cref="BackgroundShellType"/>.</summary>
    public const string BackgroundAgentType = "subagent";

    private static readonly FlowTask[] BackgroundNone = [];

    /// <summary>
    /// The list as last told. Never changed in place: a change makes a new array, so that the one a
    /// <see cref="BackgroundListed"/> decision carries stays as it was told, also after the next call.
    /// </summary>
    private FlowTask[] _backgroundTasks = BackgroundNone;

    /// <summary>
    /// The ids of the workers that stopped in this run. Events reach the window each through a process of its own, so a
    /// stop event made just before a worker's stop can arrive after it, and the worker's own stop event lists it still
    /// running; a worker that stopped is never taken back onto the list. Ids are not used again, so this hides nothing
    /// that runs.
    /// </summary>
    private readonly HashSet<string> _backgroundStopped = new(StringComparer.Ordinal);

    /// <summary>The tasks listed as running at the last stop, without those whose worker has stopped since (EG-7.1).</summary>
    public IReadOnlyList<FlowTask> Tasks => _backgroundTasks;

    /// <summary>How many workers an event said had started, since this core was made: for the diagnostics alone (EG-7.1).</summary>
    public int BackgroundWorkersStarted { get; private set; }

    /// <summary>A stop event's list takes the place of the one before.</summary>
    private void BackgroundStop(TurnStopped stop)
    {
        var listed = new List<FlowTask>(stop.Tasks.Count);
        foreach (var task in stop.Tasks)
        {
            if (!_backgroundStopped.Contains(task.Id))
            {
                listed.Add(task);
            }
        }

        if (listed.SequenceEqual(_backgroundTasks))
        {
            return;
        }

        BackgroundTell(listed.Count == 0 ? BackgroundNone : [.. listed]);
    }

    /// <summary>A worker started or stopped: a stop takes its id off the list; a start adds nothing by itself (EG-D-12).</summary>
    private void BackgroundWorker(string id, string type, bool started)
    {
        if (started)
        {
            BackgroundWorkersStarted++;
            return;
        }

        _backgroundStopped.Add(id);
        var index = -1;
        for (var i = 0; i < _backgroundTasks.Length; i++)
        {
            if (string.Equals(_backgroundTasks[i].Id, id, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        var rest = new FlowTask[_backgroundTasks.Length - 1];
        Array.Copy(_backgroundTasks, 0, rest, 0, index);
        Array.Copy(_backgroundTasks, index + 1, rest, index, rest.Length - index);
        BackgroundTell(rest.Length == 0 ? BackgroundNone : rest);
    }

    /// <summary>The conversation or the run started afresh: nothing is listed, and the stopped workers are forgotten.</summary>
    private void BackgroundReset()
    {
        _backgroundStopped.Clear();
        if (_backgroundTasks.Length > 0)
        {
            BackgroundTell(BackgroundNone);
        }
    }

    /// <summary>The list changed: kept, and told to the window with the generation of the moment.</summary>
    private void BackgroundTell(FlowTask[] tasks)
    {
        _backgroundTasks = tasks;
        Decide(new BackgroundListed(_generation, tasks));
    }
}
