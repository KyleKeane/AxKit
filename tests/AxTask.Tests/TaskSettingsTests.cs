using AxTask.Core;

namespace AxTask.Tests;

/// <summary>axTask's settings (docs/axTask/SPEC.md TK-8): the folder and the last list kept; a last list that is rooted or climbs out of the folder is dropped, the safety rule of the file.</summary>
public sealed class TaskSettingsTests
{
    [Fact]
    public void The_last_list_is_kept_only_inside_the_folder()
    {
        using var temp = new TempFolder("axtask-settings");
        var path = Path.Combine(temp.Path, "axTask.json");
        var settings = TaskSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Null(settings.Folder);
        Assert.Null(settings.LastList);

        settings.Folder = temp.Path;
        settings.LastList = "Home";
        settings.HideDone = true;
        settings.Save(path);
        var back = TaskSettings.Load(path, out _);
        Assert.Equal(temp.Path, back.Folder);
        Assert.Equal("Home", back.LastList);
        Assert.True(back.HideDone);

        foreach (var bad in new[] { "..\\Other", "C:\\Else", "  " })
        {
            File.WriteAllText(path, "{ \"lastList\": \"" + bad.Replace("\\", "\\\\") + "\", \"folder\": \" \" }");
            var mended = TaskSettings.Load(path, out _);
            Assert.Null(mended.LastList);
            Assert.Null(mended.Folder);
        }
    }
}