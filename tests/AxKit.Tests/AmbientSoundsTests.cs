using AxKit.Core.Audio;

namespace AxKit.Tests;

/// <summary>
/// The sounds of background work and the working music (axClaude FR-7.10, FR-7.9): what can be said of a sound
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
            // Under the chimes (0.35), about the recurring chords' (0.22 to 0.30): the loudest sample is 0.24 of full scale.
            Assert.Equal(AmbientSounds.FlourishPeak * short.MaxValue, Peak(samples), 1.5);

            // No click at either end: it starts at nothing, a note is cut where it has died away to under a
            // fiftieth of the loudest sample, and the last fifth of a second is silence.
            Assert.Equal(0, samples[0]);
            Assert.True(Peak(samples, 0, Rate / 1000) < Peak(samples) / 3, "a note should rise over some milliseconds");
            Assert.True(Peak(samples, 500 * Rate / 1000, 530 * Rate / 1000) < Peak(samples) / 50, "the last note should have died away where it is cut");
            Assert.Equal(0, Peak(samples, 550 * Rate / 1000, samples.Length));
        }
    }

    /// <summary>
    /// The chord synthesis of the script that played the recurring sound to the user on 2026-11-15
    /// (chords.ps1, its class ChordSynth, copied as it was), the independent reference the app's chords are held to.
    /// </summary>
    private static class ChordSynth
    {
        public const int Rate = 22050;

        public static double[] Render(double[] tones, double seconds, double bright, double ping)
        {
            int count = (int)(Rate * seconds);
            double[] dry = new double[count];
            if (ping > 0)
            {
                double at = 0.9;
                for (int i = (int)(at * Rate); i < count; i++)
                {
                    double t = i / (double)Rate - at;
                    double rise = Math.Min(1.0, t / 0.03);
                    dry[i] += 0.22 * rise * Math.Exp(-t / 0.7) * (Math.Sin(2 * Math.PI * ping * t) + 0.15 * Math.Sin(4 * Math.PI * ping * t));
                }
            }
            double attack = 0.7, hold = 1.5, release = 1.2;
            for (int v = 0; v < tones.Length; v++)
            {
                double f = tones[v];
                double level = 1.0 / Math.Pow(v + 1, 0.35);
                double start = 0.12 * v;
                for (int i = (int)(start * Rate); i < count; i++)
                {
                    double t = i / (double)Rate - start;
                    double env = t < attack ? 0.5 - 0.5 * Math.Cos(Math.PI * t / attack)
                        : t < attack + hold ? 1.0
                        : Math.Exp(-(t - attack - hold) / (release / 3.0));
                    double a = 2 * Math.PI * f * t, b = 2 * Math.PI * (f * 1.003) * t;
                    double tone = 0.5 * (Math.Sin(a) + Math.Sin(b))
                        + bright * (0.22 * Math.Sin(2 * a) + 0.10 * Math.Sin(3 * b) + 0.04 * Math.Sin(4 * a));
                    dry[i] += level * env * tone;
                }
            }

            int[] combs = { 1103, 1277, 1409, 1559 };
            double[] wet = new double[count];
            foreach (int delay in combs)
            {
                double[] line = new double[delay];
                int at = 0;
                double damp = 0;
                for (int i = 0; i < count; i++)
                {
                    double outSample = line[at];
                    damp = 0.7 * damp + 0.3 * outSample;
                    line[at] = dry[i] + 0.86 * damp;
                    at = (at + 1) % delay;
                    wet[i] += outSample * 0.25;
                }
            }
            foreach (int delay in new[] { 347, 113 })
            {
                double[] line = new double[delay];
                int at = 0;
                for (int i = 0; i < count; i++)
                {
                    double buffered = line[at];
                    double input = wet[i];
                    wet[i] = buffered - 0.5 * input;
                    line[at] = input + 0.5 * buffered;
                    at = (at + 1) % delay;
                }
            }
            double[] mix = new double[count];
            int fade = (int)(Rate * 0.4);
            for (int i = 0; i < count; i++)
            {
                double tail = i > count - fade ? (count - i) / (double)fade : 1.0;
                mix[i] = (0.6 * dry[i] + 0.8 * wet[i]) * tail;
            }
            return mix;
        }

        /// <summary>The script's Wav without its header: the samples scaled to the peak and rounded.</summary>
        public static short[] Samples(double[] samples, double peak)
        {
            double max = 1e-9;
            foreach (double s in samples)
            {
                max = Math.Max(max, Math.Abs(s));
            }

            double scale = peak / max;
            return [.. samples.Select(s => (short)Math.Round(s * scale * 32767))];
        }
    }

    // The script's $book and $accents, typed out again from it: each chord's tones from the bottom up, and its accent.
    private static readonly Dictionary<string, (double[] Tones, double Accent)> Book = new()
    {
        ["C"] = ([261.63, 392.00, 329.63, 493.88, 587.33, 783.99], 783.99),
        ["Am"] = ([220.00, 329.63, 261.63, 392.00, 493.88, 659.26], 1318.51),
        ["F"] = ([349.23, 523.25, 440.00, 659.26, 783.99, 1046.50], 880.00),
        ["G"] = ([392.00, 587.33, 493.88, 659.26, 880.00, 1174.66], 1174.66),
    };

    /// <summary>What the script played for a chord and a number of things, at the peak the user asked for after it ("yes, but softer": 0.22 where he heard 0.28).</summary>
    private static short[] ChordAsChosen(string name, int things)
    {
        var (tones, accent) = Book[name];
        return ChordSynth.Samples(ChordSynth.Render(tones[..(things + 1)], 4.6, 1.0, accent), 0.22 * Math.Pow(things, 0.2));
    }

    [Theory]
    [InlineData("Am", 1)]
    [InlineData("G", 1)]
    [InlineData("F", 3)]
    [InlineData("C", 3)]
    [InlineData("G", 5)]
    public void The_recurring_chords_are_the_ones_the_user_chose_by_ear(string name, int things)
    {
        var chord = AmbientSounds.ChordNames.ToList().IndexOf(name);
        Assert.Equal(ChordAsChosen(name, things), SamplesOf(AmbientSounds.BackgroundChord(chord, things)));
    }

    [Fact]
    public void A_chord_lasts_four_seconds_and_six_tenths_fades_to_nothing_and_is_fuller_with_more_things()
    {
        Assert.Equal(["C", "Am", "F", "G"], AmbientSounds.ChordNames);
        Assert.Equal(4.6, AmbientSounds.ChordSeconds);
        Assert.Equal(5, AmbientSounds.MostThings);
        for (var chord = 0; chord < AmbientSounds.ChordNames.Count; chord++)
        {
            short[] before = [];
            for (var things = 1; things <= AmbientSounds.MostThings; things++)
            {
                var samples = SamplesOf(AmbientSounds.BackgroundChord(chord, things));
                Assert.Equal((int)(Rate * 4.6), samples.Length);

                // The peak the user asked for: 0.22 for one thing, n to the power of 0.2 times it for n (0.30 for five).
                Assert.Equal(0.22 * Math.Pow(things, 0.2) * short.MaxValue, Peak(samples), 1.5);

                // No click: it starts at nothing, rises over a few tenths, and the last sample is nothing.
                Assert.Equal(0, samples[0]);
                Assert.True(Peak(samples, 0, Rate / 100) < Peak(samples) / 20, "a chord should rise slowly");
                Assert.InRange(Math.Abs((int)samples[^1]), 0, 1);

                // The tail fades: the last half second is far quieter than the first two seconds.
                Assert.True(Peak(samples, samples.Length - Rate / 2, samples.Length) < Peak(samples, 0, 2 * Rate) / 4, "the tail should fade");

                // Fuller: every further thing adds the chord's next tone on top. Heard by its upper sine, 0.3% above it,
                // which no tone below has (the tone itself can be the second harmonic of one below: E5 over E4 in A minor).
                if (things > 1)
                {
                    var added = 1.003 * Book[AmbientSounds.ChordNames[chord]].Tones[things];
                    Assert.True(Strength(samples, Rate, 3 * Rate, added) > 3 * Strength(before, Rate, 3 * Rate, added), $"chord {chord}, {things} things: the tone {added} should be added");
                }

                before = samples;
            }
        }

        // More than five things sound as five, and none is never asked for: the count is clamped.
        Assert.Equal(AmbientSounds.BackgroundChord(2, 5), AmbientSounds.BackgroundChord(2, 9));
        Assert.Equal(AmbientSounds.BackgroundChord(2, 1), AmbientSounds.BackgroundChord(2, 0));
    }

    /// <summary>The chords are made off the UI thread at their first use (rule 3): this measures how long one takes, for the notes; it fails only if one takes a whole second.</summary>
    [Fact]
    public void A_chord_is_made_in_well_under_a_second()
    {
        AmbientSounds.BackgroundChord(0, 1);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        AmbientSounds.BackgroundChord(3, 5);
        Assert.InRange(watch.ElapsedMilliseconds, 0, 1000);
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
        foreach (var (name, things) in new[] { ("C", 1), ("Am", 1), ("F", 3), ("G", 5) })
        {
            var chord = AmbientSounds.ChordNames.ToList().IndexOf(name);
            File.WriteAllBytes(Path.Combine(folder, $"background-chord-{name}-{things}.wav"), AmbientSounds.BackgroundChord(chord, things));
        }

        File.WriteAllBytes(Path.Combine(folder, "tool-pop.wav"), WaveTone.Pop(0.3, 700, 300, 0.006, 0.030));

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
