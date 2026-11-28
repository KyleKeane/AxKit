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
        var unmarked = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(@"\bin\", StringComparison.Ordinal) && !path.Contains(@"\obj\", StringComparison.Ordinal)))
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"GetTempPath\(\), ""([^""]+-)"" \+( TempFolder\.Run \+)?"))
            {
                own.Add(match.Groups[1].Value);
                if (!match.Groups[2].Success)
                {
                    unmarked.Add($"{Path.GetFileName(file)}: {match.Groups[1].Value}");
                }
            }
        }

        var missing = own.Distinct().Where(prefix => !table.Any(start => prefix.StartsWith(start, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(missing.Count == 0, "Folders the tests make that test.ps1 does not count: " + string.Join(", ", missing));
        // L4 (the fourth pass): test.ps1 counts its own run's folders by the run's mark right after the beginning.
        Assert.True(unmarked.Count == 0, "Folders the tests make without the run's mark after their beginning (TempFolder.Run): " + string.Join(", ", unmarked));
        Assert.Contains("axGIT-no-hooks-", table);
    }

    /// <summary>L4: the run's mark comes from the variable test.ps1 sets, and stands right after the beginning of a TempFolder's name.</summary>
    [Fact]
    public void A_folder_carries_the_runs_mark_after_its_beginning()
    {
        var run = Environment.GetEnvironmentVariable("AXKIT_TEST_RUN");
        Assert.Equal(string.IsNullOrEmpty(run) ? string.Empty : run + "-", TempFolder.Run);
        using var probe = new TempFolder("axkit-tempfolder");
        Assert.StartsWith(TempFolder.Mark + TempFolder.Run + "axkit-tempfolder-", Path.GetFileName(probe.Path), StringComparison.Ordinal);
        var script = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "test.ps1"));
        Assert.Contains("$env:AXKIT_TEST_RUN = $testRun", script, StringComparison.Ordinal);
        Assert.Contains("$name.StartsWith($_ + $testRun + '-'", script, StringComparison.Ordinal);
    }
}
