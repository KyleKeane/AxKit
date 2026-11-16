namespace AxKit.Core;

/// <summary>
/// Which paths a menu may ask the file system about as it opens (docs/axKit/SPEC.md AX-9.4, AX-9.5): a path on a
/// drive of this computer answers at once, while a network share or a mapped network drive can take the network's
/// timeout to answer, which no menu may wait for (2026-10-23: the recent lists checked every path, and an unreachable
/// share stalled the menu). Pure, tested.
/// </summary>
public static class LocalPaths
{
    /// <summary>Whether the path is on a drive of this computer (fixed, removable, a disc, a RAM disk); false for a share, a mapped network drive, or a path without a root. Asking the drive's kind touches no network.</summary>
    public static bool IsLocal(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            return new DriveInfo(root).DriveType is DriveType.Fixed or DriveType.Removable or DriveType.CDRom or DriveType.Ram;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>What <see cref="NotOpenedAtStart"/> names as the way to the path, in the windows with an Open recent submenu.</summary>
    public const string OpenRecent = "Open recent";

    /// <summary>What a window says when the path it remembered is not on a drive of this computer and was not looked at as it started (AX-9.5a; repairs.md R-64); <paramref name="how"/> is the command that opens it.</summary>
    public static string NotOpenedAtStart(string path, string how = OpenRecent) =>
        $"{path} is on the network and was not opened at start; {how} opens it.";

    /// <summary>
    /// The path a window remembered, to start on (AX-9.5a; repairs.md R-64): the path when it is on a drive of this
    /// computer and <paramref name="exists"/> says it is there; null otherwise, for the window's fallback. A path
    /// elsewhere (a share, a mapped network drive) is not asked about at all, since the answer can take the
    /// network's time limit while no window shows: <paramref name="notOpened"/> is then the sentence to say.
    /// A path that is not whole (no drive) is only not found.
    /// </summary>
    public static string? AtStart(string? remembered, Func<string, bool> exists, out string? notOpened, string how = OpenRecent)
    {
        notOpened = null;
        if (string.IsNullOrWhiteSpace(remembered) || !Path.IsPathFullyQualified(remembered))
        {
            return null;
        }

        if (!IsLocal(remembered))
        {
            notOpened = NotOpenedAtStart(remembered, how);
            return null;
        }

        return exists(remembered) ? remembered : null;
    }

    /// <summary>Whether the path still exists, asked through <paramref name="exists"/> of a local path only: a path elsewhere is listed as it is, and found missing when it is chosen.</summary>
    public static bool ExistsQuickly(string path, Func<string, bool> exists) => !IsLocal(path) || exists(path);
}
