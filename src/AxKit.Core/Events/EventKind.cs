namespace AxKit.Core.Events;

/// <summary>
/// The kinds of event of axKit (docs/axKit/events.md EV-3): everything that reaches the user is one of these. The list
/// is closed and is docs/axKit/families.json's, "events", in its order and its six classes; that file says for each
/// kind when it happens, what starts it and its words, and <c>EventKindTests</c> holds this list and the file to each
/// other both ways. A new kind is added in both on purpose, with its sound or none (<see cref="EventPresenter"/>),
/// never invented where it is raised. A kind's id in the file is its name here in small letters with hyphens
/// (<see cref="EventPresenter.Id"/>).
/// </summary>
public enum EventKind
{
    // ---- What came of your own command ----

    /// <summary>A character typed into a field.</summary>
    Typed,

    /// <summary>The key or command moved you, and there is something there.</summary>
    Landed,

    /// <summary>The move landed on a heading of a text or a section row of a list.</summary>
    LandedOnHeading,

    /// <summary>In a text's navigation mode, a letter was taken as a command that has somewhere to go.</summary>
    Taken,

    /// <summary>The command ran and there is nothing of that kind to go to.</summary>
    NothingFound,

    /// <summary>Nothing further that way: the first or last entry, the top level, the end of the history.</summary>
    Edge,

    /// <summary>The key has no command where you are.</summary>
    NoCommand,

    /// <summary>The change asked for is not allowed here and nothing changed.</summary>
    Refused,

    /// <summary>A command that only tells something said it.</summary>
    Said,

    /// <summary>A setting or a state of the window changed.</summary>
    Changed,

    /// <summary>The command did its work at once.</summary>
    Done,

    /// <summary>Your message, command or answer went out to the program underneath.</summary>
    Sent,

    /// <summary>An earlier command came back into the field from the history.</summary>
    Recalled,

    /// <summary>The command asks before it acts.</summary>
    Asked,

    /// <summary>Something opened at your command.</summary>
    Opened,

    /// <summary>A form opened in the window at your command: several things asked for one action (AX-7.11).</summary>
    FormOpened,

    /// <summary>A list to choose one of opened in the window at your command (AX-7.11).</summary>
    ListOpened,

    /// <summary>One field to type in opened at your command: a name, a search, a rename in place (AX-7.11).</summary>
    FieldOpened,

    /// <summary>A text to read opened in the window at your command: a help text, a question's words, an error (AX-7.11).</summary>
    TextOpened,

    /// <summary>The command started work that takes time.</summary>
    Busy,

    /// <summary>The command could not do it.</summary>
    Failed,

    /// <summary>You said no, pressed Escape, or the thing was called off.</summary>
    Cancelled,

    // ---- Something arrived ----

    /// <summary>A line of output arrived from the program.</summary>
    Line,

    /// <summary>Claude called a tool.</summary>
    ToolCall,

    /// <summary>Lines of a reply or of a command's output became final and are spoken.</summary>
    ReplySpoken,

    /// <summary>The program announced something of its own.</summary>
    ProgramSaid,

    /// <summary>The status bar or the title followed the program.</summary>
    StatusChanged,

    /// <summary>Something was copied in any program while axClip keeps the history.</summary>
    ClipboardEntry,

    /// <summary>Another program changed the file or the folder the window shows.</summary>
    ChangedOnDisk,

    /// <summary>Another axKit window handed something to this one.</summary>
    HandedOver,

    // ---- Where the program stands ----

    /// <summary>The program is ready for your first message or command of this run.</summary>
    Ready,

    /// <summary>Claude began its reply.</summary>
    Responding,

    /// <summary>The program is working; its sound lasts while it does (<c>EventDoor.Working</c>).</summary>
    Working,

    /// <summary>The program began a turn by itself.</summary>
    TurnSelfStarted,

    /// <summary>The turn is over.</summary>
    TurnDone,

    /// <summary>The next file of the queue started playing.</summary>
    NextTrack,

    /// <summary>The program underneath ended or crashed.</summary>
    ProgramEnded,

    // ---- Something asks you ----

    /// <summary>The program asks a question or a permission.</summary>
    ProgramAsks,

    /// <summary>Claude shows a screen that only informs.</summary>
    ProgramScreen,

    /// <summary>The program took its question back by itself.</summary>
    QuestionClosed,

    /// <summary>Claude offers a command for you to run yourself.</summary>
    CommandOffer,

    /// <summary>An AI tool asks to run a command of this window that acts outside the window.</summary>
    AiAsksToRun,

    /// <summary>A new AI client asks to use axKit.</summary>
    Pairing,

    /// <summary>A notice opened that no command of yours asked for.</summary>
    NoticeByItself,

    /// <summary>A notice that comes some time after your command, when its work is finished.</summary>
    LateNotice,

    /// <summary>The program is done or asks while its window is not in front.</summary>
    CalledFromBehind,

    // ---- Work that goes on behind ----

    /// <summary>Claude started work in the background.</summary>
    BackgroundStarted,

    /// <summary>Work is still running in the background; its sound is a chord by number (<c>EventDoor.BackgroundRunning</c>).</summary>
    BackgroundRunning,

    /// <summary>Work in the background finished.</summary>
    BackgroundFinished,

    /// <summary>Work of axKit's own that you started a while ago ended.</summary>
    JobFinished,

    /// <summary>An AI tool ran a command of this window or added a task.</summary>
    AiRan,

    /// <summary>axPass locked the database by itself.</summary>
    LockedItself,

    /// <summary>axKit did something small without a word; named so that its silence is a decision, and never raised (EV-5.6).</summary>
    Upkeep,

    // ---- The window itself ----

    /// <summary>A window opened or came to the front for the first time.</summary>
    WindowOpened,

    /// <summary>A hotkey brought the window back while its field still held unsent text.</summary>
    BroughtUp,

    /// <summary>The window had something to tell at its start.</summary>
    StartNote,
}

/// <summary>
/// The sounds of axKit by name: the ids of docs/axKit/families.json, "sounds", each one sound of
/// <c>AxKit.Forms/Sounds</c>. A sound is made or changed there, by the maintainer's ear; which kind of event has
/// which is <see cref="EventPresenter"/>'s.
/// </summary>
public enum EventSound
{
    Click,
    Tool,
    Edge,
    Heading,
    NavKey,
    Nothing,
    Unassigned,
    Recall,

    /// <summary>The four sounds of a way of working starting (AX-7.11): bell pings, a run of beeps up and then the kind going down.</summary>
    FormOpen,
    ListOpen,
    FieldOpen,
    TextOpen,
    Ready,
    Sent,
    Responding,
    Done,
    Question,
    Notice,
    Offer,
    Alert,

    /// <summary>The working music, which lasts: started and stopped, never played once.</summary>
    Music,
    BackgroundStart,
    BackgroundFinish,

    /// <summary>A chord of the recurring sound of background work, by chord and number of things.</summary>
    BackgroundChord,
}
