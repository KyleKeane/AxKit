namespace AxKit.Core;

/// <summary>
/// Which processes belong to the test run this process belongs to (docs/axKit/automation.md, "Only the run's own
/// windows"; 2026-11-15). The NVDA tier runs a run's windows on the user's own desktop, where a script's Close all
/// windows once reached the user's own axClaude window: a window of a run must never list, message or close a window
/// outside the run.
///
/// The mark is a named object of Windows, one per process: every process of a run holds a mutex named after the run's
/// mark (<see cref="AppPaths.RunMark"/>) and its own process id for as long as it lives (<see cref="Join"/>, the
/// program's <c>Main</c>), and another process of the run asks whether that name exists (<see cref="Carries"/>). It
/// was chosen over the other ways because it is the plainest that holds for every kind of window: it needs no right
/// beyond the user's own (reading another process's environment does), no file to keep current (a list of process ids
/// goes stale and a number is reused), and it marks the process, so that hidden windows, message-only windows and a
/// window not yet shown are covered without each window doing anything (a window property would have to be set by
/// each). Windows removes the name when the process ends, so a reused process id never carries it.
///
/// It fails safe: in a run, a process whose name cannot be opened, for any reason, is not the run's and is left alone
/// (<see cref="Reaches(string?, bool)"/>). Outside a run nothing is asked and nothing changes.
/// </summary>
public static class RunMembership
{
    /// <summary>Held for the life of the process: the name lives as long as the process does.</summary>
    private static Mutex? _held;

    /// <summary>The name a process of the run with this mark holds.</summary>
    public static string Name(string mark, int processId) =>
        @"Local\AxKit.Run." + mark + "." + processId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The rule, pure: outside a run (<paramref name="ownMark"/> null) every window of the bundle is reached, as it
    /// always was; in a run only a process that carries the run's mark.
    /// </summary>
    public static bool Reaches(string? ownMark, bool otherCarriesMark) => ownMark is null || otherCarriesMark;

    /// <summary>This process takes the mark of its run; nothing outside a run. The program's <c>Main</c> calls it right after the run's variables are read, before any window exists.</summary>
    public static void Join()
    {
        if (_held is null && AppPaths.RunMark is { } mark)
        {
            _held = Hold(mark, Environment.ProcessId);
        }
    }

    /// <summary>The mark itself: the named mutex, never owned, only there to exist. The caller keeps it.</summary>
    public static Mutex Hold(string mark, int processId) => new(false, Name(mark, processId));

    /// <summary>Whether a process carries the mark of the run: its name exists. False for a process that never took it, has ended, or cannot be asked.</summary>
    public static bool Carries(string mark, int processId)
    {
        try
        {
            if (Mutex.TryOpenExisting(Name(mark, processId), out var mutex))
            {
                mutex.Dispose();
                return true;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or WaitHandleCannotBeOpenedException)
        {
            // Not this user's to open, or the name is another kind of object: not the run's.
        }

        return false;
    }

    /// <summary>Whether this process may list, message or close a window of that process: any of the bundle's outside a run, only the run's own in a run.</summary>
    public static bool Reaches(int processId)
    {
        var mark = AppPaths.RunMark;
        return Reaches(mark, mark is not null && Carries(mark, processId));
    }
}
