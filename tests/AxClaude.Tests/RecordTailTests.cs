using System.Text;
using AxClaude.Core.Record;

namespace AxClaude.Tests;

/// <summary>
/// The reader of a session file as Claude Code appends to it, <see cref="RecordTail"/>
/// (docs/axClaude/events-and-grounding.md EG-3.3, EG-3.6), over files a test writes line by line with invented texts
/// in the shapes of tests/fixtures/records, read through Claude Code's own line reader.
/// </summary>
public sealed class RecordTailTests
{
    [Fact]
    public void Lines_appended_are_returned_once_in_order()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "Name three colours."), Skipped(), Reply("a2", "Red, green and blue.")));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);

        var first = tail.Poll(0);
        Assert.Equal(new[] { "a1", "a2" }, first.Select(e => e.Id));
        Assert.Equal(new[] { RecordKind.Prompt, RecordKind.ReplyText }, first.Select(e => e.Kind));
        Assert.Equal("Red, green and blue.", first[1].Text);
        Assert.Empty(tail.Poll(50));

        Append(path, TurnEnd("a3"), Prompt("a4", "And three more."));
        Assert.Equal(new[] { "a3", "a4" }, tail.Poll(100).Select(e => e.Id));
        Assert.Empty(tail.Poll(150));
        Assert.Equal(RecordTailState.Following, tail.State);
        Assert.Equal((4, 1, 0, 0, 0, 0), (tail.Read, tail.Skipped, tail.NotJson, tail.TooLong, tail.Unknown, tail.Repeated));
    }

    [Fact]
    public void A_line_without_its_line_end_waits()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "One question.")));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
        Assert.Single(tail.Poll(0));

        // The next line arrives cut inside the two bytes of a letter, and the rest of it later with its line end.
        var reply = Encoding.UTF8.GetBytes(Reply("a2", "Café au lait.") + "\n");
        var cut = Array.IndexOf(reply, (byte)0xC3) + 1;
        AppendBytes(path, reply[..cut]);
        Assert.Empty(tail.Poll(50));
        Assert.Empty(tail.Poll(100));

        AppendBytes(path, reply[cut..]);
        var entries = tail.Poll(150);
        var entry = Assert.Single(entries);
        Assert.Equal("a2", entry.Id);
        Assert.Equal("Café au lait.", entry.Text);
        Assert.Equal(0, tail.NotJson);
    }

    [Fact]
    public void A_line_over_4_MB_is_passed_over_and_counted()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "Write a long story.")));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
        Assert.Single(tail.Poll(0));

        // A reply of more than 4 MB, which comes in two looks, then a line after it.
        var huge = Encoding.UTF8.GetBytes(Reply("a2", new string('x', RecordTail.MaxLineBytes)));
        var part = 3 * 1024 * 1024;
        AppendBytes(path, huge[..part]);
        Assert.Empty(tail.Poll(50));
        AppendBytes(path, [.. huge[part..], (byte)'\n']);
        Append(path, TurnEnd("a3"));

        var entry = Assert.Single(tail.Poll(100));
        Assert.Equal("a3", entry.Id);
        Assert.Equal(1, tail.TooLong);
        Assert.Equal(0, tail.NotJson);

        // One that is too long within a single look is passed over the same way.
        Append(path, Reply("a4", new string('y', RecordTail.MaxLineBytes)), Prompt("a5", "Shorter now."));
        Assert.Equal("a5", Assert.Single(tail.Poll(150)).Id);
        Assert.Equal(2, tail.TooLong);
        Assert.Equal(RecordTailState.Following, tail.State);
    }

    [Fact]
    public void A_file_that_shrank_is_read_again_and_seen_entries_are_dropped()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "First."), Reply("a2", "A first reply of some length."), TurnEnd("a3")));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
        Assert.Equal(3, tail.Poll(0).Count);

        // Written anew, shorter: the first line again and a line not seen.
        File.WriteAllText(path, Lines(Prompt("a1", "First."), TurnEnd("b9")));
        var entry = Assert.Single(tail.Poll(50));
        Assert.Equal("b9", entry.Id);
        Assert.Equal(1, tail.Repeated);

        Append(path, Reply("b10", "Then on."));
        Assert.Equal("b10", Assert.Single(tail.Poll(100)).Id);
    }

    [Fact]
    public void A_file_that_does_not_exist_yet_is_followed_once_it_does()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = Path.Combine(temp.Path, "project", "s.jsonl");
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
        Assert.Empty(tail.Poll(0));
        Assert.Equal(RecordTailState.Missing, tail.State);
        Assert.Empty(tail.Poll(5000));
        Assert.Equal(RecordTailState.Missing, tail.State);

        temp.File(Path.Combine("project", "s.jsonl"), Lines(Prompt("a1", "Hello there.")));
        Assert.Equal("a1", Assert.Single(tail.Poll(5050)).Id);
        Assert.Equal(RecordTailState.Following, tail.State);
    }

    [Fact]
    public void Reading_starts_at_the_offset_given()
    {
        using var temp = new TempFolder("axclaude-tail");
        var first = Prompt("a1", "Before the start.");
        var path = temp.File("s.jsonl", Lines(first, Reply("a2", "After the start.")));
        var start = Encoding.UTF8.GetByteCount(first) + 1;

        var tail = new RecordTail(path, start, ClaudeCodeTranscript.Parse);
        Assert.Equal("a2", Assert.Single(tail.Poll(0)).Id);

        // A start inside a line (the line was being written when the length was taken) passes over the rest of it.
        var inside = new RecordTail(path, start / 2, ClaudeCodeTranscript.Parse);
        Assert.Equal("a2", Assert.Single(inside.Poll(0)).Id);
        Assert.Equal((1, 0), (inside.Read, inside.NotJson));
    }

    [Fact]
    public void From_the_end_a_file_is_read_from_its_end_and_a_new_one_from_its_start()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "Old history."), Reply("a2", "Old reply.")));
        var tail = new RecordTail(path, RecordTail.FromEnd, ClaudeCodeTranscript.Parse);
        Assert.Empty(tail.Poll(0));
        Append(path, Prompt("a3", "New question."));
        Assert.Equal("a3", Assert.Single(tail.Poll(50)).Id);

        var later = Path.Combine(temp.Path, "later.jsonl");
        var fresh = new RecordTail(later, RecordTail.FromEnd, ClaudeCodeTranscript.Parse);
        Assert.Empty(fresh.Poll(0));
        temp.File("later.jsonl", Lines(Prompt("b1", "The first line of a new session."), Reply("b2", "Hello.")));
        Assert.Equal(new[] { "b1", "b2" }, fresh.Poll(50).Select(e => e.Id));
    }

    [Fact]
    public void Three_lines_that_are_no_json_stop_the_reader()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines("not json at all", """{"type":"user",""", Prompt("a1", "Still here."), "plain words", "more plain words"));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);

        // Two, a line read, two: never three in a row.
        Assert.Equal("a1", Assert.Single(tail.Poll(0)).Id);
        Assert.Equal(RecordTailState.Following, tail.State);
        Assert.Equal(4, tail.NotJson);

        Append(path, "a third in a row", Prompt("a2", "Never read."));
        Assert.Empty(tail.Poll(50));
        Assert.Equal(RecordTailState.Stopped, tail.State);
        Assert.Equal(RecordTailStop.NotJson, tail.StopReason);

        Append(path, Prompt("a3", "Nor this."));
        Assert.Empty(tail.Poll(100));
    }

    [Fact]
    public void More_than_ten_of_fifty_entries_without_their_fields_stop_the_reader()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", "");
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);

        // Ten without their fields, then fifty whole: the ten leave the last fifty, and ten more are still not too many.
        Append(path, [.. Enumerable.Range(1, 10).Select(i => Shapeless("u" + i))]);
        Append(path, [.. Enumerable.Range(1, 50).Select(i => TurnEnd("g" + i))]);
        Append(path, [.. Enumerable.Range(11, 10).Select(i => Shapeless("u" + i))]);
        var entries = tail.Poll(0);
        Assert.Equal(70, entries.Count);
        Assert.Equal(RecordTailState.Following, tail.State);
        Assert.Equal((50, 20), (tail.Read, tail.Unknown));

        // An eleventh among the last fifty.
        Append(path, Shapeless("u21"), TurnEnd("g51"));
        Assert.Equal("u21", Assert.Single(tail.Poll(50)).Id);
        Assert.Equal(RecordTailState.Stopped, tail.State);
        Assert.Equal(RecordTailStop.Fields, tail.StopReason);
        Assert.Empty(tail.Poll(100));
    }

    [Fact]
    public void A_file_that_cannot_be_opened_for_two_seconds_stops_the_reader()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = temp.File("s.jsonl", Lines(Prompt("a1", "Locked away.")));
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);

        // Held without sharing for less than two seconds: read once it is free, and the count starts again.
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Empty(tail.Poll(1000));
            Assert.Empty(tail.Poll(2500));
        }

        Assert.Equal("a1", Assert.Single(tail.Poll(2999)).Id);

        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Empty(tail.Poll(4000));
            Assert.Empty(tail.Poll(5999));
            Assert.Equal(RecordTailState.Following, tail.State);
            Assert.Empty(tail.Poll(6000));
            Assert.Equal(RecordTailState.Stopped, tail.State);
            Assert.Equal(RecordTailStop.Unreadable, tail.StopReason);
        }

        Append(path, Reply("a2", "Too late."));
        Assert.Empty(tail.Poll(6500));
    }

    [Fact]
    public void The_file_is_read_while_another_handle_writes_it()
    {
        using var temp = new TempFolder("axclaude-tail");
        var path = Path.Combine(temp.Path, "s.jsonl");

        // The writer shares reading only, as a program that appends may.
        using var writer = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
        Assert.Empty(tail.Poll(0));

        writer.Write(Encoding.UTF8.GetBytes(Lines(Prompt("a1", "Written while open."))));
        writer.Flush();
        Assert.Equal("a1", Assert.Single(tail.Poll(50)).Id);

        writer.Write(Encoding.UTF8.GetBytes(Lines(Reply("a2", "Still open."))));
        writer.Flush();
        Assert.Equal("a2", Assert.Single(tail.Poll(100)).Id);
        Assert.Equal(RecordTailState.Following, tail.State);
    }

    [Fact]
    public void ReadAll_reads_a_whole_file()
    {
        var path = Path.Combine(Fixtures.Folder(), "records", "plain-turn.jsonl");
        var entries = RecordTail.ReadAll(path, ClaudeCodeTranscript.Parse);
        Assert.Equal(
            new[] { RecordKind.Prompt, RecordKind.ReplyText, RecordKind.Other, RecordKind.TurnEnd, RecordKind.Note, RecordKind.Note, RecordKind.Note },
            entries.Select(e => e.Kind));
        Assert.Equal("Name three colours of a rainbow.", entries[0].Text);

        Assert.Empty(RecordTail.ReadAll(Path.Combine(Fixtures.Folder(), "records", "no-such-file.jsonl"), ClaudeCodeTranscript.Parse));
    }

    [Fact]
    public void ReadAll_stops_at_the_end_given()
    {
        var path = Path.Combine(Fixtures.Folder(), "records", "plain-turn.jsonl");
        var bytes = File.ReadAllBytes(path);

        // The end after the tenth line, the reply: the prompt and the reply. An end inside the next line reads no more.
        var end = 0;
        for (var line = 0; line < 10; line++)
        {
            end = Array.IndexOf(bytes, (byte)'\n', end) + 1;
        }

        Assert.Equal(new[] { RecordKind.Prompt, RecordKind.ReplyText }, RecordTail.ReadAll(path, ClaudeCodeTranscript.Parse, end).Select(e => e.Kind));
        Assert.Equal(new[] { RecordKind.Prompt, RecordKind.ReplyText }, RecordTail.ReadAll(path, ClaudeCodeTranscript.Parse, end + 20).Select(e => e.Kind));
    }

    // ---- Lines in the shapes of Claude Code's session file (tests/fixtures/records), texts invented ----

    internal static string Prompt(string id, string text) =>
        $$"""{"type":"user","uuid":"{{id}}","promptId":"p-{{id}}","promptSource":"typed","message":{"role":"user","content":"{{text}}"},"timestamp":"2026-11-16T00:13:01.942Z"}""";

    internal static string Reply(string id, string text) =>
        $$$"""{"type":"assistant","uuid":"{{{id}}}","message":{"id":"msg_{{{id}}}","role":"assistant","content":[{"type":"text","text":"{{{text}}}"}],"stop_reason":"end_turn"}}""";

    internal static string TurnEnd(string id) =>
        $$"""{"type":"system","subtype":"turn_duration","durationMs":1200,"uuid":"{{id}}"}""";

    /// <summary>A line of a kind that is skipped at its type.</summary>
    internal static string Skipped() => """{"type":"ai-title","aiTitle":"An invented title"}""";

    /// <summary>An entry of a kind read that lacks what its kind needs: an assistant entry with no message.</summary>
    internal static string Shapeless(string id) => $$"""{"type":"assistant","uuid":"{{id}}"}""";

    internal static string Lines(params string[] lines) => string.Concat(lines.Select(line => line + "\n"));

    internal static void Append(string path, params string[] lines) => File.AppendAllText(path, Lines(lines));

    private static void AppendBytes(string path, byte[] bytes)
    {
        using var file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        file.Write(bytes);
    }
}
