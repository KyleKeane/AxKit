using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The Store's copy and its background apps (docs/axKit/SPEC.md AX-13.4a, AX-13.4b): which are started hidden as a
/// process starts, by the marker, the version and whether the process is a startup task's own start.
/// </summary>
public class StoreFirstStartTests
{
    private static readonly Func<App, bool> On = _ => false;
    private static readonly Func<App, bool> Off = _ => true;

    [Fact]
    public void Only_the_Stores_copy_outside_a_test_run_starts_background_apps_itself()
    {
        Assert.True(StoreFirstStart.Applies(packaged: true, testRun: false));
        Assert.False(StoreFirstStart.Applies(packaged: false, testRun: false));
        Assert.False(StoreFirstStart.Applies(packaged: true, testRun: true));
    }

    [Fact]
    public void The_first_start_after_an_install_starts_every_background_app_of_both_kinds()
    {
        var verbs = StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: null, "5.11.0", On);

        Assert.Equal(Dispatch.Apps.Where(app => app.AtSignIn || app.InBackground).Select(app => app.Verb), verbs);
        Assert.Contains(Dispatch.AxStart, verbs);
        Assert.Contains(Dispatch.AxTemplate, verbs);
    }

    [Fact]
    public void A_background_app_does_not_start_itself_again()
    {
        var verbs = StoreFirstStart.ToStart(Dispatch.AxStart, startedHidden: true, marker: null, "5.11.0", On);

        Assert.DoesNotContain(Dispatch.AxStart, verbs);
        Assert.Contains(Dispatch.AxClip, verbs);
        // axTemplate's own start is its browser, which starts the launcher itself.
        Assert.DoesNotContain(Dispatch.AxTemplate, StoreFirstStart.ToStart(Dispatch.AxTemplate, startedHidden: false, marker: null, "5.11.0", On));
    }

    [Fact]
    public void After_the_first_start_a_plain_start_of_the_same_version_starts_nothing()
    {
        Assert.Empty(StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: "5.11.0", "5.11.0", On));
        // A window app started with an option of that name is no startup task.
        Assert.Empty(StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: true, marker: "5.11.0", "5.11.0", On));
    }

    [Fact]
    public void The_first_start_after_an_update_starts_the_app_without_a_startup_task_and_no_other()
    {
        // The marker of an older version, and the empty marker written before 2026-11-23.
        Assert.Equal([Dispatch.AxTemplate], StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: "5.10.0", "5.11.0", On));
        Assert.Equal([Dispatch.AxTemplate], StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: "", "5.11.0", On));
    }

    [Fact]
    public void A_startup_tasks_hidden_start_brings_the_app_without_one_along()
    {
        Assert.Equal([Dispatch.AxTemplate], StoreFirstStart.ToStart(Dispatch.AxClip, startedHidden: true, marker: "5.11.0", "5.11.0", On));
        // Shown by its hotkey's own start or by the user: not the sign-in.
        Assert.Empty(StoreFirstStart.ToStart(Dispatch.AxClip, startedHidden: false, marker: "5.11.0", "5.11.0", On));
    }

    [Fact]
    public void Its_own_setting_turned_off_keeps_it_from_starting_after_the_first_start()
    {
        Assert.Empty(StoreFirstStart.ToStart(Dispatch.AxClip, startedHidden: true, marker: "5.11.0", "5.11.0", Off));
        Assert.Empty(StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: "5.10.0", "5.11.0", Off));
        // The setting is never asked of an app that has a startup task: Windows' own page decides for it.
        var asked = new List<string>();
        StoreFirstStart.ToStart(Dispatch.AxFile, startedHidden: false, marker: "5.10.0", "5.11.0", app => { asked.Add(app.Name); return false; });
        Assert.Equal(["axTemplate"], asked);
    }

    [Fact]
    public void The_setting_is_off_only_when_the_file_says_so()
    {
        Assert.True(SignInStart.TurnedOff("{ \"hotkey\": \"Ctrl+Shift+Semicolon\", \"startWithWindows\": false }"));
        Assert.True(SignInStart.TurnedOff("{ \"StartWithWindows\": false }"));
        Assert.False(SignInStart.TurnedOff("{ \"startWithWindows\": true }"));
        Assert.False(SignInStart.TurnedOff("{ \"checkForUpdates\": false }"));
        Assert.False(SignInStart.TurnedOff(null));
        Assert.False(SignInStart.TurnedOff(""));
        Assert.False(SignInStart.TurnedOff("{ not json"));
        Assert.False(SignInStart.TurnedOff("[false]"));
    }

    [Fact]
    public void The_run_key_is_the_users_and_never_a_test_runs_or_the_Store_copys()
    {
        // Review, 2026-11-27: a test run's window with the setting off deleted the user's own value.
        Assert.True(SignInStart.Applies(packaged: false, testRun: false));
        Assert.False(SignInStart.Applies(packaged: false, testRun: true));
        Assert.False(SignInStart.Applies(packaged: true, testRun: false));
        // And Apply asks it with the run's own answer, before it opens the key.
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Core", "SignInStart.cs"));
        var apply = source[source.IndexOf("public static void Apply(", StringComparison.Ordinal)..];
        var asked = apply.IndexOf("if (!Applies(PackageIdentity.IsPackaged, AutomationEnvironment.Requested))", StringComparison.Ordinal);
        Assert.InRange(asked, 0, apply.IndexOf("Registry.CurrentUser", StringComparison.Ordinal));
    }

    [Fact]
    public void The_marker_is_kept_with_the_local_files_which_a_package_removes_with_itself()
    {
        Assert.EndsWith(Path.Combine("AxKit", "store-first-start.done"), StoreFirstStart.MarkerFile);
        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StoreFirstStart.MarkerFile);
    }

    [Fact]
    public void An_app_in_the_background_without_an_entry_is_no_startup_task()
    {
        // The package's manifest gives a startup task to an app with AtSignIn, which needs an entry (AX-13.2).
        Assert.All(Dispatch.Apps.Where(app => app.InBackground), app => Assert.False(app.AtSignIn || app.StoreEntry));
    }
}
