using System.Globalization;
using System.Text;

namespace AxKit.Core;

/// <summary>
/// Text from outside shown as a list's line (security audit S-42): a file's name, a commit's subject, an item's title.
/// Characters that change how the rest looks without being seen themselves (the bidirectional overrides and isolates
/// that can show "invoice‮txt.exe" as "invoiceexe.txt", zero-width characters, control characters) are written as
/// "‹U+202E›", so that the eye and braille see what NVDA reads; anything else is left as it is. Pure, tested.
/// The one place that says which characters these are (<see cref="Hidden"/>): what a program puts in front of the
/// user to decide on (a question's text, a command offered) goes through here before it is shown, so that nothing
/// stands in it unseen and unheard.
/// </summary>
public static class DisplayText
{
    public static string Visible(string text) => Written(text, spaces: false);

    /// <summary>
    /// One line as it is put into a control that shows a tab as a gap (a question's name, an answer): as
    /// <see cref="Visible"/>, a tab kept, since a command may hold one and it hides nothing.
    /// </summary>
    public static string VisibleLine(string line) =>
        line.Contains('\t', StringComparison.Ordinal) ? string.Join('\t', line.Split('\t').Select(Visible)) : Visible(line);

    /// <summary>
    /// A command offered to run, or the label of a permission question, as it is shown: as <see cref="Visible"/>, and
    /// every space that is not the plain one (U+0020) written as its code too: a no-break space, an em space, the
    /// ideographic space. They look like a space and are read as one, and a shell may take them for part of a word
    /// or for a blank between two: where the user decides whether a command runs, they see which it is.
    /// </summary>
    public static string VisibleCommand(string text) => Written(text, spaces: true);

    /// <summary>Whether <paramref name="text"/> holds a character that <see cref="Visible"/> writes out.</summary>
    public static bool Hides(string text) => FirstWritten(text, spaces: false) >= 0;

    private static string Written(string text, bool spaces)
    {
        var first = FirstWritten(text, spaces);
        if (first < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + 16);
        builder.Append(text, 0, first);
        for (var i = first; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (WrittenOut(text, i, code, length, spaces))
            {
                builder.Append($"‹U+{code:X4}›");
            }
            else
            {
                builder.Append(text, i, length);
            }

            i += length - 1;
        }

        return builder.ToString();
    }

    /// <summary>The place of the first character that is written out, or -1 when the text is shown as it is.</summary>
    private static int FirstWritten(string text, bool spaces)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (WrittenOut(text, i, code, length, spaces))
            {
                return i;
            }

            i += length - 1;
        }

        return -1;
    }

    /// <summary>The character at <paramref name="index"/> as its code point, and how many places of the text it takes: two for one written as a pair (the tag characters are); half a pair alone is its own code.</summary>
    private static int CodeAt(string text, int index, out int length)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(text[index], text[index + 1]);
        }

        length = 1;
        return text[index];
    }

    /// <summary>The code point that ends right before <paramref name="index"/>, and where it begins; -1 at the start of the text.</summary>
    private static int CodeBefore(string text, int index, out int start)
    {
        if (index >= 2 && char.IsLowSurrogate(text[index - 1]) && char.IsHighSurrogate(text[index - 2]))
        {
            start = index - 2;
            return char.ConvertToUtf32(text[index - 2], text[index - 1]);
        }

        start = index - 1;
        return index >= 1 ? text[index - 1] : -1;
    }

    /// <summary>The variation selector that asks for a symbol's picture form, and the joiner that makes one picture of two.</summary>
    private const int PictureSelector = 0xFE0F, Joiner = 0x200D;

    /// <summary>
    /// Whether the character at <paramref name="index"/> is written as its code. The one exception to
    /// <see cref="Hidden"/>, so that an emoji in an answer stays an emoji: U+FE0F passes right after a symbol, and
    /// U+200D passes between two symbols, the first of which may carry its U+FE0F; anywhere else both are written
    /// out like every other character that is not seen.
    /// </summary>
    private static bool WrittenOut(string text, int index, int code, int length, bool spaces)
    {
        // The plain characters of most text, without a look at the tables.
        if (code is >= 0x20 and < 0x7F)
        {
            return false;
        }

        if (code == PictureSelector)
        {
            return !IsSymbol(CodeBefore(text, index, out _));
        }

        if (code == Joiner)
        {
            var before = CodeBefore(text, index, out var start);
            if (before == PictureSelector)
            {
                before = CodeBefore(text, start, out _);
            }

            var after = index + length < text.Length ? CodeAt(text, index + length, out _) : -1;
            return !(IsSymbol(before) && IsSymbol(after));
        }

        return Hidden(code) || (spaces && CharUnicodeInfo.GetUnicodeCategory(code) == UnicodeCategory.SpaceSeparator);
    }

    /// <summary>A symbol by Unicode's own classes (So, Sm, Sc, Sk): the emoji, the arrows and signs that have a picture form, and the skin tones.</summary>
    private static bool IsSymbol(int code) =>
        code >= 0 && CharUnicodeInfo.GetUnicodeCategory(code) is UnicodeCategory.OtherSymbol or UnicodeCategory.MathSymbol
            or UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol;

    /// <summary>
    /// The characters that are not seen or heard themselves, by Unicode's own classes in place of a list of them
    /// (the third review of 2026-11-27; until then a table of code points, which each review found short): every
    /// code point whose general category is Format (the direction marks, overrides and isolates, the zero-width
    /// characters, the soft hyphen, the byte order mark, the tag characters, which can carry a whole text unseen),
    /// Control, Surrogate (half of a pair, alone), PrivateUse or OtherNotAssigned (a code point that is no character
    /// in the Unicode version .NET knows), and the line and paragraph separators, which break a line where none is
    /// written. And the code points Unicode calls default ignorable that have another category
    /// (<see cref="DefaultIgnorable"/>). The categories are .NET's own tables, which need no data of the system (the
    /// executable runs with invariant globalization).
    /// </summary>
    private static bool Hidden(int code) =>
        CharUnicodeInfo.GetUnicodeCategory(code) is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
        || DefaultIgnorable(code);

    /// <summary>
    /// The default ignorable code points whose general category is a letter's or a mark's, so that
    /// <see cref="Hidden"/>'s classes do not hold them (the Unicode Character Database, DerivedCoreProperties.txt,
    /// Default_Ignorable_Code_Point; written as numbers, since such a character in this file would itself be
    /// unseen): the combining grapheme joiner (034F); the Hangul fillers (115F, 1160, 3164, FFA0); the Khmer
    /// inherent vowels (17B4, 17B5); the Mongolian free variation selectors (180B to 180D, 180F); and the variation
    /// selectors (FE00 to FE0F, E0100 to E01EF).
    /// </summary>
    private static bool DefaultIgnorable(int code) =>
        code is 0x034F or 0x115F or 0x1160 or 0x17B4 or 0x17B5 or 0x180B or 0x180C or 0x180D or 0x180F or 0x3164 or 0xFFA0
        || (code >= 0xFE00 && code <= 0xFE0F)
        || (code >= 0xE0100 && code <= 0xE01EF);
}
