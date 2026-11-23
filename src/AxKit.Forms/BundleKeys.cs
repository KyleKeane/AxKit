namespace AxKit.Forms;

/// <summary>
/// The keys that mean the same in every window of axKit (docs/axKit/SPEC.md AX-8.2). Every window reads them from
/// here, so a change is one line; a window's own keys live in its own table (<c>AxDownKeys</c>, <c>AxClaudeKeys</c>),
/// and the collision test in <c>tests/AxKit.Tests</c> fails when a window key equals one of these. Tab and Shift+Tab,
/// Alt+F4, Alt and F10 are Windows' own and need no constant.
/// </summary>
public static class BundleKeys
{
    public const Keys Shortcuts = Keys.F1;

    /// <summary>The command palette (AX-10): every command of the window's menus by name.</summary>
    public const Keys CommandPalette = Keys.Control | Keys.Shift | Keys.P;

    /// <summary>A report to axKit's maintainer, a bug, an idea or feedback (docs/axKit/reports.md RP-2.1).</summary>
    public const Keys Report = Keys.F4;

    /// <summary>A question to Claude about this window, with what the window knows about itself (docs/axKit/ai-help.md AH-6); Windows' own key for help about the thing in front.</summary>
    public const Keys AskClaude = Keys.Shift | Keys.F1;

    /// <summary>
    /// A further key of the same command (AX-8.15, 2026-11-16): Ctrl+Slash, the in-window partner of the system-wide
    /// Ctrl+Shift+Slash, which brings up axHelp's conversation from anywhere (AX-8.11).
    /// </summary>
    public const Keys AskClaudeAlso = Keys.Control | Keys.OemQuestion;

    /// <summary>
    /// The context menu where the focus is (AX-8.15, 2026-11-16): whatever Shift+F10 and the Applications key do there,
    /// which stay. The window base runs the window's command on Shift+F10 for it (a list's Context menu), and makes
    /// Windows' own request for a context menu where no command takes that (<c>AxKitForm.ProcessCmdKey</c>).
    /// </summary>
    public const Keys ContextMenu = Keys.Control | Keys.OemPeriod;

    /// <summary>
    /// Say the status (AX-7.10, the user's key of 2026-11-15): the window's status bar as one announcement of axKit's
    /// own, so that the status never depends on NVDA finding the status bar (NVDA+End said "blank" over a full bar).
    /// </summary>
    public const Keys SayStatus = Keys.F8;

    /// <summary>
    /// The main action of where you are (AX-8.10 rule 3): a form sent from any of its fields, a long text kept from inside its box, a
    /// notice's main button, and in a window its one main action (axGIT's commit, axMCP's call, axFile's play the
    /// folder, a console window's send), which the window registers as a command on this key; the edge sound where
    /// there is none.
    /// </summary>
    public const Keys MainAction = Keys.Control | Keys.Return;

    /// <summary>
    /// The mode key, "the magic key" (AX-8.13, the user's decision of 2026-11-15): in an editable text of several lines,
    /// edit mode or navigation mode, said each time; in a text that cannot be edited, one line that says so; elsewhere
    /// the edge sound. The user may find it confusing in use: a change of key is this one line.
    /// </summary>
    public const Keys ToggleMode = Keys.Control | Keys.Space;

    public const Keys NextControl = Keys.Control | Keys.Tab;
    public const Keys PreviousControl = Keys.Control | Keys.Shift | Keys.Tab;
    public const Keys NextControlAlso = Keys.F6;

    public const Keys Find = Keys.Control | Keys.F;
    public const Keys FindNext = Keys.F3;
    public const Keys FindPrevious = Keys.Shift | Keys.F3;

    /// <summary>What the window holds: axClaude's conversation as a text file, axDown's file.</summary>
    public const Keys Save = Keys.Control | Keys.S;

    public const Keys LargerText = Keys.Control | Keys.Oemplus;
    public const Keys LargerTextNumpad = Keys.Control | Keys.Add;
    public const Keys SmallerText = Keys.Control | Keys.OemMinus;
    public const Keys SmallerTextNumpad = Keys.Control | Keys.Subtract;

    /// <summary>One screen in an edit control, and to its ends, through <see cref="EditKeys.Page"/>.</summary>
    public const Keys PageUp = Keys.PageUp;
    public const Keys PageDown = Keys.PageDown;

    /// <summary>The start of the previous or next paragraph in an edit control, and with Shift the selection grown to there, through <see cref="EditKeys.Paragraph"/>.</summary>
    public const Keys ParagraphUp = Keys.Control | Keys.Up;
    public const Keys ParagraphDown = Keys.Control | Keys.Down;
    public const Keys SelectParagraphUp = Keys.Control | Keys.Shift | Keys.Up;
    public const Keys SelectParagraphDown = Keys.Control | Keys.Shift | Keys.Down;

    /// <summary>Every key above with its name, for the collision test and the F1 texts.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Page up", PageUp),
        ("Page down", PageDown),
        ("Paragraph up", ParagraphUp),
        ("Paragraph down", ParagraphDown),
        ("Select to the paragraph above", SelectParagraphUp),
        ("Select to the paragraph below", SelectParagraphDown),
        ("Keyboard shortcuts", Shortcuts),
        ("Command palette", CommandPalette),
        ("Report", Report),
        ("Ask Claude about this window", AskClaude),
        ("Ask Claude about this window, as Shift+F1", AskClaudeAlso),
        ("The Context menu, as Shift+F10", ContextMenu),
        ("Say the status", SayStatus),
        ("The main action", MainAction),
        ("Edit mode or navigation mode", ToggleMode),
        ("Next control", NextControl),
        ("Previous control", PreviousControl),
        ("Next control (F6)", NextControlAlso),
        ("Find", Find),
        ("Find next", FindNext),
        ("Find previous", FindPrevious),
        ("Save", Save),
        ("Larger text", LargerText),
        ("Larger text (numpad)", LargerTextNumpad),
        ("Smaller text", SmallerText),
        ("Smaller text (numpad)", SmallerTextNumpad),
    ];

    /// <summary>
    /// The system-wide keys (AX-8.11): the background apps' hotkeys as they are at first, which Windows takes from
    /// every program while those apps run, axKit's windows among them, and the one kept for later. No table, the
    /// bundle's or a window's, may hold one (<c>KeyTests</c>): the key would never reach the window. The hotkeys
    /// themselves are settings (<c>AxKit.Core/HotkeySettings</c>); the test holds these against their defaults.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> SystemWide =
    [
        ("axStart's search field", Keys.Control | Keys.Shift | Keys.Space),
        ("axHelp's conversation", Keys.Control | Keys.Shift | Keys.OemQuestion),
        ("axHelp's screenshot", Keys.Control | Keys.Shift | Keys.Oemcomma),
        ("axClip's search field", Keys.Control | Keys.Shift | Keys.OemSemicolon),
        ("axTemplate's search field", Keys.Control | Keys.Shift | Keys.OemQuotes),
        ("Kept for a later help launcher", Keys.Control | Keys.Shift | Keys.OemPeriod),
    ];

    /// <summary>
    /// "Ctrl+Shift+S", "F3", "Ctrl+Plus", "Ctrl+Shift+Period", "Shift+Escape": a key as a menu shows it next to its item, from the tables.
    /// A key of the number row ends in a full stop ("Ctrl+1."): NVDA speaks the shortcut directly before the position
    /// of the item in the menu, and the synthesizer runs "Ctrl+1  1 of 9" together into "Ctrl+11 of 9" (AX-8.5); the
    /// full stop is a pause between the two.
    /// </summary>
    public static string Text(Keys key)
    {
        var parts = new List<string>();
        if (key.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (key.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        if (key.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        var code = key & Keys.KeyCode;
        parts.Add(code switch
        {
            Keys.Oemplus or Keys.Add => "Plus",
            Keys.OemMinus or Keys.Subtract => "Minus",
            Keys.OemPeriod => "Period",
            Keys.Oemcomma => "Comma",
            Keys.OemQuestion => "Slash",
            Keys.OemSemicolon => "Semicolon",
            Keys.OemQuotes => "Apostrophe",
            Keys.Back => "Backspace",
            // The enumeration gives Page Down its other name, Next, and Page Up as one word (found 2026-11-15 in the
            // command inventory, where axStart's and axClip's Page down read "Next").
            Keys.PageDown => "Page Down",
            Keys.PageUp => "Page Up",
            >= Keys.D0 and <= Keys.D9 => ((char)('0' + (code - Keys.D0))).ToString(),
            _ => code.ToString(),
        });
        var text = string.Join("+", parts);
        return code is >= Keys.D0 and <= Keys.D9 ? text + "." : text;
    }

    /// <summary>
    /// "ctrl+shift+s", "f3", "ctrl+1", "ctrl+enter", "ctrl+plus": a key as data, in the words a script presses it with
    /// (<see cref="AxKit.Core.KeyChord"/>, docs/axKit/user-workflows.md WF-2.2), for the window's list of its commands
    /// (<see cref="AxKitForm.CommandRows"/>). Empty for <see cref="Keys.None"/> and for a key the vocabulary has no
    /// name for; <see cref="Text"/> is what a person reads, this is what a program compares and presses.
    /// </summary>
    public static string Chord(Keys key) =>
        AxKit.Core.KeyChord.Text((int)(key & Keys.KeyCode), key.HasFlag(Keys.Control), key.HasFlag(Keys.Shift), key.HasFlag(Keys.Alt));
}
