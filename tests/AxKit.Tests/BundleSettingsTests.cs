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
}
