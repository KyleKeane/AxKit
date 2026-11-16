using AxKit.Core;
using AxTerminal.Core;

namespace AxTerminal.Tests;

/// <summary>
/// The seal over axTerminal's shells (SPEC.md AT-8.1a, bundle AX-5.6; repairs.md R-53): a shell or a command line
/// that something other than axKit wrote into the settings file is not started, and the consent for a command line
/// of the user's own is for that command line (AT-1.6b).
/// </summary>
public sealed class TerminalSealTests
{
    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axTerminal.json");
        var settings = new TerminalSettings { Shell = "custom", CustomShell = "cmd.exe /k", OutputSpeech = OutputSpeechMode.None };
        settings.UserShells.Add(new UserShell("Dev cmd", "cmd.exe /k"));
        settings.UserShellName = "Dev cmd";
        settings.RememberFolder(@"C:\work");
        settings.Save(path);
        return path;
    }

    [Fact]
    public void A_sealed_file_loads_as_it_was_saved()
    {
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        Assert.True(SettingsText.HasSeal(path));

        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("custom", settings.Shell);
        Assert.Equal("cmd.exe /k", settings.CustomShell);
        Assert.Equal([new UserShell("Dev cmd", "cmd.exe /k")], settings.UserShells);
        Assert.Equal("Dev cmd", settings.UserShellName);

        // A change through axKit's own menus is sealed again by its save.
        settings.UserShells.Add(new UserShell("Plain", "powershell -NoProfile"));
        settings.RemoveUserShell("Dev cmd");
        settings.Save(path);
        var again = TerminalSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal([new UserShell("Plain", "powershell -NoProfile")], again.UserShells);
    }

    [Fact]
    public void A_command_line_changed_by_hand_is_not_used_and_the_other_settings_stay()
    {
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["userShells"]![0]!["commandLine"] = @"C:\elsewhere\other.exe");

        var settings = TerminalSettings.Load(path, out var error);
        Assert.Equal(TerminalSettings.ShellsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Null(settings.Shell);
        Assert.Null(settings.CustomShell);
        Assert.Empty(settings.UserShells);
        Assert.Null(settings.UserShellName);
        // What decides nothing that is run is kept.
        Assert.Equal(OutputSpeechMode.None, settings.OutputSpeech);
        Assert.Equal(@"C:\work", settings.LastFolder);

        // Said once: the window's next save seals the file as it is now in use.
        settings.Save(path);
        TerminalSettings.Load(path, out error);
        Assert.Null(error);
    }

    [Fact]
    public void A_changed_custom_command_line_or_shell_word_is_refused_too()
    {
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["customShell"] = "other.exe");
        TerminalSettings.Load(path, out var error);
        Assert.Equal(TerminalSettings.ShellsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);

        path = Saved(temp);
        SettingsText.Change(path, whole => whole["shell"] = "cmd");
        TerminalSettings.Load(path, out error);
        Assert.Equal(TerminalSettings.ShellsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);

        // A setting outside the seal changed by hand is the user's to change.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["outputSpeech"] = "all");
        var settings = TerminalSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal(OutputSpeechMode.All, settings.OutputSpeech);
        Assert.Single(settings.UserShells);
    }

    [Fact]
    public void A_file_from_before_the_seal_is_accepted_and_sealed_by_the_next_save()
    {
        using var temp = new TempFolder("axterminal-seal");
        var path = Path.Combine(temp.Path, "axTerminal.json");
        File.WriteAllText(path, """{"shell":"custom","customShell":"cmd.exe /k","userShells":[{"name":"Dev cmd","commandLine":"cmd.exe /k"}]}""");

        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("cmd.exe /k", settings.CustomShell);
        Assert.Single(settings.UserShells);

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.Single(TerminalSettings.Load(path, out error).UserShells);
        Assert.Null(error);

        // Taking the seal out afterwards is no way around it: this install has sealed the file.
        SettingsText.RemoveSeal(path);
        settings = TerminalSettings.Load(path, out error);
        Assert.Equal(TerminalSettings.ShellsNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.Empty(settings.UserShells);
        Assert.Null(settings.CustomShell);
    }

    [Fact]
    public void A_consent_for_one_command_line_is_not_a_consent_for_another()
    {
        using var temp = new TempFolder("axterminal-seal");
        var file = Path.Combine(temp.Path, "consent.json");
        var agreed = ConsentTarget.CommandLineShell("Dev cmd", "cmd.exe /k");
        Consent.Record(file, "axTerminal", agreed.Program, "5.9.0", new DateTime(2026, 11, 16));

        Assert.True(Consent.Given(file, "axTerminal", ConsentTarget.CommandLineShell("Dev cmd", "cmd.exe /k").Program));
        Assert.False(Consent.Given(file, "axTerminal", ConsentTarget.CommandLineShell("Dev cmd", @"C:\elsewhere\other.exe").Program));
        Assert.False(Consent.Given(file, "axTerminal", ConsentTarget.CommandLineShell("Custom shell", "cmd.exe /k").Program));
        // The agreement by name alone, as it was recorded before 2026-11-16, is not one for any command line.
        Consent.Record(file, "axTerminal", "Custom shell", "5.9.0", new DateTime(2026, 11, 16));
        Assert.False(Consent.Given(file, "axTerminal", ConsentTarget.CommandLineShell("Custom shell", "pwsh -NoProfile").Program));
        // The question names the command line.
        Assert.Contains("cmd.exe /k", agreed.Program);
        Assert.Contains("cmd.exe /k", agreed.Work);
    }

    [Fact]
    public void A_save_keeps_another_windows_shells_under_a_seal_that_checks()
    {
        // R-62 (bundle AX-5.7): the other window's save, made first so that its text carries a real seal.
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        var before = File.ReadAllText(path);
        var other = TerminalSettings.Load(path, out _);
        other.UserShells.Add(new UserShell("Plain", "powershell -NoProfile"));
        other.OutputSpeech = OutputSpeechMode.All;
        other.Save(path);
        var theirs = File.ReadAllText(path);
        File.WriteAllText(path, before);

        // This window loaded the file before that save, and changes another sealed field.
        var mine = TerminalSettings.Load(path, out _);
        File.WriteAllText(path, theirs);
        mine.CustomShell = "pwsh -NoLogo";
        mine.Save(path);

        var now = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("pwsh -NoLogo", now.CustomShell);
        Assert.Equal([new UserShell("Dev cmd", "cmd.exe /k"), new UserShell("Plain", "powershell -NoProfile")], now.UserShells);
        Assert.Equal(OutputSpeechMode.All, now.OutputSpeech);
    }

    [Fact]
    public void A_save_takes_nothing_from_a_file_whose_seal_does_not_check()
    {
        // R-53 stands under R-62: a shell written into the file while a window is open is not carried into its save.
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        var mine = TerminalSettings.Load(path, out _);
        SettingsText.Change(path, whole =>
        {
            whole["userShells"]![0]!["commandLine"] = @"C:\elsewhere\other.exe";
            whole["outputSpeech"] = "all";
        });
        mine.AnnounceQuestions = false;
        mine.Save(path);

        var now = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new UserShell("Dev cmd", "cmd.exe /k")], now.UserShells);
        Assert.Equal(OutputSpeechMode.None, now.OutputSpeech);
        Assert.False(now.AnnounceQuestions);
        // What was found is kept beside the file, as at a load (AX-5.6).
        Assert.True(File.Exists(path + ".changed"));
    }

    [Fact]
    public void A_member_this_version_does_not_know_is_kept_and_is_under_no_seal()
    {
        // R-67 (bundle AX-5.8): a setting a newer version wrote stays in the file through a load and a save.
        using var temp = new TempFolder("axterminal-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["aNewerSetting"] = new System.Text.Json.Nodes.JsonObject { ["level"] = 3 });

        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Single(settings.UserShells);
        settings.OutputSpeech = OutputSpeechMode.All;
        settings.Save(path);

        var whole = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal(3, whole["aNewerSetting"]!["level"]!.GetValue<int>());
        Assert.False(whole.ContainsKey("otherMembers"));
        Assert.Single(TerminalSettings.Load(path, out error).UserShells);
        Assert.Null(error);
    }
}
