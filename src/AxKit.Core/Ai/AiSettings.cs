using System.Text.Json.Serialization;

namespace AxKit.Core.Ai;

/// <summary>
/// AI help's one setting for the bundle (docs/axKit/ai-help.md AH-1.1), <c>%APPDATA%\AxKit\ai-help.json</c> through
/// <see cref="SettingsFile"/>: off by default; the folders Claude may read (AH-2.3); the model, empty for Claude Code's
/// default; and Claude Code's program when it is not where axKit looks (<see cref="ClaudeCodeProgram"/>), a relative
/// path taken from the settings folder (the automation's fake lives there); and Claude Code's working folder (AH-2.1),
/// the one it starts in, null for <see cref="DefaultWorkingFolder"/>. Read again at every use, since another window
/// may have changed it (AH-D5), as the AI tools' setting is.
/// </summary>
public sealed class AiSettings
{
    public bool Enabled { get; set; }

    public List<string> Folders { get; set; } = [];

    public string? Model { get; set; }

    public string? ClaudePath { get; set; }

    /// <summary>The folder Claude Code starts in (AH-2.1), where its CLAUDE.md, its settings and its memory live; null for the default.</summary>
    public string? WorkingFolder { get; set; }

    /// <summary>Documents\axKit Claude, a folder of Claude's own, made when first needed (AH-D6); under a test run's own home.</summary>
    public static string DefaultWorkingFolder =>
        Path.Combine(AutomationEnvironment.Home ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "axKit Claude");

    /// <summary>The working folder as a full path: the file's, or the default.</summary>
    public string ResolvedWorkingFolder() => WorkingFolder is null ? DefaultWorkingFolder : Path.GetFullPath(WorkingFolder);

    /// <summary>The working folder, made when it is missing; throws as <see cref="Directory.CreateDirectory(string)"/> does.</summary>
    public string EnsureWorkingFolder()
    {
        var folder = ResolvedWorkingFolder();
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>The folder the file was read from, which a relative <see cref="ClaudePath"/> is taken from.</summary>
    [JsonIgnore]
    public string Folder { get; private set; } = string.Empty;

    public static string DefaultPath => AppPaths.AiHelpFile;

    /// <summary>The seal over the switch, the folders, Claude Code's program and the working folder (bundle AX-5.6, AH-1.1a): written at every save, checked at every load.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>What the load's error text says when the seal does not check (AH-1.1a).</summary>
    public const string NotUsed = "AI help's settings were changed outside axKit and are not used: Claude is off, it may read no folder of yours, and Claude Code and its working folder are the usual ones. Turn Claude on and add the folders again from the axKit menu, AI help.";

    /// <summary>The sealed fields, in their order: the switch, Claude Code's program, the working folder, then each folder Claude may read.</summary>
    private string SealedText() =>
        SettingsSeal.Text([Enabled ? "on" : "off", ClaudePath, WorkingFolder, .. Folders ?? []]);

    public static AiSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<AiSettings>(path, out error);
        if (!SettingsSeal.Checks(path, settings.Seal, settings.SealedText(), new AiSettings().SealedText()))
        {
            settings.Enabled = false;
            settings.Folders = [];
            settings.ClaudePath = null;
            settings.WorkingFolder = null;
            error = SettingsSeal.Refused(error, NotUsed);
        }

        settings.Folder = Path.GetDirectoryName(path) ?? string.Empty;
        settings.Sanitize();
        return settings;
    }

    public void Save(string path)
    {
        Seal = SettingsSeal.Of(path, SealedText());
        // R-62: what another window saved meanwhile is kept, from a file whose seal checks, and sealed anew.
        if (SettingsFile.Save(path, this,
            file => SettingsSeal.Checks(path, file.Seal, file.SealedText(), new AiSettings().SealedText()),
            merged => merged.Seal = SettingsSeal.Of(path, merged.SealedText())))
        {
            SettingsSeal.Remember(path);
        }
    }

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
        WorkingFolder = string.IsNullOrWhiteSpace(WorkingFolder) ? null : WorkingFolder.Trim();
    }
}
