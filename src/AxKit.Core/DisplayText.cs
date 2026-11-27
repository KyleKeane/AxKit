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
    public static string Visible(string text)
    {
        if (!Hides(text))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + 16);
        for (var i = 0; i < text.Length; i++)
        {
            var code = CodeAt(text, i, out var length);
            if (Hidden(code))
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

    /// <summary>
    /// One line as it is put into a control that shows a tab as a gap (a question's name, an answer): as
    /// <see cref="Visible"/>, a tab kept, since a command may hold one and it hides nothing.
    /// </summary>
    public static string VisibleLine(string line) =>
        line.Contains('\t', StringComparison.Ordinal) ? string.Join('\t', line.Split('\t').Select(Visible)) : Visible(line);

    /// <summary>Whether <paramref name="text"/> holds a character that <see cref="Visible"/> writes out.</summary>
    public static bool Hides(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (Hidden(CodeAt(text, i, out var length)))
            {
                return true;
            }

            i += length - 1;
        }

        return false;
    }

    /// <summary>The character at <paramref name="index"/> as its code point, and how many places of the text it takes: two for one written as a pair (the tag characters are).</summary>
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

    /// <summary>
    /// The characters that are not seen or heard themselves, by code point (written as numbers: such a character
    /// in this file would itself be unseen): the C0 and C1 controls; the soft hyphen (00AD); the Arabic letter mark
    /// (061C); the Mongolian vowel separator (180E); the zero-width characters and the direction marks (200B to
    /// 200F); the line and paragraph separators (2028, 2029), which break a line where none is written; the
    /// direction overrides (202A to 202E); the word joiner and the invisible operators (2060 to 2064); the direction
    /// isolates (2066 to 2069); the fillers that show as nothing (115F, 1160, 3164, FFA0); the byte order mark
    /// (FEFF); and the tag characters (E0000 to E007F), which can carry a whole text unseen. 00AD, 180E, 2028, 2029,
    /// 2061 to 2064, the fillers and the tags since the second review of 2026-11-27 (<c>DisplayTextTests</c>' table).
    /// </summary>
    private static bool Hidden(int code) =>
        code < 0x20 || (code >= 0x7F && code <= 0x9F)
        || code is 0x00AD or 0x061C or 0x115F or 0x1160 or 0x180E or 0x2028 or 0x2029 or 0x3164 or 0xFEFF or 0xFFA0
        || (code >= 0x200B && code <= 0x200F)
        || (code >= 0x202A && code <= 0x202E)
        || (code >= 0x2060 && code <= 0x2064)
        || (code >= 0x2066 && code <= 0x2069)
        || (code >= 0xE0000 && code <= 0xE007F);
}
