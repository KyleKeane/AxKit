namespace AxClaude.Core.Record;

/// <summary>
/// What an entry of an agent program's record is, reduced to what axClaude uses (docs/axClaude/events-and-grounding.md,
/// "EG" below, section 6.3). Neutral: a program's own reader (for Claude Code, <c>ClaudeCodeTranscript</c>) maps its
/// file to these kinds, and everything after it reads only them.
/// </summary>
public enum RecordKind
{
    /// <summary>A message that opens a turn: typed, queued, or one the program started by itself; with its text when the user wrote it.</summary>
    Prompt,

    /// <summary>A command, a shell line or a notification the program wrote in the user's place: no prompt, no text.</summary>
    Note,

    /// <summary>The program's own summary of the conversation after a compaction: never a prompt, no text.</summary>
    CompactSummary,

    /// <summary>A tool's result: the call it answers and whether it failed, never its text.</summary>
    ToolResult,

    /// <summary>The user interrupted the turn.</summary>
    Interrupt,

    /// <summary>One block of text of a reply, with its source as the model wrote it.</summary>
    ReplyText,

    /// <summary>One tool call: its id and the tool's name, never its input.</summary>
    ToolCall,

    /// <summary>A thinking block: nothing of it is read.</summary>
    Thinking,

    /// <summary>The turn ended, with its duration.</summary>
    TurnEnd,

    /// <summary>A compaction began a new part of the record.</summary>
    CompactBoundary,

    /// <summary>A message the user sent while a turn ran was put in the queue, with its text.</summary>
    Enqueued,

    /// <summary>A queued message was taken up.</summary>
    Dequeued,

    /// <summary>A queued message was taken out of the queue unsent.</summary>
    Removed,

    /// <summary>An entry of a kind read whose block, subtype or operation is not known, or an agent's own entry.</summary>
    Other,
}

/// <summary>Who started a prompt.</summary>
public enum PromptOrigin
{
    /// <summary>Not said, or not a prompt.</summary>
    None,

    /// <summary>The user typed it at the prompt.</summary>
    Typed,

    /// <summary>The user sent it while a turn ran, and it waited in the queue.</summary>
    Queued,

    /// <summary>The program started the turn by itself (a background task or agent that finished).</summary>
    SelfStarted,
}

/// <summary>
/// What a program's reader made of one line of its record, for the counters of the reader (EG-3.4 to EG-3.6): an
/// entry read, a line of a kind not read, a line that is no JSON object, or an entry of a kind read that lacked what
/// the table needs (no uuid, no message, no content, no operation), which is still returned, as Other or with a local
/// id, and counted against the reader (EG-3.6).
/// </summary>
public enum RecordLine
{
    /// <summary>An entry of a kind read, in a shape known: returned.</summary>
    Read,

    /// <summary>A line of a kind not read, or with no type: nothing returned and nothing of it made into a string.</summary>
    Skipped,

    /// <summary>Not one JSON object: nothing returned.</summary>
    NotJson,

    /// <summary>An entry of a kind read that lacked what the table needs: returned, and counted (EG-3.6).</summary>
    Unknown,
}

/// <summary>
/// One entry of the session file, reduced to what axClaude uses (EG-3.4, EG-6.3). Text is held in memory only: it is
/// never written to a log, a report, the diagnostics or a file but the export the user asks for (EG-10.3).
/// <see cref="Id"/> is the entry's uuid, or a local id made from the line's offset when it has none (EG-3.5).
/// </summary>
public sealed record RecordEntry(
    RecordKind Kind, string Id,
    string? TurnKey = null,          // promptId
    string? MessageId = null,        // message.id of an assistant entry
    string? Text = null,             // a Prompt's text, a ReplyText's source, an Enqueued's content
    string? ToolName = null, string? ToolUseId = null, bool IsError = false,
    bool Cut = false,                // isAbortedMidStream
    PromptOrigin Origin = PromptOrigin.None,
    DateTime? Time = null, long DurationMs = 0, bool Sidechain = false);
