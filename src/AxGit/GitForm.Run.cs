using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>axGIT's window, running git (AG-1.5, AG-D6): every command on a background task, its answer back on this thread.</summary>
public sealed partial class GitForm
{
    // ---- running git (AG-1.5, AG-D6) ----

    /// <summary>
    /// One git command in the repository: the status bar says what runs, the git items are disabled meanwhile, and
    /// the result comes back for the caller to announce. Null when nothing ran: no repository, git missing, or a
    /// command still running ("Working…").
    /// </summary>
    private Task<GitResult?> RunGitAsync(IReadOnlyList<string> arguments, TimeSpan timeout, string verb) =>
        RunGitAsync(arguments, timeout, verb, changes: false);

    /// <summary>
    /// A command that changes the repository (stage, unstage, discard, commit, switch, a new branch, fetch, pull,
    /// push): run through the session's one way for it, which checks what the repository would run before the
    /// command and runs it only when the check holds (P3 of the review's fifth pass), and again after it, whether it
    /// succeeded or failed (AG-1.6b, D8); then what the last check found is acted on: the repository closed when it
    /// is no longer known, the notice when the risks are other ones. Null when nothing ran, or when the check closed
    /// the repository or asks. <paramref name="settings"/>: the command's own (a fetch's addresses with no redirect, L1).
    /// </summary>
    private async Task<GitResult?> ChangeGitAsync(IReadOnlyList<string> arguments, TimeSpan timeout, string verb, IReadOnlyList<GitSetting>? settings = null)
    {
        var (root, session) = (_root, _session);
        var result = await RunGitAsync(arguments, timeout, verb, changes: true, settings);
        if (root is null || session is null || result is null)
        {
            return null;
        }

        // A command the check before it refused ran nothing: the notice says what is there now, and no failure.
        var failed = result.Ok || result.NotRun ? null : $"{char.ToUpperInvariant(verb[0])}{verb[1..]} failed. Git said: {result.FirstLine}";
        return AfterCheck(root, session, failed) && !result.NotRun ? result : null;
    }

    private async Task<GitResult?> RunGitAsync(IReadOnlyList<string> arguments, TimeSpan timeout, string verb, bool changes, IReadOnlyList<GitSetting>? settings = null)
    {
        if (_root is null || _session is not { } session)
        {
            Refuse("No repository");
            return null;
        }

        if (_gitMissing)
        {
            GitMissing();
            return null;
        }

        if (RefusedWhileWorking())
        {
            return null;
        }

        BeginWork($"Working: {verb}…");
        SetGitItems(false);
        UpdateStatusBar();
        var watch = Stopwatch.StartNew();
        try
        {
            var result = changes ? await session.ChangeAsync(arguments, timeout, default, settings) : await session.RunAsync(arguments, timeout);
            // The subcommand alone (AG-8.2): the arguments hold the commit message and the paths.
            Log.Info($"git {arguments[0]}: exit {result.ExitCode} in {watch.ElapsedMilliseconds} ms{(result.Ok ? string.Empty : ", " + result.FirstLine)}");
            return result;
        }
        catch (GitNotFoundException)
        {
            GitMissing();
            return null;
        }
        finally
        {
            EndWork();
            if (!IsDisposed)
            {
                SetGitItems(true);
                UpdateStatusBar();
            }
        }
    }

    private void SetGitItems(bool enabled)
    {
        foreach (var item in _gitItems)
        {
            item.Enabled = enabled && !_gitMissing;
        }
    }

    /// <summary>git is not on the PATH (AG-1.5): a notice each time a command is asked for, with the chime the first time, and every command stays disabled. It holds its keys for a moment each time (docs/axKit/SPEC.md AX-6.4): it comes in place of what the user asked for.</summary>
    private void GitMissing()
    {
        var first = !_gitMissing;
        _gitMissing = true;
        SetGitItems(false);
        UpdateStatusBar();
        ShowNotice(new Notice("Git was not found",
            "axGIT runs the git program, and it is not on this computer: neither on the PATH nor in Program Files. Install it now installs Git for Windows (Windows asks whether the installer may make changes: choose Yes); axGIT then opens the repository. Or install it by hand from git-scm.com.",
        [
            // AG-1.5a: installed, then found the way every git command finds it (GitRunner.Program).
            new OverlayChoice("&Install Git now", () => InstallProgram(ProgramInstall.Git, () => File.Exists(GitRunner.Program), () => // Not a path of the repository's: git's own program, after its install.
            {
                _gitMissing = false;
                SetGitItems(true);
                Do(() => StartAsync(_root ?? _startPath));
            })),
            new OverlayChoice("&Open git-scm.com", () => OpenUrl(ProgramInstall.Git.Page), StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Silent = !first,
        }.ByItself());
    }

    /// <summary>A failed command: one announcement when git said one line, a notice with everything it said otherwise (AG-7, the base's rule).</summary>
    private void Failed(string title, GitResult result) => Failed(title, result.AllText);
}
