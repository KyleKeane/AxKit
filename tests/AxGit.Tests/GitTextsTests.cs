using System.Text.RegularExpressions;
using AxGit.Core;

namespace AxGit.Tests;

/// <summary>
/// Git's words stay in their one place (root CLAUDE.md rule 12; docs/axKit/access-layers.md): the window writes no
/// option of git's and matches no text of git's, and each of the most characteristic words stands in the one file of
/// <c>src/AxGit.Core</c> that owns it. Read off the sources with <see cref="SourceTexts"/>; the commands of
/// <see cref="GitTexts"/> are pinned here too, so that a move changes nothing git is told.
/// </summary>
public sealed partial class GitTextsTests
{
    /// <summary>What a window file must not hold in a string: words only git's command line and output have.</summary>
    private static readonly string[] NeverInTheWindow =
    [
        "porcelain", "rev-parse", "get-url", "%(", "%x09", "# branch.", "GIT_", "core.", "color.ui", "HEAD",
    ];

    /// <summary>The characteristic words, each with the files of src/AxGit.Core that may hold it.</summary>
    private static readonly (string Text, string[] Files)[] OnePlace =
    [
        ("--porcelain", ["GitStatus.cs"]),
        ("# branch.", ["GitStatus.cs"]),
        ("--untracked-files", ["GitStatus.cs"]),
        ("%x09", ["GitLog.cs"]),
        ("%(refname", ["GitBranches.cs"]),
        ("get-url", ["GitRemote.cs"]),
        ("--no-includes", ["GitTrust.cs"]),
        ("--refmap=", ["GitTexts.cs"]),
        // The parser's run with no repository, and the variables a run in a repository is given none of (P1).
        ("GIT_DIR", ["GitTrustScan.cs", "GitRunner.cs"]),
        ("GIT_TERMINAL_PROMPT", ["GitRunner.cs"]),
        ("core.quotepath", ["GitRunner.cs"]),
        ("--no-ext-diff", ["GitStatus.cs", "GitLog.cs"]),
        ("--ff-only", ["GitTexts.cs"]),
        ("--prune", ["GitTexts.cs"]),
        ("--set-upstream", ["GitTexts.cs"]),
        ("Already up to date", ["GitTexts.cs"]),
    ];

    /// <summary>An option as git takes one: one or two dashes, then a letter.</summary>
    [GeneratedRegex(@"^--?[A-Za-z]")]
    private static partial Regex Option();

    [Fact]
    public void The_window_writes_no_option_and_no_word_of_gits()
    {
        var offenders = SourceTexts.LiteralsUnder("src/AxGit")
            .Where(literal => Option().IsMatch(literal.Text) || NeverInTheWindow.Any(text => literal.Text.Contains(text, StringComparison.Ordinal)))
            .Select(literal => literal.ToString())
            .ToList();
        Assert.True(offenders.Count == 0, "A word of git's in a window file; it belongs in src/AxGit.Core (rule 12): " + string.Join("; ", offenders));
    }

    /// <summary>The window chooses no announcement by git's output itself: no pattern, and no search in a result's text.</summary>
    [Fact]
    public void The_window_matches_no_text_of_gits()
    {
        var root = SourceTexts.RepositoryRoot();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src", "AxGit"), "*.cs"))
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("Regex", source);
            Assert.DoesNotContain("Output.Contains(", source);
            Assert.DoesNotContain("Error.Contains(", source);
            Assert.DoesNotContain("AllText.Contains(", source);
            Assert.DoesNotContain("FirstLine.Contains(", source);
        }
    }

    [Fact]
    public void Each_characteristic_word_is_written_in_its_own_file_of_the_core()
    {
        var literals = SourceTexts.LiteralsUnder("src/AxGit.Core");
        foreach (var (text, files) in OnePlace)
        {
            var holders = literals.Where(literal => literal.Text.Contains(text, StringComparison.Ordinal)).ToList();
            Assert.True(holders.Count > 0, $"\"{text}\" is watched and written nowhere: the list is stale.");
            var strays = holders.Where(literal => !files.Contains(Path.GetFileName(literal.File))).Select(literal => literal.ToString()).ToList();
            Assert.True(strays.Count == 0, $"\"{text}\" belongs in {string.Join(" or ", files)} alone (rule 12): " + string.Join("; ", strays));
        }
    }

    [Fact]
    public void The_commands_on_the_whole_repository_are_what_they_were()
    {
        Assert.Equal(["add", "-A"], GitTexts.StageAllArguments);
        Assert.Equal(["restore", "--staged", "."], GitTexts.UnstageAllArguments);
        Assert.Equal(["commit", "-m", "Fix the thing"], GitTexts.CommitArguments("Fix the thing"));
        // Never into the submodules, whose remotes are servers the consent did not name (2026-11-27). The remote
        // the consent was read for is on the command line, after "--", and fetch.all is not obeyed (AG-5.4a).
        // The upload program is git's own on the command line, which comes before a remote's setting (D2).
        // Since the fourth pass (T3) each names its own line with an empty refmap: no line of the repository's
        // settings or of a legacy remotes file applies (M1).
        Assert.Equal(["fetch", "--prune", "--no-recurse-submodules", "--no-all", "--upload-pack=git-upload-pack", "--refmap=", "--", "origin", "+refs/heads/*:refs/remotes/origin/*"], GitTexts.FetchArguments("origin"));
        Assert.Equal(["pull", "--ff-only", "--no-recurse-submodules", "--no-all", "--upload-pack=git-upload-pack", "--refmap=", "--", "-x", "+refs/heads/main:refs/remotes/-x/main"], GitTexts.PullArguments("-x", "refs/heads/main"));
        // From the repository itself no remote branch has the name: the branches are read, and the pull names the branch followed.
        Assert.Equal("HEAD", GitTexts.FetchArguments(".")[^1]);
        Assert.Equal("refs/heads/main", GitTexts.PullArguments(".", "refs/heads/main")![^1]);
        // A branch that follows none is not pulled: nothing names what to pull.
        Assert.Null(GitTexts.PullArguments("origin", null));
    }

    /// <summary>T3: the branch a pull names is read from the settings, and one in a form no plain branch has is no branch to pull.</summary>
    [Fact]
    public void The_branch_a_pull_names_is_the_one_followed_in_a_plain_form()
    {
        var settings = GitRemote.Settings("local\0branch.main.merge\nrefs/heads/main\0local\0branch.odd.merge\nrefs/heads/a:refs/heads/b\0local\0branch.plus.merge\n+refs/heads/x\0local\0branch.star.merge\nrefs/heads/*\0local\0branch.tag.merge\nrefs/tags/v1\0");
        Assert.Equal("refs/heads/main", GitTexts.MergeOf(settings, "main"));
        Assert.Null(GitTexts.MergeOf(settings, "odd"));
        Assert.Null(GitTexts.MergeOf(settings, "plus"));
        Assert.Null(GitTexts.MergeOf(settings, "star"));
        Assert.Null(GitTexts.MergeOf(settings, "none"));
        Assert.Equal("+refs/tags/v1:refs/remotes/origin/tags/v1", GitTexts.PullArguments("origin", GitTexts.MergeOf(settings, "tag"))![^1]);
    }

    /// <summary>
    /// The commands on several files (AG-3.2a, AG-3.4a): every path after "--", so that a file named like an option
    /// is a file; nothing but the paths given; the discard of the index and the working tree together.
    /// </summary>
    [Fact]
    public void The_commands_on_several_files_put_every_path_after_the_two_dashes()
    {
        string[] paths = ["src/a.cs", "--force", "notes with spaces.txt"];
        Assert.Equal(["add", "--", "src/a.cs", "--force", "notes with spaces.txt"], GitTexts.StageArguments(paths));
        Assert.Equal(["restore", "--staged", "--", "src/a.cs", "--force", "notes with spaces.txt"], GitTexts.UnstageArguments(paths));
        Assert.Equal(["restore", "--staged", "--worktree", "--", "src/a.cs", "--force", "notes with spaces.txt"], GitTexts.DiscardArguments(paths));
        // Fifty paths a call: far under what one Windows command line holds, whatever the paths' lengths.
        Assert.Equal(50, GitTexts.PathsPerCall);
    }

    [Fact]
    public void A_push_ties_a_branch_without_an_upstream_to_origin()
    {
        // The remote is always on the command line (AG-5.4a): git chooses none itself.
        // The branch is named on both sides, whatever push.default says (D6), and the receive program is git's own (D2).
        Assert.Equal(["push", "--no-recurse-submodules", "--receive-pack=git-receive-pack", "--", "backup", "refs/heads/main:refs/heads/main"], GitTexts.PushArguments("backup", "origin/main", "main"));
        Assert.Equal(["push", "--no-recurse-submodules", "--receive-pack=git-receive-pack", "--set-upstream", "--", "origin", "refs/heads/topic:refs/heads/topic"], GitTexts.PushArguments("origin", null, "topic"));
        // A branch named +main is that branch, never a forced push of main (M4).
        Assert.Equal("refs/heads/+main:refs/heads/+main", GitTexts.PushArguments("origin", "origin/+main", "+main")[^1]);
    }

    /// <summary>D6: a branch that follows a branch of another name is not pushed; one that follows its own name, or none, is.</summary>
    [Fact]
    public void A_branch_that_follows_a_branch_of_another_name_is_not_pushed()
    {
        var settings = GitRemote.Settings("local\0branch.main.merge\nrefs/heads/other\0local\0branch.topic.merge\nrefs/heads/topic\0");
        Assert.Contains("other", GitTexts.FollowsAnotherName(settings, "main"));
        Assert.Null(GitTexts.FollowsAnotherName(settings, "topic"));
        Assert.Null(GitTexts.FollowsAnotherName(settings, "free"));
    }

    /// <summary>The remote the consent before a push is asked for is the one the push reaches (AX-11.5, 2026-11-27).</summary>
    [Fact]
    public void The_remote_of_a_push_is_origin_where_the_push_names_it_and_the_settings_choice_otherwise()
    {
        var settings = GitRemote.Settings("local\0branch.main.remote\norigin\0local\0branch.main.pushremote\nbackup\0local\0branch.topic.pushremote\nbackup\0");
        // A branch without an upstream is pushed to origin by name, whatever else is set.
        Assert.Equal("origin", GitTexts.PushRemote(null, "topic", settings));
        // With an upstream: the branch's pushRemote or remote.pushDefault first.
        Assert.Equal("backup", GitTexts.PushRemote("origin/main", "main", settings));
        Assert.Equal("origin", GitTexts.PushRemote("origin/other", "other", settings));
    }

    [Fact]
    public void A_pull_that_brought_nothing_is_known_by_gits_line()
    {
        Assert.True(GitTexts.SaysUpToDate("Already up to date.\n"));
        Assert.True(GitTexts.SaysUpToDate("Already up-to-date.\nAlready up to date.\n"));
        Assert.True(GitTexts.SaysUpToDate("already up to date"));
        Assert.False(GitTexts.SaysUpToDate("Updating 3a1f2c0..9b8e7d6\nFast-forward\n a.txt | 1 +\n"));
    }

    [Fact]
    public void The_hash_of_a_new_commit_is_read_from_gits_summary_line()
    {
        Assert.Equal("3a1f2c0", GitTexts.CommittedHash("[main 3a1f2c0] Fix the thing\n 1 file changed, 1 insertion(+)\n"));
        Assert.Equal("9b8e7d6", GitTexts.CommittedHash("[main (root-commit) 9b8e7d6] First\n"));
        Assert.Equal("3a1f2c0", GitTexts.CommittedHash("warning: something\n[topic 3a1f2c0] Subject\n"));
        Assert.Null(GitTexts.CommittedHash("nothing to commit, working tree clean\n"));
    }
}
