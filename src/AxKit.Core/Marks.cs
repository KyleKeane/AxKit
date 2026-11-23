namespace AxKit.Core;

/// <summary>
/// A row of a list as the marks see it (docs/axKit/SPEC.md AX-8.16): the key its mark is kept by (a file's path, an
/// item's file), its name, and whether it can be selected at all (a heading and, in axTask, a list cannot: the keys
/// that select many pass over it).
/// </summary>
public sealed record MarkRow(string Key, string Name, bool CanMark = true);

/// <summary>
/// What one change of the marks did (AX-8.16), for the window to apply: the rows of the list whose mark flipped, so
/// that those items alone are rewritten, and what the key says, empty when it says nothing. Until 2026-11-22 it
/// also carried the line axFile wrote into its history of operations for the change and the marks before it for
/// that line's undo; the history holds only what changed files since (docs/axKit/framework.md, section 9).
/// </summary>
public sealed record MarkChange(IReadOnlyList<int> Changed, string Said);

/// <summary>
/// The marks on a list's rows (docs/axKit/SPEC.md AX-8.16; from axFile, AF-3.7, on its second use, the item windows,
/// 2026-11-22), the selection a window's commands act on: a set of keys among the rows the list shows, and the Shift
/// range being made, where it began, where it has reached and the marks there were before it. Each key of
/// <c>AxKit.Forms/SelectionKeys</c> is one method over the shown rows and the current row, returning a
/// <see cref="MarkChange"/> the window applies through <c>AxKit.Forms/ListSelection</c>. The marks are on shown rows
/// only: what the list leaves out (<see cref="Keep"/>) is not marked, so that the count said and the rows acted on
/// are one set. The words every window says for a selection are made here and nowhere else. Pure, tested.
/// </summary>
public sealed class Marks
{
    private readonly HashSet<string> _marked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The marks there were before the Shift range began, which the range keeps.</summary>
    private HashSet<string> _base = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Where the Shift range began and where it has reached, rows of the list; -1 while none is being made.</summary>
    private int _anchor = -1;
    private int _end = -1;

    public int Count => _marked.Count;

    public bool Contains(string key) => _marked.Contains(key);

    /// <summary>The marked keys, a copy.</summary>
    public IReadOnlyList<string> Keys => [.. _marked];

    /// <summary>"1 item", "3 items".</summary>
    public static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

    /// <summary>Space: the current row marked, or unmarked again; "Selected" or "Deselected", with the count when more than that one is marked. The window asks only for a row that can be marked.</summary>
    public MarkChange Toggle(IReadOnlyList<MarkRow> shown, int current)
    {
        var row = shown[current];
        var on = _marked.Add(row.Key);
        if (!on)
        {
            _marked.Remove(row.Key);
        }

        EndRange();
        var word = on ? "Selected" : "Deselected";
        var said = _marked.Count > (on ? 1 : 0) ? $"{word}, {_marked.Count} selected" : word;
        return new([current], said);
    }

    /// <summary>Ctrl+A: every row shown that can be marked, "n selected".</summary>
    public MarkChange All(IReadOnlyList<MarkRow> shown)
    {
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (shown[i].CanMark && _marked.Add(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, $"{_marked.Count} selected");
    }

    /// <summary>Ctrl+Shift+A, Escape: every mark gone, "None selected".</summary>
    public MarkChange None(IReadOnlyList<MarkRow> shown)
    {
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (_marked.Contains(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        _marked.Clear();
        EndRange();
        return new(changed, "None selected");
    }

    /// <summary>
    /// Shift+Space: every row from the nearest marked row to the current one marked, above or below, the one above
    /// when both are as near, "Selected, n selected"; with no other row marked it is Space (<see cref="Toggle"/>).
    /// A row between them that cannot be marked is passed over.
    /// </summary>
    public MarkChange ToHere(IReadOnlyList<MarkRow> shown, int current)
    {
        var nearest = -1;
        for (var distance = 1; nearest < 0 && (current - distance >= 0 || current + distance < shown.Count); distance++)
        {
            if (current - distance >= 0 && _marked.Contains(shown[current - distance].Key))
            {
                nearest = current - distance;
            }
            else if (current + distance < shown.Count && _marked.Contains(shown[current + distance].Key))
            {
                nearest = current + distance;
            }
        }

        if (nearest < 0)
        {
            return Toggle(shown, current);
        }

        var changed = new List<int>();
        for (var i = Math.Min(nearest, current); i <= Math.Max(nearest, current); i++)
        {
            if (shown[i].CanMark && _marked.Add(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, $"Selected, {_marked.Count} selected");
    }

    /// <summary>
    /// Shift with Up, Down, Home or End: the current row moves from <paramref name="current"/> to
    /// <paramref name="target"/>, a row of the list, and every row between where the range began and there is
    /// marked, one that cannot be marked passed over. The range begins at the current row when none is being made or
    /// the current row is not where the last Shift key left it, and keeps the marks made before it; moving back
    /// shrinks it. Nothing is said: each row passed reads ", selected" by itself.
    /// </summary>
    public MarkChange Extend(IReadOnlyList<MarkRow> shown, int current, int target)
    {
        if (_anchor < 0 || current != _end)
        {
            _anchor = current;
            _base = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        }

        _end = target;
        var before = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        _marked.Clear();
        _marked.UnionWith(_base);
        for (var i = Math.Min(_anchor, target); i <= Math.Max(_anchor, target); i++)
        {
            if (shown[i].CanMark)
            {
                _marked.Add(shown[i].Key);
            }
        }

        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (before.Contains(shown[i].Key) != _marked.Contains(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        return new(changed, string.Empty);
    }

    /// <summary>The list made again (a read, a new order or filter): the marks on rows it no longer shows are gone, and the range ends. Nothing said; the window fills its items anew.</summary>
    public void Keep(IReadOnlyList<MarkRow> shown)
    {
        _marked.IntersectWith(shown.Where(row => row.CanMark).Select(row => row.Key));
        EndRange();
    }

    /// <summary>Another level shown (another folder, another list): no marks, no range.</summary>
    public void Clear()
    {
        _marked.Clear();
        EndRange();
    }

    private void EndRange()
    {
        _anchor = -1;
        _end = -1;
    }
}
