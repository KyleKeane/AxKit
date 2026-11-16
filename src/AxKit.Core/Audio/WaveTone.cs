namespace AxKit.Core.Audio;

/// <summary>
/// The app's sounds as WAV data (16-bit mono PCM), generated in code so that no media file ships: the tick for a
/// new line, the pop for a tool call, the chimes for a sent message and for Claude replying, finishing and asking, the music loop that
/// plays while Claude works and the bells and chords of work in the background (axClaude FR-7.3, FR-7.8, FR-7.9, FR-7.10).
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
    /// A tiny mouth pop: a round tone, a sine with 0.08 of its second harmonic, whose pitch drops fast from
    /// <paramref name="fromHertz"/> towards <paramref name="toHertz"/> as to + (from - to) * exp(-t / drop), over in
    /// <paramref name="seconds"/>: it rises in a straight line over 1.5 ms, dies away as exp(-t / (seconds / 4)) and
    /// is brought to nothing in a straight line over its last 4 ms, so neither end clicks and nothing rings on. The
    /// whole is scaled so that its loudest sample is <paramref name="peak"/>. The tool call's sound in axClaude
    /// (FR-7.8), chosen by the user by ear on 2026-11-15 from a script that played candidates ("something more like a
    /// tiny mouth pop so it is not elongated"; the first of three), and built here to give the same samples.
    /// </summary>
    public static byte[] Pop(double peak, double fromHertz, double toHertz, double drop, double seconds)
    {
        var count = (int)(SampleRate * seconds);
        var mix = new double[count];
        var phase = 0.0;
        var loudest = 1e-9;
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)SampleRate;
            var pitch = toHertz + (fromHertz - toHertz) * Math.Exp(-t / drop);
            phase += 2 * Math.PI * pitch / SampleRate;
            var attack = Math.Min(1.0, t / 0.0015);
            var body = Math.Exp(-t / (seconds / 4));
            var release = Math.Min(1.0, (seconds - t) / 0.004);
            mix[i] = attack * body * release * (Math.Sin(phase) + 0.08 * Math.Sin(2 * phase));
            loudest = Math.Max(loudest, Math.Abs(mix[i]));
        }

        var scale = peak / loudest;
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            samples[i] = (short)Math.Round(mix[i] * scale * 32767);
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
    /// One soft synth chord with a long tail: the recurring sound of axClaude's background work (FR-7.10). Chosen
    /// by the user by ear on 2026-11-15 from a script that played it, and built here to give the same samples;
    /// change nothing of it without his ear. Each of <paramref name="tones"/> is a pad voice, two sines 0.3% apart
    /// at half level each with a little of the second, third and fourth harmonics; a tone further up the list is
    /// softer and enters 120 ms after the one before it; each rises as a raised cosine over 0.7 s, is full for
    /// 1.5 s and dies away. <paramref name="ping"/>, when not 0, is the accent, as the working music has: one soft
    /// bell note of that pitch 0.9 s in. A small reverb makes the tail: four damped comb filters side by side, then
    /// two all-pass filters, mixed over the dry sound; the last 0.4 s fade to nothing in a straight line. The whole
    /// lasts <paramref name="seconds"/> and is scaled so that its loudest sample is <paramref name="peak"/>.
    /// </summary>
    public static byte[] PadChord(double peak, double seconds, double ping, params double[] tones)
    {
        var count = (int)(SampleRate * seconds);
        var dry = new double[count];
        if (ping > 0)
        {
            const double at = 0.9;
            for (var i = (int)(at * SampleRate); i < count; i++)
            {
                var t = i / (double)SampleRate - at;
                var rise = Math.Min(1.0, t / 0.03);
                dry[i] += 0.22 * rise * Math.Exp(-t / 0.7) * (Math.Sin(2 * Math.PI * ping * t) + 0.15 * Math.Sin(4 * Math.PI * ping * t));
            }
        }

        const double attack = 0.7;
        const double hold = 1.5;
        const double release = 1.2 / 3.0; // 0.4 s, written as the script wrote it, for the same samples
        for (var v = 0; v < tones.Length; v++)
        {
            var f = tones[v];
            var level = 1.0 / Math.Pow(v + 1, 0.35);
            var start = 0.12 * v;
            for (var i = (int)(start * SampleRate); i < count; i++)
            {
                var t = i / (double)SampleRate - start;
                var envelope = t < attack ? 0.5 - 0.5 * Math.Cos(Math.PI * t / attack)
                    : t < attack + hold ? 1.0
                    : Math.Exp(-(t - attack - hold) / release);
                var a = 2 * Math.PI * f * t;
                var b = 2 * Math.PI * (f * 1.003) * t;
                var tone = 0.5 * (Math.Sin(a) + Math.Sin(b)) + (0.22 * Math.Sin(2 * a) + 0.10 * Math.Sin(3 * b) + 0.04 * Math.Sin(4 * a));
                dry[i] += level * envelope * tone;
            }
        }

        // The reverb: four comb filters side by side, each feeding its own output back through a one-pole damping.
        var wet = new double[count];
        foreach (var delay in (int[])[1103, 1277, 1409, 1559])
        {
            var line = new double[delay];
            var at = 0;
            var damped = 0.0;
            for (var i = 0; i < count; i++)
            {
                var delayed = line[at];
                damped = 0.7 * damped + 0.3 * delayed;
                line[at] = dry[i] + 0.86 * damped;
                at = (at + 1) % delay;
                wet[i] += delayed * 0.25;
            }
        }

        // Then two all-pass filters one after the other, which smear the combs' echoes into a tail.
        foreach (var delay in (int[])[347, 113])
        {
            var line = new double[delay];
            var at = 0;
            for (var i = 0; i < count; i++)
            {
                var delayed = line[at];
                var input = wet[i];
                wet[i] = delayed - 0.5 * input;
                line[at] = input + 0.5 * delayed;
                at = (at + 1) % delay;
            }
        }

        var fade = (int)(SampleRate * 0.4);
        var loudest = 1e-9;
        for (var i = 0; i < count; i++)
        {
            var tail = i > count - fade ? (count - i) / (double)fade : 1.0;
            dry[i] = (0.6 * dry[i] + 0.8 * wet[i]) * tail;
            loudest = Math.Max(loudest, Math.Abs(dry[i]));
        }

        var scale = peak / loudest;
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            samples[i] = (short)Math.Round(dry[i] * scale * 32767);
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

    /// <summary>
    /// A click, a key's tick: a sine of <paramref name="hertz"/> mixed half and half with white noise, rising in a
    /// straight line over <paramref name="attackSamples"/> samples and dying away as exp(-<paramref name="decay"/> t),
    /// over in <paramref name="seconds"/>. The whole is scaled so that its loudest sample is <paramref name="peak"/>.
    /// The noise is the same every time. The sound of a letter taken as a command in a text's navigation mode
    /// (docs/axKit/SPEC.md AX-8.13): 8 ms at 2200 Hz, a peak of 0.35, 20 samples of attack, a decay of 600, chosen by
    /// the maintainer by ear on 2026-11-16, so that it is over before the screen reader speaks.
    /// </summary>
    public static byte[] Click(double peak, double hertz, double seconds, int attackSamples, double decay)
    {
        var count = (int)(SampleRate * seconds);
        var mix = new double[count];
        var random = new Random(7);
        var loudest = 1e-9;
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)SampleRate;
            var attack = Math.Min(1.0, (double)i / attackSamples);
            var noise = random.NextDouble() * 2 - 1;
            mix[i] = attack * Math.Exp(-decay * t) * (0.5 * Math.Sin(2 * Math.PI * hertz * t) + 0.5 * noise);
            loudest = Math.Max(loudest, Math.Abs(mix[i]));
        }

        var scale = peak / loudest;
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            samples[i] = (short)Math.Round(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    /// <summary>
    /// One flat tone: a sine of <paramref name="hertz"/> that never moves in pitch, <paramref name="milliseconds"/>
    /// long, at <paramref name="peak"/>, faded in and out in a straight line over <paramref name="fadeMilliseconds"/>.
    /// The sound of a navigation key that found nothing of its kind (AX-8.13).
    /// </summary>
    public static byte[] Flat(double peak, double hertz, int milliseconds, int fadeMilliseconds)
    {
        var count = Samples(milliseconds);
        var fade = Math.Min(count / 2, Samples(fadeMilliseconds));
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            var envelope = Math.Min(1.0, Math.Min((double)i / fade, (double)(count - 1 - i) / fade));
            samples[i] = Sample(hertz, i, peak * envelope);
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
