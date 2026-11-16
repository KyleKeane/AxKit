namespace AxClaude.Core.Flow;

/// <summary>How the events of the program's hooks are used in this run.</summary>
public enum HookUse
{
    /// <summary>The user's switch is off.</summary>
    Off,

    /// <summary>The switch is on and the settings file was not passed; <see cref="SourceHealth.HooksReason"/> says why in a word.</summary>
    NotPassed,

    /// <summary>The settings file was passed with the start.</summary>
    Passed,
}

/// <summary>How the session's record is used in this run.</summary>
public enum RecordUse
{
    /// <summary>The user's switch is off.</summary>
    Off,

    /// <summary>No file of the session was found yet.</summary>
    NotFound,

    /// <summary>The reader follows the session's file.</summary>
    Followed,

    /// <summary>The reader switched itself off; <see cref="SourceHealth.RecordReason"/> says why in a word.</summary>
    Stopped,
}

/// <summary>
/// The numbers of where axClaude's events come from (events-and-grounding.md EG-9.3, EG-9.4): plain fields that the
/// core, the hooks' pipe and the record's reader count in, each its own, all on the UI thread, and the two texts
/// made of them. Numbers and fixed words only: nothing here ever holds a word of the conversation, a path or a
/// tool's name, so both texts may be spoken, copied with the diagnostics and logged (EG-10.3). The window owns the
/// object and decides when a count starts afresh (<see cref="Reset"/>); the core only counts.
/// </summary>
public sealed class SourceHealth
{
    // The core's turns, by how they ended.
    public int TurnsFinished;
    public int TurnsInterrupted;
    public int TurnsCut;
    public int TurnsStale;

    // The marks and the bells of the console stream.
    public int MarksA;
    public int MarksC;
    public int MarksD;
    public int Bells;

    // The hooks: the window says how they are used, the pipe counts.
    public HookUse Hooks = HookUse.NotPassed;

    /// <summary>One fixed word why the settings file was not passed (<c>version</c>, <c>arguments</c>), or empty.</summary>
    public string HooksReason = "";
    public int HooksReceived;
    public int HooksRefused;
    public int HooksMalformed;
    public int HooksOtherSession;

    /// <summary>The first three numbers of the installed program's version, and of the version the events' table was verified with; empty while not known.</summary>
    public string ProgramVersion = "";
    public string TableVersion = "";

    // The session file: the window says how it is used, the reader counts.
    public RecordUse Record = RecordUse.NotFound;

    /// <summary>One fixed word why the reader stopped (<c>json</c>, <c>fields</c>, <c>file</c>), or empty.</summary>
    public string RecordReason = "";
    public int EntriesRead;
    public int LinesSkipped;
    public int LinesNotJson;
    public int LinesTooLong;

    // The gate (EG-5.9).
    public int GateReleased;
    public int GateOutsideTurn;
    public int GateUnconfirmed;
    public int GateStale;
    public int GatePending;

    // Grounding.
    public int RepliesBound;
    public int Pinned;
    public int BlocksMoved;
    public int BindingsLost;

    /// <summary>Every mark counted: the letters A, C and D together.</summary>
    public int Marks => MarksA + MarksC + MarksD;

    /// <summary>Every turn that has ended, however it did.</summary>
    public int TurnsEnded => TurnsFinished + TurnsInterrupted + TurnsCut + TurnsStale;

    /// <summary>
    /// The words for the hooks (EG-9.3): <c>off</c>, <c>not passed, version</c>, <c>on, 9 received</c>, or
    /// <c>passed, none received</c> once a turn has ended and no event has come (EG-2.10).
    /// </summary>
    public string HooksWords => Hooks switch
    {
        HookUse.Off => "off",
        HookUse.NotPassed => HooksReason.Length == 0 ? "not passed" : "not passed, " + HooksReason,
        _ => HooksReceived == 0 && TurnsEnded > 0 ? "passed, none received" : $"on, {HooksReceived} received",
    };

    /// <summary>The words for the session file (EG-9.3): <c>off</c>, <c>not found</c>, <c>followed, 212 entries</c> or <c>stopped, json</c>.</summary>
    public string RecordWords => Record switch
    {
        RecordUse.Off => "off",
        RecordUse.NotFound => "not found",
        RecordUse.Followed => $"followed, {EntriesRead} entries",
        _ => RecordReason.Length == 0 ? "stopped" : "stopped, " + RecordReason,
    };

    /// <summary>
    /// What Say the event sources says (EG-9.3). <paramref name="coreWords"/> are the window's words for what the
    /// core is in charge of: <c>in charge of state, Done and speech</c>, or <c>in shadow</c>.
    /// </summary>
    public string Said(string coreWords) =>
        $"Events. Marks: {Marks}. Hooks: {HooksWords}. Session file: {RecordWords}. Core: {coreWords}.";

    /// <summary>
    /// The lines Copy diagnostics gains (EG-9.4). <paramref name="coreWords"/> as for <see cref="Said"/>, in the
    /// diagnostics' form: <c>in charge of state, Done, speech</c>, or <c>in shadow</c>.
    /// </summary>
    public IReadOnlyList<string> Lines(string coreWords) =>
    [
        $"Event core: {coreWords}; turns {TurnsFinished} finished, {TurnsInterrupted} interrupted, {TurnsCut} cut, {TurnsStale} stale",
        $"Marks: {Marks} (A {MarksA}, C {MarksC}, D {MarksD}), bells {Bells}",
        $"Hooks: {HooksWords}, {HooksRefused} refused, {HooksMalformed} malformed, {HooksOtherSession} of another session; Claude Code {Known(ProgramVersion)}, table {Known(TableVersion)}",
        $"Session file: {RecordWordsRead}, {LinesSkipped} lines skipped, {LinesNotJson} not JSON, {LinesTooLong} too long",
        $"Gate: {GateReleased} released, {GateOutsideTurn} outside a turn, {GateUnconfirmed} unconfirmed, {GateStale} stale, {GatePending} pending",
        $"Grounding: {RepliesBound} replies bound, {Pinned} pinned, {BlocksMoved} blocks moved, {BindingsLost} bindings lost",
    ];

    /// <summary>
    /// Every count back to zero, for a start of the program when the window wants the numbers of this run alone.
    /// What the window itself says of the hooks and the session file (<see cref="Hooks"/>, <see cref="Record"/>,
    /// the reasons, the versions) stays: it sets those with each start.
    /// </summary>
    public void Reset()
    {
        TurnsFinished = TurnsInterrupted = TurnsCut = TurnsStale = 0;
        MarksA = MarksC = MarksD = Bells = 0;
        HooksReceived = HooksRefused = HooksMalformed = HooksOtherSession = 0;
        EntriesRead = LinesSkipped = LinesNotJson = LinesTooLong = 0;
        GateReleased = GateOutsideTurn = GateUnconfirmed = GateStale = GatePending = 0;
        RepliesBound = Pinned = BlocksMoved = BindingsLost = 0;
    }

    /// <summary>The session file's words with the count named as read, as the diagnostics' line has it.</summary>
    private string RecordWordsRead => Record == RecordUse.Followed
        ? $"followed, {EntriesRead} entries read"
        : $"{RecordWords}, {EntriesRead} entries read";

    private static string Known(string version) => version.Length == 0 ? "unknown" : version;
}
