namespace AxKit.Forms;

/// <summary>
/// The places of the conversation window (<see cref="ConversationForm"/>, and the console window over it;
/// docs/axKit/workflow-graph.md WG-1.1), named once for every app on it: axClaude, axTerminal, axCalc. The choice that
/// stands in the field's place (<see cref="ControlArea"/>) is no place of its own: it is what is over the field
/// ("choice", <see cref="AxKitForm.OverId"/>). A place is changed here and nowhere else.
/// </summary>
public static class ConversationPlaces
{
    /// <summary>What was said, above: the conversation, the shell's output, the answers given.</summary>
    public static readonly Place Text = new("text", "the conversation");

    /// <summary>The field under it, where the window starts: the message, the command, the expression.</summary>
    public static readonly Place Field = new("field", "the field", Waypoint: true);

    /// <summary>Every place of the type, for the places test.</summary>
    public static readonly IReadOnlyList<Place> All = [Text, Field];
}
