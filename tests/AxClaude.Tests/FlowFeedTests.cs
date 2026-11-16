using System.Text;
using AxClaude.Core.Flow;
using AxClaude.Core.Hooks;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// What turns the sources' records into the core's inputs (docs/axClaude/events-and-grounding.md EG-4.2, section 6.4;
/// <see cref="FlowFeed"/>): a frame of the model, a mark's letter, an event of a hook, an entry of the record. The
/// window and the replayer of the timelines feed the core through these same four methods.
/// </summary>
public class FlowFeedTests
{
    private const string Busy = "auto mode on (shift+tab to cycle)  ·  esc to interrupt";
    private const string Idle = "auto mode on (shift+tab to cycle)";

    [Fact]
    public void A_frame_carries_the_models_work_epoch_and_proposals()
    {
        var model = new SessionModel(80, 12);
        var arrivals = new Arrivals();
        Feed(model, $"{Esc}[?25l${Esc}[?25h");

        // Before a mode line was read the screen says nothing of Claude's work.
        var frame = FlowFeed.FromFrame(model, arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false), asking: false, speechOn: true);
        Assert.Equal(WorkSeen.Unknown, frame.Work);
        Assert.Equal(model.Epoch, frame.Epoch);
        Assert.Empty(frame.Proposed);

        // Claude works on a first message, and a second one waits under its hint row: the frame says working and
        // one message waiting, and proposes nothing, since the reply's row may still grow.
        model.Send("first");
        Feed(model, $"{Esc}[?25l{Esc}[1;1Hyou: first{Esc}[K\r\nclaude: Working on it.{Esc}[K\r\nRunning…{Esc}[K\r\n{Busy}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        model.Send("second");
        Feed(model, $"{Esc}[?25l{Esc}[3;1Hyou: second{Esc}[K\r\nctrl+x ctrl+s to send now{Esc}[K\r\nRunning…{Esc}[K\r\n{Busy}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        var arrival = arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false);
        frame = FlowFeed.FromFrame(model, arrival, asking: true, speechOn: false);
        Assert.Equal(WorkSeen.Working, frame.Work);
        Assert.Equal(1, frame.WaitingShown);
        Assert.True(frame.LineAdded);
        Assert.True(frame.Asking);
        Assert.False(frame.SpeechOn);
        Assert.Empty(frame.Proposed);

        // The turn ends and the waiting message is taken up: the screen is idle, nothing waits, and the reply lines
        // that became final are proposed, each with its head and its kind, as the pass over the conversation gave them.
        Feed(model, $"{Esc}[?25l{Esc}[3;1HDone.{Esc}[K\r\nyou: second{Esc}[K\r\nclaude: Second answer.{Esc}[K\r\nIt has two rows.{Esc}[K\r\n{Idle}{Esc}[K\r\n${Esc}[K{Esc}[?25h");
        arrival = arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false);
        var proposed = arrival.Proposed.Select(line => (line.Line.Id, line.HeadId, line.Kind)).ToList();
        Assert.Equal([ProposedKind.Head, ProposedKind.Row], proposed.Select(line => line.Kind));
        frame = FlowFeed.FromFrame(model, arrival, asking: false, speechOn: true);
        Assert.Equal(WorkSeen.Idle, frame.Work);
        Assert.Equal(0, frame.WaitingShown);
        Assert.True(frame.LineAdded);
        Assert.False(frame.Asking);
        Assert.True(frame.SpeechOn);
        Assert.Equal(proposed, frame.Proposed.Select(line => (line.Line.Id, line.HeadId, line.Kind)));

        // The pass reuses its list at the next frame; the input, which may wait in the window's queue, keeps its own.
        var next = arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false);
        Assert.Empty(next.Proposed);
        Assert.False(FlowFeed.FromFrame(model, next, asking: false, speechOn: true).LineAdded);
        Assert.Equal(proposed, frame.Proposed.Select(line => (line.Line.Id, line.HeadId, line.Kind)));

        // A new session under the window: the epoch the next frame carries has risen.
        var before = frame.Epoch;
        model.Clear();
        frame = FlowFeed.FromFrame(model, arrivals.Update(model.Lines, model.Working, ReplySpeechMode.All, toolCalls: false), asking: false, speechOn: true);
        Assert.Equal(before + 1, frame.Epoch);
        Assert.Equal(WorkSeen.Unknown, frame.Work);
    }

    [Fact]
    public void Letters_A_C_D_become_marks_and_others_nothing()
    {
        Assert.Equal(new MarkSeen(TurnMark.Start), FlowFeed.FromMark('A'));
        Assert.Equal(new MarkSeen(TurnMark.Output), FlowFeed.FromMark('C'));
        Assert.Equal(new MarkSeen(TurnMark.End), FlowFeed.FromMark('D'));

        // B (the prompt's end) and every other letter are no mark of a turn; the letters are upper case as written.
        foreach (var letter in "BEPLa cd0")
        {
            Assert.Null(FlowFeed.FromMark(letter));
        }
    }

    [Fact]
    public void Each_hook_kind_becomes_its_neutral_input()
    {
        const string session = "7aac8697-1111-4222-8333-444444444444";

        // A session named: its id, the record's path as the event has it, and why, in the core's own words.
        var named = Assert.IsType<SessionNamed>(FlowFeed.FromHook(new HookEvent(HookKind.SessionStarted, session, null, RecordPath: @"C:\Temp\projects\C--Temp-x\a.jsonl", Source: ClaudeCodeHooks.SourceClear)));
        Assert.Equal(session, named.SessionId);
        Assert.Equal(@"C:\Temp\projects\C--Temp-x\a.jsonl", named.RecordPath);
        Assert.Equal(SessionSource.Clear, named.Source);
        Assert.Equal(SessionSource.Startup, Source(ClaudeCodeHooks.SourceStartup));
        Assert.Equal(SessionSource.Resume, Source(ClaudeCodeHooks.SourceResume));
        Assert.Equal(SessionSource.Compact, Source(ClaudeCodeHooks.SourceCompact));
        Assert.Equal(SessionSource.Fork, Source("fork"));
        Assert.Equal(SessionSource.Other, Source("something new"));
        Assert.Equal(SessionSource.Other, Source(null));
        Assert.Null(Assert.IsType<SessionNamed>(FlowFeed.FromHook(new HookEvent(HookKind.SessionStarted, session, null))).RecordPath);

        // A turn stopped: the screen key of its last message, and of the tasks it listed those that still run.
        var stopped = Assert.IsType<TurnStopped>(FlowFeed.FromHook(new HookEvent(HookKind.TurnStopped, session, "turn", LastReply: "# Result\n\nok",
            Tasks:
            [
                new ListedTask("b0jdigiy3", "shell", "running", "Run ping in background", null),
                new ListedTask("b1", "shell", "completed", "Ended before the stop", null),
                new ListedTask("a7", "subagent", "running", "Count the files", "general-purpose"),
            ])));
        Assert.Equal("resultok", stopped.LastReplyKey);
        Assert.Equal(
            [new FlowTask("b0jdigiy3", "shell", "Run ping in background", null), new FlowTask("a7", "subagent", "Count the files", "general-purpose")],
            stopped.Tasks);

        // No last message, or one without a letter, is no key; no list at all is an empty one.
        stopped = Assert.IsType<TurnStopped>(FlowFeed.FromHook(new HookEvent(HookKind.TurnStopped, session, null, LastReply: string.Empty)));
        Assert.Null(stopped.LastReplyKey);
        Assert.Empty(stopped.Tasks);
        Assert.Null(Assert.IsType<TurnStopped>(FlowFeed.FromHook(new HookEvent(HookKind.TurnStopped, session, null))).LastReplyKey);
        Assert.Null(Assert.IsType<TurnStopped>(FlowFeed.FromHook(new HookEvent(HookKind.TurnStopped, session, null, LastReply: "…"))).LastReplyKey);

        Assert.IsType<TurnFailed>(FlowFeed.FromHook(new HookEvent(HookKind.TurnFailed, session, null)));

        // A worker with its type; one without a type is a helper of the program's own and no input (EG-2.9).
        Assert.Equal(new WorkerStarted("a7", "general-purpose"), FlowFeed.FromHook(new HookEvent(HookKind.WorkerStarted, session, null, WorkerId: "a7", WorkerType: "general-purpose")));
        Assert.Equal(new WorkerStopped("a7", "general-purpose"), FlowFeed.FromHook(new HookEvent(HookKind.WorkerStopped, session, null, WorkerId: "a7", WorkerType: "general-purpose")));
        Assert.Null(FlowFeed.FromHook(new HookEvent(HookKind.WorkerStarted, session, null, WorkerId: "side")));
        Assert.Null(FlowFeed.FromHook(new HookEvent(HookKind.WorkerStopped, session, null, WorkerId: "side")));

        Assert.Equal(new AttentionAsked(AttentionKind.Permission), FlowFeed.FromHook(new HookEvent(HookKind.PermissionAsked, session, null, Tool: "Write")));
        Assert.Equal(new AttentionAsked(AttentionKind.Notification), FlowFeed.FromHook(new HookEvent(HookKind.Notified, session, null, Reason: "permission_prompt")));
        Assert.Equal(new SessionEnded("clear"), FlowFeed.FromHook(new HookEvent(HookKind.SessionEnded, session, null, Reason: "clear")));
        Assert.Equal(new SessionEnded(string.Empty), FlowFeed.FromHook(new HookEvent(HookKind.SessionEnded, session, null)));

        // Every kind is mapped: a kind added to the events without its input would fall through here.
        foreach (var kind in Enum.GetValues<HookKind>())
        {
            Assert.NotNull(FlowFeed.FromHook(new HookEvent(kind, session, null, WorkerId: "a", WorkerType: "t")));
        }

        // From a body as Claude Code writes it (section 6.2), through its own place and then here.
        var body = """{"session_id":"7aac8697-1111-4222-8333-444444444444","transcript_path":"C:\\Temp\\x.jsonl","cwd":"C:\\Temp\\axclaude-fixture","hook_event_name":"Stop","prompt_id":"5d0c1f6e","permission_mode":"auto","stop_hook_active":false,"last_assistant_message":"ok","background_tasks":[{"id":"b0jdigiy3","type":"shell","status":"running","description":"Run ping in background","command":"ping -n 8 127.0.0.1"}],"session_crons":[]}""";
        stopped = Assert.IsType<TurnStopped>(FlowFeed.FromHook(ClaudeCodeHooks.Parse(Encoding.UTF8.GetBytes(body))!));
        Assert.Equal("ok", stopped.LastReplyKey);
        Assert.Equal([new FlowTask("b0jdigiy3", "shell", "Run ping in background", null)], stopped.Tasks);

        static SessionSource Source(string? word) =>
            Assert.IsType<SessionNamed>(FlowFeed.FromHook(new HookEvent(HookKind.SessionStarted, "s", null, Source: word))).Source;
    }

    [Fact]
    public void Each_record_kind_becomes_its_neutral_input()
    {
        // A prompt's key is the letters of its text; one the program started by itself has no text and says so.
        Assert.Equal(new EntryRead(EntryKind.Prompt, "u1", "replywiththesinglewordok"),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.Prompt, "u1", Text: "Reply with the single word ok.", Origin: PromptOrigin.Typed)));
        Assert.Equal(new EntryRead(EntryKind.Prompt, "u2", "second"),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.Prompt, "u2", Text: "second", Origin: PromptOrigin.Queued)));
        Assert.Equal(new EntryRead(EntryKind.Prompt, "u3", null, SelfStarted: true),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.Prompt, "u3", Origin: PromptOrigin.SelfStarted)));

        // A reply's key is its screen key (the fence's lines dropped, the letters kept), and a cut reply says so.
        Assert.Equal(new EntryRead(EntryKind.ReplyText, "a1", "resultok"),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.ReplyText, "a1", MessageId: "msg_01", Text: "# Result\n\nok")));
        Assert.Equal(new EntryRead(EntryKind.ReplyText, "a2", "runthisdir"),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.ReplyText, "a2", Text: "Run this:\n```powershell\ndir\n```")));
        Assert.Equal(new EntryRead(EntryKind.ReplyText, "a3", "thelastlight", Cut: true),
            FlowFeed.FromEntry(new RecordEntry(RecordKind.ReplyText, "a3", Text: "The Last Light", Cut: true)));

        Assert.Equal(new EntryRead(EntryKind.ToolCall, "t1"), FlowFeed.FromEntry(new RecordEntry(RecordKind.ToolCall, "t1", ToolName: "Bash", ToolUseId: "toolu_1")));
        Assert.Equal(new EntryRead(EntryKind.ToolResult, "t2"), FlowFeed.FromEntry(new RecordEntry(RecordKind.ToolResult, "t2", ToolUseId: "toolu_1", IsError: true)));
        Assert.Equal(new EntryRead(EntryKind.Interrupt, "i1"), FlowFeed.FromEntry(new RecordEntry(RecordKind.Interrupt, "i1")));
        Assert.Equal(new EntryRead(EntryKind.TurnEnd, "e1"), FlowFeed.FromEntry(new RecordEntry(RecordKind.TurnEnd, "e1", DurationMs: 2412)));
        Assert.Equal(new EntryRead(EntryKind.Compact, "c1"), FlowFeed.FromEntry(new RecordEntry(RecordKind.CompactBoundary, "c1")));

        // The queue: a message put in with the letters of its text; one taken up and one taken out unsent both count as leaving it.
        Assert.Equal(new EntryRead(EntryKind.Enqueued, "@10", "second"), FlowFeed.FromEntry(new RecordEntry(RecordKind.Enqueued, "@10", Text: "second")));
        Assert.Equal(new EntryRead(EntryKind.Dequeued, "@20"), FlowFeed.FromEntry(new RecordEntry(RecordKind.Dequeued, "@20")));
        Assert.Equal(new EntryRead(EntryKind.Dequeued, "@30"), FlowFeed.FromEntry(new RecordEntry(RecordKind.Removed, "@30")));

        // What the core does not mind: a note, a thinking block, a compaction's summary, an agent's own entry.
        foreach (var kind in new[] { RecordKind.Note, RecordKind.Thinking, RecordKind.CompactSummary, RecordKind.Other })
        {
            Assert.Equal(new EntryRead(EntryKind.Other, "o1"), FlowFeed.FromEntry(new RecordEntry(kind, "o1", Text: "never a key", Sidechain: kind == RecordKind.Other)));
        }

        // Every kind is mapped.
        foreach (var kind in Enum.GetValues<RecordKind>())
        {
            Assert.IsType<EntryRead>(FlowFeed.FromEntry(new RecordEntry(kind, "x")));
        }
    }

    [Fact]
    public void A_sent_messages_key_is_the_key_of_its_prompt_in_the_record()
    {
        // The window notes the key of what it sent, and the folder rule looks for a prompt of the same key (EG-3.2):
        // the lines of a message are joined by Claude Code's own new line, which is no letter.
        const string message = "Reply with the number of lines in this message.\nThis is the second line.";
        var prompt = Assert.IsType<EntryRead>(FlowFeed.FromEntry(new RecordEntry(RecordKind.Prompt, "u1", Text: message, Origin: PromptOrigin.Typed)));
        Assert.Equal(prompt.Key, FlowFeed.SentKey(message));
        Assert.Equal("replywiththenumberoflinesinthismessagethisisthesecondline", FlowFeed.SentKey(message));
    }
}
