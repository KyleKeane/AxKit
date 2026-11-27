using AxGit.Core;

namespace AxGit.Tests;

/// <summary>A repository's own programs and settings (SPEC.md AG-1.6, AG-1.6c; security audit S-2): what is found, and how it is set aside.</summary>
public sealed class GitTrustTests
{
    [Fact]
    public void Every_name_that_is_not_known_to_be_harmless_is_a_risk_and_the_harmless_ones_are_left()
    {
        const string names = "core.repositoryformatversion\ncore.fsmonitor\ndiff.pdf.textconv\nfilter.lfs.clean\nremote.origin.url\nuser.name\ncore.sshCommand\ncredential.helper\nlfs.extension.x.clean\ngc.recentObjectsHook\n";
        IReadOnlyList<string> risks = ["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper", "lfs.extension.x.clean", "gc.recentObjectsHook"];
        Assert.Equal(risks, GitTrust.Risks(names, null));
        // The notice says which of them it knows to run a program, and which it does not know.
        Assert.Equal(["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper"], GitTrust.Programs(risks));
        Assert.Equal(["lfs.extension.x.clean", "gc.recentObjectsHook"], GitTrust.NotKnown(risks));
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
        Assert.Empty(GitTrust.Risks("local\tcore.worktree\nlocal\tcore.bare\n", null, inside: true));
        Assert.Equal(["core.worktree"], GitTrust.Risks("local\tcore.worktree\nlocal\tcore.bare\n", null));
    }

    [Fact]
    public void A_hook_is_a_risk_and_a_sample_is_not()
    {
        using var temp = new TempFolder("axgit-hooks");
        var hooks = temp.Path;
        File.WriteAllText(Path.Combine(hooks, "pre-commit.sample"), string.Empty);
        File.WriteAllText(Path.Combine(hooks, "post-checkout"), string.Empty);
        Assert.Equal(["hook post-checkout"], GitTrust.Risks(string.Empty, hooks));
        Assert.Equal(["hook post-checkout"], GitTrust.Programs(["hook post-checkout"]));
    }

    // ---- What git is run with while a repository is not trusted ----

    private static GitSetting Empty(string name) => new(name, string.Empty);

    /// <summary>No hooks and no credential helper, for every repository that is not trusted, last in the list (an empty helper empties git's list of them).</summary>
    private static readonly GitSetting[] Careful = [new("credential.helper", string.Empty), new("core.hooksPath", Path.GetFullPath(GitTrust.NoHooksFolder))];

    [Fact]
    public void An_untrusted_repository_runs_git_with_each_program_set_to_nothing_and_without_hooks_or_credential_helpers()
    {
        Assert.Equal(Careful, GitTrust.Careful);
        Assert.Equal([Empty("diff.pdf.textconv"), Empty("core.sshCommand"), .. Careful], GitTrust.Overrides(["diff.pdf.textconv", "hook pre-commit", "core.sshCommand"], ["lfs"]));
        // The hooks folder a repository names is not set to nothing: it is set to the folder that holds none.
        Assert.Equal(Careful, GitTrust.Overrides(["core.hookspath"], []));
        // A repository that holds no risk, and a trusted one, runs with nothing of axGIT's.
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
        // A repository inside whose settings could not be read is not known either.
        Assert.Contains(Empty("filter.lfs.process"), GitTrust.Overrides(GitTrust.Inside("gone", [GitTrust.NotRead]), ["lfs"]));
        // Hooks and known programs alone leave the user's filters running: nothing unknown is there for them to obey.
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

    [Fact]
    public void The_filters_git_knows_are_those_of_every_file_each_once()
    {
        const string listed = "system\tfilter.lfs.clean\nsystem\tfilter.lfs.smudge\nsystem\tfilter.lfs.process\nsystem\tfilter.lfs.required\nglobal\tfilter.Crypt.Smudge\nlocal\tfilter.a=b.clean\nlocal\tfilter.dotted.name.process\nlocal\tfilter.only.required\nlocal\tcore.bare\n";
        Assert.Equal(["lfs", "Crypt", "a=b", "dotted.name"], GitTrust.Filters(listed));
        Assert.Empty(GitTrust.Filters(string.Empty));
    }

    /// <summary>
    /// Every command keeps the file monitor and the ext transport off, and git's automatic maintenance (finding
    /// 2: it runs the program of gc.recentObjectsHook, which an empty value does not set aside). What is set
    /// aside for a repository is no argument (finding 3): it is in the environment.
    /// </summary>
    [Fact]
    public void Every_command_keeps_the_file_monitor_the_ext_transport_and_automatic_maintenance_off()
    {
        var command = GitRunner.Command(@"C:\repo", ["status"]);
        Assert.Equal(
            ["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "maintenance.auto=false", "-c", "gc.auto=0", "status"],
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

    // ---- The review of 2026-11-27: the work tree's own settings, the names that were missing, the submodules ----

    /// <summary>
    /// The settings are asked with their scope, and the repository's are every scope but the computer's, the user's
    /// and the command line's: the work tree's own file (extensions.worktreeConfig) is the repository's too, which
    /// "--local", asked until then, left out.
    /// </summary>
    [Fact]
    public void The_work_trees_own_settings_count_and_the_users_and_the_command_lines_do_not()
    {
        Assert.Equal(["config", "--show-scope", "--includes", "--name-only", "--list"], GitTrust.ConfigArguments);
        const string listed = "system\tcore.editor\nglobal\tcredential.helper\nlocal\tcore.bare\nlocal\textensions.worktreeconfig\nworktree\tcore.sshcommand\ncommand\tcore.fsmonitor\nlocal\tfilter.lfs.clean\r\n";
        Assert.Equal(["core.bare", "extensions.worktreeconfig", "core.sshcommand", "filter.lfs.clean"], GitTrust.OwnNames(listed));
        Assert.Equal(["core.sshcommand", "filter.lfs.clean"], GitTrust.Risks(listed, null));
        // A scope this code does not know is the repository's, never passed over.
        Assert.Equal(["core.pager"], GitTrust.Risks("somewhere\tcore.pager\n", null));
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
    // Plain settings the older rule named as programs (every name under uploadpack and receivepack).
    [InlineData("uploadpack.allowFilter")]
    [InlineData("receivepack.denyCurrentBranch")]
    public void A_name_that_is_no_program_by_its_name_is_not_called_one(string name)
    {
        Assert.False(GitTrust.RunsAProgram(name));
        Assert.Equal([name], GitTrust.NotKnown(GitTrust.Risks(name + "\n", null)));
    }

    [Fact]
    public void The_entries_of_the_index_that_are_another_repository_are_found()
    {
        var index = "100644 587be6b4c3f93f93c489c0111bba5596147a26cb 0\t.gitmodules\0"
            + "160000 e6701ffa59157d27c7ab2f8e25cc97b87fa5512b 0\tlibs/zip\0"
            + "100755 587be6b4c3f93f93c489c0111bba5596147a26cb 0\tbuild 160000 notes.txt\0"
            + "160000 e6701ffa59157d27c7ab2f8e25cc97b87fa5512b 0\tvendor/a b\tc\0";
        Assert.Equal(["libs/zip", "vendor/a b\tc"], GitTrust.Links(index));
        Assert.Empty(GitTrust.Links(string.Empty));
        Assert.Equal(["ls-files", "--stage", "-z"], GitTrust.LinksArguments);
    }

    /// <summary>
    /// Finding 4: a folder with a repository of its own that the index does not hold. git lists it as one entry
    /// with a slash at its end, since it does not go into it; an untracked file never ends in one.
    /// </summary>
    [Fact]
    public void The_untracked_folders_that_are_a_repository_of_their_own_are_found()
    {
        Assert.Equal(["ls-files", "--others", "--exclude-standard", "-z"], GitTrust.OthersArguments);
        Assert.Equal(["vendor", "deep/in side/tool"], GitTrust.Nested("notes.txt\0vendor/\0src/new.cs\0deep/in side/tool/\0/\0"));
        Assert.Empty(GitTrust.Nested(string.Empty));
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
        Assert.Equal(["diff.pdf.textconv", "diff.PDF.textconv"], GitTrust.Risks("local\tdiff.pdf.textconv\nlocal\tdiff.PDF.textconv\nworktree\tdiff.pdf.textconv\n", null));
        Assert.Equal(GitTrust.Inside("gone", [GitTrust.NotRead]), GitTrust.NotKnown(GitTrust.Inside("gone", [GitTrust.NotRead])));
        // A trust is for exactly these: a submodule's risk is in the mark.
        Assert.NotEqual(GitTrust.Mark(["core.sshCommand"]), GitTrust.Mark(["core.sshCommand", .. inside]));
    }

    /// <summary>
    /// A trust given before this rule stays good for a repository that holds nothing the older rule did not name:
    /// the mark is of the same names, written the same way.
    /// </summary>
    [Fact]
    public void The_mark_of_hooks_and_programs_is_what_it_was()
    {
        Assert.Equal("A233B165D8D6FC569F9D9A8C10FF20BEB2AE4D2D562C7ADA13685A0BED426A94", GitTrust.Mark(["core.hookspath", "hook pre-push"]));
        Assert.Equal(GitTrust.Mark(["core.hookspath", "hook pre-push"]), GitTrust.Mark(GitTrust.Risks("local\tcore.hookspath\nlocal\tcore.bare\nlocal\tbranch.main.remote\n", null).Append("hook pre-push").ToList()));
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

        // Every driver section with every variable that names a program.
        Assert.Contains("mergetool.x.cmd", GitTrust.ProgramNamesTable.SelectMany(kind => kind.Examples));
    }

    /// <summary>L4: the hooks folder of an untrusted repository is a full path of axKit's own, and is emptied before it is given.</summary>
    [Fact]
    public void The_hooks_folder_of_an_untrusted_repository_is_a_full_path_and_empty()
    {
        var folder = GitTrust.EmptyHooksFolder();
        Assert.True(Path.IsPathFullyQualified(folder), folder);
        File.WriteAllText(Path.Combine(folder, "pre-commit"), "#!/bin/sh\n");
        Directory.CreateDirectory(Path.Combine(folder, "inside"));
        Assert.Equal(folder, GitTrust.EmptyHooksFolder());
        Assert.Empty(Directory.EnumerateFileSystemEntries(folder));
        Assert.Equal(new GitSetting("core.hooksPath", folder), GitTrust.Careful[1]);
    }

    /// <summary>
    /// D5, M3: where the value decides the harm, the value is read. A fetch line is harmless only when it writes
    /// under refs/remotes/; a submodule's work tree only on this computer. The user's own and the computer's files
    /// are not the repository's.
    /// </summary>
    [Fact]
    public void A_fetch_that_writes_outside_refs_remotes_and_a_work_tree_on_another_computer_are_risks()
    {
        Assert.Equal(["config", "--show-scope", "--includes", "--null", "--get-regexp", @"^(remote\..+\.fetch|core\.worktree)$"], GitTrust.ValueArguments);
        var output = "local\0remote.origin.fetch\n+refs/heads/*:refs/remotes/origin/*\0"
            + "local\0remote.origin.fetch\n^refs/heads/secret\0"
            + "local\0remote.origin.fetch\nrefs/tags/v1\0"
            + "local\0remote.mirror.fetch\n+refs/heads/*:refs/heads/*\0"
            + "local\0remote.Odd.fetch\nrefs/heads/a:refs/remotes/../heads/a\0"
            + "global\0remote.mine.fetch\n+refs/heads/*:refs/heads/*\0"
            + "local\0core.worktree\n../../../libs/zip\0"
            + "local\0core.worktree\n//server.invalid/share/zip\0";
        Assert.Equal(
            ["remote.mirror.fetch writes outside refs/remotes", "remote.Odd.fetch writes outside refs/remotes", "core.worktree lies on another computer"],
            GitTrust.ValueRisks(output));
        Assert.Empty(GitTrust.ValueRisks(string.Empty));
    }

    [Theory]
    [InlineData(@"\\server\share\x", "server")]
    [InlineData("//Server.Invalid/share", "server.invalid")]
    [InlineData(@"\\?\UNC\host\share", "host")]
    [InlineData(@"\\?\C:\repo", null)]
    [InlineData(@"C:\repo", null)]
    [InlineData("../x", null)]
    [InlineData("/c/repo", null)]
    public void A_share_names_its_computer_and_a_path_here_none(string path, string? host) => Assert.Equal(host, GitTrust.OtherComputer(path));

    /// <summary>
    /// D9: the guard before the first git run reads, in C#, a work tree's .git file, the includes of the settings
    /// (also of a file included) and the alternate object stores, and names the other computer any of them points
    /// at. The share is never touched: these hosts do not exist.
    /// </summary>
    [Fact]
    public void A_repository_whose_own_files_point_at_another_computer_is_known_before_git_runs()
    {
        using var temp = new TempFolder("axgit-elsewhere");
        var plain = Directory.CreateDirectory(Path.Combine(temp.Path, "plain", ".git", "objects")).Parent!.Parent!.FullName;
        File.WriteAllText(Path.Combine(plain, ".git", "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(plain, ".git", "config"), "[core]\n\tbare = false\n[include]\n\tpath = local.cfg\n");
        File.WriteAllText(Path.Combine(plain, ".git", "local.cfg"), "[user]\n\tname = x\n");
        Directory.CreateDirectory(Path.Combine(plain, "src"));
        Assert.Null(GitTrust.PointsElsewhere(Path.Combine(plain, "src")));

        // An include of a file included.
        File.WriteAllText(Path.Combine(plain, ".git", "local.cfg"), "[includeIf \"onbranch:main\"]\n\tpath = \"//included.invalid/share/x.cfg\"\n");
        Assert.Equal("included.invalid", GitTrust.PointsElsewhere(Path.Combine(plain, "src")));
        File.WriteAllText(Path.Combine(plain, ".git", "local.cfg"), "[user]\n\tname = x\n");

        // An alternate object store.
        Directory.CreateDirectory(Path.Combine(plain, ".git", "objects", "info"));
        File.WriteAllText(Path.Combine(plain, ".git", "objects", "info", "alternates"), "# a note\n\\\\objects.invalid\\share\\objects\n");
        Assert.Equal("objects.invalid", GitTrust.PointsElsewhere(plain));
        File.Delete(Path.Combine(plain, ".git", "objects", "info", "alternates"));

        // A work tree whose .git file names a repository on a share.
        var tree = Directory.CreateDirectory(Path.Combine(temp.Path, "tree")).FullName;
        File.WriteAllText(Path.Combine(tree, ".git"), "gitdir: //gitdir.invalid/share/real.git\n");
        Assert.Equal("gitdir.invalid", GitTrust.PointsElsewhere(tree));
        Assert.Null(GitTrust.PointsElsewhere(plain));
    }
}
