using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>What Say the status says (docs/axKit/SPEC.md AX-7.10, F8): the status bar's texts as separate parts.</summary>
public sealed class StatusWordsTests
{
    [Fact]
    public void The_texts_are_said_in_order_as_separate_parts()
    {
        Assert.Equal("Claude: ready | default mode on. C:\\work", AxKitForm.StatusWords(["Claude: ready | default mode on", "C:\\work"]));
    }

    [Fact]
    public void An_empty_label_is_left_out_and_a_trailing_full_stop_is_not_doubled()
    {
        Assert.Equal("3 matches", AxKitForm.StatusWords(["3 matches", ""]));
        Assert.Equal("Saved. Line 3, column 4", AxKitForm.StatusWords(["  Saved. ", "Line 3, column 4"]));
    }

    [Fact]
    public void A_window_without_a_status_text_says_so()
    {
        Assert.Equal("No status", AxKitForm.StatusWords([]));
        Assert.Equal("No status", AxKitForm.StatusWords(["", "  "]));
        Assert.Equal(AxKitForm.NoStatus, AxKitForm.StatusWords([]));
    }

    [Fact]
    public void The_key_is_F8_and_a_bundle_key()
    {
        Assert.Equal(Keys.F8, BundleKeys.SayStatus);
        Assert.Contains(BundleKeys.All, key => key.Key == BundleKeys.SayStatus);
    }
}
