using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// A form's group of checks of one kind as one list (docs/axKit/SPEC.md AX-8.10 rule 19; the maintainer's choice of
/// 2026-11-23, "One list, Space checks", over a check box each): one Tab stop; Down and Up move, as in every list of
/// the bundle (type-to-find and the edge sound are <see cref="AxKitList"/>'s); Space checks or unchecks the line, as
/// Space selects in every list (AX-8.16); Ctrl+A checks every line and Ctrl+Shift+A none. Each line says its state,
/// "axDown, checked" or "axDown, not checked", and a change of it is said alone. The windows' item form makes one
/// for two or more checks in a row, with a Check all button before it (<c>ItemForm.Form.cs</c>).
/// </summary>
public sealed class FormCheckList : AxKitList
{
    private readonly List<string> _names;
    private readonly bool[] _checked;

    public FormCheckList(IReadOnlyList<(string Name, bool Checked)> items)
    {
        _names = [.. items.Select(item => item.Name)];
        _checked = [.. items.Select(item => item.Checked)];
        Items.AddRange([.. Lines()]);
        if (Items.Count > 0)
        {
            SelectedIndex = 0;
        }

        // Type-to-find searches the names, never the words of the state after them.
        Names = () => _names;
    }

    /// <summary>The names of the lines, in order.</summary>
    public IReadOnlyList<string> ItemNames => _names;

    /// <summary>Whether line <paramref name="index"/> is checked.</summary>
    public bool IsChecked(int index) => _checked[index];

    /// <summary>Every line checked, or none; said in one line.</summary>
    public void SetAll(bool value)
    {
        Array.Fill(_checked, value);
        SetLines(Lines());
        (FindForm() as AxKitForm)?.Announce(EventKind.Changed, value ? $"All {_names.Count} checked" : "None checked", true);
    }

    /// <summary>The line on <paramref name="name"/> selected, the name compared without case; false for a name the list has not.</summary>
    public bool SelectName(string name)
    {
        var at = _names.FindIndex(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        if (at >= 0)
        {
            SelectedIndex = at;
        }

        return at >= 0;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyData == Keys.Space && SelectedIndex >= 0)
        {
            var at = SelectedIndex;
            _checked[at] = !_checked[at];
            SetLines(Lines());
            SelectedIndex = at;
            (FindForm() as AxKitForm)?.Announce(EventKind.Changed, _checked[at] ? "checked" : "not checked", true);
            e.Handled = e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyData is SelectionKeys.SelectAll or SelectionKeys.SelectNone)
        {
            SetAll(e.KeyData == SelectionKeys.SelectAll);
            e.Handled = e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private List<string> Lines() => [.. _names.Select((name, i) => _checked[i] ? $"{name}, checked" : $"{name}, not checked")];
}
