namespace AxFile.Core;

/// <summary>The kinds of operation a window can take back (SPEC.md AF-4.14).</summary>
public enum OperationKind
{
    Rename,
    NewFolder,
    NewFile,
    Move,
    Copy,
    Compress,
    Extract,
    Delete,
}

/// <summary>
/// What an entry was when an operation made it (SPEC.md AF-4.14), to know at the undo whether it was changed since:
/// a file's size and the time it was last written; for a folder, how many files and folders it holds all the way
/// down, their sizes together and the newest write time among its files.
/// </summary>
public sealed record EntryStamp(long Count, long Size, DateTime Written)
{
    /// <summary>The stamp of the entry at a path now; null when nothing is there or it cannot be read.</summary>
    public static EntryStamp? Of(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var file = new FileInfo(path);
                return new EntryStamp(1, file.Length, file.LastWriteTimeUtc);
            }

            if (!Directory.Exists(path))
            {
                return null;
            }

            long count = 0, size = 0;
            var written = DateTime.MinValue;
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                count++;
                if (entry is FileInfo file)
                {
                    size += file.Length;
                    written = file.LastWriteTimeUtc > written ? file.LastWriteTimeUtc : written;
                }
            }

            return new EntryStamp(count, size, written);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>
/// One operation the window made, with what its undo needs (SPEC.md AF-4.14): the paths before it and after it. A
/// rename has the old path and the new; a new folder, a new file, a compress and an extract only what they made; a
/// move and a copy the sources and where each landed (a move into a new folder names it, to remove when it is empty
/// again); a delete the paths that went to the Recycle Bin. The window's history (<see cref="ActionLog"/>) holds the
/// operations the window can still take back. An operation whose undo removes what it made (a paste that copied, a
/// compress, an extract) also keeps what each result was when it was made (<see cref="Made"/>, from
/// <see cref="Stamped"/>), so that the undo never removes one that was changed since, and whether the drive it is
/// on has a Recycle Bin (<see cref="ToBin"/>), which decides the undo's words and whether it asks first.
/// </summary>
public sealed record Operation(OperationKind Kind, IReadOnlyList<string> Before, IReadOnlyList<string> After, string? MadeFolder = null)
{
    /// <summary>What each entry of <see cref="After"/> was when the operation made it; null for an operation that keeps none.</summary>
    public IReadOnlyList<EntryStamp?>? Made { get; init; }

    /// <summary>Whether what the undo removes goes to the Recycle Bin: false on a drive that has none (<see cref="RecycleBin.Has"/>), where it is deleted for good.</summary>
    public bool ToBin { get; init; } = true;

    /// <summary>The operation with each result as it is on disk now, and whether its drive has a Recycle Bin. Reads the disk: call it on the operation's own task, right after it ended.</summary>
    public Operation Stamped() => this with { Made = [.. After.Select(EntryStamp.Of)], ToBin = After.Count == 0 || RecycleBin.Has(After[0]) };

    /// <summary>Whether the undo removes what the operation made, through the Recycle Bin where the drive has one.</summary>
    public bool UndoRemoves => Kind is OperationKind.Copy or OperationKind.Compress or OperationKind.Extract;

    /// <summary>Whether the undo puts entries back where they were (<see cref="Before"/>), rather than removing what the operation made (<see cref="After"/>).</summary>
    private bool PutsBack => Kind is OperationKind.Rename or OperationKind.Move or OperationKind.Delete;

    /// <summary>The paths the undo lands on: where the entries go back to, or what is removed.</summary>
    private IReadOnlyList<string> Paths => PutsBack ? Before : After;

    /// <summary>The folder the undo lands in, to show afterwards.</summary>
    public string Folder => Path.GetDirectoryName(Paths[0]) ?? Paths[0];

    /// <summary>The entry to select afterwards: the one put back; null when the undo removed what it made.</summary>
    public string? Select => PutsBack ? Path.GetFileName(Before[0]) : null;

    /// <summary>Where the operation left things (Recent operations, AF-9.3): the folder of what it made or moved into, or of what it deleted.</summary>
    public string ResultFolder => Kind == OperationKind.Delete
        ? Path.GetDirectoryName(Before[0]) ?? Before[0]
        : Path.GetDirectoryName(MadeFolder ?? After[0]) ?? After[0];

    /// <summary>The entry to select there: what it made, the folder it moved into, or the first entry it copied or moved; null for a delete.</summary>
    public string? Result => Kind == OperationKind.Delete ? null : Path.GetFileName(MadeFolder ?? After[0]);

    /// <summary>"notes.md" or "3 items".</summary>
    public string What => Paths.Count == 1 ? Path.GetFileName(Paths[0]) : $"{Paths.Count} items";

    /// <summary>The announcement once the undo is done.</summary>
    public string DoneText => Kind switch
    {
        OperationKind.Rename => $"Undone: renamed back to {Path.GetFileName(Before[0])}",
        OperationKind.NewFolder or OperationKind.NewFile => $"Undone: {What} removed",
        OperationKind.Move => "Undone: moved back",
        OperationKind.Delete => $"Undone: {What} restored",
        _ => ToBin ? $"Undone: {What} sent to the Recycle Bin" : $"Undone: {What} deleted for good",
    };

    /// <summary>
    /// The question an undo asks first when it would delete for good (AF-4.14): a title and a text; null when the undo
    /// deletes nothing, or sends what it removes to the Recycle Bin.
    /// </summary>
    public (string Title, string Text)? UndoQuestion => UndoRemoves && !ToBin
        ? ($"Delete {What} for good?", $"Undo removes what this operation made, and this drive has no Recycle Bin: {What} would be deleted for good and cannot be brought back.")
        : null;
}

