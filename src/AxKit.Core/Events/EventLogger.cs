namespace AxKit.Core.Events;

/// <summary>
/// The log of events on the user's computer (docs/axKit/events.md section 9 point 5, the maintainer's decision of
/// 2026-11-25): one line for each event raised at the door, its time and its kind, and never a word of what was said
/// or shown. Off until the user turns it on (axKit menu, Keep a log of events; <see cref="BundleSettings.KeepEventLog"/>),
/// and nothing is written or kept while it is off. The file is <see cref="AppPaths.EventLogFile"/>, on this computer
/// alone; past <see cref="RollOverBytes"/> it becomes <c>events.old.log</c>, replacing the one before, as the
/// diagnostic logs do (<see cref="Log"/>), so that the two files never hold more than about two megabytes.
///
/// The door notes an event on the UI thread; the lines are written on a thread of the pool, so that the window never
/// waits on the disk (bundle rule 3). Several windows of axKit write to the same file, each line whole. Never throws.
/// The door also notes one kind once in a turn of the window's thread (its sound and its words are one event).
/// Tested in <c>tests/AxKit.Tests/EventLoggerTests</c>.
/// </summary>
public static class EventLogger
{
    public const long RollOverBytes = 1024 * 1024;

    private static readonly object Gate = new();
    private static readonly object WriteGate = new();
    private static readonly List<string> Waiting = [];
    private static bool _writing;
    private static volatile bool _on;
    /// <summary>The time the bundle's file was last written when this process last read it; null before the first read.</summary>
    private static DateTime? _settingsStamp;

    /// <summary>Whether events are written: the bundle's switch as this process last read it (<see cref="FollowSettings"/>).</summary>
    public static bool On
    {
        get => _on;
        set => _on = value;
    }

    /// <summary>The file: the user's (<see cref="AppPaths.EventLogFile"/>), or a test's own.</summary>
    public static string FilePath { get; set; } = AppPaths.EventLogFile;

    /// <summary>
    /// Reads the switch from the bundle's settings when their file changed since this process last read it: called as a
    /// window is made and activated, so that a switch turned in another window holds here from the next activation.
    /// Without the file the log is off, as by default.
    /// </summary>
    public static void FollowSettings(string path)
    {
        try
        {
            var stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (stamp == _settingsStamp)
            {
                return;
            }

            _settingsStamp = stamp;
            _on = stamp != DateTime.MinValue && SettingsFile.Load<BundleSettings>(path, out _).KeepEventLog;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The settings could not be read this time: the switch stays as it was.
        }
    }

    /// <summary>The line of one event: its local time and its kind's id, "2026-11-25 14:03:05.123 refused". Nothing else.</summary>
    public static string Line(EventKind kind, DateTime time) => $"{time:yyyy-MM-dd HH:mm:ss.fff} {EventPresenter.Id(kind)}";

    /// <summary>Notes one event: nothing at all while the log is off; otherwise its line waits and a thread of the pool writes it.</summary>
    public static void Note(EventKind kind)
    {
        if (!_on)
        {
            return;
        }

        var line = Line(kind, DateTime.Now);
        lock (Gate)
        {
            Waiting.Add(line);
            if (_writing)
            {
                return;
            }

            _writing = true;
        }

        ThreadPool.QueueUserWorkItem(_ => Flush());
    }

    /// <summary>Writes what waits, now, on the caller's thread: the pool's work, and a test's.</summary>
    public static void Flush()
    {
        string[] lines;
        lock (Gate)
        {
            lines = [.. Waiting];
            Waiting.Clear();
            _writing = false;
        }

        if (lines.Length > 0)
        {
            // One writer at a time in this process: a line noted while these are written starts the next writer.
            lock (WriteGate)
            {
                Write(FilePath, lines);
            }
        }
    }

    /// <summary>Appends the lines to the file, which first becomes <c>events.old.log</c> when it has passed <see cref="RollOverBytes"/>.</summary>
    public static void Write(string path, IReadOnlyList<string> lines)
    {
        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
            }

            if (File.Exists(path) && new FileInfo(path).Length > RollOverBytes)
            {
                File.Move(path, Path.ChangeExtension(path, ".old.log"), overwrite: true);
            }

            File.AppendAllText(path, string.Concat(lines.Select(line => line + "\r\n")));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Another window rolled the file over at the same moment, or the disk refused: these lines are lost, and
            // the window goes on (a log of events must never take an app down).
        }
    }
}
