namespace AxKit.Core;

/// <summary>
/// The mutex of a file that several windows read, change and write (docs/axKit/SPEC.md AX-5.7; repairs.md R-62):
/// its name, the same for every process that writes that file, and the wait for it. From
/// <see cref="RecentlyClosed"/> on its second use, the settings files (<see cref="SettingsFile"/>), 2026-11-22: two
/// windows of one app closed in the same instant each read the file, merged and wrote, and the second could find
/// the file being replaced, give up the merge and write its own settings alone, losing what the first had saved.
/// </summary>
public static class FileMutex
{
    /// <summary>The name of the mutex of <paramref name="file"/>: <paramref name="kind"/> and a hash of the file's full path, whatever its case.</summary>
    public static string Name(string kind, string file)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(file).ToLowerInvariant()));
        return kind + Convert.ToHexString(hash, 0, 16);
    }

    /// <summary>Whether this thread now holds the mutex, after waiting at most <paramref name="waitMilliseconds"/>; one a process ended while holding is held too.</summary>
    public static bool Held(Mutex mutex, int waitMilliseconds)
    {
        try
        {
            return mutex.WaitOne(waitMilliseconds);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }
}
