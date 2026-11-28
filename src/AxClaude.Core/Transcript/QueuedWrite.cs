using AxKit.Core.Pty;

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
    /// 2026-11-27 only the first half was asked, and a kept message went into whatever was asked next. A question the
    /// window could not read (null) is the same as no other (the fifth review of that day, L4): two that cannot be
    /// read were taken for one.
    /// </summary>
    public static bool MessageGoes(bool askedThen, string? waitedFor, bool asksNow, string? waitingNow) =>
        askedThen ? asksNow && StillWaitsOn(waitedFor, waitingNow) : !asksNow;

    /// <summary>
    /// The one rule for a write bound to what Claude Code waited on when it was queued, a message sent into a question
    /// and a typed answer alike (the sixth review of 2026-11-27, L2): it goes only while that very thing waits, and a
    /// question the window could not read (null) is bound to nothing, so nothing is written into it. Until then a
    /// message was refused there and a typed answer went, into whatever replaced it.
    /// </summary>
    public static bool StillWaitsOn(string? waitedFor, string? waitingNow) => waitedFor is not null && waitingNow == waitedFor;

    /// <summary>
    /// The first write of a message's text (the sixth review of 2026-11-27, F1: a message's text goes only onto a bare
    /// prompt row). Over when the message no longer goes (<see cref="MessageGoes"/>). Goes for a message sent into a
    /// question on purpose, whose keys are that question's, and for one queued while nothing was asked once the frame
    /// shows Claude Code's prompt row bare (<paramref name="draft"/> empty, <c>ClaudeModel.PromptDraft</c>). Not yet
    /// while the row holds other text or is not drawn, for <paramref name="longestMs"/>; then over, and the window
    /// gives the message back. Until then a message was typed onto whatever the row held: a message whose Enter Claude
    /// Code had dropped, and the two went as one.
    /// </summary>
    public static WriteCheck MessageFirst(bool askedThen, string? waitedFor, bool asksNow, string? waitingNow, string? draft, long waitedMs, int longestMs) =>
        !MessageGoes(askedThen, waitedFor, asksNow, waitingNow) ? WriteCheck.Over
        : askedThen || draft is { Length: 0 } ? WriteCheck.Goes
        : waitedMs < longestMs ? WriteCheck.NotYet
        : WriteCheck.Over;

    /// <summary>
    /// The keys that empty Claude Code's prompt row of a message given back, before the next message is typed (FR-2.3a;
    /// the sixth review of 2026-11-27, F1): asked once, at the first of them. Over when the next message no longer goes;
    /// goes only while the row holds exactly the text given back (<paramref name="rowHoldsIt"/>, the same comparison
    /// as <c>ClaudeModel.PromptHolds</c>); else skipped, and nothing is cleared: the row holds other text, which the
    /// next message's first write then waits on (<see cref="MessageFirst"/>). Until then the row was emptied whatever it
    /// showed, and a Ctrl+U cleared a message the window had counted as sent along with the one given back.
    /// </summary>
    public static WriteCheck ClearGivenBack(bool messageGoes, bool rowHoldsIt) =>
        !messageGoes ? WriteCheck.Over : rowHoldsIt ? WriteCheck.Goes : WriteCheck.Skip;

    /// <summary>
    /// A message after its Enter (the sixth review of 2026-11-27, F1: a message is in flight until Claude Code has
    /// taken it): the Enter sent again once, <paramref name="againMs"/> after the first, while Claude Code's prompt
    /// row still holds the message (FR-2.1: Claude Code drops an Enter in its first seconds). Skipped when the row no
    /// longer holds it, Claude Code asks something, or the message was sent into a question. Never for a text that
    /// holds a hidden character (<paramref name="hides"/>; the seventh review of 2026-11-27, K4): Claude Code takes
    /// such a character out of its prompt and may send a text the user did not hear, so a second is not risked. A
    /// line end and a tab are none (the eighth review, M1: a message of two lines got no second Enter).
    /// </summary>
    public static WriteCheck EnterAgain(bool askedThen, bool asksNow, bool promptHolds, bool hides, long sinceEnterMs, int againMs) =>
        askedThen || asksNow || !promptHolds || hides ? WriteCheck.Skip
        : sinceEnterMs < againMs ? WriteCheck.NotYet
        : WriteCheck.Goes;

    /// <summary>
    /// Whether the flight of a message whose Enter went is over (the sixth review of 2026-11-27, F1): skipped, and the
    /// queue goes on, once its prompt row is bare again (<paramref name="draft"/> empty), at once for a message sent
    /// into a question or when Claude Code asks, and at <paramref name="longestMs"/> after the Enter whatever the row
    /// shows. Not yet before that while the row is not bare, with everything queued behind it waiting. Never over: the
    /// eighth review of 2026-11-28 (M-A) gives back no message whose Enter went. The sixth and seventh reviews gave
    /// one back on what the row showed after the Enter (a row not bare; a row that still showed the message, K1; the
    /// last row before a question, K2), and each guess sent a message Claude Code had taken a second time: the /clear
    /// recording draws no row from 13448 ms to 16144 ms after its Enter, and /permissions draws its screen in the
    /// row's place in the frame that takes it. The next message still waits for a bare row before it is typed
    /// (<see cref="MessageFirst"/>), so nothing is joined; a draft left on the row is said by the window.
    /// </summary>
    public static WriteCheck Taken(bool askedThen, bool asksNow, string? draft, long sinceEnterMs, int longestMs) =>
        askedThen || asksNow || draft is { Length: 0 } || sinceEnterMs >= longestMs ? WriteCheck.Skip
        : WriteCheck.NotYet;

    /// <summary>
    /// A message's Enter: only on a positive check (the fourth review of 2026-11-27, N3), since the window reads
    /// Claude Code's screen as of the last frame it took in and an Enter answers whatever stands there by the time it
    /// arrives. Over when the message's other writes no longer go (<see cref="MessageGoes"/>). Goes for a message
    /// sent into a question, and for one queued while nothing was asked once the frame shows it on Claude Code's
    /// prompt row (<paramref name="promptHolds"/>): then the Enter is the message's. Not yet while the row does not
    /// show it and it has waited less than <paramref name="longestMs"/> (<paramref name="waitedMs"/> since it was
    /// first asked): the Enter stays first in the window's queue and everything behind it waits (the fifth review of
    /// that day, M1: one message in flight). Over once it has waited that long: the window takes the message back.
    /// </summary>
    public static WriteCheck MessageEnter(bool askedThen, string? waitedFor, bool asksNow, string? waitingNow, bool promptHolds, long waitedMs, int longestMs) =>
        !MessageGoes(askedThen, waitedFor, asksNow, waitingNow) ? WriteCheck.Over
        : askedThen || promptHolds ? WriteCheck.Goes
        : waitedMs < longestMs ? WriteCheck.NotYet
        : WriteCheck.Over;

    /// <summary>
    /// A key of a typed answer: over when Claude Code waits on something else than the answer was typed for
    /// (<paramref name="typedFor"/> against <paramref name="waiting"/>, each what waited as the window identifies it:
    /// a question by all it shows, a screen by its title), or on nothing for as long as a settle
    /// (<paramref name="goneForMs"/> against <paramref name="settleMs"/>). Not at once when nothing waits: Claude Code
    /// clears a question's rows for a frame while it draws them again, and a key dropped then would leave an answer
    /// half typed. An answer typed when nothing the window could read waited (null) is bound to nothing, as a message
    /// is (<see cref="StillWaitsOn"/>, the sixth review of 2026-11-27, L2): over at once. Until then it was never
    /// over, and its keys went into whatever was asked by then.
    /// </summary>
    public static bool AnswerOver(string? typedFor, string? waiting, long goneForMs, int settleMs) =>
        typedFor is null || (waiting is null ? goneForMs >= settleMs : !StillWaitsOn(typedFor, waiting));
}
