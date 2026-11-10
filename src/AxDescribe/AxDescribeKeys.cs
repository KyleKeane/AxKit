namespace AxDescribe;

/// <summary>
/// axDescribe's window-specific keys (SPEC.md DE-6; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>DescribeForm</c> and the F1 text read this table. The bundle-wide keys (F1, the
/// palette, find, Ctrl+S, text size, Page Up and Page Down) are <c>AxKit.Forms.BundleKeys</c>; the hotkey is a
/// setting, not a key of the window. The caret stays in the field (axStart AS-D2).
/// </summary>
public static class AxDescribeKeys
{
    public const Keys NextMatch = Keys.Down;
    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>The typed question asked about the selected description's picture; with nothing typed, that description again.</summary>
    public const Keys Ask = Keys.Return;

    /// <summary>A picture file of the user's chosen in the Windows file picker and described (DE-2.7).</summary>
    public const Keys DescribeFile = Keys.Control | Keys.D;

    /// <summary>The last picture opened in axPreview, which reads the text in it.</summary>
    public const Keys OpenPicture = Keys.Control | Keys.O;

    /// <summary>The window hidden at once, whatever the field holds.</summary>
    public const Keys Hide = Keys.Escape;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Next match", NextMatch),
        ("Previous match", PreviousMatch),
        ("First match", FirstMatch),
        ("Last match", LastMatch),
        ("Ask", Ask),
        ("Describe a picture file", DescribeFile),
        ("Open the picture", OpenPicture),
        ("Hide", Hide),
    ];
}
