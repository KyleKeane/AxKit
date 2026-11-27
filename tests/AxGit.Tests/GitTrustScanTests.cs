using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The reads that say what a repository would run and which servers a push reaches (docs/axGIT/SPEC.md AG-1.6,
/// AG-5.4; the review of 2026-11-27), against git itself, in repositories the test makes: the answers are git's,
/// and what is tested is that axGIT asks the right questions. Where git is not installed the tests pass without
/// running, as axPass's test of KeePassXC's tool does. In one collection with the tests that put a fake in git's
/// place, so that the two never run at once.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitTrustScanTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-scan");

    /// <summary>Whether git is where the runner finds it.</summary>
    private static bool GitThere => File.Exists(GitRunner.Program);

    public void Dispose() => _dir.Dispose();

    /// <summary>git itself in a folder, with the user's and the computer's settings out of it; fails the test when git does.</summary>
    private static string Git(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in new[] { "-c", "user.name=test", "-c", "user.email=test", "-c", "protocol.file.allow=always", "-c", "init.defaultBranch=main" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(folder, "no-such-file");
        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    /// <summary>A repository with one commit, in a folder of the test's.</summary>
    private string Repository(string name)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, name)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "one\n");
        Git(folder, "add", "a.txt");
        Git(folder, "commit", "-q", "-m", "one");
        return folder;
    }

    [Fact]
    public async Task A_plain_repository_runs_nothing()
    {
        if (!GitThere)
        {
            return;
        }

        var scan = await GitTrustScan.ReadAsync(Repository("plain"));
        Assert.Null(scan.Problem);
        Assert.Empty(scan.Risks);
    }

    [Fact]
    public async Task The_work_trees_own_settings_file_is_read()
    {
        if (!GitThere)
        {
            return;
        }

        var root = Repository("worktree");
        Git(root, "config", "extensions.worktreeConfig", "true");
        Git(root, "config", "--worktree", "core.sshCommand", "never-run");
        Git(root, "config", "gpg.ssh.defaultKeyCommand", "never-run");
        var scan = await GitTrustScan.ReadAsync(root);
        Assert.Null(scan.Problem);
        Assert.Equal(["core.sshcommand", "gpg.ssh.defaultkeycommand"], scan.Risks.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// A repository inside another, with no .gitmodules to say so: the entry of the index is enough for git to run
    /// itself there on a plain status, and so for the scan to read its settings and hooks.
    /// </summary>
    [Fact]
    public async Task What_a_repository_inside_would_run_is_found_whether_git_keeps_it_inside_or_under_modules()
    {
        if (!GitThere)
        {
            return;
        }

        var library = Repository("library");
        var root = Repository("top");
        // Embedded: its own .git folder, added as an entry of the index.
        Git(root, "clone", "-q", library, "vendor");
        Git(Path.Combine(root, "vendor"), "config", "core.fsmonitor", "never-run");
        File.WriteAllText(Path.Combine(root, "vendor", ".git", "hooks", "post-checkout"), "#!/bin/sh\n");
        Git(root, "add", "vendor");
        // A submodule git made: its repository under .git/modules, a .git file in its work tree.
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Git(Path.Combine(root, "libs", "zip"), "config", "filter.x.smudge", "never-run");
        Git(root, "commit", "-q", "-m", "two inside");

        var scan = await GitTrustScan.ReadAsync(root);
        Assert.Null(scan.Problem);
        Assert.Equal(
            ["submodule libs/zip: filter.x.smudge", "submodule vendor: core.fsmonitor", "submodule vendor: hook post-checkout"],
            scan.Risks.Order(StringComparer.Ordinal));
        Assert.Equal(["-c", "filter.x.smudge=", "-c", "core.fsmonitor=", "-c", "core.hooksPath=NUL"], GitTrust.Overrides(scan.Risks));

        // The submodule's work tree taken away, its repository stays under .git/modules, and is still read.
        Git(root, "submodule", "--quiet", "deinit", "-f", "libs/zip");
        var after = await GitTrustScan.ReadAsync(root);
        Assert.Contains("submodule libs/zip: filter.x.smudge", after.Risks);
    }

    [Fact]
    public async Task A_folder_that_is_no_repository_is_a_problem_and_never_nothing_to_run()
    {
        if (!GitThere)
        {
            return;
        }

        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, "not-one")).FullName;
        // Kept from finding a repository above the temporary folder, should there be one.
        File.WriteAllText(Path.Combine(folder, ".git"), "gitdir: nowhere\n");
        var scan = await GitTrustScan.ReadAsync(folder);
        Assert.NotNull(scan.Problem);
        Assert.Empty(scan.Risks);
    }

    /// <summary>
    /// The consent before a push is for where the push goes (bundle AX-11.5): the branch's pushRemote, every pushurl
    /// of that remote, and an address as git rewrites it. The addresses are of the reserved name "invalid"; nothing
    /// is reached.
    /// </summary>
    [Fact]
    public async Task The_servers_of_a_push_are_the_push_remotes_addresses_as_git_rewrites_them()
    {
        if (!GitThere)
        {
            return;
        }

        var root = Repository("remotes");
        Git(root, "remote", "add", "origin", "https://fetch.example.invalid/a/b.git");
        Git(root, "remote", "add", "backup", "https://old.example.invalid/a/b.git");
        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.merge", "refs/heads/main");

        async Task<IReadOnlyList<string>> Hosts(bool push)
        {
            var remotes = await GitRunner.RunAsync(root, GitRemote.RemotesArguments("main"), GitRunner.Short);
            Assert.True(remotes.Ok, remotes.AllText);
            var remote = push ? GitTexts.PushRemote("origin/main", "main", remotes.Output) : GitRemote.Remote(remotes.Output, push: false);
            var urls = await GitRunner.RunAsync(root, GitRemote.UrlArguments(remote, push), GitRunner.Short);
            Assert.True(urls.Ok, urls.AllText);
            return GitRemote.Hosts(urls.Output);
        }

        // Nothing else set: a push goes where a fetch comes from.
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: true));

        // A push address of the remote's own.
        Git(root, "remote", "set-url", "--push", "origin", "https://push.example.invalid/a/b.git");
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));
        Assert.Equal(["push.example.invalid"], await Hosts(push: true));

        // The branch pushed to another remote, which has two push addresses, one of them rewritten.
        Git(root, "config", "branch.main.pushRemote", "backup");
        Git(root, "remote", "set-url", "--add", "--push", "backup", "https://old.example.invalid/a/b.git");
        Git(root, "remote", "set-url", "--add", "--push", "backup", "https://second.example.invalid/a/b.git");
        Git(root, "config", "url.https://rewritten.example.invalid/.insteadOf", "https://old.example.invalid/");
        Assert.Equal(["rewritten.example.invalid", "second.example.invalid"], await Hosts(push: true));
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));

        // remote.pushDefault, where the branch names none.
        Git(root, "config", "--unset", "branch.main.pushRemote");
        Git(root, "config", "remote.pushDefault", "backup");
        Assert.Equal(["rewritten.example.invalid", "second.example.invalid"], await Hosts(push: true));
    }
}

/// <summary>The tests that run git through <see cref="GitRunner"/>, whose program is one setting for the whole process: one at a time.</summary>
public static class GitProgram
{
    public const string Collection = "the git program";
}
