using System.Text.Json;

namespace AxKit.Core;

/// <summary>What a command line starts: an app's verb (<see cref="Dispatch.AxClaude"/> and the rest, or
/// <see cref="Dispatch.Bundle"/>), the arguments that app gets, and an error text when the command line names a path
/// that does not exist.</summary>
public sealed record Launch(string App, string[] Arguments, string? Error = null);

/// <summary>
/// An app of the bundle: <paramref name="Name"/> is its name as shown and spoken ("axClaude", "axGIT", lower-case "ax"
/// so that a screen reader says it as a word; 2026-10-23), which ends every title of its windows ("notes.md - axDown")
/// and names its Start menu entry; <paramref name="Verb"/> starts it (AX-1.1); <paramref name="Project"/> is the name
/// of its code (the folder src/AxClaude, the class AxClaudeApp, the icon AxClaude.ico), which keeps the C# spelling.
/// <paramref name="Group"/> is its place among the apps (AX-9.2: the axKit menu's lists keep the groups together with a
/// separator between them); <paramref name="Opens"/> what it opens from a path; <paramref name="ShowIn"/> whether every
/// window's Show current folder in submenu offers it (AX-9.10). Every list of apps in the windows reads these, so a new
/// app is in the axKit menu, Open in axKit, the Show in submenus and Recently closed by its line in <see cref="Dispatch.Apps"/>.
/// The properties below are for the scripts (docs/axkit/app-kit.md AK-4.1): the installer reads them through
/// <c>AxKit.exe apps</c> (<see cref="Dispatch.Listing"/>), tools/make-icon.ps1 and run.ps1 off this file.
/// </summary>
public sealed record App(string Name, string Verb, string Project, AppGroup Group, Opens Opens, bool ShowIn = false)
{
    /// <summary>The Start menu entry's description (AX-2.2): the name, a comma, what the app is.</summary>
    public string Description { get; init; } = "";

    /// <summary>The icon's letters and colour, which tools/make-icon.ps1 draws into src/AxKit/&lt;Project&gt;.ico.</summary>
    public AppIcon Icon { get; init; } = new("Ax", 31, 78, 121);

    /// <summary>What File Explorer's Open with axKit submenu offers the app on (AX-2.3), when it is not <see cref="Opens"/>.</summary>
    public Opens? Explorer { get; init; }

    /// <summary>The file types the submenu's file entry is shown on; every file when empty.</summary>
    public IReadOnlyList<string> Types { get; init; } = [];

    /// <summary>The file types the app is a program for under Windows' Open with (AX-2.3, B-9), with the type name Windows shows for them; <c>*</c> is every file.</summary>
    public IReadOnlyList<string> OpenWith { get; init; } = [];

    public string TypeName { get; init; } = "";

    /// <summary>
    /// Whether the app runs in the background from sign-in (axStart, axClip; docs/axstart/SPEC.md AS-1.3): the
    /// installer's value under the Run key, and the package's startup task (tools/make-manifest.ps1).
    /// </summary>
    public bool AtSignIn { get; init; }

    /// <summary>
    /// Whether the Store's package has a Start menu entry for the app (docs/axkit/SPEC.md AX-13.2): the Store allows
    /// eight per package and no hidden one (measured 2026-11-09), so these are the maintainer's eight; every other app
    /// is reached by its console command, by a file of its kind, by the axKit menu's New window and from axFile.
    /// </summary>
    public bool StoreEntry { get; init; }

    /// <summary>
    /// The program an access layer works through ("Claude Code"), empty for a standalone app (AX-11.1): an app that
    /// starts or talks to a program axKit does not include is an access layer, in <see cref="AppGroup.AccessLayers"/>.
    /// </summary>
    public string AccessLayerFor { get; init; } = "";

    public bool IsAccessLayer => AccessLayerFor.Length > 0;

    /// <summary>
    /// The app's name wherever the user meets it (AX-11.2): the name alone for a standalone app, "axClaude, access layer
    /// for Claude Code" for an access layer. Window titles, menus, About and the installer's entries use this, never
    /// <see cref="Name"/> alone.
    /// </summary>
    public string Title => IsAccessLayer ? $"{Name}, {Dispatch.AccessLayer} for {AccessLayerFor}" : Name;
}

/// <summary>An app's icon: two letters in white on a rounded square of the colour.</summary>
public sealed record AppIcon(string Letters, int Red, int Green, int Blue);

/// <summary>The groups of the apps, in the axKit menu's order (AX-9.2): the standalone apps' groups, then the access layers (AX-11.3).</summary>
public enum AppGroup
{
    /// <summary>Files and what is in them: the file manager, the editors, the viewer, the player.</summary>
    Files,

    /// <summary>Lists of one's own: tasks.</summary>
    Organise,

    /// <summary>Small tools for the whole computer: the launcher, the clipboard, the calculator, the processes.</summary>
    Utilities,

    /// <summary>The apps that work through a program axKit does not include (AX-11.1): Claude Code, Git, the shells, KeePassXC. Always last.</summary>
    AccessLayers,
}

/// <summary>What an app opens from a path: a file's app given a folder is refused, a folder's app given a file gets the file's folder.</summary>
public enum Opens
{
    Nothing,
    Folders,
    Files,
    FilesAndFolders,
}

/// <summary>
/// Decides which app <c>AxKit.exe</c> starts (docs/axkit/SPEC.md AX-1). The first argument may be a verb naming one of
/// <see cref="Apps"/>; without one, a folder goes to AxClaude and a file to AxDown, AxJSON or AxMedia by its kind; an
/// option (<c>--project</c>, a bare <c>--</c>) or no argument at all means AxClaude (B-8). <c>--help</c> and
/// <c>--version</c> alone are answered by the bundle itself. Pure code: it only looks at the arguments and asks the
/// file system whether a path is a folder or a file.
/// </summary>
public static class Dispatch
{
    public const string AxClaude = "claude";
    public const string AxDown = "down";
    public const string AxJSON = "json";
    public const string AxGit = "git";
    public const string AxTerminal = "terminal";
    public const string AxFile = "file";
    public const string AxMedia = "media";
    public const string AxPreview = "preview";
    public const string AxStart = "start";
    public const string AxProcess = "process";
    public const string AxTask = "task";
    public const string AxPass = "pass";
    public const string AxCalc = "calc";
    public const string AxClip = "clip";

    /// <summary>Not an app: the executable answers <c>--help</c> and <c>--version</c>, or reports <see cref="Launch.Error"/>.</summary>
    public const string Bundle = "axkit";

    /// <summary>Not an app: the AI tools' broker, started by an MCP client and speaking to it on standard input and output (docs/axkit/mcp.md MC-1.1).</summary>
    public const string Mcp = "mcp";

    /// <summary>Not an app: the apps of <see cref="Apps"/> as JSON on standard output, for install.ps1 (AX-1.4, AX-2.3).</summary>
    public const string Listing = "apps";

    /// <summary>The keyword of every access layer's title (AX-11.2): "axClaude, access layer for Claude Code".</summary>
    public const string AccessLayer = "access layer";

    /// <summary>
    /// The apps, in the order every list of the windows shows them, by group (AX-9.2, 2026-10-24), the standalone apps
    /// first and the access layers last (AX-11.3, a test keeps it so): one line here puts an
    /// app in the axKit menu, axFile's Open in axKit, the Show in submenus and Recently closed, and with its properties in
    /// the installer's Start menu, File Explorer and Open with entries and the icon script. tools/new-app.ps1 writes a
    /// new app's line; each entry starts with <c>new("name", Verb, "Project", ...</c> on a line of its own, which the
    /// scripts read (tools/apps.ps1).
    /// </summary>
    public static readonly IReadOnlyList<App> Apps =
    [
        new("axFile", AxFile, "AxFile", AppGroup.Files, Opens.FilesAndFolders, ShowIn: true)
        {
            Description = "axFile, a file manager for one folder", Icon = new("Af", 90, 50, 130), StoreEntry = true,
        },
        new("axDown", AxDown, "AxDown", AppGroup.Files, Opens.Files)
        {
            Description = "axDown, a plain editor for Markdown and text files", Icon = new("Ad", 31, 110, 70), StoreEntry = true,
            OpenWith = [".md", ".markdown", ".txt"], TypeName = "Text document (axDown)",
        },
        new("axJSON", AxJSON, "AxJSON", AppGroup.Files, Opens.Files)
        {
            Description = "axJSON, an editor for JSON files", Icon = new("Aj", 120, 70, 20),
            Types = [".json"], OpenWith = [".json"], TypeName = "JSON document (axJSON)",
        },
        new("axPreview", AxPreview, "AxPreview", AppGroup.Files, Opens.Files)
        {
            Description = "axPreview, a read-only viewer for text files", Icon = new("Ap", 70, 70, 140),
            OpenWith = ["*"], TypeName = "File (axPreview)",
        },
        new("axMedia", AxMedia, "AxMedia", AppGroup.Files, Opens.FilesAndFolders)
        {
            Description = "axMedia, a player for sound and video files", Icon = new("Am", 20, 100, 110), Explorer = Opens.Files,
            Types = [.. MediaFiles.SoundExtensions, .. MediaFiles.VideoExtensions], OpenWith = [.. MediaFiles.SoundExtensions, .. MediaFiles.VideoExtensions],
            TypeName = "Media file (axMedia)",
        },
        new("axTask", AxTask, "AxTask", AppGroup.Organise, Opens.Folders)
        {
            Description = "axTask, tasks and lists", Icon = new("Ak", 30, 120, 80), StoreEntry = true,
        },
        new("axStart", AxStart, "AxStart", AppGroup.Utilities, Opens.Nothing)
        {
            Description = "axStart, a program launcher behind a hotkey", Icon = new("As", 150, 90, 20), AtSignIn = true, StoreEntry = true,
        },
        new("axClip", AxClip, "AxClip", AppGroup.Utilities, Opens.Nothing)
        {
            Description = "axClip, a clipboard history behind a hotkey", Icon = new("Ab", 160, 50, 50), AtSignIn = true, StoreEntry = true,
        },
        new("axCalc", AxCalc, "AxCalc", AppGroup.Utilities, Opens.Nothing)
        {
            Description = "axCalc, a calculator with a history", Icon = new("Ac", 90, 100, 30),
        },
        new("axProcess", AxProcess, "AxProcess", AppGroup.Utilities, Opens.Nothing)
        {
            Description = "axProcess, the processes, services and startup programs", Icon = new("Ar", 120, 40, 90),
        },
        new("axClaude", AxClaude, "AxClaude", AppGroup.AccessLayers, Opens.Folders, ShowIn: true)
        {
            AccessLayerFor = "Claude Code", Description = "axClaude, access layer for Claude Code: its conversation as plain text", Icon = new("Ax", 31, 78, 121), StoreEntry = true,
        },
        new("axTerminal", AxTerminal, "AxTerminal", AppGroup.AccessLayers, Opens.Folders, ShowIn: true)
        {
            AccessLayerFor = "your shell", Description = "axTerminal, access layer for your shell: PowerShell, Command Prompt or Git Bash as a transcript", Icon = new("At", 40, 40, 40), StoreEntry = true,
        },
        new("axGIT", AxGit, "AxGit", AppGroup.AccessLayers, Opens.FilesAndFolders, ShowIn: true)
        {
            AccessLayerFor = "Git", Description = "axGIT, access layer for Git: one repository's changes, log and branches", Icon = new("Ag", 110, 40, 40), Explorer = Opens.Folders, StoreEntry = true,
        },
        new("axPass", AxPass, "AxPass", AppGroup.AccessLayers, Opens.Files)
        {
            AccessLayerFor = "KeePassXC", Description = "axPass, access layer for KeePassXC: a KeePass password database", Icon = new("Aw", 20, 40, 100),
            Types = [".kdbx"], OpenWith = [".kdbx"], TypeName = "KeePass database (axPass)",
        },
    ];

    /// <summary>
    /// What <c>AxKit.exe apps</c> prints (AX-1.4): one JSON object per app of <see cref="Apps"/>, in its order, with the
    /// installer's words for File Explorer's submenu ("folders", "files"). install.ps1 builds its Start menu entries,
    /// commands, submenu entries and Open with registrations from it, so the installer keeps no list of its own.
    /// </summary>
    public static string ListingJson() => JsonSerializer.Serialize(
        Apps.Select(app => new
        {
            name = app.Name,
            title = app.Title,
            accessLayer = app.IsAccessLayer,
            verb = app.Verb,
            project = app.Project,
            description = app.Description,
            explorer = (app.Explorer ?? app.Opens) switch
            {
                Opens.Folders => new[] { "folders" },
                Opens.Files => ["files"],
                Opens.FilesAndFolders => ["folders", "files"],
                _ => [],
            },
            types = app.Types,
            openWith = app.OpenWith,
            typeName = app.TypeName,
            atSignIn = app.AtSignIn,
            storeEntry = app.StoreEntry,
        }),
        new JsonSerializerOptions { WriteIndented = true });

    /// <summary>The app of a verb.</summary>
    public static App Of(string verb) => Apps.First(app => app.Verb == verb);

    /// <summary>The app of a name as shown and spoken ("axDown", any case), null for a name no app has; the one place a name is matched (review, 2026-11-02).</summary>
    public static App? ByName(string name) => Apps.FirstOrDefault(app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The path an app is started on from a file or a folder in hand (AX-9.10, axFile AF-4.9): the path itself when the
    /// app opens its kind, the folder of a file for an app that opens folders only, null for a folder given to an app
    /// that opens files only, or for an app that opens nothing.
    /// </summary>
    public static string? PathFor(App app, string path, bool isFolder) => (app.Opens, isFolder) switch
    {
        (Opens.FilesAndFolders, _) or (Opens.Folders, true) or (Opens.Files, false) => path,
        (Opens.Folders, false) => System.IO.Path.GetDirectoryName(path),
        _ => null,
    };

    public const string Usage = """
        Usage: AxKit [claude|down|json|git|terminal|file|media|preview|start|process|task|pass|calc|clip] [arguments]

          AxKit claude [<folder>] ...  axClaude, the front end for Claude Code (AxKit claude --help lists its options)
          AxKit down [<file>]          axDown, the text editor
          AxKit json [<file>]          axJSON, the JSON editor
          AxKit git [<folder or file>] axGIT, the git manager, on the repository that holds the path
          AxKit terminal [<folder>]    axTerminal, a shell in the folder as a transcript
          AxKit file [<folder or file>] axFile, the file manager, on the folder (a file selected in its folder)
          AxKit media [<file or folder>...] [--add]  axMedia, the player: the file with its folder's sound and video files after it, or a folder's; --add appends to the open window's queue
          AxKit preview [<file>]       axPreview, the read-only viewer, on the file (the open axPreview window takes it)
          AxKit start [--hidden]       axStart, the program launcher: its search field, or with --hidden running until its hotkey
          AxKit process                axProcess, the processes, services and startup programs
          AxKit task [<folder>]        axTask, tasks and lists as a folder of small files
          AxKit pass [<database>]      axPass, a KeePass password database
          AxKit calc                   axCalc, the calculator
          AxKit clip [--hidden]        axClip, the clipboard history: its search field, or with --hidden running until its hotkey
          AxKit <folder>               the same as AxKit claude <folder>
          AxKit <file>                 the same as AxKit down <file>, AxKit json <file> for a .json file, AxKit pass <file> for a .kdbx file, or AxKit media <file> for a sound or video file
          AxKit                        axClaude on the current folder
          AxKit mcp                    the AI tools' server, for an MCP client to start (AxKit menu, AI tools)
          AxKit apps                   the apps as JSON on standard output, for the installer and other scripts
          --help, --version           this text, or the version

        The installed console commands axkit, axclaude, axdown, axjson, axgit, axterminal, axfile, axmedia, axpreview, axstart, axprocess, axtask, axpass, axcalc and axclip stand for AxKit, AxKit claude, AxKit down, AxKit json, AxKit git, AxKit terminal, AxKit file, AxKit media, AxKit preview, AxKit start, AxKit process, AxKit task, AxKit pass, AxKit calc and AxKit clip.
        """;

    /// <summary>Whether an argument asks for the usage text: the forms Windows and Unix programs take (the apps' hosts read it too).</summary>
    public static bool IsHelpFlag(string argument) => argument is "--help" or "-h" or "-?" or "/?";

    /// <summary>
    /// The editor a file opens in (AX-1.2; axFile's Open in editor AF-1.3, axPreview's Edit AP-1.4, axGIT's Open):
    /// axJSON's verb for a .json file, axDown's for any other.
    /// </summary>
    public static string EditorOf(string path) =>
        Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? AxJSON : AxDown;

    /// <summary>The file types the two editors open (their Open with lists), so that a viewer knows what is text whatever its bytes say.</summary>
    public static IEnumerable<string> EditorExtensions => Apps.Where(app => app.Verb is AxDown or AxJSON).SelectMany(app => app.OpenWith);

    /// <summary>
    /// The verb of a console command's name (AX-13.2): the Store's package starts every app's command (<c>axdown.exe</c>)
    /// through one entry, and Windows gives the process the command's own path as the first word of its command line,
    /// so the name says which app was meant. Null for <c>axkit</c>, <c>AxKit</c> and any other name.
    /// </summary>
    public static string? VerbOfProgramName(string? programPath)
    {
        if (string.IsNullOrEmpty(programPath))
        {
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(programPath);
        return Apps.FirstOrDefault(app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase))?.Verb;
    }

    /// <summary>
    /// The arguments with the verb the Store's package means put first (docs/axkit/SPEC.md AX-13.2): the console
    /// command's name when the process was started through one (<paramref name="programPath"/>, the command line's
    /// first word, <see cref="VerbOfProgramName"/>), else the package entry's (<paramref name="applicationId"/>,
    /// <see cref="PackageIdentity.ApplicationId"/>: the entry "AxDown" means down; tools/make-manifest.ps1 names the
    /// entries after the projects). Nothing changes for the entry "AxKit", the first entry, which carries every
    /// command and the file types of the apps without an entry and lets <see cref="Decide"/> read the arguments as
    /// outside the package; nor when the arguments already begin with a verb or a word of the bundle's, since Windows
    /// may pass the entry's own parameters as well.
    /// </summary>
    public static string[] WithApplicationVerb(string[] args, string? applicationId, string? programPath = null)
    {
        if (args.Length > 0)
        {
            var first = args[0];
            if (Apps.Any(app => string.Equals(first, app.Verb, StringComparison.OrdinalIgnoreCase))
                || IsHelpFlag(first) || first == "--version"
                || string.Equals(first, Mcp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(first, Listing, StringComparison.OrdinalIgnoreCase))
            {
                return args;
            }
        }

        var verb = VerbOfProgramName(programPath)
            ?? (string.IsNullOrEmpty(applicationId) ? null : Apps.FirstOrDefault(app => app.Project == applicationId)?.Verb);
        return verb is null ? args : [verb, .. args];
    }

    public static Launch Decide(string[] args)
    {
        if (args.Length == 0)
        {
            return new Launch(AxClaude, args);
        }

        var first = args[0];
        if (Apps.FirstOrDefault(app => string.Equals(first, app.Verb, StringComparison.OrdinalIgnoreCase)) is { } named)
        {
            return new Launch(named.Verb, args[1..]);
        }

        if (IsHelpFlag(first) || first == "--version")
        {
            return new Launch(Bundle, args);
        }

        if (string.Equals(first, Mcp, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(Mcp, args[1..]);
        }

        if (string.Equals(first, Listing, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(Listing, args[1..]);
        }

        if (first.StartsWith("--", StringComparison.Ordinal))
        {
            // An AxClaude option such as --project, or the bare -- that starts a new conversation.
            return new Launch(AxClaude, args);
        }

        if (Directory.Exists(first))
        {
            return new Launch(AxClaude, args);
        }

        if (File.Exists(first))
        {
            // AX-1.2: a .json file is AxJSON's, a KeePass database axPass's, a sound or video file AxMedia's, any other file AxDown's.
            if (EditorOf(first) == AxJSON)
            {
                return new Launch(AxJSON, args);
            }

            if (Path.GetExtension(first).Equals(".kdbx", StringComparison.OrdinalIgnoreCase))
            {
                return new Launch(AxPass, args);
            }

            return new Launch(MediaFiles.IsMedia(first) ? AxMedia : AxDown, args);
        }

        return new Launch(Bundle, args, $"There is no folder or file at {first}.");
    }
}
