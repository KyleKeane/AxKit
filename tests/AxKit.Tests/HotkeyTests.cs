using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The hotkey as text both ways and the keys refused (docs/axStart/SPEC.md AS-4.2); from AxStart.Tests with the record, 2026-10-23.</summary>
public sealed class HotkeyTests
{
    [Fact]
    public void The_default_is_ctrl_shift_space()
    {
        Assert.Equal("Ctrl+Shift+Space", Hotkey.Default.ToString());
    }

    [Theory]
    [InlineData("Ctrl+Shift+Space")]
    [InlineData("Ctrl+Alt+Space")]
    [InlineData("Ctrl+Shift+F12")]
    [InlineData("Ctrl+Windows+Alt+Shift+Page Down")]
    [InlineData("Alt+A")]
    [InlineData("Ctrl+Shift+Slash")]
    [InlineData("Ctrl+Shift+Comma")]
    [InlineData("Ctrl+Shift+Semicolon")]
    public void The_text_comes_back_the_same(string text)
    {
        Assert.Equal(text, Hotkey.Parse(text)!.ToString());
    }

    [Fact]
    public void Any_order_and_case_is_read()
    {
        Assert.Equal(new Hotkey(Hotkey.Alt | Hotkey.Windows, 0x20), Hotkey.Parse("alt + win + space"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+Space")]
    [InlineData("Ctrl+Period")]
    public void Nonsense_is_not_a_hotkey(string text)
    {
        Assert.Null(Hotkey.Parse(text));
    }

    [Fact]
    public void A_key_needs_ctrl_alt_or_windows()
    {
        Assert.Equal("Use Ctrl, Alt or the Windows key with it", new Hotkey(Hotkey.Shift, 0x41).Refusal());
        Assert.Equal("Use Ctrl, Alt or the Windows key with it", new Hotkey(0, 0x20).Refusal());
        Assert.Null(new Hotkey(Hotkey.Windows, 0x41).Refusal());
        Assert.Equal("That key cannot be a hotkey", new Hotkey(Hotkey.Control, 0xBE).Refusal());
    }

    [Fact]
    public void A_key_outside_the_names_is_written_by_its_number_and_refused()
    {
        var odd = new Hotkey(Hotkey.Control | Hotkey.Alt, 0xFF);
        Assert.Equal("Ctrl+Alt+Key 255", odd.ToString());
        Assert.Equal("That key cannot be a hotkey", odd.Refusal());
        Assert.Null(Hotkey.Parse(odd.ToString()));
    }

    /// <summary>AX-8.11: the period's key is kept for a later help launcher, so it is no hotkey's key, while Slash, Comma and Semicolon are.</summary>
    [Fact]
    public void Slash_comma_and_semicolon_are_hotkey_keys_and_period_is_not()
    {
        Assert.Null(new Hotkey(Hotkey.Control | Hotkey.Shift, 0xBF).Refusal());
        Assert.Null(new Hotkey(Hotkey.Control | Hotkey.Shift, 0xBC).Refusal());
        Assert.Null(new Hotkey(Hotkey.Control | Hotkey.Shift, 0xBA).Refusal());
        Assert.Equal("That key cannot be a hotkey", new Hotkey(Hotkey.Control | Hotkey.Shift, 0xBE).Refusal());
        Assert.Null(Hotkey.Parse("Ctrl+Shift+Period"));
    }

    // ---- a retired default taken over (AX-8.11; HotkeySettings.TakenOver) ----

    private static readonly Hotkey Retired = new(Hotkey.Windows | Hotkey.Alt, 'H');
    private static readonly Hotkey Now = new(Hotkey.Control | Hotkey.Shift, 0xBF);

    [Theory]
    [InlineData("Windows+Alt+H")]
    [InlineData("alt + win + h")]
    public void A_text_that_still_holds_the_retired_default_becomes_the_new_one(string text)
    {
        Assert.Equal("Ctrl+Shift+Slash", HotkeySettings.TakenOver(text, Retired, Now));
    }

    [Theory]
    [InlineData("Ctrl+Alt+K")]
    [InlineData("Windows+Alt+S")]
    [InlineData("Ctrl+Windows+Alt+H")]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData(null)]
    public void Any_other_text_is_left_as_it_is(string? text)
    {
        Assert.Equal(text, HotkeySettings.TakenOver(text, Retired, Now));
    }

    [Fact]
    public void Without_a_retired_default_nothing_is_taken_over()
    {
        Assert.Equal("Windows+Alt+H", HotkeySettings.TakenOver("Windows+Alt+H", null, Now));
    }
}
