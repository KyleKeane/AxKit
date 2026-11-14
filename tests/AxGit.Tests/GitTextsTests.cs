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
        ("rev-parse", ["GitRunner.cs", "GitTrust.cs"]),
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
        Assert.Equal(["fetch", "--prune"], GitTexts.FetchArguments);
        Assert.Equal(["pull", "--ff-only"], GitTexts.PullArguments);
    }

    [Fact]
    public void A_push_ties_a_branch_without_an_upstream_to_origin()
    {
        Assert.Equal(["push"], GitTexts.PushArguments("origin/main", "main"));
        Assert.Equal(["push", "--set-upstream", "origin", "topic"], GitTexts.PushArguments(null, "topic"));
        // A detached HEAD: plain, and git refuses in its own words.
        Assert.Equal(["push"], GitTexts.PushArguments(null, null));
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
    public void The_hooks_folder_is_read_from_gits_one_line()
    {
        Assert.Equal(@"C:\repo\.git\hooks", GitTrust.HooksFolder(".git/hooks\n", @"C:\repo"));
        Assert.Equal(@"D:\shared\hooks", GitTrust.HooksFolder("D:/shared/hooks\r\n", @"C:\repo"));
        Assert.Null(GitTrust.HooksFolder(" \n", @"C:\repo"));
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
