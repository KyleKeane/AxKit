using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// How the window reads Claude Code's prompt row for a message in flight (docs/axClaude/SPEC.md D33, "One message in
/// flight"; the seventh review of 2026-11-27, K3, K4 and L3): every reading is one whole frame's, a text with a hidden
/// character is held only by a row that shows the character, and a blank between two words on one row counts.
/// </summary>
public sealed class PromptRowReadingTests
{
    /// <summary>
    /// K3: every check of the window's queue reads one frame. The window asks between two chunks of the pseudo console
    /// and inside a frame, whenever its pump's timer comes round; until the seventh review the prompt row was read at
    /// the live cursor, which the parser had already moved down into the next frame, over rows of the frame before:
    /// trust-banner-fast at 8119 ms read the message and the banner under it while the frame showed the row bare, and
    /// a reading mid-frame of early-two-lines held one of the message's two lines. Replayed as the window gets them,
    /// a frame ending at the cursor shown again or after 100 ms of quiet, each chunk cut at random: every reading is
    /// the one of the last frame's end.
    /// </summary>
    [Theory]
    [InlineData("trust-banner-fast-claude2.1.309-240x50.vt", "Reply with the single word hello.", 1)]
    [InlineData("trust-banner-fast-claude2.1.309-240x50.vt", "Reply with the single word hello.", 2)]
    [InlineData("early-two-lines-claude2.1.309-240x50.vt", "What is 2 plus 2?\nAnswer in one line.", 1)]
    [InlineData("early-two-lines-claude2.1.309-240x50.vt", "What is 2 plus 2?\nAnswer in one line.", 3)]
    [InlineData("clear-claude2.1.319-240x50.vt", "/clear", 1)]
    [InlineData("long-message-claude2.1.278-240x50.vt", "-", 4)]
    public void Every_reading_is_the_one_of_the_last_frames_end(string name, string message, int seed)
    {
        var differs = new List<string>();
        var bareAt8119 = false;
        WindowReplay.Run(name, seed, (ms, live, atFrameEnd) =>
        {
            var reading = Reading(live, message);
            var wanted = Reading(atFrameEnd, message);
            if (reading != wanted)
            {
                differs.Add($"{ms} ms: {reading}, the frame's end read {wanted}");
            }

            bareAt8119 |= ms == 8119 && live.PromptDraft is { Length: 0 };
        });

        Assert.True(differs.Count == 0, $"{differs.Count} readings were not the frame's: " + string.Join("; ", differs.Take(3)));
        if (name.StartsWith("trust-banner-fast", StringComparison.Ordinal))
        {
            Assert.True(bareAt8119, "trust-banner-fast's row reads bare at 8119 ms, as its frame shows it");
        }
    }

    private static string Reading(SessionModel model, string message) =>
        $"draft {Shown(model.PromptDraft)}, asks {model.PromptPending}, holds {message != "-" && model.PromptHolds(message)}";

    private static string Shown(string? draft) => draft is null ? "none" : $"\"{(draft.Length > 40 ? draft[..40] + "..." : draft).Replace("\n", "|")}\"";

    /// <summary>
    /// K4: a text that holds a hidden character is on the row only when the row shows it with that character. Claude
    /// Code takes such a character out of a prompt ("Removed 1 invisible character from your prompt") and may send a
    /// text the user did not hear; until the seventh review the comparison took the hidden characters out of both
    /// sides, and the Enter went for a row that showed the text without them. A text without one is compared as before.
    /// </summary>
    [Fact]
    public void A_text_with_a_hidden_character_is_held_only_by_a_row_that_shows_it()
    {
        var plain = new SessionModel(80, 6);
        Feed(plain, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} Reply with the single word hello.{Esc}[?25h");
        Assert.False(plain.PromptHolds("Reply with the single​ word hello."));
        Assert.False(plain.PromptHolds("Reply with the single word hello.⁠"));
        Assert.True(plain.PromptHolds("Reply with the single word hello."));

        var drawn = new SessionModel(80, 6);
        Feed(drawn, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} Reply with the‍ single word hello.{Esc}[?25h");
        Assert.True(drawn.PromptHolds("Reply with the‍ single word hello."));
        // A row that shows a hidden character holds the text as it was typed, and still holds the text without it.
        Assert.True(drawn.PromptHolds("Reply with the single word hello."));
        Assert.False(drawn.PromptHolds("Reply with the single‍ word hello."));
    }

    /// <summary>
    /// L3: "exactly the text given back" keeps a blank between two words that stand on one row ("a b" is not "ab"); only
    /// a row join may lose one, where the screen's width breaks the draft. Until the seventh review every blank was taken
    /// out of both sides.
    /// </summary>
    [Fact]
    public void A_blank_between_two_words_on_one_row_counts()
    {
        var model = new SessionModel(80, 6);
        Feed(model, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} Reply with the single word hello.{Esc}[?25h");
        Assert.False(model.PromptHolds("Replywith the single word hello."));
        Assert.False(model.PromptHolds("Reply with the singleword hello."));
        Assert.True(model.PromptHolds("Reply  with the single word hello."));

        // A draft broken at the width (20 columns): the blank at the break is the row join's, and a word broken in
        // two is joined without one. A blank inside a row still counts there; at the join the row cannot tell.
        var narrow = new SessionModel(20, 8);
        Feed(narrow, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} Reply with the\r\nsingle word hel\r\nlo.{Esc}[?25h");
        Assert.True(narrow.PromptHolds("Reply with the single word hello."));
        Assert.False(narrow.PromptHolds("Reply with the singleword hello."));
        Assert.False(narrow.PromptHolds("Replywith the single word hello."));
    }

    /// <summary>
    /// L3: a line Claude Code drew as a paste is held by its mark; two marks of one draft are two pastes, numbered in
    /// order, so a mark drawn twice does not stand for two lines.
    /// </summary>
    [Fact]
    public void Paste_marks_of_one_draft_are_numbered_in_order()
    {
        var two = new SessionModel(80, 8);
        Feed(two, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} [Pasted text #1]\r\n[Pasted text #2]{Esc}[?25h");
        Assert.True(two.PromptHolds("first line\nsecond line"));

        var repeated = new SessionModel(80, 8);
        Feed(repeated, $"{Esc}[?25lclaude: Hi.\r\n\r\n${ClaudeCodeTexts.DraftSpace} [Pasted text #1]\r\n[Pasted text #1]{Esc}[?25h");
        Assert.False(repeated.PromptHolds("first line\nsecond line"));
    }
}

/// <summary>
/// A recording replayed as the window's pseudo console hands it over (the seventh review of 2026-11-27, K3): each chunk at
/// its recorded time, cut at random into pieces; a frame ends at the cursor shown again (inside Feed) or after 100 ms
/// of quiet (EndFrame, as the window's quiet timer). After each piece and each quiet end the caller reads the model as
/// the window's pump would, beside a second model fed only up to the last frame's end, which is what one whole frame
/// shows.
/// </summary>
internal static class WindowReplay
{
    private static readonly byte[] FrameEnd = "\u001b[?25h"u8.ToArray();

    public static void Run(string name, int seed, Action<long, SessionModel, SessionModel> look)
    {
        var (columns, rows) = FixtureSize(name);
        var bytes = Fixture(name);
        var live = new SessionModel(columns, rows);
        var atFrameEnd = new SessionModel(columns, rows);
        var fed = 0;
        var frameEnd = 0;
        // The frames the quiet ended, by where they ended: the second model ends them too, and only them, so that it
        // has every frame end the first had and no other.
        var quietEnds = new Queue<int>();
        var random = new Random(seed);
        void Look(long ms)
        {
            while (quietEnds.Count > 0 && quietEnds.Peek() <= frameEnd)
            {
                var quiet = quietEnds.Dequeue();
                atFrameEnd.Feed(bytes.AsSpan(fed, quiet - fed));
                atFrameEnd.EndFrame();
                fed = quiet;
            }

            if (frameEnd > fed)
            {
                atFrameEnd.Feed(bytes.AsSpan(fed, frameEnd - fed));
                fed = frameEnd;
            }

            look(ms, live, atFrameEnd);
        }

        long previous = -1;
        foreach (var (offset, length, ms) in Chunks(name))
        {
            if (previous >= 0 && ms - previous >= 100)
            {
                live.EndFrame();
                frameEnd = offset;
                quietEnds.Enqueue(offset);
                Look(previous + 100);
            }

            var at = offset;
            while (at < offset + length)
            {
                var piece = Math.Min(random.Next(1, Math.Max(2, length / 2 + 1)), offset + length - at);
                live.Feed(bytes.AsSpan(at, piece));
                var from = Math.Max(0, at - FrameEnd.Length + 1);
                at += piece;
                var end = bytes.AsSpan(from, at - from).LastIndexOf(FrameEnd);
                if (end >= 0)
                {
                    frameEnd = Math.Max(frameEnd, from + end + FrameEnd.Length);
                }

                Look(ms);
            }

            previous = ms;
        }

        live.EndFrame();
        frameEnd = bytes.Length;
        quietEnds.Enqueue(bytes.Length);
        Look(previous + 100);
    }
}
