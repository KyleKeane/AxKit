using AxKit.Core;

namespace AxFile.Core;

/// <summary>
/// Everything axFile remembers between runs (SPEC.md AF-8.1), in <c>%APPDATA%\AxKit\axFile.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>,
/// plus the last folder, the recent folders, the favorites, whether hidden entries show, the sort order and the user's
/// own programs. A missing or unreadable file falls back to the defaults; the player's keys of an older file (speed,
/// seekSeconds, sayEachFile) are axMedia's now and are ignored here.
/// </summary>
public sealed class FileSettings : WindowSettings
{

    /// <summary>The folder the window had when it closed, for the start without a path.</summary>
    public string? LastFolder { get; set; }

    /// <summary>The folders opened by command (the start, Open folder, Go to folder, a recent item, the close), newest first, for Folder → Open recent folder.</summary>
    public List<string> RecentFolders { get; set; } = [];

    /// <summary>Hidden and system entries in the list (AF-2.2); off, as in File Explorer.</summary>
    public bool ShowHidden { get; set; }

    /// <summary>Entries whose name starts with a period (.git, .env) in the list (AF-2.2); off.</summary>
    public bool ShowDotFiles { get; set; }

    /// <summary>The list's order (AF-3.8), kept for the next folder and the next window.</summary>
    public SortBy SortBy { get; set; } = SortBy.Name;

    /// <summary>The folders the user pinned with Ctrl+D (AF-3.9), in the order added, for Folder → Favorites.</summary>
    public List<string> Favorites { get; set; } = [];

    /// <summary>The user's own programs and scripts of Open with a custom program ▸ (AF-4.11), in the order added.</summary>
    public List<UserProgram> Programs { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axFile");

    /// <summary>The seal over the user's own programs (bundle AX-5.6, AF-8.1a): written at every save, checked at every load.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>What the load's error text says when the seal does not check (AF-8.1a).</summary>
    public const string ProgramsNotUsed = "Your programs of Open with in axFile's settings were changed outside axKit and are not used. Add them again from Options, Add a program to Open with.";

    /// <summary>The sealed fields: each program as its name and its path.</summary>
    private string SealedText() =>
        SettingsSeal.Text((Programs ?? []).SelectMany(program => new[] { program?.Name, program?.Path }));

    public static FileSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<FileSettings>(path, out error);
        if (!SettingsSeal.Checks(path, settings.Seal, settings.SealedText(), new FileSettings().SealedText()))
        {
            settings.Programs = [];
            error = SettingsSeal.Refused(error, ProgramsNotUsed);
        }

        settings.Sanitize();
        return settings;
    }

    public override void Save(string path)
    {
        Seal = SettingsSeal.Of(path, SealedText());
        // R-62: what another window saved meanwhile is kept, from a file whose seal checks, and sealed anew.
        if (SettingsFile.Save(path, this,
            file => SettingsSeal.Checks(path, file.Seal, file.SealedText(), new FileSettings().SealedText()),
            merged => merged.Seal = SettingsSeal.Of(path, merged.SealedText())))
        {
            SettingsSeal.Remember(path);
        }
    }

    /// <summary>Puts the folder at the front of <see cref="RecentFolders"/>, once, keeps the list to <see cref="RecentPaths.Max"/>, and makes it the last folder.</summary>
    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RecentPaths.Remember(RecentFolders, folder);
    }

    private void Sanitize()
    {
        SanitizeWindow();
        RecentFolders = RecentPaths.Sanitize(RecentFolders);
        if (string.IsNullOrWhiteSpace(LastFolder))
        {
            LastFolder = null;
        }

        Programs ??= [];
        Programs.RemoveAll(program => string.IsNullOrWhiteSpace(program?.Name) || string.IsNullOrWhiteSpace(program?.Path));
        if (!Enum.IsDefined(SortBy))
        {
            SortBy = SortBy.Name;
        }

        Favorites ??= [];
        Favorites.RemoveAll(string.IsNullOrWhiteSpace);
        Favorites = Favorites.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Whether the folder is a favorite (AF-3.9).</summary>
    public bool IsFavorite(string folder) => Favorites.Any(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds the folder to the favorites, or removes it when it is one; true when it was added.</summary>
    public bool ToggleFavorite(string folder)
    {
        if (IsFavorite(folder))
        {
            Favorites.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            return false;
        }

        Favorites.Add(folder);
        return true;
    }
}
