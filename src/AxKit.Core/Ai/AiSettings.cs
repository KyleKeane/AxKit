using System.Text.Json.Serialization;

namespace AxKit.Core.Ai;

/// <summary>
/// AI help's one setting for the bundle (docs/axKit/ai-help.md AH-1.1), <c>%APPDATA%\AxKit\ai-help.json</c> through
/// <see cref="SettingsFile"/>: off by default; the folders Claude may read (AH-2.3); the model, empty for Claude Code's
/// default; and Claude Code's program when it is not where axKit looks (<see cref="ClaudeCodeProgram"/>), a relative
/// path taken from the settings folder (the automation's fake lives there). Read again at every use, since another
/// window may have changed it (AH-D5), as the AI tools' setting is.
/// </summary>
public sealed class AiSettings
{
    public bool Enabled { get; set; }

    public List<string> Folders { get; set; } = [];

    public string? Model { get; set; }

    public string? ClaudePath { get; set; }

    /// <summary>The folder the file was read from, which a relative <see cref="ClaudePath"/> is taken from.</summary>
    [JsonIgnore]
    public string Folder { get; private set; } = string.Empty;

    public static string DefaultPath => AppPaths.AiHelpFile;

    public static AiSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<AiSettings>(path, out error);
        settings.Folder = Path.GetDirectoryName(path) ?? string.Empty;
        settings.Sanitize();
        return settings;
    }

    public void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Claude Code's program file: the one named here, made absolute from the settings folder, or the one axClaude would find; null when there is none.</summary>
    public string? FindClaude() => ClaudeCodeProgram.Find(ResolvedClaudePath());

    /// <summary>The path to search first, or null for the usual search.</summary>
    public string? ResolvedClaudePath() =>
        ClaudePath is null ? null : Path.IsPathRooted(ClaudePath) ? ClaudePath : Path.Combine(Folder, ClaudePath);

    private void Sanitize()
    {
        Folders = (Folders ?? [])
            .Select(folder => (folder ?? string.Empty).Trim())
            .Where(folder => folder.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Model = string.IsNullOrWhiteSpace(Model) ? null : Model.Trim();
        ClaudePath = string.IsNullOrWhiteSpace(ClaudePath) ? null : ClaudePath.Trim();
    }
}
