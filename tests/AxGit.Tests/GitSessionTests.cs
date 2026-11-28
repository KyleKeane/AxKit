using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The review's third and fourth passes (docs/axGIT/SPEC.md AG-1.6, AG-1.6b, AG-5.4, AG-5.4c), against git itself,
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

    // ---- The second line: every program name set aside, every command (T4's settings; H1) ----

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
    /// The test of the second line (the review's third pass, H1 of the fourth, L3 and L7 of the fifth): every name of
    /// every kind of <see cref="GitTrust.ProgramNamesTable"/>, each in a clone of its own with a submodule, set in the
    /// clone and in the submodule to a program in the clone's work tree that leaves a mark (core.hooksPath to a folder
    /// of such hooks there), with the clone's own core.unsetenvvars clearing the variables axGIT's settings travel in;
    /// every command axGIT has, through a trusted <see cref="GitSession"/> exactly as the window calls it: the reads
    /// as they are (they may run the program: trusted), the changing commands with what the check found in the work
    /// tree set aside (T4). No changing command leaves a mark, and every one succeeds but a signed commit, whose
    /// program is set aside, and a fetch, pull or push whose remote's helper (remote.name.vcs) is set to nothing,
    /// which reaches nothing; a hook of the settings (hook.x.command, git 2.54 and later) is set aside by its event
    /// and its switch, under which the commit succeeds (git 2.53 does not know such hooks, and on it this case runs
    /// nothing either way). The half that shows the test can fail: three of the names, run with nothing set aside,
    /// leave their marks.
    /// </summary>
    [GitFact]
    public async Task No_program_set_aside_runs_in_any_command_also_in_a_submodule()
    {
        var origin = Origin("origin");
        var library = Origin("library");
        var names = GitTrust.ProgramNamesTable.SelectMany(kind => kind.Examples).ToList();
        var marked = new List<string>();
        var failed = new List<string>();

        async Task<string[]> OneAsync(string name, int number, bool aside)
        {
            // Each clone with a bare origin of its own, which its push goes to.
            var root = Path.Combine(_dir.Path, $"{(aside ? "n" : "t")}{number}");
            var bare = root + ".git";
            Git(_dir.Path, "clone", "-q", "--bare", origin, bare);
            Git(_dir.Path, "clone", "-q", bare, root);
            Git(root, "submodule", "--quiet", "add", library, "libs/zip");
            Git(root, "commit", "-q", "-m", "a submodule");
            Git(root, "push", "-q", "origin", "main");
            Git(root, "config", "core.unsetenvvars", "GIT_CONFIG_COUNT,GIT_CONFIG_PARAMETERS");
            var module = Path.Combine(root, ".git", "modules", "libs", "zip");
            var marks = MarksOf(root);
            // The program lies in the clone's work tree, as T4 sets aside.
            var script = Path.Combine(root, "tools", "marker.sh");
            Directory.CreateDirectory(Path.GetDirectoryName(script)!);
            File.WriteAllText(script, "#!/bin/sh\necho \"$1\" >> \"$2\"\ncat\n");
            var program = $"sh \"{script.Replace('\\', '/')}\" n{number} \"{marks.Replace('\\', '/')}\"";
            foreach (var place in new[] { root, module })
            {
                if (name.Equals("core.hooksPath", StringComparison.OrdinalIgnoreCase))
                {
                    var folder = Path.Combine(root, "tools", "hooks");
                    foreach (var hook in new[] { "pre-commit", "post-commit", "post-checkout", "post-index-change", "reference-transaction" })
                    {
                        MarkerFile(Path.Combine(folder, hook), marks, $"n{number}");
                    }

                    Git(place, "config", name, folder.Replace('\\', '/'));
                }
                else
                {
                    Git(place, "config", name, program);
                }

                foreach (var (companion, value) in Companions(name))
                {
                    Git(place, "config", companion, value);
                }
            }

            Ran(marks);
            if (!aside)
            {
                // The way it ran: the same commands with nothing set aside.
                await GitTests.EveryCommandAsync(root, "origin", (arguments, _) => GitRunner.RunAsync(root, arguments, GitRunner.Short, environment: _env));
                return Ran(marks);
            }

            var session = Trusted(root);
            var scan = await session.CheckAsync();
            Assert.True(scan.InTree!.Any(setting => setting.EndsWith(name, StringComparison.OrdinalIgnoreCase)), $"{name}: not found in the work tree ({string.Join(", ", scan.InTree!)})");
            // A signed commit fails with its program set aside; a remote's helper set to nothing is the helper
            // "remote-", which git does not find, so that remote is reached by nothing.
            var signing = Companions(name).Any(companion => companion.Name == "commit.gpgSign");
            var helper = name.EndsWith(".vcs", StringComparison.OrdinalIgnoreCase);
            var changed = new List<string>();
            await GitTests.EveryCommandAsync(root, "origin", async (arguments, change) =>
            {
                Ran(marks);
                var result = change ? await session.ChangeAsync(arguments, GitRunner.Short) : await session.RunAsync(arguments, GitRunner.Short);
                if (!change)
                {
                    return result;
                }

                if (Ran(marks) is { Length: > 0 } left)
                {
                    changed.Add($"git {arguments[0]}: {string.Join(", ", left)}");
                }

                if (!result.Ok && !(signing && arguments[0] == "commit") && !(helper && arguments[0] is "fetch" or "pull" or "push"))
                {
                    lock (failed)
                    {
                        failed.Add($"{name}: git {string.Join(' ', arguments)}: {result.FirstLine}");
                    }
                }

                return result;
            });

            return [.. changed];
        }

        // Eight at a time, each in its own repository with its own marks.
        foreach (var some in names.Select((name, number) => (name, number)).Chunk(8))
        {
            var found = await Task.WhenAll(some.Select(one => OneAsync(one.name, one.number, aside: true)));
            for (var at = 0; at < some.Length; at++)
            {
                if (found[at].Length > 0)
                {
                    marked.Add($"{some[at].name}: {string.Join(", ", found[at])}");
                }
            }
        }

        Assert.True(marked.Count == 0, "Programs set aside ran: " + string.Join("; ", marked));
        Assert.True(failed.Count == 0, "Commands that failed with a program set aside: " + string.Join("; ", failed));

        // The way it ran: with nothing set aside, the same commands leave the marks of a filter's two sides and of the hooks.
        var plainMarks = await Task.WhenAll(new[] { "filter.x.clean", "filter.x.smudge", "core.hooksPath" }.Select(name => OneAsync(name, names.IndexOf(name), aside: false)));
        Assert.All(plainMarks, found => Assert.NotEmpty(found));
    }

    // ---- T1: nothing runs before trust ----

    /// <summary>
    /// T1 (the review's fourth pass): a repository that holds a risk the user has not trusted runs nothing: every
    /// command is refused with the reason, fetch, pull and push among them, and so no git process starts in it; a
    /// repository with no risk is asked about too (G1); the user's trust for the mark the check found lets it run, and a
    /// trust for another mark does not.
    /// </summary>
    [GitFact]
    public async Task A_repository_that_is_not_trusted_runs_nothing()
    {
        var root = Origin("untrusted");
        var marks = MarksOf(root);
        Git(root, "config", "filter.x.clean", Marker(marks, "filter"));
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.False(session.Trusted);
        Assert.Equal("axGIT runs nothing in this repository until you trust it.", session.Refused);
        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        foreach (var arguments in new[] { GitStatus.Arguments, GitTexts.StageAllArguments, GitTexts.FetchArguments("origin") })
        {
            var result = await session.ChangeAsync(arguments, GitRunner.Short);
            Assert.False(result.Ok);
            Assert.Equal(session.Refused, result.Error);
        }

        Assert.Empty(Ran(marks));
        // Trusted for another mark: still nothing; for the one found: git runs.
        Assert.False(await session.TrustAsync(GitTrust.Mark(["something else"])));
        Assert.False(session.Trusted);
        Assert.True(await session.TrustAsync(session.Scan.Mark));
        Assert.True((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);

        // A repository with no risk is asked about too (G1 of the sixth pass), and runs once trusted.
        var plain = Untrusted(Origin("plain"));
        await plain.CheckAsync();
        Assert.Empty(plain.Scan.Risks);
        Assert.False(plain.Trusted);
        Assert.NotNull(plain.Refused);
        Assert.True(await plain.TrustAsync(plain.Scan.Mark));
        Assert.Null(plain.Refused);

        // The way it ran: the stage with nothing set aside runs the filter.
        await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: _env);
        Assert.Equal(["filter"], Ran(marks));
    }

    /// <summary>
    /// H1, D2: a partial clone fetched a missing object by itself on its first status, running the remote's upload
    /// program; git takes the first remote.origin.uploadpack, the repository's. Every run forbids the lazy fetch,
    /// also in a trusted repository: the status fails with git's own words, and nothing runs.
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

        var session = Trusted(root);
        await session.CheckAsync();
        Assert.True(session.Trusted);
        var status = await session.RunAsync(GitStatus.Arguments, GitRunner.Short);
        await session.RunAsync(GitLog.Arguments, GitRunner.Short);
        Assert.False(status.Ok);
        Assert.Empty(Ran(marks));

        // The way it ran: the same status with the lazy fetch allowed.
        Plain(root, null, [.. GitStatus.Arguments]);
        Assert.Equal(["upload-pack"], Ran(marks));
    }

    // ---- D8: the check after every command, in one place ----

    /// <summary>
    /// D8, L7: the session checks again after a command that changes the repository, also after one that failed,
    /// and what it found is in force for the next command, without the test making the check itself; since the
    /// fifth pass (P3) it checks before it too. Here the command fails, and the ground moves under a repository that
    /// held no risk (a filter appears in its settings) after the check that followed it: since the fourth pass
    /// nothing runs after it until the user trusts what is there now.
    /// </summary>
    [GitFact]
    public async Task After_a_failed_command_the_check_is_made_and_what_it_found_is_in_force()
    {
        var root = Origin("moving");
        var marks = MarksOf(root);
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.True(await session.TrustAsync(session.Scan.Mark));
        var checks = session.Checks;

        var failed = await session.ChangeAsync(["switch", "no-such-branch"], GitRunner.Short);
        Assert.False(failed.Ok);
        Assert.False(failed.NotRun);
        Assert.Equal(checks + 2, session.Checks);
        Assert.True(session.Trusted);

        Git(root, "config", "filter.x.clean", Marker(marks, "moved-filter"));

        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        Assert.True((await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short)).NotRun);
        Assert.False(session.Trusted);
        Assert.Empty(Ran(marks));

        // The way it ran: the next command as before the failed one.
        await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: _env);
        Assert.Equal(["moved-filter"], Ran(marks));
    }

    // ---- T4: trusted means the same files ----

    /// <summary>
    /// M3, T4 (tried with git 2.53): in a trusted repository whose hooks folder is in its work tree, a switch brought
    /// a post-checkout hook and ran it, before the check after it could see the new hook. A changing command of such
    /// a repository runs with no hooks; the check after it finds the new hook, and the repository is not trusted
    /// for it.
    /// </summary>
    [GitFact]
    public async Task A_hook_a_switch_brings_into_the_work_tree_is_not_run()
    {
        var root = Origin("m3");
        var marks = MarksOf(root);
        Directory.CreateDirectory(Path.Combine(root, ".githooks"));
        File.WriteAllText(Path.Combine(root, ".githooks", "pre-commit.sample"), "a sample, which is no hook\n");
        Git(root, "add", ".githooks");
        Git(root, "commit", "-q", "-m", "hooks folder");
        Git(root, "switch", "-q", "-c", "other");
        MarkerFile(Path.Combine(root, ".githooks", "post-checkout"), marks, "brought-hook");
        Git(root, "add", ".githooks/post-checkout");
        Git(root, "commit", "-q", "-m", "a hook");
        Git(root, "switch", "-q", "main");
        Git(root, "config", "core.hooksPath", ".githooks");
        Ran(marks);

        string? trusted = null;
        var session = new GitSession(root, mark => mark == trusted, _env);
        var scan = await session.CheckAsync();
        Assert.Equal(["core.hookspath"], scan.Risks);
        Assert.Equal(["core.hookspath"], scan.InTree!);
        trusted = scan.Mark;
        Assert.True(await session.TrustAsync(scan.Mark));

        Assert.True((await session.ChangeAsync(["switch", "other"], GitRunner.Short)).Ok);
        Assert.Empty(Ran(marks));
        Assert.Contains("hook post-checkout", session.Scan.Risks);
        Assert.False(session.Trusted);

        // The way it ran: the switch of a trusted repository with nothing set aside.
        Git(root, "switch", "-q", "main");
        Ran(marks);
        await GitRunner.RunAsync(root, ["switch", "other"], GitRunner.Short, environment: _env);
        Assert.Equal(["brought-hook"], Ran(marks));
    }

    /// <summary>T4: the mark holds each hook's content, each file a setting names, an included settings file's content and the hooks folder's listing: a change of any of them is asked about again.</summary>
    [GitFact]
    public async Task A_change_of_a_hook_or_a_file_a_setting_names_changes_the_mark()
    {
        var root = Origin("marks");
        var hook = Path.Combine(root, ".git", "hooks", "pre-commit");
        File.WriteAllText(hook, "#!/bin/sh\necho one\n");
        var program = _dir.File("tool.sh", "#!/bin/sh\n");
        Git(root, "config", "core.sshCommand", $"sh {program.Replace('\\', '/')}");
        var included = _dir.File("extra.cfg", "[user]\n\tname = one\n");
        Git(root, "config", "include.path", included.Replace('\\', '/'));

        async Task<string> MarkAsync() => (await GitTrustScan.ReadAsync(root, default, _env)).Mark;
        var first = await MarkAsync();
        Assert.Equal(first, await MarkAsync());

        File.WriteAllText(hook, "#!/bin/sh\necho two\n");
        var second = await MarkAsync();
        Assert.NotEqual(first, second);

        File.WriteAllText(program, "#!/bin/sh\necho changed\n");
        var third = await MarkAsync();
        Assert.NotEqual(second, third);

        File.WriteAllText(included, "[user]\n\tname = two\n");
        var fourth = await MarkAsync();
        Assert.NotEqual(third, fourth);

        // A sample more changes the folder's listing, though a sample is no hook.
        File.WriteAllText(Path.Combine(root, ".git", "hooks", "another.sample"), "a sample\n");
        Assert.NotEqual(fourth, await MarkAsync());

        // A value of a setting that is not harmless is marked too.
        var fifth = await MarkAsync();
        Git(root, "config", "core.sshCommand", "ssh -v");
        Assert.NotEqual(fifth, await MarkAsync());
    }

    /// <summary>T4: a program in the work tree (a filter's, tracked) is set aside on a changing command of a trusted repository, and runs on its reads, whose check made after every change saw it as it is.</summary>
    [GitFact]
    public async Task A_program_in_the_work_tree_is_set_aside_on_a_changing_command()
    {
        var root = Origin("tree-program");
        var marks = MarksOf(root);
        var tool = Path.Combine(root, "tools", "clean.sh");
        Directory.CreateDirectory(Path.GetDirectoryName(tool)!);
        File.WriteAllText(tool, $"#!/bin/sh\necho tree-filter >> \"{marks.Replace('\\', '/')}\"\ncat\n");
        Git(root, "config", "filter.x.clean", "sh ./tools/clean.sh");
        var session = new GitSession(root, _ => true, _env);
        var scan = await session.CheckAsync();
        Assert.Equal(["filter.x.clean"], scan.InTree!);

        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        Assert.True((await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short)).Ok);
        Assert.Empty(Ran(marks));

        // The way it ran: the stage with nothing set aside.
        File.AppendAllText(Path.Combine(root, "b.txt"), "changed\n");
        await GitRunner.RunAsync(root, GitTexts.StageAllArguments, GitRunner.Short, environment: _env);
        Assert.Equal(["tree-filter"], Ran(marks));
    }

    // ---- T2: a push goes to a server alone ----

    private async Task<Reach> ReachAsync(GitSession session, bool push)
    {
        var status = GitStatus.Parse((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Output);
        var reach = await GitReach.ReadAsync(async arguments => await session.RunAsync(arguments, GitRunner.Short), push, status.Branch, status.Upstream);
        Assert.NotNull(reach);
        return reach;
    }

    /// <summary>
    /// H2, T2 (tried with git 2.53): with the address ./vendor/mirror, a plain folder of that name beside a bare
    /// repository vendor/mirror.git, the reads before the push (git -C vendor/mirror) read the repository above,
    /// which held nothing, and the push entered vendor/mirror.git and ran its receive hook. Since the fourth pass a
    /// push to any folder is refused before it starts: that one, a bare repository in the work tree, the
    /// repository itself, a path elsewhere on this computer and a share. A fetch from a folder stays.
    /// </summary>
    [GitFact]
    public async Task A_push_to_a_folder_is_refused()
    {
        var root = Origin("pusher");
        var marks = MarksOf(root);
        Directory.CreateDirectory(Path.Combine(root, "vendor", "mirror"));
        Git(root, "init", "-q", "--bare", "vendor/mirror.git");
        MarkerFile(Path.Combine(root, "vendor", "mirror.git", "hooks", "pre-receive"), marks, "target-hook");
        Git(root, "remote", "add", "origin", "./vendor/mirror");
        var session = Trusted(root);
        await session.CheckAsync();

        var reach = await ReachAsync(session, push: true);
        Assert.Null(reach.Problem);
        Assert.Equal(["./vendor/mirror"], reach.Folders);
        Assert.Equal("This remote is a folder; push to it in a terminal.", GitReach.PushRefused(reach));
        Assert.Empty(Ran(marks));

        foreach (var address in new[] { Path.Combine(_dir.Path, "elsewhere.git"), "//localhost/C$/repo.git", @"\\server.invalid\share\repo.git", "file:///C:/repo.git" })
        {
            Git(root, "remote", "set-url", "origin", address);
            Assert.NotNull(GitReach.PushRefused(await ReachAsync(session, push: true)));
        }

        Git(root, "config", "branch.main.remote", ".");
        Git(root, "config", "branch.main.merge", "refs/heads/main");
        Assert.NotNull(GitReach.PushRefused(await ReachAsync(session, push: true)));

        // A server is pushed to.
        Git(root, "config", "--unset", "branch.main.remote");
        Git(root, "remote", "set-url", "origin", "https://server.example.invalid/a.git");
        Assert.Null(GitReach.PushRefused(await ReachAsync(session, push: true)));

        // The way it ran: the push itself into ./vendor/mirror.
        Git(root, "remote", "set-url", "origin", "./vendor/mirror");
        await session.ChangeAsync(GitTexts.PushArguments("origin", null, "main"), GitRunner.Short);
        Assert.Equal(["target-hook"], Ran(marks));
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
        Assert.True(await session.TrustAsync(session.Scan.Mark));
        Assert.True((await session.ChangeAsync(GitTexts.FetchArguments("origin"), GitRunner.Short)).Ok);
        Git(root, "config", "branch.main.remote", "origin");
        Git(root, "config", "branch.main.merge", "refs/heads/main");
        Assert.True((await session.ChangeAsync(GitTexts.PullArguments("origin", "refs/heads/main")!, GitRunner.Short)).Ok, "pull");
        Assert.Empty(Ran(marks));
    }

    // ---- M3 of the third pass with T3, M4, D7 ----

    /// <summary>A fetch line that writes outside refs/remotes is a risk; and since T3 axGIT's fetch names its own line, so that a trusted repository with one keeps its local branches. With axGIT's --prune it deleted them.</summary>
    [GitFact]
    public async Task A_fetch_that_writes_into_the_local_branches_is_a_risk_and_axgits_fetch_obeys_it_not()
    {
        Git(_dir.Path, "init", "-q", "--bare", "m3.git");
        var root = Origin("m3-fetch");
        Git(root, "remote", "add", "origin", Path.Combine(_dir.Path, "m3.git"));
        Git(root, "push", "-q", "origin", "main");
        Git(root, "branch", "keep");
        Git(root, "config", "--replace-all", "remote.origin.fetch", "+refs/heads/*:refs/heads/*");
        var session = Untrusted(root);
        await session.CheckAsync();
        Assert.Contains("remote.origin.fetch writes outside refs/remotes", session.Scan.Risks);
        Assert.NotNull(session.Refused);

        Git(root, "checkout", "-q", "--detach");
        Assert.True((await GitRunner.RunAsync(root, GitTexts.FetchArguments("origin"), GitRunner.Short, environment: _env)).Ok);
        Assert.Contains("keep", Git(root, "branch", "--list"));

        // The way it ran: the fetch of the third pass, from a detached HEAD.
        await GitRunner.RunAsync(root, ["fetch", "--prune", "--no-recurse-submodules", "--no-all", "--upload-pack=git-upload-pack", "--", "origin"], GitRunner.Short, environment: _env);
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
        Assert.True(await session.TrustAsync(session.Scan.Mark));
        Assert.Null(session.LfsPushRefused());
        var trace = new Dictionary<string, string>(_env) { ["GIT_TRACE"] = "1" };
        var switched = await GitRunner.RunAsync(root, ["switch", "other"], GitRunner.Short, environment: trace);
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

    // ---- L3 of the third pass, D9 ----

    /// <summary>L3: the hooks of a repository inside are those of the folder its core.hooksPath names, relative to its work tree, where git runs them.</summary>
    [GitFact]
    public async Task The_hooks_of_a_repository_inside_are_found_where_git_runs_them()
    {
        var library = Origin("library");
        var root = Origin("top");
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Git(root, "commit", "-q", "-m", "a submodule");
        var module = Path.Combine(root, ".git", "modules", "libs", "zip");
        Git(module, "config", "core.hooksPath", "myhooks");
        MarkerFile(Path.Combine(root, "libs", "zip", "myhooks", "post-checkout"), MarksOf(root), "inside-hook");
        var scan = await GitTrustScan.ReadAsync(root, default, _env);
        Assert.Contains("submodule libs/zip: core.hookspath", scan.Risks);
        Assert.Contains("submodule libs/zip: hook post-checkout", scan.Risks);
        Assert.Contains("submodule libs/zip: core.hookspath", scan.InTree!);
    }

    /// <summary>D9, M2: a repository whose settings include a file on another computer names it as a risk, and nothing runs in it until it is trusted.</summary>
    [GitFact]
    public async Task A_repository_that_points_at_another_computer_runs_nothing_until_trusted()
    {
        var root = Origin("away");
        Git(root, "config", "include.path", "//elsewhere.invalid/share/x.cfg");
        var session = Untrusted(root);
        var scan = await session.CheckAsync();
        Assert.Contains("include.path names another computer, elsewhere.invalid", scan.Risks);
        Assert.Contains("include.path", scan.Risks);
        Assert.False(session.Closed);
        Assert.False((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);
    }
}
