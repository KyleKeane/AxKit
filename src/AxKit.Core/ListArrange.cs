namespace AxKit.Core;

/// <summary>
/// A row of a list as the filter and the sort see it (docs/axKit/framework.md FW-4.10a): its name, whether it is a
/// list one goes into (a folder), and whether it is a heading, which is no thing of the list and only stands over
/// the rows that follow it.
/// </summary>
public sealed record ArrangeRow(string Name, bool IsList = false, bool IsHeading = false);

/// <summary>
/// The filter and the sort of a navigator's list (docs/axKit/framework.md FW-4.10a; from axFile, AF-3.8, for every
/// navigator, 2026-11-22: "i want any list to be able to be filtered or sorted"): which of a level's rows are
/// shown, and in which order, as positions in the rows given. The window's side is
/// <c>AxKit.Forms/ItemForm.Arrange.cs</c>. Pure, tested.
/// </summary>
public static class ListArrange
{
    /// <summary>
    /// The rows by name, in Windows' natural order (2 before 10), lists before the other rows as folders come
    /// before files. A heading stays where it is, and the rows under it, up to the next heading, are ordered
    /// among themselves.
    /// </summary>
    public static IReadOnlyList<int> ByName(IReadOnlyList<ArrangeRow> rows)
    {
        var sorted = new List<int>(rows.Count);
        var block = new List<int>();
        void EndBlock()
        {
            sorted.AddRange(block.OrderBy(index => !rows[index].IsList).ThenBy(index => rows[index].Name, NaturalOrder.Instance));
            block.Clear();
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].IsHeading)
            {
                EndBlock();
                sorted.Add(i);
            }
            else
            {
                block.Add(i);
            }
        }

        EndBlock();
        return sorted;
    }

    /// <summary>
    /// Whether a name passes the filter: it contains the text, without regard to case; or, when the text holds a
    /// <c>*</c> or a <c>?</c>, the whole name matches it as a pattern, <c>*</c> any characters and <c>?</c> one
    /// (<c>*.mp3</c>, <c>chapter ?</c>; asked 2026-11-22: choose entries by a pattern, then act on them all).
    /// </summary>
    public static bool Passes(string name, string text) =>
        text.AsSpan().IndexOfAny('*', '?') >= 0
            ? System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(text, name, ignoreCase: true)
            : name.Contains(text, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The rows whose name passes the filter (<see cref="Passes"/>), in the order given; all of
    /// them for an empty text. A heading stays while a row under it, up to the next heading, passes; a heading's
    /// own name is not looked at.
    /// </summary>
    public static IReadOnlyList<int> Filter(IReadOnlyList<ArrangeRow> rows, string text)
    {
        if (text.Length == 0)
        {
            return [.. Enumerable.Range(0, rows.Count)];
        }

        var kept = new List<int>();
        var heading = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].IsHeading)
            {
                heading = i;
                continue;
            }

            if (!Passes(rows[i].Name, text))
            {
                continue;
            }

            if (heading >= 0)
            {
                kept.Add(heading);
                heading = -1;
            }

            kept.Add(i);
        }

        return kept;
    }

    /// <summary>The list's name under a filter: the level's own, the text, and how many rows pass it, "Home, 4 items, filter tap, 1 shown"; the level's own without one.</summary>
    public static string Name(string name, string text, int shown) =>
        text.Length == 0 ? name : $"{name}, filter {text}, {shown} shown";

    /// <summary>The one line of a level whose rows all fail the filter: "No names contain chap".</summary>
    public static string NoMatch(string text) => "No names contain " + text;
}
