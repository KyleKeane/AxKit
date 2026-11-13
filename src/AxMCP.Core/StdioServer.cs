using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AxMCP.Core;

/// <summary>
/// A stdio server as a running process (SPEC.md MP-2.1, MP-2.3): started with its command, arguments and environment
/// in its folder, its standard input and output wired to a <see cref="McpClient"/>, its standard error to the log line
/// by line. <see cref="Stop"/> closes its input, gives it two seconds, and ends it with its children. The one outside
/// call of axMCP (bundle B-17).
/// </summary>
public sealed class StdioServer : IDisposable
{
    private readonly Process _process;
    private bool _stopping;

    private StdioServer(Process process, McpClient client)
    {
        _process = process;
        Client = client;
    }

    public McpClient Client { get; }

    /// <summary>The process ended, by itself or by <see cref="Stop"/>; the exit code, and whether Stop asked for it.</summary>
    public event Action<int, bool>? Exited;

    public bool HasExited => _process.HasExited;

    /// <summary>The extensions tried for a command written without one, in Windows' order for programs.</summary>
    private static readonly string[] Extensions = [".exe", ".cmd", ".bat"];

    /// <summary>
    /// The program a server's command names (docs/axKit/repairs.json R-25, security audit S-1): a path as it is; a bare
    /// name found in the PATH's full folders only, with .exe, .cmd or .bat when it has no extension (npx is npx.cmd).
    /// Handed to Windows as it was until 2026-11-13, a bare name was looked for in the current folder first, and a
    /// name without .exe that is a batch file did not start at all. Throws, naming the command, when nothing is found.
    /// </summary>
    public static string Resolve(string command, string? path)
    {
        if (command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar) || command.Contains(':'))
        {
            return command;
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

    private static bool IsBatch(string program) =>
        program.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || program.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    /// <summary>Starts the server's program; throws as <see cref="Process.Start()"/> does when it cannot (MP-10), or with the reason when its command names no program or cannot be started safely.</summary>
    public static StdioServer Start(McpServer server, string? folder, Action<string> log)
    {
        var program = Resolve(server.Command, Environment.GetEnvironmentVariable("PATH"));
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
            WorkingDirectory = folder is not null && Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };
        foreach (var argument in server.Args)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var variable in server.Env)
        {
            start.Environment[variable.Key] = variable.Value;
        }

        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        // Outside call, program, MCP servers: the MCP server the user chose to start (its command line from Claude
        // Code's configuration or from axMCP's own list), as a child process speaking the protocol on its standard
        // input and output; it does whatever its maker built it to do, under the user's consent (AX-11.4).
        process.Start();
        var client = new McpClient(process.StandardOutput, process.StandardInput, log);
        var running = new StdioServer(process, client);
        process.Exited += (_, _) => running.Exited?.Invoke(ExitCodeOf(process), running._stopping);
        var errors = new Thread(() =>
        {
            try
            {
                while (process.StandardError.ReadLine() is { } line)
                {
                    log($"{server.Name}: {line}");
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

    /// <summary>MP-2.3: the input closed, two seconds, then the process tree ended.</summary>
    public void Stop()
    {
        _stopping = true;
        Client.Dispose();
        try
        {
            if (!_process.WaitForExit(2000))
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // Gone already.
        }
    }

    public void Dispose()
    {
        Stop();
        _process.Dispose();
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
