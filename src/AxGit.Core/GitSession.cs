namespace AxGit.Core;

/// <summary>
/// One repository open in axGIT and what git is run with in it (SPEC.md AG-1.6, AG-1.6b, AG-5.4; the review's
/// third and fourth passes of 2026-11-27 and 2026-11-28): the last check of what it would run (<see cref="Scan"/>),
/// whether it is trusted, and the rules that hold whatever the window does:
/// <list type="bullet">
/// <item>Nothing runs in a repository that is not trusted (T1): every run goes through <see cref="RunAsync"/> or
/// <see cref="ChangeAsync"/>, which refuse while the last check found risks the user has not trusted, or could not
/// read the repository's files. A repository that holds no risk is trusted as it is.</item>
/// <item>A command that changes the repository goes through <see cref="ChangeAsync"/>, which checks again after
/// it, whether it succeeded or failed, before anything else can run (D8).</item>
/// <item>A changing command of a trusted repository whose hooks folder or a program lies in its work tree runs
/// without them (<see cref="TrustScan.InTree"/>, <see cref="GitTrust.Overrides"/>, T4): a switch or a pull can
/// change them while git runs, and the check after it would come too late.</item>
/// </list>
/// Plain and tested against git itself (<c>GitSessionTests</c>); the window holds one and asks it.
/// </summary>
public sealed class GitSession
{
    private readonly Func<string, bool> _trusts;
    private readonly IReadOnlyDictionary<string, string>? _environment;

    /// <summary>
    /// A session on <paramref name="root"/>, before its first check. <paramref name="trusts"/> says whether the
    /// user trusts the repository with exactly this mark (<see cref="TrustScan.Mark"/>; the window binds an older
    /// trust to it and saves it); <paramref name="environment"/>: variables for every run (the tests' settings files).
    /// </summary>
    public GitSession(string root, Func<string, bool> trusts, IReadOnlyDictionary<string, string>? environment = null)
    {
        Root = root;
        _trusts = trusts;
        _environment = environment;
    }

    public string Root { get; }

    /// <summary>The last check; a problem in it until the first (<see cref="CheckAsync"/>).</summary>
    public TrustScan Scan { get; private set; } = new([], "not checked yet");

    /// <summary>Whether git may run in the repository: its files could be read, and it holds no risk or the user trusts it for exactly this mark.</summary>
    public bool Trusted { get; private set; }

    /// <summary>Whether nothing may run any more: the last check could not be made.</summary>
    public bool Closed => !Scan.Known;

    /// <summary>How many checks were made, for the tests of D8.</summary>
    public int Checks { get; private set; }

    /// <summary>The check: what the repository would run read again (<see cref="GitTrustScan"/>), and whether it is trusted.</summary>
    public async Task<TrustScan> CheckAsync(CancellationToken cancellation = default)
    {
        var scan = await GitTrustScan.ReadAsync(Root, cancellation, _environment);
        Checks++;
        Scan = scan;
        Trusted = scan.Known && (scan.Risks.Count == 0 || _trusts(scan.Mark));
        return Scan;
    }

    /// <summary>The user trusted the repository for <paramref name="mark"/>: git runs in it from now on, when that is the mark the last check found (a notice answered after another check is for what that one found).</summary>
    public void TrustNow(string mark)
    {
        if (Scan.Known && Scan.Mark == mark)
        {
            Trusted = true;
        }
    }

    /// <summary>Why nothing runs now; null when git may run.</summary>
    public string? Refused =>
        Closed ? "axGIT runs nothing in this repository: what it would run is not known."
        : !Trusted ? "axGIT runs nothing in this repository until you trust it."
        : null;

    /// <summary>One git command that reads the repository. Refused while <see cref="Refused"/> says so: then nothing runs, and the result says why.</summary>
    public async Task<GitResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        if (Refused is { } refused)
        {
            return new GitResult(-1, string.Empty, refused);
        }

        return await GitRunner.RunAsync(Root, arguments, timeout, cancellation, environment: _environment);
    }

    /// <summary>
    /// A command that changes the repository: refused as <see cref="RunAsync"/> is; run with what lies in the work
    /// tree set aside (T4); then the check again, always, also when git failed, could not start or was refused
    /// (D8). The result is git's; <see cref="Scan"/> is the check after it.
    /// </summary>
    public async Task<GitResult> ChangeAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        try
        {
            if (Refused is { } refused)
            {
                return new GitResult(-1, string.Empty, refused);
            }

            // The empty hooks folder is made and given back on a task: it is a folder on disk (rule 3, L2).
            var inTree = Scan.InTree ?? [];
            var aside = await Task.Run(() => GitTrust.Overrides(inTree, []));
            try
            {
                return await GitRunner.RunAsync(Root, arguments, timeout, cancellation, aside, _environment);
            }
            finally
            {
                await Task.Run(() =>
                {
                    foreach (var hooks in aside.Where(setting => setting.Name == "core.hooksPath"))
                    {
                        GitTrust.ReleaseHooksFolder(hooks.Value);
                    }
                });
            }
        }
        finally
        {
            await CheckAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// D7: why a push does not start in a repository whose pre-push hook is Git LFS's: the hook would send the
    /// LFS files to the server the repository's own LFS settings name (a tracked <c>.lfsconfig</c> may name any
    /// host), which was not asked about, so axGIT pushes nothing there; null otherwise. Read from the hook's own
    /// text, in the hooks folder the last check found.
    /// </summary>
    public string? LfsPushRefused()
    {
        if (Scan.Hooks is not { } hooks)
        {
            return null;
        }

        try
        {
            var hook = Path.Combine(hooks, "pre-push");
            if (!File.Exists(hook))
            {
                return null;
            }

            using var reader = new StreamReader(hook);
            var buffer = new char[8192];
            var text = new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length));
            return text.Contains("git lfs", StringComparison.OrdinalIgnoreCase) || text.Contains("git-lfs", StringComparison.OrdinalIgnoreCase)
                ? "This repository's pre-push hook is Git LFS's, which would send the LFS files to a server of its own that axGIT does not ask about. Push in a terminal, where Git LFS sends them."
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "This repository's pre-push hook could not be read, so whether it is Git LFS's is not known. Push in a terminal.";
        }
    }
}
