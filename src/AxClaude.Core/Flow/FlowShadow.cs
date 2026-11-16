using System.Globalization;
using System.Text;
using AxKit.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>What the window's own code did, as the shadow is told it: it said Done, its working state began or ended. The lines it spoke go to <see cref="FlowShadow.WindowSpoke"/>.</summary>
public enum ShadowFact
{
    SaidDone,
    Working,
    Idle,
}

/// <summary>
/// The kinds of what a side said in a turn, the closed set the shadow's line names them by (events-and-grounding.md
/// EG-4.11): Done, or a spoken line by the kind the conversation gives it (<see cref="FlowShadow.KindOf"/>). The
/// line is built from these values alone, so no text of a line can reach it.
/// </summary>
public enum ShadowKind
{
    /// <summary>The utterance Done at a turn's end.</summary>
    Done,

    /// <summary>A reply's first row, the one with Claude's label.</summary>
    Reply,

    /// <summary>A plain row: a reply's further rows.</summary>
    Row,

    /// <summary>A tool call's row, or a tool's error.</summary>
    Tool,

    /// <summary>A row of Claude's thinking.</summary>
    Thinking,

    /// <summary>A system line, an error or a warning.</summary>
    Notice,

    /// <summary>A prompt row, an answer's row, or the app's record of a question or a side question.</summary>
    Question,

    /// <summary>The user's own message: its echo, its rows, its markers.</summary>
    Sent,

    /// <summary>A turn's summary row.</summary>
    Summary,

    /// <summary>A live progress row of work in the background.</summary>
    Background,

    /// <summary>Any other kind of line, and any value this version does not know.</summary>
    Other,
}

/// <summary>
/// The comparison of the core with the window's own code while the core runs in shadow (events-and-grounding.md
/// EG-4.11): the window tells it what its old code did (<see cref="Window"/>, <see cref="WindowSpoke"/>,
/// <see cref="QuestionOpen"/>) and hands it every decision of the core it did not carry out (<see cref="Core"/>),
/// and once for each turn it gives one line of numbers saying where the two differed (<see cref="TurnLine"/>): when
/// each said Done, how many reply lines each would speak, and for how long one said working while the other did
/// not; and, for a turn that differed, the kinds of what each said in order, each side's states with their
/// milliseconds, and whether a question was open. Numbers and fixed words only: nothing of the conversation is
/// kept, only counts, times, and the kinds and states as values of <see cref="ShadowKind"/> and
/// <see cref="FlowState"/> (G7).
///
/// A turn here is a stretch of work: from the first thing either side does until both are idle and neither has
/// done anything for <see cref="SettleMs"/>. So the window's Done, which comes 400 ms after the bell, and the
/// core's, which may come later still, both land in the turn they belong to; and a message that waited, whose turn
/// starts within moments of the turn before, makes one stretch with two Dones on each side, never two stretches
/// that each miss one.
/// </summary>
public sealed class FlowShadow
{
    /// <summary>How long both sides must have been idle and silent before a turn counts as ended by both: past the window's settle after the bell (400 ms) and the core's latest Done (600 ms after the bell).</summary>
    public const int SettleMs = 1500;

    /// <summary>
    /// The two states apart for less than this are one turn seen through two sources: the core hears the marks, the
    /// window's code the frame that follows them, up to a frame's quiet time later at each end (109 ms in the first
    /// run with a stand-in). From here on the line names it.
    /// </summary>
    public const int StateDiffersMs = 250;

    /// <summary>The most entries a list of kinds or of states holds in the line; what comes after them is the one word <c>more</c>.</summary>
    public const int MaxListed = 24;

    private const long Never = long.MaxValue;

    private readonly List<long> _windowDone = [];
    private readonly List<long> _coreDone = [];
    private readonly List<(ShadowKind Kind, int Count)> _windowKinds = [];
    private readonly List<(ShadowKind Kind, int Count)> _coreKinds = [];
    private readonly List<(FlowState State, long Ms)> _windowStates = [];
    private readonly List<(FlowState State, long Ms)> _coreStates = [];
    private bool _noted;
    private int _turns;
    private long _startAt;
    private long _lastAt;
    private bool _windowWorking;
    private bool _coreWorking;
    private long _windowSince;
    private long _coreSince;
    private long _differSince;
    private long _differedMs;
    private int _windowLines;
    private int _coreLines;
    private bool _question;

    /// <summary>What the window's own code did, other than speaking lines.</summary>
    public void Window(ShadowFact fact, long nowMs)
    {
        switch (fact)
        {
            case ShadowFact.SaidDone:
                Note(nowMs);
                _windowDone.Add(nowMs - _startAt);
                Said(_windowKinds, ShadowKind.Done);
                break;
            case ShadowFact.Working:
                SetWorking(window: true, working: true, nowMs);
                break;
            case ShadowFact.Idle:
                SetWorking(window: true, working: false, nowMs);
                break;
        }
    }

    /// <summary>The window's own code spoke these lines as one utterance: their number is counted and their kinds are kept, never their text.</summary>
    public void WindowSpoke(IReadOnlyList<Line> lines, long nowMs)
    {
        if (lines.Count > 0)
        {
            Note(nowMs);
            _windowLines += lines.Count;
            Said(_windowKinds, lines);
        }
    }

    /// <summary>
    /// The window found Claude waiting on a question, a permission prompt or a screen, as it does at every pass
    /// while one is open. Inside a turn it marks the turn; it opens none and keeps none open.
    /// </summary>
    public void QuestionOpen()
    {
        if (_noted)
        {
            _question = true;
        }
    }

    /// <summary>A decision of the core's that the window did not carry out: its state, its Done, the reply lines it would speak. Any other decision says nothing of a turn.</summary>
    public void Core(FlowDecision decision, long nowMs)
    {
        switch (decision)
        {
            case StateChanged state:
                SetWorking(window: false, state.State == FlowState.Working, nowMs);
                break;
            case SayDone:
                Note(nowMs);
                _coreDone.Add(nowMs - _startAt);
                Said(_coreKinds, ShadowKind.Done);
                break;
            case SpeakReply speak:
                Note(nowMs);
                _coreLines += speak.Lines.Count;
                Said(_coreKinds, speak.Lines);
                break;
        }
    }

    /// <summary>When <see cref="TurnLine"/> will have its line, for the window's timer; a time that never comes while a side works or nothing was noted.</summary>
    public long DueAt => _noted && !_windowWorking && !_coreWorking ? _lastAt + SettleMs : Never;

    /// <summary>
    /// The one line for a turn that both have ended, and null until then: something was noted, both sides are idle,
    /// and nothing more was noted for <see cref="SettleMs"/>. The time that never comes (<see cref="long.MaxValue"/>)
    /// asks for the line of a turn noted now whatever its age: the program has ended. The line, for example:
    /// <c>turn=3 differ=done done window=5412 core=none spoke window=2 core=2 state-differed=38</c>. The times of
    /// Done are milliseconds after the turn's first sign; <c>differ</c> names what differed: <c>done</c> (how often
    /// it was said), <c>spoke</c> (how many lines), <c>state</c> (for <see cref="StateDiffersMs"/> or more), or
    /// <c>none</c>. A turn that differed, and no other, goes on with what differed:
    /// <c> kinds window=[reply,row*4,done] core=[reply,row*2,done] states window=[working:4200,idle:1800]
    /// core=[working:6000] question=open</c>: the kinds of what each side said, in order (<see cref="ShadowKind"/>,
    /// a kind said several times running as <c>row*4</c>), each side's states from the turn's first sign to its last
    /// with their milliseconds (<see cref="FlowState"/>), and <c>question=open</c> when Claude waited on a question
    /// in the turn. Each list holds <see cref="MaxListed"/> entries at most, then <c>more</c>.
    /// </summary>
    public string? TurnLine(long nowMs)
    {
        if (!_noted || _windowWorking || _coreWorking || (nowMs != Never && nowMs - _lastAt < SettleMs))
        {
            return null;
        }

        _turns++;
        var differ = new List<string>(3);
        if (_windowDone.Count != _coreDone.Count)
        {
            differ.Add("done");
        }

        if (_windowLines != _coreLines)
        {
            differ.Add("spoke");
        }

        if (_differedMs >= StateDiffersMs)
        {
            differ.Add("state");
        }

        var line = string.Create(CultureInfo.InvariantCulture,
            $"turn={_turns} differ={(differ.Count == 0 ? "none" : string.Join(',', differ))} done window={Times(_windowDone)} core={Times(_coreDone)} spoke window={_windowLines} core={_coreLines} state-differed={_differedMs}");
        if (differ.Count > 0)
        {
            // Both are idle here: each side's last stretch runs to the turn's last sign.
            Stretch(_windowStates, FlowState.Idle, _lastAt - _windowSince);
            Stretch(_coreStates, FlowState.Idle, _lastAt - _coreSince);
            line += What(_windowKinds, _coreKinds, _windowStates, _coreStates, _question);
        }

        _noted = false;
        _windowDone.Clear();
        _coreDone.Clear();
        _windowKinds.Clear();
        _coreKinds.Clear();
        _windowStates.Clear();
        _coreStates.Clear();
        _windowLines = 0;
        _coreLines = 0;
        _differedMs = 0;
        _question = false;
        return line;
    }

    /// <summary>
    /// The kind a spoken line is named by: from the line's kind alone, never its text. A kind this table does not
    /// name is <see cref="ShadowKind.Other"/>.
    /// </summary>
    public static ShadowKind KindOf(LineKind kind) => kind switch
    {
        LineKind.ClaudeReply => ShadowKind.Reply,
        LineKind.Plain => ShadowKind.Row,
        LineKind.Tool or LineKind.ToolError => ShadowKind.Tool,
        LineKind.Thinking => ShadowKind.Thinking,
        LineKind.System or LineKind.Error or LineKind.Warning => ShadowKind.Notice,
        LineKind.Prompt or LineKind.PromptOption or LineKind.Question or LineKind.SideQuestion or LineKind.AnswerRecord => ShadowKind.Question,
        LineKind.UserEcho or LineKind.UserMessage or LineKind.InputMarker or LineKind.OutputMarker => ShadowKind.Sent,
        LineKind.TurnSummary => ShadowKind.Summary,
        LineKind.Progress => ShadowKind.Background,
        _ => ShadowKind.Other,
    };

    /// <summary>The word of a kind in the line; a value outside the set is <c>other</c>.</summary>
    public static string Word(ShadowKind kind) => kind switch
    {
        ShadowKind.Done => "done",
        ShadowKind.Reply => "reply",
        ShadowKind.Row => "row",
        ShadowKind.Tool => "tool",
        ShadowKind.Thinking => "thinking",
        ShadowKind.Notice => "notice",
        ShadowKind.Question => "question",
        ShadowKind.Sent => "sent",
        ShadowKind.Summary => "summary",
        ShadowKind.Background => "background",
        _ => "other",
    };

    /// <summary>The word of a state in the line, the core's own names; a value outside the set is <c>other</c>.</summary>
    public static string Word(FlowState state) => state switch
    {
        FlowState.Idle => "idle",
        FlowState.Working => "working",
        _ => "other",
    };

    /// <summary>
    /// What a turn that differed goes on with (<see cref="TurnLine"/>), from values alone: the words of
    /// <see cref="Word(ShadowKind)"/> and <see cref="Word(FlowState)"/>, whole numbers, and the line's own fixed
    /// words. A count or a time below zero is written as 0. Whatever is handed in, nothing else can be in the text.
    /// </summary>
    public static string What(
        IReadOnlyList<(ShadowKind Kind, int Count)> windowKinds,
        IReadOnlyList<(ShadowKind Kind, int Count)> coreKinds,
        IReadOnlyList<(FlowState State, long Ms)> windowStates,
        IReadOnlyList<(FlowState State, long Ms)> coreStates,
        bool question)
    {
        var text = new StringBuilder(" kinds window=");
        Kinds(text, windowKinds);
        text.Append(" core=");
        Kinds(text, coreKinds);
        text.Append(" states window=");
        States(text, windowStates);
        text.Append(" core=");
        States(text, coreStates);
        if (question)
        {
            text.Append(" question=open");
        }

        return text.ToString();
    }

    private static void Kinds(StringBuilder text, IReadOnlyList<(ShadowKind Kind, int Count)> kinds)
    {
        text.Append('[');
        for (var i = 0; i < kinds.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            if (i == MaxListed)
            {
                text.Append("more");
                break;
            }

            text.Append(Word(kinds[i].Kind));
            if (kinds[i].Count > 1)
            {
                text.Append('*').Append(kinds[i].Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        text.Append(']');
    }

    private static void States(StringBuilder text, IReadOnlyList<(FlowState State, long Ms)> states)
    {
        text.Append('[');
        for (var i = 0; i < states.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            if (i == MaxListed)
            {
                text.Append("more");
                break;
            }

            text.Append(Word(states[i].State)).Append(':').Append(Math.Max(0, states[i].Ms).ToString(CultureInfo.InvariantCulture));
        }

        text.Append(']');
    }

    /// <summary>A side said one thing of this kind: it joins the kind said last when that is the same, and nothing is kept past the line's limit but the sign that there was more.</summary>
    private static void Said(List<(ShadowKind Kind, int Count)> kinds, ShadowKind kind)
    {
        if (kinds.Count > 0 && kinds[^1].Kind == kind && kinds.Count <= MaxListed)
        {
            kinds[^1] = (kind, kinds[^1].Count + 1);
        }
        else if (kinds.Count <= MaxListed)
        {
            kinds.Add((kind, 1));
        }
    }

    private static void Said(List<(ShadowKind Kind, int Count)> kinds, IReadOnlyList<Line> lines)
    {
        foreach (var line in lines)
        {
            Said(kinds, KindOf(line.Kind));
        }
    }

    /// <summary>A side was in this state for this long; a stretch of no time is none, and nothing is kept past the line's limit but the sign that there was more.</summary>
    private static void Stretch(List<(FlowState State, long Ms)> states, FlowState state, long ms)
    {
        if (ms > 0 && states.Count <= MaxListed)
        {
            states.Add((state, ms));
        }
    }

    /// <summary>Something of a turn happened: the first such thing opens the turn's count, and both sides' states are timed from it.</summary>
    private void Note(long now)
    {
        if (!_noted)
        {
            _noted = true;
            _startAt = now;
            _windowSince = now;
            _coreSince = now;
            _question = false;
        }

        _lastAt = now;
    }

    /// <summary>One side's working state changed: the time the two stood apart until now is counted, and the side's stretch in the state it leaves is kept.</summary>
    private void SetWorking(bool window, bool working, long now)
    {
        if ((window ? _windowWorking : _coreWorking) == working)
        {
            return;
        }

        if (_windowWorking != _coreWorking)
        {
            _differedMs += now - _differSince;
        }

        Note(now);
        var left = working ? FlowState.Idle : FlowState.Working;
        if (window)
        {
            Stretch(_windowStates, left, now - _windowSince);
            _windowSince = now;
            _windowWorking = working;
        }
        else
        {
            Stretch(_coreStates, left, now - _coreSince);
            _coreSince = now;
            _coreWorking = working;
        }

        if (_windowWorking != _coreWorking)
        {
            _differSince = now;
        }
    }

    private static string Times(List<long> times) =>
        times.Count == 0 ? "none" : string.Join(',', times.Select(time => time.ToString(CultureInfo.InvariantCulture)));
}
