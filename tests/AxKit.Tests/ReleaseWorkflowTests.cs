using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The release workflow has AxKit.exe signed for Windows before it zips it, in a job of its own (docs/axkit/SPEC.md
/// AX-3.4; docs/axkit/code-signing.md). Read off .github/workflows/release.yml, as <see cref="LayoutTests"/> reads the
/// sources, so that a later edit cannot zip an unsigned program unnoticed or put the signing identity's ids beside the
/// tests and their packages (security audit S-30).
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
        var login = Index("uses: azure/login@");
        var signing = Index("uses: azure/artifact-signing-action@");
        var check = Index("Get-AuthenticodeSignature publish\\win-x64\\AxKit.exe");
        var zip = Index(".\\publish.ps1 -ZipOnly");
        var package = Index(".\\publish.ps1 -Msix");

        // The sign job, the "release" environment, publishes: the reporter token of the reports goes into that build
        // (docs/axkit/reports.md RP-6.2), and the build job, which runs the tests and their packages, never holds it.
        Assert.True(build < sign && sign < publish && publish < login, "the build job tests; the sign job publishes the folder before signing it");
        Assert.True(sign < login && login < signing && signing < check && check < zip && zip < draft, "the sign job signs in, signs, checks the signature, then zips");

        // The Store's package (AX-13.6) is built after the zip, in the same job, and attached to the draft with it.
        Assert.True(zip < package && package < draft, "the sign job builds the Store package after the zip");
        Assert.Contains("publish/AxKit-*.msix", text[package..draft], StringComparison.Ordinal);   // the release artifact carries it
        Assert.Contains("publish/AxKit-*.msix", text[draft..], StringComparison.Ordinal);           // attested and uploaded with the zip
        Assert.Contains("\"$msix\"", text[draft..], StringComparison.Ordinal);
        Assert.Contains("secrets.AXKIT_REPORTER_TOKEN", text[sign..publish], StringComparison.Ordinal);
        Assert.Contains("-ne 'Valid'", text[check..zip], StringComparison.Ordinal);

        // The switch: the certificate profile's name turns both signing steps on, and a release made without it says
        // in its notes that the program is unsigned.
        var signJob = text[sign..draft];
        Assert.Equal(2, Regex.Matches(signJob, @"if: vars\.AZURE_CERTIFICATE_PROFILE != ''").Count);
        Assert.Contains("carries no Windows signature", text[check..zip], StringComparison.Ordinal);

        // The identity's ids are the sign job's alone: the build job, which runs the tests and their packages, uses no
        // secret at all, and only the "release" environment's runs get the token that becomes the identity.
        Assert.DoesNotContain("secrets.", text[build..sign], StringComparison.Ordinal);
        Assert.Contains("environment: release", signJob, StringComparison.Ordinal);
        Assert.Contains("id-token: write", signJob, StringComparison.Ordinal);
        Assert.Contains("secrets.AZURE_CLIENT_ID", signJob, StringComparison.Ordinal);
        Assert.Contains("needs: sign", text[draft..], StringComparison.Ordinal);
    }

    /// <summary>The Store submission (AX-13.6): on a published release, behind the STORE_SUBMIT switch, in the release environment, with the three secrets and the published package, never a rebuild.</summary>
    [Fact]
    public void The_store_submission_runs_on_a_published_release_behind_its_switch()
    {
        var text = File.ReadAllText(Path.Combine(WorkflowsFolder, "store.yml"));
        Assert.Contains("types: [published]", text, StringComparison.Ordinal);
        Assert.Contains("if: vars.STORE_SUBMIT == 'on'", text, StringComparison.Ordinal);
        Assert.Contains("environment: release", text, StringComparison.Ordinal);
        Assert.Contains("secrets.PARTNER_CENTER_TENANT_ID", text, StringComparison.Ordinal);
        Assert.Contains("secrets.PARTNER_CENTER_CLIENT_ID", text, StringComparison.Ordinal);
        Assert.Contains("secrets.PARTNER_CENTER_CLIENT_SECRET", text, StringComparison.Ordinal);
        Assert.Contains("gh release download", text, StringComparison.Ordinal);
        Assert.Contains("./tools/store-submit.ps1 -Package", text, StringComparison.Ordinal);
        Assert.DoesNotContain("publish.ps1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("contents: write", text, StringComparison.Ordinal);
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
