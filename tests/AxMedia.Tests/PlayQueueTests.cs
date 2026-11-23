using AxMedia.Core;

namespace AxMedia.Tests;

/// <summary>The queue (docs/axMedia/SPEC.md AM-3).</summary>
public class PlayQueueTests
{
    private static readonly string[] Three = [@"C:\m\01 One.mp3", @"C:\m\02 Two.mp3", @"C:\m\03 Three.mp3"];

    [Fact]
    public void An_empty_queue_is_one_line_and_a_set_queue_marks_the_file_playing()
    {
        var queue = new PlayQueue();
        Assert.Equal([PlayQueue.EmptyText], queue.Lines(active: false));
        Assert.Null(queue.PlayingPath);
        Assert.Equal("Queue", queue.ListName);
        Assert.Equal(string.Empty, queue.PlaceText(1));

        queue.Set(Three, 1);
        Assert.Equal(1, queue.Playing);
        Assert.Equal("02 Two.mp3", queue.PlayingName);
        Assert.Equal(["01 One.mp3", "02 Two.mp3 (playing)", "03 Three.mp3"], queue.Lines(active: true));
        Assert.Equal(["01 One.mp3", "02 Two.mp3", "03 Three.mp3"], queue.Lines(active: false));
        Assert.Equal("02 Two.mp3, 2 of 3", queue.Information(1));
        Assert.Equal("Queue, 3 items", queue.ListName);
        Assert.Equal("2 of 3, speed 1.25", queue.PlaceText(1.25));
    }

    [Fact]
    public void Moving_on_stops_at_the_end_and_select_takes_a_file()
    {
        var queue = new PlayQueue();
        queue.Set(Three, 0);
        Assert.True(queue.MoveOn());
        Assert.True(queue.MoveOn());
        Assert.False(queue.MoveOn());
        Assert.Equal(2, queue.Playing);
        Assert.True(queue.Select(0));
        Assert.False(queue.Select(3));
        Assert.Equal(0, queue.Playing);
    }

    [Fact]
    public void Added_files_follow_and_nothing_plays_until_selected()
    {
        var queue = new PlayQueue();
        Assert.Equal(2, queue.Add([Three[0], Three[1]]));
        Assert.Equal(-1, queue.Playing);
        Assert.Null(queue.PlayingName);
        Assert.Equal(1, queue.Add([Three[2]]));
        Assert.Equal(3, queue.Count);

        var one = new PlayQueue();
        one.Add([Three[0]]);
        Assert.Equal("Queue, 1 item", one.ListName);
    }

    [Fact]
    public void Removing_a_file_keeps_the_playing_index_on_its_file()
    {
        var queue = new PlayQueue();
        queue.Set(Three, 2);
        Assert.Equal("01 One.mp3", queue.Remove(0));
        Assert.Equal(1, queue.Playing);
        Assert.Equal("03 Three.mp3", queue.PlayingName);

        // The file playing removed: the index now names the one after it, or nothing past the end.
        queue.Set(Three, 1);
        Assert.Equal("02 Two.mp3", queue.Remove(1));
        Assert.Equal(1, queue.Playing);
        Assert.Equal("03 Three.mp3", queue.PlayingName);
        Assert.Equal("03 Three.mp3", queue.Remove(1));
        Assert.Equal(-1, queue.Playing);
        Assert.Null(queue.Remove(5));
        // The last file, playing, taken out: nothing is left to play, and the index is past the end.
        queue.Set(Three, 2);
        Assert.Equal("03 Three.mp3", queue.Remove(2));
        Assert.Equal(-1, queue.Playing);
        Assert.Null(queue.PlayingPath);
    }

    [Fact]
    public void Clear_drops_everything()
    {
        var queue = new PlayQueue();
        queue.Set(Three, 0);
        queue.Clear();
        Assert.Equal(0, queue.Count);
        Assert.Equal(-1, queue.Playing);
        Assert.Equal([PlayQueue.EmptyText], queue.Lines(active: true));
    }

    [Fact]
    public void Sort_by_name_reorders_the_queue_itself_and_the_order_added_puts_it_back()
    {
        // AM-3.6: the files then play in the order shown; the one playing stays the one playing.
        var queue = new PlayQueue();
        queue.Set([@"C:\m\b 10.mp3", @"C:\m\b 2.mp3", @"C:\m\a.mp3"], 0);
        queue.OrderBy(name: true);
        Assert.True(queue.ByName);
        Assert.Equal([@"C:\m\a.mp3", @"C:\m\b 2.mp3", @"C:\m\b 10.mp3"], queue.Paths);
        Assert.Equal(@"C:\m\b 10.mp3", queue.PlayingPath);
        Assert.Equal(2, queue.Playing);
        Assert.False(queue.MoveOn());

        // A file added while the queue is kept by name takes its place by name.
        Assert.Equal(1, queue.Add([@"C:\m\b 5.mp3"]));
        Assert.Equal([@"C:\m\a.mp3", @"C:\m\b 2.mp3", @"C:\m\b 5.mp3", @"C:\m\b 10.mp3"], queue.Paths);
        Assert.Equal(@"C:\m\b 10.mp3", queue.PlayingPath);

        // A removed file takes its number with it.
        Assert.Equal("b 2.mp3", queue.Remove(1));

        queue.OrderBy(name: false);
        Assert.False(queue.ByName);
        Assert.Equal([@"C:\m\b 10.mp3", @"C:\m\a.mp3", @"C:\m\b 5.mp3"], queue.Paths);
        Assert.Equal(@"C:\m\b 10.mp3", queue.PlayingPath);
        Assert.True(queue.MoveOn());
        Assert.Equal(@"C:\m\a.mp3", queue.PlayingPath);
    }
}
