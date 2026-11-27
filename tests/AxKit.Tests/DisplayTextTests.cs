using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Text from outside shown as a list's line (security audit S-42).</summary>
public sealed class DisplayTextTests
{
    [Fact]
    public void A_hidden_override_is_made_visible_and_plain_text_is_left()
    {
        Assert.Equal("invoice‹U+202E›txt.exe", DisplayText.Visible("invoice\u202Etxt.exe"));
        Assert.Equal("a‹U+001B›[31mb", DisplayText.Visible("a\u001b[31mb"));
        Assert.Equal("Café, 日本語, notes.md", DisplayText.Visible("Café, 日本語, notes.md"));
    }

    /// <summary>
    /// The table of what is written out, by code point (numbers, so that no such character stands unseen in this
    /// file). The second half came with the second review of 2026-11-27: the soft hyphen, the Mongolian vowel
    /// separator, the line and paragraph separators, the invisible operators, the fillers and the tag characters
    /// passed through unseen.
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
    [InlineData(0x2065)]
    [InlineData(0x3163)]
    [InlineData(0xFE0F)]
    [InlineData(0x1F600)]
    [InlineData(0xE0080)]
    [InlineData(0xDFFFF)]
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
    public void Half_of_a_pair_is_left_as_it_is()
    {
        // A lone surrogate is no character of the table: it is not taken for the tag it could have begun.
        var half = "a" + (char)0xDB40;
        Assert.Equal(half, DisplayText.Visible(half));
    }
}