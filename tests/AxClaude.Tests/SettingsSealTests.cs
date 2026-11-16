using AxClaude.Core;
using AxKit.Core;

namespace AxClaude.Tests;

/// <summary>The seal over axClaude's claudePath (SPEC.md Appendix C, bundle AX-5.6; repairs.md R-53): a program something other than axKit wrote into the settings file is not started as Claude Code.</summary>
public sealed class SettingsSealTests
{
    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axClaude.json");
        var settings = new AppSettings { ClaudePath = @"C:\tools\claude.exe", SpeakToolCalls = true };
        settings.RememberFolder(@"C:\work");
        settings.Save(path);
        return path;
    }

    [Fact]
    public void A_sealed_file_loads_as_it_was_saved()
    {
        using var temp = new TempFolder("axclaude-seal");
        var path = Saved(temp);
        Assert.True(SettingsText.HasSeal(path));
        var settings = AppSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(@"C:\tools\claude.exe", settings.ClaudePath);

        // Locate claude.exe, and the clearing after an install, are sealed again by their save.
        settings.ClaudePath = @"C:\other\claude.cmd";
        settings.Save(path);
        Assert.Equal(@"C:\other\claude.cmd", AppSettings.Load(path, out error).ClaudePath);
        Assert.Null(error);
        settings.ClaudePath = null;
        settings.Save(path);
        Assert.Null(AppSettings.Load(path, out error).ClaudePath);
        Assert.Null(error);
    }

    [Fact]
    public void A_path_changed_by_hand_is_not_used_and_the_other_settings_stay()
    {
        using var temp = new TempFolder("axclaude-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["claudePath"] = @"C:\elsewhere\other.exe");

        var settings = AppSettings.Load(path, out var error);
        Assert.Equal(AppSettings.ClaudePathNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Null(settings.ClaudePath);
        Assert.True(settings.SpeakToolCalls);
        Assert.Equal(@"C:\work", settings.LastProjectFolder);

        // A setting outside the seal changed by hand is the user's to change.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["speakToolCalls"] = false);
        settings = AppSettings.Load(path, out error);
        Assert.Null(error);
        Assert.False(settings.SpeakToolCalls);
        Assert.Equal(@"C:\tools\claude.exe", settings.ClaudePath);
    }

    [Fact]
    public void A_file_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal()
    {
        using var temp = new TempFolder("axclaude-seal");
        var path = temp.File("axClaude.json", """{"claudePath": "C:\\tools\\claude.exe", "speakToolCalls": true}""");
        var settings = AppSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(@"C:\tools\claude.exe", settings.ClaudePath);

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.Equal(@"C:\tools\claude.exe", AppSettings.Load(path, out error).ClaudePath);
        Assert.Null(error);

        SettingsText.RemoveSeal(path);
        settings = AppSettings.Load(path, out error);
        Assert.Equal(AppSettings.ClaudePathNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.Null(settings.ClaudePath);
        Assert.True(settings.SpeakToolCalls);
    }
}
