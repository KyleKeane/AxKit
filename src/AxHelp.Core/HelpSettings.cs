using AxKit.Core;

namespace AxHelp.Core;

/// <summary>
/// Everything axHelp remembers between runs (SPEC.md HE-9), in <c>%APPDATA%\AxKit\axhelp.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>.
/// </summary>
public sealed class HelpSettings : WindowSettings
{
    public static string DefaultPath => AppPaths.SettingsFile("axhelp");

    public static HelpSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<HelpSettings>(path, out error);
        settings.SanitizeWindow();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
