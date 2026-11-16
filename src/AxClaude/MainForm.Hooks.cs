using System.IO.Pipes;
using AxClaude.Core.Flow;
using AxClaude.Core.Hooks;
using AxClaude.Core.Record;
using AxKit.Core;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- Claude Code's hooks in the window (docs/axClaude/events-and-grounding.md EG-2, EG-9.1, EG-10) ----
    //
    // With a start of Claude Code the window passes axKit's own settings file (--settings), never a file of the
    // user's: Claude Code then starts "AxKit.exe hook" for eight kinds of events of that session, and the hook's
    // process tells this window over a named pipe that the event happened. The pipe and its secret are new for each
    // start and end with it. An event is a signal, no more trusted than a row of the screen (EG-2.9): it is checked
    // and counted here and given to the core as one more input; nothing it carries is run, typed, shown or logged.
    // Where the file is not passed (the switch off, an older Claude Code, the Store's copy, the user's own
    // --settings), the start is the start it always was: no file, no pipe, no variable, no wait.

    /// <summary>How long a start waits for Claude Code's version, counted from the moment it was asked (EG-2.4).</summary>
    private const int HookVersionWaitMs = 2000;

    /// <summary>How long after the first end mark of a run "no event has come" is judged (EG-2.10): a hook's process takes up to two seconds to deliver what Claude Code fired.</summary>
    private const int HookNoneAfterMs = 3000;

    // The one word of the diagnostics for why the file was not passed (EG-2.3, EG-9.4).
    private const string HookReasonStore = "store";
    private const string HookReasonProgram = "program";
    private const string HookReasonArguments = "arguments";
    private const string HookReasonVersion = "version";
    private const string HookReasonFile = "file";
    private const string HookReasonPipe = "pipe";
    private const string HookReasonPath = "path";

    /// <summary>What <c>claude --version</c> printed, for each program file asked in this run of the app (EG-2.4, FR-13.2): the answer, on its way or in, and when it was asked.</summary>
    private readonly Dictionary<string, (Task<string?> Answer, long AskedAt)> _hookVersions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The pipe of the Claude Code that runs; null when it runs without hooks, and when none runs.</summary>
    private HookChannel? _hookChannel;

    /// <summary>The session a start event named: from then on an event of another session is counted and ignored (EG-2.9).</summary>
    private string? _hookSession;

    /// <summary>Raised by every start and every stop: a start that waits for the version goes on only while no other came (EG-2.4).</summary>
    private int _hookStart;

    /// <summary>A start waits for Claude Code's version: Claude is starting, not stopped (the status bar, a message sent meanwhile).</summary>
    private bool _hookVersionWait;

    /// <summary>When the last end mark of a turn arrived (Environment.TickCount64), zero before the first of a run.</summary>
    private long _hookEndMarkAt;

    private bool _hookOtherSessionLogged;
    private bool _hookPathLogged;

    /// <summary>Runs once, <see cref="HookNoneAfterMs"/> after the first end mark of a run with hooks (EG-2.10).</summary>
    private readonly System.Windows.Forms.Timer _hookNoneTimer = new() { Interval = HookNoneAfterMs };

    /// <summary>
    /// Called once from the constructor. Claude Code's version is asked here already, as soon as its program file is
    /// known, so that the answer is in, or nearly, when the window starts Claude and that start waits little or not
    /// at all (EG-2.4). Only when that start is sure to follow and could pass the hooks: the project folder is
    /// known, and the user has agreed to Claude Code before (bundle AX-11.4: no program of Claude Code's is started
    /// ahead of that, nor for a window that may never start Claude). Otherwise it is asked with the start, as it
    /// always was.
    /// </summary>
    private void WireHooks()
    {
        _model.Marked += letter =>
        {
            if (letter != 'D')
            {
                return;
            }

            if (_hookEndMarkAt == 0 && _hookChannel is not null)
            {
                _hookNoneTimer.Start();
            }

            _hookEndMarkAt = Environment.TickCount64;
        };
        _hookNoneTimer.Tick += (_, _) =>
        {
            _hookNoneTimer.Stop();
            // EG-2.10: one line, and the session goes on; the pipe stays, and an event that comes later still counts.
            if (_hookChannel is not null && _health.HooksReceived == 0)
            {
                Log.Info("Events: the hooks' file was passed and no event has come by the end of the first turn");
            }
        };

        if (_folder is not null
            && HookMayPass(out _)
            && ClaudeCodeProgram.Find(_options.ClaudePath ?? _settings.ClaudePath) is { } claude
            && Consent.Given(AppPaths.ConsentFile, AppName, ConsentTarget.ClaudeCode.Program))
        {
            HookVersionOf(claude);
        }
    }

    /// <summary>
    /// Claude Code's version for a program file: asked with the first call for that file and kept for this run of the
    /// app (EG-2.4), so that a claude.exe the user located later, or an install, is asked for itself.
    /// </summary>
    private (Task<string?> Answer, long AskedAt) HookVersionOf(string claude)
    {
        if (!_hookVersions.TryGetValue(claude, out var asked))
        {
            asked = (AskClaudeVersionAsync(claude), Environment.TickCount64);
            _hookVersions[claude] = asked;
        }

        return asked;
    }

    /// <summary>The answer when it is in: what <c>claude --version</c> printed; null while it is on its way, and for a Claude Code that gave none.</summary>
    private static string? HookVersionLine(Task<string?> answer) => answer.IsCompletedSuccessfully ? answer.Result : null;

    /// <summary>
    /// Whether a start may pass the hooks as far as the window itself knows, Claude Code's version aside (EG-2.3):
    /// not with the switch off (no reason: the hooks are off), not in the Store's copy until Claude Code is known to
    /// read a file under the package's folders (EG-2.11), not when the user's own arguments already pass a settings
    /// file, since Claude Code would get two.
    /// </summary>
    private bool HookMayPass(out string reason)
    {
        reason = string.Empty;
        if (!_settings.ClaudeHooks)
        {
            return false;
        }

        if (PackageIdentity.IsPackaged)
        {
            reason = HookReasonStore;
        }
        else if (PackageIdentity.ProgramPath is null)
        {
            reason = HookReasonProgram;
        }
        else if (ClaudeCodeHooks.UserPassesSettings(ClaudeArgs))
        {
            reason = HookReasonArguments;
        }

        return reason.Length == 0;
    }

    /// <summary>
    /// How long this start may still wait for Claude Code's version (EG-2.4): nothing when the answer is in, when the
    /// hooks would not be passed whatever it says, or when the two seconds since it was asked are over, in which case
    /// this start goes without hooks and a later one uses the answer.
    /// </summary>
    private int HookWaitLeft((Task<string?> Answer, long AskedAt) version) =>
        version.Answer.IsCompleted || !HookMayPass(out _)
            ? 0
            : (int)Math.Clamp(version.AskedAt + HookVersionWaitMs - Environment.TickCount64, 0, HookVersionWaitMs);

    /// <summary>
    /// The start goes on when Claude Code's version is in, or when the time is over (EG-2.4). Until then no Claude
    /// runs: the status bar says starting, and a message stays in its field. Nothing happens when the window is gone,
    /// when Claude runs, or when another start or a stop came meanwhile, which each raise <see cref="_hookStart"/>.
    /// </summary>
    private async void HookStartWhenAnswered(string claude, Task<string?> answer, int waitMs)
    {
        var start = _hookStart;
        var since = Environment.TickCount64;
        _hookVersionWait = true;
        UpdateStatus();
        await Task.WhenAny(answer, Task.Delay(waitMs));
        if (start != _hookStart || IsDisposed || ProcessRunning)
        {
            return;
        }

        _hookVersionWait = false;
        Log.Info($"Events: Claude's start waited {Environment.TickCount64 - since} ms for Claude Code's version{(answer.IsCompleted ? string.Empty : ", which has not answered")}");
        StartClaudeNow(claude);
    }

    /// <summary>A start that waited for the version is over: a new start, or a stop, takes its place.</summary>
    private void HookCancelStart()
    {
        _hookStart++;
        if (_hookVersionWait)
        {
            _hookVersionWait = false;
            UpdateStatus();
        }
    }

    /// <summary>
    /// What passes the hooks to this start of Claude Code (EG-2.3): <c>--settings</c> and axKit's own file, written
    /// when it is missing or names another program file, with the pipe of this start served from here on; or nothing,
    /// with the reason in the diagnostics' one word, which the start's line in the log names (EG-9.5). The caller
    /// puts the arguments right after <c>--ax-screen-reader</c>, calls <see cref="HookNotPassed"/> when cmd.exe
    /// would refuse the line with them, and <see cref="HookStartFailed"/> when the start fails after this.
    /// </summary>
    private IReadOnlyList<string> HookArguments(string claude)
    {
        HookStopChannel();
        var version = HookVersionLine(HookVersionOf(claude).Answer);
        _health.ProgramVersion = ClaudeCodeHooks.VersionNumbers(version);
        if (!HookMayPass(out var reason))
        {
            _health.Hooks = _settings.ClaudeHooks ? HookUse.NotPassed : HookUse.Off;
            _health.HooksReason = reason;
            return [];
        }

        // EG-2.4: an event name a Claude Code does not know stops its start with a question, so the file goes only
        // to a version the events' table was verified with, or a later one; no answer is no.
        if (!ClaudeCodeHooks.Supports(version))
        {
            return HookNotPassed(HookReasonVersion);
        }

        try
        {
            if (ClaudeCodeHooks.WriteSettings(AppPaths.ClaudeHooksFile, PackageIdentity.ProgramPath!, HookRelay.Verb))
            {
                Log.Info("Events: wrote the file that registers axClaude's hooks for a start of Claude Code");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Info("Events: the hooks' file could not be written: " + ex.Message);
            return HookNotPassed(HookReasonFile);
        }

        if (HookChannel.Start(this, HookArrived, _health) is not { } channel)
        {
            return HookNotPassed(HookReasonPipe);
        }

        _hookChannel = channel;
        _health.Hooks = HookUse.Passed;
        _health.HooksReason = string.Empty;
        return ClaudeCodeHooks.SessionArguments(AppPaths.ClaudeHooksFile);
    }

    /// <summary>This start goes without hooks after all: the pipe, if one was started for it, is stopped, and the diagnostics get the reason's word.</summary>
    private IReadOnlyList<string> HookNotPassed(string reason)
    {
        HookStopChannel();
        _health.Hooks = HookUse.NotPassed;
        _health.HooksReason = reason;
        return [];
    }

    /// <summary>The start failed after the hooks were prepared (a line cmd.exe refuses, a process that did not start): no pipe is left, and the diagnostics say no more than the switch.</summary>
    private void HookStartFailed()
    {
        HookStopChannel();
        _health.Hooks = _settings.ClaudeHooks ? HookUse.NotPassed : HookUse.Off;
        _health.HooksReason = string.Empty;
    }

    /// <summary>
    /// The pipe ends with its Claude Code (EG-2.7), and what belongs to that run is forgotten. What its thread still
    /// posts is dropped: an event of a Claude Code that is no longer the window's is no event of this session.
    /// </summary>
    private void HookStopChannel()
    {
        _hookChannel?.Stop();
        _hookChannel = null;
        _hookSession = null;
        _hookEndMarkAt = 0;
        _hookOtherSessionLogged = false;
        _hookPathLogged = false;
        _hookNoneTimer.Stop();
    }

    /// <summary>
    /// An event of the Claude Code that runs, on the UI thread (EG-2.9). Once a start event has named the session, an
    /// event of another session is counted and ignored, but for a start event itself, which names the session anew
    /// (/clear). The path of the session's file is used only when it names a session file under a projects folder of
    /// this Claude Code; otherwise it is left out and counted with the refused. Then the event is one more input of
    /// the core. The log gets numbers and fixed words, never a word, a path or an id of the event (EG-9.5).
    /// </summary>
    private void HookArrived(HookEvent e)
    {
        if (e.Kind == HookKind.SessionStarted)
        {
            _hookSession = e.SessionId;
        }
        else if (_hookSession is { } session && !string.Equals(e.SessionId, session, StringComparison.Ordinal))
        {
            _health.HooksOtherSession++;
            if (!_hookOtherSessionLogged)
            {
                _hookOtherSessionLogged = true;
                Log.Info("Events: an event of another session was ignored; the diagnostics count them");
            }

            return;
        }

        _health.HooksReceived++;
        if (e.RecordPath is not null && !ClaudeCodeTranscript.IsSessionFile(e.RecordPath, SessionProjectsFolders()))
        {
            _health.HooksRefused++;
            e = e with { RecordPath = null };
            if (!_hookPathLogged)
            {
                _hookPathLogged = true;
                Log.Info("Events: a start event named no session file under Claude Code's projects folder; its path was not used");
            }
        }

        if (e.Kind == HookKind.TurnStopped && _hookEndMarkAt != 0 && !FlowDecides(FlowCharge.Done))
        {
            // While Done is the window's own: when the event of a turn's end came against the end mark, for the day
            // the core says Done (events-and-grounding.md 4.3: with hooks Claude Code does not wait for, after it).
            Log.Info($"Events: stop event {Environment.TickCount64 - _hookEndMarkAt} ms after the last end mark");
        }

        EnqueueFlow(FlowFeed.FromHook(e));
    }
}

/// <summary>
/// The window's end of Claude Code's hooks (docs/axClaude/events-and-grounding.md EG-2.7): one named pipe for one
/// start of Claude Code, <c>AxKit-hook-&lt;user SID&gt;-&lt;random&gt;</c>, inbound only, for the current user alone and
/// never over the network, with a secret of its own. A connection is one event (<see cref="HookFrame"/>): the first
/// line must be the secret, or the connection is dropped and counted. The thread never touches the window: it reads,
/// parses (<see cref="ClaudeCodeHooks.Parse"/>) and posts, one post for a burst; what it counts is added to the
/// window's numbers on the UI thread. <see cref="Stop"/> ends the wait and the pipe at once and never waits for the
/// thread. Neutral but for the parser it calls: a second agent program's events would arrive the same way.
/// </summary>
internal sealed class HookChannel
{
    /// <summary>How long one connection may take to send its frame.</summary>
    private const int ReadLimitMs = 1000;

    /// <summary>The pipe's inbound buffer: an event of the usual size is written whole at once, so that the hook's process ends without waiting for this thread.</summary>
    private const int InBufferBytes = 65_536;

    /// <summary>After this many failures in a row the thread gives up; the session goes on without events.</summary>
    private const int MostFailures = 20;

    private readonly Control _owner;
    private readonly Action<HookEvent> _deliver;
    private readonly SourceHealth _health;
    private readonly string _name;
    private readonly string _secret;
    private readonly CancellationTokenSource _cancel = new();

    /// <summary>The events read and not yet handed over; also the lock of <see cref="_posted"/>.</summary>
    private readonly List<HookEvent> _inbox = [];
    private bool _posted;

    // Counted on the pipe's thread, taken on the UI thread.
    private int _refused;
    private int _malformed;

    /// <summary>What the first connection that was dropped failed on, a fixed word, for the one line in the log.</summary>
    private volatile string? _firstFault;

    // The UI thread's own.
    private bool _stopped;
    private bool _faultLogged;

    private HookChannel(Control owner, Action<HookEvent> deliver, SourceHealth health, string name, string secret)
    {
        _owner = owner;
        _deliver = deliver;
        _health = health;
        _name = name;
        _secret = secret;
        VariableValue = HookFrame.VariableValue(name, secret);
    }

    /// <summary>The value of <see cref="HookFrame.Variable"/> for the environment of the Claude Code this pipe is for (EG-2.8).</summary>
    public string VariableValue { get; }

    /// <summary>
    /// Opens a new pipe with a new name and a new secret and serves it on a thread of its own. The pipe exists when
    /// this returns, so that the first event finds it. Null, with one line in the log, when it could not be opened:
    /// Claude Code is then started without hooks. <paramref name="deliver"/> gets each event on the thread of
    /// <paramref name="owner"/>, in arrival order, until <see cref="Stop"/>.
    /// </summary>
    public static HookChannel? Start(Control owner, Action<HookEvent> deliver, SourceHealth health)
    {
        try
        {
            var channel = new HookChannel(owner, deliver, health, HookFrame.NewPipeName(UserPipe.User().Value), HookFrame.NewSecret());
            var pipe = channel.Open();
            new Thread(() => channel.Serve(pipe)) { IsBackground = true, Name = "AxKit hooks" }.Start();
            return channel;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The exception's type alone: its message would name the pipe.
            Log.Info("Events: the hooks' pipe was not opened (" + ex.GetType().Name + ")");
            return null;
        }
    }

    /// <summary>
    /// The pipe closes with its Claude Code: the thread's wait and its read are cancelled, it closes the pipe and
    /// ends. Called on the UI thread, which never waits for it; what the thread had posted is dropped.
    /// </summary>
    public void Stop()
    {
        _stopped = true;
        _cancel.Cancel();
    }

    private NamedPipeServerStream Open()
    {
        // Outside call, pipe, Claude Code events: the window's own named pipe, which only this Windows account may
        // open, where "AxKit.exe hook", started by Claude Code for an event of the session, says that the event
        // happened; each connection must send the window's secret.
        return NamedPipeServerStreamAcl.Create(_name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, InBufferBytes, 0, UserPipe.Security());
    }

    /// <summary>
    /// The pipe's thread. One instance of the pipe serves every connection in turn and is never closed between two,
    /// so that the name is never free for another program to take while Claude Code runs; a connection that fails
    /// is counted, and a pipe that cannot go on is opened again after a pause. Every wait and read is under the
    /// token <see cref="Stop"/> cancels. No exception leaves the thread: one that did would end the process and the
    /// window with it.
    /// </summary>
    private void Serve(NamedPipeServerStream first)
    {
        NamedPipeServerStream? pipe = first;
        var failures = 0;
        try
        {
            while (!_cancel.IsCancellationRequested)
            {
                try
                {
                    pipe ??= Open();
                    pipe.WaitForConnectionAsync(_cancel.Token).GetAwaiter().GetResult();
                    Take(pipe);
                    pipe.Disconnect();
                    failures = 0;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ObjectDisposedException)
                {
                    // A client that left before it was served, a pipe that broke: this instance is given up.
                    Interlocked.Increment(ref _refused);
                    _firstFault ??= ex.GetType().Name;
                    Post();
                    pipe?.Dispose();
                    pipe = null;
                    if (++failures > MostFailures)
                    {
                        Log.Error($"Events: the hooks' pipe gave up after {failures} failures in a row ({ex.GetType().Name})");
                        return;
                    }

                    _cancel.Token.WaitHandle.WaitOne(Math.Min(2000, 50 * failures));
                }
            }
        }
        catch (Exception ex)
        {
            // The type and the place, never the message: it could quote what an event carried (EG-9.5).
            Log.Error($"Events: the hooks' pipe ended on {ex.GetType().Name}\n{ex.StackTrace}");
        }
        finally
        {
            pipe?.Dispose();
        }
    }

    /// <summary>One connection: the frame read within its time, the secret checked first, the body parsed, the event queued for the window.</summary>
    private void Take(NamedPipeServerStream pipe)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(_cancel.Token);
        limit.CancelAfter(ReadLimitMs);
        var body = HookFrame.Read(new TimedRead(pipe, limit.Token), _secret, out var fault);
        if (body is null)
        {
            // A wrong secret, a body over the limit, or a connection that broke or took too long (EG-2.7).
            Interlocked.Increment(ref _refused);
            _firstFault ??= fault.ToString();
        }
        else if (ClaudeCodeHooks.Parse(body) is { } e)
        {
            lock (_inbox)
            {
                _inbox.Add(e);
            }
        }
        else
        {
            Interlocked.Increment(ref _malformed);
            _firstFault ??= "an event that could not be read";
        }

        Post();
    }

    /// <summary>One post for a burst: a second event read before the window got to the first rides with it.</summary>
    private void Post()
    {
        lock (_inbox)
        {
            if (_posted)
            {
                return;
            }

            _posted = true;
        }

        var sent = false;
        if (!_owner.IsDisposed && _owner.IsHandleCreated)
        {
            try
            {
                _owner.BeginInvoke(Deliver);
                sent = true;
            }
            catch (InvalidOperationException)
            {
                // The window is closing.
            }
        }

        if (!sent)
        {
            lock (_inbox)
            {
                _posted = false;
            }
        }
    }

    /// <summary>On the UI thread: what the pipe's thread counted joins the window's numbers, and each event is handed over; nothing of a pipe that was stopped.</summary>
    private void Deliver()
    {
        HookEvent[] batch;
        lock (_inbox)
        {
            batch = [.. _inbox];
            _inbox.Clear();
            _posted = false;
        }

        var refused = Interlocked.Exchange(ref _refused, 0);
        var malformed = Interlocked.Exchange(ref _malformed, 0);
        if (_stopped)
        {
            return;
        }

        _health.HooksRefused += refused;
        _health.HooksMalformed += malformed;
        if (refused + malformed > 0 && !_faultLogged)
        {
            _faultLogged = true;
            // A fixed word: the frame's fault (WrongSecret, TooLong, Broken), an exception's type, or that the event could not be read.
            Log.Info($"Events: the hooks' pipe dropped a connection ({_firstFault}); the diagnostics count this one and any after it");
        }

        foreach (var e in batch)
        {
            _deliver(e);
        }
    }

    /// <summary>
    /// The pipe as <see cref="HookFrame.Read"/> reads it, every read under a token: a client that connects and sends
    /// nothing, or stops half way, holds the pipe only until the token is cancelled, and the read then ends as a
    /// broken stream does. What the pipe holds is taken in pieces of the buffer's size and handed on from there:
    /// the frame's reader asks for its header a byte at a time, and each read of this pipe is a round trip through
    /// the thread pool, some seventy of them for one event without the buffer. Measured on 2026-11-15 with every
    /// processor busy: 4 to 5 s for one event a byte at a time, 50 to 70 ms through the buffer; a stop event that
    /// was dropped at the connection's time limit in a window run of that day, while other builds ran, is put down
    /// to this. Past a wrong secret nothing of the buffer is looked at.
    /// </summary>
    private sealed class TimedRead(PipeStream pipe, CancellationToken token) : Stream
    {
        private readonly byte[] _taken = new byte[InBufferBytes];
        private int _next;
        private int _end;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_next == _end)
            {
                try
                {
                    _end = pipe.ReadAsync(_taken.AsMemory(), token).AsTask().GetAwaiter().GetResult();
                    _next = 0;
                }
                catch (OperationCanceledException)
                {
                    throw new IOException("The frame was not sent in time.");
                }
            }

            var given = Math.Min(count, _end - _next);
            Array.Copy(_taken, _next, buffer, offset, given);
            _next += given;
            return given;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
