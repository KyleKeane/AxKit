namespace AxKit.Core.Audio;

/// <summary>
/// The soft sounds of things that go on by themselves, as WAV data: axClaude's working music (FR-7.9) and the
/// three sounds of work in the background (FR-7.10). One family: soft sines in C major, so that the background
/// sounds are in tune over the music. Each sound is one method here, the one place to change it;
/// <c>AxKit.Forms/Sounds</c> plays them, and the tests render them to files for the ear
/// (<c>AmbientSoundsTests</c>). PROVISIONAL, the three of background work: the user chooses the final ones by ear.
/// </summary>
public static class AmbientSounds
{
    /// <summary>The most layers the recurring sound has; more things running sound as this many.</summary>
    public const int MostLayers = 5;

    /// <summary>The loudest sample any of the three sounds of background work may have, of full scale: under the chimes' 0.35.</summary>
    public const double SweepLimit = 0.31;

    /// <summary>The loudest sample of the working music, of full scale.</summary>
    public const double MusicPeak = 0.05;

    /// <summary>The length of one chord of the working music; four of them are the loop, 14 s.</summary>
    public const int MusicChordMilliseconds = 3500;

    // The layers of the recurring sound: C3, G3, C4, E4, G4, a chord of C major, wide open (the root, a fifth, the
    // octave, the tenth, the twelfth), the key of the working music. Pure ratios (2, 3, 4, 5 and 6 times 65.405 Hz),
    // so that the voices blend into one body and never beat against one another. The phases were found by trying
    // every combination in steps of 15 degrees for the lowest peaks at every count of voices: with them five voices
    // peak at 2.9 times one voice, with all at 0 at 4.5 times, for the same loudness.
    private const double PulseRoot = 65.405;
    private static readonly (double Hertz, double Phase)[] PulseLayers =
        [(2 * PulseRoot, 0), (3 * PulseRoot, 4.4506), (4 * PulseRoot, 4.9742), (5 * PulseRoot, 0.2618), (6 * PulseRoot, 4.7124)];

    /// <summary>The length of the start sound and of the finish sound.</summary>
    public const int SweepMilliseconds = 650;

    /// <summary>Work started in the background: one sweep rising a fifth, C4 up to G4, 650 ms. What arrives rises.</summary>
    public static byte[] BackgroundStart() => WaveTone.Sweeps(0.2, 261.63, 392.00, SweepMilliseconds, 0, 1);

    /// <summary>Work in the background ended: the same sweep falling, G4 down to C4. What leaves falls.</summary>
    public static byte[] BackgroundFinish() => WaveTone.Sweeps(0.2, 392.00, 261.63, SweepMilliseconds, 0, 1);

    /// <summary>
    /// The recurring sound while work runs in the background: a process, not an event (the user's word on the first
    /// version, which was notes: "it sounds like an event rather than a process"). One slow breath of
    /// <see cref="PulseMilliseconds"/> with no onset: it rises from nothing, stays full for two fifths of its
    /// length and sinks again. <paramref name="layers"/> voices, one for each thing running (1 to
    /// <see cref="MostLayers"/>), the lowest on C3 for the whole breath, each further one entering 100 ms later and
    /// leaving 100 ms earlier, so more things sound fuller and wider and never longer. Each voice drifts up into
    /// its pitch from a little under a semitone below while it rises. Low, with body and clearly louder than the
    /// version before it, which was an octave higher, thin and went unheard among other sounds (2026-11-15).
    /// </summary>
    public static byte[] BackgroundPulse(int layers) =>
        WaveTone.Swell(0.155, PulseMilliseconds, 100, 0.4, 0.95,PulseLayers[..Math.Clamp(layers, 1, MostLayers)]);

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
