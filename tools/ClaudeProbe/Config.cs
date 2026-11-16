using System.Reflection;
using System.Text.Json.Nodes;
using AxClaude.Core.Transcript;

namespace Probe;

/// <summary>The hook events of the documentation reference (code.claude.com/docs/en/hooks, fetched 2026-11-15): 33.</summary>
internal static class HookEvents
{
    public static readonly string[] All =
    [
        "SessionStart", "Setup", "UserPromptSubmit", "UserPromptExpansion", "PreToolUse", "PermissionRequest",
        "PermissionDenied", "PostToolUse", "PostToolUseFailure", "PostToolBatch", "Notification", "MessageDisplay",
        "SubagentStart", "SubagentStop", "TaskCreated", "TaskCompleted", "Stop", "StopFailure", "TeammateIdle",
        "InstructionsLoaded", "ConfigChange", "CwdChanged", "DirectoryAdded", "FileChanged", "WorktreeCreate",
        "WorktreeRemove", "PreCompact", "PostCompact", "PreModelSwitch", "PostModelSwitch", "Elicitation",
        "ElicitationResult", "SessionEnd",
    ];

    /// <summary>The reference: these two take command and mcp_tool handlers only, no http.</summary>
    public static readonly HashSet<string> CommandOnly = new(StringComparer.Ordinal) { "SessionStart", "Setup" };

    /// <summary>A hook on these does the work itself (it gives the worktree's path): never registered unless the plan names them.</summary>
    public static readonly HashSet<string> NotObservers = new(StringComparer.Ordinal) { "WorktreeCreate", "WorktreeRemove" };
}

/// <summary>Which hook each event gets and how the listener answers it.</summary>
internal sealed class HookPlan
{
    /// <summary>"plan" or "none" (no hooks key in the settings at all).</summary>
    public string Mode = "plan";

    /// <summary>For every event the plan does not name: auto, http, command, both or none.</summary>
    public string Default = "auto";

    public Dictionary<string, string> Events = new(StringComparer.Ordinal);
    public Dictionary<string, int> DelayMs = new(StringComparer.Ordinal);
    public Dictionary<string, int> TimeoutSeconds = new(StringComparer.Ordinal);
    public HashSet<string> Async = new(StringComparer.Ordinal);
    public Dictionary<string, string> Matchers = new(StringComparer.Ordinal);
    public Dictionary<string, JsonObject> Replies = new(StringComparer.Ordinal);

    /// <summary>More fields for the handlers of an event ("*" for every event): statusMessage, onFailure, headers and the like, written as given.</summary>
    public Dictionary<string, JsonObject> Extra = new(StringComparer.Ordinal);

    /// <summary>The http hooks point at a port nothing listens on; the command hooks still reach the listener.</summary>
    public bool ClosedPort;

    /// <summary>How a command hook is written: exec (command plus args, no shell), bash, powershell, or shell (a command string, Claude Code's default shell).</summary>
    public string CommandForm = "exec";

    public IEnumerable<string> EventNames() => HookEvents.All.Concat(Events.Keys.Where(name => !HookEvents.All.Contains(name)));

    public string TransportFor(string name)
    {
        if (Mode == "none")
        {
            return "none";
        }

        if (Events.TryGetValue(name, out var asked))
        {
            return asked;
        }

        if (HookEvents.NotObservers.Contains(name))
        {
            return "none";
        }

        return Default switch
        {
            "auto" => HookEvents.CommandOnly.Contains(name) ? "command" : "http",
            "http" or "both" => HookEvents.CommandOnly.Contains(name) ? "command" : Default,
            "command" => "command",
            _ => "none",
        };
    }

    public int DelayFor(string name) => DelayMs.TryGetValue(name, out var ms) ? ms : DelayMs.TryGetValue("*", out var all) ? all : 0;

    public static HookPlan From(JsonNode? node)
    {
        var plan = new HookPlan();
        if (node is JsonValue value && value.TryGetValue<string>(out var word))
        {
            // "hooks": "none" or "hooks": "both" and the like.
            if (word == "none")
            {
                plan.Mode = "none";
            }
            else
            {
                plan.Default = word;
            }

            return plan;
        }

        if (node is not JsonObject o)
        {
            return plan;
        }

        plan.Mode = Json.Str(o, "mode") ?? plan.Mode;
        plan.Default = Json.Str(o, "default") ?? plan.Default;
        plan.ClosedPort = Json.Bool(o, "closedPort", false);
        plan.CommandForm = Json.Str(o, "commandForm") ?? plan.CommandForm;
        foreach (var pair in o["events"] as JsonObject ?? new JsonObject())
        {
            plan.Events[pair.Key] = pair.Value!.GetValue<string>();
        }

        foreach (var pair in o["delayMs"] as JsonObject ?? new JsonObject())
        {
            plan.DelayMs[pair.Key] = (int)pair.Value!.GetValue<double>();
        }

        foreach (var pair in o["timeoutSeconds"] as JsonObject ?? new JsonObject())
        {
            plan.TimeoutSeconds[pair.Key] = (int)pair.Value!.GetValue<double>();
        }

        foreach (var item in o["async"] as JsonArray ?? new JsonArray())
        {
            plan.Async.Add(item!.GetValue<string>());
        }

        foreach (var pair in o["matchers"] as JsonObject ?? new JsonObject())
        {
            plan.Matchers[pair.Key] = pair.Value!.GetValue<string>();
        }

        foreach (var pair in o["replies"] as JsonObject ?? new JsonObject())
        {
            if (pair.Value is JsonObject reply)
            {
                plan.Replies[pair.Key] = reply;
            }
        }

        foreach (var pair in o["extra"] as JsonObject ?? new JsonObject())
        {
            if (pair.Value is JsonObject extra)
            {
                plan.Extra[pair.Key] = extra;
            }
        }

        return plan;
    }

    /// <summary>The extra fields of the plan written into a handler: those for every event first, then the event's own.</summary>
    public void AddExtra(string name, JsonObject handler)
    {
        foreach (var key in new[] { "*", name })
        {
            if (Extra.TryGetValue(key, out var extra))
            {
                foreach (var pair in extra)
                {
                    handler[pair.Key] = pair.Value?.DeepClone();
                }
            }
        }
    }

    public JsonObject Resolved()
    {
        var o = new JsonObject();
        foreach (var name in EventNames())
        {
            o[name] = TransportFor(name);
        }

        return o;
    }
}

internal sealed class Config
{
    public string Cwd = string.Empty;
    public string Out = string.Empty;
    public List<string> Args = [];
    public string Model = "haiku";
    public int Cols = 240;
    public int Rows = 50;
    public bool DebugFile = true;
    public bool ScreenReader = true;
    public bool PassSettings = true;
    public Dictionary<string, string> Env = new(StringComparer.OrdinalIgnoreCase);
    public string? Claude;

    /// <summary>Testing only: another program in the pseudo console in place of Claude Code (no Claude arguments are added).</summary>
    public List<string>? Program;

    /// <summary>The listener's port; 0 takes a free one.</summary>
    public int Port;
    public int MaxRunMs = 300_000;
    public int LingerMs = 2_000;
    public HookPlan Hooks = new();
    public JsonObject? ExtraSettings;
    public JsonArray Script = new();

    /// <summary>%NAME% in a path or argument of the configuration becomes that environment variable's value, so that a configuration need not spell the profile folder.</summary>
    private static string? Expand(string? text) => text is null ? null : Environment.ExpandEnvironmentVariables(text);

    public static Config Load(string path)
    {
        var root = JsonNode.Parse(File.ReadAllText(path), null, Json.Lenient) as JsonObject
            ?? throw new InvalidOperationException("The configuration is not a JSON object.");
        var config = new Config
        {
            Cwd = Path.GetFullPath(Expand(Json.Str(root, "cwd")) ?? throw new InvalidOperationException("The configuration needs \"cwd\", the working folder for Claude Code.")),
            Out = Path.GetFullPath(Expand(Json.Str(root, "out")) ?? throw new InvalidOperationException("The configuration needs \"out\", the output folder.")),
        };
        config.Model = Json.Str(root, "model") ?? config.Model;
        config.Cols = Json.Int(root, "cols", config.Cols);
        config.Rows = Json.Int(root, "rows", config.Rows);
        config.DebugFile = Json.Bool(root, "debugFile", config.DebugFile);
        config.ScreenReader = Json.Bool(root, "screenReader", config.ScreenReader);
        config.PassSettings = Json.Bool(root, "passSettings", config.PassSettings);
        config.Claude = Expand(Json.Str(root, "claude"));
        config.Port = Json.Int(root, "port", 0);
        config.MaxRunMs = Json.Int(root, "maxRunMs", config.MaxRunMs);
        config.LingerMs = Json.Int(root, "lingerMs", config.LingerMs);
        foreach (var item in root["args"] as JsonArray ?? new JsonArray())
        {
            config.Args.Add(Expand(item!.GetValue<string>())!);
        }

        foreach (var pair in root["env"] as JsonObject ?? new JsonObject())
        {
            config.Env[pair.Key] = Expand(pair.Value!.GetValue<string>())!;
        }

        if (root["program"] is JsonArray program)
        {
            config.Program = program.Select(item => Expand(item!.GetValue<string>())!).ToList();
        }

        config.Hooks = HookPlan.From(root["hooks"]);
        config.ExtraSettings = root["settings"] as JsonObject;
        config.Script = root["script"] as JsonArray ?? new JsonArray();
        return config;
    }
}

/// <summary>The settings file given to Claude Code with --settings: the hooks of the plan, and nothing of the user's.</summary>
internal static class SettingsWriter
{
    public static JsonObject Build(Config config, int listenerPort, int httpPort, string probeExe)
    {
        var settings = new JsonObject();
        var plan = config.Hooks;
        if (plan.Mode != "none")
        {
            var hooks = new JsonObject();
            foreach (var name in plan.EventNames())
            {
                var transport = plan.TransportFor(name);
                if (transport == "none")
                {
                    continue;
                }

                var handlers = new JsonArray();
                if (transport is "http" or "both")
                {
                    var http = new JsonObject
                    {
                        ["type"] = "http",
                        ["url"] = $"http://127.0.0.1:{httpPort}/hook/{name}?via=http",
                    };
                    if (plan.TimeoutSeconds.TryGetValue(name, out var seconds))
                    {
                        http["timeout"] = seconds;
                    }

                    plan.AddExtra(name, http);
                    handlers.Add(http);
                }

                if (transport is "command" or "both")
                {
                    var command = new JsonObject { ["type"] = "command" };
                    switch (plan.CommandForm)
                    {
                        case "bash":
                            command["shell"] = "bash";
                            command["command"] = $"'{probeExe.Replace('\\', '/')}' relay {listenerPort} {name}";
                            break;
                        case "powershell":
                            command["shell"] = "powershell";
                            command["command"] = $"& '{probeExe}' relay {listenerPort} {name}";
                            break;
                        case "shell":
                            command["command"] = $"\"{probeExe}\" relay {listenerPort} {name}";
                            break;
                        default:
                            command["command"] = probeExe;
                            command["args"] = new JsonArray("relay", listenerPort.ToString(), name);
                            break;
                    }

                    if (plan.TimeoutSeconds.TryGetValue(name, out var seconds))
                    {
                        command["timeout"] = seconds;
                    }

                    if (plan.Async.Contains(name))
                    {
                        command["async"] = true;
                    }

                    plan.AddExtra(name, command);
                    handlers.Add(command);
                }

                var group = new JsonObject();
                if (plan.Matchers.TryGetValue(name, out var matcher))
                {
                    group["matcher"] = matcher;
                }

                group["hooks"] = handlers;
                hooks[name] = new JsonArray(group);
            }

            settings["hooks"] = hooks;
        }

        foreach (var pair in config.ExtraSettings ?? new JsonObject())
        {
            settings[pair.Key] = pair.Value?.DeepClone();
        }

        return settings;
    }
}

/// <summary>The names a script's "keys" step takes: axClaude's own table (ClaudeCodeKeys) and a few more.</summary>
internal static class Keys
{
    public static Dictionary<string, string> Table()
    {
        var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in typeof(ClaudeCodeKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
            {
                table[field.Name] = (string)field.GetRawConstantValue()!;
            }
        }

        table["Space"] = " ";
        table["Backspace"] = "\x7f";
        table["Delete"] = "\x1b[3~";
        table["Home"] = "\x1b[H";
        table["End"] = "\x1b[F";
        table["PageUp"] = "\x1b[5~";
        table["PageDown"] = "\x1b[6~";
        return table;
    }

    /// <summary>The bytes for a key name, a single character, or Ctrl+letter; null when the name is unknown.</summary>
    public static string? Bytes(string name)
    {
        if (Table().TryGetValue(name, out var bytes))
        {
            return bytes;
        }

        if (name.StartsWith("Ctrl+", StringComparison.OrdinalIgnoreCase) && name.Length == 6 && char.IsAsciiLetter(name[5]))
        {
            return ((char)(char.ToUpperInvariant(name[5]) & 0x1f)).ToString();
        }

        return name.Length == 1 || (name.Length == 2 && char.IsSurrogatePair(name, 0)) ? name : null;
    }

    public static string Show(string bytes) => string.Concat(bytes.Select(c => c < ' ' || c == '\x7f' ? $"\\x{(int)c:x2}" : c.ToString()));
}
