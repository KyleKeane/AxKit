using System.Text;
using System.Text.Json;

namespace AxClaude.Core.Streaming;

/// <summary>What one thing in a line of the stream is, in axClaude's own words.</summary>
public enum StreamKind
{
    /// <summary>A line or a block of a kind not known here: passed over, never an error.</summary>
    Unknown,

    /// <summary>The process is up and takes messages (its first line, and again before each later turn).</summary>
    Started,

    /// <summary>Text Claude wrote: a whole message, or one part of it between two tool calls.</summary>
    Text,

    /// <summary>Claude's thinking; its text is not read.</summary>
    Thinking,

    /// <summary>Claude calls a tool.</summary>
    ToolCall,

    /// <summary>A tool's result came back; <see cref="StreamPart.Error"/> when it failed or was refused.</summary>
    ToolResult,

    /// <summary>The turn is over: the last line of a turn.</summary>
    Ended,

    /// <summary>The answer to a control request of the host's (an interrupt).</summary>
    Control,

    /// <summary>A piece of a message as it is written, only with partial messages asked for.</summary>
    Delta,

    /// <summary>Something Claude Code says about itself: a status, a task started, a limit. Known and not spoken.</summary>
    Note,
}

/// <summary>One thing a line holds. <see cref="Text"/> is the text of a <see cref="StreamKind.Text"/> or a <see cref="StreamKind.Delta"/>, the subtype of a <see cref="StreamKind.Note"/>, else empty; <see cref="Tool"/> the tool's name of a call.</summary>
public sealed record StreamPart(StreamKind Kind, string Text, string Tool, bool Error);

/// <summary>
/// One line of the stream read down to what axClaude needs. <see cref="FromWorker"/>: the line belongs to work Claude
/// handed to a subagent. For an <see cref="StreamKind.Ended"/> line: <see cref="Failed"/>, <see cref="Terminal"/>
/// (Claude Code's word for how the turn ended), <see cref="Denials"/> (tool calls refused in the turn) and
/// <see cref="Queued"/> (messages still waiting). For a <see cref="StreamKind.Started"/> line: Claude Code's
/// <see cref="Version"/> and the permission <see cref="Mode"/> in force.
/// </summary>
public sealed record StreamLine(IReadOnlyList<StreamPart> Parts, bool FromWorker, bool Failed, string Terminal, int Denials, int Queued, string Version, string Mode)
{
    /// <summary>A line with nothing known in it.</summary>
    public static readonly StreamLine Nothing = new([], false, false, string.Empty, 0, 0, string.Empty, string.Empty);

    /// <summary>Whether the line holds a part of this kind.</summary>
    public bool Has(StreamKind kind) => Parts.Any(part => part.Kind == kind);
}

/// <summary>A line's shape with no value in it: its name (the type, with what tells its kind), its keys in order, and for a message the keys of each kind of block.</summary>
public sealed record StreamShape(string Name, IReadOnlyList<string> Keys, IReadOnlyDictionary<string, IReadOnlyList<string>> Blocks);

/// <summary>
/// Claude Code's non-interactive stream, the one place that knows its words (root CLAUDE.md rule 12): the options
/// that start it, the lines a host writes on standard input, and each line of standard output read down to a neutral
/// <see cref="StreamLine"/>. With <c>-p --input-format stream-json --output-format stream-json --verbose</c> Claude
/// Code is one process that takes a JSON line for each message and writes a JSON line for each thing that happens:
/// no screen, no rows to tell apart, no frames. Measured against Claude Code 2.1.326 on 2026-11-17 with
/// <c>tools/ClaudeProbe stream</c>, which plays a set of cases and is the check to run when Claude Code changes;
/// what the documentation says and what was measured are in <c>docs/axClaude/stream.md</c>. The reader is tolerant:
/// a line that is no JSON object, a type or a block not known, a field missing all give <see cref="StreamLine.Nothing"/>
/// or an <see cref="StreamKind.Unknown"/> part, never an exception. Nothing here is used by the window yet.
/// </summary>
public static class ClaudeCodeStream
{
    /// <summary>The options of a process that takes messages on standard input and writes the stream; <c>--verbose</c> is required with it (the run ends at once without).</summary>
    public static readonly IReadOnlyList<string> Options = ["-p", "--input-format", "stream-json", "--output-format", "stream-json", "--verbose"];

    /// <summary>With it, a message is also written piece by piece (<see cref="StreamKind.Delta"/>) before its whole line.</summary>
    public const string PartialOption = "--include-partial-messages";

    /// <summary>The option that names the MCP tool Claude Code asks before a tool call that needs the user's word, and for Claude's own questions.</summary>
    public const string PermissionToolOption = "--permission-prompt-tool";

    /// <summary>The option that keeps the run's session out of Claude Code's store; for a check, never for the user's conversation.</summary>
    public const string NoSessionOption = "--no-session-persistence";

    /// <summary>The tool whose call is Claude's own question to the user: its input holds the questions, and the answer goes back with the same input and <c>answers</c>.</summary>
    public const string QuestionTool = "AskUserQuestion";

    /// <summary>The line that sends a message of the user's.</summary>
    public static string UserLine(string text) => Json(writer =>
    {
        writer.WriteString("type", "user");
        writer.WriteStartObject("message");
        writer.WriteString("role", "user");
        writer.WriteString("content", text);
        writer.WriteEndObject();
    });

    /// <summary>The line that ends the running turn and leaves the process alive; Claude Code answers it with a <see cref="StreamKind.Control"/> line that carries the same id.</summary>
    public static string InterruptLine(string id) => Json(writer =>
    {
        writer.WriteString("type", "control_request");
        writer.WriteString("request_id", id);
        writer.WriteStartObject("request");
        writer.WriteString("subtype", "interrupt");
        writer.WriteEndObject();
    });

    /// <summary>The permission tool's answer that lets the call run with this input (the input as it was asked, or changed).</summary>
    public static string AllowAnswer(JsonElement input) => Json(writer =>
    {
        writer.WriteString("behavior", "allow");
        writer.WritePropertyName("updatedInput");
        input.WriteTo(writer);
    });

    /// <summary>The permission tool's answer to a question of Claude's: the input as asked, with the answer chosen for each question's text.</summary>
    public static string QuestionAnswer(JsonElement input, IReadOnlyDictionary<string, string> answers) => Json(writer =>
    {
        writer.WriteString("behavior", "allow");
        writer.WriteStartObject("updatedInput");
        if (input.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in input.EnumerateObject().Where(property => property.Name != "answers"))
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteStartObject("answers");
        foreach (var (question, answer) in answers)
        {
            writer.WriteString(question, answer);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    });

    /// <summary>The permission tool's answer that refuses the call, with what Claude is told.</summary>
    public static string DenyAnswer(string message) => Json(writer =>
    {
        writer.WriteString("behavior", "deny");
        writer.WriteString("message", message);
    });

    /// <summary>One line of the stream, read down; <see cref="StreamLine.Nothing"/> for what is no JSON object.</summary>
    public static StreamLine Read(string line)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return StreamLine.Nothing;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return StreamLine.Nothing;
            }

            var type = Text(root, "type");
            var subtype = Text(root, "subtype");
            var worker = root.TryGetProperty("parent_tool_use_id", out var parent) && parent.ValueKind == JsonValueKind.String;
            switch (type)
            {
                case "system" when subtype == "init":
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Started, string.Empty, string.Empty, false)], Version = Text(root, "claude_code_version"), Mode = Text(root, "permissionMode") };
                case "system":
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Note, subtype, string.Empty, false)] };
                case "rate_limit_event":
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Note, type, string.Empty, false)] };
                case "assistant":
                case "user":
                    return StreamLine.Nothing with { Parts = Blocks(root, type == "assistant"), FromWorker = worker };
                case "result":
                    return StreamLine.Nothing with
                    {
                        Parts = [new(StreamKind.Ended, Text(root, "result"), string.Empty, Flag(root, "is_error"))],
                        Failed = Flag(root, "is_error"),
                        Terminal = Text(root, "terminal_reason"),
                        Denials = root.TryGetProperty("permission_denials", out var denials) && denials.ValueKind == JsonValueKind.Array ? denials.GetArrayLength() : 0,
                        Queued = root.TryGetProperty("queued_turn_count", out var queued) && queued.TryGetInt32(out var count) ? count : 0,
                    };
                case "control_response":
                    var failed = !(root.TryGetProperty("response", out var response) && Text(response, "subtype") == "success");
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Control, root.TryGetProperty("response", out var answered) ? Text(answered, "request_id") : string.Empty, string.Empty, failed)], Failed = failed };
                case "stream_event":
                    var piece = root.TryGetProperty("event", out var happened) && happened.TryGetProperty("delta", out var delta) ? Text(delta, "text") : string.Empty;
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Delta, piece, string.Empty, false)], FromWorker = worker };
                default:
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Unknown, type, string.Empty, false)] };
            }
        }
    }

    /// <summary>A line's shape with no value in it, for the record of what the stream looks like; null for what is no JSON object.</summary>
    public static StreamShape? ShapeOf(string line)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var type = Text(root, "type");
            var name = type switch
            {
                "system" or "result" => type + "/" + Text(root, "subtype"),
                "stream_event" => type + "/" + (root.TryGetProperty("event", out var happened) ? Text(happened, "type") : string.Empty),
                "control_response" => type + "/" + (root.TryGetProperty("response", out var response) ? Text(response, "subtype") : string.Empty),
                _ => type,
            };
            var blocks = new SortedDictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
                && message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray().Where(block => block.ValueKind == JsonValueKind.Object))
                {
                    blocks[Text(block, "type")] = Keys(block);
                }
            }

            return new StreamShape(name, Keys(root), blocks);
        }
    }

    private static List<StreamPart> Blocks(JsonElement root, bool assistant)
    {
        var parts = new List<StreamPart>();
        if (!root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("content", out var content))
        {
            return parts;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            // A user line whose content is one text: the host's own message come back, or a note of Claude Code's.
            parts.Add(new(StreamKind.Note, "user text", string.Empty, false));
            return parts;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return parts;
        }

        foreach (var block in content.EnumerateArray().Where(block => block.ValueKind == JsonValueKind.Object))
        {
            parts.Add(Text(block, "type") switch
            {
                "text" when assistant => new(StreamKind.Text, Text(block, "text"), string.Empty, false),
                "text" => new(StreamKind.Note, "user text", string.Empty, false),
                "thinking" => new(StreamKind.Thinking, string.Empty, string.Empty, false),
                "tool_use" => new(StreamKind.ToolCall, string.Empty, Text(block, "name"), false),
                "tool_result" => new(StreamKind.ToolResult, string.Empty, string.Empty, Flag(block, "is_error")),
                var other => new(StreamKind.Unknown, other, string.Empty, false),
            });
        }

        return parts;
    }

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static bool Flag(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static List<string> Keys(JsonElement element) =>
        [.. element.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
