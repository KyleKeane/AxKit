using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Probe;

/// <summary>
/// One recorded case of Claude Code's non-interactive stream made into a stream fixture
/// (docs/axClaude/stream-mode.md SM-10.1): <c>tests/fixtures/streams/&lt;case&gt;.jsonl</c>, every line Claude Code
/// wrote and what the host wrote between them, on one clock, for the tests that replay the stream's model. As
/// <see cref="FixtureMaker"/> is for a timeline, this is the whitelist that makes a fixture safe to commit, and
/// nothing of a case reaches the repository but through here:
/// <list type="number">
/// <item>A line is reduced to the fields named here for its kind; a kind not known keeps its type alone. What is
/// dropped is counted by its key's name, so that a field the model turns out to need can be added by name.</item>
/// <item>Ids become numbers in the order they are met: a session's, a tool call's, a task's. No other id is kept.</item>
/// <item>The folder the case ran in becomes <c>C:\work</c>, and every kept string is masked as the timeline
/// fixtures' strings are.</item>
/// <item>The check: the file is read back and refused, and deleted, when it still holds an e-mail address, an
/// account path, one of the masked names or the probe's own folder.</item>
/// </list>
/// The raw lines are never written to a file. What it prints are counts and the names of keys, never a text.
/// </summary>
internal static class StreamFixtureMaker
{
    /// <summary>What stands for the folder a case ran in.</summary>
    private const string WorkFolder = @"C:\work";

    /// <summary>A kept text is cut after this many characters: a tool's result can be a whole file.</summary>
    private const int LongText = 400;

    /// <summary>The members kept of an object whose shape is Claude Code's own and not read in full: a task of the list of background tasks, a task's change.</summary>
    private static readonly string[] TaskFields = ["type", "task_type", "status", "description", "subagent_type", "agent_type", "is_backgrounded", "name", "summary", "error"];

    /// <summary>The numbers given to ids in the order they are met, each kind counted by itself.</summary>
    private sealed class Ids
    {
        private readonly Dictionary<string, string> _given = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

        public JsonNode? Of(string kind, JsonNode? id)
        {
            if (id is not JsonValue value || !value.TryGetValue<string>(out var text) || text.Length == 0)
            {
                return null;
            }

            if (!_given.TryGetValue(kind + "\n" + text, out var given))
            {
                _counts[kind] = _counts.GetValueOrDefault(kind) + 1;
                _given[kind + "\n" + text] = given = kind + "-" + _counts[kind].ToString(CultureInfo.InvariantCulture);
            }

            return given;
        }
    }

    /// <summary>What a case's lines are cleaned with: the folder it ran in, the masker, the ids, and the names of the keys dropped.</summary>
    private sealed class Cleaner(string folder, Masker masker)
    {
        private readonly string[] _folders =
        [
            folder,
            folder.Replace('\\', '/'),
            Path.GetDirectoryName(folder) ?? folder,
            (Path.GetDirectoryName(folder) ?? folder).Replace('\\', '/'),
        ];

        public Ids Ids { get; } = new();

        public SortedSet<string> Dropped { get; } = new(StringComparer.Ordinal);

        /// <summary>Ids, which are kept as numbers where they are needed or not at all: never worth a mention among the keys dropped.</summary>
        private static readonly string[] IdKeys = ["id", "uuid", "session_id", "task_id", "tool_use_id", "run_id", "request_id"];

        public string Text(string text, int most = int.MaxValue)
        {
            foreach (var path in _folders)
            {
                text = text.Replace(path, WorkFolder, StringComparison.OrdinalIgnoreCase);
            }

            text = masker.Text(text);
            return text.Length > most ? text[..most] + "…" : text;
        }

        /// <summary>A string cleaned, a number or a flag as it is; null for anything else.</summary>
        public JsonNode? Scalar(JsonNode? node, int most = int.MaxValue) =>
            node is JsonValue value
                ? value.TryGetValue<string>(out var text) ? JsonValue.Create(Text(text, most)) : value.DeepClone()
                : null;

        /// <summary>The named members of an object that are there and are scalars; every other member's name is noted as dropped.</summary>
        public JsonObject Pick(JsonObject source, string where, params string[] fields)
        {
            var picked = new JsonObject();
            foreach (var (name, node) in source)
            {
                if (fields.Contains(name, StringComparer.Ordinal) && Scalar(node) is { } kept)
                {
                    picked[name] = kept;
                }
                else if (!fields.Contains(name, StringComparer.Ordinal) && !IdKeys.Contains(name, StringComparer.Ordinal))
                {
                    Dropped.Add(where + "." + name);
                }
            }

            return picked;
        }
    }

    /// <summary>
    /// Writes the fixture of one case. <paramref name="records"/>: each line read (<c>out</c>) and each line the host
    /// wrote (<c>in</c>, empty for standard input closed) with its time; <paramref name="toolLog"/>: the stand-in
    /// permission tool's log of the case, whose fourth and fifth fields are the time and the question. True when
    /// the file was written; false, and nothing left, when the check refused it.
    /// </summary>
    public static bool Write(string fixtures, string name, string version, IReadOnlyList<(long Ms, string Kind, string Text)> records,
        string toolLog, long startedUtcTicks, string folder, string permission, int exitCode, SortedSet<string> dropped)
    {
        var masker = new Masker(FixtureMaker.Names());
        var cleaner = new Cleaner(folder, masker);
        var entries = new List<(long Ms, int Order, JsonObject Entry)>();
        var order = 0;
        int read = 0, typeAlone = 0, wrote = 0, asks = 0;
        foreach (var (ms, kind, text) in records)
        {
            if (kind == "out")
            {
                var reduced = Line(Json.TryObject(text), cleaner, out var known);
                read++;
                typeAlone += known ? 0 : 1;
                entries.Add((ms, order++, new JsonObject { ["t"] = ms, ["k"] = "out", ["line"] = reduced }));
            }
            else
            {
                wrote++;
                var entry = new JsonObject { ["t"] = ms, ["k"] = "in" };
                foreach (var (member, value) in Written(Json.TryObject(text), cleaner))
                {
                    entry[member] = value?.DeepClone();
                }

                entries.Add((ms, order++, entry));
            }
        }

        if (File.Exists(toolLog))
        {
            foreach (var asked in File.ReadAllLines(toolLog))
            {
                // "tool, the input's keys, the question's keys, the time in ticks, the question": the last two for this tool alone.
                var parts = asked.Split('\t');
                if (parts.Length < 5 || !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks))
                {
                    continue;
                }

                var ms = Math.Max(0, (ticks - startedUtcTicks) / TimeSpan.TicksPerMillisecond);
                var question = Json.TryObject(parts[4]);
                var tool = Json.Str(question, "tool_name") ?? parts[0];
                asks++;
                entries.Add((ms, order++, new JsonObject
                {
                    ["t"] = ms,
                    ["k"] = "ask",
                    ["tool"] = cleaner.Text(tool),
                    ["call"] = cleaner.Ids.Of("call", question?["tool_use_id"]),
                    ["input"] = Input(tool, question?["input"] as JsonObject, cleaner),
                }));
                // The stand-in answers at once: a question of Claude's with the first option of each, anything else as the case says.
                entries.Add((ms, order++, new JsonObject { ["t"] = ms, ["k"] = "answer", ["behavior"] = tool == "AskUserQuestion" ? "answered" : permission.Length == 0 ? "allow" : permission }));
            }
        }

        entries.Add((records.Count > 0 ? records[^1].Ms : 0, order++, new JsonObject { ["t"] = records.Count > 0 ? records[^1].Ms : 0, ["k"] = "exit", ["code"] = exitCode }));

        var lines = new StringBuilder();
        lines.Append(new JsonObject
        {
            ["k"] = "meta",
            ["claudeCode"] = version,
            ["recorded"] = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["by"] = "tools/ClaudeProbe stream --record",
            ["permission"] = permission.Length == 0 ? "allow" : permission,
        }.ToJsonString(Json.Line)).Append('\n');
        foreach (var (_, _, entry) in entries.OrderBy(entry => entry.Ms).ThenBy(entry => entry.Order))
        {
            lines.Append(entry.ToJsonString(Json.Line)).Append('\n');
        }

        Directory.CreateDirectory(fixtures);
        var path = Path.Combine(fixtures, name + ".jsonl");
        File.WriteAllText(path, lines.ToString(), new UTF8Encoding(false));

        // The check, on what is on the disk now.
        var written = File.ReadAllText(path);
        var (emails, paths, names) = FixtureMaker.Check(written, masker.Names);
        var own = written.Contains("axkit-stream-probe", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (emails + paths + names + own > 0)
        {
            File.Delete(path);
            Console.WriteLine($"  fixture refused, and deleted: {emails} e-mail addresses, {paths} account paths, {names} names, {own} of the probe's own folder");
            return false;
        }

        Console.WriteLine($"  fixture written: {read} lines read ({typeAlone} kept as their type alone), {wrote} written by the host, {asks} questions to the permission tool; names masked: {masker.Masked}");
        // The denials are kept, as the tools' names; the rest is said once, for all the cases, by the caller.
        cleaner.Dropped.Remove("result.permission_denials");
        dropped.UnionWith(cleaner.Dropped);
        return true;
    }

    /// <summary>One line of the stream reduced to the fields kept for its kind; <paramref name="known"/> false for a kind kept as its type alone.</summary>
    private static JsonObject Line(JsonObject? line, Cleaner cleaner, out bool known)
    {
        known = true;
        var type = Json.Str(line, "type");
        var subtype = Json.Str(line, "subtype");
        switch (type)
        {
            case "system" when subtype == "init":
            {
                var kept = Pick(line!, "type", "subtype", "claude_code_version", "permissionMode");
                kept["session_id"] = cleaner.Ids.Of("session", line!["session_id"]);
                return kept;
            }

            case "system" when subtype == "status":
                return Pick(line!, "type", "subtype", "status", "permissionMode");

            case "system" when subtype == "task_started":
            {
                var kept = cleaner.Pick(line!, "task_started", "type", "subtype", "description", "task_type", "subagent_type", "is_backgrounded", "spawn_depth");
                kept["task_id"] = cleaner.Ids.Of("task", line!["task_id"]);
                kept["tool_use_id"] = cleaner.Ids.Of("call", line["tool_use_id"]);
                return kept;
            }

            case "system" when subtype == "task_notification":
            {
                var kept = cleaner.Pick(line!, "task_notification", "type", "subtype", "status", "summary");
                kept["task_id"] = cleaner.Ids.Of("task", line!["task_id"]);
                kept["tool_use_id"] = cleaner.Ids.Of("call", line["tool_use_id"]);
                return kept;
            }

            case "system" when subtype == "task_updated":
            {
                var kept = Pick(line!, "type", "subtype");
                kept["task_id"] = cleaner.Ids.Of("task", line!["task_id"]);
                if (line["patch"] is JsonObject patch)
                {
                    kept["patch"] = cleaner.Pick(patch, "task_updated.patch", TaskFields);
                }

                return kept;
            }

            case "system" when subtype == "background_tasks_changed":
            {
                var kept = Pick(line!, "type", "subtype");
                var tasks = new JsonArray();
                foreach (var task in (line!["tasks"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    var one = cleaner.Pick(task, "background_tasks_changed.tasks", TaskFields);
                    one["id"] = cleaner.Ids.Of("task", task["id"] ?? task["task_id"]);
                    one["tool_use_id"] = cleaner.Ids.Of("call", task["tool_use_id"]);
                    tasks.Add(one);
                }

                kept["tasks"] = tasks;
                return kept;
            }

            case "system":
                return Pick(line!, "type", "subtype");

            case "assistant":
            {
                var kept = Pick(line!, "type", "is_api_error_message");
                if (cleaner.Scalar(line!["error"]) is { } error)
                {
                    kept["error"] = error;
                }

                kept["parent_tool_use_id"] = cleaner.Ids.Of("call", line["parent_tool_use_id"]);
                var blocks = new JsonArray();
                foreach (var block in (((line["message"] as JsonObject)?["content"]) as JsonArray ?? []).OfType<JsonObject>())
                {
                    var kind = Json.Str(block, "type") ?? string.Empty;
                    switch (kind)
                    {
                        case "text":
                            blocks.Add(new JsonObject { ["type"] = kind, ["text"] = cleaner.Text(Json.Str(block, "text") ?? string.Empty) });
                            break;
                        case "tool_use":
                            var tool = Json.Str(block, "name") ?? string.Empty;
                            blocks.Add(new JsonObject { ["type"] = kind, ["id"] = cleaner.Ids.Of("call", block["id"]), ["name"] = cleaner.Text(tool), ["input"] = Input(tool, block["input"] as JsonObject, cleaner) });
                            break;
                        default:
                            // Thinking among them: its text is not kept.
                            blocks.Add(new JsonObject { ["type"] = cleaner.Text(kind) });
                            break;
                    }
                }

                kept["message"] = new JsonObject { ["content"] = blocks };
                return kept;
            }

            case "user":
            {
                var kept = Pick(line!, "type", "isReplay", "isSynthetic");
                kept["parent_tool_use_id"] = cleaner.Ids.Of("call", line!["parent_tool_use_id"]);
                var content = (line["message"] as JsonObject)?["content"];
                JsonNode? keptContent = null;
                if (content is JsonValue value && value.TryGetValue<string>(out var said))
                {
                    // A line Claude Code wrote in the user's place (the summary a compaction leaves) is no message:
                    // its start is enough to know it by.
                    keptContent = cleaner.Text(said, kept["isSynthetic"] is JsonValue synthetic && synthetic.TryGetValue<bool>(out var made) && made ? 80 : int.MaxValue);
                }
                else if (content is JsonArray list)
                {
                    var blocks = new JsonArray();
                    foreach (var block in list.OfType<JsonObject>())
                    {
                        var kind = Json.Str(block, "type") ?? string.Empty;
                        switch (kind)
                        {
                            case "text":
                                blocks.Add(new JsonObject { ["type"] = kind, ["text"] = cleaner.Text(Json.Str(block, "text") ?? string.Empty, LongText) });
                                break;
                            case "tool_result":
                                // What a tool gave back is kept only when it failed: the model shows the first
                                // line of a failure and nothing of a success, and a success can be a whole file,
                                // a path of the user's or an id of Claude Code's own.
                                var result = new JsonObject { ["type"] = kind, ["tool_use_id"] = cleaner.Ids.Of("call", block["tool_use_id"]) };
                                if (block["is_error"] is JsonValue failed && failed.TryGetValue<bool>(out var isError))
                                {
                                    result["is_error"] = isError;
                                    if (isError)
                                    {
                                        result["content"] = cleaner.Text(ResultText(block["content"]), LongText);
                                    }
                                }

                                blocks.Add(result);
                                break;
                            default:
                                blocks.Add(new JsonObject { ["type"] = cleaner.Text(kind) });
                                break;
                        }
                    }

                    keptContent = blocks;
                }

                kept["message"] = new JsonObject { ["content"] = keptContent };
                return kept;
            }

            case "result":
            {
                var kept = cleaner.Pick(line!, "result", "type", "subtype", "is_error", "result", "terminal_reason", "stop_reason", "queued_turn_count", "num_turns");
                var denials = new JsonArray();
                foreach (var denial in (line!["permission_denials"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    denials.Add(new JsonObject { ["tool_name"] = cleaner.Text(Json.Str(denial, "tool_name") ?? string.Empty) });
                }

                kept["permission_denials"] = denials;
                return kept;
            }

            case "control_response":
            {
                var kept = Pick(line!, "type");
                if (line!["response"] is JsonObject response)
                {
                    kept["response"] = Pick(response, "subtype", "request_id");
                }

                return kept;
            }

            case "conversation_reset":
            {
                var kept = Pick(line!, "type", "trigger");
                kept["session_id"] = cleaner.Ids.Of("session", line!["session_id"]);
                kept["new_conversation_id"] = cleaner.Ids.Of("session", line["new_conversation_id"]);
                return kept;
            }

            default:
                known = false;
                return type is null ? [] : new JsonObject { ["type"] = cleaner.Text(type) };
        }

        // The named scalars of a line whose other members are known and need no count.
        JsonObject Pick(JsonObject source, params string[] fields)
        {
            var picked = new JsonObject();
            foreach (var field in fields)
            {
                if (cleaner.Scalar(source[field]) is { } kept)
                {
                    picked[field] = kept;
                }
            }

            return picked;
        }
    }

    /// <summary>
    /// A tool's input: a question of Claude's with its questions, headers, options and whether several may be
    /// chosen; any other tool's with its scalars, a long one cut. What is nested and not a question is dropped by name.
    /// </summary>
    private static JsonObject Input(string tool, JsonObject? input, Cleaner cleaner)
    {
        var kept = new JsonObject();
        if (input is null)
        {
            return kept;
        }

        foreach (var (name, node) in input)
        {
            if (tool == "AskUserQuestion" && name == "questions" && node is JsonArray questions)
            {
                var list = new JsonArray();
                foreach (var question in questions.OfType<JsonObject>())
                {
                    var one = new JsonObject();
                    foreach (var field in new[] { "question", "header", "multiSelect" })
                    {
                        if (cleaner.Scalar(question[field]) is { } value)
                        {
                            one[field] = value;
                        }
                    }

                    var options = new JsonArray();
                    foreach (var option in (question["options"] as JsonArray ?? []).OfType<JsonObject>())
                    {
                        var choice = new JsonObject();
                        foreach (var field in new[] { "label", "description" })
                        {
                            if (cleaner.Scalar(option[field]) is { } value)
                            {
                                choice[field] = value;
                            }
                        }

                        options.Add(choice);
                    }

                    one["options"] = options;
                    list.Add(one);
                }

                kept[name] = list;
            }
            else if (cleaner.Scalar(node, LongText) is { } scalar)
            {
                kept[name] = scalar;
            }
            else
            {
                cleaner.Dropped.Add("input of " + tool + "." + name);
            }
        }

        return kept;
    }

    /// <summary>A tool result's content as one text: the text itself, or the texts of its blocks one under the other.</summary>
    private static string ResultText(JsonNode? content)
    {
        if (content is JsonValue value && value.TryGetValue<string>(out var text))
        {
            return text;
        }

        return content is JsonArray blocks
            ? string.Join("\n", blocks.OfType<JsonObject>().Select(block => Json.Str(block, "text") ?? string.Empty).Where(part => part.Length > 0))
            : string.Empty;
    }

    /// <summary>What the host wrote, in axClaude's words: a message with its text, an interrupt, a mode, or standard input closed.</summary>
    private static JsonObject Written(JsonObject? line, Cleaner cleaner)
    {
        var entry = new JsonObject();
        if (line is null)
        {
            entry["what"] = "closed";
            return entry;
        }

        if (Json.Str(line, "type") == "user")
        {
            entry["what"] = "message";
            entry["text"] = cleaner.Text(Json.Str(line["message"] as JsonObject, "content") ?? string.Empty);
            return entry;
        }

        var request = line["request"] as JsonObject;
        var subtype = Json.Str(request, "subtype") ?? string.Empty;
        entry["what"] = subtype == "set_permission_mode" ? "mode" : cleaner.Text(subtype);
        entry["id"] = cleaner.Text(Json.Str(line, "request_id") ?? string.Empty);
        if (Json.Str(request, "mode") is { } mode)
        {
            entry["mode"] = cleaner.Text(mode);
        }

        return entry;
    }
}
