using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// The selection of one list of a window (docs/axKit/SPEC.md AX-8.16; from axFile's window on its second use, the
/// item windows, 2026-11-22): the marks (<see cref="Marks"/>) on the rows the list shows, and the one way a change of
/// them reaches the user: the items whose mark flipped are rewritten in place, so that the list is not made again
/// under NVDA, a row that is selected reads ", selected" after its words (<see cref="Label"/>), and what the key
/// says is said once. The list stays a single-selection list box, where the arrow keys choose the current row; the
/// selection is text in the items (axFile's AF-D9). The window gives the rows as the marks see them, the text of a
/// row's item and its way of speaking; it makes the commands on the keys of <see cref="SelectionKeys"/>
/// (<see cref="MoveCommands"/> for the four that move), fills its items with <see cref="Label"/>, calls
/// <see cref="Keep"/> when it fills the same level again and <see cref="Clear"/> when it shows another, and asks
/// <see cref="Keys"/> or <see cref="Contains"/> for what its commands act on.
/// </summary>
public sealed class ListSelection
{
    private readonly AxKitList _list;
    private readonly Func<IReadOnlyList<MarkRow>> _rows;
    private readonly Func<int, string> _text;
    private readonly Action<string> _say;
    private readonly Marks _marks = new();

    /// <param name="list">The list whose rows are selected.</param>
    /// <param name="rows">The rows the list shows now, one for each of its items that stands for a thing, in its order.</param>
    /// <param name="text">The text of the item of a row, made with <see cref="Label"/>.</param>
    /// <param name="say">The window's announcement.</param>
    public ListSelection(AxKitList list, Func<IReadOnlyList<MarkRow>> rows, Func<int, string> text, Action<string> say)
    {
        _list = list;
        _rows = rows;
        _text = text;
        _say = say;
    }

    /// <summary>What a selected row's item ends in.</summary>
    public const string Suffix = ", selected";

    /// <summary>What a window does with a change besides showing and saying it (axFile's line in its Actions list); called before the words are said.</summary>
    public Action<MarkChange>? Recorded { get; set; }

    public int Count => _marks.Count;

    public bool Contains(string key) => _marks.Contains(key);

    /// <summary>The selected keys, a copy, in no order: a window acts on its rows that <see cref="Contains"/> holds, in the list's order.</summary>
    public IReadOnlyList<string> Keys => _marks.Keys;

    /// <summary>Whether a Shift range is being made.</summary>
    public bool RangeOn => _marks.RangeOn;

    /// <summary>A row's words as its item shows them: with ", selected" while it is.</summary>
    public string Label(string key, string label) => _marks.Contains(key) ? label + Suffix : label;

    /// <summary>The current row, when it is one that can be selected.</summary>
    private bool OnRow(out IReadOnlyList<MarkRow> rows, out int current)
    {
        rows = _rows();
        current = _list.SelectedIndex;
        return current >= 0 && current < rows.Count && rows[current].CanMark;
    }

    /// <summary>Space: the current row selected, or deselected again; false, and nothing done, on a row that cannot be selected, where the window says its own words.</summary>
    public bool Toggle()
    {
        if (!OnRow(out var rows, out var current))
        {
            return false;
        }

        Apply(_marks.Toggle(rows, current));
        return true;
    }

    /// <summary>Shift+Space: from the nearest selected row to the current one; false on a row that cannot be selected.</summary>
    public bool ToHere()
    {
        if (!OnRow(out var rows, out var current))
        {
            return false;
        }

        Apply(_marks.ToHere(rows, current));
        return true;
    }

    /// <summary>Ctrl+A: every row shown that can be selected, "n selected".</summary>
    public void All() => Apply(_marks.All(_rows(), _list.SelectedIndex));

    /// <summary>Ctrl+Shift+A, Escape: no row selected, "None selected".</summary>
    public void None() => Apply(_marks.None(_rows(), _list.SelectedIndex));

    /// <summary>
    /// Shift with Up, Down, Home or End: the current row moves to <paramref name="target"/> (the first or the last
    /// row past either end) and every row from where the range began to there is selected. The items first, the
    /// move after, so that the row reached is read once, with its mark; the move is the user's own
    /// (<see cref="AxKitList.SelectForCommand"/>). At an end, where the key cannot move, the edge sound. False,
    /// and nothing done, when the list has no row.
    /// </summary>
    public bool Extend(int target)
    {
        var rows = _rows();
        var current = _list.SelectedIndex;
        if (rows.Count == 0 || current < 0 || current >= rows.Count)
        {
            return false;
        }

        target = Math.Clamp(target, 0, rows.Count - 1);
        Apply(_marks.Extend(rows, current, target));
        if (target != current)
        {
            _list.SelectForCommand(target);
        }
        else
        {
            // At an end of the list the key cannot move: the edge sound, as for the arrow alone (AX-8.6).
            EventDoor.Raise(EventKind.Edge);
        }

        return true;
    }

    /// <summary>The same level filled again (a read, a new order or filter): the marks on rows it no longer shows are gone; the window fills its items with <see cref="Label"/> after this.</summary>
    public void Keep() => _marks.Keep(_rows());

    /// <summary>Another level shown: no marks.</summary>
    public void Clear() => _marks.Clear();

    /// <summary>The marks as they were before a recorded change (a window's undo of a selection line).</summary>
    public void Restore(IReadOnlyList<string> marks) => Apply(_marks.Restore(marks, _rows()));

    /// <summary>
    /// The four keys that select as they move, as commands for the window's command list: "Select to the next
    /// entry" on Shift+Down and the like, with the window's word for a row and where the keys act.
    /// <paramref name="otherwise"/> runs when the list has no row to move over (an empty folder's one line).
    /// </summary>
    public IReadOnlyList<Command> MoveCommands(string noun, Func<bool> when, Action? otherwise = null)
    {
        Command Move(string which, Keys key, Func<int> target) =>
            new($"Select to the {which} {noun}", key, () =>
            {
                if (!Extend(target()))
                {
                    otherwise?.Invoke();
                }
            })
            { When = when, Mark = CommandMark.Change };

        return
        [
            Move("next", SelectionKeys.SelectDown, () => _list.SelectedIndex + 1),
            Move("previous", SelectionKeys.SelectUp, () => _list.SelectedIndex - 1),
            Move("first", SelectionKeys.SelectToStart, () => 0),
            Move("last", SelectionKeys.SelectToEnd, () => int.MaxValue),
        ];
    }

    /// <summary>One change of the marks applied: the items whose mark flipped rewritten, the window's record of it, and what the key says said.</summary>
    private void Apply(MarkChange change)
    {
        if (change.Changed.Count > 1)
        {
            _list.BeginUpdate();
        }

        foreach (var row in change.Changed)
        {
            _list.Items[row] = _text(row);
        }

        if (change.Changed.Count > 1)
        {
            _list.EndUpdate();
        }

        Recorded?.Invoke(change);
        if (change.Said.Length > 0)
        {
            _say(change.Said);
        }
    }
}
