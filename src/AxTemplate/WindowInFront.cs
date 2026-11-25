using System.Runtime.InteropServices;
using System.Text;
using AxTemplate.Core;
using static AxKit.Forms.User32;

namespace AxTemplate;

/// <summary>
/// What the window the launcher came from does with a template's line breaks (SPEC.md TP-4.8): its own class and the
/// class and style of the control that has the focus in its thread, as Windows gives them for any program, read with
/// no key sent; <see cref="PasteTarget.Of"/> decides. A window that cannot be read keeps the template as it is.
/// </summary>
internal static class WindowInFront
{
    private const int GWL_STYLE = -16;

    public static LineBreaks LineBreaksOf(nint window)
    {
        if (window == 0)
        {
            return LineBreaks.Kept;
        }

        var info = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        var focus = GetGUIThreadInfo(GetWindowThreadProcessId(window, out _), ref info) ? info.hwndFocus : 0;
        var style = focus == 0 ? 0 : (long)GetWindowLongPtrW(focus, GWL_STYLE);
        return PasteTarget.Of(ClassOf(window), focus == 0 ? string.Empty : ClassOf(focus), style);
    }

    private static string ClassOf(nint window)
    {
        var name = new StringBuilder(256);
        return GetClassNameW(window, name, name.Capacity) > 0 ? name.ToString() : string.Empty;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public nint hwndActive;
        public nint hwndFocus;
        public nint hwndCapture;
        public nint hwndMenuOwner;
        public nint hwndMoveSize;
        public nint hwndCaret;
        public RECT rcCaret;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GUITHREADINFO info);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(nint window, StringBuilder name, int length);
}
