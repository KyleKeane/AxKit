using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxMCP.Core;

/// <summary>A tool as the server lists it (SPEC.md MP-3.1): its name, a title when it gives one, what it does, and its arguments as a JSON schema.</summary>
public sealed record ServerTool(string Name, string Title, string Description, JsonObject InputSchema);

/// <summary>A resource, or a resource template, as the server lists it (MP-4.1).</summary>
public sealed record ServerResource(string Name, string Uri, string Description, string? MimeType, long? Size, bool IsTemplate);

/// <summary>A prompt's one argument (MP-5.2).</summary>
public sealed record PromptArgument(string Name, string Description, bool Required);

/// <summary>A prompt as the server lists it (MP-5.1).</summary>
public sealed record ServerPrompt(string Name, string Title, string Description, IReadOnlyList<PromptArgument> Arguments);

/// <summary>What <c>initialize</c> gave back: the server's name and version, and which of the three lists it has.</summary>
public sealed record ServerInfo(string Name, string Version, bool HasTools, bool HasResources, bool HasPrompts);

/// <summary>A JSON-RPC error answer, as the exception a request throws.</summary>
public sealed class McpError(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

/// <summary>
/// The Model Context Protocol as a client speaks it (SPEC.md MP-2, MP-3): JSON-RPC 2.0, one message per line, over
/// any reader and writer, so that the tests drive it with a fake server in memory and the window with a process's
/// pipes (<see cref="StdioServer"/>). The reader runs on a thread of its own; each request is a task completed by its
/// answer, so a window awaits it on its own thread (AX-7.5). Requests from the server (sampling, elicitation, roots)
/// are answered with "method not found" (MP-7); its notifications are logged and dropped. The log takes method names
/// and ids, never arguments or results (MP-10).
/// </summary>
public sealed class McpClient : IDisposable
{
    public const string ProtocolVersion = "2025-06-18";

    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly Action<string> _log;
    private readonly object _writeLock = new();
    private readonly Dictionary<int, TaskCompletionSource<JsonObject>> _waiting = new();
    private int _nextId;
    private bool _closed;

    public McpClient(TextReader input, TextWriter output, Action<string>? log = null)
    {
        _input = input;
        _output = output;
        _log = log ?? (_ => { });
    }

    /// <summary>The server's standard output ended: every request still waiting fails.</summary>
    public event Action? Closed;

    /// <summary>Starts the reader thread; called once.</summary>
    public void Start()
    {
        var reader = new Thread(ReadLoop) { IsBackground = true, Name = "axMCP reader" };
        reader.Start();
    }

    /// <summary>The handshake (MP-2.1): <c>initialize</c>, then the <c>initialized</c> notification.</summary>
    public async Task<ServerInfo> InitializeAsync(string clientVersion, CancellationToken cancellation)
    {
        var result = await RequestAsync("initialize", new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject { ["name"] = "axMCP", ["version"] = clientVersion },
        }, cancellation).ConfigureAwait(false);
        Notify("notifications/initialized", null);
        var capabilities = result["capabilities"] as JsonObject;
        var info = result["serverInfo"] as JsonObject;
        return new ServerInfo(
            Text(info?["name"]) ?? "the server",
            Text(info?["version"]) ?? string.Empty,
            capabilities?["tools"] is JsonObject,
            capabilities?["resources"] is JsonObject,
            capabilities?["prompts"] is JsonObject);
    }

    public async Task<IReadOnlyList<ServerTool>> ListToolsAsync(CancellationToken cancellation)
    {
        var tools = new List<ServerTool>();
        await foreach (var item in PagesAsync("tools/list", "tools", cancellation).ConfigureAwait(false))
        {
            if (item is JsonObject tool && Text(tool["name"]) is { Length: > 0 } name)
            {
                tools.Add(new ServerTool(name, Text(tool["title"]) ?? name, Text(tool["description"]) ?? string.Empty, tool["inputSchema"] as JsonObject ?? new JsonObject()));
            }
        }

        return tools;
    }

    /// <summary>The resources, then the resource templates, each marked (MP-4.1).</summary>
    public async Task<IReadOnlyList<ServerResource>> ListResourcesAsync(CancellationToken cancellation)
    {
        var resources = new List<ServerResource>();
        await foreach (var item in PagesAsync("resources/list", "resources", cancellation).ConfigureAwait(false))
        {
            if (item is JsonObject resource && Text(resource["uri"]) is { Length: > 0 } uri)
            {
                resources.Add(new ServerResource(Text(resource["name"]) ?? uri, uri, Text(resource["description"]) ?? string.Empty, Text(resource["mimeType"]), Number(resource["size"]), IsTemplate: false));
            }
        }

        try
        {
            await foreach (var item in PagesAsync("resources/templates/list", "resourceTemplates", cancellation).ConfigureAwait(false))
            {
                if (item is JsonObject template && Text(template["uriTemplate"]) is { Length: > 0 } uri)
                {
                    resources.Add(new ServerResource(Text(template["name"]) ?? uri, uri, Text(template["description"]) ?? string.Empty, Text(template["mimeType"]), null, IsTemplate: true));
                }
            }
        }
        catch (McpError ex) when (ex.Code == -32601)
        {
            // A server without templates may not know the method at all.
        }

        return resources;
    }

    public async Task<IReadOnlyList<ServerPrompt>> ListPromptsAsync(CancellationToken cancellation)
    {
        var prompts = new List<ServerPrompt>();
        await foreach (var item in PagesAsync("prompts/list", "prompts", cancellation).ConfigureAwait(false))
        {
            if (item is JsonObject prompt && Text(prompt["name"]) is { Length: > 0 } name)
            {
                var arguments = new List<PromptArgument>();
                if (prompt["arguments"] is JsonArray given)
                {
                    foreach (var argument in given.OfType<JsonObject>())
                    {
                        if (Text(argument["name"]) is { Length: > 0 } argumentName)
                        {
                            arguments.Add(new PromptArgument(argumentName, Text(argument["description"]) ?? string.Empty, argument["required"] is JsonValue flag && flag.TryGetValue<bool>(out var required) && required));
                        }
                    }
                }

                prompts.Add(new ServerPrompt(name, Text(prompt["title"]) ?? name, Text(prompt["description"]) ?? string.Empty, arguments));
            }
        }

        return prompts;
    }

    /// <summary>MP-3.4: the whole answer, for <see cref="ResultText"/> and for Ctrl+J.</summary>
    public Task<JsonObject> CallToolAsync(string name, JsonObject arguments, CancellationToken cancellation) =>
        RequestAsync("tools/call", new JsonObject { ["name"] = name, ["arguments"] = arguments }, cancellation);

    public Task<JsonObject> ReadResourceAsync(string uri, CancellationToken cancellation) =>
        RequestAsync("resources/read", new JsonObject { ["uri"] = uri }, cancellation);

    public Task<JsonObject> GetPromptAsync(string name, JsonObject arguments, CancellationToken cancellation) =>
        RequestAsync("prompts/get", new JsonObject { ["name"] = name, ["arguments"] = arguments }, cancellation);

    /// <summary>
    /// One request, answered by the result object of the same id; a JSON-RPC error answer throws <see cref="McpError"/>.
    /// A cancelled wait (the window's 60 s, MP-2.4) sends <c>notifications/cancelled</c> and throws
    /// <see cref="OperationCanceledException"/>; the server's late answer, if any, is dropped.
    /// </summary>
    public async Task<JsonObject> RequestAsync(string method, JsonObject? parameters, CancellationToken cancellation)
    {
        var completion = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        int id;
        lock (_waiting)
        {
            if (_closed)
            {
                throw new McpError(-32000, "The server is not running");
            }

            id = ++_nextId;
            _waiting[id] = completion;
        }

        var request = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method };
        if (parameters is not null)
        {
            request["params"] = parameters;
        }

        _log($"-> {method} ({id})");
        Write(request);
        using var registration = cancellation.Register(() =>
        {
            lock (_waiting)
            {
                _waiting.Remove(id);
            }

            completion.TrySetCanceled(cancellation);
            Notify("notifications/cancelled", new JsonObject { ["requestId"] = id, ["reason"] = "axMCP stopped waiting" });
        });
        return await completion.Task.ConfigureAwait(false);
    }

    /// <summary>A notification: no id, no answer.</summary>
    public void Notify(string method, JsonObject? parameters)
    {
        var message = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method };
        if (parameters is not null)
        {
            message["params"] = parameters;
        }

        Write(message);
    }

    private async IAsyncEnumerable<JsonNode?> PagesAsync(string method, string listKey, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation)
    {
        string? cursor = null;
        for (var page = 0; page < 100; page++)
        {
            var result = await RequestAsync(method, cursor is null ? null : new JsonObject { ["cursor"] = cursor }, cancellation).ConfigureAwait(false);
            if (result[listKey] is JsonArray items)
            {
                foreach (var item in items)
                {
                    yield return item;
                }
            }

            cursor = Text(result["nextCursor"]);
            if (string.IsNullOrEmpty(cursor))
            {
                yield break;
            }
        }
    }

    private void Write(JsonObject message)
    {
        try
        {
            lock (_writeLock)
            {
                _output.Write(message.ToJsonString(Compact));
                _output.Write('\n');
                _output.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            _log("The server's input is closed: " + ex.Message);
            FailAll();
        }
    }

    private void ReadLoop()
    {
        try
        {
            while (_input.ReadLine() is { } line)
            {
                if (line.Length > 0)
                {
                    Handle(line);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            _log("The server's output ended: " + ex.Message);
        }

        FailAll();
        Closed?.Invoke();
    }

    private void Handle(string line)
    {
        JsonObject message;
        try
        {
            message = JsonNode.Parse(line) as JsonObject ?? throw new JsonException("not an object");
        }
        catch (JsonException)
        {
            _log("A line from the server was not JSON and was skipped");
            return;
        }

        var method = Text(message["method"]);
        var id = message["id"];
        if (method is not null)
        {
            if (id is null)
            {
                _log($"<- notification {method}");
                return;
            }

            // A request from the server: nothing of these is offered (SPEC.md MP-7).
            _log($"<- request {method}, answered: not supported");
            Write(new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id.DeepClone(),
                ["error"] = new JsonObject { ["code"] = -32601, ["message"] = $"axMCP does not support {method}" },
            });
            return;
        }

        if (id is not JsonValue value || !value.TryGetValue<int>(out var number))
        {
            _log("An answer without a known id was skipped");
            return;
        }

        TaskCompletionSource<JsonObject>? waiting;
        lock (_waiting)
        {
            _waiting.Remove(number, out waiting);
        }

        if (waiting is null)
        {
            _log($"<- answer {number}, nobody waiting");
            return;
        }

        if (message["error"] is JsonObject error)
        {
            var code = error["code"] is JsonValue codeValue && codeValue.TryGetValue<int>(out var c) ? c : -32000;
            _log($"<- error {number}: {code}");
            waiting.TrySetException(new McpError(code, Text(error["message"]) ?? "The server reported an error"));
            return;
        }

        _log($"<- answer {number}, {line.Length} characters");
        waiting.TrySetResult(message["result"] as JsonObject ?? new JsonObject());
    }

    private void FailAll()
    {
        List<TaskCompletionSource<JsonObject>> waiting;
        lock (_waiting)
        {
            _closed = true;
            waiting = [.. _waiting.Values];
            _waiting.Clear();
        }

        foreach (var completion in waiting)
        {
            completion.TrySetException(new McpError(-32000, "The server ended before it answered"));
        }
    }

    public void Dispose()
    {
        FailAll();
        try
        {
            _output.Dispose();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // Closed by the server already.
        }
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static long? Number(JsonNode? node) => node is JsonValue value && value.TryGetValue<long>(out var number) ? number : null;
}
