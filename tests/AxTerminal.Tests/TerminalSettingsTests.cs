using AxTerminal.Core;

namespace AxTerminal.Tests;

public sealed class TerminalSettingsTests
{
    [Fact]
    public void Missing_file_gives_the_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "axterminal-tests-" + Guid.NewGuid().ToString("N"), "axTerminal.json");
        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Null(settings.Shell);
        Assert.Equal(ShellLauncher.DefaultKind(), settings.ShellKind);
        Assert.Equal(240, settings.PtyColumns);
        Assert.Equal(50, settings.PtyRows);
        Assert.True(settings.SoundOnBell);
        Assert.True(settings.ClickOnNewLine);
        Assert.False(settings.WorkingMusic);
        Assert.True(settings.AnnounceQuestions);
        Assert.Equal(OutputSpeechMode.All, settings.OutputSpeech);
        Assert.Equal(20000, settings.MaxTranscriptLines);
        Assert.True(settings.CheckForUpdates);
    }

    [Fact]
    public void Nonsense_falls_back_and_values_round_trip()
    {
        using var temp = new TempFolder("axterminal-tests");
        var dir = temp.Path;
        var path = Path.Combine(dir, "axTerminal.json");
        File.WriteAllText(path, """{"shell":"fish","ptyColumns":3,"ptyRows":9000,"maxTranscriptLines":5,"recentFolders":["", "C:\\src"],"outputSpeech":7}""");
        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Null(settings.Shell);
        Assert.Equal(240, settings.PtyColumns);
        Assert.Equal(50, settings.PtyRows);
        Assert.Equal(20000, settings.MaxTranscriptLines);
        Assert.Equal(["C:\\src"], settings.RecentFolders);
        Assert.Equal(OutputSpeechMode.All, settings.OutputSpeech);

        settings.Shell = "cmd";
        settings.OutputSpeech = OutputSpeechMode.All;
        settings.RememberFolder("C:\\work");
        settings.RememberFolder("C:\\src");
        settings.Save(path);
        var again = TerminalSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal(ShellKind.CommandPrompt, again.ShellKind);
        Assert.Equal(OutputSpeechMode.All, again.OutputSpeech);
        Assert.Equal(["C:\\src", "C:\\work"], again.RecentFolders);
        Assert.Equal("C:\\src", again.LastFolder);
    }

    [Fact]
    public void User_shells_round_trip_and_blank_ones_are_dropped()
    {
        using var temp = new TempFolder("axterminal-tests");
        var path = Path.Combine(temp.Path, "axTerminal.json");
        File.WriteAllText(path, """{"userShells":[{"name":"Dev cmd","commandLine":"cmd.exe /k"},{"name":"","commandLine":"x"},{"name":"No line","commandLine":" "},null],"userShellName":"Gone"}""");
        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new UserShell("Dev cmd", "cmd.exe /k")], settings.UserShells);
        Assert.Null(settings.UserShellName);

        settings.UserShells.Add(new UserShell("Plain PowerShell", "powershell -NoProfile"));
        settings.UserShellName = "plain powershell";
        settings.Save(path);
        Assert.Contains("\"commandLine\": \"powershell -NoProfile\"", File.ReadAllText(path));
        var again = TerminalSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal(2, again.UserShells.Count);
        Assert.Equal("plain powershell", again.UserShellName);
        Assert.Equal(new UserShell("Plain PowerShell", "powershell -NoProfile"), again.FindUserShell(again.UserShellName));
    }

    [Fact]
    public void A_new_shell_name_must_be_new_and_not_blank()
    {
        var settings = new TerminalSettings { UserShells = [new UserShell("Dev cmd", "cmd.exe /k")] };
        Assert.Equal("Type a name", settings.RefusedShellName("  "));
        Assert.Equal("A shell named dev CMD is already there", settings.RefusedShellName(" dev CMD "));
        Assert.Equal("A shell named command prompt is already there", settings.RefusedShellName("command prompt"));
        Assert.Null(settings.RefusedShellName("Node"));
        Assert.Null(settings.FindUserShell(null));
        Assert.Null(settings.FindUserShell("Node"));
    }
}
