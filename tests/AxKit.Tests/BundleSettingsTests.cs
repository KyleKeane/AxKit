using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The bundle's one update switch (docs/axKit/SPEC.md AX-3.1, 2026-11-24): on by default, off when any app of before had turned it off.</summary>
public sealed class BundleSettingsTests
{
    [Fact]
    public void Without_the_file_the_switch_follows_the_apps_of_before()
    {
        Assert.True(BundleSettings.FromApps([]));
        Assert.True(BundleSettings.FromApps([null, "{\"checkForUpdates\": true}", "{}"]));
        Assert.False(BundleSettings.FromApps(["{\"checkForUpdates\": true}", "{\"fontSize\": 12, \"checkForUpdates\": false}"]));
        // A file that does not read decides nothing.
        Assert.True(BundleSettings.FromApps(["not json", "[1, 2]"]));
    }

    [Fact]
    public void The_file_once_written_is_what_counts()
    {
        using var temp = new TempFolder("bundle-settings-tests");
        var path = Path.Combine(temp.Path, "bundle.json");
        Assert.False(BundleSettings.Load(path, () => ["{\"checkForUpdates\": false}"]).CheckForUpdates);

        new BundleSettings { CheckForUpdates = true }.Save(path);
        Assert.True(BundleSettings.Load(path, () => ["{\"checkForUpdates\": false}"]).CheckForUpdates);
    }

    [Fact]
    public void A_file_that_does_not_read_gives_no_update_check_for_that_run()
    {
        // Review, 2026-11-27: the default is on, so a damaged or held file brought back a check the user had turned off.
        using var temp = new TempFolder("bundle-settings-tests");
        var path = Path.Combine(temp.Path, "bundle.json");
        new BundleSettings { CheckForUpdates = false }.Save(path);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.False(BundleSettings.Load(path, () => []).CheckForUpdates);
        }

        var damaged = Path.Combine(temp.Path, "damaged", "bundle.json");
        Directory.CreateDirectory(Path.GetDirectoryName(damaged)!);
        File.WriteAllText(damaged, "{ not json");
        Assert.False(BundleSettings.Load(damaged, () => []).CheckForUpdates);

        // One value that does not read: the rest is the file's, and the check stays off.
        var partly = Path.Combine(temp.Path, "partly", "bundle.json");
        Directory.CreateDirectory(Path.GetDirectoryName(partly)!);
        File.WriteAllText(partly, "{ \"checkForUpdates\": \"no\", \"keepEventLog\": true }");
        var read = BundleSettings.Load(partly, () => []);
        Assert.False(read.CheckForUpdates);
        Assert.True(read.KeepEventLog);

        // A file that reads is what it says.
        var whole = Path.Combine(temp.Path, "whole", "bundle.json");
        new BundleSettings { CheckForUpdates = true }.Save(whole);
        Assert.True(BundleSettings.Load(whole, () => []).CheckForUpdates);
    }
}
