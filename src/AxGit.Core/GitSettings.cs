using System.Text.Json.Serialization;
using AxKit.Core;

namespace AxGit.Core;

/// <summary>
/// A repository whose own programs the user chose to let git run (SPEC.md AG-1.6): its root, and the mark of what it
/// would run when it was trusted (<see cref="GitTrust.Mark"/>); an empty mark is a trust from a file written before
/// 2026-11-16, bound at the repository's next opening.
/// </summary>
public sealed record TrustedRepository(string Path, string Risks);

/// <summary>
/// Everything axGIT remembers between runs (SPEC.md AG-8.1), in <c>%APPDATA%\AxKit\axGIT.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>,
/// plus the recent repositories and the trusted ones, under the bundle's seal (AG-8.1a). A missing or unreadable file
/// falls back to the defaults.
/// </summary>
public sealed class GitSettings : WindowSettings
{
    /// <summary>The repositories opened, newest first, for Repository → Open recent repository and the start without a path.</summary>
    public List<string> RecentRepositories { get; set; } = [];

    /// <summary>The repositories whose own programs (hooks, a file monitor, diff drivers) the user chose to let git run (AG-1.6), each with the mark of what it ran then.</summary>
    public List<TrustedRepository> Trusted { get; set; } = [];

    /// <summary>The trusted repositories as a file written before 2026-11-16 holds them, bare paths: read, turned into <see cref="Trusted"/> with an empty mark, and no longer written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? TrustedRepositories { get; set; }

    /// <summary>The seal over the trusted repositories (bundle AX-5.6, AG-8.1a): written at every save, checked at every load.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>What the load's error text says when the seal does not check (AG-8.1a).</summary>
    public const string TrustNotUsed = "The repositories you trusted in axGIT's settings were changed outside axKit and are not used: git runs none of their own programs. To trust one again, open it and choose Trust this repository.";

    public static string DefaultPath => AppPaths.SettingsFile("axGIT");

    /// <summary>
    /// Whether the user trusts the repository's own programs as they are now (AG-1.6): trusted, and what it would run
    /// (<paramref name="risks"/>, <see cref="GitTrust.Risks"/>) is what it ran when it was trusted. A trust from before
    /// the marks counts once, and <see cref="Trust"/> then binds it.
    /// </summary>
    public bool Trusts(string root, IReadOnlyList<string> risks) =>
        Find(root) is { } trusted && (trusted.Risks.Length == 0 || trusted.Risks == GitTrust.Mark(risks));

    /// <summary>Whether the repository was trusted at all, with whatever it ran then: the notice says that its programs changed.</summary>
    public bool WasTrusted(string root) => Find(root) is not null;

    /// <summary>The repository trusted with what it would run now; false when it is trusted so already and nothing is to be saved.</summary>
    public bool Trust(string root, IReadOnlyList<string> risks)
    {
        var entry = new TrustedRepository(root, GitTrust.Mark(risks));
        if (Find(root) == entry)
        {
            return false;
        }

        Trusted.RemoveAll(trusted => string.Equals(trusted.Path, root, StringComparison.OrdinalIgnoreCase));
        Trusted.Add(entry);
        return true;
    }

    private TrustedRepository? Find(string root) =>
        Trusted.FirstOrDefault(trusted => string.Equals(trusted.Path, root, StringComparison.OrdinalIgnoreCase));

    /// <summary>The sealed fields: each trusted repository as its path and its mark, then the bare paths of an older file.</summary>
    private string SealedText() =>
        SettingsSeal.Text((Trusted ?? []).SelectMany(trusted => new[] { trusted?.Path, trusted?.Risks })
            .Concat((TrustedRepositories ?? []).SelectMany(path => new[] { path, null })));

    public static GitSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<GitSettings>(path, out error);
        if (!SettingsSeal.Checks(path, settings.Seal, settings.SealedText(), new GitSettings().SealedText()))
        {
            settings.Trusted = [];
            settings.TrustedRepositories = null;
            error = SettingsSeal.Refused(error, TrustNotUsed);
        }

        settings.Sanitize();
        return settings;
    }

    public override void Save(string path)
    {
        Seal = SettingsSeal.Of(path, SealedText());
        if (SettingsFile.Save(path, this))
        {
            SettingsSeal.Remember(path);
        }
    }

    /// <summary>Puts the repository at the front of <see cref="RecentRepositories"/>, once, and keeps the list to <see cref="RecentPaths.Max"/>.</summary>
    public void RememberRepository(string root) => RecentPaths.Remember(RecentRepositories, root, RecentPaths.Max);

    private void Sanitize()
    {
        SanitizeWindow();
        RecentRepositories = RecentPaths.Sanitize(RecentRepositories, RecentPaths.Max);
        // An older file's bare paths become trusts with no mark yet; a path named twice is kept the first time.
        var all = (Trusted ?? []).Where(trusted => trusted is not null && !string.IsNullOrWhiteSpace(trusted.Path))
            .Select(trusted => trusted with { Risks = trusted.Risks ?? string.Empty })
            .Concat((TrustedRepositories ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => new TrustedRepository(path, string.Empty)));
        Trusted = [];
        foreach (var trusted in all)
        {
            if (Find(trusted.Path) is null)
            {
                Trusted.Add(trusted);
            }
        }

        TrustedRepositories = null;
    }
}
