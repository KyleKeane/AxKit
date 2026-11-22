using AxJSON.Core;
using AxKit.Forms;

namespace AxJSON;

/// <summary>
/// axJSON's window-specific keys (SPEC.md AJ-2.2, AJ-2.3; bundle AX-8). A key is changed here and nowhere else: the
/// menu items, the key handling and the F1 text read this table. The editor shell's keys (new, open, save as, go to
/// line, word wrap, show in File Explorer) are <c>AxKit.Forms.EditorKeys</c>, the same in every editor window, and
/// <see cref="All"/> includes them; the bundle-wide keys are <c>AxKit.Forms.BundleKeys</c>. The letters follow axDown's
/// where the concept exists in both (L a list and an array, I an item in both); the others are JSON's own (AJ-D5).
/// </summary>
public static class AxJSONKeys
{
    public const Keys NextKey = Keys.Control | Keys.K;
    public const Keys PreviousKey = Keys.Control | Keys.Shift | Keys.K;

    /// <summary>J, the braces of JSON: O is Open in every editor window.</summary>
    public const Keys NextObject = Keys.Control | Keys.J;

    public const Keys PreviousObject = Keys.Control | Keys.Shift | Keys.J;
    public const Keys NextArray = Keys.Control | Keys.L;
    public const Keys PreviousArray = Keys.Control | Keys.Shift | Keys.L;
    public const Keys NextItem = Keys.Control | Keys.I;
    public const Keys PreviousItem = Keys.Control | Keys.Shift | Keys.I;
    public const Keys NextError = Keys.Control | Keys.E;
    public const Keys PreviousError = Keys.Control | Keys.Shift | Keys.E;

    /// <summary>Edit → Format (AJ-2.3).</summary>
    public const Keys Format = Keys.Control | Keys.Shift | Keys.F;

    // The options (AJ-5) are Alt with an arrow, the maintainer's rule of 2026-11-21 for the choices of what the caret
    // is on, in axKit's own arrows: Right goes into the value's list, Up and Down change the value in line as they
    // select in a list, Left steps out to the object around it. Ctrl with an arrow moves the cursor, and Ctrl+Shift
    // with Up or Down is kept for moving an item of a list. None of the four means anything to an edit control or NVDA.

    /// <summary>Edit, Choose a value: the values the member at the caret can take, as a list the arrows move in.</summary>
    public const Keys ChooseValue = Keys.Alt | Keys.Right;

    /// <summary>Edit, Next value: the member's value changed in line to the next of its values.</summary>
    public const Keys NextValue = Keys.Alt | Keys.Down;

    /// <summary>Edit, Previous value: the same, to the one before.</summary>
    public const Keys PreviousValue = Keys.Alt | Keys.Up;

    /// <summary>Edit, Add a member: the members the object at the caret can have and does not have yet.</summary>
    public const Keys AddMember = Keys.Alt | Keys.Left;

    /// <summary>Every key of the window with its name, the editor shell's first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ..EditorKeys.All,
        ("Next key", NextKey),
        ("Previous key", PreviousKey),
        ("Next object", NextObject),
        ("Previous object", PreviousObject),
        ("Next array", NextArray),
        ("Previous array", PreviousArray),
        ("Next item", NextItem),
        ("Previous item", PreviousItem),
        ("Next error", NextError),
        ("Previous error", PreviousError),
        ("Format", Format),
        ("Choose a value", ChooseValue),
        ("Next value", NextValue),
        ("Previous value", PreviousValue),
        ("Add a member", AddMember),
    ];

    /// <summary>The keys of AJ-2.2 as pairs, one per kind, with the outline's functions: the editor and every notice text take this list (bundle AX-7.2).</summary>
    public static readonly IReadOnlyList<TextJump> Jumps =
    [
        new(NextKey, PreviousKey, "key", JsonOutline.NextKey, JsonOutline.PreviousKey),
        new(NextObject, PreviousObject, "object", JsonOutline.NextObject, JsonOutline.PreviousObject),
        new(NextArray, PreviousArray, "array", JsonOutline.NextArray, JsonOutline.PreviousArray),
        new(NextItem, PreviousItem, "item", JsonOutline.NextItem, JsonOutline.PreviousItem),
        new(NextError, PreviousError, "error", JsonOutline.NextError, JsonOutline.PreviousError),
    ];
}
