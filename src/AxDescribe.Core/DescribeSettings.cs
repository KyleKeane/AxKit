using AxKit.Core;

namespace AxDescribe.Core;

/// <summary>
/// Everything axDescribe remembers between runs (SPEC.md DE-9), in <c>%APPDATA%\AxKit\axdescribe.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>.
/// </summary>
public sealed class DescribeSettings : WindowSettings
{
    public static string DefaultPath => AppPaths.SettingsFile("axdescribe");

    public static DescribeSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<DescribeSettings>(path, out error);
        settings.SanitizeWindow();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
