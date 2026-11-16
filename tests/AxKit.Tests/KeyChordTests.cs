using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The one chord vocabulary (docs/axKit/user-workflows.md WF-2.2): every name a key press could be written with before
/// there was one vocabulary keeps the code it had, so that every script presses what it pressed; the four names that
/// came with it; and how a chord is written, put in order and told from a control's own key.
/// </summary>
public sealed class KeyChordTests
{
    /// <summary>
    /// The names of the automation channel's own table (<c>AutomationChannel.ParseKey</c>) and of the NVDA tier's
    /// (<c>Native.VirtualKey</c>) as they were at f56b1ee, each with the virtual key code it had there. The two tables
    /// agreed wherever both held a name; the channel's held period, comma and apps beside the tier's.
    /// </summary>
    private static readonly (string Name, int Code)[] Known =
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
        ("f1", 0x70),
        ("f2", 0x71),
        ("f3", 0x72),
        ("f4", 0x73),
        ("f5", 0x74),
        ("f6", 0x75),
        ("f7", 0x76),
        ("f8", 0x77),
        ("f9", 0x78),
        ("f10", 0x79),
        ("f11", 0x7A),
        ("f12", 0x7B),
    ];

    [Fact]
    public void Every_name_the_channel_and_the_NVDA_tier_knew_has_the_code_it_had()
    {
        foreach (var (name, code) in Known)
        {
            Assert.True(KeyChord.Code(name) == code, $"{name} must stay 0x{code:X2}; it is {KeyChord.Code(name):X2}");
        }

        // Written in any case, as both tables took a name ("Enter", "ESCAPE").
        Assert.Equal(0x0D, KeyChord.Code("Enter"));
        Assert.Equal(0x1B, KeyChord.Code("ESCAPE"));
        // What neither table held is no name: a function key past f12, f0, a word.
        Assert.Null(KeyChord.Code("f13"));
        Assert.Null(KeyChord.Code("f0"));
        Assert.Null(KeyChord.Code("return"));
        Assert.Null(KeyChord.Code(string.Empty));
    }

    [Fact]
    public void A_letter_or_a_digit_is_its_own_name()
    {
        Assert.Equal(0x41, KeyChord.Code("a"));
        Assert.Equal(0x41, KeyChord.Code("A"));
        Assert.Equal(0x5A, KeyChord.Code("z"));
        Assert.Equal(0x30, KeyChord.Code("0"));
        Assert.Equal(0x31, KeyChord.Code("1"));
        Assert.Equal(0x39, KeyChord.Code("9"));
        Assert.Equal("a", KeyChord.Name(0x41));
        Assert.Equal("z", KeyChord.Name(0x5A));
        Assert.Equal("1", KeyChord.Name(0x31));
        // Two characters are no letter, and a sign is no name of a key.
        Assert.Null(KeyChord.Code("ab"));
        Assert.Null(KeyChord.Code("+"));
        Assert.Null(KeyChord.Code("."));
    }

    [Fact]
    public void The_four_new_names_have_their_codes()
    {
        Assert.Equal(0xBF, KeyChord.Code("slash"));
        Assert.Equal(0xBA, KeyChord.Code("semicolon"));
        // The numeric keypad's plus and minus, which the text size keys also take.
        Assert.Equal(0x6B, KeyChord.Code("add"));
        Assert.Equal(0x6D, KeyChord.Code("subtract"));
    }

    [Fact]
    public void Every_name_is_the_name_of_its_code_but_the_second_name_of_escape()
    {
        foreach (var (name, code) in Known)
        {
            Assert.Equal(name == "esc" ? "escape" : name, KeyChord.Name(code));
        }

        Assert.Equal("slash", KeyChord.Name(0xBF));
        Assert.Equal("semicolon", KeyChord.Name(0xBA));
        Assert.Equal("add", KeyChord.Name(0x6B));
        Assert.Equal("subtract", KeyChord.Name(0x6D));
    }

    [Fact]
    public void A_chord_is_written_ctrl_shift_alt_and_the_key_in_lower_case()
    {
        Assert.Equal("ctrl+shift+w", KeyChord.Text(0x57, ctrl: true, shift: true, alt: false));
        Assert.Equal("f3", KeyChord.Text(0x72, ctrl: false, shift: false, alt: false));
        Assert.Equal("ctrl+1", KeyChord.Text(0x31, ctrl: true, shift: false, alt: false));
        Assert.Equal("ctrl+enter", KeyChord.Text(0x0D, ctrl: true, shift: false, alt: false));
        Assert.Equal("ctrl+shift+alt+up", KeyChord.Text(0x26, ctrl: true, shift: true, alt: true));
        Assert.Equal("alt+f4", KeyChord.Text(0x73, ctrl: false, shift: false, alt: true));
    }

    [Fact]
    public void A_key_without_a_name_has_no_chord()
    {
        // Scroll Lock, and no key at all.
        Assert.Null(KeyChord.Name(0x91));
        Assert.Equal(string.Empty, KeyChord.Text(0x91, ctrl: true, shift: false, alt: false));
        Assert.Equal(string.Empty, KeyChord.Text(0, ctrl: false, shift: false, alt: false));
    }

    [Fact]
    public void A_press_is_put_in_the_one_order()
    {
        Assert.Equal("ctrl+shift+p", KeyChord.Normal("Shift+Ctrl+P"));
        Assert.Equal("ctrl+shift+alt+f6", KeyChord.Normal("alt+shift+ctrl+F6"));
        Assert.Equal("ctrl+f", KeyChord.Normal("ctrl+f"));
        Assert.Equal("escape", KeyChord.Normal("esc"));
        Assert.Equal("down", KeyChord.Normal("Down"));
        // A part the vocabulary does not hold: the press as it was given, the NVDA tier's own modifier among them.
        Assert.Equal("nvda+1", KeyChord.Normal("nvda+1"));
        Assert.Equal("Ctrl+Bogus", KeyChord.Normal("Ctrl+Bogus"));
        Assert.Equal("ctrl+", KeyChord.Normal("ctrl+"));
    }

    [Theory]
    [InlineData("ctrl+f", true)]
    [InlineData("alt+up", true)]
    [InlineData("f2", true)]
    [InlineData("shift+f3", true)]
    [InlineData("ctrl+shift+tab", true)]
    [InlineData("enter", false)]
    [InlineData("shift+tab", false)]
    [InlineData("h", false)]
    [InlineData("down", false)]
    [InlineData("ctrl+bogus", false)]
    public void A_command_chord_carries_ctrl_or_alt_or_is_a_function_key(string press, bool expected)
    {
        Assert.Equal(expected, KeyChord.IsCommandChord(press));
    }
}
