using AxKit.Core.Audio;

namespace AxKit.Tests;

/// <summary>
/// The sounds of a way of working starting (docs/axKit/SPEC.md AX-7.11), as the maintainer chose them by ear on
/// 2026-11-22: their lengths, that the four differ, and that each begins with the mark and holds sound to its end.
/// </summary>
public sealed class OpeningSoundsTests
{
    private static int Samples(byte[] wav) => (wav.Length - WaveTone.HeaderLength) / 2;

    private static short At(byte[] wav, int sample) => BitConverter.ToInt16(wav, WaveTone.HeaderLength + (2 * sample));

    private static int Loudest(byte[] wav, double fromSeconds, double toSeconds)
    {
        var loudest = 0;
        for (var i = (int)(fromSeconds * WaveTone.SampleRate); i < Math.Min(Samples(wav), (int)(toSeconds * WaveTone.SampleRate)); i++)
        {
            loudest = Math.Max(loudest, Math.Abs((int)At(wav, i)));
        }

        return loudest;
    }

    [Fact]
    public void Each_sound_has_the_length_that_was_chosen()
    {
        Assert.Equal((int)Math.Round(WaveTone.SampleRate * 0.44), Samples(OpeningSounds.Field()));
        Assert.Equal((int)Math.Round(WaveTone.SampleRate * 0.44), Samples(OpeningSounds.Text()));
        Assert.Equal((int)Math.Round(WaveTone.SampleRate * 0.40), Samples(OpeningSounds.List()));
        Assert.Equal((int)Math.Round(WaveTone.SampleRate * 0.70), Samples(OpeningSounds.Form()));
    }

    [Fact]
    public void The_four_differ_and_are_the_same_every_time()
    {
        byte[][] sounds = [OpeningSounds.Form(), OpeningSounds.List(), OpeningSounds.Field(), OpeningSounds.Text()];
        for (var i = 0; i < sounds.Length; i++)
        {
            for (var j = i + 1; j < sounds.Length; j++)
            {
                Assert.False(sounds[i].AsSpan().SequenceEqual(sounds[j]), $"Sounds {i} and {j} are the same.");
            }
        }

        Assert.True(OpeningSounds.Form().AsSpan().SequenceEqual(OpeningSounds.Form()));
    }

    [Fact]
    public void Each_begins_with_the_mark_and_then_plays_its_kind()
    {
        foreach (var wav in new[] { OpeningSounds.Form(), OpeningSounds.List(), OpeningSounds.Field(), OpeningSounds.Text() })
        {
            // The mark, three beeps within the first tenth of a second, and the kind's own part after 0.14 s.
            Assert.True(Loudest(wav, 0.0, 0.03) > 2000, "No first beep of the mark.");
            Assert.True(Loudest(wav, 0.07, 0.10) > 2000, "No third beep of the mark.");
            Assert.True(Loudest(wav, 0.14, 0.20) > 2000, "Nothing after the mark.");
        }

        // The form goes on where the others have ended: its second tone begins 0.43 s in.
        Assert.True(Loudest(OpeningSounds.Form(), 0.43, 0.50) > 2000);
    }
}
