using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>One axKit process on the test desktop, its automation pipe and its timeline.</summary>
public sealed class Window : IDisposable
{
    private readonly Process _process;
    private readonly NamedPipeClientStream _pipe;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly List<Event> _all = [];

    /// <summary>The events an expectation has taken, by their index in <see cref="_all"/>: the speech and the sounds are two streams (<see cref="Take"/>).</summary>
    private readonly HashSet<int> _taken = [];

    /// <summary>The announcement the last text expectation matched and where its match ended: the next may match later in the same one, two things said as one (AX-7.3).</summary>
    private int _lastSay = -1;

    private int _lastSayEnd;

    /// <summary>The events the last text expectation took, where a neighbour that came the other way round is found.</summary>
    private List<int> _lastBatch = [];

    /// <summary>When the last text expectation's match happened: a sound from well before it is stale (<see cref="Stale"/>).</summary>
    private long _lastTextMs;

    private bool _closing;

    /// <summary>
    /// A sound more than a second older than the last text matched: text expectations do not take the sounds before
    /// them, so a sound from an earlier step would wait there for a later expectation of the same name (a question's
    /// chime answered a later "♪ Notice" at once, and the keys went before the question came, 2026-11-01). The second
    /// keeps a sound that came just before its text, as the ready ping does.
    /// </summary>
    private bool Stale(Event sound) => sound.Ms < _lastTextMs - 1000;

    /// <summary>For a refusal: a sound left over from an earlier step is no failure.</summary>
    public bool StaleSound(Event e) => e.Kind == "sound" && Stale(e);

    private Window(Process process, string secret, int connectMs = 30_000)
    {
        _process = process;
        // CurrentUserOnly: the pipe must be the user's own, never one another program made under the same name.
        // Outside call, pipe, Test automation: the automation channel of a window of the run, opened with the run's secret.
        _pipe = new NamedPipeClientStream(".", $"axkit-auto-{process.Id}", PipeDirection.InOut, PipeOptions.CurrentUserOnly);
        // The window opens its pipe when it first shows.
        _pipe.Connect(connectMs);
        _reader = new StreamReader(_pipe, new UTF8Encoding(false));
        _writer = new StreamWriter(_pipe, new UTF8Encoding(false)) { AutoFlush = true };
        _writer.WriteLine(secret);
        if (_reader.ReadLine() is not { } answer || !answer.Contains("\"ok\":true", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The window of process {process.Id} did not accept the run's secret.");
        }
    }

    /// <summary>A run's secret, 256 random bits, made once per run (<see cref="Run"/>).</summary>
    public static string NewSecret() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    public int ProcessId => _process.Id;

    public bool EndedByItself => !_closing && _process.HasExited;

    public string ExitCode
    {
        get
        {
            try
            {
                return _process.ExitCode.ToString();
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }
    }

    /// <summary>A close the script asked for (Alt+F4): the process ending is no failure.</summary>
    public void Closing() => _closing = true;

    public bool WaitClosed(int milliseconds)
    {
        _closing = true;
        return _process.WaitForExit(milliseconds);
    }

    /// <summary>Waits up to 5 s for the window's first focus, so that the first keys go where a user's would; a window shown without activation has none, which is no error.</summary>
    public void WaitForFocus()
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 5000 && !_process.HasExited)
        {
            if (!Send("focus").Contains("\"name\":null", StringComparison.Ordinal))
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    /// <summary>An axKit process the run did not start, joined through its pipe; null when it has none (yet).</summary>
    public static Window? Attach(Process process, string secret)
    {
        try
        {
            return new Window(process, secret, 500);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or InvalidOperationException)
        {
            // No pipe, or a window of another run that refused this run's secret: not this run's.
            return null;
        }
    }

    /// <summary>A start that hands over to an open window and ends (axMedia, axPreview), started with <see cref="Create"/>: waited for.</summary>
    public static (bool, string?) Launch(int id)
    {
        try
        {
            using var process = Process.GetProcessById(id);
            return process.WaitForExit(30_000) ? (true, $"process {id} ended") : (false, $"process {id} still running after 30 s");
        }
        catch (ArgumentException)
        {
            return (true, $"process {id} ended");
        }
    }

    /// <summary>The window of a process started with <see cref="Create"/>, joined through its pipe with the run's secret.</summary>
    public static Window Start(int id, string secret) => new(Process.GetProcessById(id), secret);

    /// <summary>
    /// AxKit.exe with the arguments, on the desktop (null: the user's own), with the run's secret and folder in its
    /// environment alone: the variables are handed to the child in its own environment block and never set on this
    /// process, so that nothing else this process starts inherits them (security audit S-11; the window axAutomate,
    /// which runs workflows too, starts the user's own windows as well). Returns the process id.
    /// </summary>
    public static int Create(string exe, string arguments, string? desktop, string home, string secret)
    {
        // This process's own block as Windows keeps it, so that the hidden per-drive folders (=C:=C:\...) reach the
        // child as an inherited block would carry them; Environment.GetEnvironmentVariables leaves them out.
        var variables = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var own = Native.GetEnvironmentStringsW();
        try
        {
            for (var at = own; Marshal.PtrToStringUni(at) is { Length: > 0 } entry; at += (entry.Length + 1) * sizeof(char))
            {
                // A name may start with '=' (the per-drive folders): the separator is the first '=' after the first character.
                var equals = entry.IndexOf('=', 1);
                if (equals > 0)
                {
                    variables[entry[..equals]] = entry[(equals + 1)..];
                }
            }
        }
        finally
        {
            Native.FreeEnvironmentStringsW(own);
        }

        // The run's secret (security audit S-11): the windows open their channel to a client that sends it first.
        variables[AxKit.Core.AutomationEnvironment.SecretVariable] = secret;
        // The run's own settings, logs and recently closed list, never the user's (AppPaths).
        variables[AxKit.Core.AutomationEnvironment.HomeVariable] = home;
        var block = new StringBuilder();
        foreach (var (name, value) in variables)
        {
            block.Append(name).Append('=').Append(value).Append('\0');
        }

        block.Append('\0');
        var environment = Marshal.StringToHGlobalUni(block.ToString());
        try
        {
            var startup = new Native.STARTUPINFO { cb = Marshal.SizeOf<Native.STARTUPINFO>(), lpDesktop = desktop is null ? null : "WinSta0\\" + desktop };
            var commandLine = new StringBuilder($"\"{exe}\" {arguments}");
            // Outside call, program, axKit: a window of a workflow's run (axAutomate's window or tools/AxAutomate), on the
            // run's own desktop with the run's secret and folder, so that it never takes the user's keyboard or settings.
            // 0x400 is CREATE_UNICODE_ENVIRONMENT.
            if (!Native.CreateProcessW(null, commandLine, 0, 0, false, 0x400, environment, null, ref startup, out var info))
            {
                throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");
            }

            Native.CloseHandle(info.hThread);
            Native.CloseHandle(info.hProcess);
            return info.dwProcessId;
        }
        finally
        {
            Marshal.FreeHGlobal(environment);
        }
    }

    public string Send(string command)
    {
        if (command == "close")
        {
            _closing = true;
        }

        if (_hung)
        {
            return HungAnswer;
        }

        _writer.WriteLine(command);
        // A limit on the answer (docs/axKit/repairs.json R-34): a window whose thread hangs with its process alive never
        // answers, and the run waited for ever, in the NVDA tier with the keyboard taken. The read that timed out still
        // holds the pipe, so the window answers nothing more in this run.
        var answer = _reader.ReadLineAsync();
        if (!answer.Wait(AnswerLimitMs))
        {
            _hung = true;
            return HungAnswer;
        }

        return answer.Result ?? "{\"error\":\"the window closed\"}";
    }

    /// <summary>How long a window may take to answer one request of the run.</summary>
    private const int AnswerLimitMs = 30_000;

    /// <summary>What a window that did not answer in time answers from then on: the step fails with it, and so does every later step on the window.</summary>
    private string HungAnswer => $"{{\"error\":\"the window (process {ProcessId}) did not answer within {AnswerLimitMs / 1000} seconds: its thread is busy or hung; see the window's log in the run's home folder (--keep keeps it)\"}}";

    private bool _hung;

    /// <summary>The events not yet taken by an expectation, after fetching what the window has recorded since the last look.</summary>
    public List<Event> Pending()
    {
        Fetch();
        var pending = new List<Event>();
        for (var i = 0; i < _all.Count; i++)
        {
            if (!_taken.Contains(i))
            {
                pending.Add(_all[i]);
            }
        }

        return pending;
    }

    /// <summary>
    /// The first pending event that holds the text, with every pending event of its stream before it, all taken; null
    /// when none does. The sounds ("♪ …") and the rest are two streams (2026-11-01): a sound plays at once while the
    /// text it goes with may wait to be joined to another (AX-7.3), so their order is not the window's to promise, and a
    /// text expectation never takes a sound, nor a sound expectation a text. A text found later in the announcement the
    /// last expectation matched counts too (<see cref="Taken.Within"/>): "Sent. hello. Done" answers "Sent" and then
    /// "Done", as it does for a listener.
    /// </summary>
    public Taken? Take(string text)
    {
        Fetch();
        var sound = text.StartsWith('♪');
        var (fits, words) = Wanted(text);
        // An announcement only, and words to find: what the user typed into a field is a value, not something said as
        // one, and a bare "!" (any interrupting announcement) must find a new one, since "" is found anywhere.
        if (!sound && words.Length > 0 && _lastSay >= 0 && _all[_lastSay].Kind == "say" && fits(_all[_lastSay]))
        {
            var within = _all[_lastSay].Spoken.IndexOf(words, _lastSayEnd, StringComparison.OrdinalIgnoreCase);
            if (within >= 0)
            {
                _lastSayEnd = within + words.Length;
                return new Taken([_all[_lastSay]], Within: true);
            }
        }

        var indices = new List<int>();
        for (var i = 0; i < _all.Count; i++)
        {
            if (_taken.Contains(i) || (_all[i].Kind == "sound") != sound)
            {
                continue;
            }

            if (sound && Stale(_all[i]))
            {
                _taken.Add(i);
                continue;
            }

            indices.Add(i);
            var at = fits(_all[i]) ? _all[i].Spoken.IndexOf(words, StringComparison.OrdinalIgnoreCase) : -1;
            if (at >= 0)
            {
                _taken.UnionWith(indices);
                if (!sound)
                {
                    _lastSay = i;
                    _lastSayEnd = at + words.Length;
                    _lastBatch = indices;
                    _lastTextMs = _all[i].Ms;
                }

                return new Taken(indices.Select(index => _all[index]).ToList(), Within: false);
            }
        }

        // Two neighbours in either order: the text in what the last expectation took counts too (a Fetch and a Pull
        // pressed at once say "Working…" and "Fetched" in the order the keys' pace decides, 2026-11-01). Never the event
        // that expectation matched itself, and each neighbour once: a repeated expectation must find a new event, or a
        // window that fell silent on the second press would pass (review, 2026-11-02).
        if (!sound && words.Length > 0)
        {
            foreach (var i in _lastBatch.Where(i => i != _lastSay).ToList())
            {
                if (fits(_all[i]) && _all[i].Spoken.Contains(words, StringComparison.OrdinalIgnoreCase))
                {
                    _lastBatch.Remove(i);
                    return new Taken([_all[i]], Within: false);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// What an expectation's text asks for: its mark names the kind of event ("♪ " a sound, "! " an interrupting
    /// announcement, "→ " a focus move, "· " a value; none, anything but a sound), and the rest is the words to find in
    /// it. The mark is a kind, not the start of the text: "Sent" with "Background music on" joined to it reads
    /// "! Sent. Background music on" (AX-7.3), and "! Background music on" must still find it.
    /// </summary>
    public static (Func<Event, bool> Fits, string Words) Wanted(string text)
    {
        if (text.StartsWith('♪'))
        {
            return (e => e.Kind == "sound", text.TrimStart('♪', ' '));
        }

        if (text == "!" || text.StartsWith("! ", StringComparison.Ordinal))
        {
            // "!" alone: any interrupting announcement (a question's name, whatever it is); nothing for NVDA to be heard with.
            return (e => e.Kind == "say" && e.Interrupt, text.Length > 2 ? text[2..] : string.Empty);
        }

        if (text.StartsWith("→ ", StringComparison.Ordinal))
        {
            return (e => e.Kind == "focus", text[2..]);
        }

        if (text.StartsWith("· ", StringComparison.Ordinal))
        {
            return (e => e.Kind == "value", text[2..]);
        }

        return (e => e.Kind != "sound", text);
    }

    /// <summary>Whether the text comes later in the announcement the last expectation matched: a refusal looks there too.</summary>
    public bool WithinLast(string text)
    {
        var (fits, words) = Wanted(text);
        return words.Length > 0 && _lastSay >= 0 && _all[_lastSay].Kind == "say" && fits(_all[_lastSay]) && _all[_lastSay].Spoken.IndexOf(words, _lastSayEnd, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public List<Event> TakeAll()
    {
        var pending = Pending();
        _taken.UnionWith(Enumerable.Range(0, _all.Count));
        _lastSay = -1;
        _lastBatch = [];
        _lastTextMs = _all.Count > 0 ? _all[^1].Ms : _lastTextMs;
        return pending;
    }

    public List<Event> Everything()
    {
        try
        {
            Fetch();
        }
        catch (IOException)
        {
            // The window is gone; what was fetched stays.
        }

        return _all;
    }

    private void Fetch()
    {
        if (_process.HasExited)
        {
            return;
        }

        using var document = JsonDocument.Parse(Send("said"));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var record in document.RootElement.EnumerateArray())
        {
            var item = record.GetProperty("item");
            _all.Add(new Event(record.GetProperty("t").GetInt64(), item.GetProperty("kind").GetString()!, item.GetProperty("text").GetString() ?? string.Empty, item.GetProperty("interrupt").GetBoolean()));
        }
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                Send("close");
                if (!_process.WaitForExit(5000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // Gone already, or its pipe with it.
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (Exception) when (_process.HasExited)
            {
            }
        }

        _pipe.Dispose();
        _process.Dispose();
    }
}
