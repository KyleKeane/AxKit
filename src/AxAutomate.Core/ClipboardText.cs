using System.Runtime.InteropServices;

namespace AxAutomate.Core;

/// <summary>
/// The clipboard's text, read and written through Windows itself (docs/axKit/automation.md, tier 2). The run's desktop
/// shares the clipboard with the user's, and some scripts write it on purpose (axClip's), so the command line reads the
/// text before its first script and puts it back after its last. A clipboard that held no text before the run has a
/// script's text taken off it again, so that a test text is not what the user pastes next; a picture or files it held
/// before cannot be kept, since a script's text replaced them.
/// </summary>
public static class ClipboardText
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(nint owner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    private static extern nint GetClipboardData(uint format);

    [DllImport("user32.dll")]
    private static extern nint SetClipboardData(uint format, nint memory);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalFree(nint memory);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(nint memory);

    /// <summary>The clipboard's text, or null when it holds none or could not be opened.</summary>
    public static string? Read()
    {
        if (!Open())
        {
            return null;
        }

        try
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                return null;
            }

            var memory = GetClipboardData(CF_UNICODETEXT);
            var pointer = memory == 0 ? 0 : GlobalLock(memory);
            if (pointer == 0)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                GlobalUnlock(memory);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Puts the text on the clipboard in place of what is there; false when the clipboard could not be opened or written.</summary>
    public static bool Write(string text)
    {
        if (!Open())
        {
            return false;
        }

        try
        {
            var bytes = (text.Length + 1) * 2;
            var memory = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes);
            var pointer = memory == 0 ? 0 : GlobalLock(memory);
            if (pointer == 0)
            {
                return false;
            }

            Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
            Marshal.WriteInt16(pointer, text.Length * 2, 0);
            GlobalUnlock(memory);
            EmptyClipboard();
            if (SetClipboardData(CF_UNICODETEXT, memory) == 0)
            {
                // The memory is Windows' only once it took it.
                GlobalFree(memory);
                return false;
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// Puts back the text <see cref="Read"/> gave before the run, when the clipboard no longer holds it. Null (no text
    /// before the run) changes nothing. Returns what it did, for the console.
    /// </summary>
    public static string PutBack(string? before)
    {
        if (before is null)
        {
            // No text before. Text there now is a script's test text, which must not be what the user pastes next: it
            // is taken off. What was there before, if anything (a picture, files), the script replaced and is gone.
            if (Read() is null)
            {
                return "The clipboard held no text before the run and holds none now.";
            }

            return Clear()
                ? "The clipboard held no text before the run: a script's text was taken off it."
                : "The clipboard held no text before the run; a script's text could not be taken off it.";
        }

        if (Read() == before)
        {
            return "The clipboard's text is as it was before the run.";
        }

        return Write(before)
            ? "The clipboard's text of before the run is back."
            : "The clipboard's text of before the run could not be put back.";
    }

    /// <summary>Empties the clipboard; false when it could not be opened.</summary>
    public static bool Clear()
    {
        if (!Open())
        {
            return false;
        }

        try
        {
            return EmptyClipboard();
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Another program may hold the clipboard for a moment: ten tries, 50 ms apart.</summary>
    private static bool Open()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (OpenClipboard(0))
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }
}
