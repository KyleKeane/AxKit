using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The take-over of the folders of the bundle's earlier name (docs/axKit/SPEC.md AX-5.4, B-16): what the old folder
/// holds moves into the new one, what the new one has stays, and the old folder goes once it is empty.
/// </summary>
public sealed class AppPathsTests
{
    /// <summary>The background apps' one-at-a-time mutex (axStart AS-1.2): the user's name outside a test run, where the tests themselves run.</summary>
    [Fact]
    public void The_one_at_a_time_name_is_the_apps_under_the_users_home()
    {
        Assert.Equal(@"Local\AxKit.AxStart", AppPaths.OneAtATime("AxStart"));
        Assert.Equal(@"Local\AxKit.AxHelp", AppPaths.OneAtATime("AxHelp"));
    }

    [Fact]
    public void A_whole_folder_moves_when_the_new_one_is_missing_and_nothing_happens_without_an_old_one()
    {
        using var temp = new TempFolder("axkit-tests");
        var old = Path.Combine(temp.Path, "Axit");
        var current = Path.Combine(temp.Path, "AxKit");
        temp.File(@"Axit\recently-closed.json", "[]");
        temp.File(@"Axit\updates\4.0.0\install.ps1", "# installer");

        AppPaths.TakeOverFolder(old, current);
        Assert.False(Directory.Exists(old));
        Assert.Equal("[]", File.ReadAllText(Path.Combine(current, "recently-closed.json")));
        Assert.Equal("# installer", File.ReadAllText(Path.Combine(current, "updates", "4.0.0", "install.ps1")));

        AppPaths.TakeOverFolder(old, current);
        Assert.True(Directory.Exists(current));
    }

    [Fact]
    public void What_the_new_folder_has_stays_and_the_rest_is_taken_over_entry_by_entry()
    {
        using var temp = new TempFolder("axkit-tests");
        var old = Path.Combine(temp.Path, "Axit");
        var current = Path.Combine(temp.Path, "AxKit");
        temp.File(@"Axit\axCalc.json", "{ \"old\": 1 }");
        temp.File(@"Axit\logs\axCalc.log", "old log");
        temp.File(@"Axit\logs\axDown.log", "moved log");
        temp.File(@"AxKit\axCalc.json", "{ \"new\": 2 }");
        temp.File(@"AxKit\logs\axCalc.log", "new log");

        AppPaths.TakeOverFolder(old, current);
        Assert.Equal("{ \"new\": 2 }", File.ReadAllText(Path.Combine(current, "axCalc.json")));
        Assert.Equal("new log", File.ReadAllText(Path.Combine(current, "logs", "axCalc.log")));
        Assert.Equal("moved log", File.ReadAllText(Path.Combine(current, "logs", "axDown.log")));
        // What the new folder had stays where it was, and so does the old folder, for a later start.
        Assert.True(File.Exists(Path.Combine(old, "axCalc.json")));
        Assert.True(File.Exists(Path.Combine(old, "logs", "axCalc.log")));
        Assert.False(File.Exists(Path.Combine(old, "logs", "axDown.log")));

        File.Delete(Path.Combine(old, "axCalc.json"));
        File.Delete(Path.Combine(old, "logs", "axCalc.log"));
        AppPaths.TakeOverFolder(old, current);
        Assert.False(Directory.Exists(old));
    }
}
