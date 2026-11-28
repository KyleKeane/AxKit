using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;
using Xunit.Abstractions;

namespace AxGit.Tests;

/// <summary>
/// The review's fifth pass of 2026-11-28 (batch B4e; docs/axGIT/SPEC.md AG-1.6a, AG-1.6b): git works on the
/// repository that was read and no other (P1), a path from the repository is read only in a positive local form (P2),
/// trusted is what was seen (P3), and the lows with them. Against git itself, in repositories the test makes; a
/// program of a repository's is a script that leaves a line in a file of the test's, and each test runs once the way
/// that did run it, so that it is known to be able to fail. A share is this computer's own, //localhost/C$.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitBoundsTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-bounds");
    private readonly Dictionary<string, string> _env;
    private readonly ITestOutputHelper _output;

    public GitBoundsTests(ITestOutputHelper output)
    {
        _output = output;
        _env = GitTests.Environment(_dir);
    }

    public void Dispose() => _dir.Dispose();

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

    private string Git(string folder, params string[] arguments)
    {
        var result = Plain(folder, arguments);
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    private string Marks => Path.Combine(_dir.Path, "marks.log");

    private string[] Ran()
    {
        if (!File.Exists(Marks))
        {
            return [];
        }

        var lines = File.ReadAllLines(Marks).Distinct().ToArray();
        File.Delete(Marks);
        return lines;
    }

    private void MarkerFile(string path, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"#!/bin/sh\necho {line} >> \"{Marks.Replace('\\', '/')}\"\nexit 0\n");
    }

    private string Marker(string line)
    {
        var script = _dir.File("marker.sh", $"#!/bin/sh\necho \"$1\" >> \"{Marks.Replace('\\', '/')}\"\ncat\n");
        return $"sh \"{script.Replace('\\', '/')}\" {line}";
    }

    private string Repository(string path)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, path)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "one\n");
        File.WriteAllText(Path.Combine(folder, "b.txt"), "two\n");
        Git(folder, "add", "a.txt", "b.txt");
        Git(folder, "commit", "-q", "-m", "one");
        return folder;
    }

    private static readonly string[] Hooks = ["pre-commit", "post-commit", "post-checkout", "post-index-change", "reference-transaction", "prepare-commit-msg", "commit-msg", "pre-push"];

    // ---- P1: git works on the repository that was read, and no other ----

    /// <summary>
    /// P1 (the fifth pass, H1; tried with git 2.53): a repository inside another whose .git folder git does not take
    /// for a repository, by a HEAD that is no reference and no object id, or by a commondir that names nothing. axGIT
    /// read the inner one and found nothing; git, run there, climbed to the outer one and ran its hooks on Stage all.
    /// Now the inner folder is no repository for axGIT, and a session made on it runs nothing of the outer one, at the
    /// opening and on every command. The half that shows the climb: git as a console runs it there.
    /// </summary>
    [GitFact]
    public async Task Git_works_on_the_repository_that_was_read_and_no_other()
    {
        var outer = Repository("outer");
        foreach (var hook in Hooks)
        {
            MarkerFile(Path.Combine(outer, ".git", "hooks", hook), "outer-" + hook);
        }

        foreach (var kind in new[] { "garbage-head", "commondir-nowhere" })
        {
            var inner = Repository(Path.Combine("outer", kind));
            if (kind == "garbage-head")
            {
                File.WriteAllText(Path.Combine(inner, ".git", "HEAD"), "garbage\n");
            }
            else
            {
                File.WriteAllText(Path.Combine(inner, ".git", "commondir"), "../nowhere\n");
            }

            Ran();
            Assert.Null(GitTrustScan.RootOf(inner));
            Assert.Null(GitTrustScan.RootOf(Path.Combine(inner, "a.txt")));
            var session = new GitSession(inner, _ => true, _env);
            await session.CheckAsync();
            Assert.False(session.Trusted, kind);
            await GitTests.EveryCommandAsync(inner, "origin", (arguments, change) => change ? session.ChangeAsync(arguments, GitRunner.Short) : session.RunAsync(arguments, GitRunner.Short));
            Assert.Empty(Ran());

            // The way it ran: git itself, from the inner folder.
            File.AppendAllText(Path.Combine(inner, "a.txt"), "more\n");
            Plain(inner, "add", "-A");
            Assert.Contains("outer-post-index-change", Ran());
            Git(outer, "reset", "-q");
        }
    }

    /// <summary>
    /// P1: git's own variables that name another repository's folders, its index or its object store, from the
    /// environment axKit was started with, are not passed on to axGIT's runs in a repository. With GIT_DIR naming
    /// another repository, Stage all ran that repository's hook.
    /// </summary>
    [GitFact]
    public async Task The_variables_that_name_another_repository_are_not_passed_on()
    {
        var other = Repository("other");
        MarkerFile(Path.Combine(other, ".git", "hooks", "post-index-change"), "other-hook");
        var root = Repository("mine");
        File.AppendAllText(Path.Combine(root, "a.txt"), "more\n");
        var elsewhere = new Dictionary<string, string>(_env)
        {
            ["GIT_DIR"] = Path.Combine(other, ".git"),
            ["GIT_WORK_TREE"] = other,
            ["GIT_INDEX_FILE"] = Path.Combine(other, ".git", "index"),
        };
        Ran();
        var staged = await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: elsewhere);
        Assert.True(staged.Ok, staged.AllText);
        Assert.Empty(Ran());
        Assert.Contains("a.txt", Git(root, "diff", "--cached", "--name-only"));
    }

    /// <summary>The environment of a test's runs with one variable more.</summary>
    private Dictionary<string, string> With(string name, string value) => new(_env) { [name] = value };

    /// <summary>
    /// L3 (the review's sixth pass): GIT_COMMON_DIR, on its own. Naming another repository's folder, it is where git
    /// takes the hooks from: Stage all ran that repository's hook. Not passed on to axGIT's runs.
    /// </summary>
    [GitFact]
    public async Task The_common_folder_of_the_environment_is_not_passed_on()
    {
        var other = Repository("common-other");
        MarkerFile(Path.Combine(other, ".git", "hooks", "post-index-change"), "common-hook");
        var root = Repository("common-mine");
        var elsewhere = With("GIT_COMMON_DIR", Path.Combine(other, ".git"));
        File.AppendAllText(Path.Combine(root, "a.txt"), "more\n");
        Ran();
        Assert.True((await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: elsewhere)).Ok);
        Assert.Empty(Ran());

        // The way it ran: git with the variable, as a console started with it runs.
        File.AppendAllText(Path.Combine(root, "a.txt"), "again\n");
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = root };
        start.ArgumentList.Add("add");
        start.ArgumentList.Add("-A");
        GitTests.Put(start, elsewhere);
        ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.Equal(["common-hook"], Ran());
    }

    /// <summary>git as a console started with <paramref name="environment"/> runs it, in <paramref name="folder"/>.</summary>
    private ProcessResult GitWith(string folder, IReadOnlyDictionary<string, string> environment, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, environment);
        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
    }

    /// <summary>L3: GIT_OBJECT_DIRECTORY, on its own. Naming another repository's object store, it is where Stage all writes the file's content. Not passed on.</summary>
    [GitFact]
    public async Task The_object_store_of_the_environment_is_not_passed_on()
    {
        var other = Repository("objects-other");
        var root = Repository("objects-mine");
        var elsewhere = With("GIT_OBJECT_DIRECTORY", Path.Combine(other, ".git", "objects"));
        File.WriteAllText(Path.Combine(root, "c.txt"), "content of mine alone\n");
        var hash = Git(root, "hash-object", "c.txt").Trim();
        Assert.True((await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: elsewhere)).Ok);
        Assert.NotEqual(0, Plain(other, "cat-file", "-e", hash).ExitCode);
        Assert.Equal(0, Plain(root, "cat-file", "-e", hash).ExitCode);

        // The way it ran: with the variable, the content goes into the other store.
        Git(root, "reset", "-q");
        GitWith(root, elsewhere, "add", "-A");
        Assert.Equal(0, Plain(other, "cat-file", "-e", hash).ExitCode);
    }

    /// <summary>L3: GIT_ALTERNATE_OBJECT_DIRECTORIES, on its own. Naming another repository's object store, git reads objects from it. Not passed on.</summary>
    [GitFact]
    public async Task The_alternate_object_stores_of_the_environment_are_not_passed_on()
    {
        var other = Repository("alternates-other");
        File.WriteAllText(Path.Combine(other, "c.txt"), "content of the other alone\n");
        var hash = Git(other, "hash-object", "-w", "c.txt").Trim();
        var root = Repository("alternates-mine");
        var elsewhere = With("GIT_ALTERNATE_OBJECT_DIRECTORIES", Path.Combine(other, ".git", "objects"));
        Assert.False((await GitRunner.RunAsync(root, ["cat-file", "-e", hash], GitRunner.Short, environment: elsewhere)).Ok);

        // The way it ran: with the variable, git finds the other's object.
        Assert.Equal(0, GitWith(root, elsewhere, "cat-file", "-e", hash).ExitCode);
    }

    /// <summary>L3: GIT_NAMESPACE, on its own. It makes git's transport see only the branches of one namespace of a repository: a read of a folder's branches shows none. Not passed on.</summary>
    [GitFact]
    public async Task The_namespace_of_the_environment_is_not_passed_on()
    {
        var other = Repository("namespace-other");
        var root = Repository("namespace-mine");
        var elsewhere = With("GIT_NAMESPACE", "elsewhere");
        var listed = await GitRunner.RunAsync(root, ["ls-remote", "--", other], GitRunner.Short, environment: elsewhere);
        Assert.True(listed.Ok, listed.AllText);
        Assert.Contains("refs/heads/", listed.Output);

        // The way it ran: with the variable, the other repository's branches are not seen.
        Assert.DoesNotContain("refs/heads/", GitWith(root, elsewhere, "ls-remote", "--", other).Output);
    }

    /// <summary>
    /// P1's own cost, kept: a named git folder passes over git's check of who owns a repository (safe.directory;
    /// tried with git 2.53, git's own switch GIT_TEST_ASSUME_DIFFERENT_OWNER standing for a repository another account
    /// owns). So the first check that trusts a repository asks git, finding it itself, whether it opens it: here git
    /// refuses, and the repository is not opened. The half that shows the cost: the named folder alone runs.
    /// </summary>
    [GitFact]
    public async Task A_repository_git_refuses_for_its_owner_is_not_opened()
    {
        var root = Repository("owned");
        var foreign = new Dictionary<string, string>(_env) { ["GIT_TEST_ASSUME_DIFFERENT_OWNER"] = "1" };
        var session = new GitSession(root, _ => true, foreign);
        await session.CheckAsync();
        Assert.False(session.Trusted);
        Assert.True(session.Closed);
        Assert.StartsWith("git does not open it:", session.Scan.Problem);
        Assert.True((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).NotRun);

        // The way it ran: the same run with the git folder named, which git does not check.
        Assert.True((await GitRunner.RunAsync(root, GitStatus.Arguments, GitRunner.Short, environment: foreign)).Ok);
    }

    // ---- P2: a path from the repository is read only in a positive local form ----

    /// <summary>The path of a file of the test's through this computer's own share, in the form given.</summary>
    private string Shared(string path, string form) => form + path[0] + "$" + path[2..];

    /// <summary>
    /// P2 (the fifth pass, H2): every form of a path that is not a plain path on a drive of this computer is a place
    /// axGIT does not read, wherever the repository names it: a share in every spelling (//localhost/C$,
    /// \??\UNC\localhost\C$, \\?\UNC\localhost\C$), a device path (\\.\C:, \??\C:), an alternate stream, a junction
    /// on the way. That nothing there is read is seen in the mark: the file's content changes, and the mark of a
    /// repository whose include names it stays the same.
    /// </summary>
    [GitFact]
    public async Task A_place_axgit_does_not_read_is_named_and_never_read()
    {
        var target = _dir.File(Path.Combine("far", "inc.cfg"), "[user]\n\tname = one\n");
        File.WriteAllText(target + ":hidden", "[user]\n\tname = one\n");
        var junction = Path.Combine(_dir.Path, "junction");
        Assert.True(Junction(junction, Path.Combine(_dir.Path, "far")), "a junction could not be made");
        var forms = new Dictionary<string, string>
        {
            ["share"] = Shared(target, "//localhost/").Replace('\\', '/'),
            ["nt-unc"] = Shared(target, @"\??\UNC\localhost\"),
            ["long-unc"] = Shared(target, @"\\?\UNC\localhost\"),
            ["device"] = @"\\.\" + target,
            ["nt-device"] = @"\??\" + target,
            ["stream"] = target + ":hidden",
            ["junction"] = Path.Combine(junction, "inc.cfg"),
        };
        var read = new List<string>();
        foreach (var (kind, form) in forms)
        {
            var root = Repository("p2-" + kind);
            File.AppendAllText(Path.Combine(root, ".git", "config"), $"[include]\n\tpath = \"{form.Replace("\\", "\\\\", StringComparison.Ordinal)}\"\n");
            File.WriteAllText(target, "[user]\n\tname = one\n");
            File.WriteAllText(target + ":hidden", "[user]\n\tname = one\n");
            var before = await GitTrustScan.ReadAsync(root, default, _env);
            Assert.Null(before.Problem);
            File.WriteAllText(target, "[user]\n\tname = two\n");
            File.WriteAllText(target + ":hidden", "[user]\n\tname = two\n");
            var after = await GitTrustScan.ReadAsync(root, default, _env);
            if (before.Mark != after.Mark || !after.Risks.Any(risk => risk.StartsWith("include.path names ", StringComparison.Ordinal)))
            {
                read.Add($"{kind}: {form}");
            }
        }

        Assert.True(read.Count == 0, "Read, or not named: " + string.Join("; ", read));

        // The way a plain path is read: the same change changes the mark.
        var plainRoot = Repository("p2-plain");
        Git(plainRoot, "config", "include.path", target.Replace('\\', '/'));
        var plain = await GitTrustScan.ReadAsync(plainRoot, default, _env);
        File.WriteAllText(target, "[user]\n\tname = three\n");
        Assert.NotEqual(plain.Mark, (await GitTrustScan.ReadAsync(plainRoot, default, _env)).Mark);
    }

    /// <summary>A junction, made with mklink /J, which needs no privilege; false where it could not be made.</summary>
    internal static bool Junction(string at, string target)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("mklink");
        start.ArgumentList.Add("/J");
        start.ArgumentList.Add(at);
        start.ArgumentList.Add(target);
        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(30)) is { ExitCode: 0, Stopped: false };
    }

    /// <summary>
    /// P2 and P1: a .git file whose folder lies through a junction is a place axGIT does not read, and nothing of the
    /// folder there is read: for the repository opened it is not opened (git would work on a folder not read); for a
    /// submodule, it is named as a risk.
    /// </summary>
    [GitFact]
    public async Task A_git_file_through_a_junction_is_named_and_its_folder_not_read()
    {
        var real = Repository("real");
        MarkerFile(Path.Combine(real, ".git", "hooks", "pre-commit"), "real-hook");
        Assert.True(Junction(Path.Combine(_dir.Path, "real-link"), real));
        var tree = Directory.CreateDirectory(Path.Combine(_dir.Path, "through")).FullName;
        File.WriteAllText(Path.Combine(tree, ".git"), $"gitdir: {Path.Combine(_dir.Path, "real-link", ".git")}\n");
        var scan = await GitTrustScan.ReadAsync(tree, default, _env);
        Assert.Equal("Its .git file names a place axGIT does not read, so axGIT does not open it.", scan.Problem);

        // Inside: a gitlink whose .git file leads through the junction.
        var root = Repository("holder");
        var inside = Repository(Path.Combine("holder", "inside"));
        Git(root, "add", "inside");
        // Set aside, not deleted: git's objects are read-only.
        Directory.Move(Path.Combine(inside, ".git"), Path.Combine(_dir.Path, "inside-git-aside"));
        File.WriteAllText(Path.Combine(inside, ".git"), $"gitdir: {Path.Combine(_dir.Path, "real-link", ".git")}\n");
        var held = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Null(held.Problem);
        Assert.Contains("submodule inside: its .git file names a place axGIT does not read", held.Risks);
        Assert.DoesNotContain("submodule inside: hook pre-commit", held.Risks);
    }

    // ---- P3: trusted is what was seen ----

    /// <summary>
    /// P3 (the fifth pass, M1): a filter added while the notice is open. The user trusts what the notice showed; the
    /// repository is checked again, holds another mark, and is not trusted: Stage all runs nothing.
    /// </summary>
    [GitFact]
    public async Task A_filter_added_while_the_notice_is_open_does_not_run_after_trust()
    {
        var root = Repository("race");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.txt filter=x\n");
        File.WriteAllText(Path.Combine(root, ".git", "hooks", "pre-push"), "#!/bin/sh\n");
        var session = new GitSession(root, _ => false, _env);
        var shown = await session.CheckAsync();
        Assert.False(session.Trusted);

        Git(root, "config", "filter.x.clean", Marker("race-filter"));
        Assert.False(await session.TrustAsync(shown));
        Assert.False(session.Trusted);
        File.AppendAllText(Path.Combine(root, "a.txt"), "more\n");
        await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short);
        Assert.Empty(Ran());

        // Trusted for what is there now, it runs.
        Assert.True(await session.TrustAsync(session.Scan));
        Assert.True((await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short)).Ok);
        Assert.Equal(["race-filter"], Ran());
    }

    /// <summary>
    /// P3 (M3): the check before every changing command. A trusted repository whose settings gain a filter between
    /// two commands, with no command of axGIT's in between: the next stage is refused, and runs nothing.
    /// </summary>
    [GitFact]
    public async Task A_changing_command_is_checked_before_it_runs()
    {
        var root = Repository("before");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.txt filter=x\n");
        var session = new GitSession(root, _ => false, _env);
        await session.CheckAsync();
        Assert.True(await session.TrustAsync(session.Scan));

        Git(root, "config", "filter.x.clean", Marker("between"));
        File.AppendAllText(Path.Combine(root, "a.txt"), "more\n");
        var refused = await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short);
        Assert.False(refused.Ok);
        Assert.True(refused.NotRun);
        Assert.False(session.Trusted);
        Assert.Empty(Ran());

        // The way it ran: the same stage as a session that does not check first.
        await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: _env);
        Assert.Equal(["between"], Ran());
    }

    // ---- L2: the check fails closed ----

    /// <summary>L2: a check that throws leaves the repository not trusted, its scan a problem; until then the trust of the check before stayed.</summary>
    [GitFact]
    public async Task A_check_that_fails_leaves_nothing_trusted()
    {
        var root = Repository("closed");
        var session = new GitSession(root, _ => true, _env);
        await session.CheckAsync();
        Assert.True(session.Trusted);
        // A settings file that changed, so that the parser must run (an unchanged one's settings are kept).
        Git(root, "config", "user.name", "changed");
        var program = GitRunner.Program;
        GitRunner.Program = Path.Combine(_dir.Path, "no-git-here.exe");
        try
        {
            await Assert.ThrowsAsync<GitNotFoundException>(() => session.CheckAsync());
        }
        finally
        {
            GitRunner.Program = program;
        }

        Assert.False(session.Trusted);
        Assert.True(session.Closed);
        Assert.NotNull(session.Refused);
    }

    // ---- L1: no redirect, whatever the settings of an address say ----

    /// <summary>
    /// L1: a repository's setting for one address (http.&lt;address&gt;.followRedirects=true) is more particular
    /// than axGIT's general one, and git took it. A fetch, pull or push now names every address it reaches with
    /// redirects off. Read with git config --get-urlmatch under the same settings.
    /// </summary>
    [GitFact]
    public async Task A_setting_for_one_address_does_not_turn_redirects_on()
    {
        var root = Repository("redirects");
        const string Address = "https://server.example.invalid/a/b.git";
        Git(root, "remote", "add", "origin", Address);
        Git(root, "config", $"http.{Address}.followRedirects", "true");
        // An address with a user, written in two pieces so that it is no mail address to the privacy check.
        var withUser = "https://user" + "@" + "server.example.invalid";
        Git(root, "config", $"http.{withUser}/.followRedirects", "true");
        var session = new GitSession(root, _ => true, _env);
        await session.CheckAsync();
        var reach = await GitReach.ReadAsync(async arguments => await session.RunAsync(arguments, GitRunner.Short), push: false, "main", null);
        Assert.NotNull(reach);
        Assert.Null(reach.Problem);
        var asked = await GitRunner.RunAsync(root, ["config", "--get-urlmatch", "http.followRedirects", Address], GitRunner.Short, overrides: GitReach.NoRedirects(reach), environment: _env);
        Assert.Equal("false", asked.Output.Trim());

        // With a user in the address, the setting for that user is more particular still.
        Git(root, "remote", "set-url", "origin", withUser + "/a/b.git");
        reach = await GitReach.ReadAsync(async arguments => await session.RunAsync(arguments, GitRunner.Short), push: true, "main", null);
        asked = await GitRunner.RunAsync(root, ["config", "--get-urlmatch", "http.followRedirects", withUser + "/a/b.git"], GitRunner.Short, overrides: GitReach.NoRedirects(reach!), environment: _env);
        Assert.Equal("false", asked.Output.Trim());

        // The way it ran: the command's general setting alone.
        var general = await GitRunner.RunAsync(root, ["config", "--get-urlmatch", "http.followRedirects", Address], GitRunner.Short, environment: _env);
        Assert.Equal("true", general.Output.Trim());
    }

    /// <summary>L1: each http and https address named whole, its user kept and its password left out, an equals sign kept (the setting goes through the environment); other transports have no redirect.</summary>
    [Fact]
    public void Every_http_address_is_named_with_redirects_off_and_without_a_password()
    {
        var at = "@";
        var reach = new Reach("origin", [], [], null, Addresses: [$"https://user:secret{at}h.example.invalid/a=b.git", "ssh://h.example.invalid/x", "http://g.example.invalid:8080/y", "http://g.example.invalid:8080/y"]);
        Assert.Equal(
            [new GitSetting($"http.https://user{at}h.example.invalid/a=b.git.followRedirects", "false"), new GitSetting("http.http://g.example.invalid:8080/y.followRedirects", "false")],
            GitReach.NoRedirects(reach));
        Assert.Empty(GitReach.NoRedirects(new Reach("origin", [], [], null)));
    }

    // ---- L5: an included hooks folder; a relative include of the work tree's file; an old trust ----

    /// <summary>L5: core.hooksPath set in an included file is the hooks folder git uses: its hooks are risks, and a push with Git LFS's pre-push hook there does not start.</summary>
    [GitFact]
    public async Task A_hooks_folder_of_an_included_file_is_read()
    {
        var root = Repository("included-hooks");
        var hooks = Path.Combine(_dir.Path, "included-hooks-folder");
        Directory.CreateDirectory(hooks);
        File.WriteAllText(Path.Combine(hooks, "pre-push"), "#!/bin/sh\ncommand -v git-lfs >/dev/null 2>&1 || exit 2\ngit lfs pre-push \"$@\"\n");
        var included = _dir.File("hooks.cfg", $"[core]\n\thooksPath = {hooks.Replace('\\', '/')}\n");
        Git(root, "config", "include.path", included.Replace('\\', '/'));
        var session = new GitSession(root, _ => true, _env);
        var scan = await session.CheckAsync();
        Assert.Contains("hook pre-push", scan.Risks);
        Assert.NotNull(session.LfsPushRefused());
    }

    /// <summary>L5: a relative include in a linked work tree's own settings file is read where git reads it, beside that file in the work tree's git folder.</summary>
    [GitFact]
    public async Task A_relative_include_of_the_work_trees_settings_is_read_beside_it()
    {
        var root = Repository("linked-main");
        var second = Path.Combine(_dir.Path, "linked-second");
        Git(root, "worktree", "add", "-q", second, "-b", "second");
        Git(root, "config", "extensions.worktreeConfig", "true");
        Git(second, "config", "--worktree", "include.path", "extra.cfg");
        var beside = Path.Combine(root, ".git", "worktrees", "linked-second", "extra.cfg");
        File.WriteAllText(beside, "[user]\n\tname = one\n");
        var first = (await GitTrustScan.ReadAsync(second, default, _env)).Mark;
        File.WriteAllText(beside, "[user]\n\tname = two\n");
        Assert.NotEqual(first, (await GitTrustScan.ReadAsync(second, default, _env)).Mark);
    }

    // ---- P4: the repositories inside are those git enters ----

    /// <summary>
    /// P4 (tried with git 2.53 on every command axGIT has): git enters a repository inside only where the index holds
    /// it as a gitlink (a submodule, or a repository Stage all added); an untracked one and one in an ignored folder
    /// it never enters. So those are found from the index and .gitmodules, and the untracked and ignored ones are not
    /// asked about; once Stage all has made one a gitlink, the check after it finds it.
    /// </summary>
    [GitFact]
    public async Task The_repositories_inside_are_those_the_index_holds()
    {
        var root = Repository("inside");
        File.WriteAllText(Path.Combine(root, ".gitignore"), "ignored/\n");
        foreach (var folder in new[] { "vendor", Path.Combine("ignored", "tool") })
        {
            var nested = Repository(Path.Combine("inside", folder));
            Git(nested, "config", "core.sshCommand", "never-run");
        }

        var session = new GitSession(root, _ => false, _env);
        var scan = await session.CheckAsync();
        Assert.Empty(scan.Risks);
        Assert.True(await session.TrustAsync(scan));

        Assert.True((await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short)).Ok);
        Assert.Equal(["submodule vendor: core.sshcommand"], session.Scan.Risks);
        Assert.False(session.Trusted);
    }

    /// <summary>P4: an index whose format axGIT does not read is a risk named as such, never a repository with nothing inside.</summary>
    [GitFact]
    public async Task An_index_axgit_does_not_read_is_a_risk()
    {
        var root = Repository("odd-index");
        var index = Path.Combine(root, ".git", "index");
        var bytes = File.ReadAllBytes(index);
        bytes[7] = 9;
        File.WriteAllBytes(index, bytes);
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Contains(GitTrust.IndexNotRead, scan.Risks);
    }
}
