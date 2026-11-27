using System.Runtime.InteropServices;

namespace AxKit.Forms;

/// <summary>
/// A secret on the clipboard (the security audit of 2026-10-24 S-15; axPass PW-5.4): the text with the marks
/// password managers set, which Windows clipboard history (Windows+V), the cloud clipboard and clipboard monitors such
/// as axClip honour by keeping no copy: <c>ExcludeClipboardContentFromMonitorProcessing</c>,
/// <c>CanIncludeInClipboardHistory</c> 0, <c>CanUploadToCloudClipboard</c> 0 and the older
/// <c>Clipboard Viewer Ignore</c>. <see cref="ClearIfUnchanged"/> empties the clipboard later only when nothing else
/// was copied meanwhile, told by the clipboard's sequence number, so that the secret is never read back to compare.
/// </summary>
public static class PrivateClipboard
{
    /// <summary>The clipboard's sequence number now: it changes with every change of the clipboard, so two readings tell whether anything was copied between them without reading what (axTemplate's launcher waits on it for the other program's copy, and gives the clipboard back only while it is unchanged).</summary>
    public static uint Sequence => GetClipboardSequenceNumber();

    /// <summary>Puts <paramref name="text"/> on the clipboard marked private; the clipboard's sequence number after it, for <see cref="ClearIfUnchanged"/>. Throws <see cref="ExternalException"/> when another program holds the clipboard.</summary>
    public static uint Copy(string text)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, text);
        foreach (var format in ExcludingFormats)
        {
            data.SetData(format, new MemoryStream([0]));
        }

        data.SetData(HistoryFormat, new MemoryStream(BitConverter.GetBytes(0)));
        data.SetData(CloudFormat, new MemoryStream(BitConverter.GetBytes(0)));
        Clipboard.SetDataObject(data, copy: true);
        return GetClipboardSequenceNumber();
    }

    /// <summary>Empties the clipboard when it still holds what <see cref="Copy"/> put there; true when it did.</summary>
    public static bool ClearIfUnchanged(uint sequence)
    {
        if (GetClipboardSequenceNumber() != sequence)
        {
            return false;
        }

        try
        {
            Clipboard.Clear();
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    /// <summary>The formats whose presence alone marks a copy as private: no clipboard history or monitor keeps it.</summary>
    public static readonly IReadOnlyList<string> ExcludingFormats = ["ExcludeClipboardContentFromMonitorProcessing", "Clipboard Viewer Ignore"];

    /// <summary>The format whose value 0 keeps a copy out of clipboard history (Windows' own history honours it too).</summary>
    public const string HistoryFormat = "CanIncludeInClipboardHistory";

    /// <summary>The format whose value 0 keeps a copy off the cloud clipboard.</summary>
    public const string CloudFormat = "CanUploadToCloudClipboard";

    /// <summary>
    /// The rule of a private copy, pure: it carries one of <see cref="ExcludingFormats"/>, or
    /// <see cref="HistoryFormat"/> or <see cref="CloudFormat"/> with the value 0 (<see cref="FlagValue"/>, which gives
    /// 0 for a value that is there and cannot be read, and null without the format).
    /// </summary>
    public static bool IsPrivate(bool excludingFormat, int? history, int? cloud) => excludingFormat || history == 0 || cloud == 0;

    /// <summary>
    /// Whether what the clipboard holds now was copied as private (a password from axPass or another password
    /// manager): axTemplate's launcher asks as it comes up, and never puts such a text back as plain text (TP-4.3).
    /// A clipboard another program holds open at that moment counts as private: the safe answer when it cannot be told.
    /// </summary>
    public static bool HoldsPrivateCopy(nint owner)
    {
        try
        {
            return IsPrivate(HasExcludingFormat(), FlagValue(HistoryFormat, owner), FlagValue(CloudFormat, owner));
        }
        catch (ExternalException)
        {
            return true;
        }
    }

    /// <summary>Whether the clipboard carries one of <see cref="ExcludingFormats"/> now; asked without opening the clipboard.</summary>
    public static bool HasExcludingFormat() => ExcludingFormats.Any(format => IsClipboardFormatAvailable(RegisterClipboardFormat(format)));

    /// <summary>
    /// The value of a format that holds a number in its first four bytes (<see cref="HistoryFormat"/>,
    /// <see cref="CloudFormat"/>) when the clipboard carries it; null without the format. A value that is there and
    /// cannot be read is 0, the value that keeps a copy out: until 2026-11-27 axClip, whose code this was, took it
    /// for no value and kept the copy. Read through the clipboard API, since WinForms' GetData is obsolete in
    /// .NET 10; a clipboard held open by another program throws <see cref="ExternalException"/>, as Clipboard's own
    /// reads do, for the caller's retry. <paramref name="owner"/> is the asking window's handle.
    /// </summary>
    public static int? FlagValue(string format, nint owner)
    {
        var id = RegisterClipboardFormat(format);
        if (!IsClipboardFormatAvailable(id))
        {
            return null;
        }

        if (!OpenClipboard(owner))
        {
            throw new ExternalException("The clipboard is open in another program");
        }

        try
        {
            var data = GetClipboardData(id);
            var pointer = data == 0 ? 0 : GlobalLock(data);
            if (pointer == 0)
            {
                return 0;
            }

            try
            {
                return GlobalSize(data) >= sizeof(int) ? Marshal.ReadInt32(pointer) : 0;
            }
            finally
            {
                GlobalUnlock(data);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterClipboardFormat(string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(nint owner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint GlobalSize(nint memory);
}
