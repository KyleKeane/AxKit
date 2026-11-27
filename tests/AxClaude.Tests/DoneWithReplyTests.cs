using System.Text;
using AxClaude.Core.Flow;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;
using AxKit.TestShared;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// "Done" first, then the reply's last lines, as one utterance (SPEC.md FR-7.2a), with the user's silent words
/// (FR-7.4a), replayed from a turn Haiku answered in a second (Claude Code 2.1.332, 2026-11-26): the end marks and the
/// bell arrive right after the spinner, and the reply's rows and the turn's summary row come in the one frame after
/// the bell. With the turn's stop event before its end mark, as Claude Code's hooks deliver it, the core decides Done
/// on that very frame; the window held the frame's lines only after handing the frame to the core, so Done went
/// alone and the line after it (found replaying this recording for tests/scripts/axClaude/17-what-is-spoken.txt,
/// "Done. green apples"; what that script met live the same evening was the usage row, UsageLimitRowTests).
/// The replay feeds the model, Arrivals and the core in the window's order (MainForm.Model.cs, OnTranscriptChanged):
/// the frame's lines held, then the frame to the core, whose Done speaks what is held after its word.
/// </summary>
public class DoneWithReplyTests
{
    private const string Fixture = "silent-words-claude2.1.332-240x50.vt";

    private const string Message = "Reply with exactly two lines and nothing else: first line the words pelican feathers, second line the words green apples.";

    /// <summary>The window's hold of reply speech (MainForm.Area.cs, SpeechHoldMs) and how long a bell keeps it for its Done (DoneFirstMs).</summary>
    private const int SpeechHoldMs = 250;

    private const int DoneFirstMs = 800;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Done_is_said_with_the_reply_drawn_in_the_frame_after_the_bell(bool stopEventFirst)
    {
        var said = Replay(stopEventFirst, ["pelican", "walrus"]);

        Assert.Equal("Done. green apples", said[0]);
        Assert.DoesNotContain(said, text => text.Contains("feathers", StringComparison.Ordinal));
    }

    [Fact]
    public void With_the_stop_event_first_the_core_decides_Done_on_the_frame_that_brings_the_reply()
    {
        // The fact behind the window's order: Done comes inside the frame's own input, after the speech of its lines.
        var said = Replay(stopEventFirst: true, [], out var doneOnReplyFrame);

        Assert.True(doneOnReplyFrame);
        Assert.Equal("Done. claude: pelican feathers. green apples", said[0]);
    }

    [Fact]
    public void The_window_holds_a_frames_lines_before_the_core_hears_of_the_frame()
    {
        var file = Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxClaude", "MainForm.Model.cs");
        var code = File.ReadAllText(file);
        var hold = code.IndexOf("_heldLines.AddRange(arrival.SpokenLines);", StringComparison.Ordinal);
        var core = code.IndexOf("FlowFrame(arrival);", StringComparison.Ordinal);
        Assert.True(hold > 0 && core > 0 && hold < core, "OnTranscriptChanged holds the frame's reply lines before FlowFrame, or a Done the core decides on that frame goes without them (FR-7.2a).");
    }

    private static List<string> Replay(bool stopEventFirst, IReadOnlyList<string> silentWords) => Replay(stopEventFirst, silentWords, out _);

    private static List<string> Replay(bool stopEventFirst, IReadOnlyList<string> silentWords, out bool doneOnReplyFrame)
    {
        var (columns, rows) = FixtureSize(Fixture);
        var model = new SessionModel(columns, rows);
        var arrivals = new Arrivals();
        var flow = new SessionFlow(new SourceHealth());
        var said = new List<string>();
        var held = new List<Line>();
        long now = 0;
        long wake = long.MaxValue;
        long holdAt = long.MaxValue;
        long doneAwaitedSince = 0;
        var ready = false;
        var inFrame = false;
        var doneInFrame = false;

        void Speak(string? first)
        {
            holdAt = long.MaxValue;
            var text = new StringBuilder();
            foreach (var line in SpeechFilter.Spoken(held, silentWords))
            {
                Utterance.Append(text, Arrivals.SpokenText(line), line.JoinedToPrevious);
            }

            if (first is not null)
            {
                text.Insert(0, text.Length > 0 ? first + ". " : first);
            }

            if (text.Length > 0)
            {
                said.Add(text.ToString());
            }

            held.Clear();
        }

        void Handle(FlowInput? input)
        {
            if (input is null)
            {
                return;
            }

            foreach (var decision in flow.Handle(input, now))
            {
                if (decision is WakeAt wakeAt)
                {
                    wake = wakeAt.AtMs;
                }
                else if (decision is SayDone)
                {
                    doneAwaitedSince = 0;
                    doneInFrame |= inFrame && held.Count > 0;
                    Speak("Done");
                }
            }
        }

        void Until(long at)
        {
            while (Math.Min(wake, holdAt) <= at)
            {
                if (holdAt <= wake)
                {
                    now = holdAt;
                    holdAt = long.MaxValue;
                    if (doneAwaitedSince != 0 && now - doneAwaitedSince < DoneFirstMs)
                    {
                        // FR-7.2a: the hold asks again on its next tick while a Done is awaited.
                        holdAt = now + SpeechHoldMs;
                    }
                    else
                    {
                        Speak(null);
                    }
                }
                else
                {
                    now = wake;
                    wake = long.MaxValue;
                    Handle(new Tick());
                }
            }
        }

        model.Marked += letter =>
        {
            if (stopEventFirst && letter == 'D')
            {
                // Claude Code's stop event, 12 to 130 ms before the end mark (measured; SessionFlow.Stopped).
                Handle(new TurnStopped(RecordText.Letters("pelican feathers\ngreen apples"), []));
            }

            Handle(FlowFeed.FromMark(letter));
        };
        model.Bell += () =>
        {
            doneAwaitedSince = now;
            Handle(new BellSeen());
        };
        model.Changed += () =>
        {
            var arrival = arrivals.Update(model.Lines, model.Working, ready ? ReplySpeechMode.All : ReplySpeechMode.None, toolCalls: false);
            if (arrival.Speech is not null)
            {
                held.AddRange(arrival.SpokenLines);
                holdAt = now + SpeechHoldMs;
            }

            inFrame = true;
            Handle(FlowFeed.FromFrame(model, arrival, model.PromptPending, ready));
            inFrame = false;
            if (!ready && model.Mode is not null && !model.Working && !model.PromptPending)
            {
                ready = true;
                arrivals.Skip(model.Lines);
                model.MarkReplayedExchanges();
            }
        };

        Handle(new ProcessStarted());
        if (stopEventFirst)
        {
            // The session file followed: the stop event's last message stands in for its entry (EG-D-11).
            Handle(new RecordFound());
        }

        var bytes = AxKit.TestShared.Fixtures.Bytes(Fixture);
        var previous = 0;
        var sent = false;
        foreach (var chunk in Chunks(Fixture))
        {
            Until(chunk.Ms);
            if (chunk.Ms - previous >= 100)
            {
                model.EndFrame();
            }

            now = chunk.Ms;
            var data = bytes.AsSpan(chunk.Offset, chunk.Length);
            if (!sent && Encoding.UTF8.GetString(data).Contains(Message[..30], StringComparison.Ordinal))
            {
                sent = true;
                model.Send(Message);
                Handle(new UserActed(UserAction.Sent, FlowFeed.SentKey(Message)));
            }

            model.Feed(data);
            previous = chunk.Ms;
        }

        model.EndFrame();
        Until(now + 2000);
        Assert.True(sent);
        doneOnReplyFrame = doneInFrame;
        return said;
    }
}
