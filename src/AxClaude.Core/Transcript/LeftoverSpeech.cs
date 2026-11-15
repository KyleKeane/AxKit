namespace AxClaude.Core.Transcript;

/// <summary>
/// Whether the screen reader may still be reading reply text the window spoke (FR-7.4b): a question that opens
/// then cuts it off, whatever Go to them at once says. The window cannot ask NVDA whether it is speaking, so the end
/// of the speech is an estimate from the length of what was handed over: <see cref="MsPerCharacter"/> for every
/// character, an utterance handed over while the one before is still counted as running starting after it, and never
/// more than <see cref="LongestMs"/> ahead. The estimate is on the long side for a fast voice: an interrupting
/// question when the reply's speech has just ended cuts nothing off. Pure, so that the rule is tested without a window.
/// </summary>
public static class LeftoverSpeech
{
    /// <summary>The time one character of a reply is counted as taking to speak: about 16 characters a second, a voice at an unhurried rate.</summary>
    public const int MsPerCharacter = 60;

    /// <summary>The longest that spoken reply text counts as still running; past it the user has long moved on.</summary>
    public const int LongestMs = 30_000;

    /// <summary>
    /// When the speech is counted as over after <paramref name="length"/> more characters were spoken at
    /// <paramref name="now"/>, given when what was spoken before ends (<paramref name="endsBefore"/>; 0 for nothing).
    /// </summary>
    public static long EndsAt(long now, long endsBefore, int length) =>
        Math.Min(Math.Max(now, endsBefore) + ((long)length * MsPerCharacter), now + LongestMs);

    /// <summary>Whether spoken reply text is still counted as running at <paramref name="now"/>.</summary>
    public static bool Running(long now, long endsAt) => now < endsAt;
}
