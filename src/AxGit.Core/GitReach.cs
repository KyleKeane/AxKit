namespace AxGit.Core;

/// <summary>
/// What a fetch, pull or push would reach (SPEC.md AG-5.4a): the remote to put on its command line, the hosts of
/// that remote's addresses, each of which needs the user's consent first, and the proxies the repository's own
/// settings send the connection through, which need it too and are said. With a <paramref name="Problem"/>, a
/// sentence for the user, nothing is known for certain and the command must not run.
/// <paramref name="Folders"/>: the addresses git reaches as a folder, with its own transport (a path on this
/// computer, a share, and "." for the repository itself), to which axGIT never pushes (AG-5.4c);
/// <paramref name="Ports"/>: the port an address gives for a host, said in the reminder; <paramref name="Merge"/>:
/// for a fetch or pull, the branch of the remote the current branch follows (<see cref="GitTexts.MergeOf"/>), which
/// a pull names; <paramref name="Addresses"/>: the addresses as git said them, each of which the command runs with
/// redirects off (<see cref="GitReach.NoRedirects"/>).
/// </summary>
public sealed record Reach(string Remote, IReadOnlyList<string> Hosts, IReadOnlyList<string> Proxies, string? Problem, IReadOnlyList<string>? Folders = null, IReadOnlyDictionary<string, int>? Ports = null, string? Merge = null, IReadOnlyList<string>? Addresses = null)
{
    /// <summary>The words for a host in the reminder: "host", or "host, port 2222" when the address gives a port.</summary>
    public string Place(string host) => GitRemote.Place(host, Ports is not null && Ports.TryGetValue(host, out var port) ? port : null);
}

/// <summary>
/// The questions asked of git before a fetch, pull or push, in their order (SPEC.md AG-5.4a, AG-5.4b; the review's
/// second pass of 2026-11-27): its settings, from which the remote is read by git's own order
/// (<see cref="GitRemote.FetchRemote"/>, <see cref="GitTexts.PushRemote"/>); that remote's addresses as git
/// rewrites them; and, where the settings name a proxy at all, the proxy git would use for each address. The
/// window gives the way to run git (its one way, which shows the work and logs it) and runs the command with
/// <see cref="Reach.Remote"/> on the command line, so that the server asked about is the one reached; the tests
/// give git itself, in repositories with addresses of the reserved name "invalid".
///
/// Nothing runs when git cannot name the server: a remote git does not know (also a name that is an address, or
/// one like an option), an address no server can be read from (<see cref="AddressKind.NotRead"/>), a remote's
/// name that is a group of remotes too, a push without a branch. Until then such a command ran with nobody
/// asked. None of the questions reaches a server or runs a program the repository names.
/// </summary>
public static class GitReach
{
    /// <summary>
    /// What the command would reach, asked through <paramref name="run"/>, which gives null when git did not run
    /// (and has said why): then null here too. <paramref name="branch"/> and <paramref name="upstream"/> are the
    /// status's: the branch HEAD is on, null when it is on none, and what that branch follows.
    /// </summary>
    public static async Task<Reach?> ReadAsync(Func<IReadOnlyList<string>, Task<GitResult?>> run, bool push, string? branch, string? upstream)
    {
        if (push && branch is null)
        {
            return Stopped("HEAD is on no branch (detached), and axGIT pushes a branch. Switch to a branch first.");
        }

        if (await run(GitRemote.SettingsArguments) is not { } listed)
        {
            return null;
        }

        if (!listed.Ok || listed.OutputHeldOpen)
        {
            return Stopped($"Git could not list the repository's settings. Git said: {listed.FirstLine}");
        }

        var settings = GitRemote.Settings(listed.Output);
        if (push && upstream is not null && GitTexts.FollowsAnotherName(settings, branch!) is { } another)
        {
            // D6: the push names its branch on both sides, and a branch that follows another name is not pushed.
            return Stopped(another);
        }

        var remote = push ? GitTexts.PushRemote(upstream, branch!, settings) : GitRemote.FetchRemote(settings, branch);
        var merge = push || branch is null ? null : GitTexts.MergeOf(settings, branch);
        if (remote == GitRemote.Here)
        {
            // A branch that follows another branch of this repository: no server; a push into the repository itself is not made (T2).
            return new Reach(remote, [], [], null, [GitRemote.Here], Merge: merge);
        }

        if (GitRemote.IsGroup(settings, remote))
        {
            return Stopped($"{remote} is the name of a remote and of a group of remotes, and git would reach every remote of the group. axGIT reaches one remote; use a terminal for the group.");
        }

        if (await run(GitRemote.UrlArguments(remote, push)) is not { } urls)
        {
            return null;
        }

        if (!urls.Ok || urls.OutputHeldOpen)
        {
            return Stopped($"Git could not say where the remote {remote} leads. Git said: {urls.FirstLine}");
        }

        var hosts = new List<string>();
        var proxies = new List<string>();
        var folders = new List<string>();
        var ports = new Dictionary<string, int>(StringComparer.Ordinal);
        var lines = urls.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var address = GitRemote.Read(line);
            if (address.Kind == AddressKind.NotRead)
            {
                var punycode = line.Any(letter => letter > '\u007f') ? " A host with letters outside ASCII is read in its punycode form alone, the one that starts with xn--." : string.Empty;
                return Stopped($"The remote {remote} has an address axGIT does not read: {GitRemote.WithoutSignIn(line)}. axGIT reads an address of https, http, ssh or git, the form user@host:path, a share, or a path on this computer.{punycode}");
            }

            if (address.Host is { } host && !hosts.Contains(host))
            {
                hosts.Add(host);
            }

            if (address is { Host: { } portHost, Port: { } port })
            {
                ports.TryAdd(portHost, port);
            }

            if (address.Folder is { } folder && !folders.Contains(folder))
            {
                folders.Add(folder);
            }

            if (!GitRemote.UsesHttp(line) || !settings.Any(setting => GitTrust.NamesAProxy(setting.Name) || setting.Name == $"remote.{remote}.proxy"))
            {
                continue;
            }

            // git itself says which proxy fits this address; it gives exit code 1 and no line when none does.
            if (await run(GitRemote.ProxyArguments(line)) is not { } asked)
            {
                return null;
            }

            if (GitRemote.RepositoryProxy(settings, remote, asked.Ok ? asked.Output : null) is not { } proxy)
            {
                continue;
            }

            if (proxy.Host is not { } through)
            {
                return Stopped($"The repository's settings name a proxy for {remote} whose server axGIT cannot read.");
            }

            if (!proxies.Contains(through))
            {
                proxies.Add(through);
            }
        }

        return lines.Length == 0
            ? Stopped($"Git named no address for the remote {remote}.")
            : new Reach(remote, hosts, proxies, null, folders, ports, merge, lines);
    }

    // ---- A remote that is a folder (AG-5.4c; the review's third and fourth passes, D3, H2, M4) ----

    /// <summary>
    /// Why a push does not start: the remote is a folder (<see cref="Reach.Folders"/>: a path on this computer, a
    /// share, the repository itself); null for a push to servers alone. A push into a folder or a share runs the
    /// other repository's receive hooks with git's own transport, which clears what axGIT gives through the
    /// environment, and which repository it enters is git's to decide: with the address ./vendor/mirror it
    /// entered vendor/mirror.git, while the reads before it, made with git -C there, read the repository above
    /// (H2, tried with git 2.53); a share can change between a read and the push (M4). So axGIT pushes only to a
    /// server (T2), and a folder is pushed to in a terminal. A fetch or pull from a folder runs nothing of the
    /// other repository's (tried: its hooks, uploadpack.packObjectsHook, core.fsmonitor and
    /// core.alternateRefsCommand, with the upload program named on the command line) and stays allowed.
    /// </summary>
    public static string? PushRefused(Reach reach) =>
        reach.Folders is { Count: > 0 } ? "This remote is a folder; push to it in a terminal." : null;

    /// <summary>
    /// L1 (the review's fifth pass): what a fetch, pull or push is run with so that it follows no redirect from any
    /// address it reaches, <c>http.&lt;address&gt;.followRedirects=false</c> for each http and https address of
    /// <see cref="Reach.Addresses"/>. git takes the setting of the most particular address that fits (git-config,
    /// http.&lt;url&gt;.*: the host, then the longest path, then the user), so the general
    /// <c>http.followRedirects=false</c> of every run lost to a repository's setting for its own address; this one
    /// is the whole address, with its user and without a password, given as the command's own setting, which git
    /// reads last. Given through the environment (<see cref="GitRunner.SetOverrides"/>), where an address with an
    /// equals sign in it stays whole.
    /// </summary>
    public static IReadOnlyList<GitSetting> NoRedirects(Reach reach) =>
        [.. (reach.Addresses ?? []).Where(GitRemote.UsesHttp).Select(address => new GitSetting($"http.{WithoutPassword(address.Trim())}.followRedirects", "false")).Distinct()];

    /// <summary>An address with the password of its sign-in taken out and its user kept: "https://user@host/a.git".</summary>
    private static string WithoutPassword(string address)
    {
        var scheme = address.IndexOf("://", StringComparison.Ordinal) + 3;
        var slash = address.IndexOf('/', scheme);
        var authority = slash < 0 ? address[scheme..] : address[scheme..slash];
        var at = authority.LastIndexOf('@');
        var colon = at < 0 ? -1 : authority.IndexOf(':', 0, at);
        if (colon < 0)
        {
            return address;
        }

        return address[..scheme] + authority[..colon] + authority[at..] + (slash < 0 ? string.Empty : address[slash..]);
    }

    /// <summary>Nothing may run, for the reason given; the sentence goes to the user and the log, so without the sign-in of any address in it (a remote's name can be an address).</summary>
    private static Reach Stopped(string problem) => new(string.Empty, [], [], GitRemote.WithoutSignIn(problem));
}
