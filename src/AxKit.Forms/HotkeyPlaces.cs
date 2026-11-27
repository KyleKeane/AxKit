namespace AxKit.Forms;

/// <summary>
/// The places of the hotkey window (<see cref="HotkeyForm"/>; docs/axKit/workflow-graph.md WG-1.1), named once for every
/// app on it: axStart, axClip, axTemplate's launcher. Its list never takes the focus (docs/axKit/framework.md FW-4.12),
/// so the field is its one place. axHelp's window shows a text in place of the field, a place of its own table
/// (<c>AxHelpPlaces</c>). A place is changed here and nowhere else.
/// </summary>
public static class HotkeyPlaces
{
    /// <summary>The search field, which always holds the caret.</summary>
    public static readonly Place Field = new("field", "the search field", Waypoint: true);

    /// <summary>Every place of the type, for the places test.</summary>
    public static readonly IReadOnlyList<Place> All = [Field];
}
