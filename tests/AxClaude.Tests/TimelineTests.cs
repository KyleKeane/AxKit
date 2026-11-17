using System.Text;
using System.Text.Json;
using AxClaude.Core.Flow;
using AxClaude.Core.Record;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The timeline fixtures replayed through the code the window uses (docs/axClaude/events-and-grounding.md EG-11.2;
/// <see cref="TimelineReplay"/> says how). Each fixture is one recorded session of Claude Code 2.1.325: its console
/// stream, its hook events, its session file's entries and what the script sent, on one clock.
///
/// In the recorded order the core's decisions must equal the fixture's <c>expected.txt</c>. That order is one the
/// window never has: the probe's hooks were ones Claude Code waited for, so an event comes before the marks of its
/// turn's end, where the window's come some hundred milliseconds after them (section 4.3). It is kept as the
/// regression; the window's own order is <see cref="ReplayOrder.EventsLate700"/>. In every order the invariants of
/// EG-11.2 must hold, and the tests below say what each recording shows of section 4.
///
/// What an order can do to a reply (section 11, R3): with a record whose entries come a second late nothing
/// confirms a reply's rows in their time, and they are dropped unspoken, counted. So where a test says that a reply
/// is spoken, it plays the orders in which the record is there in time (<see cref="RecordInTime"/>) or there is no
/// record at all (<see cref="NoRecord"/>), where the old hold releases the rows.
/// </summary>
public class TimelineTests
{
    private static readonly ReplayOrder[] Orders = Enum.GetValues<ReplayOrder>();

    /// <summary>The orders in which the core gets the marks: all but the screen alone.</summary>
    private static readonly ReplayOrder[] WithMarks = [.. Orders.Where(order => order != ReplayOrder.ScreenAlone)];

    /// <summary>The orders with a record whose entries come as recorded.</summary>
    private static readonly ReplayOrder[] RecordInTime =
    [
        ReplayOrder.Recorded, ReplayOrder.EventsEarly300, ReplayOrder.EventsLate300, ReplayOrder.EventsLate700, ReplayOrder.EventsLate1200, ReplayOrder.NoEvents,
    ];

    /// <summary>The orders with marks and no record: a proposed line is released after the old hold.</summary>
    private static readonly ReplayOrder[] NoRecord = [ReplayOrder.NoEntries, ReplayOrder.Neither];

    /// <summary>The orders in which the events arrive.</summary>
    private static readonly ReplayOrder[] WithEvents =
    [
        ReplayOrder.Recorded, ReplayOrder.EventsEarly300, ReplayOrder.EventsLate300, ReplayOrder.EventsLate700, ReplayOrder.EventsLate1200,
        ReplayOrder.EntriesLate1000, ReplayOrder.NoEntries,
    ];

    public static IEnumerable<object[]> Fixtures() => TimelineReplay.Names().Select(name => new object[] { name });

    public static IEnumerable<object[]> FixturesInEveryOrder() =>
        TimelineReplay.Names().SelectMany(name => Orders.Select(order => new object[] { name, order }));

    /// <summary>What a failing check shows: the fixture, the order, and the decisions with their times.</summary>
    private static string Shown(TimelineReplay replay, string what = "") =>
        $"{replay.Fixture}, {replay.Order}{(what.Length == 0 ? "" : ": " + what)}\n{string.Join("\n", replay.Faults)}\n{string.Join("\n", replay.Lines)}";

    /// <summary>The Dones and the releases in the order decided: a Done as <c>done</c>, a release as its lines' texts.</summary>
    private static List<string> SpokenAndDone(TimelineReplay replay) =>
        [.. replay.Decisions.Where(d => d.Decision is SayDone or SpeakReply).Select(d => d.Decision is SayDone ? "done" : string.Join(" | ", d.Texts))];

    private static List<long> Times(TimelineReplay replay, char letter) => [.. replay.Marks.Where(mark => mark.Letter == letter).Select(mark => mark.At)];

    private static List<(long At, FlowState State)> States(TimelineReplay replay) =>
        [.. replay.Decisions.Where(d => d.Decision is StateChanged).Select(d => (d.At, ((StateChanged)d.Decision).State))];

    [Fact]
    public void The_fifteen_recordings_are_there()
    {
        Assert.Equal(
            [
                "background-agents", "background-shell", "clear", "compact", "continue", "interrupts", "long-reply", "no-hooks",
                "plain-turns", "questions", "queued-one", "queued-two", "send-now", "text-tool-text", "tool-question",
            ],
            TimelineReplay.Names());
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Timeline_gives_the_expected_decisions(string fixture)
    {
        var replay = TimelineReplay.Run(fixture);
        var actual = string.Join("\n", replay.Lines) + "\n";
        var path = Path.Combine(TimelineReplay.FolderOf(fixture), "expected.txt");
        if (Environment.GetEnvironmentVariable("AXCLAUDE_UPDATE_EXPECTED") == "1")
        {
            // Only a file that differs is written: the others keep their bytes, and git lists the one that changed.
            AxKit.TestShared.Fixtures.WriteExpected(path, actual);
            return;
        }

        Assert.True(File.Exists(path), $"Missing {path}. Run this test once with AXCLAUDE_UPDATE_EXPECTED=1 and review the result.");
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }

    [Theory]
    [MemberData(nameof(FixturesInEveryOrder))]
    public void Hostile_orders_keep_the_invariants(string fixture, ReplayOrder order)
    {
        var replay = TimelineReplay.Run(fixture, order);
        Assert.True(replay.Faults.Count == 0, Shown(replay));

        // Nothing is left open or held when the process has ended.
        Assert.False(replay.Flow.Working || replay.Flow.Ending, Shown(replay, "a turn is still open or ending"));
        Assert.Equal(0, replay.Health.GatePending);

        // No decision differs between two runs of the same order: the wake times and the released texts too.
        Assert.Equal(replay.Everything, TimelineReplay.Run(fixture, order).Everything);
    }

    [Fact]
    public void Plain_turns_say_Done_three_times_each_after_its_reply()
    {
        // 4.1 "A turn ends", three times, each "Reply with the single word ok."; then /exit, a command Claude Code
        // handles itself, whose marks and bell come together: a turn that ended with its bell by 4.2, with no reply.
        foreach (var order in RecordInTime.Concat(NoRecord))
        {
            var replay = TimelineReplay.Run("plain-turns", order);
            Assert.True(
                SpokenAndDone(replay).SequenceEqual(["claude: ok", "done", "claude: ok", "done", "claude: ok", "done", "done"]),
                Shown(replay));
            Assert.Equal([1, 2, 3, 4], replay.Dones.Select(d => ((SayDone)d.Decision).Turn));
        }

        // The same with no hooks at all, as recorded: the folder rule finds the file by the first prompt sent.
        var plain = TimelineReplay.Run("no-hooks");
        Assert.True(SpokenAndDone(plain).SequenceEqual(["claude: ok", "done", "claude: ok", "done", "claude: ok", "done", "done"]), Shown(plain));
        Assert.Equal(RecordUse.Followed, plain.Health.Record);
        Assert.Equal(0, plain.Health.HooksReceived);

        // The screen alone: the replies are spoken inside the turns the screen shows, and the core says no Done.
        var alone = TimelineReplay.Run("plain-turns", ReplayOrder.ScreenAlone);
        Assert.Equal(["claude: ok", "claude: ok", "claude: ok"], SpokenAndDone(alone));
        Assert.All(alone.Decisions.Select(d => d.Decision).OfType<AttentionBell>(), bell => Assert.True(bell.NoMarks));
        Assert.Equal(alone.Bells.Count, alone.Decisions.Count(d => d.Decision is AttentionBell));
    }

    [Fact]
    public void Interrupts_say_no_Done_and_leave_the_state_idle()
    {
        // 4.1 "An interrupt (Escape)", three times: twice while a reply was written, once while a tool ran. C and D
        // with no bell; the turn is closed and nothing is said (4.2). The one Done is /exit's.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("interrupts", order);
            Assert.Equal(3, replay.Health.TurnsInterrupted);
            Assert.Equal(1, replay.Health.TurnsFinished);
            var done = Assert.Single(replay.Dones);
            Assert.True(done.At >= Assert.Single(replay.BellEnds), Shown(replay));

            // At each of the three end marks the state is idle, and stays so until the next message's turn.
            var ends = Times(replay, 'D');
            var starts = Times(replay, 'A');
            Assert.Equal(4, ends.Count);
            for (var i = 0; i < 3; i++)
            {
                var between = States(replay).Where(s => s.At >= ends[i] && s.At < starts[i + 1]).ToList();
                Assert.True(between.Count > 0 && between[0] == (ends[i], FlowState.Idle), Shown(replay, $"the state at the end mark {ends[i]}"));
            }
        }
    }

    [Fact]
    public void An_interrupted_replys_shown_part_is_released()
    {
        // 4.1 "An interrupt": the part of the cut reply that was shown is drawn after C and D (here 29 and 24 after
        // them), and its cut entry is read later still (128 and 138 after them). The turn stays ending for those
        // rows, with no Done, so the row is released when its entry confirms it, once. The row Claude Code prints
        // under it stands in no entry and is not spoken as a reply.
        const string Head = "claude: The Last Light at Gannet Rock";
        foreach (var order in RecordInTime)
        {
            var replay = TimelineReplay.Run("interrupts", order);
            Assert.True(replay.Spoken.SequenceEqual([Head, Head]), Shown(replay));
        }

        // The entries 100 later than recorded, past the bell's time of 150 after the end mark: still the turn's.
        var late = TimelineReplay.Run("interrupts", ReplayOrder.Recorded, entriesLateMs: 100);
        Assert.True(late.Spoken.SequenceEqual([Head, Head]), Shown(late));
        var ends = Times(late, 'D');
        Assert.True(late.Speaks[0].At > ends[0] + SessionFlow.BellAfterEndMs && late.Speaks[0].At <= ends[0] + SessionFlow.DoneCapMs, Shown(late));
        Assert.True(late.Speaks[1].At > ends[1] + SessionFlow.BellAfterEndMs && late.Speaks[1].At <= ends[1] + SessionFlow.DoneCapMs, Shown(late));
        Assert.Empty(late.Faults);

        // Without a record the hold of 250 releases what the screen proposed, as before the core: the shown part
        // and Claude Code's own row under it, 250 after they were drawn, which is past the bell's time too.
        foreach (var order in NoRecord)
        {
            var replay = TimelineReplay.Run("interrupts", order);
            Assert.True(replay.Spoken.Count == 4 && replay.Spoken[0] == Head && replay.Spoken[2] == Head, Shown(replay));
            Assert.StartsWith("Interrupted", replay.Spoken[1]);
            Assert.StartsWith("Interrupted", replay.Spoken[3]);
        }
    }

    [Fact]
    public void A_tool_question_is_an_attention_bell_and_Done_comes_only_at_the_turns_end()
    {
        // 4.1 "A tool's question opens": a bell alone, no C or D, while the turn is open; the answer; the turn's end.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("tool-question", order);
            var attention = Assert.Single(replay.Decisions, d => d.Decision is AttentionBell);
            Assert.False(((AttentionBell)attention.Decision).NoMarks);
            Assert.Equal(replay.Bells[0], attention.At);

            // The turn stays open through the question, ten seconds of it: no state change between its start mark
            // and its end mark, and the first Done at the turn's own bell, not at the question's.
            var start = Times(replay, 'A')[0];
            var end = Times(replay, 'D')[0];
            Assert.True(attention.At > start && attention.At < end);
            Assert.DoesNotContain(States(replay), s => s.At > start && s.At < end);
            Assert.Equal(2, replay.Dones.Count);
            Assert.True(replay.Dones[0].At >= replay.BellEnds[0] && replay.BellEnds[0] > attention.At, Shown(replay));
        }

        // The reply after the answer is spoken once, in front of Done; nothing of the question's rows is.
        foreach (var order in RecordInTime.Concat(NoRecord))
        {
            var replay = TimelineReplay.Run("tool-question", order);
            Assert.True(SpokenAndDone(replay).SequenceEqual(["claude: finished", "done", "done"]), Shown(replay));
        }
    }

    [Fact]
    public void Two_queued_messages_are_one_turn_and_the_state_never_reads_idle_between()
    {
        // 4.1 "Two queued messages": one A, two dequeues, two prompts, one turn. EG-4.6: between a turn's end and
        // the A of the waiting messages' turn the state is not ready.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("queued-two", order);

            // Three start marks: the first message's turn, one turn for both queued messages, and /exit.
            var starts = Times(replay, 'A');
            var ends = Times(replay, 'D');
            Assert.Equal(3, starts.Count);
            Assert.Equal([1, 2, 3], replay.Dones.Select(d => ((SayDone)d.Decision).Turn));
            Assert.Equal(3, replay.Health.TurnsFinished);

            // Working from the first turn's start to the queued turn's end mark: the first idle is said there.
            var states = States(replay);
            Assert.True(states[0].State == FlowState.Working && states[0].At <= starts[0], Shown(replay));
            Assert.True(states[1] == (ends[1], FlowState.Idle), Shown(replay));
        }
    }

    [Fact]
    public void Send_now_says_no_Done_for_the_cut_reply()
    {
        // 4.1 "Send now (Ctrl+X Ctrl+S)": C, D and the bell 62 after the chord, then A: the running reply was cut
        // for the queued message, an end the user asked for (EG-D-14). Nothing is said, and the next A opens the
        // queued message's turn; that turn's own end is said.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("send-now", order);
            Assert.Equal(1, replay.Health.TurnsCut);
            Assert.Equal(0, replay.Health.TurnsInterrupted);
            Assert.Equal(3, replay.Bells.Count);
            Assert.Equal(2, replay.BellEnds.Count);
            Assert.DoesNotContain(replay.Decisions, d => d.Decision is AttentionBell);
            Assert.Equal([2, 3], replay.Dones.Select(d => ((SayDone)d.Decision).Turn));
            Assert.True(replay.Dones[0].At >= replay.BellEnds[0], Shown(replay));

            // The state is working from the first start mark through the cut to the queued turn's end mark.
            var states = States(replay);
            Assert.True(states[0].State == FlowState.Working && states[1] == (Times(replay, 'D')[1], FlowState.Idle), Shown(replay));
        }

        // The cut reply's shown part is drawn inside the queued message's turn and confirmed by its cut entry.
        foreach (var order in RecordInTime)
        {
            var replay = TimelineReplay.Run("send-now", order);
            Assert.True(SpokenAndDone(replay).SequenceEqual(["claude: The Keeper of Halden Point", "claude: second", "done", "done"]), Shown(replay));
        }
    }

    [Fact]
    public void A_background_shell_is_listed_until_a_stop_lists_none()
    {
        // 4.1 "A background shell": the stop event lists it while it runs; its end is a turn Claude Code starts by
        // itself, and that turn's stop lists none (EG-7.1).
        foreach (var order in WithEvents)
        {
            var replay = TimelineReplay.Run("background-shell", order);
            var listed = replay.Decisions.Select(d => d.Decision).OfType<BackgroundListed>().ToList();
            Assert.True(listed.Count == 2, Shown(replay));
            Assert.Equal(SessionFlow.BackgroundShellType, Assert.Single(listed[0].Tasks).Type);
            Assert.Empty(listed[1].Tasks);
            Assert.Empty(replay.Flow.Tasks);

            // The turn Claude Code started for the shell's end has its own marks, bell and Done, between the two
            // turns of the user's messages. (The recording's stream ends before the script's /exit.)
            Assert.True(replay.Dones.Select(d => ((SayDone)d.Decision).Turn).SequenceEqual([1, 2, 3]), Shown(replay));
            Assert.Equal(3, Times(replay, 'A').Count);
        }

        // Without events nothing is listed, and the screen's own count stays the window's (EG-7.5).
        foreach (var order in new[] { ReplayOrder.NoEvents, ReplayOrder.Neither, ReplayOrder.ScreenAlone })
        {
            Assert.DoesNotContain(TimelineReplay.Run("background-shell", order).Decisions, d => d.Decision is BackgroundListed);
        }
    }

    [Fact]
    public void A_turn_Claude_Code_began_by_itself_is_said_so_once_and_no_other_turn_is()
    {
        // 4.1 "A background shell" and the agents' hand-backs: the turn Claude Code starts when work in the
        // background ends has a prompt of the source system in the file, read inside the turn (73, 202 and 228
        // after its start mark). EG-4.6: the core says once that the program began it. The shell's recording has
        // one such turn, the agents' two; the agents' two hand-back notes, whose entries are read after their
        // turns' ends, say nothing; and no turn of any other recording is said to be one.
        foreach (var fixture in TimelineReplay.Names())
        {
            foreach (var order in Orders)
            {
                var replay = TimelineReplay.Run(fixture, order);
                var said = replay.Decisions.Where(d => d.Decision is TurnSelfStarted).ToList();

                // The record alone says whose a turn is: without its entries nothing is said, in any recording.
                var withRecord = RecordInTime.Contains(order) || order == ReplayOrder.EntriesLate1000;
                var expected = !withRecord ? 0 : fixture == "background-shell" ? 1 : fixture == "background-agents" ? 2 : 0;
                Assert.True(said.Count == expected, Shown(replay, $"{said.Count} turns said to be self-started, not {expected}"));

                // Each inside its turn: the last mark before it is that turn's start mark.
                foreach (var decided in said)
                {
                    Assert.True(replay.Marks.Last(mark => mark.At <= decided.At).Letter == 'A', Shown(replay, $"self-started at {decided.At}"));
                }
            }
        }
    }

    [Fact]
    public void Clear_asks_for_the_new_file()
    {
        // 4.1 "/clear": a new session id and a new file at once, named by the session's start event (EG-3.2, EG-4.9).
        foreach (var order in new[] { ReplayOrder.Recorded, ReplayOrder.EventsEarly300, ReplayOrder.EventsLate300, ReplayOrder.EventsLate700, ReplayOrder.EventsLate1200, ReplayOrder.EntriesLate1000 })
        {
            var replay = TimelineReplay.Run("clear", order);
            var follows = replay.Decisions.Select(d => d.Decision).OfType<FollowRecord>().ToList();
            Assert.True(follows.Count == 2, Shown(replay));
            Assert.Equal(2, replay.Sessions.Count);
            Assert.Equal(replay.Sessions[0], Path.GetFileNameWithoutExtension(follows[0].Path));
            Assert.Equal(replay.Sessions[1], Path.GetFileNameWithoutExtension(follows[1].Path));
            Assert.NotEqual(replay.Sessions[0], replay.Sessions[1]);
        }

        // The conversation started afresh under the window: the Done owed for /clear's own marks and bell goes
        // with it (EG-4.9). The Dones left are /hooks' at its Escape and /exit's, the first and the third turn.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("clear", order);
            Assert.Equal(3, replay.BellEnds.Count);
            Assert.True(replay.Dones.Select(d => ((SayDone)d.Decision).Turn).SequenceEqual([1, 3]), Shown(replay));
        }
    }

    [Fact]
    public void Continue_releases_nothing_of_the_redrawn_conversation()
    {
        // 4.1 "--continue": the whole old conversation in one frame, before the session's start event, with no mark.
        // It is there for reading: nothing of it is proposed, and so nothing is released, dropped or counted.
        foreach (var order in RecordInTime.Concat(NoRecord).Append(ReplayOrder.ScreenAlone))
        {
            var replay = TimelineReplay.Run("continue", order);
            Assert.True(replay.Spoken.SequenceEqual(["claude: banana"]), Shown(replay));
            var health = replay.Health;
            Assert.Equal((1, 0, 0, 0), (health.GateReleased, health.GateOutsideTurn, health.GateUnconfirmed, health.GateStale));

            // The redrawn exchange stands in the conversation with its markers, above the one of this run.
            var texts = replay.Model.Lines.Where(line => !line.Hidden).Select(line => line.Text).ToList();
            var old = texts.IndexOf("# Output 1 from previous session Reply from Claude");
            Assert.True(old >= 0 && old < texts.IndexOf("# Output 1 Reply from Claude"), Shown(replay));
        }
    }

    [Fact]
    public void Compaction_keeps_the_file_and_the_turn()
    {
        // 4.1 "/compact": the same file; a worker's stop with an empty type, Claude Code's own helper, which is no
        // worker of the user's; a session start with the source compact; a bell at its end, after C and D.
        foreach (var order in WithEvents)
        {
            var replay = TimelineReplay.Run("compact", order);

            // One file, followed once and never forgotten; nothing listed for the helper.
            Assert.Single(replay.Decisions, d => d.Decision is FollowRecord);
            Assert.DoesNotContain(replay.Decisions, d => d.Decision is ForgetRecord or BackgroundListed);
            Assert.Contains(replay.Trace, line => line.EndsWith("> named Compact file=yes", StringComparison.Ordinal));
            Assert.DoesNotContain(replay.Trace, line => line.Contains("> Worker", StringComparison.Ordinal));

            // The compaction's turn is the third: open from its start mark to its end mark through the session
            // start in between, and said Done at its bell like any other. The turns keep their numbers.
            var start = Times(replay, 'A')[2];
            var end = Times(replay, 'D')[2];
            Assert.DoesNotContain(States(replay), s => s.At > start && s.At < end);
            Assert.True(replay.Dones.Select(d => ((SayDone)d.Decision).Turn).SequenceEqual([1, 2, 3, 4]), Shown(replay));
        }

        // Both replies before the compaction are spoken, each in front of its Done. The second one's row is drawn
        // 15 after the turn summary row and 35 after the bell, when the stop event and the entry have both come:
        // Done waits for the head of the reply the record holds.
        foreach (var order in RecordInTime)
        {
            var replay = TimelineReplay.Run("compact", order);
            Assert.True(SpokenAndDone(replay).SequenceEqual(["claude: red", "done", "claude: blue", "done", "done", "done"]), Shown(replay));
        }
    }

    [Fact]
    public void Every_row_of_the_long_reply_is_confirmed_by_its_entry()
    {
        // A reply of 40 numbered lines, and a Markdown sample (a heading, a list, a table, a link, a code block):
        // every row the screen draws of them stands in its entry's key in order (EG-3.7, EG-5.3), so every row is
        // released once, in the conversation's order, and none is dropped.
        foreach (var order in RecordInTime)
        {
            var replay = TimelineReplay.Run("long-reply", order);
            var health = replay.Health;
            Assert.True((health.GateOutsideTurn, health.GateUnconfirmed, health.GateStale) == (0, 0, 0), Shown(replay));

            // What was released is what stands in the conversation as Claude's replies: each claude: row and the
            // rows under it that hold text, down to the next row of another kind (the turn summary row).
            var replies = new List<string>();
            var inReply = false;
            foreach (var line in replay.Model.Lines.Where(line => !line.Hidden))
            {
                if (line.Kind == LineKind.ClaudeReply)
                {
                    inReply = true;
                    replies.Add(line.Text);
                }
                else if (line.Kind != LineKind.Plain)
                {
                    inReply = false;
                }
                else if (inReply && line.Text.Length > 0)
                {
                    replies.Add(line.Text);
                }
            }

            Assert.Equal(62, replies.Count);
            Assert.True(replay.Spoken.SequenceEqual(replies), Shown(replay));
            Assert.StartsWith("claude: 1. ", replay.Spoken[0]);
            Assert.StartsWith("40. ", replay.Spoken[39]);

            // Each reply, in however many releases its frames made of it, in front of its turn's Done; /exit's last.
            var steps = new List<string>();
            foreach (var decided in replay.Decisions.Where(d => d.Decision is SayDone or SpeakReply))
            {
                var step = decided.Decision is SayDone ? "done" : "speak";
                if (step == "done" || steps.Count == 0 || steps[^1] != "speak")
                {
                    steps.Add(step);
                }
            }

            Assert.True(steps.SequenceEqual(["speak", "done", "speak", "done", "done"]), Shown(replay));
        }
    }

    [Fact]
    public void A_hand_back_notes_own_entry_is_a_note_and_opens_no_turn()
    {
        // 4.1 "A hand-back note with no turn": A, C and D within 1 and no bell, after the turn Claude Code started
        // for an agent's hand-back; then the note's own entry in the file, of the source system, for the record
        // alone (read 159 after the note's end mark). Claude Code marks that entry queueTranscriptOnly; the fixture's
        // whitelist does not keep the field, so here it is added to the fixture's two such lines as Claude Code
        // writes it.
        var notes = 0;
        foreach (var line in File.ReadLines(Path.Combine(TimelineReplay.FolderOf("background-agents"), "entries.jsonl")))
        {
            if (line.Length == 0)
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            var entry = document.RootElement.GetProperty("entry");
            var raw = entry.GetRawText();
            var asKept = ClaudeCodeTranscript.Parse(Encoding.UTF8.GetBytes(raw), 0, out _);
            if (asKept is not { Kind: RecordKind.Prompt, Origin: PromptOrigin.SelfStarted } || entry.TryGetProperty("isMeta", out _))
            {
                continue;
            }

            // Without the field it reads as a prompt Claude Code started; with it, as a note, never a prompt.
            notes++;
            var asWritten = ClaudeCodeTranscript.Parse(Encoding.UTF8.GetBytes("{\"queueTranscriptOnly\":true," + raw[1..]), 0, out var what);
            Assert.Equal(RecordLine.Read, what);
            Assert.Equal(new RecordEntry(RecordKind.Note, asKept.Id, asKept.TurnKey, Origin: PromptOrigin.SelfStarted), asWritten);
            Assert.Equal(EntryKind.Other, ((EntryRead)FlowFeed.FromEntry(asWritten!)!).Kind);
        }

        Assert.Equal(2, notes);

        // The field said false, or with another value, changes nothing.
        var prompt = ClaudeCodeTranscript.Parse("""{"type":"user","uuid":"u","promptSource":"system","queueTranscriptOnly":false,"message":{"content":"Continue the task."}}"""u8, 0, out _);
        Assert.Equal(new RecordEntry(RecordKind.Prompt, "u", Text: "Continue the task.", Origin: PromptOrigin.SelfStarted), prompt);

        // In the timeline: where the marks speak the note opens no turn, in whatever order its entry is read; the
        // two hand-backs are turns Claude Code started, each with its own Done, and nothing was interrupted.
        foreach (var order in WithMarks)
        {
            var replay = TimelineReplay.Run("background-agents", order);
            Assert.Equal(5, replay.Dones.Count);
            Assert.Equal(5, replay.Health.TurnsFinished);
            Assert.Equal(0, replay.Health.TurnsInterrupted);
            Assert.Equal(0, replay.Health.TurnsStale);
            Assert.False(replay.Flow.Working || replay.Flow.Ending, Shown(replay));
        }

        // Each agent is listed by the stop event of the turn that started it, and taken off by its own stop (EG-7.1).
        var listed = TimelineReplay.Run("background-agents", ReplayOrder.EventsLate700).Decisions.Select(d => d.Decision).OfType<BackgroundListed>().Select(l => l.Tasks.Count).ToList();
        Assert.Equal([1, 0, 1, 0], listed);
    }
}
