using AxKit.Core.Audio;

namespace AxKit.Tests;

/// <summary>The user's own sounds (docs/axKit/SPEC.md AX-7.12): a WAV file of a sound's name in place of the built-in one, any common shape made into the bundle's.</summary>
public sealed class CustomSoundsTests
{
    /// <summary>A WAV file of the given shape and frames, each frame's channels the same sample value.</summary>
    private static byte[] Wav(int format, int channels, int rate, int bits, params double[] frames)
    {
        var bytesPerSample = bits / 8;
        var data = new byte[frames.Length * channels * bytesPerSample];
        for (var frame = 0; frame < frames.Length; frame++)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                var at = (frame * channels + channel) * bytesPerSample;
                var value = frames[frame];
                switch (bits)
                {
                    case 8: data[at] = (byte)Math.Round(value * 127 + 128); break;
                    case 16: BitConverter.TryWriteBytes(data.AsSpan(at), (short)Math.Round(value * 32767)); break;
                    case 24:
                        var three = (int)Math.Round(value * 8388607);
                        data[at] = (byte)three;
                        data[at + 1] = (byte)(three >> 8);
                        data[at + 2] = (byte)(three >> 16);
                        break;
                    case 32 when format == 3: BitConverter.TryWriteBytes(data.AsSpan(at), (float)value); break;
                    default: BitConverter.TryWriteBytes(data.AsSpan(at), (int)Math.Round(value * int.MaxValue)); break;
                }
            }
        }

        var file = new List<byte>();
        file.AddRange("RIFF"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(36 + data.Length));
        file.AddRange("WAVE"u8.ToArray());
        file.AddRange("fmt "u8.ToArray());
        file.AddRange(BitConverter.GetBytes(16));
        file.AddRange(BitConverter.GetBytes((short)format));
        file.AddRange(BitConverter.GetBytes((short)channels));
        file.AddRange(BitConverter.GetBytes(rate));
        file.AddRange(BitConverter.GetBytes(rate * channels * bytesPerSample));
        file.AddRange(BitConverter.GetBytes((short)(channels * bytesPerSample)));
        file.AddRange(BitConverter.GetBytes((short)bits));
        file.AddRange("data"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(data.Length));
        file.AddRange(data);
        return [.. file];
    }

    private static short[] Samples(byte[] wav) =>
        [.. Enumerable.Range(0, (wav.Length - WaveTone.HeaderLength) / 2).Select(i => BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2))];

    [Fact]
    public void A_file_of_the_bundles_shape_is_taken_as_it_is()
    {
        var converted = CustomSounds.Convert(Wav(1, 1, WaveTone.SampleRate, 16, 0.5, -0.5, 0.25), out var problem);
        Assert.Null(problem);
        Assert.Equal([16384, -16384, 8192], Samples(converted!).Select(s => (int)Math.Round(s / 2.0) * 2));
    }

    [Theory]
    [InlineData(1, 2, 8)]
    [InlineData(1, 2, 24)]
    [InlineData(1, 1, 32)]
    [InlineData(3, 2, 32)]
    public void Other_shapes_are_mixed_to_one_channel(int format, int channels, int bits)
    {
        var converted = CustomSounds.Convert(Wav(format, channels, WaveTone.SampleRate, bits, 0.5, 0.5), out var problem);
        Assert.Null(problem);
        Assert.All(Samples(converted!), sample => Assert.InRange(sample, 15800, 16700));
    }

    [Fact]
    public void Another_rate_is_made_into_the_bundles()
    {
        var converted = CustomSounds.Convert(Wav(1, 1, WaveTone.SampleRate / 2, 16, 0, 0.5, 0, 0.5), out _);
        Assert.Equal(8, Samples(converted!).Length);
    }

    [Fact]
    public void What_is_no_sound_is_passed_over_and_the_built_in_one_stays()
    {
        Assert.Null(CustomSounds.Convert("not a wav"u8.ToArray(), out var notWav));
        Assert.Contains("not a WAV", notWav, StringComparison.Ordinal);
        Assert.Null(CustomSounds.Convert(Wav(2, 1, 44100, 4, 0), out var shape));
        Assert.Contains("shape", shape, StringComparison.Ordinal);
        Assert.Null(CustomSounds.Convert(Wav(1, 1, 1000, 16, new double[(CustomSounds.MaxSeconds + 1) * 1000]), out var tooLong));
        Assert.Contains("longer", tooLong, StringComparison.Ordinal);

        using var temp = new TempFolder("custom-sounds-tests");
        byte[] made = [1, 2, 3];
        var problems = new List<string>();
        Assert.Same(made, CustomSounds.Or(temp.Path, "edge", made, problems.Add));
        File.WriteAllText(Path.Combine(temp.Path, "edge.wav"), "nonsense");
        Assert.Same(made, CustomSounds.Or(temp.Path, "edge", made, problems.Add));
        Assert.Single(problems);
        File.WriteAllBytes(Path.Combine(temp.Path, "edge.wav"), Wav(1, 1, WaveTone.SampleRate, 16, 0.1));
        Assert.NotSame(made, CustomSounds.Or(temp.Path, "edge", made, problems.Add));
    }

    [Fact]
    public void The_names_file_lists_every_name()
    {
        var text = CustomSounds.NamesText();
        Assert.All(CustomSounds.Names, name => Assert.Contains(name.Name + ".wav: ", text, StringComparison.Ordinal));
        Assert.Equal(CustomSounds.Names.Count, CustomSounds.Names.Select(name => name.Name).Distinct().Count());
    }
}
