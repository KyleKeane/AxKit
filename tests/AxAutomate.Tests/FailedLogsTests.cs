using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>A failed script's logs kept although the run's folder is deleted (FailedLogs), in a folder of the test's own.</summary>
public sealed class FailedLogsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "axkit-failedlogs-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void A_failed_scripts_logs_are_copied_under_the_runs_folder_by_the_scripts_name()
    {
        var temp = Path.Combine(_root, "run", "02-commands-1");
        var logs = Directory.CreateDirectory(Path.Combine(temp, "home", "AxKit", "logs")).FullName;
        File.WriteAllText(Path.Combine(logs, "axTerminal.log"), "ERROR An exception");
        var kept = Path.Combine(_root, "kept", "1234-010203");

        var folder = FailedLogs.Keep(temp, "axTerminal\\02-commands.txt", kept);

        Assert.Equal(Path.Combine(kept, "axTerminal-02-commands"), folder);
        Assert.Equal("ERROR An exception", File.ReadAllText(Path.Combine(folder!, "axTerminal.log")));
    }

    [Fact]
    public void A_script_that_left_no_log_keeps_nothing()
    {
        var temp = Directory.CreateDirectory(Path.Combine(_root, "run", "empty")).FullName;
        Directory.CreateDirectory(Path.Combine(temp, "home", "AxKit", "logs"));

        Assert.Null(FailedLogs.Keep(temp, "01-start.txt", Path.Combine(_root, "kept")));
        Assert.Null(FailedLogs.Keep(Path.Combine(_root, "none"), "01-start.txt", Path.Combine(_root, "kept")));
        Assert.False(Directory.Exists(Path.Combine(_root, "kept")));
    }

    [Fact]
    public void Only_the_newest_runs_folders_stay()
    {
        var folder = Path.Combine(_root, "failed");
        for (var i = 0; i < FailedLogs.RunsKept + 3; i++)
        {
            var run = Directory.CreateDirectory(Path.Combine(folder, $"run{i:00}")).FullName;
            Directory.SetCreationTimeUtc(run, new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        FailedLogs.Prune(folder);

        var left = Directory.GetDirectories(folder).Select(Path.GetFileName).Order().ToList();
        Assert.Equal(FailedLogs.RunsKept, left.Count);
        Assert.Equal("run03", left[0]);
        Assert.Equal($"run{FailedLogs.RunsKept + 2:00}", left[^1]);
    }
}
