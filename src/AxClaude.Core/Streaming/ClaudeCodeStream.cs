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

    /// <summary>
    /// A message of the user's that Claude took up: the host's own line come back, with its text (only with
    /// <see cref="ClaudeCodeStream.ReplayOption"/>). It comes just before the reply it starts, never earlier, and for
    /// a message written while a tool ran, inside the running turn (the fixtures of tests/fixtures/streams).
    /// </summary>
    Message,

    /// <summary>What a slash command printed, come back in the user's place inside Claude Code's own tag (<c>/compact</c>: "Compacted"); the text is what stands in the tag.</summary>
    CommandOutput,
}

/// <summary>
/// One thing a line holds. <see cref="Text"/> is the text of a <see cref="StreamKind.Text"/>, a
/// <see cref="StreamKind.Delta"/>, a <see cref="StreamKind.Message"/> or a <see cref="StreamKind.CommandOutput"/>,
/// the subtype of a <see cref="StreamKind.Note"/>, the first line of what a failed
/// <see cref="StreamKind.ToolResult"/> gave back, else empty; <see cref="Tool"/> the tool's name of a call.
/// <see cref="Error"/>: a result that failed or was refused, a text that is the service's error and no reply.
/// </summary>
public sealed record StreamPart(StreamKind Kind, string Text, string Tool, bool Error)
{
    /// <summary>The call's id, on a <see cref="StreamKind.ToolCall"/> and on the <see cref="StreamKind.ToolResult"/> that answers it: what pairs the two. An id: never shown.</summary>
    public string Call { get; init; } = string.Empty;

    /// <summary>What a <see cref="StreamKind.ToolCall"/> is about, in the call's own words: the command, the file, the pattern, the address; empty when the input names none.</summary>
    public string About { get; init; } = string.Empty;
}

/// <summary>
/// One thing that runs in the background, as Claude Code lists it: its <see cref="Kind"/> in Claude Code's word
/// (<see cref="Shell"/>, <see cref="AgentKind"/>), its description, and for an agent its type.
/// </summary>
public sealed record StreamTask(string Id, string Kind, string Description, string Agent)
{
    /// <summary>Claude Code's word for a command run in the background.</summary>
    public const string Shell = "local_bash";

    /// <summary>Claude Code's word for a subagent.</summary>
    public const string AgentKind = "local_agent";
}

/// <summary>
/// One line of the stream read down to what axClaude needs. <see cref="FromWorker"/>: the line belongs to work Claude
/// handed to a subagent. For an <see cref="StreamKind.Ended"/> line: <see cref="Failed"/>, <see cref="Terminal"/>
/// (Claude Code's word for how the turn ended), <see cref="Denials"/> (tool calls refused in the turn) and
/// <see cref="Queued"/> (messages still waiting). For a <see cref="StreamKind.Started"/> line: Claude Code's
/// <see cref="Version"/>, the permission <see cref="Mode"/> in force, the <see cref="Session"/> and the
/// <see cref="Commands"/> the run takes.
/// </summary>
public sealed record StreamLine(IReadOnlyList<StreamPart> Parts, bool FromWorker, bool Failed, string Terminal, int Denials, int Queued, string Version, string Mode)
{
    /// <summary>A line with nothing known in it.</summary>
    public static readonly StreamLine Nothing = new([], false, false, string.Empty, 0, 0, string.Empty, string.Empty);

    /// <summary>The session's id, on a <see cref="StreamKind.Started"/> line: what a later run is given to take the conversation up again. An id: never shown, said or logged.</summary>
    public string Session { get; init; } = string.Empty;

    /// <summary>The slash commands this run takes as a message, by name without the slash, on a <see cref="StreamKind.Started"/> line; the user's own commands and skills are among them, so the names are never logged as a list.</summary>
    public IReadOnlyList<string> Commands { get; init; } = [];

    /// <summary>
    /// On a note of the kind status: Claude Code's word for what it is doing beside a turn (<c>compacting</c> while
    /// it shortens the conversation), empty when that is over. Such a note can also name the permission mode now in
    /// force (<see cref="Mode"/>), after the host or Claude Code changed it.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// On the note that lists the work in the background (<c>background_tasks_changed</c>): everything that runs
    /// there now, whole each time; empty when nothing runs. Null on every other line, which says nothing of it.
    /// </summary>
    public IReadOnlyList<StreamTask>? Tasks { get; init; }

    /// <summary>
    /// On an <see cref="StreamKind.Ended"/> line: how many turns of the model it counts. None for a slash command
    /// that Claude Code ran by itself (<c>/model</c>, <c>/clear</c>, <c>/compact</c>): such an end is no turn's end.
    /// </summary>
    public int Turns { get; init; }

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

    /// <summary>With it, each message the host writes comes back on the output as a line of the user's, as an acknowledgement that it was taken.</summary>
    public const string ReplayOption = "--replay-user-messages";

    /// <summary>The option that names the MCP tool Claude Code asks before a tool call that needs the user's word, and for Claude's own questions.</summary>
    public const string PermissionToolOption = "--permission-prompt-tool";

    /// <summary>The option that keeps the run's session out of Claude Code's store; for a check, never for the user's conversation.</summary>
    public const string NoSessionOption = "--no-session-persistence";

    /// <summary>The option that takes up a saved session, followed by the session's id (<see cref="StreamLine.Session"/>).</summary>
    public const string ResumeOption = "--resume";

    /// <summary>The option that takes up the folder's last session.</summary>
    public const string ContinueOption = "--continue";

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

    /// <summary>
    /// The permission tool's answer that lets the call run and, for the rest of this session, every later call of
    /// the same tool without asking: the user's "always" (documented for the Agent SDK's callback as an answer with
    /// <c>updatedPermissions</c>; measured for the raw tool by the probe's case permission-always). The rule is the
    /// session's alone: it is written to no settings file, and a new session asks again.
    /// </summary>
    public static string AlwaysAnswer(JsonElement input, string tool) => Json(writer =>
    {
        writer.WriteString("behavior", "allow");
        writer.WritePropertyName("updatedInput");
        input.WriteTo(writer);
        writer.WriteStartArray("updatedPermissions");
        writer.WriteStartObject();
        writer.WriteString("type", "addRules");
        writer.WriteStartArray("rules");
        writer.WriteStartObject();
        writer.WriteString("toolName", tool);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteString("behavior", "allow");
        writer.WriteString("destination", "session");
        writer.WriteEndObject();
        writer.WriteEndArray();
    });

    /// <summary>
    /// The line that sets the permission mode of the running session (the interactive program's Shift+Tab); Claude Code
    /// answers it with a <see cref="StreamKind.Control"/> line that carries the same id. The mode is one of Claude
    /// Code's own names (<see cref="StreamLine.Mode"/> gives the one in force at the start). Documented as a method of
    /// the Agent SDK only; the line is measured by the probe's case mode.
    /// </summary>
    public static string ModeLine(string id, string mode) => Json(writer =>
    {
        writer.WriteString("type", "control_request");
        writer.WriteString("request_id", id);
        writer.WriteStartObject("request");
        writer.WriteString("subtype", "set_permission_mode");
        writer.WriteString("mode", mode);
        writer.WriteEndObject();
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
                    return StreamLine.Nothing with
                    {
                        Parts = [new(StreamKind.Started, string.Empty, string.Empty, false)],
                        Version = Text(root, "claude_code_version"),
                        Mode = Text(root, "permissionMode"),
                        Session = Text(root, "session_id"),
                        Commands = Texts(root, "slash_commands"),
                    };
                case "system" when subtype == "status":
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Note, subtype, string.Empty, false)], Status = Text(root, "status"), Mode = Text(root, "permissionMode") };
                case "system" when subtype == TasksChanged:
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Note, subtype, string.Empty, false)], Tasks = Tasks(root) };
                case "system":
                    return StreamLine.Nothing with { Parts = [new(StreamKind.Note, subtype, string.Empty, false)] };
                case "rate_limit_event":
                // The conversation was emptied and a new session begins (the slash command /clear, measured 2026-11-17):
                // the start line that follows names the new session.
                case "conversation_reset":
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
                        Turns = root.TryGetProperty("num_turns", out var turns) && turns.TryGetInt32(out var turnCount) ? turnCount : 0,
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
            // A user line whose content is one text. Marked as a replay: the host's own message come back, or what
            // a slash command printed, in Claude Code's tag. Any other: a note of Claude Code's in the user's place
            // (the summary a compaction leaves), which is no message.
            var said = content.GetString() ?? string.Empty;
            if (!Flag(root, "isReplay") || Flag(root, "isSynthetic"))
            {
                parts.Add(new(StreamKind.Note, UserText, string.Empty, false));
            }
            else if (said.StartsWith(CommandOutputOpen, StringComparison.Ordinal))
            {
                var end = said.IndexOf(CommandOutputClose, StringComparison.Ordinal);
                parts.Add(new(StreamKind.CommandOutput, said[CommandOutputOpen.Length..(end < 0 ? said.Length : end)].Trim(), string.Empty, false));
            }
            else
            {
                parts.Add(new(StreamKind.Message, said, string.Empty, false));
            }

            return parts;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return parts;
        }

        // A text of the assistant's that is the service's error and no reply (a model that does not exist).
        var failure = assistant && Flag(root, "is_api_error_message");
        foreach (var block in content.EnumerateArray().Where(block => block.ValueKind == JsonValueKind.Object))
        {
            parts.Add(Text(block, "type") switch
            {
                "text" when assistant => new(StreamKind.Text, Text(block, "text"), string.Empty, failure),
                "text" => new(StreamKind.Note, UserText, string.Empty, false),
                "thinking" => new(StreamKind.Thinking, string.Empty, string.Empty, false),
                "tool_use" => new(StreamKind.ToolCall, string.Empty, Text(block, "name"), false) { Call = Text(block, "id"), About = block.TryGetProperty("input", out var input) ? Subject(input) : string.Empty },
                "tool_result" => Result(block),
                var other => new(StreamKind.Unknown, other, string.Empty, false),
            });
        }

        return parts;
    }

    /// <summary>The subtype of the note that lists the work in the background.</summary>
    private const string TasksChanged = "background_tasks_changed";

    // What a note's text is for the notes a model acts on (a StreamKind.Note carries Claude Code's own word for it).

    /// <summary>The note that says what Claude Code does beside a turn (<see cref="StreamLine.Status"/>) or names the permission mode now in force.</summary>
    public const string StatusNote = "status";

    /// <summary>The note that marks where a compaction cut the conversation.</summary>
    public const string CompactedNote = "compact_boundary";

    /// <summary>The note that says the conversation was emptied and a new session begins (<c>/clear</c>).</summary>
    public const string ClearedNote = "conversation_reset";

    /// <summary>The <see cref="StreamLine.Status"/> of a status note while Claude Code shortens the conversation.</summary>
    public const string CompactingStatus = "compacting";

    /// <summary>The text of the note that stands for a line of the user's that is no message of theirs.</summary>
    public const string UserText = "user text";

    /// <summary>Claude Code's tag around what a slash command printed, when that comes back in the user's place.</summary>
    private const string CommandOutputOpen = "<local-command-stdout>";
    private const string CommandOutputClose = "</local-command-stdout>";

    /// <summary>
    /// The members of a tool's input that say what a call is about, in the order they are looked for: a command
    /// before its description, a file before a pattern's folder. The first that is there with a text is taken.
    /// </summary>
    private static readonly string[] SubjectKeys = ["command", "file_path", "notebook_path", "pattern", "url", "query", "description", "skill", "title", "path", "prompt"];

    /// <summary>What a tool call is about: the first of <see cref="SubjectKeys"/> its input holds, its first line, 200 characters at most.</summary>
    private static string Subject(JsonElement input)
    {
        foreach (var key in SubjectKeys)
        {
            if (Text(input, key) is { Length: > 0 } value)
            {
                var first = FirstLine(value);
                return first.Length > 200 ? first[..200] : first;
            }
        }

        return string.Empty;
    }

    /// <summary>A tool's result: whose call it answers, whether it failed, and when it did, the first line of what it gave back.</summary>
    private static StreamPart Result(JsonElement block)
    {
        var failed = Flag(block, "is_error");
        var said = string.Empty;
        if (failed && block.TryGetProperty("content", out var content))
        {
            said = content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? string.Empty
                : content.ValueKind == JsonValueKind.Array
                    ? string.Join("\n", content.EnumerateArray().Select(part => Text(part, "text")).Where(text => text.Length > 0))
                    : string.Empty;
        }

        // Words meant for the model alone, when the user refused or interrupted the call: the user knows, and is not told again.
        if (said.StartsWith(NotProceeding, StringComparison.Ordinal) || said == RefusedWords)
        {
            said = string.Empty;
        }

        return new(StreamKind.ToolResult, FirstLine(said), string.Empty, failed) { Call = Text(block, "tool_use_id") };
    }

    /// <summary>What a host's permission tool tells Claude when the user refuses a call (<see cref="DenyAnswer"/>): words for the model, which a reader of the stream does not show the user again.</summary>
    public const string RefusedWords = "The user refused this tool call.";

    /// <summary>How Claude Code's own result of a call the user interrupted or refused begins (the fixture interrupt); the rest tells the model to stop and wait.</summary>
    private const string NotProceeding = "The user doesn't want to proceed with this tool use.";

    private static string FirstLine(string text)
    {
        var end = text.IndexOfAny(['\r', '\n']);
        return (end < 0 ? text : text[..end]).Trim();
    }

    /// <summary>Everything the note lists as running in the background; a task without an id or a kind is passed over.</summary>
    private static IReadOnlyList<StreamTask> Tasks(JsonElement root)
    {
        var tasks = new List<StreamTask>();
        if (!root.TryGetProperty("tasks", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return tasks;
        }

        foreach (var task in list.EnumerateArray().Where(task => task.ValueKind == JsonValueKind.Object))
        {
            var kind = Text(task, "task_type");
            if (kind.Length > 0)
            {
                tasks.Add(new(Text(task, "id"), kind, Text(task, "description"), Text(task, "subagent_type")));
            }
        }

        return tasks;
    }

    /// <summary>
    /// The permission mode in the words Claude Code's own mode line has for it on the screen ("manual mode on",
    /// "accept edits on"), so that a window says the same whichever source feeds it; a mode not known here by its
    /// own name, and nothing for no name.
    /// </summary>
    public static string? ModeWords(string mode) => mode switch
    {
        "" => null,
        "default" => "manual mode on",
        "acceptEdits" => "accept edits on",
        "plan" => "plan mode on",
        "auto" => "auto mode on",
        "bypassPermissions" => "bypass permissions on",
        _ => mode + " mode on",
    };

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static bool Flag(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>The texts of a list of texts; empty for a key that is missing or holds something else.</summary>
    private static IReadOnlyList<string> Texts(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var texts = new List<string>();
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
            {
                texts.Add(text);
            }
        }

        return texts;
    }

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
