namespace AxKit.Core;

/// <summary>
/// A row of a list as the marks see it (docs/axKit/SPEC.md AX-8.16): the key its mark is kept by (a file's path, an
/// item's file), its name for the words, and whether it can be selected at all (a heading and, in axTask, a list
/// cannot: the keys that select many pass over it).
/// </summary>
public sealed record MarkRow(string Key, string Name, bool CanMark = true);

/// <summary>
/// What one change of the marks did (AX-8.16; axFile's AF-3.7, AF-9.2), for the window to apply: the rows of the
/// list whose mark flipped, so that those items alone are rewritten; the marks before the change, which a window
/// that records it (axFile's Actions list) puts back on undo; the line such a window records, null when nothing is
/// recorded (nothing changed, the marks kept through a read, the marks put back); whether a Shift range began with
/// it, a new line, rather than went on, its line rewritten; the row the line leads to; and what the key says, empty
/// when it says nothing.
/// </summary>
public sealed record MarkChange(IReadOnlyList<int> Changed, IReadOnlyList<string> Before, string? Line, bool RangeBegins, string? Select, string Said);

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

    /// <summary>The marks there were before the Shift range began, which the range keeps and its line's undo puts back.</summary>
    private HashSet<string> _base = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Where the Shift range began and where it has reached, rows of the list; -1 while none is being made.</summary>
    private int _anchor = -1;
    private int _end = -1;

    public int Count => _marked.Count;

    public bool Contains(string key) => _marked.Contains(key);

    /// <summary>The marked keys, a copy.</summary>
    public IReadOnlyList<string> Keys => [.. _marked];

    /// <summary>Whether a Shift range is being made: the next Shift key goes on with it while the current row is where it reached.</summary>
    public bool RangeOn => _anchor >= 0;

    /// <summary>"1 item", "3 items".</summary>
    public static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

    /// <summary>Space: the current row marked, or unmarked again; "Selected" or "Deselected", with the count when more than that one is marked. The window asks only for a row that can be marked.</summary>
    public MarkChange Toggle(IReadOnlyList<MarkRow> shown, int current)
    {
        var before = Keys;
        var row = shown[current];
        var on = _marked.Add(row.Key);
        if (!on)
        {
            _marked.Remove(row.Key);
        }

        EndRange();
        var word = on ? "Selected" : "Deselected";
        var said = _marked.Count > (on ? 1 : 0) ? $"{word}, {_marked.Count} selected" : word;
        return new([current], before, $"{word} {row.Name}", false, row.Name, said);
    }

    /// <summary>Ctrl+A: every row shown that can be marked, "n selected"; the line "Selected all, n items", none when nothing changed.</summary>
    public MarkChange All(IReadOnlyList<MarkRow> shown, int current)
    {
        var before = Keys;
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (shown[i].CanMark && _marked.Add(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, before, changed.Count > 0 ? $"Selected all, {Items(_marked.Count)}" : null, false, NameAt(shown, current), $"{_marked.Count} selected");
    }

    /// <summary>Ctrl+Shift+A, Escape: every mark gone, "None selected"; the line "Deselected all, n items", none when there was none.</summary>
    public MarkChange None(IReadOnlyList<MarkRow> shown, int current)
    {
        var before = Keys;
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
        return new(changed, before, before.Count > 0 ? $"Deselected all, {Items(before.Count)}" : null, false, NameAt(shown, current), "None selected");
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

        var before = Keys;
        var changed = new List<int>();
        var counted = 0;
        for (var i = Math.Min(nearest, current); i <= Math.Max(nearest, current); i++)
        {
            if (!shown[i].CanMark)
            {
                continue;
            }

            counted++;
            if (_marked.Add(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, before, $"Selected {Items(counted)}", false, shown[current].Name, $"Selected, {_marked.Count} selected");
    }

    /// <summary>
    /// Shift with Up, Down, Home or End: the current row moves from <paramref name="current"/> to
    /// <paramref name="target"/>, a row of the list, and every row between where the range began and there is
    /// marked, one that cannot be marked passed over. The range begins at the current row when none is being made or
    /// the current row is not where the last Shift key left it, and keeps the marks made before it; moving back
    /// shrinks it. Nothing is said: each row passed reads ", selected" by itself. The line is "Selected n items",
    /// counting the rows of the range that can be marked, new when the range begins and rewritten as it goes on;
    /// its undo puts back the marks before the range.
    /// </summary>
    public MarkChange Extend(IReadOnlyList<MarkRow> shown, int current, int target)
    {
        var begins = _anchor < 0 || current != _end;
        if (begins)
        {
            _anchor = current;
            _base = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        }

        _end = target;
        var before = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        _marked.Clear();
        _marked.UnionWith(_base);
        var counted = 0;
        for (var i = Math.Min(_anchor, target); i <= Math.Max(_anchor, target); i++)
        {
            if (shown[i].CanMark)
            {
                counted++;
                _marked.Add(shown[i].Key);
            }
        }

        return new(Flipped(shown, before), [.. _base], $"Selected {Items(counted)}", begins, shown[target].Name, string.Empty);
    }

    /// <summary>The list made again (a read, a new order or filter): the marks on rows it no longer shows are gone, and the range ends. No line, nothing said; the window fills its items anew.</summary>
    public MarkChange Keep(IReadOnlyList<MarkRow> shown)
    {
        var before = Keys;
        _marked.IntersectWith(shown.Where(row => row.CanMark).Select(row => row.Key));
        EndRange();
        return new([], before, null, false, null, string.Empty);
    }

    /// <summary>The marks as they were before a recorded change (axFile's Ctrl+Z on a selection line, AF-9.3), among the rows shown (rows gone since are skipped), the range ended; "Undone: n selected" or "Undone: none selected".</summary>
    public MarkChange Restore(IReadOnlyList<string> marks, IReadOnlyList<MarkRow> shown)
    {
        var before = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        var wanted = new HashSet<string>(marks, StringComparer.OrdinalIgnoreCase);
        _marked.Clear();
        _marked.UnionWith(shown.Where(row => row.CanMark).Select(row => row.Key).Where(wanted.Contains));
        EndRange();
        return new(Flipped(shown, before), [.. before], null, false, null, _marked.Count > 0 ? $"Undone: {_marked.Count} selected" : "Undone: none selected");
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

    /// <summary>The rows whose mark differs from <paramref name="before"/>.</summary>
    private List<int> Flipped(IReadOnlyList<MarkRow> shown, HashSet<string> before)
    {
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (before.Contains(shown[i].Key) != _marked.Contains(shown[i].Key))
            {
                changed.Add(i);
            }
        }

        return changed;
    }

    private static string? NameAt(IReadOnlyList<MarkRow> shown, int current) => current >= 0 && current < shown.Count ? shown[current].Name : null;
}
