using AxClaude.Core.Flow;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The core (docs/axClaude/events-and-grounding.md EG-4, EG-5.2, EG-5.6, EG-6.6): the turn, the state, Done and
/// time, each test a list of inputs with their times and the decisions' lines. Every test names the moment of
/// section 4 (4.1 the moments of a turn, 4.2 telling the ends apart) it is about; the times are the measured ones
/// of 2026-11-15 where a moment was measured, in milliseconds. Wake times are kept apart from the other decisions,
/// as a timeline's expected lines keep them.
/// </summary>
public class SessionFlowTests
{
    private static readonly MarkSeen A = new(TurnMark.Start);
    private static readonly MarkSeen C = new(TurnMark.Output);
    private static readonly MarkSeen D = new(TurnMark.End);
    private static readonly BellSeen Bell = new();
    private static readonly Tick Tick = new();
    private static readonly TurnStopped Stop = new("ok", []);

    /// <summary>One core with its health. <see cref="At"/> feeds an input and gives the lines of its decisions, wake times apart.</summary>
    private sealed class Session
    {
        public readonly SourceHealth Health = new();
        public readonly SessionFlow Flow;

        /// <summary>Every decision so far but the wake times, each as its time and its line: <c>8086 done turn=1 after=75</c>.</summary>
        public readonly List<string> Said = [];

        /// <summary>Every wake time asked for so far, in order.</summary>
        public readonly List<long> Wakes = [];

        /// <summary>The decisions of the last input, wake times among them.</summary>
        public List<FlowDecision> Last = [];

        public Session()
        {
            Flow = new SessionFlow(Health);
        }

        public string[] At(long time, FlowInput input)
        {
            Last = [.. Flow.Handle(input, time)];
            var lines = new List<string>();
            foreach (var decision in Last)
            {
                if (decision is WakeAt wake)
                {
                    Wakes.Add(wake.AtMs);
                    continue;
                }

                lines.Add(decision.ToLine());
                Said.Add($"{time} {decision.ToLine()}");
            }

            return [.. lines];
        }

        /// <summary>
        /// Feeds the inputs in the order of their times, those of one time in the order given, with a tick at each
        /// time the core asked to be woken at, as the window's one timer sends them (a later wake time takes the
        /// place of the one before), and ticks after the last input until nothing is due.
        /// </summary>
        public void Play(IEnumerable<(long Time, FlowInput Input)> inputs)
        {
            var wake = long.MaxValue;
            foreach (var (time, input) in inputs.OrderBy(item => item.Time))
            {
                while (wake <= time)
                {
                    wake = Feed(wake, new Tick(), wake);
                }

                wake = Feed(time, input, wake);
            }

            for (var i = 0; i < 20 && wake != long.MaxValue; i++)
            {
                wake = Feed(wake, new Tick(), wake);
            }
        }

        /// <summary>Feeds one input and gives the timer's time after it: the one asked for now, the one before while it is still to come, or none.</summary>
        private long Feed(long time, FlowInput input, long wake)
        {
            At(time, input);
            return Last.OfType<WakeAt>().LastOrDefault() is { } asked ? asked.AtMs : wake > time ? wake : long.MaxValue;
        }
    }

    private static FrameSeen Frame(WorkSeen work, bool added = false, int waiting = 0, int epoch = 1, bool asking = false, params Proposed[] proposed) =>
        new(epoch, work, asking, added, SpeechOn: true, waiting, proposed);

    /// <summary>A frame while replies are not spoken: the gate then holds nothing and awaits no reply's head, so Done goes by its end's confirmation and the frame alone.</summary>
    private static FrameSeen Unspoken(WorkSeen work, bool added = false) => new(1, work, false, added, SpeechOn: false, 0, []);

    private static Proposed Head(int id, string text) => new(new Line(id, LineKind.ClaudeReply, text), id, ProposedKind.Head);

    private static Proposed Row(int id, int head, string text) => new(new Line(id, LineKind.Plain, text), head, ProposedKind.Row);

    // A tool row and an error row pass the gate unconfirmed (EG-5.5, EG-D-24): inside a turn they are released at
    // once whatever the record says, so these tests use them where a line must be released, and hold whatever
    // rules the gate has for a reply's own rows.
    private static Proposed ToolRow(int id, string text) => new(new Line(id, LineKind.Tool, text), id, ProposedKind.Tool);

    private static Proposed ErrorRow(int id, string text) => new(new Line(id, LineKind.Error, text), id, ProposedKind.Error);

    private static EntryRead Entry(EntryKind kind, bool cut = false) => new(kind, "entry", Cut: cut);

    [Fact]
    public void A_turn_opens_at_A_and_the_state_is_working()
    {
        // 4.1 "A message sent at the idle prompt": the start mark 14 to 62 after Enter (here 196, with hooks).
        // 4.2: A, Claude took a prompt up, a turn is open.
        var s = new Session();
        Assert.Empty(s.At(5649, new UserActed(UserAction.Sent, "replywiththesinglewordok")));
        Assert.False(s.Flow.Working);

        Assert.Equal(["state working"], s.At(5845, A));
        Assert.True(s.Flow.Working);
        Assert.False(s.Flow.Ending);
        Assert.Equal(1, s.Health.MarksA);

        // 4.1 "Claude works": no mark, no event, nothing in the file; the spinner's frames change nothing.
        Assert.Empty(s.At(5846, Frame(WorkSeen.Working)));
        Assert.Empty(s.At(6846, Frame(WorkSeen.Working)));
        Assert.True(s.Flow.Working);
    }

    [Fact]
    public void D_with_its_bell_ends_the_turn_and_Done_follows_the_frame_that_adds_a_line()
    {
        // 4.1 "A turn ends", the plain turn of 6.4 as it was measured: the stop event 22 before D, C and D in one
        // write, the bell 1 after D, the reply's rows 75 after the bell, the reply's entry 110 after the bell.
        // 4.2: C, D, then the bell within 150: the turn finished; Done (EG-5.6).
        var s = new Session();
        s.At(5845, A);
        s.At(5846, Frame(WorkSeen.Working));
        Assert.Empty(s.At(7988, Stop));
        Assert.Empty(s.At(8010, C));
        Assert.Equal(["state idle"], s.At(8010, D));
        Assert.False(s.Flow.Working);
        Assert.True(s.Flow.Ending);
        Assert.Empty(s.At(8011, Bell));
        Assert.True(s.Flow.Ending);

        // A frame after the bell that adds no line (the spinner going) is not the reply yet.
        Assert.Empty(s.At(8050, Frame(WorkSeen.Idle)));
        Assert.Equal(["done turn=1 after=75"], s.At(8086, Frame(WorkSeen.Idle, added: true)));
        Assert.False(s.Flow.Ending);

        // The reply's entry and the turn's end in the record come after Done and change nothing.
        Assert.Empty(s.At(8121, new EntryRead(EntryKind.ReplyText, "b8142ecf", "ok")));
        Assert.Empty(s.At(8121, Entry(EntryKind.TurnEnd)));
        Assert.Equal(["5845 state working", "8010 state idle", "8086 done turn=1 after=75"], s.Said);
        Assert.Equal(1, s.Health.TurnsFinished);

        // A line released by the same frame is spoken before Done (EG-5.6).
        var spoken = new Session();
        spoken.At(0, A);
        spoken.At(1990, Stop);
        spoken.At(2000, D);
        spoken.At(2001, Bell);
        Assert.Equal(["speak lines=1 letters=8", "done turn=1 after=49"], spoken.At(2050, Frame(WorkSeen.Idle, added: true, proposed: ToolRow(9, "tool: Read (a.txt)"))));
    }

    [Fact]
    public void A_plain_turn_says_one_Done_after_its_reply_in_every_order_of_its_sources()
    {
        // 4.1 "A message sent at the idle prompt" and "A turn ends", the plain turn as measured: Enter 5649, A 5845,
        // the prompt's entry 6182, the stop event 7988, C and D 8010, the bell 8011, the reply's rows 8086, the
        // reply's entry and the record's turn end 8121. Events and entries share no clock with the console's
        // frames, so the core accepts every order of them (EG-4.8), with a source missing too (EG-11.2).
        // Replies are not spoken here: Done goes by its end's confirmation and the frame that adds the reply's
        // rows. With replies spoken Done also follows the reply's speech, which SpeechGateTests plays in the same
        // orders (A_plain_turns_reply_is_spoken_once_and_before_Done_in_every_order_of_its_sources).
        (long, FlowInput)[] marks = [(5845, A), (8010, C), (8010, D)];
        (long, FlowInput)[] screen =
        [
            (5649, new UserActed(UserAction.Sent, "replywiththesinglewordok")),
            (5846, Unspoken(WorkSeen.Working)),
            (8011, Bell),
            (8086, Unspoken(WorkSeen.Idle, added: true)),
        ];
        static (long, FlowInput)[] Events(long shift) => [(7988 + shift, Stop)];
        static (long, FlowInput)[] Entries(long shift) =>
        [
            (6182 + shift, Entry(EntryKind.Prompt)),
            (8121 + shift, new EntryRead(EntryKind.ReplyText, "b8142ecf", "ok")),
            (8121 + shift, Entry(EntryKind.TurnEnd)),
        ];

        var orders = new (string Name, (long, FlowInput)[] Inputs, string Done)[]
        {
            ("as recorded", [.. marks, .. screen, .. Events(0), .. Entries(0)], "8086 done turn=1 after=75"),
            ("every event 300 early", [.. marks, .. screen, .. Events(-300), .. Entries(0)], "8086 done turn=1 after=75"),
            ("every event 300 late", [.. marks, .. screen, .. Events(300), .. Entries(0)], "8121 done turn=1 after=110"),
            ("every entry 1 s late", [.. marks, .. screen, .. Events(0), .. Entries(1000)], "8086 done turn=1 after=75"),
            ("no entries", [.. marks, .. screen, .. Events(0)], "8086 done turn=1 after=75"),
            ("no events", [.. marks, .. screen, .. Entries(0)], "8121 done turn=1 after=110"),
            ("neither", [.. marks, .. screen], "8311 done turn=1 after=300"),
        };
        foreach (var (name, inputs, done) in orders)
        {
            // One Done, not before the frame that adds the reply's rows, and the state never idle between A and D.
            var s = new Session();
            s.At(0, new RecordFound());
            s.Play(inputs);
            string[] expected = ["5845 state working", "8010 state idle", done];
            Assert.True(expected.SequenceEqual(s.Said), $"{name}: {string.Join("; ", s.Said)}");
            Assert.False(s.Flow.Working || s.Flow.Ending, name);
        }

        // No marks, the screen alone with the bell: the turn is the screen's, the bell is the window's to settle,
        // and no Done comes from the core.
        var alone = new Session();
        alone.Play([.. screen, .. Events(0), .. Entries(0)]);
        Assert.Equal(["5846 state working", "8011 bell attention no-marks", "8086 state idle"], alone.Said);
        Assert.False(alone.Flow.Working || alone.Flow.Ending);
    }

    [Fact]
    public void D_without_a_bell_closes_the_turn_and_says_nothing()
    {
        // 4.1 "An interrupt (Escape)": C and D 71 after the key and no bell, the Interrupted row 29 later, the cut
        // entry and the interrupt's entry 198 after the key, no event at all.
        // 4.2: C, D, and no bell within 150: the turn is closed; nothing is said.
        var s = new Session();
        s.At(5100, A);
        s.At(5200, Frame(WorkSeen.Working));
        Assert.Empty(s.At(5753, new UserActed(UserAction.Escaped)));
        Assert.Empty(s.At(5824, C));
        Assert.Equal(["state idle"], s.At(5824, D));
        Assert.Equal([5974], s.Wakes);
        Assert.Empty(s.At(5853, Frame(WorkSeen.Idle, added: true)));
        Assert.Empty(s.At(5952, Entry(EntryKind.ReplyText, cut: true)));
        Assert.Empty(s.At(5952, Entry(EntryKind.Interrupt)));

        // A tick at 150 after D: the bell's time is over. The turn stays ending, with no Done owed, until
        // DoneCapMs after D: the shown part of the cut reply is drawn after the end mark and its entry is read
        // later still, and both are still the turn's (the interrupts timeline: the row 30, the entry 128 after D).
        Assert.Empty(s.At(5974, Tick));
        Assert.True(s.Flow.Ending);
        Assert.False(s.Flow.Working);
        Assert.Equal([5974, 6424], s.Wakes);
        Assert.Empty(s.At(6424, Tick));
        Assert.False(s.Flow.Ending);
        Assert.Empty(s.At(6600, Tick));
        Assert.DoesNotContain(s.Said, line => line.Contains("done"));
        Assert.Equal(1, s.Health.TurnsInterrupted);
        Assert.Equal(0, s.Health.TurnsFinished);
    }

    [Fact]
    public void A_bell_alone_is_an_attention_bell_and_the_turn_stays_open()
    {
        // 4.1 "A tool's question opens": the permission event 9 to 13 before a bell alone, no C or D, the
        // question's rows after it. 4.2: a bell with no D in the 150 before it is a question from a tool.
        var s = new Session();
        s.At(1000, A);
        s.At(1100, Frame(WorkSeen.Working));
        Assert.Empty(s.At(3000, new AttentionAsked(AttentionKind.Permission)));
        Assert.Equal(["bell attention"], s.At(3010, Bell));
        Assert.True(s.Flow.Working);
        Assert.False(s.Flow.Ending);

        // The question stands on the screen: no mode line, Claude asks. The turn is open through it.
        Assert.Empty(s.At(3040, Frame(WorkSeen.Unknown, added: true, asking: true)));
        Assert.Empty(s.At(9040, Frame(WorkSeen.Unknown, asking: true)));
        Assert.True(s.Flow.Working);

        // The answer, and the turn's own end: Done comes only now.
        Assert.Empty(s.At(9500, new UserActed(UserAction.Answered)));
        s.At(11000, Stop);
        Assert.Equal(["state idle"], s.At(11020, D));
        Assert.Empty(s.At(11021, Bell));
        Assert.Equal(["done turn=1 after=39"], s.At(11060, Frame(WorkSeen.Idle, added: true)));
        Assert.Single(s.Said, line => line.Contains("done"));
    }

    [Fact]
    public void A_C_D_at_once_without_a_bell_says_nothing()
    {
        // 4.1 "A hand-back note with no turn": A, C and D within 1 and no bell.
        // 4.2: A, C, D within 150 with no bell: nothing; no turn was open for longer than the note.
        var s = new Session();
        Assert.Equal(["state working"], s.At(9000, A));
        Assert.Empty(s.At(9000, C));
        Assert.Equal(["state idle"], s.At(9001, D));
        Assert.Empty(s.At(9151, Tick));
        Assert.Empty(s.At(9700, Tick));
        Assert.Equal(["9000 state working", "9001 state idle"], s.Said);
        Assert.Equal(0, s.Health.TurnsInterrupted);
        Assert.Equal(0, s.Health.TurnsFinished);

        // The note was no turn: the turn after it is the first.
        s.At(10000, A);
        s.At(11990, Stop);
        s.At(12000, D);
        s.At(12001, Bell);
        Assert.Equal(["done turn=1 after=49"], s.At(12050, Frame(WorkSeen.Idle, added: true)));

        // Two notes one after the other, the second's A before the first's bell time is over: the A closes what
        // stood before it without a word, and neither is a turn.
        var two = new Session();
        Assert.Equal(["state working"], two.At(1000, A));
        Assert.Equal(["state idle"], two.At(1001, D));
        Assert.Equal(["state working"], two.At(1100, A));
        Assert.Equal(["state idle"], two.At(1101, D));
        Assert.Empty(two.At(1251, Tick));
        Assert.Equal(0, two.Health.TurnsInterrupted);
        two.At(2000, A);
        two.At(3000, D);
        two.At(3001, Bell);
        Assert.Equal(["done turn=1 after=600"], two.At(3601, Tick));
    }

    [Fact]
    public void Send_now_then_D_and_the_bell_is_a_cut_without_Done()
    {
        // 4.1 "Send now (Ctrl+X Ctrl+S)": C, D and the bell 31 after the chord, then A 71 after the bell; no stop
        // event for the cut turn; the cut entry, a dequeue and the queued prompt in the file.
        // 4.2: C, D and the bell within 2 s of the user's Send now: nothing is said; the next A opens the queued turn.
        var s = new Session();
        s.At(0, new RecordFound());
        s.At(5085, A);
        s.At(7170, Entry(EntryKind.Enqueued));
        s.At(7177, Frame(WorkSeen.Working, waiting: 1));
        Assert.Empty(s.At(7275, new UserActed(UserAction.SentNow)));
        Assert.Empty(s.At(7337, C));
        Assert.Empty(s.At(7337, D));
        Assert.Empty(s.At(7338, Bell));
        Assert.Empty(s.At(7409, A));
        Assert.True(s.Flow.Working);
        Assert.Empty(s.At(7491, Entry(EntryKind.Dequeued)));
        Assert.Empty(s.At(7491, Entry(EntryKind.ReplyText, cut: true)));
        Assert.Empty(s.At(7491, Entry(EntryKind.Prompt)));
        Assert.Empty(s.At(7499, Frame(WorkSeen.Working, added: true)));
        Assert.True(s.Flow.Working);

        // The queued message's own turn finishes, and that end is said, behind its reply: with a record, Done
        // waits for the head of the reply the stop event named, which the frame of the reply's rows proposes.
        s.At(9994, Stop);
        Assert.Equal(["state idle"], s.At(10010, D));
        s.At(10011, Bell);
        Assert.Equal(["speak lines=1 letters=2", "done turn=2 after=29"], s.At(10040, Frame(WorkSeen.Idle, added: true, proposed: Head(8, "claude: ok"))));
        Assert.Equal(["5085 state working", "10010 state idle", "10040 speak lines=1 letters=2", "10040 done turn=2 after=29"], s.Said);
        Assert.Equal(1, s.Health.TurnsCut);
        Assert.Equal(1, s.Health.TurnsFinished);

        // Measured too (M13 3.3): the chord with no message waiting does nothing, and the turn's end is a finish.
        var none = new Session();
        none.At(0, A);
        none.At(100, Frame(WorkSeen.Working));
        none.At(400, new UserActed(UserAction.SentNow));
        none.At(1380, Stop);
        none.At(1400, D);
        none.At(1401, Bell);
        Assert.Equal(["done turn=1 after=29"], none.At(1430, Frame(WorkSeen.Idle, added: true)));
        Assert.Equal(0, none.Health.TurnsCut);
    }

    [Fact]
    public void Done_waits_for_the_stop_event_or_the_records_end_or_300_ms()
    {
        // 4.1 "A turn ends": the stop event comes 12 to 130 before D, the record's turn end after the reply's
        // entry, 51 to 190 after the bell. EG-5.6, the second condition: the end is confirmed.

        // The stop event came: Done with the frame that adds the reply's rows, 29 after the bell.
        var stop = new Session();
        stop.At(0, A);
        stop.At(1988, Stop);
        stop.At(2010, D);
        stop.At(2011, Bell);
        Assert.Equal(["done turn=1 after=29"], stop.At(2040, Frame(WorkSeen.Idle, added: true)));

        // No events (the recording without hooks): the rows are there, and Done waits for the record's turn end.
        var record = new Session();
        record.At(0, new RecordFound());
        record.At(0, A);
        record.At(2010, D);
        record.At(2011, Bell);
        Assert.Empty(record.At(2040, Frame(WorkSeen.Idle, added: true)));
        Assert.Empty(record.At(2110, Entry(EntryKind.ReplyText)));
        Assert.Equal(["done turn=1 after=99"], record.At(2110, Entry(EntryKind.TurnEnd)));

        // Neither: the end counts as confirmed 300 after the bell.
        var neither = new Session();
        neither.At(0, A);
        neither.At(2010, D);
        neither.At(2011, Bell);
        Assert.Empty(neither.At(2040, Frame(WorkSeen.Idle, added: true)));
        Assert.Empty(neither.At(2310, Tick));
        Assert.Equal(["done turn=1 after=300"], neither.At(2311, Tick));
        Assert.Contains(2311, neither.Wakes);

        // A stop event that comes late, after the bell, confirms the Done that is owed.
        var late = new Session();
        late.At(0, A);
        late.At(2010, D);
        late.At(2011, Bell);
        late.At(2040, Frame(WorkSeen.Idle, added: true));
        Assert.Equal(["done turn=1 after=179"], late.At(2190, Stop));
    }

    [Fact]
    public void Done_comes_600_ms_after_the_bell_at_the_latest()
    {
        // 4.1 "A command Claude Code handles itself (/usage at its Escape)": A, C, D and the bell together, and no
        // row of a reply after them. EG-5.6: Done at the latest DoneCapMs after the bell.
        var s = new Session();
        Assert.Equal(["state working"], s.At(1000, A));
        Assert.Empty(s.At(1000, C));
        Assert.Equal(["state idle"], s.At(1000, D));
        Assert.Empty(s.At(1000, Bell));
        Assert.Empty(s.At(1060, Frame(WorkSeen.Idle)));
        Assert.Empty(s.At(1300, Tick));
        Assert.Empty(s.At(1599, Tick));
        Assert.True(s.Flow.Ending);
        Assert.Equal(["done turn=1 after=600"], s.At(1600, Tick));
        Assert.False(s.Flow.Ending);

        // A tick that comes late says it then, once.
        var late = new Session();
        late.At(1000, A);
        late.At(1000, D);
        late.At(1000, Bell);
        Assert.Equal(["done turn=1 after=950"], late.At(1950, Tick));
        Assert.Empty(late.At(2600, Tick));

        // With no tick at all, the next input finds it due: Done first, then what the input itself leads to.
        var next = new Session();
        next.At(1000, A);
        next.At(1000, D);
        next.At(1000, Bell);
        Assert.Equal(["done turn=1 after=4000", "state working"], next.At(5000, A));
    }

    [Fact]
    public void Done_is_not_dropped_by_a_new_generation()
    {
        // 4.1 "A turn ends": the reply's rows come up to 117 after the bell, and the user can have sent his next
        // message in between. EG-5.7, EG-D-13: Done is never dropped by a generation.
        var s = new Session();
        s.At(0, A);
        s.At(2000, Stop);
        s.At(2020, D);
        s.At(2021, Bell);
        var before = s.Flow.Generation;
        Assert.Empty(s.At(2040, new UserActed(UserAction.Sent, "next")));
        Assert.Empty(s.At(2050, new QuestionShown()));
        Assert.Equal(before + 2, s.Flow.Generation);

        Assert.Equal(["done turn=1 after=69"], s.At(2090, Frame(WorkSeen.Idle, added: true)));
        Assert.Equal(s.Flow.Generation, Assert.IsType<SayDone>(s.Last[0]).Generation);
    }

    [Fact]
    public void A_turn_opened_by_A_alone_closes_as_stale_after_two_seconds_of_an_idle_screen()
    {
        // 4.2: an A can come with no turn behind it. EG-4.6: opened by A alone, the screen idle in every frame for
        // IdleContradictsMs, neither an event nor an entry of that turn.
        var s = new Session();
        Assert.Equal(["state working"], s.At(0, A));
        Assert.Empty(s.At(4, Frame(WorkSeen.Idle)));
        Assert.Equal([2004], s.Wakes);
        Assert.Empty(s.At(1000, Frame(WorkSeen.Idle)));
        Assert.Empty(s.At(2003, Tick));
        Assert.Equal(["note stale-turn after=2004", "state idle"], s.At(2004, Tick));
        Assert.False(s.Flow.Working);
        Assert.False(s.Flow.Ending);
        Assert.Equal(1, s.Health.TurnsStale);

        // A turn stood behind it after all (4.1 "Claude works": the screen can show nothing of it): its end mark
        // and its bell still give Done, and it is the first turn.
        Assert.Empty(s.At(9000, D));
        Assert.Empty(s.At(9001, Bell));
        Assert.Empty(s.At(9040, Frame(WorkSeen.Idle, added: true)));
        Assert.Equal(["done turn=1 after=300"], s.At(9301, Tick));

        // 4.1 "A message sent at the idle prompt": the prompt's entry, 125 to 373 after Enter, is an entry of that
        // turn; so is a stop event, a permission request, a tool call.
        foreach (var evidence in new FlowInput[] { Entry(EntryKind.Prompt), Stop, new AttentionAsked(AttentionKind.Permission), Entry(EntryKind.ToolCall), new WorkerStarted("a1", "general-purpose") })
        {
            var real = new Session();
            real.At(0, A);
            real.At(4, Frame(WorkSeen.Idle));
            real.At(300, evidence);
            Assert.Empty(real.At(2004, Tick));
            Assert.Empty(real.At(60000, Frame(WorkSeen.Idle)));
            Assert.True(real.Flow.Working);
        }

        // A frame that shows work, or a question, breaks the two seconds.
        var worked = new Session();
        worked.At(0, A);
        worked.At(4, Frame(WorkSeen.Idle));
        worked.At(1000, Frame(WorkSeen.Working));
        worked.At(1500, Frame(WorkSeen.Idle));
        Assert.Empty(worked.At(2004, Tick));
        Assert.Empty(worked.At(3000, Frame(WorkSeen.Idle, asking: true)));
        Assert.Empty(worked.At(3100, Frame(WorkSeen.Idle)));
        Assert.Empty(worked.At(5099, Tick));
        Assert.Equal(["note stale-turn after=5100", "state idle"], worked.At(5100, Tick));

        // The idle prompt that stood before the mark counts from the mark.
        var prompt = new Session();
        prompt.At(0, Frame(WorkSeen.Idle));
        prompt.At(100, A);
        Assert.Equal([2100], prompt.Wakes);
        Assert.Equal(["note stale-turn after=2000", "state idle"], prompt.At(2100, Tick));
    }

    [Fact]
    public void Without_marks_the_screen_opens_and_closes_the_turn_and_the_bell_is_an_attention_bell()
    {
        // The screen-only path (EG-4.6, EG-4.5 "with none of it"): a program that draws no marks, as the batch
        // stand-ins of the window scripts. 4.1 "Claude works": the spinner row and esc to interrupt.
        var s = new Session();
        Assert.Empty(s.At(0, Frame(WorkSeen.Idle)));
        Assert.Empty(s.At(100, new UserActed(UserAction.Sent, "hello")));
        Assert.Equal(["state working"], s.At(200, Frame(WorkSeen.Working)));

        // The bell is all there is of an end: the window's settle says Done by the old rule.
        Assert.Equal(["bell attention no-marks"], s.At(2000, Bell));
        Assert.True(Assert.IsType<AttentionBell>(s.Last[0]).NoMarks);

        // The screen shows Claude idle: the turn is closed, and stays ending for DoneCapMs, so that its last rows
        // are still inside it (EG-5.2): a row proposed then is released, one proposed after it is dropped.
        Assert.Equal(["state idle"], s.At(2030, Frame(WorkSeen.Idle, added: true)));
        Assert.False(s.Flow.Working);
        Assert.True(s.Flow.Ending);
        Assert.Equal([2630], s.Wakes);
        Assert.Equal(["speak lines=1 letters=24"], s.At(2300, Frame(WorkSeen.Idle, added: true, proposed: ErrorRow(7, "error: API Error: 401 Invalid API key"))));
        Assert.Empty(s.At(2630, Tick));
        Assert.False(s.Flow.Ending);
        Assert.Empty(s.At(2700, Frame(WorkSeen.Idle, added: true, proposed: ErrorRow(8, "error: API Error: 401 Invalid API key"))));
        Assert.Equal(1, s.Health.GateOutsideTurn);

        // No Done comes from the core for such a turn.
        Assert.Empty(s.At(5000, Tick));
        Assert.DoesNotContain(s.Said, line => line.Contains("done"));

        // A frame with no mode line (a picker over the prompt) does not close a turn the screen opened.
        var picker = new Session();
        picker.At(0, Frame(WorkSeen.Working));
        Assert.Empty(picker.At(500, Frame(WorkSeen.Unknown)));
        Assert.True(picker.Flow.Working);
        Assert.Equal(["state idle"], picker.At(900, Frame(WorkSeen.Idle)));

        // The screen was first and the start mark follows (EG-4.5, the order of the evidence): from the mark on
        // the marks speak for the turn, and an idle screen closes it no more.
        var marked = new Session();
        Assert.Equal(["state working"], marked.At(0, Frame(WorkSeen.Working)));
        Assert.Empty(marked.At(50, A));
        Assert.Empty(marked.At(60, Entry(EntryKind.Prompt)));
        Assert.Empty(marked.At(100, Frame(WorkSeen.Idle)));
        Assert.True(marked.Flow.Working);
        Assert.Equal(["state idle"], marked.At(2000, D));
        marked.At(2001, Bell);
        Assert.Equal(["done turn=1 after=600"], marked.At(2601, Tick));
    }

    [Fact]
    public void After_an_end_by_marks_the_screens_spinner_does_not_reopen_for_400_ms()
    {
        // 4.1 "A turn ends": the spinner row can still stand in a frame after C and D (measured: the rows of the
        // working block for up to 117 after the bell). EG-4.6: QuietAfterEndMs after a turn closed by its marks.
        var s = new Session();
        s.At(0, A);
        s.At(1990, Stop);
        Assert.Equal(["state idle"], s.At(2000, D));
        s.At(2001, Bell);

        // The frame that adds the reply's rows still shows the spinner: it was cut in the middle of the redraw
        // that ends the turn, and Done waits for the frame that shows the turn's work gone (EG-5.6; measured in
        // the timelines, where Done on the cut frame came in front of the reply's own row).
        Assert.Empty(s.At(2050, Frame(WorkSeen.Working, added: true)));
        Assert.Empty(s.At(2100, Frame(WorkSeen.Working)));
        Assert.False(s.Flow.Working);
        Assert.Equal(["done turn=1 after=117"], s.At(2118, Frame(WorkSeen.Idle)));
        Assert.Empty(s.At(2399, Frame(WorkSeen.Working)));
        Assert.False(s.Flow.Working);

        // After the quiet the screen's sign of work is evidence again: a turn no mark opened.
        Assert.Equal(["state working"], s.At(2400, Frame(WorkSeen.Working)));
        Assert.Equal(["state idle"], s.At(2500, Frame(WorkSeen.Idle)));
    }

    [Fact]
    public void A_prompt_entry_opens_a_turn_when_no_mark_did()
    {
        // 4.1 "A message sent at the idle prompt": the user entry, typed, 125 to 373 after Enter. EG-4.5: the
        // second evidence of a turn's start, while none is open.
        var s = new Session();
        s.At(0, new RecordFound());
        s.At(5649, new UserActed(UserAction.Sent, "hello"));
        Assert.Equal(["state working"], s.At(5800, new EntryRead(EntryKind.Prompt, "u1", "hello")));
        Assert.True(s.Flow.Working);

        // 4.1 "A background shell": a prompt Claude Code started by itself opens one as well, and the turn is said
        // to be one the user did not send (the test below).
        var self = new Session();
        Assert.Equal(["state working", "self-started"], self.At(100, new EntryRead(EntryKind.Prompt, "u2", "task", SelfStarted: true)));

        // With the mark first, the entry opens nothing more.
        var marked = new Session();
        Assert.Equal(["state working"], marked.At(5845, A));
        Assert.Empty(marked.At(6182, new EntryRead(EntryKind.Prompt, "u1", "hello")));

        // A turn the record opened is closed by the record (the tests below), not by a screen that shows nothing of
        // the work; when the record is lost, the screen is what is left (EG-4.5, "with none of it").
        Assert.Empty(s.At(6000, Frame(WorkSeen.Idle)));
        Assert.True(s.Flow.Working);
        Assert.Empty(s.At(7000, new RecordLost()));
        Assert.Equal(["state idle"], s.At(7100, Frame(WorkSeen.Idle)));

        // None is opened by an entry read within 150 of an end: it is that turn's, read late.
        var ended = new Session();
        ended.At(100, Entry(EntryKind.Prompt));
        ended.At(2000, Entry(EntryKind.TurnEnd));
        Assert.Equal(["note record-end after=2050", "state idle"], ended.At(2150, Tick));
        Assert.Empty(ended.At(2299, new EntryRead(EntryKind.Prompt, "u1", "hello")));
        Assert.False(ended.Flow.Working);
        Assert.Equal(["state working"], ended.At(2300, new EntryRead(EntryKind.Prompt, "u3", "more")));

        // In a run whose marks speak, a prompt read with no turn open opens none: every turn has its A before its
        // prompt is in the file. 4.1 "A hand-back note with no turn" as measured (M13 7): the turn's stop event
        // 14129, C and D and the bell 14152, the note's A 14241 and its C and D 14242 with no bell, the reply's
        // rows 14285, and the note's own entry, a prompt Claude Code started for the record alone, 14401: 159
        // after the note's D. A turn opened for it would have nothing to close it.
        var note = new Session();
        note.At(0, new RecordFound());
        note.At(12838, A);
        note.At(14129, new TurnStopped("done", []));
        note.At(14152, D);
        note.At(14152, Bell);
        note.At(14241, A);
        note.At(14242, C);
        note.At(14242, D);
        note.At(14285, Frame(WorkSeen.Idle, added: true, proposed: Head(31, "claude: done")));
        Assert.Empty(note.At(14392, Tick));
        Assert.Empty(note.At(14401, new EntryRead(EntryKind.Prompt, "u9", "tasknotification", SelfStarted: true)));
        Assert.Empty(note.At(20000, Tick));
        Assert.False(note.Flow.Working || note.Flow.Ending);
        Assert.Equal(
            [
                "12838 state working", "14152 state idle", "14241 state working", "14242 state idle",
                "14285 speak lines=1 letters=4", "14285 done turn=1 after=133",
            ],
            note.Said);
    }

    [Fact]
    public void A_turn_the_program_began_by_itself_is_said_so_once()
    {
        // 4.1 "A background shell", as measured (the background-shell timeline): the shell ends, Claude Code starts
        // a turn by itself, A 23964, and the turn's prompt, of the source system, is in the file 73 later. EG-4.6:
        // the turn is said to be self-started when that prompt is read while the turn is open, once.
        static EntryRead Self(string id) => new(EntryKind.Prompt, id, null, SelfStarted: true);
        var s = new Session();
        s.At(0, new RecordFound());
        Assert.Equal(["state working"], s.At(23964, A));
        Assert.Equal(["self-started"], s.At(24037, Self("u5")));
        Assert.Equal(s.Flow.Generation, Assert.IsType<TurnSelfStarted>(s.Last[0]).Generation);

        // Not twice in one turn: a second such prompt read in it says nothing more.
        Assert.Empty(s.At(24500, Self("u6")));
        s.At(25395, Stop);
        Assert.Equal(["state idle"], s.At(25408, D));
        s.At(25409, Bell);
        Assert.Equal(["done turn=1 after=600"], s.At(26009, Tick));

        // The next turn the program begins is said again: the flag is the turn's.
        Assert.Equal(["state working"], s.At(40725, A));
        Assert.Equal(["self-started"], s.At(40800, Self("u7")));

        // A prompt the user typed, or one that waited in the queue, says nothing: open by the mark, or opened by
        // the entry itself in a run without marks.
        var typed = new Session();
        typed.At(0, new RecordFound());
        typed.At(5845, A);
        Assert.Empty(typed.At(6182, new EntryRead(EntryKind.Prompt, "u1", "hello")));
        var unmarked = new Session();
        unmarked.At(0, new RecordFound());
        Assert.Equal(["state working"], unmarked.At(5800, new EntryRead(EntryKind.Prompt, "u1", "hello")));

        // In a run without marks the prompt itself opens the turn: the state first, then that the program began it.
        var opened = new Session();
        opened.At(0, new RecordFound());
        Assert.Equal(["state working", "self-started"], opened.At(100, Self("u2")));
        Assert.Empty(opened.At(200, Self("u3")));

        // A turn the screen's sign of work opened, and the prompt read inside it: said as well.
        var screen = new Session();
        screen.At(0, new RecordFound());
        Assert.Equal(["state working"], screen.At(100, Frame(WorkSeen.Working)));
        Assert.Equal(["self-started"], screen.At(180, Self("u2")));

        // With no turn open in a run whose marks speak, nothing: the prompt is a turn's that has ended (read after
        // its end mark and its bell), or a note's for the record alone (the test above, the hand-back note).
        var late = new Session();
        late.At(0, new RecordFound());
        late.At(1000, A);
        late.At(1990, Stop);
        late.At(2000, D);
        late.At(2001, Bell);
        Assert.Empty(late.At(2100, Self("u4")));
        Assert.Equal(["done turn=1 after=600"], late.At(2601, Tick));
        Assert.Empty(late.At(5000, Self("u5")));
        Assert.False(late.Flow.Working || late.Flow.Ending);
        Assert.DoesNotContain(late.Said, line => line.EndsWith("self-started", StringComparison.Ordinal));

        // Between the end mark and its bell the turn is closing, not open: nothing either.
        var closing = new Session();
        closing.At(0, new RecordFound());
        closing.At(1000, A);
        closing.At(2000, D);
        Assert.Empty(closing.At(2050, Self("u4")));

        // Whatever the order of the sources: the prompt read before the start mark, in a run that has shown marks,
        // opens nothing and says nothing; the turn's own mark then opens it, unsaid. Never a turn said twice.
        var early = new Session();
        early.At(0, new RecordFound());
        early.At(100, A);
        early.At(200, D);
        early.At(201, Bell);
        early.At(900, Tick);
        Assert.Empty(early.At(5000, Self("u8")));
        Assert.Equal(["state working"], early.At(5010, A));
        Assert.Empty(early.At(5020, new EntryRead(EntryKind.ToolCall, "t1")));
        Assert.DoesNotContain(early.Said, line => line.EndsWith("self-started", StringComparison.Ordinal));
    }

    [Fact]
    public void The_records_turn_end_closes_a_turn_whose_D_was_missed()
    {
        // 4.1 "A turn ends": the last reply's entry, then the record's turn end. EG-4.6: it closes a turn that is
        // still open BellAfterEndMs after it was read (a missed mark).
        var s = new Session();
        s.At(0, new RecordFound());
        s.At(1000, A);
        s.At(1300, Entry(EntryKind.Prompt));
        Assert.Empty(s.At(3000, Entry(EntryKind.ReplyText)));
        Assert.Empty(s.At(3001, Entry(EntryKind.TurnEnd)));
        Assert.Equal([3151], s.Wakes);
        Assert.Empty(s.At(3150, Tick));
        Assert.Equal(["note record-end after=2151", "state idle"], s.At(3151, Tick));
        Assert.DoesNotContain(s.Said, line => line.Contains("done"));

        // A spinner the screen still shows right after such an end opens nothing, as after an end by the marks.
        Assert.Empty(s.At(3200, Frame(WorkSeen.Working)));
        Assert.Equal(["state working"], s.At(3551, Frame(WorkSeen.Working)));

        // The end mark comes within the 150 after all: the marks decide, and the record's end confirms Done.
        var marked = new Session();
        marked.At(1000, A);
        marked.At(3001, Entry(EntryKind.TurnEnd));
        Assert.Equal(["state idle"], marked.At(3050, D));
        marked.At(3051, Bell);
        Assert.Equal(["done turn=1 after=29"], marked.At(3080, Frame(WorkSeen.Idle, added: true)));
        Assert.Empty(marked.At(3151, Tick));

        // 4.1 "A queued message taken up": the turn end of the turn before is read 105 after its bell, 55 after
        // the A of the waiting message's turn, and must not close that turn.
        var queued = new Session();
        queued.At(5085, A);
        queued.At(12433, Stop);
        queued.At(12445, D);
        queued.At(12446, Bell);
        queued.At(12452, Frame(WorkSeen.Working, added: true));
        Assert.Equal(["state working"], queued.At(12496, A));
        Assert.Empty(queued.At(12551, Entry(EntryKind.TurnEnd)));
        Assert.Empty(queued.At(12701, Tick));
        Assert.Empty(queued.At(13000, Tick));
        Assert.True(queued.Flow.Working);
    }

    [Fact]
    public void An_interrupt_or_a_cut_reply_in_the_record_closes_a_turn_no_mark_opened()
    {
        // 4.1 "An interrupt (Escape)": a cut assistant entry, then the interrupt's user entry. EG-4.5: in the
        // record they stand in for D without its bell; where the marks spoke for the turn, the marks decide.
        var s = new Session();
        s.At(0, new RecordFound());
        Assert.Equal(["state working"], s.At(100, Entry(EntryKind.Prompt)));
        Assert.Equal(["note record-cut after=1900", "state idle"], s.At(2000, Entry(EntryKind.ReplyText, cut: true)));
        Assert.Empty(s.At(2000, Entry(EntryKind.Interrupt)));
        Assert.Equal(1, s.Health.TurnsInterrupted);

        // 4.1 "Send now": the cut entry of the turn before is read 82 after the A of the queued turn, which stays open.
        var marked = new Session();
        marked.At(7409, A);
        Assert.Empty(marked.At(7491, Entry(EntryKind.ReplyText, cut: true)));
        Assert.Empty(marked.At(7491, Entry(EntryKind.Interrupt)));
        Assert.True(marked.Flow.Working);
    }

    [Fact]
    public void A_failed_turn_closes_a_second_after_its_event_when_no_marks_close_it()
    {
        // EG-4.5 "A turn failed": the failure event, then FailedCloseMs without marks.
        var s = new Session();
        s.At(0, A);
        Assert.Empty(s.At(500, new TurnFailed()));
        Assert.Equal([1500], s.Wakes);
        Assert.Empty(s.At(1499, Tick));
        Assert.Equal(["note failed-turn after=1500", "state idle"], s.At(1500, Tick));

        // With its marks the turn ends as any other: the error's row, and Done once the end counts as confirmed
        // (a failure is no stop event).
        var marked = new Session();
        marked.At(0, A);
        marked.At(500, new TurnFailed());
        Assert.Equal(["state idle"], marked.At(520, D));
        marked.At(521, Bell);
        Assert.Empty(marked.At(560, Frame(WorkSeen.Idle, added: true)));
        Assert.Equal(["done turn=1 after=300"], marked.At(821, Tick));
        Assert.Empty(marked.At(1500, Tick));
    }

    [Fact]
    public void The_process_end_closes_everything_and_drops_Done()
    {
        // 4.1 "A session ends". EG-4.6: the process's end closes everything; a Done not yet said is dropped.
        var s = new Session();
        s.At(0, new ProcessStarted());
        s.At(100, A);
        s.At(2000, D);
        s.At(2001, Bell);
        Assert.True(s.Flow.Ending);
        Assert.Empty(s.At(2010, new ProcessEnded()));
        Assert.False(s.Flow.Ending);
        Assert.Empty(s.At(2050, Frame(WorkSeen.Idle, added: true)));
        Assert.Empty(s.At(2601, Tick));
        Assert.DoesNotContain(s.Said, line => line.Contains("done"));

        // An open turn is closed, and the next run counts its turns from one, with no mark seen yet.
        var open = new Session();
        open.At(0, A);
        Assert.Equal(["state idle"], open.At(500, new ProcessEnded()));
        Assert.False(open.Flow.Working);
        open.At(600, new ProcessStarted());
        Assert.Equal(["bell attention no-marks"], open.At(700, Bell));
        open.At(800, A);
        open.At(900, D);
        open.At(901, Bell);
        Assert.Equal(["done turn=1 after=600"], open.At(1501, Tick));
    }

    [Fact]
    public void A_new_epoch_forgets_the_turn()
    {
        // 4.1 "/clear": the model empties the conversation and its epoch rises (EG-1.3, EG-4.9): the turn is
        // closed without a word, and the generation keeps rising.
        var s = new Session();
        s.At(0, Frame(WorkSeen.Idle, epoch: 1));
        s.At(1000, A);
        s.At(1100, Frame(WorkSeen.Working, epoch: 1));
        var generation = s.Flow.Generation;
        Assert.Equal(["state idle"], s.At(1500, Frame(WorkSeen.Idle, epoch: 2)));
        Assert.False(s.Flow.Working);
        Assert.False(s.Flow.Ending);
        Assert.True(s.Flow.Generation > generation);

        // A Done that was owed goes with the turn.
        s.At(2000, A);
        s.At(2500, D);
        s.At(2501, Bell);
        Assert.True(s.Flow.Ending);
        Assert.Empty(s.At(2550, Frame(WorkSeen.Idle, added: true, epoch: 3)));
        Assert.False(s.Flow.Ending);
        Assert.Empty(s.At(3200, Tick));
        Assert.DoesNotContain(s.Said, line => line.Contains("done"));

        // The same epoch again and again changes nothing.
        var same = new Session();
        same.At(0, Frame(WorkSeen.Idle, epoch: 4));
        same.At(100, A);
        Assert.Empty(same.At(200, Frame(WorkSeen.Working, epoch: 4)));
        Assert.True(same.Flow.Working);

        // 4.1 "/clear": a new file at once. With no event to name it, the record followed is the old
        // conversation's: it is forgotten, and the window looks again.
        var record = new Session();
        record.At(0, new RecordFound());
        record.At(10, Frame(WorkSeen.Idle, epoch: 1));
        Assert.Equal(["forget"], record.At(500, Frame(WorkSeen.Idle, epoch: 2)));

        // With events the session's naming says which file (the next test), in whichever order the two arrive.
        var named = new Session();
        named.At(0, new SessionNamed("s1", @"C:\Temp\projects\C--Temp-axclaude-fixture\s1.jsonl", SessionSource.Startup));
        named.At(5, new RecordFound());
        named.At(10, Frame(WorkSeen.Idle, epoch: 1));
        Assert.Empty(named.At(500, Frame(WorkSeen.Idle, epoch: 2)));
    }

    [Fact]
    public void The_state_stays_working_between_a_turn_and_a_waiting_message()
    {
        // 4.1 "A message sent while a turn runs" and "A queued message taken up", as measured: the enqueue in the
        // file 112 after the Enter, the hint row on the screen; the stop event, C and D, the bell, the story's last
        // rows 6 after the bell, the A of the queued turn 50 after the bell, the file's batch 55 after that A.
        // EG-4.6: the state stays working for BellAfterEndMs after such an end.
        var s = new Session();
        s.At(0, new RecordFound());
        s.At(5085, A);
        s.At(7177, Frame(WorkSeen.Working, waiting: 1));
        s.At(7278, Entry(EntryKind.Enqueued));
        Assert.Equal(1, s.Flow.Waiting);
        s.At(12433, Stop);
        Assert.Empty(s.At(12445, C));
        Assert.Empty(s.At(12445, D));
        Assert.True(s.Flow.Working);
        Assert.True(s.Flow.Ending);
        Assert.Empty(s.At(12446, Bell));

        // Done is said for the turn that finished, also when a queued turn follows at once (EG-D-14), behind the
        // reply the stop event named. The frame still shows work, the waiting message's: it counts as it is.
        Assert.Equal(["speak lines=1 letters=2", "done turn=1 after=6"], s.At(12452, Frame(WorkSeen.Working, added: true, waiting: 1, proposed: Head(40, "claude: ok"))));
        Assert.Empty(s.At(12496, A));
        Assert.Empty(s.At(12551, Entry(EntryKind.ReplyText)));
        Assert.Empty(s.At(12551, Entry(EntryKind.Dequeued)));
        Assert.Empty(s.At(12551, Entry(EntryKind.TurnEnd)));
        Assert.Empty(s.At(12551, Entry(EntryKind.Prompt)));
        Assert.Empty(s.At(12555, Frame(WorkSeen.Working, added: true)));
        Assert.Equal(0, s.Flow.Waiting);
        Assert.True(s.Flow.Working);

        // The queued turn's own end, with nothing waiting: idle at D.
        s.At(13392, Stop);
        Assert.Equal(["state idle"], s.At(13409, D));
        s.At(13409, Bell);
        Assert.Equal(["done turn=2 after=24"], s.At(13433, Frame(WorkSeen.Idle, added: true)));
        Assert.Equal(["5085 state working", "12452 speak lines=1 letters=2", "12452 done turn=1 after=6", "13409 state idle", "13433 done turn=2 after=24"], s.Said);

        // Without a record the screen's hint row says that a message waits; and when no start mark follows within
        // the 150, the state is idle then.
        var screen = new Session();
        screen.At(0, A);
        screen.At(100, Frame(WorkSeen.Working, waiting: 1));
        Assert.Empty(screen.At(1000, D));
        Assert.Equal([1150], screen.Wakes);
        Assert.True(screen.Flow.Working);
        Assert.Equal(["state idle"], screen.At(1150, Tick));
    }

    [Fact]
    public void A_Done_still_owed_is_said_when_the_next_turn_finishes()
    {
        // 4.1 "A queued message taken up" with a second turn shorter than Done's cap: one Done for each turn that
        // finished, in their order (EG-D-14), never one for two.
        var s = new Session();
        s.At(0, A);
        s.At(1000, D);
        s.At(1001, Bell);
        s.At(1050, A);
        Assert.Equal(["state idle"], s.At(1400, D));
        Assert.Equal(["done turn=1 after=400"], s.At(1401, Bell));
        Assert.Equal(["done turn=2 after=600"], s.At(2001, Tick));
        Assert.Equal(2, s.Health.TurnsFinished);
    }

    [Fact]
    public void Every_due_time_is_asked_for_with_WakeAt()
    {
        // EG-4.10: the core has no timer; what is due later is asked for, the earliest time alone.
        var s = new Session();
        s.At(0, A);
        Assert.Empty(s.Wakes);

        // The two seconds of an idle screen; a frame that shows work ends the wait, and nothing is asked.
        s.At(4, Frame(WorkSeen.Idle));
        s.At(50, Frame(WorkSeen.Working));
        Assert.Equal([2004], s.Wakes);

        // The bell's time after D; with the bell, the 300 of the unconfirmed end; then Done's cap.
        s.At(2000, D);
        s.At(2001, Bell);
        s.At(2301, Tick);
        Assert.Equal([2004, 2150, 2301, 2601], s.Wakes);
        Assert.Equal(["done turn=1 after=600"], s.At(2601, Tick));
        Assert.Equal([2004, 2150, 2301, 2601], s.Wakes);

        // Each wake carries the generation and prints as a number.
        var wake = new Session();
        wake.At(0, A);
        wake.At(2000, D);
        var asked = Assert.IsType<WakeAt>(wake.Last[^1]);
        Assert.Equal(2150, asked.AtMs);
        Assert.Equal("wake at=2150", asked.ToLine());

        // After a tick that came early the time is given again; nothing else happens.
        Assert.Empty(wake.At(2100, Tick));
        Assert.Equal([2150, 2150], wake.Wakes);

        // The failed turn's second, and the 150 after the record's turn end.
        var failed = new Session();
        failed.At(0, A);
        failed.At(500, new TurnFailed());
        Assert.Equal([1500], failed.Wakes);
        failed.At(700, Entry(EntryKind.TurnEnd));
        Assert.Equal([1500, 850], failed.Wakes);
    }

    [Fact]
    public void User_actions_a_shown_question_and_a_permission_event_raise_the_generation()
    {
        // EG-4.7. 4.1 "A tool's question opens": the permission event comes 9 to 13 before the question's bell.
        var s = new Session();
        var generation = s.Flow.Generation;
        foreach (var action in Enum.GetValues<UserAction>())
        {
            s.At(100, new UserActed(action));
            Assert.Equal(++generation, s.Flow.Generation);
        }

        s.At(200, new QuestionShown());
        Assert.Equal(++generation, s.Flow.Generation);
        s.At(300, new AttentionAsked(AttentionKind.Permission));
        Assert.Equal(++generation, s.Flow.Generation);

        // Nothing else raises it: a notification, the marks, the bell, a frame, an entry, a stop, a tick. Moving in
        // the conversation is no input at all.
        foreach (var input in new FlowInput[] { new AttentionAsked(AttentionKind.Notification), A, C, D, Bell, Frame(WorkSeen.Idle), Entry(EntryKind.Prompt), Stop, new WorkerStarted("a1", "x"), new WorkerStopped("a1", "x"), new SessionEnded("other"), new RecordFound(), new RecordLost(), Tick })
        {
            s.At(400, input);
            Assert.Equal(generation, s.Flow.Generation);
        }

        // Every decision carries the generation it was made in.
        var made = new Session();
        made.At(0, new UserActed(UserAction.Sent, "hello"));
        made.At(10, new QuestionShown());
        Assert.Equal(["state working"], made.At(50, A));
        Assert.Equal(made.Flow.Generation, made.Last[0].Generation);
        Assert.Equal(2, made.Last[0].Generation);
    }

    [Fact]
    public void A_session_named_asks_to_follow_its_file_and_clear_asks_again()
    {
        // 4.1 "A session starts": the start event names the session's file. "/compact", "--continue": the same
        // file. "/clear": the end event, then a start event with a new id and a new file (EG-4.9).
        const string first = @"C:\Temp\projects\C--Temp-axclaude-fixture\11111111.jsonl";
        const string second = @"C:\Temp\projects\C--Temp-axclaude-fixture\22222222.jsonl";
        var s = new Session();
        Assert.Equal(["follow"], s.At(0, new SessionNamed("11111111", first, SessionSource.Startup)));
        Assert.Equal(first, Assert.IsType<FollowRecord>(s.Last[0]).Path);
        s.At(10, new RecordFound());

        // The same file named again is followed already.
        Assert.Empty(s.At(20, new SessionNamed("11111111", first, SessionSource.Compact)));
        Assert.Empty(s.At(30, new SessionNamed("11111111", first, SessionSource.Resume)));

        s.At(40, Entry(EntryKind.Enqueued));
        Assert.Equal(1, s.Flow.Waiting);
        s.At(1000, A);
        var generation = s.Flow.Generation;
        Assert.Empty(s.At(2000, new SessionEnded("clear")));

        // Clear: the turn is forgotten, the count with it, and the new file is asked for.
        Assert.Equal(["state idle", "follow"], s.At(2010, new SessionNamed("22222222", second, SessionSource.Clear)));
        Assert.Equal(second, Assert.IsType<FollowRecord>(s.Last[1]).Path);
        Assert.True(s.Flow.Generation > generation);
        Assert.Equal(0, s.Flow.Waiting);

        // A new conversation whose file the event did not name in a usable way: the old record is forgotten.
        s.At(2020, new RecordFound());
        Assert.Equal(["forget"], s.At(3000, new SessionNamed("33333333", null, SessionSource.Fork)));
        Assert.Empty(s.At(3010, new SessionNamed("33333333", null, SessionSource.Startup)));
    }

    [Fact]
    public void Enqueued_minus_dequeued_is_the_waiting_count()
    {
        // 4.1 "A message sent while a turn runs": an enqueue in the file. "Two queued messages": one hint row
        // for both on the screen, two dequeues in the file. EG-6.6.
        var s = new Session();
        s.At(0, A);
        s.At(100, Frame(WorkSeen.Working, waiting: 1));
        Assert.Equal(1, s.Flow.Waiting);

        // While there is a record the count is the record's.
        s.At(200, new RecordFound());
        Assert.Equal(0, s.Flow.Waiting);
        s.At(300, Entry(EntryKind.Enqueued));
        s.At(400, Entry(EntryKind.Enqueued));
        Assert.Equal(2, s.Flow.Waiting);
        s.At(500, Entry(EntryKind.Dequeued));
        Assert.Equal(1, s.Flow.Waiting);
        s.At(500, Entry(EntryKind.Dequeued));
        Assert.Equal(0, s.Flow.Waiting);
        s.At(600, Entry(EntryKind.Dequeued));
        Assert.Equal(0, s.Flow.Waiting);

        // Without one it is the hint rows' again.
        s.At(700, new RecordLost());
        Assert.Equal(1, s.Flow.Waiting);
    }

    [Fact]
    public void A_proposed_line_outside_a_turn_is_dropped()
    {
        // 4.1 "--continue, --resume": the whole old conversation in one frame with no mark and no event; a repaint
        // at idle. EG-5.2: a proposed line is considered only while a turn is open or ending.
        var s = new Session();
        Assert.Empty(s.At(100, Frame(WorkSeen.Idle, added: true, proposed: [Head(2, "claude: An old reply."), Row(3, 2, "Its second row.")])));
        Assert.Equal(2, s.Health.GateOutsideTurn);
        Assert.Equal(0, s.Health.GateReleased);

        // Inside the turn (4.1 "Text before a tool call"): one decision for a frame's lines, in the order proposed.
        s.At(1000, A);
        Assert.Equal(["speak lines=2 letters=32"], s.At(1500, Frame(WorkSeen.Working, added: true, proposed: [ToolRow(5, "tool: Read (a.txt)"), ErrorRow(6, "error: API Error: 401 Invalid API key")])));
        var spoken = Assert.IsType<SpeakReply>(s.Last[0]);
        Assert.Equal([5, 6], spoken.Lines.Select(line => line.Id));

        // While the turn is ending: between D and its bell, and until Done.
        s.At(1990, Stop);
        s.At(2000, D);
        Assert.Equal(["speak lines=1 letters=7"], s.At(2000, Frame(WorkSeen.Working, proposed: ToolRow(7, "tool: Bash (dir)"))));
        s.At(2001, Bell);
        Assert.Equal(["speak lines=1 letters=8", "done turn=1 after=49"], s.At(2050, Frame(WorkSeen.Idle, added: true, proposed: ToolRow(8, "tool: Read (b.txt)"))));

        // After Done the turn's window is closed, for a reply's row and for a tool's alike.
        Assert.Empty(s.At(2500, Frame(WorkSeen.Idle, added: true, proposed: [Head(9, "claude: A repaint."), ToolRow(10, "tool: Read (a.txt)")])));
        Assert.Equal(4, s.Health.GateOutsideTurn);
    }

    [Fact]
    public void Decisions_print_as_numbers_and_fixed_words_only()
    {
        // 6.5: each decision's line for the log and the kept files; never a text of the conversation, never a path.
        const string path = @"C:\Temp\projects\C--Temp-axclaude-fixture\11111111.jsonl";
        var reply = new Line(412, LineKind.ClaudeReply, "claude: ok");
        var row = new Line(413, LineKind.Plain, "Note: 2 files");
        var task = new FlowTask("b0jdigiy3", "shell", "Run ping in background", null);
        var decisions = new (FlowDecision Decision, string Line)[]
        {
            (new StateChanged(3, FlowState.Working), "state working"),
            (new StateChanged(3, FlowState.Idle), "state idle"),
            (new AttentionBell(3, NoMarks: false), "bell attention"),
            (new AttentionBell(3, NoMarks: true), "bell attention no-marks"),
            (new TurnSelfStarted(3), "self-started"),
            (new SayDone(3, 1, 74), "done turn=1 after=74"),
            (new SpeakReply(3, [reply]), "speak lines=1 letters=2"),
            (new SpeakReply(3, [reply, row]), "speak lines=2 letters=12"),
            (new FollowRecord(3, path), "follow"),
            (new ForgetRecord(3), "forget"),
            (new EntryBound(3, "b8142ecf", 412), "bound head=412"),
            (new BackgroundListed(3, [task]), "background tasks=1"),
            (new WakeAt(3, 8160), "wake at=8160"),
            (new Note(3, "stale-turn after=2004"), "note stale-turn after=2004"),
        };
        foreach (var (decision, line) in decisions)
        {
            Assert.Equal(line, decision.ToLine());
            // Printed whole, by mistake or in a failed assertion, a decision gives the same line and nothing more.
            Assert.Equal(line, decision.ToString());
            Assert.Equal(3, decision.Generation);
        }

        // An input holds letters of the conversation and prints as its type's name alone.
        Assert.Equal("UserActed", new UserActed(UserAction.Sent, "thelettersofamessage").ToString());
        Assert.Equal("EntryRead", new EntryRead(EntryKind.ReplyText, "b8142ecf", "thelettersofareply").ToString());
        Assert.Equal("TurnStopped", new TurnStopped("thelettersofareply", [task]).ToString());
        Assert.Equal("SessionNamed", new SessionNamed("11111111", path, SessionSource.Startup).ToString());
        Assert.Equal("FrameSeen", Frame(WorkSeen.Idle, proposed: Head(1, "claude: text")).ToString());
    }

    [Fact]
    public void The_health_says_the_sources_in_numbers_and_fixed_words()
    {
        // EG-9.3 and EG-9.4: what Say the event sources says and what Copy diagnostics gains, from the counters.
        var health = new SourceHealth();
        Assert.Equal("Events. Marks: 0. Hooks: not passed. Session file: not found. Core: in shadow.", health.Said("in shadow"));

        health.TurnsFinished = 5;
        health.TurnsInterrupted = 1;
        health.MarksA = 5;
        health.MarksC = 4;
        health.MarksD = 4;
        health.Bells = 5;
        health.Hooks = HookUse.Passed;
        health.HooksReceived = 9;
        health.ProgramVersion = "2.1.325";
        health.TableVersion = "2.1.325";
        health.Record = RecordUse.Followed;
        health.EntriesRead = 212;
        health.LinesSkipped = 1803;
        health.GateReleased = 12;
        health.GateOutsideTurn = 3;
        health.GateUnconfirmed = 1;
        health.RepliesBound = 11;
        health.Pinned = 1;
        Assert.Equal("Events. Marks: 13. Hooks: on, 9 received. Session file: followed, 212 entries. Core: in charge of state, Done and speech.", health.Said("in charge of state, Done and speech"));
        Assert.Equal(
            [
                "Event core: in charge of state, Done, speech; turns 5 finished, 1 interrupted, 0 cut, 0 stale",
                "Marks: 13 (A 5, C 4, D 4), bells 5",
                "Hooks: on, 9 received, 0 refused, 0 malformed, 0 of another session; Claude Code 2.1.325, table 2.1.325",
                "Session file: followed, 212 entries read, 1803 lines skipped, 0 not JSON, 0 too long",
                "Gate: 12 released, 3 outside a turn, 1 unconfirmed, 0 stale, 0 pending",
                "Grounding: 11 replies bound, 1 pinned, 0 blocks moved, 0 bindings lost",
            ],
            health.Lines("in charge of state, Done, speech"));

        // The words of the hooks and of the session file (EG-9.3). Passed and none received by the end of the
        // first turn (EG-2.10) has its own.
        health.HooksReceived = 0;
        Assert.Equal("passed, none received", health.HooksWords);
        health.Hooks = HookUse.NotPassed;
        health.HooksReason = "version";
        Assert.Equal("not passed, version", health.HooksWords);
        health.Hooks = HookUse.Off;
        Assert.Equal("off", health.HooksWords);
        health.Record = RecordUse.Stopped;
        health.RecordReason = "json";
        Assert.Equal("stopped, json", health.RecordWords);
        health.Record = RecordUse.Off;
        Assert.Equal("off", health.RecordWords);

        // Before a turn has ended, nothing received is not yet a fault.
        var early = new SourceHealth { Hooks = HookUse.Passed };
        Assert.Equal("on, 0 received", early.HooksWords);

        // A reset keeps what the window said of the sources and clears the counts.
        health.Reset();
        Assert.Equal(0, health.Marks + health.TurnsEnded + health.EntriesRead + health.GateReleased + health.RepliesBound);
        Assert.Equal("off", health.HooksWords);

        // The core counts its own: the marks, the bells, the turns by their ends.
        var s = new Session();
        s.At(0, A);
        s.At(10, C);
        s.At(10, D);
        s.At(11, Bell);
        s.At(5000, Bell);
        Assert.Equal((1, 1, 1, 2, 1), (s.Health.MarksA, s.Health.MarksC, s.Health.MarksD, s.Health.Bells, s.Health.TurnsFinished));
    }
}
