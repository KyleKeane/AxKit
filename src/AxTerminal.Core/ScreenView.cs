using System.Text.RegularExpressions;
using AxKit.Core.Vt;

namespace AxTerminal.Core;

/// <summary>One row of the screen's text replaced: where it starts in the text, how long it was, and its new text.</summary>
public sealed record RowEdit(int Start, int OldLength, string Text);

/// <summary>
/// The screen view's text and words (SPEC.md AT-9.2, AT-9.3, AT-9.5): the page's rows as one text, one line per row
/// with Windows' line breaks; where a row and column of the page stand in it; the rows that changed as edits, so that
/// a frame replaces only those; the name of the program that holds the screen; and the words of the two ways to be in
/// the view. Pure: the window puts the text into its control and says the words.
/// </summary>
public static class ScreenView
{
    /// <summary>Said, and in the status bar, while the keys stay in the text and the arrows read the page.</summary>
    public const string ReadingWords = "Reading the screen";

    /// <summary>Said, and in the status bar, while every key goes to the program: "Keys to edit".</summary>
    public static string KeysWords(string program) => "Keys to " + program;

    /// <summary>What NVDA calls the view: "Screen of edit".</summary>
    public static string Name(string program) => "Screen of " + program;

    /// <summary>The page as the text of the view: its rows, one line each.</summary>
    public static string Whole(IReadOnlyList<string> rows) => string.Join("\r\n", rows);

    /// <summary>Where the page's row and column stand in the text; a column past the row's end stands at its end, since the row's trailing spaces are not in the text.</summary>
    public static int Index(IReadOnlyList<string> rows, int row, int column)
    {
        row = Math.Clamp(row, 0, Math.Max(0, rows.Count - 1));
        var index = 0;
        for (var r = 0; r < row && r < rows.Count; r++)
        {
            index += rows[r].Length + 2;
        }

        return index + (rows.Count == 0 ? 0 : Math.Clamp(column, 0, rows[row].Length));
    }

    /// <summary>The row and column of a place in the text (<see cref="Index"/> backwards); a place on a line break is the end of its row.</summary>
    public static (int Row, int Column) Position(IReadOnlyList<string> rows, int index)
    {
        var start = 0;
        for (var r = 0; r < rows.Count; r++)
        {
            if (index <= start + rows[r].Length || r == rows.Count - 1)
            {
                return (r, Math.Clamp(index - start, 0, rows[r].Length));
            }

            start += rows[r].Length + 2;
        }

        return (0, 0);
    }

    /// <summary>
    /// The rows of <paramref name="next"/> that differ from those <paramref name="shown"/>, as edits of the text shown,
    /// the last row first, so that each edit's start is still right when the ones before it are made. A page of
    /// another height is one edit of the whole text.
    /// </summary>
    public static IReadOnlyList<RowEdit> Edits(IReadOnlyList<string> shown, IReadOnlyList<string> next)
    {
        if (shown.Count != next.Count)
        {
            return [new RowEdit(0, Whole(shown).Length, Whole(next))];
        }

        var edits = new List<RowEdit>();
        var start = 0;
        for (var r = 0; r < shown.Count; r++)
        {
            if (shown[r] != next[r])
            {
                edits.Add(new RowEdit(start, shown[r].Length, next[r]));
            }

            start += shown[r].Length + 2;
        }

        edits.Reverse();
        return edits;
    }

    /// <summary>
    /// The program the command started, for the view's name and words: the command's first word without its folder
    /// and its extension, "edit" of "edit notes.txt" and of "C:\Windows\System32\edit.exe"; "the program" when there
    /// is no command (one sent without recording, or a program the shell started by itself).
    /// </summary>
    public static string ProgramName(string? command)
    {
        var text = command?.Trim() ?? string.Empty;
        if (text.Length == 0 || text == TerminalModel.UnrecordedCommand)
        {
            return "the program";
        }

        var first = text[0] is '"' or '\'' && text.IndexOf(text[0], 1) is > 0 and var close ? text[1..close] : text.Split(' ', 2)[0];
        var name = first.Replace('/', '\\').Split('\\')[^1];
        var dot = name.LastIndexOf('.');
        if (dot > 0)
        {
            name = name[..dot];
        }

        return name.Length == 0 ? "the program" : name;
    }

    /// <summary>A marked run as it is said: the gaps of two spaces or more between its parts as a pause, "New File, Ctrl+N" of "New File    Ctrl+N".</summary>
    public static string Spoken(string run) => Regex.Replace(run.Trim(), " {2,}", ", ");
}

/// <summary>
/// What the screen view says by itself (SPEC.md AT-9.4): a marked run (an open menu's selected entry, a dialog's
/// button in focus) once, when it is at a row and a first cell where no run was marked on the page before; never
/// again while it stays, whatever the program redraws. A page with no run forgets the places, so that a menu closed
/// and opened again says its entry again. Pure: given each page at a frame's end, it gives the words or null.
/// </summary>
public sealed class ScreenMarks
{
    private HashSet<(int Row, int Start)> _places = [];

    /// <summary>The words for the runs of this page that moved, joined by a full stop; null when none moved.</summary>
    public string? Said(ScreenPage page)
    {
        var places = new HashSet<(int Row, int Start)>();
        var said = new List<string>();
        foreach (var run in page.Marked)
        {
            places.Add((run.Row, run.Start));
            if (!_places.Contains((run.Row, run.Start)) && ScreenView.Spoken(run.Text) is { Length: > 0 } words)
            {
                said.Add(words);
            }
        }

        _places = places;
        return said.Count == 0 ? null : string.Join(". ", said);
    }

    /// <summary>A new screen view: nothing marked before it.</summary>
    public void Reset() => _places = [];
}
