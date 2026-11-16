using System.Text;
using System.Text.Json.Nodes;
using AxClaude.Core.Hooks;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's hooks as axClaude registers and reads them (docs/axClaude/events-and-grounding.md EG-2.1, EG-2.2,
/// EG-2.4, EG-2.5, EG-2.9, section 6): the table, the settings file, the version gate and the events. The bodies below
/// are written by hand in the shape Claude Code 2.1.325 sends (M13 9, the trial of WP-A3), with invented texts.
/// </summary>
public class ClaudeCodeHooksTests
{
    private const string Program = @"C:\Tools\AxKit\AxKit.exe";
    private const string Verb = "hook";

    private static HookEvent? Parse(string body) => ClaudeCodeHooks.Parse(Encoding.UTF8.GetBytes(body));

    [Fact]
    public void The_table_names_the_eight_events_each_with_the_version_it_was_verified_with()
    {
        Assert.Equal(
            ["SessionStart", "Stop", "StopFailure", "SubagentStart", "SubagentStop", "PermissionRequest", "Notification", "SessionEnd"],
            ClaudeCodeHooks.Events.Select(spec => spec.Name));
        Assert.All(ClaudeCodeHooks.Events, spec => Assert.Matches(@"^[0-9]+\.[0-9]+\.[0-9]+$", spec.VerifiedWith));
        Assert.Equal("2.1.325", ClaudeCodeHooks.TableVersion);

        // The trial of WP-A3 (events-and-grounding.md 4.3): an async SessionEnd never arrived, since Claude Code ended
        // first; every other event is not waited for.
        Assert.False(ClaudeCodeHooks.Events.Single(spec => spec.Name == "SessionEnd").Async);
        Assert.All(ClaudeCodeHooks.Events.Where(spec => spec.Name != "SessionEnd"), spec => Assert.True(spec.Async, spec.Name));

        // Every event registered is read; none is registered that would print rows into the conversation.
        foreach (var spec in ClaudeCodeHooks.Events)
        {
            Assert.NotNull(Parse($$"""{"hook_event_name":"{{spec.Name}}","session_id":"s-1"}"""));
        }

        Assert.DoesNotContain(ClaudeCodeHooks.Events, spec => spec.Name is "PreCompact" or "PostCompact" or "PreToolUse" or "UserPromptSubmit" or "MessageDisplay");
    }

    [Fact]
    public void The_settings_text_has_one_exec_command_hook_for_each_event_and_nothing_else()
    {
        var text = ClaudeCodeHooks.SettingsText(Program, Verb);
        var root = JsonNode.Parse(text)!.AsObject();

        Assert.Equal(["hooks"], root.Select(pair => pair.Key));
        var hooks = root["hooks"]!.AsObject();
        Assert.Equal(ClaudeCodeHooks.Events.Select(spec => spec.Name), hooks.Select(pair => pair.Key));
        foreach (var spec in ClaudeCodeHooks.Events)
        {
            // One group, with no matcher: the group holds its hooks and nothing else.
            var group = Assert.Single(hooks[spec.Name]!.AsArray())!.AsObject();
            Assert.Equal(["hooks"], group.Select(pair => pair.Key));

            var handler = Assert.Single(group["hooks"]!.AsArray())!.AsObject();
            var keys = handler.Select(pair => pair.Key).ToList();
            List<string> expected = spec.Async ? ["type", "command", "args", "async", "timeout"] : ["type", "command", "args", "timeout"];
            Assert.Equal(expected, keys);
            Assert.DoesNotContain("shell", keys);
            Assert.Equal("command", handler["type"]!.GetValue<string>());
            Assert.Equal(Program, handler["command"]!.GetValue<string>());
            Assert.Equal([Verb], handler["args"]!.AsArray().Select(node => node!.GetValue<string>()));
            Assert.Equal(5, handler["timeout"]!.GetValue<int>());
            if (spec.Async)
            {
                Assert.True(handler["async"]!.GetValue<bool>());
            }
        }

        // Indented, LF only, one line end at the end.
        Assert.Contains("\n  \"hooks\": {", text);
        Assert.DoesNotContain("\r", text);
        Assert.EndsWith("}\n", text);
    }

    [Theory]
    [InlineData(@"C:\Program Files\ax Kit\AxKit.exe")]
    [InlineData(@"D:\Werkzeuge\Übung (2)\AxKit.exe")]
    [InlineData(@"\\server\share\ax+kit & co\AxKit.exe")]
    public void A_program_path_with_spaces_and_backslashes_survives_the_json(string program)
    {
        var text = ClaudeCodeHooks.SettingsText(program, Verb);

        var hooks = JsonNode.Parse(text)!["hooks"]!.AsObject();
        Assert.All(hooks, pair => Assert.Equal(program, pair.Value![0]!["hooks"]![0]!["command"]!.GetValue<string>()));
        // Backslashes are JSON's own escape, every other character is written as it is.
        Assert.Contains(program.Replace(@"\", @"\\"), text);
    }

    [Fact]
    public void WriteSettings_writes_only_when_the_text_differs()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, "AxKit", "claude-hooks.json");
        try
        {
            // Missing, folder and all: written.
            Assert.True(ClaudeCodeHooks.WriteSettings(file, Program, Verb));
            Assert.Equal(ClaudeCodeHooks.SettingsText(Program, Verb), File.ReadAllText(file));
            // UTF-8 without a mark.
            Assert.Equal((byte)'{', File.ReadAllBytes(file)[0]);

            // The same text: not written again.
            var written = File.GetLastWriteTimeUtc(file);
            Assert.False(ClaudeCodeHooks.WriteSettings(file, Program, Verb));
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));

            // Another program file (another build, a copy that moved): written for this one.
            Assert.True(ClaudeCodeHooks.WriteSettings(file, @"E:\Other\AxKit.exe", Verb));
            Assert.Contains(@"E:\\Other\\AxKit.exe", File.ReadAllText(file));
            Assert.True(ClaudeCodeHooks.WriteSettings(file, Program, Verb));

            // A file changed by hand, a key added or the formatting changed: written back.
            File.WriteAllText(file, ClaudeCodeHooks.SettingsText(Program, Verb).Replace("\n", "\r\n"));
            Assert.True(ClaudeCodeHooks.WriteSettings(file, Program, Verb));
            Assert.Equal(ClaudeCodeHooks.SettingsText(Program, Verb), File.ReadAllText(file));

            // Nothing is left beside it.
            Assert.Equal([file], Directory.GetFiles(Path.GetDirectoryName(file)!));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("2.1.325 (Claude Code)", true)]
    [InlineData("2.1.326 (Claude Code)", true)]
    [InlineData("2.2.0", true)]
    [InlineData("3.0.1 (Claude Code)", true)]
    [InlineData("2.10.0 (Claude Code)", true)]
    [InlineData("2.1.324 (Claude Code)", false)]
    [InlineData("2.0.999 (Claude Code)", false)]
    [InlineData("1.9.400", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0.0.0 (stand-in)", false)]
    [InlineData("Claude Code", false)]
    [InlineData("2.1 (Claude Code)", false)]
    [InlineData("99999999999.1.1", false)]
    public void Supports_needs_at_least_the_tables_version(string? versionLine, bool expected)
    {
        Assert.Equal(expected, ClaudeCodeHooks.Supports(versionLine));
    }

    [Fact]
    public void The_users_own_settings_argument_is_seen()
    {
        Assert.True(ClaudeCodeHooks.UserPassesSettings(["--settings", "x.json"]));
        Assert.True(ClaudeCodeHooks.UserPassesSettings(["--model", "haiku", "--settings=x.json"]));
        Assert.True(ClaudeCodeHooks.UserPassesSettings(["--settings={\"a\":1}"]));

        Assert.False(ClaudeCodeHooks.UserPassesSettings([]));
        Assert.False(ClaudeCodeHooks.UserPassesSettings(["--continue"]));
        // Another option of Claude Code's that only starts the same way.
        Assert.False(ClaudeCodeHooks.UserPassesSettings(["--setting-sources", "user"]));
        Assert.False(ClaudeCodeHooks.UserPassesSettings(["--settingsfile"]));

        Assert.Equal(["--settings", @"D:\a b\claude-hooks.json"], ClaudeCodeHooks.SessionArguments(@"D:\a b\claude-hooks.json"));
    }

    [Fact]
    public void A_stop_event_gives_the_last_message_and_the_listed_tasks()
    {
        const string Body = """
            {"session_id":"7aac8697-1111-4222-8333-444444444444","transcript_path":"%USERPROFILE%\\.claude\\projects\\C--Temp-axclaude-fixture\\7aac8697-1111-4222-8333-444444444444.jsonl","cwd":"C:\\Temp\\axclaude-fixture","hook_event_name":"Stop","prompt_id":"5d0c1f6e-aaaa-4bbb-8ccc-dddddddddddd","permission_mode":"auto","effort":{"level":"medium"},"stop_hook_active":false,"last_assistant_message":"# Done\n\nThe list has two lines.","background_tasks":[{"id":"b0jdigiy3","type":"shell","status":"running","description":"Run ping in background","command":"ping -n 8 127.0.0.1"},{"id":"a0123456789abcdef","type":"subagent","status":"running","description":"Look for the word","agent_type":"general-purpose"}],"session_crons":[]}
            """;

        var e = Parse(Body);

        Assert.NotNull(e);
        Assert.Equal(HookKind.TurnStopped, e.Kind);
        Assert.Equal("7aac8697-1111-4222-8333-444444444444", e.SessionId);
        Assert.Equal("5d0c1f6e-aaaa-4bbb-8ccc-dddddddddddd", e.TurnKey);
        Assert.Equal("# Done\n\nThe list has two lines.", e.LastReply);
        Assert.Equal(
            [
                new ListedTask("b0jdigiy3", "shell", "running", "Run ping in background", null),
                new ListedTask("a0123456789abcdef", "subagent", "running", "Look for the word", "general-purpose"),
            ],
            e.Tasks!);
        // A Stop's own fields only: its transcript_path is not kept (SessionStart names the file).
        Assert.Null(e.RecordPath);
        Assert.Null(e.Source);
        Assert.Null(e.WorkerId);
        Assert.Null(e.Tool);
        Assert.Null(e.Reason);

        // An empty list is none running; no list at all is not known.
        Assert.Empty(Parse("""{"hook_event_name":"Stop","session_id":"s","background_tasks":[]}""")!.Tasks!);
        Assert.Null(Parse("""{"hook_event_name":"Stop","session_id":"s"}""")!.Tasks);
        Assert.Null(Parse("""{"hook_event_name":"Stop","session_id":"s","background_tasks":{"id":"x"}}""")!.Tasks);
        // An entry that is no object is passed over; one with missing texts keeps them empty.
        Assert.Equal(
            [new ListedTask("t1", string.Empty, string.Empty, string.Empty, null)],
            Parse("""{"hook_event_name":"Stop","session_id":"s","background_tasks":[3,"x",null,{"id":"t1","agent_type":""}]}""")!.Tasks!);
    }

    [Fact]
    public void A_session_start_gives_the_id_the_file_and_the_source()
    {
        const string Body = """
            {"session_id":"3f0b2c1d-aaaa-4bbb-8ccc-0123456789ab","transcript_path":"%USERPROFILE%\\.claude\\projects\\C--Temp-axclaude-fixture\\3f0b2c1d-aaaa-4bbb-8ccc-0123456789ab.jsonl","cwd":"C:\\Temp\\axclaude-fixture","scratchpad_dir":"C:\\Temp\\scratch","hook_event_name":"SessionStart","source":"startup","model":"claude-haiku-5-5"}
            """;

        var e = Parse(Body);

        Assert.NotNull(e);
        Assert.Equal(HookKind.SessionStarted, e.Kind);
        Assert.Equal("3f0b2c1d-aaaa-4bbb-8ccc-0123456789ab", e.SessionId);
        // SessionStart carries no prompt id.
        Assert.Null(e.TurnKey);
        Assert.Equal(@"%USERPROFILE%\.claude\projects\C--Temp-axclaude-fixture\3f0b2c1d-aaaa-4bbb-8ccc-0123456789ab.jsonl", e.RecordPath);
        Assert.Equal(ClaudeCodeHooks.SourceStartup, e.Source);
        Assert.Null(e.LastReply);
        Assert.Null(e.Tasks);

        Assert.Equal(ClaudeCodeHooks.SourceClear, Parse("""{"source":"clear","session_id":"n","hook_event_name":"SessionStart"}""")!.Source);
    }

    [Fact]
    public void Subagent_events_give_the_workers_id_and_type()
    {
        var started = Parse("""{"session_id":"s","hook_event_name":"SubagentStart","prompt_id":"p","agent_id":"a0123456789abcdef","agent_type":"general-purpose"}""");
        Assert.Equal(new HookEvent(HookKind.WorkerStarted, "s", "p", WorkerId: "a0123456789abcdef", WorkerType: "general-purpose"), started);

        var stopped = Parse("""{"session_id":"s","hook_event_name":"SubagentStop","prompt_id":"p","agent_id":"a0123456789abcdef","agent_type":"general-purpose","agent_transcript_path":"C:\\Temp\\x.jsonl","stop_hook_active":false,"background_tasks":[{"id":"b1"}],"last_assistant_message":"agent words"}""");
        Assert.Equal(new HookEvent(HookKind.WorkerStopped, "s", "p", WorkerId: "a0123456789abcdef", WorkerType: "general-purpose"), stopped);

        // A helper of Claude Code's own (a side question, a compaction) has an empty type: no worker of the user's (EG-2.9).
        var helper = Parse("""{"session_id":"s","hook_event_name":"SubagentStop","agent_id":"c1","agent_type":""}""");
        Assert.NotNull(helper);
        Assert.Equal(HookKind.WorkerStopped, helper.Kind);
        Assert.Null(helper.WorkerType);
    }

    [Fact]
    public void Permission_notification_and_session_end_give_their_one_field()
    {
        var permission = Parse("""{"session_id":"s","hook_event_name":"PermissionRequest","prompt_id":"p","permission_mode":"auto","tool_name":"AskUserQuestion","tool_input":{"questions":[{"question":"Which one?"}]}}""");
        Assert.Equal(new HookEvent(HookKind.PermissionAsked, "s", "p", Tool: "AskUserQuestion"), permission);

        var notification = Parse("""{"session_id":"s","hook_event_name":"Notification","prompt_id":"p","message":"Claude needs your permission","notification_type":"permission_prompt"}""");
        Assert.Equal(new HookEvent(HookKind.Notified, "s", "p", Reason: "permission_prompt"), notification);

        var end = Parse("""{"session_id":"s","transcript_path":"C:\\Temp\\s.jsonl","hook_event_name":"SessionEnd","prompt_id":"p","reason":"prompt_input_exit"}""");
        Assert.Equal(new HookEvent(HookKind.SessionEnded, "s", "p", Reason: "prompt_input_exit"), end);

        var failed = Parse("""{"session_id":"s","hook_event_name":"StopFailure","prompt_id":"p","error":"rate_limit","last_assistant_message":"cut"}""");
        Assert.Equal(new HookEvent(HookKind.TurnFailed, "s", "p"), failed);
    }

    [Fact]
    public void The_event_name_may_come_anywhere_and_a_mark_in_front_is_no_fault()
    {
        var last = Parse("""{"reason":"clear","session_id":"s","hook_event_name":"SessionEnd"}""");
        Assert.Equal(new HookEvent(HookKind.SessionEnded, "s", null, Reason: "clear"), last);

        var marked = ClaudeCodeHooks.Parse([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("""{"hook_event_name":"Stop","session_id":"s"}""")]);
        Assert.Equal(HookKind.TurnStopped, marked!.Kind);

        // A field of the wrong type is a missing one.
        Assert.Null(Parse("""{"hook_event_name":"Stop","session_id":"s","last_assistant_message":{"text":"x"},"prompt_id":7}""")!.LastReply);
    }

    [Theory]
    [InlineData("""{"hook_event_name":"PreCompact","session_id":"s"}""")]
    [InlineData("""{"hook_event_name":"stop","session_id":"s"}""")]
    [InlineData("""{"hook_event_name":"MadeUpEvent","session_id":"s"}""")]
    [InlineData("""{"session_id":"s"}""")]
    [InlineData("""{"hook_event_name":"Stop"}""")]
    [InlineData("""{"hook_event_name":"Stop","session_id":""}""")]
    [InlineData("""{"hook_event_name":"Stop","session_id":12}""")]
    [InlineData("""{"hook_event_name":["Stop"],"session_id":"s"}""")]
    [InlineData("""{"payload":{"hook_event_name":"Stop","session_id":"s"}}""")]
    [InlineData("""[{"hook_event_name":"Stop","session_id":"s"}]""")]
    [InlineData("""{"hook_event_name":"Stop","session_id":"s"}{"x":1}""")]
    [InlineData("""{"hook_event_name":"Stop","session_id":"s",}""")]
    [InlineData("""{"hook_event_name":"Stop","session_id":"s" """)]
    [InlineData("""// a comment {"hook_event_name":"Stop"}""")]
    [InlineData("\"Stop\"")]
    [InlineData("null")]
    [InlineData("not json at all")]
    [InlineData("")]
    public void An_unknown_event_or_a_body_that_is_no_object_is_null(string body)
    {
        Assert.Null(Parse(body));
    }

    [Fact]
    public void Bytes_that_are_no_UTF8_text_are_null_not_thrown()
    {
        byte[] body = [.. Encoding.UTF8.GetBytes("""{"hook_event_name":"Stop","session_id":"s","last_assistant_message":" """), 0xC3, 0x28, .. Encoding.UTF8.GetBytes("\"}")];

        Assert.Null(ClaudeCodeHooks.Parse(body));
    }

    [Fact]
    public void Fields_axClaude_does_not_use_are_not_kept()
    {
        string[] bodies =
        [
            """{"session_id":"s","hook_event_name":"PermissionRequest","prompt_id":"p","cwd":"MUST-NOT-BE-READ","tool_name":"Bash","tool_input":{"command":"MUST-NOT-BE-READ","description":"MUST-NOT-BE-READ"},"permission_suggestions":["MUST-NOT-BE-READ"]}""",
            """{"session_id":"s","hook_event_name":"Stop","transcript_path":"MUST-NOT-BE-READ","source":"MUST-NOT-BE-READ","tool_name":"MUST-NOT-BE-READ","reason":"MUST-NOT-BE-READ","agent_type":"MUST-NOT-BE-READ","background_tasks":[{"id":"b","type":"shell","status":"running","description":"Run it","command":"MUST-NOT-BE-READ","output":{"text":"MUST-NOT-BE-READ"}}]}""",
            """{"session_id":"s","hook_event_name":"SessionStart","source":"resume","last_assistant_message":"MUST-NOT-BE-READ","background_tasks":[{"description":"MUST-NOT-BE-READ"}],"model":"MUST-NOT-BE-READ"}""",
            """{"session_id":"s","hook_event_name":"SubagentStop","agent_id":"a","agent_type":"Explore","last_assistant_message":"MUST-NOT-BE-READ","agent_transcript_path":"MUST-NOT-BE-READ","tool_name":"MUST-NOT-BE-READ"}""",
            """{"session_id":"s","hook_event_name":"Notification","message":"MUST-NOT-BE-READ","title":"MUST-NOT-BE-READ","notification_type":"idle_prompt","reason":"MUST-NOT-BE-READ"}""",
            """{"session_id":"s","hook_event_name":"SessionEnd","reason":"other","notification_type":"MUST-NOT-BE-READ","transcript_path":"MUST-NOT-BE-READ"}""",
            """{"session_id":"s","hook_event_name":"StopFailure","error":"MUST-NOT-BE-READ","error_details":"MUST-NOT-BE-READ","last_assistant_message":"MUST-NOT-BE-READ"}""",
        ];

        foreach (var body in bodies)
        {
            var e = Parse(body);
            Assert.NotNull(e);
            Assert.All(Texts(e), text => Assert.DoesNotContain("MUST-NOT-BE-READ", text));
        }
    }

    [Fact]
    public void An_events_text_shows_numbers_only_never_what_was_said()
    {
        var e = Parse("""{"session_id":"7aac8697-1111-4222-8333-444444444444","hook_event_name":"Stop","prompt_id":"p","last_assistant_message":"secret words","background_tasks":[{"id":"b","type":"shell","status":"running","description":"private description"}]}""")!;

        Assert.Equal("TurnStopped session=7aac8697 turn=1 last=12 tasks=1", e.ToString());
        Assert.DoesNotContain("private", e.Tasks![0].ToString());
    }

    /// <summary>Every text the event holds, its tasks' included.</summary>
    private static IEnumerable<string> Texts(HookEvent e)
    {
        string?[] own = [e.SessionId, e.TurnKey, e.RecordPath, e.Source, e.LastReply, e.WorkerId, e.WorkerType, e.Tool, e.Reason];
        var tasks = (e.Tasks ?? []).SelectMany(task => new string?[] { task.Id, task.Type, task.Status, task.Description, task.WorkerType });
        return own.Concat(tasks).OfType<string>();
    }
}
