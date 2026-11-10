using AxKit.Core;

namespace AxMCP.Core;

/// <summary>One of the user's own servers (SPEC.md MP-1.3, MP-8): a name, the command line as typed, and a folder to start it in, or none.</summary>
public sealed class OwnServer
{
    public string Name { get; set; } = string.Empty;

    public string CommandLine { get; set; } = string.Empty;

    public string? Folder { get; set; }
}

/// <summary>
/// Everything axMCP remembers between runs (SPEC.md MP-8), in <c>%APPDATA%\AxKit\axMCP.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>, and
/// the user's own servers. No environment variables: a token belongs in the user's own environment or in Claude Code's
/// file, never here.
/// </summary>
public sealed class MCPSettings : WindowSettings
{
    public List<OwnServer> Servers { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axMCP");

    public static MCPSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<MCPSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>The own servers as the navigator lists them (MP-1.1), under <see cref="ServerSources.Own"/>.</summary>
    public IReadOnlyList<McpServer> OwnServers()
    {
        var servers = new List<McpServer>();
        foreach (var own in Servers)
        {
            var words = CommandLines.Split(own.CommandLine);
            if (words.Count > 0)
            {
                servers.Add(new McpServer(own.Name, ServerKind.Stdio, words[0], [.. words.Skip(1)], new Dictionary<string, string>(), null, ServerSources.Own, own.Folder, Own: true));
            }
        }

        return servers;
    }

    private void Sanitize()
    {
        SanitizeWindow();
        Servers = (Servers ?? []).Where(server => server is { Name.Length: > 0 } && server.CommandLine.Trim().Length > 0).ToList();
        foreach (var server in Servers)
        {
            server.Name = server.Name.Trim();
            if (server.Folder is { } folder && folder.Trim().Length == 0)
            {
                server.Folder = null;
            }
        }
    }
}
