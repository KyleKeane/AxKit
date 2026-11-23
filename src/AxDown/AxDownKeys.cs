using AxKit.Forms;

namespace AxDown;

/// <summary>
/// axDown's window-specific keys (SPEC.md AD-5; bundle AX-8). The editor shell's keys (new, open, save as, go to line,
/// word wrap, show in File Explorer) are <c>AxKit.Forms.EditorKeys</c>, the same in every editor window; the Markdown
/// navigation keys of SPEC.md §9 are <c>AxKit.Forms.MarkdownKeys</c>, shared with axPreview; <see cref="All"/> includes
/// both. The bundle-wide keys (F1, find, save, text size, the controls) are <c>AxKit.Forms.BundleKeys</c>. The collision
/// test in <c>tests/AxKit.Tests</c> fails when a key here equals one of those.
/// </summary>
public static class AxDownKeys
{
    // The format commands with a key (SPEC.md AD-12.5): bold as in every editor, and the two that add and take a
    // number sign of a heading, Word's keys for a heading's level. The others are in Actions, Format and the palette.
    public const Keys Bold = Keys.Control | Keys.B;
    public const Keys HeadingDeeper = Keys.Alt | Keys.Shift | Keys.Right;
    public const Keys HeadingShallower = Keys.Alt | Keys.Shift | Keys.Left;

    /// <summary>Every key of the window with its name, the editor shell's first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ..EditorKeys.All,
        ..MarkdownKeys.Named,
        ("Bold", Bold),
        ("Heading deeper", HeadingDeeper),
        ("Heading shallower", HeadingShallower),
    ];

    /// <summary>The keys of SPEC.md §9 as pairs, one per kind, for the shell.</summary>
    public static IReadOnlyList<TextJump> Jumps => MarkdownKeys.Jumps;
}
