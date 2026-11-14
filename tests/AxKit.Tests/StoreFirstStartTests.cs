using AxKit.Core;

namespace AxKit.Tests;

/// <summary>AX-13.4a: the background apps started once by the first start of the Store's copy.</summary>
public class StoreFirstStartTests
{
    [Fact]
    public void Only_the_Stores_copy_outside_a_test_run_without_the_marker_starts_them()
    {
        Assert.True(StoreFirstStart.Due(packaged: true, testRun: false, markerExists: false));
        Assert.False(StoreFirstStart.Due(packaged: false, testRun: false, markerExists: false));
        Assert.False(StoreFirstStart.Due(packaged: true, testRun: true, markerExists: false));
        Assert.False(StoreFirstStart.Due(packaged: true, testRun: false, markerExists: true));
    }

    [Fact]
    public void A_window_app_starts_every_app_that_runs_from_sign_in()
    {
        var verbs = StoreFirstStart.Verbs(Dispatch.AxFile);

        Assert.Equal(Dispatch.Apps.Where(app => app.AtSignIn).Select(app => app.Verb), verbs);
        Assert.Contains(Dispatch.AxStart, verbs);
    }

    [Fact]
    public void A_background_app_does_not_start_itself_again()
    {
        var verbs = StoreFirstStart.Verbs(Dispatch.AxStart);

        Assert.DoesNotContain(Dispatch.AxStart, verbs);
        Assert.Contains(Dispatch.AxClip, verbs);
    }

    [Fact]
    public void The_marker_is_kept_with_the_local_files_which_a_package_removes_with_itself()
    {
        Assert.EndsWith(Path.Combine("AxKit", "store-first-start.done"), StoreFirstStart.MarkerFile);
        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StoreFirstStart.MarkerFile);
    }
}
