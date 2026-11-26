using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>
/// The door (docs/axKit/events.md EV-5): the one place of axKit that plays a sound. A window or a control never plays
/// a sound for something that happened; it raises the kind of event here, and the kind's sound comes from the one
/// table, <see cref="EventPresenter"/>, so that the same kind sounds the same in every window. Whether the event is
/// raised at all (a switch of the user's, a window that is not in front) is still decided where it happens, as before
/// the door. As built on 2026-11-17 the door carried the sounds alone (EV-5.1); since 2026-11-25 (events.md section 9
/// point 4) the words come through it too: every announcement of a window, a notice or a shared control is raised
/// here with its kind (<see cref="Say(EventKind, AxKitForm, string, bool)"/>, <see cref="SayOn"/>) and then said as
/// before, by the window's own speaking, so that nothing a user hears changed with either step. Every event that
/// reaches the door, by its sound or by its words, is noted in the log of events while the user keeps one
/// (<see cref="EventLogger"/>: its time and its kind, never its words); a kind's sound and then its words in the same
/// turn are one event, noted once.
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

        Note(kind);
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

        Note(kind);
        Play(undecided);
    }

    // ---- the words of events (EV-5.3; section 9 point 4, through the door from the first day, 2026-11-25) ----

    /// <summary>
    /// The words of an event, said by a window: the kind is noted, and the words go to the window's own speaking, which
    /// joins them, holds them until the window is in front and drops them while it is put away, as before (AX-7.3).
    /// No sound: a kind's sound is raised by its own call, where it was before the words came here, so that the order
    /// and the timing of what is heard stay as they were. The window's <c>Announce</c> calls it; an
    /// announcement that names no kind is raised as <see cref="EventKind.Said"/>.
    /// </summary>
    public static bool Say(EventKind kind, AxKitForm window, string words, bool interrupt)
    {
        Note(kind);
        return window.Speak(words, interrupt);
    }

    /// <summary>Words of an event said ahead of what follows ("Claude ran Word wrap", MC-7.5): noted, and held by the window to be joined to the next words.</summary>
    public static void SayAhead(EventKind kind, AxKitForm window, string words)
    {
        Note(kind);
        window.SpeakAhead(words);
    }

    /// <summary>The words of an event said at once on a control, as one notification: a check box's new state, a notice's own words, the transcript's.</summary>
    public static bool SayOn(EventKind kind, Control control, string words, bool interrupt)
    {
        Note(kind);
        return Announcer.Raise(control, words, interrupt);
    }

    /// <summary>
    /// Words already raised at the door by <see cref="Say(EventKind, AxKitForm, string, bool)"/>, said now: the window's
    /// joined sentence, what waited for its activation. The window base and the notice alone call it (<c>EventDoorTests</c>).
    /// </summary>
    internal static bool Speak(Control control, string words, bool interrupt) => Announcer.Raise(control, words, interrupt);

    /// <summary>The kind last noted, and in which turn: the same kind again in the same turn (its sound and its words, a burst of lines in one batch) is the same event.</summary>
    private static EventKind _notedKind;

    private static long _notedTurn = -1;

    /// <summary>Notes the event in the log of events (<see cref="EventLogger"/>, which writes nothing while it is off), once for a kind in a turn.</summary>
    private static void Note(EventKind kind)
    {
        if (_counting && _notedTurn == _turn && _notedKind == kind)
        {
            return;
        }

        _notedKind = kind;
        _notedTurn = _turn;
        EventLogger.Note(kind);
    }

    /// <summary>The program began or stopped working (<see cref="EventKind.Working"/>): the working music starts or stops.</summary>
    public static void Working(bool on)
    {
        if (on)
        {
            Note(EventKind.Working);
            Sounds.StartMusic();
        }
        else
        {
            Sounds.StopMusic();
        }
    }

    /// <summary>Work still runs in the background (<see cref="EventKind.BackgroundRunning"/>): one chord of the recurring sound, by chord and by the number of things running.</summary>
    public static void BackgroundRunning(int chord, int things)
    {
        Note(EventKind.BackgroundRunning);
        Sounds.BackgroundChord(chord, things);
    }

    /// <summary>The turn is over (<see cref="EventKind.TurnDone"/>) in a window that is put away and whose one switch says its end is still heard: the sound through the mute.</summary>
    public static void TurnDoneThroughMute()
    {
        Note(EventKind.TurnDone);
        Sounds.DoneWhileMuted();
    }

    /// <summary>No sound at all while set: the process's one window is put away (<see cref="Sounds.Muted"/>).</summary>
    public static bool Muted
    {
        get => Sounds.Muted;
        set => Sounds.Muted = value;
    }

    /// <summary>The sound by its name: each goes to the method of <see cref="Sounds"/> of the same name.</summary>
    // ---- turns (AX-7.11): which sounds came of one and the same key press or arrival ----

    /// <summary>The turn of the window's thread now: one for each message it takes off its queue (a key, a timer's tick, a posted call).</summary>
    private static long _turn;

    /// <summary>The turn in which a sound of a moment last played; the ticks of arriving lines and the entry sounds themselves do not count.</summary>
    private static long _momentTurn = -1;

    private static bool _counting;

    private sealed class TurnCounter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            _turn++;
            return false;
        }
    }

    /// <summary>Starts counting the turns of this thread, once; every window calls it as it is made.</summary>
    public static void CountTurns()
    {
        if (!_counting)
        {
            _counting = true;
            Application.AddMessageFilter(new TurnCounter());
        }
    }

    /// <summary>The turn now, for a caller that asks <see cref="SoundedIn"/> about it later.</summary>
    public static long Turn => _turn;

    /// <summary>
    /// Whether a sound of something that happened played in that turn: an entry sound asked for in the same turn
    /// then stays silent (<see cref="AxKitForm.SoundOnEntry"/>, AX-7.11), so that a refusal, a notice that opened by
    /// itself or a program's question is heard alone, and never cut off by the sound of the focus arriving where
    /// that same key or arrival sent it. Exact, and no matter of time: two keys pressed one right after the other
    /// are two turns (a rule of "within 150 ms" was tried first and silenced the second key's entry).
    /// </summary>
    public static bool SoundedIn(long turn) => _momentTurn == turn;

    private static void Play(EventSound sound)
    {
        if (sound is not (EventSound.Click or EventSound.Tool or EventSound.FormOpen or EventSound.ListOpen or EventSound.FieldOpen or EventSound.TextOpen))
        {
            _momentTurn = _turn;
        }

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
