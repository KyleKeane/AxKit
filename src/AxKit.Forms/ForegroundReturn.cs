using System.Runtime.InteropServices;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The one rule of the foreground for a window that hides and runs on (docs/axStart/SPEC.md AS-3.4; from
/// <see cref="HotkeyForm"/> on its second use, axClaude's window that hides on closing, docs/axClaude/SPEC.md
/// FR-9.6, 2026-11-15): while the window is hidden, its process never keeps the foreground. Windows activates a
/// hidden window of the process that hid one or that started hidden, and the user's typing then goes into a window
/// nobody can see; whenever that happens, the window that was in front before gets the foreground back. The window
/// holds one of these, calls <see cref="Remember"/> as it shows and <see cref="GiveBack"/> as it hides and on an
/// activation while hidden.
/// </summary>
public sealed class ForegroundReturn
{
    /// <summary>
    /// The window the foreground goes back to: the window of another program that was in front when this one last
    /// showed, or, before any showing, when the process started.
    /// </summary>
    public nint Window { get; private set; } = GetForegroundWindow();

    /// <summary>
    /// Before the window shows: the window in front now, kept as the one to give the foreground back to when it is
    /// another process's (this process's own windows, after a start that failed or a hide Windows answered with one
    /// of them, are not it). Returns the window in front, kept or not.
    /// </summary>
    public nint Remember()
    {
        var before = GetForegroundWindow();
        GetWindowThreadProcessId(before, out var owner);
        if (owner != (uint)Environment.ProcessId)
        {
            Window = before;
        }

        return before;
    }

    /// <summary>
    /// The rule: when <paramref name="hidden"/>, this process's window, is not visible and this process holds the
    /// foreground all the same, <see cref="Window"/> gets it, allowed because this process is in front at that moment.
    /// Nothing happens while the window shows or when another program has the front.
    /// </summary>
    public void GiveBack(nint hidden)
    {
        if (IsWindowVisible(hidden))
        {
            return;
        }

        GetWindowThreadProcessId(GetForegroundWindow(), out var owner);
        if (owner == (uint)Environment.ProcessId && Window != 0 && IsWindow(Window))
        {
            SetForegroundWindow(Window);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);
}
