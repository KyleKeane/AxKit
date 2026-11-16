using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The Recently closed list of the axKit menu (docs/axKit/SPEC.md AX-9.4): the order, the doubles, the cap, the titles and the file.</summary>
public sealed class RecentlyClosedTests
{
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "axkit-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void The_newest_comes_first_and_an_item_closed_again_moves_to_the_front_once()
    {
        IReadOnlyList<ClosedItem> items = [];
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work\a.md"));
        items = RecentlyClosed.Add(items, new("axClaude", @"C:\work"));
        items = RecentlyClosed.Add(items, new("axDown", @"c:\WORK\A.MD"));
        Assert.Equal([@"c:\WORK\A.MD", @"C:\work"], items.Select(i => i.Path));

        // The same path under another app is another item: a folder axClaude held is not axDown's file.
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work"));
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void Ten_are_kept()
    {
        IReadOnlyList<ClosedItem> items = [];
        for (var i = 0; i < 12; i++)
        {
            items = RecentlyClosed.Add(items, new("axDown", $@"C:\work\{i}.md"));
        }

        Assert.Equal(RecentlyClosed.Capacity, items.Count);
        Assert.Equal(@"C:\work\11.md", items[0].Path);
        Assert.Equal(@"C:\work\2.md", items[^1].Path);
    }

    [Theory]
    [InlineData("axDown", @"C:\work\notes.md", @"notes.md (C:\work\notes.md) - axDown")]
    [InlineData("axClaude", @"C:\work\myproject\", @"myproject (C:\work\myproject\) - axClaude, access layer for Claude Code")]
    [InlineData("axClaude", @"C:\", @"C:\ - axClaude, access layer for Claude Code")]
    public void The_title_is_the_name_the_full_path_and_the_app(string app, string path, string title) =>
        Assert.Equal(title, RecentlyClosed.Title(new(app, path)));

    [Fact]
    public void An_app_that_holds_no_file_is_kept_by_its_name_once()
    {
        IReadOnlyList<ClosedItem> items = [];
        items = RecentlyClosed.Add(items, new("axProcess", string.Empty));
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work\a.md"));
        items = RecentlyClosed.Add(items, new("axProcess", string.Empty));
        Assert.Equal(["axProcess", "axDown"], items.Select(i => i.App));
        Assert.Equal("axProcess", RecentlyClosed.Title(items[0]));
    }

    [Fact]
    public void The_file_round_trips_and_a_missing_file_is_an_empty_list()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            Assert.Empty(RecentlyClosed.Read(file));
            RecentlyClosed.Write(file, [new("axDown", @"C:\work\a.md"), new("axClaude", @"C:\work")]);
            var read = RecentlyClosed.Read(file);
            Assert.Equal(["axDown", "axClaude"], read.Select(i => i.App));
            Assert.Equal(@"C:\work", read[1].Path);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>repairs.md R-68: what is in the file and is no whole path is not read, and such a path is not written.</summary>
    [Fact]
    public void Only_whole_paths_are_read_from_the_file_and_written_to_it()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(file, """
                { "items": [
                    { "app": "axDown", "path": "--secret" },
                    { "app": "axDown", "path": "C:\\work\\a.md" },
                    { "app": "axFile", "path": "notes" },
                    null,
                    { "app": "", "path": "C:\\work" },
                    { "app": "axDown" },
                    { "app": "axProcess", "path": "" }
                ] }
                """);
            Assert.Equal([@"C:\work\a.md", string.Empty], RecentlyClosed.Read(file).Select(i => i.Path));

            Assert.False(RecentlyClosed.Remember(file, "axDown", "--automation-home"));
            Assert.False(RecentlyClosed.Remember(file, "axDown", "b.md"));
            Assert.True(RecentlyClosed.Remember(file, "axDown", @"C:\work\b.md"));
            Assert.Equal([@"C:\work\b.md", @"C:\work\a.md", string.Empty], RecentlyClosed.Read(file).Select(i => i.Path));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>repairs.md R-68: one writer at a time, so that windows closing together are all on the list.</summary>
    [Fact]
    public void Writers_at_the_same_time_all_end_up_on_the_list()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            var threads = Enumerable.Range(0, 8)
                .Select(i => new Thread(() => Assert.True(RecentlyClosed.Remember(file, "axDown", $@"C:\work\{i}.md", waitMilliseconds: 30000))))
                .ToList();
            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
            Assert.Equal(Enumerable.Range(0, 8).Select(i => $@"C:\work\{i}.md"), RecentlyClosed.Read(file).Select(i => i.Path).Order());
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>repairs.md R-68: a writer that does not get its turn gives up after its wait and leaves the file as it is.</summary>
    [Fact]
    public void A_writer_that_waits_too_long_writes_nothing_and_does_not_hang()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            Assert.True(RecentlyClosed.Remember(file, "axDown", @"C:\work\a.md"));
            using var taken = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var holder = new Thread(() =>
            {
                using var mutex = new Mutex(false, RecentlyClosed.MutexName(file));
                mutex.WaitOne();
                taken.Set();
                release.Wait();
                mutex.ReleaseMutex();
            });
            holder.Start();
            taken.Wait();
            try
            {
                Assert.False(RecentlyClosed.Remember(file, "axDown", @"C:\work\b.md", waitMilliseconds: 50));
            }
            finally
            {
                release.Set();
                holder.Join();
            }

            Assert.Equal([@"C:\work\a.md"], RecentlyClosed.Read(file).Select(i => i.Path));
            Assert.True(RecentlyClosed.Remember(file, "axDown", @"C:\work\b.md"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
