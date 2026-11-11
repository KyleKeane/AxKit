using AxKit.Forms;

namespace AxTask;

/// <summary>
/// axTask's window-specific keys (SPEC.md TK-7; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// docs/axKit/item-apps.md IA-3) and axTask's own. A key is changed here or in <see cref="ItemKeys"/> and nowhere else:
/// the commands (<c>ItemForm.AddSharedKeys</c>, the menus of <c>TaskForm</c>) and the F1
/// text read these tables. The bundle-wide keys (F1, the palette, find, Ctrl+S, text size) are <c>BundleKeys</c>.
/// </summary>
public static class AxTaskKeys
{
    public const Keys OpenInAxJson = Keys.Control | Keys.J;
    public const Keys ShowInAxFile = Keys.Control | Keys.E;
    public const Keys HideDone = Keys.Control | Keys.Shift | Keys.D;

    /// <summary>All tasks; Ctrl+0 since the digits 1 to 9 became the list's action keys (docs/axKit/reports.md RP-8.4, 2026-11-07).</summary>
    public const Keys AllTasks = Keys.Control | Keys.D0;

    /// <summary>Words found in every list, the user's own and every project's (TK-7b.4).</summary>
    public const Keys FindEverywhere = Keys.Control | Keys.Shift | Keys.F;

    /// <summary>On a list among the sections: its section chosen (TK-3a.3).</summary>
    public const Keys MoveToSection = Keys.Control | Keys.M;

    /// <summary>Ctrl+1 to Ctrl+9: the action the list's <c>list.json</c> assigns to the digit (RP-8.1).</summary>
    public static Keys ActionKey(int digit) => Keys.Control | (Keys)((int)Keys.D0 + digit);

    /// <summary>
    /// At a top level, the next heading of level 1 or 2 (TK-3a.2, TK-D14): the same keys as action keys 1 and 2, which
    /// act in a list only, so that each place gives them one meaning.
    /// </summary>
    public const Keys NextHeading1 = Keys.Control | Keys.D1;

    public const Keys NextHeading2 = Keys.Control | Keys.D2;

    /// <summary>At a top level, the heading of level 1 or 2 before the selection (TK-3a.2).</summary>
    public const Keys PreviousHeading1 = Keys.Control | Keys.Shift | Keys.D1;

    public const Keys PreviousHeading2 = Keys.Control | Keys.Shift | Keys.D2;

    /// <summary>At a top level, the next and previous heading of level 3, the symbols under Tags (TK-D20); in a list Ctrl+3 is action key 3.</summary>
    public const Keys NextHeading3 = Keys.Control | Keys.D3;

    public const Keys PreviousHeading3 = Keys.Control | Keys.Shift | Keys.D3;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All,
        ("Open in axJSON", OpenInAxJson),
        ("Show in axFile", ShowInAxFile),
        ("Hide or show done tasks", HideDone),
        ("All tasks", AllTasks),
        ("Move the list to a section", MoveToSection),
        ("Find in every list", FindEverywhere),
        // Ctrl+1 to Ctrl+3 are one entry each: the next heading at a top level, the action key in a list (TK-D14, TK-D20).
        ("Action key 1 in a list, the next heading of level 1 at a top level", NextHeading1),
        ("Action key 2 in a list, the next heading of level 2 at a top level", NextHeading2),
        ("Action key 3 in a list, the next heading of level 3 at a top level", NextHeading3),
        .. Enumerable.Range(4, 6).Select(digit => ($"Action key {digit}", ActionKey(digit))),
        ("Previous heading level 1", PreviousHeading1),
        ("Previous heading level 2", PreviousHeading2),
        ("Previous heading level 3", PreviousHeading3),
    ];
}
