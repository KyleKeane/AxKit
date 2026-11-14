namespace AxKit.Core;

/// <summary>
/// The Store's copy right after its install (docs/axKit/SPEC.md AX-13.4a; the user's report of 2026-11-14 from a
/// second computer: axStart's hotkey did nothing until Windows was restarted). The background apps (axStart, axClip,
/// axHelp: <see cref="App.AtSignIn"/>) are startup tasks of the package, which Windows runs at a sign-in and
/// only after an app of the package was started once. So the first start of any app of the Store's copy starts them
/// itself, hidden, once per install: a marker file says it was done, and since a packaged process's new files land in
/// the package's private folder, the marker goes with an uninstall and a new install starts them again. After that
/// Windows' Startup apps page alone decides (AX-13.4), so an app the user turned off there is never started behind
/// their back. The rules are pure; <c>Program.Main</c> starts the processes.
/// </summary>
public static class StoreFirstStart
{
    /// <summary>The marker: its existence alone says the background apps were started once for this install.</summary>
    public static string MarkerFile { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxKit", "store-first-start.done");

    /// <summary>
    /// Whether this start is the one that starts the background apps: only in the Store's copy, never in a test run
    /// (whose apps must not become the user's own), and only while the marker is missing.
    /// </summary>
    public static bool Due(bool packaged, bool testRun, bool markerExists) =>
        packaged && !testRun && !markerExists;

    /// <summary>
    /// The verbs to start with <c>--hidden</c>: every app that runs from sign-in but the one this process is itself
    /// (<paramref name="startedApp"/>, a verb), which is already running.
    /// </summary>
    public static IReadOnlyList<string> Verbs(string startedApp) =>
        Dispatch.Apps.Where(app => app.AtSignIn && app.Verb != startedApp).Select(app => app.Verb).ToList();

    /// <summary>Writes the marker; false when it could not be written, and then nothing is started, so that a folder that cannot be written never means three starts with every window.</summary>
    public static bool Mark()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MarkerFile)!);
            File.WriteAllText(MarkerFile, "");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
