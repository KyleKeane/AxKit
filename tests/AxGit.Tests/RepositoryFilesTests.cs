using System.Text.RegularExpressions;

namespace AxGit.Tests;

/// <summary>
/// The check that closes P2's class (the review's fifth pass of 2026-11-28; L3 of the sixth): every read of a path
/// that came from a repository goes through <c>AxGit.Core/RepositoryFiles</c>, which reads only a positive local form.
/// Read from the sources: the files of the check, the session, the reach and the remote, and the window's, reach the
/// disk through no other way, and GitTrust's own file work is that of axKit's own empty hooks folder alone. A new read
/// written beside it fails here. A line of the window's that touches a path that is not the repository's (the folder
/// the user chose, git's own program) says so at its end, <see cref="NotTheRepositorys"/> and why.
/// </summary>
public sealed partial class RepositoryFilesTests
{
    /// <summary>The ways .NET reaches a file or a folder's content or attributes, handles and memory maps among them.</summary>
    [GeneratedRegex(@"\b(File|Directory|FileInfo|DirectoryInfo|FileStream|StreamReader|FileSystemEnumerable|DriveInfo|RandomAccess|MemoryMappedFile|FileSystemWatcher)\s*[.(<]|new\s+(StreamReader|FileStream|FileInfo|DirectoryInfo|DriveInfo|FileSystemWatcher)\b|\bPath\s*\.\s*Exists\b|\bFileSystemInfo\b|\bSafeFileHandle\b")]
    private static partial Regex DiskWay();

    /// <summary>What a window's line that touches a path not of the repository's ends with, before its reason.</summary>
    private const string NotTheRepositorys = "// Not a path of the repository's:";

    private static string Source(string project, string file) =>
        File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", project, file));

    private static IEnumerable<string> Code(string text) =>
        text.Split('\n').Select(line => line.Trim()).Where(line => !line.StartsWith("//", StringComparison.Ordinal) && !line.StartsWith("///", StringComparison.Ordinal));

    [Theory]
    [InlineData("AxGit.Core", "GitTrustScan.cs")]
    [InlineData("AxGit.Core", "GitSession.cs")]
    [InlineData("AxGit.Core", "GitIndex.cs")]
    [InlineData("AxGit.Core", "UntrackedFile.cs")]
    [InlineData("AxGit.Core", "GitReach.cs")]
    [InlineData("AxGit.Core", "GitRemote.cs")]
    public void The_reads_of_a_repositorys_paths_go_through_the_one_function(string project, string file)
    {
        var ways = Code(Source(project, file)).Where(line => DiskWay().IsMatch(line)).ToList();
        Assert.True(ways.Count == 0, $"{file} reaches the disk past RepositoryFiles: " + string.Join(" | ", ways));
    }

    /// <summary>The window's files: a line that reaches the disk names a path that is not the repository's, and says why at its end.</summary>
    [Fact]
    public void The_window_reads_no_path_of_the_repositorys_past_the_one_function()
    {
        var folder = Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxGit");
        var files = Directory.GetFiles(folder, "GitForm*.cs");
        Assert.NotEmpty(files);
        var ways = new List<string>();
        foreach (var file in files)
        {
            foreach (var line in Code(File.ReadAllText(file)).Where(line => DiskWay().IsMatch(line)))
            {
                var at = line.IndexOf(NotTheRepositorys, StringComparison.Ordinal);
                if (at < 0 || line[(at + NotTheRepositorys.Length)..].Trim().Length < 10)
                {
                    ways.Add($"{Path.GetFileName(file)}: {line}");
                }
            }
        }

        Assert.True(ways.Count == 0, "The window reaches the disk past RepositoryFiles: " + string.Join(" | ", ways));
    }

    [Fact]
    public void GitTrust_works_on_the_disk_only_in_axkits_own_hooks_folder()
    {
        var text = Source("AxGit.Core", "GitTrust.cs");
        var start = text.IndexOf("// ---- The hooks folder of a run that must run none ----", StringComparison.Ordinal);
        var end = text.IndexOf("// ----", start + 10, StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var outside = Code(text[..start] + text[end..]).Where(line => DiskWay().IsMatch(line)).ToList();
        Assert.True(outside.Count == 0, "GitTrust reaches the disk outside its hooks folder: " + string.Join(" | ", outside));
    }

    /// <summary>The pattern itself: each way it is to catch, caught.</summary>
    [Theory]
    [InlineData("var ok = Path.Exists(path);")]
    [InlineData("using var handle = File.OpenHandle(path);")]
    [InlineData("RandomAccess.Read(handle, buffer, 0);")]
    [InlineData("using var map = MemoryMappedFile.CreateFromFile(path);")]
    [InlineData("var watcher = new FileSystemWatcher(folder);")]
    [InlineData("SafeFileHandle handle = null;")]
    [InlineData("foreach (FileSystemInfo entry in list)")]
    public void Every_way_to_the_disk_is_caught(string line) => Assert.Matches(DiskWay(), line);
}
