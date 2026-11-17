// probe stream: Claude Code's non-interactive stream, measured (docs/axClaude/stream.md). A fixed set of cases is
// played against the installed Claude Code, each a process started with ClaudeCodeStream.Options: messages written on
// standard input, every line of standard output read through ClaudeCodeStream.Read, and what the case expects held to
// the lines. It prints kinds, names and times only: never a text Claude wrote, a path or an id. It is the check to run
// when Claude Code changes (tools\upstream.ps1 names it), and what it prints for a new version is compared with
// docs/axClaude/stream-shape.json, the shape of every kind of line seen, written with --shape.
//
//   probe stream [--only <case>] [--shape <file>] [--keep <folder>] [--model <name>]
//
// Every run uses the smallest model unless --model says otherwise, saves no session (ClaudeCodeStream.NoSessionOption),
// loads no MCP server but its own stand-in permission tool, and works in a folder of its own under the temporary
// folder, deleted at the end. --keep copies the raw lines there for the developer's eyes; they hold what Claude wrote
// and the session's ids, and are never committed.
//
// The stand-in permission tool is this program again, "probe permission-tool": a server of one MCP tool over standard
// input and output, which answers as the variable AXKIT_PROBE_PERMISSION says (allow or deny; a question of Claude's
// gets the first option of each question) and writes the name of the tool asked about, with the keys of its input, to
// the file AXKIT_PROBE_LOG.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AxClaude.Core.Streaming;
using AxKit.Core;

namespace Probe;

internal static class StreamProbe
{
    private const string PermissionVariable = "AXKIT_PROBE_PERMISSION";
    private const string LogVariable = "AXKIT_PROBE_LOG";
    private const string ToolName = "mcp__axprobe__approve";

    /// <summary>A line written on standard input: at the start (<see cref="When"/> null) or after the first line read that <see cref="When"/> holds for, <see cref="DelayMs"/> later. A null <see cref="Line"/> closes standard input.</summary>
    private sealed record Step(Func<StreamLine, bool>? When, int DelayMs, string? Line, string What);

    /// <summary>What a case saw: each line with its time, the tools the permission tool was asked about, the exit code, the folder the case ran in.</summary>
    private sealed record Outcome(List<(long Ms, StreamLine Line)> Lines, List<string> Asked, int ExitCode, string Folder, bool TimedOut)
    {
        public IEnumerable<StreamPart> Parts => Lines.SelectMany(entry => entry.Line.Parts);

        public int Count(StreamKind kind) => Parts.Count(part => part.Kind == kind);

        public bool Says(string word) => Parts.Any(part => part.Kind == StreamKind.Text && part.Text.Contains(word, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<StreamLine> Ends => Lines.Select(entry => entry.Line).Where(line => line.Has(StreamKind.Ended));
    }

    /// <summary>One case: its options beside the stream's own, how the permission tool answers (empty: no tool), its steps, what must hold (each a sentence said when it does not), and what is only reported.</summary>
    private sealed record Case(string Name, string What, string[] Options, string Permission, Step[] Steps, Func<Outcome, IEnumerable<string>> Problems, Func<Outcome, IEnumerable<string>>? Findings = null, int LimitSeconds = 90);

    private static Step Say(string text, Func<StreamLine, bool>? when = null, int delayMs = 0, string? what = null) =>
        new(when, delayMs, ClaudeCodeStream.UserLine(text), what ?? "a message");

    private static Step Close(Func<StreamLine, bool> when, int delayMs = 0) => new(when, delayMs, null, "standard input closed");

    private static bool Ended(StreamLine line) => line.Has(StreamKind.Ended);

    private static bool Calls(StreamLine line) => line.Has(StreamKind.ToolCall);

    private static IEnumerable<string> Unless(bool holds, string problem)
    {
        if (!holds)
        {
            yield return problem;
        }
    }

    private static readonly string[] Asking = ["--permission-mode", "default", ClaudeCodeStream.PermissionToolOption, ToolName];

    private static readonly Case[] Cases =
    [
        new("turn", "one message, one reply, the turn's end", [], string.Empty,
            [Say("Reply with exactly: ok"), Close(Ended)],
            o => Unless(o.Count(StreamKind.Started) >= 1, "no line said the process was up")
                .Concat(Unless(o.Says("ok"), "the reply's text did not arrive"))
                .Concat(Unless(o.Ends.Count() == 1 && !o.Ends.First().Failed, "the turn did not end once and well"))
                .Concat(Unless(o.Lines.Count > 0 && o.Lines[0].Line.Version.Length > 0, "the first line did not give Claude Code's version")),
            o => [$"the process was up after {o.Lines.FirstOrDefault(e => e.Line.Has(StreamKind.Started)).Ms} ms, the turn over after {o.Lines.LastOrDefault(e => e.Line.Has(StreamKind.Ended)).Ms} ms", "thinking came as a part of its own: " + (o.Count(StreamKind.Thinking) > 0 ? "yes" : "no")]),

        new("two-messages", "a second message to the same process after the first turn", [], string.Empty,
            [Say("Reply with exactly: one"), Say("Reply with exactly: two", Ended), Close(Ended)],
            o => Unless(o.Ends.Count() == 2, $"two turns should end, {o.Ends.Count()} did")
                .Concat(Unless(o.Says("one") && o.Says("two"), "both replies should arrive"))
                .Concat(Unless(o.ExitCode == 0, $"the process ended with code {o.ExitCode}")),
            o => o.Ends.Count() == 2 ? new[] { $"the second turn took {SecondTurnMs(o)} ms from its message to its end, with no new process" } : Array.Empty<string>()),

        // Which tool the model reaches for first is its own: on 2026-11-17 it once tried another shell's tool, which no
        // rule allowed, and then the one asked for. So a result that is no error must come, not no error at all.
        new("tool", "a tool call allowed by a rule: the call, its result, the reply", ["--permission-mode", "default", "--allowedTools", "Bash(ping *)"], string.Empty,
            [Say("Run this command with the Bash tool: ping -n 2 127.0.0.1\nAfter it finishes, reply with exactly: pinged"), Close(Ended)],
            o => Unless(InOrder(o, StreamKind.ToolCall, StreamKind.ToolResult, StreamKind.Text, StreamKind.Ended), "the call, its result, the reply and the end should come in that order")
                .Concat(Unless(o.Parts.Any(part => part.Kind == StreamKind.ToolCall && part.Tool == "Bash"), "a call should name the tool asked for"))
                .Concat(Unless(o.Parts.Any(part => part.Kind == StreamKind.ToolResult && !part.Error), "a result that is no error should come"))
                .Concat(Unless(o.Says("pinged"), "the reply after the tool should arrive")),
            o => [$"tools called: {string.Join(", ", o.Parts.Where(part => part.Kind == StreamKind.ToolCall).Select(part => part.Tool))}; results that were errors: {o.Parts.Count(part => part.Kind == StreamKind.ToolResult && part.Error)}", "Claude Code's own notes seen: " + Notes(o)]),

        new("permission-allowed", "a tool call that needs the user's word, answered yes by the permission tool", Asking, "allow",
            [Say("Use the Write tool to create a file named probe-allowed.txt in the current folder with the content hi. Then reply with exactly: written"), Close(Ended)],
            o => Unless(o.Asked.Contains("Write"), "the permission tool should be asked about Write")
                .Concat(Unless(File.Exists(Path.Combine(o.Folder, "probe-allowed.txt")), "the file should be made after a yes"))
                .Concat(Unless(o.Ends.Any() && o.Ends.Last().Denials == 0, "no denial should be counted"))),

        new("permission-denied", "the same, answered no", Asking, "deny",
            [Say("Use the Write tool to create a file named probe-denied.txt in the current folder with the content hi. Then say in one short sentence whether it was created."), Close(Ended)],
            o => Unless(o.Asked.Contains("Write"), "the permission tool should be asked about Write")
                .Concat(Unless(!File.Exists(Path.Combine(o.Folder, "probe-denied.txt")), "the file must not be made after a no"))
                .Concat(Unless(o.Parts.Any(part => part.Kind == StreamKind.ToolResult && part.Error), "the tool's result should be an error"))
                .Concat(Unless(o.Ends.Any() && o.Ends.Last().Denials == 1, "one denial should be counted at the turn's end"))),

        new("question", "a question of Claude's to the user, answered through the permission tool", Asking, "allow",
            [Say("Use the AskUserQuestion tool to ask me one question: which colour I prefer, with the options Red and Blue. Then reply with the colour I chose and nothing else."), Close(Ended)],
            o => Unless(o.Asked.Contains(ClaudeCodeStream.QuestionTool), "the permission tool should be asked with Claude's question")
                .Concat(Unless(o.Says("Red"), "the reply should name the first option, which the stand-in chose"))),

        new("interrupt", "a running turn ended by the host, the process kept", ["--permission-mode", "default", "--allowedTools", "Bash(ping *)"], string.Empty,
            [
                Say("Run this command with the Bash tool: ping -n 20 127.0.0.1\nAfter it finishes, reply with exactly: finished"),
                new(Calls, 2500, ClaudeCodeStream.InterruptLine("probe-1"), "an interrupt"),
                Say("Reply with exactly: still here", Ended, 300, "a message after the interrupt"),
                Close(Ended),
            ],
            o => Unless(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-1" && !part.Error), "the interrupt should be answered with its id")
                .Concat(Unless(o.Ends.Any() && o.Ends.First().Failed, "the interrupted turn should end as failed"))
                .Concat(Unless(!o.Says("finished"), "the interrupted turn must not finish its work"))
                .Concat(Unless(o.Says("still here"), "the process should answer a message after the interrupt"))
                .Concat(Unless(o.ExitCode == 0, $"the process ended with code {o.ExitCode}")),
            o => o.Ends.Any() ? new[] { $"the interrupted turn ended with Claude Code's word '{o.Ends.First().Terminal}', {InterruptMs(o)} ms after the interrupt was answered" } : Array.Empty<string>()),

        // Measured 2026-11-17: the message is taken up inside the running turn, after the tool's result, and steers
        // it: one turn ends, and the smallest model answered the newer message alone. So the second answer is what
        // must come; whether the first instruction was still followed is the model's, and only reported.
        new("message-mid-turn", "a second message written while a tool runs", ["--permission-mode", "default", "--allowedTools", "Bash(ping *)"], string.Empty,
            [
                Say("Run this command with the Bash tool: ping -n 5 127.0.0.1\nAfter it finishes, reply with exactly: first done"),
                Say("Reply with exactly: second answer", Calls, 1200, "a second message, while the tool runs"),
                Close(Ended, 9000),
            ],
            o => Unless(o.Says("second answer"), "the message written while the tool ran should be answered")
                .Concat(Unless(InOrder(o, StreamKind.ToolCall, StreamKind.ToolResult, StreamKind.Text), "its answer should come after the running tool's result")),
            o => [$"turns ended: {o.Ends.Count()}; the first instruction was still followed: {(o.Says("first done") ? "yes" : "no")}; messages still waiting at the first end: {(o.Ends.Any() ? o.Ends.First().Queued : 0)}"],
            LimitSeconds: 120),

        new("partial", "a reply written piece by piece", [ClaudeCodeStream.PartialOption], string.Empty,
            [Say("Count from one to five in words, one per line."), Close(Ended)],
            o => Unless(o.Count(StreamKind.Delta) > 0, "pieces should arrive before the whole message")
                .Concat(Unless(o.Count(StreamKind.Text) > 0, "the whole message should still arrive")),
            o => [$"pieces: {o.Count(StreamKind.Delta)}, of which with text: {o.Parts.Count(part => part.Kind == StreamKind.Delta && part.Text.Length > 0)}"]),

        new("worker", "work handed to a subagent (reported, not required: the model may answer by itself)", ["--permission-mode", "default"], string.Empty,
            [Say("Use the Agent tool to start a general-purpose subagent whose whole task is to reply with exactly: from the worker. Then reply with exactly: handed over"), Close(Ended)],
            o => Unless(o.Ends.Any(), "the turn should end"),
            o => [$"lines marked as a subagent's: {o.Lines.Count(e => e.Line.FromWorker)}; tools called: {string.Join(", ", o.Parts.Where(part => part.Kind == StreamKind.ToolCall).Select(part => part.Tool).Distinct())}", "Claude Code's own notes seen: " + Notes(o)],
            LimitSeconds: 150),

        new("unknown-model", "a model that does not exist (reported: how a failure looks)", ["--model", "no-such-model-of-the-probe"], string.Empty,
            [Say("Reply with exactly: ok"), Close(Ended)],
            _ => [],
            o => [$"exit code {o.ExitCode}; turns ended: {o.Ends.Count()}, failed: {o.Ends.Count(end => end.Failed)}; lines read: {o.Lines.Count}", "Claude Code's own notes seen: " + Notes(o)],
            LimitSeconds: 60),
    ];

    public static int Run(string[] args)
    {
        string? only = null, shapeFile = null, keep = null;
        var model = "haiku";
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--only" when i + 1 < args.Length: only = args[++i]; break;
                case "--shape" when i + 1 < args.Length: shapeFile = args[++i]; break;
                case "--keep" when i + 1 < args.Length: keep = args[++i]; break;
                case "--model" when i + 1 < args.Length: model = args[++i]; break;
                default:
                    Console.Error.WriteLine("Usage: probe stream [--only <case>] [--shape <file>] [--keep <folder>] [--model <name>]");
                    Console.Error.WriteLine("Cases: " + string.Join(", ", Cases.Select(c => c.Name)));
                    return 2;
            }
        }

        var claude = ClaudeCodeProgram.Find(null);
        if (claude is null)
        {
            Console.Error.WriteLine("Claude Code was not found.");
            return 2;
        }

        var work = Path.Combine(Path.GetTempPath(), "axkit-stream-probe-" + Environment.ProcessId);
        Directory.CreateDirectory(work);
        var shapes = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var blocks = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var asked = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var version = string.Empty;
        var failed = new List<string>();
        try
        {
            // The stand-in permission tool is this program: its path goes into a file of the work folder only.
            var config = Path.Combine(work, "mcp.json");
            File.WriteAllText(config, JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["mcpServers"] = new Dictionary<string, object>
                {
                    ["axprobe"] = new Dictionary<string, object> { ["command"] = Environment.ProcessPath!, ["args"] = new[] { "permission-tool" } },
                },
            }));

            foreach (var c in Cases.Where(c => only is null || c.Name == only))
            {
                Console.WriteLine($"== {c.Name}: {c.What}");
                var folder = Path.Combine(work, c.Name);
                Directory.CreateDirectory(folder);
                // A case's own model option (the one that does not exist) goes last, so that it wins.
                string[] options = [.. ClaudeCodeStream.Options, ClaudeCodeStream.NoSessionOption, ClaudeCodeProgram.StrictMcpConfigOption, "--mcp-config", config, "--model", model, .. c.Options];
                var outcome = Play(claude, options, c, folder, shapes, blocks, asked, keep);
                version = outcome.Lines.Select(entry => entry.Line.Version).FirstOrDefault(v => v.Length > 0) ?? version;
                var problems = outcome.TimedOut ? new List<string> { $"no end within {c.LimitSeconds} s; the process was ended" } : c.Problems(outcome).ToList();
                foreach (var finding in c.Findings?.Invoke(outcome) ?? [])
                {
                    Console.WriteLine("  found: " + finding);
                }

                if (problems.Count == 0)
                {
                    Console.WriteLine("  passed");
                }
                else
                {
                    failed.Add(c.Name);
                    foreach (var problem in problems)
                    {
                        Console.WriteLine("  FAILED: " + problem);
                    }
                }
            }
        }
        finally
        {
            try
            {
                Directory.Delete(work, recursive: true);
            }
            catch (IOException)
            {
                // A process of a case may still hold a file for a moment: the folder is under the temporary folder.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        if (shapeFile is not null && only is null)
        {
            File.WriteAllText(shapeFile, ShapeText(version, shapes, blocks, asked));
            Console.WriteLine($"The shape of {shapes.Count} kinds of line is written to {Path.GetFileName(shapeFile)}.");
        }

        Console.WriteLine(failed.Count == 0
            ? $"Claude Code {version}: every case of the stream passed."
            : $"Claude Code {version}: FAILED: {string.Join(", ", failed)}.");
        return failed.Count == 0 ? 0 : 1;
    }

    private static Outcome Play(string claude, string[] options, Case c, string folder,
        SortedDictionary<string, SortedSet<string>> shapes, SortedDictionary<string, SortedSet<string>> blocks, SortedDictionary<string, SortedSet<string>> asked, string? keep)
    {
        var line = ClaudeCodeProgram.LineFor(claude, options);
        if (line.Refusal is not null)
        {
            Console.WriteLine("  cannot start: " + line.Refusal);
            return new Outcome([], [], -1, folder, true);
        }

        var log = Path.Combine(folder, "permission-tool.log");
        var start = new ProcessStartInfo(line.FileName, line.Arguments)
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.Environment[PermissionVariable] = c.Permission;
        start.Environment[LogVariable] = log;
        var clock = Stopwatch.StartNew();
        // Outside call, process, Claude Code: the non-interactive stream of the installed program, for a measurement.
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        var input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        var gate = new object();
        var pending = new Queue<Step>(c.Steps);
        var raw = new List<string>();
        var lines = new List<(long Ms, StreamLine Line)>();

        void Write(Step step)
        {
            void Now()
            {
                lock (gate)
                {
                    try
                    {
                        if (step.Line is null)
                        {
                            input.Close();
                        }
                        else
                        {
                            input.WriteLine(step.Line);
                        }

                        Console.WriteLine($"  {clock.ElapsedMilliseconds,6} ms  > {step.What}");
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"  {clock.ElapsedMilliseconds,6} ms  > {step.What}: the process no longer reads");
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }

            if (step.DelayMs > 0)
            {
                _ = Task.Delay(step.DelayMs).ContinueWith(_ => Now(), TaskScheduler.Default);
            }
            else
            {
                Now();
            }
        }

        while (pending.Count > 0 && pending.Peek().When is null)
        {
            Write(pending.Dequeue());
        }

        var timedOut = false;
        var reading = process.StandardOutput.ReadLineAsync();
        while (true)
        {
            if (!reading.Wait(1000))
            {
                if (clock.Elapsed.TotalSeconds > c.LimitSeconds)
                {
                    timedOut = true;
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    break;
                }

                continue;
            }

            var text = reading.Result;
            if (text is null)
            {
                break;
            }

            raw.Add(text);
            var read = ClaudeCodeStream.Read(text);
            lines.Add((clock.ElapsedMilliseconds, read));
            if (ClaudeCodeStream.ShapeOf(text) is { } shape)
            {
                Add(shapes, shape.Name, shape.Keys);
                foreach (var (kind, keys) in shape.Blocks)
                {
                    Add(blocks, kind, keys);
                }
            }

            // A reply's pieces are counted, not listed: there are many.
            if (!read.Has(StreamKind.Delta))
            {
                Console.WriteLine($"  {clock.ElapsedMilliseconds,6} ms  {Words(read)}");
            }

            if (pending.Count > 0 && pending.Peek().When is { } when && when(read))
            {
                Write(pending.Dequeue());
            }

            reading = process.StandardOutput.ReadLineAsync();
        }

        process.WaitForExit(15000);
        var said = errors.Wait(5000) ? errors.Result : string.Empty;
        var code = process.HasExited ? process.ExitCode : -1;
        Console.WriteLine($"  exit code {code} after {clock.ElapsedMilliseconds} ms" + (said.Length > 0 ? $"; {said.Length} characters on standard error" : string.Empty));

        var tools = new List<string>();
        if (File.Exists(log))
        {
            foreach (var entry in File.ReadAllLines(log))
            {
                // "tool<TAB>key,key": written by the permission tool below.
                var parts = entry.Split('\t');
                tools.Add(parts[0]);
                Add(asked, parts[0], parts.Length > 1 ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : []);
            }

            Console.WriteLine("  the permission tool was asked about: " + string.Join(", ", tools));
        }

        if (keep is not null)
        {
            Directory.CreateDirectory(keep);
            File.WriteAllLines(Path.Combine(keep, c.Name + ".jsonl"), raw);
        }

        return new Outcome(lines, tools, code, folder, timedOut);
    }

    /// <summary>A line in kinds and names: never a text.</summary>
    private static string Words(StreamLine line)
    {
        var parts = line.Parts.Select(part => part.Kind switch
        {
            StreamKind.ToolCall => "ToolCall " + part.Tool,
            StreamKind.ToolResult => part.Error ? "ToolResult, an error" : "ToolResult",
            StreamKind.Note => "Note " + part.Text,
            StreamKind.Unknown => "Unknown " + part.Text,
            StreamKind.Control => part.Error ? "Control, refused" : "Control, done",
            StreamKind.Ended => $"Ended, {(line.Failed ? "failed" : "well")}, Claude Code's word: {line.Terminal}, denials {line.Denials}, waiting {line.Queued}",
            StreamKind.Started => $"Started, Claude Code {line.Version}, mode {line.Mode}",
            StreamKind.Text => $"Text, {part.Text.Length} characters",
            _ => part.Kind.ToString(),
        });
        var text = string.Join("; ", parts);
        return (text.Length == 0 ? "nothing known" : text) + (line.FromWorker ? " (a subagent's)" : string.Empty);
    }

    private static string Notes(Outcome outcome)
    {
        var notes = outcome.Parts.Where(part => part.Kind is StreamKind.Note or StreamKind.Unknown).Select(part => part.Text).Distinct().Order(StringComparer.Ordinal).ToList();
        return notes.Count == 0 ? "none" : string.Join(", ", notes);
    }

    /// <summary>Whether parts of these kinds come in this order, other parts between them allowed.</summary>
    private static bool InOrder(Outcome outcome, params StreamKind[] kinds)
    {
        var next = 0;
        foreach (var part in outcome.Parts)
        {
            if (next < kinds.Length && part.Kind == kinds[next])
            {
                next++;
            }
        }

        return next == kinds.Length;
    }

    private static long SecondTurnMs(Outcome outcome)
    {
        var ends = outcome.Lines.Where(entry => entry.Line.Has(StreamKind.Ended)).Select(entry => entry.Ms).ToList();
        return ends.Count == 2 ? ends[1] - ends[0] : 0;
    }

    private static long InterruptMs(Outcome outcome)
    {
        var control = outcome.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Control)).Ms;
        var end = outcome.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Ended)).Ms;
        return end - control;
    }

    private static void Add(SortedDictionary<string, SortedSet<string>> into, string name, IEnumerable<string> keys)
    {
        if (!into.TryGetValue(name, out var set))
        {
            into[name] = set = new SortedSet<string>(StringComparer.Ordinal);
        }

        set.UnionWith(keys);
    }

    /// <summary>The shape file: for each kind of line its keys, for each kind of block its keys, for each tool the permission tool was asked about the keys of its input. One entry a line, so that a new Claude Code version shows as a short diff.</summary>
    private static string ShapeText(string version, SortedDictionary<string, SortedSet<string>> shapes, SortedDictionary<string, SortedSet<string>> blocks, SortedDictionary<string, SortedSet<string>> asked)
    {
        static string Section(SortedDictionary<string, SortedSet<string>> entries) =>
            string.Join(",\n", entries.Select(entry => $"    {JsonSerializer.Serialize(entry.Key)}: {JsonSerializer.Serialize(entry.Value)}"));

        return "{\n"
            + "  \"format\": \"axkit-stream-shape 1\",\n"
            + "  \"note\": \"Written by tools/ClaudeProbe stream --shape, never by hand: the keys of every kind of line Claude Code's non-interactive stream gave in the probe's cases, with no value. A key that comes only now and then (thinking) may be in one run and not the next: the cases' own expectations, not this file, say whether the stream still works.\",\n"
            + $"  \"claudeCode\": {JsonSerializer.Serialize(version)},\n"
            + "  \"lines\": {\n" + Section(shapes) + "\n  },\n"
            + "  \"blocks\": {\n" + Section(blocks) + "\n  },\n"
            + "  \"askedOfThePermissionTool\": {\n" + Section(asked) + "\n  }\n"
            + "}\n";
    }

    // ---- the stand-in permission tool: probe permission-tool ----

    /// <summary>A server of one MCP tool over standard input and output (JSON-RPC, one object a line), which answers a permission question as the environment says and writes what it was asked about, without a value, to the log.</summary>
    public static int PermissionTool()
    {
        var mode = Environment.GetEnvironmentVariable(PermissionVariable) ?? string.Empty;
        var log = Environment.GetEnvironmentVariable(LogVariable);
        var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
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

                var method = root.TryGetProperty("method", out var named) ? named.GetString() : null;
                var idText = id.GetRawText();
                switch (method)
                {
                    case "initialize":
                        var protocol = root.TryGetProperty("params", out var asked) && asked.TryGetProperty("protocolVersion", out var wanted) ? wanted.GetRawText() : "\"2024-11-05\"";
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"protocolVersion\":{protocol},\"capabilities\":{{\"tools\":{{}}}},\"serverInfo\":{{\"name\":\"axprobe\",\"version\":\"1\"}}}}}}");
                        break;
                    case "tools/list":
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"tools\":[{{\"name\":\"approve\",\"description\":\"Answers a permission question.\",\"inputSchema\":{{\"type\":\"object\",\"properties\":{{\"tool_name\":{{\"type\":\"string\"}},\"input\":{{\"type\":\"object\"}},\"tool_use_id\":{{\"type\":\"string\"}}}},\"required\":[\"tool_name\",\"input\"]}}}}]}}}}");
                        break;
                    case "tools/call":
                        var answer = Answer(root, mode, log);
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":{JsonSerializer.Serialize(answer)}}}]}}}}");
                        break;
                    default:
                        output.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{idText},\"result\":{{}}}}");
                        break;
                }
            }
        }

        return 0;
    }

    private static string Answer(JsonElement call, string mode, string? log)
    {
        if (!call.TryGetProperty("params", out var parameters) || !parameters.TryGetProperty("arguments", out var arguments))
        {
            return ClaudeCodeStream.DenyAnswer("The probe did not understand the question.");
        }

        var tool = arguments.TryGetProperty("tool_name", out var named) ? named.GetString() ?? string.Empty : string.Empty;
        var hasInput = arguments.TryGetProperty("input", out var input) && input.ValueKind == JsonValueKind.Object;
        if (log is not null)
        {
            var keys = hasInput ? string.Join(",", input.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)) : string.Empty;
            File.AppendAllText(log, tool + "\t" + keys + "\n");
        }

        if (!hasInput)
        {
            return ClaudeCodeStream.DenyAnswer("The probe was given no input.");
        }

        if (tool == ClaudeCodeStream.QuestionTool && input.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
        {
            // The first option of each question, as a user who presses Enter on the list would choose.
            var answers = new Dictionary<string, string>();
            foreach (var question in questions.EnumerateArray())
            {
                var text = question.TryGetProperty("question", out var asked) ? asked.GetString() ?? string.Empty : string.Empty;
                var first = question.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array && options.GetArrayLength() > 0
                    && options[0].TryGetProperty("label", out var label) ? label.GetString() ?? string.Empty : string.Empty;
                answers[text] = first;
            }

            return ClaudeCodeStream.QuestionAnswer(input, answers);
        }

        return mode == "deny" ? ClaudeCodeStream.DenyAnswer("The user said no.") : ClaudeCodeStream.AllowAnswer(input);
    }
}
