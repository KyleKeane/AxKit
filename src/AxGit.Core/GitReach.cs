namespace AxGit.Core;

/// <summary>
/// What a fetch, pull or push would reach (SPEC.md AG-5.4a): the remote to put on its command line, the hosts of
/// that remote's addresses, each of which needs the user's consent first, and the proxies the repository's own
/// settings send the connection through, which need it too and are said. With a <paramref name="Problem"/>, a
/// sentence for the user, nothing is known for certain and the command must not run.
/// </summary>
public sealed record Reach(string Remote, IReadOnlyList<string> Hosts, IReadOnlyList<string> Proxies, string? Problem);

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
        var remote = push ? GitTexts.PushRemote(upstream, branch!, settings) : GitRemote.FetchRemote(settings, branch);
        if (remote == GitRemote.Here)
        {
            // A branch that follows another branch of this repository: no server.
            return new Reach(remote, [], [], null);
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
        var lines = urls.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var address = GitRemote.Read(line);
            if (address.Kind == AddressKind.NotRead)
            {
                return Stopped($"The remote {remote} has an address axGIT cannot read a server from: {GitRemote.WithoutSignIn(line)}");
            }

            if (address.Host is { } host && !hosts.Contains(host))
            {
                hosts.Add(host);
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
            : new Reach(remote, hosts, proxies, null);
    }

    /// <summary>Nothing may run, for the reason given; the sentence goes to the user and the log, so without the sign-in of any address in it (a remote's name can be an address).</summary>
    private static Reach Stopped(string problem) => new(string.Empty, [], [], GitRemote.WithoutSignIn(problem));
}
