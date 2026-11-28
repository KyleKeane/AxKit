namespace AxKit.Core.Pty;

/// <summary>
/// What a write still in a console window's paced queue is told right before its turn (AxKit.Forms ConsoleForm's
/// <c>Enqueue</c>; docs/axClaude/SPEC.md D33). Distinct answers in one type, so that a check cannot say one and mean
/// another (the fifth review of 2026-11-27, M1: a message's Enter that waited for Claude Code's prompt row answered
/// false for "not yet", the queue read false as "over", and every write queued behind it was dropped).
/// </summary>
public enum WriteCheck
{
    /// <summary>The write goes now, and the queue goes on after its pause.</summary>
    Goes,

    /// <summary>Not yet: the write stays first in the queue, everything behind it waits, and it is asked again a moment later.</summary>
    NotYet,

    /// <summary>What the write was for is over: it and every write behind it are dropped, and the window is told.</summary>
    Over,

    /// <summary>This write alone is no longer needed (axClaude: an Enter sent again for a message the row no longer holds): it is dropped, and the queue goes on with the next.</summary>
    Skip,
}
