// The stand-in of Claude Code's stream for window scripts (docs/axClaude/stream-mode.md SM-10.2; docs/axKit/automation.md
// section 2, "The stand-in of Claude Code"). A second mode of the stand-in (StandIn.cs), for a window the stream feeds:
// where the first draws frames in a pseudo console, this one is a process of lines, as Claude Code is with
// -p --input-format stream-json --output-format stream-json. A script's claude.cmd is one line:
//
//   @"%AXKIT_STANDIN%" standin --stream <case> %*
//
// It plays a recording of the real stream, one of tests/fixtures/streams (their page says what a file holds): the
// lines Claude Code wrote are written to standard output in their order, and where the recording has something the
// host wrote, the stand-in waits until the window has written that kind of thing to its standard input. So a script
// follows a real turn of Claude Code 2.1.327 without Claude Code, the network or a model, and the window's own
// writing is what moves the play on. It needs nothing of the user's and writes only stream-standin.log in the working
// folder (kinds and numbers, never a text) and, with --session, a session file inside the run's folder.
//
// Usage:
//   AxAutomate standin --stream <case | file.jsonl> [--session <record>] <Claude Code's arguments>
//       --session writes tests/fixtures/records/<record>.jsonl as the session file of the stand-in's conversation,
//       where Claude Code keeps it, before the play: under CLAUDE_CONFIG_DIR, which must lie in the run's folder
//       (the script's claude.cmd sets it), so that a window started there after it takes that conversation up
//       (docs/axClaude/stream-mode.md SM-8.2; script 51). Several records separated by commas are several earlier
//       conversations, the last named the newest (SM-8.5; script 58).
//       <case> is a file's name without its ending under tests/fixtures/streams of the run's repository: the one the
//       runner works in, which it hands to everything it starts in AXKIT_STANDIN_REPOSITORY (--repository, or the
//       folder above the runner that holds AxKit.sln), else the one above this program. A runner built outside the
//       repository, as nvda-pass.ps1 builds it into %TEMP%, has none above itself: until 2026-11-18 only that place
//       was looked in, and every stream script of the first NVDA pass ended at once, with code 2. A path that ends
//       in .jsonl is taken as it is. --version as the first of Claude Code's arguments prints the recording's
//       version and ends.
//   Exit code: the recording's own (0, or what Claude Code ended with), or 2 with one line on standard error
//   ("stand-in: ...") for a recording it cannot play or a window that wrote something else than the recording has;
//   the window shows that line when the process ends (SM-9.3), where a script's expectations meet it.
//
// What it does with each record of a recording, in order:
//   meta                  the version, for --version and for its own answer to the first request
//   in, message           waits for a message of the window's. Its text need not be the recording's: where Claude
//                         Code's line that sends the message back holds the recorded text, the window's text is put
//                         in its place, so that the window sees its own message come back
//   in, initialize | interrupt | mode
//                         waits for that request of the window's; the answer the recording holds for it is written
//                         with the name the window gave its request
//   in, closed            waits until the window closes the input, which is how it tells Claude Code to end; but when
//                         the recording ends with a code that is not 0, Claude Code ended by itself there, and so
//                         does the stand-in
//   out                   the line, after the pause the recording has before it, 150 ms at most
//   ask                   Claude Code asked its permission tool here. The stand-in does the same with the tool the
//                         window named: it starts the one server of the --mcp-config file as Claude Code starts it
//                         (the window's AxKit.exe ask), calls the tool of --permission-prompt-tool with the recorded
//                         question, and waits for the answer as long as the window takes. Without those two
//                         arguments nothing is asked
//   answer                what the recording's own permission tool answered, for the log: the lines that follow in
//                         the recording are what Claude Code wrote after that answer, whatever the window answered
//   exit                  ends with the code
// A request of the window's that the recording does not have where it comes (the first request of every start, which
// only one recording holds) is answered by the stand-in itself as Claude Code answers it: the first request with the
// permission mode "default" and the recording's version, any other with success. A message where the recording has
// none ends the play with the line on standard error.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxAutomate;

/// <summary>
/// The stand-in of Claude Code's stream: a recording of the real stream played to a window as Claude Code would
/// write it, moved on by what the window writes (the file's header has the usage and the records).
/// </summary>
internal static class StreamStandIn
{
    /// <summary>The stand-in's first argument for this mode (StandIn.Run).</summary>
    public const string Option = "--stream";

    /// <summary>
    /// The variable the runner hands the run's repository on in (Program.Main), for the recordings a script names by
    /// their case: every window, cmd.exe and stand-in of the run inherits it, wherever the runner was built.
    /// </summary>
    public const string RepositoryVariable = "AXKIT_STANDIN_REPOSITORY";

    private const string VersionOption = "--version";

    /// <summary>The stand-in's own option, right after the case: a file of tests/fixtures/records written as the session file of the stand-in's conversation (<see cref="WriteSession"/>).</summary>
    private const string SessionOption = "--session";

    /// <summary>The id of the session the stand-in writes: a session's id in Claude Code's form, which no real session has.</summary>
    private const string StandInSession = "70000000-0000-4000-8000-000000000001";

    /// <summary>The variable that names Claude Code's configuration folder.</summary>
    private const string ConfigFolderVariable = "CLAUDE_CONFIG_DIR";
    private const string McpConfigOption = "--mcp-config";
    private const string PermissionToolOption = "--permission-prompt-tool";
    private const string Usage = "Usage: AxAutomate standin --stream <case | file.jsonl> <Claude Code's arguments>. See tools/AxAutomate/StreamStandIn.cs.";
    private const int CannotPlay = 2;

    /// <summary>The longest pause before a line: a recording's seconds of a model's thinking are not waited out.</summary>
    private const int LongestPauseMs = 150;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>What the window wrote and the stand-in has not taken yet, in order; null marks the end of the input.</summary>
    private static readonly BlockingCollection<string?> Written = [];

    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return Refuse(Usage);
        }

        Process? relay = null;
        try
        {
            var records = Records(FixturePath(args[0]));
            var claude = args[1..];
            string? session = null;
            if (claude.Length >= 2 && claude[0] == SessionOption)
            {
                session = claude[1];
                claude = claude[2..];
            }

            var version = records.Select(record => StringOf(record, "claudeCode")).FirstOrDefault(text => text is not null) ?? "0.0.0";
            if (claude.Length > 0 && claude[0] == VersionOption)
            {
                Console.Out.WriteLine(version + " (stand-in)");
                return 0;
            }

            if (session is not null)
            {
                WriteSession(session);
            }

            return Play(records, claude, version, ref relay);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
            or InvalidOperationException or ArgumentException or NotSupportedException or KeyNotFoundException or Win32Exception)
        {
            return Refuse(ex.Message);
        }
        finally
        {
            End(relay);
        }
    }

    /// <summary>
    /// The session file Claude Code keeps of a conversation, as the stand-in's own (docs/axClaude/stream-mode.md
    /// SM-8.2): a hand-written file of tests/fixtures/records copied as it is to
    /// <c>&lt;CLAUDE_CONFIG_DIR&gt;\projects\&lt;the working folder's project folder&gt;\&lt;StandInSession&gt;.jsonl</c>, where
    /// Claude Code would have written it, so that a window started in the same folder after it takes that
    /// conversation up. Only into a configuration folder inside the run's own folder (the one the working folder is
    /// in): a script's claude.cmd names it, and the user's own is never written. The project folder's name is Claude
    /// Code's rule, which axClaude keeps in <c>ClaudeCodeTranscript.ProjectFolderName</c>: every character that is not
    /// an ASCII letter or digit a hyphen.
    /// </summary>
    private static void WriteSession(string named)
    {
        var config = Environment.GetEnvironmentVariable(ConfigFolderVariable);
        var run = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Environment.CurrentDirectory));
        if (string.IsNullOrEmpty(config) || run is null
            || !Path.GetFullPath(config).StartsWith(Path.TrimEndingDirectorySeparator(run) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{SessionOption} writes a session file only into a configuration folder inside the run's own folder, named by {ConfigFolderVariable}.");
        }

        var project = new StringBuilder();
        foreach (var c in Path.TrimEndingDirectorySeparator(Environment.CurrentDirectory))
        {
            project.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        var folder = Path.Combine(Path.GetFullPath(config), "projects", project.ToString());
        Directory.CreateDirectory(folder);
        // Several records, separated by commas (docs/axClaude/stream-mode.md SM-8.5; script 58), are several earlier
        // conversations of the folder: each its own session, the first named the stand-in's own and the oldest, each
        // next a minute newer, so that a window lists them newest first.
        var records = named.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var now = DateTime.UtcNow;
        for (var i = 0; i < records.Length; i++)
        {
            var file = Path.Combine(folder, (i == 0 ? StandInSession : StandInSession[..^1] + (i + 1).ToString(CultureInfo.InvariantCulture)) + ".jsonl");
            File.Copy(FixturePath(records[i], "records"), file, overwrite: true);
            File.SetLastWriteTimeUtc(file, now.AddMinutes(i - records.Length));
        }
    }

    private static int Refuse(string message)
    {
        Console.Error.WriteLine("stand-in: " + message);
        return CannotPlay;
    }

    /// <summary>
    /// A case's file under the stream fixtures of the run's repository, or the path given. The repository is the one
    /// the runner names (<see cref="RepositoryVariable"/>), else the one above this program; the message names no
    /// folder, since the window shows it.
    /// </summary>
    private static string FixturePath(string named, string fixtures = "streams")
    {
        if (named.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(named);
        }

        var given = Environment.GetEnvironmentVariable(RepositoryVariable);
        var repository = !string.IsNullOrEmpty(given) && File.Exists(Path.Combine(given, "AxKit.sln")) ? given : RunOptions.FindRepository();
        if (repository is null)
        {
            throw new InvalidDataException($"No repository to find the recording {named} in: the runner named none and none is above this program; give the path of a .jsonl file.");
        }

        return Path.Combine(repository, "tests", "fixtures", fixtures, named + ".jsonl");
    }

    private static List<JsonElement> Records(string path)
    {
        var records = new List<JsonElement>();
        foreach (var line in File.ReadLines(path, Utf8))
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind != JsonValueKind.Object || StringOf(document.RootElement, "k") is null)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} holds a line that is no record of a recording.");
            }

            records.Add(document.RootElement.Clone());
        }

        return records;
    }

    private static int Play(List<JsonElement> records, string[] claude, string version, ref Process? relay)
    {
        var log = new Log(Path.Combine(Environment.CurrentDirectory, "stream-standin.log"));
        log.Line($"started, process {Environment.ProcessId}: {records.Count} records");
        var exitCode = records.Where(record => StringOf(record, "k") == "exit").Select(record => Number(record, "code")).LastOrDefault();
        using var output = Console.OpenStandardOutput();
        var reader = new Thread(ReadInput) { IsBackground = true, Name = "stand-in input" };
        reader.Start();

        // The names the window gave its requests and the texts of its messages, by what the recording has in their place.
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        string? asked = null;
        long before = 0;
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var at = record.TryGetProperty("t", out var time) && time.TryGetInt64(out var ms) ? ms : before;
            var pause = (int)Math.Clamp(at - before, 0, LongestPauseMs);
            before = at;
            var kind = StringOf(record, "k");
            switch (kind)
            {
                case "meta":
                case "answer":
                    if (kind == "answer")
                    {
                        var recorded = StringOf(record, "behavior") ?? string.Empty;
                        log.Line($"record {i + 1}: the recording's answer was {recorded}{(asked is null ? string.Empty : asked == recorded || (asked == "allow" && recorded is "always" or "answered") ? ", as the window's" : ", the window's was " + asked)}");
                        asked = null;
                    }

                    break;

                case "in":
                    var what = StringOf(record, "what") ?? string.Empty;
                    log.Line($"record {i + 1}: waits for the window's {what}");
                    if (what == "closed")
                    {
                        if (exitCode != 0)
                        {
                            // Claude Code ended by itself here.
                            break;
                        }

                        if (!AwaitEnd(output, version, log))
                        {
                            return Refuse("the window wrote a message after the recording's last turn");
                        }

                        break;
                    }

                    switch (Await(what, output, version, log, out var written))
                    {
                        case Awaited.Ended:
                            log.Line("ended: the window closed the input");
                            return 0;
                        case Awaited.Other:
                            return Refuse($"the window wrote a message where the recording has its {what}");
                    }

                    if (what == "message")
                    {
                        texts[StringOf(record, "text") ?? string.Empty] = MessageText(written!);
                    }
                    else if (StringOf(record, "id") is { } id)
                    {
                        names[id] = StringOf(written!.RootElement, "request_id") ?? id;
                    }

                    written!.Dispose();
                    break;

                case "out":
                    Thread.Sleep(pause);
                    Write(output, Substituted(record.GetProperty("line"), names, texts));
                    break;

                case "ask":
                    Thread.Sleep(pause);
                    asked = Ask(record, claude, ref relay, log);
                    break;

                case "exit":
                    Thread.Sleep(pause);
                    log.Line($"ended: the recording's end, code {exitCode}");
                    return exitCode;

                default:
                    throw new InvalidDataException($"record {i + 1} is of a kind the stand-in does not know: {kind}");
            }
        }

        log.Line("ended: every record played");
        return exitCode;
    }

    // --- What the window writes -------------------------------------------------------------------------------------

    private enum Awaited
    {
        Came,
        Ended,
        Other,
    }

    private static void ReadInput()
    {
        try
        {
            using var input = new StreamReader(Console.OpenStandardInput(), Utf8);
            while (input.ReadLine() is { } line)
            {
                Written.Add(line);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // The pipe broke with the window: the input has ended.
        }

        Written.Add(null);
    }

    /// <summary>
    /// Waits for the window to write what the recording has here. A request of another kind that comes first is
    /// answered by the stand-in itself and the wait goes on; a message where a request is awaited is
    /// <see cref="Awaited.Other"/>; the end of the input is <see cref="Awaited.Ended"/>.
    /// </summary>
    private static Awaited Await(string what, Stream output, string version, Log log, out JsonDocument? written)
    {
        written = null;
        while (true)
        {
            var line = Written.Take();
            if (line is null)
            {
                return Awaited.Ended;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                log.Line("a line of the window's that is no JSON: passed over");
                continue;
            }

            var kind = KindOf(document.RootElement);
            if (kind == what)
            {
                log.Line($"the window wrote its {what}");
                written = document;
                return Awaited.Came;
            }

            if (kind == "message")
            {
                document.Dispose();
                return Awaited.Other;
            }

            AnswerItself(kind, document.RootElement, output, version, log);
            document.Dispose();
        }
    }

    /// <summary>After the recording's last turn: requests are still answered, until the window closes the input. False for a message, which nothing would answer.</summary>
    private static bool AwaitEnd(Stream output, string version, Log log)
    {
        while (Written.Take() is { } line)
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var kind = KindOf(document.RootElement);
                if (kind == "message")
                {
                    return false;
                }

                AnswerItself(kind, document.RootElement, output, version, log);
            }
            catch (JsonException)
            {
                log.Line("a line of the window's that is no JSON: passed over");
            }
        }

        log.Line("the window closed the input");
        return true;
    }

    /// <summary>What a line of the window's is, in the recordings' words: message, initialize, interrupt, mode, or request for any other request.</summary>
    private static string KindOf(JsonElement line) => StringOf(line, "type") switch
    {
        "user" => "message",
        "control_request" => (line.TryGetProperty("request", out var request) ? StringOf(request, "subtype") : null) switch
        {
            "initialize" => "initialize",
            "interrupt" => "interrupt",
            "set_permission_mode" => "mode",
            _ => "request",
        },
        _ => "unknown",
    };

    /// <summary>
    /// The slash commands the stand-in's own answer to the first request lists: some of those Claude Code 2.1.332
    /// listed (the probe's case command-lists, 2026-11-26), none of a user's own. A command of Claude Code's screen
    /// alone (/help) is not among them, as it was not there (docs/axClaude/stream-mode.md SM-5.8).
    /// </summary>
    private static readonly string[] Commands = ["compact", "clear", "model", "effort", "context", "config", "usage", "goal", "mcp", "agents", "advisor"];

    /// <summary>The stand-in's own answer to a request the recording does not have here, as Claude Code 2.1.327 answers it: the first request with the mode, the version and (as 2.1.332 does) the commands, a change of the mode with success and the note that names the new mode, any other with success.</summary>
    private static void AnswerItself(string kind, JsonElement line, Stream output, string version, Log log)
    {
        if (StringOf(line, "request_id") is not { } id)
        {
            log.Line($"a line of the window's of the kind {kind}: nothing to answer");
            return;
        }

        var response = new JsonObject { ["subtype"] = "success", ["request_id"] = id };
        if (kind == "initialize")
        {
            var commands = new JsonArray();
            foreach (var name in Commands)
            {
                commands.Add(new JsonObject { ["name"] = name, ["description"] = string.Empty });
            }

            response["response"] = new JsonObject { ["current_permission_mode"] = "default", ["claude_code_version"] = version, ["commands"] = commands };
        }

        Write(output, new JsonObject { ["type"] = "control_response", ["response"] = response }.ToJsonString());
        if (kind == "mode" && line.TryGetProperty("request", out var request) && StringOf(request, "mode") is { } mode)
        {
            Write(output, new JsonObject { ["type"] = "system", ["subtype"] = "status", ["permissionMode"] = mode }.ToJsonString());
        }

        log.Line($"answered the window's {kind} itself");
    }

    private static string MessageText(JsonDocument written) =>
        written.RootElement.TryGetProperty("message", out var message) ? StringOf(message, "content") ?? string.Empty : string.Empty;

    // --- What Claude Code wrote -------------------------------------------------------------------------------------

    /// <summary>A recorded line as one line of JSON, with the window's own names and texts where the recording has the probe's.</summary>
    private static string Substituted(JsonElement line, Dictionary<string, string> names, Dictionary<string, string> texts)
    {
        var node = JsonNode.Parse(line.GetRawText())!;
        if (node is JsonObject root)
        {
            if (root["type"]?.GetValue<string>() == "control_response" && root["response"] is JsonObject response
                && response["request_id"] is JsonValue name && name.TryGetValue<string>(out var recorded) && names.TryGetValue(recorded, out var given))
            {
                response["request_id"] = given;
            }

            if (root["type"]?.GetValue<string>() == "user" && root["isReplay"] is JsonValue replay && replay.TryGetValue<bool>(out var isReplay) && isReplay
                && root["message"] is JsonObject message && message["content"] is JsonValue content && content.TryGetValue<string>(out var text) && texts.TryGetValue(text, out var sent))
            {
                message["content"] = sent;
            }
        }

        return node.ToJsonString();
    }

    private static void Write(Stream output, string line)
    {
        output.Write(Utf8.GetBytes(line + "\n"));
        output.Flush();
    }

    // --- The permission tool ----------------------------------------------------------------------------------------

    /// <summary>
    /// Asks the window what the recording's Claude Code asked its permission tool: through the server and the tool
    /// the window named in Claude Code's arguments, as Claude Code does it (MCP's JSON-RPC over the server's standard
    /// input and output). The server is started once and kept. Gives the answer's <c>behavior</c>, or null when
    /// nothing was asked or no answer could be read.
    /// </summary>
    private static string? Ask(JsonElement record, string[] claude, ref Process? relay, Log log)
    {
        var tool = Argument(claude, PermissionToolOption)?.Split("__") is ["mcp", var server, var named] ? (Server: server, Name: named) : default;
        if (tool.Name is null || Argument(claude, McpConfigOption) is not { } config)
        {
            log.Line("ask: no permission tool among the arguments, nothing asked");
            return null;
        }

        if (relay is null)
        {
            relay = StartServer(config, tool.Server);
            Exchange(relay, 1, "initialize", new JsonObject { ["protocolVersion"] = "2024-11-05", ["capabilities"] = new JsonObject(), ["clientInfo"] = new JsonObject { ["name"] = "stand-in", ["version"] = "1" } });
            relay.StandardInput.WriteLine("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
            relay.StandardInput.Flush();
            log.Line("ask: the server of the window's file was started");
        }

        var arguments = new JsonObject
        {
            ["tool_name"] = StringOf(record, "tool") ?? string.Empty,
            ["input"] = record.TryGetProperty("input", out var input) ? JsonNode.Parse(input.GetRawText()) : new JsonObject(),
            ["tool_use_id"] = StringOf(record, "call") ?? string.Empty,
        };
        var since = Stopwatch.StartNew();
        var answer = Exchange(relay, 2, "tools/call", new JsonObject { ["name"] = tool.Name, ["arguments"] = arguments });
        string? behavior = null;
        try
        {
            var text = answer?["result"]?["content"]?[0]?["text"]?.GetValue<string>();
            behavior = text is null ? null : JsonNode.Parse(text)?["behavior"]?.GetValue<string>();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            // An answer of another shape: no behavior.
        }

        log.Line($"ask: the window's answer was {behavior ?? "not read"}, after {since.ElapsedMilliseconds} ms");
        return behavior;
    }

    /// <summary>The server the window's file names under <paramref name="server"/>, started as Claude Code starts it: the program and its arguments, no shell, no window, this process's environment (the window's pipe among it).</summary>
    private static Process StartServer(string configFile, string server)
    {
        using var config = JsonDocument.Parse(File.ReadAllText(configFile));
        if (!config.RootElement.TryGetProperty("mcpServers", out var servers) || !servers.TryGetProperty(server, out var named)
            || StringOf(named, "command") is not { Length: > 0 } command)
        {
            throw new InvalidDataException($"The file of --mcp-config names no server {server} with a command.");
        }

        var start = new ProcessStartInfo(command)
        {
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8,
            StandardOutputEncoding = Utf8,
        };
        if (named.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Array)
        {
            foreach (var argument in args.EnumerateArray())
            {
                start.ArgumentList.Add(argument.GetString() ?? string.Empty);
            }
        }

        // Outside call, program, Claude Code questions: the server a window named for Claude Code's questions, started
        // as Claude Code starts it (in a window of a test run, AxKit.exe ask of the run's own build).
        var process = Process.Start(start) ?? throw new InvalidDataException("The server of --mcp-config was not started.");
        _ = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        return process;
    }

    /// <summary>One request and its answer: the line with the request's number, however long the server takes.</summary>
    private static JsonNode? Exchange(Process server, int number, string method, JsonObject parameters)
    {
        // Outside call, pipe, Claude Code questions: the server's standard input and output, on which a question is
        // written and its answer read, as Claude Code does it.
        server.StandardInput.WriteLine(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = number, ["method"] = method, ["params"] = parameters }.ToJsonString());
        server.StandardInput.Flush();
        while (server.StandardOutput.ReadLine() is { } line)
        {
            try
            {
                var node = JsonNode.Parse(line);
                if (node?["id"] is JsonValue id && id.TryGetValue<int>(out var answered) && answered == number)
                {
                    return node;
                }
            }
            catch (JsonException)
            {
                // A line that is no answer: passed over.
            }
        }

        return null;
    }

    private static void End(Process? server)
    {
        if (server is null)
        {
            return;
        }

        try
        {
            if (!server.HasExited)
            {
                server.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // It ended meanwhile.
        }

        server.Dispose();
    }

    // --- Values -----------------------------------------------------------------------------------------------------

    /// <summary>The value after an option among Claude Code's arguments, or after its equals sign; null when it is not there.</summary>
    private static string? Argument(string[] claude, string option)
    {
        for (var i = 0; i < claude.Length; i++)
        {
            if (claude[i] == option && i + 1 < claude.Length)
            {
                return claude[i + 1];
            }

            if (claude[i].StartsWith(option + "=", StringComparison.Ordinal))
            {
                return claude[i][(option.Length + 1)..];
            }
        }

        return null;
    }

    private static string? StringOf(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int Number(JsonElement element, string key) =>
        element.TryGetProperty(key, out var value) && value.TryGetInt32(out var number) ? number : 0;

    /// <summary>stream-standin.log in the working folder: what the stand-in did, for a script's author reading a kept run (--keep). Kinds and numbers, never a text.</summary>
    private sealed class Log(string path)
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        public void Line(string text)
        {
            try
            {
                File.AppendAllText(path, string.Create(CultureInfo.InvariantCulture, $"{_clock.ElapsedMilliseconds,7} {text}\r\n"), Utf8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The line is lost, the play goes on.
            }
        }
    }
}
