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

    /// <summary>
    /// The variables of a test's git runs: neither the computer's settings nor the user's are read, a file of the
    /// test's stands for the user's (Git LFS's filter set up as its installer sets it, the file transport
    /// allowed); every connection goes through a proxy on a closed port of this computer, 127.0.0.1:9, so that
    /// an address under "invalid" reaches nothing even where a resolver answers for it.
    /// </summary>
    public static Dictionary<string, string> Environment(TempFolder folder)
    {
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
