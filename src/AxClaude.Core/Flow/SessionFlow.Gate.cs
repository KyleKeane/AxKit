using AxKit.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>
/// The gate of reply speech (events-and-grounding.md EG-5), in its simplest form: a line the screen proposes is
/// released at once while a turn is open or ending, and dropped and counted outside one (EG-5.2): a repaint at
/// idle, a redraw after a picker, a replay. Nothing is ever pending. The rules by which the record confirms a line
/// before it is spoken (EG-5.3 to EG-5.5, EG-5.7) are written into these same methods; what they are given by
/// <c>SessionFlow.cs</c>: <c>InTurn</c>, <c>HasRecord</c>, the generation, <c>Decide</c> for their decisions,
/// and a call of <see cref="GateTick"/> at the start of every input, before the input itself.
/// </summary>
public sealed partial class SessionFlow
{
    /// <summary>A proposed line waits for the record: Done waits for it too (EG-5.6, the first condition).</summary>
    private bool GatePending => false;

    /// <summary>The earliest time a pending line is due, for the core's one wake time; <see cref="Never"/> with none pending.</summary>
    private long GateDue => Never;

    /// <summary>A frame's proposals: released as one decision, in the conversation's order, or dropped outside a turn.</summary>
    private void GateFrame(FrameSeen frame, long now)
    {
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

        // A list of the decision's own: the frame's list is reused by the next pass over the conversation.
        var lines = new Line[proposed.Count];
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = proposed[i].Line;
        }

        _health.GateReleased += lines.Length;
        Decide(new SpeakReply(_generation, lines));
    }

    /// <summary>An entry of the record was read: a reply's text confirms the lines that wait for it.</summary>
    private void GateEntry(EntryRead entry, long now)
    {
    }

    /// <summary>A turn stopped: its last message stands in for the record's entry until that arrives (EG-D-11).</summary>
    private void GateStop(TurnStopped stop, long now)
    {
    }

    /// <summary>Time has passed: a line that waited too long is dropped, or released when there is no record (EG-5.4).</summary>
    private void GateTick(long now)
    {
    }

    /// <summary>The generation rose: every pending line is dropped, unspoken and counted (EG-5.7).</summary>
    private void GateBump()
    {
    }

    /// <summary>The conversation or the run started afresh: the pending lines, the heads and the reply records go (EG-4.9).</summary>
    private void GateReset()
    {
    }
}
