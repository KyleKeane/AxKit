namespace AxKit.Tests;

/// <summary>
/// The tests' own temporary folder goes with the test (tests/Shared/TempFolder.cs). The review's second pass of
/// 2026-11-27 found 28 folders of one test class left under the temporary path: git writes every object of a
/// repository read-only, a read-only file stops the delete, and the failed delete was passed over in silence.
/// </summary>
public sealed class TempFolderTests
{
    [Fact]
    public void A_folder_with_read_only_files_in_it_is_deleted_with_the_test()
    {
        string path;
        using (var temp = new TempFolder("axkit-tempfolder"))
        {
            path = temp.Path;
            var file = temp.File(Path.Combine("objects", "17", "an-object"), "x");
            File.SetAttributes(file, FileAttributes.ReadOnly);
            Assert.True(Directory.Exists(path));
        }

        Assert.False(Directory.Exists(path), "The folder stayed behind: " + path);
    }
}
