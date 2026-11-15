namespace AxKit.Core.Audio;

/// <summary>
/// The soft sounds of things that go on by themselves, as WAV data: axClaude's working music (FR-7.9) and the
/// three sounds of work in the background (FR-7.10). One family: soft tones of the scale of C major, so that the
/// background sounds are in tune over the music. Each sound is one method here, the one place to change it;
/// <c>AxKit.Forms/Sounds</c> plays them, and the tests render them to files for the ear
/// (<c>AmbientSoundsTests</c>). None is provisional any more: the music is settled, and the start sound, the
/// finish sound and the recurring sound were each chosen by the user by ear (2026-11-15) from scripts that played
/// candidates; the tests hold the samples to what he heard.
/// </summary>
public static class AmbientSounds
{
    /// <summary>The most layers the recurring sound has; more things running sound as this many.</summary>
    public const int MostLayers = 5;

    /// <summary>The loudest sample the recurring sound may have with any number of voices, of full scale: under the chimes' 0.35.</summary>
    public const double PulseLimit = 0.33;

    /// <summary>The loudest sample of the working music, of full scale.</summary>
    public const double MusicPeak = 0.05;

    /// <summary>The length of one chord of the working music; four of them are the loop, 14 s.</summary>
    public const int MusicChordMilliseconds = 3500;

    // The layers of the recurring sound: the root G4, 392 Hz, and a fifth, an octave, a tenth and a twelfth above it
    // in pure ratios (1.5, 2, 2.5 and 3 times): 392, 588, 784, 980 and 1176 Hz. The register is the user's choice
    // by ear (2026-11-15): C3 he could not hear on his laptop at all, only feel, and of three roots played to him
    // he took the middle one, then heard one, two and three voices of it and kept them as played.
    private const double PulseRoot = 392;
    private static readonly double[] PulseLayers = [PulseRoot, 1.5 * PulseRoot, 2 * PulseRoot, 2.5 * PulseRoot, 3 * PulseRoot];

    /// <summary>The loudest sample of the recurring sound with one voice, of full scale; n voices peak at n to the power of 0.3 times this.</summary>
    public const double PulsePeak = 0.18;

    /// <summary>The length of the start sound and of the finish sound, in seconds; the notes are over well before it.</summary>
    public const double FlourishSeconds = 0.75;

    /// <summary>The loudest sample of the start sound and of the finish sound, of full scale.</summary>
    public const double FlourishPeak = 0.24;

    /// <summary>
    /// Work started in the background: punctuation, three quick bell notes up, G4, D5, G5 (the recurring sound's
    /// first three voices), 75 ms apart, and a sparkle, one soft ping an octave above the last. What arrives rises.
    /// CHOSEN BY EAR by the user on 2026-11-15 ("a bit of flourish and sparkle, a bit faster, more like
    /// punctuation"; the second of three candidates, "as played"): change nothing here without his ear.
    /// </summary>
    public static byte[] BackgroundStart() => Flourish(PulseRoot, 1.5 * PulseRoot, 2 * PulseRoot);

    /// <summary>Work in the background ended: the same three notes down, G5, D5, G4, with their sparkle. What leaves falls. Chosen by ear with the start sound.</summary>
    public static byte[] BackgroundFinish() => Flourish(2 * PulseRoot, 1.5 * PulseRoot, PulseRoot);

    /// <summary>Three bell notes 75 ms apart, each ringing 0.38 s, and at 0.15 s a ping an octave above the last note at a quarter of a note's level, ringing 0.3 s.</summary>
    private static byte[] Flourish(double first, double second, double third) => WaveTone.Pings(FlourishPeak, FlourishSeconds,
        (0.075 * 0, first, 1.0, 0.38), (0.075 * 1, second, 1.0, 0.38), (0.075 * 2, third, 1.0, 0.38), (0.15, third * 2, 0.25, 0.3));

    /// <summary>
    /// The recurring sound while work runs in the background: a process, not an event (the user's word on the first
    /// version, which was notes: "it sounds like an event rather than a process"). One slow breath of
    /// <see cref="PulseMilliseconds"/> with no onset: it rises from nothing over three tenths of its length, is
    /// full for four and sinks over three, and each voice's two sines, 0.6 Hz apart, make it come and go inside
    /// that. <paramref name="layers"/> voices, one for each thing running (1 to <see cref="MostLayers"/>), the
    /// lowest on G4 for the whole breath, each further one entering 100 ms later and leaving 100 ms earlier, so
    /// more things sound fuller and wider and never longer. CHOSEN BY EAR by the user on 2026-11-15, with one, two
    /// and three voices, "as played": the samples are those of the script he heard (to within one step of a
    /// sample); change nothing here without his ear.
    /// </summary>
    public static byte[] BackgroundPulse(int layers) =>
        WaveTone.Swell(PulsePeak, PulseLimit,PulseMilliseconds, 100, 0.4, 0.6, PulseLayers[..Math.Clamp(layers, 1, MostLayers)]);

    /// <summary>The length of the recurring sound, whatever the number of layers.</summary>
    public const int PulseMilliseconds = 4800;

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
