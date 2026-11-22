using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// The door (docs/axKit/events.md EV-5): the one place of axKit that plays a sound. A window or a control never plays
/// a sound for something that happened; it raises the kind of event here, and the kind's sound comes from the one
/// table, <see cref="EventPresenter"/>, so that the same kind sounds the same in every window. Whether the event is
/// raised at all (a switch of the user's, a window that is not in front) is still decided where it happens, as before
/// the door, and the words of an event are still said by the window's <c>Announce</c>: as built on 2026-11-17 the
/// door carries the sounds alone (EV-5.1), and nothing a user hears changed with it.
///
/// A kind with one sound is raised by its name alone. Three kinds still have two sounds (EV-3.3): each is raised
/// with its sound named, so that every place not yet decided reads <c>EventSound.</c> in the code and can be found;
/// when the maintainer has decided one by ear, its second sound leaves the table and the name leaves the calls.
/// <c>EventDoorTests</c> reads the sources: no other file plays a sound, every raise names its kind in so many words,
/// a sound is named only for those three kinds, and each sound here goes to the method of its own name.
///
/// The two sounds that last have calls of their own, since they are started, stopped or chosen by number:
/// <see cref="Working"/> and <see cref="BackgroundRunning"/>. UI thread only, as <see cref="Sounds"/> is.
/// </summary>
public static class EventDoor
{
    /// <summary>Raises a kind that has one sound: the sound plays.</summary>
    public static void Raise(EventKind kind)
    {
        var sounds = EventPresenter.SoundsOf(kind);
        if (sounds.Count != 1)
        {
            // A programming fault that EventDoorTests finds before a build is used; a window script fails on the line.
            Log.Error($"Events: {EventPresenter.Id(kind)} was raised by its name alone and has {sounds.Count} sounds");
        }

        if (sounds.Count > 0)
        {
            Play(sounds[0]);
        }
    }

    /// <summary>Raises one of the kinds that still have two sounds, with the sound this place has played until now.</summary>
    public static void Raise(EventKind kind, EventSound undecided)
    {
        if (EventPresenter.SoundsOf(kind).Count < 2 || !EventPresenter.Allows(kind, undecided))
        {
            Log.Error($"Events: {EventPresenter.Id(kind)} was raised with the sound {undecided}, which the table does not give it as one of two");
        }

        Play(undecided);
    }

    /// <summary>The program began or stopped working (<see cref="EventKind.Working"/>): the working music starts or stops.</summary>
    public static void Working(bool on)
    {
        if (on)
        {
            Sounds.StartMusic();
        }
        else
        {
            Sounds.StopMusic();
        }
    }

    /// <summary>Work still runs in the background (<see cref="EventKind.BackgroundRunning"/>): one chord of the recurring sound, by chord and by the number of things running.</summary>
    public static void BackgroundRunning(int chord, int things) => Sounds.BackgroundChord(chord, things);

    /// <summary>The turn is over (<see cref="EventKind.TurnDone"/>) in a window that is put away and whose one switch says its end is still heard: the sound through the mute.</summary>
    public static void TurnDoneThroughMute() => Sounds.DoneWhileMuted();

    /// <summary>No sound at all while set: the process's one window is put away (<see cref="Sounds.Muted"/>).</summary>
    public static bool Muted
    {
        get => Sounds.Muted;
        set => Sounds.Muted = value;
    }

    /// <summary>The sound by its name: each goes to the method of <see cref="Sounds"/> of the same name.</summary>
    private static void Play(EventSound sound)
    {
        switch (sound)
        {
            case EventSound.Click: Sounds.Click(); break;
            case EventSound.Tool: Sounds.Tool(); break;
            case EventSound.Edge: Sounds.Edge(); break;
            case EventSound.Heading: Sounds.Heading(); break;
            case EventSound.NavKey: Sounds.NavKey(); break;
            case EventSound.Nothing: Sounds.Nothing(); break;
            case EventSound.Unassigned: Sounds.Unassigned(); break;
            case EventSound.Recall: Sounds.Recall(); break;
            case EventSound.FormOpen: Sounds.FormOpen(); break;
            case EventSound.ListOpen: Sounds.ListOpen(); break;
            case EventSound.FieldOpen: Sounds.FieldOpen(); break;
            case EventSound.TextOpen: Sounds.TextOpen(); break;
            case EventSound.Ready: Sounds.Ready(); break;
            case EventSound.Sent: Sounds.Sent(); break;
            case EventSound.Responding: Sounds.Responding(); break;
            case EventSound.Done: Sounds.Done(); break;
            case EventSound.Question: Sounds.Question(); break;
            case EventSound.Notice: Sounds.Notice(); break;
            case EventSound.Offer: Sounds.Offer(); break;
            case EventSound.Alert: Sounds.Alert(); break;
            case EventSound.BackgroundStart: Sounds.BackgroundStart(); break;
            case EventSound.BackgroundFinish: Sounds.BackgroundFinish(); break;
            default:
                // The music and the chords last or take a number: Working and BackgroundRunning are their calls.
                Log.Error($"Events: the sound {sound} is not played by a raise; it has a call of its own");
                break;
        }
    }
}
