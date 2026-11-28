using AxKit.Core;
using AxKit.Core.Pty;

namespace AxKit.Forms;

public abstract partial class ConsoleForm
{
    // ---- the program in its pseudo console (axClaude FR-1, §7.3; axTerminal AT-1) ----

    /// <summary>A frame ends after this long without output, besides at the cursor shown again.</summary>
    private readonly System.Windows.Forms.Timer _quiet = new() { Interval = 100 };

    /// <summary>The exit is reported after this long, so that output still in the pipe arrives first.</summary>
    private readonly System.Windows.Forms.Timer _exitSettle = new() { Interval = 300 };

    /// <summary>The paced writes, with what each answer of a write's check makes of them (<see cref="WriteQueue"/>, pure and tested in AxKit.Core).</summary>
    private readonly WriteQueue _writes = new();

    /// <summary>The one timer between two paced writes: a restart stops it, so that no second pump runs on (two ran after a restart until 2026-10-23).</summary>
    private readonly System.Windows.Forms.Timer _pump = new();

    private PtyHost? _host;
    private int _exitCode;
    private bool _writing;

    /// <summary>
    /// The program runs: its process was started and has not ended or been stopped. Virtual for a window that can
    /// also run its program another way than in this pseudo console (axClaude's stream source,
    /// docs/axClaude/stream-mode.md SM-3.5): whatever the window base asks about the program, it asks here.
    /// </summary>
    protected virtual bool ProcessRunning => _host is not null;

    /// <summary>When output last arrived from the program (Environment.TickCount64), for a window's quiet heuristics (axTerminal AT-5.4).</summary>
    protected long LastOutputTick { get; private set; }

    /// <summary>A write is on its way or queued: the program has not seen everything typed yet.</summary>
    protected bool WritesPending => _writing || _writes.Count > 0;

    /// <summary>
    /// Starts the program in a pseudo console. Chunks that arrive while the UI thread is busy are handed over
    /// together: one post per burst, not one per chunk, so a large output does not queue up hundreds of window
    /// messages ahead of the user's keys. The handlers go in with Start, so that nothing the program prints first is
    /// missed; they run on background threads and only post to this one, which gets to the posted work after the
    /// host is assigned. False, after the error notice, when the program could not be started (<paramref name="program"/>
    /// names it: "Claude", "The shell").
    /// </summary>
    protected bool StartProcess(PtyOptions options, string program)
    {
        var inbox = new List<byte[]>();
        var posted = false;
        PtyHost? host = null;
        try
        {
            // Outside call, program, Claude Code, Shells: starts the window's program in a pseudo console: "claude
            // --ax-screen-reader" for axClaude, the chosen shell for axTerminal.
            host = PtyHost.Start(options, OnData, OnExited);
        }
        catch (Exception ex)
        {
            // ConPTY's start fails in several native ways (Win32Exception from Native.Error, EntryPointNotFoundException on
            // an old Windows, IOException on the pipes); a type left out would crash the window instead of showing the notice.
            Log.Error($"{program} could not be started", ex);
            var reason = ex is EntryPointNotFoundException
                ? "this version of Windows has no pseudo console. Windows 10 version 1809 or later is needed"
                : ex.Message;
            Fail($"{program} could not be started: {reason}");
            return false;
        }

        // A session's id on the line (claude --resume <id>) is never logged: the line goes in with it masked.
        Log.Info($"Started {Redaction.WithoutIds(options.CommandLine)} (pid {host.ProcessId}) in {options.WorkingDirectory}, console {options.Columns}x{options.Rows}");
        _host = host;
        LastOutputTick = Environment.TickCount64;
        return true;

        void OnData(byte[] data)
        {
            try
            {
                _recorder?.Write(data);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A full disk or a locked file must not end the read loop, which would freeze the transcript.
                Post(() => RecordingFailed(ex.Message));
            }

            lock (inbox)
            {
                inbox.Add(data);
                if (posted)
                {
                    return;
                }

                posted = true;
            }

            if (!Post(FeedInbox))
            {
                lock (inbox)
                {
                    posted = false;
                }
            }
        }

        void FeedInbox()
        {
            byte[][] batch;
            lock (inbox)
            {
                batch = [.. inbox];
                inbox.Clear();
                posted = false;
            }

            if (!ReferenceEquals(host, _host))
            {
                return;
            }

            foreach (var chunk in batch)
            {
                Model.Feed(chunk);
            }

            LastOutputTick = Environment.TickCount64;
            _quiet.Stop();
            _quiet.Start();
        }

        void OnExited(int code) => Post(() =>
        {
            if (ReferenceEquals(host, _host))
            {
                // Let output that is still in the pipe arrive before the exit is reported.
                _exitCode = code;
                _exitSettle.Stop();
                _exitSettle.Start();
            }
        });
    }

    /// <summary>The process ended and its last output is in: the frame closed, the process stopped, the window told.</summary>
    private void FinishExit()
    {
        _quiet.Stop();
        Model.EndFrame();
        StopProcess();
        Log.Info($"{AppName}'s program exited with code {_exitCode}");
        OnProcessExited(_exitCode);
    }

    /// <summary>
    /// Stops the program and everything that was waiting on it: the timers, the write pump and its queue, the
    /// control area (nothing waits on an answer any more), the status bar and the music. The program gets two
    /// seconds to end on its console closing, unless <paramref name="terminateAtOnce"/>: a restart or a close the
    /// user confirmed while it was busy ends it now (axClaude FR-1.7, axTerminal AT-1.6). A window with timers of its
    /// own stops them first and calls here.
    /// </summary>
    protected virtual void StopProcess(bool terminateAtOnce = false)
    {
        var host = _host;
        _host = null;
        _quiet.Stop();
        _exitSettle.Stop();
        _pump.Stop();
        _writes.Clear();
        _writing = false;
        if (terminateAtOnce)
        {
            host?.Kill();
        }

        host?.Dispose();
        CloseArea();
        UpdateStatus();
        UpdateMusic();
    }

    /// <summary>
    /// Runs the action on the UI thread; false when the window is gone. For what a background thread hands the
    /// window: the pseudo console's chunks here, and a window's own second way to run its program (axClaude's stream
    /// source, docs/axClaude/stream-mode.md SM-4.1).
    /// </summary>
    protected bool Post(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return false;
        }

        try
        {
            BeginInvoke(action);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
            return false;
        }
    }

    // ---- the writes (axClaude §7.3, axTerminal AT-3.1): one paced queue, one pump ----

    /// <summary>
    /// Text straight to the program, now, when <paramref name="still"/> says so: the check the write asks right
    /// before it goes, or null, written out, for a write that needs none (the stop key). Required, as for
    /// <see cref="Enqueue"/>, so that the compiler has every caller state its check (the fourth review of
    /// 2026-11-27, L3: a table of the callers in a test, read off the sources by a pattern, could be passed by).
    /// </summary>
    protected void Write(string text, Func<bool>? still)
    {
        if (still is null || still())
        {
            _host?.Write(text);
        }
    }

    /// <summary>
    /// A write for the queue, with the pause that follows it before the next; <see cref="StartWrites"/> sends them.
    /// <paramref name="still"/> is asked right before this write goes to the program, which can be seconds after it
    /// was queued, and answers one of three (<see cref="WriteCheck"/>). Over when what the write was for is over
    /// (axClaude: a question was drawn under a message's Enter; the question an answer was typed for was replaced):
    /// the write is dropped with everything queued behind it, the one who asked says so, and
    /// <see cref="WritesDropped"/> is told. Not yet when it may still go (axClaude: a message's Enter while Claude
    /// Code's prompt row does not show the message yet): it stays first, everything behind it waits in order, and it
    /// is asked again <see cref="NotYetMs"/> later; the check itself says when waiting is over. Until the third review
    /// of 2026-11-27 a write was judged when it was queued and never again; until the fifth review of that day the
    /// check was a yes or no, and a "not yet" dropped what stood behind it. Required, null written out for a write
    /// that needs no check (<see cref="Write"/>). What the queue does with each answer is <see cref="WriteQueue.Next"/>'s
    /// (the sixth review of that day, L7). A write with an empty text is never written: it is a point where the queue
    /// waits on its check (axClaude: until Claude Code has taken a message).
    /// </summary>
    protected void Enqueue(string text, int delayAfter, Func<WriteCheck>? still) => _writes.Add(text, delayAfter, still);

    /// <summary>How long a write that was told "not yet" waits before it is asked again.</summary>
    protected const int NotYetMs = WriteQueue.NotYetMs;

    /// <summary>
    /// The queue was dropped with writes still in it (a check said over, or <see cref="ClearWrites"/>): a window that
    /// counts on writes it queued (axClaude: a message not yet sent) takes them back here. <paramref name="failed"/>
    /// when a check threw (<see cref="WriteStepKind.Failed"/>). Nothing by default.
    /// </summary>
    protected virtual void WritesDropped(bool failed)
    {
    }

    /// <summary>Starts the write pump when it is idle; a pump already running takes what was queued after it.</summary>
    protected void StartWrites()
    {
        if (!_writing)
        {
            PumpWrites();
        }
    }

    /// <summary>What is still queued goes (an interrupt: an answer typed out a key at a time must not land after it), and the window is told (<see cref="WritesDropped"/>).</summary>
    protected void ClearWrites()
    {
        if (_writes.Clear())
        {
            WritesDropped(failed: false);
        }
    }

    /// <summary>One turn of the pump: what the queue says (<see cref="WriteQueue.Next"/>), done.</summary>
    private void PumpWrites()
    {
        var step = _writes.Next();
        switch (step.Kind)
        {
            case WriteStepKind.Idle:
                _writing = false;
                return;
            case WriteStepKind.Dropped:
                // What a write was for is over: it and what waited behind it went unwritten.
                _writing = false;
                WritesDropped(failed: false);
                return;
            case WriteStepKind.Failed:
                // A check threw (L2): the pump is free again, and the window is told; the log names the exception's
                // type alone, never a text that was queued.
                _writing = false;
                Log.Error($"{AppName}: a check of the write queue failed ({step.Text}); what was queued was dropped");
                WritesDropped(failed: true);
                return;
            case WriteStepKind.Write:
                _host?.Write(step.Text);
                break;
        }

        // A write's pause, or a write told "not yet" asked again: nothing behind it goes before it.
        _writing = true;
        _pump.Interval = step.WaitMs;
        _pump.Start();
    }
}
