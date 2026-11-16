using System.Security.Cryptography;
using System.Text;
using AxKit.Core;

namespace AxMCP.Core;

/// <summary>
/// What the user agrees to before a server starts (SPEC.md MP-2.1a; bundle AX-11.4, AX-11.5; repairs.md R-61): the
/// whole of what would run, as <see cref="ConsentTarget.CommandLineShell"/> does for a shell. The consent's target
/// holds the server's name, its program's resolved path with the arguments, the names of the environment variables
/// its definition sets, the folder it starts in, and a mark: the SHA-256 of all of these and of the variables' values,
/// exactly as they are. A definition that differs in any of them has another target, so the user is asked again. A
/// variable's value is never in the target, the notice or a log: it may be a token, and only the mark stands for it.
/// Tested in <c>tests/AxMCP.Tests/ServerConsentTests</c>.
/// </summary>
public static class ServerConsent
{
    /// <summary>The program's resolved path and the arguments as one line, as the notice reads it.</summary>
    public static string CommandLine(McpServer server, string program) =>
        server.Args.Count == 0 ? CommandLines.Quote(program) : CommandLines.Quote(program) + " " + string.Join(" ", server.Args.Select(CommandLines.Quote));

    /// <summary>The names of the environment variables the definition sets, in order of the alphabet; never a value.</summary>
    public static IReadOnlyList<string> EnvironmentNames(McpServer server) =>
        [.. server.Env.Keys.OrderBy(name => name, StringComparer.Ordinal)];

    /// <summary>
    /// The mark: SHA-256, as hex, of the name, the program, each argument, each variable's name and value and the
    /// folder, each with its length before it (<see cref="SettingsSeal.Text"/>), so that none runs into the next.
    /// </summary>
    public static string Mark(McpServer server, string program, string folder)
    {
        var fields = new List<string?> { server.Name, program, server.Args.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        fields.AddRange(server.Args);
        foreach (var name in EnvironmentNames(server))
        {
            fields.Add(name);
            fields.Add(server.Env[name]);
        }

        fields.Add(folder);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SettingsSeal.Text(fields)))).ToLowerInvariant();
    }

    /// <summary>
    /// The consent's target for a server whose program was found as <paramref name="program"/> and that would start
    /// in <paramref name="folder"/>. <see cref="ConsentTarget.Name"/> is the server's name, for the notice's
    /// sentences; <see cref="ConsentTarget.Program"/>, the record, is the whole.
    /// </summary>
    public static ConsentTarget For(McpServer server, string program, string folder)
    {
        var line = CommandLine(server, program);
        var names = EnvironmentNames(server);
        var environment = names.Count == 0 ? "no environment variables" : "environment variables " + string.Join(", ", names);
        var set = names.Count == 0
            ? "Its definition sets no environment variables."
            : $"Its definition sets the environment variables {string.Join(", ", names)}; their values are not read out, since they may be tokens.";
        return new ConsentTarget(
            $"{server.Name}, command line {line}, {environment}, folder {folder}, mark {Mark(server, program, folder)}",
            $"The MCP server {server.Name} is a program on this computer, which axMCP starts and talks to for you. Its command line is {line}\n"
            + $"It starts in the folder {folder}\n"
            + set + "\n"
            + "It does whatever its maker built it to do, on this computer and on any network it reaches, with your rights. axMCP asks again when the program, an argument, an environment variable or the folder changes.",
            "the server's own licence and terms",
            "https://modelcontextprotocol.io/")
        {
            Name = "the MCP server " + server.Name,
        };
    }
}
