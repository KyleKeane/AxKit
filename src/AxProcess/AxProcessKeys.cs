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
/// axProcess's own: F5 reads again off the UI thread, Ctrl+Q is the summary; and four it has no use for are no
/// keys of the window (F2, Ctrl+Z, Ctrl+N, Ctrl+Shift+N; <c>ProcessForm.KeysWithoutUse</c>).
/// </summary>
public static class AxProcessKeys
{
    /// <summary>The three lists (PR-2.2), each a jump from anywhere in the window; the key names the list when it already shows (axClaude D26).</summary>
    public const Keys ViewProcesses = Keys.Alt | Keys.D1;

    public const Keys ViewServices = Keys.Alt | Keys.D2;
    public const Keys ViewStartup = Keys.Alt | Keys.D3;

    /// <summary>The same three on Ctrl, their key until 2026-11-16, when they moved to Alt+1, 2 and 3 (bundle AX-8.14): further keys of the same commands, shown in no menu, with nothing extra said.</summary>
    public const Keys ViewProcessesAlso = Keys.Control | Keys.D1;

    public const Keys ViewServicesAlso = Keys.Control | Keys.D2;
    public const Keys ViewStartupAlso = Keys.Control | Keys.D3;

    public const Keys Refresh = Keys.F5;

    public const Keys ShowProgram = Keys.Control | Keys.E;
    public const Keys CopyLine = Keys.Control | Keys.C;
    public const Keys Summary = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Processes", ViewProcesses),
        ("Services", ViewServices),
        ("Startup", ViewStartup),
        ("Processes (Ctrl+1, the older key)", ViewProcessesAlso),
        ("Services (Ctrl+2, the older key)", ViewServicesAlso),
        ("Startup (Ctrl+3, the older key)", ViewStartupAlso),
        ("Refresh", Refresh),
        // The navigator's keys (ItemKeys), by what they do here.
        ("Into the list, at This computer (in the list)", ItemKeys.Into),
        ("Up to This computer (in the list)", ItemKeys.Up),
        ("Up to This computer (in the list, Backspace)", ItemKeys.UpAlso),
        ("Details, or into the list at This computer (in the list)", ItemKeys.Open),
        ("End the process, or in Startup remove the startup program; with rows selected, all of them (in the list)", ItemKeys.Delete),
        // The bundle's selection keys (AX-8.16), on processes and startup programs.
        .. SelectionKeys.All,
        ("Show the program in axFile", ShowProgram),
        ("Copy the line", CopyLine),
        ("Summary", Summary),
        ("Context menu (in the list)", ItemKeys.ItemMenu),
        ("Context menu (in the list, the Applications key)", ItemKeys.ItemMenuAlso),
    ];
}
