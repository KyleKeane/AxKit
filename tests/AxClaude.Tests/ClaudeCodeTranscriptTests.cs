using System.Text;
using AxClaude.Core;
using AxClaude.Core.Record;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's session file read through its one place, <see cref="ClaudeCodeTranscript"/>
/// (docs/axClaude/events-and-grounding.md EG-3.1, EG-3.4, EG-3.5), over the lines written by hand in
/// tests/fixtures/records, whose README says what each file shows. Every text in them is invented; the words
/// MUST-NOT-BE-READ stand in every line of a kind that is skipped and in every field a kind read must not read.
/// </summary>
public sealed class ClaudeCodeTranscriptTests
{
    private const string Forbidden = "MUST-NOT-BE-READ";

    private static readonly string[] Files = ["plain-turn.jsonl", "tool-turn.jsonl", "interrupt.jsonl", "queued.jsonl", "odd-lines.jsonl"];

    private static readonly IReadOnlyDictionary<string, string> NoEnvironment = new Dictionary<string, string>();

    [Fact]
    public void A_typed_prompt_a_reply_and_the_turn_end_are_read_in_file_order()
    {
        var parsed = ParseFile("plain-turn.jsonl");
        Assert.Equal(18, parsed.Count);
        Assert.Equal(11, parsed.Count(p => p.What == RecordLine.Skipped));

        var entries = Entries("plain-turn.jsonl");
        Assert.Equal(
            new[] { RecordKind.Prompt, RecordKind.ReplyText, RecordKind.Other, RecordKind.TurnEnd, RecordKind.Note, RecordKind.Note, RecordKind.Note },
            entries.Select(e => e.Kind));
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "10000000-0000-4000-8000-000000000001", TurnKey: "10000000-0000-4000-8000-0000000000b1",
                Text: "Name three colours of a rainbow.", Origin: PromptOrigin.Typed, Time: new DateTime(2026, 11, 16, 0, 13, 1, 942, DateTimeKind.Utc)),
            entries[0]);
        Assert.Equal(DateTimeKind.Utc, entries[0].Time!.Value.Kind);
        Assert.Equal(
            new RecordEntry(RecordKind.ReplyText, "10000000-0000-4000-8000-000000000003", MessageId: "msg_01PlainTurnColours",
                Text: "# Colours\n\nRed, **green** and blue.", Time: new DateTime(2026, 11, 16, 0, 13, 4, 933, DateTimeKind.Utc)),
            entries[1]);

        // The Stop hook's summary is a system entry of another subtype: its id and nothing else.
        Assert.Equal(new RecordEntry(RecordKind.Other, "10000000-0000-4000-8000-000000000004"), entries[2]);
        Assert.Equal(new RecordEntry(RecordKind.TurnEnd, "10000000-0000-4000-8000-000000000005", DurationMs: 2345), entries[3]);
        Assert.All(parsed.Where(p => p.Entry is not null), p => Assert.Equal(RecordLine.Read, p.What));
    }

    [Fact]
    public void One_entry_for_each_content_block_text_tool_call_thinking()
    {
        var entries = Entries("tool-turn.jsonl");
        Assert.Equal(
            new[]
            {
                RecordKind.Prompt, RecordKind.Thinking, RecordKind.ReplyText, RecordKind.ToolCall, RecordKind.ToolResult,
                RecordKind.ToolCall, RecordKind.ToolResult, RecordKind.ReplyText, RecordKind.TurnEnd,
            },
            entries.Select(e => e.Kind));

        // Nothing of a thinking block is read; the text and the call of one response share its message id.
        Assert.Equal(new RecordEntry(RecordKind.Thinking, "20000000-0000-4000-8000-000000000003"), entries[1]);
        Assert.Equal(
            new RecordEntry(RecordKind.ReplyText, "20000000-0000-4000-8000-000000000004", MessageId: "msg_02FirstResponse",
                Text: "Starting now.", Time: new DateTime(2026, 11, 16, 1, 0, 1, 1, DateTimeKind.Utc)),
            entries[2]);
        Assert.Equal(
            new RecordEntry(RecordKind.ToolCall, "20000000-0000-4000-8000-000000000005", MessageId: "msg_02FirstResponse",
                ToolName: "Bash", ToolUseId: "toolu_02ListFiles"),
            entries[3]);
        Assert.Equal(
            new RecordEntry(RecordKind.ToolCall, "20000000-0000-4000-8000-000000000007", MessageId: "msg_02SecondResponse",
                ToolName: "Read", ToolUseId: "toolu_02ReadNotes"),
            entries[5]);
        Assert.Equal("finished", entries[7].Text);
        Assert.Equal("msg_02ThirdResponse", entries[7].MessageId);
        Assert.Equal(4012, entries[8].DurationMs);
    }

    [Fact]
    public void A_tool_result_carries_its_calls_id_and_error_flag_and_never_its_text()
    {
        var results = Entries("tool-turn.jsonl").Where(e => e.Kind == RecordKind.ToolResult).ToList();
        Assert.Equal(
            new[]
            {
                new RecordEntry(RecordKind.ToolResult, "20000000-0000-4000-8000-000000000006", TurnKey: "20000000-0000-4000-8000-0000000000b1",
                    ToolUseId: "toolu_02ListFiles"),
                new RecordEntry(RecordKind.ToolResult, "20000000-0000-4000-8000-000000000008", TurnKey: "20000000-0000-4000-8000-0000000000b1",
                    ToolUseId: "toolu_02ReadNotes", IsError: true),
            },
            results);

        // The call each result answers is in the record before it.
        var calls = Entries("tool-turn.jsonl").Where(e => e.Kind == RecordKind.ToolCall).Select(e => e.ToolUseId).ToList();
        Assert.Equal(calls, results.Select(e => e.ToolUseId));
    }

    [Fact]
    public void Skipped_kinds_return_null_and_nothing_of_their_text()
    {
        var skipped = 0;
        var read = 0;
        foreach (var file in Files)
        {
            foreach (var line in ParseFile(file))
            {
                if (line.What is RecordLine.Skipped or RecordLine.NotJson)
                {
                    Assert.Null(line.Entry);
                    // The fixture's promise: every line skipped holds the words it must not give away.
                    Assert.Contains(Forbidden, line.Text);
                    skipped += line.What == RecordLine.Skipped ? 1 : 0;
                    continue;
                }

                Assert.NotNull(line.Entry);
                Assert.DoesNotContain(Forbidden, line.Entry!.ToString());
                read++;
            }
        }

        Assert.Equal(17, skipped);
        Assert.Equal(59, read);

        // The printed record holds the fields that are read, so that the search above would find a forbidden one.
        Assert.Contains("Name three colours of a rainbow.", Entries("plain-turn.jsonl")[0].ToString());
    }

    [Fact]
    public void Property_order_does_not_matter()
    {
        var inOrder = Parse("""{"type":"assistant","uuid":"u1","timestamp":"2026-11-16T00:13:04.933Z","isAbortedMidStream":true,"message":{"id":"msg_1","content":[{"type":"text","text":"ok"}],"stop_reason":null}}""");
        var reordered = Parse("""{"message":{"stop_reason":null,"content":[{"text":"ok","type":"text"}],"id":"msg_1"},"isAbortedMidStream":true,"timestamp":"2026-11-16T00:13:04.933Z","uuid":"u1","type":"assistant"}""");
        Assert.Equal(new RecordEntry(RecordKind.ReplyText, "u1", MessageId: "msg_1", Text: "ok", Cut: true, Time: new DateTime(2026, 11, 16, 0, 13, 4, 933, DateTimeKind.Utc)), inOrder);
        Assert.Equal(inOrder, reordered);

        Assert.Equal(
            Parse("""{"type":"user","uuid":"u2","promptId":"p","isMeta":true,"promptSource":"system","message":{"content":"<task-notification>x</task-notification>"}}"""),
            Parse("""{"message":{"content":"<task-notification>x</task-notification>"},"promptSource":"system","isMeta":true,"promptId":"p","uuid":"u2","type":"user"}"""));
        Assert.Equal(
            Parse("""{"type":"queue-operation","operation":"enqueue","content":"later"}"""),
            Parse("""{"content":"later","operation":"enqueue","type":"queue-operation"}"""));

        // The fixture's prompt with its type last, and JSON's escapes in its text undone.
        var prompt = Entries("odd-lines.jsonl").Single(e => e.Id == "50000000-0000-4000-8000-000000000001");
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "50000000-0000-4000-8000-000000000001", TurnKey: "50000000-0000-4000-8000-0000000000b1",
                Text: "Café au lait,\nplease.", Origin: PromptOrigin.Typed, Time: new DateTime(2026, 11, 16, 4, 0, 0, DateTimeKind.Utc)),
            prompt);
    }

    [Fact]
    public void A_line_that_is_no_json_is_counted_not_thrown()
    {
        var odd = ParseFile("odd-lines.jsonl");
        Assert.All(odd.Take(4), line =>
        {
            Assert.Equal(RecordLine.NotJson, line.What);
            Assert.Null(line.Entry);
        });

        foreach (var text in new[] { "", "   ", "{", "}", "null", "42", "\"user\"", """{"type":"user",}""", """{"type":"user"}{"type":"user"}""" })
        {
            Assert.Null(ClaudeCodeTranscript.Parse(Encoding.UTF8.GetBytes(text), 0, out var what));
            Assert.Equal(RecordLine.NotJson, what);
        }

        // A text that is no UTF-8.
        byte[] broken = [.. Encoding.UTF8.GetBytes("""{"type":"user","uuid":"u","message":{"content":"a"""), 0xFF, 0xFE, .. Encoding.UTF8.GetBytes("""b"}}""")];
        Assert.Null(ClaudeCodeTranscript.Parse(broken, 0, out var brokenWhat));
        Assert.Equal(RecordLine.NotJson, brokenWhat);

        // A line end and a byte order mark around a line are no reason to refuse it.
        byte[] marked = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("{\"type\":\"user\",\"uuid\":\"u\",\"message\":{\"content\":\"hi\"}}\r")];
        Assert.Equal(RecordKind.Prompt, ClaudeCodeTranscript.Parse(marked, 0, out var markedWhat)?.Kind);
        Assert.Equal(RecordLine.Read, markedWhat);
    }

    [Fact]
    public void A_missing_uuid_gets_a_local_id_from_the_offset()
    {
        var line = ParseFile("odd-lines.jsonl").Single(p => p.Entry?.Text == "A prompt without its id.");
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "@" + line.Offset, TurnKey: "50000000-0000-4000-8000-0000000000b2",
                Text: "A prompt without its id.", Origin: PromptOrigin.Typed),
            line.Entry);
        Assert.True(line.Offset > 0);

        // It is read, and counted as an entry that lacked what the table needs (EG-3.6).
        Assert.Equal(RecordLine.Unknown, line.What);

        // A queue operation never has a uuid: its local id is no fault.
        var queue = ParseFile("queued.jsonl").Where(p => p.Entry?.Kind is RecordKind.Enqueued or RecordKind.Dequeued or RecordKind.Removed).ToList();
        Assert.Equal(6, queue.Count);
        Assert.All(queue, p =>
        {
            Assert.Equal("@" + p.Offset, p.Entry!.Id);
            Assert.Equal(RecordLine.Read, p.What);
        });
        Assert.Equal(queue.Count, queue.Select(p => p.Entry!.Id).Distinct().Count());
    }

    [Fact]
    public void An_unknown_block_or_subtype_is_Other()
    {
        var odd = ParseFile("odd-lines.jsonl");

        // Known kinds with a block, a subtype or an operation not known: Other, read.
        foreach (var id in new[]
        {
            "50000000-0000-4000-8000-000000000003", // a block of another type
            "50000000-0000-4000-8000-000000000005", // a subtype not known
            "50000000-0000-4000-8000-000000000006", // a local command's output
            "50000000-0000-4000-8000-000000000015", // a tag not known
            "50000000-0000-4000-8000-000000000016", // a text and a picture
        })
        {
            var line = odd.Single(p => p.Entry?.Id == id);
            Assert.Equal(new RecordEntry(RecordKind.Other, id), line.Entry);
            Assert.Equal(RecordLine.Read, line.What);
        }

        var shuffle = odd.Single(p => p.Text.Contains("shuffle", StringComparison.Ordinal));
        Assert.Equal(new RecordEntry(RecordKind.Other, "@" + shuffle.Offset), shuffle.Entry);
        Assert.Equal(RecordLine.Read, shuffle.What);

        // Without what the table needs (a message, a block, a content): Other, and counted.
        foreach (var id in new[] { "50000000-0000-4000-8000-000000000002", "50000000-0000-4000-8000-000000000004", "50000000-0000-4000-8000-000000000017" })
        {
            var line = odd.Single(p => p.Entry?.Id == id);
            Assert.Equal(new RecordEntry(RecordKind.Other, id), line.Entry);
            Assert.Equal(RecordLine.Unknown, line.What);
        }

        // An agent's entry is passed on as Other, marked.
        Assert.Equal(new RecordEntry(RecordKind.Other, "50000000-0000-4000-8000-000000000007", Sidechain: true),
            odd.Single(p => p.Entry?.Id == "50000000-0000-4000-8000-000000000007").Entry);

        // A duration written as a decimal is still the duration.
        Assert.Equal(new RecordEntry(RecordKind.TurnEnd, "50000000-0000-4000-8000-000000000018", DurationMs: 1234),
            odd.Single(p => p.Entry?.Id == "50000000-0000-4000-8000-000000000018").Entry);

        // Types that are not read, also a type that is no string and a line with no type: skipped.
        Assert.Equal(3, odd.Count(p => p.What == RecordLine.Skipped));
    }

    [Fact]
    public void Queue_operations_are_read_with_the_enqueued_text()
    {
        var entries = Entries("queued.jsonl");
        var queue = entries.Where(e => e.Kind is RecordKind.Enqueued or RecordKind.Dequeued or RecordKind.Removed).ToList();
        Assert.Equal(
            new[] { RecordKind.Enqueued, RecordKind.Dequeued, RecordKind.Enqueued, RecordKind.Removed, RecordKind.Enqueued, RecordKind.Dequeued },
            queue.Select(e => e.Kind));
        Assert.Equal(
            new[] { "Then say the date.", null, "Never mind.", null, "<task-notification>A background task finished.</task-notification>", null },
            queue.Select(e => e.Text));

        // The queued message is taken up as a prompt of its own origin, with the text that was queued.
        var queued = entries.Single(e => e.Kind == RecordKind.Prompt && e.Origin == PromptOrigin.Queued);
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "40000000-0000-4000-8000-000000000004", TurnKey: "40000000-0000-4000-8000-0000000000b2",
                Text: "Then say the date.", Origin: PromptOrigin.Queued, Time: new DateTime(2026, 11, 16, 3, 0, 3, 300, DateTimeKind.Utc)),
            queued);
        Assert.Equal(queue[0].Text, queued.Text);

        // In file order: the enqueue comes before the reply it was sent during.
        Assert.Equal(new[] { RecordKind.Prompt, RecordKind.Enqueued, RecordKind.ReplyText, RecordKind.TurnEnd, RecordKind.Dequeued, RecordKind.Prompt },
            entries.Take(6).Select(e => e.Kind));
    }

    [Fact]
    public void An_interrupt_and_a_cut_reply_are_told()
    {
        var entries = Entries("interrupt.jsonl");
        Assert.Equal(
            new[]
            {
                RecordKind.Prompt, RecordKind.ReplyText, RecordKind.Interrupt, RecordKind.Prompt, RecordKind.ToolCall,
                RecordKind.ToolResult, RecordKind.Interrupt,
            },
            entries.Select(e => e.Kind));
        Assert.Equal(
            new RecordEntry(RecordKind.ReplyText, "30000000-0000-4000-8000-000000000002", MessageId: "msg_03CutPoem",
                Text: "The sea is wide and", Cut: true, Time: new DateTime(2026, 11, 16, 2, 0, 2, DateTimeKind.Utc)),
            entries[1]);
        Assert.Equal(new RecordEntry(RecordKind.Interrupt, "30000000-0000-4000-8000-000000000003", TurnKey: "30000000-0000-4000-8000-0000000000b1"), entries[2]);
        Assert.Equal(
            new RecordEntry(RecordKind.ToolResult, "30000000-0000-4000-8000-000000000006", TurnKey: "30000000-0000-4000-8000-0000000000b2",
                ToolUseId: "toolu_03Slow", IsError: true),
            entries[5]);
        Assert.Equal(new RecordEntry(RecordKind.Interrupt, "30000000-0000-4000-8000-000000000007", TurnKey: "30000000-0000-4000-8000-0000000000b2"), entries[6]);

        // A reply that was not cut says so.
        Assert.False(Entries("plain-turn.jsonl")[1].Cut);

        // The interrupt's words as a plain text are taken the same way.
        Assert.Equal(RecordKind.Interrupt, Parse("""{"type":"user","uuid":"u","message":{"content":"[Request interrupted by user]"}}""")?.Kind);
    }

    [Fact]
    public void A_compact_summary_a_meta_entry_and_a_command_are_no_prompts()
    {
        var odd = Entries("odd-lines.jsonl");
        RecordEntry ById(string id) => odd.Single(e => e.Id == id);

        // /compact: the command written with escaped tags, the boundary, the summary, the command's output.
        Assert.Equal(new RecordEntry(RecordKind.Note, "50000000-0000-4000-8000-000000000008", TurnKey: "50000000-0000-4000-8000-0000000000b3"),
            ById("50000000-0000-4000-8000-000000000008"));
        Assert.Equal(new RecordEntry(RecordKind.CompactBoundary, "50000000-0000-4000-8000-000000000009"), ById("50000000-0000-4000-8000-000000000009"));
        Assert.Equal(new RecordEntry(RecordKind.CompactSummary, "50000000-0000-4000-8000-000000000010"), ById("50000000-0000-4000-8000-000000000010"));
        Assert.Equal(RecordKind.Note, ById("50000000-0000-4000-8000-000000000011").Kind);

        // A shell line and its output, and an agent's message.
        Assert.Equal(new RecordEntry(RecordKind.Note, "50000000-0000-4000-8000-000000000012", TurnKey: "50000000-0000-4000-8000-0000000000b4"),
            ById("50000000-0000-4000-8000-000000000012"));
        Assert.Equal(RecordKind.Note, ById("50000000-0000-4000-8000-000000000013").Kind);
        Assert.Equal(RecordKind.Note, ById("50000000-0000-4000-8000-000000000014").Kind);

        // /exit: the caveat Claude Code marks as its own, the command, its output.
        var exit = Entries("plain-turn.jsonl").Where(e => e.Kind == RecordKind.Note).ToList();
        Assert.Equal(
            new[]
            {
                new RecordEntry(RecordKind.Note, "10000000-0000-4000-8000-000000000006", TurnKey: "10000000-0000-4000-8000-0000000000b2"),
                new RecordEntry(RecordKind.Note, "10000000-0000-4000-8000-000000000007", TurnKey: "10000000-0000-4000-8000-0000000000b2"),
                new RecordEntry(RecordKind.Note, "10000000-0000-4000-8000-000000000008", TurnKey: "10000000-0000-4000-8000-0000000000b2"),
            },
            exit);

        // The only prompts of that file are the two the user typed.
        Assert.Equal(2, odd.Count(e => e.Kind == RecordKind.Prompt));
    }

    [Fact]
    public void A_notification_prompt_is_one_Claude_Code_started_itself()
    {
        var entries = Entries("queued.jsonl");

        // The entry Claude Code marks as its own, of source system, opens its turn: a prompt without a text.
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "40000000-0000-4000-8000-000000000007", TurnKey: "40000000-0000-4000-8000-0000000000b3",
                Origin: PromptOrigin.SelfStarted),
            entries.Single(e => e.Id == "40000000-0000-4000-8000-000000000007"));

        // The copy of the notification kept for the conversation is a note.
        Assert.Equal(new RecordEntry(RecordKind.Note, "40000000-0000-4000-8000-000000000008", TurnKey: "40000000-0000-4000-8000-0000000000b3"),
            entries.Single(e => e.Id == "40000000-0000-4000-8000-000000000008"));

        // A plain text of source system is the same kind of prompt, and keeps its text.
        Assert.Equal(
            new RecordEntry(RecordKind.Prompt, "u", Text: "Continue the task.", Origin: PromptOrigin.SelfStarted),
            Parse("""{"type":"user","uuid":"u","promptSource":"system","message":{"content":"Continue the task."}}"""));
    }

    [Fact]
    public void The_project_folder_name_replaces_every_other_character_by_a_hyphen()
    {
        Assert.Equal("C--Temp-axclaude-fixture", ClaudeCodeTranscript.ProjectFolderName(@"C:\Temp\axclaude-fixture"));
        Assert.Equal("C--Temp-axclaude-fixture", ClaudeCodeTranscript.ProjectFolderName(@"C:\Temp\axclaude-fixture\"));
        Assert.Equal("D--Work-my-project--2-", ClaudeCodeTranscript.ProjectFolderName(@"D:\Work\my project (2)"));
        Assert.Equal("C--Temp-caf--x", ClaudeCodeTranscript.ProjectFolderName(@"C:\Temp\café.x"));
        Assert.Equal("C--", ClaudeCodeTranscript.ProjectFolderName(@"C:\"));
    }

    [Fact]
    public void A_session_file_lies_directly_in_a_project_folder_of_a_projects_folder()
    {
        var projects = ClaudeCodeTranscript.ProjectsFolders(@"D:\Profiles\tester", NoEnvironment, null);
        const string Session = "11111111-2222-4333-8444-555555555555";

        Assert.True(ClaudeCodeTranscript.IsSessionFile($@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl", projects));
        Assert.True(ClaudeCodeTranscript.IsSessionFile($@"d:\PROFILES\Tester\.Claude\Projects\C--Temp-axclaude-fixture\{Session}.JSONL", projects));
        Assert.True(ClaudeCodeTranscript.IsSessionFile($"D:/Profiles/tester/.claude/projects/C--Temp-axclaude-fixture/{Session}.jsonl", projects));

        string?[] refused =
        [
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}\subagents\agent-a0000000000000001.jsonl",
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}\tool-results\toolu_01.jsonl",
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\..\C--Temp-axclaude-fixture\{Session}.jsonl",
            $@"D:\Profiles\tester\.claude\projects\..\..\.claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl",
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}.json",
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl.superseded-1763251200",
            $@"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}.orphaned-1763251200-x1.jsonl",
            @"D:\Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\.jsonl",
            $@"D:\Profiles\tester\.claude\projects\{Session}.jsonl",
            $@"E:\Other\.claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl",
            $@"D:\Profiles\tester\.claude\other\C--Temp-axclaude-fixture\{Session}.jsonl",
            $@".claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl",
            $@"D:Profiles\tester\.claude\projects\C--Temp-axclaude-fixture\{Session}.jsonl",
            "",
            null,
        ];
        Assert.All(refused, path => Assert.False(ClaudeCodeTranscript.IsSessionFile(path, projects), path));
    }

    [Fact]
    public void ProjectsFolders_follow_the_configuration_folder_and_add_a_test_runs_own()
    {
        const string Home = @"D:\Profiles\tester";
        Assert.Equal(new[] { @"D:\Profiles\tester\.claude\projects" }, ClaudeCodeTranscript.ProjectsFolders(Home, NoEnvironment, null));

        // The configuration folder Claude Code is given, as ClaudeCodeSettings finds the user settings file in it.
        var moved = new Dictionary<string, string> { [ClaudeCodeSettings.ConfigDirVariable] = @"E:\ClaudeConfig" };
        Assert.Equal(new[] { @"E:\ClaudeConfig\projects" }, ClaudeCodeTranscript.ProjectsFolders(Home, moved, null));
        Assert.Equal(
            Path.GetDirectoryName(ClaudeCodeSettings.UserSettingsFile(Home, moved)),
            Path.GetDirectoryName(ClaudeCodeTranscript.ProjectsFolders(Home, moved, null)[0]));

        // A folder that is not a full path is not Claude Code's.
        var relative = new Dictionary<string, string> { [ClaudeCodeSettings.ConfigDirVariable] = "config" };
        Assert.Equal(new[] { @"D:\Profiles\tester\.claude\projects" }, ClaudeCodeTranscript.ProjectsFolders(Home, relative, null));

        // A test run's own folder is one more, and a folder is never there twice.
        Assert.Equal(
            new[] { @"D:\Profiles\tester\.claude\projects", @"T:\Runs\r1\.claude\projects" },
            ClaudeCodeTranscript.ProjectsFolders(Home, NoEnvironment, @"T:\Runs\r1"));
        Assert.Equal(new[] { @"D:\Profiles\tester\.claude\projects" }, ClaudeCodeTranscript.ProjectsFolders(Home, NoEnvironment, @"d:\profiles\TESTER"));

        // So a stand-in's file under the run's folder is a session file, and the user's are still found.
        var both = ClaudeCodeTranscript.ProjectsFolders(Home, NoEnvironment, @"T:\Runs\r1");
        Assert.True(ClaudeCodeTranscript.IsSessionFile(@"T:\Runs\r1\.claude\projects\T--Runs-r1-work\s1.jsonl", both));
        Assert.True(ClaudeCodeTranscript.IsSessionFile(@"D:\Profiles\tester\.claude\projects\C--Temp-x\s1.jsonl", both));
    }

    /// <summary>One line of a file: where it starts, its text, and what <see cref="ClaudeCodeTranscript.Parse"/> made of it.</summary>
    private sealed record Parsed(long Offset, string Text, RecordEntry? Entry, RecordLine What);

    /// <summary>
    /// Every line of a file of tests/fixtures/records, split at LF as the reader splits them, with its offset in the
    /// file. A checkout may have made the line ends CRLF: the CR is taken off, as JSON would pass over it anyway.
    /// </summary>
    private static List<Parsed> ParseFile(string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Fixtures.Folder(), "records", name));
        var parsed = new List<Parsed>();
        var start = 0;
        while (start < bytes.Length)
        {
            var end = Array.IndexOf(bytes, (byte)'\n', start);
            if (end < 0)
            {
                end = bytes.Length;
            }

            var line = bytes.AsSpan(start, end - start);
            if (line.Length > 0 && line[^1] == (byte)'\r')
            {
                line = line[..^1];
            }

            if (line.Length > 0)
            {
                var entry = ClaudeCodeTranscript.Parse(line, start, out var what);
                parsed.Add(new Parsed(start, Encoding.UTF8.GetString(line), entry, what));
            }

            start = end + 1;
        }

        return parsed;
    }

    private static List<RecordEntry> Entries(string name) => ParseFile(name).Where(p => p.Entry is not null).Select(p => p.Entry!).ToList();

    private static RecordEntry? Parse(string line) => ClaudeCodeTranscript.Parse(Encoding.UTF8.GetBytes(line), 0, out _);
}
