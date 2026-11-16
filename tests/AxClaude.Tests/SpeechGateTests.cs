using AxClaude.Core.Flow;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The gate of reply speech (docs/axClaude/events-and-grounding.md EG-5.3 to EG-5.5, EG-5.7, EG-5.9, and Done's
/// first condition in EG-5.6): each test a list of inputs with their times and the decisions' lines, as in
/// <see cref="SessionFlowTests"/>. The keys of the entries and of the stop events are made as the window makes
/// them, with Claude Code's own rules (<see cref="ClaudeCodeTranscript.ScreenKey"/>); the texts are invented.
/// Times are milliseconds; where a moment of section 4.1 was measured, the test names it and uses its times.
/// </summary>
public class SpeechGateTests
{
    private static readonly MarkSeen A = new(TurnMark.Start);
    private static readonly MarkSeen D = new(TurnMark.End);
    private static readonly BellSeen Bell = new();
    private static readonly Tick Tick = new();

    /// <summary>One core with its health. <see cref="At"/> feeds an input and gives the lines of its decisions, wake times apart.</summary>
    private sealed class Session
    {
        public readonly SourceHealth Health = new();
        public readonly SessionFlow Flow;

        /// <summary>Every decision so far but the wake times, each as its time and its line: <c>8086 speak lines=1 letters=2</c>.</summary>
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
        /// time the core asked to be woken at, as the window's one timer sends them, and ticks after the last input
        /// until nothing is due.
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

        /// <summary>The line ids of the one speak decision of the last input, in its order.</summary>
        public int[] Spoken() => [.. Assert.Single(Last.OfType<SpeakReply>()).Lines.Select(line => line.Id)];

        /// <summary>The one binding the last input reported.</summary>
        public EntryBound Bound() => Assert.Single(Last.OfType<EntryBound>());
    }

    /// <summary>A session with a turn open since 100 by its start mark, with a record (found at 0) or without one.</summary>
    private static Session Turn(bool record = true)
    {
        var s = new Session();
        if (record)
        {
            s.At(0, new RecordFound());
        }

        s.At(100, A);
        return s;
    }

    private static FrameSeen Frame(WorkSeen work, bool added = true, bool speech = true, int epoch = 1, params Proposed[] proposed) =>
        new(epoch, work, Asking: false, added, speech, WaitingShown: 0, proposed);

    private static Proposed Head(int id, string text) => new(new Line(id, LineKind.ClaudeReply, text), id, ProposedKind.Head);

    private static Proposed Row(int id, int head, string text) => new(new Line(id, LineKind.Plain, text), head, ProposedKind.Row);

    private static Proposed ToolRow(int id, string text) => new(new Line(id, LineKind.Tool, text), id, ProposedKind.Tool);

    private static Proposed ErrorRow(int id, string text) => new(new Line(id, LineKind.Error, text), id, ProposedKind.Error);

    private static Proposed UnderError(int id, int head, string text) => new(new Line(id, LineKind.Plain, text), head, ProposedKind.Error);

    /// <summary>A reply text entry of the record, with the key the window gives it: the screen key of its source.</summary>
    private static EntryRead Reply(string id, string source) => new(EntryKind.ReplyText, id, ClaudeCodeTranscript.ScreenKey(source));

    /// <summary>A prompt entry: the next turn's message as the record holds it.</summary>
    private static EntryRead Prompt() => new(EntryKind.Prompt, "prompt", "thenextmessage");

    /// <summary>A stop event with the key of the turn's last message, or with none.</summary>
    private static TurnStopped Stop(string? lastMessage) => new(lastMessage is null ? null : ClaudeCodeTranscript.ScreenKey(lastMessage), []);

    [Fact]
    public void Without_a_record_a_line_is_released_after_250_ms_inside_a_turn()
    {
        // EG-5.4: no record (grounding off, the file not found), so the gate is open: today's hold of 250, inside a turn.
        var s = Turn(record: false);
        Assert.Empty(s.At(1000, Frame(WorkSeen.Working, proposed: [Head(5, "claude: ok"), Row(6, 5, "A second row.")])));
        Assert.Equal(2, s.Health.GatePending);
        Assert.Equal(1250, s.Wakes[^1]);

        Assert.Empty(s.At(1249, Tick));
        Assert.Equal(["speak lines=2 letters=12"], s.At(1250, Tick));
        Assert.Equal([5, 6], s.Spoken());
        Assert.Equal(2, s.Health.GateReleased);
        Assert.Equal(0, s.Health.GatePending);

        // Outside a turn the same line is dropped at once, record or none (EG-5.2): a repaint at idle.
        var idle = new Session();
        Assert.Empty(idle.At(1000, Frame(WorkSeen.Idle, proposed: Head(5, "claude: ok"))));
        Assert.Equal(1, idle.Health.GateOutsideTurn);
        Assert.Equal(0, idle.Health.GatePending);
        Assert.Empty(idle.Wakes);
    }

    [Fact]
    public void A_head_waits_for_its_entry_and_is_released_when_it_arrives()
    {
        // 4.1 "Text before a tool call": the rows in one chunk, the text's entry 68 to 82 after the rows.
        var s = Turn();
        Assert.Empty(s.At(1000, Frame(WorkSeen.Working, proposed: Head(412, "claude: I will read the file first."))));
        Assert.Equal(1, s.Health.GatePending);
        Assert.Equal(1500, s.Wakes[^1]);

        Assert.Equal(["speak lines=1 letters=21", "bound head=412"], s.At(1075, Reply("b8142ecf", "I will read the file first.")));
        Assert.Equal([412], s.Spoken());
        Assert.Equal(1, s.Health.GateReleased);
        Assert.Equal(0, s.Health.GatePending);

        // Nothing is left for the cap.
        Assert.Empty(s.At(1500, Tick));
        Assert.Equal(0, s.Health.GateUnconfirmed);
    }

    [Fact]
    public void The_stop_events_last_message_confirms_the_head_before_the_entry()
    {
        // 4.1 "A turn ends" with a hook the program waits for: the stop event 22 before D with the last message's
        // text, the bell 1 after D, the reply's rows 75 after the bell, the reply's entry 110 after the bell
        // (EG-D-11). The head is spoken with its rows, and Done follows in the same step.
        var s = Turn();
        Assert.Empty(s.At(7988, Stop("ok")));
        s.At(8010, D);
        s.At(8011, Bell);

        Assert.Equal(["speak lines=1 letters=2", "done turn=1 after=75"], s.At(8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok"))));
        Assert.Equal(1, s.Health.GateReleased);

        // The last message has no id: nothing is bound until the entry comes.
        Assert.DoesNotContain(s.Last, decision => decision is EntryBound);

        // A stop event without a last message (a turn that ended on a tool call) confirms nothing.
        var none = Turn();
        none.At(7988, Stop(null));
        Assert.Empty(none.At(8000, Frame(WorkSeen.Working, proposed: Head(412, "claude: ok"))));
        Assert.Equal(1, none.Health.GatePending);
    }

    [Fact]
    public void The_entry_that_follows_takes_the_provisional_records_place()
    {
        var s = Turn();
        s.At(7988, Stop("ok"));
        s.At(8010, D);
        s.At(8011, Bell);
        s.At(8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok")));

        // 4.1: the last reply's entry 51 to 190 after the bell. It is the reply the head took: bound now, with the entry's id.
        Assert.Equal(["bound head=412"], s.At(8121, Reply("b8142ecf", "ok")));
        Assert.Equal("b8142ecf", s.Bound().EntryId);

        // One reply, not two: in the next turn a new row with the same text finds no candidate.
        s.At(9000, A);
        Assert.Empty(s.At(9500, Frame(WorkSeen.Working, proposed: Head(500, "claude: ok"))));
        Assert.Empty(s.At(10000, Tick));
        Assert.Equal(1, s.Health.GateUnconfirmed);
        Assert.Equal(1, s.Health.GateReleased);

        // It keeps what was matched. 4.3: once the rows came before the marks; here the stop event, the first two
        // rows, the entry, then the third row.
        const string source = "The first row.\nThe second row.\nThe third row.";
        var rows = Turn();
        rows.At(1990, Stop(source));
        Assert.Equal(["speak lines=2 letters=23"], rows.At(1995, Frame(WorkSeen.Working, proposed: [Head(412, "claude: The first row."), Row(413, 412, "The second row.")])));
        Assert.Equal(["bound head=412"], rows.At(2100, Reply("b8142ecf", source)));
        Assert.Equal(["speak lines=1 letters=11"], rows.At(2110, Frame(WorkSeen.Working, proposed: Row(414, 412, "The third row."))));

        // The place stands behind the third row: the first row's letters, in front of it, are not found again.
        Assert.Empty(rows.At(2120, Frame(WorkSeen.Working, proposed: Row(415, 412, "The first row."))));
        Assert.Equal(1, rows.Health.GatePending);
    }

    [Fact]
    public void A_stop_event_that_comes_after_its_entry_makes_no_second_reply()
    {
        // 4.3, measured by WP-A3: with hooks the program does not wait for, the stop event arrives some 500 after
        // D, after the reply's entry and the record's turn end. The record holds the reply already.
        var s = Turn();
        s.At(8010, D);
        s.At(8011, Bell);
        s.At(8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok")));
        Assert.Equal(["speak lines=1 letters=2", "bound head=412"], s.At(8121, Reply("b8142ecf", "ok")));
        Assert.Equal(["done turn=1 after=110"], s.At(8121, new EntryRead(EntryKind.TurnEnd, "t1")));
        Assert.Empty(s.At(8500, Stop("ok")));

        // So a repaint of the reply in the next turn, a new row with the same text, is confirmed by nothing.
        s.At(9000, A);
        Assert.Empty(s.At(9100, Frame(WorkSeen.Working, proposed: Head(500, "claude: ok"))));
        Assert.Empty(s.At(9600, Tick));
        Assert.Equal(1, s.Health.GateUnconfirmed);
        Assert.Equal(1, s.Health.GateReleased);

        // Later still (a loaded computer): after the next turn's prompt was read and its replies were closed.
        var late = Turn();
        late.At(1000, Reply("e1", "ok"));
        late.At(1010, Frame(WorkSeen.Working, proposed: Head(1, "claude: ok")));
        late.At(2000, Prompt());
        Assert.Empty(late.At(2600, Stop("ok")));
        Assert.Empty(late.At(2700, Frame(WorkSeen.Working, proposed: Head(9, "claude: ok"))));
        Assert.Empty(late.At(3200, Tick));
        Assert.Equal(1, late.Health.GateUnconfirmed);
        Assert.Equal(1, late.Health.GateReleased);
    }

    [Fact]
    public void A_head_no_entry_confirms_is_dropped_at_500_ms()
    {
        // EG-5.4: a repaint, a replay, a row of the program's own. The record holds no such text.
        var s = Turn();
        Assert.Empty(s.At(1000, Frame(WorkSeen.Working, proposed: Head(7, "claude: A repaint of an old reply."))));
        Assert.Equal(1500, s.Wakes[^1]);

        Assert.Empty(s.At(1499, Tick));
        Assert.Equal(1, s.Health.GatePending);
        Assert.Empty(s.At(1500, Tick));
        Assert.Equal(0, s.Health.GatePending);
        Assert.Equal(1, s.Health.GateUnconfirmed);

        // An entry that comes after the cap (every entry a second late) speaks nothing and binds nothing.
        Assert.Empty(s.At(2075, Reply("e1", "A repaint of an old reply.")));
        Assert.Equal(0, s.Health.GateReleased);
    }

    [Fact]
    public void An_entry_releases_once()
    {
        // 4.3: once the file came before the screen. The head finds its entry there and is released at once.
        var s = Turn();
        Assert.Empty(s.At(1000, Reply("e1", "ok")));
        Assert.Equal(["speak lines=1 letters=2", "bound head=7"], s.At(1010, Frame(WorkSeen.Working, proposed: Head(7, "claude: ok"))));

        // A repaint: the same text on a new row. The entry was taken.
        Assert.Empty(s.At(1200, Frame(WorkSeen.Working, proposed: Head(9, "claude: ok"))));
        Assert.Empty(s.At(1700, Tick));
        Assert.Equal(1, s.Health.GateReleased);
        Assert.Equal(1, s.Health.GateUnconfirmed);
    }

    [Fact]
    public void An_entry_read_before_a_later_prompt_is_never_a_candidate()
    {
        // A reply whose head was never proposed, and the next turn's prompt behind it: once 500 have passed since
        // the prompt, a head with that text (the earlier turn drawn again) is confirmed by nothing.
        var s = Turn();
        s.At(1000, Reply("e1", "ok"));
        s.At(2000, Prompt());
        Assert.Empty(s.At(2500, Frame(WorkSeen.Working, proposed: Head(7, "claude: ok"))));
        Assert.Empty(s.At(3000, Tick));
        Assert.Equal(1, s.Health.GateUnconfirmed);
        Assert.Equal(0, s.Health.GateReleased);

        // Or sooner, once the next turn's first reply text has been read: that one is the candidate now.
        var next = Turn();
        next.At(1000, Reply("e1", "ok"));
        next.At(2000, Prompt());
        next.At(2100, Reply("e2", "Something else."));
        Assert.Empty(next.At(2200, Frame(WorkSeen.Working, proposed: Head(7, "claude: ok"))));
        Assert.Equal(["speak lines=1 letters=13", "bound head=8"], next.At(2210, Frame(WorkSeen.Working, proposed: Head(8, "claude: Something else."))));
        Assert.Equal("e2", next.Bound().EntryId);
        Assert.Empty(next.At(2700, Tick));
        Assert.Equal(1, next.Health.GateUnconfirmed);
    }

    [Fact]
    public void A_head_drawn_after_its_entry_and_the_next_prompt_is_still_confirmed()
    {
        // 4.1 "A queued message taken up" as measured: the start mark of the waiting message's turn 45 to 53 after
        // the bell; the last reply's entry, the record's turn end and the waiting message's prompt read in one
        // batch; and the file ahead of the screen (4.3: once, by 65), so the reply's rows are drawn after all three.
        var s = Turn();
        s.At(12400, D);
        s.At(12401, Bell);
        s.At(12450, A);
        Assert.Empty(s.At(12551, Reply("e1", "The first reply.")));
        Assert.Empty(s.At(12551, new EntryRead(EntryKind.TurnEnd, "t1")));
        Assert.Empty(s.At(12551, Prompt()));

        Assert.Equal(["speak lines=1 letters=13", "bound head=412", "done turn=1 after=215"],
            s.At(12616, Frame(WorkSeen.Working, proposed: Head(412, "claude: The first reply."))));
        Assert.Equal("e1", s.Bound().EntryId);
        Assert.Equal(1, s.Health.GateReleased);
        Assert.Equal(0, s.Health.GateUnconfirmed);
    }

    [Fact]
    public void Rows_under_a_head_are_released_when_their_letters_follow_in_the_key()
    {
        var s = Turn();
        s.At(1000, Reply("e1", "The plan has three steps.\n\n1. Read the file.\n2. Change the line.\n\n---\n\n3. Run the tests."));

        // The head, a row that follows it in the key, and a row without a letter, which goes with its head.
        Assert.Equal(["speak lines=3 letters=32", "bound head=1"],
            s.At(1010, Frame(WorkSeen.Working, proposed: [Head(1, "claude: The plan has three steps."), Row(2, 1, "1. Read the file."), Row(3, 1, "---")])));
        Assert.Equal([1, 2, 3], s.Spoken());

        // A row of a later frame is found behind the place matched so far, a few letters further on: the row
        // between was not proposed.
        Assert.Equal(["speak lines=1 letters=12"], s.At(1100, Frame(WorkSeen.Working, proposed: Row(5, 1, "3. Run the tests."))));

        // The place has moved behind it: the row in front of it is not found any more.
        Assert.Empty(s.At(1110, Frame(WorkSeen.Working, proposed: Row(4, 1, "2. Change the line."))));
        Assert.Equal(1, s.Health.GatePending);

        // A short row is looked for no more than 80 letters further on; a row of 16 letters or more however far.
        const string between = "The pond fills with frog spawn in the second week of March, and the blackbirds start to sing before the light comes up.";
        Assert.True(RecordText.Letters(between).Length > 80);
        var far = Turn();
        far.At(1000, Reply("e2", $"Start here.\n\n{between}\n\nA short row.\n\nA much longer row of words."));
        Assert.Equal(["speak lines=2 letters=30", "bound head=1"],
            far.At(1010, Frame(WorkSeen.Working, proposed: [Head(1, "claude: Start here."), Row(2, 1, "A short row."), Row(3, 1, "A much longer row of words.")])));
        Assert.Equal([1, 3], far.Spoken());
        Assert.Empty(far.At(1510, Tick));
        Assert.Equal(1, far.Health.GateUnconfirmed);
    }

    [Fact]
    public void A_row_that_is_not_in_the_entry_is_dropped()
    {
        // 4.1 "A tool's question opens": the text before the tool call has its entry; the question's rows, drawn
        // under the reply, stand in no entry. The window says the question; the gate never speaks its rows.
        var s = Turn();
        s.At(1000, Reply("e1", "I will edit the file."));
        Assert.Equal(["speak lines=1 letters=16", "bound head=1"], s.At(1010, Frame(WorkSeen.Working, proposed:
            [Head(1, "claude: I will edit the file."), Row(2, 1, "Do you want to make this edit to notes.txt?"), Row(3, 1, "Esc to cancel")])));
        Assert.Equal(2, s.Health.GatePending);

        Assert.Empty(s.At(1510, Tick));
        Assert.Equal(0, s.Health.GatePending);
        Assert.Equal(2, s.Health.GateUnconfirmed);
        Assert.Equal(1, s.Health.GateReleased);
    }

    [Fact]
    public void A_wrapped_row_and_a_table_row_are_found()
    {
        // 4.3: a paragraph wrapped at the console width at a space, and a table drawn as one row for each body
        // row, "Header: cell. Header: cell.", without its header and separator lines.
        const string first = "Every spring the garden wakes slowly, first with the green tips of the garlic that went into the ground in October, then with the crocus by the gate and the rhubarb that pushes its red fists up through the mulch. The pond fills with frog";
        const string second = "spawn in the second week of March, and the blackbirds start to sing before the light comes up.";
        Assert.True(first.Length <= 240);
        var source = string.Join('\n', "The beds this year:", "", "| Bed | Plant |", "| --- | --- |", "| North | Tomatoes |", "| East | Beans |", "", first + " " + second);

        var s = Turn();
        s.At(1000, Reply("e1", source));
        var lines = s.At(1010, Frame(WorkSeen.Working, proposed:
        [
            Head(1, "claude: The beds this year:"),
            Row(2, 1, "Bed: North. Plant: Tomatoes."),
            Row(3, 1, "Bed: East. Plant: Beans."),
            Row(4, 1, first),
            Row(5, 1, second),
        ]));

        Assert.Equal([1, 2, 3, 4, 5], s.Spoken());
        Assert.Equal("bound head=1", lines[^1]);
        Assert.Equal(0, s.Health.GatePending);
    }

    [Fact]
    public void A_summary_of_an_earlier_reply_is_dropped()
    {
        // EG-8.3: in a later turn the program redraws the head of an earlier reply as its own summary of it. The
        // row is the same line with another text, and the summary starts with the reply's own first 24 letters.
        var head = new Line(7, LineKind.ClaudeReply, "claude: I fixed the failing test in the parser and ran the whole suite again.");
        var s = Turn();
        s.At(1000, Reply("e1", "I fixed the failing test in the parser and ran the whole suite again.\n\nIt passes."));
        Assert.Equal(["speak lines=1 letters=55", "bound head=7"], s.At(1010, Frame(WorkSeen.Working, proposed: new Proposed(head, 7, ProposedKind.Head))));

        s.At(2000, Prompt());
        head.Text = "claude: I fixed the failing test in the parser · summary";

        // The entry was taken by this very head: it releases once. A row under the summary, though its letters
        // follow in the entry's key, waits with its head and goes with it.
        Assert.Empty(s.At(3000, Frame(WorkSeen.Working, proposed: [new Proposed(head, 7, ProposedKind.Head), Row(8, 7, "It passes.")])));
        Assert.Equal(2, s.Health.GatePending);
        Assert.Empty(s.At(3500, Tick));
        Assert.Equal(2, s.Health.GateUnconfirmed);
        Assert.Equal(1, s.Health.GateReleased);
    }

    [Fact]
    public void A_reply_of_two_letters_is_confirmed_only_at_an_entrys_start()
    {
        // "ok" stands inside "look at this": a head is compared with the start of a key, never found within one.
        var s = Turn();
        s.At(1000, Reply("e1", "Look at this."));
        Assert.Empty(s.At(1010, Frame(WorkSeen.Working, proposed: Head(1, "claude: ok"))));
        Assert.Equal(1, s.Health.GatePending);

        // Its own entry comes: the first candidate in the record's order whose key starts with the head's letters.
        Assert.Equal(["speak lines=1 letters=2", "bound head=1"], s.At(1100, Reply("e2", "ok")));
        Assert.Equal("e2", s.Bound().EntryId);

        // A head longer than its entry is not that entry's: "ok then" against "ok".
        var longer = Turn();
        longer.At(1000, Reply("e1", "ok"));
        Assert.Empty(longer.At(1010, Frame(WorkSeen.Working, proposed: Head(1, "claude: ok then"))));
        Assert.Equal(1, longer.Health.GatePending);
    }

    [Fact]
    public void Error_rows_and_tool_rows_are_released_at_once()
    {
        // EG-5.5, EG-D-24: they stand in no reply's entry. With a record that holds nothing of them:
        var s = Turn();
        Assert.Equal(["speak lines=3 letters=46"], s.At(1000, Frame(WorkSeen.Working, proposed:
            [ToolRow(5, "tool: Read (a.txt)"), ErrorRow(6, "error: API Error: 401 Invalid API key"), UnderError(7, 6, "Please run /login")])));
        Assert.Equal([5, 6, 7], s.Spoken());
        Assert.Equal(3, s.Health.GateReleased);
        Assert.Equal(0, s.Health.GatePending);
        Assert.Empty(s.Wakes);

        // Without a record they do not wait for the hold either.
        var open = Turn(record: false);
        Assert.Equal(["speak lines=1 letters=8"], open.At(1000, Frame(WorkSeen.Working, proposed: ToolRow(5, "tool: Read (a.txt)"))));

        // A reply's head that still waits does not hold the tool row behind it back.
        var beside = Turn();
        Assert.Equal(["speak lines=1 letters=8"], beside.At(1000, Frame(WorkSeen.Working, proposed: [Head(4, "claude: I will read the file."), ToolRow(5, "tool: Read (a.txt)")])));
        Assert.Equal([5], beside.Spoken());
        Assert.Equal(1, beside.Health.GatePending);
    }

    [Fact]
    public void A_new_generation_drops_what_is_pending_and_counts_it()
    {
        // EG-4.7, EG-5.7: he has moved on. Each of these raises the generation and drops what the gate holds.
        FlowInput[] moves =
        [
            new UserActed(UserAction.Sent, "anothermessage"),
            new UserActed(UserAction.Answered),
            new UserActed(UserAction.Escaped),
            new UserActed(UserAction.ScreenKey),
            new UserActed(UserAction.PutAway),
            new QuestionShown(),
            new AttentionAsked(AttentionKind.Permission),
        ];
        foreach (var move in moves)
        {
            var s = Turn();
            s.At(1000, Frame(WorkSeen.Working, proposed: [Head(1, "claude: ok"), Row(2, 1, "A second row.")]));
            var generation = s.Flow.Generation;

            Assert.Empty(s.At(1050, move));
            Assert.Equal(generation + 1, s.Flow.Generation);
            Assert.Equal(2, s.Health.GateStale);
            Assert.Equal(0, s.Health.GatePending);

            // The entry that comes now finds nothing held: nothing is spoken, then or at the cap.
            Assert.Empty(s.At(1075, Reply("e1", "ok\n\nA second row.")));
            Assert.Empty(s.At(1500, Tick));
            Assert.Equal(0, s.Health.GateReleased);
            Assert.Equal(0, s.Health.GateUnconfirmed);
        }
    }

    [Fact]
    public void Released_lines_of_one_input_are_one_decision_in_line_order()
    {
        // One frame: a tool row, a reply's head and row the record holds, an error row. One decision, in the order
        // proposed, which is the conversation's; the binding follows it.
        var s = Turn();
        s.At(1000, Reply("e1", "The file is read.\n\nIt has two lines."));
        Assert.Equal(["speak lines=4 letters=58", "bound head=5"], s.At(1010, Frame(WorkSeen.Working, proposed:
            [ToolRow(4, "tool: Read (a.txt)"), Head(5, "claude: The file is read."), Row(6, 5, "It has two lines."), ErrorRow(7, "error: API Error: 401 Invalid API key")])));
        Assert.Equal([4, 5, 6, 7], s.Spoken());

        // Without a record: the lines of two frames whose holds have ended, and the tool row of the frame that
        // comes then, are one decision too.
        var open = Turn(record: false);
        open.At(1000, Frame(WorkSeen.Working, proposed: [Head(1, "claude: The file is read."), Row(2, 1, "It has two lines.")]));
        open.At(1050, Frame(WorkSeen.Working, proposed: Row(3, 1, "And a third.")));
        Assert.Equal(["speak lines=4 letters=43"], open.At(1300, Frame(WorkSeen.Working, proposed: ToolRow(4, "tool: Read (a.txt)"))));
        Assert.Equal([1, 2, 3, 4], open.Spoken());
        Assert.Equal(4, open.Health.GateReleased);
    }

    [Fact]
    public void Done_waits_for_a_pending_head_and_follows_its_release()
    {
        // 4.1 "A turn ends" without a waited-for hook: C and D, the bell, the reply's rows 75 after the bell, and
        // the reply's entry late. EG-5.6: Done's first condition is that no proposed line is pending.
        var s = Turn();
        s.At(8010, D);
        s.At(8011, Bell);
        Assert.Empty(s.At(8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok"))));

        // 300 after the bell the end counts as confirmed, and a frame added a line: Done still waits for the head.
        Assert.Empty(s.At(8311, Tick));
        Assert.True(s.Flow.Ending);

        Assert.Equal(["speak lines=1 letters=2", "bound head=412", "done turn=1 after=389"], s.At(8400, Reply("b8142ecf", "ok")));
        Assert.False(s.Flow.Ending);

        // A head no entry confirms is dropped at its cap, 500 after its frame, and Done follows in that step.
        var dropped = Turn();
        dropped.At(8010, D);
        dropped.At(8011, Bell);
        dropped.At(8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok")));
        Assert.Empty(dropped.At(8311, Tick));
        Assert.Equal(["done turn=1 after=575"], dropped.At(8586, Tick));
        Assert.Equal(1, dropped.Health.GateUnconfirmed);
        Assert.Equal(0, dropped.Health.GateReleased);
    }

    [Fact]
    public void Done_waits_for_the_rows_of_a_reply_that_is_half_drawn()
    {
        // Measured (the long-reply recording, its turn of Markdown): the reply's rows 54 after the bell, the
        // reply's entry and the record's turn end 81 after it, and the reply's last row in a write of its own,
        // shown 157 after the bell. The end is confirmed and a frame added a line when the turn end is read, but
        // the reply's key goes on behind the rows released: Done waits for the last row and follows it.
        const string source = "The first row.\nThe second row.\nThe last row.";
        static Session UntilTheTurnEnd(params Proposed[] rows)
        {
            var s = Turn();
            s.At(22400, D);
            s.At(22425, Bell);
            Assert.Empty(s.At(22479, Frame(WorkSeen.Idle, proposed: rows)));
            s.At(22506, Reply("e1", source));
            return s;
        }

        var s = UntilTheTurnEnd(Head(59, "claude: The first row."), Row(61, 59, "The second row."));
        Assert.Equal([59, 61], s.Spoken());
        Assert.Empty(s.At(22506, new EntryRead(EntryKind.TurnEnd, "t1")));
        Assert.True(s.Flow.Ending);
        Assert.Equal(["speak lines=1 letters=10", "done turn=1 after=157"], s.At(22582, Frame(WorkSeen.Idle, proposed: Row(86, 59, "The last row."))));
        Assert.Equal(0, s.Health.GateOutsideTurn);
        Assert.Equal(3, s.Health.GateReleased);

        // Rows that never come are given up 500 after the last one that came, and Done follows then.
        var never = UntilTheTurnEnd(Head(59, "claude: The first row."), Row(61, 59, "The second row."));
        never.At(22506, new EntryRead(EntryKind.TurnEnd, "t1"));
        Assert.Equal(23006, never.Wakes[^1]);
        Assert.Empty(never.At(23005, Tick));
        Assert.Equal(["done turn=1 after=581"], never.At(23006, Tick));

        // Where only the first line of a reply is spoken, no row is ever awaited: the key goes on behind the
        // head, and Done comes with the record's turn end.
        var heads = UntilTheTurnEnd(Head(59, "claude: The first row."));
        Assert.Equal([59], heads.Spoken());
        Assert.Equal(["done turn=1 after=81"], heads.At(22506, new EntryRead(EntryKind.TurnEnd, "t1")));

        // He moved on while rows were awaited: nothing holds Done back any more, and no generation drops it.
        var moved = UntilTheTurnEnd(Head(59, "claude: The first row."), Row(61, 59, "The second row."));
        moved.At(22506, new EntryRead(EntryKind.TurnEnd, "t1"));
        Assert.Equal(["done turn=1 after=95"], moved.At(22520, new UserActed(UserAction.Sent, "thenextmessage")));
    }

    [Fact]
    public void A_line_still_pending_when_Done_is_said_at_its_cap_is_dropped()
    {
        // The last rows come late, 399 after the bell, with no record: their hold would end 649 after the bell,
        // and Done's cap is 600. Done is said, the turn's window closes (EG-5.2), and nothing is spoken after it.
        var s = Turn(record: false);
        s.At(2000, D);
        s.At(2001, Bell);
        Assert.Empty(s.At(2400, Frame(WorkSeen.Idle, proposed: Head(5, "claude: ok"))));
        Assert.Equal(1, s.Health.GatePending);

        Assert.Equal(["done turn=1 after=600"], s.At(2601, Tick));
        Assert.Equal(2650, s.Wakes[^1]);
        Assert.Empty(s.At(2650, Tick));
        Assert.Equal(0, s.Health.GatePending);
        Assert.Equal(1, s.Health.GateOutsideTurn);
        Assert.Equal(0, s.Health.GateReleased);
    }

    [Fact]
    public void With_reply_speech_off_nothing_is_pending()
    {
        var s = Turn();
        s.At(1000, Frame(WorkSeen.Working, proposed: Head(1, "claude: ok")));
        Assert.Equal(1, s.Health.GatePending);

        // Speak replies was switched off: the next frame says so. What waited is dropped; a tool row, spoken by
        // its own switch, is released as ever.
        Assert.Equal(["speak lines=1 letters=8"], s.At(1100, Frame(WorkSeen.Working, speech: false, proposed: ToolRow(2, "tool: Read (a.txt)"))));
        Assert.Equal(0, s.Health.GatePending);
        Assert.Equal(1, s.Health.GateStale);
        Assert.Empty(s.At(1150, Reply("e1", "ok")));

        // A reply line proposed all the same is neither held nor spoken, though the record holds its text.
        Assert.Empty(s.At(1200, Frame(WorkSeen.Working, speech: false, proposed: Head(3, "claude: ok"))));
        Assert.Equal(0, s.Health.GatePending);
        Assert.Equal(2, s.Health.GateStale);

        // So Done never waits for the gate: it comes with the frame that adds the reply's rows.
        s.At(1990, Stop(null));
        s.At(2000, D);
        s.At(2001, Bell);
        Assert.Equal(["done turn=1 after=49"], s.At(2050, Frame(WorkSeen.Idle, speech: false, proposed: Head(4, "claude: Another reply."))));
        Assert.Equal(1, s.Health.GateReleased);
        Assert.Equal(3, s.Health.GateStale);
    }

    [Fact]
    public void A_confirmed_head_is_reported_bound()
    {
        // EG-5.3, EG-8.1: the core says which entry was bound to which line, in the generation it was decided in,
        // behind the decision that speaks the head.
        var s = Turn();
        s.At(1000, Reply("b8142ecf", "ok\n\nA second row."));
        Assert.Equal(["speak lines=1 letters=2", "bound head=412"], s.At(1010, Frame(WorkSeen.Working, proposed: Head(412, "claude: ok"))));
        var bound = s.Bound();
        Assert.Equal(("b8142ecf", 412, s.Flow.Generation), (bound.EntryId, bound.HeadLineId, bound.Generation));
        Assert.IsType<SpeakReply>(s.Last[0]);
        Assert.IsType<EntryBound>(s.Last[1]);

        // One binding for a reply: a row under the head, released with a later frame, reports none.
        Assert.Equal(["speak lines=1 letters=10"], s.At(1100, Frame(WorkSeen.Working, proposed: Row(413, 412, "A second row."))));

        // A head released without a record is bound to nothing; nor is an error row or a tool row.
        var open = Turn(record: false);
        Assert.Equal(["speak lines=2 letters=32"], open.At(1000, Frame(WorkSeen.Working, proposed:
            [Head(412, "claude: ok"), ErrorRow(413, "error: API Error: 401 Invalid API key"), ToolRow(414, "tool: Read (a.txt)")])));
        Assert.Equal(["speak lines=1 letters=2"], open.At(1250, Tick));
        Assert.Equal([412], open.Spoken());
    }

    [Fact]
    public void A_new_epoch_and_a_lost_record_forget_the_replies()
    {
        // EG-4.9: the conversation started afresh under the window. What was pending goes, counted, and a reply
        // read before is no candidate in the new conversation.
        var s = Turn();
        s.At(1000, Reply("e1", "ok"));
        s.At(1010, Frame(WorkSeen.Working, proposed: Head(1, "claude: Not in the record.")));
        Assert.Equal(["state idle", "forget", "state working"], s.At(1100, Frame(WorkSeen.Working, added: false, epoch: 2)));
        Assert.Equal(1, s.Health.GateStale);
        Assert.Equal(0, s.Health.GatePending);

        s.At(1200, new RecordFound());
        Assert.Empty(s.At(1300, Frame(WorkSeen.Working, epoch: 2, proposed: Head(2, "claude: ok"))));
        Assert.Empty(s.At(1800, Tick));
        Assert.Equal(1, s.Health.GateUnconfirmed);
        Assert.Equal(0, s.Health.GateReleased);

        // The record is lost (EG-3.6): the gate is open, a line goes by the hold. When a record is there again,
        // it starts with what it reads from then on.
        var lost = Turn();
        lost.At(1000, Reply("e1", "ok"));
        lost.At(1100, new RecordLost());
        lost.At(1200, Frame(WorkSeen.Working, proposed: Head(1, "claude: Something.")));
        Assert.Equal(["speak lines=1 letters=9"], lost.At(1450, Tick));
        lost.At(1500, new RecordFound());
        Assert.Empty(lost.At(1600, Frame(WorkSeen.Working, proposed: Head(2, "claude: ok"))));
        Assert.Empty(lost.At(2100, Tick));
        Assert.Equal(1, lost.Health.GateUnconfirmed);
        Assert.Equal(1, lost.Health.GateReleased);
    }

    [Fact]
    public void The_replies_kept_are_bounded()
    {
        // One long turn: 300 reply texts with no prompt between them and no head that takes them. The 256 newest
        // are kept: a window lives for days, and nothing in it may grow without an end.
        var s = Turn();
        for (var i = 0; i < 300; i++)
        {
            s.At(1000 + i, Reply($"e{i}", $"Reply number {i}."));
        }

        Assert.Empty(s.At(2000, Frame(WorkSeen.Working, proposed: Head(1, "claude: Reply number 43."))));
        Assert.Equal(["speak lines=1 letters=13", "bound head=2"], s.At(2010, Frame(WorkSeen.Working, proposed: Head(2, "claude: Reply number 44."))));
        Assert.Equal("e44", s.Bound().EntryId);
        Assert.Equal(["speak lines=1 letters=14", "bound head=3"], s.At(2020, Frame(WorkSeen.Working, proposed: Head(3, "claude: Reply number 299."))));
        Assert.Equal(1, s.Health.GatePending);
    }

    [Fact]
    public void A_plain_turns_reply_is_spoken_once_and_before_Done_in_every_order_of_its_sources()
    {
        // 4.1 the plain turn as measured: A 5845, the prompt's entry 6182, the stop event 7988, C and D 8010, the
        // bell 8011, the reply's rows 8086, the reply's entry and the record's turn end 8121. Events and entries
        // share no clock with the console's frames (EG-4.8), and hooks the program does not wait for come some
        // 500 later than measured here (4.3): the gate holds in every order (EG-11.2). One release at most, never
        // after Done, one Done.
        (long, FlowInput)[] marks = [(5845, A), (8010, D)];
        (long, FlowInput)[] screen =
        [
            (5846, Frame(WorkSeen.Working, added: false)),
            (8011, Bell),
            (8086, Frame(WorkSeen.Idle, proposed: Head(412, "claude: ok"))),
        ];
        static (long, FlowInput)[] Events(long shift) => [(7988 + shift, Stop("ok"))];
        static (long, FlowInput)[] Entries(long shift) =>
        [
            (6182 + shift, Prompt()),
            (8121 + shift, Reply("b8142ecf", "ok")),
            (8121 + shift, new EntryRead(EntryKind.TurnEnd, "t1")),
        ];

        // Confirmed by the stop event's last message: spoken with its rows, Done behind it, bound when the entry comes.
        string[] byTheEvent = ["8086 speak lines=1 letters=2", "8086 done turn=1 after=75", "8121 bound head=412"];

        // Confirmed by the entry: spoken and bound when it comes, and Done with the record's turn end.
        string[] byTheEntry = ["8121 speak lines=1 letters=2", "8121 bound head=412", "8121 done turn=1 after=110"];

        // Confirmed by nothing in its time (section 11, R3): not spoken, and Done when its head is dropped.
        string[] byNothing = ["8586 done turn=1 after=575"];

        var orders = new (string Name, (long, FlowInput)[] Inputs, string[] After)[]
        {
            ("as recorded", [.. marks, .. screen, .. Events(0), .. Entries(0)], byTheEvent),
            ("every event 300 early", [.. marks, .. screen, .. Events(-300), .. Entries(0)], byTheEvent),
            ("every event 300 late", [.. marks, .. screen, .. Events(300), .. Entries(0)], byTheEntry),
            ("every event 700 late", [.. marks, .. screen, .. Events(700), .. Entries(0)], byTheEntry),
            ("every event 1200 late", [.. marks, .. screen, .. Events(1200), .. Entries(0)], byTheEntry),
            ("every entry 1 s late", [.. marks, .. screen, .. Events(0), .. Entries(1000)], ["8086 speak lines=1 letters=2", "8086 done turn=1 after=75", "9121 bound head=412"]),
            ("no entries", [.. marks, .. screen, .. Events(0)], ["8086 speak lines=1 letters=2", "8086 done turn=1 after=75"]),
            ("no events", [.. marks, .. screen, .. Entries(0)], byTheEntry),
            ("every event 700 late and every entry 1 s late", [.. marks, .. screen, .. Events(700), .. Entries(1000)], byNothing),
            ("neither", [.. marks, .. screen], byNothing),
        };
        foreach (var (name, inputs, after) in orders)
        {
            var s = new Session();
            s.At(0, new RecordFound());
            s.Play(inputs);
            string[] expected = ["5845 state working", "8010 state idle", .. after];
            Assert.True(expected.SequenceEqual(s.Said), $"{name}: {string.Join("; ", s.Said)}");
            Assert.False(s.Flow.Working || s.Flow.Ending, name);
            Assert.Equal(0, s.Health.GatePending);
        }

        // With no record at all the hold decides: 250 after the rows, and Done behind the reply.
        var open = new Session();
        open.Play([.. marks, .. screen, .. Events(0)]);
        Assert.Equal(["5845 state working", "8010 state idle", "8336 speak lines=1 letters=2", "8336 done turn=1 after=325"], open.Said);
    }

    [Fact]
    public void Every_number_is_counted_in_the_health()
    {
        // EG-5.9: every proposal ends in one number: released, outside a turn, unconfirmed, stale, or pending now.
        var s = new Session();
        s.At(0, new RecordFound());

        // Outside a turn: one.
        s.At(50, Frame(WorkSeen.Idle, proposed: Head(1, "claude: An old reply.")));
        s.At(100, A);

        // Released: a tool row, and a head and its row that the record confirms.
        s.At(1000, Reply("e1", "First.\n\nSecond."));
        s.At(1010, Frame(WorkSeen.Working, proposed: [ToolRow(2, "tool: Read (a.txt)"), Head(3, "claude: First."), Row(4, 3, "Second.")]));

        // Unconfirmed: a row that is not in the entry, at its cap.
        s.At(1100, Frame(WorkSeen.Working, proposed: Row(5, 3, "Not in the entry.")));
        s.At(1600, Tick);

        // Stale: a head and its row that waited when he sent a message.
        s.At(2000, Frame(WorkSeen.Working, proposed: [Head(6, "claude: Too late."), Row(7, 6, "Its row.")]));
        s.At(2050, new UserActed(UserAction.Sent, "next"));

        // Pending now: one.
        s.At(2100, Frame(WorkSeen.Working, proposed: Head(8, "claude: Still waiting.")));

        var health = s.Health;
        Assert.Equal((3, 1, 1, 2, 1), (health.GateReleased, health.GateOutsideTurn, health.GateUnconfirmed, health.GateStale, health.GatePending));
        Assert.Equal(8, health.GateReleased + health.GateOutsideTurn + health.GateUnconfirmed + health.GateStale + health.GatePending);
        Assert.Contains("Gate: 3 released, 1 outside a turn, 1 unconfirmed, 2 stale, 1 pending", health.Lines("in shadow"));

        // The process's end, before that line's cap, counts what was still held as stale, and nothing is pending.
        s.At(2200, new ProcessEnded());
        Assert.Equal((3, 0), (health.GateStale, health.GatePending));
        Assert.Equal(8, health.GateReleased + health.GateOutsideTurn + health.GateUnconfirmed + health.GateStale + health.GatePending);
    }
}
