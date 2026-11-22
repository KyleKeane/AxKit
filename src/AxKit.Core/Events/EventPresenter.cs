using System.Text;

namespace AxKit.Core.Events;

/// <summary>
/// The table behind the door (docs/axKit/events.md EV-5.2): for each kind of event its sound, or none. One kind, one
/// sound, so that the same thing sounds the same in every window; a sound is given to a kind or changed here and
/// nowhere else, and only when the maintainer has chosen it by ear. Three kinds still have two sounds, as the windows
/// played them before the door (2026-11-17): a place that raises one of them names which of the two it plays, until
/// the maintainer has decided for each whether it becomes one sound or two kinds (EV-3.3). <c>EventKindTests</c>
/// holds the table to docs/axKit/families.json, where each kind's sounds are written for people, in its order.
/// </summary>
public static class EventPresenter
{
    // Before the table below, which is filled from it: static fields are made in the order they are written.
    private static readonly IReadOnlyList<EventSound> None = [];

    /// <summary>The sounds of each kind, by the kind's number: made once, so that asking allocates nothing (the tick of an arriving line asks for every line).</summary>
    private static readonly IReadOnlyList<EventSound>[] Table = MakeTable();

    /// <summary>The sounds of a kind: none, one, or two for the three kinds not yet decided.</summary>
    public static IReadOnlyList<EventSound> SoundsOf(EventKind kind) =>
        (int)kind >= 0 && (int)kind < Table.Length ? Table[(int)kind] : None;

    /// <summary>Whether the kind has this sound.</summary>
    public static bool Allows(EventKind kind, EventSound sound)
    {
        var sounds = SoundsOf(kind);
        for (var i = 0; i < sounds.Count; i++)
        {
            if (sounds[i] == sound)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The kinds are numbered from 0 in the order they are written (<c>EventKindTests</c> holds it), so a kind's number is its place in the table.</summary>
    private static IReadOnlyList<EventSound>[] MakeTable()
    {
        var kinds = Enum.GetValues<EventKind>();
        var table = new IReadOnlyList<EventSound>[kinds.Length];
        foreach (var kind in kinds)
        {
            table[(int)kind] = Given(kind);
        }

        return table;
    }

    /// <summary>The table as it is written: read here, changed here.</summary>
    private static IReadOnlyList<EventSound> Given(EventKind kind) => kind switch
    {
        // What came of your own command.
        EventKind.LandedOnHeading => [EventSound.Heading],
        EventKind.Taken => [EventSound.NavKey],
        EventKind.NothingFound => [EventSound.Nothing, EventSound.Edge],
        EventKind.Edge => [EventSound.Edge],
        EventKind.NoCommand => [EventSound.Unassigned, EventSound.Edge],
        EventKind.Refused => [EventSound.Edge],
        EventKind.Sent => [EventSound.Sent],
        EventKind.Recalled => [EventSound.Recall],
        EventKind.FormEntered => [EventSound.FormOpen],
        EventKind.ListEntered => [EventSound.ListOpen],
        EventKind.FieldEntered => [EventSound.FieldOpen],
        EventKind.TextEntered => [EventSound.TextOpen],
        EventKind.Failed => [EventSound.Notice],

        // Something arrived.
        EventKind.Line => [EventSound.Click],
        EventKind.ToolCall => [EventSound.Tool],

        // Where the program stands.
        EventKind.Ready => [EventSound.Ready],
        EventKind.Responding => [EventSound.Responding],
        EventKind.Working => [EventSound.Music],
        EventKind.TurnDone => [EventSound.Done],

        // Something asks you.
        EventKind.ProgramAsks => [EventSound.Notice, EventSound.Question],
        EventKind.ProgramScreen => [EventSound.Notice],
        EventKind.CommandOffer => [EventSound.Offer],
        EventKind.AiAsksToRun => [EventSound.Offer],
        EventKind.Pairing => [EventSound.Notice],
        EventKind.NoticeByItself => [EventSound.Notice],
        EventKind.CalledFromBehind => [EventSound.Alert],

        // Work that goes on behind.
        EventKind.BackgroundStarted => [EventSound.BackgroundStart],
        EventKind.BackgroundRunning => [EventSound.BackgroundChord],
        EventKind.BackgroundFinished => [EventSound.BackgroundFinish],
        EventKind.AiRan => [EventSound.Notice],

        // The window itself.
        EventKind.BroughtUp => [EventSound.Recall],

        // Every other kind is silent: words alone, or nothing at all.
        _ => None,
    };

    /// <summary>
    /// The kind's id as docs/axKit/families.json writes it: its name in small letters, a hyphen before each capital
    /// after the first ("landed-on-heading").
    /// </summary>
    public static string Id(EventKind kind)
    {
        var name = kind.ToString();
        var id = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                id.Append('-');
            }

            id.Append(char.ToLowerInvariant(name[i]));
        }

        return id.ToString();
    }

    /// <summary>The kind an id of the file names; null for a word that is no kind.</summary>
    public static EventKind? Of(string id)
    {
        foreach (var kind in Enum.GetValues<EventKind>())
        {
            if (Id(kind) == id)
            {
                return kind;
            }
        }

        return null;
    }
}
