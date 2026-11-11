using AxKit.Core;
using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- New session and Switch shell (AT-1.6), folders (AT-1.5) ----

    /// <summary>The shells of the New session notice, in its order; Custom comes after them. The argument is the settings' word for the shell.</summary>
    private static readonly SessionPreset[] ShellPresets =
    [
        new(ShellLauncher.Describe(ShellKind.PowerShell7), [ShellLauncher.Key(ShellKind.PowerShell7)]),
        new(ShellLauncher.Describe(ShellKind.WindowsPowerShell), [ShellLauncher.Key(ShellKind.WindowsPowerShell)]),
        new(ShellLauncher.Describe(ShellKind.CommandPrompt), [ShellLauncher.Key(ShellKind.CommandPrompt)]),
        new(ShellLauncher.Describe(ShellKind.GitBash), [ShellLauncher.Key(ShellKind.GitBash)]),
    ];

    /// <summary>The Switch shell submenu's shells, each with its letter; Custom… follows them and opens the notice.</summary>
    private static readonly (ShellKind Kind, string Text)[] ShellChoices =
    [
        (ShellKind.PowerShell7, "PowerShell &7"),
        (ShellKind.WindowsPowerShell, "&Windows PowerShell"),
        (ShellKind.CommandPrompt, "&Command Prompt"),
        (ShellKind.GitBash, "&Git Bash"),
    ];

    /// <summary>The preset word of a user's shell in the New session notice: its arguments are this word and the shell's name.</summary>
    private const string UserShellPreset = "user";

    /// <summary>The Switch shell submenu's items of the user's shells and the Remove a shell submenu's, made again each time it opens.</summary>
    private readonly List<ToolStripItem> _userShellItems = [];

    /// <summary>
    /// Session → New session… (Ctrl+N): the folder with a Choose folder button under it, the shells as radio buttons,
    /// the user's own shells after the built-in ones (AT-1.6a), Custom with a command line field
    /// (<see cref="SessionControls"/>, the window's own control in the notice, with the shell's wording). Enter ends the
    /// current shell and starts the chosen one in the folder, the transcript cleared; Escape keeps the current shell.
    /// </summary>
    private void NewSession()
    {
        // AT-1.2a: only the shells this computer has; then the user's own, whatever they start, an ampersand in a name
        // doubled so that the radio button shows it rather than taking it for a mnemonic.
        var presets = ShellPresets.Where(preset => ShellThere(ShellLauncher.Parse(preset.Arguments[0])))
            .Concat(_settings.UserShells.Select(shell => new SessionPreset(shell.Name.Replace("&", "&&"), [UserShellPreset, shell.Name])))
            .ToArray();
        var selected = _userShell is { } user ? Array.FindIndex(presets, p => p.Arguments.Count > 1 && p.Arguments[1] == user.Name)
            : _shell == ShellKind.Custom ? presets.Length
            : Array.FindIndex(presets, p => p.Arguments[0] == ShellLauncher.Key(_shell));
        var text = "Choose the shell and the folder. Starting ends the current shell and starts a new session in the folder.";
        if (ProcessRunning && _model.Running)
        {
            text = $"{_model.RunningCommand} is still running. A new session ends it.\n" + text;
        }

        var controls = new SessionControls(CurrentDirectory, presets, selected < 0 ? presets.Length : selected, _customShell ?? string.Empty,
            "Shell", "Command line",
            "The command line of the shell to start, as typed at a prompt. Enter starts the session, Shift+Enter starts a new line.",
            start => PickFolder(start ?? CurrentDirectory), said => Announce(said, true), Overlay.SetHeadLine, Overlay.PressDefault);
        ShowNotice(new Notice("New session", text,
        [
            new OverlayChoice("&Start session", () => StartSession(controls), IsDefault: true),
            new OverlayChoice("Cancel", IsCancel: true),
        ])
        {
            Custom = controls,
            Accepts = controls.Ready,
            HeadLine = controls.FolderLine,
        });
    }

    private void StartSession(SessionControls controls)
    {
        var folder = controls.Folder;
        if (folder is null || !Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        if (controls.SelectedPreset is { Arguments: [UserShellPreset, var name] } && _settings.FindUserShell(name) is { } user)
        {
            _shell = ShellKind.Custom;
            _userShell = user;
        }
        else if (controls.SelectedPreset is { } preset)
        {
            _shell = ShellLauncher.Parse(preset.Arguments[0]);
            _userShell = null;
        }
        else
        {
            _shell = ShellKind.Custom;
            _userShell = null;
            _customShell = controls.CustomArguments.Trim();
        }

        SetFolder(folder);
        Announce($"New session in {PathName.Of(folder)}", false);
        Relaunch($"New session: {ShellName} in {folder}", clearTranscript: true);
    }

    /// <summary>
    /// Session → Switch shell → a shell (AT-1.6): the chosen shell in the current folder, the transcript kept; after the
    /// notice "Switch the shell?" while a command runs. Custom… in the submenu opens the New session notice instead.
    /// </summary>
    private void SwitchShell(ShellKind kind) => SwitchShell(kind, null);

    /// <summary>Switch shell → one of the user's shells (AT-1.6a): started as the custom shell is, under its own name.</summary>
    private void SwitchShell(UserShell shell) => SwitchShell(ShellKind.Custom, shell);

    private void SwitchShell(ShellKind kind, UserShell? user)
    {
        var name = user?.Name ?? ShellLauncher.Describe(kind);
        if (ProcessRunning && _model.Running)
        {
            ShowNotice(Notice.Ask("Switch the shell?",
                $"{_model.RunningCommand} is still running. Switching ends it and starts {name} in the same folder. The transcript stays in the window.",
                "&Switch now", () => ForceSwitch(kind, user), "&Keep it running"));
            return;
        }

        ForceSwitch(kind, user);
    }

    private void ForceSwitch(ShellKind kind, UserShell? user)
    {
        _shell = kind;
        _userShell = user;
        _folder = Directory.Exists(CurrentDirectory) ? CurrentDirectory : _folder;
        Announce(ShellName, false);
        Relaunch($"Switched to {ShellName} in {_folder}", clearTranscript: false);
    }

    /// <summary>
    /// The user's shells in Switch shell ▸ (AT-1.6a), made again as it opens: each as "1. name (command line)" after the
    /// built-in shells, the one in use checked, and in Remove a shell ▸ as "1. name"; with none, Remove a shell holds one
    /// item that says so (an empty list's item, AX-9.5).
    /// </summary>
    private void FillUserShells(ToolStripMenuItem switchShell, int at, ToolStripMenuItem remove)
    {
        foreach (var old in _userShellItems)
        {
            old.Owner?.Items.Remove(old);
            old.Dispose();
        }

        _userShellItems.Clear();
        // AX-6: the numbers are mnemonics only where no fixed item of the submenu has the digit (PowerShell &7).
        var taken = switchShell.DropDownItems.Cast<ToolStripItem>().Select(fixedItem => ShellMenuLabels.Mnemonic(fixedItem.Text ?? string.Empty)).OfType<char>().ToHashSet();
        var shells = _settings.UserShells;
        for (var i = 0; i < shells.Count; i++)
        {
            var shell = shells[i];
            var item = new ToolStripMenuItem(ShellMenuLabels.Switch(i, shell, taken), null, (_, _) => SwitchShell(shell))
            {
                Checked = _userShell is { } user && string.Equals(user.Name, shell.Name, StringComparison.OrdinalIgnoreCase),
            };
            switchShell.DropDownItems.Insert(at + i, item);
            _userShellItems.Add(item);
        }

        if (shells.Count == 0)
        {
            var none = new ToolStripMenuItem("&No shells of your own yet");
            remove.DropDownItems.Add(none);
            _userShellItems.Add(none);
        }

        for (var i = 0; i < shells.Count; i++)
        {
            var shell = shells[i];
            var item = new ToolStripMenuItem(ShellMenuLabels.Remove(i, shell), null, (_, _) => RemoveUserShell(shell));
            remove.DropDownItems.Add(item);
            _userShellItems.Add(item);
        }
    }

    /// <summary>
    /// Switch shell → Add a shell… (AT-1.6a): the notice "Add a shell" with the field "Type the shell's name" (a blank
    /// name or one a shell already has is said and the notice stays), then the notice with the field "Type the command
    /// line that starts it"; "Added <name>". The shell is not started: Switch shell or New session starts it.
    /// </summary>
    private void AddUserShell()
    {
        AskText("Add a shell", "The name Switch shell and the New session notice show for the shell.", "Type the shell's name:", "Type the shell's name", string.Empty, "&Next",
            typed =>
            {
                var name = typed.Trim();
                AskText("Add a shell", $"The command line that starts {name}, as typed at a prompt, for example cmd.exe /k or pwsh -NoProfile. It starts as a custom shell does, without the integration marks unless its prompt prints them.",
                    "Type the command line that starts it:", "Type the command line that starts it", string.Empty, "&Add",
                    line =>
                    {
                        _settings.UserShells.Add(new UserShell(name, line.Trim()));
                        SaveSettings();
                        Announce($"Added {name}", true);
                    },
                    line => line.Trim().Length == 0 ? "Type the command line" : null, emptyMessage: "Type the command line");
            },
            _settings.RefusedShellName, emptyMessage: "Type a name");
    }

    /// <summary>
    /// Switch shell → Remove a shell ▸ one shell (AT-1.6a): taken off the list (<see cref="TerminalSettings.RemoveUserShell"/>);
    /// "Removed <name>". When it is the shell in use, the machine's default built-in shell starts in its place in the
    /// same folder, the transcript kept, after the notice "Remove the shell?" while a command runs (Keep it keeps the
    /// shell and the list as they were); the custom command line is left alone.
    /// </summary>
    private void RemoveUserShell(UserShell shell)
    {
        if (_userShell is not { } inUse || !string.Equals(inUse.Name, shell.Name, StringComparison.OrdinalIgnoreCase))
        {
            ForgetUserShell(shell);
            return;
        }

        var fallback = ShellLauncher.DefaultKind();
        if (ProcessRunning && _model.Running)
        {
            ShowNotice(Notice.Ask("Remove the shell?",
                $"{_model.RunningCommand} is still running in {shell.Name}. Removing the shell ends it and starts {ShellLauncher.Describe(fallback)} in the same folder. The transcript stays in the window.",
                "&Remove now", () => RemoveShellInUse(shell, fallback), "&Keep it"));
            return;
        }

        RemoveShellInUse(shell, fallback);
    }

    private void RemoveShellInUse(UserShell shell, ShellKind fallback)
    {
        ForgetUserShell(shell);
        ForceSwitch(fallback, null);
    }

    private void ForgetUserShell(UserShell shell)
    {
        _settings.RemoveUserShell(shell.Name);
        SaveSettings();
        Announce($"Removed {shell.Name}", true);
    }

    /// <summary>Session, Switch shell, Install (AT-1.2a): the shell's program installed on the user's command (bundle AX-2.9), then switched to; the agreement is the shell's own (AX-11.5), so that the switch asks nothing more.</summary>
    private void InstallShell(ProgramInstall program, ShellKind kind) =>
        InstallProgram(program, () => ShellLauncher.Find(kind) is not null, () =>
        {
            LookUpShells();
            SwitchShell(kind);
        }, AxKit.Core.ConsentTarget.Shell(ShellLauncher.Describe(kind)));

    /// <summary>The bundle's folder picker (one of the few separate windows, AX-7.2), on the folder or the user's profile; null when it was cancelled.</summary>
    private string? PickFolder(string? start) =>
        ChooseFolder("Choose the folder the shell starts in", start ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private void SetFolder(string folder)
    {
        if (!string.Equals(_folder, folder, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the folder left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember(AxTerminalApp.Name, _folder);
        }

        _folder = folder;
    }

    /// <summary>Session → Open recent folder: a shell in that folder, the transcript cleared, with a system line and an announcement.</summary>
    protected override void SwitchFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        SetFolder(folder);
        Announce($"Folder {PathName.Of(folder)}", false);
        Relaunch($"New session: {ShellName} in {folder}", clearTranscript: true);
    }

    /// <summary>Session → Show current folder in → File Explorer (Ctrl+W): the shell's current directory, one of the system's own windows; "Folder missing" is the base's (AX-9.10).</summary>
    private void ShowInExplorer() => OpenInExplorer(CurrentDirectory);

}
