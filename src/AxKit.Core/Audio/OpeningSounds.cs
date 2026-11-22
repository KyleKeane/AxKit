namespace AxKit.Core.Audio;

/// <summary>
/// The sounds of a way of working starting (docs/axKit/SPEC.md AX-7.11): a form, a list to choose from, a field to
/// type in, a text to read, each opening in the window at the user's command. One family in one voice, the bell
/// pings of <see cref="WaveTone.Pings"/>, used for nothing else: every sound begins with the same mark, three quick
/// beeps running up a chord, which says that the way of working changed, and then goes down as its kind: an
/// extended tone stands for text (a field's tone, a text's an octave lower), discrete beeps stand for items (a
/// list's four, down by steps), and a form mixes both, the most complex because a form is.
/// Chosen by the maintainer by ear on 2026-11-22 from sets played by a script (his words: "the interaction mode
/// change should be a quickly rising set of beeps and then the type of interaction could be a descending earcon";
/// "a field that can take text has more of an extended tone and items should be discrete beeps, and the form that
/// mixes both should have both"), and built here to give the same samples. Pure, tested.
/// </summary>
public static class OpeningSounds
{
    private const double Peak = 0.4;

    /// <summary>Where the kind's own part begins, after the mark, in seconds.</summary>
    private const double Body = 0.14;

    /// <summary>The mark: A5, C sharp 6, E6, 35 ms apart.</summary>
    private static readonly (double At, double Hertz, double Level, double Ring)[] Mark =
        [(0.0, 880.0, 0.9, 0.035), (0.035, 1108.73, 0.9, 0.035), (0.07, 1318.5, 1.0, 0.045)];

    /// <summary>An extended tone: text.</summary>
    private static (double At, double Hertz, double Level, double Ring) Tone(double at, double hertz, double level = 1.0) => (at, hertz, level, 0.24);

    /// <summary>A discrete beep: an item.</summary>
    private static (double At, double Hertz, double Level, double Ring) Beep(double at, double hertz, double level = 0.9) => (at, hertz, level, 0.04);

    private static byte[] Make(double seconds, params (double At, double Hertz, double Level, double Ring)[] body) =>
        WaveTone.Pings(Peak, seconds, [.. Mark, .. body]);

    /// <summary>A field to type in, the simplest: the mark, then one extended tone, A4. 0.44 s.</summary>
    public static byte[] Field() => Make(0.44, Tone(Body, 440.0));

    /// <summary>A text to read: the mark, then one extended tone an octave lower, A3, a little softer. 0.44 s.</summary>
    public static byte[] Text() => Make(0.44, Tone(Body, 220.0, 0.9));

    /// <summary>A list to choose from: the mark, then four quick beeps in a line going down by steps, A5, F sharp 5, E5, D5. 0.40 s.</summary>
    public static byte[] List() => Make(0.40, Beep(Body, 880.0), Beep(Body + 0.06, 739.99), Beep(Body + 0.12, 659.25), Beep(Body + 0.18, 587.33));

    /// <summary>
    /// A form: the mark, then beep, beep, tone, and the same lower: two beeps on D6 and a tone on D5, two beeps on A5
    /// and a tone on A4. 0.70 s, the one sound of a moment longer than half a second beside the call from another
    /// window, as the maintainer chose it: the form's words are said over its end.
    /// </summary>
    public static byte[] Form() => Make(0.70,
        Beep(Body, 1174.66, 0.8), Beep(Body + 0.05, 1174.66, 0.8), Tone(Body + 0.10, 587.33),
        Beep(Body + 0.19, 880.0, 0.8), Beep(Body + 0.24, 880.0, 0.8), Tone(Body + 0.29, 440.0));
}
