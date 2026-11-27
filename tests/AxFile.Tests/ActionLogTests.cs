using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The window's history of file operations (docs/axFile/SPEC.md AF-9.2, AF-9.3, AF-4.14): the entries, their states, what Ctrl+Z takes back and in what order, where Recent operations goes, and the limits.</summary>
public sealed class ActionLogTests
{
    private static readonly string Folder = @"C:\work";

    private static string In(string name) => Path.Combine(Folder, name);

    private static ActionLog Log(DateTime at) => new(() => at);

    private static Operation Rename(string from, string to) => new(OperationKind.Rename, [In(from)], [In(to)]);

    [Fact]
    public void A_running_entry_is_replaced_when_it_ends_and_an_instant_one_is_done_at_once()
    {
        var log = Log(new DateTime(2026, 10, 22, 21, 4, 0));
        var copy = log.Begin("Copying 3 items", Folder);
        Assert.Equal("Copying 3 items, running", copy.HistoryLine);
        Assert.False(copy.CanUndo);
        Assert.Equal((Folder, (string?)null), copy.Location);
        Assert.Same(copy, log.Latest);

        log.Progress(copy, "Copying 3 items, please wait");
        Assert.Equal("Copying 3 items, please wait", copy.Line);

        var operation = new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt", @"C:\elsewhere\b.txt", @"C:\elsewhere\c.txt"], [In("a.txt"), In("b.txt"), In("c.txt")]);
        log.End(copy, "Copied 3 items", operation);
        Assert.Equal("Copied 3 items, 21:04", copy.HistoryLine);
        Assert.True(copy.CanUndo);
        Assert.Equal((Folder, "a.txt"), copy.Location);
        Assert.Same(copy, log.Newest());

        var rename = log.Add("Renamed notes.md to todo.md", Rename("notes.md", "todo.md"));
        Assert.Equal((Folder, "todo.md"), rename.Location);
        Assert.Same(rename, log.Newest());
        Assert.Same(rename, log.Latest);
    }

    [Fact]
    public void Recent_operations_are_listed_newest_first_whatever_their_state()
    {
        var log = Log(new DateTime(2026, 11, 23, 9, 30, 0));
        Assert.Empty(log.Recent());
        var rename = log.Add("Renamed notes.md to todo.md", Rename("notes.md", "todo.md"));
        var copy = log.Begin("Copying 3 items", Folder);
        var zip = log.Begin("Compressing", Folder);
        log.Fail(zip, "The file is in use");
        Assert.Equal([zip, copy, rename], log.Recent());
        Assert.Equal(
            ["Failed: The file is in use, 09:30", "Copying 3 items, running", "Renamed notes.md to todo.md, 09:30"],
            log.Recent().Select(entry => entry.HistoryLine));
        // The status bar's line is the newest entry's, whatever its state.
        Assert.Same(zip, log.Latest);
    }

    [Fact]
    public void A_failed_entry_says_so_and_cannot_be_undone()
    {
        var log = Log(DateTime.Now);
        var zip = log.Begin("Compressing", Folder);
        log.Progress(zip, "Compressing, 40 of 120 files");
        log.Fail(zip, "The file is in use");
        Assert.Equal("Failed: The file is in use", zip.Line);
        Assert.Equal(ActionState.Failed, zip.State);
        Assert.False(zip.CanUndo);
        Assert.Null(log.Newest());
        // A progress report after the end changes nothing.
        log.Progress(zip, "Compressing, 50 of 120 files");
        Assert.Equal("Failed: The file is in use", zip.Line);
    }

    [Fact]
    public void Undo_takes_the_operations_back_newest_first_one_per_press()
    {
        // Ctrl+Z (AF-4.14): each press takes the newest operation that can still be taken back; a failed one, a running
        // one and one already undone are passed over.
        var log = Log(DateTime.Now);
        var made = log.Add("Created temp", new Operation(OperationKind.NewFolder, [], [In("temp")]));
        var rename = log.Add("Renamed temp to notes", Rename("temp", "notes"));
        var failed = log.Begin("Copying 2 items", Folder);
        log.Fail(failed, "The disk is full");
        var running = log.Begin("Deleting 4 items", Folder);

        Assert.Same(rename, log.Newest());
        log.Undone(rename);
        Assert.Equal("Renamed temp to notes, undone", rename.Line);
        Assert.False(rename.CanUndo);

        Assert.Same(made, log.Newest());
        log.Undone(made);

        Assert.Null(log.Newest());

        // The running delete ends: it is the next to take back.
        log.End(running, "Deleted 4 items", new Operation(OperationKind.Delete, [In("a"), In("b"), In("c"), In("d")], []));
        Assert.Same(running, log.Newest());
    }

    [Fact]
    public void An_undone_entry_reads_so_and_leads_where_the_undo_put_things_back()
    {
        var log = Log(DateTime.Now);
        var rename = log.Add("Renamed notes.md to todo.md", Rename("notes.md", "todo.md"));
        var made = log.Add("Created temp", new Operation(OperationKind.NewFolder, [], [In("temp")]));
        log.Undone(made);
        Assert.Equal("Created temp, undone", made.Line);
        Assert.False(made.CanUndo);
        // What was made is gone: Recent operations goes to the folder it was in.
        Assert.Equal((Folder, (string?)null), made.Location);
        Assert.Same(rename, log.Newest());

        log.Undone(rename);
        Assert.Equal((Folder, "notes.md"), rename.Location);
        Assert.Null(log.Newest());

        var moved = log.Add("Moved 2 items into bundle", new Operation(OperationKind.Move, [In("a.txt"), In("b.txt")], [Path.Combine(Folder, "bundle", "a.txt"), Path.Combine(Folder, "bundle", "b.txt")], In("bundle")));
        Assert.Equal((Folder, "bundle"), moved.Location);
        log.NotUndone(moved, "a.txt has changed");
        Assert.Equal("Moved 2 items into bundle, not undone: a.txt has changed", moved.Line);
        Assert.False(moved.CanUndo);
        Assert.Null(log.Newest());
    }

    [Fact]
    public void A_delete_leads_back_to_its_folder_and_to_the_restored_entry_once_undone()
    {
        var log = Log(DateTime.Now);
        var delete = log.Begin("Deleting 2 items", Folder);
        log.End(delete, "Deleted 2 items", new Operation(OperationKind.Delete, [In("a.txt"), In("b.txt")], []));
        Assert.Equal((Folder, (string?)null), delete.Location);
        log.Undone(delete);
        Assert.Equal((Folder, "a.txt"), delete.Location);
    }

    [Fact]
    public void Fifty_entries_stay_and_twenty_can_be_undone()
    {
        var log = Log(DateTime.Now);
        for (var i = 1; i <= 55; i++)
        {
            log.Add($"Created {i}", new Operation(OperationKind.NewFolder, [], [In(i.ToString())]));
        }

        Assert.Equal(ActionLog.Limit, log.Count);
        Assert.Equal("Created 6", log.Entries[0].Line);
        Assert.Equal("Created 55", log.Recent()[0].Line);
        Assert.Equal("Created 6", log.Recent()[^1].Line);
        Assert.Equal(ActionLog.UndoLimit, log.Entries.Count(entry => entry.CanUndo));
        Assert.False(log.Entries[0].CanUndo);
        Assert.True(log.Entries[^1].CanUndo);
        // The oldest that lost its operation keeps its line and its place.
        Assert.Equal((Folder, "36"), log.Entries[^20].Location);
        Assert.Equal((Folder, "35"), log.Entries[^21].Location);
        Assert.Null(log.Entries[^21].Operation);
    }

    /// <summary>AF-4.14: Ctrl+Z takes the operation that ended last, the one last heard of: a copy that began before a rename and ended after it goes first.</summary>
    [Fact]
    public void Undo_takes_the_operation_that_ended_last_not_the_one_that_began_last()
    {
        var log = Log(new DateTime(2026, 10, 22, 21, 4, 0));
        var copy = log.Begin("Copying 3 items", Folder);
        var rename = log.Add("Renamed a.txt to b.txt", Rename("a.txt", "b.txt"));
        Assert.Same(rename, log.Newest());

        log.End(copy, "Copied 3 items", new Operation(OperationKind.Copy, [@"C:\elsewhere\a"], [In("a")]));
        Assert.Same(copy, log.Newest());
        log.Undone(copy);
        Assert.Same(rename, log.Newest());
    }

    [Fact]
    public void A_running_entry_that_fell_off_the_log_still_ends()
    {
        // The window holds the entry of a slow operation while fifty quick ones push it off the log.
        var log = Log(DateTime.Now);
        var slow = log.Begin("Copying 1 item", Folder);
        for (var i = 1; i <= ActionLog.Limit; i++)
        {
            log.Add($"Created {i}", new Operation(OperationKind.NewFolder, [], [In(i.ToString())]));
        }

        Assert.DoesNotContain(slow, log.Entries);
        log.End(slow, "Copied 1 item", new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt"], [In("a.txt")]));
        Assert.Equal("Copied 1 item", slow.Line);
        Assert.DoesNotContain(slow, log.Entries);
    }

    [Fact]
    public void An_operation_knows_where_it_left_things()
    {
        Assert.Equal(Folder, Rename("a", "b").ResultFolder);
        Assert.Equal("b", Rename("a", "b").Result);
        var move = new Operation(OperationKind.Move, [In("a")], [Path.Combine(Folder, "bundle", "a")], In("bundle"));
        Assert.Equal(Folder, move.ResultFolder);
        Assert.Equal("bundle", move.Result);
        var paste = new Operation(OperationKind.Move, [@"C:\elsewhere\a"], [In("a")]);
        Assert.Equal(Folder, paste.ResultFolder);
        Assert.Equal("a", paste.Result);
        var delete = new Operation(OperationKind.Delete, [In("a"), In("b")], []);
        Assert.Equal(Folder, delete.ResultFolder);
        Assert.Null(delete.Result);
        Assert.Equal("1 item", ActionLog.Items(1));
        Assert.Equal("5 items", ActionLog.Items(5));
    }
}
