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
    /// <summary>Ctrl+Shift+Comma (CP-D1, bundle AX-8.11; since 2026-11-24, the maintainer's move that gives Ctrl+Shift+Apostrophe to axAutomate's launcher; Apostrophe since the swap with axTemplate of 2026-11-23 before that).</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Control | Hotkey.Shift, 0xBC);

    /// <summary>Windows+Alt+V, the default until 2026-11-14, and Ctrl+Shift+Semicolon, until 2026-11-23 (CP-D1): a file that holds one is taken over to the new default.</summary>
    protected override IReadOnlyList<Hotkey> RetiredDefaults => [new(Hotkey.Windows | Hotkey.Alt, 'V'), new(Hotkey.Control | Hotkey.Shift, 0xBA), new(Hotkey.Control | Hotkey.Shift, 0xDE)];

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

    /// <summary>What the window says once when <see cref="HistoryNotRead"/> (CP-3.6; bundle repairs.md R-69).</summary>
    public const string HistoryNotReadText = "The kept clipboard history could not be read on this computer and starts empty.";

    /// <summary>
    /// The most text a kept history holds, in characters (CP-3.6): about 8 MB. The newest items are kept up to it and
    /// the older ones are left out of the file; the history in memory is not cut by it.
    /// </summary>
    public const int MaxKeptCharacters = 8 * 1024 * 1024;

    /// <summary>
    /// The file held a kept history that could not be opened (CP-3.6): Windows did not give it back (a profile moved
    /// to another computer, a password reset) or what it gave was no list of items. The history starts empty, the
    /// file as it was is kept beside it once, and the window says <see cref="HistoryNotReadText"/>.
    /// </summary>
    [JsonIgnore]
    public bool HistoryNotRead { get; private set; }

    public static ClipSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<ClipSettings>(path, out error);
        settings.Sanitize();
        if (settings.HistoryNotRead)
        {
            // Not an error of axClip's: the account or the computer changed. The window says it.
            Log.Info("The kept clipboard history could not be read and starts empty" + (KeepUnreadCopy(path) ? "; the file as it was is kept with .bad at the end of its name" : string.Empty));
        }

        return settings;
    }

    public override void Save(string path)
    {
        ProtectedHistory = KeepHistory && History.Count > 0 ? UserSecret.Protect(JsonSerializer.Serialize(History)) : null;
        SettingsFile.Save(path, this);
    }

    /// <summary>The window's history into the settings, or nothing of it with Keep history off.</summary>
    public void Keep(IEnumerable<ClipEntry> history) =>
        History = KeepHistory ? WithinTheLimit(history) : [];

    /// <summary>
    /// The newest items of a history, at most <see cref="ClipHistory.MaxItems"/> and at most
    /// <see cref="MaxKeptCharacters"/> of text together (CP-3.6): the list ends before the first item that would go
    /// over, so the oldest are the ones dropped and no newer item is kept behind a dropped one.
    /// </summary>
    public static List<ClipEntry> WithinTheLimit(IEnumerable<ClipEntry> newestFirst, int maxCharacters = MaxKeptCharacters)
    {
        var kept = new List<ClipEntry>();
        long total = 0;
        foreach (var entry in newestFirst.Take(ClipHistory.MaxItems))
        {
            total += entry.Text.Length;
            if (total > maxCharacters)
            {
                break;
            }

            kept.Add(entry);
        }

        return kept;
    }

    private void Sanitize()
    {
        SanitizeHotkey();
        List<ClipEntry>? read = null;
        if (KeepHistory && !string.IsNullOrEmpty(ProtectedHistory))
        {
            read = UserSecret.Unprotect(ProtectedHistory) is { } json ? Read(json) : null;
            HistoryNotRead = read is null;
        }

        History = WithinTheLimit((read ?? []).Where(entry => entry is { Text.Length: > 0 and <= ClipPolicy.MaxLength }));
    }

    /// <summary>The items of the JSON; null when it is no list of items.</summary>
    private static List<ClipEntry>? Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ClipEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The settings file copied beside itself as <c>.bad</c> (CP-3.6), since the next save writes the empty history
    /// over the one that could not be opened: on the computer or account it was kept on it can still be read. The
    /// copy holds the history only as the file did, encrypted. Once: not again while the copy is as new as the file.
    /// False when the copy is there already or cannot be made. Clear history removes it (CP-6.1).
    /// </summary>
    private static bool KeepUnreadCopy(string path)
    {
        var bad = path + ".bad";
        try
        {
            if (!File.Exists(path) || (File.Exists(bad) && File.GetLastWriteTimeUtc(bad) >= File.GetLastWriteTimeUtc(path)))
            {
                return false;
            }

            File.Copy(path, bad, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
