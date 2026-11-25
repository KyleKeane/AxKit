using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The changelog's rule (root CLAUDE.md rule 4, docs/axKit/longevity.md 3.11): every bullet of the section being written
/// names its app or apps, "- axFile: " or "- axDown, axJSON: ", or is "- For developers: ". release.ps1 holds a release
/// to it (Get-ChangelogProblem) with the same pattern; this test holds every commit to it, so that a bullet the release
/// would refuse is found the day it is written, not on the release's (docs/axKit/repairs.json R-98: five bullets of
/// 2026-11-24 said "axDown and axJSON:", which the release refuses).
/// </summary>
public class ChangelogTests
{
    [Fact]
    public void Every_bullet_of_the_unreleased_section_names_its_apps_as_the_release_reads_them()
    {
        var root = SourceTexts.RepositoryRoot();
        var text = File.ReadAllText(Path.Combine(root, "CHANGELOG.md")).Replace("\r\n", "\n");
        var start = text.IndexOf("\n## Unreleased", StringComparison.Ordinal);
        if (start < 0)
        {
            // Right after a release there is no section being written.
            return;
        }

        var end = text.IndexOf("\n## ", start + 4, StringComparison.Ordinal);
        var section = end < 0 ? text[start..] : text[start..end];

        // The apps as release.ps1 names them: the folders of docs that hold a SPEC.md, axkit among them.
        var apps = string.Join("|", Directory.GetDirectories(Path.Combine(root, "docs"))
            .Where(folder => File.Exists(Path.Combine(folder, "SPEC.md")))
            .Select(folder => Regex.Escape(Path.GetFileName(folder))));
        var prefix = new Regex($"^- (({apps})(, ({apps}))*|For developers): ", RegexOptions.IgnoreCase);

        var unnamed = section.Split('\n').Where(line => line.StartsWith("- ", StringComparison.Ordinal) && !prefix.IsMatch(line)).ToList();
        Assert.True(unnamed.Count == 0, "These bullets of '## Unreleased' do not start with their app or apps ('- axFile: ', '- axDown, axJSON: ') or with '- For developers: ', and the release would refuse them:\n" + string.Join("\n", unnamed));
    }
}
