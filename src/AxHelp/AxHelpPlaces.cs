using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's places beside its keys (docs/axKit/workflow-graph.md WG-1.1): the one place its kind, the hotkey window
/// (<see cref="HotkeyPlaces"/>), does not have. Its window shows a text that says what axHelp does, in place of the
/// search field, which it never shows. A place is changed here and nowhere else.
/// </summary>
public static class AxHelpPlaces
{
    /// <summary>The text of the window, where it starts.</summary>
    public static readonly Place Text = new("text", "the help text", Waypoint: true);

    /// <summary>Every place of the window's own, for the places test.</summary>
    public static readonly IReadOnlyList<Place> All = [Text];
}
