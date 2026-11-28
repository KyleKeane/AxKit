using System.Text;
using AxClaude.Core.Transcript;
using AxKit.Core.Pty;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// A message in flight (docs/axClaude/SPEC.md D33, "One message in flight"), run with the very checks the window wires
/// (<see cref="MessageFlight"/>) through the window's own queue (<see cref="WriteQueue"/>), over the recordings, as the
/// window meets them: each chunk at its recorded time and cut at random, a frame ending at the cursor shown again or
/// after 100 ms of quiet, the queue asked whenever its pump would come round, with the recorded clock (the seventh
/// review of 2026-11-27, L9). The rule of the eighth review (2026-11-28, M-A): once a message's Enter went, axClaude
/// never gives it back; a draft left on Claude Code's row is said, and the user decides.
/// </summary>
public sealed class MessageFlightTests
{
    /// <summary>
    /// Every message and slash command of each recording, played with the window's own frame rule (a frame ends at
    /// the cursor shown again or after 100 ms of quiet) and cut at random, is taken once its Enter went, and none is
    /// given back after it (the eighth review of 2026-11-28, M-A). The permissions and tasks-screen recordings draw
    /// the command's screen in place of the prompt row in the frame that takes the command, so that no frame shows
    /// the row bare: until that review the last frame before the screen, which still showed the command, gave it
    /// back, taken as it was, for a second run. The /clear recording draws no prompt row from 13448 ms to 16144 ms
    /// after the command's Enter (the seventh review, K1). <paramref name="enteredByTheRecording"/>: the messages
    /// whose Enter the recording's script sent 150 ms after the text, whatever the row showed, before the window's
    /// check had come round to the frame that showed the draft (clear's last message: the quiet frame from 16066 ms,
    /// the check at 16065 and 16165 ms, the recording's Enter at 16116 ms; long-message's two: the recording's Enter
    /// before any quiet). The recording took them, the window's own Enter never went, and they come back before their
    /// Enter, never after it. The window sends no Enter but its own, so it meets no such case.
    /// </summary>
    [Theory]
    [InlineData("clear-claude2.1.319-240x50.vt", "clear.script", 1, new[] { "Reply with the single word again." })]
    [InlineData("clear-claude2.1.319-240x50.vt", "clear.script", 11, new[] { "Reply with the single word again." })]
    [InlineData("steering-claude2.1.278-240x50.vt", "steering.script", 2, new string[0])]
    [InlineData("long-message-claude2.1.278-240x50.vt", "long-message.script", 14, new[] { "Please reply", "Second message" })]
    [InlineData("permissions-claude2.1.327-240x50.vt", "permissions.script", 1, new string[0])]
    [InlineData("permissions-claude2.1.327-240x50.vt", "permissions.script", 9, new string[0])]
    [InlineData("tasks-screen-claude2.1.281-240x50.vt", "tasks-screen.script", 3, new string[0])]
    [InlineData("tasks-screen-claude2.1.281-240x50.vt", "tasks-screen.script", 17, new string[0])]
    public void Every_recorded_message_is_taken_and_none_given_back(string name, string script, int seed, string[] enteredByTheRecording)
    {
        var messages = Messages(script);
        Assert.NotEmpty(messages);
        foreach (var (flight, outcome) in Fly(name, messages, seed))
        {
            if (enteredByTheRecording.Any(start => flight.Text.StartsWith(start, StringComparison.Ordinal)))
            {
                Assert.False(flight.Entered, $"{Shown(flight.Text)}: the window's Enter never went");
                Assert.True(outcome.StartsWith("given back", StringComparison.Ordinal) && flight.Why == GivenBack.NotShown, $"{Shown(flight.Text)}: {outcome}");
                continue;
            }

            Assert.True(outcome == "taken", $"{Shown(flight.Text)}: {outcome}");
        }
    }

    /// <summary>
    /// K1 on the /clear recording itself: the command's flight reaches the three seconds' limit on a row not drawn and is
    /// taken there, at once, so that the next message is not held behind it.
    /// </summary>
    [Fact]
    public void The_clear_commands_row_not_drawn_at_the_limit_counts_as_taken()
    {
        var flights = Fly("clear-claude2.1.319-240x50.vt", ["/clear"], seed: 5);
        var (flight, outcome) = flights.Single();
        Assert.Equal("taken", outcome);
        Assert.True(flight.Entered && flight.Done);
    }

    private const string Hello = "Reply with the single word hello.";

    private static string Bare() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace}{Esc}[?25h";

    private static string Held(string draft) => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} {draft.Replace("\n", "\r\n")}{Esc}[?25h";

    private static string Gone() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n{Esc}[?25h";

    private static string Question() => $"{Esc}[?25l\r\nPermission Required: Create file\r\nalpha.txt\r\nDo you want to create alpha.txt?\r\n1. Yes\r\n2. No\r\nSelect with numbers [1-2]. Then Enter to submit or Escape to cancel:{Esc}[?25h";

    /// <summary>
    /// M-A (the eighth review of 2026-11-28): once a message's Enter went, its flight is never given back, on a screen
    /// drawn here (a row of 80 columns): at the three seconds' limit a row that still shows the message, or holds other
    /// text, or is not drawn, or is bare, ends the flight as taken, and so does a question drawn over the held draft.
    /// Until that review the row that still showed the message gave it back (K1), and so did a question drawn over it
    /// (K2): a guess, which a screen drawn in the row's place in one frame made wrong, and the message was sent twice.
    /// A draft still on the row at the limit, the message or any other, is left there (<see cref="MessageFlight.LeftOnRow"/>):
    /// the window says so once, and the user decides; a question drawn over it is simply taken.
    /// </summary>
    [Fact]
    public void A_flight_whose_Enter_went_is_never_given_back()
    {
        (WriteCheck Check, bool Left) Run(string after, bool questionNext)
        {
            var now = 0L;
            var model = new SessionModel(80, 20);
            Feed(model, Bare());
            var flight = new MessageFlight(model, Hello, () => null, () => now);
            Assert.Equal(WriteCheck.Goes, flight.First());
            Feed(model, Held(Hello));
            Assert.Equal(WriteCheck.Goes, flight.Enter());
            Feed(model, after);
            now = 1500;
            flight.Again();
            if (questionNext)
            {
                Feed(model, Question());
                Assert.True(model.PromptPending, "the question is read");
            }

            now = questionNext ? 1600 : 3000;
            var check = flight.Taken();
            Assert.True(flight.Done, "the flight is over");
            Assert.Null(flight.Remark);
            return (check, flight.LeftOnRow);
        }

        Assert.Equal((WriteCheck.Skip, true), Run(Held(Hello), questionNext: false));
        Assert.Equal((WriteCheck.Skip, true), Run(Held("something else"), questionNext: false));
        Assert.Equal((WriteCheck.Skip, false), Run(Gone(), questionNext: false));
        Assert.Equal((WriteCheck.Skip, false), Run(Bare(), questionNext: false));
        Assert.Equal((WriteCheck.Skip, false), Run(Held(Hello), questionNext: true));
        Assert.Equal((WriteCheck.Skip, false), Run(Bare(), questionNext: true));
    }

    /// <summary>
    /// M-A: a draft left on the row carries Claude Code's own line about it when one stands on the screen, in either
    /// of its recorded forms ("Removed 1 invisible character · review and press Enter to send" above the row, Claude
    /// Code 2.1.309, the trust-banner-fast recording; "Removed 1 invisible character from your prompt" under the mode
    /// line, 2.1.330), which the window says with its own words.
    /// </summary>
    [Theory]
    [InlineData("Removed 1 invisible character · review and press Enter to send", true)]
    [InlineData("Removed 2 invisible characters from your prompt", false)]
    public void A_draft_left_on_the_row_carries_Claude_Codes_line_about_it(string remark, bool above)
    {
        var now = 0L;
        var model = new SessionModel(80, 20);
        Feed(model, Bare());
        var flight = new MessageFlight(model, Hello, () => null, () => now);
        Assert.Equal(WriteCheck.Goes, flight.First());
        Feed(model, Held(Hello));
        Assert.Equal(WriteCheck.Goes, flight.Enter());
        var row = $"${ClaudeCodeTexts.DraftSpace} {Hello}";
        Feed(model, above
            ? $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n{remark}\r\n{row}{Esc}[?25h"
            : $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n{row}\r\nauto mode on (shift+tab to cycle)\r\n{remark}{Esc}[2A{Esc}[{row.Length + 1}G{Esc}[?25h");
        now = 3000;
        flight.Again();
        Assert.Equal(WriteCheck.Skip, flight.Taken());
        Assert.True(flight.LeftOnRow);
        Assert.Equal(remark, flight.Remark);
    }

    /// <summary>
    /// M1 for the user's own message: its Enter's check compares the row without hidden characters (the user wrote
    /// them, and Claude Code taking one out of its prompt harms nothing), and it never gets a second Enter. Any other
    /// text (an offered line, a menu's command) is compared with them (K4).
    /// </summary>
    [Fact]
    public void The_users_own_message_is_compared_without_its_hidden_characters()
    {
        var now = 0L;
        var model = new SessionModel(80, 6);
        Feed(model, Bare());
        var flight = new MessageFlight(model, "Reply with the​ single word hello.", () => null, () => now, own: true);
        Assert.True(flight.Hides);
        Assert.Equal(WriteCheck.Goes, flight.First());
        Feed(model, Held(Hello));
        Assert.Equal(WriteCheck.Goes, flight.Enter());
        now = 1500;
        Assert.Equal(WriteCheck.Skip, flight.Again());
        Assert.False(flight.EnteredAgain);
    }

    /// <summary>
    /// M1 (the eighth review of 2026-11-28): a line end or a tab is no hidden character. Until that review a message
    /// of two lines counted as one that holds a hidden character, and Claude Code's dropped Enter was never sent again
    /// for it (FR-2.1).
    /// </summary>
    [Fact]
    public void A_message_of_two_lines_gets_the_Enter_again()
    {
        var now = 0L;
        var model = new SessionModel(80, 8);
        Feed(model, Bare());
        var flight = new MessageFlight(model, "first line\nsecond line", () => null, () => now);
        Assert.Equal(WriteCheck.Goes, flight.First());
        Feed(model, Held("first line\nsecond line"));
        Assert.Equal(WriteCheck.Goes, flight.Enter());
        now = 1500;
        Assert.Equal(WriteCheck.Goes, flight.Again());
        Assert.True(flight.EnteredAgain);
        Assert.False(new MessageFlight(model, "first\tcolumn\r\nsecond line", () => null, () => now).Hides);
    }

    /// <summary>
    /// M3 (the eighth review of 2026-11-28): a message that waits for a bare row that is not drawn comes back with a
    /// reason of its own, so that the window gives no advice to empty a row that is not there.
    /// </summary>
    [Fact]
    public void A_message_whose_row_is_not_drawn_comes_back_as_not_drawn()
    {
        var now = 0L;
        var model = new SessionModel(80, 8);
        Feed(model, Gone());
        var flight = new MessageFlight(model, Hello, () => null, () => now);
        Assert.Equal(WriteCheck.NotYet, flight.First());
        now = 3000;
        Assert.Equal(WriteCheck.Over, flight.First());
        Assert.Equal(GivenBack.NotDrawn, flight.Why);

        var other = new SessionModel(80, 8);
        Feed(other, Held("something else"));
        var held = new MessageFlight(other, Hello, () => null, () => now);
        Assert.Equal(WriteCheck.NotYet, held.First());
        now = 6000;
        Assert.Equal(WriteCheck.Over, held.First());
        Assert.Equal(GivenBack.NotBare, held.Why);
    }

    /// <summary>
    /// K3 at the flight's outcome: a check asked inside a frame reads the last frame's end, never the live screen. The
    /// frame showed the message on the row; the next frame has cleared the screen and not yet drawn the row again.
    /// Read live, the row would not show the message, and the Enter would wait; read as the frame showed it, it goes.
    /// </summary>
    [Fact]
    public void A_check_inside_a_frame_reads_the_last_frames_end()
    {
        var now = 0L;
        var model = new SessionModel(80, 8);
        Feed(model, Bare());
        var flight = new MessageFlight(model, Hello, () => null, () => now);
        Assert.Equal(WriteCheck.Goes, flight.First());
        Feed(model, Held(Hello));
        model.Feed(Encoding.UTF8.GetBytes($"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n"));
        Assert.Equal(WriteCheck.Goes, flight.Enter());
        model.EndFrame();
        var late = new MessageFlight(model, Hello, () => null, () => now);
        Assert.Equal(WriteCheck.NotYet, late.First());
    }

    /// <summary>K4: a text with a hidden character gets no Enter for a row that shows it without one, and never a second Enter.</summary>
    [Fact]
    public void A_text_with_a_hidden_character_gets_no_Enter_it_was_not_seen_with()
    {
        var now = 0L;
        var model = new SessionModel(80, 6);
        Feed(model, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace}{Esc}[?25h");
        var flight = new MessageFlight(model, "Reply with the​ single word hello.", () => null, () => now);
        Assert.Equal(WriteCheck.Goes, flight.First());
        // Claude Code drew it without the character (it takes such characters out of its prompt).
        Feed(model, Held(Hello));
        Assert.Equal(WriteCheck.NotYet, flight.Enter());
        now = 3000;
        Assert.Equal(WriteCheck.Over, flight.Enter());
        Assert.Equal(GivenBack.NotShown, flight.Why);

        var drawn = new SessionModel(80, 6);
        Feed(drawn, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace}{Esc}[?25h");
        var shown = new MessageFlight(drawn, "Reply with the‍ single word hello.", () => null, () => now);
        Assert.Equal(WriteCheck.Goes, shown.First());
        Feed(drawn, Held("Reply with the‍ single word hello."));
        Assert.Equal(WriteCheck.Goes, shown.Enter());
        now += 1500;
        Assert.Equal(WriteCheck.Skip, shown.Again());
    }

    /// <summary>The messages a recording's script sends, in order: a line sent after a lone newline write belongs to the message before it (Ctrl+J); the slash commands with them; the answers of one key left out.</summary>
    private static List<string> Messages(string script)
    {
        var lines = File.ReadAllLines(Path.Combine(SourceTexts.RepositoryRoot(), "tests", "fixtures", script));
        var messages = new List<string>();
        for (var i = 0; i < lines.Length; i++)
        {
            var text = lines[i].StartsWith("send ", StringComparison.Ordinal) ? lines[i]["send ".Length..] : null;
            if (text is null || text.StartsWith('\\') || text.Length < 2)
            {
                continue;
            }

            if (i >= 2 && lines[i - 2] == @"send \n")
            {
                messages[^1] += "\n" + text;
            }
            else
            {
                messages.Add(text);
            }
        }

        return messages;
    }

    private static string Shown(string text) => text.Length > 40 ? text[..40] + "..." : text;

    /// <summary>One message's flight in the replay: its text, its checks once it is queued, its own queue of the window's writes, when the queue is asked next, and what became of it.</summary>
    private sealed class Flown(string text, long queuedAt)
    {
        public string Text { get; } = text;

        public MessageFlight? Flight { get; set; }

        public WriteQueue Queue { get; } = new();

        public long NextAsk { get; set; } = queuedAt;

        public string Outcome { get; set; } = $"in flight since {queuedAt} ms";

        public List<string> Readings { get; } = [];
    }

    /// <summary>
    /// Plays the recording as the window meets it and flies each message through a queue of the window's writes with
    /// the window's checks, queued just before the chunk that first holds the start of its text, when the recording
    /// typed it; each queue is asked whenever its pump would come round, between the pieces of a chunk too, and on after
    /// the recording's end until it is idle. Each message has a queue of its own: the window holds the next message
    /// behind one in flight, but the recordings typed each when they did, and a message held back would meet a row
    /// the recording had already moved past. A frame ends as the window ends one: at the cursor shown again, inside
    /// the model, or after 100 ms of quiet (until the eighth review of 2026-11-28 the test could also end one in front
    /// of every frame's start, which no window does).
    /// </summary>
    private static List<(MessageFlight Flight, string Outcome)> Fly(string name, IReadOnlyList<string> messages, int seed)
    {
        var (columns, rows) = FixtureSize(name);
        var bytes = Fixture(name);
        var chunks = Chunks(name);
        var model = new SessionModel(columns, rows);
        var random = new Random(seed);
        var now = 0L;

        // When each message is queued: just before the first chunk, after the one the message before was found in,
        // that holds the start of its text.
        var flights = new List<Flown>();
        var from = 0;
        foreach (var message in messages)
        {
            var start = Encoding.UTF8.GetBytes(message.Split('\n')[0][..Math.Min(12, message.Length)]);
            var found = chunks.FindIndex(from, chunk => bytes.AsSpan(chunk.Offset, Math.Min(chunk.Length + start.Length, bytes.Length - chunk.Offset)).IndexOf(start) >= 0);
            Assert.True(found >= 0, $"{Shown(message)} is in the recording");
            flights.Add(new Flown(message, chunks[found].Ms - 1));
            from = found + 1;
        }

        void Pump(long until)
        {
            while (flights.Where(one => one.NextAsk <= until).MinBy(one => one.NextAsk) is { } flown)
            {
                now = flown.NextAsk;
                if (flown.Flight is null)
                {
                    // Queued now: the flight takes what Claude Code waits on as of this moment.
                    flown.Flight = new MessageFlight(model, flown.Text, () => null, () => now);
                    Queue(flown, flown.Flight);
                }

                var step = flown.Queue.Next();
                flown.NextAsk = step.Kind switch
                {
                    WriteStepKind.Write or WriteStepKind.Wait => now + step.WaitMs,
                    _ => long.MaxValue,
                };
                if (step.Kind is WriteStepKind.Dropped or WriteStepKind.Failed)
                {
                    flown.Outcome = $"given back at {now} ms, {flown.Outcome}: {flown.Flight.Why}, typed {flown.Flight.Typed}, "
                        + $"its Enter's readings {string.Join("; ", flown.Readings.Select(Shown))}";
                }
            }
        }

        // The window's own writes of the message (MessageFlight.Writes), each check read as the window's wrapper reads it.
        void Queue(Flown flown, MessageFlight flight)
        {
            foreach (var (text, delay, check) in flight.Writes())
            {
                flown.Queue.Add(text, delay, () =>
                {
                    if (!flight.Entered)
                    {
                        var reading = model.PromptDraft ?? "not drawn";
                        if (flown.Readings.Count == 0 || !flown.Readings[^1].EndsWith(": " + reading, StringComparison.Ordinal))
                        {
                            flown.Readings.Add($"{now}: {reading}");
                        }
                    }

                    var answer = check();
                    flown.Outcome = flight.Done ? "taken" : flown.Outcome;
                    return answer;
                });
            }
        }

        long previous = -1;
        foreach (var (offset, length, ms) in chunks)
        {
            if (previous >= 0 && ms - previous >= 100)
            {
                Pump(previous + 99);
                model.EndFrame();
            }

            Pump(ms - 1);
            var at = offset;
            while (at < offset + length)
            {
                var piece = Math.Min(random.Next(1, Math.Max(2, length / 2 + 1)), offset + length - at);
                model.Feed(bytes.AsSpan(at, piece));
                at += piece;
                Pump(ms);
            }

            previous = ms;
        }

        model.EndFrame();
        Pump(previous + 10_000);
        Assert.True(flights.All(one => one.Flight is not null), "every message was queued");
        return [.. flights.Select(one => (one.Flight!, one.Outcome))];
    }
}
