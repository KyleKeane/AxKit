namespace AxKit.Core;

/// <summary>
/// Where the bundle keeps its files (docs/axKit/SPEC.md AX-5.1): one folder for the bundle, one file per app. Settings
/// in <c>%APPDATA%\AxKit\&lt;app&gt;.json</c>, logs in <c>%LOCALAPPDATA%\AxKit\logs</c>, downloaded updates in
/// <c>%LOCALAPPDATA%\AxKit\updates</c>. The files are named after the apps in lower case: <c>axClaude.json</c>, <c>axDown.log</c>.
/// </summary>
public static class AppPaths
{
    public static string SettingsFile(string app) =>
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", app + ".json");

    /// <summary>The folder every settings file is in.</summary>
    public static string SettingsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit");

    /// <summary>
    /// The folder of what the user writes with axKit and would want to keep, find, back up or sync: <c>axKit</c> in
    /// the user's Documents (docs/axKit/SPEC.md AX-5.11; the maintainer's decision of 2026-11-23). Settings, logs and
    /// what axKit keeps for itself stay under AppData; nothing here is needed for axKit to run, so deleting any of
    /// it breaks nothing. And AppData is the package's private folder in the Store's copy, which Windows removes
    /// with an uninstall: Documents is not. In a test run it is a Documents folder of the run's own home.
    /// </summary>
    public static string UserFolder { get; } =
        AutomationEnvironment.Home is { } home
            ? Path.Combine(home, "Documents", "axKit")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "axKit");

    /// <summary>The folder of the bundle's templates (docs/axTemplate/SPEC.md TP-2.1): a list of the item format, every item one template; <c>Templates</c> in <see cref="UserFolder"/>.</summary>
    public static string TemplatesRoot => Path.Combine(UserFolder, "Templates");

    /// <summary>
    /// axTask's top folder of lists when the user set none (docs/axKit/SPEC.md AX-5.11): <c>Tasks</c> in
    /// <see cref="UserFolder"/>; for someone whose lists are in <c>Documents\Tasks</c> already, the default until
    /// 2026-11-23, and who has none in the new place, that folder still (an update moves it, <see cref="TakeOverUserFolders"/>;
    /// one that could not be moved stays in use). Asked of the disk at each use, so that it is asked after that move.
    /// </summary>
    public static string TasksFolder =>
        TasksFolderFor(UserFolder, Path.Combine(Path.GetDirectoryName(UserFolder)!, "Tasks"), Directory.Exists);

    /// <summary>
    /// The first start of a version that keeps the user's content in <see cref="UserFolder"/> (AX-5.11; the
    /// maintainer's choice of 2026-11-23, "Move both on update"): what the earlier defaults held moves in, by
    /// <see cref="TakeOverFolder"/>, entry by entry, so that a start that could not move everything (OneDrive or an
    /// older axTask holding a file) finishes the next time, and the folder not yet moved stays in use meanwhile.
    /// <c>Documents\Tasks</c> into <c>Tasks</c>, only when it holds axTask's lists (<see cref="IsTaskFolder"/>: the
    /// name is a common one) and axTask's settings do not name it as a folder the user chose; <c>Documents\axKit
    /// Claude</c> into <c>Claude</c>, unless AI help's settings name it. Claude Code finds its conversations by the
    /// folder's path, so axHelp starts a new conversation after the move; the folder's own files, its CLAUDE.md and
    /// its permissions, move with it. Never in a test run, whose folders are its own.
    /// </summary>
    public static void TakeOverUserFolders()
    {
        if (AutomationEnvironment.Home is not null)
        {
            return;
        }

        var documents = Path.GetDirectoryName(UserFolder)!;
        var tasks = Path.Combine(documents, "Tasks");
        if (IsTaskFolder(tasks) && !NamesFolder(ReadOrNull(SettingsFile("axTask")), "folder", tasks))
        {
            TakeOverFolder(tasks, Path.Combine(UserFolder, "Tasks"));
        }

        var claude = Path.Combine(documents, "axKit Claude");
        if (Directory.Exists(claude) && !NamesFolder(ReadOrNull(AiHelpFile), "workingFolder", claude))
        {
            TakeOverFolder(claude, Path.Combine(UserFolder, "Claude"));
        }
    }

    /// <summary>Whether a folder holds axTask's lists: a <c>list.json</c> in it or in a folder of it, or a smart list (<c>.filter.json</c>).</summary>
    public static bool IsTaskFolder(string folder)
    {
        try
        {
            return Directory.Exists(folder)
                && (File.Exists(Path.Combine(folder, "list.json"))
                    || Directory.EnumerateFiles(folder, "*.filter.json").Any()
                    || Directory.EnumerateDirectories(folder).Any(list => File.Exists(Path.Combine(list, "list.json"))));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Whether a settings file's text names <paramref name="folder"/> in its property <paramref name="name"/> (case and a last separator not minded); false for no text and a text that does not read.</summary>
    public static bool NamesFolder(string? settingsText, string name, string folder)
    {
        if (string.IsNullOrWhiteSpace(settingsText))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(settingsText, new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return false;
            }

            foreach (var member in document.RootElement.EnumerateObject())
            {
                if (string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)
                    && member.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    && member.Value.GetString() is { Length: > 0 } named)
                {
                    return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(named)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string? ReadOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The rule of <see cref="TasksFolder"/>, pure: the new place, unless only the earlier one is there.</summary>
    public static string TasksFolderFor(string userFolder, string earlier, Func<string, bool> exists)
    {
        var now = Path.Combine(userFolder, "Tasks");
        return !exists(now) && exists(earlier) ? earlier : now;
    }

    /// <summary>The key of the settings' seal and the names of the sealed files (AX-5.6), kept with DPAPI; beside the AI tools' key, never roamed.</summary>
    public static string SettingsSealFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "settings-seal.key");

    /// <summary>The bundle's one list of recently closed files and folders, for the Window menu (AX-9.4).</summary>
    public static string RecentlyClosedFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "recently-closed.json");

    /// <summary>What the user has agreed to (AX-11.5): each access layer's first connection to a program, and each update's terms.</summary>
    public static string ConsentFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "consent.json");

    /// <summary>The user's own sounds (AX-7.12, 2026-11-24): a WAV file named as a sound plays in its place (<c>Audio/CustomSounds</c>).</summary>
    public static string SoundsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "sounds");

    /// <summary>The settings of the whole bundle (AX-3.1, 2026-11-24): the update check at start, one switch for every app.</summary>
    public static string BundleSettingsFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "bundle.json");

    /// <summary>The user's own app keys (AX-8.18 rule 6, 2026-11-25): an app's name and its key, or none, over the defaults (<see cref="AppLaunchKeys"/>); one file for the bundle.</summary>
    public static string AppKeysFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "app-keys.json");

    /// <summary>The theme (docs/axKit/theme.md TH-1.1): a colour or "system" for every class of object the windows draw; one file for the bundle.</summary>
    public static string ThemeFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "theme.json");

    /// <summary>AI help (docs/axKit/ai-help.md AH-1.1): the switch, the folders Claude may read, the model; one file for the bundle.</summary>
    public static string AiHelpFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "ai-help.json");

    /// <summary>The AI tools' access (docs/axKit/mcp.md MC-6.1): on or off, and the clients the user paired.</summary>
    public static string McpFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "mcp.json");

    /// <summary>The key the broker proves itself with to the windows (MC-6.3), kept with DPAPI; never roamed, so under the local folder.</summary>
    public static string McpKeyFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "mcp.key");

    /// <summary>Every call of an AI tool, one line each (MC-6.6).</summary>
    public static string McpAuditFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "mcp-audit.log");

    /// <summary>
    /// This AxKit.exe as an MCP server for Claude Code's <c>--mcp-config</c> (MC-6.9): written before a conversation
    /// that loads the AI tools by itself starts (axHelp's); it names a program file of this computer, so it never roams.
    /// </summary>
    public static string McpClaudeConfigFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "claude-mcp.json");

    /// <summary>
    /// The hooks axClaude registers for each start of Claude Code with <c>--settings</c>
    /// (docs/axClaude/events-and-grounding.md EG-2.2, section 6.1): one command hook for each event, this program file
    /// with the verb <c>hook</c>. Beside <see cref="McpClaudeConfigFile"/> and for the same reason: it names a program
    /// file of this computer, so it never roams; a test run's own folder in a run.
    /// </summary>
    public static string ClaudeHooksFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "claude-hooks.json");

    /// <summary>
    /// This program file as the one MCP server Claude Code asks its questions of when axClaude runs it over its
    /// stream (docs/axClaude/stream-mode.md SM-7.3): the program file with the verb <c>ask</c>, for Claude Code's
    /// <c>--mcp-config</c>. Beside <see cref="ClaudeHooksFile"/> and for the same reason; it holds nothing of a
    /// window's pipe or its secret, which travel in the environment alone.
    /// </summary>
    public static string ClaudeAskFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "claude-ask.json");

    /// <summary>
    /// The settings file a question of AI help hands Claude Code with <c>--settings</c> (docs/axKit/ai-help.md
    /// AH-2.6): it turns the hooks off for that run. Written again before each question when it does not hold
    /// axKit's text; beside <see cref="McpClaudeConfigFile"/>.
    /// </summary>
    public static string ClaudeNoHooksFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "claude-no-hooks.json");

    /// <summary>The files in Claude's working folder the user accepted (AH-7.8), each with its hash, under the settings' seal.</summary>
    public static string AiFolderFilesFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "ai-folder-files.json");

    public static string LogsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "logs");

    /// <summary>The log of events (docs/axKit/events.md section 9 point 5): times and kinds alone, written only while the bundle's switch is on (<see cref="Events.EventLogger"/>).</summary>
    public static string EventLogFile { get; } = Path.Combine(LogsFolder, "events.log");

    /// <summary>The user's reports as an axTask list (docs/axKit/reports.md RP-5.1): one item per report, sent or not.</summary>
    public static string ReportsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "reports");

    /// <summary>The reports' own settings (RP-6.3): the install's reporter id, and the blocklist last read.</summary>
    public static string ReportsFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "reports.json");

    /// <summary>The names of the report files axKit's report form wrote (RP-6.9), under the settings' seal: the sender sends no other.</summary>
    public static string ReportsWrittenFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "AxKit", "reports-written.json");

    public static string UpdatesFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "AxKit", "updates");

    /// <summary>Where install.ps1 puts the bundle (AX-2.1), and the program file there, which an update starts again.</summary>
    public static string InstallFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "AxKit");

    public static string InstalledExe => Path.Combine(InstallFolder, "AxKit.exe");

    /// <summary>
    /// The name of the mutex that keeps one process of a background app (axStart AS-1.2; axClip, axHelp)
    /// per home: <c>Local\AxKit.AxStart</c> for the user's, and with a mark of the run's folder for a test run
    /// (<see cref="AutomationEnvironment.Home"/>), so that the run's app and the user's own, which keeps running from
    /// sign-in, never take each other for a second start (2026-11-09: the user's axDescribe, now axHelp, made every test start end
    /// at once).
    /// </summary>
    public static string OneAtATime(string app) => RunMark is { } mark ? @"Local\AxKit." + app + "." + mark : @"Local\AxKit." + app;

    /// <summary>
    /// A mark of the test run this process belongs to, twelve hex digits from its folder
    /// (<see cref="AutomationEnvironment.Home"/>); null for the user's own processes. It keeps a run's mutexes and AI
    /// tools' pipes apart from the user's (<see cref="OneAtATime"/>, <see cref="Mcp.McpPipeName"/>).
    /// </summary>
    public static string? RunMark => AutomationEnvironment.Home is { } home ? MarkOf(home) : null;

    /// <summary>The mark of a run's folder, the case not minded.</summary>
    public static string MarkOf(string home) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(home.ToUpperInvariant())), 0, 6);

    /// <summary>
    /// The first start after the rename (docs/axKit/SPEC.md AX-5.4, B-16): what Axit, the bundle's name up to 4.0.0,
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
    /// (docs/axKit/automation.md); a run's folder counts only with the run's secret (security audit S-11).
    /// </summary>
    private static string Home(Environment.SpecialFolder folder) =>
        AutomationEnvironment.Home ?? Environment.GetFolderPath(folder);
}
