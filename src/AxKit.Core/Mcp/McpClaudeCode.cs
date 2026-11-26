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
    /// are the same either way. <c>alwaysLoad</c> (2026-11-26): Claude Code connects its servers in the background, and a
    /// message sent in the first seconds went out before axKit's tools were there (Claude answered that it had none,
    /// live script 60); with it Claude Code waits for this server before the first request, five seconds at most, and
    /// loads its tools up front, so that Claude knows them from its first message.
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
                    ["alwaysLoad"] = true,
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
    /// What a start of Claude Code gets for axKit's AI tools (MC-6.9), in every axClaude window since 2026-11-26: the
    /// <c>--mcp-config</c> arguments while the user has the AI tools on, nothing while they are off, and nothing when
    /// the user's own Claude Code configuration already names the server (<paramref name="userNamesServer"/>, MC-6.8),
    /// which then gives the tools itself and must not be named a second time. Pure: the caller reads the sealed
    /// setting and the user's file, and writes the file.
    /// </summary>
    public static IReadOnlyList<string> SessionArguments(bool toolsOn, bool userNamesServer, string configFile) =>
        toolsOn && !userNamesServer ? ConfigArguments(configFile) : [];

    /// <summary>The name of Claude Code's own file of the user's, whose <c>mcpServers</c> is the user scope MC-6.8 adds to.</summary>
    public const string UserFileName = ".claude.json";

    /// <summary>
    /// Claude Code's own file of the user's for a Claude Code started with <paramref name="configDir"/> as its
    /// <c>CLAUDE_CONFIG_DIR</c>: <see cref="UserFileName"/> in that folder when it is a full path, else in
    /// <paramref name="home"/> (the user profile, or a test run's own folder). Read, never written.
    /// </summary>
    public static string UserFile(string home, string? configDir) =>
        configDir is { } folder && Path.IsPathFullyQualified(folder.Trim())
            ? Path.Combine(folder.Trim(), UserFileName)
            : Path.Combine(home, UserFileName);

    private static readonly System.Text.Json.JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = System.Text.Json.JsonCommentHandling.Skip };

    /// <summary>
    /// Whether the text of the user's own Claude Code file (<see cref="UserFile"/>) names a server
    /// <see cref="ServerName"/> in its user scope, the top-level <c>mcpServers</c>, as MC-6.8's add writes it: the
    /// user added axKit to Claude Code himself. Nothing else of the file is looked at; a missing file, a text that is
    /// no JSON object, or no such entry is false.
    /// </summary>
    public static bool NamesServer(string? userFileText)
    {
        if (string.IsNullOrWhiteSpace(userFileText))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(userFileText, Lenient);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && document.RootElement.TryGetProperty("mcpServers", out var servers)
                && servers.ValueKind == System.Text.Json.JsonValueKind.Object
                && servers.TryGetProperty(ServerName, out _);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
