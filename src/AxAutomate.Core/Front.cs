namespace AxAutomate.Core;

/// <summary>What the runner does at one look at the foreground while no window of the run is in front (<see cref="Front.At"/>).</summary>
public enum FrontMove
{
    /// <summary>Nothing: the look after next may find the window in front.</summary>
    Wait,

    /// <summary>The window is asked to activate itself, with the runner's leave (AllowSetForegroundWindow): enough whenever Windows grants the runner the foreground.</summary>
    Ask,

    /// <summary>The runner takes the foreground for the window itself, with one Alt key press.</summary>
    Take,
}

/// <summary>
/// The NVDA tier's rules of the foreground, pure (docs/axKit/automation.md, section 3; 2026-11-15). NVDA follows the
/// window in front and the keys go to it, so a window of the run must be in front before any key. A full pass once
/// failed 120 of 123 scripts with "no window of the run is in front": the desktop was in front as the pass began, and
/// Windows then lets no other program's window come forward on its own asking. So the runner asks first and, where
/// that is not enough, takes the foreground itself (<see cref="Run"/>, <c>Native.TakeForeground</c>); and when even
/// that fails for the first window of the whole run, the run stops with one line instead of failing every script.
/// </summary>
public static class Front
{
    /// <summary>How often the foreground is looked at before a step gives up, 100 ms apart: three seconds.</summary>
    public const int Looks = 30;

    /// <summary>
    /// What to do at a look that found no window of the run in front: ask at once, take at half a second and again
    /// at a second and a half, wait otherwise. Two takes at most per step: each is an Alt key press that reaches the
    /// window in front, the only key the runner ever sends while a window outside the run is in front.
    /// </summary>
    public static FrontMove At(int look) => look switch
    {
        0 => FrontMove.Ask,
        5 or 15 => FrontMove.Take,
        _ => FrontMove.Wait,
    };

    /// <summary>
    /// Whether the whole run stops: a window that shows could not be brought to the front, and no window of the run
    /// has been in front yet, so every script after it would fail at its first key the same way. Once a window has
    /// been in front, a later failure is one step's (another program came up meanwhile), and the run goes on. A window
    /// that does not show (a background app started hidden) is never brought to the front and stops nothing.
    /// </summary>
    public static bool StopsRun(bool inFront, bool everInFront, bool windowShows) => !inFront && !everInFront && windowShows;

    /// <summary>The one line a stopped run says; <paramref name="inFront"/> names the program whose window stayed in front.</summary>
    public static string CannotTake(string inFront) =>
        $"The NVDA tier could not bring its window to the front: the window in front stayed {inFront}'s, after the run's window asked for the foreground and the runner took it twice with an Alt key press. No other key was sent, and the run stops here: bring any window but the desktop to the front, or unlock the session, and start the pass again.";

    /// <summary>
    /// After a take: whether Escape is pressed, once the window says its menu bar is active (the Alt key's release
    /// reached it). Only while that very window is in front: never a key to a window outside the run.
    /// </summary>
    public static bool LeavesMenu(bool windowInFront, bool menuActive) => windowInFront && menuActive;
}
