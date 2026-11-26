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

    /// <summary>What the tool answers while the switch is off (MC-7.2), the way to allow it included.</summary>
    public const string SwitchOff = "The user has not allowed AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu, AI tools, Let AI tools run commands.";

    /// <summary>The refusal for an app that never runs a command for an AI tool, or null when it may.</summary>
    public static string? AppRefusal(string appVerb) =>
        NeverRun.Contains(appVerb, StringComparer.OrdinalIgnoreCase)
            ? appVerb.Equals(Dispatch.AxPass, StringComparison.OrdinalIgnoreCase)
                ? "axPass's commands are never run by AI tools: its windows hold passwords. The user runs them."
                : "axClip's commands are never run by AI tools: its window holds the clipboard's history. The user runs them."
            : null;

    /// <summary>What the tool answers while the window shows a Windows dialog (MC-7.3).</summary>
    public const string DialogShown = "The window shows the user a Windows dialog: commands run once the user has closed it.";

    /// <summary>What the tool answers for an act within a minute of the user's No (MC-7.3): whichever client asks, the window asks no more.</summary>
    public const string DeniedLately = "The user denied running a command a moment ago; this window asks no more within a minute.";

    /// <summary>What the tool answers while the window shows a notice (MC-7.3), which it names by its title.</summary>
    public static string NoticeShown(string? title) =>
        $"The window shows the user a notice, {title}: commands run once the user has answered it, and AI tools never answer it.";

    /// <summary>
    /// The words of a refusal the gate gave the AI tool's path (<see cref="CommandGate"/>,
    /// docs/axKit/command-language.md section 3): one sentence per reason, each written once, here or where this
    /// points. Only the reasons that path can get: a name's refusals are <see cref="Find"/>'s, and the first switch is
    /// the pipe's own answer; any other reason is a mistake of the caller.
    /// </summary>
    public static string Refusal(CommandRefusal why, string appVerb, string? noticeTitle) => why switch
    {
        CommandRefusal.AppNeverRun => AppRefusal(appVerb) ?? throw new ArgumentOutOfRangeException(nameof(appVerb), appVerb, "an app whose commands AI tools may run"),
        CommandRefusal.RunCommandsOff => SwitchOff,
        CommandRefusal.KeyTest => KeyTest.AiToolsRefusal,
        CommandRefusal.Notice => NoticeShown(noticeTitle),
        CommandRefusal.Dialog => DialogShown,
        CommandRefusal.UserBusy => UserBusy,
        CommandRefusal.DeniedLately => DeniedLately,
        _ => throw new ArgumentOutOfRangeException(nameof(why), why, "no refusal of axkit_window_run"),
    };

    /// <summary>A label or a menu path as it is matched:the letter's marker and the trailing dots gone, the spaces trimmed (<see cref="CommandNames.Plain"/>).</summary>
    public static string Plain(string text) => CommandNames.Plain(text);

    /// <summary>
    /// The command a name gives (MC-7.1), found by the bundle's one resolver (<see cref="CommandNames.Resolve"/>,
    /// docs/axKit/user-workflows.md WF-1.7): the label as <c>axkit_window_commands</c> lists it or as the palette shows
    /// it (its letter's marker and dots do not matter, nor case); <paramref name="menu"/>, when given, the menus that
    /// hold it ("Navigate, Next"), which tells apart two commands of one name; or, without a menu, the whole line as the
    /// palette lists it without its key ("Heading, Navigate, Next"). The index in <paramref name="commands"/>, or the
    /// refusal that says why none, in this tool's own sentences, which scripts and tests expect word for word.
    /// </summary>
    public static (int Index, string? Refusal) Find(IReadOnlyList<McpCommandName> commands, string? name, string? menu)
    {
        var names = commands.Select(command => new CommandName(command.Label, command.Menu)).ToList();
        var found = CommandNames.Resolve(names, name, menu);
        // The nearest commands, as the checks' do step names them (command-language.md section 8 (b)): one rule for both.
        var nearest = found.Candidates.Select(i => CommandNames.Line(names[i])).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return found.Match switch
        {
            CommandMatch.Found => (found.Index, null),
            CommandMatch.NoName => (-1, "Name the command, as axkit_window_commands lists it."),
            CommandMatch.Unknown when nearest.Count > 0 => (-1, $"The window has no command {found.Wanted}; the nearest: {string.Join("; ", nearest)}. axkit_window_commands lists them all."),
            CommandMatch.Unknown => (-1, $"The window has no command {found.Wanted}: axkit_window_commands lists them."),
            CommandMatch.UnknownInMenu => (-1, $"The window has no command {found.Wanted} in the menu {found.WantedMenu}: axkit_window_commands lists the commands with their menus."),
            _ when Menus(commands, found.Candidates) is [var only] =>
                (-1, $"The window has several commands named {found.Wanted}, all {(only == NoMenu ? "with no menu" : "in the menu " + only)}: neither the name nor the menu tells them apart, and none was run."),
            _ => (-1, $"The window has several commands named {found.Wanted}: give the menu too, one of: {string.Join("; ", Menus(commands, found.Candidates))}."),
        };
    }

    /// <summary>What an ambiguous name's commands are told apart by: their menus, each once, in the list's order (docs/axKit/user-workflows.md WF-1.5).</summary>
    private static List<string> Menus(IReadOnlyList<McpCommandName> commands, IReadOnlyList<int> candidates) =>
        [.. candidates.Select(i => commands[i].Menu.Length > 0 ? commands[i].Menu : NoMenu).Distinct(StringComparer.OrdinalIgnoreCase)];

    private const string NoMenu = "no menu";

    /// <summary>
    /// The names Claude Code gives itself in MCP's <c>initialize</c>, which the window says as "Claude" (MC-7.5); matched
    /// whole, so that a client that only starts with the word ("Claudette", "claude-whatever") is said by its own name.
    /// </summary>
    public static readonly IReadOnlyList<string> ClaudeCodeNames = ["claude-code", "Claude Code", "claude"];

    /// <summary>How long after the user's last key or mouse move a window in front refuses a run (MC-7.8, MC-D8), so that no command moves the caret or the focus under the user's hands.</summary>
    public const int UserBusyMs = 3000;

    /// <summary>The refusal while the user works in the window (MC-7.8).</summary>
    public const string UserBusy = "The user is working in this window right now (a key or the mouse within the last 3 seconds): commands run in a window in front only while the user pauses. Try again in a moment.";

    /// <summary>
    /// Why a run that was accepted did not happen (MC-7.8): the user began to work in the window between the check
    /// and the moment the window took the run up. The answer's <c>notRun</c> holds it ("Find did not run: the user
    /// was working in the window."), and nothing ran, so a client may ask again.
    /// </summary>
    public const string UserWasBusy = "the user was working in the window";

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
    public static string Said(string client, string label, string? text = null) => $"{Speaker(client)} ran {Named(label, text)}";

    /// <summary>The longest text a run carries for the field a command asks for (MC-7.9): a field of one line.</summary>
    public const int TextLimit = 1000;

    /// <summary>
    /// MC-7.9: why a run's text is refused before anything runs, or null: a field a command asks for takes one line of
    /// at most <see cref="TextLimit"/> characters. Null for no text.
    /// </summary>
    public static string? TextRefusal(string? text) => text switch
    {
        null => null,
        { Length: 0 } => "The text is empty: leave it out for a command that asks for none.",
        { Length: > TextLimit } => $"The text is longer than {TextLimit} characters: the field a command asks for takes one line.",
        _ when text.Contains('\n') || text.Contains('\r') => "The text has a line break: the field a command asks for takes one line.",
        _ => null,
    };

    /// <summary>MC-7.9: what the answer says when the command ran and opened no field for the text.</summary>
    public const string TextNotAsked = "The command asked for no text, so the text was not used.";

    /// <summary>MC-7.9: what the answer says when the field the command opened hides what is typed (a password): an AI tool never fills one.</summary>
    public const string TextHidden = "The command asks for a password, which AI tools never type: the user answers its question.";

    /// <summary>MC-7.10: what <c>axkit_window_field</c> answers when the window shows no form.</summary>
    public const string NoForm = "The window shows no form: axkit_window_state names the notice it shows, if any. A form's fields are listed under form there.";

    /// <summary>MC-7.10: what it answers for a call without a field.</summary>
    public const string NoFieldNamed = "Name the field: its number, its name or its key, as axkit_window_state lists them under form.";

    /// <summary>MC-7.10: what it answers for a field the form does not have.</summary>
    public static string NoSuchField(string field) => $"The form has no field {ShownTarget(field)}: axkit_window_state lists its fields under form.";

    /// <summary>MC-7.10: what the window says as an AI tool puts the focus on a field: "Claude moved to Due".</summary>
    public static string MovedTo(string client, string field) => $"{Speaker(client)} moved to {ShownTarget(field)}";

    /// <summary>A command's label as the user hears it, with the text it is run with when there is one (MC-7.9): "Find with the text budget".</summary>
    public static string Named(string label, string? text) =>
        text is null ? Plain(label) : $"{Plain(label)} with the text {ShownTarget(text)}";

    /// <summary>The title of the question an act command asks first (MC-7.3): "Claude asks to run Delete".</summary>
    public static string AskTitle(string client, string label, string? text = null) => $"{McpOffer.Label}: {Speaker(client)} asks to run {Named(label, text)}";

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
    public static string AskName(string client, string label, string menu, string target, string? text = null)
    {
        var place = menu.Length > 0 ? $" ({menu})" : string.Empty;
        var on = target.Trim().Length > 0 ? $" on {ShownTarget(target)}" : string.Empty;
        return $"{McpOffer.Start(McpOffer.ReachWindow)}. {Speaker(client)} asks to run this window's command {Named(label, text)}{place}{on}. {AiCaution.Asked(Speaker(client))}";
    }

    public const string ExplainTitle = "Command offer: about this window command";

    /// <summary>The explanation the list's first answer opens, a text to read by line and by heading: the caution first (bundle AX-11.12), the command, what it would act on, what an act may do, and what each answer does.</summary>
    public static string ExplainText(string client, string label, string menu, string target, string? text = null)
    {
        var who = Speaker(client);
        var shown = ShownTarget(target);
        return $"# Read this first\n\n{AiCaution.RunAsked(who)} {AiCaution.Request} "
            + $"axKit does not know why {who} asks: the conversation with it may say. If you do not know what the command will do here, do not allow it. Allowing it is your own act.\n\n"
            + "# How far it reaches\n\nIt acts outside this window: it may delete, move, send, open or start a program, or end something, exactly as when you run it yourself. It is this window's own menu command and does only what that menu item does; it is not a shell command.\n\n"
            + $"# The command\n\n{Plain(label)}{(menu.Length > 0 ? $", in the menu {menu}" : string.Empty)}.\n\n"
            + (text is null ? string.Empty : $"# The text it is run with\n\n{ShownTarget(text)}\n\nIt is typed into the field the command asks for, and the field's main button pressed, as you would.\n\n")
            + $"# What it would act on\n\n{(shown.Length > 0 ? shown : "Nothing in particular: the window names no selection.")}\n\n"
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
    public static string AskText(string client, string label, string menu, string target, string? text = null)
    {
        var place = menu.Length > 0 ? $" ({menu})" : string.Empty;
        var on = target.Trim().Length > 0 ? $" on {ShownTarget(target)}" : string.Empty;
        return $"{Speaker(client)} asks to run {Named(label, text)}{place}{on} in this window. It acts outside the window: it may delete, move, send, open or start a program, or end something, as when you run it yourself. {AiCaution.Request}\n\n"
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
