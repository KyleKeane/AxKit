using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AxClaude.Core.Hooks;

namespace AxClaude.Core.Streaming;

/// <summary>
/// The verb <c>AxKit.exe ask</c> (docs/axClaude/stream-mode.md SM-7.3): the program a Claude Code run over its
/// stream starts as its permission tool, and asks before a tool call that needs the user's word and for Claude's own
/// questions. It is a server of one tool over its standard input and output (MCP's JSON-RPC, one object a line),
/// and it holds no logic: each question goes, as it came, to the axClaude window that started this Claude Code, over
/// the window's own pipe, named with its secret in the environment the window gave (<see cref="Variable"/>), and
/// the window's answer goes back as it came. Where the window cannot be asked, or answers what is no answer, the
/// answer is no (<see cref="NoWindowWords"/>): nothing runs without the user's word because a pipe broke.
/// It waits for the user as long as the user takes; Claude Code ends it with the session.
/// <c>Program.Main</c> decides this verb before anything else runs, so that the process loads nothing of the windows.
/// Neutral towards the question: what a question and an answer hold is <see cref="ClaudeCodeStream"/>'s and the
/// window's. No window starts it yet.
/// </summary>
public static class AskRelay
{
    /// <summary>The verb on the command line: <c>AxKit.exe ask</c>.</summary>
    public const string Verb = "ask";

    /// <summary>The server's name in the configuration Claude Code is given, and the one tool it has.</summary>
    public const string Server = "axask";
    public const string Tool = "approve";

    /// <summary>The tool as Claude Code names it, for <see cref="ClaudeCodeStream.PermissionToolOption"/>.</summary>
    public const string ToolName = "mcp__" + Server + "__" + Tool;

    /// <summary>The environment variable that carries the window's pipe and its secret, <c>&lt;name&gt;;&lt;secret&gt;</c>, as the hooks' variable does for their pipe.</summary>
    public const string Variable = "AXKIT_ASK_PIPE";

    /// <summary>Every pipe of the questions starts so; a name that does not is never connected to.</summary>
    public const string PipePrefix = "AxKit-ask-";

    /// <summary>How long the connection to the window's pipe may take. The answer itself has no limit: it is the user's.</summary>
    public const int ConnectMs = 2000;

    /// <summary>What Claude is told when the window could not be asked: no call runs for want of an answer.</summary>
    public const string NoWindowWords = "The window that asks the user could not be reached, so this was not allowed.";

    /// <summary>The verb's whole run: Claude Code's requests on standard input, the answers on standard output, until the input ends.</summary>
    public static void Run()
    {
        try
        {
            // Outside call, pipe, Claude Code questions: the standard input of "AxKit.exe ask", on which the Claude
            // Code that started it as its permission tool writes each question (MCP's JSON-RPC); the answers go to
            // its standard output.
            using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            var variable = Environment.GetEnvironmentVariable(Variable);
            Serve(input, output, question => Ask(question, variable, Connect));
        }
        catch (Exception)
        {
            // Nothing is printed beside the protocol's own lines: Claude Code reads this process's output as answers.
        }
    }

    /// <summary>
    /// The server's loop: one request a line, one answer a line for every request that has an id. A question
    /// (<c>tools/call</c>) is handed to <paramref name="ask"/> as the bytes of its arguments, and what comes back is
    /// the tool's result when it is an answer (<see cref="IsAnswer"/>); null, or anything else, is answered with no.
    /// A line that is no request is passed over. Ends when the input ends.
    /// </summary>
    internal static void Serve(TextReader input, TextWriter output, Func<byte[], byte[]?> ask)
    {
        while (input.ReadLine() is { } line)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("id", out var id))
                {
                    // A notification: nothing to answer.
                    continue;
                }

                var method = root.TryGetProperty("method", out var named) && named.ValueKind == JsonValueKind.String ? named.GetString() : null;
                var idText = id.GetRawText();
                switch (method)
                {
                    case "initialize":
                        var protocol = root.TryGetProperty("params", out var asked) && asked.ValueKind == JsonValueKind.Object && asked.TryGetProperty("protocolVersion", out var wanted) && wanted.ValueKind == JsonValueKind.String
                            ? wanted.GetRawText()
                            : "\"2024-11-05\"";
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"protocolVersion\":{protocol},\"capabilities\":{{\"tools\":{{}}}},\"serverInfo\":{{\"name\":\"{Server}\",\"version\":\"1\"}}}}}}");
                        break;

                    case "tools/list":
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"tools\":[{{\"name\":\"{Tool}\",\"description\":\"Asks the user of axClaude whether a tool call may run, and passes Claude's own questions on.\",\"inputSchema\":{ClaudeCodeStream.PermissionToolInputSchema}}}]}}}}");
                        break;

                    case "tools/call":
                        var answer = Answer(root, ask);
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":{JsonSerializer.Serialize(answer)}}}]}}}}");
                        break;

                    default:
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{}}}}");
                        break;
                }
            }
        }
    }

    /// <summary>The answer to one question: the window's, when the question could be asked and what came back is an answer; else no.</summary>
    private static string Answer(JsonElement call, Func<byte[], byte[]?> ask)
    {
        if (!call.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object
            || !parameters.TryGetProperty("arguments", out var arguments) || arguments.ValueKind != JsonValueKind.Object)
        {
            return ClaudeCodeStream.DenyAnswer(NoWindowWords);
        }

        byte[]? answered;
        try
        {
            answered = ask(Encoding.UTF8.GetBytes(arguments.GetRawText()));
        }
        catch (Exception)
        {
            answered = null;
        }

        if (answered is null)
        {
            return ClaudeCodeStream.DenyAnswer(NoWindowWords);
        }

        var text = Encoding.UTF8.GetString(answered);
        return IsAnswer(text) ? text : ClaudeCodeStream.DenyAnswer(NoWindowWords);
    }

    /// <summary>Whether a text is an answer Claude Code takes: one JSON object whose <c>behavior</c> is allow or deny.</summary>
    public static bool IsAnswer(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("behavior", out var behavior)
                && behavior.ValueKind == JsonValueKind.String
                && behavior.GetString() is "allow" or "deny";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// One question to the window: the pipe named in the variable is connected to, the question written as one frame
    /// with the secret, and the answer's frame read, however long the user takes. Null when the variable names no
    /// pipe of the questions, the window is not there in time, the pipe breaks, or the answer's frame is not the
    /// window's; never an exception.
    /// </summary>
    internal static byte[]? Ask(byte[] question, string? variableValue, Func<string, int, Stream?> connect)
    {
        try
        {
            if (question.Length > HookFrame.MaxBody || !HookFrame.TryParseVariable(variableValue, PipePrefix, out var name, out var secret))
            {
                return null;
            }

            using var pipe = connect(name, ConnectMs);
            if (pipe is null)
            {
                return null;
            }

            HookFrame.Write(pipe, secret, question);
            return HookFrame.Read(pipe, secret, out _);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>The window's pipe by its name, connected within <paramref name="timeoutMs"/>; it throws when the pipe is not there in time.</summary>
    internal static Stream? Connect(string name, int timeoutMs)
    {
        // Outside call, pipe, Claude Code questions: connects to the pipe of the axClaude window that started this
        // Claude Code, named in the environment it gave, writes the question and reads the user's answer; on this
        // computer, never over the network.
        var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut);
        try
        {
            pipe.Connect(timeoutMs);
            return pipe;
        }
        catch (Exception)
        {
            pipe.Dispose();
            throw;
        }
    }

    /// <summary>A new name for the window's pipe of one start of Claude Code, and the variable's value for that start's environment.</summary>
    public static string NewPipeName(string userSid) => HookFrame.NewPipeName(PipePrefix, userSid);

    public static string VariableValue(string pipeName, string secret) => HookFrame.VariableValue(pipeName, secret);

    /// <summary>
    /// The configuration that names this program as the one server, for Claude Code's <c>--mcp-config</c>: the
    /// program's path and the verb, nothing of the pipe or its secret, which travel in the environment alone and are
    /// never written to a file.
    /// </summary>
    public static string ConfigText(string program)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteStartObject("mcpServers");
            writer.WriteStartObject(Server);
            writer.WriteString("command", program);
            writer.WriteStartArray("args");
            writer.WriteStringValue(Verb);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
