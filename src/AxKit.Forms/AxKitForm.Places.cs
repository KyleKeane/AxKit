using AxKit.Core.Events;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- the places of the window and what is over them (docs/axKit/workflow-graph.md WG-1) ----
    //
    // Each kind of window names its places once in a table beside its keys (ConversationPlaces, EditorPlaces, ItemPlaces,
    // HotkeyPlaces) and gives the control that is each place here; an app adds a table only for a place its kind does not
    // have (AxHelpPlaces). The automation channel says the place and what is over it in its state and in every focus
    // move, and the inventory run writes the graph from them (docs/axKit/workflow-graph.json). Nothing here changes what
    // a user hears or presses: only the channel, on in a test run alone, asks.

    /// <summary>The words of the palette's notice, which is also how <see cref="OverId"/> tells the palette from another notice.</summary>
    internal const string PaletteTitle = "Command palette";

    /// <summary>
    /// The window's places, each with the control that is it (WG-1.1): the kind of window gives its own, an app adds its
    /// own after them. A place may have more than one control when the window shows one or the other.
    /// </summary>
    protected internal virtual IReadOnlyList<(Place Place, Control Control)> Places => [];

    /// <summary>The place the focus was last in, for while something is over the places (a notice, a choice).</summary>
    private string? _lastPlace;

    /// <summary>A place's stable id: the app's name and the place's own part, <c>axCalc.field</c>.</summary>
    internal string PlaceId(Place place) => AppName + "." + place.Id;

    /// <summary>
    /// The place the focus is in (WG-1.2): the place whose control holds the focused control; while the focus is in
    /// something over the places, or nowhere, the place it was last in; null before it was in any.
    /// </summary>
    internal string? PlaceNow()
    {
        if (PlaceOf(FocusNow()) is { } place)
        {
            _lastPlace = PlaceId(place);
        }

        return _lastPlace;
    }

    /// <summary>The control with the focus as the channel reads it (the thread's focus: a window shown without activation has no active control), when it is this window's.</summary>
    private Control? FocusNow() => Control.FromChildHandle(User32.GetFocus()) is { } control && control.FindForm() == this ? control : null;

    /// <summary>The place a control is, or is inside of; null for one of no place.</summary>
    private Place? PlaceOf(Control? control)
    {
        var places = Places;
        for (var at = control; at is not null && at != this; at = at.Parent)
        {
            foreach (var (place, placed) in places)
            {
                if (placed == at)
                {
                    return place;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// What is over the places and takes the keys (WG-1.3), by an id: <c>menu</c> (the menu bar in the keyboard's
    /// hands), <c>dialog</c> (one of the Windows dialogs), the notice by its kind (<c>palette</c>, <c>form</c>,
    /// <c>error</c>, <c>question</c>, <c>notice</c>), <c>choice</c> (the focus in the choice that stands in a place,
    /// <see cref="ControlArea"/>), or <c>none</c>.
    /// </summary>
    internal string OverId()
    {
        if (MainMenuStrip is { } bar && bar.Items.Cast<ToolStripItem>().Any(item => item.Pressed || item is ToolStripDropDownItem { DropDown.Visible: true }))
        {
            return "menu";
        }

        if (DialogShowing)
        {
            return "dialog";
        }

        if (NoticeOpen && Overlay.Shown is { } notice)
        {
            return notice switch
            {
                { Title: PaletteTitle } => "palette",
                { Custom: FormControls } => "form",
                { Kind: EventKind.Failed } => "error",
                { Input: not null } or { Custom: not null } => "question",
                { Choices.Count: > 1 } => "question",
                _ => "notice",
            };
        }

        for (var at = FocusNow(); at is not null && at != this; at = at.Parent)
        {
            if (at is ControlArea)
            {
                return "choice";
            }
        }

        return "none";
    }

    /// <summary>The focus put on a place's control by its id, for the inventory run's crawl alone; why not, or null when it moved.</summary>
    internal string? GoToPlace(string id)
    {
        foreach (var (place, control) in Places)
        {
            if (PlaceId(place) == id && control is { Visible: true, Enabled: true, CanFocus: true })
            {
                control.Select();
                control.Focus();
                return FocusNow() is { } focused && PlaceOf(focused) == place ? null : $"the focus did not move to {id}";
            }
        }

        return $"no place {id} on view now";
    }

    /// <summary>
    /// The controls on view that take the focus and are no place (WG-1.1), each as its name and role: what the places
    /// test reads from the graph file, where the crawl writes them. A control inside a notice or a choice is what is
    /// over a place, and never counted; nor is a list that never takes the focus (no tab stop, the hotkey window's).
    /// </summary>
    internal IReadOnlyList<string> Unplaced()
    {
        var places = Places;
        var found = new List<string>();
        Look(this);
        return found;

        void Look(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (!child.Visible || child == Overlay || child is ControlArea or ToolStrip || places.Any(entry => entry.Control == child))
                {
                    continue;
                }

                if (child is TextBoxBase or ListBox or ButtonBase or ComboBox or TreeView or ListView or TrackBar)
                {
                    if (child.Enabled && child.TabStop)
                    {
                        found.Add($"{child.AccessibleName} ({child.AccessibilityObject.Role})");
                    }

                    continue;
                }

                Look(child);
            }
        }
    }

    /// <summary>
    /// The window's places for the channel: each id, its words, whether it is a waypoint, and whether it is on view now;
    /// the controls of no place (<see cref="Unplaced"/>); and the bundle's keys the crawl tries where no command of the
    /// window lists them (WG-2.2), as chords: the bundle table's (<see cref="BundleKeys.Fixed"/>) but the moves inside
    /// a place and the keypad's second keys (<see cref="NoEdges"/>), and Windows' own Escape, Tab and Shift+Tab, which
    /// every window has.
    /// </summary>
    internal object PlacesListing() => new
    {
        places = Places.Select(entry => new
        {
            id = PlaceId(entry.Place),
            words = entry.Place.Words,
            waypoint = entry.Place.Waypoint,
            shown = entry.Control is { Visible: true, Enabled: true },
        }).ToArray(),
        unplaced = Unplaced(),
        bundle = BundleKeys.Fixed.Where(key => !NoEdges.Contains(key.Key)).Select(key => BundleKeys.Chord(key.Key))
            .Concat(["escape", "tab", "shift+tab"]).Where(chord => chord.Length > 0).Distinct().ToArray(),
    };

    /// <summary>
    /// The bundle's keys the crawl leaves alone: those that move inside a place and never to another (the page's section
    /// 3), and the keypad's plus and minus, second keys of the text size, whose first keys are the window's commands.
    /// </summary>
    private static readonly Keys[] NoEdges =
    [
        BundleKeys.PageUp, BundleKeys.PageDown, BundleKeys.ParagraphUp, BundleKeys.ParagraphDown,
        BundleKeys.SelectParagraphUp, BundleKeys.SelectParagraphDown, BundleKeys.LargerTextNumpad, BundleKeys.SmallerTextNumpad,
    ];
}
