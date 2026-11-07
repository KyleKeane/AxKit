namespace AxKit.Core;

/// <summary>
/// Where the bundle keeps its files (docs/axkit/SPEC.md AX-5.1): one folder for the bundle, one file per app. Settings
/// in <c>%APPDATA%\AxKit\&lt;app&gt;.json</c>, logs in <c>%LOCALAPPDATA%\AxKit\logs</c>, downloaded updates in
/// <c>%LOCALAPPDATA%\AxKit\updates</c>. The files are named after the apps in lower case: <c>axclaude.json</c>, <c>axdown.log</c>.
/// </summary>
public static class AppPaths
{
    public static string SettingsFile(string app) =>
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", app + ".json");

    /// <summary>The bundle's one list of recently closed files and folders, for the axKit menu (AX-9.4).</summary>
    public static string RecentlyClosedFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "recently-closed.json");

    /// <summary>What the user has agreed to (AX-11.5): each access layer's first connection to a program, and each update's terms.</summary>
    public static string ConsentFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "consent.json");

    /// <summary>The AI tools' access (docs/axkit/mcp.md MC-6.1): on or off, and the clients the user paired.</summary>
    public static string McpFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "mcp.json");

    /// <summary>The key the broker proves itself with to the windows (MC-6.3), kept with DPAPI; never roamed, so under the local folder.</summary>
    public static string McpKeyFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "mcp.key");

    /// <summary>Every call of an AI tool, one line each (MC-6.6).</summary>
    public static string McpAuditFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "mcp-audit.log");

    public static string LogsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "logs");

    /// <summary>The user's reports as an axTask list (docs/axkit/reports.md RP-5.1): one item per report, sent or not.</summary>
    public static string ReportsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "reports");

    /// <summary>The reports' own settings (RP-6.3): the install's reporter id, and the blocklist last read.</summary>
    public static string ReportsFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "reports.json");

    /// <summary>The user's own GitHub token for axTask's GitHub lists (docs/axkit/reports.md RP-7.2), kept with DPAPI as the MCP key is; never roamed.</summary>
    public static string GitHubTokenFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "github.token");

    public static string UpdatesFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxKit", "updates");

    /// <summary>Where install.ps1 puts the bundle (AX-2.1), and the program file there, which an update starts again.</summary>
    public static string InstallFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "AxKit");

    public static string InstalledExe => Path.Combine(InstallFolder, "AxKit.exe");

    /// <summary>
    /// The first start after the rename (docs/axkit/SPEC.md AX-5.4, B-16): what Axit, the bundle's name up to 4.0.0,
    /// kept in <c>%APPDATA%\Axit</c> and <c>%LOCALAPPDATA%\Axit</c> moves into the AxKit folders, so that every app
    /// finds its settings, the recently closed list, the consents, the AI tools' pairings and their key, and the logs
    /// as they were. Never in a test run, whose folders are its own.
    /// </summary>
    public static void TakeOverOldFolders()
    {
        if (AutomationEnvironment.Home is not null)
        {
            return;
        }

        foreach (var folder in new[] { Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.LocalApplicationData })
        {
            var root = Environment.GetFolderPath(folder);
            TakeOverFolder(Path.Combine(root, "Axit"), Path.Combine(root, "AxKit"));
        }
    }

    /// <summary>
    /// Moves what <paramref name="oldFolder"/> holds into <paramref name="folder"/>: a file the new folder lacks is
    /// moved, a file it has stays, a subfolder is taken over the same way, and the old folder goes once it is empty;
    /// nothing when it does not exist. Entry by entry, so that a start that could not move everything (a file the old
    /// program still held) finishes the next time.
    /// </summary>
    public static void TakeOverFolder(string oldFolder, string folder)
    {
        if (!Directory.Exists(oldFolder))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
            foreach (var entry in Directory.GetFileSystemEntries(oldFolder))
            {
                var destination = Path.Combine(folder, Path.GetFileName(entry));
                if (Directory.Exists(entry))
                {
                    if (Directory.Exists(destination))
                    {
                        TakeOverFolder(entry, destination);
                    }
                    else
                    {
                        Directory.Move(entry, destination);
                    }
                }
                else if (!File.Exists(destination))
                {
                    File.Move(entry, destination);
                }
            }

            if (Directory.GetFileSystemEntries(oldFolder).Length == 0)
            {
                Directory.Delete(oldFolder);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"{oldFolder} could not be moved into {folder}; it is tried again at the next start", ex);
        }
    }

    /// <summary>
    /// The folder the settings, the recently closed list and the logs live under: the user's, or a test run's own
    /// (<see cref="AutomationEnvironment.Home"/>), so that its windows neither read nor write the user's files
    /// (docs/axkit/automation.md); a run's folder counts only with the run's secret (security audit S-11).
    /// </summary>
    private static string Home(Environment.SpecialFolder folder) =>
        AutomationEnvironment.Home ?? Environment.GetFolderPath(folder);
}
