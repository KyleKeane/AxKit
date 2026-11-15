namespace AxKit.Core;

/// <summary>Where the focus is while a key is tested (docs/axKit/SPEC.md AX-8.12): what kind of control takes the keys no command has.</summary>
public enum KeyTestFocus
{
    /// <summary>A control the window cannot say more about, or none.</summary>
    Other,

    /// <summary>A text field of several lines that can be typed in.</summary>
    Text,

    /// <summary>A text field of one line that can be typed in.</summary>
    OneLineText,

    /// <summary>A text that is only read.</summary>
    ReadOnlyText,

    /// <summary>A list.</summary>
    List,

    /// <summary>A check box.</summary>
    CheckBox,

    /// <summary>A button.</summary>
    Button,
}

/// <summary>
/// One key press of the key test: the virtual key code, the modifiers held, and the key as the bundle spells it
/// ("Ctrl+Shift+P", "F8", "Alt+Left"; <c>BundleKeys.Text</c> without the full stop of a number row key).
/// </summary>
public sealed record KeyTestPress(int Code, bool Ctrl, bool Shift, bool Alt, string Text);

/// <summary>
/// A key with what it does, for the key test: the key as the bundle spells it, the words said after it ("Say the
/// status", "opens the Help menu"), and whether it acts now (a command's <c>When</c>; a bundle key the window base
/// handles everywhere is true, one that only a window's command or the focused control gives a meaning is false).
/// </summary>
public sealed record KeyTestCommand(string Key, string Label, bool Applies = true);

/// <summary>
/// The key test's words (docs/axKit/SPEC.md AX-8.12, 2026-11-15, the user's ask after a function key never reached
/// the window): what one key does in a window, from the window's command list and the bundle's keys, and for a key
/// neither has, what the focused control makes of it, said plainly and only where it is known. Pure, so that it is
/// tested without a window; the window base (<c>AxKitForm.KeyTest.cs</c>) catches the keys and says the lines.
/// </summary>
public static class KeyTest
{
    /// <summary>Said as the test starts.</summary>
    public const string Started = "Key test. Press any key to hear its name and what it does here. Press Escape twice to end.";

    /// <summary>Said as the test ends, however it ends.</summary>
    public const string Ended = "Key test ended";

    /// <summary>Said after the first Escape's own line.</summary>
    public const string EscapeAgain = "Press Escape again to end the key test";

    /// <summary>What a key does that neither a command nor the focused control is known to take.</summary>
    public const string NoCommand = "no command of this window";

    /// <summary>Why an AI tool's command is not run while the test runs (docs/axKit/mcp.md MC-7.3).</summary>
    public const string AiToolsRefusal = "The window is in its key test: the user is trying keys, and nothing is carried out meanwhile. Commands run once the user has ended it.";

    private const int Back = 0x08, Tab = 0x09, Enter = 0x0D, Escape = 0x1B, Space = 0x20, PageUp = 0x21, PageDown = 0x22, End = 0x23,
        Home = 0x24, Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28, Delete = 0x2E, Apps = 0x5D, F4 = 0x73, F10 = 0x79;

    /// <summary>
    /// "F8: Say the status": the key, then what it does. In order: a bundle key the window base handles everywhere
    /// (<paramref name="bundle"/> entries that apply); the first of the window's <paramref name="commands"/> on the key
    /// that acts now, which is how a window's key takes a bundle key's place (Ctrl+Up by section in a list, by
    /// paragraph in a text); Windows' own keys for a window; <paramref name="own"/>, what the window says of a key its
    /// focused control handles itself; what a control of the <paramref name="focus"/>'s kind does with the key; and
    /// <see cref="NoCommand"/> where nothing is known.
    /// </summary>
    public static string Describe(KeyTestPress press, IReadOnlyList<KeyTestCommand> commands, IReadOnlyList<KeyTestCommand> bundle, KeyTestFocus focus, string? own = null)
    {
        var does = bundle.FirstOrDefault(key => key.Applies && Same(key.Key, press.Text))?.Label
            ?? commands.FirstOrDefault(key => key.Applies && Same(key.Key, press.Text))?.Label
            ?? OfWindows(press)
            ?? own
            ?? OfControl(press, focus, bundle)
            ?? NoCommand;
        return $"{press.Text}: {does}";
    }

    /// <summary>"Alt alone: opens the menu bar": a modifier, or several, pressed and released with no other key.</summary>
    public static string ModifierAlone(string modifiers) => modifiers switch
    {
        "Alt" => "Alt alone: opens the menu bar",
        "Windows" => "Windows key alone: opens the Start menu, Windows' own key",
        _ => $"{modifiers} alone: {NoCommand}",
    };

    private static bool Same(string key, string pressed) => string.Equals(key.TrimEnd('.'), pressed, StringComparison.OrdinalIgnoreCase);

    /// <summary>The keys Windows gives every window, which need no table (AX-8.2).</summary>
    private static string? OfWindows(KeyTestPress press) => press switch
    {
        { Code: F4, Alt: true, Ctrl: false, Shift: false } => "closes this window",
        { Code: Space, Alt: true, Ctrl: false, Shift: false } => "opens the window's own menu: restore, move, size, close",
        { Code: F10, Alt: false, Ctrl: false, Shift: false } => "opens the menu bar",
        { Code: F10, Alt: false, Ctrl: false, Shift: true } or { Code: Apps, Alt: false, Ctrl: false, Shift: false } => "opens the Context menu",
        { Code: Enter, Ctrl: true, Alt: false, Shift: false } => "the main action; there is none where you are",
        _ => null,
    };

    /// <summary>What the focused control does with a key no command has; null where that is not known.</summary>
    private static string? OfControl(KeyTestPress press, KeyTestFocus focus, IReadOnlyList<KeyTestCommand> bundle)
    {
        var plain = !press.Ctrl && !press.Alt;
        if (press.Code == Tab && plain)
        {
            return press.Shift ? "moves to the previous control" : "moves to the next control";
        }

        var text = focus is KeyTestFocus.Text or KeyTestFocus.OneLineText or KeyTestFocus.ReadOnlyText;
        var typed = focus is KeyTestFocus.Text or KeyTestFocus.OneLineText;
        var moves = press.Code is Left or Up or Right or Down or Home or End or PageUp or PageDown;
        if (text)
        {
            // The bundle's paragraph keys in a text of several lines (AX-8.2), by the bundle's own names.
            if (focus != KeyTestFocus.OneLineText && bundle.FirstOrDefault(key => !key.Applies && Same(key.Key, press.Text) && key.Label.Contains("aragraph", StringComparison.Ordinal)) is { } paragraph)
            {
                return paragraph.Label;
            }

            if (moves && !press.Alt)
            {
                return press.Shift ? "selects text" : "moves in the text";
            }

            if (press is { Ctrl: true, Alt: false, Shift: false })
            {
                var edit = (char)press.Code switch
                {
                    'A' => "selects all the text",
                    'C' => "copies the selected text",
                    'X' when typed => "cuts the selected text",
                    'V' when typed => "pastes",
                    'Z' when typed => "undoes the last change of the text",
                    _ => null,
                };
                if (edit is not null)
                {
                    return edit;
                }
            }

            if (typed && plain)
            {
                if (press.Code == Enter)
                {
                    return focus == KeyTestFocus.Text && !press.Shift ? "makes a new line" : null;
                }

                if (!press.Shift && press.Code is Back or Delete)
                {
                    return press.Code == Back ? "deletes the character before the caret" : "deletes the character after the caret";
                }

                if (Typed(press.Code) is { } what)
                {
                    return what;
                }
            }

            return null;
        }

        switch (focus)
        {
            case KeyTestFocus.List:
                if (moves && plain && !press.Shift)
                {
                    return "moves in the list";
                }

                // AX-8.6: every list finds by typed letters.
                return plain && press.Code != Space && Typed(press.Code) is not null ? "finds the next item that starts with what you type" : null;
            case KeyTestFocus.CheckBox:
                return press.Code == Space && plain && !press.Shift ? "checks or unchecks the box" : null;
            case KeyTestFocus.Button:
                return press.Code is Space or Enter && plain && !press.Shift ? "presses the button" : null;
            default:
                return moves && plain && !press.Shift ? "moves in the list or the text" : null;
        }
    }

    /// <summary>What a key types, when it types: a letter, a space, or another character (a digit, a sign, the number pad's).</summary>
    private static string? Typed(int code) => code switch
    {
        >= 'A' and <= 'Z' => "types the letter",
        Space => "types a space",
        >= '0' and <= '9' or (>= 0x60 and <= 0x6F) or (>= 0xBA and <= 0xC0) or (>= 0xDB and <= 0xDF) or 0xE2 => "types the character",
        _ => null,
    };

    /// <summary>Whether a virtual key code is a modifier's, either side: Shift, Ctrl, Alt or the Windows key.</summary>
    public static bool IsModifier(int code) => code is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or (>= 0xA0 and <= 0xA5);

    /// <summary>Whether a virtual key code is the Windows key's.</summary>
    public static bool IsWindowsKey(int code) => code is 0x5B or 0x5C;

    /// <summary>Whether the press is Escape with nothing held.</summary>
    public static bool IsEscape(KeyTestPress press) => press is { Code: Escape, Ctrl: false, Shift: false, Alt: false };
}

/// <summary>
/// One run of the key test (AX-8.12): what is said for each key that goes down or comes up, and when the test ends.
/// It holds the two things a single press does not show: that the key before was Escape, and that a modifier went
/// down with no other key since. The window hands it each press once, never a repeat of a held key.
/// </summary>
public sealed class KeyTestSession
{
    private bool _escapeBefore;
    private string? _modifiersAlone;

    /// <summary>
    /// A key went down. A modifier says nothing yet (<paramref name="modifiers"/> names the ones held now, "Ctrl+Shift");
    /// any other key says its line, <paramref name="line"/>; Escape says its line and how to end, and a second Escape
    /// right after it ends the test.
    /// </summary>
    public (string? Said, bool Ends) Down(KeyTestPress press, string modifiers, Func<string> line)
    {
        if (KeyTest.IsModifier(press.Code))
        {
            _modifiersAlone = modifiers;
            _escapeBefore = false;
            return (null, false);
        }

        _modifiersAlone = null;
        if (KeyTest.IsEscape(press))
        {
            if (_escapeBefore)
            {
                return (KeyTest.Ended, true);
            }

            _escapeBefore = true;
            return ($"{line()}. {KeyTest.EscapeAgain}", false);
        }

        _escapeBefore = false;
        return (line(), false);
    }

    /// <summary>A key came up: a modifier released with no other key pressed since it went down is named once, "Alt alone: opens the menu bar"; anything else says nothing.</summary>
    public string? Up(int code)
    {
        if (!KeyTest.IsModifier(code) || _modifiersAlone is not { Length: > 0 } alone)
        {
            return null;
        }

        _modifiersAlone = null;
        return KeyTest.ModifierAlone(alone);
    }
}
