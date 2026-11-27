namespace AxPass.Tests;

/// <summary>
/// Nothing of a database reaches the window after the lock (docs/axPass/SPEC.md PW-4.3). The window waits for
/// KeePassXC's tool and for a backup's copy in one way, <c>PassForm.Tool</c>, which gives the answer only while
/// the window is as it was when the work was asked for: not locked since, not unlocked again, not pointed at
/// another database. The review's second pass of 2026-11-27 found the comparison written by hand in three places
/// and missing in two (the unlock itself, the new database). Read off the sources, as
/// <see cref="KeePassCliTextsTests"/> reads them: no wait of the window goes around that one way.
/// </summary>
public sealed class PassWindowTests
{
    private static IEnumerable<(string File, int Number, string Line)> Lines()
    {
        var folder = Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxPass");
        foreach (var file in Directory.GetFiles(folder, "*.cs"))
        {
            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    yield return (Path.GetFileName(file), number, line);
                }
            }
        }
    }

    [Fact]
    public void The_window_starts_work_off_its_thread_in_one_place()
    {
        var starts = Lines().Where(line => line.Line.Contains("Task.Run(", StringComparison.Ordinal)).ToList();
        var one = Assert.Single(starts);
        Assert.Equal("PassForm.cs", one.File);
        Assert.Equal("var result = await Task.Run(work);", one.Line.Trim());

        string[] otherWays = ["ThreadPool.", "new Thread(", "Task.Factory", ".ContinueWith(", "BackgroundWorker", ".ConfigureAwait(false)"];
        var offenders = Lines().Where(line => otherWays.Any(way => line.Line.Contains(way, StringComparison.Ordinal))).Select(line => $"{line.File}:{line.Number}").ToList();
        Assert.True(offenders.Count == 0, "Work off the window's thread started another way than PassForm.Tool, which is what drops an answer that comes after a lock (PW-4.3): " + string.Join(", ", offenders));
    }

    [Fact]
    public void Every_run_of_the_tool_and_every_backup_is_waited_for_through_the_one_way()
    {
        var offenders = Lines()
            .Where(line => line.Line.Contains("KeePassCli.Run(", StringComparison.Ordinal) || line.Line.Contains("Backups.Make(", StringComparison.Ordinal))
            .Where(line => !line.Line.Contains("await Tool(() => ", StringComparison.Ordinal))
            .Select(line => $"{line.File}:{line.Number}: {line.Line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "KeePassXC's tool run, or a backup made, outside PassForm.Tool: its answer could reach the window after a lock (PW-4.3): " + string.Join("; ", offenders));
    }

    /// <summary>The count the one way compares is moved wherever the window's state changes hands: asked for and made unlocks, the locks, Windows' own lock, another database, the close.</summary>
    [Fact]
    public void The_count_moves_at_every_change_of_the_windows_state()
    {
        var moves = Lines().Count(line => line.Line.Trim() == "_unlock++;");
        Assert.True(moves >= 7, $"The unlock count is moved in {moves} places; an unlock asked for, an unlock made, Lock twice (locked and not), Windows' lock, another database and the close are seven.");
    }
}
