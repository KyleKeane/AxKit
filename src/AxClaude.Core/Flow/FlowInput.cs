using AxClaude.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>A mark of a turn in the console stream: OSC 133 with the letter A (a turn starts), C or D (a turn ends).</summary>
public enum TurnMark
{
    Start,
    Output,
    End,
}

/// <summary>What the last frame showed of the program's work: nothing readable, the idle prompt, or a sign of work.</summary>
public enum WorkSeen
{
    Unknown,
    Idle,
    Working,
}

/// <summary>What the user did that he has moved on by: each raises the generation (EG-4.7).</summary>
public enum UserAction
{
    Sent,
    Answered,
    Escaped,
    SentNow,
    ScreenKey,
    PutAway,
}

/// <summary>Why a session was named: a start, a conversation taken up again, a new one in the same run, a compaction.</summary>
public enum SessionSource
{
    Startup,
    Resume,
    Clear,
    Compact,
    Fork,
    Other,
}

/// <summary>What asked for the user's attention through an event: a permission request, or a notification.</summary>
public enum AttentionKind
{
    Permission,
    Notification,
}

/// <summary>What an entry of the record is, as far as the core minds.</summary>
public enum EntryKind
{
    Prompt,
    ReplyText,
    ToolCall,
    ToolResult,
    Interrupt,
    TurnEnd,
    Compact,
    Enqueued,
    Dequeued,
    Other,
}

/// <summary>
/// One thing that happened, as the core takes it (events-and-grounding.md EG-4.2, section 6.4): plain records in
/// arrival order, none of which names the program under the window. Some carry letters of the conversation (a sent
/// message's key, a reply's key), which stay in memory: an input prints as its type's name and nothing else, so
/// that one written to a log by mistake gives nothing away (G7).
/// </summary>
public abstract record FlowInput
{
    public sealed override string ToString() => GetType().Name;
}

/// <summary>The program was started: a run begins, and nothing is known of it yet.</summary>
public sealed record ProcessStarted : FlowInput;

/// <summary>The program ended or was stopped: everything of the run is over.</summary>
public sealed record ProcessEnded : FlowInput;

/// <summary>A mark arrived in the console stream.</summary>
public sealed record MarkSeen(TurnMark Mark) : FlowInput;

/// <summary>The bell arrived in the console stream.</summary>
public sealed record BellSeen : FlowInput;

/// <summary>
/// A frame of the screen ended: the model's epoch, what the screen shows of work, whether the program asks, whether
/// the frame added a visible line to the conversation, whether replies are spoken, how many waiting messages the
/// screen shows, and the reply lines the screen proposes for speech (the list is the pass's own: read at once).
/// </summary>
public sealed record FrameSeen(int Epoch, WorkSeen Work, bool Asking, bool LineAdded, bool SpeechOn, int WaitingShown, IReadOnlyList<Proposed> Proposed) : FlowInput;

/// <summary>The user acted. <paramref name="SentKey"/>: the letters of a sent message.</summary>
public sealed record UserActed(UserAction Action, string? SentKey = null) : FlowInput;

/// <summary>A question or a screen of the program's was shown in the control area.</summary>
public sealed record QuestionShown : FlowInput;

/// <summary>An event named the session: its id, its record's file when the event's path may be used, and why.</summary>
public sealed record SessionNamed(string SessionId, string? RecordPath, SessionSource Source) : FlowInput;

/// <summary>An event said a turn stopped: the key of its last message, and the tasks it left running.</summary>
public sealed record TurnStopped(string? LastReplyKey, IReadOnlyList<FlowTask> Tasks) : FlowInput;

/// <summary>An event said a turn failed.</summary>
public sealed record TurnFailed : FlowInput;

/// <summary>An event said a worker (an agent) started.</summary>
public sealed record WorkerStarted(string Id, string Type) : FlowInput;

/// <summary>An event said a worker stopped.</summary>
public sealed record WorkerStopped(string Id, string Type) : FlowInput;

/// <summary>An event asked for attention.</summary>
public sealed record AttentionAsked(AttentionKind Kind) : FlowInput;

/// <summary>An event said the session ended.</summary>
public sealed record SessionEnded(string Reason) : FlowInput;

/// <summary>
/// An entry of the record was read. <paramref name="Key"/>: the letters of a prompt, the screen key of a reply.
/// <paramref name="SelfStarted"/>: a prompt the program started by itself. <paramref name="Cut"/>: a reply cut short.
/// </summary>
public sealed record EntryRead(EntryKind Kind, string Id, string? Key = null, bool SelfStarted = false, bool Cut = false) : FlowInput;

/// <summary>The window's reader follows the session's record now: from here on there is a record.</summary>
public sealed record RecordFound : FlowInput;

/// <summary>The reader stopped or lost its file: from here on there is no record.</summary>
public sealed record RecordLost : FlowInput;

/// <summary>The time asked for with <see cref="WakeAt"/> has come, or any later moment.</summary>
public sealed record Tick : FlowInput;

/// <summary>One task an event listed as running in the background.</summary>
public sealed record FlowTask(string Id, string Type, string Description, string? WorkerType);
