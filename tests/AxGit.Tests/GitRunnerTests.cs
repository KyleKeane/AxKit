using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>The pure parts of running git and the settings (docs/axGIT/SPEC.md AG-1.5, AG-8.1); no git process here.</summary>
public sealed class GitRunnerTests
{
    [Fact]
    public void A_command_names_the_repository_and_turns_colour_and_quoting_off()
    {
        Assert.Equal(
            ["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "maintenance.auto=false", "-c", "gc.auto=0", "-c", "core.unsetenvvars=PERL5LIB", "-c", "http.followRedirects=false", "status", "--porcelain=v2"],
            GitRunner.Command(@"C:\repo", ["status", "--porcelain=v2"]));
        Assert.Equal(["-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "maintenance.auto=false", "-c", "gc.auto=0", "-c", "core.unsetenvvars=PERL5LIB", "-c", "http.followRedirects=false", "--version"], GitRunner.Command(null, ["--version"]));
    }

    [Fact]
    public void A_result_knows_its_first_line_and_all_its_text()
    {
        var failed = new GitResult(1, "", "\nerror: pathspec 'x' did not match\nhint: more\n");
        Assert.False(failed.Ok);
        Assert.Equal("error: pathspec 'x' did not match", failed.FirstLine);
        Assert.Equal("error: pathspec 'x' did not match\nhint: more", failed.AllText);
        Assert.Equal("Already up to date.", new GitResult(0, "Already up to date.\n", "").FirstLine);
        Assert.Equal("git failed", new GitResult(128, "", "").FirstLine);
    }

    /// <summary>
    /// A run that was stopped at the time limit is no success, whatever it printed and whatever its exit code reads
    /// (the review of 2026-11-27); one git finished, with its output held open by a program it started, is git's
    /// own result, whole.
    /// </summary>
    [Fact]
    public void A_stopped_run_is_never_ok_and_a_finished_one_with_its_output_held_open_is_gits_own()
    {
        var stopped = GitRunner.Result(new ProcessResult(-1, "1 .M N... 100644 100644 100644 a b src/Foo.cs\n", "warning: slow\n", TimedOut: true, Finished: false), TimeSpan.FromSeconds(60));
        Assert.False(stopped.Ok);
        Assert.True(stopped.TimedOut);
        Assert.Equal(-1, stopped.ExitCode);
        Assert.Equal("warning: slow\ngit did not finish within 60 seconds and was stopped.", stopped.Error);
        // Even a zero exit code would not make it one.
        Assert.False(new GitResult(0, string.Empty, string.Empty, TimedOut: true).Ok);

        var held = GitRunner.Result(new ProcessResult(0, "[main 3a1f2c0] Subject\n", string.Empty, TimedOut: true, Finished: true), TimeSpan.FromSeconds(60));
        Assert.True(held.Ok);
        Assert.False(held.TimedOut);
        Assert.True(held.OutputHeldOpen);
        Assert.Equal("[main 3a1f2c0] Subject\n", held.Output);
        Assert.Contains("kept its output open", held.Error);

        var failedAndHeld = GitRunner.Result(new ProcessResult(1, string.Empty, "error: gpg failed to sign the data\n", TimedOut: true, Finished: true), TimeSpan.FromSeconds(60));
        Assert.False(failedAndHeld.Ok);
        Assert.Equal("error: gpg failed to sign the data", failedAndHeld.FirstLine);

        var plain = GitRunner.Result(new ProcessResult(0, "out", "err", TimedOut: false, Finished: true), TimeSpan.FromSeconds(60));
        Assert.Equal(new GitResult(0, "out", "err"), plain);
    }

    [Fact]
    public async Task An_untracked_file_is_read_no_further_than_what_is_shown()
    {
        using var temp = new TempFolder("axgit-untracked");
        var small = temp.File("small.txt", "one\ntwo\nthree");
        var (text, cut) = await UntrackedFile.ReadAsync(small);
        Assert.Equal(("one\ntwo\nthree", false), (text, cut));
        Assert.Equal("Untracked file, 3 lines\none\ntwo\nthree", UntrackedFile.Details(text, cut));
        Assert.Equal("Untracked file, 0 lines\n", UntrackedFile.Details(string.Empty, false));

        // Longer than the limit: the limit's characters, and that there is more.
        var large = temp.File("large.txt", string.Concat(Enumerable.Repeat("0123456789\n", 100)));
        (text, cut) = await UntrackedFile.ReadAsync(large, limit: 22);
        Assert.Equal(("0123456789\n0123456789\n", true), (text, cut));
        Assert.Equal("Untracked file, the first 3 lines\n0123456789\n0123456789\n\n(cut after 200 KB)", UntrackedFile.Details(text, cut));
        // Exactly the limit: all of it, nothing cut.
        (text, cut) = await UntrackedFile.ReadAsync(small, limit: 13);
        Assert.Equal(("one\ntwo\nthree", false), (text, cut));
        Assert.Equal(200 * 1024, UntrackedFile.Limit);
    }

    [Fact]
    public void Recent_repositories_are_kept_newest_first_without_repeats()
    {
        var settings = new GitSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.RememberRepository($@"C:\repo{i}");
        }

        settings.RememberRepository(@"c:\REPO11");
        Assert.Equal(RecentPaths.Max, settings.RecentRepositories.Count);
        Assert.Equal(@"c:\REPO11", settings.RecentRepositories[0]);
        Assert.Equal(@"C:\repo10", settings.RecentRepositories[1]);
        Assert.DoesNotContain(@"C:\repo0", settings.RecentRepositories);
    }
}
