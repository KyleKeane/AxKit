namespace AxProcess;

/// <summary>
/// axProcess's window-specific keys (SPEC.md PR-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>ProcessForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. Enter and Delete act in the list.
/// </summary>
public static class AxProcessKeys
{
    /// <summary>The three views of the list (PR-2.2); the key names the view when it already shows (axClaude D26).</summary>
    public const Keys ViewProcesses = Keys.Alt | Keys.D1;

    public const Keys ViewServices = Keys.Alt | Keys.D2;
    public const Keys ViewStartup = Keys.Alt | Keys.D3;

    /// <summary>The same three on Ctrl, their key until 2026-11-16, when the views moved to Alt+1, 2 and 3 (bundle AX-8.14): further keys of the same commands, shown in no menu, with nothing extra said.</summary>
    public const Keys ViewProcessesAlso = Keys.Control | Keys.D1;

    public const Keys ViewServicesAlso = Keys.Control | Keys.D2;
    public const Keys ViewStartupAlso = Keys.Control | Keys.D3;

    /// <summary>The views in turn (PR-2.2): the window has one control, so Tab and Shift+Tab go round the views (the user's ask, 2026-10-24).</summary>
    public const Keys NextView = Keys.Tab;

    public const Keys PreviousView = Keys.Shift | Keys.Tab;

    public const Keys Refresh = Keys.F5;

    /// <summary>In the list: the item's details with what can be done to it.</summary>
    public const Keys Act = Keys.Return;

    /// <summary>In the list, by the view shown: end the process (PR-3.3), or in the Startup view remove the startup program (PR-5.6), each after a question.</summary>
    public const Keys End = Keys.Delete;

    public const Keys ShowProgram = Keys.Control | Keys.E;
    public const Keys CopyLine = Keys.Control | Keys.C;
    public const Keys Summary = Keys.Control | Keys.Q;

    /// <summary>In the list: the view's menu from the bar, Process or Service, as Alt and its letter open it (PR-6.1; the window base's OpenBarMenu).</summary>
    public const Keys ItemMenu = Keys.Shift | Keys.F10;

    public const Keys ItemMenuAlso = Keys.Apps;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Processes view", ViewProcesses),
        ("Services view", ViewServices),
        ("Startup view", ViewStartup),
        ("Processes view (Ctrl+1, the older key)", ViewProcessesAlso),
        ("Services view (Ctrl+2, the older key)", ViewServicesAlso),
        ("Startup view (Ctrl+3, the older key)", ViewStartupAlso),
        ("Next view", NextView),
        ("Previous view", PreviousView),
        ("Refresh", Refresh),
        ("Details (in the list)", Act),
        ("End the process, or in the Startup view remove the startup program (in the list)", End),
        ("Show the program in axFile", ShowProgram),
        ("Copy the line", CopyLine),
        ("Summary", Summary),
        ("Context menu (in the list)", ItemMenu),
        ("Context menu (in the list, the Applications key)", ItemMenuAlso),
    ];
}
