using AxGit.Core;

namespace AxGit.Tests;

/// <summary>
/// A repository's own programs and settings (SPEC.md AG-1.6, AG-1.6c; security audit S-2): what is a risk, how it
/// is named and said, and what is set aside. In the collection of the tests that run git (L3 of the review's fourth
/// pass): <see cref="GitTrust.NoHooksFolder"/> is one setting for the process.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitTrustTests
{
    public GitTrustTests() => GitTests.UseOwnHooksFolder();

    private static IReadOnlyList<FileSetting> Settings(params string[] names) => [.. names.Select(name => new FileSetting(name, "x"))];

    private static readonly Func<char, bool> NoNetworkDrive = _ => false;

    [Fact]
    public void Every_name_that_is_not_known_to_be_harmless_is_a_risk_and_the_harmless_ones_are_left()
    {
        var settings = Settings("core.repositoryformatversion", "core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "remote.origin.url", "user.name", "core.sshCommand", "credential.helper", "lfs.extension.x.clean", "gc.recentObjectsHook");
        IReadOnlyList<string> risks = ["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper", "lfs.extension.x.clean", "gc.recentObjectsHook"];
        Assert.Equal(risks, GitTrust.SettingRisks(settings));
        // The notice says which of them it knows to run a program, and which it does not know.
        Assert.Equal(["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper"], GitTrust.Programs(risks));
        Assert.Equal(["lfs.extension.x.clean", "gc.recentObjectsHook"], GitTrust.NotKnown(risks));
        Assert.Empty(GitTrust.Places(risks));
    }

    // ---- The parser of one settings file (T1) ----

    /// <summary>
    /// T1: git as a parser of one file given by its path, its includes not followed, each setting ended by a zero
    /// character: the name, a line break, the value, which may hold line breaks; a name without a value is one git
    /// reads as true.
    /// </summary>
    [Fact]
    public void The_parser_reads_one_file_without_its_includes_and_its_answer_is_read_whole()
    {
        Assert.Equal(["config", "--file", @"C:\repo\.git\config", "--no-includes", "--list", "-z"], GitTrust.ParserArguments(@"C:\repo\.git\config"));
        var settings = GitTrust.ParseSettings("core.bare\nfalse\0core.fsmonitor\0alias.two\nline one\nline two\0include.path\n//host.invalid/x\0\0");
        Assert.Equal(
            [new FileSetting("core.bare", "false"), new FileSetting("core.fsmonitor", null), new FileSetting("alias.two", "line one\nline two"), new FileSetting("include.path", "//host.invalid/x")],
            settings);
        Assert.Empty(GitTrust.ParseSettings(string.Empty));
    }

    // ---- The table of the harmless settings (AG-1.6c; the review's second pass of 2026-11-27) ----

    /// <summary>
    /// The form of the table: every name in lower case, in the shape a setting is looked up by (a section, a
    /// star for a middle part, a variable), once, and with a reason that is a sentence. A name without a reason
    /// has not been shown to be harmless.
    /// </summary>
    [Fact]
    public void Every_harmless_setting_is_written_in_the_tables_form_once_and_with_its_reason()
    {
        Assert.NotEmpty(GitTrust.Inert);
        foreach (var setting in GitTrust.Inert)
        {
            Assert.Equal(setting.Name, GitTrust.Shape(setting.Name));
            Assert.Matches(@"^[a-z]+(\.\*)?\.[a-z0-9-]+$", setting.Name);
            Assert.True(setting.Reason.Length >= 20 && char.IsUpper(setting.Reason[0]) && setting.Reason.EndsWith('.'), $"{setting.Name} needs a reason, a sentence: \"{setting.Reason}\"");
            Assert.DoesNotContain('\n', setting.Reason);
        }

        Assert.Equal(GitTrust.Inert.Count, GitTrust.Inert.Select(setting => setting.Name).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// No name the table calls harmless is one that runs a program, names a proxy or brings another file in:
    /// an entry added by mistake fails here. The includes are never harmless: what they bring in changes with
    /// the branch (includeIf onbranch) and with any file they name.
    /// </summary>
    [Fact]
    public void No_harmless_setting_runs_a_program_names_a_proxy_or_includes_a_file()
    {
        foreach (var setting in GitTrust.Inert)
        {
            var name = setting.Name.Replace("*", "x", StringComparison.Ordinal);
            Assert.False(GitTrust.RunsAProgram(name), setting.Name);
            Assert.False(GitTrust.NamesAProxy(name), setting.Name);
            Assert.False(name.StartsWith("include", StringComparison.Ordinal), setting.Name);
        }

        Assert.False(GitTrust.IsInert("include.path"));
        Assert.False(GitTrust.IsInert("includeif.onbranch:other.path"));
        Assert.False(GitTrust.IsInert("includeIf.gitdir:C:/work/.path"));
    }

    /// <summary>
    /// What an everyday repository carries, so that it opens without the notice: the names git 2.53 wrote for
    /// git init, git clone (also shallow, without tags), git submodule add (in the repository and in the
    /// submodule's own under .git/modules), git worktree add, git sparse-checkout, git branch with an upstream
    /// and a description, and Git LFS's own number; and the notes GitHub's gh and Visual Studio Code keep. Listed
    /// from repositories made for this (the names alone, 2026-11-27).
    /// </summary>
    [Theory]
    [InlineData("core.repositoryformatversion")]
    [InlineData("core.filemode")]
    [InlineData("core.bare")]
    [InlineData("core.logallrefupdates")]
    [InlineData("core.symlinks")]
    [InlineData("core.ignorecase")]
    [InlineData("core.autocrlf")]
    [InlineData("core.longpaths")]
    [InlineData("core.sparsecheckout")]
    [InlineData("core.sparsecheckoutcone")]
    [InlineData("extensions.worktreeconfig")]
    [InlineData("remote.origin.url")]
    [InlineData("remote.origin.fetch")]
    [InlineData("remote.origin.pushurl")]
    [InlineData("remote.origin.tagopt")]
    [InlineData("remote.upstream.gh-resolved")]
    [InlineData("remote.pushdefault")]
    [InlineData("branch.main.remote")]
    [InlineData("branch.main.merge")]
    [InlineData("branch.feature/one.two.pushremote")]
    [InlineData("branch.topic.description")]
    [InlineData("branch.main.vscode-merge-base")]
    [InlineData("branch.main.gh-merge-base")]
    [InlineData("branch.main.github-pr-owner-number")]
    [InlineData("submodule.libs/zip.url")]
    [InlineData("submodule.libs/zip.active")]
    [InlineData("user.name")]
    [InlineData("user.email")]
    [InlineData("lfs.repositoryformatversion")]
    [InlineData("lfs.https://github.com/a/b.git/info/lfs.access")]
    // A name's section and variable are in any case; the middle part is as written.
    [InlineData("Remote.Origin.URL")]
    public void A_setting_of_an_everyday_repository_is_harmless(string name) => Assert.True(GitTrust.IsInert(name), name);

    [Theory]
    [InlineData("lfs.extension.x.clean")]
    [InlineData("lfs.customtransfer.x.path")]
    [InlineData("lfs.url")]
    [InlineData("gc.recentObjectsHook")]
    [InlineData("filter.a=b.clean")]
    [InlineData("core.gitProxy")]
    [InlineData("core.unsetenvvars")]
    [InlineData("http.proxy")]
    [InlineData("http.https://github.com/.proxy")]
    [InlineData("http.sslVerify")]
    [InlineData("http.curloptResolve")]
    [InlineData("url.https://example.org/.insteadOf")]
    [InlineData("remotes.origin")]
    [InlineData("remote.origin.promisor")]
    [InlineData("remote.origin.push")]
    [InlineData("extensions.partialClone")]
    [InlineData("fetch.all")]
    [InlineData("alias.co")]
    [InlineData("submodule.libs/zip.update")]
    [InlineData("uploadpack.allowFilter")]
    [InlineData("commit.gpgsign")]
    [InlineData("remote..url")]
    [InlineData("core")]
    // In the repository opened a work tree elsewhere is no ordinary thing; in a submodule git writes it for each.
    [InlineData("core.worktree")]
    public void A_setting_that_is_not_in_the_table_is_not_harmless(string name) => Assert.False(GitTrust.IsInert(name), name);

    [Fact]
    public void A_submodules_work_tree_setting_is_harmless_in_a_repository_inside_alone()
    {
        Assert.True(GitTrust.IsInert("core.worktree", inside: true));
        Assert.Empty(GitTrust.SettingRisks(Settings("core.worktree", "core.bare"), inside: true));
        Assert.Equal(["core.worktree"], GitTrust.SettingRisks(Settings("core.worktree", "core.bare")));
    }

    [Fact]
    public void A_hook_is_a_risk_and_a_sample_is_not()
    {
        Assert.True(GitTrust.IsHook("post-checkout"));
        Assert.False(GitTrust.IsHook("pre-commit.sample"));
        Assert.Equal("hook post-checkout", GitTrust.Hook("post-checkout"));
        Assert.Equal(["hook post-checkout"], GitTrust.Programs(["hook post-checkout"]));
    }

    // ---- What a changing command is run with where a program lies in the work tree (T4) ----

    private static GitSetting Empty(string name) => new(name, string.Empty);

    /// <summary>No hooks, last in the list. Since the fourth pass no empty credential helper: no run is made in a repository that is not trusted, and a trusted one signs in with the user's own.</summary>
    private static GitSetting[] Careful => [new("core.hooksPath", Path.GetFullPath(GitTrust.NoHooksFolder))];

    [Fact]
    public void A_program_set_aside_is_set_to_nothing_and_no_hook_runs()
    {
        Assert.Equal(Careful, GitTrust.Careful);
        Assert.Equal([Empty("diff.pdf.textconv"), Empty("core.sshCommand"), .. Careful], GitTrust.Overrides(["diff.pdf.textconv", "hook pre-commit", "core.sshCommand"], ["lfs"]));
        // The hooks folder a repository names is not set to nothing: it is set to the folder that holds none.
        Assert.Equal(Careful, GitTrust.Overrides(["core.hookspath"], []));
        // Nothing to set aside, nothing of axGIT's.
        Assert.Empty(GitTrust.Overrides([], ["lfs"]));
    }

    /// <summary>
    /// The review's second pass, finding 1: a setting axGIT does not know cannot be set aside by its name, and a
    /// filter of the user's own (Git LFS) is a program that would obey it. So with such a setting no filter
    /// runs: each one's three programs set to nothing, and none required.
    /// </summary>
    [Fact]
    public void With_a_setting_that_is_not_known_every_filter_is_off()
    {
        Assert.Equal(
            [Empty("filter.lfs.clean"), Empty("filter.lfs.smudge"), Empty("filter.lfs.process"), new GitSetting("filter.lfs.required", "false"),
                Empty("filter.a=b.clean"), Empty("filter.a=b.smudge"), Empty("filter.a=b.process"), new GitSetting("filter.a=b.required", "false"), .. Careful],
            GitTrust.Overrides(["lfs.extension.x.clean"], ["lfs", "a=b"]));
        Assert.Contains(Empty("filter.lfs.process"), GitTrust.Overrides(GitTrust.Inside("gone", [GitTrust.NotRead]), ["lfs"]));
        Assert.DoesNotContain(Empty("filter.lfs.process"), GitTrust.Overrides(["hook pre-push", "core.sshcommand"], ["lfs"]));
    }

    /// <summary>
    /// Finding 3: git takes the first proxy command it reads, the repository's, so an empty one after it sets
    /// nothing aside; the transport that would start it is refused instead. A proxy for http is set to nothing by
    /// its name (finding 10).
    /// </summary>
    [Fact]
    public void A_proxy_command_stops_the_git_transport_and_an_http_proxy_is_set_to_nothing()
    {
        Assert.Equal([Empty("core.gitProxy"), new GitSetting("protocol.git.allow", "never"), .. Careful], GitTrust.Overrides(["core.gitProxy"], []));
        Assert.Equal([Empty("http.proxy"), Empty("http.https://a.example/.proxy"), Empty("remote.origin.proxy"), .. Careful],
            GitTrust.Overrides(["http.proxy", "http.https://a.example/.proxy", "remote.origin.proxy"], ["lfs"]));
        Assert.True(GitTrust.NamesAProxy("HTTP.https://a.example/.Proxy"));
        Assert.False(GitTrust.NamesAProxy("http.proxyAuthMethod"));
    }

    /// <summary>
    /// Every command keeps the file monitor and the ext transport off, git's automatic maintenance (finding 2),
    /// and since the fourth pass a repository's own core.unsetenvvars (H1: it cleared what a submodule's run was
    /// given through the environment) and http redirects (L1). What is set aside is no argument: it is in the
    /// environment.
    /// </summary>
    [Fact]
    public void Every_command_has_its_fixed_settings_on_the_command_line()
    {
        var command = GitRunner.Command(@"C:\repo", ["status"]);
        Assert.Equal(
            ["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "maintenance.auto=false", "-c", "gc.auto=0",
                "-c", "core.unsetenvvars=PERL5LIB", "-c", "http.followRedirects=false", "status"],
            command);
    }

    [Fact]
    public void What_is_set_aside_goes_into_the_environment_after_what_the_user_has_there()
    {
        var environment = new Dictionary<string, string?>();
        GitRunner.SetOverrides(environment, []);
        Assert.Empty(environment);

        GitRunner.SetOverrides(environment, [new GitSetting("filter.a=b.clean", string.Empty), new GitSetting("core.hooksPath", "NUL")]);
        Assert.Equal("2", environment["GIT_CONFIG_COUNT"]);
        Assert.Equal("filter.a=b.clean", environment["GIT_CONFIG_KEY_0"]);
        Assert.Equal(string.Empty, environment["GIT_CONFIG_VALUE_0"]);
        Assert.Equal("core.hooksPath", environment["GIT_CONFIG_KEY_1"]);
        Assert.Equal("NUL", environment["GIT_CONFIG_VALUE_1"]);

        // The user's own, set outside axKit, stay, and axGIT's come after them, so that they win.
        var users = new Dictionary<string, string?> { ["GIT_CONFIG_COUNT"] = "1", ["GIT_CONFIG_KEY_0"] = "core.hooksPath", ["GIT_CONFIG_VALUE_0"] = "C:/mine" };
        GitRunner.SetOverrides(users, [new GitSetting("core.hooksPath", "NUL")]);
        Assert.Equal("2", users["GIT_CONFIG_COUNT"]);
        Assert.Equal("C:/mine", users["GIT_CONFIG_VALUE_0"]);
        Assert.Equal("NUL", users["GIT_CONFIG_VALUE_1"]);
    }

    [Theory]
    [InlineData("gpg.ssh.defaultKeyCommand")]
    [InlineData("gpg.program")]
    [InlineData("gpg.ssh.program")]
    [InlineData("diff.external")]
    [InlineData("diff.word.command")]
    [InlineData("merge.ours.driver")]
    [InlineData("filter.lfs.process")]
    [InlineData("pager.log")]
    [InlineData("core.pager")]
    [InlineData("core.editor")]
    [InlineData("sequence.editor")]
    [InlineData("core.askPass")]
    [InlineData("core.gitProxy")]
    [InlineData("core.hooksPath")]
    [InlineData("credential.https://example.org.helper")]
    [InlineData("trailer.sign.command")]
    [InlineData("trailer.sign.cmd")]
    [InlineData("hook.lint.command")]
    [InlineData("remote.origin.uploadpack")]
    [InlineData("remote.origin.receivepack")]
    [InlineData("remote.origin.vcs")]
    [InlineData("uploadpack.packObjectsHook")]
    public void A_name_that_makes_git_run_a_program_is_one(string name)
    {
        Assert.True(GitTrust.RunsAProgram(name));
        Assert.False(GitTrust.IsInert(name));
    }

    /// <summary>These run no program by their name, so the notice calls them settings axGIT does not know; each is a risk all the same.</summary>
    [Theory]
    [InlineData("url.https://example.org/.insteadOf")]
    [InlineData("include.path")]
    [InlineData("includeIf.gitdir:C:/work/.path")]
    [InlineData("gpg.format")]
    [InlineData("gpg.ssh.allowedSignersFile")]
    [InlineData("diff.tool")]
    [InlineData("user.signingkey")]
    [InlineData("alias.co")]
    [InlineData("submodule.libs/zip.update")]
    [InlineData("remote.my origin.url.x")]
    [InlineData("uploadpack.allowFilter")]
    [InlineData("receivepack.denyCurrentBranch")]
    public void A_name_that_is_no_program_by_its_name_is_not_called_one(string name)
    {
        Assert.False(GitTrust.RunsAProgram(name));
        Assert.Equal([name], GitTrust.NotKnown(GitTrust.SettingRisks(Settings(name))));
    }

    /// <summary>The places the scan names without reading them are said as places, apart from the programs and the settings, also inside a submodule.</summary>
    [Fact]
    public void A_place_the_scan_does_not_read_is_said_as_a_place()
    {
        IReadOnlyList<string> places =
        [
            GitTrust.Link(".git/hooks"), GitTrust.Alternates, GitTrust.OutsideTheWorkTree, GitTrust.Legacy("remotes", "origin"),
            GitTrust.OnAnotherComputer("core.excludesfile", "server.invalid"), GitTrust.OnAnotherComputer("include.path", "."),
            GitTrust.NotRead, GitTrust.TooDeep, .. GitTrust.Inside("libs/zip", [GitTrust.Legacy("branches", "old.one")]),
        ];
        Assert.Equal(places, GitTrust.Places(places));
        Assert.Empty(GitTrust.Programs(places));
        Assert.Empty(GitTrust.NotKnown(places));
        Assert.Equal(["remote.origin.fetch writes outside refs/remotes"], GitTrust.NotKnown(["remote.origin.fetch writes outside refs/remotes"]));
    }

    [Fact]
    public void A_work_trees_repository_is_its_git_folder_or_the_folder_its_git_file_names()
    {
        using var temp = new TempFolder("axgit-links");
        var embedded = Directory.CreateDirectory(Path.Combine(temp.Path, "embedded", ".git")).FullName;
        Assert.Equal(embedded, GitTrust.GitFolderOf(Path.Combine(temp.Path, "embedded")));

        var modules = Directory.CreateDirectory(Path.Combine(temp.Path, ".git", "modules", "libs", "zip")).FullName;
        temp.File(Path.Combine("libs", "zip", ".git"), "gitdir: ../../.git/modules/libs/zip\n");
        Assert.Equal(modules, GitTrust.GitFolderOf(Path.Combine(temp.Path, "libs", "zip")));

        // Never fetched: no .git at all, and git runs nothing there.
        Directory.CreateDirectory(Path.Combine(temp.Path, "empty"));
        Assert.Null(GitTrust.GitFolderOf(Path.Combine(temp.Path, "empty")));
        // A .git file that names no folder is no repository to git either.
        temp.File(Path.Combine("odd", ".git"), "something else\n");
        Assert.Null(GitTrust.GitFolderOf(Path.Combine(temp.Path, "odd")));
    }

    [Fact]
    public void The_repositories_git_keeps_for_submodules_are_found_also_one_inside_another()
    {
        using var temp = new TempFolder("axgit-modules");
        string Repository(params string[] parts)
        {
            var folder = Directory.CreateDirectory(Path.Combine([temp.Path, .. parts])).FullName;
            File.WriteAllText(Path.Combine(folder, "config"), string.Empty);
            File.WriteAllText(Path.Combine(folder, "HEAD"), string.Empty);
            return folder;
        }

        var zip = Repository("libs", "zip");
        var inner = Repository("libs", "zip", "modules", "deep");
        var plain = Repository("plain");
        // A folder inside a repository that is not its modules folder is not looked into.
        Repository("plain", "objects", "not-one");
        var (found, tooDeep) = GitTrust.ModuleFolders(temp.Path);
        Assert.Equal([zip, inner, plain], found);
        Assert.Empty(tooDeep);
        Assert.Empty(GitTrust.ModuleFolders(Path.Combine(temp.Path, "no-such-folder")).Found);
    }

    /// <summary>
    /// The folders are followed a bounded number of levels down (the review's second pass: the walk had no end
    /// but the disk's). What lies deeper is not passed over in silence: the folder where the walk stopped is
    /// given, and the scan names it as not read.
    /// </summary>
    [Fact]
    public void The_walk_through_the_submodules_folder_ends_at_its_depth_and_says_where()
    {
        using var temp = new TempFolder("axgit-deep");
        var parts = Enumerable.Range(0, GitTrust.Deep + 2).Select(level => "d" + level).ToArray();
        var deepest = Directory.CreateDirectory(Path.Combine([temp.Path, .. parts])).FullName;
        File.WriteAllText(Path.Combine(deepest, "config"), string.Empty);
        File.WriteAllText(Path.Combine(deepest, "HEAD"), string.Empty);
        var (found, tooDeep) = GitTrust.ModuleFolders(temp.Path);
        Assert.Empty(found);
        Assert.Equal([Path.Combine([temp.Path, .. parts.Take(GitTrust.Deep)])], tooDeep);
    }

    /// <summary>
    /// A name found in a submodule is set to nothing once for every run, since axGIT's settings reach git's runs
    /// inside a submodule; a submodule's hook, and one whose settings could not be read, are named with it.
    /// </summary>
    [Fact]
    public void What_a_submodule_would_run_is_named_with_it_and_set_aside_with_the_rest()
    {
        var inside = GitTrust.Inside("libs/zip", ["core.sshCommand", "hook post-checkout"]);
        Assert.Equal(["submodule libs/zip: core.sshCommand", "submodule libs/zip: hook post-checkout"], inside);
        Assert.Equal(inside, GitTrust.Programs(inside));
        Assert.Equal(
            [Empty("core.sshcommand"), Empty("filter.x.smudge"), .. Careful],
            GitTrust.Overrides(["core.sshcommand", "submodule libs/zip: core.sshcommand", "submodule libs/zip: filter.x.smudge", "submodule libs/zip: hook post-checkout"], []));
        // A name's middle part is another name in other case, so both are set aside.
        Assert.Equal([Empty("diff.pdf.textconv"), Empty("diff.PDF.textconv"), .. Careful], GitTrust.Overrides(["diff.pdf.textconv", "diff.PDF.textconv"], []));
        Assert.Equal(["diff.pdf.textconv", "diff.PDF.textconv"], GitTrust.SettingRisks(Settings("diff.pdf.textconv", "diff.PDF.textconv", "diff.pdf.textconv")));
        Assert.Equal(GitTrust.Inside("gone", [GitTrust.NotRead]), GitTrust.Places(GitTrust.Inside("gone", [GitTrust.NotRead])));
    }

    // ---- The review's third pass of 2026-11-27 ----

    /// <summary>
    /// Every kind of program name carries names of its kind written out, each a program name by the rule and by its
    /// own kind's pattern, so that the test that sets each to a program (GitSessionTests) reaches every kind.
    /// </summary>
    [Fact]
    public void Every_kind_of_program_name_has_names_written_out_that_are_of_it()
    {
        Assert.NotEmpty(GitTrust.ProgramNamesTable);
        foreach (var kind in GitTrust.ProgramNamesTable)
        {
            Assert.NotEmpty(kind.Examples);
            var own = new System.Text.RegularExpressions.Regex("^(" + kind.Pattern + ")$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var name in kind.Examples)
            {
                Assert.True(own.IsMatch(name), $"{name} is not of the kind {kind.Pattern}");
                Assert.True(GitTrust.RunsAProgram(name), name);
                Assert.False(GitTrust.IsInert(name), name);
            }
        }

        Assert.Contains("mergetool.x.cmd", GitTrust.ProgramNamesTable.SelectMany(kind => kind.Examples));
    }

    /// <summary>L4 of the third pass: the hooks folder of a run without hooks is a full path of the tests' own here (L3 of the fourth), emptied before it is given.</summary>
    [Fact]
    public void The_hooks_folder_of_a_run_without_hooks_is_a_full_path_and_empty()
    {
        var folder = GitTrust.EmptyHooksFolder();
        Assert.True(Path.IsPathFullyQualified(folder), folder);
        Assert.DoesNotContain(@"\AxKit\axGIT-no-hooks", folder, StringComparison.OrdinalIgnoreCase);
        File.WriteAllText(Path.Combine(folder, "pre-commit"), "#!/bin/sh\n");
        Directory.CreateDirectory(Path.Combine(folder, "inside"));
        Assert.Equal(folder, GitTrust.EmptyHooksFolder());
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
        Assert.Equal(new GitSetting("core.hooksPath", folder), GitTrust.Careful[0]);
        // The folder of axKit's own is kept after a run.
        GitTrust.ReleaseHooksFolder(folder);
        Assert.True(Directory.Exists(folder));
    }

    /// <summary>L3 of the fourth pass: when the folder cannot be emptied, a fresh one of the temporary folder is given, with the run's mark, and is deleted after its run.</summary>
    [Fact]
    public void A_fresh_hooks_folder_for_one_run_is_deleted_after_it()
    {
        var folder = GitTrust.EmptyHooksFolder();
        using (var held = File.Open(Path.Combine(folder, "held"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            var fresh = GitTrust.EmptyHooksFolder();
            Assert.NotEqual(folder, fresh);
            Assert.StartsWith(GitTrust.FallbackPrefix, Path.GetFileName(fresh), StringComparison.Ordinal);
            Assert.StartsWith("axGIT-no-hooks-" + TempFolder.Run, Path.GetFileName(fresh), StringComparison.Ordinal);
            Assert.Empty(Directory.EnumerateFileSystemEntries(fresh));
            GitTrust.ReleaseHooksFolder(fresh);
            Assert.False(Directory.Exists(fresh));
        }

        Assert.Equal(folder, GitTrust.EmptyHooksFolder());
    }

    /// <summary>
    /// D5, M3 of the third pass and M2 of the fourth: where the value decides the harm, the value is read. A fetch
    /// line is harmless only when it writes under refs/remotes/; a value on another computer is a risk whatever
    /// its name, harmless or not, in each form a path to another computer takes.
    /// </summary>
    [Fact]
    public void A_fetch_that_writes_outside_refs_remotes_and_a_value_on_another_computer_are_risks()
    {
        IReadOnlyList<FileSetting> settings =
        [
            new("remote.origin.fetch", "+refs/heads/*:refs/remotes/origin/*"),
            new("remote.origin.fetch", "^refs/heads/secret"),
            new("remote.origin.fetch", "refs/tags/v1"),
            new("remote.mirror.fetch", "+refs/heads/*:refs/heads/*"),
            new("remote.Odd.fetch", "refs/heads/a:refs/remotes/../heads/a"),
            new("core.worktree", "../../../libs/zip"),
            new("core.worktree", "//server.invalid/share/zip"),
            new("core.excludesfile", @"\\server.invalid\share\ignore"),
            new("core.attributesfile", @"\\?\UNC\attributes.invalid\share\a"),
            new("mailmap.file", "file://mail.invalid/share/map"),
            new("commit.template", "Z:/share/template"),
            new("remote.origin.url", "//origin.invalid/share/repo.git"),
            new("user.name", "Someone"),
            new("core.hookspath", @"\\.\pipe\x"),
        ];
        Assert.Equal(
            [
                "remote.mirror.fetch writes outside refs/remotes", "remote.Odd.fetch writes outside refs/remotes", "core.worktree names another computer, server.invalid",
                "core.excludesfile names another computer, server.invalid", "core.attributesfile names another computer, attributes.invalid", "mailmap.file names another computer, mail.invalid",
                "commit.template names another computer, drive Z:", "remote.origin.url names another computer, origin.invalid", "core.hookspath names a device path",
            ],
            GitTrust.ValueRisks(settings, null, letter => letter == 'Z'));
        Assert.Empty(GitTrust.ValueRisks([], null, NoNetworkDrive));
    }

    [Theory]
    [InlineData(@"\\server\share\x", "server")]
    [InlineData("//Server.Invalid/share", "server.invalid")]
    [InlineData(@"\\?\UNC\host\share", "host")]
    [InlineData(@"\\?\C:\repo", null)]
    [InlineData(@"\\?\GLOBALROOT\Device\Mup\host\share", ".")]
    [InlineData(@"C:\repo", null)]
    [InlineData("../x", null)]
    [InlineData("/c/repo", null)]
    public void A_share_names_its_computer_and_a_path_here_none(string path, string? host) => Assert.Equal(host, GitTrust.OtherComputer(path));

    /// <summary>M2: a value names another computer as a share, a file address with a host, or a path on a network drive; the computer the repository lies on is its own.</summary>
    [Theory]
    [InlineData("//host/share/x", null, "host")]
    [InlineData("//host/share/x", "host", null)]
    [InlineData("\"//host/share/x\"", null, "host")]
    [InlineData("file://host/share/x", null, "host")]
    [InlineData("file://localhost/C$/x", null, "localhost")]
    [InlineData("file:////host/share/x", null, "host")]
    [InlineData("file:///C:/work/x", null, null)]
    [InlineData("Z:/x", null, "drive Z:")]
    [InlineData("z:/x", "drive Z:", null)]
    [InlineData("C:/x", null, null)]
    [InlineData("https://example.invalid/x", null, null)]
    [InlineData("true", null, null)]
    public void A_value_on_another_computer_is_known_from_its_text(string value, string? own, string? host) =>
        Assert.Equal(host, GitTrust.ElsewhereOf(value, own, letter => letter == 'Z'));

    [Fact]
    public void The_words_of_a_value_that_may_name_a_file_are_those_with_a_slash()
    {
        Assert.Equal(["./tools/clean.sh"], GitTrust.PathWords("sh \"./tools/clean.sh\" --x %f"));
        Assert.Equal([@"C:\Program Files\x\y.exe", "a/b"], GitTrust.PathWords(@"'C:\Program Files\x\y.exe' a/b"));
        Assert.Empty(GitTrust.PathWords("ssh -v"));
        Assert.Empty(GitTrust.PathWords(string.Empty));
    }

    /// <summary>T4: the mark is of the lines given, whatever their order and case: a hook's hash among them changes it.</summary>
    [Fact]
    public void The_mark_changes_with_any_line()
    {
        Assert.Equal(GitTrust.Mark(["hook a 01", "risk b"]), GitTrust.Mark(["RISK B", "hook a 01"]));
        Assert.NotEqual(GitTrust.Mark(["hook a 01", "risk b"]), GitTrust.Mark(["hook a 02", "risk b"]));
        Assert.Equal(64, GitTrust.Mark([]).Length);
    }
}
