// The stand-in of Claude Code for window scripts (docs/axClaude/events-and-grounding.md EG-11.3 and section 6.7,
// decision EG-D-17; docs/axKit/automation.md section 2, "The stand-in of Claude Code"). A mode of this runner, since
// the runner's own program is the one thing every run has: Program.Main puts its path into AXKIT_STANDIN before the
// first script, the windows of the run inherit it, and a script's claude.cmd is one line that calls it:
//
//   @"%AXKIT_STANDIN%" standin "%~dp0scenario.json" %*
//
// It plays a scenario as Claude Code would, in the window's pseudo console: frames, the turn's marks and bells on the
// console, entries appended to a session file under the run's own folder, and the command hooks of the --settings
// file it was given, each started with the event on its standard input. It needs no Claude Code, no network and no
// model. It acts on no key the window sends: what is typed is only drawn on the prompt row, as Claude Code draws a
// draft, while the last frame ends on a bare prompt row (EchoTyped), and a waitFile step stands for the moment Claude
// would take it up. Only tools/AxAutomate sets AXKIT_STANDIN; a workflow played in
// axAutomate's window has no stand-in.
//
// Usage:
//   AxAutomate standin <scenario.json> <Claude Code's arguments>
//       --version as the first of Claude Code's arguments prints the scenario's version and ends; otherwise the steps
//       are played. --settings <file> (or --settings=<file>) among the arguments names the hooks; the other arguments
//       are taken and ignored.
//   AxAutomate standin --prepare <scenario.json> <working folder>
//       appends the scenario's history steps to the session file of that working folder and ends: a session that
//       holds turns before the window starts Claude Code (--continue). A script runs it before its start step:
//       run "%AXKIT_STANDIN%" standin --prepare scenario.json <folder>
//   AxAutomate standin --stream <case | file.jsonl> <Claude Code's arguments>
//       the stand-in of Claude Code's stream, for a window the stream feeds: a recording of the real stream played
//       line by line and moved on by what the window writes. StreamStandIn.cs has its usage and its records.
//   Exit code 0, or 2 with one line on standard error ("stand-in: ...") for a scenario it cannot play; in a window
//   that line is drawn on the screen, where a script's expectations meet it.
//
// The scenario is one JSON object, written by a script's file step, which turns \n into a line end and \uXXXX into
// its character: so no string in it holds an escape, and rows and lines are arrays of strings (a single string is
// one line). Its keys: "version" (what --version prints; "0.0.0 (stand-in)" when left out, which the hooks' version
// gate refuses), "session" (the session id, the file's name; a new one for each start when left out, and --prepare
// needs it), "home" (a folder under the scenario's own, "home" when
// left out: the run's AXKIT_HOME), "steps" and "history" (arrays of steps), and three for what is typed (the fifth
// review of 2026-11-27, docs/axClaude/SPEC.md D33, "One message in flight"): "echoAfter" (milliseconds: the first
// character of a draft is drawn this long after it was read, the keys typed meanwhile waiting in the console's input,
// so that a message's Enter waits for its row), "echo" (false: nothing typed is drawn, the row never shows a message)
// and "keys" (a file name: every key read is appended to that file in the working folder, a character as itself,
// Enter as <Enter> and a line end, Ctrl+J as <Ctrl+J>, Ctrl+U as <Ctrl+U>, any other control as <U+XXXX>, so that a
// script compares what reached Claude Code's prompt, in order, with a file of its own), and one for an Enter
// Claude Code drops (the sixth review of that day, F1): "dropEnters" (a number: so many Enters on a typed draft
// leave it on the row, as Claude Code leaves a message whose Enter it dropped in its first seconds), and one for a row
// Claude Code does not draw after it took a message (the seventh review, K1): "hideAfterTake" (milliseconds: after an
// Enter takes a typed draft the row is emptied, its mark drawn again this long later, unless a frame came meanwhile,
// as Claude Code leaves its row during /clear), and "takeFrame" (a frame step, {"frame": rows, "clear": ...}: the
// first Enter that takes a typed draft draws it in the same write as the emptied row, so that no frame shows the row
// bare, as /permissions draws its screen in the row's place; the eighth review, L4). A frame step without "clear" is
// drawn where the cursor stands, so a question drawn by one after a held draft stands under the draft. The session
// file is
//   <the scenario's folder>\<home>\.claude\projects\<the working folder's full path, every character not an ASCII
//   letter or digit as ->\<session>.jsonl
// A step is an object; the first of these keys it holds says what it does:
//   frame (rows), clear       one frame: the cursor hidden; with clear the screen cleared and the cursor home; the
//                             rows joined by CR LF; the cursor shown. The cursor ends on the last row
//   mark ("A", "C" or "D")    the turn's mark, OSC 133 ended by ESC \ (A with ;redraw=0, as Claude Code writes it)
//   bell                      BEL
//   hook (name), fields, last (lines), tasks
//                             the event: session_id, transcript_path (the session file), cwd, hook_event_name,
//                             prompt_id (the last user step's), then the fields given (a field given replaces the one
//                             made: another session's id, a path to refuse), last joined by line ends as
//                             last_assistant_message, tasks as background_tasks. Every command hook the --settings
//                             file names for the event (whatever its matcher) is started without a shell and without a
//                             window, the event on its standard input, and waited for up to its timeout unless it says
//                             async. No --settings, or no hook for the event there: nothing
//   user (lines), source      a user entry with a new promptId and its promptSource (typed when left out)
//   assistant (lines), cut    an assistant text entry; with cut, isAbortedMidStream, then the interrupt's user entry
//   toolUse (name)            a tool call's entry
//   toolResult                the last tool call's result entry; true, or "error" for a failed one. A step may hold
//                             both keys: the call, then its result
//   turnEnd (ms)              a system turn_duration entry
//   queue (enqueue, dequeue, remove), text    a queue-operation entry, the text as its content
//   raw (object)              the object as one line: a kind the reader skips or does not know
//   wait (ms)                 waits
//   waitFile (name)           waits until the file is in the working folder (a script's file step makes it)
//   idle                      waits until the window stops Claude Code, which closes the console (an hour at most)
// A history holds only the steps that write the session file (user, assistant, toolUse, toolResult, turnEnd, queue,
// raw). The entries of one response share a message id, as Claude Code's do: a text and the tool call after it, up to
// the next user entry, tool result, interrupt or turn end. What the stand-in did, step by step, goes to standin.log
// beside the scenario: step kinds, hook programs and their ends, never a text.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AxAutomate;

/// <summary>
/// The stand-in of Claude Code: a scenario played in a window's pseudo console as Claude Code would play a session
/// (the file's header has the usage and the steps). Plain steps on a plain session file, so that a window script can
/// follow a turn, its marks, its events and its record without Claude Code, the network or a model.
/// </summary>
internal static class StandIn
{
    /// <summary>The runner's first argument for this mode (Program.Main).</summary>
    public const string Verb = "standin";

    /// <summary>The variable the runner puts its own program's path into for the windows of a run and their claude.cmd.</summary>
    public const string Variable = "AXKIT_STANDIN";

    private const string PrepareOption = "--prepare";
    private const string VersionOption = "--version";
    private const string SettingsOption = "--settings";
    private const string DefaultVersion = "0.0.0 (stand-in)";
    private const string Usage = "Usage: AxAutomate standin <scenario.json> <Claude Code's arguments>, or AxAutomate standin --prepare <scenario.json> <working folder>. See tools/AxAutomate/StandIn.cs.";
    private const int CannotPlay = 2;

    /// <summary>How often a waitFile step looks for its file.</summary>
    private const int PollMs = 50;

    /// <summary>The longest an idle or a waitFile step waits: a script's run is over long before.</summary>
    private const int LongestWaitMs = 3_600_000;

    /// <summary>Claude Code's own limit for a hook that names none, in seconds.</summary>
    private const int DefaultHookSeconds = 60;

    private const char Escape = (char)27;
    private const char Bell = (char)7;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonDocumentOptions ReadOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    /// <summary>The steps that write the session file, the only ones a history holds.</summary>
    private static readonly string[] RecordSteps = ["user", "assistant", "toolUse", "toolResult", "turnEnd", "queue", "raw"];

    /// <summary>Every step, in the order a step's keys are asked: toolUse before toolResult, so that a step with both is a call and its result.</summary>
    private static readonly string[] AllSteps = ["frame", "mark", "bell", "hook", .. RecordSteps, "wait", "waitFile", "idle"];

    /// <summary>The mode's arguments, after the verb: a scenario and Claude Code's arguments, or <c>--prepare</c>, a scenario and a working folder.</summary>
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == PrepareOption)
            {
                return args.Length == 3 ? Prepare(Path.GetFullPath(args[1]), Path.GetFullPath(args[2])) : Refuse(Usage);
            }

            // The stand-in of Claude Code's stream, for a window the stream feeds (StreamStandIn.cs).
            if (args.Length > 0 && args[0] == StreamStandIn.Option)
            {
                return StreamStandIn.Run(args[1..]);
            }

            if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
            {
                return Refuse(Usage);
            }

            var scenarioPath = Path.GetFullPath(args[0]);
            using var scenario = Read(scenarioPath);
            var claude = args[1..];
            if (claude.Length > 0 && claude[0] == VersionOption)
            {
                // What `claude --version` prints, read by the window's version gate (EG-2.4) through a pipe.
                Console.Out.WriteLine(StringOf(scenario.RootElement, "version") ?? DefaultVersion);
                return 0;
            }

            return Play(scenarioPath, scenario.RootElement, claude);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
            or InvalidOperationException or ArgumentException or NotSupportedException or KeyNotFoundException)
        {
            return Refuse(ex.Message);
        }
    }

    private static int Refuse(string message)
    {
        Console.Error.WriteLine("stand-in: " + message);
        return CannotPlay;
    }

    /// <summary>The steps of the scenario played in order, in the working folder Claude Code was started in.</summary>
    private static int Play(string scenarioPath, JsonElement root, string[] claude)
    {
        var steps = Steps(root, "steps", AllSteps);
        var folder = Environment.CurrentDirectory;
        var settings = SettingsFile(claude);
        // Claude Code reads its settings once, at its start (EG-2.3); so does the stand-in.
        var hooks = settings is null ? [] : Hooks(Path.GetFullPath(settings));
        var session = Session(scenarioPath, root, folder);
        var log = new Log(Path.Combine(Path.GetDirectoryName(scenarioPath)!, "standin.log"));
        log.Line($"started, process {Environment.ProcessId}: {steps.Count} steps, {(settings is null ? "no settings file" : hooks.Count + " hooks in the settings file")}");

        PrepareConsole();
        using var output = Console.OpenStandardOutput();
        _echoAfterMs = root.TryGetProperty("echoAfter", out _) ? Number(root, "echoAfter") : 0;
        _echo = !root.TryGetProperty("echo", out var echo) || echo.ValueKind != JsonValueKind.False;
        _keysFile = StringOf(root, "keys") is { Length: > 0 } keys ? Path.Combine(folder, keys) : null;
        _dropEnters = root.TryGetProperty("dropEnters", out _) ? Number(root, "dropEnters") : 0;
        _hideAfterTakeMs = root.TryGetProperty("hideAfterTake", out _) ? Number(root, "hideAfterTake") : 0;
        _takeFrame = root.TryGetProperty("takeFrame", out var take) && take.ValueKind == JsonValueKind.Object ? take.Clone() : null;
        new Thread(() => EchoTyped(output)) { IsBackground = true, Name = "typed" }.Start();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var kind = KindOf(step)!;
            log.Line($"step {i + 1}: {kind}");
            switch (kind)
            {
                case "frame":
                    lock (Writing)
                    {
                        Draw(output, step);
                    }

                    break;
                case "mark":
                    Write(output, Mark(step));
                    break;
                case "bell":
                    Write(output, Bell.ToString());
                    break;
                case "hook":
                    StartHooks(Text(step.GetProperty("hook")), step, hooks, session, folder, log);
                    break;
                case "wait":
                    Thread.Sleep(Number(step, "wait"));
                    break;
                case "waitFile":
                    WaitForFile(Path.Combine(folder, Text(step.GetProperty("waitFile"))));
                    break;
                case "idle":
                    // The window stops Claude Code by closing its console, which ends this process.
                    Thread.Sleep(LongestWaitMs);
                    break;
                default:
                    session.Append(kind, step);
                    break;
            }
        }

        log.Line("ended: every step played");
        return 0;
    }

    /// <summary>A session's earlier turns, written before the window starts (<c>--prepare</c>).</summary>
    private static int Prepare(string scenarioPath, string folder)
    {
        using var scenario = Read(scenarioPath);
        if (StringOf(scenario.RootElement, "session") is null)
        {
            // Without it the window's stand-in would start a session of its own, and the history would be in another file.
            throw new InvalidDataException("A history needs the scenario's \"session\".");
        }

        var history = Steps(scenario.RootElement, "history", RecordSteps);
        var session = Session(scenarioPath, scenario.RootElement, folder);
        foreach (var step in history)
        {
            session.Append(KindOf(step)!, step);
        }

        Console.Out.WriteLine($"stand-in: {history.Count} steps of history written");
        return 0;
    }

    private static JsonDocument Read(string path)
    {
        var document = JsonDocument.Parse(File.ReadAllText(path), ReadOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new InvalidDataException($"{Path.GetFileName(path)} is not one JSON object.");
        }

        return document;
    }

    /// <summary>A scenario's array of steps, each checked before any is played, so that a slip fails before the first frame.</summary>
    private static List<JsonElement> Steps(JsonElement root, string name, string[] allowed)
    {
        var steps = new List<JsonElement>();
        if (!root.TryGetProperty(name, out var array))
        {
            return steps;
        }

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"\"{name}\" is an array of steps.");
        }

        foreach (var step in array.EnumerateArray())
        {
            var kind = KindOf(step);
            if (kind is null || !allowed.Contains(kind))
            {
                var text = step.GetRawText();
                throw new InvalidDataException($"step {steps.Count + 1} of \"{name}\" is not a step {(name == "history" ? "a history holds" : "the stand-in knows")}: {text[..Math.Min(text.Length, 80)]}");
            }

            steps.Add(step);
        }

        return steps;
    }

    private static string? KindOf(JsonElement step) =>
        step.ValueKind == JsonValueKind.Object ? AllSteps.FirstOrDefault(name => step.TryGetProperty(name, out _)) : null;

    /// <summary>The session file of the working folder, with what its entries carry of the scenario.</summary>
    private static SessionFile Session(string scenarioPath, JsonElement root, string folder)
    {
        var id = StringOf(root, "session") ?? Guid.NewGuid().ToString("D");
        if (id.Length == 0 || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidDataException("\"session\" is a session id, which names a file.");
        }

        var home = StringOf(root, "home") ?? "home";
        var version = (StringOf(root, "version") ?? DefaultVersion).Split(' ')[0];
        var file = Path.Combine(Path.GetDirectoryName(scenarioPath)!, home, ".claude", "projects", ProjectFolderName(folder), id + ".jsonl");
        return new SessionFile(file, id, folder, version);
    }

    /// <summary>
    /// Claude Code's name for a working folder's project folder (EG-3.1, measured with 2.1.325): the full path with every
    /// character that is not an ASCII letter or digit as a hyphen. Written here as Claude Code does it, apart from
    /// axClaude's reader (ClaudeCodeTranscript.ProjectFolderName), so that a script tests that reader against it.
    /// </summary>
    private static string ProjectFolderName(string folder)
    {
        var name = new StringBuilder();
        foreach (var c in Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)))
        {
            name.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        return name.ToString();
    }

    /// <summary>A frame step drawn, in one write after <paramref name="before"/>, and what the echo of typed keys knows of it: whether it ends on a prompt row, and with a draft. The caller holds <see cref="Writing"/>.</summary>
    private static void Draw(Stream output, JsonElement step, string before = "")
    {
        Write(output, before + Frame(step));
        var rows = Lines(step.GetProperty("frame"));
        var last = rows.Count > 0 ? rows[^1].TrimEnd() : string.Empty;
        _atPrompt = last.Length > 0 && last[0] is '$' or '>' or '!' && (last.Length == 1 || char.IsWhiteSpace(last[1]));
        _promptMark = _atPrompt ? last[0] : '$';
        _drafting = _atPrompt && last.Length > 1;
        _draftBreaks = 0;
        _typedSinceFrame = false;
        _frames++;
    }

    private static string Frame(JsonElement step)
    {
        var frame = new StringBuilder().Append(Escape).Append("[?25l");
        if (Flag(step, "clear"))
        {
            frame.Append(Escape).Append("[2J").Append(Escape).Append("[H");
        }

        frame.Append(string.Join("\r\n", Lines(step.GetProperty("frame"))));
        return frame.Append(Escape).Append("[?25h").ToString();
    }

    private static string Mark(JsonElement step) => StringOf(step, "mark") switch
    {
        "A" => $"{Escape}]133;A;redraw=0{Escape}\\",
        "C" => $"{Escape}]133;C{Escape}\\",
        "D" => $"{Escape}]133;D{Escape}\\",
        _ => throw new InvalidDataException("A mark is \"A\", \"C\" or \"D\"."),
    };

    private static void Write(Stream output, string text)
    {
        lock (Writing)
        {
            output.Write(Utf8.GetBytes(text));
            output.Flush();
        }
    }

    /// <summary>One writer at a time to the console: the steps' and the echo of what is typed.</summary>
    private static readonly object Writing = new();

    /// <summary>Whether the last frame ended on a prompt row ("$", "&gt;" or "!", bare or with a draft on it), where Claude Code draws what is typed; until an Enter.</summary>
    private static bool _atPrompt;

    /// <summary>The mark the prompt row starts with.</summary>
    private static char _promptMark = '$';

    /// <summary>Whether the prompt row holds a draft (drawn by the frame, or typed since).</summary>
    private static bool _drafting;

    /// <summary>How many line ends (Ctrl+J) the draft typed since the last frame has, so that an Enter clears all its rows.</summary>
    private static int _draftBreaks;

    /// <summary>Whether a character was typed onto the prompt row since the last frame (or the last Enter): only such a draft is taken by an Enter.</summary>
    private static bool _typedSinceFrame;

    /// <summary>
    /// What the window types, read and drawn on the prompt row as Claude Code draws a draft (the fourth review of
    /// 2026-11-27, docs/axClaude/SPEC.md D33: the window sends a message's Enter only once the frame shows the message
    /// there): while the last frame ends on a prompt row, each typed character at the cursor, the first of a draft
    /// after the no-break space Claude Code puts after the prompt's mark, Ctrl+J as a line end, Ctrl+U as the row
    /// emptied to its mark; an Enter empties the rows of a draft typed since the last frame back to the mark, as
    /// Claude Code does when it takes a message (the next frame draws what it did with it), and leaves a draft the
    /// frame drew, which stands for an Enter Claude Code dropped. Nothing is drawn while a frame ends on anything else (a
    /// question, a screen), so an answer's keys change no row a script reads; other keys are read and dropped. Until
    /// that day the stand-in read no key.
    /// </summary>
    private static void EchoTyped(Stream output)
    {
        try
        {
            while (true)
            {
                var typed = Console.ReadKey(intercept: true).KeyChar;
                if (_keysFile is not null)
                {
                    File.AppendAllText(_keysFile, KeyWritten(typed), Utf8);
                }

                if (!_echo)
                {
                    continue;
                }

                if (_echoAfterMs > 0 && !_drafting && _atPrompt && !char.IsControl(typed) && typed != '\0')
                {
                    // The draft's first character drawn late: the keys after it wait in the console's input meanwhile.
                    Thread.Sleep(_echoAfterMs);
                }

                lock (Writing)
                {
                    if (!_atPrompt)
                    {
                        continue;
                    }

                    if (typed == '\r')
                    {
                        // Claude Code takes the draft and empties its prompt row: the draft's rows cleared, the mark left.
                        // A draft the frame drew itself stays: it stands for a message whose Enter Claude Code dropped.
                        if (!_typedSinceFrame)
                        {
                            continue;
                        }

                        // An Enter Claude Code drops in its first seconds (FR-2.1): the typed draft stays on the row.
                        if (_dropEnters > 0)
                        {
                            _dropEnters--;
                            continue;
                        }

                        _typedSinceFrame = false;
                        var clear = new StringBuilder();
                        for (var i = 0; i < _draftBreaks; i++)
                        {
                            clear.Append('\r').Append(Escape).Append("[2K").Append(Escape).Append("[1A");
                        }

                        _drafting = false;
                        _draftBreaks = 0;
                        if (_takeFrame is { } take)
                        {
                            // The take's own frame in the row's place, in the same write: no frame shows the row bare
                            // (the permissions recording: /permissions' screen, the eighth review, L4).
                            _takeFrame = null;
                            Draw(output, take, clear.Append('\r').Append(Escape).Append("[2K").ToString());
                            continue;
                        }

                        if (_hideAfterTakeMs > 0)
                        {
                            // The row not drawn for a while after the take, as Claude Code leaves it during /clear (the
                            // clear recording: 13448 ms to 16144 ms): the row emptied, its mark drawn again later.
                            Write(output, clear.Append('\r').Append(Escape).Append("[2K").ToString());
                            _atPrompt = false;
                            var frames = _frames;
                            new Thread(() => DrawMarkLater(output, frames)) { IsBackground = true, Name = "mark" }.Start();
                            continue;
                        }

                        Write(output, clear.Append('\r').Append(Escape).Append("[2K").Append(_promptMark).ToString());
                    }
                    else if (typed == '\n')
                    {
                        if (_drafting)
                        {
                            Write(output, "\r\n");
                            _draftBreaks++;
                        }
                    }
                    else if (typed == (char)21)
                    {
                        // Ctrl+U: the row emptied back to its mark.
                        Write(output, $"\r{Escape}[2K{_promptMark}");
                        _drafting = false;
                    }
                    else if (!char.IsControl(typed) && typed != '\0')
                    {
                        if (!_drafting)
                        {
                            Write(output, " ");
                            _drafting = true;
                        }

                        Write(output, typed.ToString());
                        _typedSinceFrame = true;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            // No console to read (a test from a pipe), or the window closed it: nothing more is typed.
        }
    }

    /// <summary>A key read as the scenario's "keys" file holds it: a character as itself, the keys a window sends as names.</summary>
    private static string KeyWritten(char typed) => typed switch
    {
        '\r' => "<Enter>\r\n",
        '\n' => "<Ctrl+J>",
        (char)21 => "<Ctrl+U>",
        _ when char.IsControl(typed) || typed == '\0' => $"<U+{(int)typed:X4}>",
        _ => typed.ToString(),
    };

    /// <summary>The scenario's "echoAfter": how long the first character of a draft waits before it is drawn.</summary>
    private static int _echoAfterMs;

    /// <summary>The scenario's "echo": false when nothing typed is drawn.</summary>
    private static bool _echo = true;

    /// <summary>The scenario's "keys": the file in the working folder every key read is appended to, or null.</summary>
    private static string? _keysFile;

    /// <summary>The scenario's "dropEnters": how many Enters on a typed draft are still to be dropped, the draft left on the row.</summary>
    private static int _dropEnters;

    /// <summary>The scenario's "hideAfterTake": how long, in milliseconds, the prompt row stays not drawn after an Enter took a typed draft (0: drawn bare at once).</summary>
    private static int _hideAfterTakeMs;

    /// <summary>The scenario's "takeFrame": a frame step drawn by the first Enter that takes a typed draft, in place of the emptied row; null when there is none, or once drawn.</summary>
    private static JsonElement? _takeFrame;

    /// <summary>How many frames the steps have drawn: a mark drawn later is drawn only while no frame came since.</summary>
    private static int _frames;

    /// <summary>The prompt row's mark drawn again, bare, <see cref="_hideAfterTakeMs"/> after a take, unless a frame of the steps came meanwhile.</summary>
    private static void DrawMarkLater(Stream output, int frames)
    {
        Thread.Sleep(_hideAfterTakeMs);
        lock (Writing)
        {
            if (_frames != frames)
            {
                return;
            }

            Write(output, $"\r{Escape}[2K{_promptMark}");
            _atPrompt = true;
        }
    }

    private static void WaitForFile(string path)
    {
        var until = Environment.TickCount64 + LongestWaitMs;
        while (!File.Exists(path))
        {
            if (Environment.TickCount64 > until)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} did not come within an hour.");
            }

            Thread.Sleep(PollMs);
        }
    }

    // --- The console ------------------------------------------------------------------------------------------------

    private const int StandardOutputHandle = -11;
    private const uint VirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int which);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(nint handle, uint mode);

    /// <summary>
    /// UTF-8 for the console, as Claude Code writes, and its escape sequences taken as sequences: cmd.exe, which ran
    /// claude.cmd, turns the console's sequence processing off for the programs it starts.
    /// </summary>
    private static void PrepareConsole()
    {
        try
        {
            Console.OutputEncoding = Utf8;
        }
        catch (IOException)
        {
            // No console (a test from a pipe): the bytes go out as they are.
        }

        var handle = GetStdHandle(StandardOutputHandle);
        if (GetConsoleMode(handle, out var mode))
        {
            SetConsoleMode(handle, mode | VirtualTerminalProcessing);
        }
    }

    // --- Hooks ------------------------------------------------------------------------------------------------------

    /// <summary>One command hook of the settings file: the event it is for, the program and its arguments (exec form, no shell), whether Claude Code waits for it, and for how long at most.</summary>
    private sealed record Hook(string Event, string Command, IReadOnlyList<string> Arguments, bool Async, int TimeoutSeconds);

    private static string? SettingsFile(IReadOnlyList<string> claude)
    {
        for (var i = 0; i < claude.Count; i++)
        {
            if (claude[i] == SettingsOption && i + 1 < claude.Count)
            {
                return claude[i + 1];
            }

            if (claude[i].StartsWith(SettingsOption + "=", StringComparison.Ordinal))
            {
                return claude[i][(SettingsOption.Length + 1)..];
            }
        }

        return null;
    }

    /// <summary>
    /// The command hooks of a settings file of Claude Code's shape (events-and-grounding.md section 6.1): under
    /// <c>hooks</c>, for each event, groups of handlers. A handler of another type is passed over, as is a matcher:
    /// none of the events axClaude registers takes one.
    /// </summary>
    private static List<Hook> Hooks(string file)
    {
        using var settings = Read(file);
        var hooks = new List<Hook>();
        if (!settings.RootElement.TryGetProperty("hooks", out var events) || events.ValueKind != JsonValueKind.Object)
        {
            return hooks;
        }

        foreach (var hookEvent in events.EnumerateObject())
        {
            if (hookEvent.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var group in hookEvent.Value.EnumerateArray())
            {
                if (group.ValueKind != JsonValueKind.Object || !group.TryGetProperty("hooks", out var handlers) || handlers.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var handler in handlers.EnumerateArray())
                {
                    if (StringOf(handler, "type") != "command" || StringOf(handler, "command") is not { Length: > 0 } command)
                    {
                        continue;
                    }

                    var arguments = handler.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Array ? Lines(args) : [];
                    var timeout = handler.TryGetProperty("timeout", out var seconds) && seconds.TryGetInt32(out var limit) && limit > 0 ? limit : DefaultHookSeconds;
                    hooks.Add(new Hook(hookEvent.Name, command, arguments, Flag(handler, "async"), timeout));
                }
            }
        }

        return hooks;
    }

    private static void StartHooks(string name, JsonElement step, List<Hook> hooks, SessionFile session, string folder, Log log)
    {
        var mine = hooks.Where(hook => hook.Event == name).ToList();
        if (mine.Count == 0)
        {
            log.Line($"hook {name}: no hook for it, or no settings file");
            return;
        }

        var body = EventBody(name, step, session, folder);
        foreach (var hook in mine)
        {
            StartHook(hook, body, folder, log);
        }
    }

    /// <summary>The event as Claude Code writes it to a hook's standard input (section 6.2): one JSON object.</summary>
    private static byte[] EventBody(string name, JsonElement step, SessionFile session, string folder)
    {
        var fields = step.TryGetProperty("fields", out var given) && given.ValueKind == JsonValueKind.Object ? given : default;
        bool Given(string field) => fields.ValueKind == JsonValueKind.Object && fields.TryGetProperty(field, out _);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            if (!Given("session_id"))
            {
                writer.WriteString("session_id", session.SessionId);
            }

            if (!Given("transcript_path"))
            {
                writer.WriteString("transcript_path", session.FilePath);
            }

            if (!Given("cwd"))
            {
                writer.WriteString("cwd", folder);
            }

            if (!Given("hook_event_name"))
            {
                writer.WriteString("hook_event_name", name);
            }

            if (session.PromptId is { } promptId && !Given("prompt_id"))
            {
                writer.WriteString("prompt_id", promptId);
            }

            if (fields.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in fields.EnumerateObject())
                {
                    field.WriteTo(writer);
                }
            }

            if (step.TryGetProperty("last", out var last))
            {
                writer.WriteString("last_assistant_message", Text(last));
            }

            if (step.TryGetProperty("tasks", out var tasks))
            {
                writer.WritePropertyName("background_tasks");
                tasks.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// One hook started as Claude Code starts a command hook in exec form: the program itself, no shell, no window,
    /// in the working folder, with this process's environment (the window's <c>AXKIT_HOOK_PIPE</c> among it). What it
    /// prints is read and dropped: Claude Code reads it and never draws it.
    /// </summary>
    private static void StartHook(Hook hook, byte[] body, string folder, Log log)
    {
        var start = new ProcessStartInfo(hook.Command)
        {
            WorkingDirectory = folder,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // No byte order mark: closing the input would otherwise write one after the event.
            StandardInputEncoding = Utf8,
        };
        foreach (var argument in hook.Arguments)
        {
            start.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            // Outside call, program, Claude Code events: the command a window script's settings file names for the event,
            // as Claude Code starts it (in a window of a test run, AxKit.exe hook of the run's own build).
            process = Process.Start(start)!;
        }
        catch (Win32Exception ex)
        {
            log.Line($"hook {hook.Event}: {Path.GetFileName(hook.Command)} did not start: {ex.Message}");
            return;
        }

        _ = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
        _ = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        if (hook.Async)
        {
            // Claude Code goes on at once (EG-2.5); the event is written beside the steps that follow.
            _ = Task.Run(() => Feed(process, body));
            log.Line($"hook {hook.Event}: {Path.GetFileName(hook.Command)} started, not waited for");
            return;
        }

        Feed(process, body);
        var ended = process.WaitForExit(hook.TimeoutSeconds * 1000);
        if (!ended)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // It ended meanwhile.
            }
        }

        log.Line($"hook {hook.Event}: {Path.GetFileName(hook.Command)} {(ended ? "ended with " + process.ExitCode.ToString(CultureInfo.InvariantCulture) : "ended at its timeout")}");
        process.Dispose();
    }

    private static void Feed(Process process, byte[] body)
    {
        try
        {
            // Outside call, pipe, Claude Code events: the hook's standard input, on which the event is written as Claude
            // Code writes it, with a line end, and closed.
            var input = process.StandardInput;
            input.BaseStream.Write(body);
            input.BaseStream.WriteByte((byte)'\n');
            input.Close();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // The hook ended before it read the event; Claude Code goes on the same way.
        }
    }

    // --- Values -----------------------------------------------------------------------------------------------------

    /// <summary>Rows or lines: an array of strings, or one string as one line.</summary>
    private static List<string> Lines(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => [value.GetString()!],
        JsonValueKind.Array when value.EnumerateArray().All(line => line.ValueKind == JsonValueKind.String) =>
            value.EnumerateArray().Select(line => line.GetString()!).ToList(),
        _ => throw new InvalidDataException($"Rows and lines are an array of strings, not {value.GetRawText()}."),
    };

    private static string Text(JsonElement value) => string.Join("\n", Lines(value));

    private static string? StringOf(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Flag(JsonElement element, string key) =>
        element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.True;

    private static int Number(JsonElement step, string key) =>
        step.GetProperty(key).TryGetInt32(out var number) && number >= 0
            ? number
            : throw new InvalidDataException($"\"{key}\" is a whole number of milliseconds.");

    // --- The session file -------------------------------------------------------------------------------------------

    /// <summary>
    /// The session file as Claude Code appends it (events-and-grounding.md section 4.3, the fixtures of
    /// tests/fixtures/records): one JSON object for each line, UTF-8 without a mark, LF, append only, each line in one
    /// write, open to readers while it is written. It keeps what the next entry carries: the entry before it, the
    /// prompt of the turn, the response's message id, the last tool call.
    /// </summary>
    private sealed class SessionFile(string filePath, string sessionId, string folder, string version)
    {
        private const string Model = "stand-in";

        private string? _parent;
        private string? _promptId;
        private string? _messageId;
        private string? _toolId;
        private string? _toolEntry;
        private int _messages;
        private int _tools;

        public string FilePath { get; } = filePath;

        public string SessionId { get; } = sessionId;

        /// <summary>The prompt of the turn the last user step opened; null before the first.</summary>
        public string? PromptId => _promptId;

        public void Append(string kind, JsonElement step)
        {
            switch (kind)
            {
                case "user":
                    User(Text(step.GetProperty("user")), StringOf(step, "source") ?? "typed");
                    break;
                case "assistant":
                    Assistant(Text(step.GetProperty("assistant")), Flag(step, "cut"));
                    break;
                case "toolUse":
                    ToolUse(Text(step.GetProperty("toolUse")));
                    if (step.TryGetProperty("toolResult", out var result))
                    {
                        ToolResult(result);
                    }

                    break;
                case "toolResult":
                    ToolResult(step.GetProperty("toolResult"));
                    break;
                case "turnEnd":
                    TurnEnd(Number(step, "turnEnd"));
                    break;
                case "queue":
                    Queue(Text(step.GetProperty("queue")), step.TryGetProperty("text", out var text) ? Text(text) : null);
                    break;
                case "raw":
                    var raw = step.GetProperty("raw");
                    if (raw.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidDataException("\"raw\" is one JSON object, written as one line.");
                    }

                    Line(raw.WriteTo);
                    break;
                default:
                    throw new InvalidDataException($"{kind} writes nothing to the session file.");
            }
        }

        private void User(string text, string source)
        {
            _promptId = NewId();
            UserEntry(writer => writer.WriteString("content", text), writer => writer.WriteString("promptSource", source));
        }

        private void Assistant(string text, bool cut)
        {
            var message = AssistantEntry(
                writer =>
                {
                    writer.WriteString("type", "text");
                    writer.WriteString("text", text);
                },
                cut ? null : "end_turn",
                cut);
            if (cut)
            {
                // Claude Code's entry for the interrupt follows the cut reply (M13 section 4).
                UserEntry(
                    writer =>
                    {
                        writer.WriteStartArray("content");
                        writer.WriteStartObject();
                        writer.WriteString("type", "text");
                        writer.WriteString("text", "[Request interrupted by user]");
                        writer.WriteEndObject();
                        writer.WriteEndArray();
                    },
                    writer => writer.WriteString("interruptedMessageId", message));
            }
        }

        private void ToolUse(string name)
        {
            _toolId = "toolu_standin" + (++_tools).ToString(CultureInfo.InvariantCulture);
            var id = _toolId;
            AssistantEntry(
                writer =>
                {
                    writer.WriteString("type", "tool_use");
                    writer.WriteString("id", id);
                    writer.WriteString("name", name);
                    writer.WriteStartObject("input");
                    writer.WriteEndObject();
                },
                "tool_use",
                cut: false);
            _toolEntry = _parent;
        }

        private void ToolResult(JsonElement value)
        {
            var failed = value.ValueKind == JsonValueKind.String && value.GetString() == "error";
            var toolId = _toolId ?? string.Empty;
            var toolEntry = _toolEntry;
            UserEntry(
                writer =>
                {
                    writer.WriteStartArray("content");
                    writer.WriteStartObject();
                    writer.WriteString("tool_use_id", toolId);
                    writer.WriteString("type", "tool_result");
                    writer.WriteString("content", string.Empty);
                    writer.WriteBoolean("is_error", failed);
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                },
                writer =>
                {
                    if (toolEntry is not null)
                    {
                        writer.WriteString("sourceToolAssistantUUID", toolEntry);
                    }
                });
        }

        private void TurnEnd(int milliseconds)
        {
            var uuid = NewId();
            Line(writer =>
            {
                writer.WriteStartObject();
                Parent(writer);
                writer.WriteString("type", "system");
                writer.WriteString("subtype", "turn_duration");
                writer.WriteNumber("durationMs", milliseconds);
                writer.WriteString("timestamp", Now());
                writer.WriteString("uuid", uuid);
                writer.WriteBoolean("isMeta", false);
                Context(writer);
                writer.WriteEndObject();
            });
            _parent = uuid;
            _messageId = null;
        }

        private void Queue(string operation, string? text)
        {
            Line(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "queue-operation");
                writer.WriteString("operation", operation);
                writer.WriteString("timestamp", Now());
                writer.WriteString("sessionId", SessionId);
                if (text is not null)
                {
                    writer.WriteString("content", text);
                }

                writer.WriteEndObject();
            });
        }

        /// <summary>A user entry of the turn (a prompt, a tool's result, an interrupt): the content, then what else it carries. It ends the response before it.</summary>
        private void UserEntry(Action<Utf8JsonWriter> content, Action<Utf8JsonWriter> more)
        {
            var uuid = NewId();
            Line(writer =>
            {
                writer.WriteStartObject();
                Parent(writer);
                if (_promptId is not null)
                {
                    writer.WriteString("promptId", _promptId);
                }

                writer.WriteString("type", "user");
                writer.WriteStartObject("message");
                writer.WriteString("role", "user");
                content(writer);
                writer.WriteEndObject();
                writer.WriteString("uuid", uuid);
                writer.WriteString("timestamp", Now());
                more(writer);
                Context(writer);
                writer.WriteEndObject();
            });
            _parent = uuid;
            _messageId = null;
        }

        /// <summary>An assistant entry with one content block, in the response that is open or a new one; returns the response's message id.</summary>
        private string AssistantEntry(Action<Utf8JsonWriter> block, string? stopReason, bool cut)
        {
            _messageId ??= "msg_standin" + (++_messages).ToString(CultureInfo.InvariantCulture);
            var message = _messageId;
            var uuid = NewId();
            Line(writer =>
            {
                writer.WriteStartObject();
                Parent(writer);
                writer.WriteStartObject("message");
                writer.WriteString("model", Model);
                writer.WriteString("id", message);
                writer.WriteString("type", "message");
                writer.WriteString("role", "assistant");
                writer.WriteStartArray("content");
                writer.WriteStartObject();
                block(writer);
                writer.WriteEndObject();
                writer.WriteEndArray();
                if (stopReason is null)
                {
                    writer.WriteNull("stop_reason");
                }
                else
                {
                    writer.WriteString("stop_reason", stopReason);
                }

                writer.WriteEndObject();
                writer.WriteString("type", "assistant");
                writer.WriteString("uuid", uuid);
                writer.WriteString("timestamp", Now());
                if (cut)
                {
                    writer.WriteBoolean("isAbortedMidStream", true);
                }

                Context(writer);
                writer.WriteEndObject();
            });
            _parent = uuid;
            return message;
        }

        private void Parent(Utf8JsonWriter writer)
        {
            if (_parent is null)
            {
                writer.WriteNull("parentUuid");
            }
            else
            {
                writer.WriteString("parentUuid", _parent);
            }

            writer.WriteBoolean("isSidechain", false);
        }

        private void Context(Utf8JsonWriter writer)
        {
            writer.WriteString("cwd", folder);
            writer.WriteString("sessionId", SessionId);
            writer.WriteString("version", version);
        }

        private void Line(Action<Utf8JsonWriter> write)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                write(writer);
            }

            var line = new byte[buffer.WrittenCount + 1];
            buffer.WrittenSpan.CopyTo(line);
            line[^1] = (byte)'\n';
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            using var file = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            file.Write(line);
        }

        private static string NewId() => Guid.NewGuid().ToString("D");

        private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>standin.log beside the scenario: what the stand-in did, for a script's author reading a kept run (--keep). Kinds and programs, never a text.</summary>
    private sealed class Log(string path)
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        public void Line(string text)
        {
            try
            {
                File.AppendAllText(path, $"{_clock.ElapsedMilliseconds,7} {text}\r\n", Utf8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A second stand-in of the same scenario writing at the same moment: the line is lost, the play goes on.
            }
        }
    }
}
