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

    /// <summary>
    /// L7 (the review's third pass of 2026-11-27): test.ps1 counts the tests' own folders in the temporary folder
    /// before and after a run, by a table of their names' beginnings. Every TempFolder begins with its mark, and
    /// every test that makes a folder of its own for one run (a name ending in a hyphen, then a fresh part) is in
    /// the table too, so that its leak would show.
    /// </summary>
    [Fact]
    public void Test_ps1_counts_every_kind_of_folder_the_tests_make()
    {
        var root = SourceTexts.RepositoryRoot();
        var line = File.ReadLines(Path.Combine(root, "test.ps1")).Single(text => text.StartsWith("$testFolderPrefixes = @(", StringComparison.Ordinal));
        var table = System.Text.RegularExpressions.Regex.Matches(line, "'([^']+)'").Select(match => match.Groups[1].Value).ToList();
        Assert.Contains(TempFolder.Mark, table);
        using (var probe = new TempFolder("axkit-tempfolder"))
        {
            Assert.StartsWith(TempFolder.Mark, Path.GetFileName(probe.Path), StringComparison.Ordinal);
        }

        var own = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(@"\bin\", StringComparison.Ordinal) && !path.Contains(@"\obj\", StringComparison.Ordinal)))
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"GetTempPath\(\), ""([^""]+-)"" \+"))
            {
                own.Add(match.Groups[1].Value);
            }
        }

        var missing = own.Distinct().Where(prefix => !table.Any(start => prefix.StartsWith(start, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(missing.Count == 0, "Folders the tests make that test.ps1 does not count: " + string.Join(", ", missing));
    }
}
