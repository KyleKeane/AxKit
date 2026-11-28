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
    public static string Visible(string text) => Written(text, command: false);

    /// <summary>
    /// One line as it is put into a control that shows a tab as a gap (a question's name, an answer): as
    /// <see cref="Visible"/>, a tab kept, since a command may hold one and it hides nothing. A space that is not the
    /// plain one is left here, as in <see cref="Visible"/> (the fourth review of 2026-11-27 asked; the answer is no):
    /// a question's name and answers are sentences, Claude Code itself writes a no-break space after its bullets and
    /// its prompt's dollar sign, and French and Japanese text hold them as a matter of course; the one place where
    /// such a space decides what runs, the command a permission question names, is its label, which every answer
    /// carries and which is <see cref="VisibleCommand"/>'s.
    /// </summary>
    public static string VisibleLine(string line) =>
        line.Contains('\t', StringComparison.Ordinal) ? string.Join('\t', line.Split('\t').Select(Visible)) : Visible(line);

    /// <summary>
    /// A command offered to run, or the label of a permission question, as it is shown: a command is run, not read,
    /// so nothing in it is left unseen, an emoji's joiner and picture selector included (the fourth review of
    /// 2026-11-27: "echo added &gt;&gt; target.txt" with a joiner between its two signs showed as an append, and bash
    /// ran it as a truncation). Every character <see cref="Hidden"/> names, every U+200D, U+200C and variation
    /// selector, every tag character, and every space that is not the plain one (U+0020: a no-break space, an em
    /// space, the ideographic space, which look and read as a space and which a shell may take for part of a word)
    /// is written as its code. None of the exceptions of <see cref="Visible"/> holds here.
    /// </summary>
    public static string VisibleCommand(string text) => Written(text, command: true);

    /// <summary>Whether <paramref name="text"/> holds a character that <see cref="Visible"/> writes out.</summary>
    public static bool Hides(string text) => FirstWritten(text, command: false) >= 0;

    /// <summary>Whether <paramref name="text"/> holds a character that <see cref="VisibleCommand"/> writes out: one a command offered to run may not hold (axKit's McpOffer, the seventh review of 2026-11-27, K4).</summary>
    public static bool HidesInCommand(string text) => FirstWritten(text, command: true) >= 0;

    /// <summary>
    /// The text with every character <see cref="Hidden"/> names taken out: for comparing a text with what a program
    /// drew of it, since a program that shows a text may drop such characters (Claude Code: "Removed 1 invisible
    /// character from your prompt"; axClaude's prompt row check, the sixth review of 2026-11-27, F4).
    /// </summary>
    public static string WithoutHidden(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (!Hidden(code))
            {
                builder.Append(text, i, length);
            }

            i += length - 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Whether <paramref name="text"/> holds a character <see cref="WithoutHidden"/> takes out, a line end and a tab
    /// aside: the one test of "holds a hidden character" for comparing a text with what a program drew of it
    /// (axClaude's prompt row check and its second Enter, the eighth review of 2026-11-28, M1: until then every
    /// message of two lines counted as one that holds a hidden character).
    /// </summary>
    public static bool HoldsHidden(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (Hidden(code) && code is not ('\r' or '\n' or '\t'))
            {
                return true;
            }

            i += length - 1;
        }

        return false;
    }

    private static string Written(string text, bool command)
    {
        var first = FirstWritten(text, command);
        if (first < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + 16);
        builder.Append(text, 0, first);
        for (var i = first; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (WrittenOut(text, i, code, length, command))
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
    private static int FirstWritten(string text, bool command)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (WrittenOut(text, i, code, length, command))
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

    /// <summary>The variation selector that asks for a symbol's picture form, the joiner that makes one picture of two, and the non-joiner that keeps two letters apart.</summary>
    private const int PictureSelector = 0xFE0F, Joiner = 0x200D, NonJoiner = 0x200C;

    /// <summary>The keycap that encloses a digit, # or * with its picture selector (U+20E3), and the braille blank, which looks like a space and is a symbol (U+2800).</summary>
    private const int Keycap = 0x20E3, BrailleBlank = 0x2800;

    /// <summary>The tag characters (U+E0020 to U+E007E) and the one that ends a run of them (U+E007F).</summary>
    private const int TagFirst = 0xE0020, TagLast = 0xE007E, TagEnd = 0xE007F;

    /// <summary>
    /// Whether the character at <paramref name="index"/> is written as its code. In a command (<paramref name="command"/>)
    /// every character that is not seen is, with no exception, and every space but the plain one. Elsewhere, so that
    /// an emoji in an answer stays an emoji and a word of Persian stays a word, a few exceptions to
    /// <see cref="Hidden"/>, each between characters that make it seen and never next to ASCII or a sign of
    /// mathematics, money or a modifier (the fourth review of 2026-11-27: the exception was "between two symbols",
    /// and "&gt;", "|", "=" are symbols; a joiner between two "&gt;" hid an append): U+FE0F right after a picture
    /// (<see cref="Pictographic"/>) or as the middle of a keycap; U+200D between two pictures, the first of which may
    /// carry its U+FE0F or a skin tone; U+200C between two letters of a script that writes with it; the tag
    /// characters of the three flags Unicode recommends (England, Scotland, Wales); U+20E3 as the end of a keycap.
    /// And two that are seen yet hide what they stand on, in every text: the braille blank, and a combining mark
    /// whose cluster does not begin with a letter (<see cref="OnNoLetter"/>).
    /// </summary>
    private static bool WrittenOut(string text, int index, int code, int length, bool command)
    {
        // The plain characters of most text, without a look at the tables.
        if (code is >= 0x20 and < 0x7F)
        {
            return false;
        }

        if (command && (Hidden(code) || code == BrailleBlank || CharUnicodeInfo.GetUnicodeCategory(code) == UnicodeCategory.SpaceSeparator))
        {
            return true;
        }

        var after = index + length < text.Length ? CodeAt(text, index + length, out _) : -1;
        switch (code)
        {
            case PictureSelector:
                var before = CodeBefore(text, index, out _);
                return !(Pictographic(before) || (before is >= '0' and <= '9' or '#' or '*' && after == Keycap));
            case Joiner:
                return !(Pictographic(PictureBefore(text, index)) && Pictographic(after)) && !Virama(CodeBefore(text, index, out _));
            case NonJoiner:
                return !(JoinsLetters(CodeBefore(text, index, out _)) && JoinsLetters(after)) && !Virama(CodeBefore(text, index, out _));
            case >= TagFirst and <= TagEnd:
                return !InFlag(text, index);
            case BrailleBlank:
                return true;
            case Keycap when !command && CodeBefore(text, index, out var selector) == PictureSelector
                && CodeBefore(text, selector, out _) is >= '0' and <= '9' or '#' or '*':
                return false;
        }

        return Hidden(code) || OnNoLetter(text, index, code);
    }

    /// <summary>The picture a joiner at <paramref name="index"/> follows: the code before it, past a picture selector and a skin tone that the picture carries.</summary>
    private static int PictureBefore(string text, int index)
    {
        var before = CodeBefore(text, index, out var start);
        if (before == PictureSelector || before is >= 0x1F3FB and <= 0x1F3FF)
        {
            before = CodeBefore(text, start, out _);
        }

        return before;
    }

    /// <summary>
    /// A picture of Unicode's Extended_Pictographic property, from <see cref="PictureRanges"/>, that is seen itself
    /// and is no sign of mathematics, money or a modifier (the property holds the double arrow and the squares of
    /// class Sm, which stand in commands and formulas as signs): what an emoji's joiner and picture selector may
    /// stand beside. No ASCII character is one.
    /// </summary>
    private static bool Pictographic(int code)
    {
        if (code < 0xA9 || Hidden(code)
            || CharUnicodeInfo.GetUnicodeCategory(code) is UnicodeCategory.MathSymbol or UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol)
        {
            return false;
        }

        foreach (var (first, last) in PictureRanges)
        {
            if (code < first)
            {
                return false;
            }

            if (code <= last)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The code points of the property Extended_Pictographic, as ranges in order (the Unicode Character Database,
    /// emoji/emoji-data.txt, Unicode 15.1; written as numbers, the file's own lines). It includes code points not yet
    /// given a picture, which <see cref="Hidden"/> writes out as no character until they are.
    /// </summary>
    private static readonly (int First, int Last)[] PictureRanges =
    [
        (0x00A9, 0x00A9), (0x00AE, 0x00AE), (0x203C, 0x203C), (0x2049, 0x2049), (0x2122, 0x2122), (0x2139, 0x2139),
        (0x2194, 0x2199), (0x21A9, 0x21AA), (0x231A, 0x231B), (0x2328, 0x2328), (0x2388, 0x2388), (0x23CF, 0x23CF),
        (0x23E9, 0x23F3), (0x23F8, 0x23FA), (0x24C2, 0x24C2), (0x25AA, 0x25AB), (0x25B6, 0x25B6), (0x25C0, 0x25C0),
        (0x25FB, 0x25FE), (0x2600, 0x2605), (0x2607, 0x2612), (0x2614, 0x2685), (0x2690, 0x2705), (0x2708, 0x2712),
        (0x2714, 0x2714), (0x2716, 0x2716), (0x271D, 0x271D), (0x2721, 0x2721), (0x2728, 0x2728), (0x2733, 0x2734),
        (0x2744, 0x2744), (0x2747, 0x2747), (0x274C, 0x274C), (0x274E, 0x274E), (0x2753, 0x2755), (0x2757, 0x2757),
        (0x2763, 0x2767), (0x2795, 0x2797), (0x27A1, 0x27A1), (0x27B0, 0x27B0), (0x27BF, 0x27BF), (0x2934, 0x2935),
        (0x2B05, 0x2B07), (0x2B1B, 0x2B1C), (0x2B50, 0x2B50), (0x2B55, 0x2B55), (0x3030, 0x3030), (0x303D, 0x303D),
        (0x3297, 0x3297), (0x3299, 0x3299), (0x1F000, 0x1F0FF), (0x1F10D, 0x1F10F), (0x1F12F, 0x1F12F),
        (0x1F16C, 0x1F171), (0x1F17E, 0x1F17F), (0x1F18E, 0x1F18E), (0x1F191, 0x1F19A), (0x1F1AD, 0x1F1E5),
        (0x1F201, 0x1F20F), (0x1F21A, 0x1F21A), (0x1F22F, 0x1F22F), (0x1F232, 0x1F23A), (0x1F23C, 0x1F23F),
        (0x1F249, 0x1F3FA), (0x1F400, 0x1F53D), (0x1F546, 0x1F64F), (0x1F680, 0x1F6FF), (0x1F774, 0x1F77F),
        (0x1F7D5, 0x1F7FF), (0x1F80C, 0x1F80F), (0x1F848, 0x1F84F), (0x1F85A, 0x1F85F), (0x1F888, 0x1F88F),
        (0x1F8AE, 0x1F8FF), (0x1F90C, 0x1F93A), (0x1F93C, 0x1F945), (0x1F947, 0x1FAFF), (0x1FC00, 0x1FFFD),
    ];

    /// <summary>
    /// A letter or a mark of a script that writes with U+200C between its letters (Persian, Urdu, Kurdish in the
    /// Arabic script; Syriac; the Indic scripts from Devanagari to Sinhala), which a non-joiner between two of them
    /// changes the look of and so is seen.
    /// </summary>
    private static bool JoinsLetters(int code) =>
        code >= 0 && (code is >= 0x0600 and <= 0x074F or >= 0x0750 and <= 0x077F or >= 0x08A0 and <= 0x08FF
            or >= 0x0900 and <= 0x0DFF or >= 0xFB50 and <= 0xFDFF or >= 0xFE70 and <= 0xFEFF)
        && CharUnicodeInfo.GetUnicodeCategory(code) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.OtherLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;

    /// <summary>
    /// A virama, the sign that kills a consonant's vowel in the Indic scripts and their kin (canonical combining class
    /// 9 in the Unicode Character Database, UnicodeData.txt; written as numbers, Unicode 15.1). A joiner or non-joiner
    /// right after one asks for a half form or keeps a conjunct apart, and so is seen (the sixth review of
    /// 2026-11-27, L6: KA, VIRAMA, ZERO WIDTH JOINER, SSA lost its half form to a written-out code). Ordinary text only: a command writes every
    /// joiner out (<see cref="WrittenOut"/>).
    /// </summary>
    private static bool Virama(int code) =>
        code is 0x094D or 0x09CD or 0x0A4D or 0x0ACD or 0x0B4D or 0x0BCD or 0x0C4D or 0x0CCD or 0x0D3B or 0x0D3C or 0x0D4D
            or 0x0DCA or 0x0E3A or 0x0EBA or 0x0F84 or 0x1039 or 0x103A or 0x1714 or 0x1715 or 0x1734 or 0x17D2 or 0x1A60
            or 0x1B44 or 0x1BAA or 0x1BAB or 0x1BF2 or 0x1BF3 or 0x2D7F or 0xA806 or 0xA82C or 0xA8C4 or 0xA953 or 0xA9C0
            or 0xAAF6 or 0xABED or 0x10A3F or 0x11046 or 0x11070 or 0x1107F or 0x110B9 or 0x11133 or 0x11134 or 0x111C0
            or 0x11235 or 0x112EA or 0x1134D or 0x11442 or 0x114C2 or 0x115BF or 0x1163F or 0x116B6 or 0x1172B or 0x11839
            or 0x1193D or 0x1193E or 0x119E0 or 0x11A34 or 0x11A47 or 0x11A99 or 0x11C3F or 0x11D44 or 0x11D45 or 0x11D97
            or 0x11F41 or 0x11F42;

    /// <summary>The tag sequences Unicode recommends, the flags of England, Scotland and Wales (emoji-sequences.txt, RGI_Emoji_Tag_Sequence): a black flag, these letters as tag characters, and the end tag. Any other run of tags can carry a text unseen.</summary>
    private static readonly string[] FlagTags = ["gbeng", "gbsct", "gbwls"];

    /// <summary>Whether the tag character at <paramref name="index"/> is part of one of the <see cref="FlagTags"/> flags.</summary>
    private static bool InFlag(string text, int index)
    {
        // Back to the first tag of the run, which must follow the black flag (U+1F3F4).
        var start = index;
        while (CodeBefore(text, start, out var previous) is >= TagFirst and <= TagLast)
        {
            start = previous;
        }

        if (CodeBefore(text, start, out _) != 0x1F3F4)
        {
            return false;
        }

        var letters = new StringBuilder();
        for (var i = start; i < text.Length; i += 2)
        {
            var code = CodeAt(text, i, out _);
            if (code == TagEnd)
            {
                return index <= i && FlagTags.Contains(letters.ToString(), StringComparer.Ordinal);
            }

            if (code is < TagFirst or > TagLast || letters.Length == 5)
            {
                return false;
            }

            letters.Append((char)(code - 0xE0000));
        }

        return false;
    }

    /// <summary>
    /// A combining mark (Mn, Mc, Me) whose cluster does not begin with a letter (category L): at the start of the
    /// text, or on a sign, a digit, a space, a picture, a control or format character, the braille blank, past the
    /// other marks and variation selectors before it (the fifth review of 2026-11-27: "&gt;U+0338&gt;" looked like
    /// "&gt;&gt;" crossed out, or like an append, and bash ran it as a truncation into a file named by the mark). On
    /// a sign a mark makes it look like another sign, or like itself; a shell reads it as a word of its own. Marks on
    /// letters stay: a decomposed accent, the vowel signs of Devanagari and Arabic. The one exception, in ordinary
    /// text only, is a keycap's U+20E3 after a digit, # or * with its picture selector (<see cref="WrittenOut"/>).
    /// </summary>
    private static bool OnNoLetter(string text, int index, int code)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(code) is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
        {
            return false;
        }

        var at = index;
        int before;
        while (true)
        {
            before = CodeBefore(text, at, out var start);
            if (before < 0 || !(CharUnicodeInfo.GetUnicodeCategory(before) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
            {
                break;
            }

            at = start;
        }

        // Hidden first (the sixth review of 2026-11-27, L6): the Hangul fillers are letters by their category and are
        // seen as nothing, so a mark on one sat on nothing seen and stayed.
        return before < 0
            || Hidden(before)
            || CharUnicodeInfo.GetUnicodeCategory(before) is not (UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter);
    }

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
