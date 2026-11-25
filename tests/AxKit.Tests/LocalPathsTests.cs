using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Which paths a menu may check as it opens (docs/axKit/SPEC.md AX-9.4, AX-9.5): local ones, never a share.</summary>
public sealed class LocalPathsTests
{
    [Fact]
    public void A_share_and_a_rootless_path_are_not_local_and_a_fixed_drive_is()
    {
        Assert.False(LocalPaths.IsLocal(@"\\server\share\notes.md"));
        Assert.False(LocalPaths.IsLocal("notes.md"));
        Assert.False(LocalPaths.IsLocal(string.Empty));
        // The system drive is a fixed one on every Windows the tests run on.
        Assert.True(LocalPaths.IsLocal(Path.GetTempPath()));
    }

    [Fact]
    public void Existence_is_asked_of_a_local_path_only()
    {
        var asked = new List<string>();
        bool Exists(string path)
        {
            asked.Add(path);
            return false;
        }

        Assert.True(LocalPaths.ExistsQuickly(@"\\server\share\notes.md", Exists));
        Assert.Empty(asked);
        Assert.False(LocalPaths.ExistsQuickly(Path.Combine(Path.GetTempPath(), "nope"), Exists));
        Assert.Single(asked);
    }

    [Fact]
    public void A_remembered_path_on_the_network_is_not_asked_about_at_a_start_and_is_said()
    {
        // R-64 (AX-9.5a): the window opens on its fallback and says where the path is and what opens it.
        var asked = new List<string>();
        bool Exists(string path)
        {
            asked.Add(path);
            return path.EndsWith("there", StringComparison.Ordinal);
        }

        Assert.Null(LocalPaths.AtStart(@"\\server\share\project", Exists, out var notOpened));
        Assert.Empty(asked);
        Assert.Equal(LocalPaths.NotOpenedAtStart(@"\\server\share\project"), notOpened);
        Assert.StartsWith(@"\\server\share\project is on the network", notOpened);
        Assert.EndsWith(LocalPaths.OpenRecent + " opens it.", notOpened);

        // The command that opens it is the window's own where it has no Open recent submenu.
        LocalPaths.AtStart(@"\\server\share\mine.kdbx", Exists, out notOpened, "Open database");
        Assert.Equal(LocalPaths.NotOpenedAtStart(@"\\server\share\mine.kdbx", "Open database"), notOpened);

        // A path on this computer is asked about and nothing is said, there or not; nothing remembered and a path without a drive are only not found.
        var there = Path.Combine(Path.GetTempPath(), "there");
        Assert.Equal(there, LocalPaths.AtStart(there, Exists, out notOpened));
        Assert.Null(notOpened);
        Assert.Null(LocalPaths.AtStart(Path.Combine(Path.GetTempPath(), "gone"), Exists, out notOpened));
        Assert.Null(notOpened);
        Assert.Equal(2, asked.Count);
        Assert.Null(LocalPaths.AtStart(null, Exists, out notOpened));
        Assert.Null(LocalPaths.AtStart("there", Exists, out notOpened));
        Assert.Null(notOpened);
        Assert.Equal(2, asked.Count);
    }

    [Fact]
    public void No_window_asks_whether_a_remembered_path_exists_except_through_LocalPaths()
    {
        // Read off the sources, as ListTests does: a line that names a path the settings remember (settings.LastFolder,
        // settings.RecentRepositories, settings.Folder) together with Directory.Exists or File.Exists goes through
        // LocalPaths (the start, AX-9.5a) or RecentMenu (the Open recent submenus, AX-9.5), which ask only about a
        // path on a drive of this computer. A check written over two lines is not seen.
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (System.Text.RegularExpressions.Regex.IsMatch(line, @"[sS]ettings\.(Last\w*|Recent\w*|Folder|TopFolder)\b")
                    && System.Text.RegularExpressions.Regex.IsMatch(line, @"\b(Directory|File)\.Exists\b")
                    && !line.Contains("LocalPaths.", StringComparison.Ordinal)
                    && !line.Contains("RecentMenu.", StringComparison.Ordinal)
                    && !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A remembered path asked about outside LocalPaths, which could wait on the network while no window shows (AX-9.5a, repairs R-64): " + string.Join("; ", offenders));
    }
}
