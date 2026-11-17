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
        Assert.Equal(
            [new StreamPart(StreamKind.Thinking, "", "", false), new StreamPart(StreamKind.Text, "I will look.", "", false), new StreamPart(StreamKind.ToolCall, "", "Bash", false)],
            line.Parts);
        Assert.False(line.FromWorker);
        Assert.True(line.Has(StreamKind.ToolCall));
        Assert.False(line.Has(StreamKind.Ended));
    }

    [Fact]
    public void A_tool_result_says_whether_it_failed_and_a_worker_line_says_so()
    {
        var refused = ClaudeCodeStream.Read("""{"type":"user","message":{"role":"user","content":[{"type":"tool_result","tool_use_id":"t1","is_error":true,"content":"no"}]},"parent_tool_use_id":null}""");
        Assert.Equal([new StreamPart(StreamKind.ToolResult, "", "", true)], refused.Parts);

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
}
