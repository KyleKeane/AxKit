namespace AxGit.Core;

/// <summary>
/// What a fetch, pull or push would reach (SPEC.md AG-5.4a): the remote to put on its command line, the hosts of
/// that remote's addresses, each of which needs the user's consent first, and the proxies the repository's own
/// settings send the connection through, which need it too and are said. With a <paramref name="Problem"/>, a
/// sentence for the user, nothing is known for certain and the command must not run.
/// <paramref name="Folders"/>: the addresses git reaches as a folder, with its own transport (a path on this
/// computer, a share, and "." for the repository itself), which a push reads first (AG-5.4c);
/// <paramref name="Ports"/>: the port an address gives for a host, said in the reminder.
/// </summary>
public sealed record Reach(string Remote, IReadOnlyList<string> Hosts, IReadOnlyList<string> Proxies, string? Problem, IReadOnlyList<string>? Folders = null, IReadOnlyDictionary<string, int>? Ports = null)
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
        if (remote == GitRemote.Here)
        {
            // A branch that follows another branch of this repository: no server; a push goes into the repository itself, which is read first.
            return new Reach(remote, [], [], null, [GitRemote.Here]);
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
            : new Reach(remote, hosts, proxies, null, folders, ports);
    }

    // ---- A remote that is a folder: a second repository (AG-5.4c; the review's third pass of 2026-11-27, D3) ----

    /// <summary>
    /// Why a push into the repositories of <paramref name="folders"/> (<see cref="Reach.Folders"/>) does not start;
    /// null when none of them has a program or a setting of its own. A push into a folder or a share runs the
    /// other repository's receive hooks with git's own transport, which clears what axGIT gives through the
    /// environment (tried with git 2.53: a push to "." with only harmless settings, and to a bare repository in
    /// the work tree with no risk found at all, ran every receive hook). So each is read as a repository inside
    /// is read: the D9 guard, its settings, the hooks folder git names for it and the values that decide
    /// (<see cref="GitTrust.Risks"/>); anything found, or a repository that cannot be read, and nothing is pushed.
    /// No trust is asked for it. A fetch or pull from a folder runs nothing of the other repository's (tried: its
    /// hooks, uploadpack.packObjectsHook, core.fsmonitor and core.alternateRefsCommand, with the upload program
    /// named on the command line) and stays allowed. <paramref name="runIn"/> runs git in a folder with nothing
    /// set aside; <paramref name="root"/> is the repository's root, which relative paths start from.
    /// </summary>
    public static async Task<string?> FolderProblemAsync(Func<string, IReadOnlyList<string>, Task<GitResult>> runIn, string root, IReadOnlyList<string> folders)
    {
        foreach (var address in folders)
        {
            var folder = address == GitRemote.Here ? root : Path.GetFullPath(address.Replace('/', Path.DirectorySeparatorChar), root);
            if (GitTrust.PointsElsewhere(folder) is { } away)
            {
                return $"The repository this remote names, {address}, points at files on another computer, {away}.";
            }

            var config = await runIn(folder, GitTrust.ConfigArguments);
            var places = await runIn(folder, GitTrust.FoldersArguments);
            var values = await runIn(folder, GitTrust.ValueArguments);
            if (!config.Ok || !places.Ok || !(values.Ok || values is { ExitCode: 1, Output.Length: 0 }))
            {
                return $"The repository this remote names, {address}, could not be read, so what it would run is not known. Git said: {(config.Ok ? places.Ok ? values.FirstLine : places.FirstLine : config.FirstLine)}";
            }

            var risks = GitTrust.Risks(config.Output, GitTrust.Folders(places.Output, folder).Hooks, inside: true).Concat(GitTrust.ValueRisks(values.Ok ? values.Output : string.Empty)).ToList();
            if (risks.Count > 0)
            {
                return $"The repository this remote names, {address}, has programs or settings of its own: {string.Join(", ", risks)}. A push runs them there; push in a terminal if you mean to.";
            }
        }

        return null;
    }

    /// <summary>Nothing may run, for the reason given; the sentence goes to the user and the log, so without the sign-in of any address in it (a remote's name can be an address).</summary>
    private static Reach Stopped(string problem) => new(string.Empty, [], [], GitRemote.WithoutSignIn(problem));
}
