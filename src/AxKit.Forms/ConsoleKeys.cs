namespace AxKit.Forms;

/// <summary>
/// The keys every console window has in common (docs/axKit/framework.md step 11; bundle SPEC AX-8): the field's two
/// keys and the places. A console app's own table names them for its window (axClaude's "Go to the conversation",
/// axTerminal's "Go to the terminal") with these values, so that a key is changed here and nowhere else.
/// </summary>
public static class ConsoleKeys
{
    /// <summary>In the field: what it holds is sent; Shift+Enter stays the field's own new line.</summary>
    public const Keys Send = Keys.Return;

    /// <summary>In the field: the guard, which sends nothing and says which key does.</summary>
    public const Keys Guard = Keys.Escape;

    /// <summary>The places (AX-8.14): the field, the view, and the latest of what the program said, each with its older Ctrl key as a further key of the same command.</summary>
    public const Keys GoToField = Keys.Alt | Keys.D1;
    public const Keys GoToView = Keys.Alt | Keys.D2;
    public const Keys Latest = Keys.Alt | Keys.D3;
    public const Keys GoToFieldAlso = Keys.Control | Keys.D1;
    public const Keys GoToViewAlso = Keys.Control | Keys.D2;
    public const Keys LatestAlso = Keys.Control | Keys.D3;
}
