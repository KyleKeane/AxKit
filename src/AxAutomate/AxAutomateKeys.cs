using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// axAutomate's window-specific keys (SPEC.md AU-4.4; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// the panes, Enter, Right, Left, Tab, Ctrl+Q, F5) and two of its own. A key is changed here and nowhere else: the menu
/// items, the commands of <c>AutomateForm</c> and the F1 text read this table. The bundle-wide keys (F1, the palette,
/// the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>.
/// </summary>
public static class AxAutomateKeys
{
    /// <summary>Open a folder of workflows (AU-4.1), with Windows' folder picker.</summary>
    public const Keys OpenFolder = Keys.Control | Keys.O;

    /// <summary>The workflow's file as text: a script in axDown, a workflow file in axJSON (AU-4.4).</summary>
    public const Keys OpenFile = Keys.Control | Keys.J;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All,
        ("Open a folder of workflows", OpenFolder),
        ("The workflow's file as text", OpenFile),
    ];
}
