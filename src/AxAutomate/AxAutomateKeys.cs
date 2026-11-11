using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// axAutomate's window-specific keys (SPEC.md AU-4.4; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// the panes, Enter, Right, Left, Tab, Ctrl+Q) and its own. F5, the item apps' Refresh, runs here (AU-D8), and Escape,
/// the detail pane's way back, also stops a run. A key is changed here and nowhere else: the menu items, the commands
/// of <c>AutomateForm</c> and the F1 text read this table. The bundle-wide keys (F1, the palette, the control keys,
/// find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>.
/// </summary>
public static class AxAutomateKeys
{
    /// <summary>Open a folder of workflows (AU-4.1), with Windows' folder picker.</summary>
    public const Keys OpenFolder = Keys.Control | Keys.O;

    /// <summary>The workflow's file as text: a script in axDown, a workflow file in axJSON (AU-4.4).</summary>
    public const Keys OpenFile = Keys.Control | Keys.J;

    /// <summary>Run the workflow in hand, or at a folder's level every workflow of the folder (AU-4.4).</summary>
    public const Keys Run = Keys.F5;

    /// <summary>During a run: stop it after the step going on (AU-4.4); otherwise, in the detail pane, back to the navigator.</summary>
    public const Keys StopRun = Keys.Escape;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All.Where(shared => shared.Key is not (Run or StopRun)),
        ("Open a folder of workflows", OpenFolder),
        ("The workflow's file as text", OpenFile),
        ("Run the workflow, or every workflow of the folder", Run),
        ("Stop the run; in the detail pane, back to the navigator", StopRun),
    ];
}
