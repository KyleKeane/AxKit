using System.Globalization;
using System.Text;
using System.Text.Json;
using AxClaude.Core.Flow;
using AxClaude.Core.Hooks;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The orders a timeline is replayed in (docs/axClaude/events-and-grounding.md EG-11.2). Events and entries share no
/// clock with the console's frames (EG-4.8), and each source can be missing (G5): the core must keep its invariants
/// in every one of these.
/// </summary>
public enum ReplayOrder
{
    /// <summary>As recorded: the probe's hooks were ones Claude Code waited for, so its events come before the marks of their turn's end. An order the window never has; kept as the regression of <c>expected.txt</c>.</summary>
    Recorded,

    /// <summary>Every event 300 ms early.</summary>
    EventsEarly300,

    /// <summary>Every event 300 ms late.</summary>
    EventsLate300,

    /// <summary>Every event 700 ms late: the window's own order, with hooks Claude Code does not wait for (section 4.3: the stop event 424 and 524 after the end mark).</summary>
    EventsLate700,

    /// <summary>Every event 1,200 ms late: the same on a loaded computer.</summary>
    EventsLate1200,

    /// <summary>Every entry of the session file 1 s late.</summary>
    EntriesLate1000,

    /// <summary>No entries: grounding is off, or the file cannot be read. No record.</summary>
    NoEntries,

    /// <summary>No events: an older Claude Code, or the hooks' switch off. The folder rule finds the file (EG-3.2).</summary>
    NoEvents,

    /// <summary>Neither events nor entries: the marks and the screen.</summary>
    Neither,

    /// <summary>No marks, no events, no entries: the screen alone, as before the core.</summary>
    ScreenAlone,
}

/// <summary>
/// Replays one timeline fixture (<c>tests/fixtures/timelines/&lt;name&gt;</c>, EG-11.1, section 6.6) through the
/// code the window uses, and keeps what the core decided (EG-11.2). The fixture's four streams (the console's chunks,
/// the hook events, the session file's entries, what the script sent) are merged by their times and fed as the
/// window feeds them:
///
/// - a chunk to a <see cref="SessionModel"/>, with the app's frame rule: the model ends a frame when the cursor is
///   shown again, and the window's quiet timer ends one <see cref="QuietMs"/> after the last output;
/// - every change of the model through <see cref="Arrivals"/> into a frame of the core
///   (<see cref="FlowFeed.FromFrame"/>), with the window's ready rule: nothing is proposed for speech until a mode
///   line has been read while Claude neither works nor asks; then what stands there counts as spoken and the
///   replayed exchanges get their markers. From then on every reply line is proposed (Speak replies: all);
/// - the model's marks and bells in the order of the stream (EG-1.5);
/// - an event through <see cref="ClaudeCodeHooks.Parse"/> and the window's checks of EG-2.9 into
///   <see cref="FlowFeed.FromHook"/>;
/// - an entry through <see cref="ClaudeCodeTranscript.Parse"/> into <see cref="FlowFeed.FromEntry"/>, once the file
///   is followed: the file a session's start event names, or without one the file the folder rule finds, the one
///   that gained a prompt this window sent, looked for every <see cref="FinderPollMs"/> after a send (EG-3.2). What
///   the file gained before it was followed is read then, in file order (EG-3.3);
/// - what the script sent as the window's own actions: a message through <see cref="SessionModel.Send"/> and
///   <see cref="UserActed"/>, a key into a question as an answer, Escape, Send now;
/// - a question the screen has shown for <see cref="QuestionSettleMs"/> as <see cref="QuestionShown"/>, as the
///   window's settle shows it in the control area;
/// - a tick at every time the core asked to be woken at (EG-4.10);
/// - the process's end <see cref="ExitSettleMs"/> after the last output, as the window's exit settle.
///
/// All of it goes through one queue into one <see cref="SessionFlow"/>, one input at a time (EG-4.8). The clock is
/// the fixture's, in whole milliseconds.
///
/// While it runs the replayer checks the invariants of EG-11.2 against what the stream itself shows, not against
/// the core's own state, and keeps each breach as one line of <see cref="Faults"/>:
///
/// 1. One Done for each turn that ended with its bell and none for any other. A turn ended with its bell when the
///    bell came within <see cref="SessionFlow.BellAfterEndMs"/> of its end mark, and the user's Send now did not cut
///    it (EG-D-14). Its Done is due within <see cref="SessionFlow.DoneCapMs"/> of the bell; only a conversation
///    started afresh or the process's end in that time may take it (EG-4.6, EG-4.9). Where the core gets no marks it
///    says no Done at all: the window's settle does.
/// 2. No reply line released twice: a line, by its id, with the same text.
/// 3. No reply line released outside a turn or after its Done. A turn, as the stream shows it: from a start mark to
///    its end mark, and after the end mark until Done is said, or without a bell for the time the bell and Done may
///    take; or, by the screen, while it shows work and <see cref="SessionFlow.DoneCapMs"/> after it stopped.
/// 4. Released lines stand in the conversation's order: within one decision, and each decision after the one before.
/// 5. The state is never ready between a turn's start mark and its end mark.
/// 6. No decision differs between two runs of the same order: the tests run each twice and compare
///    <see cref="Everything"/>.
///
/// And two of the window's own: the core never asks to be woken in the past again and again (the window's timer
/// would spin), and a tick that still comes after the process's end leads to nothing.
///
/// What the replayer leaves out of the window: the conversation hidden until Claude is ready after
/// <c>--continue</c>, the Enter sent again while a message stays on the prompt row, and everything spoken or
/// shown. None of it is an input of the core.
/// </summary>
internal sealed class TimelineReplay
{
    /// <summary>The window's quiet timer: a frame ends this long after the last output (ConsoleForm).</summary>
    public const int QuietMs = 100;

    /// <summary>The window's exit settle: the process's end is told this long after its last output (ConsoleForm).</summary>
    public const int ExitSettleMs = 300;

    /// <summary>The window's settle before a question is shown in the control area (MainForm.Area).</summary>
    public const int QuestionSettleMs = 400;

    /// <summary>How often the reader looks for the session's file once a message was sent, and for how long (EG-3.2).</summary>
    public const int FinderPollMs = 250;
    public const int FinderGiveUpMs = 10_000;

    private const long Never = long.MaxValue;
    private const long Past = long.MinValue;

    /// <summary>One decision of the core: when, what, its line, and for released reply lines their texts as they stood then.</summary>
    public sealed record Decided(long At, FlowDecision Decision, string Line, IReadOnlyList<string> Texts);

    private enum ItemKind
    {
        Send,
        Chunk,
        Event,
        Entry,
    }

    /// <summary>One thing of a fixture's four streams, with its time on the fixture's clock.</summary>
    private sealed record Item(double At, ItemKind Kind, int Order, int Offset = 0, int Length = 0, byte[]? Json = null, string? Session = null, string? SendKind = null, string? Text = null);

    /// <summary>One session file as the replay has it: the lines written so far, how many of them the reader has read, and from which line on a prompt counts for the folder rule.</summary>
    private sealed class SessionFile
    {
        public readonly List<byte[]> Lines = [];
        public int Read;
        public int Noted;
        public bool HoldsSentPrompt;
    }

    private readonly bool _marks;
    private readonly bool _entries;
    private readonly byte[] _stream;
    private readonly List<Item> _items = [];
    private readonly long _exitAt;

    private readonly SessionModel _model;
    private readonly Arrivals _arrivals = new();
    private readonly SessionFlow _flow;
    private readonly Queue<FlowInput> _inputs = new();
    private bool _draining;
    private long _now;
    private bool _ready;
    private bool _ended;

    // The window's timers, each the time it is due at.
    private long _quietAt = Never;
    private long _questionAt = Never;
    private long _wakeAt = Never;
    private long _finderAt = Never;
    private int _pastWakes;

    // The question the screen shows, for the settle.
    private string? _asked;

    // What the script sent.
    private readonly HashSet<string> _sentKeys = [];
    private bool _afterText;
    private long _finderSentAt;

    // The events' session, and the session files.
    private string? _session;
    private string? _projects;
    private readonly Dictionary<string, SessionFile> _files = [];
    private readonly List<string> _sessions = [];
    private string? _followed;

    // What the stream itself shows, for the invariants: independent of what the core makes of it.
    private bool _markOpen;
    private long _endMarkAt = Past;
    private long _lastEndAt = Past;
    private long _sentNowAt = Past;
    private long _graceUntil = Past;
    private bool _afterDone;
    private bool _screenOpen;
    private long _screenClosedAt = Past;
    private readonly List<long> _afresh = [];
    private int _epoch;
    private readonly List<long> _bellEnds = [];
    private readonly HashSet<(int Id, string Text)> _released = [];
    private Line? _lastReleased;

    private readonly List<Decided> _decisions = [];
    private readonly List<string> _trace = [];
    private readonly HashSet<int> _traced = [];
    private readonly List<string> _faults = [];
    private readonly List<(long At, char Letter)> _marksSeen = [];
    private readonly List<long> _bells = [];

    /// <summary>The names of the timeline fixtures: the folders under tests/fixtures/timelines.</summary>
    public static IReadOnlyList<string> Names() =>
        [.. Directory.GetDirectories(Root()).Select(Path.GetFileName).OfType<string>().OrderBy(name => name, StringComparer.Ordinal)];

    /// <summary>The folder of a fixture.</summary>
    public static string FolderOf(string fixture) => Path.Combine(Root(), fixture);

    /// <summary>
    /// Replays a fixture in an order, from its first chunk to the process's end. <paramref name="entriesLateMs"/>
    /// delays every entry of the session file by that much more, for a test that needs one reading time moved.
    /// </summary>
    public static TimelineReplay Run(string fixture, ReplayOrder order = ReplayOrder.Recorded, int entriesLateMs = 0)
    {
        var replay = new TimelineReplay(fixture, order, entriesLateMs);
        replay.Play();
        return replay;
    }

    private static string Root() => Path.Combine(Fixtures.Folder(), "timelines");

    private TimelineReplay(string fixture, ReplayOrder order, int entriesLateMs)
    {
        Fixture = fixture;
        Order = order;
        _marks = order != ReplayOrder.ScreenAlone;
        var events = order is not (ReplayOrder.NoEvents or ReplayOrder.Neither or ReplayOrder.ScreenAlone);
        _entries = order is not (ReplayOrder.NoEntries or ReplayOrder.Neither or ReplayOrder.ScreenAlone);
        var eventShift = order switch
        {
            ReplayOrder.EventsEarly300 => -300,
            ReplayOrder.EventsLate300 => 300,
            ReplayOrder.EventsLate700 => 700,
            ReplayOrder.EventsLate1200 => 1200,
            _ => 0,
        };
        var entryShift = (order == ReplayOrder.EntriesLate1000 ? 1000 : 0) + entriesLateMs;

        var folder = FolderOf(fixture);
        using (var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "meta.json"))))
        {
            _model = new SessionModel(meta.RootElement.GetProperty("columns").GetInt32(), meta.RootElement.GetProperty("rows").GetInt32());
        }

        // The model's clock is the fixture's, so that two runs are one.
        var start = new DateTime(2026, 11, 15, 12, 0, 0);
        _model.Clock = () => start.AddMilliseconds(_now);
        _model.Marked += OnMark;
        _model.Bell += OnBell;
        _model.Changed += OnChanged;
        _flow = new SessionFlow(Health);

        _stream = File.ReadAllBytes(Path.Combine(folder, "pty.vt"));
        var order0 = 0;
        var lastChunk = 0L;
        foreach (var line in File.ReadLines(Path.Combine(folder, "pty.vt.chunks.txt")))
        {
            if (line.Length == 0 || !char.IsDigit(line[0]))
            {
                continue;
            }

            var parts = line.Split(' ');
            var at = long.Parse(parts[2], CultureInfo.InvariantCulture);
            lastChunk = Math.Max(lastChunk, at);
            _items.Add(new Item(at, ItemKind.Chunk, order0++, Offset: int.Parse(parts[0], CultureInfo.InvariantCulture), Length: int.Parse(parts[1], CultureInfo.InvariantCulture)));
        }

        foreach (var root in JsonLines(Path.Combine(folder, "sends.jsonl")))
        {
            var kind = root.GetProperty("kind").GetString();
            var text = root.TryGetProperty("text", out var sent) ? sent.GetString() : root.TryGetProperty("name", out var name) ? name.GetString() : null;
            _items.Add(new Item(root.GetProperty("t").GetDouble(), ItemKind.Send, order0++, SendKind: kind, Text: text));
        }

        if (events)
        {
            foreach (var root in JsonLines(Path.Combine(folder, "events.jsonl")))
            {
                _items.Add(new Item(root.GetProperty("t").GetDouble() + eventShift, ItemKind.Event, order0++, Json: Encoding.UTF8.GetBytes(root.GetProperty("event").GetRawText())));
            }
        }

        if (_entries)
        {
            foreach (var root in JsonLines(Path.Combine(folder, "entries.jsonl")))
            {
                _items.Add(new Item(root.GetProperty("t").GetDouble() + entryShift, ItemKind.Entry, order0++,
                    Json: Encoding.UTF8.GetBytes(root.GetProperty("entry").GetRawText()), Session: root.GetProperty("session").GetString()));
            }
        }

        // By time; what shares a time keeps the order of its own stream, the streams in the order above.
        _items.Sort((a, b) => a.At != b.At ? a.At.CompareTo(b.At) : a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Order.CompareTo(b.Order));
        _exitAt = lastChunk + ExitSettleMs;
    }

    public string Fixture { get; }

    public ReplayOrder Order { get; }

    /// <summary>The numbers the core, and here the replayer in the window's place, counted (EG-9.4).</summary>
    public SourceHealth Health { get; } = new();

    /// <summary>The model after the replay: the conversation as the reader has it.</summary>
    public SessionModel Model => _model;

    /// <summary>The core after the replay.</summary>
    public SessionFlow Flow => _flow;

    /// <summary>Every decision in the order made, wake times among them.</summary>
    public IReadOnlyList<Decided> Decisions => _decisions;

    /// <summary>Every decision but the wake times as its time and its line (<c>8011 done turn=1 after=74</c>): what a fixture's <c>expected.txt</c> holds for the recorded order (section 6.6).</summary>
    public IReadOnlyList<string> Lines => [.. _decisions.Where(d => d.Decision is not WakeAt).Select(d => $"{d.At} {d.Line}")];

    /// <summary>Every decision with the wake times and the texts released: two runs of one order must give the same.</summary>
    public IReadOnlyList<string> Everything => [.. _decisions.Select(d => $"{d.At} {d.Line}{(d.Texts.Count == 0 ? "" : " | " + string.Join(" | ", d.Texts))}")];

    /// <summary>The inputs and the decisions in order, an input as <c>8009 &gt; mark D</c> and a decision as <c>8011   done turn=1 after=74</c>: for a failing test's message.</summary>
    public IReadOnlyList<string> Trace => _trace;

    /// <summary>The breaches of the invariants, one line each; empty when all held.</summary>
    public IReadOnlyList<string> Faults => _faults;

    /// <summary>The marks of the stream with their times, whether or not the core got them.</summary>
    public IReadOnlyList<(long At, char Letter)> Marks => _marksSeen;

    /// <summary>The times of the stream's bells.</summary>
    public IReadOnlyList<long> Bells => _bells;

    /// <summary>The times of the bells that ended a turn: within the bell's time of an end mark, and not a cut by Send now.</summary>
    public IReadOnlyList<long> BellEnds => _bellEnds;

    /// <summary>The Dones said, in order.</summary>
    public IReadOnlyList<Decided> Dones => [.. _decisions.Where(d => d.Decision is SayDone)];

    /// <summary>The releases of reply lines, in order.</summary>
    public IReadOnlyList<Decided> Speaks => [.. _decisions.Where(d => d.Decision is SpeakReply)];

    /// <summary>The texts of every reply line released, in order.</summary>
    public IReadOnlyList<string> Spoken => [.. Speaks.SelectMany(d => d.Texts)];

    /// <summary>The ids of the session files written or followed, in the order they first were.</summary>
    public IReadOnlyList<string> Sessions => _sessions;

    private static IEnumerable<JsonElement> JsonLines(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0)
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            yield return document.RootElement.Clone();
        }
    }

    // ---- the run ----

    private void Play()
    {
        Enqueue(new ProcessStarted());
        var next = 0;
        while (!_ended)
        {
            var itemAt = next < _items.Count ? _items[next].At : double.MaxValue;
            var timerAt = Math.Min(Math.Min(Math.Min(_quietAt, _questionAt), Math.Min(_wakeAt, _finderAt)), _exitAt);
            if (timerAt <= itemAt)
            {
                Timer(timerAt);
            }
            else
            {
                var item = _items[next++];
                _now = (long)item.At;
                Take(item);
            }
        }

        // The window drops what a Claude Code that is gone still posts. Its timer may still run for a wake time asked
        // before the end: that tick must find nothing due and lead to nothing.
        if (_wakeAt != Never)
        {
            _now = Math.Max(_now, _wakeAt);
            _wakeAt = Never;
            var before = _decisions.Count;
            Enqueue(new Tick());
            if (_decisions.Count != before)
            {
                Fault("a tick after the process's end led to a decision");
            }
        }

        CheckDones();
    }

    /// <summary>The timer due first; of those due at one time the frame's end first and the process's end last.</summary>
    private void Timer(long at)
    {
        _now = Math.Max(_now, at);
        if (at == _quietAt)
        {
            _quietAt = Never;
            _model.EndFrame();
        }
        else if (at == _questionAt)
        {
            _questionAt = Never;
            if (_asked is not null && _asked == Waiting())
            {
                Enqueue(new QuestionShown());
            }
        }
        else if (at == _finderAt)
        {
            FinderPoll();
        }
        else if (at == _wakeAt)
        {
            _wakeAt = Never;
            Enqueue(new Tick());
        }
        else
        {
            // The window's exit: the last frame is closed, then the process is told ended, and nothing more is fed.
            _quietAt = Never;
            _questionAt = Never;
            _finderAt = Never;
            _model.EndFrame();
            _afresh.Add(_now);
            Enqueue(new ProcessEnded());
            _ended = true;
        }
    }

    private void Take(Item item)
    {
        switch (item.Kind)
        {
            case ItemKind.Chunk:
                _model.Feed(_stream.AsSpan(item.Offset, item.Length));
                _quietAt = _now + QuietMs;
                break;
            case ItemKind.Send:
                Sent(item.SendKind, item.Text ?? "");
                break;
            case ItemKind.Event:
                Event(item.Json!);
                break;
            case ItemKind.Entry:
                Written(item.Session ?? "", item.Json!);
                break;
        }
    }

    // ---- the window's queue (EG-4.8) ----

    private void Enqueue(FlowInput? input)
    {
        if (input is null)
        {
            return;
        }

        _inputs.Enqueue(input);
        if (_draining)
        {
            return;
        }

        _draining = true;
        while (_inputs.Count > 0)
        {
            var next = _inputs.Dequeue();
            _trace.Add($"{_now} > {Describe(next)}");
            var turnBefore = _flow.Working || _flow.Ending;
            // The core's list is its own and its next call reuses it: read here, before the next input.
            foreach (var decision in _flow.Handle(next, _now))
            {
                Carry(decision, turnBefore);
            }

            After(next);
        }

        _draining = false;
    }

    /// <summary>Keeps one decision, checks it, and carries out what the window carries out whatever the core is in charge of: the wake time, and the record to follow or to forget.</summary>
    private void Carry(FlowDecision decision, bool turnBefore)
    {
        var line = decision.ToLine();
        IReadOnlyList<string> texts = [];
        switch (decision)
        {
            case WakeAt wake:
                _wakeAt = wake.AtMs;
                _pastWakes = wake.AtMs <= _now ? _pastWakes + 1 : 0;
                if (_pastWakes > 3)
                {
                    Fault($"the core asked to be woken in the past {_pastWakes} times running, at {wake.AtMs}");
                    _wakeAt = _now + 1;
                }

                break;
            case SpeakReply speak:
                texts = [.. speak.Lines.Select(l => l.Text)];
                Released(speak, turnBefore);
                break;
            case SayDone done:
                DoneSaid(done);
                break;
            case FollowRecord follow:
                Follow(Path.GetFileNameWithoutExtension(follow.Path));
                break;
            case ForgetRecord:
                Forget();
                break;
        }

        _decisions.Add(new Decided(_now, decision, line, texts));
        _trace.Add($"{_now}   {line}");
    }

    // ---- the model's events: the window's OnTranscriptChanged, and the marks and bells ----

    private void OnMark(char letter)
    {
        // What the stream shows is noted whether or not the core gets the mark: without marks Done must not come at all.
        _marksSeen.Add((_now, letter));
        MarkOfStream(letter);
        if (_marks)
        {
            Enqueue(FlowFeed.FromMark(letter));
        }
    }

    private void OnBell()
    {
        _bells.Add(_now);
        BellOfStream();
        Enqueue(new BellSeen());
    }

    private void OnChanged()
    {
        if (_ended)
        {
            return;
        }

        var arrival = _arrivals.Update(_model.Lines, _model.Working, _ready ? ReplySpeechMode.All : ReplySpeechMode.None, toolCalls: false);
        TraceLines(arrival);
        ScreenOfStream();
        Enqueue(FlowFeed.FromFrame(_model, arrival, _model.PromptPending, _ready));

        // The window's settle: what Claude waits on is shown once the screen has held it for a moment.
        var waiting = Waiting();
        if (waiting is null)
        {
            _asked = null;
            _questionAt = Never;
        }
        else if (waiting != _asked)
        {
            _asked = waiting;
            _questionAt = _now + QuestionSettleMs;
        }

        if (!_ready && _model.Mode is not null && !_model.Working && !_model.PromptPending)
        {
            // Ready: speech starts here. What stands there is the replayed conversation, there for reading.
            _ready = true;
            _arrivals.Skip(_model.Lines);
            _model.MarkReplayedExchanges();
        }
    }

    private string? Waiting() => _model.PendingQuestion?.Signature ?? _model.PendingScreen?.Signature ?? _model.TextPrompt;

    // ---- what the script sent, as the window's own actions ----

    private void Sent(string? kind, string text)
    {
        if (kind is "message" or "text")
        {
            // A message, or a command typed without its Enter (the Enter follows as a key and is this send's own):
            // the window prints the exchange block and tells the core with the message's key (MainForm.Input).
            _model.Send(text, evenWhilePrompting: true);
            var key = FlowFeed.SentKey(text);
            _sentKeys.Add(key);
            Enqueue(new UserActed(UserAction.Sent, key));
            _afterText = kind == "text";
            _finderSentAt = _now;
            if (_entries && _followed is null)
            {
                _finderAt = _now + FinderPollMs;
            }

            return;
        }

        var ownEnter = _afterText && text == "Enter";
        _afterText = false;
        if (text == "Escape")
        {
            Enqueue(new UserActed(UserAction.Escaped));
        }
        else if (text == "SendNow")
        {
            // The window sends the chord only while the screen shows a waiting message (MainForm.Input.SendNow).
            if (_model.QueuedMessages > 0)
            {
                _sentNowAt = _now;
                Enqueue(new UserActed(UserAction.SentNow));
            }
        }
        else if (!ownEnter && _model.PromptPending)
        {
            // A key into a question or a screen of Claude's: an answer.
            Enqueue(new UserActed(UserAction.Answered));
        }
    }

    // ---- an event, with the window's checks (EG-2.9) ----

    private void Event(byte[] body)
    {
        if (ClaudeCodeHooks.Parse(body) is not { } e)
        {
            Health.HooksMalformed++;
            return;
        }

        Health.HooksReceived++;
        if (e.Kind == HookKind.SessionStarted)
        {
            _session = e.SessionId;
            // The path is used only when it names a session file under a projects folder. The fixture's paths are
            // masked, so the projects folder is the one the first session's own path stands in: a later session's
            // file must lie there too.
            _projects ??= Path.GetDirectoryName(Path.GetDirectoryName(e.RecordPath ?? ""));
            if (_projects is null || !ClaudeCodeTranscript.IsSessionFile(e.RecordPath, [_projects]))
            {
                e = e with { RecordPath = null };
            }
        }
        else if (_session is not null && e.SessionId != _session)
        {
            Health.HooksOtherSession++;
            return;
        }

        Enqueue(FlowFeed.FromHook(e));
    }

    // ---- the session files: the reader's part (EG-3.2, EG-3.3) ----

    /// <summary>A line was written to a session's file: read at once when the file is followed, else kept for when it is.</summary>
    private void Written(string session, byte[] line)
    {
        var file = FileOf(session);
        file.Lines.Add(line);
        if (_followed == session)
        {
            Read(file);
        }
        else if (file.Lines.Count > file.Noted
            && ClaudeCodeTranscript.Parse(line, file.Lines.Count, out _) is { Kind: RecordKind.Prompt, Origin: PromptOrigin.Typed or PromptOrigin.Queued, Text: { } text }
            && _sentKeys.Contains(FlowFeed.SentKey(text)))
        {
            // The folder rule's evidence: a prompt this window sent.
            file.HoldsSentPrompt = true;
        }
    }

    /// <summary>The reader looks at the project folder: the file that gained a prompt this window sent is the session's.</summary>
    private void FinderPoll()
    {
        _finderAt = Never;
        if (_followed is not null)
        {
            return;
        }

        foreach (var session in _sessions)
        {
            if (_files[session].HoldsSentPrompt)
            {
                Follow(session);
                return;
            }
        }

        if (_now - _finderSentAt < FinderGiveUpMs)
        {
            _finderAt = _now + FinderPollMs;
        }
    }

    /// <summary>The reader follows a session's file: the core is told that there is a record, and what the file gained since the start is read.</summary>
    private void Follow(string session)
    {
        if (!_entries || _followed == session)
        {
            return;
        }

        _followed = session;
        _finderAt = Never;
        Health.Record = RecordUse.Followed;
        Enqueue(new RecordFound());
        Read(FileOf(session));
    }

    /// <summary>A session's file, made when it is first written or followed.</summary>
    private SessionFile FileOf(string session)
    {
        if (!_files.TryGetValue(session, out var file))
        {
            _files[session] = file = new SessionFile();
            _sessions.Add(session);
        }

        return file;
    }

    /// <summary>The record followed is no longer the session's: the reader lets it go and notes what every file holds now, so that only a prompt sent from here on finds the new one.</summary>
    private void Forget()
    {
        _followed = null;
        Health.Record = RecordUse.NotFound;
        foreach (var file in _files.Values)
        {
            file.Noted = file.Lines.Count;
            file.Read = file.Lines.Count;
            file.HoldsSentPrompt = false;
        }
    }

    private void Read(SessionFile file)
    {
        while (file.Read < file.Lines.Count)
        {
            // Counted as read before it is handed on: what the core decides on it may bring the reader here again.
            var index = file.Read++;
            var entry = ClaudeCodeTranscript.Parse(file.Lines[index], index, out var what);
            switch (what)
            {
                case RecordLine.Skipped:
                    Health.LinesSkipped++;
                    break;
                case RecordLine.NotJson:
                    Health.LinesNotJson++;
                    break;
                default:
                    Health.EntriesRead++;
                    break;
            }

            if (entry is not null)
            {
                Enqueue(FlowFeed.FromEntry(entry));
            }
        }
    }

    // ---- the invariants (EG-11.2), from what the stream shows ----

    private void Fault(string what) => _faults.Add($"{_now} {what}");

    /// <summary>After the core took an input: the state between a start mark and its end mark is never ready (invariant 5).</summary>
    private void After(FlowInput input)
    {
        if (_marks && _markOpen && !_flow.Working)
        {
            Fault($"the state is ready between a turn's start mark and its end mark (after {Describe(input)})");
        }
    }

    private void MarkOfStream(char letter)
    {
        if (letter == 'A')
        {
            _markOpen = true;
            _afterDone = false;
        }
        else if (letter == 'D')
        {
            _markOpen = false;
            _endMarkAt = _now;
            _lastEndAt = _now;
            // The bell may still come, and Done after it; without a bell the cut reply's rows are drawn in that time.
            _graceUntil = _now + SessionFlow.BellAfterEndMs + SessionFlow.DoneCapMs;
        }
    }

    private void BellOfStream()
    {
        if (_markOpen || _endMarkAt == Past || _now - _endMarkAt > SessionFlow.BellAfterEndMs)
        {
            // A bell alone: a question's.
            return;
        }

        // Each end mark has one bell at most.
        _endMarkAt = Past;
        if (_sentNowAt != Past && _now - _sentNowAt < SessionFlow.CutAfterSendNowMs)
        {
            // The user's Send now cut the reply: an end he asked for, and no Done (EG-D-14).
            _sentNowAt = Past;
            return;
        }

        _bellEnds.Add(_now);
    }

    private void ScreenOfStream()
    {
        if (_model.Epoch != _epoch)
        {
            // The conversation started afresh: a Done owed goes with it (EG-4.9).
            _epoch = _model.Epoch;
            _afresh.Add(_now);
        }

        if (_model.Work == ScreenWork.Working)
        {
            _screenOpen = true;
            _afterDone = false;
        }
        else if (_screenOpen && _model.Work == ScreenWork.Idle && !_model.PromptPending)
        {
            _screenOpen = false;
            _screenClosedAt = _now;
        }
    }

    private void DoneSaid(SayDone done)
    {
        if (!_marks)
        {
            Fault("Done was said though the core got no marks");
        }

        // After a turn's Done nothing of it is released, unless an end mark came since its bell: then a later turn
        // has ended too and its own Done is still owed.
        if (!_markOpen && _lastEndAt <= _now - done.AfterBellMs)
        {
            _afterDone = true;
        }
    }

    private void Released(SpeakReply speak, bool turnBefore)
    {
        // Invariant 3, by the core's own word and by the stream.
        if (!turnBefore && !(_flow.Working || _flow.Ending))
        {
            Fault("lines were released while the core had no turn open or ending");
        }

        var byMarks = _marks && (_markOpen || (!_afterDone && _now <= _graceUntil));
        var byScreen = !_afterDone && (_screenOpen || _now <= _screenClosedAt + SessionFlow.DoneCapMs);
        if (!byMarks && !byScreen)
        {
            Fault(_afterDone ? "lines were released after their turn's Done" : "lines were released outside a turn");
        }

        var lines = _model.Lines;
        var before = _lastReleased is null ? -1 : IndexOf(lines, _lastReleased);
        foreach (var line in speak.Lines)
        {
            // Invariant 2.
            if (!_released.Add((line.Id, line.Text)))
            {
                Fault($"line {line.Id} was released twice with the same text");
            }

            // Invariant 4.
            var at = IndexOf(lines, line);
            if (at < 0)
            {
                Fault($"line {line.Id} was released and is no line of the conversation");
            }
            else if (at <= before)
            {
                Fault($"line {line.Id} was released after a line that stands below it");
            }

            before = Math.Max(before, at);
            _lastReleased = line;
        }
    }

    private static int IndexOf(IReadOnlyList<Line> lines, Line line)
    {
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(lines[i], line))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Invariant 1, at the end: each turn that ended with its bell has its Done within the cap, and there is no other Done.</summary>
    private void CheckDones()
    {
        var dones = _decisions.Where(d => d.Decision is SayDone).Select(d => d.At).ToList();
        if (!_marks)
        {
            // Said and faulted as it came.
            return;
        }

        var next = 0;
        foreach (var bell in _bellEnds)
        {
            if (next < dones.Count && dones[next] >= bell && dones[next] <= bell + SessionFlow.DoneCapMs)
            {
                next++;
                continue;
            }

            // Only a conversation started afresh, or the process's end, takes a Done that is owed.
            if (!_afresh.Any(at => at >= bell && at <= bell + SessionFlow.DoneCapMs))
            {
                _faults.Add($"{bell} the turn that ended with this bell got no Done within {SessionFlow.DoneCapMs} ms");
            }
        }

        for (; next < dones.Count; next++)
        {
            _faults.Add($"{dones[next]} Done was said for no turn that ended with its bell");
        }
    }

    // ---- the trace ----

    /// <summary>What a pass over the conversation found, for the trace: the visible lines that are new, and the lines proposed for speech, each by its first words.</summary>
    private void TraceLines(Arrival arrival)
    {
        var added = new List<string>();
        foreach (var line in _model.Lines)
        {
            if (!line.Hidden && _traced.Add(line.Id))
            {
                added.Add(Start(line.Text));
            }
        }

        if (added.Count > 0 && _ready)
        {
            _trace.Add($"{_now} . new: {string.Join(" | ", added)}");
        }

        if (arrival.Proposed.Count > 0)
        {
            _trace.Add($"{_now} . proposed: {string.Join(" | ", arrival.Proposed.Select(p => $"{p.Kind} {p.Line.Id} {Start(p.Line.Text)}"))}");
        }
    }

    private static string Start(string text) => text.Length <= 48 ? text : text[..48] + "...";

    private static string Describe(FlowInput input) => input switch
    {
        MarkSeen mark => "mark " + mark.Mark switch { TurnMark.Start => "A", TurnMark.Output => "C", _ => "D" },
        BellSeen => "bell",
        FrameSeen frame => $"frame work={frame.Work} asking={frame.Asking} added={frame.LineAdded} speech={frame.SpeechOn} waiting={frame.WaitingShown} proposed={frame.Proposed.Count}",
        UserActed acted => "acted " + acted.Action,
        SessionNamed named => $"named {named.Source} file={(named.RecordPath is null ? "no" : "yes")}",
        TurnStopped stop => $"stop key={(stop.LastReplyKey is null ? "no" : "yes")} tasks={stop.Tasks.Count}",
        EntryRead entry => $"entry {entry.Kind}{(entry.Cut ? " cut" : "")}{(entry.SelfStarted ? " self-started" : "")}",
        AttentionAsked asked => "attention " + asked.Kind,
        _ => input.ToString(),
    };
}
