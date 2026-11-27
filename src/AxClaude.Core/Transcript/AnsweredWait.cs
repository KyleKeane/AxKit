namespace AxClaude.Core.Transcript;

/// <summary>
/// The window's rules around the user's own answer to what Claude Code's screen shows (SPEC.md D33; the review of
/// 2026-11-27), pure, so that they are tested apart from the window: when an answered question is over, and when the
/// settle that acts on what waits must wait itself.
///
/// With a screen a question ends when Claude Code draws something else in its place, and what it draws next can read
/// as the question just answered: two files to create in one reply are asked under one title with the same answers.
/// Until that day the window knew a question by its title and answers alone, so the first list stayed, with the first
/// file's name on its answers, nothing was said, and Enter approved the second file unheard.
/// </summary>
public static class AnsweredWait
{
    /// <summary>
    /// Whether what the screen shows now is no longer the question the user answered: something else waits or
    /// nothing does (<paramref name="waiting"/>, a signature or null, against <paramref name="answered"/>), or a
    /// question of the same title and answers says something else (<see cref="Question.Content"/> of the question
    /// answered and of the one that waits; null where no question of Claude's is meant). From then on whatever waits
    /// is shown and said afresh, also when it reads as the one answered.
    /// </summary>
    public static bool Over(string answered, string? waiting, string? answeredContent, string? waitingContent) =>
        waiting != answered || (answeredContent is not null && waitingContent is not null && waitingContent != answeredContent);

    /// <summary>
    /// Whether the settle waits and comes again: while the user's answer or text is still being typed out to Claude
    /// Code a key at a time (<paramref name="lastKeyAt"/> is when its last key will have gone, in the clock of
    /// <paramref name="now"/>), since the screen shows half of it and a row of /feedback or a search was shown again in
    /// the middle of its own typing; and, for an answered question that is over and yet stands as it stood
    /// (<paramref name="answeredStandsAsItStood"/>), until <paramref name="settleMs"/> after that last key, which
    /// Claude Code may not have taken yet: shown again before that, the question would be asked twice.
    /// </summary>
    public static bool SettleWaits(long now, long lastKeyAt, int settleMs, bool answeredStandsAsItStood) =>
        now < lastKeyAt || (answeredStandsAsItStood && now - lastKeyAt < settleMs);

    /// <summary>
    /// Whether what Claude Code waits on now has not been put to the user yet (the second review of 2026-11-27):
    /// something waits (<paramref name="waiting"/>, a signature) and the window has shown or announced nothing for it
    /// (<paramref name="handled"/>, the signature of what it handled last, null for nothing), or what it handled is
    /// over (<paramref name="over"/>) and the settle has yet to show what stands in its place. While this is true no
    /// key of the user's goes to Claude Code as an answer: not a list's Enter, not a screen's key, not a "1" or a bare
    /// Enter from the message field. The moment is the settle's 400 ms and, after a typed answer, as long as its keys
    /// go out. False when nothing waits.
    /// </summary>
    public static bool Unheard(string? waiting, string? handled, bool over) =>
        waiting is not null && (handled is null || over || handled != waiting);

    /// <summary>
    /// Whether the user's key no longer counts as an answer to the question it was pressed at: the question still
    /// stands as it stood (<paramref name="over"/> false) <paramref name="countsMs"/> after the key's last keystroke
    /// went (<paramref name="lastKeyAt"/>), so Claude Code did not take it as one (a key that answers nothing there,
    /// Claude Code's own mode key at a file question). The wait is then unanswered again. Until 2026-11-27 it stayed
    /// answered for good, and a single blank frame minutes later, as Claude Code draws in passing, read as "the
    /// answered question has left": the standing question was asked again, chime and all, its list back on its first
    /// answer.
    /// </summary>
    public static bool AnswerLapsed(long now, long lastKeyAt, int countsMs, bool over) =>
        !over && now - lastKeyAt >= countsMs;

    /// <summary>
    /// Whether a question shown and not answered was replaced by another under the same title with the same answers
    /// (<paramref name="waiting"/> equals <paramref name="shown"/>, both signatures): the two name another tool or
    /// target (<see cref="Question.Core"/>). The window then shows and says the new one afresh; until 2026-11-27 only
    /// an answered question was compared, so a file question Claude Code took back and replaced by the next file's
    /// kept its list, with the first file's name on its answers. By the core and not by every line: a question drawn
    /// again with another line (a tip, a longer diff) is the same question still, and is not asked twice.
    /// </summary>
    public static bool Replaced(string shown, string? waiting, string? shownCore, string? waitingCore) =>
        waiting == shown && shownCore != waitingCore;
}
