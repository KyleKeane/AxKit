using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace Probe;

/// <summary>
/// <c>ClaudeProbe fixture &lt;run folder&gt; &lt;fixture folder&gt;</c>: one recorded run made into a timeline fixture
/// (docs/axClaude/events-and-grounding.md EG-11.1, section 6.6). The tool is the whitelist that makes a fixture safe to
/// commit, so nothing of a run reaches the repository but through here:
/// <list type="number">
/// <item>Events: only the eight events axClaude registers (EG-2.1), each with only the fields it takes; of a pair of
/// the same event by both transports the one by command, the transport axClaude uses.</item>
/// <item>Entries: every line of the session file in file order, the kinds of EG-3.4 with only their fields, every other
/// line its type alone, so that the order and the kinds stay and nothing else does.</item>
/// <item>Masking, in the console bytes and in every kept string, length for length: the account's, the computer's and
/// the domain's names become as many <c>x</c> as they have bytes, and a <c>Users</c> right in front of one does too.</item>
/// <item>The check: every file written is read back, and the fixture is refused and deleted when it holds an e-mail
/// address, an account path or one of those names (<see cref="Redaction"/>).</item>
/// </list>
/// What it prints are counts and fixed words, never a text of the run.
/// </summary>
internal static class FixtureMaker
{
    /// <summary>The events axClaude registers (EG-2.1), each with the fields it takes beside the three every event gives.</summary>
    private static readonly Dictionary<string, string[]> EventFields = new(StringComparer.Ordinal)
    {
        ["SessionStart"] = ["transcript_path", "source"],
        ["Stop"] = ["last_assistant_message", "background_tasks"],
        ["StopFailure"] = [],
        ["SubagentStart"] = ["agent_id", "agent_type"],
        ["SubagentStop"] = ["agent_id", "agent_type"],
        ["PermissionRequest"] = ["tool_name"],
        ["Notification"] = ["notification_type"],
        ["SessionEnd"] = ["reason"],
    };

    private static readonly string[] EveryEvent = ["hook_event_name", "session_id", "prompt_id"];

    private static readonly string[] TaskFields = ["id", "type", "status", "description", "agent_type"];

    /// <summary>The files a fixture is made of, besides expected.txt, which the replayer's tests write.</summary>
    private static readonly string[] Written = ["meta.json", "pty.vt", "pty.vt.chunks.txt", "events.jsonl", "entries.jsonl", "sends.jsonl"];

    /// <summary>Exit codes: 0 written, 2 not a run folder, 4 refused by the check.</summary>
    public static int Run(string runFolder, string fixtureFolder)
    {
        var run = Path.GetFullPath(runFolder);
        var fixture = Path.GetFullPath(fixtureFolder);
        string[] needed = ["run.json", "pty.vt", "pty.vt.chunks.txt"];
        var missing = needed.Count(name => !File.Exists(Path.Combine(run, name)));
        if (missing > 0)
        {
            Console.Error.WriteLine($"Not a run folder of the probe: {missing} of {needed.Length} files missing (run.json, pty.vt, pty.vt.chunks.txt).");
            return 2;
        }

        var masker = new Masker(Names());
        var created = !Directory.Exists(fixture);
        Directory.CreateDirectory(fixture);

        // The console stream: masked byte for byte, so that every position in it, and with them the chunk index, stays.
        var pty = masker.Bytes(File.ReadAllBytes(Path.Combine(run, "pty.vt")));
        var maskedInPty = masker.Masked;
        File.WriteAllBytes(Path.Combine(fixture, "pty.vt"), pty);
        var chunks = File.ReadAllLines(Path.Combine(run, "pty.vt.chunks.txt"));
        WriteLines(Path.Combine(fixture, "pty.vt.chunks.txt"), chunks);

        var events = Events(Path.Combine(run, "hooks.jsonl"), masker, out var requests, out var twins, out var otherKinds);
        WriteLines(Path.Combine(fixture, "events.jsonl"), events.Select(record => record.ToJsonString(Json.Line)));

        var entries = Entries(Path.Combine(run, "transcript-arrivals.jsonl"), masker, out var read, out var sessionFiles, out var finished);
        WriteLines(Path.Combine(fixture, "entries.jsonl"), entries.Select(record => record.ToJsonString(Json.Line)));

        var sends = Sends(Path.Combine(run, "script.jsonl"), masker);
        WriteLines(Path.Combine(fixture, "sends.jsonl"), sends.Select(record => record.ToJsonString(Json.Line)));

        var meta = Meta(run, finished);
        WriteLines(Path.Combine(fixture, "meta.json"), [meta.ToJsonString(Json.Line)]);

        Console.WriteLine($"pty.vt: {pty.Length} bytes in {chunks.Count(line => line.Length > 0 && line[0] != '#')} chunks, {maskedInPty} names masked");
        Console.WriteLine($"events: {events.Count} kept of {requests} requests ({twins} twins by the other transport left out, {otherKinds} of kinds axClaude does not register)");
        Console.WriteLine($"entries: {entries.Count} lines of {sessionFiles} session files, {read} read, {entries.Count - read} kept as their type alone; {finished} turns finished");
        Console.WriteLine($"sends: {sends.Count}");
        Console.WriteLine($"names masked in all: {masker.Masked}");

        // The check, on what is on the disk now.
        var refusals = new List<string>();
        foreach (var name in Written)
        {
            var bytes = File.ReadAllBytes(Path.Combine(fixture, name));
            var (emails, paths, names) = Check(Encoding.UTF8.GetString(bytes), masker.Names);
            if (name == "pty.vt")
            {
                // Text the screen shows with escape sequences between its parts: checked again with them taken out.
                var (moreEmails, morePaths, moreNames) = Check(Encoding.UTF8.GetString(Masker.WithoutEscapes(bytes)), masker.Names);
                emails += moreEmails;
                paths += morePaths;
                names += moreNames;
            }

            if (emails + paths + names > 0)
            {
                refusals.Add($"{name}: {emails} e-mail addresses, {paths} account paths, {names} names");
            }
        }

        if (refusals.Count > 0)
        {
            foreach (var name in Written)
            {
                File.Delete(Path.Combine(fixture, name));
            }

            if (created && !Directory.EnumerateFileSystemEntries(fixture).Any())
            {
                Directory.Delete(fixture);
            }

            Console.WriteLine("refused, and deleted what was written: " + string.Join("; ", refusals));
            return 4;
        }

        Console.WriteLine("written");
        return 0;
    }

    /// <summary>The names to mask: the account's, the computer's and the domain's (Redaction.DefaultNames), the distinctive ones, longest first.</summary>
    private static List<string> Names() =>
        Redaction.DefaultNames()
            .Where(name => name.Length >= 3 && !name.Equals("user", StringComparison.OrdinalIgnoreCase) && !name.Equals("admin", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(name => name.Length)
            .ToList();

    /// <summary>What must never be committed, counted: e-mail addresses and account paths (Redaction.Find), and any occurrence of a masked name.</summary>
    private static (int Emails, int Paths, int Names) Check(string text, IReadOnlyList<string> names)
    {
        var hits = Redaction.Find(text).ToList();
        var emails = hits.Count(hit => hit.Contains('@'));
        var found = 0;
        foreach (var name in names)
        {
            for (var at = text.IndexOf(name, StringComparison.OrdinalIgnoreCase); at >= 0; at = text.IndexOf(name, at + name.Length, StringComparison.OrdinalIgnoreCase))
            {
                found++;
            }
        }

        return (emails, hits.Count - emails, found);
    }

    /// <summary>
    /// The events of EG-2.1 from hooks.jsonl, each reduced to its fields and masked, in the order they arrived. A request
    /// by http whose reduced event equals one by command within 2 seconds is the same event twice: the one by command stays.
    /// </summary>
    private static List<JsonObject> Events(string path, Masker masker, out int requests, out int twins, out int otherKinds)
    {
        var kept = new List<(double T, string Via, string Key, JsonObject Event)>();
        requests = 0;
        otherKinds = 0;
        foreach (var record in Json.ReadLines(path))
        {
            requests++;
            var body = Json.TryObject(Json.Str(record, "body") ?? string.Empty);
            var name = Json.Str(body, "hook_event_name");
            if (body is null || name is null || !EventFields.TryGetValue(name, out var fields))
            {
                otherKinds++;
                continue;
            }

            var reduced = new JsonObject();
            foreach (var field in EveryEvent.Concat(fields))
            {
                if (field == "background_tasks")
                {
                    if (body[field] is JsonArray tasks)
                    {
                        var list = new JsonArray();
                        foreach (var task in tasks.OfType<JsonObject>())
                        {
                            list.Add(Pick(task, TaskFields, masker));
                        }

                        reduced[field] = list;
                    }
                }
                else if (Kept(body[field], masker) is { } value)
                {
                    reduced[field] = value;
                }
            }

            kept.Add((Json.Num(record, "t") ?? 0, Json.Str(record, "via") ?? "http", reduced.ToJsonString(Json.Line), reduced));
        }

        twins = 0;
        var dropped = new bool[kept.Count];
        for (var i = 0; i < kept.Count; i++)
        {
            if (kept[i].Via != "http" || dropped[i])
            {
                continue;
            }

            for (var j = 0; j < kept.Count; j++)
            {
                if (j != i && !dropped[j] && kept[j].Via == "command" && kept[j].Key == kept[i].Key && Math.Abs(kept[j].T - kept[i].T) <= 2000)
                {
                    dropped[i] = true;
                    twins++;
                    break;
                }
            }
        }

        return kept.Where((_, index) => !dropped[index])
            .OrderBy(item => item.T)
            .Select(item => new JsonObject { ["t"] = item.T, ["event"] = item.Event })
            .ToList();
    }

    /// <summary>
    /// Every line the session files gained, from transcript-arrivals.jsonl, in file order: a session file is a .jsonl
    /// directly in the project folder (EG-3.1), so files of subagents and tool results are left out. Each line is
    /// reduced by <see cref="Entry"/> and carries its session's id, the file's name, for a run with two (/clear).
    /// </summary>
    private static List<JsonObject> Entries(string path, Masker masker, out int read, out int files, out int finished)
    {
        var entries = new List<JsonObject>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        read = 0;
        finished = 0;
        foreach (var record in Json.ReadLines(path))
        {
            var file = Json.Str(record, "file");
            if (Json.Str(record, "k") != "line" || file is null || !file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
                || file.Split('\\', '/').Length > 2 || !seen.Add(file + "|" + (Json.Num(record, "offset") ?? -1).ToString(CultureInfo.InvariantCulture)))
            {
                continue;
            }

            var session = Path.GetFileNameWithoutExtension(file);
            sessions.Add(session);
            var entry = Entry(Json.TryObject(Json.Str(record, "line") ?? string.Empty), masker, out var isRead);
            if (isRead)
            {
                read++;
            }

            if (Json.Str(entry, "type") == "system" && Json.Str(entry, "subtype") == "turn_duration")
            {
                finished++;
            }

            entries.Add(new JsonObject { ["t"] = Json.Num(record, "t") ?? 0, ["session"] = masker.Text(session), ["entry"] = entry });
        }

        files = sessions.Count;
        return entries;
    }

    /// <summary>
    /// One line of a session file reduced to the fields of EG-3.4 for its kind; every other line its type alone, and a
    /// line without a type, or no JSON object, an empty object. A string that starts with <c>&lt;</c> (a command, a shell
    /// line, a notification: Claude Code's own wrapping) keeps its first tag alone, which is all the reader looks at.
    /// </summary>
    private static JsonObject Entry(JsonObject? line, Masker masker, out bool read)
    {
        read = true;
        var type = Json.Str(line, "type");
        switch (type)
        {
            case "user":
            {
                var entry = Pick(line!, ["type", "uuid", "promptId", "promptSource", "timestamp", "isSidechain", "isMeta", "isCompactSummary"], masker);
                var meta = Json.Bool(line, "isMeta", false) || Json.Bool(line, "isCompactSummary", false);
                var content = (line!["message"] as JsonObject)?["content"];
                JsonNode? kept = null;
                if (content is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    kept = meta ? string.Empty : masker.Text(FirstTag(text));
                }
                else if (content is JsonArray blocks)
                {
                    var list = new JsonArray();
                    foreach (var block in blocks.OfType<JsonObject>())
                    {
                        var blockType = Json.Str(block, "type");
                        if (blockType == "tool_result")
                        {
                            list.Add(Pick(block, ["type", "tool_use_id", "is_error"], masker));
                        }
                        else if (blockType == "text" && Json.Str(block, "text") is { } said && said.StartsWith("[Request interrupted", StringComparison.Ordinal))
                        {
                            list.Add(Pick(block, ["type", "text"], masker));
                        }
                        else
                        {
                            list.Add(new JsonObject { ["type"] = masker.Text(blockType ?? string.Empty) });
                        }
                    }

                    kept = list;
                }

                entry["message"] = new JsonObject { ["content"] = kept };
                return entry;
            }

            case "assistant":
            {
                var entry = Pick(line!, ["type", "uuid", "timestamp", "isSidechain", "isAbortedMidStream"], masker);
                var message = line!["message"] as JsonObject;
                var kept = Pick(message ?? [], ["id", "stop_reason"], masker);
                var list = new JsonArray();
                foreach (var block in (message?["content"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    list.Add(Json.Str(block, "type") switch
                    {
                        "text" => Pick(block, ["type", "text"], masker),
                        "tool_use" => Pick(block, ["type", "id", "name"], masker),
                        var other => new JsonObject { ["type"] = masker.Text(other ?? string.Empty) },
                    });
                }

                kept["content"] = list;
                entry["message"] = kept;
                return entry;
            }

            case "system":
                return Pick(line!, Json.Str(line, "subtype") == "turn_duration" ? ["type", "subtype", "uuid", "durationMs"] : ["type", "subtype", "uuid"], masker);

            case "queue-operation":
            {
                var entry = Pick(line!, ["type", "operation", "timestamp"], masker);
                if (Json.Str(line, "operation") == "enqueue" && Json.Str(line, "content") is { } queued)
                {
                    entry["content"] = masker.Text(FirstTag(queued));
                }

                return entry;
            }

            default:
                read = false;
                return type is null ? [] : new JsonObject { ["type"] = masker.Text(type) };
        }
    }

    /// <summary>What the script wrote: a message, a key by its name (Ctrl+X then Ctrl+S is one, SendNow), a text written without Enter, bytes.</summary>
    private static List<JsonObject> Sends(string path, Masker masker)
    {
        var records = Json.ReadLines(path).Where(record => Json.Str(record, "k") == "send").ToList();
        var sends = new List<JsonObject>();
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var t = Json.Num(record, "t") ?? 0;
            var text = Json.Str(record, "text") ?? string.Empty;
            var hex = Json.Str(record, "hex") ?? string.Empty;
            switch (Json.Str(record, "what"))
            {
                case "message":
                    sends.Add(new JsonObject { ["t"] = t, ["kind"] = "message", ["text"] = masker.Text(text) });
                    break;
                case "message-enter" or "enter-again":
                    // The Enter of a message, and the window's own Enter again (FR-2.1): part of sending it.
                    break;
                case "key" when hex == "18" && i + 1 < records.Count && Json.Str(records[i + 1], "hex") == "13":
                    sends.Add(new JsonObject { ["t"] = t, ["kind"] = "key", ["name"] = "SendNow" });
                    i++;
                    break;
                case "key":
                    sends.Add(new JsonObject { ["t"] = t, ["kind"] = "key", ["name"] = hex == "1b" ? "Escape" : masker.Text(text) });
                    break;
                case "send" or "type" or "type-more":
                    sends.Add(new JsonObject { ["t"] = t, ["kind"] = "text", ["text"] = masker.Text(text) });
                    break;
                default:
                    sends.Add(new JsonObject { ["t"] = t, ["kind"] = "bytes", ["hex"] = masker.Text(hex) });
                    break;
            }
        }

        return sends;
    }

    /// <summary>meta.json: Claude Code's version, the console's size, the day of the recording, the run it came from, the turns that finished.</summary>
    private static JsonObject Meta(string run, int finished)
    {
        var info = Json.TryObject(File.ReadAllText(Path.Combine(run, "run.json")));
        var version = (Json.Str(info, "claudeVersion") ?? string.Empty).Split(' ')[0];
        var size = (Json.Str(info, "size") ?? "0x0").Split('x');
        var started = DateTimeOffset.TryParse(Json.Str(info, "startedWall"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var wall) ? wall.ToLocalTime() : DateTimeOffset.Now;
        var name = Path.GetFileName(run);
        return new JsonObject
        {
            ["claudeCode"] = version,
            ["columns"] = int.TryParse(size[0], out var columns) ? columns : 0,
            ["rows"] = size.Length > 1 && int.TryParse(size[1], out var rows) ? rows : 0,
            ["recorded"] = started.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["from"] = Path.GetFileName(Path.GetDirectoryName(run)) + "/" + (name.EndsWith("-out", StringComparison.Ordinal) ? name[..^4] : name),
            ["turns"] = finished,
        };
    }

    /// <summary>The named fields of an object that are there, strings masked; anything else as it is (numbers, flags).</summary>
    private static JsonObject Pick(JsonObject source, string[] fields, Masker masker)
    {
        var picked = new JsonObject();
        foreach (var field in fields)
        {
            if (source.ContainsKey(field))
            {
                picked[field] = Kept(source[field], masker);
            }
        }

        return picked;
    }

    /// <summary>A string masked, a number or a flag as it is, null as null; an object or an array is never kept whole.</summary>
    private static JsonNode? Kept(JsonNode? node, Masker masker) =>
        node is JsonValue value
            ? value.TryGetValue<string>(out var text) ? JsonValue.Create(masker.Text(text)) : value.DeepClone()
            : null;

    /// <summary>A string that starts with a tag, its first tag alone; any other string as it is.</summary>
    private static string FirstTag(string text)
    {
        if (!text.StartsWith('<'))
        {
            return text;
        }

        var end = text.IndexOf('>');
        return end < 0 ? "<" : text[..(end + 1)];
    }

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            text.Append(line).Append('\n');
        }

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
    }
}

/// <summary>
/// Whole-word occurrences of names replaced, byte for byte, by <c>x</c>: in a console stream every position stays, so a
/// chunk index and the cursor's moves still fit. An escape sequence counts as a word's edge (text drawn in bold follows
/// one directly), the text of a title or another string sequence as text. Case is not minded for ASCII letters. A
/// <c>Users</c> with one separator in front of a masked name (<c>\</c>, <c>\\</c>, <c>/</c>, <c>-</c>) is masked too, so
/// that what is left of an account path is no account path.
/// </summary>
internal sealed class Masker(IReadOnlyList<string> names)
{
    private enum State { Ground, Escape, Csi, OscCode, String }

    public IReadOnlyList<string> Names { get; } = names;

    /// <summary>How many names were masked, in all the calls.</summary>
    public int Masked { get; private set; }

    public string Text(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var masked = Bytes(bytes);
        return masked.AsSpan().SequenceEqual(bytes) ? text : Encoding.UTF8.GetString(masked);
    }

    public byte[] Bytes(byte[] data)
    {
        var result = (byte[])data.Clone();
        var escape = EscapeBytes(data);
        foreach (var name in Names)
        {
            var pattern = Encoding.UTF8.GetBytes(name);
            for (var i = 0; i + pattern.Length <= result.Length; i++)
            {
                if (!SameAt(result, i, pattern) || !Edge(result, escape, i - 1) || !Edge(result, escape, i + pattern.Length))
                {
                    continue;
                }

                result.AsSpan(i, pattern.Length).Fill((byte)'x');
                Masked++;
                var separator = i >= 2 && result[i - 1] == '\\' && result[i - 2] == '\\' ? 2
                    : i >= 1 && result[i - 1] is (byte)'\\' or (byte)'/' or (byte)'-' ? 1
                    : 0;
                var users = i - separator - 5;
                if (separator > 0 && users >= 0 && SameAt(result, users, "Users"u8) && Edge(result, escape, users - 1))
                {
                    result.AsSpan(users, 5).Fill((byte)'x');
                }

                i += pattern.Length - 1;
            }
        }

        return result;
    }

    /// <summary>The bytes with the escape sequences taken out: what the screen shows, for the check.</summary>
    public static byte[] WithoutEscapes(byte[] data)
    {
        var escape = EscapeBytes(data);
        var text = new List<byte>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            if (!escape[i])
            {
                text.Add(data[i]);
            }
        }

        return [.. text];
    }

    /// <summary>Which bytes belong to an escape sequence: CSI sequences, two-byte escapes, and the introducer of an OSC or another string (its text is text).</summary>
    private static bool[] EscapeBytes(byte[] data)
    {
        var escape = new bool[data.Length];
        var state = State.Ground;
        for (var i = 0; i < data.Length; i++)
        {
            var b = data[i];
            switch (state)
            {
                case State.Ground or State.String:
                    if (b == 0x1b)
                    {
                        escape[i] = true;
                        state = State.Escape;
                    }
                    else if (b == 0x07 && state == State.String)
                    {
                        escape[i] = true;
                        state = State.Ground;
                    }

                    break;
                case State.Escape:
                    escape[i] = true;
                    state = b switch
                    {
                        (byte)'[' => State.Csi,
                        (byte)']' => State.OscCode,
                        (byte)'P' or (byte)'X' or (byte)'^' or (byte)'_' => State.String,
                        _ => State.Ground,
                    };
                    break;
                case State.Csi:
                    escape[i] = true;
                    if (b is >= 0x40 and <= 0x7e)
                    {
                        state = State.Ground;
                    }

                    break;
                case State.OscCode:
                    if (b == 0x07)
                    {
                        escape[i] = true;
                        state = State.Ground;
                    }
                    else if (b == 0x1b)
                    {
                        escape[i] = true;
                        state = State.Escape;
                    }
                    else
                    {
                        escape[i] = true;
                        if (b == (byte)';')
                        {
                            state = State.String;
                        }
                    }

                    break;
            }
        }

        return escape;
    }

    /// <summary>The bytes at <paramref name="at"/> equal the pattern, ASCII letters in either case.</summary>
    private static bool SameAt(byte[] data, int at, ReadOnlySpan<byte> pattern)
    {
        for (var k = 0; k < pattern.Length; k++)
        {
            if (Lower(data[at + k]) != Lower(pattern[k]))
            {
                return false;
            }
        }

        return true;
    }

    private static byte Lower(byte b) => b is >= (byte)'A' and <= (byte)'Z' ? (byte)(b + 32) : b;

    /// <summary>A word's edge: the start or the end, a byte of an escape sequence, or a byte that is no ASCII letter, digit or underscore.</summary>
    private static bool Edge(byte[] data, bool[] escape, int at) =>
        at < 0 || at >= data.Length || escape[at] || !(char.IsAsciiLetterOrDigit((char)data[at]) || data[at] == '_');
}
