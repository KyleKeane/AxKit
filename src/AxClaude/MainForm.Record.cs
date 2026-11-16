using AxClaude.Core.Flow;
using AxClaude.Core.Record;
using AxKit.Core;

namespace AxClaude;

/// <summary>
/// The reader of the session's record on a thread of its own, "AxKit record", one for each running program
/// (docs/axClaude/events-and-grounding.md, "EG" below, EG-3.2, EG-3.3, EG-3.6 and section 7.2). Neutral: the project
/// folders and the program's reader of one line are handed in, and what comes back are <see cref="RecordEntry"/>
/// records. The thread notes the folders' files with their lengths as it starts, which is the moment the program
/// starts, and then either follows the file it was told (<see cref="Follow"/>, the path an event named) or, while it
/// was told none, looks for it by the folder rule: for 10 s after each message the window sent (<see cref="Sent"/>),
/// every 250 ms, for the file that gained a prompt with that message's letters. The file followed is looked at every
/// 50 ms while a turn is open and for 5 s after its end, every 500 ms otherwise (<see cref="Turn"/>).
///
/// The threads. The window's thread calls every public method and gets every callback; it never waits for the
/// reader's thread, and <see cref="Stop"/> only tells that thread to end. The reader's thread touches no control and
/// no <see cref="SourceHealth"/>: what it read waits in an inbox, one post for a burst, and what it counted waits in
/// fields of its own, which the delivery on the window's thread adds to the health. A delivery that arrives after
/// <see cref="Stop"/> is dropped: its program is no longer the window's.
///
/// Nothing of a file's text goes anywhere but into the entries handed over: the log gets fixed words and numbers,
/// never a path and never an exception's message, which names one (EG-9.5, EG-10.3).
/// </summary>
internal sealed class RecordHost
{
    /// <summary>How often the file followed is looked at while a turn is open, and for <see cref="AfterTurnMs"/> after its end (EG-3.3).</summary>
    public const int TurnPollMs = 50;

    /// <summary>How often the file followed is looked at otherwise (EG-3.3).</summary>
    public const int IdlePollMs = 500;

    /// <summary>How long after a turn's end the file is still looked at as often as inside it (EG-3.3): the turn's last entries come after its end.</summary>
    public const int AfterTurnMs = 5000;

    /// <summary>How often the folder rule looks at the project folders while it searches (EG-3.2).</summary>
    public const int SearchPollMs = 250;

    /// <summary>How long after a sent message the folder rule searches; then it rests until the next one (EG-3.2).</summary>
    public const int SearchForMs = 10_000;

    /// <summary>The newest this many replies are kept for <see cref="Reply"/>.</summary>
    public const int RepliesKept = 2000;

    /// <summary>The newest this many sent messages' keys are kept for the folder rule; the file is found by one of the first.</summary>
    private const int KeysKept = 50;

    /// <summary>The word of the diagnostics for a reader that ended on an exception (the three of <see cref="RecordTailStop"/> are <c>json</c>, <c>fields</c> and <c>file</c>).</summary>
    private const string FailedWord = "error";

    /// <summary>
    /// What one look of the reader's thread hands to the window's thread, in the order to carry it out: the file was
    /// opened for the first time (<paramref name="Found"/>, its path, null at every other look;
    /// <paramref name="ByRule"/> when the folder rule found it), then the entries read, then whether the file is no
    /// longer followed (<paramref name="Lost"/>: null while it is, empty when it was let go for another or forgotten,
    /// else the one word why the reader stopped).
    /// </summary>
    private sealed record Batch(IReadOnlyList<RecordEntry> Entries, string? Found, bool ByRule, string? Lost);

    private static readonly IReadOnlyList<RecordEntry> NoEntries = [];

    private readonly Control _owner;
    private readonly IReadOnlyList<string> _projectFolders;
    private readonly RecordLineParser _parse;
    private readonly Action<IReadOnlyList<RecordEntry>> _deliver;
    private readonly Action<bool> _found;
    private readonly SourceHealth _health;

    // ---- shared by the two threads, read and written under _gate ----

    private readonly object _gate = new();

    /// <summary>Raised whenever the window says which file to follow, or to forget it: the reader's thread acts when the number changed.</summary>
    private int _asked;

    /// <summary>The file the window said to follow; null for none, which leaves the finding to the folder rule.</summary>
    private string? _askedPath;

    /// <summary>The folder rule found the file since the window last said anything: no more keys are needed.</summary>
    private bool _ruleFound;

    /// <summary>The keys of the messages sent while no file was known, oldest first.</summary>
    private readonly List<string> _keys = [];

    /// <summary>The folder rule searches until this time on the window's clock.</summary>
    private long _searchUntil;

    private readonly List<Batch> _inbox = [];

    /// <summary>A delivery is on its way to the window's thread: what is added to the inbox goes with it.</summary>
    private bool _posted;

    /// <summary>The reader's thread was told to look now; kept so that a wake that comes before the wait is not lost.</summary>
    private bool _woken;

    // ---- written by one thread and read by the other, without the lock ----

    private volatile bool _stopped;

    /// <summary>The core has a turn open or ending: the pace of the looks (EG-3.3).</summary>
    private volatile bool _turnOpen;

    /// <summary>Until this time on the window's clock the pace stays that of a turn: 5 s after the last one's end.</summary>
    private long _quickUntil;

    // What the reader's thread counted since the last delivery (Interlocked), which the delivery adds to the health.
    private int _skipped;
    private int _notJson;
    private int _tooLong;

    // ---- the reader's thread alone ----

    private readonly RecordFinder _finder = new();

    /// <summary>The tail of the file followed; null while none is.</summary>
    private RecordTail? _tail;

    /// <summary>The tail's file was there at a look, and the window was told that it is followed.</summary>
    private bool _opened;

    /// <summary>The folder rule found the tail's file.</summary>
    private bool _byRule;

    // How much of the tail's own counters went into the counts above.
    private int _tailSkipped;
    private int _tailNotJson;
    private int _tailTooLong;

    // ---- the window's thread alone ----

    /// <summary>The replies read, by their ids, and the order they came in, so that the oldest goes first.</summary>
    private readonly Dictionary<string, RecordEntry> _replies = new(StringComparer.Ordinal);

    private readonly Queue<string> _replyOrder = new();

    private RecordHost(Control owner, IReadOnlyList<string> projectFolders, RecordLineParser parse, Action<IReadOnlyList<RecordEntry>> deliver, Action<bool> found, SourceHealth health)
    {
        _owner = owner;
        _projectFolders = projectFolders;
        _parse = parse;
        _deliver = deliver;
        _found = found;
        _health = health;
    }

    /// <summary>
    /// Starts the reader's thread for the program that was just started. <paramref name="projectFolders"/>: the
    /// folders a file of this session can lie in. <paramref name="parse"/>: the program's reader of one line. On the
    /// window's thread, in the order of the file: <paramref name="found"/> with true when a file is followed and with
    /// false when it no longer is, <paramref name="deliver"/> with the entries read; and the counts and the words of
    /// the session file in <paramref name="health"/>, which only that thread touches.
    /// </summary>
    public static RecordHost Start(Control owner, IReadOnlyList<string> projectFolders, RecordLineParser parse, Action<IReadOnlyList<RecordEntry>> deliver, Action<bool> found, SourceHealth health)
    {
        var host = new RecordHost(owner, [.. projectFolders], parse, deliver, found, health);
        var thread = new Thread(host.Run) { IsBackground = true, Name = "AxKit record" };
        thread.Start();
        return host;
    }

    /// <summary>
    /// Follow this file: an event named it (EG-3.2 item 1). Nothing for the path already asked for, and nothing more
    /// than that when the folder rule found the same file; any other file followed is let go, and this one is read
    /// from the length it had when the program started (0 for one that was not there, its end for one outside the
    /// folders noted). A file that does not exist yet is followed once it does.
    /// </summary>
    public void Follow(string path)
    {
        lock (_gate)
        {
            if (_askedPath is not null && SamePath(_askedPath, path))
            {
                return;
            }
        }

        Ask(path);
    }

    /// <summary>Forget the file: the conversation started afresh and nothing names the new one's. The folders are noted again as they are now, and the folder rule looks from the next sent message on.</summary>
    public void Forget() => Ask(null);

    /// <summary>The window sent a message with these letters: while no file is known, the folder rule searches for the next 10 s (EG-3.2 item 2). A message without a letter finds nothing.</summary>
    public void Sent(string key)
    {
        if (key.Length == 0)
        {
            return;
        }

        lock (_gate)
        {
            if (_askedPath is not null || _ruleFound)
            {
                return;
            }

            _keys.Remove(key);
            _keys.Add(key);
            if (_keys.Count > KeysKept)
            {
                _keys.RemoveAt(0);
            }

            _searchUntil = Environment.TickCount64 + SearchForMs;
            WakeLocked();
        }
    }

    /// <summary>The core has a turn open or ending, or no longer (EG-3.3): the looks come every 50 ms from now, or go back to every 500 ms 5 s from now.</summary>
    public void Turn(bool open)
    {
        if (_turnOpen && !open)
        {
            Volatile.Write(ref _quickUntil, Environment.TickCount64 + AfterTurnMs);
        }

        _turnOpen = open;
        if (open)
        {
            lock (_gate)
            {
                WakeLocked();
            }
        }
    }

    /// <summary>The program was stopped, or the user's switch went off: the thread is told to end and is not waited for; nothing it still hands over is delivered.</summary>
    public void Stop()
    {
        _stopped = true;
        lock (_gate)
        {
            WakeLocked();
        }
    }

    /// <summary>A reply's entry of this session by its id, with its source text, among the newest <see cref="RepliesKept"/>; null for any other id. On the window's thread, as the map is filled there.</summary>
    public RecordEntry? Reply(string id) => _replies.GetValueOrDefault(id);

    /// <summary>
    /// The file followed, as the window's thread was last told: set when a file is followed, kept when the reader
    /// stopped itself (the file is still the session's), null while none is followed and after one was let go. Held
    /// in memory only and never written to a log: it is a path of the account.
    /// </summary>
    public string? FilePath { get; private set; }

    // ---- the reader's thread ----

    private void Run()
    {
        try
        {
            Read();
        }
        catch (Exception ex)
        {
            // Never out of this thread: an exception that leaves a background thread ends the process, and the window
            // with it. Every kind, on purpose. Its type and stack alone: the message of a file's error names the file.
            Log.Error($"Events: the session file's reader failed with {ex.GetType().Name} and is off until Claude starts again\n{ex.StackTrace}");
            Post(new Batch(NoEntries, Found: null, ByRule: false, Lost: FailedWord));
        }
    }

    private void Read()
    {
        // The lengths as the program starts (EG-3.2). Here and not on the window's thread: listing a folder is I/O.
        _finder.Note(_projectFolders);
        var handled = 0;
        while (!_stopped)
        {
            var now = Environment.TickCount64;
            int asked;
            string? path;
            string[] keys;
            lock (_gate)
            {
                asked = _asked;
                path = _askedPath;
                keys = now < _searchUntil ? [.. _keys] : [];
            }

            if (asked != handled)
            {
                handled = asked;
                if (path is null)
                {
                    // Forgotten: what the folders hold now is the old conversation's, and only what they gain counts.
                    LetGo();
                    _finder.Note(_projectFolders);
                }
                else if (_tail is null || !SamePath(_tail.FilePath, path))
                {
                    LetGo();
                    Take(path, byRule: false);
                }
            }

            if (_tail is null && keys.Length > 0 && _finder.Find(keys, _parse) is { } file)
            {
                lock (_gate)
                {
                    if (_asked == handled)
                    {
                        _ruleFound = true;
                        _keys.Clear();
                    }
                }

                Take(file, byRule: true);
            }

            if (_tail is not null && Look(_tail, now))
            {
                // EG-3.6: no record for this session from here on; the next start of the program tries again.
                return;
            }

            Wait(_tail is not null ? (Quick(now) ? TurnPollMs : IdlePollMs) : keys.Length > 0 ? SearchPollMs : Timeout.Infinite);
        }
    }

    /// <summary>A tail of the file from where EG-3.3 says its reading starts.</summary>
    private void Take(string file, bool byRule)
    {
        _tail = new RecordTail(file, _finder.StartOf(file), _parse);
        _opened = false;
        _byRule = byRule;
        _tailSkipped = _tailNotJson = _tailTooLong = 0;
    }

    /// <summary>The file followed is let go, and the window is told when it had been told that the file is followed.</summary>
    private void LetGo()
    {
        if (_opened)
        {
            Post(new Batch(NoEntries, Found: null, ByRule: false, Lost: string.Empty));
        }

        _tail = null;
        _opened = false;
    }

    /// <summary>One look at the file followed: what it gained is counted and handed over. True when the reader stopped itself (EG-3.6).</summary>
    private bool Look(RecordTail tail, long now)
    {
        var entries = tail.Poll(now);
        Interlocked.Add(ref _skipped, tail.Skipped - _tailSkipped);
        Interlocked.Add(ref _notJson, tail.NotJson - _tailNotJson);
        Interlocked.Add(ref _tooLong, tail.TooLong - _tailTooLong);
        _tailSkipped = tail.Skipped;
        _tailNotJson = tail.NotJson;
        _tailTooLong = tail.TooLong;
        // Missing is no stop and no find: a session's file exists only from its first entry.
        var found = !_opened && tail.State == RecordTailState.Following;
        _opened |= found;
        var stopped = tail.State == RecordTailState.Stopped;
        if (found || entries.Count > 0 || stopped)
        {
            Post(new Batch(entries, found ? tail.FilePath : null, _byRule, stopped ? Word(tail.StopReason) : null));
        }

        return stopped;
    }

    /// <summary>The pace of a turn: one is open, or the last one ended less than 5 s ago.</summary>
    private bool Quick(long now) => _turnOpen || now < Volatile.Read(ref _quickUntil);

    /// <summary>Waits for the next look, or until the window's thread says to look now.</summary>
    private void Wait(int milliseconds)
    {
        lock (_gate)
        {
            if (!_woken)
            {
                Monitor.Wait(_gate, milliseconds);
            }

            _woken = false;
        }
    }

    /// <summary>What a look read joins the inbox; one post for a burst (EG-3.3).</summary>
    private void Post(Batch batch)
    {
        lock (_gate)
        {
            _inbox.Add(batch);
            if (_posted)
            {
                return;
            }

            _posted = true;
        }

        if (!PostToWindow())
        {
            lock (_gate)
            {
                // The window is closing; what waits goes with the next post, if one comes.
                _posted = false;
            }
        }
    }

    private bool PostToWindow()
    {
        if (_owner.IsDisposed || !_owner.IsHandleCreated)
        {
            return false;
        }

        try
        {
            _owner.BeginInvoke(Deliver);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
            return false;
        }
    }

    // ---- both threads ----

    /// <summary>Under the lock: the reader's thread looks now.</summary>
    private void WakeLocked()
    {
        _woken = true;
        Monitor.Pulse(_gate);
    }

    /// <summary>The window says which file to follow, or null to forget it; the keys of the search so far belong to what was asked before.</summary>
    private void Ask(string? path)
    {
        lock (_gate)
        {
            _askedPath = path;
            _asked++;
            _ruleFound = false;
            _keys.Clear();
            _searchUntil = 0;
            WakeLocked();
        }
    }

    private static bool SamePath(string one, string other) =>
        string.Equals(FullPath(one), FullPath(other), StringComparison.OrdinalIgnoreCase);

    private static string FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private static string Word(RecordTailStop reason) => reason switch
    {
        RecordTailStop.NotJson => "json",
        RecordTailStop.Fields => "fields",
        _ => "file",
    };

    // ---- the window's thread ----

    /// <summary>
    /// What the reader's thread read since the last delivery, in its order: the counts into the health, then for each
    /// look that a file is followed, its entries (the replies kept first, so that whoever takes the entries finds
    /// them), and that the file is no longer followed. The log gets one line of fixed words when a file is followed
    /// and one of numbers when the reader stopped itself (EG-3.6, EG-9.5).
    /// </summary>
    private void Deliver()
    {
        Batch[] batches;
        lock (_gate)
        {
            batches = [.. _inbox];
            _inbox.Clear();
            _posted = false;
        }

        if (_stopped)
        {
            return;
        }

        _health.LinesSkipped += Interlocked.Exchange(ref _skipped, 0);
        _health.LinesNotJson += Interlocked.Exchange(ref _notJson, 0);
        _health.LinesTooLong += Interlocked.Exchange(ref _tooLong, 0);
        foreach (var batch in batches)
        {
            if (batch.Found is { } file)
            {
                FilePath = file;
                _health.Record = RecordUse.Followed;
                _health.RecordReason = string.Empty;
                Log.Info(batch.ByRule ? "Events: session file followed, found by a message sent" : "Events: session file followed, named by an event");
                _found(true);
            }

            if (batch.Entries.Count > 0 && !_stopped)
            {
                _health.EntriesRead += batch.Entries.Count;
                foreach (var entry in batch.Entries)
                {
                    Keep(entry);
                }

                _deliver(batch.Entries);
            }

            if (batch.Lost is { } reason && !_stopped)
            {
                if (reason.Length == 0)
                {
                    FilePath = null;
                    _health.Record = RecordUse.NotFound;
                }
                else
                {
                    _health.Record = RecordUse.Stopped;
                    Log.Info($"Events: session file stopped, {reason}; {_health.EntriesRead} entries read, {_health.LinesSkipped} lines skipped, {_health.LinesNotJson} not JSON, {_health.LinesTooLong} too long");
                }

                _health.RecordReason = reason;
                _found(false);
            }

            if (_stopped)
            {
                // A callback stopped the reader (the switch, the program's end): the rest is no longer the window's.
                return;
            }
        }
    }

    /// <summary>A reply's entry is kept by its id for <see cref="Reply"/>, the newest <see cref="RepliesKept"/>.</summary>
    private void Keep(RecordEntry entry)
    {
        if (entry.Kind != RecordKind.ReplyText || !_replies.TryAdd(entry.Id, entry))
        {
            return;
        }

        _replyOrder.Enqueue(entry.Id);
        if (_replyOrder.Count > RepliesKept)
        {
            _replies.Remove(_replyOrder.Dequeue());
        }
    }
}

internal sealed partial class MainForm
{
    // ---- the session file in the window (docs/axClaude/events-and-grounding.md EG-3.2, EG-3.3, EG-3.6, EG-9.2) ----
    //
    // What MainForm.Flow.cs calls when Claude starts and ends, when the core names the session's file or forgets it,
    // when the user's switch changes, when a message is sent and when a turn opens or ends. The reading itself is the
    // RecordHost's, on its own thread; here its entries become inputs of the core, in the file's order.

    /// <summary>The reader of this run of Claude; null while Claude is stopped and while the user's switch is off.</summary>
    private RecordHost? _recordHost;

    /// <summary>The file the core said to follow in this run of Claude (an event named it); kept while the switch is off, so that a reader started later follows it.</summary>
    private string? _recordPath;

    /// <summary>The core was told that there is a record, and not since that there is none.</summary>
    private bool _recordFollowed;

    /// <summary>
    /// The session's file of the conversation the window shows, as the reader last followed it: kept when the reader
    /// stopped itself and when Claude ended, since the file is still that conversation's record (its export, EG-8.6);
    /// null from a start of Claude until a file is followed, after the conversation started afresh, and while the
    /// user's switch is off. In memory only, never in a log: a path of the account.
    /// </summary>
    private string? _recordFile;

    /// <summary>Claude was started: a reader for this run when the switch is on. Its first act, on its own thread, is the note of the project folders' files and their lengths (EG-3.2).</summary>
    partial void RecordProcessStarted()
    {
        _recordHost?.Stop();
        _recordHost = null;
        _recordPath = null;
        _recordFile = null;
        _recordFollowed = false;
        if (_settings.GroundInSessionFile)
        {
            RecordBegin();
        }
    }

    /// <summary>Claude ended or was stopped: the reader is told to end, and the core has already forgotten the run. The file it followed stays known: the conversation is still in the window.</summary>
    partial void RecordProcessEnded()
    {
        _recordHost?.Stop();
        _recordHost = null;
        _recordPath = null;
        _recordFollowed = false;
    }

    /// <summary>The core says which file is the session's: an event named it, and the path passed the check of EG-2.9.</summary>
    partial void RecordFollow(string path)
    {
        _recordPath = path;
        _recordHost?.Follow(path);
    }

    /// <summary>The core says the file followed is an old conversation's and nothing names the new one's: the folder rule looks again.</summary>
    partial void RecordForget()
    {
        _recordPath = null;
        _recordFile = null;
        _recordHost?.Forget();
    }

    /// <summary>
    /// The user's switch (EG-9.2). Off: the reader stops at once, and the core is told that there is no record when it
    /// had one. On again while Claude runs: a new reader, which notes the folders as they are now, so that it starts
    /// with what the file gains from then on.
    /// </summary>
    partial void RecordSwitch(bool on)
    {
        if (!on)
        {
            _recordHost?.Stop();
            _recordHost = null;
            RecordFileFound(false);
        }
        else if (ProcessRunning && _recordHost is null)
        {
            RecordBegin();
        }
    }

    /// <summary>A message or a command went to Claude: the folder rule looks for a file that gains a prompt with these letters (EG-3.2).</summary>
    partial void RecordSent(string key) => _recordHost?.Sent(key);

    /// <summary>The core has a turn open or ending, or no longer: the pace of the reader's looks (EG-3.3).</summary>
    partial void RecordTurn(bool open) => _recordHost?.Turn(open);

    /// <summary>
    /// Starts the reader for the Claude that runs: the project folder of the folder Claude was started in, under each
    /// projects folder a session file of this Claude may lie in (EG-3.1), then the turn as the core has it now, and
    /// the file an event named before the reader was there.
    /// </summary>
    private void RecordBegin()
    {
        if ((_launchedFolder ?? _folder) is not { } folder)
        {
            return;
        }

        var project = ClaudeCodeTranscript.ProjectFolderName(Path.GetFullPath(folder));
        var folders = SessionProjectsFolders().Select(projects => Path.Combine(projects, project)).ToList();
        _recordHost = RecordHost.Start(this, folders, ClaudeCodeTranscript.Parse, RecordEntries, RecordFileFound, _health);
        _recordHost.Turn(_flowTurn);
        if (_recordPath is { } path)
        {
            _recordHost.Follow(path);
        }
    }

    /// <summary>The reader follows a file, or no longer: the file as the reader has it now (none while the switch is off), and the core's input, once for each change.</summary>
    private void RecordFileFound(bool found)
    {
        _recordFile = _recordHost?.FilePath;
        if (found == _recordFollowed)
        {
            return;
        }

        _recordFollowed = found;
        EnqueueFlow(found ? new RecordFound() : new RecordLost());
    }

    /// <summary>Entries of the session file, in the file's order: each is one input of the core (EG-4.2).</summary>
    private void RecordEntries(IReadOnlyList<RecordEntry> entries)
    {
        foreach (var entry in entries)
        {
            EnqueueFlow(FlowFeed.FromEntry(entry));
        }
    }
}
