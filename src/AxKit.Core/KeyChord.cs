using System.Globalization;

namespace AxKit.Core;

/// <summary>
/// The one chord vocabulary (docs/axKit/user-workflows.md WF-2.2): the names a key press may be written with, each with
/// its virtual key code. A chord is its modifiers in the order ctrl, shift, alt and one key's name, joined by "+", in
/// lower case: "ctrl+shift+w", "f3", "ctrl+1", "ctrl+enter". The automation channel's <c>keys</c> reads a key's code
/// here (<c>AxKit.Forms/AutomationChannel</c>), and the window writes each command's key with these names when it lists
/// its commands as data (<c>AxKit.Forms/BundleKeys.Chord</c>, <see cref="CommandListing"/>), so that what a window
/// lists is what a script can press. A key without a name here has no chord. The modifiers a parser takes beyond
/// ctrl, shift and alt (the NVDA tier's <c>nvda</c>) are that parser's own.
/// </summary>
public static class KeyChord
{
    /// <summary>
    /// Every name but the letters, the digits and f1 to f12, with its code; a code's first name is the one written
    /// (<see cref="Name"/>). The names and codes are the ones the channel's and the NVDA tier's own tables held until
    /// 2026-11-15, and four more that the key tables use: slash, semicolon, and the numeric keypad's add and subtract.
    /// </summary>
    private static readonly (string Name, int Code)[] Named =
    [
        ("enter", 0x0D),
        ("escape", 0x1B),
        ("esc", 0x1B),
        ("tab", 0x09),
        ("space", 0x20),
        ("backspace", 0x08),
        ("delete", 0x2E),
        ("insert", 0x2D),
        ("up", 0x26),
        ("down", 0x28),
        ("left", 0x25),
        ("right", 0x27),
        ("home", 0x24),
        ("end", 0x23),
        ("pageup", 0x21),
        ("pagedown", 0x22),
        ("plus", 0xBB),
        ("minus", 0xBD),
        ("period", 0xBE),
        ("comma", 0xBC),
        ("apps", 0x5D),
        ("slash", 0xBF),
        ("semicolon", 0xBA),
        ("apostrophe", 0xDE),
        ("add", 0x6B),
        ("subtract", 0x6D),
    ];

    private const int F1 = 0x70;
    private const int F12 = 0x7B;

    /// <summary>The code of a key's name, in any case: "enter" 0x0D, "f3" 0x72, "a" 0x41, "1" 0x31; null for a name the vocabulary does not hold.</summary>
    public static int? Code(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Length == 1 && lower[0] is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
        {
            // A letter's code is its capital's; a digit's is the digit's own.
            return char.ToUpperInvariant(lower[0]);
        }

        if (lower.Length is 2 or 3 && lower[0] == 'f' && lower[1] is >= '1' and <= '9'
            && int.TryParse(lower.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number <= 12)
        {
            return F1 + number - 1;
        }

        foreach (var (known, code) in Named)
        {
            if (known == lower)
            {
                return code;
            }
        }

        return null;
    }

    /// <summary>The name a code is written with, the first of its names ("escape", never "esc"); null for a code without a name.</summary>
    public static string? Name(int code)
    {
        if (code is >= 'A' and <= 'Z')
        {
            return char.ToLowerInvariant((char)code).ToString();
        }

        if (code is >= '0' and <= '9')
        {
            return ((char)code).ToString();
        }

        if (code is >= F1 and <= F12)
        {
            return "f" + (code - F1 + 1).ToString(CultureInfo.InvariantCulture);
        }

        foreach (var (name, known) in Named)
        {
            if (known == code)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>A key with its modifiers as a chord, "ctrl+shift+w"; empty when the code has no name.</summary>
    public static string Text(int code, bool ctrl, bool shift, bool alt)
    {
        if (Name(code) is not { } name)
        {
            return string.Empty;
        }

        return (ctrl ? "ctrl+" : string.Empty) + (shift ? "shift+" : string.Empty) + (alt ? "alt+" : string.Empty) + name;
    }

    /// <summary>A press put in the one order and case, "Shift+Ctrl+P" as "ctrl+shift+p" and "esc" as "escape"; the press as given when a part of it is unknown.</summary>
    public static string Normal(string press) =>
        Parse(press) is { } parsed ? Text(parsed.Code, parsed.Ctrl, parsed.Shift, parsed.Alt) : press;

    /// <summary>
    /// Whether a press is the kind a window's command has: it carries ctrl or alt, or its key is f1 to f12. Enter, Tab,
    /// the arrows and a letter alone, with or without shift, are a control's own keys far more often than a command's.
    /// False for a press with an unknown part.
    /// </summary>
    public static bool IsCommandChord(string press) =>
        Parse(press) is { } parsed && (parsed.Ctrl || parsed.Alt || parsed.Code is >= F1 and <= F12);

    /// <summary>A press as its key's code and its modifiers; null when a modifier is not ctrl, shift or alt, or the key has no name.</summary>
    private static (int Code, bool Ctrl, bool Shift, bool Alt)? Parse(string press)
    {
        var parts = press.Trim().Split('+');
        var (ctrl, shift, alt) = (false, false, false);
        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                    ctrl = true;
                    break;
                case "shift":
                    shift = true;
                    break;
                case "alt":
                    alt = true;
                    break;
                default:
                    return null;
            }
        }

        return Code(parts[^1]) is { } code ? (code, ctrl, shift, alt) : null;
    }
}
