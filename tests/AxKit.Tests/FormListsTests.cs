using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The form's lists of checks and of one choice (docs/axKit/SPEC.md AX-8.10 rules 19 and 20): each line says its
/// state, Check all and Uncheck all set every line, and a choice of one keeps one line chosen.
/// </summary>
public sealed class FormListsTests
{
    [Fact]
    public void A_check_list_says_each_state_and_checks_or_unchecks_them_all()
    {
        using var list = new FormCheckList([("axDown", false), ("axJSON", true)]);
        Assert.Equal(["axDown, not checked", "axJSON, checked"], list.Items.Cast<string>());
        list.SetAll(true);
        Assert.True(list.IsChecked(0) && list.IsChecked(1));
        Assert.Equal(["axDown, checked", "axJSON, checked"], list.Items.Cast<string>());
        list.SetAll(false);
        Assert.False(list.IsChecked(0) || list.IsChecked(1));
        Assert.True(list.SelectName("AXJSON"));
        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void A_radio_list_keeps_one_line_chosen()
    {
        using var list = new FormRadioList(["Daily", "Weekly", "Monthly"], chosen: 1);
        Assert.Equal(1, list.Chosen);
        Assert.Equal(1, list.SelectedIndex);
        Assert.Equal(["Daily, not chosen", "Weekly, chosen", "Monthly, not chosen"], list.Items.Cast<string>());
        list.Choose(2);
        Assert.Equal(2, list.Chosen);
        Assert.Equal(["Daily, not chosen", "Weekly, not chosen", "Monthly, chosen"], list.Items.Cast<string>());
        using var none = new FormRadioList(["Daily", "Weekly"], chosen: -1);
        Assert.Equal(-1, none.Chosen);
        Assert.Equal(0, none.SelectedIndex);
    }
}
