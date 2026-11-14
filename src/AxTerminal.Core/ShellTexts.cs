namespace AxTerminal.Core;

/// <summary>
/// What axTerminal knows of the shells' own conventions beyond starting them (root CLAUDE.md rule 12): the keys it
/// sends, the letters and the secret's field of the prompt marks it reads, and the few facts about one shell or
/// another that decide what is sent or said. With <see cref="ShellLauncher"/> this is the one place for everything a
/// shell is told and everything read from it: the launcher finds each shell and builds its command line, its
/// environment, the prompt texts that print the marks and its continuation prompt; <see cref="TerminalModel"/> gives
/// the marks their meaning and reads the letters from here. The window sends and compares only what this table and
/// the launcher name, which <c>ShellTextsTests</c> checks off the sources. A shell's output itself is never matched:
/// it is the transcript, shown as it is. When a shell changes, the launcher or this file is adjusted and what the
/// user hears and presses stays. Nothing here is an abstraction over programs.
/// </summary>
public static class ShellTexts
{
    // ---- Keys, as a console sends them to the program in it ----

    /// <summary>Enter: a carriage return, a write of its own after each line of a command (SPEC.md AT-3.1).</summary>
    public const string Enter = "\r";

    /// <summary>Ctrl+C, which interrupts the running command (AT-3.5).</summary>
    public const string Interrupt = "\u0003";

    /// <summary>Ctrl+D, the end of input for a program that reads until it.</summary>
    public const string EndOfInput = "\u0004";

    /// <summary>Escape.</summary>
    public const string Escape = "\u001b";

    /// <summary>The Up and Down arrows, as a VT terminal sends them.</summary>
    public const string Up = "\u001b[A";
    public const string Down = "\u001b[B";

    // ---- The prompt marks (OSC 133, the Windows Terminal convention; AT-1.3), printed by the launcher's prompt texts ----

    /// <summary>A: the prompt starts; a command that ran has ended.</summary>
    public const char PromptStartMark = 'A';

    /// <summary>B: the prompt's text has ended and the shell waits for a command.</summary>
    public const char PromptEndMark = 'B';

    /// <summary>D: the command is done; the exit code follows when the shell has one to give.</summary>
    public const char CommandDoneMark = 'D';

    /// <summary>What separates the fields after a mark's letter ("D;3;axkit=…").</summary>
    public const string MarkFieldSeparator = ";";

    /// <summary>
    /// The field every mark of the shell's own prompt ends in, followed by the session's secret (security audit S-37).
    /// The launcher's prompt texts write the same word in front of <see cref="ShellLauncher.NonceToken"/>; a test
    /// keeps the two alike.
    /// </summary>
    public const string NonceField = "axkit=";

    // ---- Facts about one shell or another ----

    /// <summary>
    /// Whether a command of several lines needs one more Enter to run: PowerShell's console host, which runs without
    /// PSReadLine while a screen reader is on, ends a block only on an empty line (AT-3.1).
    /// </summary>
    public static bool EndsBlockOnEmptyLine(ShellKind kind) => kind is ShellKind.PowerShell7 or ShellKind.WindowsPowerShell;

    /// <summary>The exit code the PowerShell prompt text gives for an error of PowerShell's own, which has no code: said as "Failed" alone (AT-5.1).</summary>
    public const int ErrorWithoutCode = -1;

    /// <summary>
    /// Whether a row ends as a program's question does (AT-5.4): a colon, a question mark, a closing bracket or
    /// parenthesis ("Password:", "Continue? [y/N]", "(yes/no)"). A heuristic over any program's row, the only sign
    /// that it waits; the row is given with its trailing spaces removed.
    /// </summary>
    public static bool EndsLikeQuestion(string row) => row.Length > 0 && row[^1] is ':' or '?' or ']' or ')';
}
