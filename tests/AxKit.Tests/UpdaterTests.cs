using AxKit.Core.Updates;

namespace AxKit.Tests;

/// <summary>A failed update's details to send (docs/axKit/SPEC.md AX-3.6): what went wrong, nothing that names the user.</summary>
public class UpdaterTests
{
    [Fact]
    public void The_details_name_the_versions_and_what_failed_but_not_the_account()
    {
        // Split so that the repository's privacy test does not take the examples for real ones (PrivacyTests).
        var details = Updater.FailureDetails("3.7.0", "3.7.1", "axFile",
            "axKit was not installed: the list of apps could not be read from C:\\" + "Users\\someone\\AppData\\Local\\AxKit\\updates\\3.7.1\\AxKit.exe (AxKit.exe apps)\n"
            + "Settings of someone" + "@example.com in C:/" + "Users/someone/.local/bin",
            ["someone"]);

        Assert.StartsWith("axKit 3.7.0 updating to 3.7.1 (axFile)\nWindows ", details);
        Assert.Contains("C:\\Users\\<user>\\AppData\\Local\\AxKit\\updates\\3.7.1\\AxKit.exe", details);
        Assert.Contains("C:/Users/<user>/.local/bin", details);
        Assert.Contains("<email>", details);
        Assert.DoesNotContain("someone", details);
    }

    /// <summary>docs/axKit/repairs.json R-23: the signature covers the zip, so the program inside says which version it is.</summary>
    [Theory]
    [InlineData("5.8.0.0", "5.8.0", "5.7.0", null)]
    [InlineData("5.8.0", "5.8.0", "5.7.3", null)]
    [InlineData("5.1.0.0", "5.8.0", "5.7.0", "The release is named 5.8.0, but the program in it is version 5.1.0.")]
    [InlineData("5.7.0.0", "5.7.0", "5.7.0", "is not newer than the 5.7.0 that is running")]
    [InlineData("5.6.0.0", "5.6.0", "5.7.0", "is not newer than the 5.7.0 that is running")]
    [InlineData("", "5.8.0", "5.7.0", "does not say which version it is")]
    public void A_downloaded_program_is_installed_only_when_it_is_the_version_named_and_newer_than_the_one_running(string inZip, string named, string running, string? refusal)
    {
        var said = Updater.VersionRefusal(UpdateCheck.ParseVersion(inZip), UpdateCheck.ParseVersion(named)!, running);
        if (refusal is null)
        {
            Assert.Null(said);
        }
        else
        {
            Assert.Contains(refusal, said);
            Assert.Contains("Nothing was installed", said);
        }
    }
}
