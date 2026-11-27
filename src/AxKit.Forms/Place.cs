namespace AxKit.Forms;

/// <summary>
/// One place of a window (docs/axKit/workflow-graph.md WG-1.1): where the focus can be, named once in a table beside the
/// window's key table, as the keys are. <paramref name="Id"/> is the place's own part of its stable id, which the
/// window writes after its app's name ("field" is <c>axCalc.field</c>); <paramref name="Words"/> are what a user would
/// call it ("the field"); <paramref name="Waypoint"/> marks a place a user aims for (WG-2, section 2 of the page): the
/// window's main place, and the few more a table names. The control that is the place is the window's to give
/// (<see cref="AxKitForm.Places"/>), since only the window holds it.
/// </summary>
public sealed record Place(string Id, string Words, bool Waypoint = false);
