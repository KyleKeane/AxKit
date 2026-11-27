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
    private async Task ToServerAsync(bool push, string what, Func<string, Task> command)
    {
        if (_root is not { } root || _session is not { } session)
        {
            Refuse("No repository");
            return;
        }

        // D1 (the review's third pass of 2026-11-27): a repository that is not trusted is never connected.
        if (session.NotConnected(what) is { } notConnected)
        {
            Log.Info($"{what} not started: the repository is not trusted");
            var risks = session.Scan.Risks;
            ShowNotice(new Notice($"{what} not started",
                $"{notConnected} Fetch, Pull and Push start once you trust it: Show its settings names them, with Trust this repository. Nothing was sent and nothing was fetched.",
            [
                new OverlayChoice("&Show its settings", () => AskTrust(root, risks), IsDefault: true),
                new OverlayChoice("&Close", IsCancel: true),
            ]));
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

        if (push && await PushProblemAsync(root, session, reach) is { } stopped)
        {
            Log.Info("Push not started: the place it goes runs programs of its own");
            ShowNotice(Notice.Plain("Push not started", $"{stopped} Nothing was sent."));
            return;
        }

        await command(reach.Remote);
    }

    /// <summary>
    /// What stops a push after its servers are agreed to: Git LFS's pre-push hook, which sends to a server of its
    /// own (D7, <see cref="GitSession.LfsPushRefused"/>), and a remote that is a folder or a share whose repository
    /// has programs or settings of its own (D3, <see cref="GitReach.FolderProblemAsync"/>), read as one piece of
    /// work; null when nothing does, and when another command runs (said by the work's refusal).
    /// </summary>
    private async Task<string?> PushProblemAsync(string root, GitSession session, Reach reach)
    {
        // The hook's own text, read off the window's thread: the repository may lie on a share (rule 3).
        if (await Task.Run(session.LfsPushRefused) is { } lfs)
        {
            return lfs;
        }

        if (reach.Folders is not { Count: > 0 } folders)
        {
            return null;
        }

        if (!BeginWork("Working: reading the remote's repository…"))
        {
            return "Another command is still running.";
        }

        SetGitItems(false);
        UpdateStatusBar();
        try
        {
            return await GitReach.FolderProblemAsync(session.RunInAsync, root, folders);
        }
        catch (GitNotFoundException)
        {
            return "git was not found.";
        }
        finally
        {
            EndWork();
            if (!IsDisposed)
            {
                SetGitItems(true);
                UpdateStatusBar();
            }
        }
    }
}
