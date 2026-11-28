using System.Globalization;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace AxGit.Core;

/// <summary>
/// The one way axGIT reads a path that came from a repository (SPEC.md AG-1.6a; the review's fifth pass of
/// 2026-11-28, P2): a <c>.git</c> file's folder, a commondir, the alternates, the includes, the hooks folder, every
/// file a setting names, the repositories inside and <c>.git/modules</c>, the index, the pre-push hook, an untracked
/// file. Every read goes through <see cref="Place"/> first, and a path it refuses is never touched: not its
/// attributes, not its content (<c>RepositoryFilesTests</c> holds that the scan's files use no other way to the
/// disk).
///
/// A path is read only in a positive local form: its full path is a plain path on a drive of this computer
/// (<c>X:\</c>, not a network drive) with no link on its way from the drive's root, or it lies under the
/// repository's own folder, the one the user opened, with no link on its way from there. Anything else is a place
/// axGIT does not read: a share in any spelling (<c>\\host</c>, <c>//host</c>, <c>\\?\UNC\</c>, <c>\??\UNC\</c>), a
/// device path (<c>\\?\</c>, <c>\\.\</c>, <c>\??\</c>), an alternate stream (a colon after the drive's), a name
/// Windows takes for a device (<c>NUL</c>, <c>COM1</c>), a junction or a symbolic link on the way (each folder's own
/// attributes read from the top down, never what it points at). Tried with git 2.53 and .NET 10 through this
/// computer's own share: until then <c>\??\UNC\localhost\C$</c>, <c>\\.\C:</c>, <c>\??\C:</c>, a stream and a
/// junction were each read.
/// </summary>
public static class RepositoryFiles
{
    /// <summary>What a place this refuses is named as: "include.path names a place axGIT does not read".</summary>
    public const string NotRead = "a place axGIT does not read";

    /// <summary>The names Windows takes for a device in any folder.</summary>
    private static readonly HashSet<string> DeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// The full path of <paramref name="path"/> when it may be read, null when it is a place axGIT does not read.
    /// <paramref name="root"/>: the repository's own folder (a full path), under which a path is read wherever that
    /// folder lies; null for none. Its own attributes are the only ones read: of each folder on the way, which a
    /// link is told by.
    /// </summary>
    public static string? Place(string path, string? root) => Place(path, root, lastMayBeLink: false);

    /// <summary><see cref="Place"/>; <paramref name="lastMayBeLink"/>: the entry itself may be a link, for a read of its own attributes alone (which tell a link).</summary>
    private static string? Place(string path, string? root, bool lastMayBeLink)
    {
        string full;
        try
        {
            if (path.Length == 0 || IsDevicePath(path))
            {
                return null;
            }

            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        // A colon anywhere after a drive's is a stream; a share has none at all.
        if (IsDevicePath(full) || full.IndexOf(':', 2) >= 0)
        {
            return null;
        }

        var parts = full.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => DeviceNames.Contains(part.Split('.')[0].TrimEnd(' '))))
        {
            return null;
        }

        // Under the repository's own folder, wherever that lies (a repository opened on a share reads its own files there).
        if (root is not null && Under(root, full))
        {
            return NoLinkBetween(root, full, lastMayBeLink) ? full : null;
        }

        if (full.Length < 3 || !char.IsAsciiLetter(full[0]) || full[1] != ':' || full[2] != Path.DirectorySeparatorChar || IsNetworkDrive(full[0]))
        {
            return null;
        }

        return NoLinkBetween(full[..3], full, lastMayBeLink) ? full : null;
    }

    /// <summary>Whether a path is written as a device path: it starts with <c>\\?\</c>, <c>\\.\</c> or <c>\??\</c>, with either slash.</summary>
    private static bool IsDevicePath(string path) =>
        path.Length >= 4 && path[0] is '\\' or '/' && path[3] is '\\' or '/'
        && ((path[1] is '\\' or '/' && path[2] is '?' or '.') || (path[1] == '?' && path[2] == '?'));

    /// <summary>Whether a drive letter is a network drive, as Windows says.</summary>
    public static bool IsNetworkDrive(char letter)
    {
        try
        {
            return new DriveInfo(letter + ":\\").DriveType == DriveType.Network;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Whether a full path is <paramref name="root"/> or lies under it.</summary>
    public static bool Under(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative == "." || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }

    /// <summary>
    /// Whether no folder or file from below <paramref name="top"/> down to <paramref name="full"/> is a link, each
    /// read by its own attributes from the top down; what does not exist ends the look (nothing below it does).
    /// </summary>
    private static bool NoLinkBetween(string top, string full, bool lastMayBeLink)
    {
        var relative = Path.GetRelativePath(top, full);
        if (relative == ".")
        {
            return true;
        }

        var at = top;
        var parts = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in lastMayBeLink ? parts[..^1] : parts)
        {
            at = Path.Combine(at, part);
            FileAttributes kind;
            try
            {
                kind = File.GetAttributes(at);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Not there: nothing below it is either. Not readable: neither is what is below it.
                return ex is FileNotFoundException or DirectoryNotFoundException;
            }

            if (kind.HasFlag(FileAttributes.ReparsePoint) && IsLink(at, kind))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether an entry with a reparse point is a link to another place (a symbolic link or a junction); a folder
    /// or file of a cloud drive carries one too and is no link. One that cannot be told is taken for a link.
    /// </summary>
    public static bool IsLink(string path, FileAttributes kind)
    {
        try
        {
            return (kind.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(path).LinkTarget : new FileInfo(path).LinkTarget) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    // ---- The reads, each through Place ----

    /// <summary>An entry's own attributes (a link's, never what it points at); null when there is none or it is a place axGIT does not read.</summary>
    public static FileAttributes? Attributes(string path, string? root)
    {
        if (Place(path, root, lastMayBeLink: true) is not { } full)
        {
            return null;
        }

        try
        {
            return File.GetAttributes(full);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>A folder's entries, each with whether it is a folder and its own attributes; none when it cannot be listed or is a place axGIT does not read.</summary>
    public static List<(string Name, bool IsFolder, FileAttributes Attributes)> Entries(string folder, string? root)
    {
        if (Place(folder, root) is not { } full)
        {
            return [];
        }

        try
        {
            return [.. new FileSystemEnumerable<(string, bool, FileAttributes)>(full,
                (ref FileSystemEntry entry) => (entry.FileName.ToString(), entry.IsDirectory, entry.Attributes),
                new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true, ReturnSpecialDirectories = false })];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// The first <paramref name="limit"/> characters of a file as text, and whether it goes on; null when it is not
    /// there, is a folder or a link, cannot be read, or is a place axGIT does not read.
    /// </summary>
    public static (string Text, bool Cut)? Text(string path, string? root, int limit = 4096)
    {
        if (Place(path, root) is not { } full || Attributes(full, root) is not { } kind || kind.HasFlag(FileAttributes.Directory) || kind.HasFlag(FileAttributes.ReparsePoint))
        {
            return null;
        }

        try
        {
            using var reader = new StreamReader(full);
            var buffer = new char[limit];
            var read = reader.ReadBlock(buffer, 0, limit);
            return (new string(buffer, 0, read), reader.Peek() >= 0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// A reader of a file as text (UTF-8 unless it starts with another encoding's mark), for a read of the caller's
    /// own measure; null when it is a folder or a link, or a place axGIT does not read. Throws as opening a file does.
    /// </summary>
    public static StreamReader? Reader(string path, string? root)
    {
        if (Place(path, root) is not { } full || Attributes(full, root) is { } kind && (kind.HasFlag(FileAttributes.Directory) || kind.HasFlag(FileAttributes.ReparsePoint)))
        {
            return null;
        }

        return new StreamReader(full, detectEncodingFromByteOrderMarks: true);
    }

    /// <summary>A file's bytes, whole, up to <paramref name="limit"/>; null when it is not there, is larger, cannot be read or is a place axGIT does not read.</summary>
    public static byte[]? Bytes(string path, string? root, long limit)
    {
        if (Place(path, root) is not { } full || Attributes(full, root) is not { } kind || kind.HasFlag(FileAttributes.Directory) || kind.HasFlag(FileAttributes.ReparsePoint))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(full);
            if (stream.Length > limit)
            {
                return null;
            }

            var bytes = new byte[stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// A file's content as its SHA-256: "none" when there is no such file, "link" for a link (never read through),
    /// "unread" when it cannot be read, "not read" for a place axGIT does not read.
    /// </summary>
    public static string Hash(string path, string? root)
    {
        if (Place(path, root) is not { } full)
        {
            return "not read";
        }

        if (Attributes(full, root) is not { } kind || kind.HasFlag(FileAttributes.Directory))
        {
            return "none";
        }

        if (kind.HasFlag(FileAttributes.ReparsePoint))
        {
            return "link";
        }

        try
        {
            using var stream = File.OpenRead(full);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "unread";
        }
    }

    /// <summary>
    /// A link's target as it is written in the link, never followed (the review's seventh pass, T2: a link's risk is
    /// marked with it): "none" when there is no link there, "unread" when it cannot be read, "not read" for a place axGIT
    /// does not read.
    /// </summary>
    public static string LinkTarget(string path, string? root)
    {
        if (Place(path, root, lastMayBeLink: true) is not { } full)
        {
            return "not read";
        }

        try
        {
            if (Attributes(full, root) is not { } kind)
            {
                return "none";
            }

            return (kind.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(full).LinkTarget : new FileInfo(full).LinkTarget) ?? "none";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "unread";
        }
    }

    // ---- A folder's identity (the review's seventh pass, T1) ----

    /// <summary>
    /// Which folder <paramref name="path"/> is (<see cref="FolderIdentity"/>): its file system's name, from
    /// <c>GetVolumeInformationByHandleW</c>, and its volume's serial number and file ID, from
    /// <c>GetFileInformationByHandleEx</c> with <c>FileIdInfo</c> (128 bits, which ReFS needs; the 64 bits of
    /// <c>GetFileInformationByHandle</c> are not unique there). The handle is opened for its attributes alone, with
    /// backup semantics, which a folder needs, and on the entry itself, never what a final link points at. Null when it
    /// is a place axGIT does not read or cannot be opened; an identity with no file ID when the volume gives none.
    /// </summary>
    public static FolderIdentity? Identity(string path, string? root)
    {
        if (Place(path, root, lastMayBeLink: true) is not { } full)
        {
            return null;
        }

        using var handle = CreateFileW(full, FileReadAttributes, FileShareAll, IntPtr.Zero, OpenExisting, FileFlagBackupSemantics | FileFlagOpenReparsePoint, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            return null;
        }

        var name = new char[MaxPath + 1];
        var fileSystem = GetVolumeInformationByHandleW(handle, null, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, name, name.Length)
            ? new string(name, 0, Array.IndexOf(name, '\0') is >= 0 and var end ? end : name.Length)
            : string.Empty;
        if (!GetFileInformationByHandleEx(handle, FileIdInfoClass, out var id, Marshal.SizeOf<FileIdInfo>()) || (id.Low == 0 && id.High == 0))
        {
            return new FolderIdentity(fileSystem, string.Empty, string.Empty);
        }

        return new FolderIdentity(fileSystem, id.Volume.ToString("X16", CultureInfo.InvariantCulture), id.High.ToString("X16", CultureInfo.InvariantCulture) + id.Low.ToString("X16", CultureInfo.InvariantCulture));
    }

    private const uint FileReadAttributes = 0x80;

    private const uint FileShareAll = 0x7;

    private const uint OpenExisting = 3;

    private const uint FileFlagBackupSemantics = 0x02000000;

    private const uint FileFlagOpenReparsePoint = 0x00200000;

    private const int FileIdInfoClass = 18;

    private const int MaxPath = 260;

    /// <summary>FILE_ID_INFO: the volume's serial number and the 128 bits of the file ID, low half first.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FileIdInfo
    {
        public ulong Volume;
        public ulong Low;
        public ulong High;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformationByHandleW(SafeFileHandle file, char[]? volumeName, int volumeNameSize, IntPtr serialNumber, IntPtr maximumComponentLength, IntPtr fileSystemFlags, [Out] char[] fileSystemName, int fileSystemNameSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle file, int informationClass, out FileIdInfo information, int size);
}
