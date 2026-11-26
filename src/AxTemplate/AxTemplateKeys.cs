using AxKit.Forms;

namespace AxTemplate;

/// <summary>
/// axTemplate's window-specific keys (SPEC.md TP-7.1; bundle AX-8). The browser has the item apps' shared keys
/// (<see cref="ItemKeys"/>: the panes, Enter, Tab, F2, Delete, Ctrl+N, Ctrl+Z, F5), the bundle's selection
/// keys and one of its own. A key is changed here and nowhere else: the menu items, the commands of
/// <c>BrowserForm</c> and the F1 text read this table. The bundle-wide keys (F1, the palette, the control keys,
/// find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>; so is Ctrl+Enter, the main action
/// (<c>BundleKeys.MainAction</c>, AX-8.10), which here copies the template's text (TP-8.4).
/// </summary>
public static class AxTemplateKeys
{
    /// <summary>The template's file in axJSON (TP-8.4).</summary>
    public const Keys OpenInJson = Keys.Control | Keys.J;

    /// <summary>The navigator's keys the browser has no use for (<c>BrowserForm.KeysWithoutUse</c>): one level without sections, and no second kind of list to make.</summary>
    public static readonly IReadOnlyCollection<Keys> WithoutUse = [ItemKeys.NewList, ItemKeys.PreviousSection, ItemKeys.NextSection];

    /// <summary>Every key of the browser with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All.Where(key => !WithoutUse.Contains(key.Key)),
        // The bundle's selection keys (AX-8.16, TP-8.2); Space and Escape are in ItemKeys.All.
        .. SelectionKeys.WithoutSpaceAndEscape,
        ("The template's file in axJSON", OpenInJson),
    ];

    // ---- the launcher (TP-3.4, TP-4): a window of its own, so a table of its own ----

    /// <summary>The match after the selected one (TP-3.4); the caret stays in the field.</summary>
    public const Keys NextMatch = Keys.Down;

    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>Put the template into the program that was in front (TP-4.1).</summary>
    public const Keys PutIn = Keys.Return;

    /// <summary>The filled text on the clipboard, nothing pasted (TP-4.4).</summary>
    public const Keys CopyOnly = Keys.Shift | Keys.Return;

    /// <summary>The template's own text in a notice (TP-4.5): the bundle's read in full, F7 (AX-8.18 rule 5; Ctrl+Q until 2026-11-26), so no row of <see cref="Launcher"/>.</summary>
    public const Keys ReadInFull = BundleKeys.ReadInFull;

    /// <summary>The browser, where templates are made and changed (TP-4.6).</summary>
    public const Keys OpenBrowser = Keys.Control | Keys.B;

    public const Keys Hide = Keys.Escape;

    /// <summary>Every key of the launcher with its name, for the collision test and its F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> Launcher =
    [
        ("Next match", NextMatch),
        ("Previous match", PreviousMatch),
        ("First match", FirstMatch),
        ("Last match", LastMatch),
        ("Put the template in", PutIn),
        ("Copy without putting in", CopyOnly),
        ("Open the browser", OpenBrowser),
        ("Hide", Hide),
    ];
}
