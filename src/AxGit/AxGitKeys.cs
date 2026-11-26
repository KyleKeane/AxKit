using AxKit.Forms;

namespace AxGit;

/// <summary>
/// axGIT's window-specific keys (SPEC.md AG-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>GitForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size, the edit keys of the details) are <c>AxKit.Forms.BundleKeys</c>.
/// The window is a navigator since 2026-11-22 (docs/axKit/framework.md FW-4.10): Right, Left, Backspace, Enter,
/// Delete, Tab, the selection keys and the Context menu's keys are the item window's (<c>AxKit.Forms.ItemKeys</c>,
/// <c>SelectionKeys</c>), named in <see cref="All"/> by what they do here; they act in the list, and in the details
/// they are the control's own. Space selects a file, where it staged one until then: staging has
/// <see cref="Stage"/> and <see cref="Unstage"/>. Two of a navigator's keys are axGIT's own: F5 reads again through
/// git, Ctrl+N is New branch; and four it has no use for are no keys of the window (F2, Ctrl+Z, Ctrl+Shift+N,
/// and F7, the bundle's read in full; <c>GitForm.KeysWithoutUse</c>).
/// </summary>
public static class AxGitKeys
{
    public const Keys OpenRepository = Keys.Control | Keys.O;
    public const Keys Refresh = Keys.F5;

    /// <summary>The three lists (AG-2.2), each a jump from anywhere in the window; the key names the list when it already shows (axClaude D26).</summary>
    public const Keys ViewChanges = Keys.Alt | Keys.D1;

    public const Keys ViewLog = Keys.Alt | Keys.D2;
    public const Keys ViewBranches = Keys.Alt | Keys.D3;

    /// <summary>
    /// Stage the selected files, or the file you are on (AG-3.2, 2026-11-22): S for stage, on Ctrl+Shift as the
    /// window's other commands on the repository are. Until then Space staged or unstaged the one file, and
    /// Ctrl+Shift+A staged all, which is the bundle's Deselect all now (AX-8.16): Ctrl+A, then this key.
    /// </summary>
    public const Keys Stage = Keys.Control | Keys.Shift | Keys.S;

    /// <summary>Unstage the selected files, or the file you are on; Unstage all's key until 2026-11-22.</summary>
    public const Keys Unstage = Keys.Control | Keys.Shift | Keys.U;

    public const Keys OpenInEditor = Keys.Control | Keys.E;
    public const Keys NewBranch = Keys.Control | Keys.N;
    /// <summary>
    /// The three commands that reach the remote, on Ctrl alone (the maintainer's choice of 2026-11-26, AX-8.18 rule 2:
    /// "move everything to ctrl only so pick another letter for f"): R for fetch, as in reload, since Ctrl+F is the
    /// bundle's find; L for pull and P for push. Ctrl+Shift+F, L and H until then.
    /// </summary>
    public const Keys Fetch = Keys.Control | Keys.R;
    public const Keys Pull = Keys.Control | Keys.L;
    public const Keys Push = Keys.Control | Keys.P;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open repository", OpenRepository),
        ("Refresh", Refresh),
        ("Changes", ViewChanges),
        ("Log", ViewLog),
        ("Branches", ViewBranches),
        // The navigator's keys (ItemKeys), by what they do here.
        ("Into the list, at the repository (in the list)", ItemKeys.Into),
        ("Up to the repository (in the list)", ItemKeys.Up),
        ("Up to the repository (in the list, Backspace)", ItemKeys.UpAlso),
        ("The diff, the commit, or switch to the branch; into the list at the repository (in the list)", ItemKeys.Open),
        ("Discard the changes to the file, or to the selected files (in the list)", ItemKeys.Delete),
        ("Clear the filter (in the list)", ItemKeys.ClearFilter),
        // The navigator's own names for the two: the details are axGIT's other pane.
        ("To the other pane", ItemKeys.OtherPane),
        ("To the other pane, back", ItemKeys.OtherPaneBack),
        // The bundle's selection keys (AX-8.16), on the files of Changes; Escape also comes back from the details.
        .. SelectionKeys.All,
        ("Stage the selected files, or the file you are on", Stage),
        ("Unstage the selected files, or the file you are on", Unstage),
        ("Open the file in its editor", OpenInEditor),
        ("New branch", NewBranch),
        ("Fetch", Fetch),
        ("Pull", Pull),
        ("Push", Push),
        ("Context menu (in the list)", ItemKeys.ItemMenu),
        ("Context menu (in the list, the Applications key)", ItemKeys.ItemMenuAlso),
    ];
}
