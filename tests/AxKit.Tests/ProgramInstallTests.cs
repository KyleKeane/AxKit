using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The programs an app may install on the user's command (docs/axKit/SPEC.md AX-2.9): only from their makers, with every question answered.</summary>
public class ProgramInstallTests
{
    [Fact]
    public void WingetInstallsTheExactPackageFromItsOwnSourceSilently() =>
        Assert.Equal(["install", "--id", "Git.Git", "--exact", "--source", "winget", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity"],
            ProgramInstall.WingetArguments("Git.Git"));

    [Fact]
    public void KeePassXCIsInstalledWithoutItsSignInStartAndWithoutItsWindow()
    {
        Assert.Equal(["install", "--id", "KeePassXCTeam.KeePassXC", "--exact", "--source", "winget", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity",
                "--custom", "AUTOSTARTPROGRAM=0 LAUNCHAPPONEXIT=0"],
            ProgramInstall.WingetArguments(ProgramInstall.KeePassXC.WingetId!, ProgramInstall.KeePassXC.InstallerArguments));
        Assert.Null(ProgramInstall.Git.InstallerArguments);
        Assert.Null(ProgramInstall.PowerShell7.InstallerArguments);
        Assert.Null(ProgramInstall.ClaudeCode.InstallerArguments);
    }

    [Fact]
    public void ASignInEntryTheInstallAddedIsFoundByTheProgramsFile()
    {
        var before = new Dictionary<string, string> { ["OneDrive"] = @"""C:\Programs\OneDrive.exe"" /background" };
        var after = new Dictionary<string, string>(before) { ["KeePassXC"] = @"""C:\Program Files\KeePassXC\keepassxc.exe""" };
        Assert.Equal("KeePassXC", ProgramInstall.AddedSignInEntry(before, after, "KeePassXC.exe"));
    }

    [Fact]
    public void AnEntryThatWasThereBeforeOrIsAnotherProgramsIsNotReported()
    {
        var own = new Dictionary<string, string> { ["KeePassXC"] = @"""C:\Program Files\KeePassXC\KeePassXC.exe""" };
        Assert.Null(ProgramInstall.AddedSignInEntry(own, own, "KeePassXC.exe"));
        Assert.Null(ProgramInstall.AddedSignInEntry(own, new Dictionary<string, string> { ["keepassxc"] = own["KeePassXC"] }, "KeePassXC.exe"));
        var other = new Dictionary<string, string>(own) { ["Updater"] = @"C:\Programs\updater.exe" };
        Assert.Null(ProgramInstall.AddedSignInEntry(own, other, "KeePassXC.exe"));
        Assert.Null(ProgramInstall.AddedSignInEntry(own, new Dictionary<string, string>(), "KeePassXC.exe"));
    }

    [Fact]
    public void TheSignInNoticeSaysWhatHappenedAndWhereItIsTurnedOff()
    {
        Assert.Equal("KeePassXC's installer set it to start with Windows. axPass does not need that. To turn it off: KeePassXC, Tools, Settings, General, Automatically launch KeePassXC at system startup.",
            ProgramInstall.KeePassXC.SignInNotice("axPass"));
        Assert.Equal("KeePassXC.exe", ProgramInstall.KeePassXC.ProgramFile);
        Assert.Null(ProgramInstall.Git.ProgramFile);
    }

    [Fact]
    public void EachProgramComesFromItsMaker()
    {
        Assert.Equal("Git.Git", ProgramInstall.Git.WingetId);
        Assert.Equal("KeePassXCTeam.KeePassXC", ProgramInstall.KeePassXC.WingetId);
        Assert.Equal("Microsoft.PowerShell", ProgramInstall.PowerShell7.WingetId);
        Assert.Null(ProgramInstall.ClaudeCode.WingetId);
        Assert.StartsWith("https://", ProgramInstall.ClaudeCode.Page);
        Assert.False(ProgramInstall.ClaudeCode.AsksForAdministrator);
    }

    [Fact]
    public void ClaudeCodesOwnInstallerRunsInPowerShellWithoutAWindow()
    {
        var info = ProgramInstall.ClaudeCode.StartInfo()!;
        Assert.Equal(SystemPrograms.PowerShell, info.FileName);
        Assert.Equal(["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "irm https://claude.ai/install.ps1 | iex"], info.ArgumentList);
        Assert.True(info.CreateNoWindow);
        Assert.True(ProgramInstall.ClaudeCode.CanInstall);
    }

    [Fact]
    public void ThePathIsTheMachinesThenTheUsersThenTheProcesssOwn() =>
        Assert.Equal(@"C:\Windows;C:\Git\cmd;D:\Tools\bin;C:\Own",
            ProgramInstall.CombinePath(@"C:\Windows;C:\Git\cmd", @"D:\Tools\bin;c:\windows", @"C:\Windows;C:\Own;"));
}
