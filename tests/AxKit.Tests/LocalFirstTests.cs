namespace AxKit.Tests;

/// <summary>
/// Local first (root CLAUDE.md rule 8; docs/axKit/SPEC.md G6, B-15, AX-11.11): axKit's own code makes network requests
/// in two places, the updater and the report sender, and nowhere else; an access layer reaches the network only
/// through the program it runs. tools/outside-calls.ps1 -Check already refuses a network call without its
/// "Outside call, network" comment; this test holds where such a comment may stand, so that a standalone app, or any
/// other code, cannot start talking to the network unseen (the maintainer, 2026-11-22: standalone apps "should be as
/// barebones functional as possible and avoid any network requests"; 2026-11-16: every policy a mechanical check).
/// </summary>
public sealed class LocalFirstTests
{
    /// <summary>The two places, and the one HTTP client they share, from the repository's root.</summary>
    private static readonly string[] NetworkPlaces = ["src/AxKit.Core/Updates/", "src/AxKit.Core/Reports/", "src/AxKit.Core/AxKitHttp.cs"];

    [Fact]
    public void Only_the_updater_and_the_report_sender_reach_the_network()
    {
        var root = LayoutTests.RepositoryRoot();
        var outside = new List<string>();
        var inside = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (!line.Contains("Outside call, network", StringComparison.Ordinal))
                {
                    continue;
                }

                if (NetworkPlaces.Any(place => relative.StartsWith(place, StringComparison.Ordinal)))
                {
                    inside++;
                }
                else
                {
                    outside.Add($"{relative}:{number}");
                }
            }
        }

        Assert.True(outside.Count == 0, "A network request outside the updater and the report sender (rule 8): " + string.Join(", ", outside));
        // The comments are there to be found: a reading that found none would hold nothing.
        Assert.True(inside > 0, "No \"Outside call, network\" comment found in the updater and the report sender.");
    }
}
