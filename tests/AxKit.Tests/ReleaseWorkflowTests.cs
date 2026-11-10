using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The release workflow publishes, zips and packages in a job of its own, the "release" environment's, after the tests
/// (docs/axKit/SPEC.md AX-3.4), and says in every release's notes that the program carries no Windows signature. Read
/// off .github/workflows/release.yml, as <see cref="LayoutTests"/> reads the sources, so that a later edit cannot put
/// the environment's secret beside the tests and their packages (security audit S-30) or drop the sentence. The
/// Azure signing steps that sat here were retired on 2026-11-09 (docs/axKit/code-signing.md).
/// </summary>
public sealed class ReleaseWorkflowTests
{
    private static string WorkflowsFolder => Path.Combine(LayoutTests.RepositoryRoot(), ".github", "workflows");

    [Fact]
    public void The_release_publishes_zips_and_packages_in_its_own_job_after_the_tests()
    {
        var text = File.ReadAllText(Path.Combine(WorkflowsFolder, "release.yml"));
        int Index(string piece)
        {
            var index = text.IndexOf(piece, StringComparison.Ordinal);
            Assert.True(index >= 0, $"release.yml no longer has \"{piece}\"");
            return index;
        }

        var build = Index("\n  build:");
        var package = Index("\n  package:");
        var draft = Index("\n  draft:");
        // The run lines, with their ".\": a comment may name the scripts earlier.
        var publish = Index(".\\publish.ps1 -NoInstall -NoZip");
        var zip = Index(".\\publish.ps1 -ZipOnly");
        var msix = Index(".\\publish.ps1 -Msix");

        // The package job, the "release" environment, publishes: the reporter token of the reports goes into that build
        // (docs/axKit/reports.md RP-6.2), and the build job, which runs the tests and their packages, never holds it.
        Assert.True(build < package && package < publish && publish < zip && zip < draft, "the build job tests; the package job publishes the folder, then zips");

        // The Store's package (AX-13.6) is built after the zip, in the same job, and attached to the draft with it.
        Assert.True(zip < msix && msix < draft, "the package job builds the Store package after the zip");
        Assert.Contains("publish/AxKit-*.msix", text[msix..draft], StringComparison.Ordinal);   // the release artifact carries it
        Assert.Contains("publish/AxKit-*.msix", text[draft..], StringComparison.Ordinal);        // attested and uploaded with the zip
        Assert.Contains("\"$msix\"", text[draft..], StringComparison.Ordinal);
        Assert.Contains("secrets.AXKIT_REPORTER_TOKEN", text[package..publish], StringComparison.Ordinal);

        // Every release's notes say that the program is unsigned and where the signed copy is.
        Assert.Contains("carries no Windows signature", text[publish..zip], StringComparison.Ordinal);
        Assert.Contains("store-distribution.md", text[publish..zip], StringComparison.Ordinal);

        // The secret is the package job's alone: the build job, which runs the tests and their packages, uses no
        // secret at all; the Azure signing is gone whole, its actions and its ids with it.
        var packageJob = text[package..draft];
        Assert.DoesNotContain("secrets.", text[build..package], StringComparison.Ordinal);
        Assert.Contains("environment: release", packageJob, StringComparison.Ordinal);
        Assert.DoesNotContain("azure/", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AZURE_", text, StringComparison.Ordinal);
        Assert.Contains("needs: package", text[draft..], StringComparison.Ordinal);
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
