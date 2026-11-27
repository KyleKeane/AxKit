using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AxKit.Core;

/// <summary>
/// What a program run to its end gave (<see cref="ProcessRun"/>): its exit code, what it printed on each stream, and
/// whether the time ran out. <see cref="Finished"/> tells, after a timeout, whether the program itself had ended and
/// only a program it started still held its output open (a credential helper, a hook's daemon): then the exit code
/// is the program's and the output is all it printed. After a timeout that ended the program (<see cref="Finished"/>
/// false) the exit code is -1 and the output is what it had printed until then, a part: never a result to read as
/// the whole. <see cref="Stopped"/> is that case, and the compiler holds every caller to asking it: of a stopped
/// run <see cref="Output"/> is null, so that it can be read only after <see cref="Stopped"/> was asked (or the null
/// itself). Until the review's second pass of 2026-11-27 a test read the sources for the word and accepted it
/// anywhere in a caller's file, which held nothing. A caller that wants what was printed whatever became of the
/// run, to show it as that, says so by asking for <see cref="Printed"/>.
/// </summary>
public sealed record ProcessResult
{
    /// <summary>A run's result; <paramref name="Output"/> is what the program printed, whole or, for a run ended at its limit, the part until then.</summary>
    public ProcessResult(int ExitCode, string Output, string Error, bool TimedOut, bool Finished)
    {
        this.ExitCode = ExitCode;
        Printed = Output;
        this.Error = Error;
        this.TimedOut = TimedOut;
        this.Finished = Finished;
    }

    /// <summary>The program's own exit code; -1 for a run ended at its limit, which is never a success.</summary>
    public int ExitCode { get; }

    /// <summary>Everything the program printed, of a run that came to its own end; null of one ended at its limit, whose output is a part and no result.</summary>
    public string? Output => Stopped ? null : Printed;

    /// <summary>What the program printed, whatever became of the run: all of it, or the part until the limit. For showing as what it is, never for reading a result from.</summary>
    public string Printed { get; }

    /// <summary>What the program wrote as errors, whole or until the limit.</summary>
    public string Error { get; }

    /// <summary>The time ran out, whether or not the program itself had finished.</summary>
    public bool TimedOut { get; }

    /// <summary>The program itself came to its end.</summary>
    public bool Finished { get; }

    /// <summary>The program was ended at the time limit before it had finished: its output is a part, its exit code none, and nothing of it is a result.</summary>
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, nameof(Output))]
    public bool Stopped => TimedOut && !Finished;
}

/// <summary>
/// A program run without a window to its end, under a time limit (inventory item 3, 2026-10-25: git's runner, Claude
/// Code's version and KeePassXC's tool each wrote it): standard input given as lines, each ended by a line feed, then
/// closed; both outputs read to their end, under the limit as well as the wait, so that a full pipe never holds the
/// program and a pipe a child keeps open never hangs the caller; on a timeout the whole process tree is ended, and
/// what was printed until then is kept. The caller sets the program, its arguments as a list, its environment and its
/// encodings on the start info; this sets the redirections. A program that cannot be started throws as
/// <see cref="Process.Start()"/> does.
/// </summary>
public static class ProcessRun
{
    /// <summary>
    /// <paramref name="started"/> is handed the process right after it started, before anything is written to it:
    /// for a caller that puts it into a job of its own (<see cref="ChildJob"/>), so that it ends with the caller
    /// however the caller ends (axTask's actions).
    /// </summary>
    public static async Task<ProcessResult> RunAsync(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout, CancellationToken cancellation = default, Action<Process>? started = null)
    {
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        // Outside call, program, Claude Code, Git, KeePassXC, Windows: the one place a program is started without a
        // window and run to its end with both outputs read; which program and why is said at each call of Run and
        // RunAsync, in its own comment.
        using var process = new Process { StartInfo = start };
        process.Start();
        started?.Invoke(process);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(timeout);
        // The reads start before the input is written: a program that answers as it reads could fill a pipe first.
        // Each keeps what it has read as it reads, so that a run that is ended still says what it printed.
        using var readsEnd = new CancellationTokenSource();
        var output = new StringBuilder();
        var error = new StringBuilder();
        var reads = Task.WhenAll(ReadAsync(process.StandardOutput, output, readsEnd.Token), ReadAsync(process.StandardError, error, readsEnd.Token));
        bool finished;
        try
        {
            try
            {
                foreach (var line in input)
                {
                    await process.StandardInput.WriteAsync(line.AsMemory(), limit.Token).ConfigureAwait(false);
                    await process.StandardInput.WriteAsync("\n".AsMemory(), limit.Token).ConfigureAwait(false);
                }

                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The program ended, or closed its input, before it read all of it: not an error of the run. What it
                // wrote and its exit code say why, and the caller gets them (docs/axKit/repairs.json R-26).
            }

            await process.WaitForExitAsync(limit.Token).ConfigureAwait(false);
            // The program has ended, and its outputs end with it, unless a program it started holds one open: the
            // reads are waited for under the limit too.
            await reads.WaitAsync(limit.Token).ConfigureAwait(false);
            return new ProcessResult(process.ExitCode, Text(output), Text(error), TimedOut: false, Finished: true);
        }
        catch (OperationCanceledException)
        {
            // Whether the program itself had finished, taken before the kill, which ends it either way.
            finished = process.HasExited;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception kill) when (kill is InvalidOperationException or Win32Exception)
        {
            // Gone already.
        }

        cancellation.ThrowIfCancellationRequested();
        // What the program printed before it ended, or before it was ended, is in the pipes: the reads get a moment to
        // take it, however close to the limit it was printed, and are then ended with what they have.
        await Task.WhenAny(reads, Task.Delay(Drain, CancellationToken.None)).ConfigureAwait(false);
        readsEnd.Cancel();
        return new ProcessResult(finished ? process.ExitCode : -1, Text(output), Text(error), TimedOut: true, Finished: finished);
    }

    /// <summary>The same, waited for on the calling thread, for code that runs on a thread of its own already (axPass's tool, under <c>Task.Run</c>).</summary>
    public static ProcessResult Run(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout) =>
        RunAsync(start, input, timeout).GetAwaiter().GetResult();

    /// <summary>How long the reads go on after the time ran out, for what is already in the pipes.</summary>
    private static readonly TimeSpan Drain = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// One output read to its end into <paramref name="text"/>, piece by piece, so that what was read is there
    /// whenever the run is ended (until 2026-11-27 one read to the end, which, cancelled, gave nothing: a run that
    /// timed out had printed nothing, and a caller took an empty output with exit code 0 for a result). It ends
    /// without a word when it is cancelled, when the pipe breaks and when the process is let go.
    /// </summary>
    private static async Task ReadAsync(StreamReader reader, StringBuilder text, CancellationToken end)
    {
        var buffer = new char[4096];
        try
        {
            while (await reader.ReadAsync(buffer.AsMemory(), end).ConfigureAwait(false) is var count and > 0)
            {
                lock (text)
                {
                    text.Append(buffer, 0, count);
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
            // Ended, or nothing more can be read: what was read stays.
        }
    }

    private static string Text(StringBuilder text)
    {
        lock (text)
        {
            return text.ToString();
        }
    }
}
