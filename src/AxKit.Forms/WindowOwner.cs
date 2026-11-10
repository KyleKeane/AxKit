using System.Runtime.InteropServices;
using System.Text;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// Whose window another program's is (2026-11-09, for axHelp and axDescribe, which tell Claude what was in front when
/// the hotkey was pressed, docs/axKit/ai-help.md AH-7): the program's name and the window's title, by the handle the
/// hotkey window kept (<see cref="HotkeyForm.ForegroundBefore"/>); null for what cannot be read.
/// </summary>
public static class WindowOwner
{
    public static (string? Program, string? Title) Describe(nint window)
    {
        if (window == 0)
        {
            return (null, null);
        }

        string? program = null;
        GetWindowThreadProcessId(window, out var owner);
        if (owner != 0)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById((int)owner);
                program = process.ProcessName;
            }
            catch (ArgumentException)
            {
                // Gone already.
            }
        }

        var buffer = new StringBuilder(512);
        var length = GetWindowText(window, buffer, buffer.Capacity);
        return (program, length > 0 ? buffer.ToString(0, length) : null);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder text, int max);
}
