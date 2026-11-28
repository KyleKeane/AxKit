namespace AxGit.Core;

/// <summary>
/// One repository open in axGIT and what git is run with in it (SPEC.md AG-1.6, AG-1.6b, AG-5.4; the review's
/// third, fourth and fifth passes of 2026-11-27 and 2026-11-28): the last check of what it would run
/// (<see cref="Scan"/>), whether it is trusted, and the rules that hold whatever the window does:
/// <list type="bullet">
/// <item>Nothing runs in a repository that is not trusted (T1): every run goes through <see cref="RunAsync"/> or
/// <see cref="ChangeAsync"/>, which refuse while the last check found risks the user has not trusted, or could not
/// read the repository's files. Since the review's sixth pass (G1) no repository is trusted without the user: one whose check found nothing is asked about once too, and a trust is kept with the mark of what was found and, since the seventh pass (T1), the identity of the git folder it was found in.</item>
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
    private readonly Func<TrustScan, bool> _trusts;
    private readonly IReadOnlyDictionary<string, string>? _environment;

    /// <summary>What the user trusted in this session, once a check after it found the same (<see cref="TrustAsync"/>): its mark and its git folder's identity.</summary>
    private TrustScan? _trustedScan;

    /// <summary>The git folder git itself agreed to open by its ownership check (<see cref="CheckAsync"/>).</summary>
    private string? _agreed;

    /// <summary>
    /// A session on <paramref name="root"/>, before its first check. <paramref name="trusts"/> says whether the
    /// user trusts the repository as this check found it, with exactly its mark (<see cref="TrustScan.Mark"/>) and in
    /// exactly its git folder (<see cref="TrustScan.Identity"/>, the seventh pass, T1; the window asks its settings);
    /// <paramref name="environment"/>: variables for every run (the tests' settings files).
    /// </summary>
    public GitSession(string root, Func<TrustScan, bool> trusts, IReadOnlyDictionary<string, string>? environment = null)
    {
        Root = root;
        _trusts = trusts;
        _environment = environment;
    }

    public string Root { get; }

    /// <summary>The last check; a problem in it until the first (<see cref="CheckAsync"/>).</summary>
    public TrustScan Scan { get; private set; } = new([], "not checked yet");

    /// <summary>Whether git may run in the repository: its files could be read, and the user trusts it for exactly this mark (G1: also when nothing was found).</summary>
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

    /// <summary>
    /// Whether a check shows what <paramref name="shown"/> showed (T1 of the seventh pass): the same mark, in the same
    /// git folder. Where no identity lasts (<see cref="FolderIdentity.Lasting"/>) the ones read are compared as they
    /// are, within this session alone; a git folder whose identity could be read once and not again is another.
    /// </summary>
    public static bool Shows(TrustScan scan, TrustScan? shown) =>
        shown is not null && scan.Known && shown.Known && scan.Mark == shown.Mark && Equals(scan.Identity, shown.Identity);

    private async Task<TrustScan> CheckAsync(TrustScan? trusting, CancellationToken cancellation)
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

        // G1 (the sixth pass): trusted only for a mark the user trusted, also when the scan found nothing; T1 (the
        // seventh): and in the git folder it was trusted in.
        var trusted = Scan.Known && (Shows(Scan, trusting) || Shows(Scan, _trustedScan) || _trusts(Scan));
        if (trusted && Scan.GitDir is { } gitDir && !string.Equals(_agreed, gitDir, StringComparison.OrdinalIgnoreCase))
        {
            var asked = await GitRunner.RunAsync(Root, ["rev-parse", "--absolute-git-dir"], GitRunner.Short, cancellation, environment: _environment, findRepository: true);
            if (!asked.Ok)
            {
                Scan = new TrustScan([], $"git does not open it: {asked.FirstLine}");
                return Scan;
            }

            // H6 (the sixth pass): the git folder git finds itself is the one the scan read, or nothing runs; L1 (the
            // seventh): the same folder by its identity, so that a root reached through a junction or a subst drive,
            // which git prints by its real path, opens.
            // The identity is read on a task: the folder may lie on a share (root CLAUDE.md rule 3).
            var read = Scan.Identity;
            if (OpenedFolder(asked.Output) is not { } opened || !await Task.Run(() => SameFolder(opened, gitDir, read), cancellation))
            {
                Scan = new TrustScan([], $"git opens another git folder, {asked.Output.Trim()}, than the one axGIT read, {gitDir}.");
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
    public async Task<bool> TrustAsync(TrustScan shown, CancellationToken cancellation = default)
    {
        await CheckAsync(shown, cancellation);
        if (Trusted)
        {
            _trustedScan = Scan;
        }

        return Trusted;
    }

    /// <summary>
    /// Whether the folder git printed is the git folder the scan read (L1 of the seventh pass): by their identities
    /// where both last (<see cref="FolderIdentity.Lasting"/>), the folder git printed read through
    /// <see cref="RepositoryFiles"/> like any other; by their full paths otherwise.
    /// </summary>
    private bool SameFolder(string opened, string gitDir, FolderIdentity? read)
    {
        if (read is { Lasting: true } && RepositoryFiles.Identity(opened, Root) is { Lasting: true } printed)
        {
            return printed == read;
        }

        return string.Equals(opened, Path.TrimEndingDirectorySeparator(Path.GetFullPath(gitDir)), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The git folder git printed for <c>rev-parse --absolute-git-dir</c> (one line), as a full path; null for anything else.</summary>
    private static string? OpenedFolder(string output)
    {
        var text = output.TrimEnd('\r', '\n');
        if (text.Length == 0 || text.Contains('\n'))
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
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
