using System.IO.Pipes;

namespace AxClaude.Core.Hooks;

/// <summary>
/// The verb <c>AxKit.exe hook</c> (docs/axClaude/events-and-grounding.md EG-2.6): the program a hook of the session
/// starts for an event. It reads the event from its standard input, connects to the window's pipe named in the
/// environment the window gave the program (<see cref="HookFrame.Variable"/>), writes one frame and ends. No app, no
/// window, no output and the exit code 0 whatever happens, since the program that ran the hook prints a failing hook's
/// error into the conversation; a watchdog ends it after two seconds. Neutral: the event is relayed as bytes and read
/// by the window. <c>Program.Main</c> decides this verb before anything else runs, so that the process loads nothing of
/// the windows.
/// </summary>
public static class HookRelay
{
    /// <summary>The verb on the command line: <c>AxKit.exe hook</c>.</summary>
    public const string Verb = "hook";

    /// <summary>How long the connection to the window's pipe may take (EG-2.6).</summary>
    public const int ConnectMs = 500;

    /// <summary>How long the process lives at most, whatever it waits on (EG-2.6).</summary>
    public const int WatchdogMs = 2000;

    /// <summary>The verb's whole run: standard input to the window's pipe, then the process ends with 0.</summary>
    public static void Run()
    {
        var watchdog = new Thread(() =>
        {
            Thread.Sleep(WatchdogMs);
            Environment.Exit(0);
        })
        {
            IsBackground = true,
            Name = "AxKit hook watchdog",
        };
        watchdog.Start();
        try
        {
            // Outside call, pipe, Claude Code events: the standard input of "AxKit.exe hook", on which Claude Code writes
            // the event it started the hook for.
            using var input = Console.OpenStandardInput();
            Relay(input, Environment.GetEnvironmentVariable(HookFrame.Variable), Connect);
        }
        catch (Exception)
        {
            // Nothing is printed and nothing is thrown: a hook that fails puts an error row on the screen of the program
            // that ran it, and the exit code stays 0.
        }
    }

    /// <summary>
    /// Reads <paramref name="input"/> to its end and, when it holds a body of at most <see cref="HookFrame.MaxBody"/>
    /// bytes (its last line end left off) and the variable names a pipe, sends it as one frame through the stream
    /// <paramref name="connect"/> gives for the pipe's name and the time allowed. The input is read before anything else
    /// and always to its end, also when it is too long or there is nowhere to send it, so that the writer is never cut
    /// off in the middle of the event. True when the frame was written; every failure is false, never an exception.
    /// </summary>
    internal static bool Relay(Stream input, string? variableValue, Func<string, int, Stream?> connect)
    {
        try
        {
            var body = ReadBody(input);
            if (body is not { Length: > 0 } || !HookFrame.TryParseVariable(variableValue, out var name, out var secret))
            {
                return false;
            }

            using var pipe = connect(name, ConnectMs);
            if (pipe is null)
            {
                return false;
            }

            HookFrame.Write(pipe, secret, body);
            return true;
        }
        catch (Exception)
        {
            // A window that is gone, a pipe that refuses or breaks, input that cannot be read: the event is lost, and
            // the window works on without it (EG-2.10).
            return false;
        }
    }

    /// <summary>The window's pipe by its name, connected within <paramref name="timeoutMs"/>; it throws when the pipe is not there in time.</summary>
    internal static Stream? Connect(string name, int timeoutMs)
    {
        // Outside call, pipe, Claude Code events: connects to the pipe of the axClaude window that started this Claude
        // Code, named in the environment it gave, and writes the event; on this computer, never over the network.
        var pipe = new NamedPipeClientStream(".", name, PipeDirection.Out, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Identification); // the server may learn who connected, and may not act as them
        try
        {
            pipe.Connect(timeoutMs);
            // As the questions' relay: the secret is written only to a pipe this same program file serves (2026-11-21).
            if (!AxKit.Core.ThisProgram.Serves(pipe))
            {
                pipe.Dispose();
                return null;
            }

            return pipe;
        }
        catch (Exception)
        {
            pipe.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The input to its end without its last line end (LF or CR LF); null when it is longer than
    /// <see cref="HookFrame.MaxBody"/>. Past the limit the rest is read and dropped, never kept.
    /// </summary>
    private static byte[]? ReadBody(Stream input)
    {
        var limit = HookFrame.MaxBody + 2;
        using var kept = new MemoryStream();
        var buffer = new byte[65_536];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (kept.Length <= limit)
            {
                kept.Write(buffer, 0, read);
            }
        }

        if (kept.Length > limit)
        {
            return null;
        }

        var bytes = kept.ToArray();
        var length = bytes.Length;
        if (length > 0 && bytes[length - 1] == '\n')
        {
            length--;
            if (length > 0 && bytes[length - 1] == '\r')
            {
                length--;
            }
        }

        return length <= HookFrame.MaxBody ? bytes[..length] : null;
    }
}
