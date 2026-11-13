using System.Text.Json.Nodes;

namespace AxKit.Core.Mcp;

/// <summary>
/// The tools of the first stage (docs/axKit/mcp.md MC-6.5): what they are called, what they say of themselves, and the
/// check of <c>axkit_open</c>'s arguments, which is pure and tested. Reading a window is the window's own work
/// (<c>AxKit.Forms/McpChannel</c>); listing and opening are the broker's (<c>AxKit.Forms/McpBroker</c>).
/// </summary>
public static class McpTools
{
    public const string Apps = "axkit_apps";
    public const string Windows = "axkit_windows";
    public const string State = "axkit_window_state";
    public const string List = "axkit_window_list";
    public const string Text = "axkit_window_text";
    public const string Commands = "axkit_window_commands";
    public const string Said = "axkit_window_said";
    public const string Open = "axkit_open";
    public const string Run = "axkit_window_run";
    public const string TaskLists = "axkit_task_lists";
    public const string TaskItems = "axkit_task_items";
    public const string TaskAdd = "axkit_task_add";
    public const string Offer = "axkit_offer_command";

    /// <summary>The apps that serve no pipe and so cannot be read (MC-4.1, MC-4.2): the password database and the clipboard history.</summary>
    public static readonly IReadOnlyList<string> NeverRead = [Dispatch.AxPass, Dispatch.AxClip];

    /// <summary>The apps <c>axkit_open</c> refuses: each starts a program as it opens (Claude Code, a shell), which is an act (MC-3.1), for a later stage with its confirmation.</summary>
    public static readonly IReadOnlyList<string> NeverOpened = [Dispatch.AxClaude, Dispatch.AxTerminal];

    /// <summary>The window tools, which take the window's id from <c>axkit_windows</c>, and the request each sends to the window.</summary>
    public static readonly IReadOnlyDictionary<string, string> WindowRequests = new Dictionary<string, string>
    {
        [State] = "state",
        [List] = "list",
        [Text] = "text",
        [Commands] = "commands",
        [Said] = "said",
    };

    /// <summary>
    /// The typed data tools of one app (MC-8.1): each answered by an open window of that app (its verb here), never by the
    /// broker itself, and whether it changes something, which needs Let AI tools run commands as a run does (MC-8.3).
    /// The window's own code answers them (<c>AxKitForm.McpAppRequest</c>); the shared code holds no app's rules.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string App, bool Changes)> AppTools = new Dictionary<string, (string App, bool Changes)>
    {
        [TaskLists] = (Dispatch.AxTask, false),
        [TaskItems] = (Dispatch.AxTask, false),
        [TaskAdd] = (Dispatch.AxTask, true),
        // MC-9: not a change of the window's: the user is asked every time, whatever the switch, and alone runs it.
        [Offer] = (Dispatch.AxClaude, false),
    };

    public static IReadOnlyList<McpTool> All { get; } =
    [
        new(Apps, "The axKit apps: each one's verb for axkit_open, its name, and whether it opens files, folders or nothing.", Schema()),
        new(Windows, "The open axKit windows that can be read: an id for the window tools, the app, the title. axPass and axClip are never listed.", Schema()),
        new(State, "One window's state: its title, its status bar, the notice it shows with its text, and the focused control with its value. A password field reads as protected.", Schema(("window", "The window's id from axkit_windows."))),
        new(List, "The items of the window's focused list, or of its first list, with the selected one.", Schema(("window", "The window's id from axkit_windows."))),
        new(Text, "The lines of the window's focused text, or of its main text (an editor's file, a viewer's, a transcript), at most 1 MB.", Schema(("window", "The window's id from axkit_windows."))),
        new(Commands, "The window's commands as the user's command palette lists them, every menu item: the commands, the settings with whether each is checked, the recent files and windows; each with its label, the menus that hold it, the key, and the mark (read, open, change, act). axkit_window_run runs one by its label.", Schema(("window", "The window's id from axkit_windows."))),
        new(Said, "What the window announced to the user since the last call, the last 50 announcements.", Schema(("window", "The window's id from axkit_windows."))),
        new(Run, "Runs one command of a window, as its menu item would, by its label from axkit_window_commands, and gives what the window then said, or the question it now asks the user. Only while the user allows AI tools to run commands; never in axPass; never an answer to the window's question, which the user gives; a command that acts outside the window (marked act) asks the user first. The window says who ran it.", Require(Schema(("window", "The window's id from axkit_windows."), ("command", "The command's label as axkit_window_commands lists it, such as Word wrap."), ("menu", "Optional: the menus that hold it as axkit_window_commands lists them, such as Navigate, Next, when two commands share a label.")), "window", "command")),
        new(TaskLists, "The user's axTask lists as the open axTask window reaches them: the lists of its top folder and of the project folders it found, each with its name for axkit_task_items and axkit_task_add, its place, its number of items and of tasks not done. Needs an open axTask window: axkit_open with the app task opens one.", Schema()),
        new(TaskItems, "The items of one axTask list, each with its fields (title, type, value). Done items are left out unless done is true.", TaskItemsSchema()),
        new(TaskAdd, "Adds an item to one axTask list, made from the list's template as the window's New item makes it: a Title and optional field values, a date as 2026-11-18, today, tomorrow, +7 or a weekday such as Monday, a choice as one of its options (Priority: High), anything else as text (Notes). Only while the user allows AI tools to run commands; the window says who added it, and Ctrl+Z there takes it back. It never changes or deletes an item.", TaskAddSchema()),
        new(Offer, "Offers the user one shell command to run themselves, for when you cannot or should not run it: a sign-in that asks questions, a command your permissions refuse, something the user asked to run by hand. Only when you run inside an axClaude window. The user hears the command whole there after your turn ends and decides; on Run it is sent as a ! line and what it prints joins the conversation. Call it, say what the command is for, and end your turn. Never for a command you may run yourself.", OfferSchema()),
        new(Open, "Opens an axKit app in a new window, on a file or folder of this computer or on nothing. The user sees and hears it open.", Schema(("app", "The app's verb from axkit_apps: down, file, git ..."), ("path", "Optional: the full path of a file or folder on this computer."))),
    ];

    /// <summary>
    /// Whether a part of the path, from its root down, is a link (a reparse point): each part's attributes are read
    /// without following it, and a part that does not exist ends the walk, since nothing below it is reached.
    /// </summary>
    public static bool PassesLink(string path)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var current = root;
        foreach (var part in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            try
            {
                if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>A schema of string arguments; the first is required when the tool takes the window's id or an app.</summary>
    private static JsonObject Schema(params (string Name, string Description)[] arguments)
    {
        var properties = new JsonObject();
        foreach (var (name, description) in arguments)
        {
            properties[name] = new JsonObject { ["type"] = "string", ["description"] = description };
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties, ["additionalProperties"] = false };
        if (arguments.Length > 0)
        {
            schema["required"] = new JsonArray(arguments[0].Name);
        }

        return schema;
    }

    private static JsonObject TaskItemsSchema()
    {
        var schema = Schema(("list", "The list's name as axkit_task_lists gives it, such as Personal, or a project's list such as Work / Tasks."));
        schema["properties"]!["done"] = new JsonObject { ["type"] = "boolean", ["description"] = "Optional: true to include the items already done." };
        return schema;
    }

    private static JsonObject TaskAddSchema()
    {
        var schema = Require(Schema(
            ("list", "The list's name as axkit_task_lists gives it, such as Personal."),
            ("title", "The new item's Title, one line, such as Call the dentist.")), "list", "title");
        schema["properties"]!["fields"] = new JsonObject
        {
            ["type"] = "object",
            ["description"] = "Optional: other fields by their title, each a text as the user would type it, such as {\"Due\": \"2026-11-18\", \"Priority\": \"High\", \"Notes\": \"Ask about Tuesday\"}.",
            ["additionalProperties"] = new JsonObject { ["type"] = "string" },
        };
        return schema;
    }

    private static JsonObject OfferSchema() => Schema(
        ("command", "The command, one line, as the user would type it after the ! mark; it runs in Claude Code's shell, which is Bash on Windows too unless the user set another."),
        ("reason", "Optional: one sentence the user hears, why they should run it themselves."),
        ("window", "Optional: an axClaude window's id from axkit_windows; left out, the window this session runs in."));

    /// <summary>The schema with these arguments required, where more than the first is.</summary>
    private static JsonObject Require(JsonObject schema, params string[] names)
    {
        schema["required"] = new JsonArray([.. names.Select(name => (JsonNode)name)]);
        return schema;
    }

    /// <summary>
    /// The command line <c>axkit_open</c> starts (MC-6.5), or the reason it does not: the app by its verb; the path, when
    /// given, fully qualified, on this computer, existing, and of a kind the app opens; the arguments as a list, never
    /// a command line a shell would read; no link on the way (a junction or a symbolic link may lead to a network
    /// share, which asking whether the path exists would reach). <paramref name="isFolder"/>,
    /// <paramref name="isFile"/> and <paramref name="passesLink"/> ask the file system, so that the test gives its own
    /// answers; the link is asked first, since the other two follow links.
    /// </summary>
    public static (string[]? Arguments, string? Refusal) OpenArguments(string? verb, string? path, Func<string, bool> isFolder, Func<string, bool> isFile, Func<string, bool> passesLink)
    {
        if (Dispatch.Apps.FirstOrDefault(app => string.Equals(app.Verb, verb, StringComparison.OrdinalIgnoreCase)) is not { } app)
        {
            return (null, $"No app has the verb {verb}; axkit_apps lists them.");
        }

        if (NeverOpened.Contains(app.Verb))
        {
            return (null, $"{app.Name} starts a program as it opens, which AI tools cannot do yet: the user opens it.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return ([app.Verb], null);
        }

        // MC-D12 (2026-11-11): axTask started on a folder makes that folder its top folder and saves it, and the
        // typed tools reach the top folder's lists: a client choosing it could read and write item files anywhere.
        // So the tools open axTask only on the user's own top folder.
        if (app.Verb == Dispatch.AxTask)
        {
            return (null, "axTask opens through the AI tools only on your own lists: call axkit_open with app task and no path; the user chooses its folder.");
        }

        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return (null, "The path must be a full path on this computer, such as C:\\work\\notes.md.");
        }

        if (!LocalPaths.IsLocal(path))
        {
            return (null, "The path is not on a drive of this computer: a network share or a mapped drive is never opened.");
        }

        if (passesLink(path))
        {
            return (null, "The path goes through a link (a junction or a symbolic link), which AI tools never follow.");
        }

        var folder = isFolder(path);
        if (!folder && !isFile(path))
        {
            return (null, $"There is no file or folder at {path}.");
        }

        if (Dispatch.PathFor(app, path, folder) is not { } target)
        {
            return (null, $"{app.Name} does not open {(folder ? "a folder" : "a file")}.");
        }

        return ([app.Verb, target], null);
    }
}
