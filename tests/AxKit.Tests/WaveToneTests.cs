using System.Text;
using AxKit.Core.Audio;

namespace AxKit.Tests;

public class WaveToneTests
{
    [Fact]
    public void Notes_produce_a_valid_pcm_wav_of_the_requested_length()
    {
        // Lengths that are whole sample counts at 22050 Hz (40 ms is 882 samples; 50 ms would not be whole).
        var wav = WaveTone.Notes(0.5, (440, 100), (0, 40), (880, 60));

        var samples = 200 * WaveTone.SampleRate / 1000;
        Assert.Equal(WaveTone.HeaderLength + samples * 2, wav.Length);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal(wav.Length - 8, BitConverter.ToInt32(wav, 4));
        Assert.Equal("WAVEfmt ", Encoding.ASCII.GetString(wav, 8, 8));
        Assert.Equal(16, BitConverter.ToInt32(wav, 16));
        Assert.Equal(1, BitConverter.ToInt16(wav, 20));
        Assert.Equal(1, BitConverter.ToInt16(wav, 22));
        Assert.Equal(WaveTone.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal(WaveTone.SampleRate * 2, BitConverter.ToInt32(wav, 28));
        Assert.Equal(2, BitConverter.ToInt16(wav, 32));
        Assert.Equal(16, BitConverter.ToInt16(wav, 34));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
        Assert.Equal(samples * 2, BitConverter.ToInt32(wav, 40));
    }

    [Fact]
    public void Notes_stay_within_the_amplitude_and_a_pause_is_silent()
    {
        var wav = WaveTone.Notes(0.5, (440, 100), (0, 40));
        var limit = (int)(0.5 * short.MaxValue) + 1;
        var loudest = 0;
        for (var i = WaveTone.HeaderLength; i < wav.Length; i += 2)
        {
            loudest = Math.Max(loudest, Math.Abs((int)BitConverter.ToInt16(wav, i)));
        }

        Assert.InRange(loudest, limit / 2, limit);

        var pauseStart = WaveTone.HeaderLength + 100 * WaveTone.SampleRate / 1000 * 2;
        for (var i = pauseStart; i < wav.Length; i += 2)
        {
            Assert.Equal(0, BitConverter.ToInt16(wav, i));
        }
    }

    [Fact]
    public void Strikes_die_away_one_after_the_other()
    {
        var wav = WaveTone.Strikes(0.3, 6, 0.5, 3000, 2000);
        var samples = (wav.Length - WaveTone.HeaderLength) / 2;
        var each = 6 * WaveTone.SampleRate / 1000;
        Assert.Equal(2 * each, samples);

        int Peak(int from, int to)
        {
            var peak = 0;
            for (var i = from; i < to; i++)
            {
                peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2)));
            }

            return peak;
        }

        // Each strike is loud at its start and all but gone at its end; the second starts afresh once the first has faded.
        Assert.True(Peak(0, each / 4) > Peak(each * 3 / 4, each) * 4, "the first strike should die away");
        Assert.True(Peak(each, each + each / 4) > Peak(each * 3 / 4, each) * 4, "the second strike should start afresh");
        Assert.True(Peak(each, each + each / 4) > Peak(2 * each - each / 4, 2 * each) * 4, "the second strike should die away");
        Assert.InRange(Peak(0, samples), 0.1 * short.MaxValue, 0.3 * short.MaxValue + 1);

        // The attack: the second sample is still far below the peak (a sound that starts at full volume clicks), and the first is silent.
        Assert.Equal(0, BitConverter.ToInt16(wav, WaveTone.HeaderLength));
        var second = Math.Abs((int)BitConverter.ToInt16(wav, WaveTone.HeaderLength + 2));
        Assert.True(second < Peak(0, each / 4) / 4, $"{second} should be well below the peak");
    }

    [Fact]
    public void Swoops_glide_from_one_pitch_to_the_other_and_start_afresh()
    {
        var wav = WaveTone.Swoops(0.5, 2400, 900, 80, 3);
        var each = 80 * WaveTone.SampleRate / 1000;
        Assert.Equal(3 * each, (wav.Length - WaveTone.HeaderLength) / 2);

        int At(int i) => BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2);

        // The pitch by the zero crossings: many in a swoop's first quarter, far fewer in its last, and again in the next swoop.
        int Crossings(int from, int to)
        {
            var count = 0;
            for (var i = from + 1; i < to; i++)
            {
                if ((At(i - 1) < 0) != (At(i) < 0))
                {
                    count++;
                }
            }

            return count;
        }

        for (var n = 0; n < 3; n++)
        {
            var start = n * each;
            Assert.True(Crossings(start, start + each / 4) > Crossings(start + each * 3 / 4, start + each) * 3 / 2, $"swoop {n} should fall in pitch");
            // Each swoop starts and ends in silence, so that the jump back up does not click.
            Assert.True(Math.Abs(At(start)) < 0.05 * short.MaxValue, $"swoop {n} should start quietly");
            Assert.True(Math.Abs(At(start + each - 1)) < 0.05 * short.MaxValue, $"swoop {n} should end quietly");
        }

        var peak = 0;
        for (var i = 0; i < 3 * each; i++)
        {
            peak = Math.Max(peak, Math.Abs(At(i)));
        }

        Assert.InRange(peak, 0.4 * short.MaxValue, 0.5 * short.MaxValue + 1);
    }

    [Fact]
    public void The_tool_pop_is_the_one_the_user_chose_by_ear()
    {
        // The script that played it to him (2026-11-15, kind J): ToolSynth.Pop(700.0, 300.0, 0.006, 0.030) at a peak of 0.3.
        var count = (int)(WaveTone.SampleRate * 0.030);
        var mix = new double[count];
        var phase = 0.0;
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)WaveTone.SampleRate;
            var pitch = 300.0 + (700.0 - 300.0) * Math.Exp(-t / 0.006);
            phase += 2 * Math.PI * pitch / WaveTone.SampleRate;
            var attack = Math.Min(1.0, t / 0.0015);
            var body = Math.Exp(-t / (0.030 / 4));
            var release = Math.Min(1.0, (0.030 - t) / 0.004);
            mix[i] = attack * body * release * (Math.Sin(phase) + 0.08 * Math.Sin(2 * phase));
        }

        var max = 1e-9;
        foreach (var x in mix)
        {
            max = Math.Max(max, Math.Abs(x));
        }

        var scale = 0.3 / max;
        var wav = WaveTone.Pop(0.3, 700, 300, 0.006, 0.030);
        Assert.Equal(WaveTone.HeaderLength + 2 * count, wav.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal((short)Math.Round(mix[i] * scale * 32767), BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2));
        }
    }

    [Fact]
    public void A_pop_is_short_starts_and_ends_at_nothing_and_falls_in_pitch()
    {
        var wav = WaveTone.Pop(0.3, 700, 300, 0.006, 0.030);
        var samples = (wav.Length - WaveTone.HeaderLength) / 2;

        int At(int i) => BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2);

        // Not elongated: 30 ms, 661 samples, no tail.
        Assert.Equal(661, samples);

        var peak = 0;
        for (var i = 0; i < samples; i++)
        {
            peak = Math.Max(peak, Math.Abs(At(i)));
        }

        Assert.Equal(0.3 * short.MaxValue, peak, 1.5);

        // No click: silence at the first sample, the last sample all but silent, the loudest within the first quarter.
        Assert.Equal(0, At(0));
        Assert.True(Math.Abs(At(samples - 1)) < peak / 50, "the pop should end at nothing");

        // The pitch falls: the time between two rising zero crossings grows from the start to the end.
        var rising = new List<int>();
        for (var i = 1; i < samples; i++)
        {
            if (At(i - 1) < 0 && At(i) >= 0)
            {
                rising.Add(i);
            }
        }

        Assert.True(rising.Count >= 6, "a pop should have several cycles");
        var first = rising[1] - rising[0];
        var last = rising[^1] - rising[^2];
        Assert.True(last > first * 3 / 2, $"the pitch should fall: {first} samples a cycle at the start, {last} at the end");

        // Near 700 Hz at the start (about 31 samples a cycle) and near 300 Hz at the end (about 73).
        Assert.InRange(first, 25, 45);
        Assert.InRange(last, 60, 80);
    }
}
