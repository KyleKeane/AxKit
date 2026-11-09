using System.IO.Pipes;
using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core;
using AxMCP.Core;

namespace AxMCP.Tests;

/// <summary>axMCP's pure code (docs/axmcp/SPEC.md): the servers from the three files, a command line split, a schema to fields and back, a result to text, and the client against a fake server in memory.</summary>
public sealed class MCPTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axmcp-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // ---- MP-1.1: the servers ----

    [Fact]
    public void Claude_Codes_user_file_gives_the_user_scope_and_the_folders_local_scope()
    {
        const string text = """
            {
              "mcpServers": {
                "axkit": { "command": "C:\\Programs\\AxKit\\AxKit.exe", "args": ["mcp"] },
                "github": { "type": "http", "url": "https://api.githubcopilot.com/mcp/" },
                "odd": "not an object"
              },
              "projects": {
                "C:\\work\\thing": { "mcpServers": { "local": { "command": "npx", "args": ["-y", "server"], "env": { "TOKEN": "secret" } } } },
                "C:\\other": { "mcpServers": { "elsewhere": { "command": "x" } } }
              }
            }
            """;
        var servers = ServerSources.FromClaudeCode(text, "c:\\work\\thing\\");
        Assert.Equal(["axkit", "github", "local"], servers.Select(server => server.Name));
        Assert.Equal(ServerKind.Stdio, servers[0].Kind);
        Assert.Equal("C:\\Programs\\AxKit\\AxKit.exe mcp", servers[0].CommandLine);
        Assert.Equal(ServerSources.UserScope, servers[0].Source);
        Assert.Equal(ServerKind.Http, servers[1].Kind);
        Assert.Equal("https://api.githubcopilot.com/mcp/", servers[1].Url);
        Assert.Equal(ServerSources.LocalScope, servers[2].Source);
        Assert.Equal(["TOKEN"], servers[2].Env.Keys);
        Assert.Equal("c:\\work\\thing\\", servers[2].Folder);
        Assert.False(servers[2].Own);

        // Without a folder, the user scope alone; a file that is not JSON gives nothing.
        Assert.Equal(2, ServerSources.FromClaudeCode(text, null).Count);
        Assert.Empty(ServerSources.FromClaudeCode("{ broken", null));
        Assert.Empty(ServerSources.FromClaudeCode(null, null));
    }

    [Fact]
    public void A_folders_project_file_gives_the_project_scope_and_the_kinds_are_read()
    {
        var servers = ServerSources.FromProjectFile("""{ "mcpServers": { "a": { "command": "a.exe" }, "b": { "type": "sse", "url": "http://x" }, "c": { "type": "stdio", "command": "c", "cwd": "D:\\c" }, "d": { "type": "weird" } } }""", "C:\\work");
        Assert.Equal(["a", "b", "c", "d"], servers.Select(server => server.Name));
        Assert.All(servers, server => Assert.Equal(ServerSources.ProjectScope, server.Source));
        Assert.Equal(ServerKind.Stdio, servers[0].Kind);
        Assert.Equal("C:\\work", servers[0].Folder);
        Assert.Equal(ServerKind.Sse, servers[1].Kind);
        Assert.Equal("D:\\c", servers[2].Folder);
        Assert.Equal(ServerKind.Unknown, servers[3].Kind);
        Assert.Equal("Claude Code, project scope: a", servers[0].Key);
    }

    [Fact]
    public void The_users_own_servers_come_from_the_settings_with_their_command_lines_split()
    {
        var settings = new MCPSettings();
        settings.Servers.Add(new OwnServer { Name = "fake", CommandLine = "powershell -NoProfile -File \"C:\\my tools\\fake.ps1\"", Folder = "C:\\work" });
        settings.Servers.Add(new OwnServer { Name = "empty", CommandLine = "   " });
        var servers = settings.OwnServers();
        var fake = Assert.Single(servers);
        Assert.Equal("powershell", fake.Command);
        Assert.Equal(["-NoProfile", "-File", "C:\\my tools\\fake.ps1"], fake.Args);
        Assert.Equal(ServerSources.Own, fake.Source);
        Assert.True(fake.Own);
        Assert.Equal("powershell -NoProfile -File \"C:\\my tools\\fake.ps1\"", fake.CommandLine);
    }

    [Fact]
    public void A_missing_settings_file_gives_the_defaults_and_a_read_one_is_cleaned()
    {
        var path = Path.Combine(_folder, "axmcp.json");
        var settings = MCPSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Empty(settings.Servers);

        Directory.CreateDirectory(_folder);
        File.WriteAllText(path, """{ "servers": [ { "name": " a ", "commandLine": "a.exe", "folder": " " }, { "name": "", "commandLine": "b" }, { "name": "c", "commandLine": "" } ] }""");
        settings = MCPSettings.Load(path, out error);
        Assert.Null(error);
        var only = Assert.Single(settings.Servers);
        Assert.Equal("a", only.Name);
        Assert.Null(only.Folder);
    }

    // ---- MP-1.3: a command line ----

    [Theory]
    [InlineData("npx -y server", new[] { "npx", "-y", "server" })]
    [InlineData("  \"C:\\Program Files\\x.exe\"   --flag  ", new[] { "C:\\Program Files\\x.exe", "--flag" })]
    [InlineData("say \\\"hi\\\"", new[] { "say", "\"hi\"" })]
    [InlineData("a \"\" b", new[] { "a", "", "b" })]
    [InlineData("", new string[0])]
    public void A_command_line_splits_as_Windows_reads_it(string line, string[] words)
    {
        Assert.Equal(words, CommandLines.Split(line));
    }

    [Fact]
    public void A_word_with_a_space_is_quoted_back()
    {
        Assert.Equal("plain", CommandLines.Quote("plain"));
        Assert.Equal("\"two words\"", CommandLines.Quote("two words"));
        Assert.Equal("\"\"", CommandLines.Quote(string.Empty));
        Assert.Equal(["two words"], CommandLines.Split(CommandLines.Quote("two words")));
    }

    // ---- MP-3.3: a schema to fields and back ----

    [Fact]
    public void A_tools_schema_becomes_fields_of_the_item_apps_types()
    {
        var tool = new ServerTool("find", "find", "Finds things. In folders.", (JsonObject)JsonNode.Parse("""
            {
              "type": "object",
              "properties": {
                "path": { "type": "string", "description": "Where" },
                "depth": { "type": "integer", "default": 2 },
                "hidden": { "type": "boolean" },
                "kind": { "type": "string", "enum": ["file", "folder"] },
                "since": { "type": "string", "format": "date" },
                "filters": { "type": "array", "items": { "type": "string" } },
                "maybe": { "type": ["string", "null"] }
              },
              "required": ["path", "kind"]
            }
            """)!);
        var document = ArgumentFields.ForTool(tool);
        Assert.Equal([ArgumentFields.DescriptionField, "path (required)", "depth", "hidden", "kind (required)", "since", "filters", "maybe"], document.Fields.Select(field => field.Title));
        Assert.Equal([FieldTypes.LongText, FieldTypes.Text, FieldTypes.Number, FieldTypes.Check, FieldTypes.Choice, FieldTypes.Date, FieldTypes.LongText, FieldTypes.Text], document.Fields.Select(field => field.Type));
        Assert.Equal("Finds things. In folders.", FieldTypes.TextOf(document.Fields[0].Value));
        Assert.Equal(2, document.Fields[2].Value!.GetValue<int>());
        Assert.Equal(["file", "folder"], document.Fields[4].Options);
        Assert.Equal("file", FieldTypes.TextOf(document.Fields[4].Value));
        Assert.Equal("path", ArgumentFields.MissingRequired(document));

        var filled = document.WithValue(1, JsonValue.Create("C:\\work")).WithValue(3, JsonValue.Create(true)).WithValue(6, JsonValue.Create("[\"a\", \"b\"]"));
        Assert.Null(ArgumentFields.MissingRequired(filled));
        var arguments = ArgumentFields.ToArguments(filled);
        Assert.Equal("""{"path":"C:\\work","depth":2,"hidden":true,"kind":"file","filters":["a","b"]}""", arguments.ToJsonString());

        // A long text that is not JSON is the window's to say.
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => ArgumentFields.ToArguments(filled.WithValue(6, JsonValue.Create("not json"))));
    }

    [Fact]
    public void A_schema_without_properties_is_one_raw_field_and_a_prompts_arguments_are_texts()
    {
        var raw = ArgumentFields.ForTool(new ServerTool("any", "any", "", (JsonObject)JsonNode.Parse("""{ "type": "object" }""")!));
        Assert.Equal([ArgumentFields.DescriptionField, ArgumentFields.RawField], raw.Fields.Select(field => field.Title));
        var arguments = ArgumentFields.ToArguments(raw.WithValue(1, JsonValue.Create("""{ "x": 1 }""")));
        Assert.Equal("""{"x":1}""", arguments.ToJsonString());

        var none = ArgumentFields.ForTool(new ServerTool("bare", "bare", "Nothing to give.", new JsonObject()));
        Assert.Equal([ArgumentFields.DescriptionField], none.Fields.Select(field => field.Title));

        var prompt = ArgumentFields.ForPrompt(new ServerPrompt("summarize", "summarize", "Sums up.", [new PromptArgument("text", "The text", true), new PromptArgument("style", "", false)]));
        Assert.Equal([ArgumentFields.DescriptionField, "text (required)", "style"], prompt.Fields.Select(field => field.Title));
        Assert.Equal("text", ArgumentFields.MissingRequired(prompt));
        Assert.Equal("""{"text":"hello"}""", ArgumentFields.ToPromptArguments(prompt.WithValue(1, JsonValue.Create("hello"))).ToJsonString());
    }

    // ---- MP-3.5: a result as text ----

    [Fact]
    public void A_result_reads_as_text_with_the_error_first_and_media_as_one_line()
    {
        var result = (JsonObject)JsonNode.Parse("""
            {
              "isError": true,
              "content": [
                { "type": "text", "text": "line one\nline two" },
                { "type": "image", "mimeType": "image/png", "data": "AAAA" },
                { "type": "resource", "resource": { "uri": "file:///x", "text": "inside" } },
                { "type": "resource", "resource": { "uri": "file:///y", "blob": "AAAA" } }
              ]
            }
            """)!;
        Assert.Equal("The tool reported an error\r\n\r\nline one\r\nline two\r\n\r\n[image, image/png, 3 bytes]\r\n\r\ninside\r\n\r\n[resource, file:///y]", ResultText.OfCall(result));
        Assert.Equal(ResultText.NoContent, ResultText.OfCall(new JsonObject()));
        Assert.Equal("Hello", ResultText.OfResource((JsonObject)JsonNode.Parse("""{ "contents": [ { "uri": "x", "text": "Hello" } ] }""")!));
        Assert.Equal("Sums up.\r\n\r\nuser: Please sum up: hello", ResultText.OfPrompt((JsonObject)JsonNode.Parse("""{ "description": "Sums up.", "messages": [ { "role": "user", "content": { "type": "text", "text": "Please sum up: hello" } } ] }""")!));
    }

    // ---- MP-2, MP-7: the client against a fake server ----

    [Fact]
    public async Task The_client_speaks_the_handshake_pages_a_list_calls_a_tool_and_refuses_a_servers_request()
    {
        using var fake = new FakeServer();
        using var client = new McpClient(fake.ToClient, fake.FromClient);
        client.Start();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var info = await client.InitializeAsync("5.3.0", limit.Token);
        Assert.Equal("fake", info.Name);
        Assert.True(info.HasTools);
        Assert.False(info.HasPrompts);
        Assert.Equal("initialize", fake.Methods[0]);
        Assert.Equal("notifications/initialized", fake.Methods[1]);

        var tools = await client.ListToolsAsync(limit.Token);
        Assert.Equal(["first", "second"], tools.Select(tool => tool.Name));
        Assert.Equal("Says the text back.", tools[0].Description);

        var result = await client.CallToolAsync("first", new JsonObject { ["text"] = "hello" }, limit.Token);
        Assert.Equal("hello", ResultText.OfCall(result));

        var error = await Assert.ThrowsAsync<McpError>(() => client.RequestAsync("tools/call", new JsonObject { ["name"] = "missing" }, limit.Token));
        Assert.Equal(-32602, error.Code);
        Assert.Equal("Unknown tool", error.Message);

        // The server asks the client something: answered with "method not found", and nothing waits on it.
        fake.Ask("sampling/createMessage");
        var answer = await fake.NextFromClientAsync("\"id\":\"server-1\"", limit.Token);
        Assert.Contains("-32601", answer);

        // A wait given up: the cancellation reaches the server and the task is cancelled.
        using var short_ = new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.RequestAsync("slow", null, short_.Token));
        Assert.Contains("\"requestId\"", await fake.NextFromClientAsync("notifications/cancelled", limit.Token));
    }

    [Fact]
    public async Task A_server_that_ends_fails_what_waits_on_it()
    {
        var fake = new FakeServer();
        using var client = new McpClient(fake.ToClient, fake.FromClient);
        client.Start();
        var closed = new TaskCompletionSource();
        client.Closed += () => closed.TrySetResult();
        var pending = client.RequestAsync("slow", null, CancellationToken.None);
        fake.Dispose();
        var error = await Assert.ThrowsAsync<McpError>(() => pending);
        Assert.Contains("ended", error.Message);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAsync<McpError>(() => client.RequestAsync("ping", null, CancellationToken.None));
    }

    /// <summary>A server in memory over two anonymous pipes: it answers the handshake, a paged tools list, one tool and an unknown tool, keeps "slow" unanswered, and can ask the client something.</summary>
    private sealed class FakeServer : IDisposable
    {
        private readonly AnonymousPipeServerStream _toClientWrite = new(PipeDirection.Out);
        private readonly AnonymousPipeClientStream _toClientRead;
        private readonly AnonymousPipeServerStream _fromClientWrite = new(PipeDirection.Out);
        private readonly AnonymousPipeClientStream _fromClientRead;
        private readonly StreamWriter _out;
        private readonly StreamReader _in;
        private readonly Thread _loop;
        private readonly System.Collections.Concurrent.BlockingCollection<string> _unmatched = [];
        private int _asked;

        public FakeServer()
        {
            _toClientRead = new AnonymousPipeClientStream(PipeDirection.In, _toClientWrite.ClientSafePipeHandle);
            _fromClientRead = new AnonymousPipeClientStream(PipeDirection.In, _fromClientWrite.ClientSafePipeHandle);
            _out = new StreamWriter(_toClientWrite, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            _in = new StreamReader(_fromClientRead, new UTF8Encoding(false));
            ToClient = new StreamReader(_toClientRead, new UTF8Encoding(false));
            FromClient = new StreamWriter(_fromClientWrite, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            _loop = new Thread(Loop) { IsBackground = true };
            _loop.Start();
        }

        /// <summary>What the client reads.</summary>
        public TextReader ToClient { get; }

        /// <summary>What the client writes.</summary>
        public TextWriter FromClient { get; }

        public List<string> Methods { get; } = [];

        /// <summary>A request from the server to the client.</summary>
        public void Ask(string method) => _out.WriteLine("{\"jsonrpc\":\"2.0\",\"id\":\"server-" + (++_asked) + "\",\"method\":\"" + method + "\",\"params\":{}}");

        /// <summary>The next line the client sent that the server did not answer (an answer to its own request, a notification) and that holds <paramref name="text"/>; the others before it are dropped.</summary>
        public async Task<string> NextFromClientAsync(string text, CancellationToken cancellation)
        {
            while (true)
            {
                if (_unmatched.TryTake(out var line, 100) && line.Contains(text, StringComparison.Ordinal))
                {
                    return line;
                }

                cancellation.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private void Loop()
        {
            try
            {
                while (_in.ReadLine() is { } line)
                {
                    var message = JsonNode.Parse(line) as JsonObject;
                    var method = message?["method"]?.GetValue<string>();
                    if (method is null)
                    {
                        _unmatched.Add(line);
                        continue;
                    }

                    lock (Methods)
                    {
                        Methods.Add(method);
                    }

                    if (message!["id"] is not { } id)
                    {
                        _unmatched.Add(line);
                        continue;
                    }

                    var parameters = message["params"] as JsonObject;
                    string? result = method switch
                    {
                        "initialize" => """{"protocolVersion":"2025-06-18","capabilities":{"tools":{"listChanged":false},"resources":{}},"serverInfo":{"name":"fake","version":"1.0"}}""",
                        "tools/list" when parameters?["cursor"] is null => """{"tools":[{"name":"first","description":"Says the text back.","inputSchema":{"type":"object","properties":{"text":{"type":"string"}},"required":["text"]}}],"nextCursor":"page2"}""",
                        "tools/list" => """{"tools":[{"name":"second","title":"Second","description":"","inputSchema":{"type":"object"}}]}""",
                        "tools/call" when parameters?["name"]?.GetValue<string>() == "first" => "{\"content\":[{\"type\":\"text\",\"text\":\"" + parameters?["arguments"]?["text"]?.GetValue<string>() + "\"}]}",
                        "tools/call" => null,
                        "slow" => "wait",
                        _ => "{\"error\":{\"code\":-32601,\"message\":\"Method not found: " + method + "\"}}",
                    };
                    if (result == "wait")
                    {
                        continue;
                    }

                    var head = "{\"jsonrpc\":\"2.0\",\"id\":" + id.ToJsonString() + ",";
                    _out.WriteLine(result is null
                        ? head + "\"error\":{\"code\":-32602,\"message\":\"Unknown tool\"}}"
                        : result.StartsWith("{\"error\"", StringComparison.Ordinal)
                            ? head + result[1..]
                            : head + "\"result\":" + result + "}");
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // Closed.
            }
        }

        public void Dispose()
        {
            _out.Dispose();
            _toClientWrite.Dispose();
            _fromClientWrite.Dispose();
            _in.Dispose();
            _fromClientRead.Dispose();
        }
    }
}
