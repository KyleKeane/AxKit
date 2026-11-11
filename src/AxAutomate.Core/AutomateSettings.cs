using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// Everything axAutomate remembers between runs (SPEC.md AU-4.1), in <c>%APPDATA%\AxKit\axAutomate.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, and the folder of workflows opened last.
/// </summary>
public sealed class AutomateSettings : WindowSettings
{
    /// <summary>The folder of workflows opened last, opened again by a start without a path; null when none.</summary>
    public string? Folder { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axAutomate");

    public static AutomateSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<AutomateSettings>(path, out error);
        settings.SanitizeWindow();
        if (string.IsNullOrWhiteSpace(settings.Folder))
        {
            settings.Folder = null;
        }

        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
