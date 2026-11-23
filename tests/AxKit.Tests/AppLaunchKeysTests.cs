using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The app keys (docs/axKit/SPEC.md AX-8.18 rule 6; the maintainer's choices of 2026-11-23): Ctrl with a function key
/// opens a standalone app, Ctrl+Shift with one an access layer, each group of apps on its own block of four keys.
/// </summary>
public sealed class AppLaunchKeysTests
{
    [Fact]
    public void Each_group_has_its_block_of_four_function_keys()
    {
        Assert.Equal(
            [
                "axStart Ctrl+F1", "axFile Ctrl+F2", "axClip Ctrl+F3", "axTemplate Ctrl+F4",
                "axDown Ctrl+F5", "axJSON Ctrl+F6", "axPreview Ctrl+F7", "axMedia Ctrl+F8",
                "axTask Ctrl+F9", "axCalc Ctrl+F10", "axProcess Ctrl+F11", "axAutomate Ctrl+F12",
                "axTerminal Ctrl+Shift+F1", "axGIT Ctrl+Shift+F2", "axPass Ctrl+Shift+F3",
                "axClaude Ctrl+Shift+F5",
                "axHelp Ctrl+Shift+F9", "axMCP Ctrl+Shift+F10",
            ],
            AppLaunchKeys.Default.Select(key => $"{key.App.Name} {key.Text}"));
    }

    [Fact]
    public void An_app_past_the_fourth_of_its_group_and_a_group_past_the_third_block_have_none()
    {
        App Make(int n, AppGroup group) => new($"axApp{n}", $"app{n}", $"AxApp{n}", group, Opens.Nothing);
        var apps = new List<App>();
        apps.AddRange(Enumerable.Range(1, 5).Select(n => Make(n, AppGroup.GettingAround)));
        apps.Add(Make(6, AppGroup.Documents));
        apps.Add(Make(7, AppGroup.Tools));
        apps.Add(Make(8, AppGroup.Programs));
        var keys = AppLaunchKeys.For(apps);
        Assert.Equal(["axApp1 Ctrl+F1", "axApp2 Ctrl+F2", "axApp3 Ctrl+F3", "axApp4 Ctrl+F4", "axApp6 Ctrl+F5", "axApp7 Ctrl+F9"], keys.Select(key => $"{key.App.Name} {key.Text}"));
    }

    [Fact]
    public void No_group_holds_more_apps_than_a_block_has_keys()
    {
        Assert.All(Dispatch.Apps.GroupBy(app => app.Group), group => Assert.InRange(group.Count(), 1, AppLaunchKeys.Block));
        Assert.Equal(Dispatch.Apps.Count, AppLaunchKeys.Default.Count);
    }

    /// <summary>
    /// The apps page (written by tools/apps-page.ps1, which works the keys out by the same rule) gives every app the
    /// key the code gives it, under the heading of its kind and group: no document holds the keys by hand (the
    /// maintainer, 2026-11-23: "make the documentation fuly automatically made from the underlyfing data").
    /// </summary>
    [Fact]
    public void The_apps_page_gives_every_app_its_key_kind_and_group()
    {
        var page = File.ReadAllText(Path.Combine(LayoutTests.RepositoryRoot(), "docs", "axKit", "apps.md")).Replace("\r\n", "\n");
        var keys = AppLaunchKeys.Default;
        foreach (var app in Dispatch.Apps)
        {
            var at = page.IndexOf($"\n### {app.Name}\n", StringComparison.Ordinal);
            Assert.True(at > 0, $"{app.Name} has no heading in apps.md: run tools/apps-page.ps1");
            var heading = page[..at].Split('\n').Last(line => line.StartsWith("## ", StringComparison.Ordinal));
            Assert.Equal($"## {(app.IsAccessLayer ? "Access layers" : "Apps")}: {Dispatch.GroupName(app.Group)}", heading);
            var next = page.IndexOf("\n### ", at + 1, StringComparison.Ordinal);
            var section = next < 0 ? page[at..] : page[at..next];
            var key = keys.First(k => k.App == app);
            Assert.Contains($"- key {key.Text} in every axKit window;", section);
        }
    }

    [Fact]
    public void The_bundle_keys_hold_every_app_key_and_each_finds_its_app()
    {
        foreach (var key in AppLaunchKeys.Default)
        {
            var keys = BundleKeys.LaunchKey(key);
            Assert.Contains(BundleKeys.All, entry => entry.Key == keys);
            Assert.Equal(key.App, BundleKeys.AppOfLaunchKey(keys));
        }

        Assert.Equal(Keys.Control | Keys.F2, BundleKeys.LaunchKey(AppLaunchKeys.Of("axFile")!));
        Assert.Null(BundleKeys.AppOfLaunchKey(Keys.F1));
        Assert.Null(BundleKeys.AppOfLaunchKey(Keys.Control | Keys.Shift | Keys.F4));
    }
}
