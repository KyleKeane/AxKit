using System.Buffers.Binary;

namespace AxProcess.Core;

/// <summary>What can be done to a startup entry (SPEC.md PR-5.4 to PR-5.6).</summary>
public enum StartupAction
{
    TurnOff,
    TurnOn,
    Remove,
}

/// <summary>What a change of a startup entry came to (PR-5.7).</summary>
public enum StartupOutcome
{
    /// <summary>Changed.</summary>
    Done,

    /// <summary>Already off, or already on: nothing to do.</summary>
    Already,

    /// <summary>Set for every user of the computer, and this process has no administrator rights: nothing was tried.</summary>
    NeedsAdministrator,

    /// <summary>The entry is no longer where the list found it.</summary>
    Gone,

    /// <summary>Windows refused the change.</summary>
    Denied,

    /// <summary>Another failure, with Windows' message.</summary>
    Failed,
}

/// <summary>The outcome of a change, with Windows' message for <see cref="StartupOutcome.Failed"/>.</summary>
public sealed record StartupResult(StartupOutcome Outcome, string? Error = null);

/// <summary>
/// The decisions and the words of Turn off, Turn on and Remove (SPEC.md PR-5.4 to PR-5.7), all pure: which entries
/// need administrator rights, the bytes of Task Manager's switch, what is refused before anything is tried, the
/// question before a removal and the one sentence said after each. The registry and the folders are
/// <see cref="StartupList.Apply"/>'s.
/// </summary>
public static class StartupChange
{
    /// <summary>
    /// Whether the entry is set for every user of the computer (PR-5.5): the computer's Run keys and all users'
    /// Startup folder, which only an administrator may change, as their StartupApproved keys under
    /// HKEY_LOCAL_MACHINE. The user's own Run key and Startup folder need nothing.
    /// </summary>
    public static bool NeedsAdministrator(StartupPlace place) =>
        place is StartupPlace.MachineRunKey or StartupPlace.MachineRunKey32 or StartupPlace.CommonFolder;

    /// <summary>
    /// The value Task Manager writes under StartupApproved (PR-5.2, PR-5.4), 12 bytes: on is 02 and eleven zero
    /// bytes; off is 03, three zero bytes, and the time it was turned off as a FILETIME, low byte first.
    /// </summary>
    public static byte[] ApprovedBytes(bool on, DateTime utcNow)
    {
        var bytes = new byte[12];
        bytes[0] = on ? (byte)0x02 : (byte)0x03;
        if (!on)
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(4), utcNow.ToFileTimeUtc());
        }

        return bytes;
    }

    /// <summary>The name of an entry's StartupApproved value: the Run key's value name, or the file's name with its extension for a Startup folder.</summary>
    public static string ApprovedName(StartupEntry entry) =>
        StartupPlaces.IsFolder(entry.Place) ? Path.GetFileName(entry.Command) : entry.Name;

    /// <summary>
    /// What is refused before anything is tried (PR-5.5, PR-5.7): an entry of the whole computer without
    /// administrator rights, then one already in the state asked for; null when the change may be tried.
    /// </summary>
    public static StartupOutcome? Refusal(StartupAction action, StartupEntry entry, bool administrator)
    {
        if (NeedsAdministrator(entry.Place) && !administrator)
        {
            return StartupOutcome.NeedsAdministrator;
        }

        return (action == StartupAction.TurnOff && !entry.Enabled) || (action == StartupAction.TurnOn && entry.Enabled)
            ? StartupOutcome.Already
            : null;
    }

    /// <summary>The title of the question before a removal (PR-5.6).</summary>
    public static string RemoveQuestion(StartupEntry entry) => $"Remove {entry.Name} from the programs that start with Windows?";

    /// <summary>The question's text (PR-5.6): the command, the place, what is deleted, and that Turn off is the gentler way.</summary>
    public static string RemoveText(StartupEntry entry) => string.Join("\n",
    [
        $"Command {entry.Command}",
        $"Set in {StartupEntry.PlaceText(entry.Place)}",
        StartupPlaces.IsFolder(entry.Place)
            ? "The file in the Startup folder is deleted for good, not sent to the Recycle Bin. The program itself stays on the computer."
            : "The entry is deleted from the Run key for good. The program itself stays on the computer.",
        "Turn off is the gentler way: it keeps the entry, and Turn on brings it back.",
    ]);

    /// <summary>The one sentence said after a change or in its place (PR-5.7).</summary>
    public static string Said(StartupAction action, StartupEntry entry, StartupResult result)
    {
        var name = entry.Name;
        var verb = action switch { StartupAction.TurnOff => "turn off", StartupAction.TurnOn => "turn on", _ => "remove" };
        return result.Outcome switch
        {
            StartupOutcome.Done => action switch
            {
                StartupAction.TurnOff => $"{name} turned off: it no longer starts with Windows",
                StartupAction.TurnOn => $"{name} turned on: it starts with Windows again",
                _ => $"{name} removed from the programs that start with Windows",
            },
            StartupOutcome.Already => $"{name} is already turned {(action == StartupAction.TurnOff ? "off" : "on")}",
            StartupOutcome.NeedsAdministrator => action == StartupAction.Remove
                ? $"{name} is set for every user of this computer: removing it needs administrator rights. Task Manager's Startup apps tab, or Settings, Apps, Startup, turns it off"
                : $"{name} is set for every user of this computer: turning it {(action == StartupAction.TurnOff ? "off" : "on")} needs administrator rights. Use Task Manager's Startup apps tab, or Settings, Apps, Startup",
            StartupOutcome.Gone => $"{name} is no longer among the programs that start with Windows. Nothing changed",
            StartupOutcome.Denied => $"Could not {verb} {name}: access is denied. Nothing changed",
            _ => $"Could not {verb} {name}: {result.Error}",
        };
    }
}
