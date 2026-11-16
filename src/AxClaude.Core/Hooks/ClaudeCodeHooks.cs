using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AxClaude.Core.Hooks;

/// <summary>
/// One event axClaude registers (docs/axClaude/events-and-grounding.md EG-2.1): Claude Code's name for it, the Claude
/// Code version it was verified with, and whether its hook carries <c>"async": true</c>, so that Claude Code does not
/// wait for it (EG-2.5), or is run while Claude Code waits, as the trial of WP-A3 found needed for that event.
/// </summary>
public sealed record HookSpec(string Name, string VerifiedWith, bool Async);

/// <summary>
/// The one place of Claude Code's hooks in axClaude (root CLAUDE.md rule 12, beside <c>ClaudeCodeTexts</c>,
/// <c>ClaudeCodeKeys</c> and <c>ClaudeCodeSettings</c>; events-and-grounding.md EG-2): the events registered, each with
/// the version it was verified with; the version gate; the settings file passed with <c>--settings</c> for each start
/// of Claude Code, never written into the user's or a project's settings; and the reading of an event's body down to
/// the neutral <see cref="HookEvent"/>. Every event name and field name of Claude Code's is written here and nowhere
/// else of axClaude's code. A hook observes and returns nothing, so it decides nothing (EG-2.9, G6).
/// Measured with Claude Code 2.1.325 (events-and-grounding.md section 4); checked again when <c>tools/upstream.ps1</c>
/// reports a new version.
/// </summary>
public static partial class ClaudeCodeHooks
{
    // Claude Code's names of the events registered (code.claude.com/docs/en/hooks; recorded with 2.1.325).
    public const string SessionStart = "SessionStart";
    public const string Stop = "Stop";
    public const string StopFailure = "StopFailure";
    public const string SubagentStart = "SubagentStart";
    public const string SubagentStop = "SubagentStop";
    public const string PermissionRequest = "PermissionRequest";
    public const string Notification = "Notification";
    public const string SessionEnd = "SessionEnd";

    // SessionStart's source, Claude Code's own words (M14): a start, --continue or --resume, /clear, /compact.
    public const string SourceStartup = "startup";
    public const string SourceResume = "resume";
    public const string SourceClear = "clear";
    public const string SourceCompact = "compact";

    /// <summary>Claude Code's option that loads one more settings file for a session (EG-2.3).</summary>
    public const string SettingsOption = "--settings";

    /// <summary>How long Claude Code lets a hook run, in seconds (EG-2.5); the verb ends itself well before (EG-2.6).</summary>
    public const int TimeoutSeconds = 5;

    // The fields read from an event's body (EG-2.1); every other field is skipped unread.
    private const string EventNameField = "hook_event_name";
    private const string SessionIdField = "session_id";
    private const string PromptIdField = "prompt_id";
    private const string TranscriptPathField = "transcript_path";
    private const string SourceField = "source";
    private const string LastMessageField = "last_assistant_message";
    private const string BackgroundTasksField = "background_tasks";
    private const string AgentIdField = "agent_id";
    private const string AgentTypeField = "agent_type";
    private const string ToolNameField = "tool_name";
    private const string NotificationTypeField = "notification_type";
    private const string ReasonField = "reason";

    // The fields of one task in Stop's background_tasks (M13 9); a shell's command is never read.
    private const string TaskIdField = "id";
    private const string TaskTypeField = "type";
    private const string TaskStatusField = "status";
    private const string TaskDescriptionField = "description";

    /// <summary>
    /// The events axClaude registers, and no other (EG-2.1). Not registered: PreCompact and PostCompact (they print
    /// rows into the conversation); the events for each tool call, each piece of text shown and each prompt sent (a
    /// process for each; the marks and the session file give the same); Setup and the rest (nothing axClaude needs).
    /// An event name a Claude Code does not know stops its start with a question, which is why the version gate
    /// (<see cref="Supports"/>) holds the file back from a Claude Code older than the table.
    /// <para>
    /// The trial of WP-A3 (2026-11-15, Claude Code 2.1.325, events-and-grounding.md 4.3): with <c>async</c> on all eight
    /// Claude Code started without a warning, and SessionStart and Stop arrived, Stop's process created 17 to 40 ms
    /// before the turn's end mark instead of 254 to 358 ms. SessionEnd's process was created but its event never
    /// arrived: Claude Code ended first. So SessionEnd is run while Claude Code waits (it waits about 1.5 s at most,
    /// M14), and the rest are not waited for.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyList<HookSpec> Events =
    [
        new(SessionStart, "2.1.325", Async: true),
        new(Stop, "2.1.325", Async: true),
        new(StopFailure, "2.1.325", Async: true),
        new(SubagentStart, "2.1.325", Async: true),
        new(SubagentStop, "2.1.325", Async: true),
        new(PermissionRequest, "2.1.325", Async: true),
        new(Notification, "2.1.325", Async: true),
        new(SessionEnd, "2.1.325", Async: false),
    ];

    /// <summary>The highest "verified with" of the table: the oldest Claude Code the file may be passed to (EG-2.4).</summary>
    public static string TableVersion => Events.MaxBy(spec => VersionOf(spec.VerifiedWith))!.VerifiedWith;

    /// <summary>
    /// Whether the settings file may be passed to the Claude Code whose <c>claude --version</c> printed
    /// <paramref name="versionLine"/> (EG-2.4): its first three numbers (<c>2.1.325 (Claude Code)</c>) at least
    /// <see cref="TableVersion"/>. No line, or one without three numbers, is no: no hooks, and the window works from the
    /// marks, the session file and the screen.
    /// </summary>
    public static bool Supports(string? versionLine) =>
        VersionOf(versionLine) is { } version && version >= VersionOf(TableVersion)!;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        // A program path is written as it is (a letter of another language stays a letter); JSON's own escapes remain.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly UTF8Encoding Utf8WithoutMark = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// The settings file's text (events-and-grounding.md section 6.1): a <c>hooks</c> object with one entry for each
    /// event of <see cref="Events"/>, each one command hook in exec form, no shell: <paramref name="program"/> with the
    /// one argument <paramref name="verb"/>, <c>async</c> where the table says so, and <see cref="TimeoutSeconds"/>. No
    /// matcher, no <c>shell</c>, no other key. Indented, LF, ending with a line end.
    /// </summary>
    public static string SettingsText(string program, string verb)
    {
        var hooks = new JsonObject();
        foreach (var spec in Events)
        {
            var handler = new JsonObject
            {
                ["type"] = "command",
                ["command"] = program,
                ["args"] = new JsonArray(verb),
            };
            if (spec.Async)
            {
                handler["async"] = true;
            }

            handler["timeout"] = TimeoutSeconds;
            hooks[spec.Name] = new JsonArray(new JsonObject { ["hooks"] = new JsonArray(handler) });
        }

        return new JsonObject { ["hooks"] = hooks }.ToJsonString(WriteOptions) + "\n";
    }

    /// <summary>
    /// Writes the settings file for <paramref name="program"/> when it is missing or its text is not exactly
    /// <see cref="SettingsText"/>'s (EG-2.2), as the AI tools' file is written; true when it was written. UTF-8
    /// without a mark, written beside the file and moved over it, so that a Claude Code starting in another window
    /// never reads half a file. Throws as <see cref="File.WriteAllText(string, string)"/> does; the caller then
    /// starts Claude Code without hooks (EG-2.3).
    /// </summary>
    public static bool WriteSettings(string file, string program, string verb)
    {
        var text = SettingsText(program, verb);
        if (File.Exists(file) && File.ReadAllText(file) == text)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        var beside = file + "." + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".tmp";
        File.WriteAllText(beside, text, Utf8WithoutMark);
        try
        {
            File.Move(beside, file, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            File.Delete(beside);
            throw;
        }

        return true;
    }

    /// <summary>
    /// The arguments that pass the settings file to one start of Claude Code (EG-2.3); the window puts them right after
    /// <c>--ax-screen-reader</c>, before the user's own arguments. Claude Code reads the file once at its start, and
    /// <c>--continue</c> and <c>--resume</c> do not restore it, so every start passes it.
    /// </summary>
    public static IReadOnlyList<string> SessionArguments(string file) => [SettingsOption, file];

    /// <summary>
    /// Whether the user's own arguments for Claude Code already pass a settings file (<c>--settings x</c> or
    /// <c>--settings=x</c>): Claude Code would then get two, so axClaude passes none (EG-2.3).
    /// </summary>
    public static bool UserPassesSettings(IReadOnlyList<string> claudeArguments) =>
        claudeArguments.Any(argument =>
            argument == SettingsOption || argument.StartsWith(SettingsOption + "=", StringComparison.Ordinal));

    /// <summary>
    /// The event Claude Code wrote to a hook's standard input, reduced to what axClaude uses (EG-2.1, section 6.2):
    /// first the event's name is found, every other value skipped; then only the fields of that event are read, and no
    /// string is made of any other (a tool's input, a shell's command, <c>cwd</c>). Property order does not matter; a
    /// field of the wrong type is a missing one. Null, counted as malformed by the caller, for a body that is no single
    /// JSON object, one without <c>hook_event_name</c> or <c>session_id</c>, or an event that is not registered.
    /// A SubagentStart or SubagentStop with an empty <c>agent_type</c> is a helper of Claude Code's own (a side
    /// question, a compaction): its worker type is null, which tells the caller to ignore it (EG-2.9).
    /// </summary>
    public static HookEvent? Parse(ReadOnlySpan<byte> body)
    {
        // A UTF-8 mark, should a version ever write one, is no part of the JSON.
        if (body is [0xEF, 0xBB, 0xBF, ..])
        {
            body = body[3..];
        }

        try
        {
            return EventName(body) is { } name && KindOf(name) is { } kind ? Read(body, kind) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>The kind of a registered event's name; null for any other name.</summary>
    private static HookKind? KindOf(string name) => name switch
    {
        SessionStart => HookKind.SessionStarted,
        Stop => HookKind.TurnStopped,
        StopFailure => HookKind.TurnFailed,
        SubagentStart => HookKind.WorkerStarted,
        SubagentStop => HookKind.WorkerStopped,
        PermissionRequest => HookKind.PermissionAsked,
        Notification => HookKind.Notified,
        SessionEnd => HookKind.SessionEnded,
        _ => null,
    };

    /// <summary>The first pass: the top-level <c>hook_event_name</c>, when the body is one JSON object and nothing after it.</summary>
    private static string? EventName(ReadOnlySpan<byte> body)
    {
        var reader = new Utf8JsonReader(body);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return null;
        }

        string? name = null;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var isName = reader.ValueTextEquals(EventNameField);
            reader.Read();
            if (isName && reader.TokenType == JsonTokenType.String)
            {
                name = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        // The object closed, and nothing but white space follows it.
        return reader.TokenType == JsonTokenType.EndObject && !reader.Read() ? name : null;
    }

    /// <summary>The second pass: the common fields and those of <paramref name="kind"/>, nothing else.</summary>
    private static HookEvent? Read(ReadOnlySpan<byte> body, HookKind kind)
    {
        string? session = null, turn = null, path = null, source = null, last = null;
        string? workerId = null, workerType = null, tool = null, reason = null;
        IReadOnlyList<ListedTask>? tasks = null;

        var reader = new Utf8JsonReader(body);
        reader.Read();
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals(SessionIdField))
            {
                session = Text(ref reader);
            }
            else if (reader.ValueTextEquals(PromptIdField))
            {
                turn = Text(ref reader);
            }
            else if (kind == HookKind.SessionStarted && reader.ValueTextEquals(TranscriptPathField))
            {
                path = Text(ref reader);
            }
            else if (kind == HookKind.SessionStarted && reader.ValueTextEquals(SourceField))
            {
                source = Text(ref reader);
            }
            else if (kind == HookKind.TurnStopped && reader.ValueTextEquals(LastMessageField))
            {
                last = Text(ref reader);
            }
            else if (kind == HookKind.TurnStopped && reader.ValueTextEquals(BackgroundTasksField))
            {
                tasks = Tasks(ref reader);
            }
            else if ((kind is HookKind.WorkerStarted or HookKind.WorkerStopped) && reader.ValueTextEquals(AgentIdField))
            {
                workerId = Text(ref reader);
            }
            else if ((kind is HookKind.WorkerStarted or HookKind.WorkerStopped) && reader.ValueTextEquals(AgentTypeField))
            {
                workerType = Text(ref reader);
            }
            else if (kind == HookKind.PermissionAsked && reader.ValueTextEquals(ToolNameField))
            {
                tool = Text(ref reader);
            }
            else if (kind == HookKind.Notified && reader.ValueTextEquals(NotificationTypeField))
            {
                reason = Text(ref reader);
            }
            else if (kind == HookKind.SessionEnded && reader.ValueTextEquals(ReasonField))
            {
                reason = Text(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        if (string.IsNullOrEmpty(session))
        {
            return null;
        }

        // An empty agent type is Claude Code's own helper, no worker of the user's (EG-2.9).
        if (workerType is { Length: 0 })
        {
            workerType = null;
        }

        return new HookEvent(kind, session, turn, path, source, last, tasks, workerId, workerType, tool, reason);
    }

    /// <summary>
    /// The background tasks of a Stop event, from its property name on: each object's id, kind, status, description
    /// and worker type (a missing text empty, the worker type null); anything that is no object is passed over, and a
    /// value that is no array gives null, as a missing list does.
    /// </summary>
    private static List<ListedTask>? Tasks(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var tasks = new List<ListedTask>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                continue;
            }

            string? id = null, type = null, status = null, description = null, worker = null;
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals(TaskIdField))
                {
                    id = Text(ref reader);
                }
                else if (reader.ValueTextEquals(TaskTypeField))
                {
                    type = Text(ref reader);
                }
                else if (reader.ValueTextEquals(TaskStatusField))
                {
                    status = Text(ref reader);
                }
                else if (reader.ValueTextEquals(TaskDescriptionField))
                {
                    description = Text(ref reader);
                }
                else if (reader.ValueTextEquals(AgentTypeField))
                {
                    worker = Text(ref reader);
                }
                else
                {
                    reader.Read();
                    reader.Skip();
                }
            }

            tasks.Add(new ListedTask(id ?? string.Empty, type ?? string.Empty, status ?? string.Empty, description ?? string.Empty,
                string.IsNullOrEmpty(worker) ? null : worker));
        }

        return tasks;
    }

    /// <summary>The value after the property name the reader stands on, when it is a string; null, and the value passed over, otherwise.</summary>
    private static string? Text(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        reader.Skip();
        return null;
    }

    /// <summary>The first three numbers of a version line (<c>2.1.325 (Claude Code)</c>), or null when it has none.</summary>
    private static Version? VersionOf(string? line)
    {
        if (line is null || VersionPattern().Match(line) is not { Success: true } match)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            && int.TryParse(match.Groups[2].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            && int.TryParse(match.Groups[3].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var patch)
                ? new Version(major, minor, patch)
                : null;
    }

    [GeneratedRegex(@"([0-9]+)\.([0-9]+)\.([0-9]+)", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
