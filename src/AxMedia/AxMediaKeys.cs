using AxKit.Forms;

namespace AxMedia;

/// <summary>
/// axMedia's window-specific keys (SPEC.md AM-5.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>MediaForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. The window is a navigator
/// since 2026-11-22 (docs/axKit/framework.md FW-4.9): Enter, Delete, the Context menu's keys and the other keys of
/// a navigator are the item window's (<c>AxKit.Forms.ItemKeys</c>), named in <see cref="All"/> by what they do in
/// a queue; they act in the list only, and in a notice's field they are the field's own. Everything else works
/// from anywhere in the window. Four of the navigator's keys are axMedia's own here and not the navigator's:
/// Ctrl+N is New window, Ctrl+Q What is playing, Ctrl+Up and Ctrl+Down the speed; and four the queue has no use
/// for are no keys of the window (F2, Ctrl+Z, F5, Ctrl+Shift+N; <c>MediaForm.KeysWithoutUse</c>).
/// </summary>
public static class AxMediaKeys
{
    // Pause and resume is Ctrl+Enter, the bundle's main action key (BundleKeys.MainAction), from anywhere in the
    // window (AM-D7 as of 2026-11-22; Space in the list and Ctrl+P until then). Space and its kin select files:
    // the bundle's selection keys (AxKit.Forms/SelectionKeys, AX-8.16), which the table below includes.

    /// <summary>The file's place in the queue, and its position while it plays.</summary>
    public const Keys Information = Keys.Control | Keys.I;

    /// <summary>The Windows file picker, several files at once, appended to the queue.</summary>
    public const Keys OpenFiles = Keys.Control | Keys.O;

    /// <summary>The Windows folder picker; the folder's media files appended.</summary>
    public const Keys OpenFolder = Keys.Control | Keys.Shift | Keys.O;

    /// <summary>A new axMedia window (AX-9).</summary>
    public const Keys NewWindow = Keys.Control | Keys.N;

    /// <summary>The file playing, or the one under the selection, selected in File Explorer.</summary>
    public const Keys ShowInExplorer = Keys.Control | Keys.W;

    public const Keys Forward = Keys.Control | Keys.Right;
    public const Keys Back = Keys.Control | Keys.Left;
    public const Keys NextFile = Keys.Control | Keys.Shift | Keys.Right;
    public const Keys PreviousFile = Keys.Control | Keys.Shift | Keys.Left;
    public const Keys GoToTime = Keys.Control | Keys.T;

    /// <summary>
    /// The speed (SPEC.md AM-D8, the user's choice of 2026-11-14): Ctrl+Up faster, Ctrl+Down slower, beside Ctrl+Left
    /// and Ctrl+Right. They take the place of the bundle's paragraph keys on purpose (bundle AX-8.4): the window's one
    /// control is a list, and in a notice's text the keys are the text's own, since the window's keys wait there.
    /// Ctrl+Shift+Period and Ctrl+Shift+Comma, the keys until then, are the bundle's system-wide ones (AX-8.11).
    /// </summary>
    public const Keys Faster = Keys.Control | Keys.Up;
    public const Keys Slower = Keys.Control | Keys.Down;
    public const Keys WhatIsPlaying = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        // The navigator's keys (ItemKeys), by what they do in a queue.
        ("Play (in the list)", ItemKeys.Open),
        ("Remove (in the list)", ItemKeys.Delete),
        ("No list to go into: the edge sound (in the list)", ItemKeys.Into),
        ("No level above: the edge sound (in the list)", ItemKeys.Up),
        ("No level above: the edge sound (in the list, Backspace)", ItemKeys.UpAlso),
        // The bundle's selection keys (AX-8.16), in the queue.
        .. SelectionKeys.All,
        ("Information about the file", Information),
        ("Add files", OpenFiles),
        ("Add folder", OpenFolder),
        ("New window", NewWindow),
        ("Show the file in File Explorer", ShowInExplorer),
        ("Forward", Forward),
        ("Back", Back),
        ("Next file", NextFile),
        ("Previous file", PreviousFile),
        ("Go to time", GoToTime),
        ("Faster", Faster),
        ("Slower", Slower),
        ("What is playing", WhatIsPlaying),
        ("Context menu (in the queue)", ItemKeys.ItemMenu),
        ("Context menu (in the queue, the Applications key)", ItemKeys.ItemMenuAlso),
    ];
}
