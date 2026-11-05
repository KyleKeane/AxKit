using AxKit.Core;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>
/// The take-over of the folders of the bundle's earlier name (docs/axkit/SPEC.md AX-5.4, B-16): what the old folder
/// holds moves into the new one, what the new one has stays, and the old folder goes once it is empty.
/// </summary>
public sealed class AppPathsTests
{
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
        temp.File(@"Axit\axcalc.json", "{ \"old\": 1 }");
        temp.File(@"Axit\logs\axcalc.log", "old log");
        temp.File(@"Axit\logs\axdown.log", "moved log");
        temp.File(@"AxKit\axcalc.json", "{ \"new\": 2 }");
        temp.File(@"AxKit\logs\axcalc.log", "new log");

        AppPaths.TakeOverFolder(old, current);
        Assert.Equal("{ \"new\": 2 }", File.ReadAllText(Path.Combine(current, "axcalc.json")));
        Assert.Equal("new log", File.ReadAllText(Path.Combine(current, "logs", "axcalc.log")));
        Assert.Equal("moved log", File.ReadAllText(Path.Combine(current, "logs", "axdown.log")));
        // What the new folder had stays where it was, and so does the old folder, for a later start.
        Assert.True(File.Exists(Path.Combine(old, "axcalc.json")));
        Assert.True(File.Exists(Path.Combine(old, "logs", "axcalc.log")));
        Assert.False(File.Exists(Path.Combine(old, "logs", "axdown.log")));

        File.Delete(Path.Combine(old, "axcalc.json"));
        File.Delete(Path.Combine(old, "logs", "axcalc.log"));
        AppPaths.TakeOverFolder(old, current);
        Assert.False(Directory.Exists(old));
    }
}
