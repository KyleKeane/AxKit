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
}
