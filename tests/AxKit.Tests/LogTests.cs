using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The log of a test process (tests/Shared/TestLog.cs): never the user's own folder.</summary>
public sealed class LogTests
{
    [Fact]
    public void A_test_process_logs_into_a_folder_of_its_own_and_never_the_users()
    {
        Assert.Equal(TestLog.Folder, Log.Directory);
        Assert.NotEqual(AppPaths.LogsFolder, Log.Directory, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TestLog.Folder, Path.GetDirectoryName(Log.FilePath));

        Log.Info("A line of the log tests");
        Assert.True(File.Exists(Log.FilePath));
    }

    [Fact]
    public void Each_app_names_its_file_in_the_folder_the_log_is_in()
    {
        var before = Log.FilePath;
        try
        {
            Log.UseApp("axLogTests");
            Assert.Equal(Path.Combine(TestLog.Folder, "axLogTests.log"), Log.FilePath);
        }
        finally
        {
            Log.UseApp(Path.GetFileNameWithoutExtension(before));
        }
    }
}
