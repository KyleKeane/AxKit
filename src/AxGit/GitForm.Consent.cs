using AxGit.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>
/// Bundle AX-11.5: Git's consent before the first repository opens, and each server's before the first fetch, pull or
/// push that reaches it (the host of every address git would use, and the computer a share lies on; a remote on this
/// computer asks nothing).
/// </summary>
public sealed partial class GitForm
{
    /// <summary>The servers this window has said the sign-in line for (AX-11.6).</summary>
    private readonly HashSet<string> _reminded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs a fetch, pull or push once every server it reaches has the user's consent; asks first otherwise
    /// (AG-5.4a). What it reaches is read through git (<see cref="GitReach"/>): the remote, the hosts of its
    /// addresses as git rewrites them, a proxy the repository's own settings name. <paramref name="command"/> gets
    /// that remote and puts it on git's command line, so that the server asked about is the one reached (the
    /// review's second pass of 2026-11-27: git, run without a remote, chose for itself, and not always the one
    /// that had been asked about). When git cannot name the server nothing runs, and a notice says why;
    /// <paramref name="what"/> names the command in it ("Fetch").
    /// </summary>
    private async Task ToServerAsync(bool push, string what, Func<Reach, Task> command)
    {
        if (_root is not { } root || _session is not { } session)
        {
            Refuse("No repository");
            return;
        }

        // T4 (the review's fourth pass of 2026-11-28): the check before every fetch, pull and push, so that a hook
        // or an included file changed since the last one is asked about before anything is reached; a repository
        // that is not trusted is never connected (D1), since nothing runs in it at all (T1).
        if (!await CheckAgainAsync(root))
        {
            return;
        }

        // The window's one way of running git (AG-1.5): git gone since the repository opened, or a command still
        // running, is handled there, not here (review, 2026-11-01).
        var reach = await GitReach.ReadAsync(arguments => RunGitAsync(arguments, GitRunner.Short, "remote"), push, _repo.Branch, _repo.Upstream);
        if (reach is null)
        {
            return;
        }

        if (reach.Problem is { } problem)
        {
            Log.Info($"{what} not started: the server could not be named");
            ShowNotice(Notice.Plain($"{what} not started", $"{problem} Nothing was sent and nothing was fetched."));
            return;
        }

        // T2: a push goes to a server alone, never into a folder, a share or the repository itself.
        if (push && GitReach.PushRefused(reach) is { } folder)
        {
            Log.Info("Push not started: the remote is a folder");
            ShowNotice(Notice.Plain("Push not started", $"{folder} Nothing was sent."));
            return;
        }

        foreach (var host in reach.Hosts.Concat(reach.Proxies))
        {
            // Connect comes through here again, so that the first connection gets the sign-in line below (review, 2026-11-02).
            if (!HasConsent(ConsentTarget.Server(host), () => Do(() => ToServerAsync(push, what, command)), () => Refuse($"Not connected to {host}")))
            {
                return;
            }

            // AX-11.6: git may ask for a sign-in to the server (its credential helper's own window); whose account that is,
            // said the first time this window reaches each server, not on every command. Not interrupting: the command's
            // own result, which is, would otherwise cut it short on a fast server. A proxy of the repository's own
            // settings is where the bytes go first (AG-5.4b), and is said as that.
            if (_reminded.Add(host))
            {
                Announce(reach.Proxies.Contains(host)
                    ? $"Connecting through the proxy {host}, which this repository's settings name"
                    : $"Connecting to {reach.Place(host)}; if Git asks you to sign in, the account is between you and {host}", false);
            }
        }

        // D7: Git LFS's pre-push hook would send to a server of its own; the hook's text is read off the window's
        // thread, since the repository may lie on a share (rule 3).
        if (push && await Task.Run(session.LfsPushRefused) is { } lfs)
        {
            Log.Info("Push not started: the pre-push hook is Git LFS's");
            ShowNotice(Notice.Plain("Push not started", $"{lfs} Nothing was sent."));
            return;
        }

        await command(reach);
    }
}
