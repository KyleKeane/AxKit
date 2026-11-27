using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AxMCP.Core;

/// <summary>
/// A stdio server as a running process (SPEC.md MP-2.1, MP-2.3): started with its command, arguments and environment
/// in its folder, its standard input and output wired to a <see cref="McpClient"/>, its standard error kept line by
/// line for the window (<see cref="ErrorLines"/>), with only each line's length in the log (MP-10; bundle repairs.md
/// R-74). <see cref="Stop"/> closes its input, gives it two seconds, and ends it with its children. The one outside
/// call of axMCP (bundle B-17).
/// </summary>
public sealed class StdioServer : IDisposable
{
    private readonly Process _process;

    /// <summary>The job the server and what it starts are in, so that none of it outlives this program however it ends; null where Windows gives none.</summary>
    private readonly AxKit.Core.ChildJob? _job;
    private volatile bool _stopping;

    private StdioServer(Process process, McpClient client, AxKit.Core.ChildJob? job)
    {
        _process = process;
        _job = job;
        Client = client;
    }

    public McpClient Client { get; }

    /// <summary>What the server wrote to its standard error since it started, for the window alone (MP-10).</summary>
    public ServerErrorLines ErrorLines { get; } = new();

    /// <summary>The process ended, by itself or by <see cref="Stop"/>; the exit code, and whether Stop asked for it.</summary>
    public event Action<int, bool>? Exited;

    public bool HasExited => _process.HasExited;

    /// <summary>The extensions tried for a command written without one, in Windows' order for programs.</summary>
    private static readonly string[] Extensions = [".exe", ".cmd", ".bat"];

    /// <summary>
    /// The program a server's command names (docs/axKit/repairs.json R-25, security audit S-1; SPEC.md MP-2.1b), as a
    /// full path: a full path as it is; a path that is not full (<c>.\server.exe</c>, <c>tools\server.cmd</c>) taken
    /// from <paramref name="folder"/>, the folder the server starts in (<see cref="FolderOf"/>), which for a server
    /// of a project's <c>.mcp.json</c> is the project's folder, where that file lies and where Claude Code starts
    /// it; a bare name found in the PATH's full folders only, with .exe, .cmd or .bat when it has no extension (npx
    /// is npx.cmd). Handed to Windows as it was until 2026-11-13, a bare name was looked for in the current folder
    /// first, and a name without .exe that is a batch file did not start at all; and until 2026-11-27 a path that
    /// was not full was handed on as it was, which Windows reads from axKit's own current folder, the program's,
    /// so that the program started was not the one the definition names, or none. Throws, naming the command, when
    /// nothing is found.
    /// </summary>
    public static string Resolve(string command, string? path, string folder)
    {
        if (command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar) || command.Contains(':'))
        {
            return Path.IsPathFullyQualified(command) ? command : Path.GetFullPath(command, Path.GetFullPath(folder));
        }

        var names = Path.HasExtension(command) ? [command] : Extensions.Select(extension => command + extension);
        foreach (var name in names)
        {
            if (AxKit.Core.SystemPrograms.OnPath(name, path) is { } found)
            {
                return found;
            }
        }

        throw new FileNotFoundException($"The program {command} is not in any folder of the PATH: give its full path in the server's command line (docs/axMCP/user-guide.md, Your servers).");
    }

    /// <summary>The folder a server starts in (MP-2.1): its own or the one in hand when that is there, the user's profile otherwise. The consent names the same (<see cref="ServerConsent"/>).</summary>
    public static string FolderOf(string? folder) =>
        folder is not null && Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static bool IsBatch(string program) =>
        program.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || program.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    /// <summary>Starts the server's program; throws as <see cref="Process.Start()"/> does when it cannot (MP-10), or with the reason when its command names no program or cannot be started safely.</summary>
    public static StdioServer Start(McpServer server, string? folder, Action<string> log)
    {
        var program = Resolve(server.Command, Environment.GetEnvironmentVariable("PATH"), FolderOf(folder));
        if (IsBatch(program) && server.Args.FirstOrDefault(AxKit.Core.SystemPrograms.HasCmdSpecials) is { } unsafeArgument)
        {
            // A .cmd or .bat runs through cmd.exe, which acts on these characters even inside quotes (S-3).
            throw new InvalidOperationException($"{Path.GetFileName(program)} is a batch program, and the argument {unsafeArgument} holds a character cmd.exe acts on (one of & | < > ^ % ! ( ) , ; = \"): it is not started. Name the program the batch file runs (node.exe, python.exe) with its script instead; docs/axMCP/user-guide.md, Your servers.");
        }

        var start = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
            WorkingDirectory = FolderOf(folder),
        };
        foreach (var argument in server.Args)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var variable in server.Env)
        {
            start.Environment[variable.Key] = variable.Value;
        }

        // Outside call, program, MCP servers: the MCP server the user chose to start (its command line from Claude
        // Code's configuration or from axMCP's own list), as a child process speaking the protocol on its standard
        // input and output; it does whatever its maker built it to do, under the user's consent (AX-11.4).
        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        process.Start();
        // In a job of its own from its first moment (AxKit.Core/ChildJob): a server over pipes has nothing that
        // ends it with its window, and one that ignores its closed input would stay when axMCP closes without
        // waiting for it (MP-2.3).
        var job = AxKit.Core.ChildJob.TryCreate();
        job?.Add(process);
        var client = new McpClient(process.StandardOutput, process.StandardInput, log);
        var running = new StdioServer(process, client, job);
        process.Exited += (_, _) => running.Exited?.Invoke(ExitCodeOf(process), running._stopping);
        var errors = new Thread(() =>
        {
            try
            {
                while (process.StandardError.ReadLine() is { } line)
                {
                    // R-74: the text stays in the window; the log holds that a line came and how long it was.
                    log(running.ErrorLines.Add(server.Name, line));
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                // The process is gone.
            }
        })
        {
            IsBackground = true,
            Name = "axMCP server errors",
        };
        errors.Start();
        client.Start();
        return running;
    }

    /// <summary>How long a server has to end by itself once its input is closed (MP-2.3).</summary>
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);

    private int _stopAsked;

    /// <summary>The end <see cref="Stop"/> started, done once the process and what it started are gone; done from the start for a server never stopped.</summary>
    public Task Ending { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// MP-2.3: the input closed, two seconds, then the process tree ended. The input is closed at once and the call
    /// returns; the two seconds and the end run on a task of their own (<see cref="Ending"/>), since the caller is
    /// the window's thread (rule 3; the review of 2026-11-27: the wait was the caller's until then, so that Stop
    /// held the window for two seconds, and removing or closing with several servers for two seconds each). Asked
    /// twice, it is the one end.
    /// </summary>
    public void Stop()
    {
        _stopping = true;
        if (Interlocked.Exchange(ref _stopAsked, 1) != 0)
        {
            return;
        }

        Client.Dispose();
        Ending = Task.Run(() =>
        {
            try
            {
                if (!_process.WaitForExit(Grace))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or AggregateException)
            {
                // Gone already, or a part of its tree Windows would not end: the job ends what is left.
            }

            // What the server started and left behind, which no tree reaches once the server itself is gone.
            _job?.Terminate();
        });
    }

    /// <summary>
    /// The server stopped and let go, without a wait: the process and its job are released once the end is done.
    /// Should this program end before that (the window closing), Windows ends the server with the job.
    /// </summary>
    public void Dispose()
    {
        Stop();
        Ending.ContinueWith(_ =>
        {
            _process.Dispose();
            _job?.Dispose();
        }, TaskScheduler.Default);
    }

    private static int ExitCodeOf(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }
}
