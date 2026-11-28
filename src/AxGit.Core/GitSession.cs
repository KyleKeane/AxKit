namespace AxGit.Core;

/// <summary>
/// One repository open in axGIT and what git is run with in it (SPEC.md AG-1.6, AG-1.6b, AG-5.4; the review's
/// third, fourth and fifth passes of 2026-11-27 and 2026-11-28): the last check of what it would run
/// (<see cref="Scan"/>), whether it is trusted, and the rules that hold whatever the window does:
/// <list type="bullet">
/// <item>Nothing runs in a repository that is not trusted (T1): every run goes through <see cref="RunAsync"/> or
/// <see cref="ChangeAsync"/>, which refuse while the last check found risks the user has not trusted, or could not
/// read the repository's files. A repository that holds no risk is trusted as it is.</item>
/// <item>git works on the repository that was read and no other (P1 of the fifth pass): every run names the git
/// folder the check read (<see cref="TrustScan.GitDir"/>) and the root as the work tree.</item>
/// <item>Trusted is what was seen (P3): a trust the user gives is checked again (<see cref="TrustAsync"/>) and holds
/// only when what is there now is what the notice showed; a command that changes the repository goes through
/// <see cref="ChangeAsync"/>, which checks before it, and runs only when the check still holds, and checks again
/// after it, whether it succeeded or failed (D8).</item>
/// <item>A changing command of a trusted repository whose hooks folder or a program lies in its work tree runs
/// without them (<see cref="TrustScan.InTree"/>, <see cref="GitTrust.Overrides"/>, T4): a switch or a pull can
/// change them while git runs, and the check after it would come too late.</item>
/// </list>
/// Plain and tested against git itself (<c>GitSessionTests</c>, <c>GitBoundsTests</c>); the window holds one and asks it.
/// </summary>
public sealed class GitSession
{
    private readonly Func<string, bool> _trusts;
    private readonly IReadOnlyDictionary<string, string>? _environment;

    /// <summary>The mark the user trusted in this session, once a check after it found the same (<see cref="TrustAsync"/>).</summary>
    private string? _trustedMark;

    /// <summary>The git folder git itself agreed to open by its ownership check (<see cref="CheckAsync"/>).</summary>
    private string? _agreed;

    /// <summary>
    /// A session on <paramref name="root"/>, before its first check. <paramref name="trusts"/> says whether the
    /// user trusts the repository with exactly this mark (<see cref="TrustScan.Mark"/>; the window asks its
    /// settings); <paramref name="environment"/>: variables for every run (the tests' settings files).
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

    /// <summary>How many checks were made, for the tests of D8 and P3.</summary>
    public int Checks { get; private set; }

    /// <summary>
    /// The check: what the repository would run read again (<see cref="GitTrustScan"/>), and whether it is trusted.
    /// It fails closed (L2 of the fifth pass): nothing is trusted from its start, and a read that throws leaves the
    /// scan a problem, so that nothing runs; git missing is still thrown, for the window to say. The first time a
    /// git folder is trusted, git is asked whether it opens it itself (<c>rev-parse</c>, finding the repository from
    /// the root alone): a named git folder passes over git's own check of who owns a repository
    /// (<c>safe.directory</c>), which this keeps.
    /// </summary>
    public async Task<TrustScan> CheckAsync(CancellationToken cancellation = default) => await CheckAsync(null, cancellation);

    private async Task<TrustScan> CheckAsync(string? trusting, CancellationToken cancellation)
    {
        Trusted = false;
        Checks++;
        try
        {
            Scan = await GitTrustScan.ReadAsync(Root, cancellation, _environment);
        }
        catch (GitNotFoundException)
        {
            Scan = new TrustScan([], "git was not found, so the repository could not be checked.");
            throw;
        }
        catch (Exception ex)
        {
            Scan = new TrustScan([], $"The repository could not be checked ({ex.Message}).");
            return Scan;
        }

        var trusted = Scan.Known && (Scan.Risks.Count == 0 || Scan.Mark == trusting || Scan.Mark == _trustedMark || _trusts(Scan.Mark));
        if (trusted && Scan.GitDir is { } gitDir && !string.Equals(_agreed, gitDir, StringComparison.OrdinalIgnoreCase))
        {
            var asked = await GitRunner.RunAsync(Root, ["rev-parse", "--absolute-git-dir"], GitRunner.Short, cancellation, environment: _environment, findRepository: true);
            if (!asked.Ok)
            {
                Scan = new TrustScan([], $"git does not open it: {asked.FirstLine}");
                return Scan;
            }

            _agreed = gitDir;
        }

        Trusted = trusted;
        return Scan;
    }

    /// <summary>
    /// The user trusted the repository for <paramref name="mark"/>, the mark of what the notice showed (P3 of the
    /// fifth pass): it is checked again, and git runs in it only when what is there now has that mark. False when it
    /// does not (a filter added while the notice was open): then the notice comes again, with what is there now.
    /// </summary>
    public async Task<bool> TrustAsync(string mark, CancellationToken cancellation = default)
    {
        await CheckAsync(mark, cancellation);
        if (Trusted && Scan.Risks.Count > 0)
        {
            _trustedMark = Scan.Mark;
        }

        return Trusted;
    }

    /// <summary>Why nothing runs now; null when git may run.</summary>
    public string? Refused =>
        Closed ? "axGIT runs nothing in this repository: what it would run is not known."
        : !Trusted ? "axGIT runs nothing in this repository until you trust it."
        : null;

    /// <summary>A result for a command that did not run, with the reason.</summary>
    private static GitResult NotRun(string reason) => new(-1, string.Empty, reason) { NotRun = true };

    /// <summary>The git folder every run names: the one the last check read (P1).</summary>
    private string GitDir => Scan.GitDir ?? Path.Combine(Root, ".git");

    /// <summary>One git command that reads the repository. Refused while <see cref="Refused"/> says so: then nothing runs, and the result says why.</summary>
    public async Task<GitResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        if (Refused is { } refused)
        {
            return NotRun(refused);
        }

        return await GitRunner.RunAsync(Root, arguments, timeout, cancellation, environment: _environment, gitDir: GitDir);
    }

    /// <summary>
    /// A command that changes the repository: the check first (P3), and the command refused when it does not hold,
    /// as <see cref="RunAsync"/> refuses; run with what lies in the work tree set aside (T4) and with
    /// <paramref name="settings"/>, the command's own (a fetch's addresses with no redirect, L1); then the check
    /// again, always, also when git failed or could not start (D8). The result is git's; <see cref="Scan"/> is the
    /// check after it.
    /// </summary>
    public async Task<GitResult> ChangeAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default, IReadOnlyList<GitSetting>? settings = null)
    {
        if (Refused is { } refused)
        {
            return NotRun(refused);
        }

        await CheckAsync(cancellation);
        if (Refused is { } changed)
        {
            return NotRun(changed);
        }

        try
        {
            // The empty hooks folder is made and given back on a task: it is a folder on disk (rule 3, L2).
            var inTree = Scan.InTree ?? [];
            var aside = await Task.Run(() => GitTrust.Overrides(inTree, []));
            try
            {
                return await GitRunner.RunAsync(Root, arguments, timeout, cancellation, [.. settings ?? [], .. aside], _environment, GitDir);
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
    /// text, in every hooks folder the last check found (L5: one an included file names too), each through
    /// <see cref="RepositoryFiles"/>.
    /// </summary>
    public string? LfsPushRefused()
    {
        foreach (var hooks in Scan.Hooks ?? [])
        {
            var hook = Path.Combine(hooks, "pre-push");
            if (RepositoryFiles.Attributes(hook, Root) is null)
            {
                continue;
            }

            if (RepositoryFiles.Text(hook, Root, 8192) is not { } read)
            {
                return "This repository's pre-push hook could not be read, so whether it is Git LFS's is not known. Push in a terminal.";
            }

            if (read.Text.Contains("git lfs", StringComparison.OrdinalIgnoreCase) || read.Text.Contains("git-lfs", StringComparison.OrdinalIgnoreCase))
            {
                return "This repository's pre-push hook is Git LFS's, which would send the LFS files to a server of its own that axGIT does not ask about. Push in a terminal, where Git LFS sends them.";
            }
        }

        return null;
    }
}
