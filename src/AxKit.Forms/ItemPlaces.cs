namespace AxKit.Forms;

/// <summary>
/// The places of the navigator (<see cref="ItemForm"/>; docs/axKit/workflow-graph.md WG-1.1), named once for every app
/// on it: axTask, axPass, axMCP, axAutomate, axMedia, axProcess, axGIT, axFile, axTemplate's browser. The second pane
/// is one of three kinds or none (docs/axKit/framework.md FW-4.7), each a place of its own, since what its keys do
/// differs. A level gone into is the same place: the rows are content, not places (WG section 3). A place is changed
/// here and nowhere else.
/// </summary>
public static class ItemPlaces
{
    /// <summary>The list of rows, where the window starts.</summary>
    public static readonly Place List = new("list", "the list", Waypoint: true);

    /// <summary>The second pane as the fields of the row you are on (axTask, axPass, axMCP, axAutomate).</summary>
    public static readonly Place Fields = new("fields", "the fields of the row");

    /// <summary>The second pane as a text to read (axGIT's details).</summary>
    public static readonly Place Details = new("details", "the details");

    /// <summary>The second pane as a list the app fills (axFile's Favorites list).</summary>
    public static readonly Place Log = new("log", "the second list");

    /// <summary>Every place of the type, for the places test.</summary>
    public static readonly IReadOnlyList<Place> All = [List, Fields, Details, Log];
}
