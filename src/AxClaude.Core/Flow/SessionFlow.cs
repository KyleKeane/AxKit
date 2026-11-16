namespace AxClaude.Core.Flow;

/// <summary>
/// The core (events-and-grounding.md EG-4): what happened in a session, from every source in arrival order. It
/// takes one plain record and the time (<see cref="Handle"/>) and returns the decisions that lead from it, which
/// the window carries out in order. Pure: no window, no file, no clock, no thread, no timer of its own; when
/// something is due later it asks to be woken (<see cref="WakeAt"/>) and the window sends a <see cref="Tick"/>.
/// It is called on one thread and never from inside itself. No word of the program under the window stands in it:
/// a second program maps its own marks, events and record to the same records (rule 12).
///
/// This file holds the turn, the state, Done and time; <c>SessionFlow.Gate.cs</c> holds the gate of reply speech
/// (EG-5) and <c>SessionFlow.Background.cs</c> the tasks an event listed (EG-7).
///
/// The turn (EG-4.6, measured in section 4.2). It opens at the start mark; in a run that has shown no mark, at a
/// prompt entry of the record; and at the screen's sign of work, whichever speaks first. The end mark closes it
/// and a bell is awaited for <see cref="BellAfterEndMs"/>: with the bell the turn finished and Done is owed
/// (EG-5.6), unless the user's Send now cut it; without the bell it was interrupted, or was a note that no turn
/// stood behind, and nothing is said. A bell with no end mark before it is a question's bell, the window's to
/// settle. Where the marks are missing the record and the screen stand in, and the core says no Done of its own:
/// the window's settle after the bell says it, as before the core.
///
/// What the window must know:
/// - <see cref="Working"/> changes only with a <see cref="StateChanged"/> decision. <see cref="Ending"/> changes
///   without one (at an end mark, at Done, by time): read it after a call, not only when the state changes.
/// - <see cref="WakeAt"/> names the earliest time anything is due and takes the place of the one before; after a
///   tick it is given again while something is still due. A tick that comes early or late does no harm, since
///   every call first does what has come due by its time.
/// - <see cref="RecordFound"/> says that there is a record and <see cref="RecordLost"/> that there is none; after
///   <see cref="FollowRecord"/> for a new conversation (clear, fork) and after <see cref="ForgetRecord"/> there is
///   none until the next <see cref="RecordFound"/>.
/// </summary>
public sealed partial class SessionFlow
{
    /// <summary>How long after the end mark a bell still belongs to it (measured: at most 53).</summary>
    public const int BellAfterEndMs = 150;

    /// <summary>After a turn closed by its marks, the screen's sign of work opens no new one for this long.</summary>
    public const int QuietAfterEndMs = 400;

    /// <summary>A turn opened by the start mark alone is closed when the screen has shown the program idle for this long.</summary>
    public const int IdleContradictsMs = 2000;

    /// <summary>A turn's end this soon after the user's Send now is a cut, not a finish.</summary>
    public const int CutAfterSendNowMs = 2000;

    /// <summary>A failed turn is closed after this long when no marks close it.</summary>
    public const int FailedCloseMs = 1000;

    /// <summary>How long a proposed reply line waits for the record.</summary>
    public const int GateCapMs = 500;

    /// <summary>How long a proposed reply line waits when there is no record.</summary>
    public const int UnconfirmedHoldMs = 250;

    /// <summary>The end counts as confirmed this long after the bell when neither the stop event nor the record confirms it.</summary>
    public const int DoneNoRecordMs = 300;

    /// <summary>Done is said at the latest this long after the bell.</summary>
    public const int DoneCapMs = 600;

    /// <summary>A time that does not come: nothing is due.</summary>
    private const long Never = long.MaxValue;

    /// <summary>A time that has always passed: nothing is held.</summary>
    private const long Past = long.MinValue;

    /// <summary>Where the turn stands: none, open, or closed by its end mark with the bell still awaited.</summary>
    private enum Phase
    {
        None,
        Open,
        Closing,
    }

    /// <summary>What opened the turn, in the order of the evidence (EG-4.5): the start mark, a prompt entry, the screen's sign of work.</summary>
    private enum Opener
    {
        Mark,
        Entry,
        Screen,
    }

    private readonly SourceHealth _health;
    private readonly List<FlowDecision> _decisions = [];
    private long _now;

    // The turn.
    private Phase _phase;
    private Opener _opener;
    private int _turn;
    private long _openedAt;

    /// <summary>An event or an entry of the open turn has come: a start mark with a turn behind it (EG-4.6).</summary>
    private bool _confirmed;

    /// <summary>The stop event or the record's turn end has come for the turn that is open or closing: Done's second condition.</summary>
    private bool _endConfirmed;

    /// <summary>Since when every frame has shown the program idle while the turn is open; <see cref="Never"/> when the last one did not.</summary>
    private long _idleSince = Never;

    private long _endAt;
    private long _bellBy = Never;
    private long _failBy = Never;
    private long _recordEndBy = Never;
    private long _quietUntil = Past;
    private long _noPromptUntil = Past;
    private long _holdUntil = Past;
    private long _lingerUntil = Past;
    private long _cutUntil = Past;

    /// <summary>A turn finished by its marks and the record's turn end for it has not been read: the next one read is that turn's, not the open one's.</summary>
    private bool _endOwed;

    // Done that is owed (EG-5.6): the turn's number, zero for none.
    private int _doneTurn;
    private long _doneBellAt;
    private bool _doneConfirmed;
    private bool _doneLine;

    // The state as last told, the generation, the model's epoch.
    private bool _working;
    private int _generation;
    private bool _epochKnown;
    private int _epoch;

    // The sources alive in this run, and what the screen and the record last said.
    private bool _marksSeen;
    private bool _eventsSeen;
    private bool _record;
    private string? _recordPath;
    private WorkSeen _work;
    private bool _asking;
    private int _queued;
    private int _waitingShown;

    private long _wakeAsked = Never;

    public SessionFlow(SourceHealth health)
    {
        _health = health;
    }

    /// <summary>A turn is open, or a message waits right after one ended (EG-4.6): the state last told.</summary>
    public bool Working => _working;

    /// <summary>
    /// A turn has ended and is not yet over: its bell is awaited, its Done is owed, or, closed by the screen alone,
    /// its last rows may still come. A message sent now goes after that turn's reply (EG-6.4).
    /// </summary>
    public bool Ending => _phase == Phase.Closing || _doneTurn != 0 || _now < _lingerUntil;

    /// <summary>The generation: every action of the user and every opened question raises it (EG-4.7).</summary>
    public int Generation => _generation;

    /// <summary>How many messages wait (EG-6.6): the record's count while there is a record, the screen's hint rows otherwise.</summary>
    public int Waiting => _record ? _queued : _waitingShown;

    /// <summary>
    /// Takes one input and the time, in milliseconds on the window's clock, and returns the decisions it leads to,
    /// in the order to carry them out. The list is reused by the next call: read it at once.
    /// </summary>
    public IReadOnlyList<FlowDecision> Handle(FlowInput input, long nowMs)
    {
        _decisions.Clear();
        _now = nowMs;
        Due(nowMs);
        switch (input)
        {
            case ProcessStarted:
            case ProcessEnded:
                NewRun();
                break;
            case MarkSeen mark:
                Mark(mark.Mark, nowMs);
                break;
            case BellSeen:
                Bell(nowMs);
                break;
            case FrameSeen frame:
                Frame(frame, nowMs);
                break;
            case UserActed acted:
                Acted(acted, nowMs);
                break;
            case QuestionShown:
                Bump();
                break;
            case SessionNamed named:
                Named(named);
                break;
            case TurnStopped stop:
                Stopped(stop, nowMs);
                break;
            case TurnFailed:
                Failed(nowMs);
                break;
            case WorkerStarted worker:
                _eventsSeen = true;
                _confirmed = true;
                BackgroundWorker(worker.Id, worker.Type, started: true);
                break;
            case WorkerStopped worker:
                _eventsSeen = true;
                BackgroundWorker(worker.Id, worker.Type, started: false);
                break;
            case AttentionAsked asked:
                Asked(asked);
                break;
            case SessionEnded:
                // The process's end follows and closes everything; the event itself only shows that events arrive.
                _eventsSeen = true;
                break;
            case EntryRead entry:
                Entry(entry, nowMs);
                break;
            case RecordFound:
                _record = true;
                break;
            case RecordLost:
                _record = false;
                if (_phase == Phase.Open && _opener == Opener.Entry)
                {
                    // The record opened this turn and will not show its end: the screen is what is left to close it.
                    _opener = Opener.Screen;
                }

                break;
            case Tick:
                // What came due was done above.
                break;
        }

        TryDone(nowMs);
        TellState();
        AskWake(input is Tick, nowMs);
        return _decisions;
    }

    /// <summary>Adds a decision to what this call returns; the gate and the background list decide through it too.</summary>
    private void Decide(FlowDecision decision) => _decisions.Add(decision);

    /// <summary>A proposed line is considered only here (EG-5.2): a turn is open or ending.</summary>
    private bool InTurn => _phase != Phase.None || Ending;

    /// <summary>There is a record: the gate waits for it to confirm a reply line (EG-5.3); without one the gate is open (EG-5.4).</summary>
    private bool HasRecord => _record;

    // ---- Time (EG-4.10) ----

    /// <summary>What has come due by now, before the input is looked at: so a tick may come late, and an input that comes after a due time finds it done.</summary>
    private void Due(long now)
    {
        if (_phase == Phase.Closing && now >= _bellBy)
        {
            EndWithoutBell();
        }

        if (_phase == Phase.Open)
        {
            if (_opener == Opener.Mark && !_confirmed && _idleSince != Never && now - _idleSince >= IdleContradictsMs)
            {
                // A start mark can come with no turn behind it (4.2): the screen has shown the idle prompt ever
                // since, and neither an event nor an entry of a turn came.
                _health.TurnsStale++;
                Decide(new Note(_generation, $"stale-turn after={now - _openedAt}"));
                Close(now);
                // It was no turn, so it takes no number; an end mark that still comes gets it back.
                _turn--;
            }
            else if (now >= _failBy)
            {
                Decide(new Note(_generation, $"failed-turn after={now - _openedAt}"));
                Close(now);
            }
            else if (now >= _recordEndBy)
            {
                // The record showed the turn's end and no end mark came: a missed mark, or a run without marks.
                // Nothing is said: with no mark in the run the window's settle says Done after the bell. A sign of
                // work the screen still shows opens nothing at once.
                Decide(new Note(_generation, $"record-end after={now - _openedAt}"));
                Close(now);
                _quietUntil = now + QuietAfterEndMs;
            }
        }

        GateTick(now);
        if (_doneTurn != 0 && now - _doneBellAt >= DoneCapMs)
        {
            // Done's cap has passed: it is said before anything the input itself leads to.
            SayDone(now);
        }
    }

    /// <summary>The earliest time anything is due, asked for when it changed, and again after a tick.</summary>
    private void AskWake(bool ticked, long now)
    {
        var due = NextDue(now);
        if (due != Never && (due != _wakeAsked || ticked))
        {
            Decide(new WakeAt(_generation, due));
        }

        _wakeAsked = due;
    }

    private long NextDue(long now)
    {
        var due = Never;
        if (_phase == Phase.Closing)
        {
            due = Math.Min(due, _bellBy);
        }

        if (_phase == Phase.Open)
        {
            if (_opener == Opener.Mark && !_confirmed && _idleSince != Never)
            {
                due = Math.Min(due, _idleSince + IdleContradictsMs);
            }

            due = Math.Min(due, Math.Min(_failBy, _recordEndBy));
        }

        if (now < _holdUntil)
        {
            due = Math.Min(due, _holdUntil);
        }

        if (now < _lingerUntil)
        {
            due = Math.Min(due, _lingerUntil);
        }

        if (_doneTurn != 0)
        {
            var confirmedBy = _doneBellAt + DoneNoRecordMs;
            due = Math.Min(due, !_doneConfirmed && now < confirmedBy ? confirmedBy : _doneBellAt + DoneCapMs);
        }

        return Math.Min(due, GateDue);
    }

    // ---- The state (EG-6.1) ----

    /// <summary>
    /// Says the state when it changed. Working: a turn is open, or one just ended while a message waits, for the
    /// moment until the start mark of the waiting message's turn (EG-4.6; measured 45 to 53 ms after the bell).
    /// </summary>
    private void TellState()
    {
        var working = _phase == Phase.Open || _now < _holdUntil;
        if (working != _working)
        {
            _working = working;
            Decide(new StateChanged(_generation, working ? FlowState.Working : FlowState.Idle));
        }
    }

    // ---- The turn (EG-4.6) ----

    private void Open(Opener opener, long now)
    {
        _phase = Phase.Open;
        _opener = opener;
        _turn++;
        _openedAt = now;
        _confirmed = opener == Opener.Entry;
        _endConfirmed = false;
        // The screen showed the idle prompt before the mark and shows it still, until a frame says otherwise.
        _idleSince = opener == Opener.Mark && _work == WorkSeen.Idle && !_asking ? now : Never;
        _failBy = Never;
        _recordEndBy = Never;
        _holdUntil = Past;
        TellState();
    }

    /// <summary>Closes the open turn by something other than its end mark: nothing is said, and no Done is owed.</summary>
    private void Close(long now)
    {
        _phase = Phase.None;
        _idleSince = Never;
        _failBy = Never;
        _recordEndBy = Never;
        _noPromptUntil = now + BellAfterEndMs;
        TellState();
    }

    private void Mark(TurnMark mark, long now)
    {
        _marksSeen = true;
        switch (mark)
        {
            case TurnMark.Start:
                _health.MarksA++;
                if (_phase == Phase.Closing)
                {
                    // The next turn starts before the bell's time is over: the end before it had no bell.
                    EndWithoutBell();
                }

                if (_phase == Phase.Open)
                {
                    // The record or the screen was first; from here the marks speak for this turn.
                    _opener = Opener.Mark;
                }
                else
                {
                    Open(Opener.Mark, now);
                }

                break;
            case TurnMark.Output:
                _health.MarksC++;
                break;
            case TurnMark.End:
                _health.MarksD++;
                End(now);
                break;
        }
    }

    /// <summary>The end mark: the turn is closed, and its bell is awaited.</summary>
    private void End(long now)
    {
        if (_phase == Phase.None)
        {
            // An end whose start the core did not see as a turn (closed as stale before, or never marked): a turn
            // all the same, so that its bell gives Done.
            _turn++;
            _openedAt = now;
        }

        _phase = Phase.Closing;
        _endAt = now;
        _bellBy = now + BellAfterEndMs;
        _idleSince = Never;
        _failBy = Never;
        _recordEndBy = Never;
        _lingerUntil = Past;
        _noPromptUntil = now + BellAfterEndMs;
        _quietUntil = now + QuietAfterEndMs;
        // A message waits, by the record or by the screen's hint rows: its turn's start mark follows at once, and
        // the state is not idle in between.
        _holdUntil = (_record && _queued > 0) || _waitingShown > 0 ? now + BellAfterEndMs : Past;
        TellState();
    }

    /// <summary>The end mark had no bell: an interrupt, or a note that no turn stood behind (4.2). Nothing is said.</summary>
    private void EndWithoutBell()
    {
        _phase = Phase.None;
        _bellBy = Never;
        _endConfirmed = false;
        if (_endAt - _openedAt < BellAfterEndMs)
        {
            // Start and end at once: a hand-back note. No turn was open for longer than the note, so it is none.
            _turn--;
        }
        else
        {
            _health.TurnsInterrupted++;
        }
    }

    private void Bell(long now)
    {
        _health.Bells++;
        if (_phase != Phase.Closing)
        {
            // A bell with no end mark in the time before it: a question from a tool, and the turn stays as it is.
            // With no mark in this run the bell is all there is of an end, and the window's settle decides.
            Decide(new AttentionBell(_generation, !_marksSeen));
            return;
        }

        var confirmed = _endConfirmed;
        _phase = Phase.None;
        _bellBy = Never;
        _endConfirmed = false;
        if (now < _cutUntil)
        {
            // The user's Send now cut the running reply for the waiting message: an end he asked for, not a
            // finish (EG-D-14). The next start mark opens the waiting message's turn.
            _cutUntil = Past;
            _health.TurnsCut++;
            return;
        }

        _health.TurnsFinished++;
        _endOwed = true;
        if (_doneTurn != 0)
        {
            // A Done still owed for the turn before: one for each turn that finished, in their order.
            SayDone(now);
        }

        _doneTurn = _turn;
        _doneBellAt = now;
        _doneConfirmed = confirmed;
        _doneLine = false;
    }

    private void Frame(FrameSeen frame, long now)
    {
        if (!_epochKnown)
        {
            _epochKnown = true;
            _epoch = frame.Epoch;
        }
        else if (frame.Epoch != _epoch)
        {
            // The conversation started afresh under the window (EG-4.9).
            _epoch = frame.Epoch;
            Afresh();
            if (_record && !_eventsSeen)
            {
                // Without events nothing names the new conversation's file: the one followed is the old
                // conversation's and would confirm nothing more, so the window looks again.
                ForgetTheRecord();
            }
        }

        _work = frame.Work;
        _asking = frame.Asking;
        _waitingShown = frame.WaitingShown;
        // A screen that asks is not idle: a question stands inside its turn.
        var idle = frame.Work == WorkSeen.Idle && !frame.Asking;
        if (_phase == Phase.Open)
        {
            if (!idle)
            {
                _idleSince = Never;
            }
            else if (_opener == Opener.Screen)
            {
                // Opened by the screen alone, closed by the screen: the bell gives Done through the window's
                // settle, and the turn stays ending for its last rows.
                Close(now);
                _lingerUntil = now + DoneCapMs;
            }
            else if (_idleSince == Never)
            {
                _idleSince = now;
            }
        }
        else if (_phase == Phase.None && frame.Work == WorkSeen.Working && now >= _quietUntil)
        {
            Open(Opener.Screen, now);
        }

        if (_doneTurn != 0 && frame.LineAdded)
        {
            // A frame after the bell added a visible line: the last reply's rows, or the turn summary row.
            _doneLine = true;
        }

        GateFrame(frame, now);
    }

    private void Entry(EntryRead entry, long now)
    {
        switch (entry.Kind)
        {
            case EntryKind.Prompt:
                if (_phase == Phase.Open)
                {
                    _confirmed = true;
                }
                else if (_phase == Phase.None && now >= _noPromptUntil && !_marksSeen)
                {
                    // The record stands in for the start mark only in a run that has shown no mark. Where the
                    // marks speak, every turn has its start mark before its prompt is in the file, and a prompt
                    // read with no turn open is a turn's that has ended, or a note's for the record alone
                    // (measured: the hand-back note's entry 159 after the note's end mark): a turn opened for it
                    // would have nothing to close it.
                    Open(Opener.Entry, now);
                }

                break;
            case EntryKind.ReplyText when entry.Cut:
            case EntryKind.Interrupt:
                if (_phase == Phase.Open && !_marksSeen)
                {
                    // The record stands in for the end mark without its bell, in a run that has shown no mark:
                    // cut, nothing said. Where the marks speak they decide: the cut entry of a reply that Send now
                    // cut is read after the start mark of the waiting message's turn, which it must not close.
                    _health.TurnsInterrupted++;
                    Decide(new Note(_generation, $"record-cut after={now - _openedAt}"));
                    Close(now);
                }

                break;
            case EntryKind.ToolCall:
            case EntryKind.ToolResult:
                // Entries of a running turn. A reply's text is not taken for one: the turn before's last reply is
                // read after the next start mark.
                _confirmed = true;
                break;
            case EntryKind.TurnEnd:
                RecordEnd(now);
                break;
            case EntryKind.Enqueued:
                _queued++;
                _confirmed = true;
                break;
            case EntryKind.Dequeued:
                _queued = Math.Max(0, _queued - 1);
                break;
        }

        GateEntry(entry, now);
    }

    /// <summary>The record's turn end: it confirms the end the marks gave, or stands in for a mark that was missed.</summary>
    private void RecordEnd(long now)
    {
        if (_endOwed)
        {
            // It belongs to the turn that finished by its marks: the record writes it after that turn's bell,
            // often after the start mark of a waiting message's turn, which it must not close.
            _endOwed = false;
            if (_doneTurn != 0)
            {
                _doneConfirmed = true;
            }

            return;
        }

        if (_phase == Phase.Open)
        {
            _endConfirmed = true;
            _recordEndBy = now + BellAfterEndMs;
        }
        else if (_phase == Phase.Closing)
        {
            _endConfirmed = true;
        }
    }

    private void Stopped(TurnStopped stop, long now)
    {
        _eventsSeen = true;
        if (_doneTurn != 0 && !_doneConfirmed)
        {
            // The stop event of the turn whose Done is owed, late.
            _doneConfirmed = true;
        }
        else
        {
            // It comes before its turn's end mark (measured 12 to 130 ms).
            _endConfirmed = true;
            _confirmed = true;
        }

        GateStop(stop, now);
        BackgroundStop(stop);
    }

    private void Failed(long now)
    {
        _eventsSeen = true;
        if (_phase == Phase.Open)
        {
            _confirmed = true;
            _failBy = now + FailedCloseMs;
        }
    }

    // ---- Done (EG-5.6) ----

    /// <summary>
    /// Done for a turn that closed with its bell, at the first moment when no proposed line is pending in the gate,
    /// the end is confirmed (the stop event, the record's turn end, or <see cref="DoneNoRecordMs"/> after the bell)
    /// and a frame after the bell added a visible line; or <see cref="DoneCapMs"/> after the bell, which
    /// <see cref="Due"/> sees to. It is tried last in a call, so lines released by the same input are spoken before
    /// it. No generation drops it (EG-5.7).
    /// </summary>
    private void TryDone(long now)
    {
        if (_doneTurn == 0)
        {
            return;
        }

        var confirmed = _doneConfirmed || now - _doneBellAt >= DoneNoRecordMs;
        if (!GatePending && confirmed && _doneLine)
        {
            SayDone(now);
        }
    }

    private void SayDone(long now)
    {
        Decide(new SayDone(_generation, _doneTurn, now - _doneBellAt));
        _doneTurn = 0;
    }

    // ---- Generations (EG-4.7) ----

    /// <summary>The user has moved on: what the gate holds under the older generation is dropped (EG-5.7).</summary>
    private void Bump()
    {
        _generation++;
        GateBump();
    }

    private void Acted(UserActed acted, long now)
    {
        if (acted.Action == UserAction.SentNow && ((_record && _queued > 0) || _waitingShown > 0))
        {
            // Only with a message waiting does Send now cut the reply; pressed with none it does nothing (measured),
            // and the turn's own end is a finish.
            _cutUntil = now + CutAfterSendNowMs;
        }

        Bump();
    }

    private void Asked(AttentionAsked asked)
    {
        _eventsSeen = true;
        if (asked.Kind == AttentionKind.Permission)
        {
            // A question is coming (measured 9 to 13 ms before its bell): speech that waited would cut into it.
            _confirmed = true;
            Bump();
        }
    }

    // ---- Starting afresh (EG-4.9) ----

    /// <summary>Forgets the turn without a word: no Done, no note. The caller tells the state.</summary>
    private void ForgetTurn()
    {
        _phase = Phase.None;
        _idleSince = Never;
        _bellBy = Never;
        _failBy = Never;
        _recordEndBy = Never;
        _quietUntil = Past;
        _noPromptUntil = Past;
        _holdUntil = Past;
        _lingerUntil = Past;
        _cutUntil = Past;
        _confirmed = false;
        _endConfirmed = false;
        _endOwed = false;
        _doneTurn = 0;
    }

    /// <summary>The conversation started afresh in the same run: the turn, the gate, the listed tasks and the count go; the generation keeps rising.</summary>
    private void Afresh()
    {
        ForgetTurn();
        TellState();
        _queued = 0;
        GateReset();
        BackgroundReset();
        Bump();
    }

    /// <summary>The process started or ended: everything of a run is over, a Done not yet said with it, and nothing is known of the next.</summary>
    private void NewRun()
    {
        Afresh();
        _turn = 0;
        _epochKnown = false;
        _marksSeen = false;
        _eventsSeen = false;
        _record = false;
        _recordPath = null;
        _work = WorkSeen.Unknown;
        _asking = false;
        _waitingShown = 0;
    }

    private void Named(SessionNamed named)
    {
        _eventsSeen = true;
        var fresh = named.Source is SessionSource.Clear or SessionSource.Fork;
        if (fresh)
        {
            Afresh();
        }

        if (named.RecordPath is { } path)
        {
            if (!string.Equals(path, _recordPath, StringComparison.OrdinalIgnoreCase))
            {
                _recordPath = path;
                if (fresh)
                {
                    // The file followed until now is the old conversation's.
                    _record = false;
                }

                Decide(new FollowRecord(_generation, path));
            }
        }
        else if (fresh && (_record || _recordPath is not null))
        {
            ForgetTheRecord();
        }
    }

    private void ForgetTheRecord()
    {
        _record = false;
        _recordPath = null;
        Decide(new ForgetRecord(_generation));
    }
}
