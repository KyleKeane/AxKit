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

    /// <summary>Starts the server's program; throws as <see cref="Process.Start()"/> does when it cannot (MP-10).</summary>
    public static StdioServer Start(McpServer server, string? folder, Action<string> log)
    {
        var start = new ProcessStartInfo(server.Command)
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
