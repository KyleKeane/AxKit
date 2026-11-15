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

    /// <summary>The loudest sample any of the three sounds of background work may have, of full scale: well under the chimes' 0.35.</summary>
    public const double SweepLimit = 0.2;

    /// <summary>The loudest sample of the working music, of full scale.</summary>
    public const double MusicPeak = 0.05;

    /// <summary>The length of one chord of the working music; four of them are the loop, 14 s.</summary>
    public const int MusicChordMilliseconds = 3500;

    // The layers of the recurring sound: C4, G4, C5, E5, G5, a chord of C major, wide open (the root, a fifth, the
    // octave, the tenth, the twelfth), the key of the working music.
    private static readonly double[] PulseLayers = [261.63, 392.00, 523.25, 659.25, 783.99];

    /// <summary>Work started in the background: one sweep rising a fourth, G4 up to C5, 450 ms. What arrives rises.</summary>
    public static byte[] BackgroundStart() => WaveTone.Sweeps(0.14, 392.00, 523.25, 450, 0, 1);

    /// <summary>Work in the background ended: the same sweep falling, C5 down to G4. What leaves falls.</summary>
    public static byte[] BackgroundFinish() => WaveTone.Sweeps(0.14, 523.25, 392.00, 450, 0, 1);

    /// <summary>
    /// The recurring sound while work runs in the background: a process, not an event (the user's word on the first
    /// version, which was notes: "it sounds like an event rather than a process"). One slow breath of
    /// <see cref="PulseMilliseconds"/>, all swell and fade with no onset, as if a voice of the working music's pad
    /// rose and sank: <paramref name="layers"/> voices, one for each thing running (1 to <see cref="MostLayers"/>),
    /// the lowest on C4 for the whole breath, each further one entering 150 ms later and leaving 150 ms earlier, so
    /// more things sound fuller and wider and never longer. Each voice drifts up into its pitch from a little under
    /// a semitone below while it rises.
    /// </summary>
    public static byte[] BackgroundPulse(int layers) =>
        WaveTone.Swell(0.05, PulseMilliseconds, 150, 0.95,PulseLayers[..Math.Clamp(layers, 1, MostLayers)]);

    /// <summary>The length of the recurring sound, whatever the number of layers.</summary>
    public const int PulseMilliseconds = 3200;

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
