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
    /// The exceptions, so that an emoji in an answer stays an emoji and a word stays a word: U+FE0F right after a
    /// picture of Extended_Pictographic or in a keycap, U+200D between two pictures (the first of which may carry
    /// its U+FE0F or a skin tone), the tags of the three recommended flags, U+200C between two letters of a script
    /// that writes with it. Written as code points: a heart, a rainbow flag, a family of three, a person with a sign
    /// and its picture selector, a wave with a skin tone, an astronaut with a skin tone, a family with skin tones, a
    /// flag of two letters, a keycap, England's flag, and the Persian word "mi-khaham" with its non-joiner.
    /// </summary>
    [Theory]
    [InlineData(new[] { 0x2764, 0xFE0F })]
    [InlineData(new[] { 0x1F3F3, 0xFE0F, 0x200D, 0x1F308 })]
    [InlineData(new[] { 0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467 })]
    [InlineData(new[] { 0x1F937, 0x200D, 0x2640, 0xFE0F })]
    [InlineData(new[] { 0x1F44B, 0x1F3FD })]
    [InlineData(new[] { 0x1F469, 0x1F3FD, 0x200D, 0x1F680 })]
    [InlineData(new[] { 0x1F468, 0x1F3FB, 0x200D, 0x1F469, 0x1F3FF, 0x200D, 0x1F466 })]
    [InlineData(new[] { 0x1F1EB, 0x1F1F7 })]
    [InlineData(new[] { '1', 0xFE0F, 0x20E3 })]
    [InlineData(new[] { '#', 0xFE0F, 0x20E3 })]
    [InlineData(new[] { 0x1F3F4, 0xE0067, 0xE0062, 0xE0065, 0xE006E, 0xE0067, 0xE007F })]
    [InlineData(new[] { 0x0645, 0x06CC, 0x200C, 0x062E, 0x0648, 0x0627, 0x0647, 0x0645 })]
    [InlineData(new[] { 0x0915, 0x094D, 0x200C, 0x0937 })]
    public void An_emoji_and_a_word_that_needs_its_non_joiner_stay_as_they_are(int[] codes)
    {
        var emoji = "1. Yes " + string.Concat(codes.Select(char.ConvertFromUtf32)) + " go";
        Assert.False(DisplayText.Hides(emoji));
        Assert.Same(emoji, DisplayText.Visible(emoji));
        Assert.Same(emoji, DisplayText.VisibleLine(emoji));
    }

    /// <summary>
    /// Anywhere else they are written out like every character that is not seen: after a letter, between letters,
    /// with a picture on one side only, at an end, doubled; and next to ASCII and to signs of mathematics, which are
    /// symbols by their class but no pictures (the fourth review of 2026-11-27: a joiner between two "&gt;" passed).
    /// A run of tags that spells no recommended flag carries a text unseen.
    /// </summary>
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
    [InlineData(new[] { '>', 0x200D, '>' }, ">‹U+200D›>")]
    [InlineData(new[] { '|', 0xFE0F }, "|‹U+FE0F›")]
    [InlineData(new[] { 0x2211, 0x200D, 0x221E }, "∑‹U+200D›∞")]
    [InlineData(new[] { '$', 0x200D, 0x20AC }, "$‹U+200D›€")]
    [InlineData(new[] { 'a', 0xFE0F, 0x20E3 }, "a‹U+FE0F›⃣")]
    [InlineData(new[] { '1', 0x20E3 }, "1‹U+20E3›")]
    [InlineData(new[] { 0x1F3F4, 0xE0068, 0xE0069, 0xE007F }, "🏴‹U+E0068›‹U+E0069›‹U+E007F›")]
    [InlineData(new[] { 'a', 0xE0067, 0xE0062, 0xE0065, 0xE006E, 0xE0067, 0xE007F }, "a‹U+E0067›‹U+E0062›‹U+E0065›‹U+E006E›‹U+E0067›‹U+E007F›")]
    [InlineData(new[] { 'a', 0x200C, 'b' }, "a‹U+200C›b")]
    [InlineData(new[] { 0x0645, 0x200C, 'b' }, "م‹U+200C›b")]
    [InlineData(new[] { 0x2800 }, "‹U+2800›")]
    [InlineData(new[] { 0x0301, 'a' }, "‹U+0301›a")]
    [InlineData(new[] { 'a', '\n', 0x0301, 0x0302 }, "a‹U+000A›‹U+0301›‹U+0302›")]
    public void A_picture_selector_a_joiner_and_a_tag_anywhere_else_are_written_out(int[] codes, string expected)
    {
        Assert.Equal(expected, DisplayText.Visible(string.Concat(codes.Select(char.ConvertFromUtf32))));
    }

    /// <summary>
    /// What the fourth review of 2026-11-27 found (H1): an AI's command with a joiner or a picture selector between
    /// ASCII signs looked like another command, and bash ran it as written. Each is written out, as a command, as a
    /// question's answer and as a screen's label, and <see cref="DisplayText.Hides"/> says so.
    /// </summary>
    [Theory]
    [InlineData("echo x >‍> f", "echo x >‹U+200D›> f")]
    [InlineData("a |‍| b", "a |‹U+200D›| b")]
    [InlineData("cat <‍< EOF", "cat <‹U+200D›< EOF")]
    [InlineData("i+‍+", "i+‹U+200D›+")]
    [InlineData("a |️ b", "a |‹U+FE0F› b")]
    [InlineData("echo >‍> .bashrc", "echo >‹U+200D›> .bashrc")]
    [InlineData("x=‍$(id)", "x=‹U+200D›$(id)")]
    [InlineData("echo `‍`", "echo `‹U+200D›`")]
    public void A_joiner_or_a_picture_selector_between_signs_of_a_shell_is_written_out(string text, string expected)
    {
        Assert.True(DisplayText.Hides(text));
        Assert.Equal(expected, DisplayText.Visible(text));
        Assert.Equal(expected, DisplayText.VisibleLine(text));
        Assert.Equal(expected, DisplayText.VisibleCommand(text));
    }

    /// <summary>
    /// In a command nothing is left unseen, an emoji's own joiner and selectors included (N2: a command is run, not
    /// read): an emoji in a command shows its parts, and the non-joiner of a Persian word is written out too.
    /// </summary>
    [Theory]
    [InlineData(new[] { 0x2764, 0xFE0F }, "❤‹U+FE0F›")]
    [InlineData(new[] { 0x1F468, 0x200D, 0x1F469 }, "👨‹U+200D›👩")]
    [InlineData(new[] { '1', 0xFE0F, 0x20E3 }, "1‹U+FE0F›‹U+20E3›")]
    [InlineData(new[] { 0x1F3F4, 0xE0067, 0xE0062, 0xE0065, 0xE006E, 0xE0067, 0xE007F }, "🏴‹U+E0067›‹U+E0062›‹U+E0065›‹U+E006E›‹U+E0067›‹U+E007F›")]
    [InlineData(new[] { 0x0645, 0x200C, 0x062E }, "م‹U+200C›خ")]
    [InlineData(new[] { 'a', 0xFE0E }, "a‹U+FE0E›")]
    [InlineData(new[] { 0x2800 }, "‹U+2800›")]
    public void In_a_command_every_joiner_selector_and_tag_is_written_out(int[] codes, string expected)
    {
        Assert.Equal(expected, DisplayText.VisibleCommand("echo " + string.Concat(codes.Select(char.ConvertFromUtf32)))[5..]);
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
        // A question's line keeps them, as a sentence does (VisibleLine's summary says why).
        Assert.Same(command, DisplayText.VisibleLine(command));
        Assert.Equal("rm -rf notes ‹U+202E›", DisplayText.VisibleCommand("rm -rf notes ‮"));
        Assert.Same("git status", DisplayText.VisibleCommand("git status"));
    }

    /// <summary>
    /// What the fifth review of 2026-11-27 found (H1 of round five): a combining mark rides on the sign before it and
    /// leaves it looking like another sign, or like nothing changed, while the shell reads the mark as a word of its
    /// own. Bash ran "echo added &gt;U+0338&gt; target.txt" as a truncation and made a file named by the mark. A mark
    /// whose cluster does not begin with a letter is written out in every text (E1).
    /// </summary>
    [Theory]
    [InlineData(new[] { '>', 0x0338, '>' }, ">‹U+0338›>")]
    [InlineData(new[] { '&', 0x0338, '&' }, "&‹U+0338›&")]
    [InlineData(new[] { '|', 0x20DD, '|' }, "|‹U+20DD›|")]
    [InlineData(new[] { ' ', 0x0301 }, " ‹U+0301›")]
    [InlineData(new[] { '1', 0x0332 }, "1‹U+0332›")]
    [InlineData(new[] { 0x2764, 0x20E0 }, "❤‹U+20E0›")]
    [InlineData(new[] { '>', 0x0338, 0x0301, '>' }, ">‹U+0338›‹U+0301›>")]
    public void A_mark_on_anything_but_a_letter_is_written_out_in_every_text(int[] codes, string expected)
    {
        var text = "echo a" + string.Concat(codes.Select(char.ConvertFromUtf32)) + " b";
        Assert.True(DisplayText.Hides(text));
        Assert.Equal("echo a" + expected + " b", DisplayText.Visible(text));
        Assert.Equal("echo a" + expected + " b", DisplayText.VisibleLine(text));
        Assert.Equal("echo a" + expected + " b", DisplayText.VisibleCommand(text));
    }

    /// <summary>A mark after each of bash's metacharacters, its quotes, its dollar sign and backtick and the other signs of a word's syntax: none may look like the sign alone.</summary>
    [Fact]
    public void A_mark_after_every_sign_of_a_shell_is_written_out()
    {
        foreach (var sign in " |&;()<>'\"`$\\=*?[]{}#~!%^-+,./:@")
        {
            foreach (var mark in new[] { 0x0338, 0x20DD, 0x0903, 0x0301 })
            {
                var text = "echo a" + sign + char.ConvertFromUtf32(mark) + sign + "b";
                var expected = "echo a" + sign + $"‹U+{mark:X4}›" + sign + "b";
                Assert.Equal(expected, DisplayText.Visible(text));
                Assert.Equal(expected, DisplayText.VisibleCommand(text));
            }
        }
    }

    /// <summary>
    /// Marks on letters stay, in every text: a decomposed French word (e and U+0301), a Hindi word with its vowel
    /// signs and virama, an Arabic word with its vowel marks, a letter with two accents.
    /// </summary>
    [Theory]
    [InlineData(new[] { 'c', 'a', 'f', 'e', 0x0301 })]
    [InlineData(new[] { 0x0939, 0x093F, 0x0928, 0x094D, 0x0926, 0x0940 })]
    [InlineData(new[] { 0x0643, 0x064E, 0x062A, 0x064E, 0x0628, 0x064E })]
    [InlineData(new[] { 'a', 0x0301, 0x0323 })]
    public void A_mark_on_a_letter_stays_in_every_text(int[] codes)
    {
        var text = "echo " + string.Concat(codes.Select(char.ConvertFromUtf32)) + " done";
        Assert.False(DisplayText.Hides(text));
        Assert.Same(text, DisplayText.Visible(text));
        Assert.Same(text, DisplayText.VisibleLine(text));
        Assert.Same(text, DisplayText.VisibleCommand(text));
    }

    /// <summary>A keycap (a digit, its picture selector, U+20E3) stays an emoji in an answer, and shows its parts in a command.</summary>
    [Fact]
    public void A_keycap_stays_in_an_answer_and_is_written_out_in_a_command()
    {
        var answer = "2. Yes " + "3" + char.ConvertFromUtf32(0xFE0F) + char.ConvertFromUtf32(0x20E3) + " go";
        Assert.Same(answer, DisplayText.VisibleLine(answer));
        Assert.Equal("echo 3‹U+FE0F›‹U+20E3›", DisplayText.VisibleCommand("echo 3" + char.ConvertFromUtf32(0xFE0F) + char.ConvertFromUtf32(0x20E3)));
    }

    /// <summary>
    /// The sixth review of 2026-11-27 (L6): the Hangul fillers are letters by their category and are seen as nothing,
    /// so a mark on one sat on nothing seen and stayed. Hidden is asked before the letter: the filler and the mark on it
    /// are written out, in every text.
    /// </summary>
    [Theory]
    [InlineData(0x3164)]
    [InlineData(0x115F)]
    [InlineData(0x1160)]
    [InlineData(0xFFA0)]
    public void A_mark_on_a_hidden_filler_is_written_out(int filler)
    {
        var text = "echo a>" + char.ConvertFromUtf32(filler) + char.ConvertFromUtf32(0x0338) + "> b";
        var expected = $"echo a>‹U+{filler:X4}›‹U+0338›> b";
        Assert.Equal(expected, DisplayText.Visible(text));
        Assert.Equal(expected, DisplayText.VisibleCommand(text));
    }

    /// <summary>
    /// L6: in ordinary text a joiner or a non-joiner right after a virama stays, since it asks for a half form or keeps
    /// a conjunct apart (Devanagari KA, VIRAMA, ZWJ, SSA; Bengali KA, VIRAMA, ZWNJ, SSA). In a command every joiner is
    /// written out, whatever stands before it, and so is one after anything but a virama.
    /// </summary>
    [Theory]
    [InlineData(new[] { 0x0915, 0x094D, 0x200D, 0x0937 })]
    [InlineData(new[] { 0x0995, 0x09CD, 0x200C, 0x09B7 })]
    [InlineData(new[] { 0x0D15, 0x0D4D, 0x200D })]
    public void A_joiner_after_a_virama_stays_in_ordinary_text(int[] codes)
    {
        var word = string.Concat(codes.Select(char.ConvertFromUtf32));
        var text = "Hindi " + word + " text";
        Assert.False(DisplayText.Hides(text));
        Assert.Same(text, DisplayText.Visible(text));
        Assert.Contains("‹U+200", DisplayText.VisibleCommand(text), StringComparison.Ordinal);
        // After a letter that is no virama it is written out as before.
        Assert.Equal("a‹U+200D›b", DisplayText.Visible("a" + char.ConvertFromUtf32(0x200D) + "b"));
    }

    /// <summary>
    /// The eighth review of 2026-11-28, M1: a line end or a tab is no hidden character for a comparison with what a
    /// program drew, every character <see cref="DisplayText.WithoutHidden"/> takes out besides is one.
    /// </summary>
    [Fact]
    public void A_line_end_or_a_tab_is_no_hidden_character()
    {
        Assert.False(DisplayText.HoldsHidden("first line\r\nsecond line\nthird\tcolumn"));
        Assert.True(DisplayText.HoldsHidden("first​line"));
        Assert.True(DisplayText.HoldsHidden("first line\nsecond⁠"));
        Assert.True(DisplayText.HoldsHidden("bell\u0007"));
    }
}
