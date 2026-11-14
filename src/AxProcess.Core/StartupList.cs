using AxKit.Core;
using Microsoft.Win32;

namespace AxProcess.Core;

/// <summary>Where a startup entry is set (SPEC.md PR-5.1).</summary>
public enum StartupPlace
{
    UserRunKey,
    MachineRunKey,
    MachineRunKey32,
    UserFolder,
    CommonFolder,
}

/// <summary>One item of the Startup view (PR-5.1): its name, the command or the file it starts, where it is set, and whether Task Manager has turned it off.</summary>
public sealed record StartupEntry(string Name, string Command, StartupPlace Place, bool Enabled)
{
    /// <summary>"OneDrive, enabled, current user's Run key".</summary>
    public string Label => $"{Name}, {(Enabled ? "enabled" : "disabled")}, {PlaceText(Place)}";

    /// <summary>The key a view keeps the selection by: the name and the place.</summary>
    public string Key => $"{Place}:{Name}";

    /// <summary>The lines of the details notice (PR-5.3).</summary>
    public IReadOnlyList<string> Details =>
        CommandLine.ProgramOf(Command) is { } program
            ? [$"Command {Command}", $"Set in {PlaceText(Place)}", Enabled ? "Enabled" : "Disabled", $"Program {program}"]
            : [$"Command {Command}", $"Set in {PlaceText(Place)}", Enabled ? "Enabled" : "Disabled"];

    public static string PlaceText(StartupPlace place) => place switch
    {
        StartupPlace.UserRunKey => "current user's Run key",
        StartupPlace.MachineRunKey => "computer's Run key",
        StartupPlace.MachineRunKey32 => "computer's Run key (32-bit)",
        StartupPlace.UserFolder => "your Startup folder",
        _ => "all users' Startup folder",
    };

    public static string ListName(int count) => count == 0 ? "Startup, none" : $"Startup, {count}";
}

/// <summary>The program file a command starts (PR-5.3): the quoted path, or the text up to and with ".exe", or the whole command when it names a file.</summary>
public static class CommandLine
{
    public static string? ProgramOf(string command)
    {
        var text = Environment.ExpandEnvironmentVariables(command.Trim());
        if (text.Length == 0)
        {
            return null;
        }

        if (text[0] == '"')
        {
            var close = text.IndexOf('"', 1);
            return close > 1 ? text[1..close] : null;
        }

        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe > 0)
        {
            return text[..(exe + 4)];
        }

        // A Startup folder entry, a shortcut or a file, is the path itself; anything else names no file.
        return Path.IsPathRooted(text) ? text : null;
    }
}

/// <summary>
/// Where the five places of the Startup view are (SPEC.md PR-5.1, PR-5.8): Windows' own, or a stand-in of a test
/// run. With <see cref="Root"/> null the Run keys and the StartupApproved keys are the real ones, the user's under
/// HKEY_CURRENT_USER and the computer's under HKEY_LOCAL_MACHINE, and the folders are the two Startup folders. With a
/// root, a key under HKEY_CURRENT_USER\Software\AxKit, the same key paths lie under <c>&lt;root&gt;\User</c> and
/// <c>&lt;root&gt;\Computer</c> and the folders are the run's own, so that a check reads and changes nothing that
/// starts with Windows. <see cref="Administrator"/> says whether the entries of the whole computer may be changed.
/// </summary>
public sealed record StartupPlaces(string? Root, string UserFolder, string CommonFolder, bool Administrator)
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKey32 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    /// <summary>Every stand-in lies under this key of the user's, which is no place Windows starts anything from.</summary>
    public const string StandInParent = @"Software\AxKit\";

    /// <summary>
    /// The places of this process (PR-5.8): the stand-in of its test run when it is a window of one
    /// (<see cref="AppPaths.RunMark"/>), Windows' own otherwise. The only way to the real places for a change, and the
    /// window's alone: no test names it (<c>StartupChangeTests</c> reads the tests' sources for it).
    /// </summary>
    public static StartupPlaces Current => For(AppPaths.RunMark, AutomationEnvironment.Home);

    /// <summary>The places for a run's mark and folder; Windows' own when either is missing (the user's own window).</summary>
    public static StartupPlaces For(string? mark, string? home) =>
        mark is { Length: > 0 } && home is { Length: > 0 }
            ? Under(RunRoot(mark), Path.Combine(home, "AxKit", "startup"))
            : new StartupPlaces(
                null,
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                Environment.IsPrivilegedProcess);

    /// <summary>The key of a test run's stand-in: <c>HKEY_CURRENT_USER\Software\AxKit\Runs\&lt;mark&gt;\Startup</c>.</summary>
    public static string RunRoot(string mark) => StandInParent + @"Runs\" + mark + @"\Startup";

    /// <summary>
    /// A stand-in: the keys under <paramref name="root"/> (under HKEY_CURRENT_USER\Software\AxKit, or it is refused)
    /// and the folders <c>yours</c> and <c>everyone</c> of <paramref name="folder"/>. Never with administrator rights,
    /// so that its computer-wide entries are refused as a user's are.
    /// </summary>
    public static StartupPlaces Under(string root, string folder)
    {
        if (!root.StartsWith(StandInParent, StringComparison.OrdinalIgnoreCase) || root.Length == StandInParent.Length || root.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException($"A stand-in for the startup places lies under {StandInParent}", nameof(root));
        }

        return new StartupPlaces(root.TrimEnd('\\'), Path.Combine(folder, "yours"), Path.Combine(folder, "everyone"), false);
    }

    /// <summary>Whether the place is a Startup folder; otherwise it is a Run key.</summary>
    public static bool IsFolder(StartupPlace place) => place is StartupPlace.UserFolder or StartupPlace.CommonFolder;

    /// <summary>The Run key of a place: the hive and the key's path in it.</summary>
    public (RegistryKey Hive, string Path) RunKeyOf(StartupPlace place) => place switch
    {
        StartupPlace.UserRunKey => (Hive(computer: false), Prefix(computer: false) + RunKey),
        StartupPlace.MachineRunKey32 => (Hive(computer: true), Prefix(computer: true) + RunKey32),
        _ => (Hive(computer: true), Prefix(computer: true) + RunKey),
    };

    /// <summary>
    /// The StartupApproved key of a place (PR-5.2), in the hive of the entry itself: <c>Run</c> for the Run keys of the
    /// user and of the computer, <c>Run32</c> for the computer's 32-bit one, <c>StartupFolder</c> for the folders.
    /// </summary>
    public (RegistryKey Hive, string Path) ApprovedKeyOf(StartupPlace place)
    {
        var computer = StartupChange.NeedsAdministrator(place);
        var name = IsFolder(place) ? "StartupFolder" : place == StartupPlace.MachineRunKey32 ? "Run32" : "Run";
        return (Hive(computer), Prefix(computer) + Approved + name);
    }

    /// <summary>The folder of a folder place.</summary>
    public string FolderOf(StartupPlace place) => place == StartupPlace.UserFolder ? UserFolder : CommonFolder;

    private RegistryKey Hive(bool computer) => computer && Root is null ? Registry.LocalMachine : Registry.CurrentUser;

    private string Prefix(bool computer) => Root is null ? string.Empty : Root + (computer ? @"\Computer\" : @"\User\");
}

/// <summary>
/// The Startup view (SPEC.md PR-5): the Run keys of the user and of the computer (and its 32-bit twin) and the two
/// Startup folders, each entry enabled unless its StartupApproved value says Task Manager turned it off (PR-5.2).
/// <see cref="Read()"/> reads the registry and the folders; <see cref="Apply"/> turns an entry off or on, or removes
/// it (PR-5.4 to PR-5.7), in the places it is given, which are a test run's own in a test run (PR-5.8). The decisions
/// and the words are <see cref="StartupChange"/>; <see cref="IsEnabled"/> is pure.
/// </summary>
public static class StartupList
{
    /// <summary>
    /// Whether an entry is on: Task Manager writes 12 bytes under StartupApproved, the first even (02 or 06) when the
    /// entry runs and odd (03 or 01) when it was turned off. No value, or a short one, is on.
    /// </summary>
    public static bool IsEnabled(byte[]? approved) => approved is not { Length: > 0 } || approved[0] % 2 == 0;

    /// <summary>The list of this process's places: Windows' own, or the test run's (<see cref="StartupPlaces.Current"/>).</summary>
    public static IReadOnlyList<StartupEntry> Read() => Read(StartupPlaces.Current);

    public static IReadOnlyList<StartupEntry> Read(StartupPlaces places)
    {
        var entries = new List<StartupEntry>();
        ReadKey(entries, places, StartupPlace.UserRunKey);
        ReadKey(entries, places, StartupPlace.MachineRunKey);
        ReadKey(entries, places, StartupPlace.MachineRunKey32);
        ReadFolder(entries, places, StartupPlace.UserFolder);
        ReadFolder(entries, places, StartupPlace.CommonFolder);
        return Sorted(entries);
    }

    public static IReadOnlyList<StartupEntry> Sorted(IEnumerable<StartupEntry> entries) =>
        entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Place).ToList();

    /// <summary>
    /// Turn off, Turn on or Remove (PR-5.4 to PR-5.6), blocking; the window runs it on a background task. Nothing is
    /// tried for an entry of the whole computer without administrator rights, or one already in the state asked for
    /// (<see cref="StartupChange.Refusal"/>); an entry that is gone, a refusal of Windows and any other failure are
    /// the outcome, and nothing is changed then.
    /// </summary>
    public static StartupResult Apply(StartupPlaces places, StartupEntry entry, StartupAction action)
    {
        if (StartupChange.Refusal(action, entry, places.Administrator) is { } refused)
        {
            return new StartupResult(refused);
        }

        try
        {
            if (!Exists(places, entry))
            {
                return new StartupResult(StartupOutcome.Gone);
            }

            if (action == StartupAction.Remove)
            {
                Remove(places, entry);
            }
            else
            {
                // The switch Task Manager sets (PR-5.2, PR-5.4): the entry's value under its StartupApproved key.
                var (hive, path) = places.ApprovedKeyOf(entry.Place);
                using var approved = hive.CreateSubKey(path);
                approved.SetValue(StartupChange.ApprovedName(entry), StartupChange.ApprovedBytes(action == StartupAction.TurnOn, DateTime.UtcNow), RegistryValueKind.Binary);
            }

            return new StartupResult(StartupOutcome.Done);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new StartupResult(StartupOutcome.Denied);
        }
        catch (IOException ex)
        {
            return new StartupResult(StartupOutcome.Failed, ex.Message);
        }
    }

    /// <summary>Whether the entry is still where the list found it: the value in its Run key, or the file in its Startup folder and nowhere else.</summary>
    private static bool Exists(StartupPlaces places, StartupEntry entry)
    {
        if (StartupPlaces.IsFolder(entry.Place))
        {
            var folder = Path.GetFullPath(places.FolderOf(entry.Place)).TrimEnd('\\');
            return string.Equals(Path.GetDirectoryName(Path.GetFullPath(entry.Command)), folder, StringComparison.OrdinalIgnoreCase) && File.Exists(entry.Command);
        }

        var (hive, path) = places.RunKeyOf(entry.Place);
        using var key = hive.OpenSubKey(path);
        return key?.GetValue(entry.Name) is string;
    }

    /// <summary>
    /// The entry taken out for good (PR-5.6): the value of its Run key deleted, or the file in its Startup folder
    /// deleted (not to the Recycle Bin: the question says so). Its StartupApproved value goes with it, so that an entry
    /// of the same name made later is not off from its first day; that it could not be deleted is no failure.
    /// </summary>
    private static void Remove(StartupPlaces places, StartupEntry entry)
    {
        if (StartupPlaces.IsFolder(entry.Place))
        {
            File.Delete(entry.Command);
        }
        else
        {
            var (hive, path) = places.RunKeyOf(entry.Place);
            using var key = hive.OpenSubKey(path, writable: true);
            key?.DeleteValue(entry.Name, throwOnMissingValue: false);
        }

        try
        {
            var (hive, path) = places.ApprovedKeyOf(entry.Place);
            using var approved = hive.OpenSubKey(path, writable: true);
            approved?.DeleteValue(StartupChange.ApprovedName(entry), throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // The entry is gone; a switch left behind for it switches nothing.
        }
    }

    private static void ReadKey(List<StartupEntry> entries, StartupPlaces places, StartupPlace place)
    {
        try
        {
            var (hive, path) = places.RunKeyOf(place);
            using var key = hive.OpenSubKey(path);
            if (key is null)
            {
                return;
            }

            var (approvedHive, approvedPath) = places.ApprovedKeyOf(place);
            using var approved = approvedHive.OpenSubKey(approvedPath);
            foreach (var name in key.GetValueNames())
            {
                if (name.Length == 0 || key.GetValue(name) is not string command)
                {
                    continue;
                }

                entries.Add(new StartupEntry(name, command, place, IsEnabled(approved?.GetValue(name) as byte[])));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // A key the user may not read is left out.
        }
    }

    private static void ReadFolder(List<StartupEntry> entries, StartupPlaces places, StartupPlace place)
    {
        var folder = places.FolderOf(place);
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            return;
        }

        try
        {
            var (approvedHive, approvedPath) = places.ApprovedKeyOf(place);
            using var approved = approvedHive.OpenSubKey(approvedPath);
            foreach (var file in Directory.GetFiles(folder))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entries.Add(new StartupEntry(Path.GetFileNameWithoutExtension(file), file, place, IsEnabled(approved?.GetValue(fileName) as byte[])));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // The place is skipped; the others are listed.
        }
    }
}
