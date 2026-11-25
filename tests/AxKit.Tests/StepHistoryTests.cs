using AxKit.Core;

namespace AxKit.Tests;

public class StepHistoryTests
{
    private static Step Said(string words) => new([], words);

    [Fact]
    public void The_newest_step_comes_back_first_and_an_empty_history_gives_none()
    {
        var history = new StepHistory(5);
        Assert.Null(history.TakeLast());
        history.Add(Said("one"));
        history.Add(Said("two"));
        Assert.Equal("two", history.TakeLast()!.Said);
        Assert.Equal("one", history.TakeLast()!.Said);
        Assert.Null(history.TakeLast());
    }

    [Fact]
    public void Past_its_cap_the_oldest_step_is_dropped()
    {
        var history = new StepHistory(2);
        history.Add(Said("one"));
        history.Add(Said("two"));
        history.Add(Said("three"));
        Assert.Equal(2, history.Count);
        Assert.Equal("three", history.TakeLast()!.Said);
        Assert.Equal("two", history.TakeLast()!.Said);
        Assert.Null(history.TakeLast());
    }

    [Fact]
    public void Clear_leaves_nothing_to_undo()
    {
        var history = new StepHistory(3);
        history.Add(Said("one"));
        history.Clear();
        Assert.Equal(0, history.Count);
        Assert.Null(history.TakeLast());
    }
}
