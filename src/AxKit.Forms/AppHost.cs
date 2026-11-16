using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Starts an app's window (docs/axKit/SPEC.md AX-1.7): the log, the crash handling, the start line in the log,
/// <c>--help</c> and <c>--version</c>, then the window the app makes from its arguments, and the exit line. Every
/// app's <c>Run</c> is one call into <see cref="Run"/>; what differs, the usage text and how the arguments become a
/// window, comes as parameters. The message boxes here are the only ones allowed in the bundle: no window exists
/// yet when they show (AX-7.2).
/// </summary>
public static class AppHost
{
    /// <param name="window">Makes the window from the arguments once <c>--help</c> and <c>--version</c> are answered; null when the app reported a bad command line itself (<see cref="BadCommandLine"/>).</param>
    public static void Run(string name, string logName, string usage, string[] args, Func<string[], Form?> window)
    {
        Log.UseApp(logName);

        // An exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(name, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        // The program path tells a development build from the installed one when both write to the same log.
        // The --message text is logged as its length alone (S-54): it may be a stranger's words from GitHub, and the log's tail goes into reports.
        Log.Info($"{name} {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {CommandLineText.ForLog(args)}");

        if (args.Length == 1 && Dispatch.IsHelpFlag(args[0]))
        {
            MessageBox.Show(usage, name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"{name}, axKit {BundleInfo.Version}", name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (window(args) is not { } form)
        {
            return;
        }

        Application.Run(form);
        Log.Info($"{name} exiting");
    }

    /// <summary>The mutex of a background app, held for the life of the process, so that a second start finds the first.</summary>
    private static Mutex? _one;

    /// <summary>
    /// Whether this process is the one process of a background app (axStart, axClip, axHelp; from their three copies,
    /// docs/axKit/repairs.json R-42): the app's mutex (<see cref="AppPaths.OneAtATime"/>, a test run's apart from the
    /// user's) taken and kept when free. False when another process holds it: the caller hands over to that one's
    /// window and ends.
    /// </summary>
    public static bool FirstInstance(string app)
    {
        _one = new Mutex(initiallyOwned: true, AppPaths.OneAtATime(app), out var first);
        if (!first)
        {
            _one.Dispose();
            _one = null;
        }

        return first;
    }

    /// <summary>
    /// The start of a background app that runs one copy (axStart, axClip, axHelp; docs/axKit/SPEC.md AX-1.9;
    /// repairs.md R-65). True when this process is that copy and goes on to make its window. Otherwise the first
    /// copy's window is looked for by <paramref name="title"/> (for three seconds, since a copy that is still
    /// starting has none yet) and <paramref name="handOver"/> gives it this start's wish and says whether it took
    /// it; this process then ends (false), unless the first copy did not answer and the user, asked in a message box
    /// whose default is No, has it ended: then every process of this very program file started with
    /// <paramref name="verb"/> is ended, and this process is the copy (true). The decision and the words are
    /// <see cref="BackgroundStart"/>'s. With --hidden and in a test run nothing is asked.
    /// </summary>
    /// <param name="handOver">Given the first copy's window: hands over what this start wants (or only asks whether it answers) and returns whether it answered.</param>
    public static bool OneCopy(string name, string app, string verb, string title, bool hidden, Func<nint, bool> handOver)
    {
        if (FirstInstance(app))
        {
            return true;
        }

        nint running = 0;
        for (var tries = 0; tries < 30 && (running = HotkeyForm.FindRunning(title)) == 0; tries++)
        {
            Thread.Sleep(100);
        }

        var answered = running != 0 && handOver(running);
        if (BackgroundStart.Decide(running != 0, answered, hidden, AutomationEnvironment.Home is not null) == SecondStart.End)
        {
            if (!answered)
            {
                Log.Info($"{name} is running and did not answer; this start ends without asking");
            }

            return false;
        }

        Log.Info($"{name} is running and did not answer; asking whether to end it");
        if (MessageBox.Show(BackgroundStart.Question(name), name, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return false;
        }

        var ended = EndCopies(verb);
        Log.Info($"{name}: ended {ended} process(es) that did not answer");
        // The mutex goes with the last process that held it; a moment may pass.
        for (var tries = 0; tries < 30; tries++)
        {
            if (FirstInstance(app))
            {
                return true;
            }

            Thread.Sleep(100);
        }

        MessageBox.Show(BackgroundStart.NotEnded(name), name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    /// <summary>
    /// Ends the other processes of this very program file whose command line names <paramref name="verb"/>, in this
    /// Windows session, and returns how many: never a process by its name alone, never another app of the bundle,
    /// never another program (AX-1.9).
    /// </summary>
    private static int EndCopies(string verb)
    {
        var ended = 0;
        // The processes of this very program file come from the one place that finds other processes (BundleWindows).
        foreach (var id in BundleWindows.OtherProcesses())
        {
            if (!BackgroundStart.IsCopyOf(ThisProgram.CommandLineOf((uint)id), verb))
            {
                continue;
            }

            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(id);
                // The image path again, on the process object that is ended: an id can be given to another process meanwhile.
                if (!string.Equals(process.MainModule?.FileName, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                process.Kill();
                process.WaitForExit(3000);
                ended++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
                // Gone already, or not ours to end: the mutex tells whether the start can go on.
                Log.Info("A process that did not answer could not be ended: " + ex.Message);
            }
        }

        return ended;
    }

    /// <summary>
    /// The one path an app takes on its command line (a file, a folder): the full path, or null for none. False,
    /// with the command line reported, when there is more than one argument or an option the app does not know.
    /// </summary>
    public static bool OnePath(string name, string what, string usage, string[] args, out string? path)
    {
        path = null;
        if (args.Length > 1 || (args.Length == 1 && args[0].StartsWith("--", StringComparison.Ordinal)))
        {
            BadCommandLine(name, args.Length > 1 ? $"{name} opens one {what}." : $"Unknown option {args[0]}.", usage);
            return false;
        }

        path = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
        return true;
    }

    /// <summary>A command line the app cannot start on: logged and shown, with the usage text when there is one.</summary>
    public static void BadCommandLine(string name, string message, string? usage)
    {
        Log.Error("Bad command line: " + message);
        MessageBox.Show(usage is null ? message : message + "\n\n" + usage, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>The report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(string name, Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"{name} hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<AxKitForm>().FirstOrDefault();
        if (window is { IsDisposed: false, IsHandleCreated: true })
        {
            try
            {
                window.ShowError("Unexpected error", message);
                return;
            }
            catch (Exception ex)
            {
                Log.Error("The error could not be shown in the window", ex);
            }
        }

        MessageBox.Show(message, $"{name} error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
