using AxKit.Core;

namespace AxTemplate.Core;

/// <summary>
/// Everything axTemplate remembers between runs (SPEC.md TP-7.2), in <c>%APPDATA%\AxKit\axTemplate.json</c> through
/// the bundle's <see cref="SettingsFile"/>, one file for both of its windows: the font, the update check and the
/// browser's place from <see cref="WindowSettings"/>; the launcher's hotkey, Start with Windows and Say the first
/// match while typing from <see cref="HotkeySettings"/>; and the names of the templates used last.
/// </summary>
public sealed class TemplateSettings : HotkeySettings
{
    /// <summary>Ctrl+Shift+Apostrophe (TP-D1, bundle AX-8.11; the maintainer's choice of 2026-11-23): the key to the right of axClip's Semicolon.</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Control | Hotkey.Shift, 0xDE);

    /// <summary>The names of the templates used last from the launcher, newest first (TP-3.2): they lead the list of an empty field.</summary>
    public List<string> RecentTemplates { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axTemplate");

    public static TemplateSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TemplateSettings>(path, out error);
        settings.SanitizeHotkey();
        settings.RecentTemplates = [.. (settings.RecentTemplates ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).Take(20)];
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
