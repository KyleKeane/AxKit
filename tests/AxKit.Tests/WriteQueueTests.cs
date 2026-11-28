using AxKit.Core.Pty;

namespace AxKit.Tests;

/// <summary>
/// The paced queue of a console window's writes (<see cref="WriteQueue"/>, docs/axClaude/SPEC.md D33): what each
/// answer of a write's check does, run here so that CI holds it (the sixth review of 2026-11-27, L7: until then the
/// decision sat in the window's pump, which only window scripts ran).
/// </summary>
public sealed class WriteQueueTests
{
    [Fact]
    public void A_write_without_a_check_goes_with_its_pause_and_an_empty_queue_is_idle()
    {
        var queue = new WriteQueue();
        Assert.Equal(WriteStepKind.Idle, queue.Next().Kind);
        queue.Add("hi", 100, check: null);
        Assert.Equal(new WriteStep(WriteStepKind.Write, "hi", 100), queue.Next());
        Assert.Equal(0, queue.Count);
        Assert.Equal(WriteStepKind.Idle, queue.Next().Kind);
    }

    /// <summary>The fifth review of 2026-11-27, M1: "not yet" keeps the write first and everything behind it waits, in order; nothing is dropped.</summary>
    [Fact]
    public void Not_yet_keeps_the_write_first_and_holds_everything_behind_it()
    {
        var queue = new WriteQueue();
        var answer = WriteCheck.NotYet;
        queue.Add("\r", 50, () => answer);
        queue.Add("second", 30, check: null);
        Assert.Equal(new WriteStep(WriteStepKind.Wait, string.Empty, WriteQueue.NotYetMs), queue.Next());
        Assert.Equal(new WriteStep(WriteStepKind.Wait, string.Empty, WriteQueue.NotYetMs), queue.Next());
        Assert.Equal(2, queue.Count);
        answer = WriteCheck.Goes;
        Assert.Equal(new WriteStep(WriteStepKind.Write, "\r", 50), queue.Next());
        Assert.Equal(new WriteStep(WriteStepKind.Write, "second", 30), queue.Next());
    }

    [Fact]
    public void Over_drops_the_write_and_everything_behind_it_and_says_so()
    {
        var queue = new WriteQueue();
        queue.Add("first", 30, check: null);
        queue.Add("\r", 50, () => WriteCheck.Over);
        queue.Add("next", 30, check: null);
        Assert.Equal("first", queue.Next().Text);
        Assert.Equal(WriteStepKind.Dropped, queue.Next().Kind);
        Assert.Equal(0, queue.Count);
        Assert.Equal(WriteStepKind.Idle, queue.Next().Kind);
    }

    [Fact]
    public void Skip_drops_that_write_alone_and_the_next_is_asked_at_once()
    {
        var queue = new WriteQueue();
        queue.Add("\r", 50, () => WriteCheck.Skip);
        queue.Add("next", 30, check: null);
        Assert.Equal(new WriteStep(WriteStepKind.Write, "next", 30), queue.Next());
    }

    /// <summary>A write with an empty text is a point to wait at (axClaude: until Claude Code has taken a message): it is never written, whatever its check says.</summary>
    [Fact]
    public void An_empty_write_is_a_point_to_wait_at_and_is_never_written()
    {
        var queue = new WriteQueue();
        var answer = WriteCheck.NotYet;
        queue.Add(string.Empty, 0, () => answer);
        queue.Add("next", 30, check: null);
        Assert.Equal(WriteStepKind.Wait, queue.Next().Kind);
        answer = WriteCheck.Goes;
        Assert.Equal(new WriteStep(WriteStepKind.Write, "next", 30), queue.Next());
    }

    /// <summary>
    /// A check that throws never stops the pump (the seventh review of 2026-11-27, L2): the queue is dropped, the step
    /// says it failed and names the exception's type alone, and what is queued afterwards is written. Until then the
    /// exception left the window's pump marked as writing, and nothing queued after it ever went.
    /// </summary>
    [Fact]
    public void A_check_that_throws_drops_the_queue_and_the_pump_goes_on()
    {
        var queue = new WriteQueue();
        queue.Add("secret text", 30, () => throw new InvalidOperationException("boom"));
        queue.Add("behind it", 30, check: null);
        var step = queue.Next();
        Assert.Equal(WriteStepKind.Failed, step.Kind);
        Assert.Equal(nameof(InvalidOperationException), step.Text);
        Assert.Equal(0, queue.Count);
        queue.Add("next", 30, check: null);
        Assert.Equal(new WriteStep(WriteStepKind.Write, "next", 30), queue.Next());
    }

    /// <summary>A check that empties the queue itself (it stopped the program) writes nothing, whatever it answers.</summary>
    [Fact]
    public void A_check_that_empties_the_queue_writes_nothing()
    {
        var queue = new WriteQueue();
        queue.Add("text", 30, () =>
        {
            queue.Clear();
            return WriteCheck.Goes;
        });
        Assert.Equal(WriteStepKind.Idle, queue.Next().Kind);
        Assert.False(queue.Clear());
    }
}
