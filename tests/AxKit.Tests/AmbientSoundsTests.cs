using AxKit.Core.Audio;

namespace AxKit.Tests;

/// <summary>
/// The sweeps of background work and the working music (axClaude FR-7.10, FR-7.9): what can be said of a sound
/// without hearing it. With AXKIT_RENDER_SOUNDS set to a folder, the last test also writes the sounds there as WAV
/// files, for the ear; it plays nothing.
/// </summary>
public class AmbientSoundsTests
{
    private const int Rate = WaveTone.SampleRate;

    private static short[] SamplesOf(byte[] wav)
    {
        var samples = new short[(wav.Length - WaveTone.HeaderLength) / 2];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2);
        }

        return samples;
    }

    private static int Peak(short[] samples, int from, int to)
    {
        var peak = 0;
        for (var i = from; i < to; i++)
        {
            peak = Math.Max(peak, Math.Abs((int)samples[i]));
        }

        return peak;
    }

    private static int Peak(short[] samples) => Peak(samples, 0, samples.Length);

    private static double Energy(short[] samples)
    {
        var sum = 0.0;
        foreach (var sample in samples)
        {
            sum += (double)sample * sample;
        }

        return sum;
    }

    /// <summary>The pitch by the zero crossings between two places.</summary>
    private static int Crossings(short[] samples, int from, int to)
    {
        var count = 0;
        for (var i = from + 1; i < to; i++)
        {
            if ((samples[i - 1] < 0) != (samples[i] < 0))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How strongly a pitch sounds between two places: the samples against a sine and a cosine of it.</summary>
    private static double Strength(short[] samples, int from, int to, double hertz)
    {
        var sine = 0.0;
        var cosine = 0.0;
        for (var i = from; i < to; i++)
        {
            sine += samples[i] * Math.Sin(2 * Math.PI * hertz * i / Rate);
            cosine += samples[i] * Math.Cos(2 * Math.PI * hertz * i / Rate);
        }

        return Math.Sqrt(sine * sine + cosine * cosine);
    }

    /// <summary>
    /// The start and finish sounds as the user chose them by ear on 2026-11-15, written out a second time from the
    /// script that played them to him: three pings 75 ms apart on the notes given, each ringing 0.38 s, and one at
    /// 0.15 s an octave above the last at a quarter of the level, ringing 0.3 s; a ping rises over 4 ms and dies
    /// away as exp(-t / (ring / 5)), a sine with a fifth of its second harmonic; 0.75 s, the loudest sample 0.24.
    /// </summary>
    private static short[] FlourishAsChosen(params double[] notes)
    {
        var mix = new double[(int)Math.Round(Rate * 0.75)];

        void AddPing(double at, double f, double level, double ring)
        {
            var start = (int)Math.Round(Rate * at);
            var length = (int)Math.Round(Rate * ring);
            for (var i = 0; i < length && start + i < mix.Length; i++)
            {
                var t = i / (double)Rate;
                var attack = Math.Min(1.0, t / 0.004);
                mix[start + i] += level * attack * Math.Exp(-t / (ring / 5)) * (Math.Sin(2 * Math.PI * f * t) + 0.2 * Math.Sin(2 * Math.PI * 2 * f * t));
            }
        }

        for (var n = 0; n < 3; n++)
        {
            AddPing(0.075 * n, notes[n], 1.0, 0.38);
        }

        AddPing(0.15, notes[2] * 2, 0.25, 0.3);
        var scale = 0.24 / mix.Max(Math.Abs);
        return [.. mix.Select(sample => (short)Math.Round(sample * scale * 32767))];
    }

    [Fact]
    public void The_start_and_finish_sounds_are_the_flourishes_the_user_chose_by_ear()
    {
        Assert.Equal(FlourishAsChosen(392, 588, 784), SamplesOf(AmbientSounds.BackgroundStart()));
        Assert.Equal(FlourishAsChosen(784, 588, 392), SamplesOf(AmbientSounds.BackgroundFinish()));
    }

    [Fact]
    public void The_start_sound_is_three_quick_notes_up_and_the_finish_sound_the_same_three_down()
    {
        var start = SamplesOf(AmbientSounds.BackgroundStart());
        var finish = SamplesOf(AmbientSounds.BackgroundFinish());

        // Punctuation: three quarters of a second in the buffer, the notes over within about half of one.
        Assert.Equal((int)Math.Round(AmbientSounds.FlourishSeconds * Rate), start.Length);
        Assert.Equal(start.Length, finish.Length);
        Assert.InRange(AmbientSounds.FlourishSeconds, 0.5, 0.8);

        // Each 75 ms has its note: G4, D5, G5 going up for a start, the same going down for an end.
        double[] notes = [392, 588, 784];
        var slice = 75 * Rate / 1000;
        for (var n = 0; n < 3; n++)
        {
            var up = notes.Select(hertz => Strength(start, n * slice, (n + 1) * slice, hertz)).ToArray();
            var down = notes.Select(hertz => Strength(finish, n * slice, (n + 1) * slice, hertz)).ToArray();
            Assert.Equal(n, Array.IndexOf(up, up.Max()));
            Assert.Equal(2 - n, Array.IndexOf(down, down.Max()));
        }

        // The sparkle: an octave above the last note, from 150 ms on; before it that pitch is all but absent.
        Assert.True(Strength(start, 2 * slice, 4 * slice, 1568) > 5 * Strength(start, 0, 2 * slice, 1568), "the start sound should sparkle on G6");

        foreach (var samples in new[] { start, finish })
        {
            // Under the chimes (0.35), over the breath's one voice (0.18): the loudest sample is 0.24 of full scale.
            Assert.Equal(AmbientSounds.FlourishPeak * short.MaxValue, Peak(samples), 1.5);

            // No click at either end: it starts at nothing, a note is cut where it has died away to under a
            // fiftieth of the loudest sample, and the last fifth of a second is silence.
            Assert.Equal(0, samples[0]);
            Assert.True(Peak(samples, 0, Rate / 1000) < Peak(samples) / 3, "a note should rise over some milliseconds");
            Assert.True(Peak(samples, 500 * Rate / 1000, 530 * Rate / 1000) < Peak(samples) / 50, "the last note should have died away where it is cut");
            Assert.Equal(0, Peak(samples, 550 * Rate / 1000, samples.Length));
        }
    }

    [Fact]
    public void The_recurring_sound_starts_and_ends_at_nothing_and_swells_and_fades_slowly()
    {
        foreach (var layers in Enumerable.Range(1, AmbientSounds.MostLayers))
        {
            var samples = SamplesOf(AmbientSounds.BackgroundPulse(layers));
            var peak = Peak(samples);
            var edge = 5 * Rate / 1000;

            // No click: silence at both ends, and the first and last 5 ms far below the loudest sample.
            Assert.Equal(0, samples[0]);
            Assert.InRange(Math.Abs((int)samples[^1]), 0, 2);
            Assert.True(Peak(samples, 0, edge) < peak / 20, "the breath should start from nothing");
            Assert.True(Peak(samples, samples.Length - edge, samples.Length) < peak / 20, "the breath should end in nothing");

            // No attack: a tenth of the way in, the sound is still under a third of its loudest, and so again at the end.
            Assert.True(Peak(samples, 0, samples.Length / 10) < peak / 3, "the breath should swell slowly");
            Assert.True(Peak(samples, samples.Length - samples.Length / 10, samples.Length) < peak / 3, "the breath should fade slowly");
        }
    }

    [Fact]
    public void Pings_are_placed_by_the_second_and_scaled_to_the_peak()
    {
        var samples = SamplesOf(WaveTone.Pings(0.3, 0.5, (0.1, 500, 1.0, 0.2), (0.3, 1000, 0.5, 0.1)));
        Assert.Equal(Rate / 2, samples.Length);
        Assert.Equal(0.3 * short.MaxValue, Peak(samples), 1.5);

        // Silence before the first ping, between the two once the first is cut, and after the second.
        Assert.Equal(0, Peak(samples, 0, Rate / 10));
        Assert.True(Peak(samples, Rate / 10, Rate / 10 + Rate / 20) > 0.25 * short.MaxValue, "the first ping should be the loudest");
        Assert.True(Strength(samples, 3 * Rate / 10, 4 * Rate / 10, 1000) > 5 * Strength(samples, 3 * Rate / 10, 4 * Rate / 10, 500), "the second ping should sound alone");
        Assert.Equal(0, Peak(samples, 4 * Rate / 10 + 1, samples.Length));

        // A ping that would run past the end is cut there, and no ping at all is silence.
        Assert.Equal(Rate / 10, SamplesOf(WaveTone.Pings(0.3, 0.1, (0.05, 500, 1.0, 0.2))).Length);
        Assert.Equal(0, Peak(SamplesOf(WaveTone.Pings(0.3, 0.1))));
    }

    /// <summary>
    /// The breath as the user chose it by ear on 2026-11-15, written out a second time from the script that played
    /// it to him: voices at 1, 1.5, 2, 2.5 and 3 times 392 Hz, each two sines 0.6 Hz apart at half level with a
    /// quarter of the second harmonic and a tenth of the third, a raised-cosine rise over three tenths of the voice,
    /// full for four, a fall over three, each further voice 100 ms later in and 100 ms earlier out, 4.8 s, the
    /// loudest sample 0.18 times the voices to the power of 0.3 and never over 0.33.
    /// </summary>
    private static short[] BreathAsChosen(int voices)
    {
        double[] ratios = [1.0, 1.5, 2.0, 2.5, 3.0];
        var count = (int)(Rate * 4.8);
        var mix = new double[count];
        for (var v = 0; v < voices; v++)
        {
            var f = 392 * ratios[v];
            var lead = (int)(Rate * 0.1 * v);
            var length = count - 2 * lead;
            for (var i = 0; i < length; i++)
            {
                var x = i / (double)length;
                var envelope = x < 0.3 ? 0.5 - 0.5 * Math.Cos(Math.PI * x / 0.3) : x > 0.7 ? 0.5 - 0.5 * Math.Cos(Math.PI * (1 - x) / 0.3) : 1.0;
                var t = i / (double)Rate;
                var tone = Math.Sin(2 * Math.PI * f * t) + Math.Sin(2 * Math.PI * (f + 0.6) * t);
                tone = 0.5 * tone + 0.25 * Math.Sin(2 * Math.PI * 2 * f * t) + 0.10 * Math.Sin(2 * Math.PI * 3 * f * t);
                mix[lead + i] += envelope * tone;
            }
        }

        var max = mix.Max(Math.Abs);
        var scale = 0.18 * Math.Pow(voices, 0.3) / max;
        if (scale * max > 0.33)
        {
            scale = 0.33 / max;
        }

        return [.. mix.Select(sample => (short)Math.Round(sample * scale * 32767))];
    }

    [Fact]
    public void The_recurring_sound_is_the_breath_the_user_chose_by_ear()
    {
        for (var voices = 1; voices <= AmbientSounds.MostLayers; voices++)
        {
            var chosen = BreathAsChosen(voices);
            var built = SamplesOf(AmbientSounds.BackgroundPulse(voices));
            Assert.Equal(chosen.Length, built.Length);
            for (var i = 0; i < chosen.Length; i++)
            {
                // The same sample, to within one step: only the order of the multiplications differs.
                Assert.InRange(built[i] - chosen[i], -1, 1);
            }
        }
    }

    [Fact]
    public void The_recurring_sound_is_one_slow_breath_fuller_with_each_thing_and_never_longer()
    {
        var one = SamplesOf(AmbientSounds.BackgroundPulse(1));
        Assert.Equal(AmbientSounds.PulseMilliseconds * Rate / 1000, one.Length);
        Assert.Equal(4800, AmbientSounds.PulseMilliseconds);
        var apart = 100 * Rate / 1000;
        for (var layers = 1; layers <= AmbientSounds.MostLayers; layers++)
        {
            var samples = SamplesOf(AmbientSounds.BackgroundPulse(layers));

            // A process, not an event: the same length whatever runs, and both ends all but silent beside the middle.
            Assert.Equal(one.Length, samples.Length);
            var tenth = samples.Length / 10;
            var tenths = Enumerable.Range(0, 10).Select(n => Energy(samples[(n * tenth)..((n + 1) * tenth)])).ToArray();
            var fullest = tenths.Max();
            Assert.True(tenths[0] < fullest / 10 && tenths[9] < fullest / 10, $"{layers} layers: the ends should be all but silent");
            Assert.True(Array.IndexOf(tenths, fullest) is >= 3 and <= 6, $"{layers} layers: the fullest tenth should be in the middle");

            // Fuller with every voice by the rule he heard: the loudest sample is 0.18 times the voices to the
            // power of 0.3 (0.18, 0.22, 0.25, 0.27, 0.29), under the chimes' 0.35.
            Assert.Equal(AmbientSounds.PulsePeak * Math.Pow(layers, 0.3) * short.MaxValue, Peak(samples), 1.5);
            Assert.InRange(Peak(samples), 0, AmbientSounds.PulseLimit * short.MaxValue);

            // The first voice of several is the one breath itself at another level: where it sounds alone, the same shape.
            var scale = (double)Peak(samples, 0, apart) / Peak(one, 0, apart);
            Assert.InRange(scale, 0.2, 1.0);
            for (var i = 0; i < apart; i += 7)
            {
                Assert.Equal(one[i] * scale, samples[i], 3.0);
            }
        }

        // More than five things sound as five, and none is never asked for: the count is clamped.
        Assert.Equal(AmbientSounds.BackgroundPulse(5), AmbientSounds.BackgroundPulse(9));
        Assert.Equal(AmbientSounds.BackgroundPulse(1), AmbientSounds.BackgroundPulse(0));

        // The pitch is held from end to end, no glide: G4, 392 Hz, the register he could hear on his laptop.
        var fifth = one.Length / 5;
        var held = Crossings(one, 3 * fifth, 4 * fifth);
        Assert.InRange(held, 2 * 388 * fifth / Rate, 2 * 396 * fifth / Rate);

        // The voices above are higher: five have more crossings there than one.
        var five = SamplesOf(AmbientSounds.BackgroundPulse(5));
        Assert.True(Crossings(five, 3 * fifth, 4 * fifth) > held);
    }

    [Fact]
    public void A_swell_nests_its_layers_inside_the_first()
    {
        var one = SamplesOf(WaveTone.Swell(0.2, 0.9, 1000, 100, 0.4, 0, 300));
        var two = SamplesOf(WaveTone.Swell(0.2, 0.9, 1000, 100, 0.4, 0, 300, 450));
        Assert.Equal(Rate, one.Length);
        Assert.Equal(one.Length, two.Length);
        Assert.Equal(0, one[0]);

        // Scaled as a whole: one layer peaks at the peak asked for, two at 2 to the power of 0.3 times it, and none over the limit.
        Assert.Equal(0.2 * short.MaxValue, Peak(one), 1.5);
        Assert.Equal(0.2 * Math.Pow(2, 0.3) * short.MaxValue, Peak(two), 1.5);
        Assert.Equal(0.21 * short.MaxValue, Peak(SamplesOf(WaveTone.Swell(0.2, 0.21, 1000, 100, 0.4, 0, 300, 450))), 1.5);

        // Held at the full level for two fifths of the length (no shimmer here): every 50 ms from 300 to 700 ms is as loud as the loudest.
        for (var from = 300; from < 700; from += 50)
        {
            Assert.True(Peak(one, from * Rate / 1000, (from + 50) * Rate / 1000) > Peak(one) * 0.9, $"full at {from} ms");
        }

        // Two sines 2 Hz apart beat: silent a quarter of a second in, where one alone is full... and loud again at half a second.
        var beating = SamplesOf(WaveTone.Swell(0.2, 0.9, 1000, 100, 1, 2, 300));
        Assert.True(Peak(beating, 245 * Rate / 1000, 255 * Rate / 1000) < Peak(beating, 495 * Rate / 1000, 505 * Rate / 1000) / 2, "the two sines should beat");

        // No hold at all is still a sound, and a whole hold does not divide by nothing.
        Assert.Equal(one.Length, SamplesOf(WaveTone.Swell(0.2, 0.9, 1000, 100, 0, 0, 300)).Length);
        Assert.Equal(0.2 * short.MaxValue, Peak(SamplesOf(WaveTone.Swell(0.2, 0.9, 1000, 100, 1, 0, 300))), 1.5);

        // The second layer enters 100 ms in and has left 100 ms before the end: outside that, two are one at another level.
        var apart = 100 * Rate / 1000;
        var scale = (double)Peak(two, 0, apart) / Peak(one, 0, apart);
        for (var i = 0; i < apart; i += 5)
        {
            Assert.Equal(one[i] * scale, two[i], 3.0);
            Assert.Equal(one[^(i + 1)] * scale, two[^(i + 1)], 3.0);
        }
    }

    [Fact]
    public void Sweeps_mix_their_layers_into_one_buffer_without_clipping()
    {
        // Far louder than any sound of the app: six layers at once stay inside the range (clamped, never wrapped round).
        var samples = SamplesOf(WaveTone.Sweeps(0.9, 300, 600, 300, 0, 1, 1, 1, 1, 1, 1));
        Assert.Equal(300 * Rate / 1000, samples.Length);
        Assert.InRange(Peak(samples), 0.9 * short.MaxValue, short.MaxValue);
        for (var i = 1; i < samples.Length; i++)
        {
            // A sample that wrapped round would jump from one end of the range to the other.
            Assert.True(Math.Abs(samples[i] - samples[i - 1]) < short.MaxValue, "no sample should wrap round");
        }

        Assert.Equal(WaveTone.HeaderLength, WaveTone.Sweeps(0.2, 300, 600, 0, 0, 1).Length);
    }

    [Fact]
    public void The_working_music_is_a_quiet_pad_of_fourteen_seconds_that_loops_without_a_seam()
    {
        var samples = SamplesOf(AmbientSounds.WorkingMusic());
        Assert.Equal(4 * AmbientSounds.MusicChordMilliseconds * Rate / 1000, samples.Length);

        // Scaled as a whole: the loudest sample is the peak, far under the chimes and under the plucked loop before it (0.08).
        var peak = Peak(samples);
        Assert.InRange(peak, AmbientSounds.MusicPeak * short.MaxValue - 2, AmbientSounds.MusicPeak * short.MaxValue + 1);

        // The loop point is like any other place: the step from the last sample to the first, and the change of
        // slope across it, are no larger than the largest inside the buffer.
        var step = 0;
        var bend = 0;
        for (var i = 1; i < samples.Length; i++)
        {
            step = Math.Max(step, Math.Abs(samples[i] - samples[i - 1]));
            if (i > 1)
            {
                bend = Math.Max(bend, Math.Abs(samples[i] - 2 * samples[i - 1] + samples[i - 2]));
            }
        }

        Assert.InRange(Math.Abs(samples[0] - samples[^1]), 0, step);
        Assert.InRange(Math.Abs(samples[0] - 2 * samples[^1] + samples[^2]), 0, bend + 1);
        Assert.InRange(Math.Abs(samples[1] - 2 * samples[0] + samples[^1]), 0, bend + 1);
        Assert.True(step < peak / 3, "nothing sharp: no step between two samples near the loudest sample's size");

        // Sustained, nothing struck and no gap: every half second of it is heard, and none stands out.
        var half = Rate / 2;
        for (var from = 0; from + half <= samples.Length; from += half)
        {
            Assert.InRange(Peak(samples, from, from + half), peak / 4, peak);
        }
    }

    [Fact]
    public void A_pad_wraps_its_chords_round_the_end()
    {
        var samples = SamplesOf(WaveTone.Pad(0.1, 200, 0, ([220.0, 330.0], 0), ([262.0], 0)));
        Assert.Equal(400 * Rate / 1000, samples.Length);
        Assert.InRange(Peak(samples), 0.1 * short.MaxValue - 2, 0.1 * short.MaxValue + 1);
        Assert.InRange(Math.Abs(samples[0] - samples[^1]), 0, Peak(samples) / 5);
    }

    /// <summary>
    /// For the ear: with AXKIT_RENDER_SOUNDS naming a folder, the sounds as WAV files there (the music twice over, to
    /// hear the loop point). Nothing without it, and nothing is ever played.
    /// </summary>
    [Fact]
    public void The_sounds_are_written_as_files_when_a_folder_is_named()
    {
        var folder = Environment.GetEnvironmentVariable("AXKIT_RENDER_SOUNDS");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "background-start.wav"), AmbientSounds.BackgroundStart());
        File.WriteAllBytes(Path.Combine(folder, "background-finish.wav"), AmbientSounds.BackgroundFinish());
        foreach (var layers in new[] { 1, 2, 3, 5 })
        {
            File.WriteAllBytes(Path.Combine(folder, $"background-pulse-{layers}.wav"), AmbientSounds.BackgroundPulse(layers));
        }

        File.WriteAllBytes(Path.Combine(folder, "background-music.wav"), Twice(AmbientSounds.WorkingMusic()));
        Assert.True(File.Exists(Path.Combine(folder, "background-music.wav")));
    }

    /// <summary>A WAV file's samples twice in a row, the two lengths in its header set to match.</summary>
    private static byte[] Twice(byte[] wav)
    {
        var data = wav.Length - WaveTone.HeaderLength;
        var twice = new byte[WaveTone.HeaderLength + 2 * data];
        wav.AsSpan(0, WaveTone.HeaderLength).CopyTo(twice);
        wav.AsSpan(WaveTone.HeaderLength).CopyTo(twice.AsSpan(WaveTone.HeaderLength));
        wav.AsSpan(WaveTone.HeaderLength).CopyTo(twice.AsSpan(WaveTone.HeaderLength + data));
        BitConverter.TryWriteBytes(twice.AsSpan(4), 36 + 2 * data);
        BitConverter.TryWriteBytes(twice.AsSpan(40), 2 * data);
        return twice;
    }
}
