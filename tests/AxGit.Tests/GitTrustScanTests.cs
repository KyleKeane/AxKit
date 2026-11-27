using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The reads that say what a repository would run and which servers a fetch, pull or push reaches
/// (docs/axGIT/SPEC.md AG-1.6, AG-5.4a; the review of 2026-11-27 and its second pass), against git itself, in
/// repositories the test makes. The second pass's experiments are here as tests: a program the repository names
/// is a script that writes a line into a file of the test's, and the test runs the commands axGIT runs, with
/// what axGIT sets aside, and looks whether the line is there; each also runs once the way that did run the
/// script, so that the test is known to be able to fail. The addresses are of the reserved name "invalid", and
/// every run goes through a proxy on a closed port of this computer (<see cref="GitTests.Environment"/>), so that
/// nothing leaves it even where a resolver answers for "invalid". Where git is not installed the tests are said
/// as skipped (<see cref="GitFactAttribute"/>). In one collection with the tests that put a fake in git's place,
/// so that the two never run at once.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed partial class GitTrustScanTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-scan");
    private readonly string _marks;

    /// <summary>The variables of every git run of this class's (<see cref="GitTests.Environment"/>), given per run: nothing is set for the process.</summary>
    private readonly Dictionary<string, string> _env;

    public GitTrustScanTests()
    {
        _marks = Path.Combine(_dir.Path, "marks.log");
        _env = GitTests.Environment(_dir);
    }

    public void Dispose() => _dir.Dispose();

    /// <summary>git itself in a folder, as a console runs it, with nothing of axGIT's; fails the test when git does.</summary>
    private string Git(string folder, params string[] arguments)
    {
        var result = Plain(folder, arguments);
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    /// <summary>git itself in a folder, as a console runs it, whatever it answers.</summary>
    private ProcessResult Plain(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, _env);
        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
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

    // ---- The programs a repository names: scripts that leave a line ----

    /// <summary>The folder of the test with forward slashes, as a shell reads a path.</summary>
    private string Slashes => _dir.Path.Replace('\\', '/');

    /// <summary>
    /// A command for a setting that runs through the shell (a filter, a hook of the settings): it writes
    /// <paramref name="line"/> into the test's file and passes its input on, as a filter must.
    /// </summary>
    private string Marker(string line)
    {
        _dir.File("marker.sh", $"#!/bin/sh\necho \"$1\" >> \"{Slashes}/marks.log\"\ncat\n");
        // Without quotes where the path needs none: Git LFS splits an extension's command at its spaces alone.
        return Slashes.Contains(' ') ? $"sh \"{Slashes}/marker.sh\" {line}" : $"sh {Slashes}/marker.sh {line}";
    }

    /// <summary>A program file that writes <paramref name="line"/> into the test's file: a hook, or a program git starts by its path alone.</summary>
    private string MarkerFile(string path, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"#!/bin/sh\necho {line} >> \"{Slashes}/marks.log\"\n");
        return path.Replace('\\', '/');
    }

    /// <summary>The lines the scripts left since the last look, each once.</summary>
    private string[] Ran()
    {
        if (!File.Exists(_marks))
        {
            return [];
        }

        var lines = File.ReadAllLines(_marks).Distinct().ToArray();
        File.Delete(_marks);
        return lines;
    }

    /// <summary>What the window does with a repository: the scan, and what git is run with while the user has not trusted it.</summary>
    private async Task<IReadOnlyList<GitSetting>> NotTrustedAsync(string root)
    {
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(scan.Problem);
        Assert.NotEmpty(scan.Risks);
        return GitTrust.Overrides(scan.Risks, scan.Filters);
    }

    /// <summary>A command of the window's, as the window runs it.</summary>
    private Task<GitResult> AxGit(string root, IReadOnlyList<string> arguments, IReadOnlyList<GitSetting>? overrides) =>
        GitRunner.RunAsync(root, arguments, GitRunner.Short, overrides: overrides, environment: _env);

    [GitFact]
    public async Task A_plain_repository_runs_nothing()
    {
        var scan = await GitTrustScan.ReadAsync(Repository("plain"), default, _env);
        Assert.Null(scan.Problem);
        Assert.Empty(scan.Risks);
        // The filters are those of every file: Git LFS's, from the user's own settings.
        Assert.Equal(["lfs"], scan.Filters);
    }

    /// <summary>
    /// An everyday repository opens without the notice (AG-1.6c): a clone, with a submodule git made, a second
    /// work tree, a branch with an upstream and a description, a sparse checkout. Nothing of what git itself
    /// wrote there is a risk.
    /// </summary>
    [GitFact]
    public async Task An_everyday_repository_holds_no_risk()
    {
        var library = Repository("library");
        var root = Path.Combine(_dir.Path, "clone");
        Git(_dir.Path, "clone", "-q", "--no-tags", library, "clone");
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Git(root, "commit", "-q", "-m", "a submodule");
        Git(root, "worktree", "add", "-q", Path.Combine(_dir.Path, "second"), "-b", "second");
        Git(root, "switch", "-q", "-c", "topic");
        Git(root, "branch", "-q", "--set-upstream-to=origin/main");
        Git(root, "config", "branch.topic.description", "A text");
        Git(root, "config", "remote.pushDefault", "origin");
        Git(root, "config", "user.name", "Someone");
        Git(root, "sparse-checkout", "init", "--cone");

        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(scan.Problem);
        Assert.Empty(scan.Risks);
    }

    /// <summary>
    /// The hooks are those of the folder git itself names: the repository's own, or the one its settings point at
    /// (core.hooksPath, which is a risk by its name too). The scan asks with nothing set aside: asked with the
    /// hooks folder axGIT gives an untrusted repository, git named that one, and no hook was found (caught by the
    /// window's script while this was built).
    /// </summary>
    [GitFact]
    public async Task The_hooks_of_the_folder_git_names_are_found()
    {
        var root = Repository("hooks");
        File.WriteAllText(Path.Combine(root, ".git", "hooks", "post-checkout"), "#!/bin/sh\n");
        Assert.Equal(["hook post-checkout"], (await GitTrustScan.ReadAsync(root, default, _env)).Risks);

        Directory.CreateDirectory(Path.Combine(root, "tools", "hooks"));
        File.WriteAllText(Path.Combine(root, "tools", "hooks", "pre-push"), "#!/bin/sh\n");
        Git(root, "config", "core.hooksPath", "tools/hooks");
        Assert.Equal(["core.hookspath", "hook pre-push"], (await GitTrustScan.ReadAsync(root, default, _env)).Risks);
    }

    [GitFact]
    public async Task The_work_trees_own_settings_file_is_read()
    {
        var root = Repository("worktree");
        Git(root, "config", "extensions.worktreeConfig", "true");
        Git(root, "config", "--worktree", "core.sshCommand", "never-run");
        Git(root, "config", "gpg.ssh.defaultKeyCommand", "never-run");
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(scan.Problem);
        Assert.Equal(["core.sshcommand", "gpg.ssh.defaultkeycommand"], scan.Risks.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// A repository inside another, with no .gitmodules to say so: the entry of the index is enough for git to run
    /// itself there on a plain status, and so for the scan to read its settings and hooks.
    /// </summary>
    [GitFact]
    public async Task What_a_repository_inside_would_run_is_found_whether_git_keeps_it_inside_or_under_modules()
    {
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

        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(scan.Problem);
        Assert.Equal(
            ["submodule libs/zip: filter.x.smudge", "submodule vendor: core.fsmonitor", "submodule vendor: hook post-checkout"],
            scan.Risks.Order(StringComparer.Ordinal));
        // The filters are every repository's: the user's Git LFS, and the one the submodule names.
        Assert.Equal(["lfs", "x"], scan.Filters);
        Assert.Equal(
            [new GitSetting("filter.x.smudge", string.Empty), new GitSetting("core.fsmonitor", string.Empty), .. GitTrust.Careful],
            GitTrust.Overrides(scan.Risks, scan.Filters));

        // The submodule's work tree taken away, its repository stays under .git/modules, and is still read.
        Git(root, "submodule", "--quiet", "deinit", "-f", "libs/zip");
        var after = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Contains("submodule libs/zip: filter.x.smudge", after.Risks);
    }

    [GitFact]
    public async Task A_folder_that_is_no_repository_is_a_problem_and_never_nothing_to_run()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, "not-one")).FullName;
        // Kept from finding a repository above the temporary folder, should there be one.
        File.WriteAllText(Path.Combine(folder, ".git"), "gitdir: nowhere\n");
        var scan = await GitTrustScan.ReadAsync(folder, default, _env);
        Assert.NotNull(scan.Problem);
        Assert.Empty(scan.Risks);
    }

    /// <summary>A context that counts what is sent back to it, as a window's thread would be sent the scan's steps.</summary>
    private sealed class CountingContext : SynchronizationContext
    {
        public int Count;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Count);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Count);
            d(state);
        }
    }

    /// <summary>
    /// Root CLAUDE.md rule 3 (the second pass, finding 8): nothing of the scan comes back to the caller's thread
    /// between its steps, where the folder reads were (200 submodules took 18 seconds on the window's thread).
    /// The scan is asked from a thread with a context of its own, as the window's is; before, each of its steps
    /// was sent to that context.
    /// </summary>
    [GitFact]
    public async Task Nothing_of_the_scan_runs_on_the_callers_thread()
    {
        var root = Repository("threads");
        var before = SynchronizationContext.Current;
        var context = new CountingContext();
        Task<TrustScan> scan;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            scan = GitTrustScan.ReadAsync(root, default, _env);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(before);
        }

        Assert.Null((await scan).Problem);
        Assert.Equal(0, context.Count);
    }

    // ---- The second pass's experiments (2026-11-27): what ran, and runs no longer ----

    /// <summary>
    /// Finding 1: Git LFS runs the program of lfs.extension.name.clean, a setting of the repository's, on every
    /// add and diff of a file that goes through its filter; the name was on no list. Now the name is not known,
    /// so the repository is asked about, and while it is not trusted no filter runs. Passes without running
    /// where Git LFS is not installed.
    /// </summary>
    [GitLfsFact]
    public async Task An_extension_of_git_lfs_in_the_repositorys_settings_runs_nothing()
    {
        var root = Repository("lfs");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.bin filter=lfs\n");
        File.WriteAllText(Path.Combine(root, "x.bin"), "binary");
        Git(root, "config", "lfs.extension.x.clean", Marker("lfs-extension"));
        Git(root, "config", "lfs.extension.x.smudge", Marker("lfs-extension"));
        Git(root, "config", "lfs.extension.x.priority", "0");

        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Equal(["lfs.extension.x.clean", "lfs.extension.x.priority", "lfs.extension.x.smudge"], scan.Risks.Order(StringComparer.Ordinal));
        Assert.Equal(scan.Risks, GitTrust.NotKnown(scan.Risks));
        var overrides = GitTrust.Overrides(scan.Risks, scan.Filters);

        Assert.True((await AxGit(root, GitTexts.StageAllArguments, overrides)).Ok);
        File.WriteAllText(Path.Combine(root, "x.bin"), "binary, changed");
        Assert.True((await AxGit(root, ["diff"], overrides)).Ok);
        Assert.Empty(Ran());

        // The way it ran: the same command with nothing set aside, as for a repository that held no risk.
        await AxGit(root, ["diff"], null);
        Assert.Equal(["lfs-extension"], Ran());
    }

    /// <summary>
    /// Finding 2: gc.recentObjectsHook is run by git's automatic maintenance after a commit, and an empty value
    /// does not set it aside, since it is a setting with several values. Every run of axGIT's has the automatic
    /// maintenance off. The repository is made ripe for it: loose objects over its limit, to be pruned at once.
    /// </summary>
    [GitFact]
    public async Task The_recent_objects_hook_of_automatic_maintenance_runs_nothing()
    {
        var root = Repository("maintenance");
        var blobs = Directory.CreateDirectory(Path.Combine(_dir.Path, "blobs")).FullName;
        var paths = Enumerable.Range(0, 1500).Select(number => Path.Combine(blobs, number + ".txt")).ToList();
        // The same texts in every run, so the same objects: enough of them in the folder git counts (objects/17).
        paths.ForEach(path => File.WriteAllText(path, "blob " + Path.GetFileName(path) + "\n"));
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = root };
        start.ArgumentList.Add("hash-object");
        start.ArgumentList.Add("-w");
        start.ArgumentList.Add("--stdin-paths");
        GitTests.Put(start, _env);
        Assert.Equal(0, ProcessRun.Run(start, paths, TimeSpan.FromSeconds(120)).ExitCode);
        Git(root, "config", "gc.recentObjectsHook", Marker("recent-objects"));
        Git(root, "config", "gc.auto", "1");
        Git(root, "config", "gc.autoPackLimit", "1");
        Git(root, "config", "gc.pruneExpire", "now");

        var overrides = await NotTrustedAsync(root);
        File.AppendAllText(Path.Combine(root, "a.txt"), "two\n");
        Assert.True((await AxGit(root, GitTexts.StageAllArguments, overrides)).Ok);
        Assert.True((await AxGit(root, GitTexts.CommitArguments("two"), overrides)).Ok);
        Assert.Empty(Ran());

        // The same with the repository trusted: the maintenance is off for every run, not for an untrusted one alone.
        File.AppendAllText(Path.Combine(root, "a.txt"), "three\n");
        Assert.True((await AxGit(root, ["commit", "-a", "-m", "three"], null)).Ok);
        Assert.Empty(Ran());

        // The way it ran: a commit as a console makes it.
        File.AppendAllText(Path.Combine(root, "a.txt"), "four\n");
        Git(root, "commit", "-q", "-a", "-m", "four");
        Assert.Equal(["recent-objects"], Ran());
    }

    /// <summary>
    /// Finding 3: a filter whose name holds an equals sign. As an argument, "-c filter.a=b.clean=", git cut the
    /// name at the first sign and set nothing aside; through the environment the name is whole.
    /// </summary>
    [GitFact]
    public async Task A_filter_with_an_equals_sign_in_its_name_runs_nothing()
    {
        var root = Repository("equals");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.txt filter=a=b\n");
        Git(root, "config", "filter.a=b.clean", Marker("equals-filter"));
        File.WriteAllText(Path.Combine(root, "a.txt"), "one\nchanged\n");

        var overrides = await NotTrustedAsync(root);
        Assert.Contains(new GitSetting("filter.a=b.clean", string.Empty), overrides);
        Assert.True((await AxGit(root, ["diff"], overrides)).Ok);
        Assert.True((await AxGit(root, GitTexts.StageAllArguments, overrides)).Ok);
        Assert.Empty(Ran());

        // The way it ran: the name set to nothing as an argument, which git reads as another name.
        File.WriteAllText(Path.Combine(root, "a.txt"), "one\nchanged again\n");
        Plain(root, "-c", "filter.a=b.clean=", "diff");
        Assert.Equal(["equals-filter"], Ran());
    }

    /// <summary>
    /// Finding 3: core.gitProxy. git takes the first proxy command it reads, the repository's, so an empty one
    /// given after it set nothing aside; without the git transport none is started.
    /// </summary>
    [GitFact]
    public async Task A_proxy_command_of_the_repositorys_runs_nothing()
    {
        var root = Repository("proxy-command");
        Git(root, "config", "core.gitProxy", MarkerFile(Path.Combine(_dir.Path, "proxy-program"), "git-proxy"));
        Git(root, "remote", "add", "origin", "git://host.example.invalid/x.git");

        var overrides = await NotTrustedAsync(root);
        var fetched = await AxGit(root, GitTexts.FetchArguments("origin"), overrides);
        Assert.False(fetched.Ok);
        Assert.Empty(Ran());

        // The way it ran: the empty proxy command alone, which is what was given until now.
        await AxGit(root, GitTexts.FetchArguments("origin"), [new GitSetting("core.gitproxy", string.Empty)]);
        Assert.Equal(["git-proxy"], Ran());
    }

    /// <summary>
    /// Finding 4: a settings file included for one branch alone (includeIf onbranch). The look when the
    /// repository opened saw no filter; after axGIT's own switch the file counts, and its filter ran on the
    /// next diff. The scan is made again before the lists are read after every command (AG-1.6b), and what git
    /// is run with is worked out from what is there then.
    /// </summary>
    [GitFact]
    public async Task A_settings_file_that_counts_on_another_branch_runs_nothing_after_the_switch()
    {
        var root = Repository("branches");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.txt filter=x\n");
        Git(root, "add", ".gitattributes");
        Git(root, "commit", "-q", "-m", "attributes");
        Git(root, "branch", "other");
        File.WriteAllText(Path.Combine(root, ".git", "extra.cfg"), $"[filter \"x\"]\n\tclean = {Marker("branch-filter").Replace("\"", "\\\"", StringComparison.Ordinal)}\n");
        Git(root, "config", "includeIf.onbranch:other.path", "extra.cfg");

        // As the repository opens: the include is the one risk, and no filter of its file is known yet.
        var opened = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Equal(["includeif.onbranch:other.path"], opened.Risks);
        var first = GitTrust.Overrides(opened.Risks, opened.Filters);
        Assert.True((await AxGit(root, ["switch", "other"], first)).Ok);
        Assert.Empty(Ran());

        // Before the lists are read after the switch: the scan again, and the mark is another, so the user is asked again.
        var again = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Contains("filter.x.clean", again.Risks);
        Assert.NotEqual(GitTrust.Mark(opened.Risks), GitTrust.Mark(again.Risks));
        File.WriteAllText(Path.Combine(root, "a.txt"), "one\nchanged\n");
        var second = GitTrust.Overrides(again.Risks, again.Filters);
        Assert.True((await AxGit(root, ["diff"], second)).Ok);
        Assert.True((await AxGit(root, GitTexts.StageAllArguments, second)).Ok);
        Assert.Empty(Ran());

        // The way it ran: what was set aside when the repository opened, used after the switch.
        File.WriteAllText(Path.Combine(root, "a.txt"), "one\nchanged again\n");
        await AxGit(root, ["diff"], first);
        Assert.Equal(["branch-filter"], Ran());
    }

    /// <summary>
    /// Finding 4: a folder with a repository of its own and no entry in the index. Stage all makes it an entry,
    /// and from then on git runs itself inside it on every status, with that repository's hooks. The scan finds
    /// it before anything is staged, and no hook runs in a repository that is not trusted.
    /// </summary>
    [GitFact]
    public async Task A_hook_of_a_repository_in_a_folder_git_does_not_track_runs_nothing()
    {
        var root = Repository("nested");
        var vendor = Directory.CreateDirectory(Path.Combine(root, "vendor")).FullName;
        Git(vendor, "init", "-q");
        File.WriteAllText(Path.Combine(vendor, "v.txt"), "inside\n");
        Git(vendor, "add", "v.txt");
        Git(vendor, "commit", "-q", "-m", "inside");
        MarkerFile(Path.Combine(vendor, ".git", "hooks", "post-index-change"), "nested-hook");
        Ran();

        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Equal(["submodule vendor: hook post-index-change"], scan.Risks);
        var overrides = GitTrust.Overrides(scan.Risks, scan.Filters);

        // A file of the repository inside with another time, and the same text: its status writes its index again, which is when the hook runs.
        var time = DateTime.UtcNow;
        void Touch() => File.SetLastWriteTimeUtc(Path.Combine(vendor, "v.txt"), time = time.AddSeconds(3));

        Assert.True((await AxGit(root, GitTexts.StageAllArguments, overrides)).Ok);
        Touch();
        Assert.True((await AxGit(root, GitStatus.Arguments, overrides)).Ok);
        Assert.Empty(Ran());

        // The way it ran: the status with nothing set aside, as for a repository whose one look had found nothing.
        Touch();
        await AxGit(root, GitStatus.Arguments, null);
        Assert.Equal(["nested-hook"], Ran());
    }

    /// <summary>A repository inside another inside another is followed, each named from the repository opened.</summary>
    [GitFact]
    public async Task A_repository_inside_one_that_is_inside_is_found()
    {
        var root = Repository("levels");
        var outer = Directory.CreateDirectory(Path.Combine(root, "outer")).FullName;
        Git(outer, "init", "-q");
        var inner = Directory.CreateDirectory(Path.Combine(outer, "inner")).FullName;
        Git(inner, "init", "-q");
        Git(inner, "config", "core.sshCommand", "never-run");
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(scan.Problem);
        Assert.Equal(["submodule outer/inner: core.sshcommand"], scan.Risks);
    }

    // ---- The server asked about is the one reached (AG-5.4a; the second pass, findings 5 and 10) ----

    /// <summary>A name under the reserved "invalid" in anything git said: where it tried to go.</summary>
    [GeneratedRegex(@"[a-z0-9-]+\.example\.invalid")]
    private static partial Regex ReservedHost();

    /// <summary>A repository on main, which follows origin's main, with three remotes on servers of their own.</summary>
    private string WithRemotes(string name)
    {
        var root = Repository(name);
        foreach (var remote in new[] { "origin", "evil", "third" })
        {
            Git(root, "remote", "add", remote, $"https://{remote}.example.invalid/a/b.git");
        }

        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.merge", "refs/heads/main");
        return root;
    }

    /// <summary>
    /// A fetch, pull or push as the window makes it: the status, the questions of <see cref="GitReach"/>, and,
    /// unless they gave a problem, the command with the remote they gave on its command line. Returns what was
    /// asked about and the hosts git then tried to reach, read from what it said.
    /// </summary>
    private async Task<(Reach Asked, IReadOnlyList<string> Reached, string Said)> ToServerAsync(string root, string what, IReadOnlyList<GitSetting>? overrides = null)
    {
        var status = GitStatus.Parse((await AxGit(root, GitStatus.Arguments, overrides)).Output);
        var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments, overrides), push: what == "push", status.Branch, status.Upstream);
        Assert.NotNull(asked);
        if (asked.Problem is not null)
        {
            // Nothing runs: no remote to put on a command line.
            Assert.Empty(asked.Remote);
            Assert.Empty(asked.Hosts);
            return (asked, [], string.Empty);
        }

        var arguments = what switch
        {
            "fetch" => GitTexts.FetchArguments(asked.Remote),
            "pull" => GitTexts.PullArguments(asked.Remote),
            _ => GitTexts.PushArguments(asked.Remote, status.Upstream, status.Branch!),
        };
        // Where git went is read from its own trace (the remote helper's command line names the address), and the
        // run ends at the proxy on a closed port of this computer: nothing leaves it.
        var result = await GitRunner.RunAsync(root, arguments, GitRunner.Short, overrides: overrides, environment: new Dictionary<string, string>(_env) { ["GIT_TRACE"] = "1" });
        Assert.False(result.Ok);
        return (asked, [.. ReservedHost().Matches(result.AllText).Select(match => match.Value).Distinct()], result.AllText);
    }

    /// <summary>What was asked about is what was reached, and it is the server expected.</summary>
    private async Task ReachesAsync(string root, string what, string host)
    {
        var (asked, reached, _) = await ToServerAsync(root, what);
        Assert.Null(asked.Problem);
        Assert.Equal([host], asked.Hosts);
        Assert.Equal([host], reached);
    }

    /// <summary>Nothing is asked about and nothing runs: git could not name the server.</summary>
    private async Task RunsNothingAsync(string root, string what)
    {
        var (asked, reached, _) = await ToServerAsync(root, what);
        Assert.NotNull(asked.Problem);
        Assert.Empty(reached);
    }

    [GitFact]
    public async Task With_nothing_odd_set_each_command_reaches_origin_alone()
    {
        var root = WithRemotes("plain-remotes");
        await ReachesAsync(root, "fetch", "origin.example.invalid");
        await ReachesAsync(root, "pull", "origin.example.invalid");
        await ReachesAsync(root, "push", "origin.example.invalid");
    }

    /// <summary>Row 1: branch.main.remote without branch.main.merge. git was asked for the upstream's remote, which there is none of, so origin was asked about; git, run without a remote, fetched from the branch's.</summary>
    [GitFact]
    public async Task A_branch_with_a_remote_and_no_branch_to_follow_reaches_the_server_asked_about()
    {
        var root = WithRemotes("no-merge");
        Git(root, "config", "branch.main.remote", "evil");
        Git(root, "config", "--unset", "branch.main.merge");
        await ReachesAsync(root, "fetch", "evil.example.invalid");
        await ReachesAsync(root, "pull", "evil.example.invalid");
        // Without an upstream a push goes to origin by name, and ties the branch to it.
        await ReachesAsync(root, "push", "origin.example.invalid");
    }

    /// <summary>Row 2: an address in place of a remote's name. git does not know such a remote, nobody was asked, and the command ran; and git's complaint held the address with its sign-in.</summary>
    [GitFact]
    public async Task An_address_in_place_of_a_remotes_name_runs_nothing_and_its_sign_in_is_not_said()
    {
        var root = WithRemotes("address");
        Git(root, "config", "branch.main.remote", "https://user:secret" + "@" + "addr.example.invalid/a.git");
        await RunsNothingAsync(root, "fetch");
        await RunsNothingAsync(root, "pull");
        var (asked, _, _) = await ToServerAsync(root, "fetch");
        Assert.Contains("addr.example.invalid", asked.Problem);
        Assert.DoesNotContain("secret", asked.Problem);

        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.pushRemote", "https://addr.example.invalid/a.git");
        await RunsNothingAsync(root, "push");
    }

    /// <summary>Row 3: fetch.all. A fetch without a remote reached every remote, and one was asked about.</summary>
    [GitFact]
    public async Task With_fetch_all_set_a_fetch_reaches_the_one_remote_asked_about()
    {
        var root = WithRemotes("fetch-all");
        Git(root, "config", "fetch.all", "true");
        await ReachesAsync(root, "fetch", "origin.example.invalid");
        await ReachesAsync(root, "pull", "origin.example.invalid");
        // The way it ran: a fetch with no remote on its command line.
        Assert.Equal(3, ReservedHost().Matches((await AxGit(root, ["fetch"], null)).AllText).Select(match => match.Value).Distinct().Count());
    }

    /// <summary>Row 4: a remote named like an option. The question stopped at a complaint about a switch, nobody was asked, and the push ran.</summary>
    [GitFact]
    public async Task A_remote_named_like_an_option_is_asked_about_and_is_the_one_reached()
    {
        var root = WithRemotes("dash");
        Git(root, "config", "remote.-x.url", "https://dash.example.invalid/a/b.git");
        Git(root, "config", "branch.main.pushRemote", "-x");
        await ReachesAsync(root, "push", "dash.example.invalid");
        await ReachesAsync(root, "fetch", "origin.example.invalid");
    }

    /// <summary>Row 6: a detached HEAD with remote.pushDefault and push.default=matching. Origin was asked about, and git pushed every branch to the default remote.</summary>
    [GitFact]
    public async Task A_push_without_a_branch_runs_nothing()
    {
        var root = WithRemotes("detached");
        Git(root, "config", "remote.pushDefault", "evil");
        Git(root, "config", "push.default", "matching");
        // On the branch the default remote is the one asked about and reached.
        await ReachesAsync(root, "push", "evil.example.invalid");
        Git(root, "checkout", "-q", "--detach");
        await RunsNothingAsync(root, "push");
        // A fetch needs no branch: the one remote of the name origin.
        await ReachesAsync(root, "fetch", "origin.example.invalid");
    }

    /// <summary>Found while the rows were tried: a group of remotes with a remote's name. git fetches the group's remotes in place of the one named.</summary>
    [GitFact]
    public async Task A_remotes_name_that_is_a_group_of_remotes_runs_nothing()
    {
        var root = WithRemotes("group");
        Git(root, "config", "remotes.origin", "evil third");
        await RunsNothingAsync(root, "fetch");
        await RunsNothingAsync(root, "pull");
        // The way it ran: the fetch itself, the remote on its command line.
        var hosts = ReservedHost().Matches((await AxGit(root, GitTexts.FetchArguments("origin"), null)).AllText).Select(match => match.Value).Distinct().Order();
        Assert.Equal(["evil.example.invalid", "third.example.invalid"], hosts);
    }

    /// <summary>A remote git does not know, and an address no server can be read from: nothing runs.</summary>
    [GitFact]
    public async Task A_remote_git_does_not_know_and_an_address_that_starts_a_program_run_nothing()
    {
        var none = Repository("no-remote");
        await RunsNothingAsync(none, "fetch");
        await RunsNothingAsync(none, "push");

        var root = WithRemotes("ext");
        Git(root, "remote", "set-url", "origin", "ext::" + MarkerFile(Path.Combine(_dir.Path, "ext-program"), "ext-transport"));
        await RunsNothingAsync(root, "fetch");
        await RunsNothingAsync(root, "push");
        Assert.Empty(Ran());
    }

    /// <summary>A branch that follows another branch of the same repository reaches no server, and its pull runs.</summary>
    [GitFact]
    public async Task A_branch_that_follows_a_branch_of_this_repository_asks_about_no_server()
    {
        var root = WithRemotes("here");
        Git(root, "switch", "-q", "-c", "topic", "--track", "main");
        var status = GitStatus.Parse((await AxGit(root, GitStatus.Arguments, null)).Output);
        var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments, null), push: false, status.Branch, status.Upstream);
        Assert.NotNull(asked);
        Assert.Null(asked.Problem);
        Assert.Equal(".", asked.Remote);
        Assert.Empty(asked.Hosts);
        Assert.True((await AxGit(root, GitTexts.PullArguments(asked.Remote), null)).Ok);
    }

    /// <summary>
    /// Finding 10: a proxy the repository's own settings name is where the bytes go first. In a trusted
    /// repository it is asked about and said; in one that is not trusted it is set to nothing, and git, asked
    /// with the same settings, names none.
    /// </summary>
    [GitFact]
    public async Task A_proxy_of_the_repositorys_own_settings_is_named_or_set_to_nothing()
    {
        var root = WithRemotes("proxy");
        // The repository's proxy is this computer at another address of its own, 127.0.0.2, apart from the test's own proxy at 127.0.0.1.
        Git(root, "config", "http.https://origin.example.invalid/.proxy", "http://user:secret" + "@" + "127.0.0.2:7");

        var (trusted, reached, said) = await ToServerAsync(root, "fetch");
        Assert.Equal(["origin.example.invalid"], trusted.Hosts);
        Assert.Equal(["127.0.0.2"], trusted.Proxies);
        Assert.Equal(["origin.example.invalid"], reached);
        Assert.Contains("via 127.0.0.2", said);

        var overrides = await NotTrustedAsync(root);
        Assert.Contains(new GitSetting("http.https://origin.example.invalid/.proxy", string.Empty), overrides);
        var (kept, direct, saidDirect) = await ToServerAsync(root, "fetch", overrides);
        Assert.Empty(kept.Proxies);
        Assert.Equal(["origin.example.invalid"], direct);
        Assert.DoesNotContain("127.0.0.2", saidDirect);

        // The user's own proxy is the user's: not named.
        Git(root, "config", "--unset", "http.https://origin.example.invalid/.proxy");
        File.AppendAllText(_env["GIT_CONFIG_GLOBAL"], "[http]\n\tproxy = http://127.0.0.1:9\n");
        var (own, _, _) = await ToServerAsync(root, "fetch");
        Assert.Empty(own.Proxies);
    }

    /// <summary>
    /// The consent before a push is for where the push goes (bundle AX-11.5): the branch's pushRemote, every pushurl
    /// of that remote, and an address as git rewrites it.
    /// </summary>
    [GitFact]
    public async Task The_servers_of_a_push_are_the_push_remotes_addresses_as_git_rewrites_them()
    {
        var root = Repository("remotes");
        Git(root, "remote", "add", "origin", "https://fetch.example.invalid/a/b.git");
        Git(root, "remote", "add", "backup", "https://old.example.invalid/a/b.git");
        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.merge", "refs/heads/main");

        async Task<IReadOnlyList<string>> Hosts(bool push)
        {
            var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments, null), push, "main", "origin/main");
            Assert.Null(asked!.Problem);
            return asked.Hosts;
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
