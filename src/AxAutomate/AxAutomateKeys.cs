using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// axAutomate's window-specific keys (SPEC.md AU-4.4; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// the panes, Enter, Right, Left, Tab, F5) and its own. Ctrl+R runs (AU-D8; F5 until 2026-11-26, when F5 became the
/// item apps' Refresh here too), and Escape, the detail pane's way back, also stops a run. A key is changed here and
/// nowhere else: the menu items, the commands of <c>AutomateForm</c> and the F1 text read this table. The bundle-wide
/// keys (F1, the palette, the control keys, find, F7, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>.
/// </summary>
public static class AxAutomateKeys
{
    /// <summary>Open a folder of workflows (AU-4.1), with Windows' folder picker.</summary>
    public const Keys OpenFolder = Keys.Control | Keys.O;

    /// <summary>The workflow's file as text: a script in axDown, a workflow file in axJSON (AU-4.4).</summary>
    public const Keys OpenFile = Keys.Control | Keys.J;

    /// <summary>Run the workflow in hand, or at a folder's level every workflow of the folder (AU-4.4): R for run, an act on what you are on (AX-8.18 rule 2; F5 until 2026-11-26).</summary>
    public const Keys Run = Keys.Control | Keys.R;

    /// <summary>During a run: stop it after the step going on (AU-4.4); otherwise, in the detail pane, back to the navigator.</summary>
    public const Keys StopRun = Keys.Escape;

    /// <summary>The report of the workflow's last run in this window, every step with what was said and focused (AU-4.5); F8, read with details (AX-8.18 rule 5; Ctrl+R until 2026-11-26).</summary>
    public const Keys Report = Keys.F8;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All.Where(shared => shared.Key is not StopRun),
        // The bundle's selection keys (AX-8.16, AU-4.4a), on the workflows of a folder; Space is in ItemKeys.All, and
        // Escape, which also gives a selection up, is Stop the run's below.
        .. SelectionKeys.WithoutSpaceAndEscape,
        ("Open a folder of workflows", OpenFolder),
        ("The workflow's file as text", OpenFile),
        ("Run the workflow, or every workflow of the folder", Run),
        ("Stop the run; in the detail pane, back to the navigator", StopRun),
        ("The report of the workflow's last run", Report),
    ];
}
