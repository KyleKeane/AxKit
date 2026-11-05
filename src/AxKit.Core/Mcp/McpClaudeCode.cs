namespace AxKit.Core.Mcp;

/// <summary>
/// axKit added to Claude Code as an MCP server (docs/axkit/mcp.md MC-6.8): where Claude Code is, and the two command lines
/// that register this AxKit.exe for every folder of the user, an earlier entry removed first. Pure but for the file
/// lookups, and tested; the window runs them (<c>AxKit.Forms/AxKitForm.Mcp.cs</c>).
/// </summary>
public static class McpClaudeCode
{
    /// <summary>The name axKit has among Claude Code's MCP servers.</summary>
    public const string ServerName = "axkit";

    /// <summary>The name up to 4.0.0, when the bundle was Axit (docs/axkit/SPEC.md B-16): an entry under it goes when this one is added.</summary>
    public const string OldServerName = "axit";

    /// <summary>
    /// Claude Code's program: <c>claude.exe</c> on the PATH's full folders or in its installer's folder, or npm's
    /// <c>claude.cmd</c>; null when it is not installed.
    /// </summary>
    public static string? Find()
    {
        if (SystemPrograms.OnPath("claude.exe") is { } onPath)
        {
            return onPath;
        }

        var native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");
        if (File.Exists(native))
        {
            return native;
        }

        return SystemPrograms.OnPath("claude.cmd");
    }

    /// <summary>Claude Code's arguments that remove an earlier entry of axKit's by its name (<see cref="ServerName"/>, <see cref="OldServerName"/>), for every folder.</summary>
    public static IReadOnlyList<string> RemoveArguments(string name) => ["mcp", "remove", "--scope", "user", name];

    /// <summary>Claude Code's arguments that add this AxKit.exe with the argument <c>mcp</c>, for every folder.</summary>
    public static IReadOnlyList<string> AddArguments(string axkitExe) => ["mcp", "add", "--scope", "user", ServerName, "--", axkitExe, "mcp"];

    /// <summary>The add as one command line to type in PowerShell or cmd, for when Claude Code is not found here.</summary>
    public static string AddCommandLine(string axkitExe) => $"claude mcp add --scope user {ServerName} -- \"{axkitExe}\" mcp";
}
