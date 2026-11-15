using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The estimate of whether NVDA may still be reading reply text the window spoke (FR-7.4b): a question that opens
/// meanwhile cuts it off, whatever Go to them at once says; once the estimate has run out, nothing is cut.
/// </summary>
public class LeftoverSpeechTests
{
    [Fact]
    public void Reply_text_counts_as_running_for_its_length_and_no_longer()
    {
        // 100 characters spoken at 10 000 ms: over at 16 000 ms.
        var ends = LeftoverSpeech.EndsAt(now: 10_000, endsBefore: 0, length: 100);
        Assert.Equal(10_000 + (100 * LeftoverSpeech.MsPerCharacter), ends);
        Assert.True(LeftoverSpeech.Running(10_000, ends));
        // The question of the recording: it opened about three seconds after the reply was spoken.
        Assert.True(LeftoverSpeech.Running(13_400, ends));
        Assert.False(LeftoverSpeech.Running(ends, ends));
        Assert.False(LeftoverSpeech.Running(60_000, ends));
    }

    [Fact]
    public void Nothing_spoken_is_nothing_running()
    {
        Assert.False(LeftoverSpeech.Running(0, 0));
        Assert.False(LeftoverSpeech.Running(5_000, 0));
    }

    [Fact]
    public void A_second_utterance_starts_after_the_first_while_that_one_runs()
    {
        var first = LeftoverSpeech.EndsAt(now: 1_000, endsBefore: 0, length: 50);
        // Handed over while the first is still read: it queues behind it.
        Assert.Equal(first + (20 * LeftoverSpeech.MsPerCharacter), LeftoverSpeech.EndsAt(now: 2_000, endsBefore: first, length: 20));
        // Handed over after the first has ended: it starts now.
        Assert.Equal(90_000 + (20 * LeftoverSpeech.MsPerCharacter), LeftoverSpeech.EndsAt(now: 90_000, endsBefore: first, length: 20));
    }

    [Fact]
    public void A_long_reply_counts_as_running_for_half_a_minute_at_most()
    {
        var ends = LeftoverSpeech.EndsAt(now: 1_000, endsBefore: 0, length: 20_000);
        Assert.Equal(1_000 + LeftoverSpeech.LongestMs, ends);
        Assert.Equal(5_000 + LeftoverSpeech.LongestMs, LeftoverSpeech.EndsAt(now: 5_000, endsBefore: ends, length: 20_000));
    }
}
