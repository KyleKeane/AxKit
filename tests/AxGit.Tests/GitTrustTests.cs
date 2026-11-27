using AxGit.Core;

namespace AxGit.Tests;

/// <summary>A repository's own programs (SPEC.md AG-1.6, security audit S-2): what is found, and how it is set aside.</summary>
public sealed class GitTrustTests
{
    [Fact]
    public void The_names_that_run_a_program_are_found_and_the_rest_left()
    {
        const string names = "core.repositoryformatversion\ncore.fsmonitor\ndiff.pdf.textconv\nfilter.lfs.clean\nremote.origin.url\nuser.name\ncore.sshCommand\ncredential.helper\n";
        Assert.Equal(["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper"], GitTrust.Risks(names, null));
    }

    [Fact]
    public void A_hook_is_a_risk_and_a_sample_is_not()
    {
        using var temp = new TempFolder("axgit-hooks");
        var hooks = temp.Path;
        File.WriteAllText(Path.Combine(hooks, "pre-commit.sample"), string.Empty);
        File.WriteAllText(Path.Combine(hooks, "post-checkout"), string.Empty);
        Assert.Equal(["hook post-checkout"], GitTrust.Risks(string.Empty, hooks));
    }

    [Fact]
    public void An_untrusted_repository_runs_git_with_each_program_set_to_nothing()
    {
        Assert.Equal(["-c", "diff.pdf.textconv=", "-c", "core.sshCommand=", "-c", "core.hooksPath=NUL"], GitTrust.Overrides(["diff.pdf.textconv", "hook pre-commit", "core.sshCommand"]));
        Assert.Empty(GitTrust.Overrides([]));
    }

    [Fact]
    public void Every_command_keeps_the_file_monitor_and_the_ext_transport_off()
    {
        var command = GitRunner.Command(@"C:\repo", ["status"], ["-c", "core.sshCommand="]);
        Assert.Equal(["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "core.sshCommand=", "status"], command);
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
    public void A_name_that_makes_git_run_a_program_is_one(string name) => Assert.True(GitTrust.RunsAProgram(name));

    [Theory]
    [InlineData("remote.origin.url")]
    [InlineData("remote.origin.pushurl")]
    [InlineData("url.https://example.org/.insteadOf")]
    [InlineData("include.path")]
    [InlineData("includeIf.gitdir:C:/work/.path")]
    [InlineData("gpg.format")]
    [InlineData("gpg.ssh.allowedSignersFile")]
    [InlineData("diff.tool")]
    [InlineData("user.signingkey")]
    // No command axGIT runs reaches these (GitTrust's regex says why), so they are not set aside.
    [InlineData("alias.co")]
    [InlineData("submodule.libs/zip.update")]
    public void A_name_that_runs_no_program_in_axGITs_commands_is_none(string name) => Assert.False(GitTrust.RunsAProgram(name));

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
        Assert.Equal([zip, inner, plain], GitTrust.ModuleFolders(temp.Path));
        Assert.Empty(GitTrust.ModuleFolders(Path.Combine(temp.Path, "no-such-folder")));
    }

    /// <summary>
    /// A name found in a submodule is set to nothing once for every run, since the command line's settings reach
    /// git's runs inside a submodule; a submodule's hook, and one whose settings could not be read, turn the hooks off.
    /// </summary>
    [Fact]
    public void What_a_submodule_would_run_is_named_with_it_and_set_aside_with_the_rest()
    {
        var inside = GitTrust.Inside("libs/zip", ["core.sshCommand", "hook post-checkout"]);
        Assert.Equal(["submodule libs/zip: core.sshCommand", "submodule libs/zip: hook post-checkout"], inside);
        Assert.Equal(
            ["-c", "core.sshcommand=", "-c", "filter.x.smudge=", "-c", "core.hooksPath=NUL"],
            GitTrust.Overrides(["core.sshcommand", "submodule libs/zip: core.sshcommand", "submodule libs/zip: filter.x.smudge", "submodule libs/zip: hook post-checkout"]));
        // A name's middle part is another name in other case, so both are set aside.
        Assert.Equal(["-c", "diff.pdf.textconv=", "-c", "diff.PDF.textconv="], GitTrust.Overrides(["diff.pdf.textconv", "diff.PDF.textconv"]));
        Assert.Equal(["diff.pdf.textconv", "diff.PDF.textconv"], GitTrust.Risks("local\tdiff.pdf.textconv\nlocal\tdiff.PDF.textconv\nworktree\tdiff.pdf.textconv\n", null));
        Assert.Equal(["-c", "core.hooksPath=NUL"], GitTrust.Overrides(GitTrust.Inside("gone", [GitTrust.NotRead])));
        // A trust is for exactly these: a submodule's risk is in the mark.
        Assert.NotEqual(GitTrust.Mark(["core.sshCommand"]), GitTrust.Mark(["core.sshCommand", .. inside]));
    }
}
