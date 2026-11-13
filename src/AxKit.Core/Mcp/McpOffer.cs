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

    public static string AskTitle(string client) => $"{McpRun.Speaker(client)} offers a command for you to run";

    /// <summary>The question: the command on a line of its own, first, so that it is what is heard; then why, then what Run does.</summary>
    public static string AskText(string client, string command, string? reason)
    {
        var why = Reason(reason);
        return $"{McpRun.Speaker(client)} asks you to run this command yourself:\n\n{command.Trim()}\n\n"
            + (why.Length > 0 ? $"Its reason: {why}\n\n" : string.Empty)
            + "Run it sends the command to Claude Code as a ! line, as if you had typed it: it runs in Claude Code's own shell, in this session's folder, "
            + "without Claude Code's permission check, and what it prints joins the conversation. Do not run sends nothing.";
    }

    /// <summary>The line sent to Claude Code: its shell mode's mark, a space, the command.</summary>
    public static string Line(string command) => "! " + command.Trim();

    /// <summary>What the AI tool is told once the offer waits for the user: to end its turn, since the question shows only then.</summary>
    public const string Waiting = "The user will be asked in axClaude's window once your turn has ended, and hears the command whole. Say in a few words what the command is for, then end your turn: on Run it is sent as a ! line and what it prints joins the conversation; on Do not run nothing is sent.";

    /// <summary>What the AI tool is told when it runs outside axClaude: there is no window to ask in.</summary>
    public const string NoWindow = "This works only for a Claude Code that runs inside an axClaude window, and no axClaude window holds this session: ask the user to type the command themselves as a ! line.";
}
