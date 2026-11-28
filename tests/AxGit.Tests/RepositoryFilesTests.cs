using System.Text.RegularExpressions;

namespace AxGit.Tests;

/// <summary>
/// The check that closes P2's class (the review's fifth pass of 2026-11-28): every read of a path that came from a
/// repository goes through <c>AxGit.Core/RepositoryFiles</c>, which reads only a positive local form. Read from the
/// sources: the files of the check and of the session reach the disk through no other way, and GitTrust's own file
/// work is that of axKit's own empty hooks folder alone. A new read written beside it fails here.
/// </summary>
public sealed partial class RepositoryFilesTests
{
    /// <summary>The ways .NET reaches a file or a folder's content or attributes.</summary>
    [GeneratedRegex(@"\b(File|Directory|FileInfo|DirectoryInfo|FileStream|StreamReader|FileSystemEnumerable|DriveInfo)\s*[.(<]|new\s+(StreamReader|FileStream|FileInfo|DirectoryInfo|DriveInfo)\b")]
    private static partial Regex DiskWay();

    private static string Source(string file) =>
        File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxGit.Core", file));

    private static IEnumerable<string> Code(string text) =>
        text.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//", StringComparison.Ordinal));

    [Theory]
    [InlineData("GitTrustScan.cs")]
    [InlineData("GitSession.cs")]
    [InlineData("GitIndex.cs")]
    [InlineData("UntrackedFile.cs")]
    public void The_reads_of_a_repositorys_paths_go_through_the_one_function(string file)
    {
        var ways = Code(Source(file)).Where(line => DiskWay().IsMatch(line)).ToList();
        Assert.True(ways.Count == 0, $"{file} reaches the disk past RepositoryFiles: " + string.Join(" | ", ways));
    }

    [Fact]
    public void GitTrust_works_on_the_disk_only_in_axkits_own_hooks_folder()
    {
        var text = Source("GitTrust.cs");
        var start = text.IndexOf("// ---- The hooks folder of a run that must run none ----", StringComparison.Ordinal);
        var end = text.IndexOf("// ----", start + 10, StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var outside = Code(text[..start] + text[end..]).Where(line => DiskWay().IsMatch(line)).ToList();
        Assert.True(outside.Count == 0, "GitTrust reaches the disk outside its hooks folder: " + string.Join(" | ", outside));
    }
}
