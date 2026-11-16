namespace AxClaude.Core.Hooks;

/// <summary>
/// What an event of the agent program says happened (docs/axClaude/events-and-grounding.md EG-2.1, section 6.2). Neutral:
/// the program's own event names are its place's (<see cref="ClaudeCodeHooks"/>), which maps them to these.
/// </summary>
public enum HookKind
{
    /// <summary>A session started, resumed, was cleared or compacted: its id, its record's file and why.</summary>
    SessionStarted,

    /// <summary>A turn finished (never an interrupt, never a cut): its last message and the tasks still running.</summary>
    TurnStopped,

    /// <summary>A turn failed (a request the program could not complete).</summary>
    TurnFailed,

    /// <summary>A worker (an agent) started.</summary>
    WorkerStarted,

    /// <summary>A worker (an agent) stopped.</summary>
    WorkerStopped,

    /// <summary>A tool asks the user's permission: a question is coming.</summary>
    PermissionAsked,

    /// <summary>The program asks for the user's attention.</summary>
    Notified,

    /// <summary>The session ended.</summary>
    SessionEnded,
}

/// <summary>
/// One task a turn's end listed as running in the background: its id, its kind (<c>shell</c>, <c>subagent</c> or
/// another word), its status, its description and, for an agent, its worker type. A missing text is empty; the
/// worker type is null when the program gave none. Its <see cref="ToString"/> holds no text of the task's, as the
/// event's holds none of the event's (EG-9.5).
/// </summary>
public sealed record ListedTask(string Id, string Type, string Status, string Description, string? WorkerType)
{
    /// <summary>The task in numbers, safe for a log: the lengths of its texts.</summary>
    public override string ToString() =>
        $"task id={Id.Length} type={Type.Length} status={Status.Length} description={Description.Length} worker={WorkerType?.Length ?? 0}";
}

/// <summary>
/// One event, reduced to what axClaude uses (EG-2.1, section 6.2). Only the fields of its own kind are set; every other
/// field of what the program sent was never read. <see cref="LastReply"/> is held in memory only and never shown,
/// spoken or logged (EG-2.9). An event is a signal, no more trusted than a row on the screen: nothing it carries is
/// run, sent, typed, shown as a question's text or written to a file.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="SessionId">The session the event belongs to.</param>
/// <param name="TurnKey">The turn's key (the prompt's id), when the event names one.</param>
/// <param name="RecordPath">SessionStarted: the session's record file as the program named it, not yet checked (EG-2.9).</param>
/// <param name="Source">SessionStarted: why the session started, the program's own word.</param>
/// <param name="LastReply">TurnStopped: the turn's last message, a key to confirm reply lines with (EG-5.3).</param>
/// <param name="Tasks">TurnStopped: the tasks still running; empty when none, null when the event listed nothing.</param>
/// <param name="WorkerId">WorkerStarted, WorkerStopped: the worker's id.</param>
/// <param name="WorkerType">
/// WorkerStarted, WorkerStopped: the worker's type; null for a helper of the program's own (a side question, a
/// compaction), which is no worker of the user's and is ignored (EG-2.9).
/// </param>
/// <param name="Tool">PermissionAsked: the tool that asks.</param>
/// <param name="Reason">Notified: the kind of notification; SessionEnded: why the session ended. The program's own words.</param>
public sealed record HookEvent(
    HookKind Kind, string SessionId, string? TurnKey,
    string? RecordPath = null, string? Source = null,
    string? LastReply = null, IReadOnlyList<ListedTask>? Tasks = null,
    string? WorkerId = null, string? WorkerType = null,
    string? Tool = null,
    string? Reason = null)
{
    /// <summary>
    /// The event in numbers and fixed words, safe for a log (EG-9.5): its kind, the first eight characters of the
    /// session's id, and for each text it holds only its length. A record would print every field, the last reply
    /// among them (G7).
    /// </summary>
    public override string ToString()
    {
        var text = new System.Text.StringBuilder();
        text.Append(Kind).Append(" session=").Append(SessionId.Length > 8 ? SessionId[..8] : SessionId);
        Length(text, "turn", TurnKey);
        Length(text, "record", RecordPath);
        Length(text, "source", Source);
        Length(text, "last", LastReply);
        if (Tasks is not null)
        {
            text.Append(" tasks=").Append(Tasks.Count);
        }

        Length(text, "worker", WorkerId);
        Length(text, "type", WorkerType);
        Length(text, "tool", Tool);
        Length(text, "reason", Reason);
        return text.ToString();
    }

    private static void Length(System.Text.StringBuilder text, string name, string? value)
    {
        if (value is not null)
        {
            text.Append(' ').Append(name).Append('=').Append(value.Length);
        }
    }
}
