using System.Runtime.InteropServices;
using AxCalc;
using AxClaude;
using AxClaude.Core.Hooks;
using AxClaude.Core.Streaming;
using AxClip;
using AxDown;
using AxFile;
using AxGit;
using AxJSON;
using AxKit.Core;
using AxMedia;
using AxPass;
using AxPreview;
using AxProcess;
using AxStart;
using AxTask;
using AxTerminal;
using AxMCP;
using AxHelp;
using AxAutomate;
using AxTemplate;

namespace axKit;

/// <summary>
/// The one entry point of the bundle (docs/axKit/SPEC.md AX-1). It decides which app the command line means and hands
/// the rest of the arguments to that app's <c>Run</c>; everything after, including the crash handling and the log,
/// belongs to the app. A message box is used only here, where no window exists yet.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // The hook a Claude Code session of axClaude's starts for an event (docs/axClaude/events-and-grounding.md
        // EG-2.6): no app and no window, decided before anything else runs, so that the process loads nothing of the
        // windows and ends within the time Claude Code gives a hook. Everything else is Start's.
        if (args.Length > 0 && args[0] == HookRelay.Verb)
        {
            HookRelay.Run();
            return;
        }

        // The permission tool of a Claude Code that axClaude runs over its stream (docs/axClaude/stream-mode.md
        // SM-7.3): no app and no window either, and decided here for the same reason. It passes each question to
        // the window that started that Claude Code and the answer back, until Claude Code ends it.
        if (args.Length > 0 && args[0] == AskRelay.Verb)
        {
            AskRelay.Run();
            return;
        }

        Start(args);
    }

    /// <summary>Every start but the hook's: the Open with links, a test run's variables, then the app the command line means.</summary>
    private static void Start(string[] args)
    {
        if (StartedAsOpenWithLink(args))
        {
            return;
        }

        // First of all: a test run's variables read and taken out of the environment, so that nothing this process
        // starts inherits them (security audit S-11).
        AutomationEnvironment.Capture();
        // A process of a test run takes the run's mark before any window exists: the windows of a run list, message
        // and close only each other, never the user's own (RunMembership). Nothing outside a run.
        RunMembership.Join();
        // And the current folder kept out of every program search by bare name, in this process and in the shells and
        // programs it starts (S-1).
        SystemPrograms.KeepCurrentFolderOffSearches();
        // The folders of Axit, the bundle's name up to 4.0.0, taken over before anything reads a setting (AX-5.4).
        AppPaths.TakeOverOldFolders();
        // What the user keeps, moved into Documents\axKit by the first start of a version that keeps it there (AX-5.11).
        AppPaths.TakeOverUserFolders();
        ApplicationConfiguration.Initialize();
        // AX-7.7: the windows are light or dark as Windows is; a high contrast theme takes over as it always has.
        Application.SetColorMode(SystemColorMode.System);
        DeleteOldProgramFiles();

        // The Store's copy (AX-13.2): started through one of the package's console commands (axDown.exe, whose path
        // is the command line's first word) or from one of its entries, that app is the verb when the command line
        // carries none.
        if (PackageIdentity.IsPackaged)
        {
            args = Dispatch.WithApplicationVerb(args, PackageIdentity.ApplicationId, PackageIdentity.CommandLineProgram);
        }
        var launch = Dispatch.Decide(args);
        if (launch.Error is not null)
        {
            MessageBox.Show(launch.Error + "\n\n" + Dispatch.Usage, "axKit", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (Runs.TryGetValue(launch.App, out var run))
        {
            StartBackgroundApps(launch.App, launch.Arguments);
            // AX-1.6: the taskbar's id is the app's project name, AxKit.AxClaude and so on.
            SetAppId("AxKit." + Dispatch.Of(launch.App).Project);
            run(launch.Arguments);
        }
        else if (launch.App == Dispatch.Mcp)
        {
            // No window: MCP on standard input and output until the client closes it (docs/axKit/mcp.md MC-1.1).
            AxKit.Forms.McpBroker.Run();
        }
        else if (launch.App == Dispatch.Listing)
        {
            // No window: the apps for the installer, which reads standard output (AX-1.4). A program without a
            // console writes to the handle it was given; started from a console with nothing to catch it, nothing shows.
            using var output = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false));
            output.WriteLine(Dispatch.ListingJson());
        }
        else
        {
            // --help or --version for the bundle itself (AX-1.4).
            var text = launch.Arguments[0] == "--version" ? $"axKit {BundleInfo.Version}" : $"axKit {BundleInfo.Version}\n\n{Dispatch.Usage}";
            MessageBox.Show(text, "axKit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Every app's entry point by its verb (AX-1.7), one line per app of <see cref="Dispatch.Apps"/>; a new app adds
    /// its line here (tools/new-app.ps1 does), and <c>InventoryTests</c> fails on an app without one.
    /// </summary>
    private static readonly Dictionary<string, Action<string[]>> Runs = new()
    {
        [Dispatch.AxClaude] = AxClaudeApp.Run,
        [Dispatch.AxDown] = AxDownApp.Run,
        [Dispatch.AxJSON] = AxJSONApp.Run,
        [Dispatch.AxGit] = AxGitApp.Run,
        [Dispatch.AxTerminal] = AxTerminalApp.Run,
        [Dispatch.AxFile] = AxFileApp.Run,
        [Dispatch.AxMedia] = AxMediaApp.Run,
        [Dispatch.AxPreview] = AxPreviewApp.Run,
        [Dispatch.AxStart] = AxStartApp.Run,
        [Dispatch.AxProcess] = AxProcessApp.Run,
        [Dispatch.AxTask] = AxTaskApp.Run,
        [Dispatch.AxPass] = AxPassApp.Run,
        [Dispatch.AxCalc] = AxCalcApp.Run,
        [Dispatch.AxClip] = AxClipApp.Run,
        [Dispatch.AxMCP] = AxMCPApp.Run,
        [Dispatch.AxHelp] = AxHelpApp.Run,
        [Dispatch.AxAutomate] = AxAutomateApp.Run,
        [Dispatch.AxTemplate] = AxTemplateApp.Run,
    };

    /// <summary>
    /// AX-2.3: Windows' Open with lists one program per program file, so the installer gives each app with file types a
    /// hard link of AxKit.exe under its own name (axDown.exe) for its ProgID. Started under such a name, the process
    /// starts AxKit.exe beside it with the same arguments and ends, so that every window runs as AxKit.exe: the hand-over
    /// to an open window, the window lists and the AI tools tell this program by its path. The new process may take the
    /// foreground, which this one was given by the shell.
    /// </summary>
    private static bool StartedAsOpenWithLink(string[] args)
    {
        if (Environment.ProcessPath is not { } path
            || string.Equals(Path.GetFileName(path), "AxKit.exe", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(Path.Combine(AppContext.BaseDirectory, "AxKit.exe")))
        {
            return false;
        }

        var start = new System.Diagnostics.ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AxKit.exe"))
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        // Outside call, program, axKit: starts AxKit.exe itself with the same arguments when this process was started
        // under an app's hard-linked name from Windows' Open with (axDown.exe), so that every window runs as AxKit.exe
        // (AX-2.3).
        using var started = System.Diagnostics.Process.Start(start);
        if (started is not null)
        {
            // S-50 (security review, 2026-10-25): the right to the foreground for that process alone, not for any
            // program of the user's that asks in the same moment (ASFW_ANY until then).
            AllowSetForegroundWindow(started.Id);
        }

        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    /// <summary>
    /// AX-13.4a, AX-13.4b: the Store's copy starts its background apps itself where Windows does not
    /// (<see cref="StoreFirstStart"/>, which holds the rules). The first start of any app after the install starts
    /// them all, hidden, since Windows runs the startup tasks only from the next sign-in. axTemplate's launcher has
    /// no startup task, so it is also started with the first start after an update, and whenever a startup task
    /// starts its app hidden, unless its own Start with Windows setting is off. A start that fails is left to the
    /// next of these.
    /// </summary>
    private static void StartBackgroundApps(string startedApp, IReadOnlyList<string> arguments)
    {
        if (!StoreFirstStart.Applies(PackageIdentity.IsPackaged, AutomationEnvironment.Requested) || Environment.ProcessPath is not { } path)
        {
            return;
        }

        // The marker is written before anything is started for a first start or an update; when it cannot be
        // written, neither is due, so that a folder that cannot be written never means several starts with every window.
        var marker = StoreFirstStart.Marker();
        if (marker != BundleInfo.Version && !StoreFirstStart.Mark(BundleInfo.Version))
        {
            marker = BundleInfo.Version;
        }

        var hidden = arguments.Contains("--hidden");
        var verbs = StoreFirstStart.ToStart(startedApp, hidden, marker, BundleInfo.Version, StartWithWindowsOff);
        if (verbs.Count == 0)
        {
            return;
        }

        if (hidden)
        {
            StartHidden(path, verbs);
            return;
        }

        // A window the user opened comes first (AX-13.4a; KyleKeane/AxKit-reports issue 15: axStart said nothing on its
        // first start): a background app started before it is in front can take the foreground as its hidden window is
        // made and give it back to the window in front when it started, which was not this one. Started once this
        // window is in front, each gives the foreground back to it (HotkeyForm's rule, AS-3.4).
        new Thread(() =>
        {
            for (var waited = 0; waited < 5000 && !OwnWindowInFront(); waited += 100)
            {
                Thread.Sleep(100);
            }

            StartHidden(path, verbs);
        }) { IsBackground = true, Name = "Background apps" }.Start();
    }

    /// <summary>Whether the window in front is this process's own.</summary>
    private static bool OwnWindowInFront()
    {
        AxKit.Forms.User32.GetWindowThreadProcessId(AxKit.Forms.User32.GetForegroundWindow(), out var owner);
        return owner == (uint)Environment.ProcessId;
    }



    private static void StartHidden(string path, IReadOnlyList<string> verbs)
    {
        foreach (var verb in verbs)
        {
            try
            {
                var start = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = false };
                start.ArgumentList.Add(verb);
                start.ArgumentList.Add("--hidden");
                // Outside call, program, axKit: starts AxKit.exe itself as a background app (axStart, axClip, axHelp,
                // axTemplate's launcher), hidden, at the first start of the Store's copy after its install, and the
                // launcher also after an update and with the sign-in's startup tasks (AX-13.4a, AX-13.4b).
                using var started = System.Diagnostics.Process.Start(start);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // Windows starts it at the next sign-in, or the next of these starts does.
            }
        }
    }

    /// <summary>Whether the app's own settings file says its Start with Windows is off; a file that is not there or cannot be read does not.</summary>
    private static bool StartWithWindowsOff(App app)
    {
        try
        {
            var file = AppPaths.SettingsFile(app.Name);
            return File.Exists(file) && SignInStart.TurnedOff(File.ReadAllText(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// AX-2.8: an update installed in place renames the program file that was running to AxKit.old.exe, since a
    /// running file cannot be overwritten or deleted, and an app's Open with link the same way (axDown.old.exe). The next
    /// start removes them; one still running waits for the next. Only in the installation folder and only the names the
    /// installer makes (S-51, security review 2026-10-25): AxKit.exe run from a folder of downloads would otherwise delete
    /// another program's setup.old.exe.
    /// </summary>
    private static void DeleteOldProgramFiles()
    {
        if (!string.Equals(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd('\\'), Path.GetFullPath(AppPaths.InstallFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var patterns = new[] { "AxKit.old*.exe" }.Concat(Dispatch.Apps.Where(app => app.OpenWith.Count > 0).Select(app => app.Name + ".old.*.exe"));
            foreach (var old in patterns.SelectMany(pattern => Directory.GetFiles(AppContext.BaseDirectory, pattern)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(old);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>AX-1.6: the taskbar groups windows by this id, so each app gets its own, set before any window exists.</summary>
    private static void SetAppId(string id)
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(id);
        }
        catch (Exception)
        {
            // Cosmetic: the apps still run, grouped under one taskbar button.
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
