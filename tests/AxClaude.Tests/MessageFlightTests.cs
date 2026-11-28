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
/// review of 2026-11-27, L9: until then the recording test ran a copy of the rule on readings a chunk apart, without
/// the time limit, and left the slash commands out). The rule of that review: a message is given back only on
/// positive evidence that Claude Code did not take it (K1, K2).
/// </summary>
public sealed class MessageFlightTests
{
    /// <summary>
    /// Every message and slash command of each recording is taken, none given back. The /clear recording draws no
    /// prompt row from 13448 ms to 16144 ms, after the command's Enter: at the three seconds' limit the row is not
    /// drawn, which until the seventh review gave the command back, taken as it was, for a second send (K1). The
    /// recordings sent each Enter 150 or 300 ms after the text, whatever the row showed, and Claude Code's output
    /// followed some drafts within 100 ms (the clear recording's last message, long-message's two), so that no quiet
    /// frame showed them; the window sends no Enter before a frame shows the text, and the quiet after its text would
    /// end one. So what was drawn in front of a frame's start ends a frame here, the chunk still cut at random and the
    /// queue asked between its pieces, and each message is queued just before its text appears.
    /// </summary>
    [Theory]
    [InlineData("clear-claude2.1.319-240x50.vt", "clear.script", 1)]
    [InlineData("clear-claude2.1.319-240x50.vt", "clear.script", 7)]
    [InlineData("steering-claude2.1.278-240x50.vt", "steering.script", 2)]
    [InlineData("long-message-claude2.1.278-240x50.vt", "long-message.script", 3)]
    public void Every_recorded_message_is_taken_and_none_given_back(string name, string script, int seed)
    {
        var messages = Messages(script);
        Assert.True(messages.Count >= 3, "the script's messages are read, slash commands with them");
        var flights = Fly(name, messages, seed, quietBeforeFrames: true);
        foreach (var (flight, outcome) in flights)
        {
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
        var flights = Fly("clear-claude2.1.319-240x50.vt", ["/clear"], seed: 5, quietBeforeFrames: false);
        var (flight, outcome) = flights.Single();
        Assert.Equal("taken", outcome);
        Assert.True(flight.Entered && flight.Done);
    }

    /// <summary>
    /// K1 and K2 at the three seconds' limit and at a question, on a screen drawn here (a row of 80 columns): the row
    /// that still shows the message gives it back; a row not drawn, or holding other text, is taken; a question drawn
    /// over the held draft gives it back, typed (the next send empties the row), and one drawn after the row was seen
    /// bare takes it.
    /// </summary>
    [Fact]
    public void A_flight_is_given_back_only_on_a_row_that_still_shows_the_message()
    {
        const string message = "Reply with the single word hello.";
        string Bare() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n$ {Esc}[?25h";
        string Held() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n$  {message}{Esc}[?25h";
        string Gone() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n{Esc}[?25h";
        string Other() => $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n$  something else{Esc}[?25h";
        string Question() => $"{Esc}[?25l\r\nPermission Required: Create file\r\nalpha.txt\r\nDo you want to create alpha.txt?\r\n1. Yes\r\n2. No\r\nSelect with numbers [1-2]. Then Enter to submit or Escape to cancel:{Esc}[?25h";

        (WriteCheck Check, GivenBack Why, bool Typed) Run(string after, bool questionNext)
        {
            var now = 0L;
            var model = new SessionModel(80, 20);
            Feed(model, Bare());
            var flight = new MessageFlight(model, message, () => null, () => now);
            Assert.Equal(WriteCheck.Goes, flight.First());
            Feed(model, Held());
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
            return (check, flight.Why, flight.Typed);
        }

        Assert.Equal((WriteCheck.Over, GivenBack.StillShown, true), Run(Held(), questionNext: false));
        Assert.Equal(WriteCheck.Skip, Run(Gone(), questionNext: false).Check);
        Assert.Equal(WriteCheck.Skip, Run(Other(), questionNext: false).Check);
        Assert.Equal(WriteCheck.Skip, Run(Bare(), questionNext: false).Check);
        Assert.Equal((WriteCheck.Over, GivenBack.AskedOverIt, true), Run(Held(), questionNext: true));
        Assert.Equal(WriteCheck.Skip, Run(Bare(), questionNext: true).Check);
    }

    /// <summary>K4: a text with a hidden character gets no Enter for a row that shows it without one, and never a second Enter.</summary>
    [Fact]
    public void A_text_with_a_hidden_character_gets_no_Enter_it_was_not_seen_with()
    {
        var now = 0L;
        var model = new SessionModel(80, 6);
        Feed(model, $"{Esc}[?25lclaude: Hi.\r\n\r\n$ {Esc}[?25h");
        var flight = new MessageFlight(model, "Reply with the​ single word hello.", () => null, () => now);
        Assert.Equal(WriteCheck.Goes, flight.First());
        // Claude Code drew it without the character (it takes such characters out of its prompt).
        Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n$  Reply with the single word hello.{Esc}[?25h");
        Assert.Equal(WriteCheck.NotYet, flight.Enter());
        now = 3000;
        Assert.Equal(WriteCheck.Over, flight.Enter());
        Assert.Equal(GivenBack.NotShown, flight.Why);

        var drawn = new SessionModel(80, 6);
        Feed(drawn, $"{Esc}[?25lclaude: Hi.\r\n\r\n$ {Esc}[?25h");
        var shown = new MessageFlight(drawn, "Reply with the‍ single word hello.", () => null, () => now);
        Assert.Equal(WriteCheck.Goes, shown.First());
        Feed(drawn, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: Hi.\r\n\r\n$  Reply with the‍ single word hello.{Esc}[?25h");
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

    /// <summary>What a frame starts with: the cursor hidden.</summary>
    private static readonly byte[] FrameStart = "\u001b[?25l"u8.ToArray();

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
    /// the recording had already moved past. With <paramref name="quietBeforeFrames"/> what was drawn in front of a
    /// frame's start (the cursor hidden) ends a frame of its own, as the quiet would have ended it, and the queues are
    /// asked while it stands: the recordings' scripts sent the Enter 150 ms after the text whatever the row showed, and
    /// Claude Code drew the draft and the next frame in one read; the window sends no Enter before a frame shows the
    /// draft, and the quiet after the text would end one.
    /// </summary>
    private static List<(MessageFlight Flight, string Outcome)> Fly(string name, IReadOnlyList<string> messages, int seed, bool quietBeforeFrames)
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
                // The quiet the window would have had: what was drawn in front of a frame's start ends a frame of its
                // own. No piece reaches over a frame's start, and the frame is ended right before it.
                if (quietBeforeFrames)
                {
                    if (bytes.AsSpan(at).StartsWith(FrameStart))
                    {
                        // That frame would have stood until the window's Enter: the queues are asked while it does.
                        model.EndFrame();
                        foreach (var waiting in flights.Where(one => one.Flight is not null && one.NextAsk != long.MaxValue))
                        {
                            waiting.NextAsk = Math.Min(waiting.NextAsk, ms);
                        }

                        Pump(ms);
                    }

                    var start = bytes.AsSpan(at + 1, offset + length - at - 1).IndexOf(FrameStart);
                    piece = start >= 0 ? Math.Min(piece, start + 1) : piece;
                }

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
