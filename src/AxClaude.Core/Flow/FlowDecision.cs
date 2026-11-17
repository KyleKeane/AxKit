using AxKit.Core.Transcript;

namespace AxClaude.Core.Flow;

/// <summary>What the core says of the session's work: a turn is open, or none is.</summary>
public enum FlowState
{
    Idle,
    Working,
}

/// <summary>
/// One thing the core decided, for the window to carry out in the order given (events-and-grounding.md EG-4.4,
/// section 6.5), with the generation it was made in. <see cref="ToLine"/> is the decision for the log and for the
/// kept files of a timeline: numbers and fixed words, never a text of the conversation and never a path. A decision
/// prints as that line too, so that one written out by mistake gives nothing away (G7).
/// </summary>
public abstract record FlowDecision(int Generation)
{
    public abstract string ToLine();

    public sealed override string ToString() => ToLine();
}

/// <summary>The state changed: a turn opened, or the last one closed.</summary>
public sealed record StateChanged(int Generation, FlowState State) : FlowDecision(Generation)
{
    public override string ToLine() => State == FlowState.Working ? "state working" : "state idle";
}

/// <summary>
/// The turn that is open was begun by the program itself and not by a message of the user's: work in the background
/// ended and the program took it up (EG-4.6). Decided once for a turn, when the record shows its prompt.
/// </summary>
public sealed record TurnSelfStarted(int Generation) : FlowDecision(Generation)
{
    public override string ToLine() => "self-started";
}

/// <summary>
/// A bell with no end of a turn before it: the window's question flow starts its settle.
/// <paramref name="NoMarks"/>: no mark was seen in this run, so the bell may be Done by the old rule.
/// </summary>
public sealed record AttentionBell(int Generation, bool NoMarks) : FlowDecision(Generation)
{
    public override string ToLine() => NoMarks ? "bell attention no-marks" : "bell attention";
}

/// <summary>Say Done for the turn of this number, this long after its bell.</summary>
public sealed record SayDone(int Generation, int Turn, long AfterBellMs) : FlowDecision(Generation)
{
    public override string ToLine() => $"done turn={Turn} after={AfterBellMs}";
}

/// <summary>Speak these reply lines as one utterance, in the conversation's order.</summary>
public sealed record SpeakReply(int Generation, IReadOnlyList<Line> Lines) : FlowDecision(Generation)
{
    public override string ToLine() => $"speak lines={Lines.Count} letters={Letters(Lines)}";

    /// <summary>How many letters and digits the lines hold, a labelled row's label not counted: <c>claude: ok</c> has two.</summary>
    private static int Letters(IReadOnlyList<Line> lines)
    {
        var count = 0;
        foreach (var line in lines)
        {
            var text = line.Text;
            // A row that is not plain text starts with its label and a colon; a plain row may hold a colon of its own.
            var start = line.Kind == LineKind.Plain ? 0 : text.IndexOf(':') + 1;
            for (var i = start; i < text.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    count++;
                }
            }
        }

        return count;
    }
}

/// <summary>Follow this file as the session's record.</summary>
public sealed record FollowRecord(int Generation, string Path) : FlowDecision(Generation)
{
    public override string ToLine() => "follow";
}

/// <summary>The record followed is no longer the session's: stop following it, and look for the new one.</summary>
public sealed record ForgetRecord(int Generation) : FlowDecision(Generation)
{
    public override string ToLine() => "forget";
}

/// <summary>An entry of the record was bound to the reply whose head is the line of this id.</summary>
public sealed record EntryBound(int Generation, string EntryId, int HeadLineId) : FlowDecision(Generation)
{
    public override string ToLine() => $"bound head={HeadLineId}";
}

/// <summary>The list of tasks running in the background changed.</summary>
public sealed record BackgroundListed(int Generation, IReadOnlyList<FlowTask> Tasks) : FlowDecision(Generation)
{
    public override string ToLine() => $"background tasks={Tasks.Count}";
}

/// <summary>
/// Send a <see cref="Tick"/> at this time: something is due then. It names the earliest such time, and a later
/// one takes an earlier one's place; a tick that comes early or late does no harm.
/// </summary>
public sealed record WakeAt(int Generation, long AtMs) : FlowDecision(Generation)
{
    public override string ToLine() => $"wake at={AtMs}";
}

/// <summary>A line for the log, of fixed words and numbers: <c>stale-turn after=2004</c>.</summary>
public sealed record Note(int Generation, string Text) : FlowDecision(Generation)
{
    public override string ToLine() => "note " + Text;
}
