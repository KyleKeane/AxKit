using System.Text.RegularExpressions;
using AxKit.Core.Updates;

namespace AxKit.Tests;

/// <summary>
/// The public half of the release key stands in two places that check a release's zip (docs/axKit/SPEC.md AX-3.4;
/// security audit S-27 and S-28): the updater's constant, <see cref="ReleaseSigning.PublicKey"/>, and install.cmd, the
/// installer of the first copy, which carries it in its one PowerShell line. tools/sign-release.ps1 compares the key
/// on the maintainer's computer with the constant before it signs; this holds install.cmd to the same constant, so
/// that a key changed in one place alone fails here and not at somebody's install. Read off the file, as
/// <see cref="ReleaseWorkflowTests"/> reads the workflows.
/// </summary>
public sealed class ReleaseKeyTests
{
    [Fact]
    public void The_installer_holds_the_public_half_the_updater_holds()
    {
        var text = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "install.cmd"));

        // install.cmd builds the key from the point's X and Y in base64, written out in the line: FromBase64String('...').
        // The signature it checks is read from a file, so the line holds one such text and no other.
        var held = Assert.Single(Regex.Matches(text, @"FromBase64String\('([A-Za-z0-9+/=]+)'\)"));
        Assert.Equal(ReleaseSigning.PublicKey, held.Groups[1].Value);
    }
}
