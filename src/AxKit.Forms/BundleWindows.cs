using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AxKit.Core;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The one place that finds the windows of other axKit processes (2026-11-15): the visible ones for the Window menu,
/// Close all windows, the hand-over to an app's open window and the AI tools' order (<see cref="Visible"/>), a hidden
/// one by its title for a background app's second start (<see cref="Titled"/>), and the check of one window a
/// hand-over is sent to or came from (<see cref="Reachable"/>). Every one of them asks
/// <see cref="RunMembership.Reaches(int)"/>: a window of a test run finds only the windows of its own run, never the
/// user's own, which a script's Close all windows reached in the NVDA tier, where the run's windows are on the user's
/// desktop (docs/axKit/automation.md, "Only the run's own windows"). Outside a run nothing is left out.
/// <c>WindowReachTests</c> fails when another file of <c>src</c> lists windows or processes by name itself.
/// </summary>
public static class BundleWindows
{
    /// <summary>
    /// The visible, unowned, titled top-level windows of every process with this one's name, in Z order: the most
    /// recently used first. In a test run, only those of the run's own processes.
    /// </summary>
    public static IReadOnlyList<AxKitWindow> Visible()
    {
        var processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
        var ids = new HashSet<int>(processes.Select(p => p.Id).Where(RunMembership.Reaches));
        foreach (var process in processes)
        {
            process.Dispose();
        }

        var windows = new List<AxKitWindow>();
        EnumWindows((handle, _) =>
        {
            if (IsWindowVisible(handle) && GetWindow(handle, GW_OWNER) == 0)
            {
                GetWindowThreadProcessId(handle, out var id);
                if (ids.Contains((int)id))
                {
                    var title = TitleOf(handle);
                    if (title.Length > 0)
                    {
                        windows.Add(new AxKitWindow(handle, title));
                    }
                }
            }

            return true;
        }, 0);
        return windows;
    }

    /// <summary>
    /// The window, hidden or not, with exactly this title in another process of this very program file
    /// (<see cref="ThisProgram.Runs"/>), and in a test run of the run's own; 0 when none.
    /// </summary>
    public static nint Titled(string title)
    {
        var process = (uint)Environment.ProcessId;
        for (var window = FindWindowExW(0, 0, null, title); window != 0; window = FindWindowExW(0, window, null, title))
        {
            GetWindowThreadProcessId(window, out var owner);
            if (owner != process && Reachable(owner))
            {
                return window;
            }
        }

        return 0;
    }

    /// <summary>Whether a window is one this process may hand something to or take something from: its process runs this very program file (S-13) and, in a test run, belongs to the run.</summary>
    public static bool Reachable(nint window)
    {
        GetWindowThreadProcessId(window, out var processId);
        return Reachable(processId);
    }

    private static bool Reachable(uint processId) => ThisProgram.Runs(processId) && RunMembership.Reaches((int)processId);

    private static string TitleOf(nint handle)
    {
        var length = GetWindowTextLength(handle);
        if (length == 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        GetWindowText(handle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private const uint GW_OWNER = 4;

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string title);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint hWnd, uint cmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder text, int max);
}
