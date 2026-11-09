namespace AxMCP.Core;

/// <summary>How a server is reached (SPEC.md MP-1.1): a program on standard input and output, or a network address, which this version lists and never starts (MP-1.4).</summary>
public enum ServerKind
{
    Stdio,
    Http,
    Sse,
    Unknown,
}

/// <summary>
/// One MCP server as the navigator lists it (SPEC.md MP-1): where it was defined (<see cref="Source"/>: "Claude Code,
/// user scope", "Claude Code, project scope", "Your servers"), how it is reached, and what starts it. <see cref="Env"/>
/// holds names and values the server's definition carries; the window shows the names alone, since a value may be a
/// token (MP-1.2).
/// </summary>
public sealed record McpServer(
    string Name,
    ServerKind Kind,
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env,
    string? Url,
    string Source,
    string? Folder,
    bool Own)
{
    /// <summary>The row's key in the navigator: the source and the name, since two sources may define one name.</summary>
    public string Key => $"{Source}: {Name}";

    /// <summary>The command and its arguments as one line, for the detail pane (MP-1.2).</summary>
    public string CommandLine => Args.Count == 0 ? Command : Command + " " + string.Join(" ", Args.Select(CommandLines.Quote));

    /// <summary>The kind as the row reads it.</summary>
    public string KindText => Kind switch
    {
        ServerKind.Stdio => "stdio",
        ServerKind.Http => "http",
        ServerKind.Sse => "sse",
        _ => "unknown kind",
    };
}
