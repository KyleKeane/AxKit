using System.Buffers.Binary;
using System.Text;

namespace AxFile.Core;

/// <summary>An item of the Recycle Bin as its record tells it: where it came from and when it was deleted (a FILETIME).</summary>
public sealed record RecycleRecord(string OriginalPath, long DeletedAt);

/// <summary>
/// What the Recycle Bin remembers about an item (SPEC.md AF-4.14): a deleted file lives in the bin's folder as
/// <c>$R</c> followed by a random name, and next to it a <c>$I</c> file of the same name holds the original path and
/// the time of the deletion. The window reads the record of each item the shell lists to find the ones it deleted.
/// </summary>
public static class RecycleBin
{
    private const long Version2 = 2;
    private const int HeaderSize = 24;

    /// <summary>
    /// Whether a delete of this path goes to a Recycle Bin (AF-4.3): false on a share, a mapped network drive and a
    /// removable drive (a USB stick), which have none, and on a drive whose bin Windows was told not to use; there
    /// the shell's delete "with undo allowed" deletes for good without a word, so the window asks in those words and
    /// keeps nothing for Ctrl+Z. The drive's kind is asked first (it touches no network); only a drive of this
    /// computer is then asked for its bin (<c>SHQueryRecycleBinW</c> on its root). When Windows cannot say, the
    /// answer is no: the question is then the stronger one. Reads the disk: call it off the UI thread.
    /// </summary>
    public static bool Has(string path)
    {
        try
        {
            if (path.StartsWith(@"\\", StringComparison.Ordinal) || Path.GetPathRoot(Path.GetFullPath(path)) is not { Length: > 0 } root)
            {
                return false;
            }

            if (new DriveInfo(root).DriveType is not (DriveType.Fixed or DriveType.Ram))
            {
                return false;
            }

            var info = new SHQUERYRBINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<SHQUERYRBINFO>() };
            return SHQueryRecycleBinW(root, ref info) == 0 && !TurnedOff(root);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Whether Windows was told to delete at once on this drive (the Recycle Bin's own properties, "Don't move files
    /// to the Recycle Bin"): the value <c>NukeOnDelete</c> under the drive's volume in the user's part of the registry.
    /// </summary>
    private static bool TurnedOff(string root)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var name = new StringBuilder(64);
            if (!GetVolumeNameForVolumeMountPointW(root, name, (uint)name.Capacity))
            {
                return false;
            }

            // "\\?\Volume{guid}\": the key is named by the braces and what is between them.
            var volume = name.ToString();
            var from = volume.IndexOf('{');
            var to = volume.IndexOf('}');
            if (from < 0 || to < from)
            {
                return false;
            }

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\" + volume[from..(to + 1)]);
            return key?.GetValue("NukeOnDelete") is int nuke && nuke != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHQueryRecycleBinW(string rootPath, ref SHQUERYRBINFO info);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetVolumeNameForVolumeMountPointW(string mountPoint, StringBuilder name, uint length);

    /// <summary>The record of the item whose <c>$R…</c> path this is, read from its <c>$I…</c> file; null when the path is not such an item or the record cannot be read.</summary>
    public static RecycleRecord? ReadRecord(string recycledPath)
    {
        var name = Path.GetFileName(recycledPath);
        if (!name.StartsWith("$R", StringComparison.Ordinal) || Path.GetDirectoryName(recycledPath) is not { } folder)
        {
            return null;
        }

        try
        {
            return Parse(File.ReadAllBytes(Path.Combine(folder, "$I" + name[2..])));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// The record's bytes: a version (8 bytes), the size (8), the deletion time (8), then the path, with its length in
    /// characters (4 bytes) before it from version 2 (Windows 10) and as a fixed field before that; null when the
    /// bytes are not a record.
    /// </summary>
    public static RecycleRecord? Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length <= HeaderSize)
        {
            return null;
        }

        var version = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        var deleted = BinaryPrimitives.ReadInt64LittleEndian(bytes[16..]);
        string path;
        if (version == Version2)
        {
            if (bytes.Length < HeaderSize + 4)
            {
                return null;
            }

            var length = BinaryPrimitives.ReadInt32LittleEndian(bytes[HeaderSize..]);
            var available = bytes.Length - HeaderSize - 4;
            path = Encoding.Unicode.GetString(bytes.Slice(HeaderSize + 4, Math.Clamp(length * 2, 0, available)));
        }
        else
        {
            path = Encoding.Unicode.GetString(bytes[HeaderSize..]);
        }

        var end = path.IndexOf('\0');
        if (end >= 0)
        {
            path = path[..end];
        }

        return path.Length == 0 ? null : new RecycleRecord(path, deleted);
    }
}
