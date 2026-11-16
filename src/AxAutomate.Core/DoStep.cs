using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// What the step <c>do</c> comes to (docs/axKit/user-workflows.md WF-4): the command's key is pressed
/// (<see cref="Press"/>), its menu item is run (<see cref="Choose"/>), or the step fails: the window does not list its
/// commands as data (<see cref="NoData"/>), it shows a notice (<see cref="Notice"/>), the name finds no one command
/// (<see cref="Name"/>), or no key of the command acts where the focus is (<see cref="NoAct"/>).
/// </summary>
public enum DoKind
{
    Press,
    Choose,
    NoData,
    Notice,
    Name,
    NoAct,
}

/// <summary>
/// The decision of <see cref="DoStep.Decide"/>: its kind; the chord to press (empty unless the kind is
/// <see cref="DoKind.Press"/>); the row the name resolved to (null when none was found); and the step's detail: what a
/// passed step shows after its line ("key ctrl+f", "menu item"), or why the step fails.
/// </summary>
public sealed record DoDecision(DoKind Kind, string Chord, ListedCommand? Row, string Detail);

/// <summary>
/// The step <c>do &lt;name&gt;</c> decided from the window's own listing (docs/axKit/user-workflows.md WF-4.1 to
/// WF-4.5 and WF-4.9), pure: the name is resolved by the one resolver (<see cref="CommandNames.Resolve"/>) among the
/// rows the window listed at that moment, and the key is the one the window says would run the command now, so a key
/// changed in a key table changes no script. <see cref="Run"/> asks the window, decides here and then presses or
/// chooses on its tier. Tested in <c>tests/AxAutomate.Tests/DoStepTests</c>.
/// </summary>
public static class DoStep
{
    /// <summary>What a passed step shows when the command's key was pressed, before the chord: "key ctrl+f" (WF-4.6).</summary>
    public const string KeyDetail = "key ";

    /// <summary>What a passed step shows when the command ran by its menu item (WF-4.3).</summary>
    public const string MenuItemDetail = "menu item";

    /// <summary>
    /// In this order: no listing (a window from before the request <c>commands data</c>) is <see cref="DoKind.NoData"/>;
    /// a listing with a notice is <see cref="DoKind.Notice"/> whatever the name, since a window that shows a notice
    /// runs no command of its list; a name that is unknown or ambiguous is <see cref="DoKind.Name"/> with the
    /// resolver's refusal. For the row found: <see cref="DoKind.Press"/> with its <c>now</c> chord when it has one;
    /// <see cref="DoKind.Choose"/> when it is a menu item with no chord at all (no key, or a key the menu only shows
    /// as text); otherwise <see cref="DoKind.NoAct"/>: it has chords and none acts where the focus is, or it is a key
    /// with no menu item whose key has no name a script can press. Nothing is pressed for a step that fails.
    /// </summary>
    public static DoDecision Decide(WindowCommands? listing, string name)
    {
        if (listing is null)
        {
            return new DoDecision(DoKind.NoData, string.Empty, null, "this AxKit.exe does not list its commands as data");
        }

        if (listing.Notice is { } notice)
        {
            var shown = notice.Trim().Length > 0 ? "the notice " + notice.Trim() : "a notice";
            return new DoDecision(DoKind.Notice, string.Empty, null, $"the window shows {shown}: its commands wait");
        }

        var names = listing.Commands.Select(row => new CommandName(row.Label, row.Menu)).ToList();
        var resolution = CommandNames.Resolve(names, name);
        if (resolution.Match != CommandMatch.Found)
        {
            return new DoDecision(DoKind.Name, string.Empty, null, CommandNames.Refusal(resolution, names));
        }

        var row = listing.Commands[resolution.Index];
        if (row.Now.Length > 0)
        {
            return new DoDecision(DoKind.Press, row.Now, row, KeyDetail + row.Now);
        }

        IReadOnlyList<string> chords = row.Key.Length > 0 ? [row.Key, .. row.Also] : row.Also;
        if (chords.Count == 0 && !row.KeyOnly)
        {
            return new DoDecision(DoKind.Choose, string.Empty, row, MenuItemDetail);
        }

        // What a person reads for the key: the menu's text, or the chords of a row whose keys came from keys alone.
        var keyText = row.KeyText.Length > 0 ? row.KeyText : string.Join(", ", chords);
        return chords.Count == 0
            ? new DoDecision(DoKind.NoAct, string.Empty, row, $"the key of {row.Label} ({keyText}) has no name a script can press, and the command has no menu item; the focus is in")
            : new DoDecision(DoKind.NoAct, string.Empty, row, $"the key of {row.Label} ({keyText}) does not act where the focus is");
    }
}
