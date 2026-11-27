namespace AxClaude.Core.Transcript;

/// <summary>
/// Whether what a write still in the window's queue was for is over by the time its turn comes (SPEC.md D33, "What is
/// queued is asked about again when it is written"; the third review of 2026-11-27), pure, so that the rule is tested
/// apart from the window. The window types to Claude Code's screen through a paced queue: a message's Enter goes a
/// tenth of a second after its text, and an answer in the user's own words goes a key at a time over seconds. Until
/// that day a write was judged when it was queued and never again, so a message's Enter met a permission question
/// drawn in between and answered it, and the rest of a long answer went into the question that had replaced the one
/// it was typed for. The window asks here right before each queued write; when the answer is yes, that write and
/// those behind it are dropped and the window says that nothing was sent.
/// </summary>
public static class QueuedWrite
{
    /// <summary>
    /// A message's write (its text, a line break of its own, its Enter): over when Claude Code asks something now
    /// (<paramref name="asksNow"/>) that it did not ask when the message was queued (<paramref name="askedWhenQueued"/>):
    /// the keys would go to the question, and Enter would answer it. A message sent into a question on purpose (the
    /// kept message sent again) was queued while it asked, and goes.
    /// </summary>
    public static bool MessageOver(bool askedWhenQueued, bool asksNow) => asksNow && !askedWhenQueued;

    /// <summary>
    /// A key of a typed answer: over when Claude Code waits on something else than the answer was typed for
    /// (<paramref name="typedFor"/> against <paramref name="waiting"/>, each what waited as the window identifies it:
    /// a question by all it shows, a screen by its title), or on nothing for as long as a settle
    /// (<paramref name="goneForMs"/> against <paramref name="settleMs"/>). Not at once when nothing waits: Claude Code
    /// clears a question's rows for a frame while it draws them again, and a key dropped then would leave an answer
    /// half typed. Never over for an answer typed when nothing the window could read waited (null): it is bound to
    /// nothing.
    /// </summary>
    public static bool AnswerOver(string? typedFor, string? waiting, long goneForMs, int settleMs) =>
        typedFor is not null && (waiting is null ? goneForMs >= settleMs : waiting != typedFor);
}
