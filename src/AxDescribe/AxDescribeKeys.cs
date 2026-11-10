namespace AxDescribe;

/// <summary>
/// axDescribe's window-specific keys (SPEC.md DE-7; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>DescribeForm</c> and the F1 text read this table. The bundle-wide keys (F1, the palette,
/// the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. None yet.
/// </summary>
public static class AxDescribeKeys
{
    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All = [];
}
