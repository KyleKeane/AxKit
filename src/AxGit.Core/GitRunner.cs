using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AxKit.Core;

namespace AxGit.Core;

/// <summary>
/// What one git command gave back: the exit code, what it printed and what it complained. <see cref="TimedOut"/>:
/// git was stopped at the time limit, so the exit code is -1, the output is what it had printed until then, a part,
/// and the run is never <see cref="Ok"/>. <see cref="OutputHeldOpen"/>: git itself finished, with its own exit code
/// and all of its output, and only a program it started (a signing agent, a hook's daemon) kept the output open
/// until the limit; the complaint says so after git's own.
/// </summary>
public sealed record GitResult(int ExitCode, string Output, string Error, bool TimedOut = false, bool OutputHeldOpen = false)
{
    /// <summary>git ran to its own end and gave exit code 0; never true for a run stopped at the time limit.</summary>
    public bool Ok => ExitCode == 0 && !TimedOut;

    /// <summary>The first line of the complaint, or of the output when git complained in none; "git failed" when there is nothing.</summary>
    public string FirstLine
    {
        get
        {
            foreach (var text in new[] { Error, Output })
            {
                foreach (var line in text.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length > 0)
                    {
                        return trimmed;
                    }
                }
            }

            return "git failed";
        }
    }

    /// <summary>Everything git said, the complaint first, for a notice.</summary>
    public string AllText => string.Join("\n", new[] { Error.Trim(), Output.Trim() }.Where(text => text.Length > 0));
}

/// <summary>git is not on the PATH (SPEC.md AG-1.5).</summary>
public sealed class GitNotFoundException(string message, Exception inner) : Exception(message, inner);

/// <summary>
/// Runs the git program (SPEC.md AG-1.5, AG-D2): one process per command, in the repository, with colour and path
/// quoting off and no terminal prompts, both streams read in UTF-8 on background tasks, a timeout that kills the
/// process. The window calls it with the argument lists the parsers give.
/// </summary>
public static class GitRunner
{
    public static readonly TimeSpan Short = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Long = TimeSpan.FromMinutes(10);

    /// <summary>The program run: git, in Git for Windows' install folder or else on the PATH (AG-1.5); the tests point it at a fake git.cmd of their own.</summary>
    public static string Program
    {
        // R-70: the agreement is for Git by its name, so Git for Windows' own folder under Program Files, which only
        // an administrator can write, comes before the PATH, and PATH folders off this computer are passed over.
        // S-1: never a bare name that Windows would look for in the current folder first; with no git anywhere, Git
        // for Windows' usual place, whose absence gives "git is not installed".
        get => _program ?? SystemPrograms.Installed("git.exe", InstallFolders) ?? Path.Combine(InstallFolders[0], "git.exe");
        set => _program = value;
    }

    /// <summary>Where Git for Windows puts git.exe when it is installed for the whole computer: <c>Git\cmd</c> under Program Files and Program Files (x86).</summary>
    public static IReadOnlyList<string> InstallFolders { get; } = SystemPrograms.ProgramFilesFolders("Git", "cmd");

    private static string? _program;

    /// <summary>
    /// The whole argument list of a command: the repository, the fixed configuration, the repository's own programs
    /// set to nothing while it is not trusted (<paramref name="overrides"/>, <see cref="GitTrust.Overrides"/>), then
    /// <paramref name="arguments"/>. The fixed configuration never lets a repository's file monitor run (it would on
    /// every <c>git status</c>) nor the <c>ext::</c> transport, which runs a command named in a remote's address
    /// (security audit S-2). Pure, tested.
    /// </summary>
    public static IReadOnlyList<string> Command(string? repository, IReadOnlyList<string> arguments, IReadOnlyList<string>? overrides = null)
    {
        var list = new List<string>();
        if (repository is not null)
        {
            list.Add("-C");
            list.Add(repository);
        }

        list.AddRange(["-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never"]);
        list.AddRange(overrides ?? []);
        list.AddRange(arguments);
        return list;
    }

    public static async Task<GitResult> RunAsync(string? repository, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default, IReadOnlyList<string>? overrides = null)
    {
        var info = new ProcessStartInfo(Program)
        {
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in Command(repository, arguments, overrides))
        {
            info.ArgumentList.Add(argument);
        }

        // No prompts a terminal would show, English messages, no pager.
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["LC_ALL"] = "C.UTF-8";
        info.Environment["GIT_PAGER"] = "cat";

        // The run is the bundle's (AxKit.Core/ProcessRun, since 2026-10-25): the reads under the timeout as well as the
        // wait, since a program git started that outlives it (an agent, a hook's daemon) keeps the pipes open after git
        // has exited, and a read with no end would hang the window; whether git itself had finished is taken before
        // the kill (until 2026-10-23 it was read after, so every timeout read as the open-pipe case).
        ProcessResult result;
        try
        {
            // Outside call, program, Git: runs the user's installed git on the repository for one command and reads
            // what it prints, one process per command, its terminal prompts off; fetch, pull and push reach the
            // repository's remotes, every other command works on disk (AX-11.11).
            result = await ProcessRun.RunAsync(info, [], timeout, cancellation);
        }
        catch (Win32Exception ex)
        {
            throw new GitNotFoundException("git was not found on this computer.", ex);
        }

        return Result(result, timeout);
    }

    /// <summary>
    /// A run as git's result (review of 2026-11-27; pure, tested). Stopped at the time limit: no success whatever it
    /// printed, exit code -1, the reason after git's own complaint. Finished with its output held open by a program
    /// it started: git's own exit code and output, which are whole, and the reason after its complaint. Until then
    /// the reason replaced git's complaint, and the output of both cases was empty, so that a status read this way
    /// was an empty status with exit code 0.
    /// </summary>
    public static GitResult Result(ProcessResult result, TimeSpan timeout)
    {
        if (!result.TimedOut)
        {
            return new GitResult(result.ExitCode, result.Output, result.Error);
        }

        var reason = result.Stopped
            ? $"git did not finish within {timeout.TotalSeconds:0} seconds and was stopped."
            : "git finished, but a program it started kept its output open until the time limit.";
        var error = result.Error.Trim().Length == 0 ? reason : result.Error.TrimEnd() + "\n" + reason;
        return new GitResult(result.Stopped ? -1 : result.ExitCode, result.Output, error, TimedOut: result.Stopped, OutputHeldOpen: !result.Stopped);
    }

    /// <summary>
    /// The repository that holds <paramref name="path"/> (a folder, or a file whose folder is taken), as the full
    /// path of its root; null when the path is in none (AG-1.2).
    /// </summary>
    public static async Task<string?> RootAsync(string path, CancellationToken cancellation = default)
    {
        var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (folder is null || !Directory.Exists(folder))
        {
            return null;
        }

        var result = await RunAsync(folder, ["rev-parse", "--show-toplevel"], Short, cancellation);
        if (!result.Ok)
        {
            return null;
        }

        var root = result.Output.Trim();
        return root.Length == 0 ? null : Path.GetFullPath(root.Replace('/', Path.DirectorySeparatorChar));
    }
}
