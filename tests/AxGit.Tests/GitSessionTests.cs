using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The review's third pass of 2026-11-27 (docs/axGIT/SPEC.md AG-1.6, AG-1.6b, AG-5.4, AG-5.4c), against git itself,
/// in repositories the test makes: a repository's program is a script that writes a line into a file of the
/// test's, and each test runs what axGIT runs and looks whether the line is there. Each also runs once the way
/// that did run the script, so that it is known to be able to fail. Nothing leaves this computer: the addresses
/// are folders of the test's or under "invalid", and every connection goes through a proxy on a closed port
/// (<see cref="GitTests.Environment"/>). Said as skipped where git is not installed.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitSessionTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-session");
    private readonly Dictionary<string, string> _env;

    public GitSessionTests() => _env = GitTests.Environment(_dir);

    public void Dispose() => _dir.Dispose();

    // ---- git as a console runs it, and the marks ----

    private string Slashes => _dir.Path.Replace('\\', '/');

    private ProcessResult Plain(string folder, IReadOnlyDictionary<string, string>? more, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, _env);
        if (more is not null)
        {
            GitTests.Put(start, more);
        }

        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
    }

    private string Git(string folder, params string[] arguments)
    {
        var result = Plain(folder, null, arguments);
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    /// <summary>The file a test's marks go into, one per repository, so that runs side by side keep theirs apart.</summary>
    private string MarksOf(string root) => Path.Combine(_dir.Path, Path.GetFileName(root) + ".marks");

    /// <summary>A command that writes <paramref name="line"/> into <paramref name="marks"/> and passes its input on, as a filter must.</summary>
    private string Marker(string marks, string line)
    {
        var script = _dir.File("marker.sh", "#!/bin/sh\necho \"$1\" >> \"$2\"\ncat\n");
        return $"sh \"{script.Replace('\\', '/')}\" {line} \"{marks.Replace('\\', '/')}\"";
    }

    /// <summary>A program file that writes <paramref name="line"/> into <paramref name="marks"/>: a hook.</summary>
    private static void MarkerFile(string path, string marks, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"#!/bin/sh\necho {line} >> \"{marks.Replace('\\', '/')}\"\nexit 0\n");
    }

    /// <summary>The lines left in a marks file since the last look, each once.</summary>
    private static string[] Ran(string marks)
    {
        if (!File.Exists(marks))
        {
            return [];
        }

        var lines = File.ReadAllLines(marks).Distinct().ToArray();
        File.Delete(marks);
        return lines;
    }

    /// <summary>A repository with two commits, a.txt and b.txt, every file through the filter, diff and merge driver x.</summary>
    private string Origin(string name)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, name)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, ".gitattributes"), "*.txt filter=x diff=x merge=x\n");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "one\n");
        File.WriteAllText(Path.Combine(folder, "b.txt"), "two\n");
        Git(folder, "add", ".");
        Git(folder, "commit", "-q", "-m", "one");
        File.AppendAllText(Path.Combine(folder, "a.txt"), "more\n");
        Git(folder, "commit", "-q", "-a", "-m", "two");
        return folder;
    }

    private GitSession Untrusted(string root) => new(root, _ => false, _env);

    private GitSession Trusted(string root) => new(root, _ => true, _env);

    // ---- H1, the class: every program name, every command, untrusted ----

    /// <summary>
    /// What the window does with a repository it was asked to open, every command of it in its order, with what
    /// the session sets aside (or, trusted, nothing): the check, the three lists, a file's diff, a commit, stage,
    /// unstage, stage all, unstage all, a commit of everything, a new branch and a switch back, a discard; and
    /// the three that reach a remote, which do not start while the repository is not trusted (D1).
    /// </summary>
    private async Task EveryCommandAsync(GitSession session)
    {
        var scan = await session.CheckAsync();
        Assert.True(scan.Known, scan.Problem ?? scan.Elsewhere);
        File.AppendAllText(Path.Combine(session.Root, "a.txt"), "changed\n");
        var status = GitStatus.Parse((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Output);
        var log = GitLog.Parse((await session.RunAsync(GitLog.Arguments, GitRunner.Short)).Output);
        await session.RunAsync(GitBranches.Arguments, GitRunner.Short);
        if (status.Files.FirstOrDefault() is { } file)
        {
            await session.RunAsync(file.DiffArguments, GitRunner.Short);
        }

        if (log.FirstOrDefault() is { } commit)
        {
            await session.RunAsync(commit.ShowArguments, GitRunner.Short);
        }

        await session.RunAsync(GitTexts.StageArguments(["a.txt"]), GitRunner.Short);
        await session.RunAsync(GitTexts.UnstageArguments(["a.txt"]), GitRunner.Short);
        await session.RunAsync(GitTexts.StageAllArguments, GitRunner.Short);
        await session.RunAsync(GitTexts.UnstageAllArguments, GitRunner.Short);
        await session.RunAsync(GitTexts.StageAllArguments, GitRunner.Short);
        await session.RunAsync(GitTexts.CommitArguments("three"), GitRunner.Short);
        await session.RunAsync(GitBranches.CreateArguments("topic"), GitRunner.Short);
        var branches = GitBranches.Parse((await session.RunAsync(GitBranches.Arguments, GitRunner.Short)).Output);
        if (branches.FirstOrDefault(branch => !branch.Current) is { } other)
        {
            await session.RunAsync(other.SwitchArguments, GitRunner.Short);
        }

        File.AppendAllText(Path.Combine(session.Root, "b.txt"), "changed\n");
        await session.RunAsync(GitTexts.DiscardArguments(["b.txt"]), GitRunner.Short);
        foreach (var what in new[] { "Fetch", "Pull", "Push" })
        {
            if (session.NotConnected(what) is null)
            {
                await session.RunAsync(what == "Push" ? GitTexts.PushArguments(GitRemote.Default, null, "main") : what == "Fetch" ? GitTexts.FetchArguments(GitRemote.Default) : GitTexts.PullArguments(GitRemote.Default), GitRunner.Short);
            }
        }
    }

    /// <summary>The settings a program name needs beside it to be reached at all: signing for the signing programs, an event for a hook of the settings.</summary>
    private static IEnumerable<(string Name, string Value)> Companions(string name) => name.ToLowerInvariant() switch
    {
        "gpg.program" or "gpg.openpgp.program" => [("commit.gpgSign", "true")],
        "gpg.ssh.program" => [("commit.gpgSign", "true"), ("gpg.format", "ssh"), ("user.signingKey", "key::ssh-ed25519 AAAA")],
        "gpg.ssh.defaultkeycommand" => [("commit.gpgSign", "true"), ("gpg.format", "ssh")],
        "gpg.x509.program" => [("commit.gpgSign", "true"), ("gpg.format", "x509")],
        "hook.x.command" => [("hook.x.event", "pre-commit")],
        _ => [],
    };

    /// <summary>
    /// The test that closes H1's class (the review's third pass): every name of every kind of
    /// <see cref="GitTrust.ProgramNamesTable"/>, each in a clone of its own with the remote origin, set to a program
    /// that leaves a mark (core.hooksPath to a folder of such hooks), and every command axGIT has run with what the
    /// session sets aside: no mark comes. Until then the override list was asserted, never its effect, and
    /// remote.origin.uploadpack, whose first value git takes, was set aside by a second, empty one, which did
    /// nothing. The half that shows the test can fail: three of the names, with the repository trusted, leave
    /// their marks (a diff driver's text conversion would not: axGIT's own diff asks for none, AG-3.3).
    /// </summary>
    [GitFact]
    public async Task No_program_a_repository_names_runs_in_any_command_while_it_is_not_trusted()
    {
        var origin = Origin("origin");
        var names = GitTrust.ProgramNamesTable.SelectMany(kind => kind.Examples).Append("hooks").ToList();
        var marked = new List<string>();

        async Task<string[]> OneAsync(string name, int number, bool trusted)
        {
            var root = Path.Combine(_dir.Path, $"{(trusted ? "t" : "n")}{number}");
            Git(_dir.Path, "clone", "-q", origin, root);
            var marks = MarksOf(root);
            if (name == "hooks")
            {
                foreach (var hook in new[] { "pre-commit", "post-commit", "post-checkout", "post-index-change", "reference-transaction", "prepare-commit-msg", "commit-msg", "post-rewrite" })
                {
                    MarkerFile(Path.Combine(root, ".git", "hooks", hook), marks, $"hooks-{hook}");
                }
            }
            else if (name.Equals("core.hooksPath", StringComparison.OrdinalIgnoreCase))
            {
                var folder = Path.Combine(_dir.Path, $"hooks{(trusted ? "t" : "n")}{number}");
                foreach (var hook in new[] { "pre-commit", "post-commit", "post-checkout", "post-index-change", "reference-transaction" })
                {
                    MarkerFile(Path.Combine(folder, hook), marks, $"n{number}");
                }

                Git(root, "config", name, folder.Replace('\\', '/'));
            }
            else
            {
                Git(root, "config", name, Marker(marks, $"n{number}"));
            }

            foreach (var (companion, value) in Companions(name))
            {
                Git(root, "config", companion, value);
            }

            var session = trusted ? Trusted(root) : Untrusted(root);
            await EveryCommandAsync(session);
            Assert.Equal(!trusted, !session.Trusted);
            return Ran(marks);
        }

        // Four at a time, each in its own repository with its own marks.
        foreach (var some in names.Select((name, number) => (name, number)).Chunk(4))
        {
            var found = await Task.WhenAll(some.Select(one => OneAsync(one.name, one.number, trusted: false)));
            for (var at = 0; at < some.Length; at++)
            {
                if (found[at].Length > 0)
                {
                    marked.Add($"{some[at].name}: {string.Join(", ", found[at])}");
                }
            }
        }

        Assert.True(marked.Count == 0, "Programs of an untrusted repository ran: " + string.Join("; ", marked));

        // The way it ran: trusted, the same commands leave the marks of a filter's two sides and of the hooks.
        var trustedMarks = await Task.WhenAll(new[] { "filter.x.clean", "filter.x.smudge", "hooks" }.Select(name => OneAsync(name, names.IndexOf(name), trusted: true)));
        Assert.All(trustedMarks, found => Assert.NotEmpty(found));
    }

    /// <summary>
    /// H1, D2: a partial clone fetched a missing object by itself on its first status, running the remote's
    /// upload program before the notice was answered, whatever was set aside: git takes the first
    /// remote.origin.uploadpack, the repository's. Every run now forbids the lazy fetch; the status fails with
    /// git's own words, and nothing runs.
    /// </summary>
    [GitFact]
    public async Task A_partial_clone_fetches_nothing_by_itself()
    {
        var origin = Origin("origin");
        var root = Path.Combine(_dir.Path, "partial");
        Git(_dir.Path, "clone", "-q", "--filter=tree:0", "--no-checkout", "file://" + origin.Replace('\\', '/'), root);
        var marks = MarksOf(root);
        var program = _dir.File("upload.sh", $"#!/bin/sh\necho upload-pack >> \"{marks.Replace('\\', '/')}\"\nexec git-upload-pack \"$@\"\n");
        Git(root, "config", "remote.origin.uploadpack", $"sh \"{program.Replace('\\', '/')}\"");

        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.Contains(new GitSetting("remote.origin.uploadpack", string.Empty), session.Overrides);
        var status = await session.RunAsync(GitStatus.Arguments, GitRunner.Short);
        await session.RunAsync(GitLog.Arguments, GitRunner.Short);
        Assert.False(status.Ok);
        Assert.Empty(Ran(marks));

        // The way it ran: the same status with the old overrides in the environment and the lazy fetch allowed.
        var old = new Dictionary<string, string?>();
        GitRunner.SetOverrides(old, session.Overrides);
        Plain(root, old.ToDictionary(pair => pair.Key, pair => pair.Value ?? string.Empty), [.. GitStatus.Arguments]);
        Assert.Equal(["upload-pack"], Ran(marks));
    }

    // ---- D8: the check after every command, in one place ----

    /// <summary>
    /// D8, L7: the session checks again after a command that changes the repository, also after one that failed,
    /// and what it found is in force for the next command, without the test making the check itself (the test of
    /// the second pass made its own second scan, and passed with the window's check taken out). Here the ground
    /// moves under a repository that held no risk (a filter appears in its settings), and the next command fails.
    /// </summary>
    [GitFact]
    public async Task After_a_failed_command_the_check_is_made_and_what_it_found_is_in_force()
    {
        var root = Origin("moving");
        var marks = MarksOf(root);
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.True(session.Trusted);
        Assert.Empty(session.Overrides);
        var before = session.Overrides;
        var checks = session.Checks;

        Git(root, "config", "filter.x.clean", Marker(marks, "moved-filter"));
        var failed = await session.ChangeAsync(["switch", "no-such-branch"], GitRunner.Short);
        Assert.False(failed.Ok);
        Assert.Equal(checks + 1, session.Checks);
        Assert.False(session.Trusted);
        Assert.Contains(new GitSetting("filter.x.clean", string.Empty), session.Overrides);

        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        await session.RunAsync(GitTexts.StageAllArguments, GitRunner.Short);
        Assert.Empty(Ran(marks));

        // The way it ran: the next command with what was in force before the failed one.
        File.AppendAllText(Path.Combine(root, "b.txt"), "changed\n");
        await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, overrides: before, environment: _env);
        Assert.Equal(["moved-filter"], Ran(marks));
    }

    /// <summary>The settings file that counts on another branch alone (the second pass's finding 4), through the session: its own switch brings the check.</summary>
    [GitFact]
    public async Task A_switch_to_a_branch_whose_settings_name_a_filter_sets_it_aside_by_itself()
    {
        var root = Origin("branches");
        var marks = MarksOf(root);
        Git(root, "branch", "other");
        File.WriteAllText(Path.Combine(root, ".git", "extra.cfg"), $"[filter \"x\"]\n\tclean = {Marker(marks, "branch-filter").Replace("\"", "\\\"", StringComparison.Ordinal)}\n");
        Git(root, "config", "includeIf.onbranch:other.path", "extra.cfg");
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.DoesNotContain(new GitSetting("filter.x.clean", string.Empty), session.Overrides);
        Assert.True((await session.ChangeAsync(["switch", "other"], GitRunner.Short)).Ok);
        Assert.Contains(new GitSetting("filter.x.clean", string.Empty), session.Overrides);
        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        await session.RunAsync(["diff"], GitRunner.Short);
        Assert.Empty(Ran(marks));
    }

    // ---- D1: no connection while not trusted ----

    [GitFact]
    public async Task A_repository_that_is_not_trusted_is_never_connected()
    {
        var root = Origin("connect");
        Git(root, "config", "core.sshCommand", "never-run");
        var untrusted = Untrusted(root);
        await untrusted.CheckAsync();
        Assert.Equal("Fetch not started: this repository has settings you have not trusted.", untrusted.NotConnected("Fetch"));
        Assert.NotNull(untrusted.NotConnected("Push"));

        var trusted = Trusted(root);
        await trusted.CheckAsync();
        Assert.Null(trusted.NotConnected("Fetch"));

        // Trusted from the notice, for the risks it named.
        untrusted.TrustNow(untrusted.Scan.Risks);
        Assert.Null(untrusted.NotConnected("Pull"));
        Assert.Empty(untrusted.Overrides);
    }

    // ---- D3: a remote that is a folder is a second repository ----

    /// <summary>The reads before a push into folders, through git itself.</summary>
    private Task<string?> FolderProblemAsync(GitSession session, Reach reach) =>
        GitReach.FolderProblemAsync(session.RunInAsync, session.Root, reach.Folders ?? []);

    private async Task<Reach> ReachAsync(GitSession session, bool push)
    {
        var status = GitStatus.Parse((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Output);
        var reach = await GitReach.ReadAsync(async arguments => await session.RunAsync(arguments, GitRunner.Short), push, status.Branch, status.Upstream);
        Assert.NotNull(reach);
        return reach;
    }

    /// <summary>
    /// H2, Experiment D: a bare repository in the work tree, named by remote.origin.url as ./vendor/mirror, with a
    /// pre-receive hook. The repository opened held no risk, and git's own transport for a folder clears what
    /// axGIT gives through the environment: the hook ran. Now the push reads the other repository first, finds
    /// the hook, and nothing is pushed. A bare repository with git's samples alone takes the push.
    /// </summary>
    [GitFact]
    public async Task A_push_into_a_folder_whose_repository_has_hooks_does_not_start()
    {
        var root = Origin("pusher");
        var marks = MarksOf(root);
        Git(root, "init", "-q", "--bare", "vendor/mirror");
        MarkerFile(Path.Combine(root, "vendor", "mirror", "hooks", "pre-receive"), marks, "target-hook");
        Git(root, "remote", "add", "origin", "./vendor/mirror");
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.True(session.Trusted);

        var reach = await ReachAsync(session, push: true);
        Assert.Null(reach.Problem);
        Assert.Empty(reach.Hosts);
        Assert.Equal(["./vendor/mirror"], reach.Folders);
        var problem = await FolderProblemAsync(session, reach);
        Assert.Contains("./vendor/mirror", problem);
        Assert.Contains("hook pre-receive", problem);
        Assert.Empty(Ran(marks));

        // The way it ran: the push itself.
        await session.RunAsync(GitTexts.PushArguments(reach.Remote, null, "main"), GitRunner.Short);
        Assert.Equal(["target-hook"], Ran(marks));

        // A bare repository as git makes one takes the push, which names its branch on both sides.
        Git(_dir.Path, "init", "-q", "--bare", "clean.git");
        Git(root, "remote", "set-url", "origin", Path.Combine(_dir.Path, "clean.git"));
        var clean = await ReachAsync(session, push: true);
        Assert.Null(await FolderProblemAsync(session, clean));
        Assert.True((await session.RunAsync(GitTexts.PushArguments(clean.Remote, "origin/main", "main"), GitRunner.Short)).Ok);
        Assert.Contains("main", Git(Path.Combine(_dir.Path, "clean.git"), "branch", "--list"));
    }

    /// <summary>
    /// H2, Experiment C: only harmless settings (the branch follows another branch of this repository, by
    /// push.default=upstream) and hooks in .git/hooks, trusted: a push into "." ran every receive hook. Now a branch
    /// that follows one of another name is not pushed (D6), and a push into the repository itself reads it first
    /// and finds its hooks.
    /// </summary>
    [GitFact]
    public async Task A_push_into_the_repository_itself_reads_it_first()
    {
        var root = Origin("itself");
        var marks = MarksOf(root);
        Git(root, "branch", "other");
        Git(root, "config", "branch.main.remote", ".");
        Git(root, "config", "branch.main.merge", "refs/heads/other");
        Git(root, "config", "push.default", "upstream");
        MarkerFile(Path.Combine(root, ".git", "hooks", "pre-receive"), marks, "own-hook");
        var session = Trusted(root);
        await session.CheckAsync();

        var reach = await ReachAsync(session, push: true);
        Assert.Contains("another name, other", reach.Problem);

        Git(root, "config", "branch.main.merge", "refs/heads/main");
        var here = await ReachAsync(session, push: true);
        Assert.Equal([GitRemote.Here], here.Folders);
        Assert.Contains("hook pre-receive", await FolderProblemAsync(session, here));
        Assert.Empty(Ran(marks));
    }

    /// <summary>D3: a fetch and a pull from a folder whose repository holds hooks and programs of its own run nothing of it, with git's upload program named on the command line.</summary>
    [GitFact]
    public async Task A_fetch_and_a_pull_from_a_folder_run_nothing_of_its_repository()
    {
        var target = Origin("target");
        var marks = MarksOf(target);
        foreach (var hook in new[] { "pre-receive", "update", "post-receive", "post-update", "reference-transaction", "pre-upload-pack", "post-checkout" })
        {
            MarkerFile(Path.Combine(target, ".git", "hooks", hook), marks, "target-" + hook);
        }

        Git(target, "config", "uploadpack.packObjectsHook", Marker(marks, "pack-hook"));
        Git(target, "config", "core.alternateRefsCommand", Marker(marks, "alternate-refs"));
        Git(target, "config", "remote.self.uploadpack", Marker(marks, "upload-setting"));
        var root = Path.Combine(_dir.Path, "fetcher");
        Git(_dir.Path, "init", "-q", root);
        Git(root, "remote", "add", "origin", target);
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.True((await session.RunAsync(GitTexts.FetchArguments("origin"), GitRunner.Short)).Ok);
        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.merge", "refs/heads/main");
        Assert.True((await session.RunAsync(GitTexts.PullArguments("origin"), GitRunner.Short)).Ok, "pull");
        Assert.Empty(Ran(marks));
    }

    // ---- M3, M4, D7 ----

    /// <summary>M3: a fetch line that writes outside refs/remotes is a risk, so such a repository is not connected until trusted; with axGIT's --prune it deleted every local branch.</summary>
    [GitFact]
    public async Task A_fetch_that_writes_into_the_local_branches_is_a_risk()
    {
        Git(_dir.Path, "init", "-q", "--bare", "m3.git");
        var root = Origin("m3");
        Git(root, "remote", "add", "origin", Path.Combine(_dir.Path, "m3.git"));
        Git(root, "push", "-q", "origin", "main");
        Git(root, "branch", "keep");
        Git(root, "config", "--replace-all", "remote.origin.fetch", "+refs/heads/*:refs/heads/*");
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.Contains("remote.origin.fetch writes outside refs/remotes", session.Scan.Risks);
        Assert.NotNull(session.NotConnected("Fetch"));

        // The way it ran: the fetch, from a detached HEAD.
        Git(root, "checkout", "-q", "--detach");
        await GitRunner.RunAsync(root, GitTexts.FetchArguments("origin"), GitRunner.Short, environment: _env);
        Assert.DoesNotContain("keep", Git(root, "branch", "--list"));
    }

    /// <summary>M4: a branch named +main was pushed as a forced push of main; its full place is that branch alone.</summary>
    [GitFact]
    public async Task A_branch_named_plus_main_pushes_that_branch_and_never_forces_main()
    {
        Git(_dir.Path, "init", "-q", "--bare", "m4.git");
        var bare = Path.Combine(_dir.Path, "m4.git");
        var root = Origin("m4");
        Git(root, "remote", "add", "origin", bare);
        Git(root, "push", "-q", "origin", "main");
        var main = Git(bare, "rev-parse", "main").Trim();
        // The local main one commit behind the remote's, so that a forced push of it shows.
        Git(root, "reset", "-q", "--hard", "HEAD~1");
        Git(root, "switch", "-q", "--orphan", "+main");
        File.WriteAllText(Path.Combine(root, "c.txt"), "unrelated\n");
        Git(root, "add", "c.txt");
        Git(root, "commit", "-q", "-m", "unrelated");

        Assert.True((await GitRunner.RunAsync(root, GitTexts.PushArguments("origin", null, "+main"), GitRunner.Short, environment: _env)).Ok);
        Assert.Equal(main, Git(bare, "rev-parse", "main").Trim());
        Assert.Contains("+main", Git(bare, "branch", "--list"));

        // The way it ran: the branch's short name on the command line, read by git as a forced push of main.
        Plain(root, null, "push", "--set-upstream", "--", "origin", "+main");
        Assert.NotEqual(main, Git(bare, "rev-parse", "main").Trim());
    }

    /// <summary>
    /// D7, M5: a switch in a repository with no risk at all posted to the host a tracked .lfsconfig named, through
    /// Git LFS's filter. Now Git LFS fetches nothing in axGIT's runs: the file stays a pointer file. And a push
    /// in a repository whose pre-push hook is Git LFS's does not start (it would send to that host).
    /// </summary>
    [GitLfsFact]
    public async Task Git_lfs_reaches_no_server_of_its_own_through_axgit()
    {
        var root = Origin("lfs");
        Git(root, "branch", "other");
        Git(root, "switch", "-q", "other");
        File.WriteAllText(Path.Combine(root, ".lfsconfig"), "[lfs]\n\turl = https://lfs-host.example.invalid/x\n");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.bin filter=lfs diff=lfs merge=lfs -text\n");
        var pointer = "version https://git-lfs.github.com/spec/v1\noid sha256:" + new string('a', 64) + "\nsize 12\n";
        var blob = _dir.File("pointer.bin", pointer);
        var hash = Git(root, "hash-object", "-w", "--no-filters", blob).Trim();
        Git(root, "add", ".lfsconfig", ".gitattributes");
        Git(root, "update-index", "--add", "--cacheinfo", $"100644,{hash},p.bin");
        Git(root, "commit", "-q", "-m", "lfs");
        File.Delete(Path.Combine(root, "p.bin"));
        Git(root, "switch", "-q", "main");

        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.True(session.Trusted);
        Assert.Null(session.LfsPushRefused());
        var trace = new Dictionary<string, string>(_env) { ["GIT_TRACE"] = "1" };
        var switched = await GitRunner.RunAsync(root, ["switch", "other"], GitRunner.Short, overrides: session.Overrides, environment: trace);
        Assert.True(switched.Ok, switched.AllText);
        Assert.DoesNotContain("lfs-host", switched.AllText);
        Assert.StartsWith("version https://git-lfs", File.ReadAllText(Path.Combine(root, "p.bin")));

        // The way it ran: the same switch as a console makes it, without the variable.
        Git(root, "switch", "-q", "-f", "main");
        var console = Plain(root, new Dictionary<string, string> { ["GIT_TRACE"] = "1" }, "switch", "other");
        Assert.Contains("lfs-host.example.invalid", console.Error + console.Output);

        Git(root, "switch", "-q", "-f", "main");
        Git(root, "lfs", "install", "--local");
        await session.CheckAsync();
        Assert.Contains("Git LFS", session.LfsPushRefused());
    }

    // ---- L3, D9 ----

    /// <summary>L3: the hooks of a repository inside are those of the folder git names for it, not its hooks folder by its usual name.</summary>
    [GitFact]
    public async Task The_hooks_of_a_repository_inside_are_found_where_git_says_they_are()
    {
        var library = Origin("library");
        var root = Origin("top");
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Git(root, "commit", "-q", "-m", "a submodule");
        var module = Path.Combine(root, ".git", "modules", "libs", "zip");
        Git(module, "config", "core.hooksPath", "myhooks");
        MarkerFile(Path.Combine(module, "myhooks", "post-checkout"), MarksOf(root), "inside-hook");
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Contains("submodule libs/zip: core.hookspath", scan.Risks);
        Assert.Contains("submodule libs/zip: hook post-checkout", scan.Risks);
    }

    /// <summary>D9: a repository whose settings include a file on another computer is not read by git at all: the scan names the computer, and the session runs nothing.</summary>
    [GitFact]
    public async Task A_repository_that_points_at_another_computer_runs_nothing()
    {
        var root = Origin("away");
        Git(root, "config", "include.path", "//elsewhere.invalid/share/x.cfg");
        var session = Untrusted(root);
        var scan = await session.CheckAsync();
        Assert.Equal("elsewhere.invalid", scan.Elsewhere);
        Assert.True(session.Closed);
        Assert.False((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);
        Assert.NotNull(session.NotConnected("Fetch"));
    }
}
