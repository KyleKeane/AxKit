using AxKit.Forms;

namespace AxProcess;

/// <summary>
/// axProcess's window-specific keys (SPEC.md PR-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>ProcessForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. The window is a navigator since
/// 2026-11-22 (docs/axKit/framework.md FW-4.6): Right, Left, Backspace, Enter, Delete, the selection keys and the
/// Context menu's keys are the item window's (<c>AxKit.Forms.ItemKeys</c>, <c>SelectionKeys</c>), named in
/// <see cref="All"/> by what they do here; they act in the list. Tab and Shift+Tab, which went round the three
/// views until then, are the bundle's focus keys and come back to the list. Three of a navigator's keys are
/// axProcess's own: F5 reads again off the UI thread, F7 (the bundle's read in full) is the summary; and four it has no use for are no
/// keys of the window (F2, Ctrl+Z, Ctrl+N, Ctrl+Shift+N; <c>ProcessForm.KeysWithoutUse</c>).
/// </summary>
public static class AxProcessKeys
{
    /// <summary>The three lists (PR-2.2), each a jump from anywhere in the window; the key names the list when it already shows (axClaude D26).</summary>
    public const Keys ViewProcesses = Keys.Alt | Keys.D1;

    public const Keys ViewServices = Keys.Alt | Keys.D2;
    public const Keys ViewStartup = Keys.Alt | Keys.D3;

    public const Keys Refresh = Keys.F5;

    public const Keys ShowProgram = Keys.Control | Keys.E;
    public const Keys CopyLine = Keys.Control | Keys.C;

    /// <summary>The summary, on the bundle's read in full (F7, AX-8.18 rule 5; Ctrl+Q until 2026-11-26), so no row of <see cref="All"/>.</summary>
    public const Keys Summary = BundleKeys.ReadInFull;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Processes", ViewProcesses),
        ("Services", ViewServices),
        ("Startup", ViewStartup),
        ("Refresh", Refresh),
        // The navigator's keys (ItemKeys), by what they do here.
        ("Into the list, at This computer (in the list)", ItemKeys.Into),
        ("Up to This computer (in the list)", ItemKeys.Up),
        ("Up to This computer (in the list, Backspace)", ItemKeys.UpAlso),
        ("Details, or into the list at This computer (in the list)", ItemKeys.Open),
        ("End the process, or in Startup remove the startup program; with rows selected, all of them (in the list)", ItemKeys.Delete),
        ("Clear the filter (in the list)", ItemKeys.ClearFilter),
        // The bundle's selection keys (AX-8.16), on processes and startup programs.
        .. SelectionKeys.All,
        ("Show the program in axFile", ShowProgram),
        ("Copy the line", CopyLine),
        ("Context menu (in the list)", ItemKeys.ItemMenu),
        ("Context menu (in the list, the Applications key)", ItemKeys.ItemMenuAlso),
    ];
}
