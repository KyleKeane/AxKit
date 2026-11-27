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
}
