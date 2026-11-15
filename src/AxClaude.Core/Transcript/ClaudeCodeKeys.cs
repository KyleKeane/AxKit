namespace AxClaude.Core.Transcript;

/// <summary>
/// The one table of what axClaude sends to Claude Code for a key (root CLAUDE.md rule 12): the bytes a terminal sends,
/// written to Claude's pseudo console, which hands them on in xterm's encoding. The window's commands, the control
/// area and <see cref="ScreenReader"/> (a hint row's keys) read them from here; a test (<c>ClaudeCodeTextsTests</c>)
/// fails when an escape or control character is written in a string of another file of axClaude. The names of the
/// keys as a hint row prints them are <see cref="ClaudeCodeTexts"/>' (<c>KeyUp</c>, "↑"); what Claude Code does with a
/// key is in its documentation (code.claude.com/docs/en/interactive-mode and keybindings) and SPEC.md FR-2.4.
/// Constants only.
/// </summary>
public static class ClaudeCodeKeys
{
    /// <summary>Enter: sends the message, picks an answer. Always a write of its own (a text with it is a paste).</summary>
    public const string Enter = "\r";

    /// <summary>Ctrl+J: a new line in the draft, a write of its own.</summary>
    public const string NewLine = "\n";

    public const string Escape = "\x1b";

    public const string Tab = "\t";

    /// <summary>Shift+Tab: the next permission mode.</summary>
    public const string ShiftTab = "\x1b[Z";

    public const string Up = "\x1b[A";
    public const string Down = "\x1b[B";
    public const string Right = "\x1b[C";
    public const string Left = "\x1b[D";

    /// <summary>The arrows with Shift, xterm's modifier 2 (CSI 1;2).</summary>
    public const string ShiftUp = "\x1b[1;2A";
    public const string ShiftDown = "\x1b[1;2B";
    public const string ShiftRight = "\x1b[1;2C";
    public const string ShiftLeft = "\x1b[1;2D";

    public const string CtrlC = "\x03";
    public const string CtrlD = "\x04";

    /// <summary>Ctrl+O, the detailed transcript view (SPEC.md D14: sent only to close that view should it be open).</summary>
    public const string CtrlO = "\x0f";

    /// <summary>Ctrl+X then Ctrl+S, send the queued messages now: two writes, in this order.</summary>
    public const string CtrlX = "\x18";
    public const string CtrlS = "\x13";
}
