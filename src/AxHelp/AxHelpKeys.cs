namespace AxHelp;

/// <summary>
/// axHelp's window-specific keys (SPEC.md HE-6; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>HelpForm</c> and the F1 text read this table. The bundle-wide keys (F1, the palette,
/// find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>; the two hotkeys are settings, not keys of the window.
/// </summary>
public static class AxHelpKeys
{
    /// <summary>The conversation brought up, as the hotkey does.</summary>
    public const Keys OpenConversation = Keys.Return;

    /// <summary>A picture file of the user's chosen in the Windows file picker and put into the conversation (HE-3.3).</summary>
    public const Keys PictureFile = Keys.Control | Keys.D;

    /// <summary>The last screenshot opened in axPreview, which reads the text in it.</summary>
    public const Keys OpenScreenshot = Keys.Control | Keys.O;

    /// <summary>The window hidden at once.</summary>
    public const Keys Hide = Keys.Escape;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open the conversation", OpenConversation),
        ("Put a picture file into the conversation", PictureFile),
        ("Open the last screenshot", OpenScreenshot),
        ("Hide", Hide),
    ];
}
