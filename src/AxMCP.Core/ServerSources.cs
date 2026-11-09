using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxMCP.Core;

/// <summary>
/// Where the servers come from (SPEC.md MP-1.1): Claude Code's user file, <c>%USERPROFILE%\.claude.json</c>, with its
/// <c>mcpServers</c> (the user scope) and, for the folder axMCP was started on, that folder's entry under
/// <c>projects</c> (the local scope); the folder's own <c>.mcp.json</c> (the project scope); and the user's own list in
/// axMCP's settings. The pure parts take the files' texts, so that they are tested without a file; <see cref="Read"/>
/// reads the files. Claude Code's files are read and never written (MP-D3).
/// </summary>
public static class ServerSources
{
    public const string UserScope = "Claude Code, user scope";
    public const string LocalScope = "Claude Code, local scope";
    public const string ProjectScope = "Claude Code, project scope";
    public const string Own = "Your servers";

    /// <summary>The name of Claude Code's user file in the profile folder.</summary>
    public const string UserFileName = ".claude.json";

    /// <summary>The name of the project file in a folder.</summary>
    public const string ProjectFileName = ".mcp.json";

    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    /// <summary>Every server of the three files for <paramref name="folder"/> (null for no folder in hand): the user scope, then the local scope, then the project scope. A file missing or unreadable gives nothing of its own.</summary>
    public static IReadOnlyList<McpServer> Read(string? folder)
    {
        var servers = new List<McpServer>();
        var userFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), UserFileName);
        servers.AddRange(FromClaudeCode(TextOrNull(userFile), folder));
        if (folder is not null)
        {
            servers.AddRange(FromProjectFile(TextOrNull(Path.Combine(folder, ProjectFileName)), folder));
        }

        return servers;
    }

    /// <summary>The user scope's servers, and the local scope's for <paramref name="folder"/>, from the user file's text.</summary>
    public static IReadOnlyList<McpServer> FromClaudeCode(string? text, string? folder)
    {
        if (Parse(text) is not { } root)
        {
            return [];
        }

        var servers = new List<McpServer>(Parse(root["mcpServers"] as JsonObject, UserScope, null, own: false));
        if (folder is not null && root["projects"] is JsonObject projects)
        {
            var wanted = Normalize(folder);
            foreach (var project in projects)
            {
                if (string.Equals(Normalize(project.Key), wanted, StringComparison.OrdinalIgnoreCase) && project.Value is JsonObject entry)
                {
                    servers.AddRange(Parse(entry["mcpServers"] as JsonObject, LocalScope, folder, own: false));
                }
            }
        }

        return servers;
    }

    /// <summary>The project scope's servers from a folder's <c>.mcp.json</c> text.</summary>
    public static IReadOnlyList<McpServer> FromProjectFile(string? text, string folder) =>
        Parse(text) is { } root ? Parse(root["mcpServers"] as JsonObject, ProjectScope, folder, own: false) : [];

    /// <summary>The servers of one <c>mcpServers</c> object, in its order; an entry that is not an object is skipped.</summary>
    public static IReadOnlyList<McpServer> Parse(JsonObject? servers, string source, string? folder, bool own)
    {
        if (servers is null)
        {
            return [];
        }

        var list = new List<McpServer>();
        foreach (var pair in servers)
        {
            if (pair.Value is JsonObject entry && One(pair.Key, entry, source, folder, own) is { } server)
            {
                list.Add(server);
            }
        }

        return list;
    }

    /// <summary>One server's definition in Claude Code's shape: <c>command</c> with <c>args</c> and <c>env</c>, or <c>type</c> with <c>url</c>.</summary>
    public static McpServer? One(string name, JsonObject entry, string source, string? folder, bool own)
    {
        if (name.Trim().Length == 0)
        {
            return null;
        }

        var type = Text(entry["type"])?.Trim().ToLowerInvariant();
        var command = Text(entry["command"]) ?? string.Empty;
        var url = Text(entry["url"]);
        var kind = type switch
        {
            "stdio" => ServerKind.Stdio,
            "http" or "streamable-http" or "streamable_http" => ServerKind.Http,
            "sse" => ServerKind.Sse,
            null or "" => command.Length > 0 ? ServerKind.Stdio : url is not null ? ServerKind.Http : ServerKind.Unknown,
            _ => ServerKind.Unknown,
        };
        var args = new List<string>();
        if (entry["args"] is JsonArray given)
        {
            foreach (var item in given)
            {
                if (Text(item) is { } arg)
                {
                    args.Add(arg);
                }
            }
        }

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (entry["env"] is JsonObject variables)
        {
            foreach (var variable in variables)
            {
                if (Text(variable.Value) is { } value)
                {
                    env[variable.Key] = value;
                }
            }
        }

        var cwd = Text(entry["cwd"]);
        return new McpServer(name.Trim(), kind, command, args, env, url, source, cwd ?? folder, own);
    }

    private static JsonObject? Parse(string? text)
    {
        if (text is null)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(text, documentOptions: Lenient) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TextOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Normalize(string folder) => Path.TrimEndingDirectorySeparator(folder.Replace('/', '\\').Trim());

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
