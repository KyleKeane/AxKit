using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using AxKit.Core;

namespace AxClaude.Core.Streaming;

/// <summary>How the program of a <see cref="StreamHost"/> is started: its line (<see cref="ClaudeCodeProgram.LineFor"/>), the folder it works in, and its environment, whole.</summary>
public sealed record StreamHostOptions(ProgramLine Line, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment);

/// <summary>
/// A program run as one process over three pipes, for Claude Code's non-interactive stream
/// (docs/axClaude/stream-mode.md SM-4.1): no console window, every line of its output handed over as it is read,
/// lines written to its input in the order they were given, its end reported once with the exit code. It knows no
/// word of Claude Code's: what a line means is <see cref="ClaudeCodeStream"/>'s.
///
/// Nothing here blocks the caller (bundle rule 3). The output and the error output are each read on a thread of their
/// own, and what is written goes through a queue to a third: a pipe's write waits while the program does not read.
/// <paramref name="lineReceived"/> and <paramref name="exited"/> of <see cref="Start"/> are called on those threads
/// and only post to the caller's. What the program writes as errors is kept here in part, for the user to be shown
/// when the program ends by itself, and is never logged: it can hold a path or a text of the user's.
/// A window made for the stream runs Claude Code in it (src/AxClaude/MainForm.Stream.cs).
/// </summary>
public sealed class StreamHost : IDisposable
{
    /// <summary>As much of the end of the error output as is kept, in characters.</summary>
    private const int ErrorTailLength = 2000;

    private readonly Process _process;
    private readonly Action<string> _lineReceived;
    private readonly Action<int> _exited;

    /// <summary>The lines still to be written, in order; null closes the program's input.</summary>
    private readonly BlockingCollection<string?> _writes = [];

    private readonly object _errorGate = new();
    private readonly StringBuilder _errorTail = new();

    /// <summary>Set when the program itself has ended, before its end is reported: what <see cref="End"/> waits for.</summary>
    private readonly ManualResetEventSlim _gone = new();
    private int _errorCharacters;
    private int _exitedOnce;
    private int _disposed;

    /// <summary>
    /// The job the program and everything it starts is in (the 2026-11-21 audit): ended with them when this process
    /// ends, however it ends, and by <see cref="Kill"/> also once the program itself is gone, which the walk of its
    /// process tree could no longer do. Null where Windows gave none.
    /// </summary>
    private readonly AxKit.Core.ChildJob? _job;

    /// <summary>How long what the program left running may keep its output open once the program itself has ended.</summary>
    private const int LeftoverMs = 2000;

    private StreamHost(Process process, AxKit.Core.ChildJob? job, Action<string> lineReceived, Action<int> exited)
    {
        _process = process;
        _job = job;
        _lineReceived = lineReceived;
        _exited = exited;
    }

    public int ProcessId { get; private set; }

    /// <summary>The program has ended and its end was reported, or is about to be.</summary>
    public bool HasExited => Volatile.Read(ref _exitedOnce) == 1;

    /// <summary>How many characters the program wrote as errors, in all.</summary>
    public int ErrorCharacters => Volatile.Read(ref _errorCharacters);

    /// <summary>The end of what the program wrote as errors: for the user's eyes when it ended by itself, never for a log.</summary>
    public string ErrorTail
    {
        get
        {
            lock (_errorGate)
            {
                return _errorTail.ToString();
            }
        }
    }

    /// <summary>
    /// Starts the program. Throws what <see cref="Process.Start()"/> throws when it cannot be started
    /// (<see cref="System.ComponentModel.Win32Exception"/>, <see cref="InvalidOperationException"/>), and
    /// <see cref="ArgumentException"/> for a line that was refused. The two actions are called on background threads.
    /// </summary>
    public static StreamHost Start(StreamHostOptions options, Action<string> lineReceived, Action<int> exited)
    {
        if (options.Line.Refusal is not null)
        {
            throw new ArgumentException(options.Line.Refusal, nameof(options));
        }

        var start = new ProcessStartInfo(options.Line.FileName, options.Line.Arguments)
        {
            WorkingDirectory = options.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.Environment.Clear();
        foreach (var (name, value) in options.Environment)
        {
            start.Environment[name] = value;
        }

        // Outside call, program, Claude Code: starts the user's Claude Code without a window as one process that
        // takes messages on its standard input and writes what happens on its standard output, line by line (its
        // non-interactive stream); Claude Code talks to Anthropic under the user's account. Only a window made for
        // the stream starts it (the setting claudeSource, which is the screen at first).
        var process = Process.Start(start) ?? throw new InvalidOperationException("The program was not started.");
        // Into the job at once: what the program starts from here on is in it too. (A child it started in its first
        // moments, before this line, would not be: Claude Code starts none that early.)
        var job = AxKit.Core.ChildJob.TryCreate();
        job?.Add(process);
        var host = new StreamHost(process, job, lineReceived, exited) { ProcessId = process.Id };
        host.Run();
        return host;
    }

    /// <summary>One line for the program's input, written after those given before it. Nothing after the input was closed or the program ended.</summary>
    public void Write(string line) => Enqueue(line);

    /// <summary>Closes the program's input after what was written: Claude Code ends its run when its input ends.</summary>
    public void CloseInput() => Enqueue(null);

    /// <summary>
    /// Ends the program in its own time: its input is closed after what was written, which is how a Claude Code that
    /// is idle is told to end, and when it has not ended <paramref name="graceMs"/> later it is ended with what it
    /// started. The caller does not wait (bundle rule 3): the wait is a thread's of the pool. The end is reported
    /// as any other.
    /// </summary>
    public void End(int graceMs)
    {
        CloseInput();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            if (!_gone.Wait(graceMs))
            {
                Kill();
            }
        });
    }

    /// <summary>Ends the program and what it started, at once. Its end is still reported.</summary>
    public void Kill()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException or AggregateException)
        {
            // It ended in the meantime, or a child of it is already gone, or one of what it started may not be ended
            // by this account (the tree's failures come as one AggregateException): the end is reported by the
            // reader. Never thrown on: End calls this on a thread of the pool, where an exception ends the program.
        }

        // What the program started and left running, also when the program itself has already ended and the tree can
        // no longer be walked from it.
        _job?.Terminate();
    }

    /// <summary>Ends the program if it still runs and lets its threads end.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        Kill();
        _writes.CompleteAdding();
    }

    private void Enqueue(string? line)
    {
        try
        {
            if (!_writes.IsAddingCompleted)
            {
                _writes.Add(line);
            }
        }
        catch (InvalidOperationException)
        {
            // The queue was closed between the test and the add: the program is ending.
        }
    }

    private Thread? _errors;

    private void Run()
    {
        _errors = new Thread(ReadErrors) { IsBackground = true, Name = "axClaude stream errors" };
        _errors.Start();
        new Thread(ReadOutput) { IsBackground = true, Name = "axClaude stream output" }.Start();
        new Thread(WriteInput) { IsBackground = true, Name = "axClaude stream input" }.Start();
        if (_job is not null)
        {
            new Thread(WatchEnd) { IsBackground = true, Name = "axClaude stream end" }.Start();
        }
    }

    /// <summary>
    /// When the program itself has ended, what it started ends with it, as a console's programs end with their
    /// console: a command left running in the background holds the output's pipe open, and the reader would wait on
    /// it for ever, the window saying that Claude runs while messages go nowhere. The reader gets a moment to take
    /// the last lines first.
    /// </summary>
    private void WatchEnd()
    {
        try
        {
            _process.WaitForExit();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException or ObjectDisposedException)
        {
            // The process is gone, or was let go of: the job is closed below all the same. ExternalException
            // holds both of Windows' own: a Win32Exception, and the COMException "The handle is invalid" that
            // the wait throws when the process object is let go of just as this thread starts (seen in a test
            // run on 2026-11-22, where it ended the whole process: an exception no one takes on a thread does).
        }

        try
        {
            _gone.Wait(LeftoverMs);
        }
        catch (ObjectDisposedException)
        {
            // The host was let go of meanwhile.
        }

        _job?.Dispose();
    }

    /// <summary>Every line of the output, in order, until the output ends; then the program's end, once.</summary>
    private void ReadOutput()
    {
        try
        {
            while (_process.StandardOutput.ReadLine() is { } line)
            {
                _lineReceived(line);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // The pipe broke with the program: its end follows.
        }

        var code = -1;
        try
        {
            _process.WaitForExit();
            code = _process.ExitCode;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // The process is gone without a code to ask for.
        }

        _gone.Set();
        _writes.CompleteAdding();
        // What the program wrote as errors on its way out is read to its end first, so that whoever is told of the
        // end finds it whole; a program whose child still holds that pipe does not keep the end waiting for long.
        _errors?.Join(2000);
        if (Interlocked.Exchange(ref _exitedOnce, 1) == 0)
        {
            _exited(code);
        }

        _process.Dispose();
    }

    /// <summary>The error output, drained so that the program never waits on it, with its end kept.</summary>
    private void ReadErrors()
    {
        var buffer = new char[1024];
        try
        {
            int read;
            while ((read = _process.StandardError.Read(buffer, 0, buffer.Length)) > 0)
            {
                Interlocked.Add(ref _errorCharacters, read);
                lock (_errorGate)
                {
                    _errorTail.Append(buffer, 0, read);
                    if (_errorTail.Length > ErrorTailLength)
                    {
                        _errorTail.Remove(0, _errorTail.Length - ErrorTailLength);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // The pipe broke with the program.
        }
    }

    /// <summary>The lines given, written one by one as UTF-8 with a line feed and sent at once; the input closed when asked, or when the queue is.</summary>
    private void WriteInput()
    {
        try
        {
            using var input = new StreamWriter(_process.StandardInput.BaseStream, new UTF8Encoding(false)) { NewLine = "\n" };
            foreach (var line in _writes.GetConsumingEnumerable())
            {
                if (line is null)
                {
                    break;
                }

                input.WriteLine(line);
                input.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // The program no longer reads: it ended, and its end is reported by the reader.
        }
    }
}
