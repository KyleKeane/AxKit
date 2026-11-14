using System.Diagnostics;
using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Installing a program an app needs (docs/axKit/SPEC.md AX-2.9, 2026-10-24): from the notice that says the program is
/// missing, the window runs its installer without a window of its own, says what to expect, and checks again when it
/// ends. Started by the user only; one at a time. A window holds one of these (<see cref="AxKitForm.InstallProgram"/>
/// calls it); it holds the installer that runs, and asks of the window its notices, its announcements and the consent
/// (docs/axKit/repairs.json R-15: out of the window base, 2026-11-14).
/// </summary>
internal sealed class ProgramInstaller
{
    /// <summary>How long an install may take before the window stops waiting for it and says so.</summary>
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(15);

    private readonly AxKitForm _form;

    /// <summary>
    /// The installer running, with its program's name: a second Install it now is said, not started (two winget runs
    /// at once fail each other), and that includes an installer the window stopped waiting for, which is never killed:
    /// it is cleared only when the installer's process ends.
    /// </summary>
    private (string Name, Task Exited)? _installing;

    public ProgramInstaller(AxKitForm form)
    {
        _form = form;
    }

    /// <summary>
    /// Installs <paramref name="program"/>: a progress notice, the installer run, the PATH read again, then
    /// <paramref name="installed"/> asked; true runs <paramref name="then"/> after "Git is installed", false says what
    /// happened with the program's page to open. Without a way to install it here (no winget), the page opens at once.
    /// <paramref name="target"/> is what the consent is recorded for, the program's own unless the caller connects to it
    /// under another name (axTerminal installs Git for Windows as its shell "Git Bash").
    /// </summary>
    public void InstallProgram(ProgramInstall program, Func<bool> installed, Action then, ConsentTarget? target = null)
    {
        if (_installing is { Exited.IsCompleted: false } running)
        {
            _form.Announce($"{running.Name} is being installed already; this window says when it is done", true);
            return;
        }

        // The consent of AX-11.4 in the step the user is already taking (AX-11.5): agreed here, it counts for the app's
        // first connection to the program too, so the user is asked once.
        target ??= ConsentTarget.For(program.Name);
        if (!_form.HasConsent(target, $"Before {_form.AppName} installs {program.Name}", "&Install",
                () => InstallProgram(program, installed, then, target), () => _form.Announce($"{program.Name} not installed", true)))
        {
            return;
        }

        if (program.StartInfo() is not { } info)
        {
            _form.OpenUrl(program.Page);
            _form.Announce($"{program.Name} cannot be installed from here: its download page is opening in the browser", true);
            return;
        }

        _form.Do(async () =>
        {
            // Its own notice, kept, so that the end closes this one and never one the user opened after hiding it.
            var progress = new Notice("Installing " + program.Name,
                (program.AsksForAdministrator
                    ? "Windows will ask whether the installer may make changes to this computer: choose Yes. "
                    : "It installs for your account only; Windows asks nothing. ")
                + "This takes a minute or a few; the notice closes by itself and says when it is done. Escape hides this notice; the install goes on.",
                [OverlayChoice.Close])
            {
                Progress = true,
            };
            _form.ShowNotice(progress);
            Log.Info("Installing " + program.Name);
            // The user's sign-in entries before and after (AX-2.9): an installer that set its program to start with
            // Windows is said, never undone. Windows' own setting of this user, read, not written.
            var signInBefore = program.ProgramFile is null ? null : ProgramInstall.SignInEntries();
            var (exitCode, lastLine) = await RunInstallerAsync(info, exited => _installing = (program.Name, exited));
            ProgramInstall.RefreshPath();
            if (_form.IsDisposed)
            {
                return;
            }

            var signIn = signInBefore is not null && ProgramInstall.AddedSignInEntry(signInBefore, ProgramInstall.SignInEntries(), program.ProgramFile!) is not null
                ? program.SignInNotice(_form.AppName)
                : null;
            if (signIn is not null)
            {
                Log.Info($"{program.Name}'s installer added a sign-in entry");
            }

            var ownShowing = _form.NoticeOpen && ReferenceEquals(_form.Overlay.Shown, progress);
            if (installed())
            {
                Log.Info(program.Name + " installed");
                if (signIn is null)
                {
                    if (ownShowing)
                    {
                        _form.CloseNotice(null);
                    }

                    _form.Announce(program.Name + " is installed", true);
                    then();
                    return;
                }

                if (_form.NoticeOpen && !ownShowing)
                {
                    // The user is in another notice: said, not shown over it.
                    _form.Announce($"{program.Name} is installed. {signIn}", true);
                    then();
                    return;
                }

                // A notice, since where to turn it off is too much to hear once; the app goes on when it closes.
                _form.ShowNotice(new Notice(program.Name + " is installed", signIn, [new OverlayChoice("&Close", then, IsDefault: true, IsCancel: true)]));
                return;
            }

            Log.Info($"{program.Name} not installed: exit {exitCode}");
            var why = (exitCode is null ? "The installer took over a quarter of an hour, and the window stopped waiting for it." : $"The installer ended with code {exitCode}{(lastLine.Length > 0 ? ": " + lastLine : string.Empty)}.")
                + (program.AsksForAdministrator ? " If Windows asked whether to allow it and the answer was No, try again." : string.Empty)
                + (signIn is null ? string.Empty : " " + signIn);
            if (_form.NoticeOpen && !ownShowing)
            {
                // The user is in another notice: said, not shown over it.
                _form.Announce($"{program.Name} was not installed. {why}", true);
                return;
            }

            _form.ShowNotice(new Notice(program.Name + " was not installed", why + " Its download page installs it by hand.",
            [
                new OverlayChoice("&Try again", () => InstallProgram(program, installed, then, target)),
                new OverlayChoice("&Open the download page", () => _form.OpenUrl(program.Page), StaysOpen: true),
                OverlayChoice.Close,
            ]));
        });
    }

    /// <summary>
    /// The installer run to its end off the UI thread: its exit code (null past the timeout) and the last line it
    /// printed. <paramref name="started"/> gets the task of the process's end, which outlives the timeout. The output is
    /// read under the same deadline, and for a few seconds past the end at most: a helper the installer left running can
    /// hold the pipes open long after it, as ClaudeLauncher.VersionAsync found.
    /// </summary>
    private static async Task<(int? ExitCode, string LastLine)> RunInstallerAsync(ProcessStartInfo info, Action<Task> started)
    {
        Process? process;
        try
        {
            // Outside call, program, Installers: runs the installer of a missing program the user chose in Install it
            // now (AX-2.9): Windows' own winget from its own source for Git, KeePassXC and PowerShell 7, or Anthropic's
            // installer script for Claude Code in Windows PowerShell; each downloads the maker's installer.
            process = Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, ex.Message);
        }

        if (process is null)
        {
            return (-1, "it did not start");
        }

        // The exit code read as the process ends, then the process let go; the task outlives a timeout.
        var exited = process.WaitForExitAsync().ContinueWith(_ =>
        {
            var code = process.ExitCode;
            process.Dispose();
            return code;
        }, TaskScheduler.Default);
        started(exited);
        // The pipes are read from the start, or a full pipe would stop the installer.
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (await Task.WhenAny(exited, Task.Delay(InstallTimeout)) != exited)
        {
            return (null, string.Empty);
        }

        var reads = Task.WhenAll(output, error);
        var all = await Task.WhenAny(reads, Task.Delay(TimeSpan.FromSeconds(5))) == reads ? output.Result + "\n" + error.Result : string.Empty;
        var last = all.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.Any(char.IsLetter)) ?? string.Empty;
        return (exited.Result, last);
    }
}
