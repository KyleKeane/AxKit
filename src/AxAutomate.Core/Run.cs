using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>
/// The windows of one script, on the test desktop, each with its pipe; one step at a time (<see cref="Step"/>), as
/// <see cref="ScriptRunner"/> plays them. <paramref name="hosted"/>: the run lives inside a process that is not only
/// the run's, axAutomate's window (SPEC.md AU-5.3), so that what the end of the run ends (<see cref="Sweep"/>) is
/// found from the run's own processes and not from every child of this process, which may be the user's own windows.
/// </summary>
public sealed class Run(string exe, string temp, bool hosted = false)
{
    /// <summary>A desktop of the run's own: runs side by side never share one (a window closing in another run could activate one of this run's).</summary>
    private static readonly string DesktopName = $"axkit-tests-{Environment.ProcessId}";

    /// <summary>The NVDA tier: the user's own desktop, the foreground, real keyboard input, NVDA's speech.</summary>
    public static bool Nvda { get; set; }

    private readonly List<Window> _windows = [];
    private readonly nint _desktop = Nvda ? 0 : Native.CreateDesktopW(DesktopName, 0, 0, 0, 0x10000000, 0);
    private readonly Speech _speech = new();

    /// <summary>What NVDA said up to the last heard check's match (<see cref="Heard"/>).</summary>
    private List<string> _lastHeard = [];

    private readonly List<string> _allowed = [];
    private readonly string _home = Path.Combine(temp, "home");
    private Window? _current;
    private bool _found;
    private Broker? _broker;

    /// <summary>The run's secret (security audit S-11), handed to its windows alone.</summary>
    private readonly string _secret = Window.NewSecret();

    /// <summary>When the run began, and every process it started with its start time (<see cref="Family"/>).</summary>
    private readonly DateTime _began = DateTime.Now;

    private readonly Dictionary<int, DateTime> _started = [];

    /// <summary>AxKit.exe with the arguments on the run's desktop, its id and start time kept for the end's sweep.</summary>
    private int Create(string arguments)
    {
        var id = Window.Create(exe, arguments, Nvda ? null : DesktopName, _home, _secret);
        try
        {
            using var process = Process.GetProcessById(id);
            _started[id] = process.StartTime;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Ended already: nothing of it to sweep.
        }

        return id;
    }

    public (bool Ok, string? Detail) Step(string line)
    {
        var space = line.IndexOf(' ');
        var verb = space < 0 ? line : line[..space];
        var rest = space < 0 ? string.Empty : line[(space + 1)..];
        switch (verb)
        {
            case "mkdir":
                Directory.CreateDirectory(Path.Combine(temp, rest));
                return (true, null);
            case "file":
                // The path ends at the first space, or, in quotes, at the closing quote: axTask's items are named
                // after their titles ("Fix the tap.json").
                var quoted = rest.StartsWith('"') && rest.IndexOf('"', 1) > 0;
                var at = quoted ? rest.IndexOf('"', 1) + 1 : rest.IndexOf(' ');
                var name = quoted ? rest[1..(at - 1)] : at < 0 ? rest : rest[..at];
                var content = at < 0 || at >= rest.Length ? string.Empty : rest[(at + 1)..];
                var path = Path.Combine(temp, name);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content.Replace("\\n", "\r\n"), new UTF8Encoding(false));
                return (true, null);
            case "silence":
                var parts = rest.Split(' ', 2);
                WriteSilence(Path.Combine(temp, parts[0]), int.Parse(parts[1]));
                return (true, null);
            case "run":
                return RunCommand(rest);
            case "start":
                _current = Window.Start(Create(rest), _secret);
                _windows.Add(_current);
                _current.WaitForFocus();
                BringToFront(_current);
                return (true, $"process {_current.ProcessId}");
            case "use":
                _current = _windows[int.Parse(rest) - 1];
                BringToFront(_current);
                return (true, null);
            case "launch":
                var launched = Window.Launch(Create(rest));
                if (Nvda)
                {
                    // The open window took the hand-over and brought itself to the front: NVDA reads it first.
                    InFront();
                    Speech.WaitForQuiet(1500);
                }

                return launched;
            case "adopt":
                return Adopt();
            case "allow":
                _allowed.Add(rest.StartsWith("error ", StringComparison.Ordinal) ? rest[6..] : rest);
                return (true, null);
            case "wait":
                Thread.Sleep(int.Parse(rest));
                return (true, null);
            case "said":
                return (true, string.Join(" | ", Current.TakeAll().Select(e => e.Spoken)));
            case "expect":
                return Expect(rest, 0);
            case "refuse":
                return Refuse(rest);
            case "try":
                var (found, seen) = Expect(rest, 15_000);
                _found = found;
                return (true, found ? "found: " + seen : "not found");
            case "iffound":
                return _found ? Step(rest) : (true, "skipped");
            case "text" or "list" or "commands" or "state" or "focus":
                var answer = Current.Send(verb);
                return (!answer.Contains("\"error\""), answer);
            case "mcp" when rest == "start":
                // The runner as an MCP client, after a start: the broker gets the run's variables (Broker).
                _broker?.Dispose();
                _broker = Broker.Start(exe, _secret, _home);
                return (true, null);
            case "mcp" when rest.StartsWith("call ", StringComparison.Ordinal) && _broker is not null:
                var call = rest[5..].Trim();
                var gap = call.IndexOf(' ');
                return (true, _broker.Call(gap < 0 ? call : call[..gap], gap < 0 ? string.Empty : call[(gap + 1)..]));
            case "check" when rest == "commands":
                // The window's command list (docs/axKit/app-kit.md AK-2.4): every key in the F1 text, none shadowed.
                var problems = Current.Send("problems");
                return (problems.Contains("\"problems\":[]", StringComparison.Ordinal), problems);
            case "spoken":
                return (true, string.Join(" | ", _speech.TakeAll()));
            case "keys" when Nvda:
                // Alt+F4 closes the window on purpose, as in the channel's branch below.
                if (rest.Contains("alt+f4", StringComparison.OrdinalIgnoreCase))
                {
                    Current.Closing();
                }

                Native.Keys(rest, InFront);
                return (true, null);
            case "type" when Nvda:
                Native.Type(rest, InFront);
                return (true, null);
            case "keys":
                // One press at a time: each goes to the control that has the focus after the one before.
                foreach (var press in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (press.Equals("alt+f4", StringComparison.OrdinalIgnoreCase))
                    {
                        Current.Closing();
                    }

                    var pressed = Current.Send("keys " + press);
                    if (pressed.Contains("\"error\""))
                    {
                        return (false, pressed);
                    }

                    Thread.Sleep(60);
                }

                return (true, null);
            case "type" or "menu" or "close":
                var reply = Current.Send(line);
                return (!reply.Contains("\"error\""), reply.Contains("\"error\"") ? reply : null);
            default:
                return (false, $"unknown step {verb}");
        }
    }

    /// <summary>A command through cmd.exe in the run's folder; its output is the step's detail.</summary>
    private (bool, string?) RunCommand(string commandLine)
    {
        var start = new ProcessStartInfo("cmd.exe", "/d /s /c \"" + commandLine + "\"")
        {
            WorkingDirectory = temp,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        // Outside call, program, Workflow commands: a workflow's run step, the command line it holds, through cmd.exe in the
        // run's own folder, waited for up to 60 s.
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000))
        {
            process.Kill(entireProcessTree: true);
            return (false, "did not finish within 60 s");
        }

        var text = (output.Result + error.Result).Trim().Replace("\r\n", " / ");
        return (process.ExitCode == 0, $"exit {process.ExitCode}: {text[..Math.Min(text.Length, 300)]}");
    }

    /// <summary>A silent 8 kHz mono 16-bit WAV file.</summary>
    private static void WriteSilence(string path, int milliseconds)
    {
        var samples = (int)(8000L * milliseconds / 1000);
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + samples * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8000);
        writer.Write(16000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples * 2);
        writer.Write(new byte[samples * 2]);
    }

    private Window Current => _current ?? throw new InvalidOperationException("no window started yet");

    private (bool, string?) Expect(string rest, int timeoutMs)
    {
        var space = rest.IndexOf(' ');
        var what = space < 0 ? rest : rest[..space];
        var text = space < 0 ? string.Empty : rest[(space + 1)..];
        var window = Current;
        if (what == "closed")
        {
            return Current.WaitClosed(10_000) ? (true, null) : (false, "the window is still open after 10 s");
        }

        if (what == "mcp")
        {
            // A pairing waits on the window's notice, a window's answer on its thread: up to 60 s, as said does.
            var waited = Stopwatch.StartNew();
            while (waited.ElapsedMilliseconds < 60_000)
            {
                if (_broker?.Take(text) is { } answer)
                {
                    return (true, answer.Length > 400 ? answer[..400] + " …" : answer);
                }

                Thread.Sleep(100);
            }

            return (false, $"no MCP answer holds it; pending: {_broker?.Pending()}");
        }

        if (what == "spoken")
        {
            var until = Stopwatch.StartNew();
            while (until.ElapsedMilliseconds < 15_000)
            {
                if (_speech.Take(text) is { } heard)
                {
                    return (true, string.Join(" | ", heard));
                }

                Thread.Sleep(100);
            }

            return (false, $"NVDA did not say it within 15 s; it said: {string.Join(" | ", _speech.Pending())}");
        }

        if (what == "quiet")
        {
            Thread.Sleep(int.Parse(text));
            var said = window.Pending().Where(e => e.Kind == "say").ToList();
            window.TakeAll();
            return (said.Count == 0, said.Count == 0 ? null : "said: " + string.Join(" | ", said.Select(e => e.Spoken)));
        }

        var timeout = timeoutMs > 0 ? timeoutMs : what == "said" ? 60_000 : 10_000;
        var clock = Stopwatch.StartNew();
        string seen = string.Empty;
        while (clock.ElapsedMilliseconds < timeout)
        {
            if (what == "said")
            {
                if (window.Take(text) is { } taken)
                {
                    var detail = $"{clock.ElapsedMilliseconds} ms: {string.Join(" | ", taken.Events.Select(e => e.Spoken))}";
                    // Later in the announcement the last expectation matched: NVDA was heard for that one already.
                    return Nvda && !taken.Within ? Heard(taken.Events[^1], text, detail) : (true, detail);
                }

                seen = string.Join(" | ", window.Pending().Select(e => e.Spoken));
            }
            else if (what is "focus" or "state" or "list" or "text" or "commands")
            {
                var answer = window.Send(what);
                if (Matches(answer, text))
                {
                    return (true, answer);
                }

                seen = answer;
            }
            else
            {
                return (false, $"unknown expectation {what}");
            }

            Thread.Sleep(100);
        }

        // What NVDA said meanwhile tells where a key went when the window never answered it (a menu opened, NVDA's own
        // command, another program's hotkey).
        var meanwhile = Nvda ? $"; NVDA said meanwhile: {string.Join(" | ", _speech.Pending().Where(h => h != "[cancel]"))}" : string.Empty;
        return (false, $"not within {timeout / 1000} s; seen: {seen}{meanwhile}");
    }

    /// <summary>
    /// A window activated, as the user's switching to it would, before any key or expectation of its own: a start, use
    /// or adopt. On the test desktop that is the window's own thread's activation, which is what makes it say what it
    /// kept for its first activation (AX-7.3); on the user's desktop (the NVDA tier) it is the foreground, and NVDA done
    /// reading the window, since NVDA reads a window that comes up whole and ignores the announcements of a window behind
    /// another program's (announcements were lost under the reading, and a script's second window said things NVDA never
    /// spoke, 2026-10-31).
    /// </summary>
    private void BringToFront(Window window)
    {
        Native.AllowSetForegroundWindow(window.ProcessId);
        window.Send("activate");
        if (Nvda)
        {
            InFront();
            Speech.WaitForQuiet(1500);
        }
    }

    /// <summary>
    /// The NVDA tier: whether a window of this run is in front, bringing the current one back (up to 3 s) when another
    /// program's is, such as the user's own window after a test window closed.
    /// </summary>
    private bool InFront()
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var front = Native.ForegroundProcess();
            if (_windows.Any(window => window.ProcessId == front))
            {
                return true;
            }

            if (_current is not null && attempt % 5 == 0)
            {
                try
                {
                    Native.AllowSetForegroundWindow(_current.ProcessId);
                    _current.Send("activate");
                }
                catch (Exception error) when (error is IOException or InvalidOperationException or TimeoutException)
                {
                    // The current window has closed: nothing of the run to bring back.
                    return false;
                }
            }

            Thread.Sleep(100);
        }

        return false;
    }

    /// <summary>
    /// The NVDA tier's second half of <c>expect said</c> (2026-10-31): what the window announced, NVDA must speak too,
    /// within 5 s, so that every app script is an NVDA check when run with --nvda. By the kind of the event the
    /// expectation matched: an announcement must be heard with the expectation's words; a focus move by the control's
    /// name alone, since NVDA says the role and the value in its own words; a value or a sound is not speech of the
    /// window's and passes as it is (a value the script expects by its text had failed here, 2026-11-01).
    /// </summary>
    private (bool, string?) Heard(Event matched, string text, string detail)
    {
        string words;
        switch (matched.Kind)
        {
            case "say":
                words = Window.Wanted(text).Words;
                break;
            case "focus":
                var role = matched.Text.IndexOf(" (", StringComparison.Ordinal);
                words = role > 0 ? matched.Text[..role] : matched.Text;
                break;
            default:
                return (true, detail);
        }

        if (words.Trim().Length == 0)
        {
            return (true, detail);
        }

        // NVDA's order and the window's differ by a step now and then: a notice closing announces "Not found" and then
        // gives the focus back, and NVDA says the focus first. What the last check took is looked at again, so that two
        // neighbours may come in either order.
        var before = _lastHeard.FindIndex(line => line.Contains(words, StringComparison.OrdinalIgnoreCase));
        if (before >= 0)
        {
            // Spent: a repeated expectation must hear NVDA again (review, 2026-11-02).
            _lastHeard.RemoveAt(before);
            return (true, detail + " || NVDA: with the one before");
        }

        // Five seconds is the mark; ten more tell a slow NVDA from a silent one, the step failed all the same.
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < 15_000)
        {
            if (_speech.Take(words) is { } heard)
            {
                // What came before the match, for a neighbour said in the other order; the match itself is spent.
                _lastHeard = heard.Take(heard.Count - 1).Where(h => h != "[cancel]").ToList();
                var spoken = string.Join(" | ", heard.Where(h => h != "[cancel]"));
                return until.ElapsedMilliseconds <= 5_000
                    ? (true, detail + " || NVDA: " + spoken)
                    : (false, $"{detail} || NVDA spoke it only after {until.ElapsedMilliseconds / 1000} s: {spoken}");
            }

            Thread.Sleep(100);
        }

        return (false, $"{detail} || NVDA did not speak it within 15 s; it said: {string.Join(" | ", _speech.Pending())}");
    }

    private (bool, string?) Refuse(string rest)
    {
        var space = rest.IndexOf(' ');
        var text = rest[(space + 1)..];
        Thread.Sleep(2000);
        var verb = rest[..space];
        if (verb is "focus" or "state" or "list" or "text" or "commands")
        {
            var answer = Current.Send(verb);
            return Matches(answer, text) ? (false, answer) : (true, null);
        }

        if (verb == "mcp")
        {
            // An answer that must not come (an axPass window listed): the answers waiting, none taken.
            return _broker?.Waiting(text) is { } seen ? (false, seen) : (true, null);
        }

        if (rest.StartsWith("spoken", StringComparison.Ordinal))
        {
            var spoken = _speech.Pending().FirstOrDefault(said => said.Contains(text, StringComparison.OrdinalIgnoreCase));
            return spoken is null ? (true, null) : (false, "NVDA said: " + spoken);
        }

        // Of the kind the mark names (Window.Wanted): a refused text never fails on a sound of the same name left over.
        var (fits, words) = Window.Wanted(text);
        var bad = Current.Pending().FirstOrDefault(e => fits(e) && !Current.StaleSound(e) && e.Spoken.Contains(words, StringComparison.OrdinalIgnoreCase));
        if (bad is null && Current.WithinLast(text))
        {
            return (false, "said later in the last announcement matched");
        }

        return bad is null ? (true, null) : (false, $"said at {bad.Ms} ms: {bad.Spoken}");
    }

    /// <summary>
    /// A JSON answer as plain text: every value, its escapes read, joined with " | ", the members as "name: value", so
    /// that a path with backslashes or quotes in it matches the text a script holds.
    /// </summary>
    private static string Flattened(string json)
    {
        try
        {
            var text = new StringBuilder();
            Flatten(JsonSerializer.Deserialize<JsonElement>(json), text);
            return text.ToString();
        }
        catch (JsonException)
        {
            return json;
        }

        static void Flatten(JsonElement element, StringBuilder text)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var member in element.EnumerateObject())
                    {
                        text.Append(member.Name).Append(": ");
                        Flatten(member.Value, text);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Flatten(item, text);
                    }

                    break;
                case JsonValueKind.String:
                    text.Append(element.GetString()).Append(" | ");
                    break;
                default:
                    text.Append(element.ToString()).Append(" | ");
                    break;
            }
        }
    }

    /// <summary>Whether an answer holds the text: as sent, with its quote and backslash escapes read, or flattened to its values.</summary>
    private static bool Matches(string answer, string text) =>
        answer.Contains(text, StringComparison.OrdinalIgnoreCase)
        || answer.Replace("\\\"", "\"").Replace("\\\\", "\\").Contains(text, StringComparison.OrdinalIgnoreCase)
        || Flattened(answer).Contains(text, StringComparison.OrdinalIgnoreCase);

    /// <summary>A window the app started (it inherits the run's automation variables): the newest axKit process with a pipe that is not one of the run's.</summary>
    private (bool, string?) Adopt()
    {
        var clock = Stopwatch.StartNew();
        var known = _windows.Select(window => window.ProcessId).ToHashSet();
        var name = Path.GetFileNameWithoutExtension(exe);
        while (clock.ElapsedMilliseconds < 15_000)
        {
            // Hosted, only a process the run's own windows started: never one of the user's own windows.
            var family = hosted ? Family() : null;
            foreach (var process in Process.GetProcessesByName(name).OrderByDescending(p => p.Id))
            {
                if (known.Contains(process.Id) || (family is not null && !family.Contains(process.Id)) || Window.Attach(process, _secret) is not { } window)
                {
                    continue;
                }

                _windows.Add(window);
                _current = window;
                window.WaitForFocus();
                BringToFront(window);
                return (true, $"process {window.ProcessId}");
            }

            Thread.Sleep(200);
        }

        return (false, "no new axKit window within 15 s");
    }

    /// <summary>Every window's whole timeline, for the report.</summary>
    public string Timeline()
    {
        var text = new StringBuilder();
        foreach (var window in _windows)
        {
            text.AppendLine($"window {_windows.IndexOf(window) + 1} (process {window.ProcessId}):");
            foreach (var e in window.Everything())
            {
                text.AppendLine($"  {e.Ms,7} ms  {e.Spoken}");
            }
        }

        return text.ToString();
    }

    public string LastOfTimeline(int count)
    {
        if (_current is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder("  the last of the timeline:\n");
        var all = _current.Everything();
        foreach (var e in all.Skip(Math.Max(0, all.Count - count)))
        {
            text.AppendLine($"  {e.Ms,7} ms  {e.Spoken}");
        }

        return text.ToString();
    }

    /// <summary>
    /// The processes the run may have made: the runner's descendants on the command line; hosted (axAutomate's window),
    /// the processes the run started itself, still the ones it started (their start times), with their descendants,
    /// never the window's other children, which are the user's own windows.
    /// </summary>
    private HashSet<int> Family()
    {
        if (!hosted)
        {
            return Native.Descendants(Environment.ProcessId);
        }

        var family = new HashSet<int>();
        foreach (var (id, started) in _started)
        {
            try
            {
                using var process = Process.GetProcessById(id);
                if (process.StartTime != started)
                {
                    continue;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                continue;
            }

            family.Add(id);
            family.UnionWith(Native.Descendants(id));
        }

        return family;
    }

    /// <summary>What the end of the run ended besides its own windows (<see cref="Sweep"/>), one line each for the report; no failure.</summary>
    public List<string> Swept { get; } = [];

    /// <summary>Closes every window and says what went wrong outside the steps: a process that ended by itself, an ERROR in a log.</summary>
    public List<string> Finish()
    {
        var problems = new List<string>();
        var known = _windows.Select(window => window.ProcessId).ToHashSet();
        // The runner's descendants are read before its windows close: a window started by a window of the run is found
        // through its parent only while that parent lives. Each is kept with its start time, so that a number reused by
        // another process before the sweep is never taken for it.
        var family = StartTimes(Family());
        _broker?.Dispose();
        _broker = null;
        foreach (var window in _windows)
        {
            if (window.EndedByItself)
            {
                problems.Add($"window {_windows.IndexOf(window) + 1} (process {window.ProcessId}) ended by itself, exit code {window.ExitCode}");
            }

            window.Dispose();
        }

        _windows.Clear();
        Sweep(known, family);
        if (_desktop != 0)
        {
            Native.CloseDesktop(_desktop);
        }

        var logs = Path.Combine(_home, "AxKit", "logs");
        if (Directory.Exists(logs))
        {
            foreach (var file in Directory.GetFiles(logs, "*.log"))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (!line.Contains(" ERROR ", StringComparison.Ordinal) || _allowed.Any(allowed => line.Contains(allowed, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    // An exception's own lines follow its ERROR line without a time stamp: in the report, so that a
                    // crash can be read without the run's log, which goes with its folder (2026-11-01).
                    var problem = new StringBuilder($"{Path.GetFileName(file)}: {line}");
                    for (var j = i + 1; j < lines.Length && j <= i + 8 && lines[j].Length > 0 && !char.IsDigit(lines[j][0]); j++)
                    {
                        problem.Append(" / ").Append(lines[j].Trim());
                    }

                    problems.Add(problem.ToString());
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Ends every process of the run's program file that the run made and does not know as a window: a window a window
    /// of the run started that the script never adopted, or one whose adopt failed (it stayed in front after the night
    /// run of 2026-11-09 and the four scripts after it could not bring theirs up). A process counts as the run's only
    /// when its program file is the run's exe, by its full path, and it is a descendant of the runner (read before the
    /// windows closed and again now) or has a window on the run's own desktop: never by the name alone, so the user's
    /// own axKit, whatever its path, is never touched. Every number is checked against the start time read when it was
    /// found, and the process must have started at or after the runner itself, so a number reused meanwhile by another
    /// process is never ended. Each is ended with its process tree, one line in the report.
    /// </summary>
    private void Sweep(HashSet<int> known, Dictionary<int, DateTime> family)
    {
        var ours = new Dictionary<int, DateTime>(family);
        foreach (var (id, started) in StartTimes(Family()))
        {
            ours.TryAdd(id, started);
        }

        if (_desktop != 0)
        {
            foreach (var (id, started) in StartTimes(Native.DesktopProcesses(_desktop)))
            {
                ours.TryAdd(id, started);
            }
        }

        ours.Remove(Environment.ProcessId);
        DateTime runnerStarted;
        using (var runner = Process.GetCurrentProcess())
        {
            // Hosted, the window's process began long before the run: the run's own beginning is the bound.
            runnerStarted = hosted ? _began : runner.StartTime;
        }

        foreach (var id in ours.Keys.Where(id => !known.Contains(id)).Order())
        {
            Process process;
            try
            {
                process = Process.GetProcessById(id);
            }
            catch (ArgumentException)
            {
                continue;
            }

            using (process)
            {
                try
                {
                    if (process.HasExited
                        || process.StartTime != ours[id]
                        || process.StartTime < runnerStarted
                        || !string.Equals(process.MainModule?.FileName, exe, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    process.Kill(entireProcessTree: true);
                    Swept.Add($"ended process {id}, a process of the run's program not among its windows, with its process tree");
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    // Ended meanwhile, or not the user's to read: not the run's.
                }
            }
        }
    }

    /// <summary>
    /// The start time of each process that is still running, read now; a process that has ended or cannot be read is
    /// left out, and so is never ended.
    /// </summary>
    private static Dictionary<int, DateTime> StartTimes(IEnumerable<int> ids)
    {
        var times = new Dictionary<int, DateTime>();
        foreach (var id in ids)
        {
            try
            {
                using var process = Process.GetProcessById(id);
                times[id] = process.StartTime;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Ended meanwhile, or not the user's to read.
            }
        }

        return times;
    }
}
