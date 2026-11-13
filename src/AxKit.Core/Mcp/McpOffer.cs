namespace AxKit.Core.Mcp;

/// <summary>
/// The pure rules of <c>axkit_offer_command</c> (docs/axKit/mcp.md MC-9): a command an AI tool cannot or should not
/// run itself (a sign-in that asks questions, a command its own permissions refuse) is offered to the user, who hears
/// it whole in axClaude's window and alone decides; on Run, axClaude sends it to Claude Code as a <c>!</c> line, as
/// the user's own typing would. Nothing here runs anything: the check of the command, the question's words and the
/// line that is sent.
/// </summary>
public static class McpOffer
{
    /// <summary>The longest command offered: one a listener can still judge by ear.</summary>
    public const int MaxLength = 1000;

    /// <summary>How many offers one window holds for the user at a time; more are refused, so that no client buries the user in questions.</summary>
    public const int MaxWaiting = 3;

    /// <summary>Why a command is not offered, or null: empty, more than one line, a control character (which could hide part of it from the listener or press a key in Claude Code), or too long.</summary>
    public static string? Refusal(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "No command was given: pass the one line to run as command.";
        }

        if (command.Any(char.IsControl))
        {
            return "The command must be one line without control characters: the user hears it whole before it runs. Join several commands on one line, or offer them one by one.";
        }

        return command.Trim().Length > MaxLength
            ? $"The command is longer than {MaxLength} characters, too long to judge by ear: offer a shorter one, or write a script file and offer the line that runs it."
            : null;
    }

    /// <summary>The reason as the question shows it: one line, at most 300 characters, empty when none was given.</summary>
    public static string Reason(string? reason)
    {
        var line = new string((reason ?? string.Empty).Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return line.Length > 300 ? line[..300] : line;
    }

    /// <summary>
    /// The answers of the question, in the control area's list (MC-9.4). The first is the one an Enter pressed by
    /// accident takes, and it only explains; running is the second, and never where the list starts.
    /// </summary>
    public static readonly IReadOnlyList<string> Answers =
    [
        "1. Explain this command",
        "2. Yes, run it",
        "3. No, do not run it",
        "4. No, copy it to the clipboard instead",
        "5. No, and report a concern about it",
    ];

    public const int Explain = 0;
    public const int Run = 1;
    public const int No = 2;
    public const int Copy = 3;
    public const int Concern = 4;

    /// <summary>The name of the field that follows Report a concern: an instruction, with what Enter and Escape do.</summary>
    public const string ConcernName = "Type what concerns you about this command, or leave it empty. Enter opens Claude Code's feedback with it, for you to review before it is sent. Escape goes back";

    /// <summary>
    /// The line that opens Claude Code's own feedback with the concern (MC-9.8): the command, the reason given and the
    /// user's words on one line, 1500 characters at most. Claude Code shows it for review; nothing here sends it.
    /// </summary>
    public static string ConcernLine(string client, string command, string? reason, string? concern)
    {
        var why = Reason(reason);
        var words = Reason(concern);
        var text = $"Concern about a command {McpRun.Speaker(client)} offered me to run myself (axKit, axkit_offer_command). The command: {command.Trim()} "
            + $"The reason given: {(why.Length > 0 ? why : "none")} My concern: {(words.Length > 0 ? words : "not stated")}";
        return "/feedback " + (text.Length > 1500 ? text[..1500] : text);
    }

    /// <summary>The list's name, read each time the focus comes to it: who offers, the command whole, and that an AI wrote it (<see cref="AiCaution"/>).</summary>
    public static string AskName(string client, string command) =>
        $"{McpRun.Speaker(client)} offers a command for you to run: {command.Trim()}. {AiCaution.Short(McpRun.Speaker(client))}";

    /// <summary>What Ctrl+Q reads: the name and the reason.</summary>
    public static string AskText(string client, string command, string? reason)
    {
        var why = Reason(reason);
        return AskName(client, command) + (why.Length > 0 ? $". Its reason: {why}" : string.Empty);
    }

    /// <summary>The breakdown as the explanation shows it: its lines kept, every other control character a space, at most 4000 characters.</summary>
    public static string Details(string? details)
    {
        var text = new string((details ?? string.Empty).Replace("\r\n", "\n").Select(c => c != '\n' && char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return text.Length > 4000 ? text[..4000] : text;
    }

    public const string ExplainTitle = "About this command";

    /// <summary>
    /// The explanation, a text to read by line and by heading: the command, the reason, the breakdown the AI tool
    /// gave of each part with the pages it names (its own words, shown as such), and what running means.
    /// </summary>
    public static string ExplainText(string client, string command, string? reason, string? details)
    {
        var who = McpRun.Speaker(client);
        var why = Reason(reason);
        var parts = Details(details);
        return $"# Read this first\n\n{AiCaution.Command(who)}\n\n"
            + $"# The command\n\n{command.Trim()}\n\n"
            + $"# Why {who} offers it\n\n{(why.Length > 0 ? why : "No reason was given.")}\n\n"
            + (parts.Length > 0 ? $"# What each part does, in {who}'s words\n\n{parts}\n\n" : string.Empty)
            + "# What your answers do\n\n"
            + "Yes, run it sends the command to Claude Code as a line that starts with an exclamation mark, as if you had typed it. It runs in Claude Code's own shell, in this session's folder, without Claude Code's permission check, and what it prints joins the conversation.\n\n"
            + "No, do not run it sends nothing. No, copy it to the clipboard instead puts the command on your clipboard, in place of what was there, and sends nothing. No, and report a concern about it asks for your words, then opens Claude Code's own feedback with the command and your concern, where you review the report before anything goes to Anthropic.\n\n"
            + "While the question shows you can read the conversation: Tab or Ctrl+2 goes there, and Tab or Ctrl+1 comes back.";
    }

    /// <summary>The line sent to Claude Code: its shell mode's mark, a space, the command.</summary>
    public static string Line(string command) => "! " + command.Trim();

    /// <summary>What the AI tool is told once the offer waits for the user: to end its turn, since the question shows only then.</summary>
    public const string Waiting = "The user will be asked in axClaude's window once your turn has ended, and hears the command whole, with your reason and details on request. Say in a few words what the command is for, then end your turn: on Yes it is sent as a ! line and what it prints joins the conversation; on No nothing is sent.";

    /// <summary>What the AI tool is told when it runs outside axClaude: there is no window to ask in.</summary>
    public const string NoWindow = "This works only for a Claude Code that runs inside an axClaude window, and no axClaude window holds this session: ask the user to type the command themselves as a ! line.";
}
