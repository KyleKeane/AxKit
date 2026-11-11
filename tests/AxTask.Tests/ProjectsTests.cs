using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>The project folders (docs/axTask/SPEC.md TK-7b): folders of lists found under them, and the search across them, over a temporary folder.</summary>
public sealed class ProjectsTests : IDisposable
{
    private readonly TempFolder _temp = new("axtask-projects");

    private string Root => Path.Combine(_temp.Path, "projects");

    private string Top => Path.Combine(_temp.Path, "Tasks");

    public ProjectsTests()
    {
        List(Path.Combine(Root, "alpha", "tasks", "Home"));
        Item(Path.Combine(Root, "alpha", "tasks", "Home"), "Fix the tap", "Washer of 15 mm");
        Item(Path.Combine(Root, "alpha", "tasks", "Home"), "Water plants", null);
        List(Path.Combine(Root, "alpha", "tasks", "Work"));
        Item(Path.Combine(Root, "alpha", "tasks", "Archive"), "Archived tap", null);
        List(Path.Combine(Root, "beta", "tasks", "Bugs"));
        Item(Path.Combine(Root, "beta", "tasks", "Bugs"), "Tap drips", null);
        List(Path.Combine(Root, "gamma", "Lists"));
        // Too deep, under a skipped folder, under a hidden one, and inside the top folder: never found.
        List(Path.Combine(Root, "a", "b", "c", "d", "e", "Deep"));
        List(Path.Combine(Root, "code", "node_modules", "pkg", "Lists"));
        List(Path.Combine(Root, "code", ".git", "Lists"));
        List(Path.Combine(Root, "code", "bin", "Lists"));
        Directory.CreateDirectory(Top);
        List(Path.Combine(Top, "Home"));
        Item(Path.Combine(Top, "Home"), "Buy a tap", null);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Folders_of_lists_are_found_to_four_folders_down_and_named_apart()
    {
        var found = Projects.Find([Root], Top);
        Assert.Equal(["gamma", "tasks in alpha", "tasks in beta"], found.Select(project => project.Name));
        Assert.Equal(2, found.Single(project => project.Name == "tasks in alpha").Lists);
        Assert.Equal(Path.Combine(Root, "beta", "tasks"), found.Single(project => project.Name == "tasks in beta").Path);
    }

    [Fact]
    public void The_top_folder_is_never_a_project()
    {
        // The top folder under a project folder, and the top folder as the project folder itself.
        Assert.DoesNotContain(Projects.Find([_temp.Path], Top), project => project.Path.Equals(Top, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(Projects.Find([Top], Top));
    }

    [Fact]
    public void A_project_folder_that_is_itself_a_folder_of_lists_is_one_project()
    {
        var alpha = Path.Combine(Root, "alpha", "tasks");
        var found = Projects.Find([alpha, alpha, Path.Combine(_temp.Path, "missing")], Top);
        Assert.Equal(["tasks"], found.Select(project => project.Name));
    }

    [Fact]
    public void Each_project_knows_the_project_folder_it_was_found_under()
    {
        var beta = Path.Combine(Root, "beta");
        var found = Projects.Find([beta, Root], Top);
        // TK-7b.3: a folder under two project folders shows under the first; the rest under the folder that found them.
        Assert.Equal(beta, found.Single(project => project.Name == "tasks in beta").Root);
        Assert.Equal(Root, found.Single(project => project.Name == "gamma").Root);
    }

    [Fact]
    public void A_search_finds_every_word_in_the_title_or_a_text_field_without_minding_case()
    {
        var places = new[] { (Top, "Tasks"), (Path.Combine(Root, "alpha", "tasks"), "tasks in alpha"), (Path.Combine(Root, "beta", "tasks"), "tasks in beta") };
        var hits = Projects.Search(places, "TAP");
        Assert.Equal(["Buy a tap, in Tasks / Home", "Fix the tap, in tasks in alpha / Home", "Tap drips, in tasks in beta / Bugs"], hits.Select(hit => hit.Label));
        Assert.Equal(["Fix the tap"], Projects.Search(places, "tap 15 washer").Select(hit => hit.Title));
        Assert.Empty(Projects.Search(places, "   "));
        Assert.Empty(Projects.Search(places, "Archived"));
    }

    [Fact]
    public void Project_folders_are_full_paths_once()
    {
        var settings = TaskSettings.Load(Path.Combine(_temp.Path, "axTask.json"), out _);
        Assert.Empty(settings.ProjectFolders);
        Assert.Equal("Type the folder's path", settings.RefusedProjectFolder(" "));
        Assert.NotNull(settings.RefusedProjectFolder("projects"));
        Assert.NotNull(settings.RefusedProjectFolder(Path.Combine(_temp.Path, "missing")));
        Assert.Null(settings.RefusedProjectFolder(Root + "\\"));
        Assert.Equal(Root, settings.AddProjectFolder("\"" + Root + "\\\""));
        Assert.Equal("projects is a project folder already", settings.RefusedProjectFolder(Root.ToUpperInvariant()));

        var path = Path.Combine(_temp.Path, "axTask.json");
        File.WriteAllText(path, "{ \"projectFolders\": [\"" + Root.Replace("\\", "\\\\") + "\", \"relative\", \" \", \"" + Root.Replace("\\", "\\\\") + "\"] }");
        Assert.Equal([Root], TaskSettings.Load(path, out _).ProjectFolders);
    }

    [Fact]
    public void The_top_folder_and_a_folder_inside_it_are_refused_as_project_folders()
    {
        var settings = TaskSettings.Load(Path.Combine(_temp.Path, "axTask.json"), out _);
        settings.Folder = Top;
        Assert.Equal("That is your own top folder: its lists show at the top already", settings.RefusedProjectFolder(Top + "\\"));
        Assert.Equal("That is inside your own top folder: its lists show at the top already", settings.RefusedProjectFolder(Path.Combine(Top, "Home")));
        Assert.Null(settings.RefusedProjectFolder(_temp.Path));
    }

    [Fact]
    public void A_folder_whose_lists_were_all_archived_is_the_project_and_never_its_Archive()
    {
        List(Path.Combine(Root, "delta", "tasks", "Archive", "Old"));
        var found = Projects.Find([Path.Combine(Root, "delta")], Top);
        Assert.Equal([Path.Combine(Root, "delta", "tasks")], found.Select(project => project.Path));
        Assert.Equal(0, found[0].Lists);
    }

    [Fact]
    public void Names_still_shared_after_the_folder_above_are_told_apart_by_its_path()
    {
        var other = Path.Combine(_temp.Path, "work");
        List(Path.Combine(other, "alpha", "tasks", "Jobs"));
        var found = Projects.Find([Root, other], Top);
        Assert.Contains(found, project => project.Name == $"tasks in alpha ({Path.Combine(Root, "alpha")})");
        Assert.Contains(found, project => project.Name == $"tasks in alpha ({Path.Combine(other, "alpha")})");
        Assert.Contains(found, project => project.Name == "tasks in beta");
    }

    private static void List(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ItemDocument.ListFile), ItemDocument.NewList([]).ToText());
    }

    private static void Item(string folder, string title, string? notes)
    {
        Directory.CreateDirectory(folder);
        var item = ItemDocument.NewItem(title, []);
        if (notes is not null)
        {
            item = item.WithField(-1, "Notes", FieldTypes.LongText, []).WithValue(1, JsonValue.Create(notes));
        }

        File.WriteAllText(Path.Combine(folder, title + ".json"), item.ToText());
    }
}
