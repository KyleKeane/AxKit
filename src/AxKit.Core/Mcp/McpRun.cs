namespace AxKit.Core.Mcp;

/// <summary>One command of a window as <c>axkit_window_run</c> matches it: its label with its letter, and the menus that hold its item ("Navigate, Next"), empty for a key without an item.</summary>
public sealed record McpCommandName(string Label, string Menu);

/// <summary>
/// What an act command would act on (docs/axKit/mcp.md MC-7.3): <paramref name="Shown"/>, the few words the question
/// names it by ("report.pdf in files"), and <paramref name="Identity"/>, what the user's Run compares (axFile's full
/// paths, the item apps' list and item file), so that two selections with the same short words are never taken for
/// one. <see cref="Of"/> for a target whose words say it whole.
/// </summary>
public sealed record RunTarget(string Shown, string Identity)
{
    public static readonly RunTarget None = new(string.Empty, string.Empty);

    public static RunTarget Of(string shown) => new(shown, shown);
}

/// <summary>
/// The pure rules of <c>axkit_window_run</c> (docs/axKit/mcp.md MC-7): which windows never run a command for an AI tool,
/// the words of a refusal, how a command's name is matched, and what the window says when a command ran. The window
/// does the running (<c>AxKit.Forms/AxKitForm.Mcp.cs</c>); the switch is <see cref="McpSettings.RunCommands"/>. Tested
/// in <c>tests/AxKit.Tests/McpTests</c>.
/// </summary>
public static class McpRun
{
    /// <summary>The apps whose commands an AI tool never runs (MC-7.4): the password database, and the clipboard history, which serves no pipe at all.</summary>
    public static readonly IReadOnlyList<string> NeverRun = [Dispatch.AxPass, Dispatch.AxClip];

    /// <summary>The menu item of the switch (MC-7.2), in the axKit menu's AI tools submenu.</summary>
    public const string SwitchLabel = "Let AI tools run commands";

    /// <summary>What the tool answers while the switch is off (MC-7.2), the way to allow it included.</summary>
    public const string SwitchOff = "The user has not allowed AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu, AI tools, Let AI tools run commands.";

    /// <summary>The refusal for an app that never runs a command for an AI tool, or null when it may.</summary>
    public static string? AppRefusal(string appVerb) =>
        NeverRun.Contains(appVerb, StringComparer.OrdinalIgnoreCase)
            ? appVerb.Equals(Dispatch.AxPass, StringComparison.OrdinalIgnoreCase)
                ? "axPass's commands are never run by AI tools: its windows hold passwords. The user runs them."
                : "axClip's commands are never run by AI tools: its window holds the clipboard's history. The user runs them."
            : null;

    /// <summary>A label or a menu path as it is matched: the letter's marker and the trailing dots gone, the spaces trimmed.</summary>
    public static string Plain(string text) => MenuText.Plain(text).Trim();

    /// <summary>
    /// The command a name gives (MC-7.1): the label as <c>axkit_window_commands</c> lists it or as the palette shows it
    /// (its letter's marker and dots do not matter, nor case); <paramref name="menu"/>, when given, the menus that hold
    /// it ("Navigate, Next"), which tells apart two commands of one name. The index in <paramref name="commands"/>, or
    /// the refusal that says why none.
    /// </summary>
    public static (int Index, string? Refusal) Find(IReadOnlyList<McpCommandName> commands, string? name, string? menu)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (-1, "Name the command, as axkit_window_commands lists it.");
        }

        var wanted = Plain(name);
        var named = Enumerable.Range(0, commands.Count)
            .Where(i => string.Equals(Plain(commands[i].Label), wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrWhiteSpace(menu))
        {
            var place = Plain(menu);
            named = [.. named.Where(i => string.Equals(Plain(commands[i].Menu), place, StringComparison.OrdinalIgnoreCase))];
        }

        if (named.Count == 0)
        {
            return (-1, string.IsNullOrWhiteSpace(menu)
                ? $"The window has no command {wanted}: axkit_window_commands lists them."
                : $"The window has no command {wanted} in the menu {Plain(menu)}: axkit_window_commands lists the commands with their menus.");
        }

        if (named.Count > 1)
        {
            var places = string.Join("; ", named.Select(i => commands[i].Menu.Length > 0 ? commands[i].Menu : "no menu"));
            return (-1, $"The window has several commands named {wanted}: give the menu too, one of: {places}.");
        }

        return (named[0], null);
    }

    /// <summary>
    /// The names Claude Code gives itself in MCP's <c>initialize</c>, which the window says as "Claude" (MC-7.5); matched
    /// whole, so that a client that only starts with the word ("Claudette", "claude-whatever") is said by its own name.
    /// </summary>
    public static readonly IReadOnlyList<string> ClaudeCodeNames = ["claude-code", "Claude Code", "claude"];

    /// <summary>How long after the user's last key or mouse move a window in front refuses a run (MC-7.8, MC-D8), so that no command moves the caret or the focus under the user's hands.</summary>
    public const int UserBusyMs = 3000;

    /// <summary>The refusal while the user works in the window (MC-7.8).</summary>
    public const string UserBusy = "The user is working in this window right now (a key or the mouse within the last 3 seconds): commands run in a window in front only while the user pauses. Try again in a moment.";

    /// <summary>MC-7.8: whether a run is refused because the user is working in the window: it is in front and the user's last input is more recent than <see cref="UserBusyMs"/>.</summary>
    public static bool IsUserBusy(bool inFront, long idleMs) => inFront && idleMs >= 0 && idleMs < UserBusyMs;

    /// <summary>Who ran it, as the user hears it (MC-7.5): "Claude" for Claude Code by one of its own names (<see cref="ClaudeCodeNames"/>, any case), the client's own name otherwise, its hidden characters made visible and at most 40 characters.</summary>
    public static string Speaker(string client)
    {
        var name = DisplayText.Visible(client.Trim());
        if (ClaudeCodeNames.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        return name.Length == 0 ? "An AI tool" : name.Length > 40 ? name[..40] : name;
    }

    /// <summary>What the window says as a command runs for an AI tool (MC-7.5): "Claude ran Word wrap".</summary>
    public static string Said(string client, string label) => $"{Speaker(client)} ran {Plain(label)}";

    /// <summary>The title of the question an act command asks first (MC-7.3): "Claude asks to run Delete".</summary>
    public static string AskTitle(string client, string label) => $"{McpOffer.Label}: {Speaker(client)} asks to run {Plain(label)}";

    /// <summary>
    /// The answers of the question as a list (MC-7.3), in the order of an offered command's (<see cref="McpOffer.Answers"/>):
    /// the first only explains, so that an Enter pressed by accident runs nothing; running is never where the list starts.
    /// </summary>
    public static readonly IReadOnlyList<string> Answers =
    [
        "1. Explain this command",
        "2. Yes, run it",
        "3. No, do not run it",
    ];

    public const int Explain = 0;
    public const int Yes = 1;
    public const int No = 2;

    /// <summary>The two buttons of the question where a window has no place for the list and asks in a notice: the same words, the same order.</summary>
    public const string YesButton = "&Yes, run it";
    public const string NoButton = "&No, do not run it";

    /// <summary>The list's name, read each time the focus comes to it: the label every offer starts with, who asks, the command with its menus and what it would act on, and that an AI asks (<see cref="AiCaution"/>).</summary>
    public static string AskName(string client, string label, string menu, string target)
    {
        var place = menu.Length > 0 ? $" ({menu})" : string.Empty;
        var on = target.Trim().Length > 0 ? $" on {ShownTarget(target)}" : string.Empty;
        return $"{McpOffer.Label}. {Speaker(client)} asks to run this window's command {Plain(label)}{place}{on}. {AiCaution.Asked(Speaker(client))}";
    }

    public const string ExplainTitle = "Command offer: about this window command";

    /// <summary>The explanation the list's first answer opens, a text to read by line and by heading: the caution first (bundle AX-11.12), the command, what it would act on, what an act may do, and what each answer does.</summary>
    public static string ExplainText(string client, string label, string menu, string target)
    {
        var who = Speaker(client);
        var shown = ShownTarget(target);
        return $"# Read this first\n\n{who}, an AI model, asks to run a command of this window, as if you had chosen it from the menu. {AiCaution.Request} "
            + $"axKit does not know why {who} asks: the conversation with it may say. If you do not know what the command will do here, do not allow it. Allowing it is your own act.\n\n"
            + $"# The command\n\n{Plain(label)}{(menu.Length > 0 ? $", in the menu {menu}" : string.Empty)}.\n\n"
            + $"# What it would act on\n\n{(shown.Length > 0 ? shown : "Nothing in particular: the window names no selection.")}\n\n"
            + "# What it may do\n\nIt acts outside the window: it may delete, move, send, open or start a program, or end something, exactly as when you run it yourself.\n\n"
            + "# What your answers do\n\n"
            + "Yes, run it runs the command once, now, and not at all if what is selected has changed meanwhile.\n\n"
            + "No, do not run it, which is Escape too, runs nothing. This window then asks no more for a minute.";
    }

    /// <summary>The longest target the question shows (MC-7.3); the rest is cut with three dots, and the whole is still what is compared.</summary>
    public const int TargetShownLength = 200;

    /// <summary>The reason a Run is refused when the window's selection is no longer the one the question named (MC-7.3).</summary>
    public const string SelectionChanged = "the selection changed";

    /// <summary>Names as one target (MC-7.3): "report.pdf", "a.txt and b.txt", "a.txt, b.txt and c.txt"; empty for none.</summary>
    public static string Names(IReadOnlyList<string> names) => names.Count switch
    {
        0 => string.Empty,
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1],
    };

    /// <summary>A target as the question shows it: on one line, its hidden characters made visible, at most <see cref="TargetShownLength"/> characters.</summary>
    public static string ShownTarget(string target)
    {
        var line = DisplayText.Visible(target.ReplaceLineEndings(" ").Trim());
        return line.Length > TargetShownLength ? line[..TargetShownLength].TrimEnd() + "..." : line;
    }

    /// <summary>
    /// The text of the question an act command asks first (MC-7.3): who asks, the command with its menus, what it would act
    /// on (<paramref name="target"/>, the window's selection as <c>McpTarget</c> describes it; left out when empty), what
    /// an act may do, and that a changed selection stops it.
    /// </summary>
    public static string AskText(string client, string label, string menu, string target)
    {
        var place = menu.Length > 0 ? $" ({menu})" : string.Empty;
        var on = target.Trim().Length > 0 ? $" on {ShownTarget(target)}" : string.Empty;
        return $"{Speaker(client)} asks to run {Plain(label)}{place}{on} in this window. It acts outside the window: it may delete, move, send, open or start a program, or end something, as when you run it yourself. {AiCaution.Request}\n\n"
            + "Yes, run it runs it once, now, and not at all if what is selected has changed meanwhile. No, do not run it, does not.";
    }

    /// <summary>
    /// Why the user's Run does not run (MC-7.3): <see cref="SelectionChanged"/> when the target's identity now differs
    /// from the one asked about, null when it is the same; the shown words are never compared.
    /// </summary>
    public static string? TargetRefusal(RunTarget asked, RunTarget now) =>
        string.Equals(asked.Identity, now.Identity, StringComparison.Ordinal) ? null : SelectionChanged;

    /// <summary>How long after a chime for a run in a window behind the same window plays none for the next (MC-7.5): one chime for a burst of runs.</summary>
    public const int ChimeQuietMs = 5000;

    /// <summary>MC-7.5: whether a run in a window behind chimes and flashes: the first run, or one at least <see cref="ChimeQuietMs"/> after the last chime (-1 for none yet).</summary>
    public static bool Chimes(long lastChimeTick, long now) => lastChimeTick < 0 || now - lastChimeTick >= ChimeQuietMs;
}
