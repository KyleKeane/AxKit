using System.Text.Json;
using System.Text.Json.Serialization;
using AxKit.Core;

namespace AxClip.Core;

/// <summary>
/// Everything axClip remembers between runs (SPEC.md CP-6.1), in <c>%APPDATA%\AxKit\axClip.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font and the update check from <see cref="WindowSettings"/>, the hotkey,
/// Start with Windows and Say the first match while typing from <see cref="HotkeySettings"/>, Keep history between
/// runs, and the history only while that is on (CP-D2).
/// </summary>
public sealed class ClipSettings : HotkeySettings
{
    /// <summary>Windows+Alt+V (CP-D1).</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Windows | Hotkey.Alt, 'V');

    public bool KeepHistory { get; set; }

    /// <summary>The items, newest first, kept only while <see cref="KeepHistory"/> is on; on disk only as <see cref="ProtectedHistory"/>.</summary>
    [JsonIgnore]
    public List<ClipEntry> History { get; set; } = [];

    /// <summary>
    /// The history as the file holds it (security audit S-21): encrypted for this Windows account (<see cref="UserSecret"/>),
    /// so that the file in a backup, a roaming profile or another user's hands shows nothing of what was copied.
    /// </summary>
    [JsonPropertyName("protectedHistory")]
    public string? ProtectedHistory { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axClip");

    public static ClipSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<ClipSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path)
    {
        ProtectedHistory = KeepHistory && History.Count > 0 ? UserSecret.Protect(JsonSerializer.Serialize(History)) : null;
        SettingsFile.Save(path, this);
    }

    /// <summary>The window's history into the settings, or nothing of it with Keep history off.</summary>
    public void Keep(IEnumerable<ClipEntry> history) =>
        History = KeepHistory ? history.Take(ClipHistory.MaxItems).ToList() : [];

    private void Sanitize()
    {
        SanitizeHotkey();
        History = KeepHistory && ProtectedHistory is { } kept && UserSecret.Unprotect(kept) is { } json ? Read(json) : [];
        History = History.Where(entry => entry is { Text.Length: > 0 and <= ClipPolicy.MaxLength }).Take(ClipHistory.MaxItems).ToList();
    }

    private static List<ClipEntry> Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ClipEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
