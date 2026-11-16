using AxFile.Core;
using AxKit.Core;

namespace AxFile.Tests;

/// <summary>The seal over the user's own programs of Open with (SPEC.md AF-8.1a, bundle AX-5.6; repairs.md R-53): a program something other than axKit wrote into the settings file is not offered, so never started.</summary>
public sealed class FileSealTests
{
    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axFile.json");
        var settings = new FileSettings { ShowHidden = true, SortBy = SortBy.Date };
        settings.Favorites.Add(@"C:\music");
        settings.Programs.Add(new UserProgram("tidy", @"C:\tools\tidy.ps1"));
        settings.Save(path);
        return path;
    }

    [Fact]
    public void A_sealed_file_loads_as_it_was_saved()
    {
        using var temp = new TempFolder("axfile-seal");
        var path = Saved(temp);
        Assert.True(SettingsText.HasSeal(path));
        var settings = FileSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new UserProgram("tidy", @"C:\tools\tidy.ps1")], settings.Programs);

        // Add a program and Remove a program are sealed again by their save.
        settings.Programs.Add(new UserProgram("pack", @"C:\tools\pack.cmd"));
        settings.Programs.RemoveAt(0);
        settings.Save(path);
        settings = FileSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal([new UserProgram("pack", @"C:\tools\pack.cmd")], settings.Programs);
    }

    [Fact]
    public void A_program_changed_by_hand_is_not_used_and_the_other_settings_stay()
    {
        using var temp = new TempFolder("axfile-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["programs"]![0]!["path"] = @"C:\elsewhere\other.exe");

        var settings = FileSettings.Load(path, out var error);
        Assert.Equal(FileSettings.ProgramsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Empty(settings.Programs);
        Assert.True(settings.ShowHidden);
        Assert.Equal(SortBy.Date, settings.SortBy);
        Assert.Equal([@"C:\music"], settings.Favorites);

        // A setting outside the seal changed by hand is the user's to change.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["showHidden"] = false);
        settings = FileSettings.Load(path, out error);
        Assert.Null(error);
        Assert.False(settings.ShowHidden);
        Assert.Single(settings.Programs);
    }

    [Fact]
    public void A_file_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal()
    {
        using var temp = new TempFolder("axfile-seal");
        var path = temp.File("axFile.json", """{"showHidden": true, "programs": [{"name": "tidy", "path": "C:\\tools\\tidy.ps1"}]}""");
        var settings = FileSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Single(settings.Programs);

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.Single(FileSettings.Load(path, out error).Programs);
        Assert.Null(error);

        SettingsText.RemoveSeal(path);
        settings = FileSettings.Load(path, out error);
        Assert.Equal(FileSettings.ProgramsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.Empty(settings.Programs);
        Assert.True(settings.ShowHidden);
    }
}
