using AxKit.Forms;

namespace AxFile;

/// <summary>
/// axFile's window-specific keys (SPEC.md AF-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the window's command list (<c>FileForm</c>, app-kit AK-2) and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. Enter, Backspace, Space, F2,
/// Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+F10 and the Applications key act in the folder list
/// only; in the Favorites list Enter, Delete, F8 and Escape act on its line; in a notice's field or the rename box
/// they are the field's own.
/// </summary>
public static class AxFileKeys
{
    /// <summary>In the folder list: Enter opens the entry (a folder in the list, a file in the program Windows has for it); in the Favorites list: the favorite in the folder list.</summary>
    public const Keys Open = Keys.Return;

    /// <summary>In the folder list: the file read in axPreview (AF-3.2); a folder in a new axFile window, File Explorer's Shift+Enter.</summary>
    public const Keys Preview = Keys.Shift | Keys.Return;

    /// <summary>In the list: the parent folder, the folder just left selected (Alt+Up did the same from anywhere until 2026-11-26).</summary>
    public const Keys Up = Keys.Back;

    /// <summary>A new axFile window on the folder shown, File Explorer's key for it (AF-3.3).</summary>
    public const Keys NewWindow = Keys.Control | Keys.N;

    public const Keys Refresh = Keys.F5;

    /// <summary>The folder shown added to Favorites, or removed from them (AF-3.9); the browsers' bookmark key.</summary>
    public const Keys Favorite = Keys.Control | Keys.D;

    /// <summary>In the folder list: the selection cleared while entries are selected (the filter is Ctrl+Delete since 2026-11-26, <see cref="ItemKeys.ClearFilter"/>). In the Favorites list: back to the folder list.</summary>
    public const Keys Clear = SelectionKeys.Clear;
    public const Keys OpenFolder = Keys.Control | Keys.O;
    /// <summary>Go to folder: F12, the function key that shows the go-to field (AX-8.18 rule 5; Ctrl+G, with Alt+D beside it, until 2026-11-26).</summary>
    public const Keys GoToFolder = Keys.F12;

    // Ctrl+W and Ctrl+Shift+W, the folder and the entry in File Explorer until 2026-11-26, are free: Show current folder
    // in and the Context menu keep the two items without a key (the consolidations of that day).

    /// <summary>The entry's size and date; in the Favorites list, the favorite's full path and its place: F8, read with details (AX-8.18 rule 5; Ctrl+I until 2026-11-26).</summary>
    public const Keys Information = Keys.F8;
    public const Keys NewFolder = Keys.Control | Keys.Shift | Keys.N;

    /// <summary>An empty text file made and the rename box opened on it (AF-4.1): T for text (AX-8.18 rule 2; Ctrl+Shift+T until 2026-11-26).</summary>
    public const Keys NewFile = Keys.Control | Keys.T;

    /// <summary>In the list: the rename in place, a box over the entry (AF-4.2).</summary>
    public const Keys Rename = Keys.F2;

    /// <summary>In the folder list: the Delete question; in the Favorites list: the favorite taken out of the favorites.</summary>
    public const Keys Delete = Keys.Delete;

    public const Keys CopyPath = Keys.Control | Keys.Shift | Keys.C;

    /// <summary>In the list: the entry on the clipboard as a file, to move (AF-4.8).</summary>
    public const Keys Cut = Keys.Control | Keys.X;

    /// <summary>In the list: the entry on the clipboard as a file, to copy.</summary>
    public const Keys Copy = Keys.Control | Keys.C;

    /// <summary>In the list: the clipboard's files copied or moved into the folder shown.</summary>
    public const Keys Paste = Keys.Control | Keys.V;

    // The keys that select entries (AF-3.7: Space, Shift+Space, Shift with Up, Down, Home and End, Ctrl+A,
    // Ctrl+Shift+A) are the bundle's, AxKit.Forms/SelectionKeys (AX-8.16), the same in every window with a selection;
    // the table below includes them.

    /// <summary>In either list: the bar's Context menu on the entry, or on the Favorites list's line, opened as Alt and C open it (AF-4.6, AF-D13, AX-9.11). The navigator's key (<see cref="ItemKeys"/>).</summary>
    public const Keys ContextMenu = ItemKeys.ItemMenu;

    /// <summary>In either list: the same, from the Applications key.</summary>
    public const Keys ContextMenuAlso = ItemKeys.ItemMenuAlso;

    /// <summary>The Windows Properties sheet of the entry, File Explorer's key for it (AF-4.7).</summary>
    public const Keys Properties = Keys.Alt | Keys.Return;

    /// <summary>The newest operation of this window taken back (AF-4.14), in either list.</summary>
    public const Keys Undo = Keys.Control | Keys.Z;

    /// <summary>In either list, at the first entry: the edge earcon (AF-3.6; the bundle's `AxKitList` since 2026-10-24); the list's own key everywhere else. In the Go to folder field: the previous completion (AF-3.3).</summary>
    public const Keys FirstItem = Keys.Up;

    /// <summary>The Go to folder field's previous completion (AF-3.3): the same key as <see cref="FirstItem"/>, in the field.</summary>
    public const Keys CompletePrevious = FirstItem;

    public const Keys FirstItemAlso = Keys.Home;

    /// <summary>In either list, at the last entry: the edge earcon; the list's own key everywhere else. In the Go to folder field: the next completion (AF-3.3).</summary>
    public const Keys LastItem = Keys.Down;

    /// <summary>The Go to folder field's next completion (AF-3.3): the same key as <see cref="LastItem"/>, in the field.</summary>
    public const Keys CompleteNext = LastItem;

    /// <summary>In the folder list: into the folder or drive, as Enter, the edge earcon on a file (AF-3.1, AF-D12). In the Go to folder field, at the end of a completed folder: its first entry.</summary>
    public const Keys IntoFolder = ItemKeys.Into;

    /// <summary>In the folder list: up one level, as Backspace, the edge earcon at This PC (AF-3.1, AF-D12). The navigator's key, as <see cref="IntoFolder"/> is.</summary>
    public const Keys OutOfFolder = ItemKeys.Up;

    public const Keys LastItemAlso = Keys.End;

    // Ctrl+Enter is the bundle's main action (BundleKeys.MainAction, AX-8.10): in the folder list, axMedia on the
    // folder, its media files from the first (AF-5).

    /// <summary>In the folder list: the entry, or the marked entries, to the end of axMedia's queue (AF-5): Q for queue (AX-8.18 rule 2; Ctrl+Shift+Q until 2026-11-26, when the reading keys left Ctrl+Q).</summary>
    public const Keys AddToQueue = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open (in the folder list); open the favorite (in the Favorites list)", Open),
        ("Preview in axPreview, or a folder in a new window (in the folder list)", Preview),
        ("Up one level (in the folder list)", Up),
        ("New window", NewWindow),
        ("Refresh", Refresh),
        ("Add to or remove from Favorites", Favorite),
        ("Clear the selection (in the folder list); back to the folder list (in the Favorites list)", Clear),
        ("Clear the filter (in the folder list)", ItemKeys.ClearFilter),
        ("Open folder", OpenFolder),
        ("Go to folder", GoToFolder),
        ("Information about the entry, or the favorite's full path", Information),
        ("New folder", NewFolder),
        ("New text file", NewFile),
        ("Rename (in the folder list)", Rename),
        ("Delete (in the folder list); remove from Favorites (in the Favorites list)", Delete),
        ("Copy path", CopyPath),
        ("Cut (in the folder list)", Cut),
        ("Copy (in the folder list)", Copy),
        ("Paste (in the folder list)", Paste),
        // The bundle's selection keys (AX-8.16), in the folder list; Escape is the entry above, with its second meaning here.
        .. SelectionKeys.All.Where(key => key.Key != SelectionKeys.Clear),
        ("Context menu: the entry's, or the favorite's in the Favorites list", ContextMenu),
        ("Context menu, from the Applications key", ContextMenuAlso),
        ("Properties", Properties),
        ("Undo the newest operation", Undo),
        ("The edge earcon at the first entry (in a list); the previous completion (in the Go to folder field)", FirstItem),
        ("The edge earcon at the first entry (in a list)", FirstItemAlso),
        ("The edge earcon at the last entry (in a list); the next completion (in the Go to folder field)", LastItem),
        ("Into the folder (in the folder list); into the completed folder (in the Go to folder field)", IntoFolder),
        ("Up one level (in the folder list)", OutOfFolder),
        ("The edge earcon at the last entry (in a list)", LastItemAlso),
        ("Add to axMedia's queue (in the folder list)", AddToQueue),
    ];
}
