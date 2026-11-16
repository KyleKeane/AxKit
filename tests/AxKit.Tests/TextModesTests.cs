using AxKit.Core;

namespace AxKit.Tests;

/// <summary>docs/axKit/SPEC.md AX-8.13: the words of the two modes and which keys navigation mode refuses.</summary>
public class TextModesTests
{
    private const int Back = 0x08, Tab = 0x09, Enter = 0x0D, Escape = 0x1B, Space = 0x20, PageUp = 0x21, End = 0x23, Home = 0x24,
        Left = 0x25, Up = 0x26, Down = 0x28, Insert = 0x2D, Delete = 0x2E, F3 = 0x72, NumPad5 = 0x65, Period = 0xBE, Packet = 0xE7;

    [Fact]
    public void The_words_are_the_specs()
    {
        Assert.Equal("Navigation mode", TextModes.Toggled(true));
        Assert.Equal("Edit mode", TextModes.Toggled(false));
        Assert.Equal("Read only: there is no edit mode here", TextModes.ReadOnly);
        Assert.Equal("Navigation mode: press Ctrl+Space to edit", TextModes.Refused("Ctrl+Space"));
        Assert.Equal("moves to the previous heading level 2", TextModes.KeyTestJump("heading level 2", previous: true));
    }

    /// <summary>
    /// AX-8.13: among the refused keys, the plain ones that would type have no command at all and get the sound of an
    /// unassigned key; an edit refused (Enter, Backspace, Delete, a paste) and every key with Ctrl or Alt keep the edge.
    /// </summary>
    [Theory]
    [InlineData('A', false, false, true)]
    [InlineData('7', false, false, true)]
    [InlineData(Space, false, false, true)]
    [InlineData(NumPad5, false, false, true)]
    [InlineData(Period, false, false, true)]
    [InlineData(Enter, false, false, false)]
    [InlineData(Back, false, false, false)]
    [InlineData(Delete, false, false, false)]
    [InlineData(Insert, false, false, false)]
    [InlineData('V', true, false, false)]
    [InlineData('A', false, true, false)]
    public void A_plain_key_that_would_type_is_unassigned(int code, bool ctrl, bool alt, bool unassigned)
    {
        Assert.Equal(unassigned, TextModes.Unassigned(code, ctrl, alt));
    }

    [Theory]
    [InlineData('A', false)]
    [InlineData('Q', true)]
    [InlineData('7', false)]
    [InlineData(Space, false)]
    [InlineData(NumPad5, false)]
    [InlineData(Period, false)]
    [InlineData(Enter, false)]
    [InlineData(Enter, true)]
    [InlineData(Back, false)]
    [InlineData(Delete, false)]
    [InlineData(Delete, true)]
    [InlineData(Insert, true)]
    public void A_key_that_types_or_deletes_is_refused(int code, bool shift)
    {
        Assert.Equal(NavigationKey.Refused, TextModes.InNavigation(code, ctrl: false, shift, alt: false));
    }

    [Theory]
    [InlineData('V', false)]
    [InlineData('X', false)]
    [InlineData('Z', false)]
    [InlineData('Z', true)]
    [InlineData('Y', false)]
    [InlineData(Back, false)]
    [InlineData(Delete, false)]
    [InlineData(Enter, false)]
    public void Ctrl_with_a_key_that_changes_the_text_is_refused(int code, bool shift)
    {
        Assert.Equal(NavigationKey.Refused, TextModes.InNavigation(code, ctrl: true, shift, alt: false));
    }

    [Fact]
    public void Alt_Backspace_the_edit_controls_undo_is_refused()
    {
        Assert.Equal(NavigationKey.Refused, TextModes.InNavigation(Back, ctrl: false, shift: false, alt: true));
    }

    [Theory]
    [InlineData(Left, false, false, false)]
    [InlineData(Up, false, true, false)]
    [InlineData(Down, true, true, false)]
    [InlineData(Home, true, false, false)]
    [InlineData(End, false, true, false)]
    [InlineData(PageUp, false, false, false)]
    [InlineData('C', true, false, false)]
    [InlineData('A', true, false, false)]
    [InlineData(Insert, true, false, false)]
    [InlineData('H', true, false, false)]
    [InlineData('H', true, true, false)]
    [InlineData('G', true, false, false)]
    [InlineData('H', true, false, true)]
    [InlineData(Escape, false, false, false)]
    [InlineData(Tab, false, false, false)]
    [InlineData(Tab, false, true, false)]
    [InlineData(F3, false, false, false)]
    [InlineData('F', false, false, true)]
    [InlineData(Packet, false, false, false)]
    public void A_move_a_selection_a_copy_or_a_command_passes(int code, bool ctrl, bool shift, bool alt)
    {
        Assert.Equal(NavigationKey.Passes, TextModes.InNavigation(code, ctrl, shift, alt));
    }

    [Fact]
    public void Only_copy_and_select_all_reach_the_control_as_characters()
    {
        Assert.True(TextModes.CharacterAllowed('\u0003'));
        Assert.True(TextModes.CharacterAllowed('\u0001'));
        foreach (var character in new[] { 'a', ' ', '\t', '\r', '\n', '\b', '\u0016', '\u0018', '\u001A', '\u007F', 'é' })
        {
            Assert.False(TextModes.CharacterAllowed(character));
        }
    }
}
