namespace AxKit.Core;

/// <summary>
/// The words that go with anything an AI wrote or asks to do (docs/axKit/SPEC.md AX-11.12; the root CLAUDE.md, rule
/// 11; the user's decision of 2026-11-13): who wrote it, that it can be wrong, that axKit checked nothing of it, and
/// that acting on it is the user's own choice. One place, so that every window says it in the same words and none
/// forgets it. Reminders in plain words, as the consent notices are (AX-11.10): not legal advice.
/// </summary>
public static class AiCaution
{
    /// <summary>Under a text an AI wrote for the user to read (an answer of AI help).</summary>
    public const string Written = "Written by Claude, an AI model. It can be wrong: check what matters before you rely on it.";

    /// <summary>In the name of a question about a command an AI offers, heard every time the question is read.</summary>
    public static string Short(string who) => $"Written by {who}, an AI: read the explanation before you run it";

    /// <summary>
    /// The first section of a command's explanation: the command and its explanation are the AI's own, a command it
    /// offers may be one its own checks refused, and running it is the user's own act.
    /// </summary>
    public static string Command(string who) =>
        $"{who}, an AI model, wrote this command and everything said about it below. It can be wrong, and axKit has checked none of it. "
        + $"A command is offered this way when {who} could not or should not run it itself. That can be because Claude Code's own permission or safety checks refused it, so run it with care. "
        + "Read all the details here, and the conversation that led to it, before you run anything. If you do not understand what it does, do not run it. "
        + "Running it is your own act, as if you had typed it: what it does to your files, your accounts and your computer is yours to answer for, not axKit's and not its maker's.";

    /// <summary>
    /// In front of a command an AI offered, where axClaude writes it out in the conversation because it was not sent
    /// (docs/axClaude/SPEC.md D33; the seventh review of 2026-11-27, K5): who wrote it, that it can be wrong and that
    /// nothing of it was checked, so that the line is never read as axKit's own; the command follows as
    /// <c>McpOffer.Shown</c> writes it.
    /// </summary>
    public static string OfferedNotSent(string who) =>
        $"the line of the command {who}, an AI model, offered, which can be wrong and which axKit has checked none of; running it is your own choice";

    /// <summary>The same in front of the line that would have opened Claude Code's feedback about such a command (MC-9.8): the command in it is the AI's, the concern the user's.</summary>
    public static string ConcernNotSent(string who) =>
        $"the feedback line about a command {who}, an AI model, offered; the command in it is {who}'s and can be wrong";

    /// <summary>In the name of the question an AI tool's run of a window command asks, heard every time the question is read.</summary>
    public static string Asked(string who) => $"Asked by {who}, an AI: read the explanation before you allow it";

    /// <summary>
    /// The first section of the explanation of a tool call an AI asks leave for (axClaude over Claude Code's stream,
    /// docs/axClaude/stream-mode.md SM-7.6): the request and everything said about it are the AI's own, nothing of
    /// it is checked, and allowing it is the user's own act.
    /// </summary>
    public static string ToolCall(string who) =>
        $"{who}, an AI model, asks to do this, and everything below is {who}'s own request. It can be wrong, and axKit has checked none of it. "
        + "Read all of it, and the conversation that led to it, before you allow it. If you do not understand what it does, do not allow it. "
        + "Allowing it is your own act, as if you had done it yourself: what it does to your files, your accounts and your computer is yours to answer for, not axKit's and not its maker's.";

    /// <summary>
    /// In the name of the question about a plan an AI asks the user to approve (axClaude over Claude Code's stream,
    /// docs/axClaude/stream-mode.md SM-7.7), heard every time the question is read: who wrote it, and that approving it
    /// lets the AI carry it out.
    /// </summary>
    public static string PlanAsked(string who) => $"Written by {who}, an AI: read the whole plan before you approve it. Approving lets {who} carry it out";

    /// <summary>
    /// The first section of the whole text of a plan an AI asks the user to approve (SM-7.7): the plan is the AI's own,
    /// nothing of it is checked, approving it lets the AI act on it, with auto mode without asking for each step, and
    /// approving is the user's own act.
    /// </summary>
    public static string Plan(string who) =>
        $"{who}, an AI model, wrote this plan, and everything in it is {who}'s own. It can be wrong, and axKit has checked none of it. "
        + $"Approving it lets {who} carry it out: with manual approval {who} still asks before each edit, with auto mode Claude Code's own checks decide alone what runs without asking you. "
        + "Read all of it, and the conversation that led to it, before you approve it. If you do not understand what it would do, do not approve it. "
        + "Approving it is your own act: what it does to your files, your accounts and your computer is yours to answer for, not axKit's and not its maker's.";

    /// <summary>
    /// In the name of the question about an action of a list that runs a program or hands a command to a shell's
    /// window (axTask, docs/axTask/forms-framework.md AF-7.2), heard every time the question is read. Who wrote the
    /// action is not known: the user, another program or an AI.
    /// </summary>
    public const string ActionShort = "An AI may have written it: read the explanation before you run it";

    /// <summary>
    /// The first section of that question's explanation, in <see cref="Command"/>'s terms (the maintainer's decision
    /// of 2026-11-21: the run question uses the AI caution's words): the action may be an AI's, it can be wrong,
    /// nothing of it is checked, an AI may have put it there because its own checks refused the command, and
    /// running it is the user's own act.
    /// </summary>
    public const string Action =
        "This action is written in the list's configuration file. axKit does not know who wrote it: you, another program, or an AI model. It can be wrong, and axKit has checked none of it. "
        + "An AI can write an action into a list when it could not or should not run a command itself. That can be because its own permission or safety checks refused it, so run it with care. "
        + "Read all the details here before you run anything. If you do not understand what it does, do not run it. "
        + "Running it is your own act, as if you had typed it: what it does to your files, your accounts and your computer is yours to answer for, not axKit's and not its maker's.";

    /// <summary>
    /// With axHelp's offer to carry out the steps it gave (docs/axKit/ai-help.md AH-7.3b; the maintainer's decision of
    /// 2026-11-16, "Explain, then offer to do it"): who would act, that it can be wrong, that each command is heard,
    /// and that saying yes is the user's own choice. Claude says it with the offer, word for word.
    /// </summary>
    public const string OfferToDo = "Claude, an AI, would run these steps for you through axKit's AI tools. It can be wrong; you hear each command as it runs, and a command that acts outside the window asks you first. Say yes only if you want it done.";

    /// <summary>
    /// The first words of the report form an AI tool filled (docs/axKit/mcp.md MC-9.11): who wrote the draft, that it
    /// can be wrong, and that nothing is sent but by the user's own Send.
    /// </summary>
    public static string Drafted(string who) =>
        $"{who}, an AI model, drafted this report. It can be wrong: read every word, change what is not so, and send it only if it says what you mean. Nothing is sent until you press Send.";

    /// <summary>
    /// The first words of the question an AI tool's <c>axkit_open</c> asks before an app that starts a program, or
    /// axTask on a folder, opens (docs/axKit/mcp.md MC-6.5a): who asks, that it can be wrong, and that opening is the
    /// user's own choice.
    /// </summary>
    public static string OpenAsked(string who) =>
        $"{who}, an AI model, asks for this. It can be wrong, and axKit has checked nothing of why it asks. Open it only if you want it open: that is your own choice.";

    /// <summary>
    /// The first line of the words of a report an AI tool drafted (docs/axKit/mcp.md MC-9.11), kept in the report unless
    /// the user takes it out, so that the maintainer knows who wrote it. Here since 2026-11-25 (repairs R-98: rule 11's
    /// words come from this class alone).
    /// </summary>
    public static string ReportMark(string who) => $"(Drafted by {who}, an AI tool; read by me before sending.)";

    /// <summary>The first sentence of the notice of an AI tool's run of a window command (MC-7.3): who asks, and what is asked; <see cref="Request"/> follows it.</summary>
    public static string RunAsked(string who) => $"{who}, an AI model, asks to run a command of this window, as if you had chosen it from the menu.";

    /// <summary>In the question an AI tool's run of a window command asks.</summary>
    public const string Request = "The request comes from an AI tool, which can be wrong: allow it only if you know what the command will do here.";
}
