using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// What the tests that run git itself share (the review's third pass of 2026-11-27, L7): the variables of every
/// run, given to each run and never set for the test process (another class's git would read them), and the
/// facts that are said as skipped where git or Git LFS is not installed, never passed in silence.
/// </summary>
internal static class GitTests
{
    /// <summary>Whether git is where the runner finds it.</summary>
    public static bool GitThere => File.Exists(GitRunner.Program);

    private static readonly object HooksGate = new();
    private static TempFolder? _hooks;

    /// <summary>
    /// L3 (the review's fourth pass): the tests' runs get a hooks folder of the tests' own, never axKit's
    /// <c>%LOCALAPPDATA%\AxKit\axGIT-no-hooks</c>, in a folder deleted when the test process ends, and a fallback
    /// folder of theirs carries the run's mark, which test.ps1 counts. Once for the process; every class that runs
    /// git or asks for the folder calls it first.
    /// </summary>
    public static void UseOwnHooksFolder()
    {
        lock (HooksGate)
        {
            if (_hooks is not null)
            {
                return;
            }

            var folder = new TempFolder("axgit-own-hooks");
            _hooks = folder;
            GitTrust.NoHooksFolder = Path.Combine(folder.Path, "no-hooks");
            GitTrust.FallbackPrefix = "axGIT-no-hooks-" + TempFolder.Run;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => folder.Dispose();
        }
    }

    /// <summary>
    /// The variables of a test's git runs: neither the computer's settings nor the user's are read, a file of the
    /// test's stands for the user's (Git LFS's filter set up as its installer sets it, the file transport
    /// allowed); every connection goes through a proxy on a closed port of this computer, 127.0.0.1:9, so that
    /// an address under "invalid" reaches nothing even where a resolver answers for it.
    /// </summary>
    public static Dictionary<string, string> Environment(TempFolder folder)
    {
        UseOwnHooksFolder();
        var users = folder.File("users.gitconfig",
            "[user]\n\tname = test\n\temail = test\n[init]\n\tdefaultBranch = main\n[protocol \"file\"]\n\tallow = always\n"
            + "[uploadpack]\n\tallowFilter = true\n"
            + "[filter \"lfs\"]\n\tclean = git-lfs clean -- %f\n\tsmudge = git-lfs smudge -- %f\n\tprocess = git-lfs filter-process\n\trequired = true\n");
        return new Dictionary<string, string>
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_GLOBAL"] = users,
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["HTTP_PROXY"] = "http://127.0.0.1:9",
            ["HTTPS_PROXY"] = "http://127.0.0.1:9",
            ["ALL_PROXY"] = "http://127.0.0.1:9",
            ["NO_PROXY"] = string.Empty,
        };
    }

    /// <summary>
    /// Every command axGIT's window issues in a repository, in the order a session of it would: the three lists, a
    /// file's diff, a commit shown, stage, unstage, stage all, unstage all, a commit of everything, a new branch and
    /// a switch back, a discard, the reads before a fetch, pull and push (the settings, the addresses), and the three
    /// themselves to <paramref name="remote"/>, which follows main. <paramref name="run"/> runs one, with whether it
    /// changes the repository; each command and its result come back, for the class tests (<c>GitTrustScanTests</c>,
    /// <c>GitSessionTests</c>). The repository has a.txt and b.txt committed on main.
    /// </summary>
    public static async Task<List<(IReadOnlyList<string> Arguments, GitResult Result)>> EveryCommandAsync(string root, string remote, Func<IReadOnlyList<string>, bool, Task<GitResult>> run)
    {
        var done = new List<(IReadOnlyList<string>, GitResult)>();
        async Task<GitResult> One(IReadOnlyList<string> arguments, bool change = false)
        {
            var result = await run(arguments, change);
            done.Add((arguments, result));
            return result;
        }

        File.AppendAllText(Path.Combine(root, "a.txt"), "changed\n");
        var status = GitStatus.Parse((await One(GitStatus.Arguments)).Output);
        var log = GitLog.Parse((await One(GitLog.Arguments)).Output);
        await One(GitBranches.Arguments);
        await One(status.Files.FirstOrDefault(file => file.Path == "a.txt")?.DiffArguments ?? ["diff", "--no-ext-diff", "--no-textconv", "--", "a.txt"]);
        await One(log.FirstOrDefault()?.ShowArguments ?? ["show", "--no-ext-diff", "--no-textconv", "--stat", "--format=medium", "HEAD"]);
        await One(GitTexts.StageArguments(["a.txt"]), change: true);
        await One(GitTexts.UnstageArguments(["a.txt"]), change: true);
        await One(GitTexts.StageAllArguments, change: true);
        await One(GitTexts.UnstageAllArguments, change: true);
        await One(GitTexts.StageAllArguments, change: true);
        await One(GitTexts.CommitArguments("three"), change: true);
        await One(GitBranches.CreateArguments("topic"), change: true);
        await One(GitBranches.Arguments);
        await One(["switch", "main"], change: true);
        File.AppendAllText(Path.Combine(root, "b.txt"), "changed\n");
        await One(GitTexts.DiscardArguments(["b.txt"]), change: true);
        await One(GitRemote.SettingsArguments);
        await One(GitRemote.UrlArguments(remote, push: false));
        await One(GitRemote.UrlArguments(remote, push: true));
        await One(GitTexts.FetchArguments(remote), change: true);
        await One(GitTexts.PushArguments(remote, $"{remote}/main", "main"), change: true);
        await One(GitTexts.PullArguments(remote, "refs/heads/main")!, change: true);
        return done;
    }

    /// <summary>The variables put into a process of git's started by a test itself, as a console runs git.</summary>
    public static void Put(ProcessStartInfo start, IReadOnlyDictionary<string, string> environment)
    {
        foreach (var (name, value) in environment)
        {
            start.Environment[name] = value;
        }
    }

    /// <summary>Whether Git LFS is installed, as git finds it.</summary>
    public static bool LfsThere
    {
        get
        {
            if (!GitThere)
            {
                return false;
            }

            var start = new ProcessStartInfo(GitRunner.Program);
            start.ArgumentList.Add("lfs");
            start.ArgumentList.Add("version");
            return ProcessRun.Run(start, [], TimeSpan.FromSeconds(30)) is { ExitCode: 0, Stopped: false };
        }
    }
}

/// <summary>A test of git itself, said as skipped where git is not installed.</summary>
internal sealed class GitFactAttribute : FactAttribute
{
    public GitFactAttribute()
    {
        if (!GitTests.GitThere)
        {
            Skip = "git is not installed where axGIT finds it: the test was not run against git on this computer.";
        }
    }
}

/// <summary>A test of git and Git LFS, said as skipped where either is not installed.</summary>
internal sealed class GitLfsFactAttribute : FactAttribute
{
    public GitLfsFactAttribute()
    {
        if (!GitTests.LfsThere)
        {
            Skip = "git or Git LFS is not installed: the test was not run against them on this computer.";
        }
    }
}
