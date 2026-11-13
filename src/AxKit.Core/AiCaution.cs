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

    /// <summary>In the name of the question an AI tool's run of a window command asks, heard every time the question is read.</summary>
    public static string Asked(string who) => $"Asked by {who}, an AI: read the explanation before you allow it";

    /// <summary>In the question an AI tool's run of a window command asks.</summary>
    public const string Request = "The request comes from an AI tool, which can be wrong: allow it only if you know what the command will do here.";
}
