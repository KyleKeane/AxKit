namespace Axit.Forms;

/// <summary>
/// The check box of every app (docs/axit/SPEC.md AX-8.8, 2026-10-31): a <c>CheckBox</c> that says its new state,
/// "checked" or "not checked", when the user toggles it. NVDA read nothing on Space in the consent notice's box, so the
/// state is one announcement of its own, on the click alone (Space and the mouse both come through <see cref="OnClick"/>):
/// a change the window makes says nothing, even with the focus on the box (a notice shown again reset it, 2026-11-01).
/// <c>tests/Axit.Tests/CheckBoxTests</c> fails on a plain <c>CheckBox</c> anywhere else.
/// </summary>
public sealed class AxitCheckBox : CheckBox
{
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Announcer.Raise(this, Checked ? "checked" : "not checked", true);
    }
}
