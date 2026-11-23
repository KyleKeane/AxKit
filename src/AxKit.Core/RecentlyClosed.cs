namespace AxKit.Core;

/// <summary>A file or folder a window of the bundle held when it closed or replaced it: the app's name and the path; an empty path for an app that holds none (axProcess), reopened by its name alone.</summary>
public sealed record ClosedItem(string App, string Path);

/// <summary>The shape of the file: one list, the newest first.</summary>
internal sealed class RecentlyClosedList
{
    public List<ClosedItem> Items { get; set; } = [];

    /// <summary>The members of the file this version does not know, kept and written back (AX-5.8; repairs.md R-67).</summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? OtherMembers { get; set; }
}

/// <summary>
/// The Recently closed list of the Window menu (docs/axKit/SPEC.md AX-9.4): the last ten files and folders the windows of
/// every app held when they closed or swapped them, in one file for the bundle (<see cref="AppPaths.RecentlyClosedFile"/>,
/// AX-5.1). A window calls <see cref="Remember"/> when it closes or replaces its file; the menu calls <see cref="Load"/>
/// when it opens. The file is written whole through <see cref="SettingsFile"/>, by one window at a time, and only
/// whole paths are kept in it and read from it (repairs.md R-68). Tested in <c>tests/AxKit.Tests</c>.
/// </summary>
public static class RecentlyClosed
{
    public const int Capacity = RecentPaths.Max;

    /// <summary>The list with the item at the front and its earlier entry, same app and path, gone; at most <see cref="Capacity"/> entries.</summary>
    public static IReadOnlyList<ClosedItem> Add(IReadOnlyList<ClosedItem> items, ClosedItem item) =>
        [item, .. items.Where(i => !Same(i, item)).Take(Capacity - 1)];

    /// <summary>How long a window waits for another window's write of the list before it gives its own up: half a second.</summary>
    public const int WaitMilliseconds = 500;

    /// <summary>Puts the app's file or folder at the front of the bundle's list. A file that cannot be written is logged and nothing else.</summary>
    public static void Remember(string app, string path) => Remember(AppPaths.RecentlyClosedFile, app, path);

    /// <summary>
    /// Puts the app's file or folder at the front of the list in <paramref name="file"/>; false when nothing was
    /// written. A path that is not whole is not kept (<see cref="Keeps"/>). One writer at a time (repairs.md R-68):
    /// the reading, the adding and the writing happen under a mutex named after the file, so that two windows closing
    /// in the same instant both end up on the list. A window that does not get the mutex within
    /// <paramref name="waitMilliseconds"/> writes nothing and logs that, and never hangs; so does a file that cannot
    /// be written.
    /// </summary>
    public static bool Remember(string file, string app, string path, int waitMilliseconds = WaitMilliseconds)
    {
        var item = new ClosedItem(app, path);
        if (!Keeps(item))
        {
            Log.Info("The recently closed list keeps whole paths only: an entry was not kept");
            return false;
        }

        try
        {
            using var mutex = new Mutex(false, MutexName(file));
            if (!FileMutex.Held(mutex, waitMilliseconds))
            {
                Log.Info("The recently closed list was not written: another window was writing it");
                return false;
            }

            try
            {
                Write(file, Add(Read(file), item));
                return true;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or WaitHandleCannotBeOpenedException)
        {
            Log.Error("The recently closed list was not written", ex);
            return false;
        }
    }

    /// <summary>The name of the mutex of a list's file: the same for every process that writes that file, whatever the case of its path.</summary>
    public static string MutexName(string file) => FileMutex.Name("AxKit.RecentlyClosed.", file);

    /// <summary>
    /// Whether an entry is one the list keeps (repairs.md R-68): an app's name, and a path that is whole
    /// (<see cref="RecentPaths.IsWhole"/>) or empty, which is an app that holds no file. Anything else in the file
    /// was not written by a window, and a menu item must never hand it to an app, where text such as "--secret"
    /// would be read as an option.
    /// </summary>
    public static bool Keeps(ClosedItem? item) =>
        item is { App: not null, Path: not null } && !string.IsNullOrWhiteSpace(item.App) && (item.Path.Length == 0 || RecentPaths.IsWhole(item.Path));

    /// <summary>The list for the menu: the entries whose file or folder still exists, the newest first; one on a share is listed without the check, which could wait for the network (<see cref="LocalPaths"/>).</summary>
    public static IReadOnlyList<ClosedItem> Load() =>
        Read(AppPaths.RecentlyClosedFile).Where(i => i.Path.Length == 0 || LocalPaths.ExistsQuickly(i.Path, path => File.Exists(path) || Directory.Exists(path))).ToList();

    /// <summary>The entries of the file the list keeps (<see cref="Keeps"/>), the newest first.</summary>
    public static IReadOnlyList<ClosedItem> Read(string file) =>
        [.. (SettingsFile.Load<RecentlyClosedList>(file, out _).Items ?? []).Where(Keeps)];

    /// <summary>The file as it is, with the list replaced: what a newer version keeps beside it stays (AX-5.8).</summary>
    public static void Write(string file, IReadOnlyList<ClosedItem> items)
    {
        var list = SettingsFile.Load<RecentlyClosedList>(file, out _);
        list.Items = [.. items];
        SettingsFile.Save(file, list);
    }

    /// <summary>"notes.md (C:\notes\notes.md) - axDown", "myproject (C:\src\myproject) - axClaude": the name, the full path in parentheses, the app (AX-9.5).</summary>
    public static string Title(ClosedItem item)
    {
        // The app as the user meets it, an access layer with its keyword (AX-11.2).
        var app = Dispatch.ByName(item.App)?.Title ?? item.App;
        if (item.Path.Length == 0)
        {
            // An app that holds no file (axProcess): its window by its name.
            return app;
        }

        var name = PathName.Of(item.Path);
        return (name == item.Path ? item.Path : $"{name} ({item.Path})") + " - " + app;
    }

    private static bool Same(ClosedItem a, ClosedItem b) =>
        string.Equals(a.App, b.App, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
}
