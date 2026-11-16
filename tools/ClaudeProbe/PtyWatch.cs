using System.Text;
using System.Text.Json.Nodes;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace Probe;

/// <summary>
/// The pseudo console's output: the raw bytes in pty.vt with the index beside it in the repository's format
/// (AxKit.Core/Pty/StreamRecorder: "offset length elapsedMs", here on the probe's clock), so that
/// tools/PtyCapture --dump reads the file; and every chunk with its time in pty-events.jsonl.
/// </summary>
internal sealed class PtyRecorder : IDisposable
{
    private readonly object _gate = new();
    private readonly FileStream _file;
    private readonly StreamWriter _index;
    private readonly MemoryStream _all = new();
    private bool _disposed;

    public PtyRecorder(string path)
    {
        _file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        _index = new StreamWriter(path + ".chunks.txt", false, new UTF8Encoding(false)) { AutoFlush = true };
        _index.WriteLine("# offset length elapsedMs");
    }

    public long Length { get; private set; }

    public double LastAt { get; private set; }

    /// <summary>Appends a chunk; returns the offset it starts at.</summary>
    public long Write(byte[] data, double t)
    {
        lock (_gate)
        {
            var offset = Length;
            if (_disposed || data.Length == 0)
            {
                return offset;
            }

            _index.WriteLine($"{offset} {data.Length} {(long)t}");
            _file.Write(data);
            _file.Flush();
            _all.Write(data);
            Length += data.Length;
            LastAt = t;
            return offset;
        }
    }

    /// <summary>The raw output from an offset on, as text (UTF-8, escapes and all): what a script's waitRaw matches.</summary>
    public string TextSince(long offset)
    {
        lock (_gate)
        {
            offset = Math.Min(offset, _all.Length);
            return Encoding.UTF8.GetString(_all.GetBuffer(), (int)offset, (int)(_all.Length - offset));
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                _disposed = true;
                _index.Dispose();
                _file.Dispose();
            }
        }
    }
}

/// <summary>
/// The screen events in the byte stream, each with the time of the chunk that completed it: BEL, OSC sequences
/// (133 marks, titles, any other), the cursor hidden and shown (the brackets of a frame), other private modes,
/// the screen cleared, the cursor homed. A sequence split over two chunks is completed by the second.
/// </summary>
internal sealed class VtScanner(Jsonl log)
{
    private enum State { Ground, Escape, Csi, Osc, OscEscape, Text, TextEscape }

    private readonly StringBuilder _csi = new();
    private readonly List<byte> _osc = [];
    private State _state;

    public void Feed(byte[] data, double t, long offset)
    {
        for (var i = 0; i < data.Length; i++)
        {
            var b = data[i];
            switch (_state)
            {
                case State.Ground:
                    if (b == 0x1b)
                    {
                        _state = State.Escape;
                    }
                    else if (b == 0x07)
                    {
                        Emit(t, offset + i, "bel");
                    }

                    break;
                case State.Escape:
                    switch (b)
                    {
                        case (byte)'[':
                            _csi.Clear();
                            _state = State.Csi;
                            break;
                        case (byte)']':
                            _osc.Clear();
                            _state = State.Osc;
                            break;
                        case (byte)'P' or (byte)'X' or (byte)'^' or (byte)'_':
                            _state = State.Text;
                            break;
                        default:
                            _state = State.Ground;
                            break;
                    }

                    break;
                case State.Csi:
                    if (b is >= 0x40 and <= 0x7e)
                    {
                        Csi(_csi.ToString(), (char)b, t, offset + i);
                        _state = State.Ground;
                    }
                    else if (b == 0x1b)
                    {
                        _state = State.Escape;
                    }
                    else
                    {
                        _csi.Append((char)b);
                    }

                    break;
                case State.Osc:
                    if (b == 0x07)
                    {
                        Osc(t, offset + i);
                        _state = State.Ground;
                    }
                    else if (b == 0x1b)
                    {
                        _state = State.OscEscape;
                    }
                    else
                    {
                        _osc.Add(b);
                    }

                    break;
                case State.OscEscape:
                    Osc(t, offset + i);
                    _state = b == (byte)'\\' ? State.Ground : State.Escape;
                    break;
                case State.Text:
                    if (b == 0x1b)
                    {
                        _state = State.TextEscape;
                    }

                    break;
                case State.TextEscape:
                    _state = b == (byte)'\\' ? State.Ground : State.Text;
                    break;
            }
        }
    }

    private void Csi(string parameters, char final, double t, long offset)
    {
        if (parameters.StartsWith('?') && final is 'h' or 'l')
        {
            foreach (var mode in parameters[1..].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (mode == "25")
                {
                    Emit(t, offset, final == 'h' ? "cursor-show" : "cursor-hide");
                }
                else
                {
                    log.Write(new JsonObject { ["t"] = t, ["k"] = "mode", ["mode"] = "?" + mode, ["set"] = final == 'h', ["offset"] = offset });
                }
            }
        }
        else if (final == 'J' && parameters is "2" or "3")
        {
            log.Write(new JsonObject { ["t"] = t, ["k"] = "clear", ["param"] = parameters, ["offset"] = offset });
        }
        else if (final == 'H' && parameters is "" or "1;1")
        {
            Emit(t, offset, "home");
        }
    }

    private void Osc(double t, long offset)
    {
        var content = Encoding.UTF8.GetString(_osc.ToArray());
        var separator = content.IndexOf(';');
        var code = separator < 0 ? content : content[..separator];
        var payload = separator < 0 ? string.Empty : content[(separator + 1)..];
        if (code is "0" or "2")
        {
            log.Write(new JsonObject { ["t"] = t, ["k"] = "title", ["text"] = payload, ["offset"] = offset });
        }
        else
        {
            log.Write(new JsonObject { ["t"] = t, ["k"] = "osc", ["code"] = code, ["data"] = payload, ["offset"] = offset });
        }
    }

    private void Emit(double t, long offset, string kind) => log.Write(new JsonObject { ["t"] = t, ["k"] = kind, ["offset"] = offset });
}

/// <summary>
/// The repository's own model of Claude Code's screen (AxClaude.Core's SessionModel), fed the same bytes and framed
/// as the window frames it (a frame ends when the cursor is shown again, and after 100 ms without output), so that a
/// run says what the app would have concluded and when: its status (starting, asking, working, ready), the question
/// or screen it sees pending, each visible line as it appears, changes and is committed. The rows a frame changed
/// (chrome included) go to pty-events.jsonl as that frame's record. The window adds timers of its own on top of the
/// model (for one, "Done" 400 ms after the bell): those are not here.
/// </summary>
internal sealed class ModelWatch : IDisposable
{
    private readonly record struct Seen(string Text, LineKind Kind, bool Hidden, bool Committed);

    private readonly object _gate = new();
    private readonly SessionModel _model;
    private readonly Jsonl _pty;
    private readonly Jsonl _app;
    private readonly Dictionary<int, Seen> _seen = new();
    private readonly Timer _timer;
    private string _statusKey = string.Empty;
    private string _why = "feed";
    private double _lastData;
    private bool _dirty;

    public ModelWatch(int columns, int rows, Jsonl pty, Jsonl app)
    {
        _pty = pty;
        _app = app;
        _model = new SessionModel(columns, rows);
        _model.Changed += OnChanged;
        _model.Bell += () => _app.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "bell" });
        _model.ResponseStarted += () => _app.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "response-started" });
        _model.Announcement += text => _app.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "announcement", ["text"] = text });
        _timer = new Timer(_ => Tick(), null, 20, 20);
    }

    public bool Dirty
    {
        get
        {
            lock (_gate)
            {
                return _dirty;
            }
        }
    }

    public void Feed(byte[] data, double t)
    {
        lock (_gate)
        {
            _why = "feed";
            try
            {
                _model.Feed(data);
            }
            catch (Exception ex)
            {
                _app.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "model-error", ["where"] = "Feed", ["error"] = ex.GetType().Name + ": " + ex.Message });
            }

            _lastData = t;
            _dirty = true;
        }
    }

    /// <summary>The frame's end after 100 ms of quiet, as the window's quiet timer does; also called once at the exit.</summary>
    public void Tick(bool force = false)
    {
        if (!Monitor.TryEnter(_gate, force ? 2000 : 0))
        {
            return;
        }

        try
        {
            if (!force && (!_dirty || Clock.Ms - _lastData < 100))
            {
                return;
            }

            _dirty = false;
            _why = "quiet";
            try
            {
                _model.EndFrame();
            }
            catch (Exception ex)
            {
                _app.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "model-error", ["where"] = "EndFrame", ["error"] = ex.GetType().Name + ": " + ex.Message });
            }
        }
        finally
        {
            Monitor.Exit(_gate);
        }
    }

    /// <summary>The app's exchange block for a message it sends (SessionModel.Send), so that the model stands as the window's would.</summary>
    public void Send(string text)
    {
        lock (_gate)
        {
            _why = "send";
            _model.Send(text);
        }
    }

    public string Status()
    {
        lock (_gate)
        {
            return StatusLocked();
        }
    }

    public bool PromptHeld(string text)
    {
        lock (_gate)
        {
            return !_model.Working && !_model.PromptPending && _model.PromptHolds(text);
        }
    }

    /// <summary>The model's last lines as text, one per line: what a script's waitFor and if match.</summary>
    public string Text(int last, bool visibleOnly, string? kind)
    {
        lock (_gate)
        {
            var lines = _model.Lines;
            var sb = new StringBuilder();
            for (var i = Math.Max(0, lines.Count - last); i < lines.Count; i++)
            {
                var line = lines[i];
                if ((visibleOnly && line.Hidden) || (kind is not null && !line.Kind.ToString().Equals(kind, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                sb.Append(line.Text).Append('\n');
            }

            return sb.ToString();
        }
    }

    public void Dispose() => _timer.Dispose();

    private string StatusLocked() =>
        _model.PromptPending ? "asking"
        : _model.Mode is null ? "starting"
        : _model.Working ? "working"
        : "ready";

    // Raised by the model at the end of every frame (inside Feed, EndFrame or Send, so under the lock).
    private void OnChanged()
    {
        var t = Clock.Ms;
        var lines = _model.Lines;
        var rows = new JsonArray();
        for (var i = Math.Max(0, lines.Count - 800); i < lines.Count; i++)
        {
            var line = lines[i];
            var now = new Seen(line.Text, line.Kind, line.Hidden, line.Committed);
            var known = _seen.TryGetValue(line.Id, out var before);
            if (known && before == now)
            {
                continue;
            }

            _seen[line.Id] = now;
            var textChanged = !known || before.Text != now.Text;
            if (textChanged && !line.IsMarker)
            {
                rows.Add(new JsonObject { ["id"] = line.Id, ["hidden"] = line.Hidden, ["kind"] = line.Kind.ToString(), ["text"] = line.Text });
            }

            if (line.Text.Length == 0)
            {
                continue;
            }

            string? op = null;
            if (!line.Hidden)
            {
                op = !known ? "add"
                    : before.Hidden ? "show"
                    : textChanged ? "change"
                    : before.Kind != now.Kind ? "kind"
                    : now.Committed && !before.Committed ? "commit"
                    : null;
            }
            else if (known && !before.Hidden)
            {
                op = "hide";
            }

            if (op is not null)
            {
                _app.Write(new JsonObject
                {
                    ["t"] = t, ["k"] = "line", ["op"] = op, ["id"] = line.Id, ["kind"] = line.Kind.ToString(),
                    ["committed"] = line.Committed, ["text"] = line.Text,
                });
            }
        }

        if (rows.Count > 0)
        {
            _pty.Write(new JsonObject { ["t"] = t, ["k"] = "frame", ["why"] = _why, ["changed"] = rows.Count, ["rows"] = rows });
        }

        var status = StatusLocked();
        var key = string.Join('|', status, _model.Working, _model.PromptPending, _model.Mode, _model.QueuedMessages,
            _model.PendingQuestion?.Title, _model.PendingScreen?.Title, _model.Background, _model.TranscriptViewOpen,
            _model.AwaitsText, _model.BackgroundAgents.Count, _model.BackgroundAgentsAwaited, _model.SessionName, _model.FeedbackDrafts);
        if (key == _statusKey)
        {
            return;
        }

        _statusKey = key;
        var record = new JsonObject
        {
            ["t"] = t,
            ["k"] = "status",
            ["status"] = status,
            ["why"] = _why,
            ["working"] = _model.Working,
            ["promptPending"] = _model.PromptPending,
            ["mode"] = _model.Mode,
            ["spinner"] = _model.Spinner,
            ["queued"] = _model.QueuedMessages,
            ["question"] = _model.PendingQuestion?.Title,
            ["questionOptions"] = _model.PendingQuestion?.Options.Count,
            ["screen"] = _model.PendingScreen?.Title,
            ["awaitsText"] = _model.AwaitsText,
            ["background"] = _model.Background,
            ["agents"] = _model.BackgroundAgents.Count,
            ["agentsAwaited"] = _model.BackgroundAgentsAwaited,
            ["detailedView"] = _model.TranscriptViewOpen,
            ["sessionName"] = _model.SessionName,
        };
        _app.Write(record);
    }
}
