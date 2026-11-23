using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The app keys (docs/axKit/SPEC.md AX-8.18 rule 6; the maintainer's choices of 2026-11-23): Ctrl with a function key
/// opens a standalone app, Ctrl+Shift with one an access layer, each in the bundle's order, the same in every window.
/// </summary>
public sealed class AppLaunchKeysTests
{
    [Fact]
    public void Standalone_apps_are_on_Ctrl_and_access_layers_on_Ctrl_Shift_in_the_bundles_order()
    {
        var keys = AppLaunchKeys.Default;
        Assert.Equal(Dispatch.Apps.Where(app => !app.IsAccessLayer).Take(12).Select(app => app.Name), keys.Where(key => !key.Shift).Select(key => key.App.Name));
        Assert.Equal(Dispatch.Apps.Where(app => app.IsAccessLayer).Take(12).Select(app => app.Name), keys.Where(key => key.Shift).Select(key => key.App.Name));
        Assert.Equal("Ctrl+F1", AppLaunchKeys.Of("axFile")!.Text);
        Assert.Equal("Ctrl+Shift+F1", AppLaunchKeys.Of(keys.First(key => key.Shift).App.Name)!.Text);
        Assert.All(keys, key => Assert.InRange(key.Number, 1, AppLaunchKeys.Count));
    }

    [Fact]
    public void An_app_past_the_twelfth_function_key_has_none()
    {
        var many = Enumerable.Range(1, 14).Select(n => new App($"axApp{n}", $"app{n}", $"AxApp{n}", AppGroup.Utilities, Opens.Nothing)).ToList();
        var keys = AppLaunchKeys.For(many);
        Assert.Equal(12, keys.Count);
        Assert.Null(keys.FirstOrDefault(key => key.App.Name == "axApp13"));
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

        Assert.Equal(Keys.Control | Keys.F1, BundleKeys.LaunchKey(AppLaunchKeys.Of("axFile")!));
        Assert.Null(BundleKeys.AppOfLaunchKey(Keys.F1));
    }
}
