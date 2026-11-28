namespace AxKit.Core.Pty;

/// <summary>
/// The paced queue of a console window's writes to its program (AxKit.Forms ConsoleForm, docs/axClaude/SPEC.md D33),
/// pure, so that what the queue does with each answer of a write's check is held by unit tests (the sixth review of
/// 2026-11-27, L7: until then the decision sat inside the window's pump, and only window scripts ran it). Each write
/// has its text, the pause after it, and the check it asks right before it goes (<see cref="WriteCheck"/>; null for a
/// write that needs none). The window asks <see cref="Next"/> whenever its pump's timer comes round and does what the
/// step says: write and wait, wait, or tell the one who queued that what stood in the queue was dropped.
/// </summary>
public sealed class WriteQueue
{
    /// <summary>How long a write that was told "not yet" waits before it is asked again.</summary>
    public const int NotYetMs = 100;

    private readonly Queue<(string Text, int DelayAfter, Func<WriteCheck>? Check)> _writes = new();

    /// <summary>How many writes wait.</summary>
    public int Count => _writes.Count;

    /// <summary>A write for the end of the queue, with the pause after it and the check it asks right before it goes.</summary>
    public void Add(string text, int delayAfter, Func<WriteCheck>? check) => _writes.Enqueue((text, delayAfter, check));

    /// <summary>Every write dropped; true when there were any, which the one who queued them is told.</summary>
    public bool Clear()
    {
        var had = _writes.Count > 0;
        _writes.Clear();
        return had;
    }

    /// <summary>
    /// What the pump does now. The first write's check is asked. Goes: it is taken out and written, and the pump
    /// waits its pause (<see cref="WriteStepKind.Write"/>). Not yet: it stays first, nothing behind it goes before it,
    /// and the pump asks again <see cref="NotYetMs"/> later (<see cref="WriteStepKind.Wait"/>). Skip: this write alone
    /// is taken out, and the next one is asked at once. Over: it and every write behind it are dropped
    /// (<see cref="WriteStepKind.Dropped"/>). An empty queue, or one a check emptied itself (it stopped the program),
    /// is <see cref="WriteStepKind.Idle"/>. A write with an empty text is a point to wait at and is never written
    /// (axClaude's wait for Claude Code to take a message): its check answers not yet, skip or over.
    /// </summary>
    public WriteStep Next()
    {
        while (_writes.Count > 0)
        {
            var (text, delay, check) = _writes.Peek();
            WriteCheck answer;
            try
            {
                answer = check?.Invoke() ?? WriteCheck.Goes;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // A check that throws never stops the pump (the seventh review of 2026-11-27, L2): nothing more is
                // written, the queue is dropped as for an over, and the one who queued is told it failed. Until then
                // the exception left the pump marked as writing, and nothing queued afterwards was ever written.
                _writes.Clear();
                return new WriteStep(WriteStepKind.Failed, ex.GetType().Name, 0);
            }

            switch (answer)
            {
                case WriteCheck.Over:
                    _writes.Clear();
                    return new WriteStep(WriteStepKind.Dropped, string.Empty, 0);
                case WriteCheck.NotYet:
                    return new WriteStep(WriteStepKind.Wait, string.Empty, NotYetMs);
                case WriteCheck.Skip:
                    _writes.TryDequeue(out _);
                    continue;
            }

            if (!_writes.TryDequeue(out _))
            {
                // The check emptied the queue itself: nothing is written.
                break;
            }

            if (text.Length == 0)
            {
                continue;
            }

            return new WriteStep(WriteStepKind.Write, text, delay);
        }

        return new WriteStep(WriteStepKind.Idle, string.Empty, 0);
    }
}

/// <summary>What the pump does after <see cref="WriteQueue.Next"/>: the kind, the text to write, and how long to wait before it asks again.</summary>
public readonly record struct WriteStep(WriteStepKind Kind, string Text, int WaitMs);

/// <summary>The four things a paced queue's pump can be told to do.</summary>
public enum WriteStepKind
{
    /// <summary>Nothing waits: the pump stops.</summary>
    Idle,

    /// <summary>The text goes to the program, and the pump asks again after the write's pause.</summary>
    Write,

    /// <summary>The first write said not yet: nothing is written, and the pump asks again a moment later.</summary>
    Wait,

    /// <summary>A check said over: every write that stood in the queue was dropped, and the one who queued them is told.</summary>
    Dropped,

    /// <summary>A check threw: every write that stood in the queue was dropped, and the one who queued them is told that it failed; the step's text is the exception's type, never a text that was queued.</summary>
    Failed,
}
