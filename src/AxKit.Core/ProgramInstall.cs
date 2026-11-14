using System.Diagnostics;
using Microsoft.Win32;

namespace AxKit.Core;

/// <summary>
/// A program an app runs and Windows does not have (docs/axKit/SPEC.md AX-2.9, the user's ask of 2026-10-24): Git for
/// axGIT, KeePassXC for axPass, Claude Code for axClaude. Its name, how it installs (winget's package id, or its own
/// installer's PowerShell command), and its download page for when neither can run. The install is only ever started
/// by the user, from the notice that says the program is missing (bundle B-15: no network call of a window's own).
/// <paramref name="InstallerArguments"/> are options of the maker's installer that winget passes on beside its own
/// silent ones (<c>--custom</c>). <paramref name="ProgramFile"/> is the file name of the program's own window, by
/// which a sign-in entry its installer added is known, and <paramref name="SignInOff"/> where the program turns that
/// off; a program without a <paramref name="ProgramFile"/> is not looked for among the sign-in entries.
/// </summary>
public sealed record ProgramInstall(string Name, string? WingetId, string? PowerShellCommand, string Page, bool AsksForAdministrator,
    string? InstallerArguments = null, string? ProgramFile = null, string? SignInOff = null)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static readonly ProgramInstall Git = new("Git", "Git.Git", null, "https://git-scm.com/downloads/win", AsksForAdministrator: true);

    /// <summary>PowerShell 7, for axTerminal (Windows has Windows PowerShell 5.1 only); Microsoft's own package.</summary>
    public static readonly ProgramInstall PowerShell7 = new("PowerShell 7", "Microsoft.PowerShell", null, "https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows", AsksForAdministrator: true);

    /// <summary>
    /// KeePassXC, of which axPass runs the command line tool only. Its installer (an MSI) by default writes a value
    /// "KeePassXC" under the user's Run key, which starts KeePassXC's window at every sign-in, and offers to start it
    /// when the installer ends: the properties AUTOSTARTPROGRAM and LAUNCHAPPONEXIT, both on by default (KeePassXC's
    /// user guide, "Installer Options", with the example <c>msiexec.exe /q /i ... AUTOSTARTPROGRAM=0</c>; its
    /// share/windows/wix-template.xml, where the Run value's component has the condition AUTOSTARTPROGRAM, which "0"
    /// empties, and the start is an event of the last page's Finish button, which a silent install never shows).
    /// Nobody who installs it for axPass asked for either (the maintainer's computer, 2026-10-21 to 2026-11-14).
    /// </summary>
    public static readonly ProgramInstall KeePassXC = new("KeePassXC", "KeePassXCTeam.KeePassXC", null, "https://keepassxc.org/download/#windows", AsksForAdministrator: true,
        InstallerArguments: "AUTOSTARTPROGRAM=0 LAUNCHAPPONEXIT=0", ProgramFile: "KeePassXC.exe",
        SignInOff: "KeePassXC, Tools, Settings, General, Automatically launch KeePassXC at system startup");

    /// <summary>Claude Code's own installer, for this user only, as its setup page gives it.</summary>
    public static readonly ProgramInstall ClaudeCode = new("Claude Code", null, "irm https://claude.ai/install.ps1 | iex", "https://code.claude.com/docs/en/setup", AsksForAdministrator: false);

    /// <summary>
    /// winget, Windows' package manager (the App Installer), where Windows puts it for the user; null on a Windows
    /// without it. Never a winget.exe found on the PATH, which any folder there could hold (security audit S-10).
    /// </summary>
    private static string? Winget()
    {
        var apps = Path.Combine(SystemPrograms.WindowsApps, "winget.exe");
        return File.Exists(apps) ? apps : null;
    }

    /// <summary>Whether this program can be installed from here: its own installer, or winget being there.</summary>
    public bool CanInstall => PowerShellCommand is not null || (WingetId is not null && Winget() is not null);

    /// <summary>The process that installs it, without a window; null when it cannot be installed from here.</summary>
    public ProcessStartInfo? StartInfo()
    {
        ProcessStartInfo info;
        if (PowerShellCommand is { } command)
        {
            info = new ProcessStartInfo(SystemPrograms.PowerShell);
            foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command })
            {
                info.ArgumentList.Add(argument);
            }
        }
        else if (WingetId is { } id && Winget() is { } winget)
        {
            info = new ProcessStartInfo(winget);
            foreach (var argument in WingetArguments(id, InstallerArguments))
            {
                info.ArgumentList.Add(argument);
            }
        }
        else
        {
            return null;
        }

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        return info;
    }

    /// <summary>
    /// winget's arguments: the package by its exact id from winget's own source (S-10: never another source the user
    /// added), every question answered, the installer silent. <paramref name="installerArguments"/>, when given, go to
    /// the maker's installer in addition to winget's silent ones (<c>--custom</c>, "Arguments to be passed on to the
    /// installer in addition to the defaults"; never <c>--override</c>, which would replace the silent ones).
    /// </summary>
    public static IReadOnlyList<string> WingetArguments(string id, string? installerArguments = null)
    {
        List<string> arguments = ["install", "--id", id, "--exact", "--source", "winget", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity"];
        if (installerArguments is not null)
        {
            arguments.Add("--custom");
            arguments.Add(installerArguments);
        }

        return arguments;
    }

    /// <summary>
    /// The user's sign-in entries, the values of Windows' Run key of the user by name; empty when the key cannot be
    /// read. Read before and after an install, for <see cref="AddedSignInEntry"/>; nothing here writes the key.
    /// </summary>
    public static IReadOnlyDictionary<string, string> SignInEntries()
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            if (key is null)
            {
                return entries;
            }

            foreach (var name in key.GetValueNames())
            {
                entries[name] = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            Log.Error("The sign-in entries could not be read", ex);
        }

        return entries;
    }

    /// <summary>
    /// The name of the sign-in entry an install added for a program: a value that was not there
    /// <paramref name="before"/>, is there <paramref name="after"/>, and whose command names
    /// <paramref name="programFile"/> ("KeePassXC.exe", whatever the case); null when there is none. An entry that was
    /// there before is the user's own and is never reported.
    /// </summary>
    public static string? AddedSignInEntry(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after, string programFile)
    {
        foreach (var (name, command) in after)
        {
            if (!before.Keys.Contains(name, StringComparer.OrdinalIgnoreCase) && command.Contains(programFile, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// What the window says when the installer left the program starting at sign-in: that it did, that the app does not
    /// need it, and where it is turned off. The entry stays: it is the user's and the program's setting, not axKit's.
    /// </summary>
    public string SignInNotice(string appName) =>
        $"{Name}'s installer set it to start with Windows. {appName} does not need that. To turn it off: {SignInOff ?? "Windows' Settings, Apps, Startup"}.";

    /// <summary>
    /// The process's PATH made again from Windows' own (the machine's, then the user's), so that a program installed
    /// while the window runs is found without starting it again; the other variables stay as they are.
    /// </summary>
    public static void RefreshPath() =>
        Environment.SetEnvironmentVariable("PATH", CombinePath(
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
            Environment.GetEnvironmentVariable("PATH")));

    /// <summary>The machine's PATH, the user's, then any of the process's own entries neither holds, each once.</summary>
    public static string CombinePath(string? machine, string? user, string? current)
    {
        var entries = new List<string>();
        foreach (var part in $"{machine};{user};{current}".Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var expanded = Environment.ExpandEnvironmentVariables(part);
            if (!entries.Contains(expanded, StringComparer.OrdinalIgnoreCase))
            {
                entries.Add(expanded);
            }
        }

        return string.Join(';', entries);
    }
}
