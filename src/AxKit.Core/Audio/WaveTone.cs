namespace AxKit.Core.Audio;

/// <summary>
/// The app's sounds as WAV data (16-bit mono PCM), generated in code so that no media file ships: the tick for a
/// new line, the chimes for a sent message and for Claude replying, finishing and asking, the music loop that
/// plays while Claude works and the sweeps of work in the background (axClaude FR-7.3, FR-7.8, FR-7.9, FR-7.10).
/// </summary>
public static class WaveTone
{
    public const int SampleRate = 22050;

    /// <summary>The bytes of the WAV header before the samples.</summary>
    public const int HeaderLength = 44;

    /// <summary>
    /// A sequence of sine notes, each faded in and out over a few milliseconds so that the edges do not click.
    /// A frequency of 0 is a pause of that length.
    /// </summary>
    public static byte[] Notes(double amplitude, params (double Hertz, int Milliseconds)[] notes)
    {
        var total = 0;
        foreach (var note in notes)
        {
            total += Samples(note.Milliseconds);
        }

        var samples = new short[total];
        var at = 0;
        foreach (var (hertz, milliseconds) in notes)
        {
            var count = Samples(milliseconds);
            var fade = Math.Min(count / 2, Samples(4));
            for (var i = 0; i < count; i++)
            {
                var envelope = 1.0;
                if (i < fade)
                {
                    envelope = (double)i / fade;
                }
                else if (i >= count - fade)
                {
                    envelope = (double)(count - 1 - i) / fade;
                }

                samples[at + i] = Sample(hertz, i, amplitude * envelope);
            }

            at += count;
        }

        return Wav(samples);
    }

    /// <summary>
    /// Swoops, like a small siren: each glides from one pitch to the other in a straight line over
    /// <paramref name="millisecondsEach"/>, faded in and out over three milliseconds, the next following at once.
    /// Falling swoops sound like an alarm (the command offer's sound, axClaude FR-2.12). The phase is summed sample by
    /// sample, so that the glide has no step in it.
    /// </summary>
    public static byte[] Swoops(double amplitude, double fromHertz, double toHertz, int millisecondsEach, int count)
    {
        var each = Samples(millisecondsEach);
        var fade = Math.Max(1, Math.Min(each / 2, Samples(3)));
        var samples = new short[each * count];
        for (var n = 0; n < count; n++)
        {
            var phase = 0.0;
            for (var i = 0; i < each; i++)
            {
                phase += 2 * Math.PI * (fromHertz + (toHertz - fromHertz) * i / each) / SampleRate;
                var envelope = Math.Min(1.0, Math.Min(i, each - 1 - i) / (double)fade);
                samples[n * each + i] = (short)(Math.Sin(phase) * envelope * amplitude * short.MaxValue);
            }
        }

        return Wav(samples);
    }

    /// <summary>
    /// Strikes, like a typewriter key or a tiny hammer: each is a burst of noise over a short tone that dies away at
    /// once, the noise gone within a couple of milliseconds and the tone within <paramref name="millisecondsEach"/>,
    /// the next strike following as soon as the last has faded. One strike is heard as a single tick; two or three in
    /// a row, a few milliseconds apart, as one ratchety tick. The tone rises over its first 0.6 ms and the noise over
    /// four samples: a sound that starts at full volume is heard as a click followed by a separate falling note. The
    /// noise is the same every time.
    /// </summary>
    public static byte[] Strikes(double amplitude, int millisecondsEach, double noise, params double[] hertz)
    {
        var each = Samples(millisecondsEach);
        var attack = Math.Min(each / 4, SampleRate / 1500);
        var chiff = Math.Max(1, Samples(1));
        var random = new Random(7);
        var samples = new short[each * hertz.Length];
        for (var n = 0; n < hertz.Length; n++)
        {
            for (var i = 0; i < each; i++)
            {
                // Down to about a quarter of a percent at the end, so the cut-off is not heard.
                var envelope = Math.Exp(-6.0 * i / each);
                if (i < attack)
                {
                    envelope *= (double)i / attack;
                }

                var burst = noise * Math.Exp(-(double)i / chiff) * Math.Min(1.0, i / 4.0);
                var value = Math.Sin(2 * Math.PI * hertz[n] * i / SampleRate) * envelope + (random.NextDouble() * 2 - 1) * burst;
                samples[n * each + i] = (short)(Math.Clamp(value, -1.0, 1.0) * amplitude * short.MaxValue);
            }
        }

        return Wav(samples);
    }

    /// <summary>
    /// Soft plucked notes, one every <paramref name="millisecondsEach"/>: each is a tone with a little of its first
    /// two overtones, which die faster than the fundamental as a struck string's do, swelling over 20 ms and ringing
    /// into the next note's time, down to about two percent where the one after that starts. The tail of the last
    /// note wraps round to the start, so the sequence is exactly periodic and loops without a seam, which is what a
    /// sound that plays for as long as Claude works needs (axClaude FR-7.9). The whole is scaled so that its loudest
    /// sample is <paramref name="amplitude"/>.
    /// </summary>
    public static byte[] Plucks(double amplitude, int millisecondsEach, params double[] hertz)
    {
        var each = Samples(millisecondsEach);
        var ring = 2 * each;
        var attack = Math.Max(1, Samples(20));
        var total = each * hertz.Length;
        var mix = new double[total];
        for (var n = 0; n < hertz.Length; n++)
        {
            for (var i = 0; i < ring; i++)
            {
                var decay = Math.Exp(-4.0 * i / ring);
                var envelope = i < attack ? decay * i / attack : decay;
                var phase = 2 * Math.PI * hertz[n] * i / SampleRate;
                var tone = Math.Sin(phase) + 0.3 * Math.Sin(2 * phase) * decay + 0.1 * Math.Sin(3 * phase) * decay * decay;
                mix[(n * each + i) % total] += tone * envelope;
            }
        }

        var peak = 0.0;
        foreach (var value in mix)
        {
            peak = Math.Max(peak, Math.Abs(value));
        }

        var scale = peak > 0 ? amplitude / peak : 0;
        var samples = new short[total];
        for (var i = 0; i < total; i++)
        {
            samples[i] = (short)(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    /// <summary>
    /// How much of its second and third harmonics a sweep or a swell carries: body over the sine, nothing bright
    /// (the first versions, sines with a trace of the second harmonic, were "too thin" to be heard among other
    /// sounds, 2026-11-15).
    /// </summary>
    private const double SecondHarmonic = 0.25;
    private const double ThirdHarmonic = 0.10;

    /// <summary>
    /// How fast each layer of a sweep gets quieter as layers are added: each of n layers plays at n to the power of
    /// minus this. Under 0.5, so that the whole carries more energy with every layer (fuller), and high enough that
    /// five overlapping layers stay near one layer's peak.
    /// </summary>
    private const double SweepLayerFall = 0.35;

    /// <summary>
    /// Gentle sweeps, layered: each layer is one smooth glide in pitch from <paramref name="fromHertz"/> to
    /// <paramref name="toHertz"/> (even in musical pitch, the phase summed sample by sample, so no step is heard),
    /// a sine with some of its second and third harmonics, under a raised-cosine envelope that starts and ends at nothing:
    /// no attack, no click. Each of <paramref name="layers"/> is a pitch ratio (1 the glide as given, 1.5 a fifth
    /// above it, 2 an octave) and starts <paramref name="millisecondsApart"/> after the one before, so the layers
    /// overlap and more of them sound fuller, not much longer. They are mixed into one buffer;
    /// <paramref name="amplitude"/> is the level of one layer alone, and each of several is a little quieter
    /// (<see cref="SweepLayerFall"/>). The sounds of axClaude's background work (FR-7.10).
    /// </summary>
    public static byte[] Sweeps(double amplitude, double fromHertz, double toHertz, int millisecondsEach, int millisecondsApart, params double[] layers)
    {
        var each = Samples(millisecondsEach);
        var apart = Samples(millisecondsApart);
        var mix = new double[each + apart * Math.Max(0, layers.Length - 1)];
        var level = amplitude * Math.Pow(Math.Max(1, layers.Length), -SweepLayerFall);
        for (var n = 0; n < layers.Length; n++)
        {
            var phase = 0.0;
            for (var i = 0; i < each; i++)
            {
                var along = (double)i / each;
                var envelope = 0.5 - 0.5 * Math.Cos(2 * Math.PI * along);
                mix[n * apart + i] += (Math.Sin(phase) + SecondHarmonic * Math.Sin(2 * phase) + ThirdHarmonic * Math.Sin(3 * phase)) * envelope * level;
                phase += 2 * Math.PI * layers[n] * fromHertz * Math.Pow(toHertz / fromHertz, along) / SampleRate;
            }
        }

        var samples = new short[mix.Length];
        for (var i = 0; i < mix.Length; i++)
        {
            samples[i] = (short)(Math.Clamp(mix[i], -1.0, 1.0) * short.MaxValue);
        }

        return Wav(samples);
    }

    /// <summary>
    /// Pings, small bell notes that may overlap: each starts At seconds into the sound, rises in a straight line
    /// over 4 ms and dies away at once (down to under a hundredth after its Ring, in seconds, where it is cut), a
    /// sine with a fifth of its second harmonic, at its Level beside the others. A few in quick succession up or
    /// down a chord are a flourish, punctuation; a soft high one is a sparkle. The whole lasts
    /// <paramref name="seconds"/> and is scaled so that its loudest sample is <paramref name="peak"/>. Chosen by
    /// the user by ear on 2026-11-15 from a script that played candidates, and built here to give the same samples
    /// (the places are rounded to whole samples as the script rounded them). The start and finish sounds of
    /// axClaude's background work (FR-7.10).
    /// </summary>
    public static byte[] Pings(double peak, double seconds, params (double At, double Hertz, double Level, double Ring)[] pings)
    {
        var mix = new double[(int)Math.Round(SampleRate * seconds)];
        foreach (var (at, hertz, level, ring) in pings)
        {
            var start = (int)Math.Round(SampleRate * at);
            var length = (int)Math.Round(SampleRate * ring);
            for (var i = 0; i < length && start + i < mix.Length; i++)
            {
                var t = i / (double)SampleRate;
                var attack = Math.Min(1.0, t / 0.004);
                mix[start + i] += level * attack * Math.Exp(-t / (ring / 5)) * (Math.Sin(2 * Math.PI * hertz * t) + 0.2 * Math.Sin(2 * Math.PI * 2 * hertz * t));
            }
        }

        var loudest = 0.0;
        foreach (var value in mix)
        {
            loudest = Math.Max(loudest, Math.Abs(value));
        }

        var scale = loudest > 0 ? peak / loudest : 0;
        var samples = new short[mix.Length];
        for (var i = 0; i < mix.Length; i++)
        {
            samples[i] = (short)Math.Round(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    /// <summary>
    /// How the loudest sample of a swell grows with its layers: n layers peak at n to the power of this times one
    /// layer's peak (two at 1.23 times, five at 1.62 times).
    /// </summary>
    private const double SwellLayerRise = 0.3;

    /// <summary>
    /// A swell, like a breath or a slow wave: something that goes on, not something that happened. Chosen by the
    /// user by ear on 2026-11-15 from a script that played candidates, and built here to give the same samples;
    /// change nothing of it without his ear. Every layer is a voice with body: two sines
    /// <paramref name="shimmerHertz"/> apart at half level each, which beat slowly, so the voice comes and goes
    /// like breathing, and a quarter of the second harmonic and a tenth of the third, which stay. Its envelope has
    /// no onset to hear: a raised-cosine rise from nothing, the full level held for <paramref name="hold"/> of the
    /// layer's length (0.4 is two fifths), and the same fall. The first layer lasts the whole
    /// <paramref name="milliseconds"/>; each further one enters <paramref name="millisecondsApart"/> later and
    /// leaves as much earlier, so more layers sound fuller and wider, never longer. The whole is scaled so that
    /// its loudest sample is <paramref name="peak"/> for one layer and grows with the layers
    /// (<see cref="SwellLayerRise"/>), never over <paramref name="limit"/>. The recurring sound of axClaude's
    /// background work (FR-7.10).
    /// </summary>
    public static byte[] Swell(double peak, double limit, int milliseconds, int millisecondsApart, double hold, double shimmerHertz, params double[] hertz)
    {
        var total = Samples(milliseconds);
        var apart = Samples(millisecondsApart);
        var mix = new double[total];
        var rise = Math.Max(0.001, (1 - hold) / 2);
        for (var n = 0; n < hertz.Length; n++)
        {
            var length = total - 2 * n * apart;
            for (var i = 0; i < length; i++)
            {
                var along = (double)i / length;
                var edge = Math.Min(1.0, Math.Min(along, 1 - along) / rise);
                var envelope = 0.5 - 0.5 * Math.Cos(Math.PI * edge);
                var turn = 2 * Math.PI * i / SampleRate;
                var tone = 0.5 * (Math.Sin(turn * hertz[n]) + Math.Sin(turn * (hertz[n] + shimmerHertz)))
                    + SecondHarmonic * Math.Sin(turn * 2 * hertz[n]) + ThirdHarmonic * Math.Sin(turn * 3 * hertz[n]);
                mix[n * apart + i] += tone * envelope;
            }
        }

        var loudest = 0.0;
        foreach (var value in mix)
        {
            loudest = Math.Max(loudest, Math.Abs(value));
        }

        var scale = loudest > 0 ? Math.Min(limit, peak * Math.Pow(Math.Max(1, hertz.Length), SwellLayerRise)) / loudest : 0;
        var samples = new short[total];
        for (var i = 0; i < total; i++)
        {
            samples[i] = (short)Math.Round(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    /// <summary>
    /// A pad, slow chords of soft sustained tones that melt into one another: each chord swells from nothing over
    /// <paramref name="millisecondsEach"/> and fades over the same time while the next swells, so the level stays
    /// even and nothing is struck. A voice is a sine with a second, softer one a fraction of a hertz above it, which makes
    /// a slow shimmer, and a trace of its second harmonic; higher voices are softer. A chord's Ping, when not 0, is
    /// one soft bell-like sine at that pitch as the chord is fullest: it rises over 40 ms and rings away over a few
    /// seconds, at <paramref name="pingLevel"/> of a voice. Everything that runs past the end wraps round to the
    /// start, so the whole is exactly periodic and loops without a seam (axClaude's working music, FR-7.9). The
    /// whole is scaled so that its loudest sample is <paramref name="amplitude"/>.
    /// </summary>
    public static byte[] Pad(double amplitude, int millisecondsEach, double pingLevel, params (double[] Hertz, double Ping)[] chords)
    {
        var each = Samples(millisecondsEach);
        var total = each * chords.Length;
        var swell = 2 * each;
        var ring = Math.Min(total, Samples(3000));
        var attack = Math.Max(1, Samples(40));
        var mix = new double[total];
        for (var n = 0; n < chords.Length; n++)
        {
            var (hertz, ping) = chords[n];
            for (var v = 0; v < hertz.Length; v++)
            {
                var weight = Math.Sqrt(hertz[0] / hertz[v]);
                var shimmer = 0.4 + 0.15 * v;
                for (var i = 0; i < swell; i++)
                {
                    var envelope = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / swell);
                    var phase = 2 * Math.PI * hertz[v] * i / SampleRate;
                    // The second sine is the softer one, so the shimmer is a slow rise and fall and never a silence.
                    var tone = 0.65 * Math.Sin(phase) + 0.35 * Math.Sin(phase + 2 * Math.PI * shimmer * i / SampleRate) + 0.08 * Math.Sin(2 * phase);
                    mix[(n * each + i) % total] += tone * envelope * weight;
                }
            }

            if (ping > 0)
            {
                for (var i = 0; i < ring; i++)
                {
                    // Down to under a thousandth at the end, so the cut-off is not heard.
                    var envelope = Math.Exp(-7.0 * i / ring);
                    if (i < attack)
                    {
                        envelope *= 0.5 - 0.5 * Math.Cos(Math.PI * i / attack);
                    }

                    mix[(n * each + each + i) % total] += Math.Sin(2 * Math.PI * ping * i / SampleRate) * envelope * pingLevel;
                }
            }
        }

        var peak = 0.0;
        foreach (var value in mix)
        {
            peak = Math.Max(peak, Math.Abs(value));
        }

        var scale = peak > 0 ? amplitude / peak : 0;
        var samples = new short[total];
        for (var i = 0; i < total; i++)
        {
            samples[i] = (short)(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    private static int Samples(int milliseconds) => milliseconds * SampleRate / 1000;

    private static short Sample(double hertz, int index, double amplitude) =>
        (short)(Math.Sin(2 * Math.PI * hertz * index / SampleRate) * amplitude * short.MaxValue);

    private static byte[] Wav(short[] samples)
    {
        var dataLength = samples.Length * 2;
        var wav = new byte[HeaderLength + dataLength];
        var span = wav.AsSpan();
        "RIFF"u8.CopyTo(span);
        BitConverter.TryWriteBytes(span[4..], 36 + dataLength);
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BitConverter.TryWriteBytes(span[16..], 16);                 // fmt chunk length
        BitConverter.TryWriteBytes(span[20..], (short)1);           // PCM
        BitConverter.TryWriteBytes(span[22..], (short)1);           // mono
        BitConverter.TryWriteBytes(span[24..], SampleRate);
        BitConverter.TryWriteBytes(span[28..], SampleRate * 2);     // bytes per second
        BitConverter.TryWriteBytes(span[32..], (short)2);           // block align
        BitConverter.TryWriteBytes(span[34..], (short)16);          // bits per sample
        "data"u8.CopyTo(span[36..]);
        BitConverter.TryWriteBytes(span[40..], dataLength);
        for (var i = 0; i < samples.Length; i++)
        {
            BitConverter.TryWriteBytes(span[(HeaderLength + i * 2)..], samples[i]);
        }

        return wav;
    }
}
