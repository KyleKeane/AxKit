using AxKit.Core;

namespace AxStart.Core;

/// <summary>
/// Everything axStart remembers between runs (SPEC.md AS-5.1), in <c>%APPDATA%\AxKit\axstart.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font and the update check from <see cref="WindowSettings"/> (the window's
/// place is not used: the window is centred), the hotkey, Start with Windows and Say the first match while typing
/// from <see cref="HotkeySettings"/>, and how often axStart started each program, for the order of the matches. A
/// missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class StartSettings : HotkeySettings
{
    /// <summary>Ctrl+Shift+Space (AS-4.1).</summary>
    protected override Hotkey DefaultHotkey => Hotkey.Default;

    /// <summary>Each program's display name and how often axStart started it (AS-2.2).</summary>
    public Dictionary<string, int> Starts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static string DefaultPath => AppPaths.SettingsFile("axstart");

    public static StartSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<StartSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>One more start of the program (AS-2.3).</summary>
    public void Started(string name) => Starts[name] = (Starts.TryGetValue(name, out var count) ? count : 0) + 1;

    private void Sanitize()
    {
        SanitizeHotkey();
        // Read back with the serializer's own comparer: names differing in case only are added together.
        var starts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, count) in Starts ?? [])
        {
            if (!string.IsNullOrWhiteSpace(name) && count > 0)
            {
                starts[name] = (starts.TryGetValue(name, out var before) ? before : 0) + count;
            }
        }

        Starts = starts;
    }
}
