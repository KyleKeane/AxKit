// probe stream: Claude Code's non-interactive stream, measured (docs/axClaude/stream.md). A fixed set of cases is
// played against the installed Claude Code, each a process started with ClaudeCodeStream.Options: messages written on
// standard input, every line of standard output read through ClaudeCodeStream.Read, and what the case expects held to
// the lines. It prints kinds, names and times only: never a text Claude wrote, a path or an id. It is the check to run
// when Claude Code changes (tools\upstream.ps1 names it), and what it prints for a new version is compared with
// docs/axClaude/stream-shape.json, the shape of every kind of line seen, written with --shape.
//
//   probe stream [--only <case>] [--shape <file>] [--keep <folder>] [--model <name>]
//
// Every run uses the smallest model unless --model says otherwise, saves no session (ClaudeCodeStream.NoSessionOption;
// the one case that takes a session up again saves it, and the probe deletes that session's file from Claude Code's
// store when the case is over), loads no MCP server but its own stand-in permission tool, and works in a folder of
// its own under the temporary folder, deleted at the end. A yes that lasts is only ever a rule of the session's: no
// case writes a settings file, and none uses a mode that asks nothing. --keep copies the raw lines there for the developer's eyes; they hold what Claude wrote
// and the session's ids, and are never committed.
//
// The stand-in permission tool is this program again, "probe permission-tool": a server of one MCP tool over standard
// input and output, which answers as the variable AXKIT_PROBE_PERMISSION says (allow or deny; a question of Claude's
// gets the first option of each question) and writes the name of the tool asked about, with the keys of its input, to
// the file AXKIT_PROBE_LOG.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AxClaude.Core.Record;
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
        /// <summary>When the case's process was started, in UTC ticks: what the permission tool's own times are counted from.</summary>
        public long StartedUtcTicks { get; init; }

        /// <summary>The permission tool's log of the case.</summary>
        public string ToolLog { get; init; } = string.Empty;

        public IEnumerable<StreamPart> Parts => Lines.SelectMany(entry => entry.Line.Parts);

        public int Count(StreamKind kind) => Parts.Count(part => part.Kind == kind);

        public bool Says(string word) => Parts.Any(part => part.Kind == StreamKind.Text && part.Text.Contains(word, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<StreamLine> Ends => Lines.Select(entry => entry.Line).Where(line => line.Has(StreamKind.Ended));

        /// <summary>The session's id as the first start line gave it; kept in memory for a second process to take up, never printed.</summary>
        public string Session => Lines.Select(entry => entry.Line.Session).FirstOrDefault(id => id.Length > 0) ?? string.Empty;

        /// <summary>The slash commands the start line listed. The user's own are among them: only asked about by a name the probe gives, never printed as a list.</summary>
        public IReadOnlyList<string> Commands => Lines.Select(entry => entry.Line.Commands).FirstOrDefault(list => list.Count > 0) ?? [];

        public bool Noted(string subtype) => Parts.Any(part => part.Kind == StreamKind.Note && part.Text == subtype);
    }

    /// <summary>
    /// One case: its options beside the stream's own, how the permission tool answers (empty: no tool), its steps, what
    /// must hold (each a sentence said when it does not), and what is only reported. <see cref="Keeps"/>: the session is
    /// saved in Claude Code's store, for a case that takes it up again; the probe deletes the session's file when the
    /// case is over. <see cref="Next"/>: a second process played after this one in the same folder, made from what
    /// the first saw.
    /// </summary>
    /// <param name="Relay">
    /// For a case of axKit's own relay (<see cref="AskRelay"/>, the verb <c>ask</c>, which this program runs too): how
    /// the probe, playing the window's end of the pipe, answers (<c>allow</c>, <c>deny</c>), or <c>gone</c> for no
    /// window at all. Claude Code is then given the relay's own configuration (<see cref="AskRelay.ConfigText"/>)
    /// and the pipe in its environment, as a window will give them. Empty for every other case.
    /// </param>
    private sealed record Case(string Name, string What, string[] Options, string Permission, Step[] Steps, Func<Outcome, IEnumerable<string>> Problems, Func<Outcome, IEnumerable<string>>? Findings = null, int LimitSeconds = 90, bool Keeps = false, Func<Outcome, Case>? Next = null, string Relay = "");

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

        // ---- What docs/axClaude/stream.md, section 6, listed on 2026-11-17 as not yet measured ----

        new("long-reply", "a long reply's pieces against the clock", [ClaudeCodeStream.PartialOption], string.Empty,
            [Say("Write about 300 words on lighthouses, in plain sentences, with no list and no heading."), Close(Ended)],
            o => Unless(o.Count(StreamKind.Delta) > 0, "pieces should arrive before the whole message")
                .Concat(Unless(o.Ends.Any() && !o.Ends.Last().Failed, "the turn should end well")),
            o => [Pieces(o)],
            LimitSeconds: 120),

        new("resume", "a saved session taken up again by a second process", [], string.Empty,
            [Say("Remember this word for later: kiwi. Reply with exactly: noted"), Close(Ended)],
            o => Unless(o.Session.Length > 0, "the start line should name the session")
                .Concat(Unless(o.Ends.Any() && !o.Ends.Last().Failed, "the first turn should end well")),
            Keeps: true,
            Next: first => new("resume, the second process", "the same session, named by its id", [ClaudeCodeStream.ResumeOption, first.Session], string.Empty,
                [Say("Which word did I ask you to remember? Reply with that word alone."), Close(Ended)],
                o => Unless(o.Says("kiwi"), "the second process should know the word given to the first"),
                o => [$"the session's id was the same: {(o.Session == first.Session ? "yes" : "no")}; the first run's reply came again as a line: {(o.Says("noted") ? "yes" : "no")}; lines of the user's before the answer: {o.Parts.Count(part => part.Kind == StreamKind.Note && part.Text == "user text")}; the process was up after {StartedMs(o)} ms"],
                Keeps: true)),

        new("replay", "each message written back as an acknowledgement", [ClaudeCodeStream.ReplayOption], string.Empty,
            [Say("Reply with exactly: ok"), Close(Ended)],
            o => Unless(o.Says("ok"), "the reply should arrive"),
            o => [$"the message came back before the reply: {(InOrder(o, StreamKind.Message, StreamKind.Text) ? "yes" : "no")}, {UserTextMs(o)} ms after the start"]),

        new("continue-nothing", "taking up the last session in a folder that has none (reported)", [ClaudeCodeStream.ContinueOption], string.Empty,
            [Say("Reply with exactly: ok"), Close(Ended)],
            _ => [],
            o => [$"exit code {o.ExitCode}; the process said it was up: {(o.Count(StreamKind.Started) > 0 ? "yes" : "no")}; turns ended: {o.Ends.Count()}, failed: {o.Ends.Count(end => end.Failed)}; the reply arrived: {(o.Says("ok") ? "yes" : "no")}", "Claude Code's own notes seen: " + Notes(o)],
            LimitSeconds: 60),

        new("interrupt-idle", "an interrupt while no turn runs", [], string.Empty,
            [
                Say("Reply with exactly: ok"),
                new(Ended, 300, ClaudeCodeStream.InterruptLine("probe-2"), "an interrupt, with nothing running"),
                Say("Reply with exactly: after", line => line.Has(StreamKind.Control), 300, "a message after it"),
                Close(Ended),
            ],
            o => Unless(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-2"), "the interrupt should be answered with its id")
                .Concat(Unless(o.Says("after"), "the process should answer a message after the interrupt"))
                .Concat(Unless(o.ExitCode == 0, $"the process ended with code {o.ExitCode}")),
            o => [$"the interrupt was {(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-2" && !part.Error) ? "taken" : "refused")}; turns ended: {o.Ends.Count()}, failed: {o.Ends.Count(end => end.Failed)}"]),

        new("mode", "the permission mode changed by the host, as Shift+Tab does in the interactive program", Asking, "deny",
            [
                new(null, 0, ClaudeCodeStream.ModeLine("probe-3", "acceptEdits"), "the permission mode set to acceptEdits"),
                Say("Use the Write tool to create a file named probe-mode.txt in the current folder with the content hi. Then reply with exactly: written", line => line.Has(StreamKind.Control), 200),
                Close(Ended),
            ],
            o => Unless(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-3"), "the request should be answered with its id"),
            o => [$"the request was {(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-3" && !part.Error) ? "taken" : "refused")}; the file was made without the permission tool being asked: {(File.Exists(Path.Combine(o.Folder, "probe-mode.txt")) && !o.Asked.Contains("Write") ? "yes" : "no")} (asked about: {(o.Asked.Count == 0 ? "nothing" : string.Join(", ", o.Asked))})", "Claude Code's own notes seen: " + Notes(o)]),

        new("permission-always", "a yes for the rest of the session: the same tool is not asked about again", Asking, "always",
            [Say("Use the Write tool to create a file named always-1.txt in the current folder with the content hi. After that, use the Write tool again to create a file named always-2.txt with the content hi. Then reply with exactly: both written"), Close(Ended)],
            o => Unless(File.Exists(Path.Combine(o.Folder, "always-1.txt")) && File.Exists(Path.Combine(o.Folder, "always-2.txt")), "both files should be made"),
            o => [$"the permission tool was asked about Write {o.Asked.Count(tool => tool == "Write")} time(s) for two files (1: the first yes held for the session)"]),

        // Measured 2026-11-17: a message that is a slash command is run as that command, with no call of the model:
        // what it prints comes as a text of Claude's, and a line ends it as a turn, with no word for how it ended.
        new("slash-command", "slash commands written as messages: one that prints, the model, a new conversation", [], string.Empty,
            [
                Say("/context", what: "the slash command /context"),
                Say("/model haiku", Ended, 200, "the slash command /model with a name"),
                Say("/clear", Ended, 200, "the slash command /clear"),
                Say("Reply with exactly: ok", Ended, 200, "a message after them"),
                Close(Ended),
            ],
            o => Unless(o.Ends.Count() == 4 && !o.Ends.Any(end => end.Failed), $"four turns should end well, one for each command and the message; {o.Ends.Count()} ended, {o.Ends.Count(end => end.Failed)} failed")
                .Concat(Unless(o.Count(StreamKind.Text) >= 2, "the command that prints and the message should each bring a text"))
                .Concat(Unless(o.Says("ok"), "the message after the commands should be answered")),
            o => [$"the start line lists {o.Commands.Count} slash commands; among them compact: {Listed(o, "compact")}, clear: {Listed(o, "clear")}, model: {Listed(o, "model")}, context: {Listed(o, "context")}, cost: {Listed(o, "cost")}",
                $"ids of the session seen on start lines: {o.Lines.Select(entry => entry.Line.Session).Where(id => id.Length > 0).Distinct().Count()} (2: /clear began a new session)",
                "Claude Code's own notes seen: " + Notes(o)],
            LimitSeconds: 120),

        // Measured 2026-11-17: a status note when the compaction starts and one when it is over, then the boundary.
        new("compact", "the conversation compacted by its slash command, and still known after", [], string.Empty,
            [
                Say("Remember this word for later: mango. Reply with exactly: noted"),
                Say("/compact", Ended, 200, "the slash command /compact"),
                Say("Which word did I ask you to remember? Reply with that word alone.", Ended, 300, "a message after the compaction"),
                Close(Ended),
            ],
            o => Unless(o.Noted("compact_boundary"), "a note should mark where the compaction cut")
                .Concat(Unless(o.Lines.Any(entry => entry.Line.Status == "compacting"), "a status note should say that Claude Code is compacting"))
                .Concat(Unless(o.Says("mango"), "the word given before the compaction should still be known"))
                .Concat(Unless(o.Ends.Count() == 3 && !o.Ends.Any(end => end.Failed), $"three turns should end well; {o.Ends.Count()} ended")),
            o => [$"the compaction took {CompactionMs(o)} ms from its status note to its boundary"],
            LimitSeconds: 150),

        // What a host can learn before the first message (stream-mode.md SM-4.10): the request the Agent SDK sends
        // first. Reported, not required: the command line documents no such request. What the answer holds is read
        // from a kept copy by its keys' names alone (it may name the account).
        new("initialize", "the request a host may send before any message (reported)", [], string.Empty,
            [
                new(null, 0, ClaudeCodeStream.InitializeLine("probe-5"), "an initialize request"),
                Say("Reply with exactly: ok", line => line.Has(StreamKind.Control), 200, "a message after it"),
                Close(Ended),
            ],
            o => Unless(o.Says("ok"), "the message after the request should be answered"),
            o => [$"the request was {(o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-5" && !part.Error) ? "taken" : o.Parts.Any(part => part.Kind == StreamKind.Control && part.Text == "probe-5") ? "refused" : "not answered")}, {o.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Control)).Ms} ms after the start; lines before the first message's start line: {o.Lines.TakeWhile(entry => !entry.Line.Has(StreamKind.Started)).Count()}"],
            LimitSeconds: 60),

        // ---- axKit's own relay (docs/axClaude/stream-mode.md SM-7.3), with the probe as the window's end of its pipe ----

        new("relay", "a question through axKit's own relay, answered yes by a stand-in of the window", ["--permission-mode", "default", ClaudeCodeStream.PermissionToolOption, AskRelay.ToolName], string.Empty,
            [Say("Use the Write tool to create a file named relay-allowed.txt in the current folder with the content hi. Then reply with exactly: written"), Close(Ended)],
            o => Unless(o.Asked.Contains("Write"), "the window's stand-in should be asked about Write through the relay")
                .Concat(Unless(File.Exists(Path.Combine(o.Folder, "relay-allowed.txt")), "the file should be made after the window's yes"))
                .Concat(Unless(o.Ends.Any() && o.Ends.Last().Denials == 0, "no denial should be counted")),
            Relay: "allow"),

        new("relay-refused", "the same, answered no by the stand-in of the window", ["--permission-mode", "default", ClaudeCodeStream.PermissionToolOption, AskRelay.ToolName], string.Empty,
            [Say("Use the Write tool to create a file named relay-refused.txt in the current folder with the content hi. Then say in one short sentence whether it was created."), Close(Ended)],
            o => Unless(o.Asked.Contains("Write"), "the window's stand-in should be asked about Write through the relay")
                .Concat(Unless(!File.Exists(Path.Combine(o.Folder, "relay-refused.txt")), "the file must not be made after the window's no"))
                .Concat(Unless(o.Ends.Any() && o.Ends.Last().Denials == 1, "one denial should be counted at the turn's end")),
            Relay: "deny"),

        new("relay-gone", "the relay with no window to ask: its answer is no", ["--permission-mode", "default", ClaudeCodeStream.PermissionToolOption, AskRelay.ToolName], string.Empty,
            [Say("Use the Write tool to create a file named relay-gone.txt in the current folder with the content hi. Then say in one short sentence whether it was created."), Close(Ended)],
            o => Unless(!File.Exists(Path.Combine(o.Folder, "relay-gone.txt")), "the file must not be made when the window cannot be asked")
                .Concat(Unless(o.Parts.Any(part => part.Kind == StreamKind.ToolResult && part.Error), "the tool's result should be an error")),
            Relay: "gone"),
    ];

    private static IEnumerable<string> EndedAtLeast(Outcome outcome, int turns) =>
        Unless(outcome.Ends.Count() >= turns, $"{turns} turn(s) should end, {outcome.Ends.Count()} did");

    /// <summary>
    /// The cases recorded as stream fixtures (<c>--record</c>, docs/axClaude/stream-mode.md SM-10.1): what the
    /// stream's model is built and tested from. Each is played with what the window will start Claude Code with: the
    /// stream's own options, each message sent back when it is taken (<see cref="ClaudeCodeStream.ReplayOption"/>),
    /// and the permission tool for every question, answered as the case says (allow, deny, always; a question of
    /// Claude's gets the first option of each question). Their options here are added to those. What a case expects
    /// is only that its turns end: what happened is the fixture, read through before it is committed. Nothing of them
    /// prints an account, a path of the user's or a list of the user's own commands.
    /// </summary>
    private static readonly Case[] Recordings =
    [
        new("start", "the request a window sends first, answered before any message, then a turn", [], "allow",
            [
                new(null, 0, ClaudeCodeStream.InitializeLine("host-0"), "the first request"),
                Say("Reply with exactly: ok", line => line.Has(StreamKind.Control), 400, "a message after it"),
                Close(Ended),
            ],
            o => EndedAtLeast(o, 1)),

        new("turn", "one message and its reply", [], "allow",
            [Say("Reply with exactly: ok"), Close(Ended)], o => EndedAtLeast(o, 1)),

        new("two-turns", "a second message after the first turn's end", [], "allow",
            [Say("Reply with exactly: one"), Say("Reply with exactly: two", Ended), Close(Ended)], o => EndedAtLeast(o, 2)),

        new("headings", "a reply written with headings, a list and code", [], "allow",
            [Say("Write a short answer about the colour blue in Markdown: a level-two heading, two sentences with one word in bold, a list of three items, a second level-two heading, and one line of code in a fenced block. Use no tool."), Close(Ended)],
            o => EndedAtLeast(o, 1)),

        new("tool", "a tool call the user allows", [], "allow",
            [Say("Run this command with the Bash tool: ping -n 2 127.0.0.1\nAfter it finishes, reply with exactly: pinged"), Close(Ended)], o => EndedAtLeast(o, 1)),

        new("denied", "a tool call the user refuses", [], "deny",
            [Say("Use the Write tool to create a file named probe-denied.txt in the current folder with the content hi. Then say in one short sentence whether it was created."), Close(Ended)],
            o => EndedAtLeast(o, 1)),

        new("always", "a yes for the rest of the session", [], "always",
            [Say("Use the Write tool to create a file named always-1.txt in the current folder with the content hi. After that, use the Write tool again to create a file named always-2.txt with the content hi. Then reply with exactly: both written"), Close(Ended)],
            o => EndedAtLeast(o, 1)),

        new("question", "a question of Claude's", [], "allow",
            [Say("Use the AskUserQuestion tool to ask me one question: which colour I prefer, with the options Red and Blue. Then reply with the colour I chose and nothing else."), Close(Ended)],
            o => EndedAtLeast(o, 1)),

        new("two-questions", "two questions in one call, the second with several answers", [], "allow",
            [Say("Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry). Then reply with exactly: thanks"), Close(Ended)],
            o => EndedAtLeast(o, 1)),

        new("interrupt", "a running turn ended by the host, and a message after it", [], "allow",
            [
                Say("Run this command with the Bash tool: ping -n 20 127.0.0.1\nAfter it finishes, reply with exactly: finished"),
                new(Calls, 3500, ClaudeCodeStream.InterruptLine("host-1"), "an interrupt"),
                Say("Reply with exactly: still here", Ended, 300, "a message after the interrupt"),
                Close(Ended),
            ],
            o => EndedAtLeast(o, 2)),

        new("mid-turn", "a second message written while a tool runs", [], "allow",
            [
                Say("Run this command with the Bash tool: ping -n 5 127.0.0.1\nAfter it finishes, reply with exactly: first done"),
                Say("Reply with exactly: second answer", Calls, 2000, "a second message, while the tool runs"),
                Close(Ended, 9000),
            ],
            o => EndedAtLeast(o, 1), LimitSeconds: 120),

        new("while-writing", "a second message written while a long reply is written", [], "allow",
            [
                Say("Write about 200 words on lighthouses, in plain sentences, with no list and no heading. Use no tool."),
                Say("Reply with exactly: second", line => line.Has(StreamKind.Started), 1500, "a second message, while the reply is written"),
                Close(Ended, 12000),
            ],
            o => EndedAtLeast(o, 1), LimitSeconds: 120),

        new("mode", "the permission mode set by the host", [], "deny",
            [
                new(null, 0, ClaudeCodeStream.ModeLine("host-2", "acceptEdits"), "the permission mode set to acceptEdits"),
                Say("Use the Write tool to create a file named probe-mode.txt in the current folder with the content hi. Then reply with exactly: written", line => line.Has(StreamKind.Control), 200),
                Close(Ended),
            ],
            o => EndedAtLeast(o, 1)),

        new("compact", "the conversation compacted by its slash command", [], "allow",
            [
                Say("Remember this word for later: mango. Reply with exactly: noted"),
                Say("/compact", Ended, 200, "the slash command /compact"),
                Say("Which word did I ask you to remember? Reply with that word alone.", Ended, 300, "a message after the compaction"),
                Close(Ended),
            ],
            o => EndedAtLeast(o, 3), LimitSeconds: 150),

        new("clear", "the conversation emptied by its slash command", [], "allow",
            [
                Say("Reply with exactly: before"),
                Say("/clear", Ended, 200, "the slash command /clear"),
                Say("Reply with exactly: after", Ended, 300, "a message after it"),
                Close(Ended),
            ],
            o => EndedAtLeast(o, 3), LimitSeconds: 120),

        new("model", "a slash command that answers with one line", [], "allow",
            [Say("/model haiku", what: "the slash command /model with a name"), Say("Reply with exactly: ok", Ended, 200), Close(Ended)],
            o => EndedAtLeast(o, 2)),

        new("worker", "work handed to a subagent, and the turn Claude Code starts by itself for its hand-back", [], "allow",
            [Say("Use the Agent tool to start a general-purpose subagent whose whole task is to reply with exactly: from the worker. Then reply with exactly: handed over"), Close(Ended, 25000)],
            o => EndedAtLeast(o, 1), LimitSeconds: 150),

        new("background", "a command run in the background", [], "allow",
            [Say("Use the Bash tool with run_in_background set to true to run the command: ping -n 6 127.0.0.1. Do not wait for it. Then reply with exactly: started"), Close(Ended, 15000)],
            o => EndedAtLeast(o, 1), LimitSeconds: 120),

        new("failure", "a turn that fails: a model that does not exist", ["--model", "no-such-model-of-the-probe"], "allow",
            [Say("Reply with exactly: ok"), Close(Ended)], _ => [], LimitSeconds: 60),
    ];

    /// <summary>From the status note that says compacting to the note of the boundary.</summary>
    private static long CompactionMs(Outcome outcome)
    {
        var from = outcome.Lines.FirstOrDefault(entry => entry.Line.Status == "compacting").Ms;
        var to = outcome.Lines.FirstOrDefault(entry => entry.Line.Parts.Any(part => part.Kind == StreamKind.Note && part.Text == "compact_boundary")).Ms;
        return to - from;
    }

    private static string Listed(Outcome outcome, string command) => outcome.Commands.Contains(command) ? "yes" : "no";

    private static long StartedMs(Outcome outcome) => outcome.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Started)).Ms;

    private static long UserTextMs(Outcome outcome) =>
        outcome.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Message)).Ms;

    /// <summary>A reply's pieces in numbers: how many, how much text, when the first and the last came, the longest wait between two, and the pace.</summary>
    private static string Pieces(Outcome outcome)
    {
        var pieces = outcome.Lines
            .Select(entry => (entry.Ms, Characters: entry.Line.Parts.Where(part => part.Kind == StreamKind.Delta).Sum(part => part.Text.Length)))
            .Where(piece => piece.Characters > 0)
            .ToList();
        if (pieces.Count < 2)
        {
            return $"pieces with text: {pieces.Count}";
        }

        long longest = 0;
        for (var i = 1; i < pieces.Count; i++)
        {
            longest = Math.Max(longest, pieces[i].Ms - pieces[i - 1].Ms);
        }

        var characters = pieces.Sum(piece => piece.Characters);
        var span = pieces[^1].Ms - pieces[0].Ms;
        var whole = outcome.Lines.FirstOrDefault(entry => entry.Line.Has(StreamKind.Text)).Ms;
        return $"pieces with text: {pieces.Count}, {characters} characters in all; the first {pieces[0].Ms} ms after the start, the last at {pieces[^1].Ms} ms; "
            + $"the longest wait between two pieces {longest} ms; {(span > 0 ? characters * 1000 / span : 0)} characters a second while it was written; the whole message's line at {whole} ms";
    }

    /// <summary>
    /// The sessions a case saved are taken out of Claude Code's store again. Each is the probe's own: the session of
    /// a folder under the temporary folder that this run made, found by the id its start line gave and by nothing else.
    /// </summary>
    private static void Forget(string folder, List<string> sessions)
    {
        if (sessions.Count == 0)
        {
            return;
        }

        var environment = new Dictionary<string, string>();
        if (Environment.GetEnvironmentVariable(ConfigFolderVariable) is { Length: > 0 } configured)
        {
            environment[ConfigFolderVariable] = configured;
        }

        var removed = 0;
        foreach (var projects in ClaudeCodeTranscript.ProjectsFolders(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), environment, null))
        {
            var project = Path.Combine(projects, ClaudeCodeTranscript.ProjectFolderName(folder));
            if (!Directory.Exists(project))
            {
                continue;
            }

            foreach (var session in sessions.Distinct())
            {
                var file = Path.Combine(project, session + ClaudeCodeTranscript.SessionFileExtension);
                if (ClaudeCodeTranscript.IsSessionFile(file, [projects]) && File.Exists(file))
                {
                    File.Delete(file);
                    removed++;
                }

                // What Claude Code keeps beside a session under its id (a subagent's lines, large results).
                var beside = Path.Combine(project, session);
                if (Directory.Exists(beside))
                {
                    Directory.Delete(beside, recursive: true);
                }
            }

            if (!Directory.EnumerateFileSystemEntries(project).Any())
            {
                Directory.Delete(project);
            }
        }

        Console.WriteLine($"  the probe's own saved sessions were taken out of Claude Code's store again: {removed} of {sessions.Distinct().Count()}");
    }

    /// <summary>The variable that names Claude Code's configuration folder when the user moved it.</summary>
    private const string ConfigFolderVariable = "CLAUDE_CONFIG_DIR";

    public static int Run(string[] args)
    {
        string? only = null, shapeFile = null, keep = null, recordFolder = null;
        var model = "haiku";
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--only" when i + 1 < args.Length: only = args[++i]; break;
                case "--shape" when i + 1 < args.Length: shapeFile = args[++i]; break;
                case "--keep" when i + 1 < args.Length: keep = args[++i]; break;
                case "--model" when i + 1 < args.Length: model = args[++i]; break;
                case "--record" when i + 1 < args.Length: recordFolder = args[++i]; break;
                default:
                    Console.Error.WriteLine("Usage: probe stream [--only <case>] [--shape <file>] [--keep <folder>] [--model <name>]");
                    Console.Error.WriteLine("       probe stream --record <fixtures folder> [--only <case>] [--model <name>]");
                    Console.Error.WriteLine("Cases: " + string.Join(", ", Cases.Select(c => c.Name)));
                    Console.Error.WriteLine("Recorded cases: " + string.Join(", ", Recordings.Select(c => c.Name)));
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
        var refused = new List<string>();
        var notKept = new SortedSet<string>(StringComparer.Ordinal);
        try
        {
            // The stand-in permission tool is this program: its path goes into a file of the work folder only.
            var relayConfig = Path.Combine(work, "relay.json");
            var config = Path.Combine(work, "mcp.json");
            File.WriteAllText(config, JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["mcpServers"] = new Dictionary<string, object>
                {
                    ["axprobe"] = new Dictionary<string, object> { ["command"] = Environment.ProcessPath!, ["args"] = new[] { "permission-tool" } },
                },
            }));

            foreach (var first in (recordFolder is null ? Cases : Recordings).Where(c => only is null || c.Name == only))
            {
                var folder = Path.Combine(work, first.Name);
                Directory.CreateDirectory(folder);
                var sessions = new List<string>();
                // A case, and then the process it asks for after it, in the same folder.
                for (var c = first; c is not null;)
                {
                    Console.WriteLine($"== {c.Name}: {c.What}");
                    // A case's own model option (the one that does not exist) goes last, so that it wins. A recording
                    // is played with what the window will start Claude Code with: each message sent back when it is
                    // taken, and the permission tool for every question.
                    // A case of the relay is given the relay's own configuration, as a window will write it.
                    if (c.Relay.Length > 0)
                    {
                        File.WriteAllText(relayConfig, AskRelay.ConfigText(Environment.ProcessPath!));
                    }

                    string[] options = recordFolder is null
                        ? [.. ClaudeCodeStream.Options, .. c.Keeps ? Array.Empty<string>() : [ClaudeCodeStream.NoSessionOption], ClaudeCodeProgram.StrictMcpConfigOption, "--mcp-config", c.Relay.Length > 0 ? relayConfig : config, "--model", model, .. c.Options]
                        : [.. ClaudeCodeStream.Options, ClaudeCodeStream.ReplayOption, ClaudeCodeStream.NoSessionOption, ClaudeCodeProgram.StrictMcpConfigOption, "--mcp-config", config, "--model", model, .. Asking, .. c.Options];
                    var record = recordFolder is null ? null : new List<(long Ms, string Kind, string Text)>();
                    var outcome = Play(claude, options, c, folder, shapes, blocks, asked, keep, record);
                    if (record is not null && !outcome.TimedOut
                        && !StreamFixtureMaker.Write(recordFolder!, c.Name, outcome.Lines.Select(entry => entry.Line.Version).FirstOrDefault(v => v.Length > 0) ?? version, record, outcome.ToolLog, outcome.StartedUtcTicks, folder, c.Permission, outcome.ExitCode, notKept))
                    {
                        refused.Add(c.Name);
                    }

                    version = outcome.Lines.Select(entry => entry.Line.Version).FirstOrDefault(v => v.Length > 0) ?? version;
                    if (c.Keeps && outcome.Session.Length > 0)
                    {
                        sessions.Add(outcome.Session);
                    }

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
                        if (!failed.Contains(first.Name))
                        {
                            failed.Add(first.Name);
                        }

                        foreach (var problem in problems)
                        {
                            Console.WriteLine("  FAILED: " + problem);
                        }
                    }

                    c = problems.Count == 0 && c.Next is not null ? c.Next(outcome) : null;
                }

                Forget(folder, sessions);
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

        if (notKept.Count > 0)
        {
            // Names of keys, never a value: what a fixture leaves out, so that a field the model needs is added by name.
            Console.WriteLine("Keys the fixtures do not keep: " + string.Join(", ", notKept));
        }

        if (refused.Count > 0)
        {
            // The whitelist's own check found something personal left in a fixture and deleted it: record again.
            Console.WriteLine($"Claude Code {version}: fixtures refused: {string.Join(", ", refused)}.");
            return 4;
        }

        Console.WriteLine(failed.Count == 0
            ? $"Claude Code {version}: every case of the stream passed."
            : $"Claude Code {version}: FAILED: {string.Join(", ", failed)}.");
        return failed.Count == 0 ? 0 : 1;
    }

    /// <param name="record">For a recording: every line read (<c>out</c>) and every line written (<c>in</c>, empty for standard input closed) is added with its time, as it is, for the fixture's maker to reduce. Never written to a file here.</param>
    private static Outcome Play(string claude, string[] options, Case c, string folder,
        SortedDictionary<string, SortedSet<string>> shapes, SortedDictionary<string, SortedSet<string>> blocks, SortedDictionary<string, SortedSet<string>> asked, string? keep,
        List<(long Ms, string Kind, string Text)>? record = null)
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
        // A case of the relay: the pipe and its secret go into Claude Code's environment, which the relay it starts
        // inherits, and the probe plays the window's end, or nobody does.
        var throughRelay = new List<string>();
        if (c.Relay.Length > 0)
        {
            var pipeName = AskRelay.NewPipeName(System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? "probe");
            var secret = AxClaude.Core.Hooks.HookFrame.NewSecret();
            start.Environment[AskRelay.Variable] = AskRelay.VariableValue(pipeName, secret);
            if (c.Relay != "gone")
            {
                PlayWindow(pipeName, secret, c.Relay, throughRelay);
            }
        }

        var startedUtcTicks = DateTime.UtcNow.Ticks;
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

                        record?.Add((clock.ElapsedMilliseconds, "in", step.Line ?? string.Empty));
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
            if (record is not null)
            {
                // The host's lines are added on another thread, under the same lock.
                lock (gate)
                {
                    record.Add((clock.ElapsedMilliseconds, "out", text));
                }
            }

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
                // "tool<TAB>key,key<TAB>key,key": written by the permission tool below: the tool, its input's keys, the question's own keys.
                var parts = entry.Split('\t');
                tools.Add(parts[0]);
                Add(asked, parts[0], parts.Length > 1 ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : []);
                if (parts.Length > 2)
                {
                    Add(asked, "(the question itself)", parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            Console.WriteLine("  the permission tool was asked about: " + string.Join(", ", tools));
            if (asked.TryGetValue("(the question itself)", out var own))
            {
                Console.WriteLine("  the question itself came with the keys: " + string.Join(", ", own));
            }
        }

        lock (throughRelay)
        {
            if (throughRelay.Count > 0)
            {
                tools.AddRange(throughRelay);
                Console.WriteLine("  the window's stand-in was asked through the relay about: " + string.Join(", ", throughRelay));
            }
        }

        if (keep is not null)
        {
            Directory.CreateDirectory(keep);
            File.WriteAllLines(Path.Combine(keep, c.Name + ".jsonl"), raw);
        }

        return new Outcome(lines, tools, code, folder, timedOut) { StartedUtcTicks = startedUtcTicks, ToolLog = log };
    }

    /// <summary>
    /// The window's end of the relay's pipe, played by the probe on a thread of its own: each question is read as
    /// one frame, its tool noted by name, and answered as the case says, with the words a window will answer with.
    /// The pipe is open to this Windows account alone. The thread ends with the probe.
    /// </summary>
    private static void PlayWindow(string pipeName, string secret, string answerWith, List<string> asked)
    {
        var window = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    using var server = new System.IO.Pipes.NamedPipeServerStream(pipeName, System.IO.Pipes.PipeDirection.InOut, 1, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.CurrentUserOnly);
                    server.WaitForConnection();
                    if (AxClaude.Core.Hooks.HookFrame.Read(server, secret, out _) is not { } question)
                    {
                        continue;
                    }

                    using var document = JsonDocument.Parse(question);
                    var root = document.RootElement;
                    lock (asked)
                    {
                        asked.Add(root.TryGetProperty("tool_name", out var named) ? named.GetString() ?? string.Empty : string.Empty);
                    }

                    var answer = answerWith == "deny" || !root.TryGetProperty("input", out var input)
                        ? ClaudeCodeStream.DenyAnswer(ClaudeCodeStream.RefusedWords)
                        : ClaudeCodeStream.AllowAnswer(input);
                    AxClaude.Core.Hooks.HookFrame.Write(server, secret, Encoding.UTF8.GetBytes(answer));
                    server.WaitForPipeDrain();
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or ObjectDisposedException or InvalidOperationException)
            {
                // The case is over, or a frame was not one: the stand-in has nothing more to answer.
            }
        })
        {
            IsBackground = true,
            Name = "probe window stand-in",
        };
        window.Start();
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
            // The tool asked about, the keys of its input, and the keys of the question itself (does it bring suggestions?): names, no value.
            var keys = hasInput ? string.Join(",", input.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)) : string.Empty;
            var own = string.Join(",", arguments.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            // Then, for a recording alone: the time and the question as it came, on one line. The log is the case's
            // own, in its folder under the temporary folder, and goes with it; only the fixture's maker reads these two.
            File.AppendAllText(log, tool + "\t" + keys + "\t" + own + "\t" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t" + arguments.GetRawText().Replace("\r", string.Empty).Replace("\n", " ").Replace("\t", " ") + "\n");
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

        return mode switch
        {
            "deny" => ClaudeCodeStream.DenyAnswer(ClaudeCodeStream.RefusedWords),
            // A yes for the rest of the session: a rule of the session's alone, written to no settings file.
            "always" => ClaudeCodeStream.AlwaysAnswer(input, tool),
            _ => ClaudeCodeStream.AllowAnswer(input),
        };
    }
}
