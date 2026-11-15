namespace AxKit.Core;

/// <summary>What a key does in a text's navigation mode when it is no jump letter (docs/axKit/SPEC.md AX-8.13).</summary>
public enum NavigationKey
{
    /// <summary>The key goes on as in edit mode: a move, a selection, a copy, a command of the window, a menu.</summary>
    Passes,

    /// <summary>The key would change the text: it is swallowed with the edge sound, and nothing changes.</summary>
    Refused,
}

/// <summary>
/// The two modes of an editable text (docs/axKit/SPEC.md AX-8.13, the user's decision of 2026-11-15): edit mode, where
/// keys type as in any text, and navigation mode, where the plain letters jump by the text's structure as the
/// conversation's quick keys do in axClaude and nothing changes the text. The bundle's mode key toggles them, and in a
/// text that cannot be edited it says <see cref="ReadOnly"/>. The words and the rule of which keys are refused are
/// here, pure and tested (<c>TextModesTests</c>); the jump letters are each window's (<c>AxKit.Forms/TextJump.Letters</c>),
/// the control and the window base apply the rule (<c>AxKit.Forms/AxKitTextBox</c>, <c>AxKitForm.ProcessCmdKey</c>).
/// </summary>
public static class TextModes
{
    /// <summary>Said when the mode key turns navigation mode off.</summary>
    public const string EditMode = "Edit mode";

    /// <summary>Said when the mode key turns navigation mode on.</summary>
    public const string NavigationMode = "Navigation mode";

    /// <summary>Said when the mode key is pressed in a text that cannot be edited at all (axClaude's conversation, axTerminal's terminal, axPreview's text, a notice's text); nothing changes.</summary>
    public const string ReadOnly = "Read only: there is no edit mode here";

    /// <summary>What the status bar adds while the focused text is in navigation mode, where the window has a status bar.</summary>
    public const string StatusPart = "navigation mode";

    /// <summary>The key test's words for a key navigation mode refuses (AX-8.12).</summary>
    public const string KeyTestRefused = "changes nothing in navigation mode";

    /// <summary>What the mode key says after it toggled: the new mode.</summary>
    public static string Toggled(bool navigation) => navigation ? NavigationMode : EditMode;

    /// <summary>
    /// What a menu command that would change the text says in navigation mode (Undo, Redo, Cut, Paste, axJSON's
    /// Format): "Navigation mode: press Ctrl+Space to edit", with the mode key as the bundle spells it. A key that
    /// would change the text gets the edge sound instead (<see cref="InNavigation"/>); a menu choice is said.
    /// </summary>
    public static string Refused(string modeKey) => $"Navigation mode: press {modeKey} to edit";

    /// <summary>The key test's words for a jump letter in navigation mode: "moves to the next heading".</summary>
    public static string KeyTestJump(string name, bool previous) => $"moves to the {(previous ? "previous" : "next")} {name}";

    private const int Back = 0x08, Enter = 0x0D, Space = 0x20, Insert = 0x2D, Delete = 0x2E;

    /// <summary>
    /// What a key that is no jump letter does in navigation mode, from its virtual key code and the modifiers held.
    /// Refused: every key that types a character (a letter, a digit, a sign, Space, the number pad, Enter, Tab aside),
    /// Backspace and Delete with or without Ctrl, Alt+Backspace, Ctrl+V, Ctrl+X, Ctrl+Z, Ctrl+Y and with Shift,
    /// Shift+Insert and Shift+Delete, and Ctrl+Enter, the main action, which would send what the field holds. Passes:
    /// the arrows, Home, End, Page Up and Page Down with any modifier (they move and select), Ctrl+C, Ctrl+Insert and
    /// Ctrl+A, Escape, Tab, the function keys, Alt for the menus, every other Ctrl combination (the window's commands,
    /// axDown's Ctrl+letter jumps among them) and Ctrl+Alt (read to the end in a console window). A character that
    /// still reaches the control (Tab in an editor, a character a program sends, a character Ctrl+Alt makes on some
    /// keyboards) is stopped there by <see cref="CharacterAllowed"/>.
    /// </summary>
    public static NavigationKey InNavigation(int code, bool ctrl, bool shift, bool alt)
    {
        if (ctrl && alt)
        {
            return NavigationKey.Passes;
        }

        if (alt)
        {
            return code == Back ? NavigationKey.Refused : NavigationKey.Passes;
        }

        if (ctrl)
        {
            return code is 'V' or 'X' or 'Z' or 'Y' or Back or Delete or Enter ? NavigationKey.Refused : NavigationKey.Passes;
        }

        if (code is Back or Delete or Enter || (shift && code == Insert) || Types(code))
        {
            return NavigationKey.Refused;
        }

        return NavigationKey.Passes;
    }

    /// <summary>
    /// Whether a character message may reach the edit control in navigation mode: only Ctrl+C's and Ctrl+A's control
    /// characters, which copy and select. Every other character, a typed one, a line break, a tab or Backspace's, would
    /// change the text.
    /// </summary>
    public static bool CharacterAllowed(char character) => character is '\u0001' or '\u0003';

    /// <summary>Whether a key types a character with no Ctrl or Alt held: a letter, a digit, Space, the number pad, a sign of the main keyboard.</summary>
    private static bool Types(int code) => code is >= 'A' and <= 'Z' or >= '0' and <= '9' or Space or (>= 0x60 and <= 0x6F) or (>= 0xBA and <= 0xC0) or (>= 0xDB and <= 0xDF) or 0xE2;
}
