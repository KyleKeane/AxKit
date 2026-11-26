using System.Text;

namespace AxKit.Core.Vt;

/// <summary>A marked run of a row of the screen page (docs/axTerminal/SPEC.md AT-9.4): its row, its first cell, its length in cells and its text, trimmed.</summary>
public sealed record MarkedRun(int Row, int Start, int Length, string Text);

/// <summary>
/// The alternate screen as a page to read (docs/axTerminal/SPEC.md AT-9.2, AT-9.4), taken whole at one moment, a
/// frame's end: one text per row, its trailing spaces left out and the box-drawing and block characters (U+2500 to
/// U+259F) as spaces, since a screen reader would name each one; the cursor's row and column; and the marked runs.
/// A run is cells of one background (reverse video counted in it) between cells of another. It is marked when the
/// cells on both sides of it share one background and that background is not the page's own, the one most cells
/// have: an entry selected inside an open menu, a button in focus inside a dialog, and not the menu or the dialog
/// drawn on the screen. A run in reverse video between cells that are not is marked on the page's own background too.
/// A run at either end of its row has no cells on one side and is never marked. Pure: it only reads the screen.
/// </summary>
public sealed record ScreenPage(IReadOnlyList<string> Rows, int CursorRow, int CursorColumn, IReadOnlyList<MarkedRun> Marked)
{
    /// <summary>The page of the screen's alternate buffer, or null while the program writes on the main one.</summary>
    public static ScreenPage? Read(Screen screen)
    {
        if (screen.AlternateRows is not { } rows)
        {
            return null;
        }

        var own = OwnBackground(rows);
        var texts = new List<string>(rows.Length);
        var marked = new List<MarkedRun>();
        for (var r = 0; r < rows.Length; r++)
        {
            texts.Add(Text(rows[r], 0, rows[r].Cells.Length).TrimEnd());
            AddMarkedRuns(r, rows[r], own, marked);
        }

        var (cursorRow, cursorColumn) = screen.WritingCursor;
        return new ScreenPage(texts, cursorRow, cursorColumn, marked);
    }

    /// <summary>The background most cells of the page have.</summary>
    private static int OwnBackground(Row[] rows)
    {
        var counts = new Dictionary<int, int>();
        foreach (var row in rows)
        {
            foreach (var background in row.Backgrounds!)
            {
                counts[background] = counts.GetValueOrDefault(background) + 1;
            }
        }

        var own = 0;
        var most = -1;
        foreach (var (background, count) in counts)
        {
            if (count > most)
            {
                own = background;
                most = count;
            }
        }

        return own;
    }

    private static void AddMarkedRuns(int r, Row row, int own, List<MarkedRun> marked)
    {
        var backgrounds = row.Backgrounds!;

        // The runs of one background, as their first cells; a run ends where the next begins.
        var starts = new List<int> { 0 };
        for (var x = 1; x < backgrounds.Length; x++)
        {
            if (backgrounds[x] != backgrounds[x - 1])
            {
                starts.Add(x);
            }
        }

        for (var i = 1; i < starts.Count - 1; i++)
        {
            var start = starts[i];
            var end = starts[i + 1];
            var inside = backgrounds[start];
            var left = backgrounds[start - 1];
            var right = backgrounds[end];
            var reverse = (inside & Screen.ReverseBit) != 0 && (left & Screen.ReverseBit) == 0;
            if (left == right && (left != own || reverse))
            {
                marked.Add(new MarkedRun(r, start, end - start, Text(row, start, end).Trim()));
            }
        }
    }

    /// <summary>The cells from one column to another as text: a blank cell a space, the second half of a wide character nothing, a box-drawing or block character a space.</summary>
    private static string Text(Row row, int from, int toExclusive)
    {
        var sb = new StringBuilder(toExclusive - from);
        for (var x = from; x < toExclusive; x++)
        {
            var cell = row.Cells[x];
            if (cell is null || (cell[0] >= '─' && cell[0] <= '▟'))
            {
                sb.Append(' ');
            }
            else if (cell != Row.Continuation)
            {
                sb.Append(cell);
            }
        }

        return sb.ToString();
    }
}
