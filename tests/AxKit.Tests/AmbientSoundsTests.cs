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

    [Fact]
    public void The_start_sound_is_one_sweep_that_rises_and_the_finish_sound_the_same_falling()
    {
        var start = SamplesOf(AmbientSounds.BackgroundStart());
        var finish = SamplesOf(AmbientSounds.BackgroundFinish());
        Assert.Equal(AmbientSounds.SweepMilliseconds * Rate / 1000, start.Length);
        Assert.Equal(start.Length, finish.Length);

        // An event, short, and heard: clearly louder than the first version (0.15) and under the chimes (0.35).
        Assert.InRange(AmbientSounds.SweepMilliseconds, 500, 800);
        Assert.InRange(Peak(start), 0.20 * short.MaxValue, 0.25 * short.MaxValue);

        var third = start.Length / 3;
        Assert.True(Crossings(start, 0, third) * 11 < Crossings(start, 2 * third, start.Length) * 10, "the start sound should rise in pitch");
        Assert.True(Crossings(finish, 0, third) * 10 > Crossings(finish, 2 * third, finish.Length) * 11, "the finish sound should fall in pitch");

        // A glide, not steps: the pitch of each tenth is at or above the one before, never a jump of more than a tenth's share.
        var tenth = start.Length / 10;
        for (var n = 2; n < 9; n++)
        {
            var before = Crossings(start, (n - 1) * tenth, n * tenth);
            var here = Crossings(start, n * tenth, (n + 1) * tenth);
            Assert.InRange(here - before, -1, 6);
        }

        Assert.Equal(Peak(start), Peak(finish), 200.0);
    }

    [Fact]
    public void Every_sweep_starts_and_ends_at_nothing_and_swells_and_fades_slowly()
    {
        byte[][] sounds = [AmbientSounds.BackgroundStart(), AmbientSounds.BackgroundFinish(), .. Enumerable.Range(1, AmbientSounds.MostLayers).Select(AmbientSounds.BackgroundPulse)];
        foreach (var wav in sounds)
        {
            var samples = SamplesOf(wav);
            var peak = Peak(samples);
            var edge = 5 * Rate / 1000;

            // No click: silence at both ends, and the first and last 5 ms far below the loudest sample.
            Assert.Equal(0, samples[0]);
            Assert.InRange(Math.Abs((int)samples[^1]), 0, 2);
            Assert.True(Peak(samples, 0, edge) < peak / 20, "a sweep should start from nothing");
            Assert.True(Peak(samples, samples.Length - edge, samples.Length) < peak / 20, "a sweep should end in nothing");

            // No attack: a tenth of the way in, the sound is still under a third of its loudest, and so again at the end.
            Assert.True(Peak(samples, 0, samples.Length / 10) < peak / 3, "a sweep should swell slowly");
            Assert.True(Peak(samples, samples.Length - samples.Length / 10, samples.Length) < peak / 3, "a sweep should fade slowly");

            // Heard, and still gentle: nothing near clipping, and under the chimes (0.35 of full scale).
            Assert.InRange(peak, 0.15 * short.MaxValue, AmbientSounds.SweepLimit * short.MaxValue);
        }
    }

    [Fact]
    public void The_recurring_sound_is_one_slow_breath_fuller_with_each_thing_and_never_longer()
    {
        var one = SamplesOf(AmbientSounds.BackgroundPulse(1));
        Assert.Equal(AmbientSounds.PulseMilliseconds * Rate / 1000, one.Length);
        Assert.InRange(AmbientSounds.PulseMilliseconds, 4500, 5000);

        // Pronounced: one voice alone peaks near 0.17 of full scale (the version that went unheard: 0.05).
        Assert.InRange(Peak(one), 0.15 * short.MaxValue, 0.19 * short.MaxValue);
        var apart = 100 * Rate / 1000;
        var energy = Energy(one);
        for (var layers = 1; layers <= AmbientSounds.MostLayers; layers++)
        {
            var samples = SamplesOf(AmbientSounds.BackgroundPulse(layers));

            // A process, not an event: the same length whatever runs; the level rises through the first three
            // tenths with no onset, stays near its fullest through the middle four, and sinks through the last three.
            Assert.Equal(one.Length, samples.Length);
            var tenth = samples.Length / 10;
            var tenths = Enumerable.Range(0, 10).Select(n => Energy(samples[(n * tenth)..((n + 1) * tenth)])).ToArray();
            var fullest = tenths.Max();
            Assert.True(tenths[0] < tenths[1] && tenths[1] < tenths[2] && tenths[2] < tenths[3], $"{layers} layers: the breath should rise");
            Assert.True(tenths[6] > tenths[7] && tenths[7] > tenths[8] && tenths[8] > tenths[9], $"{layers} layers: the breath should sink");
            Assert.True(tenths[0] < fullest / 20 && tenths[9] < fullest / 20, $"{layers} layers: the ends should be all but silent");
            for (var n = 3; n < 7; n++)
            {
                Assert.True(tenths[n] > fullest * 0.7, $"{layers} layers: tenth {n} should be full");
            }

            if (layers == 1)
            {
                continue;
            }

            // Fuller with every layer, and under the chimes' level at its loudest.
            var more = Energy(samples);
            Assert.True(more > energy * 1.005, $"{layers} layers should carry more than {layers - 1}");
            energy = more;
            Assert.True(Peak(samples) > Peak(one), $"{layers} layers should be fuller than one");
            Assert.InRange(Peak(samples), 0, 0.31 * short.MaxValue);

            // The first layer of several is the one breath itself, only quieter: where it sounds alone, the same shape.
            var scale = (double)Peak(samples, 0, apart) / Peak(one, 0, apart);
            Assert.InRange(scale, 0.5, 1.0);
            for (var i = 0; i < apart; i += 7)
            {
                Assert.Equal(one[i] * scale, samples[i], 3.0);
            }
        }

        // More than five things sound as five, and none is never asked for: the count is clamped.
        Assert.Equal(AmbientSounds.BackgroundPulse(5), AmbientSounds.BackgroundPulse(9));
        Assert.Equal(AmbientSounds.BackgroundPulse(1), AmbientSounds.BackgroundPulse(0));

        // A small drift up into the pitch while it rises, and the pitch held from then on: C3, 131 Hz, low.
        var fifth = one.Length / 5;
        var rising = Crossings(one, 0, fifth);
        var held = Crossings(one, 3 * fifth, 4 * fifth);
        Assert.InRange(held - rising, 2, held / 10);
        Assert.InRange(held, 2 * 129 * fifth / Rate, 2 * 133 * fifth / Rate);

        // The layers above are higher: five have more crossings at the middle than one.
        var five = SamplesOf(AmbientSounds.BackgroundPulse(5));
        Assert.True(Crossings(five, 2 * fifth, 3 * fifth) > Crossings(one, 2 * fifth, 3 * fifth));
    }

    [Fact]
    public void A_swell_nests_its_layers_inside_the_first()
    {
        var one = SamplesOf(WaveTone.Swell(0.2, 1000, 100, 0.4, 1, (300.0, 0.0)));
        var two = SamplesOf(WaveTone.Swell(0.2, 1000, 100, 0.4, 1, (300.0, 0.0), (450.0, 1.0)));
        Assert.Equal(Rate, one.Length);
        Assert.Equal(one.Length, two.Length);
        Assert.Equal(0, one[0]);
        Assert.InRange(Peak(one), 0.18 * short.MaxValue, 0.27 * short.MaxValue);

        // Held at the full level for two fifths of the length: every 50 ms from 300 to 700 ms is as loud as the loudest.
        for (var from = 300; from < 700; from += 50)
        {
            Assert.True(Peak(one, from * Rate / 1000, (from + 50) * Rate / 1000) > Peak(one) * 0.9, $"full at {from} ms");
        }

        // No hold at all is still a sound, and a whole hold does not divide by nothing.
        Assert.Equal(one.Length, SamplesOf(WaveTone.Swell(0.2, 1000, 100, 0, 1, (300.0, 0.0))).Length);
        Assert.InRange(Peak(SamplesOf(WaveTone.Swell(0.2, 1000, 100, 1, 1, (300.0, 0.0)))), 0.18 * short.MaxValue, 0.27 * short.MaxValue);

        // A layer's phase is its tone's phase at the middle of the sound: at 0 the middle sample is all but silent,
        // at a quarter turn it is the loudest.
        Assert.InRange(Math.Abs((int)one[one.Length / 2]), 0, Peak(one) / 10);
        var quarter = SamplesOf(WaveTone.Swell(0.2, 1000, 100, 0.4, 1, (300.0, Math.PI / 2)));
        Assert.InRange(Math.Abs((int)quarter[quarter.Length / 2]), Peak(quarter) * 0.7, Peak(quarter));

        // The second layer enters 100 ms in and has left 100 ms before the end: outside that, two are one, only quieter.
        var apart = 100 * Rate / 1000;
        var scale = Math.Pow(2, -0.3);
        for (var i = 0; i < apart; i += 5)
        {
            Assert.Equal(one[i] * scale, two[i], 3.0);
            Assert.Equal(one[^(i + 1)] * scale, two[^(i + 1)], 3.0);
        }

        Assert.True(Energy(two) > Energy(one));
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
