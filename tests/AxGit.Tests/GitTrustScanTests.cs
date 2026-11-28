using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using AxKit.Core;
using Xunit.Abstractions;

namespace AxGit.Tests;

/// <summary>
/// The reads that say what a repository would run and which servers a fetch, pull or push reaches
/// (docs/axGIT/SPEC.md AG-1.6, AG-5.4a; the reviews of 2026-11-27 and 2026-11-28), against git itself, in
/// repositories the test makes. A program the repository names is a script that writes a line into a file of the
/// test's, and the test looks whether the line is there; each also runs once the way that did run the script, so
/// that the test is known to be able to fail. The addresses are of the reserved name "invalid", and every run goes
/// through a proxy on a closed port of this computer (<see cref="GitTests.Environment"/>), so that nothing leaves
/// it even where a resolver answers for "invalid"; a share is this computer's own, //localhost/C$. Where git is not
/// installed the tests are said as skipped (<see cref="GitFactAttribute"/>). In one collection with the tests that
/// put a fake in git's place, so that the two never run at once.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed partial class GitTrustScanTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-scan");
    private readonly string _marks;
    private readonly ITestOutputHelper _output;

    /// <summary>The variables of every git run of this class's (<see cref="GitTests.Environment"/>), given per run: nothing is set for the process.</summary>
    private readonly Dictionary<string, string> _env;

    public GitTrustScanTests(ITestOutputHelper output)
    {
        _output = output;
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
        File.WriteAllText(Path.Combine(folder, "b.txt"), "two\n");
        // Written an hour back, so that git never takes them for racily clean: a split index then writes such an
        // entry again with an empty name, which the strict reading refuses, and the index test failed one run in
        // three (2026-11-28).
        File.SetLastWriteTimeUtc(Path.Combine(folder, "a.txt"), DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(Path.Combine(folder, "b.txt"), DateTime.UtcNow.AddHours(-1));
        Git(folder, "add", "a.txt", "b.txt");
        Git(folder, "commit", "-q", "-m", "one");
        return folder;
    }

    private Task<TrustScan> ScanAsync(string root) => GitTrustScan.ReadAsync(root, default, _env);

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

    /// <summary>A command of the window's, as a session of a trusted repository runs it.</summary>
    private Task<GitResult> AxGit(string root, IReadOnlyList<string> arguments, IReadOnlyList<GitSetting>? overrides = null) =>
        GitRunner.RunAsync(root, arguments, GitRunner.Short, overrides: overrides, environment: _env);

    [GitFact]
    public async Task A_plain_repository_holds_no_risk()
    {
        var scan = await ScanAsync(Repository("plain"));
        Assert.Null(scan.Problem);
        Assert.Empty(scan.Risks);
        Assert.Equal(64, scan.Mark.Length);
        Assert.Empty(scan.InTree!);
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

        var scan = await ScanAsync(root);
        Assert.Null(scan.Problem);
        Assert.Empty(scan.Risks);
    }

    /// <summary>The hooks are those of the folder git uses: the repository's own, or the one core.hooksPath names, which is a risk by its name too and, in the work tree, is noted for T4.</summary>
    [GitFact]
    public async Task The_hooks_of_the_folder_git_uses_are_found()
    {
        var root = Repository("hooks");
        File.WriteAllText(Path.Combine(root, ".git", "hooks", "post-checkout"), "#!/bin/sh\n");
        var first = await ScanAsync(root);
        Assert.Equal(["hook post-checkout"], first.Risks);
        Assert.Empty(first.InTree!);
        Assert.Equal([Path.Combine(root, ".git", "hooks")], first.Hooks!);
        Assert.Equal(Path.Combine(root, ".git"), first.GitDir);

        Directory.CreateDirectory(Path.Combine(root, "tools", "hooks"));
        File.WriteAllText(Path.Combine(root, "tools", "hooks", "pre-push"), "#!/bin/sh\n");
        Git(root, "config", "core.hooksPath", "tools/hooks");
        var second = await ScanAsync(root);
        Assert.Equal(["core.hookspath", "hook pre-push"], second.Risks);
        Assert.Equal(["core.hookspath"], second.InTree!);
    }

    [GitFact]
    public async Task The_work_trees_own_settings_file_is_read()
    {
        var root = Repository("worktree");
        Git(root, "config", "extensions.worktreeConfig", "true");
        Git(root, "config", "--worktree", "core.sshCommand", "never-run");
        Git(root, "config", "gpg.ssh.defaultKeyCommand", "never-run");
        var scan = await ScanAsync(root);
        Assert.Null(scan.Problem);
        Assert.Equal(["core.sshcommand", "gpg.ssh.defaultkeycommand"], scan.Risks.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// A repository inside another that git enters, kept in its work tree or under .git/modules, also with its work
    /// tree taken away: each is found and read. One in an ignored folder that git does not track git never enters
    /// (P4 of the fifth pass, tried with git 2.53 on every command axGIT has), and is not asked about.
    /// </summary>
    [GitFact]
    public async Task What_a_repository_inside_would_run_is_found_wherever_git_keeps_it()
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
        // Not tracked at all, in an ignored folder: git never enters it.
        File.WriteAllText(Path.Combine(root, ".gitignore"), "ignored/\n");
        Git(Directory.CreateDirectory(Path.Combine(root, "ignored", "tool")).FullName, "init", "-q");
        Git(Path.Combine(root, "ignored", "tool"), "config", "core.pager", "never-run");

        var scan = await ScanAsync(root);
        Assert.Null(scan.Problem);
        Assert.Equal(
            ["submodule libs/zip: filter.x.smudge", "submodule vendor: core.fsmonitor", "submodule vendor: hook post-checkout"],
            scan.Risks.Order(StringComparer.Ordinal));

        // The submodule's work tree taken away, its repository stays under .git/modules, and is still read.
        Git(root, "submodule", "--quiet", "deinit", "-f", "libs/zip");
        Assert.Contains("submodule libs/zip: filter.x.smudge", (await ScanAsync(root)).Risks);
    }

    /// <summary>A repository inside another inside another is followed through each one's index, each named from the repository opened; the same two untracked are not entered by git and not asked about.</summary>
    [GitFact]
    public async Task A_repository_inside_one_that_is_inside_is_found()
    {
        var root = Repository("levels");
        var outer = Repository(Path.Combine("levels", "outer"));
        var inner = Repository(Path.Combine("levels", "outer", "inner"));
        Git(inner, "config", "core.sshCommand", "never-run");
        Assert.Empty((await ScanAsync(root)).Risks);

        Git(outer, "add", "inner");
        Git(outer, "commit", "-q", "-m", "inner");
        Git(root, "add", "outer");
        var scan = await ScanAsync(root);
        Assert.Null(scan.Problem);
        Assert.Equal(["submodule outer/inner: core.sshcommand"], scan.Risks);
    }

    /// <summary>
    /// P4: the repositories inside are those the index holds as gitlinks, read from the index in every form git
    /// writes (versions 2, 3 and 4, a split index with its shared index) and from .gitmodules; a gitlink inside a
    /// submodule is followed into its index.
    /// </summary>
    [GitFact]
    public async Task The_gitlinks_of_every_form_of_index_are_found()
    {
        foreach (var form in new[] { "2", "3", "4", "split" })
        {
            var root = Repository("index-" + form);
            var vendor = Repository(Path.Combine("index-" + form, "vendor"));
            Git(vendor, "config", "core.sshCommand", "never-run");
            if (form == "split")
            {
                Git(root, "config", "core.splitIndex", "true");
                Git(root, "update-index", "--split-index");
            }
            else
            {
                Git(root, "update-index", "--index-version", form);
            }

            Git(root, "add", "vendor");
            if (form == "3")
            {
                // An entry with the extended flags of version 3: intent to add.
                File.WriteAllText(Path.Combine(root, "later.txt"), "later\n");
                Git(root, "add", "--intent-to-add", "later.txt");
            }

            var entries = GitIndex.Read(File.ReadAllBytes(Path.Combine(root, ".git", "index")), 20);
            Assert.True(entries.Known, form);
            Assert.Contains("submodule vendor: core.sshcommand", (await ScanAsync(root)).Risks);
            if (form == "split")
            {
                Assert.NotNull(entries.SharedIndex);
            }
        }

        // Named by .gitmodules alone, with no gitlink in the index.
        var modules = Repository("index-modules");
        var listed = Repository(Path.Combine("index-modules", "listed"));
        Git(listed, "config", "core.pager", "never-run");
        File.WriteAllText(Path.Combine(modules, ".gitmodules"), "[submodule \"listed\"]\n\tpath = listed\n\turl = ./listed\n");
        Assert.Contains("submodule listed: core.pager", (await ScanAsync(modules)).Risks);
    }

    [GitFact]
    public async Task A_folder_that_is_no_repository_is_a_problem_and_never_nothing_to_run()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, "not-one")).FullName;
        File.WriteAllText(Path.Combine(folder, ".git"), "gitdir: nowhere\n");
        var scan = await ScanAsync(folder);
        Assert.NotNull(scan.Problem);
        Assert.Empty(scan.Risks);
        Assert.NotNull((await ScanAsync(Directory.CreateDirectory(Path.Combine(_dir.Path, "no-git")).FullName)).Problem);
    }

    /// <summary>The root of a folder inside a repository is found by axKit's own code, as git finds it; a repository's own folder and a bare one have none.</summary>
    [GitFact]
    public void The_root_is_found_going_up_without_git()
    {
        var root = Repository("roots");
        var deep = Directory.CreateDirectory(Path.Combine(root, "src", "deep")).FullName;
        Assert.Equal(root, GitTrustScan.RootOf(deep));
        Assert.Equal(root, GitTrustScan.RootOf(Path.Combine(root, "a.txt")));
        Assert.Null(GitTrustScan.RootOf(Path.Combine(root, ".git", "hooks")));
        Git(_dir.Path, "init", "-q", "--bare", "bare.git");
        Assert.Null(GitTrustScan.RootOf(Path.Combine(_dir.Path, "bare.git")));
        // A submodule's work tree is a root of its own, by its .git file.
        var library = Repository("roots-library");
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Assert.Equal(Path.Combine(root, "libs", "zip"), GitTrustScan.RootOf(Path.Combine(root, "libs", "zip")));
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

    /// <summary>Root CLAUDE.md rule 3 (L2 of the fourth pass): nothing of the scan comes back to the caller's thread between its steps, the walk and the reads of files among them.</summary>
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

    // ---- H1, T1: the class, every name of git help --config, every command before trust ----

    /// <summary>The fixture of the names git knows, with the version of git it was made with on its first line (H1).</summary>
    private const string NamesFixture = "git/help-config.txt";

    /// <summary>The command that makes the fixture again, from the repository's folder.</summary>
    private const string RenewFixture = @"& { git version; git help --config } | Out-File -Encoding utf8 tests\fixtures\git\help-config.txt";

    /// <summary>The names of a list git help --config prints: a placeholder for a middle part (&lt;name&gt;, *) as "x"; a line that is no name (an include's condition, the closing words) left out.</summary>
    private static IReadOnlyList<string> Names(IEnumerable<string> lines) =>
        [.. lines.Select(line => line.Trim()).Where(line => Regex.IsMatch(line, @"^[A-Za-z0-9.<>*_-]+$") && line.Contains('.'))
            .Select(line => Regex.Replace(line, "<[^>]+>|\\*", "x")).Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// The names to set for the class test, from the fixture and the git installed: with the same version, every
    /// name the installed git knows must be in the fixture (a name the fixture lacks fails); with another version,
    /// the names both know, said loudly, and the test fails afterwards off CI alone, with the command that renews
    /// the fixture (GitHub's build runs a newer git, which must not turn it red).
    /// </summary>
    private (IReadOnlyList<string> Names, string? Differs) NamesToSet()
    {
        var fixture = File.ReadAllLines(Path.Combine(Fixtures.Folder(), NamesFixture.Replace('/', Path.DirectorySeparatorChar)));
        var fixtureVersion = fixture[0].Trim();
        var known = Names(fixture.Skip(1));
        var installedVersion = Git(_dir.Path, "version").Trim();
        var installed = Names(Git(_dir.Path, "help", "--config").Split('\n'));
        if (installedVersion == fixtureVersion)
        {
            var missing = installed.Except(known, StringComparer.Ordinal).ToList();
            Assert.True(missing.Count == 0, $"{installedVersion} knows names the fixture {NamesFixture} lacks: {string.Join(", ", missing)}. Renew it: {RenewFixture}");
            return (known, null);
        }

        var both = known.Intersect(installed, StringComparer.Ordinal).ToList();
        var differs = $"The fixture {NamesFixture} is of {fixtureVersion} and the git installed is {installedVersion}: the class test ran the {both.Count} names both know, of {known.Count} in the fixture and {installed.Count} installed. Renew the fixture on the development computer: {RenewFixture}";
        _output.WriteLine("LOUD: " + differs);
        Console.WriteLine("::warning title=axGIT's git fixture::" + differs);
        return (both, differs);
    }

    /// <summary>A settings file's text with each name set to the value <paramref name="value"/> gives it: a name whose variable git takes in no file (gitweb's with an underscore) left out.</summary>
    private static string SettingsText(IEnumerable<string> names, Func<string, string> value)
    {
        var text = new System.Text.StringBuilder();
        foreach (var name in names)
        {
            var first = name.IndexOf('.');
            var last = name.LastIndexOf('.');
            var variable = name[(last + 1)..];
            if (!Regex.IsMatch(variable, "^[A-Za-z][A-Za-z0-9-]*$"))
            {
                continue;
            }

            var header = last > first ? $"[{name[..first]} \"{name[(first + 1)..last]}\"]" : $"[{name[..first]}]";
            var written = value(name).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            text.Append(header).Append("\n\t").Append(variable).Append(" = ").Append(written).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>
    /// The test that closes H1's class and T1's (the review's fourth pass): every name of git help --config set to a
    /// program that leaves a mark, core.unsetenvvars to the variables axGIT's settings travel in, core.fsmonitor and
    /// core.hooksPath to programs, an include of a file that names more, in the repository opened, in its work
    /// tree's own file and in a submodule's: the check before trust runs git only as the parser of one file with no
    /// repository, each run of it succeeds and lists every name, no mark comes, every name not known to be harmless
    /// is a risk; and every command axGIT has is refused in the session, so that nothing runs. The half that shows
    /// the test can fail: a plain status in a console leaves the marks.
    /// </summary>
    [GitFact]
    public async Task Before_trust_no_git_command_runs_in_the_repository_and_the_parser_runs_nothing()
    {
        var (names, differs) = NamesToSet();
        var library = Repository("every-library");
        var root = Repository("every");
        Git(root, "submodule", "--quiet", "add", library, "libs/zip");
        Git(root, "commit", "-q", "-m", "a submodule");
        var program = MarkerFile(Path.Combine(_dir.Path, "program"), "program");
        var hooks = Path.Combine(_dir.Path, "every-hooks");
        foreach (var hook in new[] { "pre-commit", "post-checkout", "post-index-change", "reference-transaction" })
        {
            MarkerFile(Path.Combine(hooks, hook), "hook-" + hook);
        }

        var included = _dir.File("included.cfg", SettingsText(["core.fsmonitor", "filter.x.clean"], name => Marker("included-" + name)));
        string Value(string name) => name.ToLowerInvariant() switch
        {
            "core.unsetenvvars" => "GIT_CONFIG_COUNT,GIT_CONFIG_PARAMETERS",
            "core.fsmonitor" or "core.askpass" or "core.editor" or "core.sshcommand" => program,
            "core.hookspath" => hooks.Replace('\\', '/'),
            "include.path" or "includeif.x.path" => included.Replace('\\', '/'),
            _ => Marker(name),
        };

        var text = SettingsText(names, Value);
        var module = Path.Combine(root, ".git", "modules", "libs", "zip");
        File.AppendAllText(Path.Combine(root, ".git", "config"), text);
        File.WriteAllText(Path.Combine(root, ".git", "config.worktree"), text);
        File.AppendAllText(Path.Combine(module, "config"), text);
        File.WriteAllText(Path.Combine(root, ".gitattributes"), "*.txt filter=x diff=x merge=x\n");
        Ran();

        var record = new List<IReadOnlyList<string>>();
        GitTrustScan.Record = record;
        TrustScan scan;
        try
        {
            scan = await ScanAsync(root);
        }
        finally
        {
            GitTrustScan.Record = null;
        }

        Assert.Null(scan.Problem);
        Assert.Empty(Ran());
        // The only git runs: the parser, of one file each, with no repository (no -C), and each succeeded (no settings unread).
        Assert.NotEmpty(record);
        foreach (var run in record)
        {
            Assert.DoesNotContain("-C", run);
            var at = run.ToList().IndexOf("config");
            Assert.True(at > 0, string.Join(' ', run));
            Assert.Equal(GitTrust.ParserArguments(run[at + 2]), run.Skip(at));
        }

        Assert.DoesNotContain(scan.Risks, risk => risk.EndsWith(GitTrust.NotRead, StringComparison.Ordinal));
        var shapes = scan.Risks.Where(risk => !risk.StartsWith("submodule ", StringComparison.Ordinal)).Select(GitTrust.Shape).ToHashSet(StringComparer.Ordinal);
        var written = names.Where(name => Regex.IsMatch(name[(name.LastIndexOf('.') + 1)..], "^[A-Za-z][A-Za-z0-9-]*$")).ToList();
        var unnamed = written.Where(name => !GitTrust.IsInert(name) && !shapes.Contains(GitTrust.Shape(name))).ToList();
        Assert.True(unnamed.Count == 0, "Names set in the repository that are not risks: " + string.Join(", ", unnamed));
        Assert.Contains("submodule libs/zip: core.fsmonitor", scan.Risks);
        Assert.Contains("core.unsetenvvars", scan.Risks);

        // Every command axGIT has, in the session of a repository the user has not trusted: each refused, none run.
        var session = new GitSession(root, _ => false, _env);
        await session.CheckAsync();
        Assert.False(session.Trusted);
        var results = await GitTests.EveryCommandAsync(root, "origin", (arguments, change) => change ? session.ChangeAsync(arguments, GitRunner.Short) : session.RunAsync(arguments, GitRunner.Short));
        Assert.All(results, one => Assert.Equal("axGIT runs nothing in this repository until you trust it.", one.Result.Error));
        Assert.Empty(Ran());

        // The way it ran (H1): the status of the third pass at the opening, in a repository whose own
        // core.unsetenvvars clears the variables axGIT's settings travel in, with a submodule's file monitor (git
        // stops at the first value of the wrong kind in the repository above, so this one holds those two alone).
        var h1 = Repository("h1");
        var h1Library = Repository("h1-library");
        Git(h1, "submodule", "--quiet", "add", h1Library, "libs/zip");
        Git(h1, "config", "core.unsetenvvars", "GIT_CONFIG_COUNT,GIT_CONFIG_PARAMETERS");
        Git(Path.Combine(h1, ".git", "modules", "libs", "zip"), "config", "core.fsmonitor", program);
        File.AppendAllText(Path.Combine(h1, "libs", "zip", "a.txt"), "changed\n");
        string[] thirdPass = ["-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "maintenance.auto=false", "-c", "gc.auto=0"];
        var aside = new Dictionary<string, string> { ["GIT_CONFIG_COUNT"] = "1", ["GIT_CONFIG_KEY_0"] = "core.fsmonitor", ["GIT_CONFIG_VALUE_0"] = string.Empty };
        PlainWith(h1, aside, [.. thirdPass, .. GitStatus.Arguments]);
        Assert.Equal(["program"], Ran());
        // And the second line of the fourth pass: git's own default given on the command line, the same status sets the monitor aside.
        PlainWith(h1, aside, [.. thirdPass, "-c", "core.unsetenvvars=PERL5LIB", .. GitStatus.Arguments]);
        Assert.Empty(Ran());

        Assert.True(differs is null || System.Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is { Length: > 0 }, differs);
    }

    /// <summary>git as a console runs it, with more variables for this run.</summary>
    private ProcessResult PlainWith(string folder, IReadOnlyDictionary<string, string> more, IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, _env);
        GitTests.Put(start, more);
        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
    }

    // ---- What every run has, trusted or not ----

    /// <summary>
    /// The second pass, finding 2: gc.recentObjectsHook is run by git's automatic maintenance after a commit, and an
    /// empty value does not set it aside, since it is a setting with several values. Every run of axGIT's has the
    /// automatic maintenance off, in a trusted repository too. The repository is made ripe for it: loose objects over
    /// its limit, to be pruned at once.
    /// </summary>
    [GitFact]
    public async Task The_recent_objects_hook_of_automatic_maintenance_runs_nothing()
    {
        var root = Repository("maintenance");
        var blobs = Directory.CreateDirectory(Path.Combine(_dir.Path, "blobs")).FullName;
        var paths = Enumerable.Range(0, 1500).Select(number => Path.Combine(blobs, number + ".txt")).ToList();
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

        File.AppendAllText(Path.Combine(root, "a.txt"), "two\n");
        Assert.True((await AxGit(root, GitTexts.StageAllArguments)).Ok);
        Assert.True((await AxGit(root, GitTexts.CommitArguments("two"))).Ok);
        Assert.Empty(Ran());

        // The way it ran: git's own automatic gc in a console, in the foreground.
        Git(root, "-c", "gc.autoDetach=false", "gc", "--auto");
        Assert.Equal(["recent-objects"], Ran());
    }

    /// <summary>
    /// The second line (T4's settings, <see cref="GitTrust.Overrides"/>), the second pass's finding 3:
    /// core.gitProxy. git takes the first proxy command it reads, the repository's, so an empty one given after it
    /// set nothing aside; without the git transport none is started.
    /// </summary>
    [GitFact]
    public async Task A_proxy_command_set_aside_runs_nothing()
    {
        var root = Repository("proxy-command");
        Git(root, "config", "core.gitProxy", MarkerFile(Path.Combine(_dir.Path, "proxy-program"), "git-proxy"));
        Git(root, "remote", "add", "origin", "git://host.example.invalid/x.git");

        var fetched = await AxGit(root, GitTexts.FetchArguments("origin"), GitTrust.Overrides(["core.gitProxy"], []));
        Assert.False(fetched.Ok);
        Assert.Empty(Ran());

        // The way it ran: the empty proxy command alone.
        await AxGit(root, GitTexts.FetchArguments("origin"), [new GitSetting("core.gitproxy", string.Empty)]);
        Assert.Equal(["git-proxy"], Ran());
    }

    /// <summary>
    /// The second pass's finding 4: a folder with a repository of its own and no entry in the index; Stage all
    /// makes it an entry, and from then on git runs itself inside it on every status, with that repository's hooks.
    /// Untracked, git does not enter it (P4 of the fifth pass), and it is not asked about; Stage all runs nothing of
    /// it, the check after it finds the gitlink and its hook, and the status after it is refused.
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

        var session = new GitSession(root, _ => false, _env);
        var scan = await session.CheckAsync();
        Assert.Empty(scan.Risks);
        Assert.True(await session.TrustAsync(scan));
        Assert.True((await session.ChangeAsync(GitTexts.StageAllArguments, GitRunner.Short)).Ok);
        Assert.Equal(["submodule vendor: hook post-index-change"], session.Scan.Risks);
        Assert.False(session.Trusted);
        var time = DateTime.UtcNow;
        void Touch() => File.SetLastWriteTimeUtc(Path.Combine(vendor, "v.txt"), time = time.AddSeconds(3));
        Touch();
        Assert.False((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);
        Assert.Empty(Ran());
        Git(root, "reset", "-q");

        // The way it ran: the stage and the status as a session that found nothing runs them.
        Assert.True((await AxGit(root, GitTexts.StageAllArguments)).Ok);
        Touch();
        await AxGit(root, GitStatus.Arguments);
        Assert.Equal(["nested-hook"], Ran());
    }

    // ---- M1: a legacy remotes file; T3: the fetch names its line ----

    /// <summary>
    /// M1 (tried with git 2.53): a legacy .git/remotes file with "Pull: +refs/heads/*:refs/heads/*" made the fetch
    /// with --prune delete every local branch the remote did not have. Such a file is a risk the scan names (and a
    /// legacy branches file too), and the fetch names its own line with an empty refmap, so that the file applies to
    /// nothing even in a trusted repository.
    /// </summary>
    [GitFact]
    public async Task A_legacy_remotes_file_is_a_risk_and_a_fetch_obeys_none()
    {
        Git(_dir.Path, "init", "-q", "--bare", "m1.git");
        var root = Repository("m1");
        Git(root, "push", "-q", Path.Combine(_dir.Path, "m1.git"), "main");
        Git(root, "branch", "keep");
        File.WriteAllText(_dir.File(Path.Combine("m1", ".git", "remotes", "leg"), string.Empty), $"URL: {Slashes}/m1.git\nPull: +refs/heads/*:refs/heads/*\n");
        _dir.File(Path.Combine("m1", ".git", "branches", "old"), $"{Slashes}/m1.git\n");
        var scan = await ScanAsync(root);
        Assert.Equal([GitTrust.Legacy("remotes", "leg"), GitTrust.Legacy("branches", "old")], scan.Risks);

        Git(root, "checkout", "-q", "--detach");
        Assert.True((await AxGit(root, GitTexts.FetchArguments("leg"))).Ok);
        Assert.Contains("keep", Git(root, "branch", "--list"));
        Assert.Contains("leg/main", Git(root, "branch", "-r"));

        // The way it ran: the fetch of the third pass, without its line.
        await AxGit(root, ["fetch", "--prune", "--no-recurse-submodules", "--no-all", "--upload-pack=git-upload-pack", "--", "leg"]);
        Assert.DoesNotContain("keep", Git(root, "branch", "--list"));
    }

    // ---- M2, D9: places on another computer, every form ----

    /// <summary>A path of the test's folder through this computer's own share, as another computer's would be reached.</summary>
    private string Share(string relative) => "//localhost/C$" + Path.Combine(_dir.Path, relative)[2..].Replace('\\', '/');

    /// <summary>
    /// M2 (tried with git 2.53 through //localhost/C$): the guard of the third pass read syntax, and an include on
    /// one line with its section, a value split over two lines, a .git link to a share, and core.excludesFile,
    /// core.worktree, core.attributesFile, mailmap.file, commit.template, core.hooksPath, an alternate object store,
    /// a .git file and a commondir on a share all reached the share before any notice. Now git's own parser reads
    /// the settings, every value that names another computer is a risk, and nothing on the share is read: a hooks
    /// folder there with a hook in it gives no hook risk. The half that shows git reaches it: a status as a console
    /// runs it reads core.excludesFile through the share.
    /// </summary>
    [GitFact]
    public async Task Every_form_that_names_another_computer_is_a_risk_and_nothing_there_is_read()
    {
        _dir.File(Path.Combine("far", "inc.cfg"), "[user]\n\tname = far\n");
        _dir.File(Path.Combine("far", "ignore"), "secret.txt\n");
        MarkerFile(Path.Combine(_dir.Path, "far", "hooks", "pre-commit"), "far-hook");
        Directory.CreateDirectory(Path.Combine(_dir.Path, "far", "objects"));
        var far = Share("far");
        // The share is this computer's own administrative one; where it is not open (a computer without it), the
        // halves that go through it are said as not tried, and the rest, which reads the text alone, runs.
        var reachable = Directory.Exists(far.Replace('/', '\\'));
        if (!reachable)
        {
            _output.WriteLine("LOUD: this computer's share C$ is not reachable: the .git link and the console's read through the share were not tried.");
        }

        async Task<IReadOnlyList<string>> RisksOf(string name, string config)
        {
            var root = Repository(name);
            File.AppendAllText(Path.Combine(root, ".git", "config"), config);
            var scan = await ScanAsync(root);
            Assert.Null(scan.Problem);
            return scan.Risks;
        }

        Assert.Contains("include.path names another computer, localhost", await RisksOf("one-line", $"[include] path = {far}/inc.cfg\n"));
        Assert.Contains("include.path names another computer, localhost", await RisksOf("split", $"[include]\n\tpath = \"//local\\\nhost/C$\"{far[14..]}/inc.cfg\n"));
        Assert.Contains("includeif.onbranch:main.path names another computer, localhost", await RisksOf("include-if", $"[includeIf \"onbranch:main\"]\n\tpath = {far}/inc.cfg\n"));
        foreach (var setting in new[] { "core.excludesFile", "core.worktree", "core.attributesFile", "mailmap.file", "commit.template" })
        {
            var section = setting[..setting.IndexOf('.')];
            var variable = setting[(setting.IndexOf('.') + 1)..];
            Assert.Contains($"{setting.ToLowerInvariant()} names another computer, localhost", await RisksOf("value-" + variable, $"[{section}]\n\t{variable} = {far}/ignore\n"));
        }

        var hooksRisks = await RisksOf("far-hooks", $"[core]\n\thooksPath = {far}/hooks\n");
        Assert.Contains("core.hookspath names another computer, localhost", hooksRisks);
        Assert.DoesNotContain("hook pre-commit", hooksRisks);

        // An alternate object store, a .git file and a commondir file on the share.
        var alternates = Repository("alternates");
        _dir.File(Path.Combine("alternates", ".git", "objects", "info", "alternates"), $"{far}/objects\n");
        Assert.Contains(GitTrust.Alternates, (await ScanAsync(alternates)).Risks);
        // Since the fifth pass (P1) the repository opened through either is not opened at all: git would work on a
        // folder that was not read.
        var tree = Directory.CreateDirectory(Path.Combine(_dir.Path, "far-tree")).FullName;
        File.WriteAllText(Path.Combine(tree, ".git"), $"gitdir: {far}/real.git\n");
        Assert.Equal("Its .git file names another computer, localhost, so axGIT does not open it.", (await ScanAsync(tree)).Problem);
        var linked = Repository("far-common");
        File.WriteAllText(Path.Combine(linked, ".git", "commondir"), $"{far}\n");
        Assert.Contains("is not a repository as git reads one", (await ScanAsync(linked)).Problem);
        Assert.Null(GitTrustScan.RootOf(linked));

        // A .git that is a link to the share: never followed (the repository there has a hook), and since the fifth
        // pass (P1) not opened.
        var real = Repository("far-real");
        MarkerFile(Path.Combine(real, ".git", "hooks", "post-checkout"), "real-hook");
        var link = Directory.CreateDirectory(Path.Combine(_dir.Path, "link-tree")).FullName;
        if (!reachable)
        {
            return;
        }

        try
        {
            Directory.CreateSymbolicLink(Path.Combine(link, ".git"), Share(Path.Combine("far-real", ".git")).Replace('/', '\\'));
            var scan = await ScanAsync(link);
            Assert.Equal("Its .git is a link, which axGIT does not follow, so axGIT does not open it.", scan.Problem);
            Assert.Equal(link, GitTrustScan.RootOf(link));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _output.WriteLine("LOUD: a symbolic link could not be made on this computer (" + ex.Message + "): the .git link was not tried.");
        }

        // The way git reaches it: core.excludesFile on the share, read at a status.
        var console = Repository("console");
        File.WriteAllText(Path.Combine(console, "secret.txt"), "x\n");
        Assert.Contains("secret.txt", Git(console, "status", "--porcelain"));
        Git(console, "config", "core.excludesFile", far + "/ignore");
        Assert.DoesNotContain("secret.txt", Git(console, "status", "--porcelain"));
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
    /// A fetch, pull or push as the window makes it in a trusted repository: the status, the questions of
    /// <see cref="GitReach"/>, and, unless they gave a problem, the command with the remote they gave on its
    /// command line (a pull with the branch it follows; one that follows none is not made). Returns what was asked
    /// about and the hosts git then tried to reach, read from what it said.
    /// </summary>
    private async Task<(Reach Asked, IReadOnlyList<string> Reached, string Said)> ToServerAsync(string root, string what)
    {
        var status = GitStatus.Parse((await AxGit(root, GitStatus.Arguments)).Output);
        var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments), push: what == "push", status.Branch, status.Upstream);
        Assert.NotNull(asked);
        if (asked.Problem is not null)
        {
            Assert.Empty(asked.Remote);
            Assert.Empty(asked.Hosts);
            return (asked, [], string.Empty);
        }

        var arguments = what switch
        {
            "fetch" => GitTexts.FetchArguments(asked.Remote),
            "pull" => GitTexts.PullArguments(asked.Remote, asked.Merge),
            _ => GitTexts.PushArguments(asked.Remote, status.Upstream, status.Branch!),
        };
        if (arguments is null)
        {
            return (asked, [], string.Empty);
        }

        // Where git went is read from its own trace (the remote helper's command line names the address), and the
        // run ends at the proxy on a closed port of this computer: nothing leaves it.
        var result = await GitRunner.RunAsync(root, arguments, GitRunner.Short, environment: new Dictionary<string, string>(_env) { ["GIT_TRACE"] = "1" });
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

    /// <summary>Row 1: branch.main.remote without branch.main.merge. The fetch reaches the branch's remote, which is asked about; a pull names the branch it follows (T3), and this one follows none, so nothing is pulled.</summary>
    [GitFact]
    public async Task A_branch_with_a_remote_and_no_branch_to_follow_reaches_the_server_asked_about()
    {
        var root = WithRemotes("no-merge");
        Git(root, "config", "branch.main.remote", "evil");
        Git(root, "config", "--unset", "branch.main.merge");
        await ReachesAsync(root, "fetch", "evil.example.invalid");
        var (pull, reached, _) = await ToServerAsync(root, "pull");
        Assert.Null(pull.Merge);
        Assert.Empty(reached);
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
        Assert.Equal(3, ReservedHost().Matches((await AxGit(root, ["fetch"])).AllText).Select(match => match.Value).Distinct().Count());
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
        await ReachesAsync(root, "push", "evil.example.invalid");
        Git(root, "checkout", "-q", "--detach");
        await RunsNothingAsync(root, "push");
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
        // The way it ran: the fetch of the third pass, the remote on its command line and no line of its own.
        var hosts = ReservedHost().Matches((await AxGit(root, ["fetch", "--prune", "--no-recurse-submodules", "--no-all", "--upload-pack=git-upload-pack", "--", "origin"])).AllText).Select(match => match.Value).Distinct().Order();
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

    /// <summary>A branch that follows another branch of the same repository reaches no server, and its pull runs, naming that branch.</summary>
    [GitFact]
    public async Task A_branch_that_follows_a_branch_of_this_repository_asks_about_no_server()
    {
        var root = WithRemotes("here");
        Git(root, "switch", "-q", "-c", "topic", "--track", "main");
        var status = GitStatus.Parse((await AxGit(root, GitStatus.Arguments)).Output);
        var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments), push: false, status.Branch, status.Upstream);
        Assert.NotNull(asked);
        Assert.Null(asked.Problem);
        Assert.Equal(".", asked.Remote);
        Assert.Equal("refs/heads/main", asked.Merge);
        Assert.Empty(asked.Hosts);
        Assert.True((await AxGit(root, GitTexts.PullArguments(asked.Remote, asked.Merge)!)).Ok);
        Assert.True((await AxGit(root, GitTexts.FetchArguments(asked.Remote))).Ok);
    }

    /// <summary>
    /// Finding 10: a proxy the repository's own settings name is where the bytes go first. In a trusted repository
    /// it is asked about and said; it is a risk by its name, so in one that is not trusted nothing runs at all.
    /// </summary>
    [GitFact]
    public async Task A_proxy_of_the_repositorys_own_settings_is_named_and_a_risk()
    {
        var root = WithRemotes("proxy");
        // The repository's proxy is this computer at another address of its own, 127.0.0.2, apart from the test's own proxy at 127.0.0.1.
        Git(root, "config", "http.https://origin.example.invalid/.proxy", "http://user:secret" + "@" + "127.0.0.2:7");

        var (trusted, reached, said) = await ToServerAsync(root, "fetch");
        Assert.Equal(["origin.example.invalid"], trusted.Hosts);
        Assert.Equal(["127.0.0.2"], trusted.Proxies);
        Assert.Equal(["origin.example.invalid"], reached);
        // curl names the proxy it failed at in words that change between versions ("via 127.0.0.2", "to 127.0.0.2").
        Assert.Contains("127.0.0.2", said);

        var session = new GitSession(root, _ => false, _env);
        Assert.Contains("http.https://origin.example.invalid/.proxy", (await session.CheckAsync()).Risks);
        Assert.False((await session.ChangeAsync(GitTexts.FetchArguments("origin"), GitRunner.Short)).Ok);

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
            var asked = await GitReach.ReadAsync(async arguments => await AxGit(root, arguments), push, "main", "origin/main");
            Assert.Null(asked!.Problem);
            return asked.Hosts;
        }

        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: true));

        Git(root, "remote", "set-url", "--push", "origin", "https://push.example.invalid/a/b.git");
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));
        Assert.Equal(["push.example.invalid"], await Hosts(push: true));

        Git(root, "config", "branch.main.pushRemote", "backup");
        Git(root, "remote", "set-url", "--add", "--push", "backup", "https://old.example.invalid/a/b.git");
        Git(root, "remote", "set-url", "--add", "--push", "backup", "https://second.example.invalid/a/b.git");
        Git(root, "config", "url.https://rewritten.example.invalid/.insteadOf", "https://old.example.invalid/");
        Assert.Equal(["rewritten.example.invalid", "second.example.invalid"], await Hosts(push: true));
        Assert.Equal(["fetch.example.invalid"], await Hosts(push: false));

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
