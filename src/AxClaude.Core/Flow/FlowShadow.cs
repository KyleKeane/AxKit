using System.Globalization;

namespace AxClaude.Core.Flow;

/// <summary>What the window's own code did, as the shadow is told it: it said Done, it spoke reply lines, its working state began or ended.</summary>
public enum ShadowFact
{
    SaidDone,
    SpokeLines,
    Working,
    Idle,
}

/// <summary>
/// The comparison of the core with the window's own code while the core runs in shadow (events-and-grounding.md
/// EG-4.11): the window tells it what its old code did (<see cref="Window"/>) and hands it every decision of the
/// core it did not carry out (<see cref="Core"/>), and once for each turn it gives one line of numbers saying where
/// the two differed (<see cref="TurnLine"/>): when each said Done, how many reply lines each would speak, and for
/// how long one said working while the other did not. Numbers and fixed words only: nothing of the conversation is
/// kept, only counts and times (G7).
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

    private const long Never = long.MaxValue;

    private readonly List<long> _windowDone = [];
    private readonly List<long> _coreDone = [];
    private bool _noted;
    private int _turns;
    private long _startAt;
    private long _lastAt;
    private bool _windowWorking;
    private bool _coreWorking;
    private long _differSince;
    private long _differedMs;
    private int _windowLines;
    private int _coreLines;

    /// <summary>What the window's own code did. <paramref name="count"/>: the lines spoken, for <see cref="ShadowFact.SpokeLines"/>.</summary>
    public void Window(ShadowFact fact, int count, long nowMs)
    {
        switch (fact)
        {
            case ShadowFact.SaidDone:
                Note(nowMs);
                _windowDone.Add(nowMs - _startAt);
                break;
            case ShadowFact.SpokeLines:
                if (count > 0)
                {
                    Note(nowMs);
                    _windowLines += count;
                }

                break;
            case ShadowFact.Working:
                SetWorking(window: true, working: true, nowMs);
                break;
            case ShadowFact.Idle:
                SetWorking(window: true, working: false, nowMs);
                break;
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
                break;
            case SpeakReply speak:
                Note(nowMs);
                _coreLines += speak.Lines.Count;
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
    /// <c>none</c>.
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
        _noted = false;
        _windowDone.Clear();
        _coreDone.Clear();
        _windowLines = 0;
        _coreLines = 0;
        _differedMs = 0;
        return line;
    }

    /// <summary>Something of a turn happened: the first such thing opens the turn's count.</summary>
    private void Note(long now)
    {
        if (!_noted)
        {
            _noted = true;
            _startAt = now;
        }

        _lastAt = now;
    }

    /// <summary>One side's working state changed: the time the two stood apart until now is counted.</summary>
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

        if (window)
        {
            _windowWorking = working;
        }
        else
        {
            _coreWorking = working;
        }

        if (_windowWorking != _coreWorking)
        {
            _differSince = now;
        }

        Note(now);
    }

    private static string Times(List<long> times) =>
        times.Count == 0 ? "none" : string.Join(',', times.Select(time => time.ToString(CultureInfo.InvariantCulture)));
}
