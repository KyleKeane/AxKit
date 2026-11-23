namespace AxKit.Core;

/// <summary>
/// The Store's copy and its background apps (docs/axKit/SPEC.md AX-13.4a, AX-13.4b). Two kinds of app run in the
/// background there.
///
/// The ones with a startup task of the package (axStart, axClip, axHelp: <see cref="App.AtSignIn"/>; the user's
/// report of 2026-11-14 from a second computer: axStart's hotkey did nothing until Windows was restarted). Windows
/// runs a startup task at a sign-in and only after an app of the package was started once. So the first start of
/// any app of the Store's copy starts them itself, hidden, once per install: a marker file says it was done, and
/// since a packaged process's new files land in the package's private folder, the marker goes with an uninstall
/// and a new install starts them again. After that Windows' Startup apps page alone decides (AX-13.4), so an app
/// the user turned off there is never started behind their back.
///
/// The one without an entry of its own, and so without a startup task (axTemplate's launcher:
/// <see cref="App.InBackground"/>; the maintainer's word of 2026-11-23: its hotkey is on for everyone after an
/// installation and after an update). It is started hidden with that first start too; with the first start of any
/// app after an update, which the marker tells by holding the version it was last written for; and whenever one of
/// the startup tasks starts its app hidden, which is the sign-in. After the first start, its own Start with
/// Windows setting says no (<see cref="SignInStart.TurnedOff"/>), since Windows' page does not list it.
///
/// The rules are pure; <c>Program.Main</c> reads the marker and the settings and starts the processes.
/// </summary>
public static class StoreFirstStart
{
    /// <summary>The marker: its existence says the background apps were started once for this install; its text, the version the apps without a startup task were last started for.</summary>
    public static string MarkerFile { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxKit", "store-first-start.done");

    /// <summary>
    /// Whether this copy starts background apps by itself at all: only the Store's copy, never in a test run (whose
    /// apps must not become the user's own).
    /// </summary>
    public static bool Applies(bool packaged, bool testRun) => packaged && !testRun;

    /// <summary>
    /// The verbs to start with <c>--hidden</c> as this process starts, never the app this process is itself
    /// (<paramref name="startedApp"/>, a verb). <paramref name="marker"/> is the marker's text, null when there is
    /// none: then every background app, of both kinds. Otherwise only the apps without a startup task, and only
    /// when the marker holds another version than <paramref name="version"/> (the first start after an update) or
    /// this process is a startup task's hidden start (<paramref name="startedHidden"/> of an app that runs from
    /// sign-in), and not one whose own setting says no (<paramref name="turnedOff"/>, asked only then).
    /// </summary>
    public static IReadOnlyList<string> ToStart(string startedApp, bool startedHidden, string? marker, string version, Func<App, bool> turnedOff)
    {
        var first = marker is null;
        var afterUpdate = marker is not null && marker != version;
        var atSignIn = startedHidden && Dispatch.Apps.Any(app => app.AtSignIn && app.Verb == startedApp);
        return [.. Dispatch.Apps
            .Where(app => app.Verb != startedApp)
            .Where(app => first ? app.AtSignIn || app.InBackground : (afterUpdate || atSignIn) && app.InBackground && !turnedOff(app))
            .Select(app => app.Verb)];
    }

    /// <summary>The marker's text; null when there is no marker, or it cannot be read (and then <see cref="Mark"/> fails too, or writes it).</summary>
    public static string? Marker()
    {
        try
        {
            return File.Exists(MarkerFile) ? File.ReadAllText(MarkerFile).Trim() : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Writes the marker with the version; false when it could not be written, and then nothing is started for a first start or an update, so that a folder that cannot be written never means several starts with every window.</summary>
    public static bool Mark(string version)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MarkerFile)!);
            File.WriteAllText(MarkerFile, version);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
