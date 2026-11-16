namespace AxClaude.Core.Flow;

/// <summary>
/// The tasks the last stop event listed as running in the background (events-and-grounding.md EG-7), in their
/// simplest form: nothing is listed. The list, a worker's stop that takes its task off it and the
/// <see cref="BackgroundListed"/> decision when it changes are written into these same methods; they decide
/// through <c>Decide</c> of <c>SessionFlow.cs</c>, which calls them at a stop event, at a worker's start and stop,
/// and when the conversation or the run starts afresh.
/// </summary>
public sealed partial class SessionFlow
{
    private static readonly IReadOnlyList<FlowTask> NoTasks = [];

    /// <summary>The tasks listed as running at the last stop, without those whose worker has stopped since (EG-7.1).</summary>
    public IReadOnlyList<FlowTask> Tasks => NoTasks;

    /// <summary>A stop event's list takes the place of the one before.</summary>
    private void BackgroundStop(TurnStopped stop)
    {
    }

    /// <summary>A worker started or stopped: a stop takes its id off the list; a start adds nothing by itself (EG-D-12).</summary>
    private void BackgroundWorker(string id, string type, bool started)
    {
    }

    /// <summary>The conversation or the run started afresh: nothing is listed.</summary>
    private void BackgroundReset()
    {
    }
}
