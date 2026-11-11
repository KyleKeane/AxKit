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
        .. Enumerable.Range(1, 9).Select(digit => ($"Action key {digit}", ActionKey(digit))),
    ];
}
