namespace AxTerminal.Core;

/// <summary>
/// A key pressed while the screen view sends the keys to a full-screen program (SPEC.md AT-9.3), as a terminal sends
/// it: the VT sequences xterm writes for the arrows, the editing keys and the function keys, with the modifiers as
/// xterm's parameter (1 plus 1 for Shift, 2 for Alt, 4 for Ctrl); Ctrl with a letter as its control character; Alt
/// with a letter or a digit as Escape and the character. The pseudo console turns these back into the keys the
/// program reads. A key that types a character is not here: its character comes as the keyboard makes it and goes as
/// it is (<see cref="Character"/>). The key codes are Windows' virtual keys, the numbers of WinForms' <c>Keys</c>.
/// Root CLAUDE.md rule 12: the window writes no sequence of its own; Escape is <see cref="ShellTexts.Escape"/>.
/// </summary>
public static class ScreenKeys
{
    private const int Back = 0x08, Tab = 0x09, Enter = 0x0D, EscapeKey = 0x1B, Space = 0x20;
    private const int PageUp = 0x21, PageDown = 0x22, End = 0x23, Home = 0x24, Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28;
    private const int Insert = 0x2D, Delete = 0x2E, D0 = 0x30, D9 = 0x39, A = 0x41, Z = 0x5A, F1 = 0x70, F12 = 0x7B;

    /// <summary>The control sequence introducer, Escape and a bracket.</summary>
    private static readonly string Csi = ShellTexts.Escape + "[";

    /// <summary>The numbers of F5 to F12 in their tilde sequences, as xterm sends them.</summary>
    private static readonly int[] FunctionNumbers = [15, 17, 18, 19, 20, 21, 23, 24];

    /// <summary>What the key sends to the program; null for a key whose character the keyboard gives (a letter, a digit, a sign) or one a terminal sends nothing for (a modifier alone).</summary>
    public static string? Sequence(int key, bool control, bool shift, bool alt)
    {
        var modifier = 1 + (shift ? 1 : 0) + (alt ? 2 : 0) + (control ? 4 : 0);
        switch (key)
        {
            case Up or Down or Right or Left or Home or End:
                var letter = key switch { Up => 'A', Down => 'B', Right => 'C', Left => 'D', Home => 'H', _ => 'F' };
                return modifier == 1 ? Csi + letter : $"{Csi}1;{modifier}{letter}";
            case Insert or Delete or PageUp or PageDown:
                var number = key switch { Insert => 2, Delete => 3, PageUp => 5, _ => 6 };
                return modifier == 1 ? $"{Csi}{number}~" : $"{Csi}{number};{modifier}~";
            case >= F1 and <= F12:
                var index = key - F1;
                if (index < 4)
                {
                    var final = (char)('P' + index);
                    return modifier == 1 ? ShellTexts.Escape + "O" + final : $"{Csi}1;{modifier}{final}";
                }

                var tilde = FunctionNumbers[index - 4];
                return modifier == 1 ? $"{Csi}{tilde}~" : $"{Csi}{tilde};{modifier}~";
            case Enter:
                return (alt ? ShellTexts.Escape : string.Empty) + ShellTexts.Enter;
            case Tab:
                return shift ? Csi + "Z" : "\t";
            case Back:
                return (alt ? ShellTexts.Escape : string.Empty) + (control ? "\b" : ((char)0x7F).ToString());
            case EscapeKey:
                return alt ? ShellTexts.Escape + ShellTexts.Escape : ShellTexts.Escape;
            case Space when control && !alt:
                return ((char)0).ToString();
            case Space when alt && !control:
                return ShellTexts.Escape + " ";
            case >= A and <= Z when control && !alt:
                return ((char)(key - A + 1)).ToString();
            case >= A and <= Z when alt && !control:
                return ShellTexts.Escape + (shift ? (char)key : char.ToLowerInvariant((char)key));
            case >= D0 and <= D9 when alt && !control && !shift:
                return ShellTexts.Escape + (char)key;
            default:
                // Ctrl+Alt is AltGr on many keyboards: the character it makes comes as typed.
                return null;
        }
    }

    /// <summary>A character the keyboard typed, as it goes to the program; with Alt held (a sign the keys above do not name), after Escape.</summary>
    public static string Character(string typed, bool alt) => alt ? ShellTexts.Escape + typed : typed;
}
