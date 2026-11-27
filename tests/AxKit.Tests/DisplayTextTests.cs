using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Text from outside shown as a list's line (security audit S-42).</summary>
public sealed class DisplayTextTests
{
    [Fact]
    public void A_hidden_override_is_made_visible_and_plain_text_is_left()
    {
        Assert.Equal("invoice‹U+202E›txt.exe", DisplayText.Visible("invoice‮txt.exe"));
        Assert.Equal("a‹U+001B›[31mb", DisplayText.Visible("a\u001b[31mb"));
        Assert.Equal("Café, 日本語, notes.md", DisplayText.Visible("Café, 日本語, notes.md"));
    }

    /// <summary>
    /// What is written out, by code point (numbers, so that no such character stands unseen in this file). The rule
    /// is by Unicode's own classes since the third review of 2026-11-27 (until then a list, which each review found
    /// short): the first block here are the characters the list had; the second are those it did not have and the
    /// classes do: the other default ignorable code points (the combining grapheme joiner, the Khmer inherent
    /// vowels, the Mongolian and the plain variation selectors), a format character outside the first plane, a code
    /// point for private use, and code points that are no character at all.
    /// </summary>
    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x0009)]
    [InlineData(0x001B)]
    [InlineData(0x007F)]
    [InlineData(0x0085)]
    [InlineData(0x009F)]
    [InlineData(0x061C)]
    [InlineData(0x200B)]
    [InlineData(0x200C)]
    [InlineData(0x200D)]
    [InlineData(0x200E)]
    [InlineData(0x200F)]
    [InlineData(0x202A)]
    [InlineData(0x202E)]
    [InlineData(0x2060)]
    [InlineData(0x2066)]
    [InlineData(0x2069)]
    [InlineData(0xFEFF)]
    [InlineData(0x00AD)]
    [InlineData(0x180E)]
    [InlineData(0x2028)]
    [InlineData(0x2029)]
    [InlineData(0x2061)]
    [InlineData(0x2062)]
    [InlineData(0x2063)]
    [InlineData(0x2064)]
    [InlineData(0x3164)]
    [InlineData(0x115F)]
    [InlineData(0x1160)]
    [InlineData(0xFFA0)]
    [InlineData(0xE0000)]
    [InlineData(0xE0001)]
    [InlineData(0xE0041)]
    [InlineData(0xE007F)]
    [InlineData(0x034F)]
    [InlineData(0x17B4)]
    [InlineData(0x17B5)]
    [InlineData(0x180B)]
    [InlineData(0x180D)]
    [InlineData(0x180F)]
    [InlineData(0xFE00)]
    [InlineData(0xFE0E)]
    [InlineData(0xFE0F)]
    [InlineData(0xE0100)]
    [InlineData(0xE01EF)]
    [InlineData(0x1BCA0)]
    [InlineData(0x1D173)]
    [InlineData(0xE000)]
    [InlineData(0xF8FF)]
    [InlineData(0xF0000)]
    [InlineData(0x2065)]
    [InlineData(0xFFF0)]
    [InlineData(0xE0080)]
    [InlineData(0xDFFFF)]
    public void A_character_that_is_neither_seen_nor_heard_is_written_out_as_its_code(int code)
    {
        var hidden = char.ConvertFromUtf32(code);
        Assert.True(DisplayText.Hides("rm " + hidden + "notes"));
        Assert.Equal($"rm ‹U+{code:X4}›notes", DisplayText.Visible("rm " + hidden + "notes"));
    }

    [Theory]
    [InlineData(0x0020)]
    [InlineData(0x007E)]
    [InlineData(0x00A0)]
    [InlineData(0x00E9)]
    [InlineData(0x0301)]
    [InlineData(0x2010)]
    [InlineData(0x2027)]
    [InlineData(0x202F)]
    [InlineData(0x3000)]
    [InlineData(0x3163)]
    [InlineData(0x20AC)]
    [InlineData(0x1F600)]
    public void A_character_that_is_seen_or_changes_nothing_unseen_is_left(int code)
    {
        var plain = "rm " + char.ConvertFromUtf32(code) + "notes";
        Assert.False(DisplayText.Hides(plain));
        Assert.Same(plain, DisplayText.Visible(plain));
    }

    [Fact]
    public void A_line_for_a_control_keeps_its_tabs_and_nothing_else_unseen()
    {
        var mark = char.ConvertFromUtf32(0x202E);
        Assert.Equal("a\tb", DisplayText.VisibleLine("a\tb"));
        Assert.Equal("a\t‹U+202E›b", DisplayText.VisibleLine("a\t" + mark + "b"));
        Assert.Equal("a‹U+0009›b", DisplayText.Visible("a\tb"));
        Assert.Equal("git status", DisplayText.VisibleLine("git status"));
    }

    [Fact]
    public void Half_of_a_pair_is_written_out_as_its_own_code()
    {
        // A lone surrogate is no character: it is written as the half it is, and is not taken for the tag it could have begun.
        Assert.Equal("a‹U+DB40›", DisplayText.Visible("a" + (char)0xDB40));
        Assert.Equal("‹U+DC00›b", DisplayText.Visible((char)0xDC00 + "b"));
    }

    /// <summary>
    /// The one exception, so that an emoji in an answer stays an emoji: U+FE0F passes right after a symbol, and
    /// U+200D passes between two symbols, the first of which may carry its U+FE0F. Written as code points: a heart,
    /// a flag made of two symbols, a family of three, a person with a sign and its picture selector.
    /// </summary>
    [Theory]
    [InlineData(new[] { 0x2764, 0xFE0F })]
    [InlineData(new[] { 0x1F3F3, 0xFE0F, 0x200D, 0x1F308 })]
    [InlineData(new[] { 0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467 })]
    [InlineData(new[] { 0x1F937, 0x200D, 0x2640, 0xFE0F })]
    [InlineData(new[] { 0x1F44B, 0x1F3FD })]
    [InlineData(new[] { 0x1F469, 0x1F3FD, 0x200D, 0x1F680 })]
    public void A_picture_selector_after_a_symbol_and_a_joiner_between_two_symbols_pass(int[] codes)
    {
        var emoji = "1. Yes " + string.Concat(codes.Select(char.ConvertFromUtf32)) + " go";
        Assert.False(DisplayText.Hides(emoji));
        Assert.Same(emoji, DisplayText.Visible(emoji));
        Assert.Same(emoji, DisplayText.VisibleCommand(emoji));
    }

    /// <summary>Anywhere else the two are written out like every character that is not seen: after a letter, between letters, with a symbol on one side only, at an end, or doubled.</summary>
    [Theory]
    [InlineData(new[] { 'a', 0xFE0F }, "a‹U+FE0F›")]
    [InlineData(new[] { 0xFE0F, 'a' }, "‹U+FE0F›a")]
    [InlineData(new[] { 0x2764, 0xFE0F, 0xFE0F }, "❤️‹U+FE0F›")]
    [InlineData(new[] { 'a', 0x200D, 'b' }, "a‹U+200D›b")]
    [InlineData(new[] { 0x2764, 0x200D, 'b' }, "❤‹U+200D›b")]
    [InlineData(new[] { 'a', 0x200D, 0x2764 }, "a‹U+200D›❤")]
    [InlineData(new[] { 0x2764, 0x200D }, "❤‹U+200D›")]
    [InlineData(new[] { 0x200D, 0x2764 }, "‹U+200D›❤")]
    [InlineData(new[] { 0x2764, 0x200D, 0x200D, 0x2764 }, "❤‹U+200D›‹U+200D›❤")]
    public void A_picture_selector_or_a_joiner_anywhere_else_is_written_out(int[] codes, string expected)
    {
        Assert.Equal(expected, DisplayText.Visible(string.Concat(codes.Select(char.ConvertFromUtf32))));
    }

    /// <summary>
    /// In a command offered to run and in a permission question's label, a space that is not the plain one is
    /// written as its code as well (the third review of 2026-11-27): it looks and reads as a space, and a shell may
    /// take it for part of a word. Everywhere else such a space is left, as in a name or a sentence.
    /// </summary>
    [Theory]
    [InlineData(0x00A0)]
    [InlineData(0x1680)]
    [InlineData(0x2003)]
    [InlineData(0x200A)]
    [InlineData(0x202F)]
    [InlineData(0x205F)]
    [InlineData(0x3000)]
    public void In_a_command_a_space_that_is_not_the_plain_one_is_written_out(int code)
    {
        var command = "rm" + char.ConvertFromUtf32(code) + "-rf notes";
        Assert.Equal($"rm‹U+{code:X4}›-rf notes", DisplayText.VisibleCommand(command));
        Assert.Same(command, DisplayText.Visible(command));
        Assert.Equal("rm -rf notes ‹U+202E›", DisplayText.VisibleCommand("rm -rf notes ‮"));
        Assert.Same("git status", DisplayText.VisibleCommand("git status"));
    }
}
