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
    /// A message's write (its text, a line break of its own): it goes while Claude Code waits on what it waited on
    /// when the message was queued. A message queued while nothing was asked (<paramref name="askedThen"/> false)
    /// goes while nothing is asked (<paramref name="asksNow"/>): the keys would go to a question drawn meanwhile. A
    /// message sent into a question on purpose (the kept message sent again, D33) is bound to that question by its
    /// identity (<paramref name="waitedFor"/> against <paramref name="waitingNow"/>, as a typed answer is,
    /// <see cref="AnswerOver"/>): a question that replaced it takes none of its keys. Until the fourth review of
    /// 2026-11-27 only the first half was asked, and a kept message went into whatever was asked next.
    /// </summary>
    public static bool MessageGoes(bool askedThen, string? waitedFor, bool asksNow, string? waitingNow) =>
        askedThen ? asksNow && waitingNow == waitedFor : !asksNow;

    /// <summary>
    /// A message's Enter: only on a positive check (the fourth review of 2026-11-27, N3), since the window reads
    /// Claude Code's screen as of the last frame it took in and an Enter answers whatever stands there by the time it
    /// arrives. As the message's other writes (<see cref="MessageGoes"/>), and for a message queued while nothing
    /// was asked, only once the frame shows the message on Claude Code's prompt row (<paramref name="promptHolds"/>):
    /// then the Enter is the message's. When not yet, the window asks again a tenth of a second later, for a few
    /// seconds, and writes the Enter once this holds. Until that day the Enter went unless a question had been seen,
    /// a negative check as old as the last frame.
    /// </summary>
    public static bool MessageEnterGoes(bool askedThen, string? waitedFor, bool asksNow, string? waitingNow, bool promptHolds) =>
        MessageGoes(askedThen, waitedFor, asksNow, waitingNow) && (askedThen || promptHolds);

    /// <summary>
    /// The Enter of a message sent again, a second and a half after the last (SPEC.md FR-2.1; Claude Code drops an
    /// Enter in its first seconds): only while Claude Code's prompt row holds the message, nothing is asked, and no interrupt came since the
    /// message was sent. Whether Claude works is not asked: a message queued behind Claude's work is typed on the
    /// same prompt row, which empties when its Enter is taken.
    /// </summary>
    public static bool EnterAgain(bool promptHolds, bool asksNow, bool interrupted) => promptHolds && !asksNow && !interrupted;

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
