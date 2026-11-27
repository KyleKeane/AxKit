namespace AxGit.Core;

/// <summary>
/// One repository open in axGIT and what git is run with in it (SPEC.md AG-1.6, AG-1.6b, AG-5.4; the review's third
/// pass of 2026-11-27): the last check of what it would run (<see cref="Scan"/>), whether it is trusted, the
/// settings set aside while it is not (<see cref="Overrides"/>), and the three rules that hold whatever the
/// window does:
/// <list type="bullet">
/// <item>Every run of git in the repository goes through <see cref="RunAsync"/>, with what is set aside now.</item>
/// <item>A command that changes the repository goes through <see cref="ChangeAsync"/>, which checks again after
/// it, whether it succeeded or failed, before anything else can run (D8): no command runs on what was set aside
/// before the command ahead of it. Until then the check came with the lists read after a command that
/// succeeded, and a failed command (a pull that stopped halfway, a switch that wrote some files) left the old
/// overrides in place.</item>
/// <item>A repository that is not trusted is never connected (D1, <see cref="NotConnected"/>): while a risk is
/// kept off, fetch, pull and push do not start, since a transport's settings are where setting aside by name
/// kept failing (a name whose first value wins, a transport that clears the environment).</item>
/// </list>
/// Plain and tested against git itself (<c>GitSessionTests</c>); the window holds one and asks it.
/// </summary>
public sealed class GitSession
{
    private readonly Func<IReadOnlyList<string>, bool> _trusts;
    private readonly IReadOnlyDictionary<string, string>? _environment;

    /// <summary>
    /// A session on <paramref name="root"/>, before its first check. <paramref name="trusts"/> says whether the
    /// user trusts the repository with exactly these risks (the window binds an older trust to them and saves it);
    /// <paramref name="environment"/>: variables for every run (the tests' settings files).
    /// </summary>
    public GitSession(string root, Func<IReadOnlyList<string>, bool> trusts, IReadOnlyDictionary<string, string>? environment = null)
    {
        Root = root;
        _trusts = trusts;
        _environment = environment;
    }

    public string Root { get; }

    /// <summary>The last check; a problem in it until the first (<see cref="CheckAsync"/>).</summary>
    public TrustScan Scan { get; private set; } = new([], [], "not checked yet");

    /// <summary>Whether the repository runs as it is: it holds no risk, or the user trusts it for exactly these.</summary>
    public bool Trusted { get; private set; }

    /// <summary>What git is run with while the repository is not trusted (<see cref="GitTrust.Overrides"/>); empty while it is.</summary>
    public IReadOnlyList<GitSetting> Overrides { get; private set; } = [];

    /// <summary>Whether nothing may run any more: the last check could not be made, or the repository points at another computer.</summary>
    public bool Closed => !Scan.Known;

    /// <summary>How many checks were made, for the tests of D8.</summary>
    public int Checks { get; private set; }

    /// <summary>The check: what the repository would run read again (<see cref="GitTrustScan"/>), and what git is run with worked out from it.</summary>
    public async Task<TrustScan> CheckAsync(CancellationToken cancellation = default)
    {
        Take(await GitTrustScan.ReadAsync(Root, cancellation, _environment));
        return Scan;
    }

    /// <summary>What git is run with from a scan: nothing set aside for a repository that holds no risk or that the user trusts for these, <see cref="GitTrust.Overrides"/> otherwise.</summary>
    private void Take(TrustScan scan)
    {
        Checks++;
        Scan = scan;
        Trusted = scan.Known && (scan.Risks.Count == 0 || _trusts(scan.Risks));
        Overrides = Trusted || !scan.Known ? [] : GitTrust.Overrides(scan.Risks, scan.Filters);
    }

    /// <summary>The user trusted the repository for <paramref name="risks"/>: nothing set aside from now on, when they are what the last check found (a notice answered after another check is for the risks it named).</summary>
    public void TrustNow(IReadOnlyList<string> risks)
    {
        if (Scan.Known && GitTrust.Mark(Scan.Risks) == GitTrust.Mark(risks))
        {
            Trusted = true;
            Overrides = [];
        }
    }

    /// <summary>One git command in the repository, with what is set aside now. Refused while <see cref="Closed"/>: then nothing runs, and the result says why.</summary>
    public async Task<GitResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        if (Closed)
        {
            return new GitResult(-1, string.Empty, "axGIT runs nothing in this repository: what it would run is not known.");
        }

        return await GitRunner.RunAsync(Root, arguments, timeout, cancellation, Overrides, _environment);
    }

    /// <summary>
    /// A command that changes the repository: run with what is set aside now, then the check again, always, also
    /// when git failed or could not start (D8). The result is git's; <see cref="Scan"/> is the check after it.
    /// </summary>
    public async Task<GitResult> ChangeAsync(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        try
        {
            return await RunAsync(arguments, timeout, cancellation);
        }
        finally
        {
            await CheckAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// A git command in another folder: the reads of a remote's own repository before a push
    /// (<see cref="GitReach.FolderProblemAsync"/>), with the empty hooks folder, but the question for the hooks
    /// folder, which would be told that one (as in the scan).
    /// </summary>
    public Task<GitResult> RunInAsync(string folder, IReadOnlyList<string> arguments) =>
        GitRunner.RunAsync(folder, arguments, GitRunner.Short,
            overrides: ReferenceEquals(arguments, GitTrust.FoldersArguments) ? null : [new GitSetting("core.hooksPath", GitTrust.EmptyHooksFolder())],
            environment: _environment);

    /// <summary>
    /// D1: why a fetch, pull or push (<paramref name="what"/>) does not start; null when the repository is trusted
    /// or holds no risk. Reading, staging, committing and switching go on with what is set aside.
    /// </summary>
    public string? NotConnected(string what) =>
        Trusted ? null : $"{what} not started: this repository has settings you have not trusted.";

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
