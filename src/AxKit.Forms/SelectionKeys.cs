namespace AxKit.Forms;

/// <summary>
/// The keys that select things in a list, the same in every window of axKit that has a selection
/// (docs/axKit/SPEC.md AX-8.16; the maintainer's decision of 2026-11-22: "use space to mean select an item
/// everywhere in axkit", "the standard item selection mechanics everywhere"; axFile's keys of 2026-10-21 until
/// then). A window's key table includes <see cref="All"/>, or the entries of it whose key has no second meaning
/// in another of its panes, and its commands are made on these constants (<see cref="ListSelection"/>): a key is
/// changed here for every window at once.
/// </summary>
public static class SelectionKeys
{
    /// <summary>The current row selected, or deselected again.</summary>
    public const Keys Select = Keys.Space;

    /// <summary>Every row from the nearest selected one to the current one selected.</summary>
    public const Keys SelectTo = Keys.Shift | Keys.Space;

    /// <summary>The range from where it began to the next row selected, as in every Windows list.</summary>
    public const Keys SelectDown = Keys.Shift | Keys.Down;

    public const Keys SelectUp = Keys.Shift | Keys.Up;
    public const Keys SelectToStart = Keys.Shift | Keys.Home;
    public const Keys SelectToEnd = Keys.Shift | Keys.End;

    /// <summary>Every row the list shows selected.</summary>
    public const Keys SelectAll = Keys.Control | Keys.A;

    /// <summary>No row selected.</summary>
    public const Keys SelectNone = Keys.Control | Keys.Shift | Keys.A;

    /// <summary>No row selected, while any is: Escape gives up a selection before it does anything else in a list.</summary>
    public const Keys Clear = Keys.Escape;

    /// <summary>The keys with their names, the same words in every window's table.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Select or deselect (in a list)", Select),
        ("Select all (in a list)", SelectAll),
        ("Deselect all (in a list)", SelectNone),
        ("Select to here, from the nearest selected one (in a list)", SelectTo),
        ("Select to the next (in a list)", SelectDown),
        ("Select to the previous (in a list)", SelectUp),
        ("Select to the first (in a list)", SelectToStart),
        ("Select to the last (in a list)", SelectToEnd),
        ("Deselect all, while any is selected (in a list)", Clear),
    ];

    /// <summary><see cref="All"/> without Space and Escape, for a window where those two keys have a second meaning in another pane and its table names both in one entry.</summary>
    public static IEnumerable<(string Name, Keys Key)> WithoutSpaceAndEscape => All.Where(key => key.Key is not (Select or Clear));
}
