using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AxKit.Tests;

/// <summary>
/// Key messages as Windows delivers them, for a test of a control that has a handle and no window on screen
/// (docs/axKit/SPEC.md AX-6.4; the third review of 2026-11-27 asked for them, since the tests of the hold had pressed
/// keys by calling the control's own event methods, which is not the road a key takes). A key goes the two steps the
/// message loop takes it: first the pre-processing every key gets (<see cref="Control.PreProcessControlMessage(ref Message)"/>:
/// PreviewKeyDown, then the chain of ProcessCmdKey from the control up through its parents, then the dialog keys,
/// where a window presses its default and cancel buttons), and, when nothing there took it, the message itself to
/// the control's window procedure, which raises the key's event. The second parameter carries bit 30 for a repeat,
/// "the key was down already", as the keyboard sends it while a key is held. Shift, Ctrl and Alt are put into the
/// thread's own keyboard state for the call, since that is where WinForms reads them, and taken out again.
/// Nothing is shown, focused or sounded: a control without a parent is parked on a window that is never on screen.
/// </summary>
internal static class KeyMessages
{
    private const int WM_SETFOCUS = 0x0007;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_CHAR = 0x0102;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSCHAR = 0x0106;
    private const long WasDown = 1L << 30;
    private const long AltDown = 1L << 29;
    private const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12;

    [DllImport("user32.dll")]
    private static extern nint SendMessageW(nint window, int message, nint first, nint second);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetKeyboardState(byte[] state);

    /// <summary>
    /// The key down message of <paramref name="key"/> (with Shift, Ctrl or Alt in it when wanted) to
    /// <paramref name="control"/>, a press or, with <paramref name="repeat"/>, the repeat of a key held down. True
    /// when the pre-processing took the key (a command key, a dialog key); false when it went on to the control.
    /// </summary>
    public static bool Press(Control control, Keys key, bool repeat = false)
    {
        var alt = (key & Keys.Alt) != 0;
        var second = (repeat ? WasDown : 0) | (alt ? AltDown : 0) | 1;
        return Deliver(control, alt ? WM_SYSKEYDOWN : WM_KEYDOWN, (nint)(int)(key & Keys.KeyCode), (nint)second, key & Keys.Modifiers);
    }

    /// <summary>The character message that follows a key down which nothing suppressed: a typed character, or with <paramref name="alt"/> a letter typed with Alt down (a button's letter).</summary>
    public static bool Type(Control control, char character, bool repeat = false, bool alt = false)
    {
        var second = (repeat ? WasDown : 0) | (alt ? AltDown : 0) | 1;
        return Deliver(control, alt ? WM_SYSCHAR : WM_CHAR, character, (nint)second, alt ? Keys.Alt : Keys.None);
    }

    /// <summary>The message that tells a control it has the keyboard focus, as Windows sends it for every way the focus arrives; the focus itself moves nowhere.</summary>
    public static void Focus(Control control) => SendMessageW(control.Handle, WM_SETFOCUS, 0, 0);

    private static bool Deliver(Control control, int message, nint first, nint second, Keys modifiers)
    {
        var before = new byte[256];
        Assert.True(GetKeyboardState(before));
        var during = (byte[])before.Clone();
        during[VK_SHIFT] = (byte)((modifiers & Keys.Shift) != 0 ? 0x80 : 0);
        during[VK_CONTROL] = (byte)((modifiers & Keys.Control) != 0 ? 0x80 : 0);
        during[VK_MENU] = (byte)((modifiers & Keys.Alt) != 0 ? 0x80 : 0);
        Assert.True(SetKeyboardState(during));
        try
        {
            var sent = Message.Create(control.Handle, message, first, second);
            if (control.PreProcessControlMessage(ref sent) == PreProcessControlState.MessageProcessed)
            {
                return true;
            }

            SendMessageW(sent.HWnd, sent.Msg, sent.WParam, sent.LParam);
            return false;
        }
        finally
        {
            SetKeyboardState(before);
        }
    }
}
