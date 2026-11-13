using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>The AI tools' first stage, its pure parts (docs/axKit/mcp.md MC-6): the protocol, the tools, the proof, the log, the limit.</summary>
public sealed class McpTests
{
    private static McpProtocol Protocol(List<(string Tool, JsonObject Arguments)>? calls = null) =>
        new(() => McpTools.All, (tool, arguments) =>
        {
            calls?.Add((tool, arguments));
            return new McpResult("answer to " + tool);
        });

    private static JsonObject Parse(string? line) => (JsonObject)JsonNode.Parse(line!)!;

    [Fact]
    public void Initialize_gives_the_version_asked_when_known_and_keeps_the_clients_name()
    {
        var protocol = Protocol();
        var answer = Parse(protocol.Handle("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","clientInfo":{"name":"Claude Code","version":"2"}}}"""));
        Assert.Equal("2025-03-26", answer["result"]!["protocolVersion"]!.GetValue<string>());
        Assert.Equal("axkit", answer["result"]!["serverInfo"]!["name"]!.GetValue<string>());
        Assert.Equal("Claude Code", protocol.ClientName);

        var newest = Parse(Protocol().Handle("""{"jsonrpc":"2.0","id":2,"method":"initialize","params":{"protocolVersion":"1999-01-01"}}"""));
        Assert.Equal(McpProtocol.Versions[0], newest["result"]!["protocolVersion"]!.GetValue<string>());
    }

    [Fact]
    public void A_notification_is_never_answered()
    {
        Assert.Null(Protocol().Handle("""{"jsonrpc":"2.0","method":"notifications/initialized"}"""));
    }

    [Fact]
    public void Tools_list_names_every_tool_with_its_schema()
    {
        var answer = Parse(Protocol().Handle("""{"jsonrpc":"2.0","id":3,"method":"tools/list"}"""));
        var names = answer["result"]!["tools"]!.AsArray().Select(tool => tool!["name"]!.GetValue<string>()).ToList();
        Assert.Equal(McpTools.All.Select(tool => tool.Name), names);
        Assert.All(answer["result"]!["tools"]!.AsArray(), tool => Assert.Equal("object", tool!["inputSchema"]!["type"]!.GetValue<string>()));
    }

    [Fact]
    public void Tools_call_hands_the_name_and_arguments_on_and_wraps_the_text()
    {
        var calls = new List<(string, JsonObject)>();
        var answer = Parse(Protocol(calls).Handle("""{"jsonrpc":"2.0","id":"a","method":"tools/call","params":{"name":"axkit_window_state","arguments":{"window":"0123456789abcdef"}}}"""));
        Assert.Equal("a", answer["id"]!.GetValue<string>());
        Assert.Equal("answer to axkit_window_state", answer["result"]!["content"]![0]!["text"]!.GetValue<string>());
        Assert.False(answer["result"]!["isError"]!.GetValue<bool>());
        Assert.Equal("0123456789abcdef", Assert.Single(calls).Item2["window"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("not json", -32700)]
    [InlineData("""{"jsonrpc":"2.0","id":4,"method":"resources/list"}""", -32601)]
    [InlineData("""{"jsonrpc":"2.0","id":5,"method":42}""", -32600)]
    [InlineData("""{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":7}}""", -32602)]
    public void A_bad_message_gets_the_protocols_error(string line, int code)
    {
        Assert.Equal(code, Parse(Protocol().Handle(line))["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public void Open_takes_a_known_app_and_a_local_path_of_its_kind()
    {
        var folder = Path.GetTempPath();
        var file = Path.Combine(folder, "notes.md");
        bool IsFolder(string path) => path == folder;
        bool IsFile(string path) => path == file;

        string[] Arguments(string verb, string? path) => McpTools.OpenArguments(verb, path, IsFolder, IsFile, _ => false).Arguments ?? [];

        Assert.Equal(["down", file], Arguments("down", file));
        Assert.Equal(["file", folder], Arguments("FILE", folder));
        Assert.Equal(["calc"], Arguments("calc", null));
        // A file given to an app that opens folders only: its folder (Dispatch.PathFor).
        Assert.Equal(["mcpclient", Path.GetDirectoryName(file)!], Arguments("mcpclient", file));
        // axTask only on the user's own top folder (MC-D12).
        Assert.Equal(["task"], Arguments("task", null));
    }

    [Theory]
    [InlineData("nosuch", null, "No app")]
    [InlineData("claude", null, "starts a program")]
    [InlineData("terminal", null, "starts a program")]
    [InlineData("down", "notes.md", "full path")]
    [InlineData("down", @"\\server\share\notes.md", "full path")]
    [InlineData("down", @"C:\nothing-here\x.md", "no file or folder")]
    [InlineData("down", "FOLDER", "does not open a folder")]
    [InlineData("calc", "FOLDER", "does not open a folder")]
    [InlineData("down", @"C:\linked", "through a link")]
    [InlineData("task", "FOLDER", "only on your own lists")]
    public void Open_refuses_with_a_reason(string verb, string? path, string reason)
    {
        var folder = Path.GetTempPath();
        path = path == "FOLDER" ? folder : path;
        var (arguments, refusal) = McpTools.OpenArguments(verb, path, p => p == folder, _ => false, p => p.EndsWith("linked", StringComparison.Ordinal));
        Assert.Null(arguments);
        Assert.Contains(reason, refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void The_proof_checks_only_under_the_same_key_and_challenge()
    {
        var key = new byte[32];
        key[0] = 1;
        var other = new byte[32];
        var challenge = McpAccess.NewChallenge();
        var proof = McpAccess.Proof(key, challenge);
        Assert.True(McpAccess.Proves(key, challenge, proof));
        Assert.False(McpAccess.Proves(other, challenge, proof));
        Assert.False(McpAccess.Proves(key, McpAccess.NewChallenge(), proof));
        Assert.False(McpAccess.Proves(key, challenge, null));
    }

    [Fact]
    public void A_paired_client_is_found_by_its_program_without_case()
    {
        var settings = new McpSettings { Clients = [new McpClient(@"C:\Tools\claude.exe", "Claude Code", "2026-10-24 21:00")] };
        Assert.NotNull(McpAccess.Find(settings, @"c:\tools\CLAUDE.EXE"));
        Assert.Null(McpAccess.Find(settings, @"C:\Tools\other.exe"));
    }

    [Fact]
    public void The_log_line_hashes_the_arguments_and_keeps_to_one_line()
    {
        var line = McpAudit.Line(new DateTime(2026, 10, 24, 21, 5, 3), "Claude\nforged line", @"C:\x\claude.exe", "axkit_open", """{"app":"down"}""", "done");
        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain("down\"", line, StringComparison.Ordinal);
        Assert.StartsWith("2026-10-24 21:05:03  Claude forged line (C:\\x\\claude.exe)  axkit_open  args ", line, StringComparison.Ordinal);
        Assert.Equal(McpAudit.Hash("""{"app":"down"}"""), McpAudit.Hash("""{"app":"down"}"""));
        Assert.Equal(16, McpAudit.Hash("x").Length);
    }

    [Fact]
    public void The_log_names_a_command_that_ran_without_a_recent_file_s_path_and_keeps_an_account_path_out_of_a_refusal()
    {
        // docs/axKit/repairs.json R-06.
        Assert.Equal("done: ran Word wrap", McpAudit.Result(McpTools.Run, "Word wrap", failed: false, "ran"));
        Assert.Equal("done: ran 1. notes.md", McpAudit.Result(McpTools.Run, @"1. notes.md (C:\Work\notes.md)", failed: false, "ran"));
        Assert.Equal("done", McpAudit.Result(McpTools.Open, null, failed: false, "opened"));
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var refused = McpAudit.Result(McpTools.Open, null, failed: true, $"There is no file or folder at {profile}\\secret plans.md\nsecond line");
        Assert.StartsWith("refused or failed: There is no file or folder at ", refused, StringComparison.Ordinal);
        Assert.DoesNotContain(profile, refused, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("second line", refused, StringComparison.Ordinal);
    }

    /// <summary>docs/axKit/repairs.json R-29: a client names itself, and a name with a tab and a line break must not seal as one client and read back as two.</summary>
    [Fact]
    public void A_client_s_name_with_a_tab_or_a_line_break_cannot_seal_as_a_second_client()
    {
        var key = new byte[32];
        var one = new McpSettings { Enabled = true, Clients = [new McpClient(@"C:\a\client.exe", "Tool\t2026-11-13 10:00\n" + @"C:\b\other.exe" + "\tOther", "2026-11-13 10:00")] };
        var two = new McpSettings
        {
            Enabled = true,
            Clients = [new McpClient(@"C:\a\client.exe", "Tool", "2026-11-13 10:00"), new McpClient(@"C:\b\other.exe", "Other", "2026-11-13 10:00")],
        };
        Assert.NotEqual(McpAccess.SealOf(key, two), McpAccess.SealOf(key, one));
    }
    [Fact]
    public void A_file_counts_only_with_its_seal()
    {
        var key = new byte[32];
        key[5] = 9;
        var settings = new McpSettings { Enabled = true, Clients = [new McpClient(@"C:\Tools\claude.exe", "Claude Code", "2026-10-24 21:00")] };
        Assert.False(McpAccess.Checks(key, settings));
        settings.Seal = McpAccess.SealOf(key, settings);
        Assert.True(McpAccess.Checks(key, settings));

        // A client added by hand, or the setting turned on by hand, breaks the seal.
        settings.Clients.Add(new McpClient(@"C:\evil.exe", "x", "2026-10-24 21:01"));
        Assert.False(McpAccess.Checks(key, settings));
        var off = new McpSettings { Enabled = false };
        off.Seal = McpAccess.SealOf(key, off);
        off.Enabled = true;
        Assert.False(McpAccess.Checks(key, off));
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\cmd.exe", false)]
    [InlineData(@"C:\Windows\System32\WindowsPowerShell\v1.0\POWERSHELL.EXE", false)]
    [InlineData(@"C:\Python\python.exe", false)]
    [InlineData(@"C:\Program Files\Git\usr\bin\bash.exe", false)]
    [InlineData(@"C:\Windows\System32\wsl.exe", false)]
    [InlineData(@"C:\Program Files\dotnet\dotnet.exe", false)]
    [InlineData(@"D:\Tools\claude\claude.exe", true)]
    [InlineData(@"C:\Program Files\nodejs\node.exe", true)]
    public void Script_hosts_are_never_paired(string program, bool may)
    {
        Assert.Equal(may, McpAccess.MayPair(program));
    }

    [Fact]
    public void Claude_Code_gets_this_AxKit_for_every_folder_an_earlier_entry_removed_first()
    {
        Assert.Equal(["mcp", "remove", "--scope", "user", "axkit"], McpClaudeCode.RemoveArguments(McpClaudeCode.ServerName));
        // The entry of up to 4.0.0, when the bundle was Axit, goes the same way (B-16).
        Assert.Equal(["mcp", "remove", "--scope", "user", "axit"], McpClaudeCode.RemoveArguments(McpClaudeCode.OldServerName));
        Assert.Equal(["mcp", "add", "--scope", "user", "axkit", "--", @"D:\AxKit\AxKit.exe", "mcp"], McpClaudeCode.AddArguments(@"D:\AxKit\AxKit.exe"));
        Assert.Equal("claude mcp add --scope user axkit -- \"D:\\AxKit\\AxKit.exe\" mcp", McpClaudeCode.AddCommandLine(@"D:\AxKit\AxKit.exe"));
    }

    [Fact]
    public void Ten_calls_a_second_and_no_more()
    {
        var rate = new McpRate();
        Assert.All(Enumerable.Range(0, McpRate.PerSecond), i => Assert.True(rate.Allow(i)));
        Assert.False(rate.Allow(500));
        Assert.True(rate.Allow(1000));
    }

    [Fact]
    public void The_password_database_and_the_clipboard_are_never_read()
    {
        Assert.Contains(Dispatch.AxPass, McpTools.NeverRead);
        Assert.Contains(Dispatch.AxClip, McpTools.NeverRead);
    }

    [Fact]
    public void The_typed_data_tools_are_listed_and_never_of_an_app_that_is_never_read()
    {
        // MC-8.1, MC-4.5: each app tool is a tool of the list, of an app of Dispatch.Apps, and never axPass's or axClip's.
        foreach (var (tool, (app, _)) in McpTools.AppTools)
        {
            Assert.Contains(tool, McpTools.All.Select(listed => listed.Name));
            Assert.Contains(app, Dispatch.Apps.Select(known => known.Verb));
            Assert.DoesNotContain(app, McpTools.NeverRead);
        }

        // MC-8.3: only the add changes anything; reading never needs the run switch.
        Assert.Equal([McpTools.TaskAdd], McpTools.AppTools.Where(pair => pair.Value.Changes).Select(pair => pair.Key));
        var add = McpTools.All.Single(listed => listed.Name == McpTools.TaskAdd).InputSchema;
        Assert.Equal(["list", "title"], add["required"]!.AsArray().Select(name => name!.GetValue<string>()));
    }

    [Fact]
    public void Run_is_never_for_the_password_database_or_the_clipboard()
    {
        Assert.Contains("passwords", McpRun.AppRefusal(Dispatch.AxPass), StringComparison.Ordinal);
        Assert.Contains("clipboard", McpRun.AppRefusal(Dispatch.AxClip), StringComparison.Ordinal);
        Assert.NotNull(McpRun.AppRefusal("PASS"));
        Assert.Null(McpRun.AppRefusal(Dispatch.AxDown));
        // Every app run refuses is one that serves no pipe as well: nothing is reachable to run in the first place.
        Assert.All(McpRun.NeverRun, verb => Assert.Contains(verb, McpTools.NeverRead));
    }

    [Fact]
    public void The_run_switch_is_sealed_and_a_file_of_before_still_checks()
    {
        var key = new byte[32];
        key[3] = 7;

        // A file written before the switch existed: its seal was made of "on" and the clients alone.
        var before = new McpSettings { Enabled = true };
        var oldSeal = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(key, System.Text.Encoding.UTF8.GetBytes("on")));
        before.Seal = oldSeal;
        Assert.True(McpAccess.Checks(key, before));
        Assert.False(before.RunCommands);

        // The switch turned on by hand breaks the seal: only axKit's own menu item turns it on.
        before.RunCommands = true;
        Assert.False(McpAccess.Checks(key, before));
        before.Seal = McpAccess.SealOf(key, before);
        Assert.True(McpAccess.Checks(key, before));
        Assert.NotEqual(oldSeal, before.Seal);
    }

    [Fact]
    public void Commands_run_only_while_the_access_and_the_switch_are_both_on()
    {
        Assert.False(McpAccess.RunsCommandsIn(new McpSettings()));
        Assert.False(McpAccess.RunsCommandsIn(new McpSettings { Enabled = true }));
        // The switch left on while the access is off counts for nothing.
        Assert.False(McpAccess.RunsCommandsIn(new McpSettings { Enabled = false, RunCommands = true }));
        Assert.True(McpAccess.RunsCommandsIn(new McpSettings { Enabled = true, RunCommands = true }));
    }

    [Fact]
    public void Turning_the_access_off_turns_the_switch_off_and_on_again_leaves_it_off()
    {
        var settings = new McpSettings { Enabled = true, RunCommands = true };
        McpAccess.Switched(settings, enabled: false);
        Assert.False(settings.Enabled);
        Assert.False(settings.RunCommands);
        McpAccess.Switched(settings, enabled: true);
        Assert.True(settings.Enabled);
        Assert.False(McpAccess.RunsCommandsIn(settings));
    }

    [Fact]
    public void Forget_takes_every_permission_back_the_switch_included()
    {
        var settings = new McpSettings { Enabled = true, RunCommands = true, Clients = [new McpClient(@"C:\Tools\claude.exe", "Claude Code", "2026-11-11 09:00")] };
        McpAccess.Forgotten(settings);
        Assert.Empty(settings.Clients);
        Assert.False(settings.RunCommands);
        // The access itself stays as the user set it: Forget is about the clients and what they were allowed.
        Assert.True(settings.Enabled);
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(true, 2999, true)]
    [InlineData(true, 3000, false)]
    [InlineData(false, 0, false)]
    [InlineData(true, -1, false)]
    public void A_run_waits_while_the_user_works_in_the_window_in_front(bool inFront, long idleMs, bool busy)
    {
        Assert.Equal(busy, McpRun.IsUserBusy(inFront, idleMs));
    }

    private static readonly McpCommandName[] EditorCommands =
    [
        new("&Word wrap", "Options, Text"),
        new("Save &as...", "File"),
        new("&Heading", "Navigate, Next"),
        new("&Heading", "Navigate, Previous"),
        new("Between the text and the details", string.Empty),
    ];

    [Theory]
    [InlineData("Word wrap", null, 0)]
    [InlineData("word WRAP", null, 0)]
    [InlineData("&Word wrap", null, 0)]
    [InlineData("Save as...", null, 1)]
    [InlineData("Save as", null, 1)]
    [InlineData("Heading", "Navigate, Next", 2)]
    [InlineData("heading", "navigate, previous", 3)]
    [InlineData("Between the text and the details", null, 4)]
    public void A_command_is_found_by_its_label_and_where_needed_its_menu(string name, string? menu, int index)
    {
        var (found, refusal) = McpRun.Find(EditorCommands, name, menu);
        Assert.Null(refusal);
        Assert.Equal(index, found);
    }

    [Theory]
    [InlineData("Heading", null, "several commands named Heading: give the menu too, one of: Navigate, Next; Navigate, Previous.")]
    [InlineData("Heading", "File", "no command Heading in the menu File")]
    [InlineData("Delete", null, "no command Delete: axkit_window_commands lists them.")]
    [InlineData("  ", null, "Name the command")]
    [InlineData(null, null, "Name the command")]
    [InlineData("Word", null, "no command Word")]
    public void A_name_that_names_no_command_or_two_is_refused(string? name, string? menu, string reason)
    {
        var (found, refusal) = McpRun.Find(EditorCommands, name, menu);
        Assert.Equal(-1, found);
        Assert.Contains(reason, refusal, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("claude-code", "Claude")]
    [InlineData("Claude Code", "Claude")]
    [InlineData("CLAUDE", "Claude")]
    [InlineData("axAutomate", "axAutomate")]
    [InlineData("Claudette", "Claudette")]
    [InlineData("claude-whatever", "claude-whatever")]
    [InlineData("", "An AI tool")]
    public void The_window_names_who_ran_a_command(string client, string speaker)
    {
        Assert.Equal(speaker, McpRun.Speaker(client));
    }

    [Fact]
    public void What_the_window_says_and_asks_names_the_client_and_the_command()
    {
        Assert.Equal("Claude ran Word wrap", McpRun.Said("claude-code", "&Word wrap"));
        Assert.Equal("Command offer: Claude asks to run New folder", McpRun.AskTitle("Claude Code", "&New folder..."));
        Assert.Equal(40, McpRun.Speaker(new string('x', 80)).Length);
        Assert.Contains("U+202E", McpRun.Speaker("a‮b"), StringComparison.Ordinal);
    }

    [Fact]
    public void The_question_as_a_list_is_an_offer_that_starts_on_the_explanation()
    {
        // The label every offer starts with, who asks, the command with its menus and target, and that an AI asks.
        Assert.Equal("Command offer: it acts outside this window. Claude asks to run this window's command Open (File) on report.pdf. Asked by Claude, an AI: read the explanation before you allow it",
            McpRun.AskName("claude-code", "&Open", "File", "report.pdf"));
        Assert.Equal("Command offer: it acts outside this window. Claude asks to run this window's command Open. Asked by Claude, an AI: read the explanation before you allow it",
            McpRun.AskName("claude-code", "Open", string.Empty, " "));

        // Running is never where the list starts (bundle AX-11.12), and the order is an offered command's.
        Assert.Equal("1. Explain this command", McpRun.Answers[McpRun.Explain]);
        Assert.Equal("2. Yes, run it", McpRun.Answers[McpRun.Yes]);
        Assert.Equal("3. No, do not run it", McpRun.Answers[McpRun.No]);
        Assert.Equal(0, McpRun.Explain);
        Assert.Equal(McpOffer.Answers[McpOffer.Explain], McpRun.Answers[McpRun.Explain]);
        Assert.Equal(McpOffer.Answers[McpOffer.Run], McpRun.Answers[McpRun.Yes]);
        Assert.Equal(McpOffer.Answers[McpOffer.No], McpRun.Answers[McpRun.No]);
        Assert.StartsWith(McpOffer.Label, McpRun.ExplainTitle, StringComparison.Ordinal);

        // The explanation: the caution first, then the command, the target, what an act may do and the answers.
        var text = McpRun.ExplainText("claude-code", "&Open", "File", "report.pdf");
        Assert.StartsWith("# Read this first\n\nClaude, an AI model, asks to run a command of this window", text, StringComparison.Ordinal);
        Assert.Contains(AiCaution.Request, text, StringComparison.Ordinal);
        Assert.Contains("# The command\n\nOpen, in the menu File.", text, StringComparison.Ordinal);
        Assert.Contains("# What it would act on\n\nreport.pdf", text, StringComparison.Ordinal);
        Assert.Contains("asks no more for a minute", text, StringComparison.Ordinal);
        Assert.Contains("Nothing in particular", McpRun.ExplainText("claude-code", "Open", string.Empty, string.Empty), StringComparison.Ordinal);
    }

    [Fact]
    public void The_question_names_what_the_command_would_act_on()
    {
        var text = McpRun.AskText("claude-code", "Open", "File", "report.pdf");
        Assert.StartsWith("Claude asks to run Open (File) on report.pdf in this window.", text, StringComparison.Ordinal);
        Assert.Contains("not at all if what is selected has changed", text, StringComparison.Ordinal);
        Assert.StartsWith("Claude asks to run Open in this window.", McpRun.AskText("claude-code", "&Open", string.Empty, "  "), StringComparison.Ordinal);

        var shown = McpRun.ShownTarget(new string('a', 500));
        Assert.Equal(McpRun.TargetShownLength + 3, shown.Length);
        Assert.EndsWith("...", shown, StringComparison.Ordinal);
        Assert.Equal("a b", McpRun.ShownTarget("a\r\nb"));
        Assert.Contains("U+202E", McpRun.ShownTarget("a‮b"), StringComparison.Ordinal);
    }

    [Fact]
    public void Several_names_are_one_target()
    {
        Assert.Equal(string.Empty, McpRun.Names([]));
        Assert.Equal("report.pdf", McpRun.Names(["report.pdf"]));
        Assert.Equal("a.txt and b.txt", McpRun.Names(["a.txt", "b.txt"]));
        Assert.Equal("a.txt, b.txt and c.txt", McpRun.Names(["a.txt", "b.txt", "c.txt"]));
    }

    [Fact]
    public void A_run_is_refused_when_the_selection_changed_since_the_question()
    {
        Assert.Null(McpRun.TargetRefusal(RunTarget.Of("report.pdf in files"), RunTarget.Of("report.pdf in files")));
        Assert.Equal("the selection changed", McpRun.TargetRefusal(RunTarget.Of("report.pdf in files"), RunTarget.Of("notes.md in files")));
        Assert.Equal("the selection changed", McpRun.TargetRefusal(RunTarget.Of("Report.pdf"), RunTarget.Of("report.pdf")));
        Assert.Null(McpRun.TargetRefusal(RunTarget.None, RunTarget.None));

        // The identity is compared, never the short words: two folders named files, each with its report.pdf.
        var asked = new RunTarget("report.pdf in files", @"D:\a\files\report.pdf");
        Assert.Equal("the selection changed", McpRun.TargetRefusal(asked, new RunTarget("report.pdf in files", @"D:\b\files\report.pdf")));
        Assert.Null(McpRun.TargetRefusal(asked, asked with { Shown = "report.pdf in files, 2 items" }));
    }

    [Fact]
    public void The_run_compares_the_selection_after_the_question_gave_the_focus_back()
    {
        // MC-7.3: the window's default target reads the focused control, so a Yes must give the focus back to the control
        // the question was asked from and only then read the target again; the other order compares the question's own
        // list or button. The comparison is in one place, Yes(); the list's answer selects the control before it calls
        // Yes(), and the notice's button closes the notice (which selects it) before it does.
        var text = File.ReadAllText(Path.Combine(LayoutTests.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Mcp.cs"));
        var ask = text.IndexOf("private void AskToRunMcp(", StringComparison.Ordinal);
        var end = text.IndexOf("protected sealed record OfferedRequest(", ask, StringComparison.Ordinal);
        Assert.True(ask > 0 && end > ask);
        var body = text[ask..end];
        const string Compare = "McpRun.TargetRefusal(target, McpTarget())";
        Assert.Equal(body.IndexOf(Compare, StringComparison.Ordinal), body.LastIndexOf(Compare, StringComparison.Ordinal));
        Assert.True(body.IndexOf("void Yes()", StringComparison.Ordinal) < body.IndexOf(Compare, StringComparison.Ordinal));

        var select = body.IndexOf("asked.Select();", StringComparison.Ordinal);
        Assert.True(select > 0 && body.IndexOf("Yes();", select, StringComparison.Ordinal) > select, "the list's Yes must select the control the question was asked from before it compares the target");
        var close = body.IndexOf("CloseNotice(null);", StringComparison.Ordinal);
        Assert.True(close > select && body.IndexOf("Yes();", close, StringComparison.Ordinal) > close, "the notice's Yes must call CloseNotice before it compares the target");
    }

    [Theory]
    [InlineData(-1, 0, true)]
    [InlineData(1000, 1000, false)]
    [InlineData(1000, 5999, false)]
    [InlineData(1000, 6000, true)]
    public void A_burst_of_runs_in_a_window_behind_chimes_once(long last, long now, bool chimes)
    {
        Assert.Equal(chimes, McpRun.Chimes(last, now));
    }

    [Fact]
    public void The_run_tool_takes_a_window_and_a_command_and_says_it_needs_the_users_word()
    {
        var run = Assert.Single(McpTools.All, tool => tool.Name == McpTools.Run);
        Assert.Equal(["window", "command"], run.InputSchema["required"]!.AsArray().Select(name => name!.GetValue<string>()));
        Assert.NotNull(run.InputSchema["properties"]!["menu"]);
        Assert.Contains("Let AI tools run commands", McpRun.SwitchOff, StringComparison.Ordinal);
        Assert.Contains("axKit menu, AI tools", McpRun.SwitchOff, StringComparison.Ordinal);
    }

    [Fact]
    public void Mcp_is_the_brokers_verb_not_an_app()
    {
        var launch = Dispatch.Decide(["mcp"]);
        Assert.Equal(Dispatch.Mcp, launch.App);
        Assert.Empty(launch.Arguments);
        Assert.DoesNotContain(Dispatch.Apps, app => app.Verb == Dispatch.Mcp);
    }
}
