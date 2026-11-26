namespace AxClaude.Core;

/// <summary>
/// Command line: <c>axKit claude [&lt;folder&gt;] [--project &lt;folder&gt;] [--claude &lt;path&gt;] [--cols N] [--rows N] [--no-ax]
/// [--record &lt;file.vt&gt;] [--message, --title, --model, --hide-on-close, --hidden, --ai-tools] [-- claude arguments]</c> (the console command <c>axclaude</c> stands for <c>axKit claude</c>;
/// a folder alone also reaches here, docs/axKit/SPEC.md AX-1). Values not given fall back to the settings file.
/// <see cref="ClaudeArgs"/> is null when no <c>--</c> was given (the window then uses <c>--continue</c>, FR-9.1) and
/// empty after a bare <c>--</c>.
/// </summary>
public sealed record StartupOptions(
    string? Folder,
    string? ClaudePath,
    bool NoAx,
    int? Columns,
    int? Rows,
    string? RecordPath,
    IReadOnlyList<string>? ClaudeArgs,
    string? Message = null,
    string? Title = null,
    string? Model = null,
    bool HideOnClose = false,
    bool Hidden = false,
    string? Source = null)
{
    /// <summary>
    /// Whether this start's window is fed by Claude Code's stream (docs/axClaude/stream-mode.md SM-9.1): by
    /// <c>--source</c> when it was given, else by the setting. The screen whenever neither names the stream.
    /// </summary>
    public bool Streams(AppSettings settings) => AppSettings.IsStream(Source ?? settings.ClaudeSource);

    /// <summary>The window starts hidden (FR-9.6): only together with <see cref="HideOnClose"/> and a folder, since a hidden window that asks for a folder, or that nothing can bring back, serves nobody.</summary>
    public bool StartsHidden => Hidden && HideOnClose && Folder is not null;

    /// <summary>
    /// The command line that starts this window again after Update and restart (FR-9.1): the verb alone without a
    /// folder; on <paramref name="folder"/> otherwise, with everything of this start that makes the window what
    /// it is, so that a window another program keeps (axHelp's conversation, FR-9.2, FR-9.6) comes back as that
    /// window: its title, its model, hiding on close (the AI tools need nothing since 2026-11-26: every window gives
    /// them while they are on, FR-9.7) and a source the command line named
    /// (SM-9.1); and on the folder's conversation when a message was sent in this window. Never
    /// <c>--hidden</c> or <c>--message</c>: the window comes back on view, with nothing put into its field.
    /// </summary>
    public IReadOnlyList<string> RestartArguments(string? folder, bool messageSent)
    {
        var arguments = new List<string> { "claude" };
        if (folder is null)
        {
            return arguments;
        }

        arguments.Add(folder);
        if (Title is { Length: > 0 })
        {
            arguments.Add("--title");
            arguments.Add(Title);
        }

        if (Model is { Length: > 0 })
        {
            arguments.Add("--model");
            arguments.Add(Model);
        }

        if (HideOnClose)
        {
            arguments.Add("--hide-on-close");
        }

        if (Source is not null)
        {
            arguments.Add("--source");
            arguments.Add(Source);
        }

        if (messageSent)
        {
            arguments.Add("--");
            arguments.Add("--continue");
        }

        return arguments;
    }

    public const string Usage = """
        Usage: axKit claude [<folder>] [--project <folder>] [--claude <path>] [--cols N] [--rows N] [--no-ax] [--record <file.vt>] [--message <text>] [--title <text>] [--model <name>] [--hide-on-close [--hidden]] [--ai-tools] [--source screen|stream] [-- claude arguments]
               axclaude ...  the same, as the installed console command

          <folder>            the project folder (default: the current folder, or the last one used)
          --claude <path>     claude.exe or claude.cmd to run (default: found on PATH or in the usual install folders)
          --cols N, --rows N  size of the hidden console Claude writes to (default 240 by 50)
          --record <file.vt>  record the raw console stream for a bug report
          --message <text>    put the text into the message field, not sent (axHelp's screenshot)
          --title <text>      the window's title in place of the folder's (axHelp's conversation, titled axHelp)
          --model <name>      Claude Code's model on every start of Claude in this window (axHelp's own model)
          --hide-on-close     Alt+F4 hides the window and Claude runs on; a start that hands it "show" or a message
                              brings it back as it was; Session, End Claude and close this window ends it (axHelp's conversation)
          --hidden            with --hide-on-close and a folder: start hidden, Claude loaded, until it is brought up
          --ai-tools          accepted and changes nothing: every window gives Claude axKit's AI tools while
                              the user has them on (until 2026-11-26, only a window started with it)
          --no-ax             start Claude without --ax-screen-reader (testing only)
          --source <name>     what feeds this window, screen or stream, in place of the setting (being built: the
                              stream is Claude Code as one process read line by line; screen is the default)
          -- ...              everything after -- goes to claude, for example -- --resume <id> or -- --permission-mode plan
                              (default: --continue, the last conversation in the folder; a bare -- starts a new one)
          --help, --version   this text, or the version
        """;

    public static StartupOptions Parse(string[] args)
    {
        string? folder = null;
        string? claude = null;
        string? record = null;
        string? message = null;
        string? title = null;
        string? model = null;
        var noAx = false;
        var hideOnClose = false;
        var hidden = false;
        string? source = null;
        int? columns = null;
        int? rows = null;
        List<string>? claudeArgs = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--":
                    claudeArgs = args.Skip(i + 1).ToList();
                    i = args.Length;
                    break;
                case "--claude":
                    claude = Next(args, ref i);
                    break;
                case "--project":
                    folder = Next(args, ref i);
                    break;
                case "--cols":
                    columns = ParseNumber(Next(args, ref i), 20, 1000, "--cols");
                    break;
                case "--rows":
                    rows = ParseNumber(Next(args, ref i), 5, 500, "--rows");
                    break;
                case "--record":
                    record = Path.GetFullPath(Next(args, ref i));
                    break;
                case "--message":
                    message = Next(args, ref i);
                    break;
                case "--title":
                    title = Next(args, ref i);
                    break;
                case "--model":
                    model = Next(args, ref i);
                    break;
                case "--no-ax":
                    noAx = true;
                    break;
                case "--hide-on-close":
                    hideOnClose = true;
                    break;
                case "--hidden":
                    hidden = true;
                    break;
                case "--ai-tools":
                    // Accepted and changes nothing since 2026-11-26 (FR-9.7): every window gives the AI tools.
                    break;
                case "--source":
                    source = Next(args, ref i);
                    if (source is not (AppSettings.SourceScreen or AppSettings.SourceStream))
                    {
                        throw new ArgumentException($"--source must be {AppSettings.SourceScreen} or {AppSettings.SourceStream}.");
                    }

                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown option {arg}.\n{Usage}");
                    }

                    if (folder is not null)
                    {
                        throw new ArgumentException($"Only one folder can be given.\n{Usage}");
                    }

                    folder = arg;
                    break;
            }
        }

        if (folder is null)
        {
            // Started from a console inside a project: use that folder. Started from Explorer or a shortcut
            // the current directory is the app's own folder, started from a Start menu entry of the Store's package
            // it is Windows' System32 folder (measured 2026-11-09); neither is ever a project.
            var current = Path.GetFullPath(Environment.CurrentDirectory);
            if (!IsNeverAProject(current))
            {
                folder = current;
            }
        }
        else
        {
            folder = Path.GetFullPath(folder);
            if (!Directory.Exists(folder))
            {
                throw new ArgumentException($"The folder does not exist: {folder}");
            }
        }

        return new StartupOptions(folder, claude, noAx, columns, rows, record, claudeArgs, message, title, model, hideOnClose, hidden, source);
    }

    private static string Next(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"{args[i]} needs a value.\n{Usage}");
        }

        return args[++i];
    }

    private static int ParseNumber(string text, int min, int max, string option)
    {
        if (!int.TryParse(text, out var value) || value < min || value > max)
        {
            throw new ArgumentException($"{option} must be a number between {min} and {max}.");
        }

        return value;
    }

    private static string TrimSeparator(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>
    /// A folder that is never a project, whatever started the app there (docs/axClaude/SPEC.md FR-8.1): the app's own
    /// folder (Explorer and the zip's shortcuts start it there) and Windows' own, the Windows folder and System32 (a
    /// Start menu entry of the Store's package starts it there). Neither is taken from the current directory nor
    /// kept as the last project folder.
    /// </summary>
    public static bool IsNeverAProject(string folder)
    {
        var path = TrimSeparator(Path.GetFullPath(folder));
        foreach (var never in new[] { AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows), Environment.SystemDirectory })
        {
            if (!string.IsNullOrEmpty(never) && string.Equals(path, TrimSeparator(Path.GetFullPath(never)), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
