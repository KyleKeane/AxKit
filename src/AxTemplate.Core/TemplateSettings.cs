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
    /// <summary>Ctrl+Shift+Semicolon (TP-D1, bundle AX-8.11; the maintainer's swap with axClip of 2026-11-23: templates are used more, so they have the key under the little finger).</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Control | Hotkey.Shift, 0xBA);

    /// <summary>Ctrl+Shift+Apostrophe, the default for a few hours of 2026-11-23 (axClip's until 2026-11-24, kept for axAutomate's launcher since): a file that holds it is taken over.</summary>
    protected override IReadOnlyList<Hotkey> RetiredDefaults => [new(Hotkey.Control | Hotkey.Shift, 0xDE)];

    /// <summary>The names of the templates used last from the launcher, newest first (TP-3.2): they lead the list of an empty field.</summary>
    public List<string> RecentTemplates { get; set; } = [];

    /// <summary>
    /// The choices of Options, Wait before the clipboard comes back (TP-4.3, TP-D6; the maintainer's answer of
    /// 2026-11-24, "a setting for it"), in milliseconds: how long after the paste key the clipboard's text of before is
    /// given back where the caret's mark cannot be found. A program that pastes late wants a longer one.
    /// </summary>
    public static readonly IReadOnlyList<int> GiveBackChoices = [300, 600, 1000, 2000, 4000];

    /// <summary>The default of <see cref="GiveBackMs"/>, the wait of 2026-11-23.</summary>
    public const int DefaultGiveBackMs = 600;

    /// <summary>How long after the paste the clipboard comes back, one of <see cref="GiveBackChoices"/>.</summary>
    public int GiveBackMs { get; set; } = DefaultGiveBackMs;

    /// <summary>A choice said as the menu and the announcement say it: "0.6 seconds".</summary>
    public static string SaidWait(int milliseconds) =>
        milliseconds % 1000 == 0 ? $"{milliseconds / 1000} second{(milliseconds == 1000 ? "" : "s")}" : (milliseconds / 1000.0).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture) + " seconds";

    public static string DefaultPath => AppPaths.SettingsFile("axTemplate");

    public static TemplateSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TemplateSettings>(path, out error);
        settings.SanitizeHotkey();
        settings.RecentTemplates = [.. (settings.RecentTemplates ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).Take(20)];
        if (!GiveBackChoices.Contains(settings.GiveBackMs))
        {
            settings.GiveBackMs = DefaultGiveBackMs;
        }
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
