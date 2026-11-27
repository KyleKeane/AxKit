namespace AxKit.Forms;

/// <summary>
/// The places of the text editor shell (<see cref="TextEditorForm"/>; docs/axKit/workflow-graph.md WG-1.1), named once
/// for every app on it: axDown, axJSON, axPreview. Go to line and the options of a place, shown under the text in the
/// choice (<see cref="ControlArea"/>), are what is over the text ("choice"), not places. A place is changed here and
/// nowhere else.
/// </summary>
public static class EditorPlaces
{
    /// <summary>The one text, written or read.</summary>
    public static readonly Place Text = new("text", "the text", Waypoint: true);

    /// <summary>The list of the file's details under a text shown read-only (axPreview).</summary>
    public static readonly Place Details = new("details", "the details");

    /// <summary>Every place of the type, for the places test.</summary>
    public static readonly IReadOnlyList<Place> All = [Text, Details];
}
