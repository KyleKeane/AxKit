namespace AxKit.Forms;

/// <summary>
/// A menu of choices of which one is checked, a radio group drawn as checked items (AX-6.4): the items made from the
/// choices in menu order, each running <paramref name="chosen"/> with its value, and <see cref="Check"/> re-checking
/// them when the value changes. The window keeps the click's work, the setting, the save and the words it announces
/// (the menu closes on a choice, so the choice is said). From axFile's Sort by, axProcess's Sort processes by,
/// axClaude's Speak replies, axTerminal's Speak output, axPass's Lock after and axCalc's Angles, 2026-11-09.
/// </summary>
public sealed class ChoiceItems<T>
    where T : notnull
{
    private readonly List<(T Value, ToolStripMenuItem Item)> _items = [];

    public ChoiceItems(ToolStripMenuItem menu, IEnumerable<(T Value, string Text)> choices, T current, Action<T> chosen)
    {
        foreach (var (value, text) in choices)
        {
            // A setting's choice is a change (docs/axKit/app-kit.md AK-2.5).
            var item = CommandPalette.Marked(new ToolStripMenuItem(text, null, (_, _) => chosen(value)) { Checked = EqualityComparer<T>.Default.Equals(value, current) }, CommandMark.Change);
            _items.Add((value, item));
            menu.DropDownItems.Add(item);
        }
    }

    /// <summary>The item of <paramref name="value"/> checked, every other unchecked.</summary>
    public void Check(T value)
    {
        foreach (var (candidate, item) in _items)
        {
            item.Checked = EqualityComparer<T>.Default.Equals(candidate, value);
        }
    }
}