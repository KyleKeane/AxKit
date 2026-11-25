using System.Text.Json;
using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's non-interactive stream read down to neutral lines (<see cref="ClaudeCodeStream"/>), on lines of the
/// shapes measured with Claude Code 2.1.326 on 2026-11-17 (<c>tools/ClaudeProbe stream</c>), written here by hand with
/// neutral values; and the lines a host writes.
/// </summary>
public class ClaudeCodeStreamTests
{
    [Fact]
    public void The_first_line_says_the_process_is_up_with_its_version_and_mode()
    {
        var line = ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.1.326","permissionMode":"default","tools":["Bash"],"session_id":"s"}""");
        Assert.Equal([new StreamPart(StreamKind.Started, "", "", false)], line.Parts);
        Assert.Equal("2.1.326", line.Version);
        Assert.Equal("default", line.Mode);
    }

    [Fact]
    public void A_message_gives_its_blocks_in_order()
    {
        var line = ClaudeCodeStream.Read("""{"type":"assistant","message":{"role":"assistant","content":[{"type":"thinking","thinking":"hm"},{"type":"text","text":"I will look."},{"type":"tool_use","id":"t1","name":"Bash","input":{"command":"dir"}}]},"parent_tool_use_id":null}""");
        // A call carries its id, which pairs it with its result, and what it is about: here the command.
        Assert.Equal(
            [new StreamPart(StreamKind.Thinking, "", "", false), new StreamPart(StreamKind.Text, "I will look.", "", false), new StreamPart(StreamKind.ToolCall, "", "Bash", false) { Call = "t1", About = "dir" }],
            line.Parts);
        Assert.False(line.FromWorker);
        Assert.True(line.Has(StreamKind.ToolCall));
        Assert.False(line.Has(StreamKind.Ended));
    }

    [Theory]
    [InlineData("""{"command":"git status","description":"Show the state"}""", "git status")]
    [InlineData("""{"file_path":"C:\\work\\a.txt","content":"hi"}""", "C:\\work\\a.txt")]
    [InlineData("""{"pattern":"TODO","path":"src"}""", "TODO")]
    [InlineData("""{"description":"Find the tests","prompt":"Look for them","subagent_type":"Explore"}""", "Find the tests")]
    [InlineData("""{"command":"first line\nsecond line"}""", "first line")]
    [InlineData("""{"questions":[]}""", "")]
    public void A_call_says_what_it_is_about(string input, string about)
    {
        var line = ClaudeCodeStream.Read("{\"type\":\"assistant\",\"message\":{\"content\":[{\"type\":\"tool_use\",\"id\":\"c\",\"name\":\"X\",\"input\":" + input + "}]}}");
        Assert.Equal(about, Assert.Single(line.Parts).About);
    }

    [Fact]
    public void A_message_come_back_a_commands_print_and_a_line_in_the_users_place_are_told_apart()
    {
        // With the option that sends each message back: the host's own message, with its text.
        var back = ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"Reply with exactly: ok"}}""");
        Assert.Equal([new StreamPart(StreamKind.Message, "Reply with exactly: ok", "", false)], back.Parts);

        // What a slash command printed comes back the same way, inside Claude Code's tag.
        var printed = ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<local-command-stdout>Compacted </local-command-stdout>"}}""");
        Assert.Equal([new StreamPart(StreamKind.CommandOutput, "Compacted", "", false)], printed.Parts);

        // The summary a compaction leaves is written in the user's place and is no message of theirs.
        var summary = ClaudeCodeStream.Read("""{"type":"user","isReplay":false,"isSynthetic":true,"message":{"content":"This session is being continued"}}""");
        Assert.Equal([new StreamPart(StreamKind.Note, ClaudeCodeStream.UserText, "", false)], summary.Parts);
    }

    [Fact]
    public void A_failed_result_gives_its_first_line_but_not_the_words_meant_for_the_model()
    {
        var failed = ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"c1","is_error":true,"content":"Exit code 1\nfatal: not a git repository"}]}}""");
        Assert.Equal([new StreamPart(StreamKind.ToolResult, "Exit code 1", "", true) { Call = "c1" }], failed.Parts);

        var blocks = ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"c2","is_error":true,"content":[{"type":"text","text":"No such file"}]}]}}""");
        Assert.Equal("No such file", Assert.Single(blocks.Parts).Text);

        // The user refused or interrupted: Claude Code's own words to the model, and the relay's, are not shown again.
        var interrupted = ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"c3","is_error":true,"content":"The user doesn't want to proceed with this tool use. The tool use was rejected. STOP what you are doing."}]}}""");
        Assert.Equal(string.Empty, Assert.Single(interrupted.Parts).Text);
        var refused = ClaudeCodeStream.Read("{\"type\":\"user\",\"message\":{\"content\":[{\"type\":\"tool_result\",\"tool_use_id\":\"c4\",\"is_error\":true,\"content\":\"" + ClaudeCodeStream.RefusedWords + "\"}]}}");
        Assert.Equal(string.Empty, Assert.Single(refused.Parts).Text);
        Assert.True(Assert.Single(refused.Parts).Error);

        // A result that did not fail gives nothing of what came back.
        var fine = ClaudeCodeStream.Read("""{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"c5","content":"a whole file"}]}}""");
        Assert.Equal([new StreamPart(StreamKind.ToolResult, "", "", false) { Call = "c5" }], fine.Parts);
    }

    [Fact]
    public void The_work_in_the_background_is_listed_whole_and_only_by_its_own_note()
    {
        var two = ClaudeCodeStream.Read("""{"type":"system","subtype":"background_tasks_changed","tasks":[{"id":"t1","task_type":"local_bash","description":"Run the build"},{"id":"t2","task_type":"local_agent","description":"Find the tests","subagent_type":"Explore"},{"id":"t3"}]}""");
        Assert.Equal(
            [new StreamTask("t1", StreamTask.Shell, "Run the build", ""), new StreamTask("t2", StreamTask.AgentKind, "Find the tests", "Explore")],
            two.Tasks);

        var none = ClaudeCodeStream.Read("""{"type":"system","subtype":"background_tasks_changed","tasks":[]}""");
        Assert.NotNull(none.Tasks);
        Assert.Empty(none.Tasks);

        // Any other line says nothing of it, which is not the same as saying that nothing runs.
        Assert.Null(ClaudeCodeStream.Read("""{"type":"system","subtype":"task_started","task_id":"t1"}""").Tasks);
    }

    [Fact]
    public void An_end_line_counts_its_turns_and_the_services_error_is_no_reply()
    {
        Assert.Equal(2, ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":2}""").Turns);
        // A slash command Claude Code ran by itself: an end line that counts no turn.
        Assert.Equal(0, ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"num_turns":0,"result":""}""").Turns);

        var error = ClaudeCodeStream.Read("""{"type":"assistant","is_api_error_message":true,"error":"model_not_found","message":{"content":[{"type":"text","text":"There's an issue with the selected model."}]}}""");
        Assert.True(Assert.Single(error.Parts).Error);
    }

    [Fact]
    public void A_window_starts_the_stream_with_its_options_the_relay_and_then_the_users_own_arguments()
    {
        // docs/axClaude/stream-mode.md SM-4.2: the stream's options, each message sent back, the relay's file and
        // its tool; the user's own arguments last, as they were given.
        var arguments = ClaudeCodeStream.StartArguments(@"C:\data\claude-ask.json", "mcp__axask__approve", null, ["--continue"]);
        Assert.Equal(
            ["-p", "--input-format", "stream-json", "--output-format", "stream-json", "--verbose", "--replay-user-messages",
                "--mcp-config", @"C:\data\claude-ask.json", "--permission-prompt-tool", "mcp__axask__approve", "--continue"],
            arguments);

        // The file's option takes every word up to the next option: an option follows it, whatever the user gave.
        Assert.StartsWith("--", ClaudeCodeStream.StartArguments("f.json", "tool", null, [])[^2]);
        Assert.Equal("--mcp-config", ClaudeCodeStream.StartArguments("f.json", "tool", null, ["words"])[7]);
        Assert.Equal("--permission-prompt-tool", ClaudeCodeStream.StartArguments("f.json", "tool", null, ["words"])[9]);

        // The window's own model, in front of the user's arguments, and only when they name none.
        Assert.Equal(["--model", "haiku", "--resume", "id"], ClaudeCodeStream.StartArguments("f.json", "tool", "haiku", ["--resume", "id"]).Skip(11));
        Assert.Equal(["--model", "opus"], ClaudeCodeStream.StartArguments("f.json", "tool", "haiku", ["--model", "opus"]).Skip(11));
        Assert.Equal(["--continue"], ClaudeCodeStream.StartArguments("f.json", "tool", string.Empty, ["--continue"]).Skip(11));

        // --ai-tools (SPEC.md FR-9.7): the file of axKit's AI tools beside the relay's, under the same option, which takes several.
        var withTools = ClaudeCodeStream.StartArguments("f.json", "tool", null, ["--continue"], @"C:\data\claude-mcp.json");
        Assert.Equal(["--mcp-config", "f.json", @"C:\data\claude-mcp.json", "--permission-prompt-tool", "tool", "--continue"], withTools.Skip(7));
        Assert.Equal(ClaudeCodeStream.StartArguments("f.json", "tool", null, []), ClaudeCodeStream.StartArguments("f.json", "tool", null, [], string.Empty));
    }

    [Fact]
    public void A_command_that_runs_the_model_comes_back_as_its_tags_read_down_to_the_command()
    {
        // The fixture custom-command (2.1.332): the tags are read, never kept as a message's text.
        var back = ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<command-message>probe-greet</command-message>\n<command-name>/probe-greet</command-name>"}}""");
        Assert.Equal([new StreamPart(StreamKind.Command, "/probe-greet", "", false)], back.Parts);

        var withArguments = ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"<command-message>review</command-message>\n<command-name>/review</command-name>\n<command-args> main </command-args>"}}""");
        Assert.Equal("/review main", Assert.Single(withArguments.Parts).Text);

        // A message of the user's that only mentions a tag is a message.
        var mention = ClaudeCodeStream.Read("""{"type":"user","isReplay":true,"message":{"content":"What does <command-name> mean?"}}""");
        Assert.Equal(StreamKind.Message, Assert.Single(mention.Parts).Kind);
    }

    [Fact]
    public void The_first_requests_answer_gives_the_models_and_a_start_line_the_model_in_use()
    {
        var answer = ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"success","request_id":"a-1","response":{"current_permission_mode":"default","models":[{"value":"haiku","displayName":"Haiku 5.5","description":"Fastest","supportedEffortLevels":["low","high"]},{"value":"old","displayName":"Old"},{"value":"two words"},{"displayName":"no value"},{"value":"x","supportedEffortLevels":["low","bad level"]}]}}}""");
        Assert.Equal(["haiku", "old", "x"], answer.Models.Select(model => model.Value));
        Assert.Equal(["low", "high"], answer.Models[0].Efforts);
        Assert.Empty(answer.Models[1].Efforts);
        // A level of more than one word is passed over: it is sent after its command as one word.
        Assert.Equal(["low"], answer.Models[2].Efforts);

        var start = ClaudeCodeStream.Read("""{"type":"system","subtype":"init","model":"claude-haiku-5-5","permissionMode":"default","session_id":"s"}""");
        Assert.Equal("claude-haiku-5-5", start.Model);
        Assert.Empty(ClaudeCodeStream.Read("""{"type":"result","subtype":"success"}""").Models);
    }

    [Theory]
    [InlineData("default", "acceptEdits")]
    [InlineData("acceptEdits", "plan")]
    [InlineData("plan", "default")]
    [InlineData("", "default")]
    [InlineData("a mode not known", "default")]
    public void The_mode_key_goes_round_the_three_modes_every_run_has(string current, string next)
    {
        Assert.Equal(next, ClaudeCodeStream.NextMode(current, []));
        // Seeing only those three changes nothing.
        Assert.Equal(next, ClaudeCodeStream.NextMode(current, ["default", "plan", "acceptEdits", ""]));
    }

    [Fact]
    public void The_mode_key_comes_back_to_a_mode_the_run_was_itself_in_and_to_no_other()
    {
        // stream-mode.md SM-5.4: the user's own settings put the run in auto mode, so the round holds it, after the
        // three every run has; the mode that asks nothing is in no round the run was never in.
        string[] seen = ["auto", "default"];
        Assert.Equal("acceptEdits", ClaudeCodeStream.NextMode("default", seen));
        Assert.Equal("auto", ClaudeCodeStream.NextMode("plan", seen));
        Assert.Equal("default", ClaudeCodeStream.NextMode("auto", seen));

        foreach (var current in new[] { "default", "acceptEdits", "plan", "auto" })
        {
            Assert.NotEqual("bypassPermissions", ClaudeCodeStream.NextMode(current, seen));
        }

        // A run the user started in the mode that asks nothing may come back to it.
        Assert.Equal("bypassPermissions", ClaudeCodeStream.NextMode("plan", ["bypassPermissions"]));
        Assert.Equal("default", ClaudeCodeStream.NextMode("bypassPermissions", ["bypassPermissions"]));
    }

    [Fact]
    public void An_end_line_of_a_turn_the_user_interrupted_is_told_from_a_failure()
    {
        var interrupted = ClaudeCodeStream.Read("""{"type":"result","subtype":"error_during_execution","is_error":true,"terminal_reason":"aborted_tools","num_turns":3}""");
        Assert.True(ClaudeCodeStream.Interrupted(interrupted));

        var failed = ClaudeCodeStream.Read("""{"type":"result","subtype":"error_during_execution","is_error":true,"terminal_reason":"api_error","num_turns":1}""");
        Assert.False(ClaudeCodeStream.Interrupted(failed));

        var well = ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"terminal_reason":"completed","num_turns":1}""");
        Assert.False(ClaudeCodeStream.Interrupted(well));
    }

    [Fact]
    public void A_windows_start_asks_no_less_than_the_users_own_Claude_Code()
    {
        // SM-7.2: axClaude names no permission mode and nothing that lets a tool run unasked; and the session is the
        // user's conversation, kept in Claude Code's store.
        var arguments = ClaudeCodeStream.StartArguments("f.json", "tool", "haiku", []);
        Assert.DoesNotContain("--permission-mode", arguments);
        Assert.DoesNotContain("--dangerously-skip-permissions", arguments);
        Assert.DoesNotContain("--allowedTools", arguments);
        Assert.DoesNotContain("--allowed-tools", arguments);
        Assert.DoesNotContain(ClaudeCodeStream.NoSessionOption, arguments);
        // Nor the option that keeps the user's own MCP servers out: this is the user's Claude Code.
        Assert.DoesNotContain(AxKit.Core.ClaudeCodeProgram.StrictMcpConfigOption, arguments);
    }

    [Fact]
    public void The_answer_to_the_first_request_names_the_mode_and_the_version_and_nothing_else_is_read()
    {
        Assert.Equal("""{"type":"control_request","request_id":"h0","request":{"subtype":"initialize"}}""", ClaudeCodeStream.InitializeLine("h0"));

        var answer = ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"success","request_id":"h0","response":{"current_permission_mode":"auto","claude_code_version":"2.1.327","account":{"email":"someone"},"commands":[{"name":"mine"}]}}}""");
        Assert.Equal([new StreamPart(StreamKind.Control, "h0", "", false)], answer.Parts);
        Assert.Equal("auto", answer.Mode);
        Assert.Equal("2.1.327", answer.Version);
        // The account and the user's commands are in the line and in nothing the reader gives.
        Assert.Empty(answer.Commands);
        Assert.Equal(string.Empty, answer.Session);

        // The answer to an interrupt names none of it.
        var plain = ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"success","request_id":"h1"}}""");
        Assert.Equal(string.Empty, plain.Mode);
        Assert.False(plain.Failed);
    }

    [Theory]
    [InlineData("default", "manual mode on")]
    [InlineData("acceptEdits", "accept edits on")]
    [InlineData("plan", "plan mode on")]
    [InlineData("auto", "auto mode on")]
    [InlineData("bypassPermissions", "bypass permissions on")]
    [InlineData("somethingNew", "somethingNew mode on")]
    [InlineData("", null)]
    public void The_permission_mode_is_said_in_the_screens_words(string mode, string? words)
    {
        Assert.Equal(words, ClaudeCodeStream.ModeWords(mode));
    }

    [Fact]
    public void A_tool_result_says_whether_it_failed_and_a_worker_line_says_so()
    {
        var refused = ClaudeCodeStream.Read("""{"type":"user","message":{"role":"user","content":[{"type":"tool_result","tool_use_id":"t1","is_error":true,"content":"no"}]},"parent_tool_use_id":null}""");
        Assert.Equal([new StreamPart(StreamKind.ToolResult, "no", "", true) { Call = "t1" }], refused.Parts);

        var worker = ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"text","text":"from the agent"}]},"parent_tool_use_id":"t9"}""");
        Assert.True(worker.FromWorker);

        // The text of a user line is not Claude's: the host's own message, or Claude Code's note of an interrupt.
        var note = ClaudeCodeStream.Read("""{"type":"user","message":{"role":"user","content":[{"type":"text","text":"[Request interrupted]"}]}}""");
        Assert.Equal([new StreamPart(StreamKind.Note, "user text", "", false)], note.Parts);
        Assert.Equal(note.Parts, ClaudeCodeStream.Read("""{"type":"user","message":{"role":"user","content":"hello"}}""").Parts);
    }

    [Fact]
    public void The_last_line_of_a_turn_says_how_it_ended()
    {
        var ok = ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"result":"done","terminal_reason":"completed","permission_denials":[],"queued_turn_count":0,"num_turns":2}""");
        Assert.Equal([new StreamPart(StreamKind.Ended, "done", "", false)], ok.Parts);
        Assert.False(ok.Failed);
        Assert.Equal("completed", ok.Terminal);
        Assert.Equal(0, ok.Denials);

        var stopped = ClaudeCodeStream.Read("""{"type":"result","subtype":"error_during_execution","is_error":true,"terminal_reason":"aborted_tools","permission_denials":[{"tool_name":"Write"}],"queued_turn_count":1}""");
        Assert.True(stopped.Failed);
        Assert.Equal("aborted_tools", stopped.Terminal);
        Assert.Equal(1, stopped.Denials);
        Assert.Equal(1, stopped.Queued);
    }

    [Fact]
    public void A_control_answer_a_piece_and_a_note_are_told_apart()
    {
        var control = ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"success","request_id":"r1","response":{}}}""");
        Assert.Equal([new StreamPart(StreamKind.Control, "r1", "", false)], control.Parts);
        Assert.True(ClaudeCodeStream.Read("""{"type":"control_response","response":{"subtype":"error","request_id":"r2"}}""").Failed);

        var piece = ClaudeCodeStream.Read("""{"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":"He"}},"parent_tool_use_id":null}""");
        Assert.Equal([new StreamPart(StreamKind.Delta, "He", "", false)], piece.Parts);

        Assert.Equal([new StreamPart(StreamKind.Note, "task_started", "", false)], ClaudeCodeStream.Read("""{"type":"system","subtype":"task_started"}""").Parts);
        Assert.Equal([new StreamPart(StreamKind.Note, "rate_limit_event", "", false)], ClaudeCodeStream.Read("""{"type":"rate_limit_event","rate_limit_info":{}}""").Parts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("{\"type\":\"assistant\"}")]
    [InlineData("{\"type\":\"assistant\",\"message\":{\"content\":7}}")]
    [InlineData("{\"type\":\"result\"}")]
    public void What_is_not_understood_never_throws(string line)
    {
        var read = ClaudeCodeStream.Read(line);
        Assert.DoesNotContain(read.Parts, part => part.Kind is StreamKind.Text or StreamKind.ToolCall);
        _ = ClaudeCodeStream.ShapeOf(line);
    }

    [Fact]
    public void A_kind_of_line_or_block_not_known_is_passed_over_by_its_name()
    {
        Assert.Equal([new StreamPart(StreamKind.Unknown, "something_new", "", false)], ClaudeCodeStream.Read("""{"type":"something_new","x":1}""").Parts);
        var block = ClaudeCodeStream.Read("""{"type":"assistant","message":{"content":[{"type":"new_block"},{"type":"text","text":"still read"}]}}""");
        Assert.Equal([new StreamPart(StreamKind.Unknown, "new_block", "", false), new StreamPart(StreamKind.Text, "still read", "", false)], block.Parts);
    }

    [Fact]
    public void A_shape_holds_names_and_keys_and_no_value()
    {
        var shape = ClaudeCodeStream.ShapeOf("""{"type":"assistant","uuid":"secret-id","message":{"role":"assistant","content":[{"type":"tool_use","name":"Bash","id":"t1","input":{"command":"private"}},{"type":"text","text":"private words"}]}}""");
        Assert.NotNull(shape);
        Assert.Equal("assistant", shape.Name);
        Assert.Equal(["message", "type", "uuid"], shape.Keys);
        Assert.Equal(["id", "input", "name", "type"], shape.Blocks["tool_use"]);
        Assert.Equal(["text", "type"], shape.Blocks["text"]);
        Assert.DoesNotContain("private", JsonSerializer.Serialize(shape));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(shape));

        Assert.Equal("system/init", ClaudeCodeStream.ShapeOf("""{"type":"system","subtype":"init"}""")!.Name);
        Assert.Equal("result/success", ClaudeCodeStream.ShapeOf("""{"type":"result","subtype":"success"}""")!.Name);
        Assert.Equal("stream_event/message_start", ClaudeCodeStream.ShapeOf("""{"type":"stream_event","event":{"type":"message_start"}}""")!.Name);
        Assert.Equal("control_response/success", ClaudeCodeStream.ShapeOf("""{"type":"control_response","response":{"subtype":"success"}}""")!.Name);
    }

    [Fact]
    public void A_users_message_is_one_line_whatever_it_holds()
    {
        var line = ClaudeCodeStream.UserLine("Say \"hi\"\nthen stop \\ here");
        Assert.DoesNotContain('\n', line);
        using var document = JsonDocument.Parse(line);
        Assert.Equal("user", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("user", document.RootElement.GetProperty("message").GetProperty("role").GetString());
        Assert.Equal("Say \"hi\"\nthen stop \\ here", document.RootElement.GetProperty("message").GetProperty("content").GetString());
    }

    [Fact]
    public void An_interrupt_carries_its_id()
    {
        using var document = JsonDocument.Parse(ClaudeCodeStream.InterruptLine("r7"));
        Assert.Equal("control_request", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("r7", document.RootElement.GetProperty("request_id").GetString());
        Assert.Equal("interrupt", document.RootElement.GetProperty("request").GetProperty("subtype").GetString());
    }

    [Fact]
    public void The_permission_tools_answers()
    {
        using var asked = JsonDocument.Parse("""{"file_path":"a.txt","content":"hi"}""");
        using var allow = JsonDocument.Parse(ClaudeCodeStream.AllowAnswer(asked.RootElement));
        Assert.Equal("allow", allow.RootElement.GetProperty("behavior").GetString());
        Assert.Equal("a.txt", allow.RootElement.GetProperty("updatedInput").GetProperty("file_path").GetString());

        using var deny = JsonDocument.Parse(ClaudeCodeStream.DenyAnswer("The user said no."));
        Assert.Equal("deny", deny.RootElement.GetProperty("behavior").GetString());
        Assert.Equal("The user said no.", deny.RootElement.GetProperty("message").GetString());

        // A question of Claude's: the questions as asked, and the answer for each question's text; an answers field
        // that came with the input is replaced, not doubled.
        using var question = JsonDocument.Parse("""{"questions":[{"question":"Which colour?","header":"Colour","options":[{"label":"Red"},{"label":"Blue"}],"multiSelect":false}],"answers":{"old":"x"}}""");
        using var answered = JsonDocument.Parse(ClaudeCodeStream.QuestionAnswer(question.RootElement, new Dictionary<string, string> { ["Which colour?"] = "Blue" }));
        var updated = answered.RootElement.GetProperty("updatedInput");
        Assert.Equal(1, updated.GetProperty("questions").GetArrayLength());
        Assert.Equal("Blue", updated.GetProperty("answers").GetProperty("Which colour?").GetString());
        Assert.False(updated.GetProperty("answers").TryGetProperty("old", out _));
    }

    /// <summary>
    /// The start line also names the session and the slash commands the run takes (measured 2026-11-17: 68 of them,
    /// the user's own among them); a line of another kind names neither.
    /// </summary>
    [Fact]
    public void The_start_line_names_the_session_and_the_commands_the_run_takes()
    {
        var line = ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.1.326","permissionMode":"auto","session_id":"0000-1111","slash_commands":["compact","clear","model",7,""]}""");
        Assert.Equal("0000-1111", line.Session);
        Assert.Equal(["compact", "clear", "model"], line.Commands);

        var other = ClaudeCodeStream.Read("""{"type":"result","subtype":"success","is_error":false,"session_id":"0000-1111"}""");
        Assert.Equal(string.Empty, other.Session);
        Assert.Empty(other.Commands);

        // A start line without the two keys, as an older Claude Code may write it, is still the start.
        var bare = ClaudeCodeStream.Read("""{"type":"system","subtype":"init","claude_code_version":"2.0.0"}""");
        Assert.True(bare.Has(StreamKind.Started));
        Assert.Equal(string.Empty, bare.Session);
        Assert.Empty(bare.Commands);
    }

    /// <summary>
    /// Measured 2026-11-17 with the slash commands /compact and /clear written as messages: a status note says
    /// compacting and a later one says nothing, a note marks the boundary, and a line of a kind of its own says that
    /// the conversation was emptied. A status note can name the permission mode now in force.
    /// </summary>
    [Fact]
    public void A_status_note_says_what_claude_code_does_and_a_reset_is_a_note_of_its_own()
    {
        var compacting = ClaudeCodeStream.Read("""{"type":"system","subtype":"status","status":"compacting","session_id":"s"}""");
        Assert.Equal([new StreamPart(StreamKind.Note, "status", "", false)], compacting.Parts);
        Assert.Equal("compacting", compacting.Status);
        Assert.Equal(string.Empty, compacting.Mode);

        var over = ClaudeCodeStream.Read("""{"type":"system","subtype":"status","status":null,"permissionMode":"acceptEdits","session_id":"s"}""");
        Assert.Equal(string.Empty, over.Status);
        Assert.Equal("acceptEdits", over.Mode);

        var boundary = ClaudeCodeStream.Read("""{"type":"system","subtype":"compact_boundary","compact_metadata":{"trigger":"manual","pre_tokens":100}}""");
        Assert.Equal([new StreamPart(StreamKind.Note, "compact_boundary", "", false)], boundary.Parts);
        Assert.Equal(string.Empty, boundary.Status);

        var reset = ClaudeCodeStream.Read("""{"type":"conversation_reset","session_id":"s2"}""");
        Assert.Equal([new StreamPart(StreamKind.Note, "conversation_reset", "", false)], reset.Parts);
    }

    /// <summary>
    /// The user's "always": the call runs, and a rule of the session's alone lets the same tool run from then on
    /// (measured 2026-11-17: two writes, asked about once). Never a rule written to a settings file.
    /// </summary>
    [Fact]
    public void An_always_answer_allows_the_call_and_adds_a_rule_of_the_session_alone()
    {
        using var asked = JsonDocument.Parse("""{"file_path":"a.txt","content":"hi"}""");
        using var always = JsonDocument.Parse(ClaudeCodeStream.AlwaysAnswer(asked.RootElement, "Write"));
        Assert.Equal("allow", always.RootElement.GetProperty("behavior").GetString());
        Assert.Equal("a.txt", always.RootElement.GetProperty("updatedInput").GetProperty("file_path").GetString());
        var updates = always.RootElement.GetProperty("updatedPermissions");
        Assert.Equal(1, updates.GetArrayLength());
        Assert.Equal("addRules", updates[0].GetProperty("type").GetString());
        Assert.Equal("allow", updates[0].GetProperty("behavior").GetString());
        Assert.Equal("session", updates[0].GetProperty("destination").GetString());
        Assert.Equal(1, updates[0].GetProperty("rules").GetArrayLength());
        Assert.Equal("Write", updates[0].GetProperty("rules")[0].GetProperty("toolName").GetString());
        Assert.False(updates[0].GetProperty("rules")[0].TryGetProperty("ruleContent", out _));
    }

    [Fact]
    public void A_mode_request_carries_its_id_and_the_mode()
    {
        using var document = JsonDocument.Parse(ClaudeCodeStream.ModeLine("r9", "acceptEdits"));
        Assert.Equal("control_request", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("r9", document.RootElement.GetProperty("request_id").GetString());
        Assert.Equal("set_permission_mode", document.RootElement.GetProperty("request").GetProperty("subtype").GetString());
        Assert.Equal("acceptEdits", document.RootElement.GetProperty("request").GetProperty("mode").GetString());
    }
}
