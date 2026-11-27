using AxKit.Core;

namespace AxKit.Tests;

/// <summary>What the user agreed to (docs/axKit/SPEC.md AX-11.5, AX-11.8): recorded once per app and program, withdrawn, and asked again when the file is missing or damaged.</summary>
public sealed class ConsentTests
{
    private static string TempFile() => Path.Combine(Path.GetTempPath(), "axkit-tests", Guid.NewGuid().ToString("N"), "consent.json");

    [Fact]
    public void An_agreement_is_kept_per_app_and_program_and_can_be_withdrawn()
    {
        var file = TempFile();
        Assert.False(Consent.Given(file, "axTerminal", "PowerShell"));

        Consent.Record(file, "axTerminal", "PowerShell", "4.0.0", new DateTime(2026, 10, 29));
        Assert.True(Consent.Given(file, "axTerminal", "PowerShell"));
        Assert.True(Consent.Given(file, "AXTERMINAL", "powershell"));
        Assert.False(Consent.Given(file, "axTerminal", "Command Prompt"));

        // Agreeing again replaces the first record rather than adding a second.
        Consent.Record(file, "axTerminal", "PowerShell", "4.0.1", new DateTime(2026, 11, 2));
        var only = Assert.Single(Consent.Read(file));
        Assert.Equal("4.0.1", only.Version);

        Consent.Withdraw(file, "axTerminal", "PowerShell");
        Assert.False(Consent.Given(file, "axTerminal", "PowerShell"));
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    [Fact]
    public void An_agreement_is_not_said_recorded_when_the_file_was_passed_over()
    {
        // Review, 2026-11-27: the write answered true whatever the save said, so "Consent recorded" was logged of a
        // file another program held at the load just before, which the save passes over (R-51).
        var file = TempFile();
        Assert.True(Consent.Record(file, "axGIT", "Git", "4.0.0", new DateTime(2026, 10, 31)));
        var before = File.ReadAllBytes(file);
        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.False(Consent.Record(file, "axTerminal", "PowerShell", "4.0.0", new DateTime(2026, 11, 2)));
        }

        Assert.Equal(before, File.ReadAllBytes(file));
        Assert.True(Consent.Record(file, "axTerminal", "PowerShell", "4.0.0", new DateTime(2026, 11, 2)));
        Assert.True(Consent.Given(file, "axTerminal", "PowerShell"));
        Assert.True(Consent.Given(file, "axGIT", "Git"));
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    [Fact]
    public void Every_agreement_of_one_app_is_withdrawn_in_one_write()
    {
        // Forget AI tools (docs/axKit/mcp.md): every paired tool's agreement goes, the other apps' stay, the file written once.
        var file = TempFile();
        Consent.Record(file, "AI tools", "Claude Code", "4.0.0", new DateTime(2026, 10, 29));
        Consent.Record(file, "AI tools", "Another tool", "4.0.0", new DateTime(2026, 10, 30));
        Consent.Record(file, "axGIT", "Git", "4.0.0", new DateTime(2026, 10, 31));

        Consent.WithdrawAll(file, "ai tools");
        var left = Assert.Single(Consent.Read(file));
        Assert.Equal("axGIT", left.App);
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    [Fact]
    public void A_damaged_file_means_nothing_was_agreed()
    {
        var file = TempFile();
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{ not json");
        Assert.False(Consent.Given(file, "axClaude", "Claude Code"));
        Assert.Empty(Consent.Read(file));
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    /// <summary>AX-11.5: a program installed from Install it now has its own target, so that agreeing there counts for the first connection; each says whose terms and where.</summary>
    [Fact]
    public void Every_program_install_has_its_consent_target()
    {
        foreach (var program in new[] { ProgramInstall.Git, ProgramInstall.KeePassXC, ProgramInstall.PowerShell7, ProgramInstall.ClaudeCode })
        {
            var target = ConsentTarget.For(program.Name);
            Assert.Equal(program.Name, target.Program);
            Assert.StartsWith("https://", target.TermsUrl);
            Assert.NotEmpty(target.Work);
        }

        Assert.Same(ConsentTarget.ClaudeCode, ConsentTarget.For("Claude Code"));
        // A program with no target is a mistake, never Windows' terms by default (review, 2026-11-02).
        Assert.Throws<ArgumentException>(() => ConsentTarget.For("Nothing"));
        Assert.Equal("github.com", ConsentTarget.Server("github.com").Program);
        Assert.Empty(ConsentTarget.Shell("Custom shell").TermsUrl);
    }

    [Fact]
    public void Each_agreement_reads_as_one_line()
    {
        Assert.Equal(
            "axClaude, Claude Code, 29 October 2026, axKit 4.0.0",
            Consent.Line(new ConsentRecord("axClaude", "Claude Code", "4.0.0", new DateTime(2026, 10, 29))));
        Assert.Equal(
            "Update to axKit 4.0.0, 29 October 2026",
            Consent.Line(new ConsentRecord(Consent.UpdateApp, "axKit 4.0.0", "3.7.6", new DateTime(2026, 10, 29))));
    }

    [Fact]
    public void Members_this_version_does_not_know_are_kept_in_the_consent_file_and_in_the_recently_closed_list()
    {
        // R-67 (AX-5.8): what a newer version wrote beside the list stays through this version's writes.
        using var temp = new AxKit.TestShared.TempFolder("axkit-consent");
        var consent = Path.Combine(temp.Path, "consent.json");
        Consent.Record(consent, "axGIT", "Git", "1", new DateTime(2026, 11, 16));
        AxKit.TestShared.SettingsText.Change(consent, whole => whole["aNewerMember"] = "kept");
        Consent.Record(consent, "axPass", "KeePassXC", "1", new DateTime(2026, 11, 16));
        Assert.Contains("\"aNewerMember\": \"kept\"", File.ReadAllText(consent));
        // The seal covers the agreements alone: both still count.
        Assert.True(Consent.Given(consent, "axGIT", "Git"));
        Assert.True(Consent.Given(consent, "axPass", "KeePassXC"));
        Assert.Null(Consent.Problem(consent));

        var closed = Path.Combine(temp.Path, "recently-closed.json");
        RecentlyClosed.Write(closed, [new("axDown", @"C:\work\a.md")]);
        AxKit.TestShared.SettingsText.Change(closed, whole => whole["aNewerMember"] = 7);
        RecentlyClosed.Write(closed, RecentlyClosed.Add(RecentlyClosed.Read(closed), new ClosedItem("axClaude", @"C:\work")));
        Assert.Contains("\"aNewerMember\": 7", File.ReadAllText(closed));
        Assert.Equal(["axClaude", "axDown"], RecentlyClosed.Read(closed).Select(item => item.App));
    }
}
