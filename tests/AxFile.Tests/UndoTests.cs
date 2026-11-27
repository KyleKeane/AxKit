using System.Text;
using AxFile.Core;

namespace AxFile.Tests;

/// <summary>Undo (docs/axFile/SPEC.md AF-4.14): the history, each operation's inverse over a temporary folder, and the Recycle Bin's records; the Recycle Bin itself is not exercised.</summary>
public sealed class UndoTests : IDisposable
{
    private readonly TempFolder _folder = new("axfile-undo");

    public UndoTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder.Path, "docs"));
        File.WriteAllText(Path.Combine(_folder.Path, "notes.md"), "# Notes");
    }

    public void Dispose() => _folder.Dispose();

    private string In(string name) => Path.Combine(_folder.Path, name);

    [Fact]
    public void An_operation_knows_its_folder_its_entry_and_its_texts()
    {
        var rename = new Operation(OperationKind.Rename, [In("notes.md")], [In("todo.md")]);
        Assert.Equal(_folder.Path, rename.Folder);
        Assert.Equal("notes.md", rename.Select);
        Assert.Equal("Undone: renamed back to notes.md", rename.DoneText);

        var copy = new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt", @"C:\elsewhere\b.txt"], [In("a.txt"), In("b.txt")]);
        Assert.Equal(_folder.Path, copy.Folder);
        Assert.Null(copy.Select);
        Assert.Equal("Undone: 2 items sent to the Recycle Bin", copy.DoneText);

        var delete = new Operation(OperationKind.Delete, [In("notes.md")], []);
        Assert.Equal("notes.md", delete.Select);
        Assert.Equal("Undone: notes.md restored", delete.DoneText);
        Assert.Equal("Undone: docs removed", new Operation(OperationKind.NewFolder, [], [In("docs")]).DoneText);
        Assert.Equal("Undone: moved back", new Operation(OperationKind.Move, [In("notes.md")], [Path.Combine(_folder.Path, "docs", "notes.md")], In("docs")).DoneText);
    }

    [Fact]
    public async Task A_rename_is_renamed_back_unless_the_entry_changed_or_the_old_name_is_taken()
    {
        var renamed = FileOperations.Rename(In("notes.md"), "todo.md");
        var operation = new Operation(OperationKind.Rename, [In("notes.md")], [renamed]);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.True(File.Exists(In("notes.md")));
        Assert.False(File.Exists(renamed));

        Assert.Equal("Not undone: todo.md has changed", FileOperations.CheckUndo(operation));
        File.WriteAllText(renamed, "x");
        Assert.Equal("Not undone: notes.md is already here", FileOperations.CheckUndo(operation));
    }

    /// <summary>
    /// AF-4.14: the undo of a paste that copied, of a compress and of an extract removes what the operation made
    /// only as the operation left it: "Not undone: name has changed" for a copy edited since, and for a folder with
    /// a file changed, added or removed anywhere inside it.
    /// </summary>
    [Fact]
    public void A_copy_or_an_extracted_folder_changed_since_is_not_removed_by_undo()
    {
        var copy = In("notes - 2.md");
        File.Copy(In("notes.md"), copy);
        var pasted = new Operation(OperationKind.Copy, [In("notes.md")], [copy]).Stamped();
        Assert.Null(FileOperations.CheckUndo(pasted));
        File.AppendAllText(copy, "\nA line written since the paste.");
        Assert.Equal("Not undone: notes - 2.md has changed", FileOperations.CheckUndo(pasted));

        // The same size, written again later: changed all the same.
        var same = In("same.md");
        File.WriteAllText(same, "12345");
        var again = new Operation(OperationKind.Copy, [In("notes.md")], [same]).Stamped();
        File.WriteAllText(same, "54321");
        File.SetLastWriteTimeUtc(same, DateTime.UtcNow.AddMinutes(1));
        Assert.Equal("Not undone: same.md has changed", FileOperations.CheckUndo(again));

        var extracted = In("archive");
        Directory.CreateDirectory(Path.Combine(extracted, "inner"));
        File.WriteAllText(Path.Combine(extracted, "inner", "a.txt"), "a");
        File.WriteAllText(Path.Combine(extracted, "b.txt"), "b");
        var extract = new Operation(OperationKind.Extract, [], [extracted]).Stamped();
        Assert.Null(FileOperations.CheckUndo(extract));
        File.WriteAllText(Path.Combine(extracted, "inner", "new.txt"), string.Empty);
        Assert.Equal("Not undone: archive has changed", FileOperations.CheckUndo(extract));
        File.Delete(Path.Combine(extracted, "inner", "new.txt"));
        Assert.Null(FileOperations.CheckUndo(extract));
        File.WriteAllText(Path.Combine(extracted, "inner", "a.txt"), "changed");
        Assert.Equal("Not undone: archive has changed", FileOperations.CheckUndo(extract));

        // A move is put back whatever its entries hold: nothing of theirs is removed.
        Assert.Null(new Operation(OperationKind.Move, [In("x")], [copy]).Stamped().UndoQuestion);
    }

    /// <summary>
    /// AF-4.3, AF-4.14: on a drive without a Recycle Bin the delete's question says "for good", and an undo that
    /// removes what an operation made asks first and says what it did; with a bin, the words of before.
    /// </summary>
    [Fact]
    public void Without_a_recycle_bin_the_words_say_deleted_for_good_and_the_undo_asks()
    {
        Assert.False(RecycleBin.Has(@"\\server\share\folder\notes.md"));
        Assert.False(RecycleBin.Has(string.Empty));
        // A drive of this computer answers without throwing, whatever it answers.
        _ = RecycleBin.Has(_folder.Path);

        Assert.Equal("It goes to the Recycle Bin.", FileOperations.DeleteText(toBin: true, several: false, anyFolder: false));
        Assert.Equal("They go to the Recycle Bin, the folders with everything in them.", FileOperations.DeleteText(toBin: true, several: true, anyFolder: true));
        foreach (var several in new[] { false, true })
        {
            foreach (var anyFolder in new[] { false, true })
            {
                var text = FileOperations.DeleteText(toBin: false, several, anyFolder);
                Assert.StartsWith("This drive has no Recycle Bin: ", text);
                Assert.Contains("deleted for good", text);
                Assert.EndsWith("cannot be brought back.", text);
                Assert.DoesNotContain("go to the Recycle Bin", text);
            }
        }

        var copy = new Operation(OperationKind.Copy, [@"C:\elsewhere\a.txt"], [In("a.txt")]);
        Assert.True(copy.ToBin);
        Assert.Null(copy.UndoQuestion);
        Assert.Equal("Undone: a.txt sent to the Recycle Bin", copy.DoneText);

        var noBin = copy with { ToBin = false };
        Assert.Equal("Undone: a.txt deleted for good", noBin.DoneText);
        Assert.Equal("Delete a.txt for good?", noBin.UndoQuestion!.Value.Title);
        Assert.Contains("no Recycle Bin", noBin.UndoQuestion!.Value.Text);
        Assert.Contains("cannot be brought back", noBin.UndoQuestion!.Value.Text);
        // A rename back deletes nothing, bin or not.
        Assert.Null((new Operation(OperationKind.Rename, [In("a")], [In("b")]) with { ToBin = false }).UndoQuestion);
    }

    /// <summary>AF-4.13: a move into the folder the entries are in already is refused, however that folder is typed.</summary>
    [Fact]
    public void A_move_into_the_folder_the_entries_are_in_is_known()
    {
        var sources = new[] { In("notes.md"), In("docs") };
        Assert.True(FileOperations.AlreadyIn(sources, _folder.Path));
        Assert.True(FileOperations.AlreadyIn(sources, _folder.Path + "\\"));
        Assert.True(FileOperations.AlreadyIn(sources, Path.Combine(_folder.Path, ".")));
        Assert.True(FileOperations.AlreadyIn(sources, Path.Combine(_folder.Path, "docs", "..")));
        Assert.True(FileOperations.AlreadyIn(sources, _folder.Path.ToUpperInvariant()));
        Assert.False(FileOperations.AlreadyIn(sources, In("docs")));
        Assert.False(FileOperations.AlreadyIn([In("notes.md"), Path.Combine(In("docs"), "x.txt")], _folder.Path));
        Assert.False(FileOperations.AlreadyIn([], _folder.Path));
    }

    [Fact]
    public async Task A_new_folder_or_file_is_removed_while_still_empty()
    {
        var made = FileOperations.CreateFolder(_folder.Path, "new");
        var folder = new Operation(OperationKind.NewFolder, [], [made]);
        File.WriteAllText(Path.Combine(made, "x.txt"), "x");
        Assert.Equal("Not undone: new has files now", FileOperations.CheckUndo(folder));
        File.Delete(Path.Combine(made, "x.txt"));
        Assert.Null(FileOperations.CheckUndo(folder));
        Assert.Null(await FileOperations.UndoAsync(folder));
        Assert.False(Directory.Exists(made));

        var file = new Operation(OperationKind.NewFile, [], [FileOperations.CreateFile(_folder.Path)]);
        Assert.Null(FileOperations.CheckUndo(file));
        File.WriteAllText(file.After[0], "typed");
        Assert.Equal("Not undone: New file.txt has content now", FileOperations.CheckUndo(file));
        File.WriteAllText(file.After[0], string.Empty);
        Assert.Null(await FileOperations.UndoAsync(file));
        Assert.False(File.Exists(file.After[0]));
    }

    [Fact]
    public async Task A_move_into_a_new_folder_goes_back_and_the_folder_goes_when_empty()
    {
        var made = FileOperations.CreateFolder(_folder.Path, "bundle");
        var sources = new[] { In("notes.md") };
        var destinations = FileOperations.Destinations(sources, made);
        Assert.Null(await FileOperations.CopyOrMoveAsync(sources, destinations, move: true));
        var operation = new Operation(OperationKind.Move, sources, destinations, made);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.True(File.Exists(In("notes.md")));
        Assert.False(Directory.Exists(made));
    }

    [Fact]
    public async Task A_change_of_case_only_is_renamed_back()
    {
        var renamed = FileOperations.Rename(In("notes.md"), "Notes.md");
        var operation = new Operation(OperationKind.Rename, [In("notes.md")], [renamed]);
        Assert.Null(FileOperations.CheckUndo(operation));
        Assert.Null(await FileOperations.UndoAsync(operation));
        Assert.Equal(["docs", "notes.md"], Directory.EnumerateFileSystemEntries(_folder.Path).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void A_folder_where_the_new_file_was_is_a_change()
    {
        var file = new Operation(OperationKind.NewFile, [], [FileOperations.CreateFile(_folder.Path)]);
        File.Delete(file.After[0]);
        Directory.CreateDirectory(file.After[0]);
        Assert.Equal("Not undone: New file.txt has changed", FileOperations.CheckUndo(file));
        var folder = new Operation(OperationKind.NewFolder, [], [In("made")]);
        File.WriteAllText(In("made"), "x");
        Assert.Equal("Not undone: made has changed", FileOperations.CheckUndo(folder));
    }

    [Fact]
    public void A_copy_whose_copies_are_gone_and_a_delete_whose_entry_is_back_are_not_undone()
    {
        Assert.Equal("Not undone: gone.txt has changed", FileOperations.CheckUndo(new Operation(OperationKind.Copy, [In("notes.md")], [In("gone.txt")])));
        Assert.Equal("Not undone: notes.md is already here", FileOperations.CheckUndo(new Operation(OperationKind.Delete, [In("notes.md")], [])));
        Assert.Null(FileOperations.CheckUndo(new Operation(OperationKind.Delete, [In("gone.txt")], [])));
    }

    [Fact]
    public void A_recycle_bin_record_gives_the_original_path_and_the_time()
    {
        var path = @"D:\Documents\notes.md";
        var chars = Encoding.Unicode.GetBytes(path + "\0");
        var bytes = new byte[24 + 4 + chars.Length];
        BitConverter.GetBytes(2L).CopyTo(bytes, 0);
        BitConverter.GetBytes(7L).CopyTo(bytes, 8);
        BitConverter.GetBytes(133_000_000_000_000_000L).CopyTo(bytes, 16);
        BitConverter.GetBytes(path.Length + 1).CopyTo(bytes, 24);
        chars.CopyTo(bytes, 28);
        Assert.Equal(new RecycleRecord(path, 133_000_000_000_000_000L), RecycleBin.Parse(bytes));

        // The older record without the length field: the path is a fixed field, read to its first null.
        var old = new byte[24 + 520];
        BitConverter.GetBytes(1L).CopyTo(old, 0);
        chars.CopyTo(old, 24);
        Assert.Equal(path, RecycleBin.Parse(old)!.OriginalPath);
        Assert.Null(RecycleBin.Parse(new byte[10]));
        Assert.Null(RecycleBin.ReadRecord(In("notes.md")));
        Assert.Null(RecycleBin.ReadRecord(In("$Rabc.md")));
    }
}
