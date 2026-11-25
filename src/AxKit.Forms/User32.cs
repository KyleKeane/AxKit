using System.Runtime.InteropServices;

namespace AxKit.Forms;

/// <summary>
/// The Win32 calls and messages more than one piece of the bundle makes, declared once (2026-10-24: each was declared
/// two to four times). A file uses them through <c>using static AxKit.Forms.User32;</c>. A call only one file makes stays
/// declared in that file, next to its use.
/// </summary>
public static class User32
{
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int EM_SCROLLCARET = 0x00B7;
    public const int EM_GETLINECOUNT = 0x00BA;

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    /// <summary>The control with the keyboard focus in the calling thread's windows, active or not.</summary>
    [DllImport("user32.dll")]
    public static extern nint GetFocus();

    /// <summary>A window's long value by its index (a style, GWL_STYLE -16, GWL_EXSTYLE -20).</summary>
    [DllImport("user32.dll")]
    public static extern nint GetWindowLongPtrW(nint hWnd, int index);

    /// <summary>Milliseconds since the user's last key or mouse move in this session (<c>GetLastInputInfo</c>), -1 when Windows cannot tell: the AI tools' wait for a busy user, axPass's lock.</summary>
    public static long IdleMilliseconds()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? unchecked((uint)Environment.TickCount - info.Time) : -1;
    }

    /// <summary>The window flashed on the taskbar until it comes to the front (<c>FlashWindowEx</c>, all of it, until in front): a console window's turn that ended while the user was away, a notice an AI tool caused.</summary>
    public static void FlashUntilForeground(nint window)
    {
        var info = new FlashInfo { Size = (uint)Marshal.SizeOf<FlashInfo>(), Window = window, Flags = FlashAll | FlashTimerNoForeground };
        FlashWindowEx(ref info);
    }

    private const uint FlashAll = 3;
    private const uint FlashTimerNoForeground = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashInfo
    {
        public uint Size;
        public nint Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashInfo info);
}
