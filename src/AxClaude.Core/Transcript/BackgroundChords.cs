using AxKit.Core.Audio;

namespace AxClaude.Core.Transcript;

/// <summary>One chord of the recurring sound to play now: which of the chords (an index into <see cref="AmbientSounds.ChordNames"/>), and for how many things running (1 to <see cref="AmbientSounds.MostThings"/>).</summary>
public sealed record BackgroundChord(int Chord, int Things);

/// <summary>
/// When the recurring sound of background work plays, and what (SPEC.md FR-7.10; the user's words, 2026-11-15: "a
/// subtle random slow chord progression, and each process can just add on top whenever it starts ... 4 s chord with
/// tail that fades and 2 s pause"). While something runs, a chord starts, sounds for <see cref="ChordMilliseconds"/>,
/// the pause follows, and the next chord starts: another of the chords at random, never the same twice running. A
/// chord is made for the number of things running at the moment it starts, so a thing that starts meanwhile is heard
/// in the next chord, and a sounding chord is never cut: when the number reaches zero the chord rings out and no new
/// one starts. Pure: the window asks <see cref="Next"/> on a timer and plays what it returns; the times are the
/// caller's milliseconds, and the random numbers come from outside, so the tests know the sequence.
/// </summary>
public sealed class BackgroundChords
{
    /// <summary>How long one chord sounds, its tail included: the length of the buffer AmbientSounds makes (<see cref="AmbientSounds.ChordSeconds"/>; a test holds the two together).</summary>
    public const int ChordMilliseconds = 4600;

    /// <summary>The pause between two chords, in seconds: the choices of Options, Background work.</summary>
    public static readonly IReadOnlyList<int> PauseSeconds = [2, 5, 10, 20];

    /// <summary>Two seconds at first, the user's own number: "only a few seconds of silence so that I do not need to wait to hear it".</summary>
    public const int DefaultPauseSeconds = 2;

    /// <summary>A number of seconds from a settings file: one of <see cref="PauseSeconds"/>, or the default.</summary>
    public static int SanitizePauseSeconds(int seconds) => PauseSeconds.Contains(seconds) ? seconds : DefaultPauseSeconds;

    /// <summary>The things a chord is made for when <paramref name="total"/> run: as many, up to <see cref="AmbientSounds.MostThings"/>; none for none.</summary>
    public static int Things(int total) => Math.Clamp(total, 0, AmbientSounds.MostThings);

    private readonly int _chords;
    private readonly Func<int, int> _random;
    private int _last = -1;
    private long _startedAt;

    /// <param name="chords">How many chords there are to choose from, at least two: <c>AmbientSounds.ChordNames.Count</c> in the app.</param>
    /// <param name="random">A whole number from 0 up to, not including, the number it is given: <c>Random.Shared.Next</c> in the app.</param>
    public BackgroundChords(int chords, Func<int, int> random)
    {
        _chords = chords;
        _random = random;
    }

    /// <summary>The chord before the next, or -1 before the first.</summary>
    public int Last => _last;

    /// <summary>
    /// The chord to start at <paramref name="now"/>, or null: nothing while nothing runs, while the last chord
    /// still sounds or its pause still lasts, or while <paramref name="held"/> (a reply is still being read; the
    /// chord then starts as soon as it is not).
    /// </summary>
    /// <param name="now">The caller's clock, in milliseconds.</param>
    /// <param name="total">How many things run in the background now.</param>
    /// <param name="pauseSeconds">The pause after a chord, as the setting is now: a change of it counts for the pause that is running.</param>
    /// <param name="held">No chord may start now, though one is due.</param>
    public BackgroundChord? Next(long now, int total, int pauseSeconds, bool held)
    {
        if (total <= 0 || held || (_last >= 0 && now < _startedAt + ChordMilliseconds + pauseSeconds * 1000L))
        {
            return null;
        }

        // Never the same chord twice running: after the first, one of the others, each as likely.
        _last = _last < 0 ? _random(_chords) : (_last + 1 + _random(_chords - 1)) % _chords;
        _startedAt = now;
        return new BackgroundChord(_last, Things(total));
    }
}
