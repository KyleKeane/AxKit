namespace AxFile.Core;

/// <summary>Where an operation of the window's history is (SPEC.md AF-9.2): running, done, failed, undone, or refused when undo was asked.</summary>
public enum ActionState
{
    Running,
    Done,
    Failed,
    Undone,
    NotUndone,
}

/// <summary>
/// One file operation of the window's history (SPEC.md AF-9.2): what it did ("Renamed notes.md to todo.md"), its
/// state, the <see cref="Operation"/> for Undo while it can still be taken back, where it left things (the folder and
/// the entry to select, for Recent operations, AF-9.3) and when it ran. The window keeps the object it was given back
/// and moves it on through the log; an entry that fell off the end of the log is left alone.
/// </summary>
public sealed class ActionEntry
{
    internal ActionEntry(string text, string folder, DateTime time)
    {
        Text = text;
        Folder = folder;
        Time = time;
    }

    /// <summary>What the operation did, or is doing ("Copying 3 items", "Compressing, 40 of 120 files"), or "Failed: <reason>".</summary>
    public string Text { get; internal set; }

    public ActionState State { get; internal set; } = ActionState.Running;

    /// <summary>The operation Undo takes back; null while it runs, after it failed, and once it is older than the last twenty undoable ones.</summary>
    public Operation? Operation { get; internal set; }

    /// <summary>The folder the operation left its result in; while it runs, the folder it runs in.</summary>
    public string Folder { get; internal set; }

    /// <summary>The entry to select there: what the operation made or moved; null for a delete and while it runs.</summary>
    public string? Select { get; internal set; }

    public DateTime Time { get; internal set; }

    /// <summary>The place of the entry among the operations that ended, counted by the log: a later end has a larger number. Zero while it runs.</summary>
    internal long Ended { get; set; }

    /// <summary>Why the undo was refused (<see cref="ActionState.NotUndone"/>), "notes.md has changed".</summary>
    public string? Reason { get; internal set; }

    /// <summary>The operation's line, which the status bar shows: the text, with ", undone" or ", not undone: <reason>" after it (AF-9.2).</summary>
    public string Line => State switch
    {
        ActionState.Undone => Text + ", undone",
        ActionState.NotUndone => Text + ", not undone: " + Reason,
        _ => Text,
    };

    /// <summary>The line in Recent operations (AF-9.3): the line and its time, "Copied 3 items, 21:04"; a running one ", running".</summary>
    public string HistoryLine => State == ActionState.Running ? Line + ", running" : $"{Line}, {Time:HH:mm}";

    /// <summary>Whether Ctrl+Z can take it back, as the newest such operation.</summary>
    public bool CanUndo => State == ActionState.Done && Operation is not null;

    /// <summary>Where Recent operations goes for it (AF-9.3): the folder and the entry the operation left, or, once undone, where the undo put things back.</summary>
    public (string Folder, string? Select) Location =>
        State == ActionState.Undone && Operation is { } operation ? (operation.Folder, operation.Select) : (Folder, Select);
}

/// <summary>
/// The window's history of file operations (SPEC.md AF-9.2): every file operation of a window, in time order, newest
/// last; the last fifty kept and the last twenty undoable; all gone with the window. A running operation begins an
/// entry and ends it with its result or its failure; one that ran at once adds its entry done. Ctrl+Z takes back the
/// newest undoable entry (<see cref="Newest"/>), the status bar shows the newest line (<see cref="Latest"/>), and
/// Actions → Recent operations lists them newest first (<see cref="Recent"/>). It was shown as a second list under
/// the folder list until 2026-11-23, when the Favorites list took that place (AF-D3).
/// </summary>
public sealed class ActionLog
{
    public const int Limit = 50;
    public const int UndoLimit = 20;

    private readonly List<ActionEntry> _entries = [];
    private readonly Func<DateTime> _clock;
    private long _ended;

    public ActionLog()
        : this(() => DateTime.Now)
    {
    }

    /// <param name="clock">The time an entry gets; the tests pass their own.</param>
    public ActionLog(Func<DateTime> clock)
    {
        _clock = clock;
    }

    /// <summary>"1 item", "5 items": a count in a line.</summary>
    public static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

    /// <summary>Every entry, in time order.</summary>
    public IReadOnlyList<ActionEntry> Entries => _entries;

    public int Count => _entries.Count;

    /// <summary>The newest entry, whatever its state, which the status bar shows (AF-2.3); null while the log is empty.</summary>
    public ActionEntry? Latest => _entries.Count > 0 ? _entries[^1] : null;

    /// <summary>The entries newest first, for Recent operations (AF-9.3).</summary>
    public IReadOnlyList<ActionEntry> Recent() => Enumerable.Reverse(_entries).ToList();

    /// <summary>A running operation's entry, "Copying 3 items", in <paramref name="folder"/>; the entry to end or fail with.</summary>
    public ActionEntry Begin(string text, string folder)
    {
        var entry = new ActionEntry(text, folder, _clock());
        Append(entry);
        return entry;
    }

    /// <summary>A running entry's text moved on, "Compressing, 40 of 120 files".</summary>
    public void Progress(ActionEntry entry, string text)
    {
        if (entry.State == ActionState.Running)
        {
            entry.Text = text;
        }
    }

    /// <summary>The operation ended: its line says what it did, and the operation, when there is one, is what Undo takes back and where Recent operations goes.</summary>
    public void End(ActionEntry entry, string text, Operation? operation)
    {
        entry.Text = text;
        entry.State = ActionState.Done;
        entry.Time = _clock();
        entry.Ended = ++_ended;
        if (operation is not null)
        {
            entry.Operation = operation;
            entry.Folder = operation.ResultFolder;
            entry.Select = operation.Result;
        }

        TrimUndoable();
    }

    /// <summary>The operation failed: "Failed: <reason>", nothing to undo.</summary>
    public void Fail(ActionEntry entry, string reason)
    {
        entry.Text = "Failed: " + reason;
        entry.State = ActionState.Failed;
        entry.Time = _clock();
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its entry, done.</summary>
    public ActionEntry Add(string text, Operation operation)
    {
        var entry = new ActionEntry(text, operation.ResultFolder, _clock())
        {
            State = ActionState.Done,
            Operation = operation,
            Select = operation.Result,
            Ended = ++_ended,
        };
        Append(entry);
        TrimUndoable();
        return entry;
    }

    /// <summary>
    /// The newest operation Undo can take back (Ctrl+Z, AF-4.14); null when none can. Newest by when it ended, which
    /// is when the user heard of it: a long copy that began before a rename and ended after it is the one Ctrl+Z
    /// takes first (until 2026-11-27 the entries' order decided, which is the order they began in, and Ctrl+Z after
    /// "Copied 3 items" took the rename back).
    /// </summary>
    public ActionEntry? Newest() => _entries.Where(entry => entry.CanUndo).MaxBy(entry => entry.Ended);

    /// <summary>The entry taken back: it reads "…, undone" and cannot be undone again, so that the next Ctrl+Z takes the one before.</summary>
    public void Undone(ActionEntry entry) => entry.State = ActionState.Undone;

    /// <summary>The undo refused (the entry has changed, the place is taken): the line says so and the operation is dropped, so that the next Ctrl+Z moves on.</summary>
    public void NotUndone(ActionEntry entry, string reason)
    {
        entry.State = ActionState.NotUndone;
        entry.Reason = reason;
    }

    /// <summary>The entry added; beyond the limit the oldest entries fall off.</summary>
    private void Append(ActionEntry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > Limit)
        {
            _entries.RemoveRange(0, _entries.Count - Limit);
        }
    }

    /// <summary>Beyond twenty undoable operations the oldest keeps its text and loses its operation (AF-4.14's limit).</summary>
    private void TrimUndoable()
    {
        // The oldest by when they ended, as Newest takes the newest by it.
        var undoable = _entries.Where(entry => entry.CanUndo).OrderBy(entry => entry.Ended).ToList();
        foreach (var entry in undoable.Take(Math.Max(0, undoable.Count - UndoLimit)))
        {
            entry.Operation = null;
        }
    }
}
