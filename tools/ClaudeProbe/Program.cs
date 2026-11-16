// ClaudeProbe: a measuring tool for axClaude (hooks, session files, the screen; docs/axClaude/events-and-grounding.md
// EG-11.4), never shipped and never started by axKit. It was the scratch probe of 2026-11-15, whose files are kept as
// they were; its commands still call it "probe".
//
//   probe run <config.json>            start Claude Code under axKit's pseudo console pieces, with hooks registered
//                                      through --settings, and record the screen, the hooks and the session file on one clock
//   probe timeline <output folder>     the three streams merged by time, one line each
//   probe shape <output folder>        the keys and value types of every hook event and transcript entry type
//   probe settings <config.json>       print the hooks settings the configuration gives (port 0 shown as PORT)
//   probe keys                         the key names a script's "keys" step takes
//   probe fixture <output folder> <fixture folder>
//                                      a run made into a timeline fixture through the whitelist (EG-11.1, FixtureMaker)
//   probe relay <port> <event>         (what a command hook runs) standard input posted to the listener; prints nothing
//
// See README.md in this folder.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Probe;

internal static class Program
{
    private static int Main(string[] args)
    {
        // First, and touching nothing else: this mode is one process per command hook.
        return args.Length >= 3 && args[0] == "relay" ? Relay.Run(args) : Commands(args);
    }

    private static int Commands(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        try
        {
            switch (args.Length > 0 ? args[0] : string.Empty)
            {
                case "run" when args.Length >= 2:
                    return Runner.Run(args[1]);
                case "timeline" when args.Length >= 2:
                    return Timeline.Run(args.Skip(1).ToArray());
                case "shape" when args.Length >= 2:
                    return Shape.Run(args.Skip(1).ToArray());
                case "settings" when args.Length >= 2:
                    return Runner.PrintSettings(args[1]);
                case "fixture" when args.Length >= 3:
                    return FixtureMaker.Run(args[1], args[2]);
                case "keys":
                    foreach (var pair in Keys.Table().OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"{pair.Key,-12} {Keys.Show(pair.Value)}");
                    }

                    Console.WriteLine("Also: any single character (\"y\", \"1\"), and Ctrl+<letter>.");
                    return 0;
                default:
                    Console.Error.WriteLine("Usage: probe run <config.json>");
                    Console.Error.WriteLine("       probe timeline <output folder> [--from <ms>] [--to <ms>] [--only PTY,HOOK,FILE,SEND,APP] [--three] [--all] [--dbg] [--width N]");
                    Console.Error.WriteLine("       probe shape <output folder> [hooks|file] [--values]");
                    Console.Error.WriteLine("       probe settings <config.json>");
                    Console.Error.WriteLine("       probe keys");
                    Console.Error.WriteLine("       probe fixture <output folder> <fixture folder>");
                    return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(Redactor.Redact(ex.ToString()));
            return 1;
        }
    }
}

/// <summary>
/// What a command hook runs: standard input read to its end and posted to the probe's listener, marked as having come
/// by command, with the times of this process (created, Main entered, input read) so that the cost of the transport
/// can be told from the hook's own timing. Prints nothing and exits 0, unless the listener's reply asks otherwise
/// (a configured reply body goes to standard output, X-Probe-Exit gives the exit code, X-Probe-Stderr the error text).
/// </summary>
internal static class Relay
{
    // Values are sent only for these; of every other CLAUDE* or ANTHROPIC* variable only the name (one could hold a token).
    private static readonly string[] ValuesShown =
    [
        "CLAUDE_PROJECT_DIR", "CLAUDE_EFFORT", "CLAUDECODE", "CLAUDE_CODE_CHILD_SESSION", "CLAUDE_CODE_ENTRYPOINT",
        "CLAUDE_PLUGIN_ROOT", "CLAUDE_PLUGIN_DATA", "CLAUDE_CODE_REMOTE", "CLAUDE_ENV_FILE", "CLAUDE_CODE_SESSION_ATTENDED",
    ];

    public static int Run(string[] args)
    {
        try
        {
            var mainAt = DateTime.UtcNow.Ticks;
            var port = int.Parse(args[1]);
            var name = args[2];
            byte[] body;
            var redirected = Console.IsInputRedirected;
            using (var memory = new MemoryStream())
            {
                if (redirected)
                {
                    using var stdin = Console.OpenStandardInput();
                    stdin.CopyTo(memory);
                }

                body = memory.ToArray();
            }

            var stdinEnd = DateTime.UtcNow.Ticks;
            long created;
            try
            {
                created = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
            }
            catch (Exception)
            {
                created = mainAt;
            }

            var env = new StringBuilder();
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = (string)entry.Key;
                if (!key.StartsWith("CLAUDE", StringComparison.OrdinalIgnoreCase) && !key.StartsWith("ANTHROPIC", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                env.Append(env.Length > 0 ? "&" : string.Empty).Append(Uri.EscapeDataString(key));
                if (ValuesShown.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    env.Append('=').Append(Uri.EscapeDataString((string?)entry.Value ?? string.Empty));
                }
            }

            var head = new StringBuilder();
            head.Append("POST /hook/").Append(name).Append("?via=command HTTP/1.1\r\n");
            head.Append("Host: 127.0.0.1:").Append(port).Append("\r\n");
            head.Append("Content-Type: application/json\r\n");
            head.Append("Content-Length: ").Append(body.Length).Append("\r\n");
            head.Append("Connection: close\r\n");
            head.Append("X-Probe-Pid: ").Append(Environment.ProcessId).Append("\r\n");
            head.Append("X-Probe-Created: ").Append(created).Append("\r\n");
            head.Append("X-Probe-Main: ").Append(mainAt).Append("\r\n");
            head.Append("X-Probe-Stdin-End: ").Append(stdinEnd).Append("\r\n");
            head.Append("X-Probe-Stdin-Redirected: ").Append(redirected ? "1" : "0").Append("\r\n");
            head.Append("X-Probe-Cwd: ").Append(Uri.EscapeDataString(Environment.CurrentDirectory)).Append("\r\n");
            head.Append("X-Probe-Env: ").Append(env).Append("\r\n");
            head.Append("\r\n");

            using var client = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
            client.Connect(IPAddress.Loopback, port);
            var stream = client.GetStream();
            stream.Write(Encoding.ASCII.GetBytes(head.ToString()));
            stream.Write(body);
            stream.Flush();

            // The reply, to the end of the connection (the listener closes it: Connection: close). It comes after the
            // configured delay, so a delayed reply holds this process, and with it the hook, for that long.
            using var response = new MemoryStream();
            stream.CopyTo(response);
            var bytes = response.ToArray();
            var split = -1;
            for (var i = 0; i + 3 < bytes.Length; i++)
            {
                if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n')
                {
                    split = i;
                    break;
                }
            }

            if (split < 0)
            {
                return 0;
            }

            var exit = 0;
            foreach (var line in Encoding.ASCII.GetString(bytes, 0, split).Split("\r\n"))
            {
                if (line.StartsWith("X-Probe-Exit:", StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(line["X-Probe-Exit:".Length..].Trim(), out exit);
                }
                else if (line.StartsWith("X-Probe-Stderr:", StringComparison.OrdinalIgnoreCase))
                {
                    using var stderr = Console.OpenStandardError();
                    stderr.Write(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(line["X-Probe-Stderr:".Length..].Trim())));
                }
            }

            if (bytes.Length > split + 4)
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(bytes, split + 4, bytes.Length - split - 4);
            }

            return exit;
        }
        catch (Exception)
        {
            // An observer never disturbs the session: no listener, no output, exit 0.
            return 0;
        }
    }
}
