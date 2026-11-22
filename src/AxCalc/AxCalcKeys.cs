using AxKit.Core.Transcript;
using AxKit.Forms;

namespace AxCalc;

/// <summary>
/// axCalc's window-specific keys (SPEC.md AC-7.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the commands of <c>CalcForm</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, the control keys, find, Ctrl+S, text size) are <c>AxKit.Forms.BundleKeys</c>. The window is a
/// conversation window since 2026-11-22 (docs/axKit/framework.md step 9): the history is its text, read with the
/// letters every conversation window has, the same as in axClaude and axTerminal (<see cref="Transcript"/>). Enter
/// and Escape mean what the place with the focus needs: the field answers and clears, the history takes a line's
/// expression into the field and goes back to the field. Delete no longer removes one line of the history: the
/// history is a text, and Clear history empties it.
/// </summary>
public static class AxCalcKeys
{
    /// <summary>In the field, the answer (AC-6.1); in the history, the line's expression back in the field (AC-5.2).</summary>
    public const Keys Act = Keys.Return;

    /// <summary>In the field, the text cleared; in the history, back to the field.</summary>
    public const Keys Back = Keys.Escape;

    public const Keys CopyAnswer = Keys.Control | Keys.Shift | Keys.C;
    public const Keys OtherBases = Keys.Control | Keys.I;
    public const Keys LastAnswer = Keys.Control | Keys.Q;
    public const Keys ClearHistory = Keys.Control | Keys.Shift | Keys.Delete;

    // ---- the history's letters, a conversation window's (the same keys as axTerminal's and axClaude's views) ----

    /// <summary>a and Shift+A: the next and the previous answer.</summary>
    public const Keys Answer = Keys.A;

    /// <summary>s and Shift+S: the next and the previous line of the app's own ("System: …").</summary>
    public const Keys SystemLine = Keys.S;

    /// <summary>k and Shift+K: the bookmarks dropped with m.</summary>
    public const Keys Bookmark = Keys.K;
    public const Keys BookmarkToggle = Keys.M;
    public const Keys LineNumber = Keys.L;
    public const Keys JumpBack = Keys.Back;

    /// <summary>In the history with a selection: copies it and says so; the control's own copy otherwise.</summary>
    public const Keys CopySelection = Keys.Control | Keys.C;

    /// <summary>From anywhere in the window: a bookmark on the history's line.</summary>
    public const Keys BookmarkLine = Keys.Control | Keys.Shift | Keys.K;

    /// <summary>Ctrl+Alt with one of the history's letters, from anywhere: every line of that kind from the caret's line to the end, spoken once.</summary>
    public const Keys ReadToEnd = Keys.Control | Keys.Alt;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Answer (in the field), or the line's expression into the field (in the history)", Act),
        ("Clear the field, or back to the field from the history", Back),
        ("Copy the answer", CopyAnswer),
        ("The answer in other bases", OtherBases),
        ("Say the last answer", LastAnswer),
        ("Clear the history", ClearHistory),
        ("Copy the selection", CopySelection),
        ("Answer (a line of the history)", Answer),
        ("System line", SystemLine),
        ("Bookmark", Bookmark),
        ("Bookmark this line (m)", BookmarkToggle),
        ("Line number", LineNumber),
        ("Back to the line before the jump", JumpBack),
        ("Bookmark this line", BookmarkLine),
    ];

    private static readonly QuickKey BookmarkKey = new(Bookmark, "bookmark", line => line.Kind == LineKind.Bookmark);

    private static readonly QuickKey AnswerKey = new(Answer, "answer", line => line.Kind == LineKind.Plain);

    /// <summary>
    /// The history's keys for the bundle's transcript view (SPEC.md AC-5.7): the kinds axCalc's text has, as data:
    /// an answer, a line of the app's own, a bookmark.
    /// </summary>
    public static readonly TranscriptKeys Transcript = new(
        QuickKeys:
        [
            AnswerKey,
            new(SystemLine, "system line", line => line.Kind == LineKind.System),
            BookmarkKey,
        ],
        Bookmark: BookmarkKey,
        Response: AnswerKey,
        BookmarkToggle: BookmarkToggle,
        LineNumber: LineNumber,
        JumpBack: JumpBack,
        Enter: Act,
        Escape: Back,
        CopySelection: CopySelection,
        BookmarkLine: BookmarkLine,
        ReadToEnd: ReadToEnd);
}
