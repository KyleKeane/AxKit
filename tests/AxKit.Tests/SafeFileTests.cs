using System.Text;
using AxKit.Core;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>A file written whole or not at all (SafeFile; docs/axKit/repairs.json R-22).</summary>
public sealed class SafeFileTests : IDisposable
{
    private readonly TempFolder _temp = new("axkit-safefile");

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void A_new_file_is_written_and_nothing_else_is_left_beside_it()
    {
        var path = Path.Combine(_temp.Path, "notes.md");
        SafeFile.WriteAllText(path, "one");
        Assert.Equal("one", File.ReadAllText(path));
        Assert.Equal([path], Directory.GetFiles(_temp.Path));
        // No byte order mark unless the encoding given has one.
        Assert.Equal((byte)'o', File.ReadAllBytes(path)[0]);
    }

    [Fact]
    public void An_existing_file_is_replaced_whole_and_keeps_its_creation_time()
    {
        var path = _temp.File("notes.md", "the old text, longer than the new");
        var created = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetCreationTimeUtc(path, created);
        SafeFile.WriteAllBytes(path, "new"u8.ToArray());
        Assert.Equal("new", File.ReadAllText(path));
        Assert.Equal(created, File.GetCreationTimeUtc(path));
        Assert.Equal([path], Directory.GetFiles(_temp.Path));
    }

    [Fact]
    public void An_encoding_s_mark_is_written_when_it_has_one()
    {
        var path = Path.Combine(_temp.Path, "marked.txt");
        SafeFile.WriteAllText(path, "a", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Assert.Equal([0xEF, 0xBB, 0xBF, (byte)'a'], File.ReadAllBytes(path));
    }

    [Fact]
    public void A_write_that_cannot_be_made_leaves_the_old_file_and_no_temporary_one()
    {
        var path = _temp.File("locked.md", "old");
        // Another program holds the file open and lets nobody write it.
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.Throws<IOException>(() => SafeFile.WriteAllText(path, "new"));
            Assert.Equal([path], Directory.GetFiles(_temp.Path));
        }

        Assert.Equal("old", File.ReadAllText(path));
    }

    [Fact]
    public void A_file_another_program_holds_open_for_writing_but_not_for_replacing_is_written_in_place_as_before()
    {
        var path = _temp.File("shared.md", "old");
        // Open with read and write shared but not delete: the swap is refused, a plain write is not.
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            SafeFile.WriteAllText(path, "new");
            Assert.Equal([path], Directory.GetFiles(_temp.Path));
        }

        Assert.Equal("new", File.ReadAllText(path));
    }
}
