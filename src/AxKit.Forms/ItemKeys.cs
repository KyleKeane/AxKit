namespace AxKit.Forms;

/// <summary>
/// The keys every item app's window shares (docs/axKit/item-apps.md IA-3; from axTask on its second use, axPass,
/// 2026-10-24), handled by <see cref="ItemForm"/>. Each app's key table includes <see cref="All"/> and adds its own,
/// as an editor's includes <c>EditorKeys</c>; a key is changed here for both apps.
/// </summary>
public static class ItemKeys
{
    /// <summary>
    /// Into a list or a group (IA-3): Right only goes into them; on an item it plays the edge sound. In the detail pane
    /// (IA-5.6): the next value of a choice, a check checked; the edge sound on any other field.
    /// </summary>
    public const Keys Into = Keys.Right;

    /// <summary>Up a level (IA-3). In the detail pane (IA-5.6): the previous value of a choice, a check unchecked; the edge sound on any other field.</summary>
    public const Keys Up = Keys.Left;
    public const Keys UpAlso = Keys.Back;

    /// <summary>In the navigator, the section header before or after (IA-2.3a); in an edit control the same keys move by paragraph (AX-8.2), the same idea.</summary>
    public const Keys PreviousSection = Keys.Control | Keys.Up;

    public const Keys NextSection = Keys.Control | Keys.Down;

    /// <summary>Open (IA-D2): a list opens, an item's fields take the focus; in the detail pane, change the field.</summary>
    public const Keys Open = Keys.Return;

    /// <summary>To the detail pane and back (IA-3).</summary>
    public const Keys OtherPane = Keys.Tab;

    public const Keys OtherPaneBack = Keys.Shift | Keys.Tab;

    /// <summary>In the detail pane: back to the navigator. In the navigator of an app with a selection, the bundle's key that deselects all while rows are selected (<see cref="SelectionKeys.Clear"/>, AX-8.16).</summary>
    public const Keys Back = SelectionKeys.Clear;

    /// <summary>
    /// In the detail pane on a check: checked or not. In the navigator it is the bundle's key that selects the row
    /// (<see cref="SelectionKeys.Select"/>, AX-8.16; until 2026-11-22 a task done or not in axTask, which is
    /// Ctrl+Enter now): the edge sound in an app whose navigator has no selection.
    /// </summary>
    public const Keys Check = SelectionKeys.Select;

    /// <summary>A new item in the navigator; no command in the detail pane (the knock of a key without one, 2026-11-23).</summary>
    public const Keys New = Keys.Control | Keys.N;

    public const Keys NewList = Keys.Control | Keys.Shift | Keys.N;
    public const Keys Rename = Keys.F2;

    /// <summary>Archive the item or list (IA-D4); no command in the detail pane.</summary>
    public const Keys Delete = Keys.Delete;

    /// <summary>
    /// Clear the filter (FW-4.10a; the maintainer's decision of 2026-11-26, AX-8.18): Ctrl+Delete clears what you
    /// typed, in the remove family, so that Escape only goes back or closes; Escape cleared the filter until then. The
    /// one key of the kind in the bundle: axCalc's field takes it from here too (<c>AxCalcKeys.ClearField</c>).
    /// </summary>
    public const Keys ClearFilter = Keys.Control | Keys.Delete;

    public const Keys Undo = Keys.Control | Keys.Z;

    /// <summary>Read in full: every field of the item, or the field's whole value; the bundle's F7 (AX-8.18 rule 5; Ctrl+Q until 2026-11-26), so no row of <see cref="All"/>.</summary>
    public const Keys ReadAll = BundleKeys.ReadInFull;

    public const Keys Refresh = Keys.F5;

    /// <summary>The bar's Context menu, what acts on the selection or the field (AX-9.11), as Alt and C open it (the window base's OpenBarMenu).</summary>
    public const Keys ItemMenu = Keys.Shift | Keys.F10;

    public const Keys ItemMenuAlso = Keys.Apps;

    /// <summary>The shared keys with their names, which each app's table includes.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Into a list; in the detail pane, the next value of a choice, or check", Into),
        ("Up a level; in the detail pane, the previous value of a choice, or uncheck", Up),
        ("Up a level (Backspace)", UpAlso),
        ("Previous section", PreviousSection),
        ("Next section", NextSection),
        ("Open, or change the field", Open),
        ("To the other pane", OtherPane),
        ("To the other pane, back", OtherPaneBack),
        ("Back to the navigator (in the detail pane); deselect all while any is selected (in the navigator)", Back),
        ("Check or uncheck the field (in the detail pane); select or deselect (in the navigator)", Check),
        ("New item", New),
        ("New list", NewList),
        ("Rename", Rename),
        ("Archive", Delete),
        ("Clear the filter (in the navigator)", ClearFilter),
        ("Undo", Undo),
        ("Refresh", Refresh),
        ("Context menu", ItemMenu),
        ("Context menu (the Applications key)", ItemMenuAlso),
    ];
}
