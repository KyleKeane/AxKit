namespace AxKit.Core.Vt;

/// <summary>
/// What a terminal answers a program that asks for its colours (docs/axTerminal/SPEC.md AT-9.10): OSC 4 with a
/// colour's number and a question mark (the palette, "4;0;?;1;?" asks two), OSC 10 with a question mark (the text's
/// colour) and OSC 11 with one (the background). The answers are those of a plain black background and light text:
/// the palette of Windows' console (Campbell) with its black made plain black, the text light grey, the background
/// black; the numbers above 15 as xterm makes them (the colour cube and the greys). Each answer is xterm's form,
/// <c>ESC ] 4 ; n ; rgb:rrrr/gggg/bbbb BEL</c>, ended by BEL as the questions recorded of Windows' edit end. Every
/// other OSC, and a colour set rather than asked, has no answer. Pure: <see cref="Screen"/> raises them, and the
/// window writes them to the program's input.
/// </summary>
public static class QueryAnswers
{
    private const char Escape = '\u001b', Bel = '\u0007';

    /// <summary>The sixteen colours, red, green and blue: Windows' console palette, its black plain black.</summary>
    private static readonly int[] Palette =
    [
        0x000000, 0xC50F1F, 0x13A10E, 0xC19C00, 0x0037DA, 0x881798, 0x3A96DD, 0xCCCCCC,
        0x767676, 0xE74856, 0x16C60C, 0xF9F1A5, 0x3B78FF, 0xB4009E, 0x61D6D6, 0xF2F2F2,
    ];

    /// <summary>The text's colour (OSC 10): light grey, the palette's 7.</summary>
    public const int Foreground = 0xCCCCCC;

    /// <summary>The background (OSC 11): plain black.</summary>
    public const int Background = 0x000000;

    /// <summary>The answers to an OSC string (without its ESC ] and its end), in the order asked; none when it asks nothing.</summary>
    public static IReadOnlyList<string> To(string osc)
    {
        var parts = osc.Split(';');
        var answers = new List<string>();
        switch (parts[0])
        {
            case "4":
                // Pairs of a number and a colour; a pair whose colour is "?" asks.
                for (var i = 1; i + 1 < parts.Length; i += 2)
                {
                    if (parts[i + 1] == "?" && int.TryParse(parts[i], out var number) && number is >= 0 and <= 255)
                    {
                        answers.Add(Answer($"4;{number}", Colour(number)));
                    }
                }

                break;
            case "10" when parts is [_, "?"]:
                answers.Add(Answer("10", Foreground));
                break;
            case "11" when parts is [_, "?"]:
                answers.Add(Answer("11", Background));
                break;
        }

        return answers;
    }

    /// <summary>A colour of the 256 as red, green and blue: the sixteen, then xterm's cube of six levels and its 24 greys.</summary>
    public static int Colour(int number)
    {
        if (number < 16)
        {
            return Palette[number];
        }

        if (number < 232)
        {
            var cube = number - 16;
            int Level(int step) => step == 0 ? 0 : 55 + step * 40;
            return (Level(cube / 36) << 16) | (Level(cube / 6 % 6) << 8) | Level(cube % 6);
        }

        var grey = 8 + (number - 232) * 10;
        return (grey << 16) | (grey << 8) | grey;
    }

    /// <summary>xterm's form of an answer: each channel's byte twice, as four hexadecimal digits.</summary>
    private static string Answer(string what, int rgb)
    {
        string Channel(int shift) => ((rgb >> shift) & 0xFF).ToString("x2");
        var r = Channel(16);
        var g = Channel(8);
        var b = Channel(0);
        return $"{Escape}]{what};rgb:{r}{r}/{g}{g}/{b}{b}{Bel}";
    }
}
