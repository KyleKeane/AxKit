using System.Text.Json.Serialization;
using AxKit.Core;

namespace AxGit.Core;

/// <summary>
/// A repository whose own programs the user chose to let git run (SPEC.md AG-1.6): its root, and the mark of what it
/// would run when it was trusted (<see cref="TrustScan.Mark"/>: the risks, the values of the settings that are not
/// harmless, and the content of every hook and of every file such a setting names); an empty mark is a trust from a
/// file written before 2026-11-16, which since the review's fifth pass (L5) trusts nothing: the repository is asked
/// about once more. A mark made before the review's fourth pass
/// (2026-11-28) marked the names alone and no longer matches: such a repository is asked about once more.
/// <paramref name="Identity"/> (the review's seventh pass, T1): which git folder it was trusted in
/// (<see cref="FolderIdentity.Text"/>); a trust without one, from before, trusts nothing and is asked about once more.
/// </summary>
public sealed record TrustedRepository(string Path, string Risks, string? Identity = null);

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
    /// (<paramref name="mark"/>, <see cref="TrustScan.Mark"/>) is the mark it had when it was trusted. A trust from before
    /// the marks, with none, is no trust (the review's fifth pass, L5: until then it counted for whatever the
    /// repository ran at its next opening): the repository is asked about once more, as one that was trusted.
    /// Since the seventh pass (T1) also in the same git folder, <paramref name="identity"/>, one whose identity lasts
    /// (<see cref="FolderIdentity.Lasting"/>): another repository at the same path, a clone made again among them, is
    /// asked about as a new one, and on a volume whose identities do not last no trust is kept.
    /// </summary>
    public bool Trusts(string root, string mark, FolderIdentity? identity) =>
        Find(root) is { } trusted && trusted.Risks.Length > 0 && trusted.Risks == mark
        && identity is { Lasting: true } && trusted.Identity == identity.Text;

    /// <summary>Whether the repository was trusted at all, with whatever it ran then: the notice says that its programs changed.</summary>
    public bool WasTrusted(string root) => Find(root) is not null;

    /// <summary>Whether what was trusted at this path was another git folder than <paramref name="identity"/>'s (T1): the notice says that nothing was trusted for this one.</summary>
    public bool TrustedAnother(string root, FolderIdentity? identity) =>
        Find(root) is { Identity: { } kept } && kept != identity?.Text;

    /// <summary>
    /// The repository trusted with what it would run now, in its git folder; false when it is trusted so already, and
    /// when its folder's identity does not last (T1), where nothing is kept: the trust holds while the repository stays
    /// open (<see cref="GitSession"/>).
    /// </summary>
    public bool Trust(string root, string mark, FolderIdentity? identity)
    {
        if (identity is not { Lasting: true })
        {
            return false;
        }

        var entry = new TrustedRepository(root, mark, identity.Text);
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

    /// <summary>
    /// The sealed fields: each trusted repository as its path and its mark, then the bare paths of an older file. A
    /// trust with its git folder's identity (T1 of the seventh pass) is written as <see cref="WithIdentity"/>, its path,
    /// its mark and its identity, so that a file written before, with none, keeps the seal it has; read as a trust of
    /// the older form, the same fields give paths that are no repository's (the words, a mark, an identity).
    /// </summary>
    private string SealedText() =>
        SettingsSeal.Text((Trusted ?? []).SelectMany(trusted => trusted?.Identity is { } identity
                ? new[] { WithIdentity, trusted.Path, trusted.Risks, identity }
                : new[] { trusted?.Path, trusted?.Risks })
            .Concat((TrustedRepositories ?? []).SelectMany(path => new[] { path, null })));

    /// <summary>The field in front of a trust with its git folder's identity, in the sealed text.</summary>
    private const string WithIdentity = "trusted in the git folder";

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
        // R-62: what another window saved meanwhile is kept, from a file whose seal checks, and sealed anew.
        if (SettingsFile.Save(path, this,
            file => SettingsSeal.Checks(path, file.Seal, file.SealedText(), new GitSettings().SealedText()),
            merged => merged.Seal = SettingsSeal.Of(path, merged.SealedText())))
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
