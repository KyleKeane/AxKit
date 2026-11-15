using AxKit.Forms;

namespace AxMCP;

/// <summary>
/// axMCP's window-specific keys (SPEC.md MP-6; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>, the
/// panes, Enter, Right, Left, Tab, F2, Delete, Ctrl+N, Ctrl+Z, Ctrl+Q, F5) and two of its own. A key is changed here
/// and nowhere else: the menu items, the commands of <c>MCPForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>; so is Ctrl+Enter,
/// the main action (<c>BundleKeys.MainAction</c>, AX-8.10), which here calls the tool or fetches the prompt with the
/// arguments in the detail pane (MP-3.4, MP-5.2).
/// </summary>
public static class AxMCPKeys
{
    /// <summary>The last result, whole, as JSON in axJSON (MP-3.6).</summary>
    public const Keys LastResult = Keys.Control | Keys.J;

    /// <summary>Stop the running server (MP-2.3).</summary>
    public const Keys StopServer = Keys.Control | Keys.Shift | Keys.S;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All,
        ("The last result as JSON in axJSON", LastResult),
        ("Stop the server", StopServer),
    ];
}
