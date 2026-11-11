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
        Assert.Equal(["task", Path.GetDirectoryName(file)!], Arguments("task", file));
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
    [InlineData("axAutomate", "axAutomate")]
    [InlineData("", "An AI tool")]
    public void The_window_names_who_ran_a_command(string client, string speaker)
    {
        Assert.Equal(speaker, McpRun.Speaker(client));
    }

    [Fact]
    public void What_the_window_says_and_asks_names_the_client_and_the_command()
    {
        Assert.Equal("Claude ran Word wrap", McpRun.Said("claude-code", "&Word wrap"));
        Assert.Equal("Claude asks to run New folder", McpRun.AskTitle("Claude Code", "&New folder..."));
        Assert.Equal(40, McpRun.Speaker(new string('x', 80)).Length);
        Assert.Contains("U+202E", McpRun.Speaker("a‮b"), StringComparison.Ordinal);
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
