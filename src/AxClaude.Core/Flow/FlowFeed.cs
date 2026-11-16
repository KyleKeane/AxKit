using AxClaude.Core.Hooks;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>
/// What turns the sources' own records into the core's inputs (events-and-grounding.md EG-4.2, section 6.4): a frame
/// of the screen's model, a mark's letter, an event of a hook, an entry of the record. One static method for each,
/// so that the window and the replayer of the timeline fixtures feed the core through the same code (EG-11.2). It
/// holds no word of the program under the window: the program's words are mapped in its own places
/// (<see cref="ClaudeCodeHooks.SourceOf"/>, <see cref="ClaudeCodeHooks.IsRunning"/>,
/// <see cref="ClaudeCodeTranscript.ScreenKey"/>), which a second program would supply in its own.
/// </summary>
public static class FlowFeed
{
    private static readonly IReadOnlyList<Proposed> NoProposals = [];
    private static readonly IReadOnlyList<FlowTask> NoTasks = [];

    /// <summary>
    /// A frame of the model as the core takes it: the model's epoch, what the screen shows of work, how many waiting
    /// messages its hint rows show, and of the pass over the conversation whether a visible line was added and the
    /// lines proposed for speech. <paramref name="asking"/>: a question or a screen of the program's is shown, which
    /// the core takes as not idle. <paramref name="speechOn"/>: replies are spoken. The proposals are copied: the
    /// pass reuses its list at the next frame, and an input can wait in the window's queue past that (EG-4.8).
    /// </summary>
    public static FrameSeen FromFrame(SessionModel model, Arrival arrival, bool asking, bool speechOn) =>
        new(model.Epoch, WorkOf(model.Work), asking, arrival.NewLine, speechOn, model.QueuedMessages,
            arrival.Proposed.Count == 0 ? NoProposals : [.. arrival.Proposed]);

    /// <summary>The mark of a turn for the letter of an OSC 133 sequence: A, C or D; null for any other letter.</summary>
    public static MarkSeen? FromMark(char letter) => letter switch
    {
        'A' => new MarkSeen(TurnMark.Start),
        'C' => new MarkSeen(TurnMark.Output),
        'D' => new MarkSeen(TurnMark.End),
        _ => null,
    };

    /// <summary>
    /// An event of a hook as the core takes it. A stopped turn carries the key of its last message (null when it had
    /// none, or one without a letter) and of the tasks it listed those that still run; an event that listed no tasks
    /// at all gives an empty list, as one that listed none running does. A worker without a type is a helper of the
    /// program's own (a side question, a compaction) and no input (EG-2.9). The path of a named session's record is
    /// passed on as the event has it: the window checks it before it calls here (EG-2.9).
    /// </summary>
    public static FlowInput? FromHook(HookEvent e) => e.Kind switch
    {
        HookKind.SessionStarted => new SessionNamed(e.SessionId, e.RecordPath, ClaudeCodeHooks.SourceOf(e.Source)),
        HookKind.TurnStopped => new TurnStopped(KeyOf(e.LastReply), Running(e.Tasks)),
        HookKind.TurnFailed => new TurnFailed(),
        HookKind.WorkerStarted => e.WorkerType is { } type ? new WorkerStarted(e.WorkerId ?? string.Empty, type) : null,
        HookKind.WorkerStopped => e.WorkerType is { } type ? new WorkerStopped(e.WorkerId ?? string.Empty, type) : null,
        HookKind.PermissionAsked => new AttentionAsked(AttentionKind.Permission),
        HookKind.Notified => new AttentionAsked(AttentionKind.Notification),
        HookKind.SessionEnded => new SessionEnded(e.Reason ?? string.Empty),
        _ => null,
    };

    /// <summary>
    /// An entry of the record as the core takes it. A prompt's key is the letters of its text (none for a prompt the
    /// program started by itself, which has no text); a reply's key is its screen key; a queued message's key is the
    /// letters of its text. A message taken out of the queue unsent counts as one taken up, since either way it
    /// waits no longer. A note, a thinking block and a compaction's summary are entries of no kind the core minds.
    /// </summary>
    public static FlowInput? FromEntry(RecordEntry e) => e.Kind switch
    {
        RecordKind.Prompt => new EntryRead(EntryKind.Prompt, e.Id, Letters(e.Text), SelfStarted: e.Origin == PromptOrigin.SelfStarted),
        RecordKind.ReplyText => new EntryRead(EntryKind.ReplyText, e.Id, e.Text is { } source ? ClaudeCodeTranscript.ScreenKey(source) : null, Cut: e.Cut),
        RecordKind.ToolCall => new EntryRead(EntryKind.ToolCall, e.Id),
        RecordKind.ToolResult => new EntryRead(EntryKind.ToolResult, e.Id),
        RecordKind.Interrupt => new EntryRead(EntryKind.Interrupt, e.Id),
        RecordKind.TurnEnd => new EntryRead(EntryKind.TurnEnd, e.Id),
        RecordKind.CompactBoundary => new EntryRead(EntryKind.Compact, e.Id),
        RecordKind.Enqueued => new EntryRead(EntryKind.Enqueued, e.Id, Letters(e.Text)),
        RecordKind.Dequeued or RecordKind.Removed => new EntryRead(EntryKind.Dequeued, e.Id),
        _ => new EntryRead(EntryKind.Other, e.Id),
    };

    /// <summary>The letters of a message this window sent, as a prompt's key is made: what the window notes of a send, to find the session's file by (EG-3.2).</summary>
    public static string SentKey(string text) => RecordText.Letters(text);

    private static WorkSeen WorkOf(ScreenWork work) => work switch
    {
        ScreenWork.Working => WorkSeen.Working,
        ScreenWork.Idle => WorkSeen.Idle,
        _ => WorkSeen.Unknown,
    };

    private static string? Letters(string? text) => text is null ? null : RecordText.Letters(text);

    /// <summary>The screen key of a turn's last message; null for no message and for one whose key is empty, which would confirm nothing.</summary>
    private static string? KeyOf(string? lastReply)
    {
        if (string.IsNullOrEmpty(lastReply))
        {
            return null;
        }

        var key = ClaudeCodeTranscript.ScreenKey(lastReply);
        return key.Length == 0 ? null : key;
    }

    private static IReadOnlyList<FlowTask> Running(IReadOnlyList<ListedTask>? listed)
    {
        if (listed is null || listed.Count == 0)
        {
            return NoTasks;
        }

        var running = new List<FlowTask>(listed.Count);
        foreach (var task in listed)
        {
            if (ClaudeCodeHooks.IsRunning(task))
            {
                running.Add(new FlowTask(task.Id, task.Type, task.Description, task.WorkerType));
            }
        }

        return running;
    }
}
