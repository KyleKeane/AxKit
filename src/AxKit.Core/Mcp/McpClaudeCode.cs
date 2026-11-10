namespace AxKit.Core.Mcp;

/// <summary>
/// axKit added to Claude Code as an MCP server (docs/axKit/mcp.md MC-6.8): the two command lines that register this
/// AxKit.exe for every folder of the user, an earlier entry removed first. Pure and tested; the window runs them
/// (<c>AxKit.Forms/AxKitForm.Mcp.cs</c>) on the Claude Code that <see cref="ClaudeCodeProgram"/> finds.
/// </summary>
public static class McpClaudeCode
{
    /// <summary>The name axKit has among Claude Code's MCP servers.</summary>
    public const string ServerName = "axkit";

    /// <summary>The name up to 4.0.0, when the bundle was Axit (docs/axKit/SPEC.md B-16): an entry under it goes when this one is added.</summary>
    public const string OldServerName = "axit";

    /// <summary>Claude Code's arguments that remove an earlier entry of axKit's by its name (<see cref="ServerName"/>, <see cref="OldServerName"/>), for every folder.</summary>
    public static IReadOnlyList<string> RemoveArguments(string name) => ["mcp", "remove", "--scope", "user", name];

    /// <summary>Claude Code's arguments that add this AxKit.exe with the argument <c>mcp</c>, for every folder.</summary>
    public static IReadOnlyList<string> AddArguments(string axkitExe) => ["mcp", "add", "--scope", "user", ServerName, "--", axkitExe, "mcp"];

    /// <summary>The add as one command line to type in PowerShell or cmd, for when Claude Code is not found here.</summary>
    public static string AddCommandLine(string axkitExe) => $"claude mcp add --scope user {ServerName} -- \"{axkitExe}\" mcp";
}
