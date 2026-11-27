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

    /// <summary>AX-5.11: what the user writes is in axKit's folder in Documents, never under AppData, which the Store's copy loses with an uninstall.</summary>
    [Fact]
    public void What_the_user_writes_is_in_the_axKit_folder_of_Documents()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Assert.Equal(Path.Combine(documents, "axKit"), AppPaths.UserFolder);
        Assert.Equal(Path.Combine(documents, "axKit", "Templates"), AppPaths.TemplatesRoot);
        Assert.DoesNotContain("AppData", AppPaths.UserFolder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AX-5.11: an update moves Documents\Tasks only when it holds axTask's lists, since the name is a common one.</summary>
    [Fact]
    public void A_folder_is_axTasks_only_when_it_holds_a_list_or_a_smart_list()
    {
        using var temp = new TempFolder("axkit-tests");
        Assert.False(AppPaths.IsTaskFolder(Path.Combine(temp.Path, "missing")));
        temp.File(@"Tasks\notes.txt", "another program's");
        Assert.False(AppPaths.IsTaskFolder(Path.Combine(temp.Path, "Tasks")));
        temp.File(@"Tasks\Home\list.json", "{}");
        Assert.True(AppPaths.IsTaskFolder(Path.Combine(temp.Path, "Tasks")));
        temp.File(@"Smart\All tasks.filter.json", "{}");
        Assert.True(AppPaths.IsTaskFolder(Path.Combine(temp.Path, "Smart")));
    }

    /// <summary>AX-5.11: a folder the user chose in a settings file is never moved by an update.</summary>
    [Fact]
    public void A_settings_file_names_a_folder_whatever_its_case_and_last_separator()
    {
        Assert.True(AppPaths.NamesFolder("""{ "folder": "D:\\Docs\\Tasks\\" }""", "folder", @"d:\docs\tasks"));
        Assert.True(AppPaths.NamesFolder("""{ "WorkingFolder": "D:\\Docs\\axKit Claude" }""", "workingFolder", @"D:\Docs\axKit Claude"));
        Assert.False(AppPaths.NamesFolder("""{ "folder": "D:\\Lists" }""", "folder", @"D:\Docs\Tasks"));
        Assert.False(AppPaths.NamesFolder("""{ "folder": null }""", "folder", @"D:\Docs\Tasks"));
        Assert.False(AppPaths.NamesFolder(null, "folder", @"D:\Docs\Tasks"));
        Assert.False(AppPaths.NamesFolder("{ not json", "folder", @"D:\Docs\Tasks"));
    }

    /// <summary>AX-5.11: axTask's lists are in the new place, but for someone whose lists are in Documents\Tasks and nowhere else.</summary>
    [Fact]
    public void The_tasks_folder_is_the_new_one_unless_only_the_earlier_one_is_there()
    {
        const string user = @"D:\Docs\axKit";
        const string earlier = @"D:\Docs\Tasks";
        const string now = @"D:\Docs\axKit\Tasks";
        Assert.Equal(now, AppPaths.TasksFolderFor(user, earlier, _ => false));
        Assert.Equal(earlier, AppPaths.TasksFolderFor(user, earlier, path => path == earlier));
        Assert.Equal(now, AppPaths.TasksFolderFor(user, earlier, _ => true));
        Assert.Equal(now, AppPaths.TasksFolderFor(user, earlier, path => path == now));
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

    /// <summary>AX-5.11 (review, 2026-11-27): a move that cannot finish leaves the old folder whole and in use, never the lists in two folders.</summary>
    [Fact]
    public void A_folder_with_a_file_in_use_stays_whole_until_it_can_move()
    {
        using var temp = new TempFolder("axkit-tests");
        var old = Path.Combine(temp.Path, "Tasks");
        var current = Path.Combine(temp.Path, "axKit", "Tasks");
        temp.File(@"Tasks\Home\list.json", "{}");
        temp.File(@"Tasks\Home\buy milk.md", "one");
        var held = temp.File(@"Tasks\Work\list.json", "{}");

        using (new FileStream(held, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            AppPaths.TakeOverFolder(old, current);
            Assert.False(Directory.Exists(current));
            Assert.True(File.Exists(Path.Combine(old, "Home", "list.json")));
            Assert.True(File.Exists(Path.Combine(old, "Home", "buy milk.md")));
            // The old folder is the one in use meanwhile.
            Assert.Equal(old, AppPaths.TasksFolderFor(Path.Combine(temp.Path, "axKit"), old, Directory.Exists));
        }

        AppPaths.TakeOverFolder(old, current);
        Assert.False(Directory.Exists(old));
        Assert.Equal("one", File.ReadAllText(Path.Combine(current, "Home", "buy milk.md")));
        Assert.True(File.Exists(Path.Combine(current, "Work", "list.json")));
        Assert.Equal(current, AppPaths.TasksFolderFor(Path.Combine(temp.Path, "axKit"), old, Directory.Exists));
    }

    /// <summary>AX-5.11 (review, 2026-11-27): without a Documents folder the user's folder is under the profile, and a path that is not whole is never moved.</summary>
    [Fact]
    public void Without_a_Documents_folder_the_profiles_is_used_and_a_relative_folder_is_never_moved()
    {
        Assert.Equal(@"D:\Docs", AppPaths.DocumentsFolderFor(@"D:\Docs", @"C:\Profiles\sam"));
        Assert.Equal(@"C:\Profiles\sam\Documents", AppPaths.DocumentsFolderFor(string.Empty, @"C:\Profiles\sam"));
        Assert.True(Path.IsPathFullyQualified(AppPaths.UserFolder));

        using var temp = new TempFolder("axkit-tests");
        var old = Path.Combine(temp.Path, "Tasks");
        temp.File(@"Tasks\list.json", "{}");
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, old);
        if (Path.IsPathFullyQualified(relative))
        {
            // The temporary folder is on another drive than the tests run on: no relative path leads there.
            return;
        }

        Assert.True(Directory.Exists(relative));
        AppPaths.TakeOverFolder(relative, Path.Combine(temp.Path, "axKit", "Tasks"));
        AppPaths.TakeOverFolder(old, Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(temp.Path, "axKit", "Tasks")));
        Assert.True(File.Exists(Path.Combine(old, "list.json")));
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "axKit")));
    }
}
