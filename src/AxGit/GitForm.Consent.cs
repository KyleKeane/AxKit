using AxGit.Core;
using AxKit.Core;

namespace AxGit;

/// <summary>
/// Bundle AX-11.5: Git's consent before the first repository opens, and each server's before the first fetch, pull or
/// push that reaches it (the host of every address git would use; a remote on this computer or a share asks nothing).
/// </summary>
public sealed partial class GitForm
{
    /// <summary>The servers this window has said the sign-in line for (AX-11.6).</summary>
    private readonly HashSet<string> _reminded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the remote command once every server it reaches has the user's consent; asks first otherwise. Which
    /// servers those are is git's own answer (<see cref="GitRemote"/>; the review of 2026-11-27): for a push
    /// (<paramref name="push"/>) the remote git pushes the branch to and every address a push to it goes to, each as
    /// git rewrites it; for a fetch or pull the upstream's remote and the address a fetch uses. Until then the
    /// consent was for the fetch address of the upstream's remote, whatever the command.
    /// </summary>
    private async Task ToServerAsync(bool push, Func<Task> command)
    {
        if (_root is null || _gitMissing)
        {
            await command();
            return;
        }

        // The window's one way of running git (AG-1.5): git gone since the repository opened, or a command still
        // running, is handled there, not here (review, 2026-11-01).
        string? remotes = null;
        if (_repo.Branch is { } branch)
        {
            if (await RunGitAsync(GitRemote.RemotesArguments(branch), GitRunner.Short, "remote") is not { } asked)
            {
                return;
            }

            remotes = asked.Ok ? asked.Output : null;
        }

        var remote = push ? GitTexts.PushRemote(_repo.Upstream, _repo.Branch, remotes) : GitRemote.Remote(remotes, push: false);
        var urls = await RunGitAsync(GitRemote.UrlArguments(remote, push), GitRunner.Short, "remote");
        if (urls is null)
        {
            return;
        }

        // A remote git does not know has no address: the command runs, and git says so in its own words.
        foreach (var host in urls.Ok ? GitRemote.Hosts(urls.Output) : [])
        {
            // Connect comes through here again, so that the first connection gets the sign-in line below (review, 2026-11-02).
            if (!HasConsent(ConsentTarget.Server(host), () => Do(() => ToServerAsync(push, command)), () => Refuse($"Not connected to {host}")))
            {
                return;
            }

            // AX-11.6: git may ask for a sign-in to the server (its credential helper's own window); whose account that is,
            // said the first time this window reaches each server, not on every command. Not interrupting: the command's
            // own result, which is, would otherwise cut it short on a fast server.
            if (_reminded.Add(host))
            {
                Announce($"Connecting to {host}; if Git asks you to sign in, the account is between you and {host}", false);
            }
        }

        await command();
    }
}
