namespace AxMedia;

/// <summary>
/// axMedia's window-specific keys (SPEC.md AM-5.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>MediaForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. Enter, Delete and Space act in
/// the list only; in a notice's field they are the field's own. Everything else works from anywhere in the window.
/// </summary>
public static class AxMediaKeys
{
    /// <summary>In the list: the file under the selection plays.</summary>
    public const Keys Play = Keys.Return;

    /// <summary>In the list: the file taken out of the queue.</summary>
    public const Keys Remove = Keys.Delete;

    /// <summary>In the list: pause, or resume (AM-D7); Ctrl+P the same from anywhere in the window.</summary>
    public const Keys PauseResume = Keys.Space;

    public const Keys PauseResumeAlso = Keys.Control | Keys.P;

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

    /// <summary>In the queue: the bar's Context menu on the file, as Alt and C open it (bundle AX-9.11, 2026-11-11).</summary>
    public const Keys ContextMenu = Keys.Shift | Keys.F10;
    public const Keys ContextMenuAlso = Keys.Apps;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Play (in the list)", Play),
        ("Remove (in the list)", Remove),
        ("Pause or resume (in the list)", PauseResume),
        ("Pause or resume", PauseResumeAlso),
        ("Information about the file", Information),
        ("Open files", OpenFiles),
        ("Open folder", OpenFolder),
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
        ("Context menu (in the queue)", ContextMenu),
        ("Context menu (in the queue, the Applications key)", ContextMenuAlso),
    ];
}
