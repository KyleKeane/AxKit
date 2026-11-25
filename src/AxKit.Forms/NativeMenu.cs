using System.Runtime.InteropServices;

namespace AxKit.Forms;

/// <summary>
/// A menu of the window's own items shown as a native Windows popup menu, the kind NVDA reads in every program
/// (axTemplate TP-5.5, 2026-11-24): a form field's templates were a WinForms <see cref="ContextMenuStrip"/>, and three
/// NVDA passes heard nothing of it open, while Windows' own edit menu in the same field was read at once. The items
/// stay the strip's (their labels with their letters, their click); this shows them and runs the one chosen. A Down
/// posted just before the menu's loop selects the first item for the keyboard, as File Explorer's Shift+F10 does, so
/// that NVDA reads it (axFile's shell menu, the same rule). The owner must be in the foreground; the call returns when
/// the menu closes.
/// </summary>
public static class NativeMenu
{
    /// <summary>Shows the enabled menu items of <paramref name="items"/> at <paramref name="screenPoint"/> and clicks the one chosen; false when nothing was chosen.</summary>
    public static bool Choose(IWin32Window owner, IReadOnlyList<ToolStripMenuItem> items, Point screenPoint)
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
        {
            return false;
        }

        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                AppendMenuW(menu, items[i].Enabled ? MfString : MfString | MfGrayed, (nuint)(i + 1), items[i].Text ?? string.Empty);
            }

            var id = (int)Track(menu, owner.Handle, screenPoint);
            if (id < 1 || id > items.Count)
            {
                return false;
            }

            items[id - 1].PerformClick();
            return true;
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    /// <summary>
    /// A native popup menu tracked at <paramref name="screenPoint"/> with its first item selected for the keyboard: a
    /// Down posted just before the menu's loop takes over, as File Explorer's Shift+F10 does, so that NVDA reads the
    /// item; the chosen item's id, 0 for none. axFile's shell menu and <see cref="Choose"/> track through it (repairs
    /// R-101, 2026-11-25: written in both until then).
    /// </summary>
    public static uint Track(nint menu, nint owner, Point screenPoint)
    {
        User32.PostMessage(owner, WmKeyDown, VkDown, 0);
        return TrackPopupMenuEx(menu, TpmReturnCmd, screenPoint.X, screenPoint.Y, owner, 0);
    }

    private const uint MfString = 0x0000;
    private const uint MfGrayed = 0x0001;
    private const uint TpmReturnCmd = 0x0100;
    private const int WmKeyDown = 0x0100;
    private const nint VkDown = 0x28;

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint menu, uint flags, nuint id, string text);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);
}
