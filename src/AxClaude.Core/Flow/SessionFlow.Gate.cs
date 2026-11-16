using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>
/// The gate of reply speech (events-and-grounding.md EG-5): it holds each reply line the screen proposes until the
/// record shows the text the line belongs to, and releases it once. So a repaint, a replay, the program's own
/// summary of an earlier reply and a question's rows under a reply are never spoken as replies: no entry of the
/// record confirms them.
///
/// The rules, in the order a line meets them:
/// - Outside a turn a proposed line is dropped (EG-5.2). A line still held when its turn is over is dropped too:
///   nothing is released after a turn's Done.
/// - An error row, the rows under it and a tool row are released at once (EG-5.5): they stand in no reply's entry.
/// - Without a record the gate is open: a line is released <see cref="UnconfirmedHoldMs"/> after it was proposed
///   (EG-5.4).
/// - With a record, a head (the first row of a reply) is confirmed by the first reply of the record, in the
///   record's order, that no head has taken and whose key starts with the head's own letters (EG-5.3). The reply
///   is then taken, and the entry is reported bound to the head's line (EG-8.1). A row under a confirmed head is
///   released when its letters follow in that reply's key; a row under a head that is not confirmed waits with it.
/// - A line the record has not confirmed <see cref="GateCapMs"/> after it was proposed is dropped (EG-5.4).
/// - A new generation drops every held line (EG-5.7). With reply speech off nothing is held.
///
/// The replies of the record. Each reply text read is one, a candidate for a head until a head takes it or a
/// prompt is read after it: an entry of an earlier turn is never a candidate. A prompt closes the replies before
/// it only once the next reply text has been read or <see cref="GateCapMs"/> have passed: the record can be ahead
/// of the screen (measured: by 65 ms), and when a waiting message is taken up the last reply's entry and the next
/// prompt are read together, before that reply's rows are drawn. A stop event's last message makes a reply of its
/// own at once, when the record does not hold that text yet (EG-D-11); the entry with the same key then takes its
/// place and keeps what was matched, and the binding is reported then, since only the entry has an id. Replies are
/// kept only while there is a record, and entries and stop events are taken only then.
///
/// Every pending line is tried after each proposal, each reply text and each stop event, in the order the lines
/// were proposed, and by time at the start of every input (<see cref="GateTick"/>). What one input releases goes
/// out as one decision, in that order, before the bindings it led to. Every proposal ends in exactly one number
/// of the health (EG-5.9): released, outside a turn, unconfirmed, stale, or pending now.
///
/// Done (EG-5.6, its first condition) waits while a line is pending, and while a reply is half drawn: rows of it
/// were released and its key goes on behind them. The last row of a long reply can come in a write of its own,
/// shown 100 ms after the others (measured), when the turn's end is already confirmed; said then, Done would
/// close the turn's window in front of that row. The rows are given up <see cref="GateCapMs"/> after the last one.
/// Done waits a third time while the record holds a reply whose head the screen has not proposed yet, for
/// <see cref="GateCapMs"/> after the record first showed it (<see cref="GateReplyAwaitedUntil"/>): the entry or the
/// stop event can come before the reply's own row is proposed.
///
/// What <c>SessionFlow.cs</c> gives: <c>InTurn</c>, <c>HasRecord</c>, the generation, the time of the input
/// (<c>_now</c>), whether a Done is owed (<c>_doneTurn</c>), <c>Decide</c> and the list it fills, and the calls of
/// the methods below. Every member of this
/// file is named Gate or _gate: the class is shared with two other files.
/// </summary>
public sealed partial class SessionFlow
{
    /// <summary>A head is compared with a reply's key by its first letters, this many, or by all of them when it has fewer (EG-5.3).</summary>
    private const int GateHeadLetters = 24;

    /// <summary>A row's letters are looked for no further than this behind the place matched so far, unless the row is a long one.</summary>
    private const int GateRowReach = 80;

    /// <summary>A row of this many letters or more is found however far on: so many letters stand in no reply by chance.</summary>
    private const int GateLongRow = 16;

    /// <summary>How many replies are kept at most: in one long turn no prompt takes the old ones away, and a window lives for days.</summary>
    private const int GateRepliesKept = 256;

    /// <summary>One reply as the record holds it: a reply text entry, or a stop event's last message until its entry arrives.</summary>
    private sealed class GateReply
    {
        public GateReply(string id, string key, bool provisional, long at)
        {
            Id = id;
            Key = key;
            Provisional = provisional;
            At = at;
        }

        /// <summary>When the record first showed it: its entry was read, or the stop event's last message stood in for it.</summary>
        public readonly long At;

        /// <summary>The entry's id; empty while only a stop event's last message stands for the entry.</summary>
        public string Id;

        /// <summary>The reply's letters as the screen draws them (EG-3.7).</summary>
        public readonly string Key;

        /// <summary>Made of a stop event's last message: the entry with the same key takes its place.</summary>
        public bool Provisional;

        /// <summary>How far into the key the screen's lines have been matched.</summary>
        public int Matched;

        /// <summary>A head has taken it, the line of <see cref="HeadId"/>: a reply releases once.</summary>
        public bool Taken;
        public int HeadId;

        /// <summary>From this time on it is no candidate for a head: a prompt was read after it. <see cref="Never"/> while none was.</summary>
        public long ClosedAt = Never;
    }

    /// <summary>One proposed line that waits: the line, its head, what it is, and when it was proposed.</summary>
    private sealed class GateLine
    {
        private string? _lettersOf;
        private string _letters = "";

        public GateLine(Proposed proposed, long at)
        {
            Line = proposed.Line;
            HeadId = proposed.HeadId;
            Kind = proposed.Kind;
            At = at;
        }

        public readonly Line Line;
        public readonly int HeadId;
        public readonly ProposedKind Kind;
        public readonly long At;

        /// <summary>
        /// The line's letters, a head's without its label: of the text the line has now, which is the text the
        /// window would speak, since a row can be rewritten while it waits.
        /// </summary>
        public string Letters
        {
            get
            {
                var text = Line.Text;
                if (!ReferenceEquals(text, _lettersOf))
                {
                    _lettersOf = text;
                    var label = ClaudeCodeTexts.ClaudeLabel;
                    var labelled = Kind == ProposedKind.Head && text.StartsWith(label, StringComparison.Ordinal);
                    _letters = RecordText.Letters(labelled ? text.AsSpan(label.Length) : text);
                }

                return _letters;
            }
        }
    }

    /// <summary>The replies of the record, in the record's order: those read since the prompt before the last one, <see cref="GateRepliesKept"/> at most.</summary>
    private readonly List<GateReply> _gateReplies = [];

    /// <summary>The confirmed heads by line id, each with the reply that confirmed it: the rows under it are matched against that reply's key.</summary>
    private readonly Dictionary<int, GateReply> _gateHeads = [];

    /// <summary>The pending lines, in the order they were proposed.</summary>
    private readonly List<GateLine> _gateLines = [];

    /// <summary>The replies whose binding one pass over the pending lines found, reported after the lines it released.</summary>
    private readonly List<GateReply> _gateBound = [];

    /// <summary>The lines this input has released so far, to which a later release of the same input is added; none when an input begins.</summary>
    private List<Line>? _gateSaid;

    /// <summary>
    /// The reply whose rows were released last, and when: while the rows have not reached the end of its key, the
    /// reply is still being drawn. Measured (the long-reply recording): a reply's last row came in a write of its
    /// own, shown 100 ms after the rows before it, when the record's turn end had already been read.
    /// </summary>
    private GateReply? _gateDrawn;
    private long _gateDrawnAt;

    /// <summary>
    /// Rows of a reply are still to come: some were released, the reply's key goes on behind them, and the last of
    /// them is less than <see cref="GateCapMs"/> old. Never for a reply of which only the head is spoken.
    /// </summary>
    private bool GateRowsAwaited => _gateDrawn is { } reply && reply.Matched < reply.Key.Length && _now - _gateDrawnAt < GateCapMs;

    /// <summary>Whether the last frame said that replies are spoken: only then does the screen propose a reply's head.</summary>
    private bool _gateSpeechOn;

    /// <summary>When the generation last rose: the head of a reply the record showed by then is awaited no more, as the rows of one are not.</summary>
    private long _gateBumpedAt = Past;

    /// <summary>
    /// Until when the record holds a reply whose head the screen has not proposed yet: a reply with letters that no
    /// head has taken and no prompt has closed, for <see cref="GateCapMs"/> after the record first showed it;
    /// <see cref="Past"/> when there is none, or replies are not spoken. Its rows are still to be drawn, or are drawn
    /// and not yet final. Measured (the timelines): the record's entry, or the stop event, came before the reply's
    /// own row was proposed, and the frame between them added another line, the turn summary row (the compact
    /// recording, second turn) or the reply's row while the screen still showed work (the tool question's turn);
    /// Done was said on that frame and the reply was dropped behind it, unspoken. A reply the record showed before
    /// the user moved on is not waited for: what he has moved on from holds nothing back (EG-5.7).
    /// </summary>
    private long GateReplyAwaitedUntil
    {
        get
        {
            if (!_gateSpeechOn || !HasRecord)
            {
                return Past;
            }

            var until = Past;
            foreach (var reply in _gateReplies)
            {
                if (!reply.Taken && reply.Key.Length > 0 && reply.At > _gateBumpedAt)
                {
                    var open = Math.Min(reply.At + GateCapMs, reply.ClosedAt);
                    if (_now < open)
                    {
                        until = until == Past ? open : Math.Min(until, open);
                    }
                }
            }

            return until;
        }
    }

    /// <summary>
    /// A proposed line waits for the record, a reply is half drawn, or the record holds a reply whose head the
    /// screen has not proposed yet: Done waits for all three (EG-5.6, the first condition), so that the last
    /// reply's rows are spoken before Done and not dropped behind it.
    /// </summary>
    private bool GatePending => _gateLines.Count > 0 || GateRowsAwaited || GateReplyAwaitedUntil != Past;

    /// <summary>
    /// The earliest time the gate is due, for the core's one wake time; <see cref="Never"/> with nothing due. The
    /// pending lines stand in the order of their times, so the first one is the earliest: its cap with a record,
    /// the end of its hold without one. Rows awaited are given up <see cref="GateCapMs"/> after the last one. A
    /// reply whose head is awaited is due only while a Done waits for it: nothing else minds it.
    /// </summary>
    private long GateDue
    {
        get
        {
            var due = _gateLines.Count == 0 ? Never : _gateLines[0].At + (HasRecord ? GateCapMs : UnconfirmedHoldMs);
            if (GateRowsAwaited)
            {
                due = Math.Min(due, _gateDrawnAt + GateCapMs);
            }

            if (_doneTurn != 0 && GateReplyAwaitedUntil is var until && until != Past)
            {
                due = Math.Min(due, until);
            }

            return due;
        }
    }

    /// <summary>A frame's proposals: each is held, and all that is pending is tried; outside a turn they are dropped.</summary>
    private void GateFrame(FrameSeen frame, long now)
    {
        _gateSpeechOn = frame.SpeechOn;
        if (!frame.SpeechOn && _gateLines.Count > 0)
        {
            // Replies are no longer spoken: what waited to be spoken is not wanted.
            _health.GateStale += GateEmpty();
        }

        var proposed = frame.Proposed;
        if (proposed.Count == 0)
        {
            return;
        }

        if (!InTurn)
        {
            _health.GateOutsideTurn += proposed.Count;
            return;
        }

        // Every proposal joins the pending lines, also the ones released at once: the pass below then releases
        // what it may in the order proposed, which within a frame is the conversation's. The frame's own list is
        // reused by the next pass over the conversation, so nothing of it is kept.
        for (var i = 0; i < proposed.Count; i++)
        {
            var line = proposed[i];
            if (line.Kind is ProposedKind.Head or ProposedKind.Row)
            {
                if (!frame.SpeechOn)
                {
                    // With reply speech off nothing is pending; a reply line proposed all the same is not spoken.
                    _health.GateStale++;
                    continue;
                }

                GateSupersede(line.Line);
                if (line.Kind == ProposedKind.Head)
                {
                    // Proposed again, the row holds another text: a head of its own, which the reply that
                    // confirmed the text before does not confirm.
                    _gateHeads.Remove(line.Line.Id);
                }
            }

            _gateLines.Add(new GateLine(line, now));
        }

        GateTry(now);
    }

    /// <summary>An entry of the record was read: a reply's text confirms the lines that wait for it, a prompt closes the replies before it.</summary>
    private void GateEntry(EntryRead entry, long now)
    {
        if (!HasRecord)
        {
            return;
        }

        if (entry.Kind == EntryKind.Prompt)
        {
            GatePrompt(now);
        }
        else if (entry.Kind == EntryKind.ReplyText)
        {
            GateReplyRead(entry, now);
        }
    }

    /// <summary>
    /// A turn stopped: its last message stands in for the record's entry until that arrives (EG-D-11). Not when
    /// the record already holds a reply of this text: then the event came after its entry, as it does when the
    /// program does not wait for its hooks, and a second reply would confirm a repaint of the first.
    /// </summary>
    private void GateStop(TurnStopped stop, long now)
    {
        if (!HasRecord || stop.LastReplyKey is not { } key)
        {
            return;
        }

        foreach (var reply in _gateReplies)
        {
            if (reply.Key == key)
            {
                return;
            }
        }

        GateKeep(new GateReply("", key, provisional: true, now));
        GateTry(now);
    }

    /// <summary>
    /// Time has passed: a line that waited too long is dropped, or released when there is no record (EG-5.4), and
    /// a line whose turn is over is dropped. Without a record no reply is kept: the record that comes next starts
    /// with what it reads from then on.
    /// </summary>
    private void GateTick(long now)
    {
        // An input begins: what the one before released was its own decision.
        _gateSaid = null;
        if (!HasRecord && (_gateReplies.Count > 0 || _gateHeads.Count > 0))
        {
            _gateReplies.Clear();
            _gateHeads.Clear();
            _gateDrawn = null;
        }

        if (_gateDrawn is not null && !(InTurn && GateRowsAwaited))
        {
            // The reply is drawn to its end, its rows were given up, or its turn is over.
            _gateDrawn = null;
        }

        GateTry(now);
    }

    /// <summary>The generation rose: every pending line is dropped, unspoken and counted (EG-5.7), and no row is awaited.</summary>
    private void GateBump()
    {
        _health.GateStale += GateEmpty();
        _gateDrawn = null;
        _gateBumpedAt = _now;
    }

    /// <summary>The conversation or the run started afresh: the pending lines, the heads and the reply records go (EG-4.9).</summary>
    private void GateReset()
    {
        _health.GateStale += GateEmpty();
        _gateReplies.Clear();
        _gateHeads.Clear();
        _gateDrawn = null;
        _gateSaid = null;
        _gateSpeechOn = false;
    }

    /// <summary>Drops every pending line and says how many there were, for the caller's number.</summary>
    private int GateEmpty()
    {
        var count = _gateLines.Count;
        _gateLines.Clear();
        _health.GatePending = 0;
        return count;
    }

    /// <summary>
    /// A line proposed again while it still waits (its row was rewritten) takes the place of its earlier proposal,
    /// whose text nothing confirmed: a line waits once, and is never released twice from one frame's pass.
    /// </summary>
    private void GateSupersede(Line line)
    {
        for (var i = 0; i < _gateLines.Count; i++)
        {
            if (ReferenceEquals(_gateLines[i].Line, line))
            {
                _gateLines.RemoveAt(i);
                _health.GateUnconfirmed++;
                return;
            }
        }
    }

    /// <summary>
    /// A prompt was read: the replies before it belong to turns that are over. They stay candidates for
    /// <see cref="GateCapMs"/> more, or until the next reply text is read (the record can be ahead of the screen),
    /// and they stay in the list until the prompt after this one, so that a stop event that comes late still finds
    /// its reply there and makes no second one.
    /// </summary>
    private void GatePrompt(long now)
    {
        var kept = 0;
        for (var i = 0; i < _gateReplies.Count; i++)
        {
            var reply = _gateReplies[i];
            if (now >= reply.ClosedAt)
            {
                // Closed by an earlier prompt: its turn is two prompts back, and no row of it is still to come.
                GateLetGo(reply);
                continue;
            }

            if (reply.ClosedAt == Never)
            {
                reply.ClosedAt = now + GateCapMs;
            }

            _gateReplies[kept++] = reply;
        }

        _gateReplies.RemoveRange(kept, _gateReplies.Count - kept);
    }

    /// <summary>A reply text was read: it takes the place of the stop event's stand-in for it, or is a reply of its own.</summary>
    private void GateReplyRead(EntryRead entry, long now)
    {
        var key = entry.Key ?? "";
        GateReply? standIn = null;
        foreach (var reply in _gateReplies)
        {
            if (reply.ClosedAt != Never)
            {
                // A prompt was read after this reply, and entries come in the record's order: the text read now
                // is the next turn's first, and the replies before that prompt are no candidates from here on.
                reply.ClosedAt = Math.Min(reply.ClosedAt, now);
            }
            else if (standIn is null && reply.Provisional && reply.Key == key)
            {
                standIn = reply;
            }
        }

        if (standIn is null)
        {
            GateKeep(new GateReply(entry.Id, key, provisional: false, now));
        }
        else
        {
            // The entry takes the stand-in's place and keeps what was matched. A head that took the stand-in is
            // bound to the entry now, unless its row has been proposed again since with another text.
            standIn.Id = entry.Id;
            standIn.Provisional = false;
            if (standIn.Taken && _gateHeads.TryGetValue(standIn.HeadId, out var held) && ReferenceEquals(held, standIn))
            {
                Decide(new EntryBound(_generation, standIn.Id, standIn.HeadId));
            }
        }

        GateTry(now);
    }

    /// <summary>Adds a reply behind the others; beyond <see cref="GateRepliesKept"/> the oldest goes.</summary>
    private void GateKeep(GateReply reply)
    {
        _gateReplies.Add(reply);
        if (_gateReplies.Count > GateRepliesKept)
        {
            GateLetGo(_gateReplies[0]);
            _gateReplies.RemoveAt(0);
        }
    }

    /// <summary>A reply leaves the list: the head it confirmed is no confirmed head any more, and a row under it finds nothing.</summary>
    private void GateLetGo(GateReply reply)
    {
        if (reply.Taken && _gateHeads.TryGetValue(reply.HeadId, out var held) && ReferenceEquals(held, reply))
        {
            _gateHeads.Remove(reply.HeadId);
        }

        if (ReferenceEquals(_gateDrawn, reply))
        {
            _gateDrawn = null;
        }
    }

    /// <summary>
    /// Tries every pending line, in the order proposed: released, dropped, or left to wait. The released lines go
    /// out as one decision, then the bindings the pass found.
    /// </summary>
    private void GateTry(long now)
    {
        if (_gateLines.Count == 0)
        {
            return;
        }

        var inTurn = InTurn;
        var record = HasRecord;
        List<Line>? released = null;
        var kept = 0;
        for (var i = 0; i < _gateLines.Count; i++)
        {
            var line = _gateLines[i];
            if (!inTurn)
            {
                // Its turn is over (Done was said at its cap, the turn was interrupted): nothing is spoken after it.
                _health.GateOutsideTurn++;
            }
            else if (GateFree(line, record, now))
            {
                // A list of the decision's own.
                (released ??= []).Add(line.Line);
            }
            else if (record && now - line.At >= GateCapMs)
            {
                // No entry confirmed it in its time: a repaint, a replay, a summary, a question's row (EG-5.4).
                _health.GateUnconfirmed++;
            }
            else
            {
                _gateLines[kept++] = line;
            }
        }

        _gateLines.RemoveRange(kept, _gateLines.Count - kept);
        _health.GatePending = kept;
        if (released is not null)
        {
            GateSay(released);
        }

        foreach (var reply in _gateBound)
        {
            Decide(new EntryBound(_generation, reply.Id, reply.HeadId));
        }

        _gateBound.Clear();
    }

    /// <summary>Whether a pending line may be spoken now.</summary>
    private bool GateFree(GateLine line, bool record, long now)
    {
        if (line.Kind is ProposedKind.Error or ProposedKind.Tool)
        {
            return true;
        }

        if (!record)
        {
            return now - line.At >= UnconfirmedHoldMs;
        }

        return line.Kind == ProposedKind.Head ? GateHead(line, now) : GateRow(line, now);
    }

    /// <summary>
    /// A head is confirmed by the first reply, in the record's order, that no head has taken, that no prompt has
    /// closed, and whose key starts with the head's first letters. A head without a letter is confirmed by the
    /// first such reply whatever its key: its rows are then looked for in that key.
    /// </summary>
    private bool GateHead(GateLine head, long now)
    {
        var letters = head.Letters;
        var start = Math.Min(letters.Length, GateHeadLetters);
        foreach (var reply in _gateReplies)
        {
            var key = reply.Key;
            if (reply.Taken || now >= reply.ClosedAt || !key.AsSpan().StartsWith(letters.AsSpan(0, start), StringComparison.Ordinal))
            {
                continue;
            }

            // The place behind the head: as far as its own letters go on in the key.
            var matched = start;
            while (matched < letters.Length && matched < key.Length && letters[matched] == key[matched])
            {
                matched++;
            }

            reply.Taken = true;
            reply.HeadId = head.Line.Id;
            reply.Matched = matched;
            _gateHeads[head.Line.Id] = reply;

            // A new reply begins: the one before it is drawn as far as it goes.
            _gateDrawn = null;
            if (!reply.Provisional)
            {
                _gateBound.Add(reply);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// A row under a confirmed head is released when its letters stand in that reply's key at or behind the place
    /// matched so far, no more than <see cref="GateRowReach"/> letters further on unless the row has
    /// <see cref="GateLongRow"/> letters or more; the place then moves behind it. A row without a letter goes with
    /// its head. A row whose head is not confirmed waits with it.
    /// </summary>
    private bool GateRow(GateLine row, long now)
    {
        if (!_gateHeads.TryGetValue(row.HeadId, out var reply))
        {
            return false;
        }

        var letters = row.Letters;
        if (letters.Length == 0)
        {
            return true;
        }

        var found = reply.Key.IndexOf(letters, reply.Matched, StringComparison.Ordinal);
        if (found < 0 || (letters.Length < GateLongRow && found - reply.Matched > GateRowReach))
        {
            return false;
        }

        reply.Matched = found + letters.Length;
        _gateDrawn = reply;
        _gateDrawnAt = now;
        return true;
    }

    /// <summary>
    /// Says the released lines: one decision for one input. When an earlier step of the same input released lines
    /// (a hold that ended by time, before a frame's own rows), these join them.
    /// </summary>
    private void GateSay(List<Line> lines)
    {
        _health.GateReleased += lines.Count;
        if (_gateSaid is { } said && _decisions.Count > 0 && _decisions[^1] is SpeakReply last && ReferenceEquals(last.Lines, said))
        {
            said.AddRange(lines);
            return;
        }

        _gateSaid = lines;
        Decide(new SpeakReply(_generation, lines));
    }
}
