using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Probe;

/// <summary>
/// The streams of a run merged by time, one line each: the time (ms on the probe's clock), the source and a short
/// description. PTY: a frame's end with the bottom rows it changed, BEL, OSC. HOOK: the event with its telling fields
/// and its transport. FILE: a line of the session file: type, role, block types, the first 8 characters of its uuid.
/// SEND: what the script wrote to Claude Code. APP: what the repository's model concluded from the screen.
/// </summary>
internal static class Timeline
{
    private sealed record Row(double T, int Order, string Source, string Text);

    public static int Run(string[] args)
    {
        var folder = args[0];
        double from = double.MinValue, to = double.MaxValue;
        var width = 240;
        var all = false;
        var debug = false;
        HashSet<string>? only = null;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--from": from = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--to": to = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--width": width = int.Parse(args[++i]); break;
                case "--all": all = true; break;
                case "--dbg": debug = true; break;
                case "--three": only = ["PTY", "HOOK", "FILE"]; break;
                case "--only":
                    only = new HashSet<string>(args[++i].Split(',').Select(name => name.Trim().ToUpperInvariant()));
                    debug |= only.Contains("DBG");
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 2;
            }
        }

        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine("No such output folder.");
            return 2;
        }

        var rows = new List<Row>();
        var order = 0;
        foreach (var record in Json.ReadLines(Path.Combine(folder, "pty-events.jsonl")))
        {
            if (Pty(record, all) is { } text)
            {
                rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "PTY", text));
            }
        }

        foreach (var record in Json.ReadLines(Path.Combine(folder, "hooks.jsonl")))
        {
            rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "HOOK", Hook(record)));
        }

        foreach (var record in Json.ReadLines(Path.Combine(folder, "transcript-arrivals.jsonl")))
        {
            if (FileRow(record, all) is { } text)
            {
                rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "FILE", text));
            }
        }

        foreach (var record in Json.ReadLines(Path.Combine(folder, "script.jsonl")))
        {
            var kind = Json.Str(record, "k");
            if (kind == "send")
            {
                rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "SEND", $"{Json.Str(record, "what")} \"{Txt.Clip(Json.Str(record, "text"), 100)}\" (bytes {Json.Str(record, "hex")?[..Math.Min(24, Json.Str(record, "hex")!.Length)]})"));
            }
            else if (Step(record, all) is { } text)
            {
                rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "STEP", text));
            }
        }

        foreach (var record in Json.ReadLines(Path.Combine(folder, "app.jsonl")))
        {
            if (App(record) is { } text)
            {
                rows.Add(new Row(Json.Num(record, "t") ?? 0, order++, "APP", text));
            }
        }

        if (debug)
        {
            // Claude Code's own account of its hooks, from its debug log (--debug-file), put on the probe's clock through
            // the wall clock: only the lines about hooks, which name the command or the address and nothing personal.
            var startText = Json.Str(Json.TryObject(File.Exists(Path.Combine(folder, "run.json")) ? File.ReadAllText(Path.Combine(folder, "run.json")) : "{}"), "startedWall");
            var debugPath = Path.Combine(folder, "claude-debug.txt");
            if (startText is not null && File.Exists(debugPath)
                && DateTime.TryParse(startText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var started))
            {
                using var stream = new FileStream(debugPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (reader.ReadLine() is { } line)
                {
                    var space = line.IndexOf(' ');
                    var close = line.IndexOf("] ", StringComparison.Ordinal);
                    if (space < 0 || close < 0)
                    {
                        continue;
                    }

                    var message = line[(close + 2)..];
                    if (!(message.StartsWith("Hooks: ", StringComparison.Ordinal) || message.StartsWith("HTTP hook ", StringComparison.Ordinal) || message.StartsWith("Hook ", StringComparison.Ordinal)
                        || message.StartsWith("hooks modules not loaded", StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    if (DateTime.TryParse(line[..space], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
                    {
                        rows.Add(new Row(Math.Round((at - started).TotalMilliseconds, 1), order++, "DBG", Txt.Clip(message, 200)));
                    }
                }
            }
        }

        var output = new StringBuilder();
        foreach (var row in rows.Where(row => row.T >= from && row.T <= to && (only is null || only.Contains(row.Source))).OrderBy(row => row.T).ThenBy(row => row.Order))
        {
            var line = $"{row.T.ToString("F1", CultureInfo.InvariantCulture),10}  {row.Source,-4}  {row.Text}";
            line = Redactor.Redact(line);
            output.AppendLine(line.Length > width ? line[..(width - 1)] + "…" : line);
        }

        Console.Out.Write(output.ToString());
        return 0;
    }

    private static string? Pty(JsonObject record, bool all)
    {
        switch (Json.Str(record, "k"))
        {
            case "bel":
                return "BEL";
            case "title":
                return $"title \"{Txt.Clip(Json.Str(record, "text"), 100)}\"";
            case "osc":
                return $"OSC {Json.Str(record, "code")};{Txt.Clip(Json.Str(record, "data"), 100)}";
            case "clear":
                return $"screen cleared (CSI {Json.Str(record, "param")} J)";
            case "frame":
                {
                    var changed = (record["rows"] as JsonArray ?? new JsonArray()).OfType<JsonObject>().Where(row => (Json.Str(row, "text") ?? string.Empty).Trim().Length > 0).ToList();
                    if (changed.Count == 0)
                    {
                        return null;
                    }

                    // The bottom rows: where the status, the prompt and the newest text are.
                    var shown = changed.Skip(Math.Max(0, changed.Count - 3)).Select(row => (Json.Bool(row, "hidden", false) ? "~" : string.Empty) + Txt.Clip(Json.Str(row, "text")!.Trim(), 100));
                    var why = Json.Str(record, "why") switch
                    {
                        "feed" => "cursor shown",
                        "quiet" => "100 ms quiet",
                        "send" => "the app's own lines",
                        var other => other,
                    };
                    return $"frame end ({why}), {changed.Count} row{(changed.Count == 1 ? string.Empty : "s")} changed: {string.Join(" | ", shown)}";
                }

            case "cursor-hide" when all:
                return "cursor hidden";
            case "cursor-show" when all:
                return "cursor shown";
            case "home" when all:
                return "cursor home";
            case "mode" when all:
                return $"mode {Json.Str(record, "mode")} {(Json.Bool(record, "set", false) ? "set" : "reset")}";
            case "chunk" when all:
                return $"chunk {Json.Int(record, "bytes", 0)} bytes at offset {Json.Int(record, "offset", 0)}";
            default:
                return null;
        }
    }

    private static readonly string[] Telling =
    [
        "source", "reason", "trigger", "notification_type", "tool_name", "tool_use_id", "agent_type", "agent_id", "permission_mode",
        "stop_hook_active", "is_interrupt", "load_reason", "memory_type", "model", "from_model", "to_model", "index", "final",
        "expansion_type", "command_name", "error", "title", "message", "prompt", "last_assistant_message", "delta", "file_path",
    ];

    private static string Hook(JsonObject record)
    {
        var sb = new StringBuilder();
        sb.Append(Json.Str(record, "event")).Append(" [").Append(Json.Str(record, "via")).Append(']');
        if (Json.Str(record, "routeEvent") is { } route)
        {
            sb.Append(" (registered for ").Append(route).Append(')');
        }

        var body = Json.TryObject(Json.Str(record, "body") ?? string.Empty);
        if (body is null)
        {
            sb.Append($" body of {Json.Int(record, "bodyBytes", 0)} bytes, not a JSON object");
        }
        else
        {
            foreach (var name in Telling)
            {
                if (body[name] is not { } value)
                {
                    continue;
                }

                var text = value is JsonValue ? value.ToString() : value.ToJsonString(Json.Line);
                if (name == "tool_use_id" && text.Length > 8)
                {
                    text = "…" + text[^8..];
                }

                sb.Append(' ').Append(name).Append('=').Append(value is JsonValue v && v.TryGetValue<string>(out _) && name is "message" or "prompt" or "last_assistant_message" or "delta" or "error" or "title" or "file_path"
                    ? "\"" + Txt.Clip(text, 60) + "\""
                    : Txt.Clip(text, 60));
            }

            if (body["tool_input"] is JsonObject input)
            {
                var telling = Json.Str(input, "command") ?? Json.Str(input, "file_path") ?? Json.Str(input, "pattern") ?? Json.Str(input, "description") ?? Json.Str(input, "prompt");
                sb.Append(" tool_input{").Append(string.Join(',', input.Select(pair => pair.Key))).Append('}');
                if (telling is not null)
                {
                    sb.Append(" \"").Append(Txt.Clip(telling, 60)).Append('"');
                }
            }

            if (body["background_tasks"] is JsonArray tasks && tasks.Count > 0)
            {
                sb.Append(" background_tasks=").Append(tasks.Count);
            }

            if (Json.Str(body, "session_id") is { Length: >= 8 } session)
            {
                sb.Append(" session=").Append(session[..8]);
            }
        }

        var t = Json.Num(record, "t") ?? 0;
        if (Json.Str(record, "via") == "http")
        {
            // Which connection and which request on it: a second request on one connection is keep-alive at work.
            sb.Append($" conn={Json.Int(record, "conn", 0)}.{Json.Int(record, "req", 0)}");
        }

        if (record["relay"] is JsonObject relay && Json.Num(relay, "tCreated") is { } created)
        {
            sb.Append(CultureInfo.InvariantCulture, $" (hook process created at {created:F1}, {t - created:F0} ms before its post)");
        }

        if (Json.Int(record, "delayMs", 0) > 0 || !Json.Bool(record, "replyWritten", true))
        {
            sb.Append(CultureInfo.InvariantCulture, $" (reply after {(Json.Num(record, "tReply") ?? t) - t:F0} ms{(Json.Bool(record, "replyWritten", true) ? string.Empty : ", not taken: the connection was closed")})");
        }

        if (Json.Int(record, "status", 200) != 200 || Json.Int(record, "replyBytes", 0) > 0)
        {
            sb.Append($" (replied {Json.Int(record, "status", 200)}, {Json.Int(record, "replyBytes", 0)} bytes)");
        }

        return sb.ToString();
    }

    private static string? FileRow(JsonObject record, bool all)
    {
        var file = ShortFile(Json.Str(record, "file"));
        switch (Json.Str(record, "k"))
        {
            case "line":
                break;
            case "tracked":
                return $"{file}: followed from now ({Json.Str(record, "why")}), {Json.Int(record, "bytesWhenFound", 0)} bytes in it";
            case "other-file":
                return $"new file {Json.Str(record, "file")} ({Json.Int(record, "bytes", 0)} bytes)";
            case "truncated":
                return $"{file}: SHORTER than before ({Json.Int(record, "from", 0)} to {Json.Int(record, "to", 0)} bytes): rewritten";
            case "replaced":
                return $"{file}: REPLACED by another file of the same name";
            case "open-failed":
                return $"{file}: could not be opened: {Json.Str(record, "error")}";
            case "root" when all:
                return $"folder followed: {Json.Str(record, "folder")} (exists: {Json.Bool(record, "exists", false)})";
            default:
                return null;
        }

        var sb = new StringBuilder();
        sb.Append(file).Append(": ");
        var raw = Json.Str(record, "line") ?? string.Empty;
        var entry = Json.TryObject(raw);
        if (entry is null)
        {
            sb.Append($"a line of {Json.Int(record, "bytes", 0)} bytes that is no JSON object");
            return sb.ToString();
        }

        sb.Append(Json.Str(entry, "type") ?? "(no type)");
        if (Json.Str(entry, "subtype") is { } subtype)
        {
            sb.Append('/').Append(subtype);
        }

        if (Json.Str(entry, "operation") is { } operation)
        {
            sb.Append(" operation=").Append(operation);
        }

        var message = entry["message"] as JsonObject;
        if (Json.Str(message, "role") is { } role)
        {
            sb.Append(" role=").Append(role);
        }

        string? preview = null;
        if (message?["content"] is JsonArray blocks)
        {
            var types = new List<string>();
            foreach (var block in blocks.OfType<JsonObject>())
            {
                var type = Json.Str(block, "type") ?? "?";
                if (type == "tool_use")
                {
                    type += ":" + Json.Str(block, "name") + (Json.Str(block, "id") is { Length: > 8 } id ? ":…" + id[^8..] : string.Empty);
                }
                else if (type == "tool_result")
                {
                    type += Json.Str(block, "tool_use_id") is { Length: > 8 } id ? ":…" + id[^8..] : string.Empty;
                    type += Json.Bool(block, "is_error", false) ? ":error" : string.Empty;
                }
                else if (type == "text")
                {
                    preview ??= Json.Str(block, "text");
                }

                types.Add(type);
            }

            sb.Append(" blocks=[").Append(string.Join(',', types)).Append(']');
        }
        else if (Json.Str(message, "content") is { } content)
        {
            sb.Append(" content=string");
            preview = content;
        }
        else if (Json.Str(entry, "content") is { } own)
        {
            preview = own;
        }
        else if (Json.Str(entry, "summary") is { } summary)
        {
            preview = summary;
        }

        if (Json.Str(message, "stop_reason") is { } stop)
        {
            sb.Append(" stop_reason=").Append(stop);
        }

        if (Json.Str(message, "id") is { Length: > 6 } messageId)
        {
            sb.Append(" msg=…").Append(messageId[^6..]);
        }

        if (Json.Str(entry, "uuid") is { Length: >= 8 } uuid)
        {
            sb.Append(" uuid=").Append(uuid[..8]);
        }

        if (Json.Str(entry, "parentUuid") is { Length: >= 8 } parent)
        {
            sb.Append(" parent=").Append(parent[..8]);
        }

        foreach (var flag in new[] { "isSidechain", "isMeta", "isCompactSummary", "isApiErrorMessage" })
        {
            if (Json.Bool(entry, flag, false))
            {
                sb.Append(' ').Append(flag);
            }
        }

        if (entry["toolUseResult"] is not null)
        {
            sb.Append(" +toolUseResult");
        }

        // How long after the time Claude Code stamped on the entry the line was in the file.
        if (Json.Str(entry, "timestamp") is { } stamp
            && DateTime.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var stamped)
            && DateTime.TryParse(Json.Str(record, "wall"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var arrived))
        {
            sb.Append(CultureInfo.InvariantCulture, $" in file {(arrived - stamped).TotalMilliseconds:+0;-0} ms after its timestamp");
        }

        if (preview is { Length: > 0 })
        {
            sb.Append(" \"").Append(Txt.Clip(preview, 50)).Append('"');
        }

        if (Json.Bool(record, "first", false))
        {
            sb.Append(" (already in the file when it was found)");
        }

        if (Json.Num(record, "tornMs") is { } torn)
        {
            sb.Append(CultureInfo.InvariantCulture, $" (written in pieces over {torn:F0} ms)");
        }

        sb.Append($" [{Json.Int(record, "bytes", 0)} bytes]");
        return sb.ToString();
    }

    /// <summary>A session file as the first 8 characters of its name, a subagent's with its folder's last part.</summary>
    private static string ShortFile(string? label)
    {
        if (label is null)
        {
            return "?";
        }

        var parts = label.Split('\\', '/');
        var name = Path.GetFileNameWithoutExtension(parts[^1]);
        var shortName = name.Length > 8 ? name[..8] : name;
        return parts.Length > 2 ? parts[^2] + "/" + (name.Length > 14 ? name[..14] : name) : shortName;
    }

    private static string? Step(JsonObject record, bool all)
    {
        switch (Json.Str(record, "k"))
        {
            case "started":
                return $"Claude Code started, process {Json.Int(record, "pid", 0)}";
            case "exit":
                return $"Claude Code's process ended, exit code {Json.Int(record, "code", 0)}";
            case "closing":
                return $"the probe closes the console ({Json.Str(record, "why")})";
            case "mark":
                return $"mark {Json.Str(record, "label")}";
            case "write-file":
                return $"file written in the working folder: {Json.Str(record, "path")}";
            case "script-error":
                return $"script error: {Json.Str(record, "error")}";
            case "waited":
                return Json.Bool(record, "ok", false)
                    ? (all ? $"{Json.Str(record, "verb")} {Txt.Clip(Json.Str(record, "detail"), 60)}: met after {Json.Num(record, "ms"):F0} ms" : null)
                    : $"{Json.Str(record, "verb")} {Txt.Clip(Json.Str(record, "detail"), 60)}: TIMED OUT after {Json.Num(record, "ms"):F0} ms";
            default:
                return null;
        }
    }

    private static string? App(JsonObject record)
    {
        switch (Json.Str(record, "k"))
        {
            case "status":
                {
                    var sb = new StringBuilder();
                    sb.Append("status ").Append(Json.Str(record, "status")).Append(" (").Append(Json.Str(record, "why")).Append(')');
                    sb.Append(" working=").Append(Json.Bool(record, "working", false) ? "yes" : "no");
                    if (Json.Str(record, "mode") is { } mode)
                    {
                        sb.Append(" mode=\"").Append(Txt.Clip(mode, 40)).Append('"');
                    }

                    if (Json.Str(record, "question") is { } question)
                    {
                        sb.Append(" question=\"").Append(Txt.Clip(question, 60)).Append("\" options=").Append(Json.Int(record, "questionOptions", 0));
                    }
                    else if (Json.Bool(record, "promptPending", false))
                    {
                        sb.Append(" a prompt waits");
                    }

                    if (Json.Str(record, "screen") is { } screen)
                    {
                        sb.Append(" screen=\"").Append(Txt.Clip(screen, 60)).Append('"');
                    }

                    if (Json.Bool(record, "awaitsText", false))
                    {
                        sb.Append(" awaits text");
                    }

                    if (Json.Int(record, "queued", 0) > 0)
                    {
                        sb.Append(" queued=").Append(Json.Int(record, "queued", 0));
                    }

                    if (Json.Str(record, "background") is { } background)
                    {
                        sb.Append(" background=\"").Append(background).Append('"');
                    }

                    if (Json.Bool(record, "detailedView", false))
                    {
                        sb.Append(" detailed view open");
                    }

                    return sb.ToString();
                }

            case "line":
                return $"line {Json.Str(record, "op")} [{Json.Str(record, "kind")}] \"{Txt.Clip(Json.Str(record, "text"), 100)}\"";
            case "bell":
                return "the model heard the bell";
            case "response-started":
                return "response started (first reply, thinking or tool line after the output marker)";
            case "announcement":
                return $"Claude Code's announcement row: \"{Txt.Clip(Json.Str(record, "text"), 100)}\"";
            case "model-error":
                return $"the model threw in {Json.Str(record, "where")}: {Json.Str(record, "error")}";
            default:
                return null;
        }
    }
}

/// <summary>
/// The structure of what a run recorded: for every hook event the keys of its input with their value types (and, for
/// hooks, one value seen, cut short), by which transports it came and how often; for the session file the same per
/// entry type, without values unless asked.
/// </summary>
internal static class Shape
{
    public static int Run(string[] args)
    {
        var folder = args[0];
        var what = args.Skip(1).FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? "both";
        var values = args.Contains("--values");
        var output = new StringBuilder();
        if (what is "both" or "hooks")
        {
            var groups = new SortedDictionary<string, (Dictionary<string, int> Via, SortedDictionary<string, KeyShape> Keys, int Count)>(StringComparer.Ordinal);
            foreach (var record in Json.ReadLines(Path.Combine(folder, "hooks.jsonl")))
            {
                var name = Json.Str(record, "event") ?? "?";
                if (!groups.TryGetValue(name, out var group))
                {
                    group = (new Dictionary<string, int>(), new SortedDictionary<string, KeyShape>(StringComparer.Ordinal), 0);
                }

                var via = Json.Str(record, "via") ?? "?";
                group.Via[via] = group.Via.GetValueOrDefault(via) + 1;
                group.Count++;
                if (Json.TryObject(Json.Str(record, "body") ?? string.Empty) is { } body)
                {
                    Walk(body, string.Empty, group.Keys, 0);
                }

                groups[name] = group;
            }

            output.AppendLine("HOOK EVENTS");
            foreach (var pair in groups)
            {
                output.AppendLine();
                output.AppendLine($"{pair.Key}: {pair.Value.Count} request(s), {string.Join(", ", pair.Value.Via.OrderBy(v => v.Key).Select(v => $"{v.Value} by {v.Key}"))}");
                foreach (var key in pair.Value.Keys)
                {
                    output.AppendLine($"  {key.Key}: {key.Value.Types(pair.Value.Count)}{(key.Value.Example is { } example ? "   e.g. " + Txt.Clip(example, 70) : string.Empty)}");
                }
            }
        }

        if (what is "both" or "file")
        {
            var groups = new SortedDictionary<string, (SortedDictionary<string, KeyShape> Keys, int Count)>(StringComparer.Ordinal);
            foreach (var record in Json.ReadLines(Path.Combine(folder, "transcript-arrivals.jsonl")))
            {
                if (Json.Str(record, "k") != "line" || Json.TryObject(Json.Str(record, "line") ?? string.Empty) is not { } entry)
                {
                    continue;
                }

                var name = (Json.Str(entry, "type") ?? "(no type)") + (Json.Str(entry, "subtype") is { } subtype ? "/" + subtype : string.Empty)
                    + (entry["message"] is JsonObject message && Json.Str(message, "role") is { } role ? " role=" + role : string.Empty);
                if (!groups.TryGetValue(name, out var group))
                {
                    group = (new SortedDictionary<string, KeyShape>(StringComparer.Ordinal), 0);
                }

                group.Count++;
                Walk(entry, string.Empty, group.Keys, 0);
                groups[name] = group;
            }

            output.AppendLine();
            output.AppendLine("SESSION FILE ENTRIES");
            foreach (var pair in groups)
            {
                output.AppendLine();
                output.AppendLine($"{pair.Key}: {pair.Value.Count} line(s)");
                foreach (var key in pair.Value.Keys)
                {
                    output.AppendLine($"  {key.Key}: {key.Value.Types(pair.Value.Count)}{(values && key.Value.Example is { } example ? "   e.g. " + Txt.Clip(example, 70) : string.Empty)}");
                }
            }
        }

        Console.Out.Write(Redactor.Redact(output.ToString()));
        return 0;
    }

    private sealed class KeyShape
    {
        private readonly SortedSet<string> _types = new(StringComparer.Ordinal);
        private int _seen;

        public string? Example { get; private set; }

        public void Add(string type, string? example)
        {
            _types.Add(type);
            _seen++;
            Example ??= example;
        }

        public string Types(int outOf) => string.Join(" | ", _types) + (_seen < outOf ? $" (in {_seen} of {outOf})" : string.Empty);
    }

    private static void Walk(JsonNode? node, string path, SortedDictionary<string, KeyShape> keys, int depth)
    {
        if (node is JsonObject o)
        {
            if (path.Length > 0)
            {
                Note(keys, path, "object", null);
            }

            if (depth >= 4)
            {
                return;
            }

            foreach (var pair in o)
            {
                Walk(pair.Value, path.Length == 0 ? pair.Key : path + "." + pair.Key, keys, depth + 1);
            }
        }
        else if (node is JsonArray array)
        {
            Note(keys, path, array.Count == 0 ? "array (empty)" : "array", null);
            if (depth >= 4)
            {
                return;
            }

            foreach (var item in array)
            {
                // An array's items under one name: content[].type and the like.
                if (item is JsonObject or JsonArray)
                {
                    Walk(item, path + "[]", keys, depth + 1);
                }
                else
                {
                    Note(keys, path + "[]", TypeOf(item), item?.ToString());
                }
            }
        }
        else
        {
            Note(keys, path, TypeOf(node), node?.ToString());
        }
    }

    private static void Note(SortedDictionary<string, KeyShape> keys, string path, string type, string? example)
    {
        if (!keys.TryGetValue(path, out var shape))
        {
            keys[path] = shape = new KeyShape();
        }

        shape.Add(type, example);
    }

    private static string TypeOf(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        return node.GetValueKind() switch
        {
            System.Text.Json.JsonValueKind.String => "string",
            System.Text.Json.JsonValueKind.Number => "number",
            System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => "boolean",
            _ => node.GetValueKind().ToString().ToLowerInvariant(),
        };
    }
}
