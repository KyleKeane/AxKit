using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// A form's choice of one as a list, the radio buttons of axKit (docs/axKit/SPEC.md AX-8.10 rule 20; the maintainer,
/// 2026-11-23: "please also make a radio button list since that will be needed eventually"). One Tab stop; Down and
/// Up move without choosing, as the bundle's lists explore (his choice of 2026-10 for the answers of a question: the
/// arrows never select), with type-to-find and the edge sound of <see cref="AxKitList"/>; Space chooses the line and
/// the line chosen before is chosen no longer. Each line says its state, "Monthly, chosen" or "Weekly, not chosen",
/// and a choice is said alone. Built for the forms that will need it; a form's plain choice is still
/// <see cref="FormControls.AddList"/>, where the line selected is the answer.
/// </summary>
public sealed class FormRadioList : AxKitList
{
    private readonly List<string> _names;

    public FormRadioList(IReadOnlyList<string> names, int chosen)
    {
        _names = [.. names];
        Chosen = chosen >= 0 && chosen < _names.Count ? chosen : -1;
        Items.AddRange([.. Lines()]);
        if (Items.Count > 0)
        {
            SelectedIndex = Math.Max(0, Chosen);
        }

        // Type-to-find searches the names, never the words of the state after them.
        Names = () => _names;
    }

    /// <summary>The line chosen, or -1 while none is.</summary>
    public int Chosen { get; private set; }

    /// <summary>The names of the lines, in order.</summary>
    public IReadOnlyList<string> ItemNames => _names;

    /// <summary>Line <paramref name="index"/> chosen, the one before no longer; its state said.</summary>
    public void Choose(int index)
    {
        if (index < 0 || index >= _names.Count)
        {
            return;
        }

        Chosen = index;
        SetLines(Lines());
        SelectedIndex = index;
        (FindForm() as AxKitForm)?.Announce(EventKind.Changed, "chosen", true);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyData == Keys.Space && SelectedIndex >= 0)
        {
            Choose(SelectedIndex);
            e.Handled = e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private List<string> Lines() => [.. _names.Select((name, i) => i == Chosen ? $"{name}, chosen" : $"{name}, not chosen")];
}
