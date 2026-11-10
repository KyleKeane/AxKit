using AxKit.Core;

namespace AxGit.Core;

/// <summary>
/// Everything axGIT remembers between runs (SPEC.md AG-8.1), in <c>%APPDATA%\AxKit\axGIT.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>,
/// plus the recent repositories. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class GitSettings : WindowSettings
{


    /// <summary>The repositories opened, newest first, for Repository → Open recent repository and the start without a path.</summary>
    public List<string> RecentRepositories { get; set; } = [];

    /// <summary>The repositories whose own programs (hooks, a file monitor, diff drivers) the user chose to let git run (AG-1.6).</summary>
    public List<string> TrustedRepositories { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axGIT");

    /// <summary>Whether the user trusts the repository's own programs.</summary>
    public bool Trusts(string root) => TrustedRepositories.Any(trusted => string.Equals(trusted, root, StringComparison.OrdinalIgnoreCase));

    public static GitSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<GitSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the repository at the front of <see cref="RecentRepositories"/>, once, and keeps the list to <see cref="RecentPaths.Max"/>.</summary>
    public void RememberRepository(string root) => RecentPaths.Remember(RecentRepositories, root, RecentPaths.Max);

    private void Sanitize()
    {
        SanitizeWindow();
        RecentRepositories = RecentPaths.Sanitize(RecentRepositories, RecentPaths.Max);
        TrustedRepositories = (TrustedRepositories ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
