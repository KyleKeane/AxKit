namespace AxKit.Core.Audio;

/// <summary>
/// The soft sounds of things that go on by themselves, as WAV data: axClaude's working music (FR-7.9) and the
/// sounds of work in the background (FR-7.10). One family: soft tones of the scale of C major and the music's own
/// four chords, so that the background sounds are in tune over the music. Each sound is one method here, the one
/// place to change it; <c>AxKit.Forms/Sounds</c> plays them, and the tests render them to files for the ear
/// (<c>AmbientSoundsTests</c>). None is provisional: the music is settled, and the start sound, the finish sound
/// and the recurring chords were each chosen by the user by ear (2026-11-15) from scripts that played candidates;
/// the tests hold the samples to what he heard.
/// </summary>
public static class AmbientSounds
{
    /// <summary>The loudest sample of the working music, of full scale.</summary>
    public const double MusicPeak = 0.05;

    /// <summary>The length of one chord of the working music; four of them are the loop, 14 s.</summary>
    public const int MusicChordMilliseconds = 3500;

    /// <summary>The first note of the start sound and the last of the finish sound: G4.</summary>
    private const double FlourishRoot = 392;

    /// <summary>The length of the start sound and of the finish sound, in seconds; the notes are over well before it.</summary>
    public const double FlourishSeconds = 0.75;

    /// <summary>The loudest sample of the start sound and of the finish sound, of full scale.</summary>
    public const double FlourishPeak = 0.24;

    /// <summary>
    /// Work started in the background: punctuation, three quick bell notes up, G4, D5, G5, 75 ms apart, and a
    /// sparkle, one soft ping an octave above the last. What arrives rises. CHOSEN BY EAR by the user on 2026-11-15
    /// ("a bit of flourish and sparkle, a bit faster, more like punctuation"; the second of three candidates, "as
    /// played"): change nothing here without his ear.
    /// </summary>
    public static byte[] BackgroundStart() => Flourish(FlourishRoot, 1.5 * FlourishRoot, 2 * FlourishRoot);

    /// <summary>Work in the background ended: the same three notes down, G5, D5, G4, with their sparkle. What leaves falls. Chosen by ear with the start sound.</summary>
    public static byte[] BackgroundFinish() => Flourish(2 * FlourishRoot, 1.5 * FlourishRoot, FlourishRoot);

    /// <summary>Three bell notes 75 ms apart, each ringing 0.38 s, and at 0.15 s a ping an octave above the last note at a quarter of a note's level, ringing 0.3 s.</summary>
    private static byte[] Flourish(double first, double second, double third) => WaveTone.Pings(FlourishPeak, FlourishSeconds,
        (0.075 * 0, first, 1.0, 0.38), (0.075 * 1, second, 1.0, 0.38), (0.075 * 2, third, 1.0, 0.38), (0.15, third * 2, 0.25, 0.3));

    /// <summary>The chords of the recurring sound, by the names the files and the tests use: the working music's four.</summary>
    public static readonly IReadOnlyList<string> ChordNames = ["C", "Am", "F", "G"];

    // Each chord's tones from the bottom up, voiced where a laptop's speakers carry them (C major seventh, A minor
    // ninth, F major seventh, G sixth): one thing running plays the first two, each further thing adds the next.
    private static readonly double[][] ChordTones =
    [
        [261.63, 392.00, 329.63, 493.88, 587.33, 783.99],
        [220.00, 329.63, 261.63, 392.00, 493.88, 659.26],
        [349.23, 523.25, 440.00, 659.26, 783.99, 1046.50],
        [392.00, 587.33, 493.88, 659.26, 880.00, 1174.66],
    ];

    // Each chord's accent, the bell note the working music plays over the same chord: G5, E6, A5, D6.
    private static readonly double[] ChordAccents = [783.99, 1318.51, 880.00, 1174.66];

    /// <summary>The most things the recurring sound tells apart: more sound as this many.</summary>
    public const int MostThings = 5;

    /// <summary>The length of one chord with its tail, in seconds.</summary>
    public const double ChordSeconds = 4.6;

    /// <summary>The loudest sample of a chord for one thing running, of full scale; n things peak at n to the power of 0.2 times this (0.22 to 0.30).</summary>
    public const double ChordPeak = 0.22;

    /// <summary>
    /// The recurring sound while work runs in the background (FR-7.10): one slow synth chord with a long reverb
    /// tail and a soft bell accent, <see cref="ChordSeconds"/> long; the window plays one, pauses, and plays
    /// another of the four at random. <paramref name="chord"/> is the index into <see cref="ChordNames"/>;
    /// <paramref name="things"/> is how many things run (1 to <see cref="MostThings"/>): one plays the chord's
    /// first two tones, each further one adds the next tone on top, so more work sounds fuller. CHOSEN BY EAR by
    /// the user on 2026-11-15 ("a subtle random slow chord progression, and each process can just add on top";
    /// heard at a peak of 0.28, "yes, but softer", so 0.22): the samples are those of the script he heard at that
    /// level; change nothing here without his ear. Takes some tens of milliseconds to make: never on the UI thread.
    /// </summary>
    public static byte[] BackgroundChord(int chord, int things)
    {
        var count = Math.Clamp(things, 1, MostThings);
        return WaveTone.PadChord(ChordPeak * Math.Pow(count, 0.2), ChordSeconds, ChordAccents[chord], ChordTones[chord][..(count + 1)]);
    }

    /// <summary>
    /// The working music: a pad of four slow chords, C major seventh, A minor ninth, F major seventh and G sixth,
    /// 3.5 s each, 14 s that loop without a seam, each chord with one soft high ping (G5, E6, A5, D6) as it is
    /// fullest. Calm lift music in a building of the future. Between 175 and 500 Hz but for the pings.
    /// </summary>
    public static byte[] WorkingMusic() => WaveTone.Pad(MusicPeak, MusicChordMilliseconds, 0.22,
        ([196.00, 261.63, 329.63, 493.88], 783.99),
        ([220.00, 261.63, 392.00, 493.88], 1318.51),
        ([174.61, 261.63, 329.63, 440.00], 880.00),
        ([196.00, 293.66, 329.63, 493.88], 1174.66));
}
