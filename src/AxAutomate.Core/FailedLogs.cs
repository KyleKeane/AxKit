namespace AxAutomate.Core;

/// <summary>
/// The logs of a script that failed, kept although the run's folder is deleted (a run without --keep): a rare failure
/// always leaves its exception to read. Seen 2026-11-14: an unexpected error in axTerminal's 02-commands.txt in one run
/// of an NVDA pass, never again, and its log gone with the run's folder. The logs are the windows' own of the run's home
/// folder (AXKIT_HOME), never the user's. They go to %TEMP%\axkit-automate-failed\&lt;run&gt;\&lt;script&gt;, and only the
/// newest ten runs' folders stay there.
/// </summary>
public static class FailedLogs
{
    /// <summary>How many runs' kept logs stay; an older run's folder is deleted when a newer one keeps logs.</summary>
    public const int RunsKept = 10;

    /// <summary>The folder every run's kept logs go under.</summary>
    public static string Folder => Path.Combine(Path.GetTempPath(), "axkit-automate-failed");

    /// <summary>
    /// Copies the logs of the failed script's run folder (<paramref name="temp"/>, its home's AxKit\logs) into
    /// <paramref name="kept"/>\&lt;script&gt;, and returns that folder; null when the script left no log.
    /// </summary>
    public static string? Keep(string temp, string scriptName, string kept)
    {
        var logs = Path.Combine(temp, "home", "AxKit", "logs");
        if (!Directory.Exists(logs) || Directory.GetFiles(logs, "*", SearchOption.AllDirectories).Length == 0)
        {
            return null;
        }

        var target = Path.Combine(kept, SafeName(scriptName));
        foreach (var file in Directory.GetFiles(logs, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(target, Path.GetRelativePath(logs, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            try
            {
                File.Copy(file, to, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A window still closing holds its log: the rest is kept.
            }
        }

        return target;
    }

    /// <summary>Deletes the oldest runs' folders under <paramref name="folder"/> until <see cref="RunsKept"/> are left.</summary>
    public static void Prune(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var old in Directory.GetDirectories(folder).OrderByDescending(Directory.GetCreationTimeUtc).Skip(RunsKept))
        {
            try
            {
                Directory.Delete(old, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Held by a reader: left for the next run.
            }
        }
    }

    /// <summary>A script's name as one folder: its folders and extension become dashes and nothing.</summary>
    public static string SafeName(string scriptName) =>
        Path.ChangeExtension(scriptName, null).Replace('\\', '-').Replace('/', '-');
}
