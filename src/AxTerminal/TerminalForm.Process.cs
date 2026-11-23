using AxKit.Core.Pty;
using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the shell process (AT-1): what is the shell's around the console window's StartProcess and StopProcess ----

    private void StartShell()
    {
        if (!Directory.Exists(_folder))
        {
            Fail($"The folder no longer exists: {_folder}. Press {BundleKeys.Text(AxTerminalKeys.NewSession)} to choose another.");
            return;
        }

        _model.ContinuationPrompt = ShellLauncher.ContinuationPrompt(_shell);
        // S-37: a new secret for this start; the model takes only marks that carry it (a custom shell prints none).
        var nonce = ShellLauncher.NewNonce();
        _model.MarkNonce = _shell == ShellKind.Custom ? null : nonce;
        var commandLine = ShellLauncher.BuildCommandLine(_shell, CustomLine, nonce);
        if (commandLine is null)
        {
            Fail(_shell == ShellKind.Custom
                ? $"No custom command line is set. Press {BundleKeys.Text(AxTerminalKeys.NewSession)} and choose a shell."
                : $"{ShellLauncher.Describe(_shell)} was not found on this computer. Press {BundleKeys.Text(AxTerminalKeys.NewSession)} and choose another shell.");
            return;
        }

        // Bundle AX-11.5: each shell asks once before its first start, those that come with Windows included.
        // A command line of the user's own (Custom, a shell of the user's) is agreed to as that command line (AT-1.6b):
        // another line under the same name asks again.
        var target = _shell == ShellKind.Custom
            ? AxKit.Core.ConsentTarget.CommandLineShell(ShellName, commandLine)
            : AxKit.Core.ConsentTarget.Shell(ShellLauncher.Describe(_shell));
        if (!HasConsent(target, StartShell, () => _model.AddSystemLine($"Not connected to {target.Program}. Press {BundleKeys.Text(AxTerminalKeys.NewSession)} to start a shell.")))
        {
            return;
        }

        if (!StartProcess(new PtyOptions(commandLine, _folder, _model.Columns, _model.Rows, ShellLauncher.BuildEnvironment(_shell, nonce)), "The shell"))
        {
            return;
        }

        _askedRow = null;
        // AT-4.3: a shell of the user's own says the command line it was started with.
        _model.AddSystemLine(_shell == ShellKind.Custom
            ? $"Shell started: {ShellName}, command line {commandLine}, in {_folder}"
            : $"Shell started: {ShellName} in {_folder}");
        _settings.RememberFolder(_folder);
        _settings.Shell = ShellLauncher.Key(_shell);
        _settings.CustomShell = _customShell;
        _settings.UserShellName = _userShell?.Name;
        SaveSettings();
        _asks.Start();
        UpdateStatus();
        UpdateTitle();
    }

    /// <summary>The shell's process ended (AT-1.7), its last output in and the process stopped: the system line, the announcement.</summary>
    protected override void OnProcessExited(int code)
    {
        _model.ShellExited(code);
        Announce("The shell ended", false);
        UpdateStatus();
    }

    /// <summary>The question heuristic's timer stops with the shell; the console window stops the process, the writes and the control area.</summary>
    protected override void StopProcess(bool terminateAtOnce = false)
    {
        _asks.Stop();
        base.StopProcess(terminateAtOnce);
    }

    /// <summary>
    /// Session → Restart shell (Ctrl+Shift+R, AT-1.6): the same shell again in its current directory, the transcript
    /// kept. A running command is ended only after the Restart the shell? notice; an ended shell, or one with
    /// nothing running, starts at once. The steps are the console window's (<see cref="ConsoleForm.RestartProgram"/>):
    /// the question first while there is one, "Restarting", then <see cref="RestartNow"/>.
    /// </summary>
    protected override ConsoleQuestion? RestartQuestion() =>
        ProcessRunning && _model.Running
            ? new ConsoleQuestion("Restart the shell?",
                $"{_model.RunningCommand} is still running. Restarting ends it and starts the shell again in the same folder. The transcript stays in the window.",
                "&Keep it running")
            : null;

    protected override void RestartNow()
    {
        _folder = Directory.Exists(CurrentDirectory) ? CurrentDirectory : _folder;
        Relaunch("Restarting the shell", clearTranscript: false);
    }

    /// <summary>
    /// Stops the shell and starts one again in the folder, after the system line. The transcript stays (Restart
    /// shell) unless <paramref name="clearTranscript"/> is set (New shell, a folder change; AT-1.6), in which case the
    /// system line is the first line of the emptied window.
    /// </summary>
    private void Relaunch(string systemLine, bool clearTranscript)
    {
        // A command still running is ended at once (AT-1.6): the user chose the restart or the new session knowing it.
        StopProcess(terminateAtOnce: ProcessRunning && _model.Running);
        // The next shell draws on a fresh screen either way: the old one's cells and hidden rows are the old shell's,
        // and a new shell assumes a blank terminal (until 2026-10-23 a new session kept them, and the new shell's
        // shorter rows showed the old ones' tails).
        _model.ResetScreen();
        if (clearTranscript)
        {
            _model.Clear();
            _arrivals.Reset();
        }

        _model.AddSystemLine(systemLine);
        StartShell();
    }
}
