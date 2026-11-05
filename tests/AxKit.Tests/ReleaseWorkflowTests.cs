using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The release workflow has AxKit.exe signed for Windows before it zips it, in a job of its own (docs/axkit/SPEC.md
/// AX-3.4; docs/axkit/code-signing.md). Read off .github/workflows/release.yml, as <see cref="LayoutTests"/> reads the
/// sources, so that a later edit cannot zip an unsigned program or put the signing token beside the tests and their
/// packages (security audit S-30).
/// </summary>
public sealed class ReleaseWorkflowTests
{
    private static string WorkflowsFolder => Path.Combine(LayoutTests.RepositoryRoot(), ".github", "workflows");

    [Fact]
    public void The_release_signs_the_program_in_its_own_job_before_the_zip()
    {
        var text = File.ReadAllText(Path.Combine(WorkflowsFolder, "release.yml"));
        int Index(string piece)
        {
            var index = text.IndexOf(piece, StringComparison.Ordinal);
            Assert.True(index >= 0, $"release.yml no longer has \"{piece}\"");
            return index;
        }

        var build = Index("\n  build:");
        var sign = Index("\n  sign:");
        var draft = Index("\n  draft:");
        // The run lines, with their ".\": a comment may name the scripts earlier.
        var publish = Index(".\\publish.ps1 -NoInstall -NoZip");
        var unsigned = Index("name: unsigned");
        var signing = Index("uses: signpath/github-action-submit-signing-request@");
        var check = Index("Get-AuthenticodeSignature publish\\win-x64\\AxKit.exe");
        var zip = Index(".\\publish.ps1 -ZipOnly");

        Assert.True(build < publish && publish < unsigned && unsigned < sign, "the build job publishes the folder without zipping it and uploads the unsigned AxKit.exe");
        Assert.True(sign < signing && signing < check && check < zip && zip < draft, "the sign job signs, checks the signature, then zips");
        Assert.Contains("-ne 'Valid'", text[check..zip], StringComparison.Ordinal);

        // The signing token is the sign job's alone: the build job, which runs the tests and their packages, never
        // sees it, and only the "release" environment's runs can present it.
        Assert.DoesNotContain("SIGNPATH_API_TOKEN", text[build..sign], StringComparison.Ordinal);
        var signJob = text[sign..draft];
        Assert.Contains("environment: release", signJob, StringComparison.Ordinal);
        Assert.Contains("secrets.SIGNPATH_API_TOKEN", signJob, StringComparison.Ordinal);
        Assert.Contains("needs: sign", text[draft..], StringComparison.Ordinal);
    }

    [Fact]
    public void Every_action_is_pinned_to_a_commit()
    {
        foreach (var file in Directory.GetFiles(WorkflowsFolder, "*.yml"))
        {
            foreach (Match use in Regex.Matches(File.ReadAllText(file), @"uses:\s*(\S+)"))
            {
                var action = use.Groups[1].Value;
                Assert.True(Regex.IsMatch(action, @"^[^@\s]+@[0-9a-f]{40}$"), $"{Path.GetFileName(file)}: {action} is not pinned to a commit");
            }
        }
    }
}
