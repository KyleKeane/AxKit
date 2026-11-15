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

    /// <summary>
    /// The file Claude Code's <c>--mcp-config</c> reads (docs/axKit/mcp.md MC-6.9): one server, <see cref="ServerName"/>,
    /// this AxKit.exe with the argument <c>mcp</c>, over standard input and output. The same name as the entry
    /// <see cref="AddArguments"/> makes, so that Claude Code holds one axKit server, never two, and the tools' names
    /// are the same either way.
    /// </summary>
    public static string ConfigText(string axkitExe) =>
        new System.Text.Json.Nodes.JsonObject
        {
            ["mcpServers"] = new System.Text.Json.Nodes.JsonObject
            {
                [ServerName] = new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "stdio",
                    ["command"] = axkitExe,
                    ["args"] = new System.Text.Json.Nodes.JsonArray("mcp"),
                },
            },
        }.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    /// <summary>Claude Code's arguments that load the servers of <paramref name="configFile"/> for one session, beside the user's own.</summary>
    public static IReadOnlyList<string> ConfigArguments(string configFile) => ["--mcp-config", configFile];

    /// <summary>
    /// The file written for <paramref name="axkitExe"/> when it is missing or names another program file (another
    /// build, a copy that moved); true when it was written. Throws as <see cref="File.WriteAllText(string, string)"/> does.
    /// </summary>
    public static bool WriteConfig(string configFile, string axkitExe)
    {
        var text = ConfigText(axkitExe);
        if (File.Exists(configFile) && File.ReadAllText(configFile) == text)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configFile)!);
        File.WriteAllText(configFile, text);
        return true;
    }

    /// <summary>
    /// What a start of Claude Code gets for axKit's AI tools (MC-6.9): the <c>--mcp-config</c> arguments when the
    /// window asked for them and the user has the AI tools on, nothing otherwise. Pure: the caller reads the sealed
    /// setting and writes the file.
    /// </summary>
    public static IReadOnlyList<string> SessionArguments(bool asked, bool toolsOn, string configFile) =>
        asked && toolsOn ? ConfigArguments(configFile) : [];
}
