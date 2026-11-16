using AxKit.Core;

namespace AxTerminal.Core;

/// <summary>What is spoken of the shell's output as it arrives (SPEC.md AT-5.3).</summary>
public enum OutputSpeechMode
{
    None,
    All,
}

/// <summary>A shell of the user's own (SPEC.md AT-1.6a): the name the menus show and the command line that starts it, as typed at a prompt; it starts as the custom shell does.</summary>
public sealed record UserShell(string Name, string CommandLine);

/// <summary>
/// Everything axTerminal remembers between runs (SPEC.md AT-8.1), in <c>%APPDATA%\AxKit\axTerminal.json</c> through
/// the bundle's <see cref="SettingsFile"/>: the shell, the user's own shells, the last folder and the speech options here; the console size,
/// the recent folders, the sounds, the signals and the transcript options from <see cref="ConsoleSettings"/>, shared
/// with axClaude; the font, the update check and the window's place from <see cref="WindowSettings"/>. A missing or
/// unreadable file falls back to the defaults; out-of-range values do too.
/// </summary>
public sealed class TerminalSettings : ConsoleSettings
{
    /// <summary>The shell's word (<see cref="ShellLauncher.Key"/>); null means the default of the machine.</summary>
    public string? Shell { get; set; }

    /// <summary>The command line of the custom shell, kept for the New session notice.</summary>
    public string? CustomShell { get; set; }

    /// <summary>The user's own shells (AT-1.6a), in the order added: a name and the command line that starts each, the shape of axFile's own programs (AF-4.11).</summary>
    public List<UserShell> UserShells { get; set; } = [];

    /// <summary>The name of the user's shell in use, when the shell is one of <see cref="UserShells"/>; null otherwise.</summary>
    public string? UserShellName { get; set; }

    public string? LastFolder { get; set; }

    /// <summary>All lines by default since 2026-10-23 (AT-5.3): the user, at the NVDA pass, "axTerminal should speak the output of any command".</summary>
    public OutputSpeechMode OutputSpeech { get; set; } = OutputSpeechMode.All;

    /// <summary>"Asks: …" when a running program seems to wait on a question (AT-5.4).</summary>
    public bool AnnounceQuestions { get; set; } = true;

    public static string DefaultPath => AppPaths.SettingsFile("axTerminal");

    /// <summary>The seal over the fields that decide what is run (bundle AX-5.6, AT-8.1a): written at every save, checked at every load.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>What the load's error text says when the seal does not check (AT-8.1a).</summary>
    public const string ShellsNotUsed = "Your shells in axTerminal's settings were changed outside axKit and are not used. Add them again from Session, Add a shell.";

    /// <summary>The sealed fields, in their order: the shell, the custom command line, and each of the user's shells as its name and its command line.</summary>
    private string SealedText() =>
        SettingsSeal.Text([Shell, CustomShell, .. (UserShells ?? []).SelectMany(shell => new[] { shell?.Name, shell?.CommandLine })]);

    public static TerminalSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TerminalSettings>(path, out error);
        if (!SettingsSeal.Checks(path, settings.Seal, settings.SealedText(), new TerminalSettings().SealedText()))
        {
            // AT-8.1a: the shell is the machine's default again and no shell of the file's is started; the rest is kept.
            settings.Shell = null;
            settings.CustomShell = null;
            settings.UserShells = [];
            error = SettingsSeal.Refused(error, ShellsNotUsed);
        }

        settings.Sanitize();
        return settings;
    }

    public override void Save(string path)
    {
        Seal = SettingsSeal.Of(path, SealedText());
        if (SettingsFile.Save(path, this))
        {
            SettingsSeal.Remember(path);
        }
    }

    /// <summary>The shell the settings name, or the machine's default.</summary>
    public ShellKind ShellKind => ShellLauncher.Parse(Shell);

    /// <summary>The folder the shell starts in: the last one, and the front of the recent list (AT-1.5).</summary>
    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RememberRecentFolder(folder);
    }

    /// <summary>The user's shell with this name, case ignored; null when there is none.</summary>
    public UserShell? FindUserShell(string? name) =>
        name is null ? null : UserShells.FirstOrDefault(shell => string.Equals(shell.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Why a typed name cannot be a new shell's (AT-1.6a): blank, or a name a built-in shell, Custom ("Custom" and
    /// "Custom shell") or one of the user's already has; null when it can.
    /// </summary>
    public string? RefusedShellName(string typed)
    {
        var name = typed.Trim();
        if (name.Length == 0)
        {
            return "Type a name";
        }

        var taken = Enum.GetValues<ShellKind>().Select(ShellLauncher.Describe).Append("Custom");
        return taken.Contains(name, StringComparer.OrdinalIgnoreCase) || FindUserShell(name) is not null
            ? $"A shell named {name} is already there"
            : null;
    }

    /// <summary>
    /// Switch shell → Remove a shell (AT-1.6a): the user's shell with this name taken off the list. When it is the
    /// shell in use, the settings fall back to the machine's default built-in shell (<see cref="Shell"/> null); the
    /// custom command line is left as it is. The shell removed, or null when there is none of that name.
    /// </summary>
    public UserShell? RemoveUserShell(string name)
    {
        var shell = FindUserShell(name);
        if (shell is null)
        {
            return null;
        }

        UserShells.Remove(shell);
        if (string.Equals(UserShellName, shell.Name, StringComparison.OrdinalIgnoreCase))
        {
            UserShellName = null;
            Shell = null;
        }

        return shell;
    }

    private void Sanitize()
    {
        SanitizeConsole();
        // AT-1.6a: names and lines trimmed, a shell without either dropped, and a name taken twice kept the first time.
        var shells = new List<UserShell>();
        foreach (var shell in UserShells ?? [])
        {
            if (shell is null || string.IsNullOrWhiteSpace(shell.Name) || string.IsNullOrWhiteSpace(shell.CommandLine))
            {
                continue;
            }

            var name = shell.Name.Trim();
            if (!shells.Any(kept => string.Equals(kept.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                shells.Add(new UserShell(name, shell.CommandLine.Trim()));
            }
        }

        UserShells = shells;
        if (FindUserShell(UserShellName) is null)
        {
            UserShellName = null;
        }

        if (Shell is not null && Shell is not ("pwsh" or "powershell" or "cmd" or "bash" or "custom"))
        {
            Shell = null;
        }

        if (!Enum.IsDefined(OutputSpeech))
        {
            OutputSpeech = OutputSpeechMode.All;
        }
    }
}
