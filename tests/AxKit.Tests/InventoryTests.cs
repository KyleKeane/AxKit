using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// Every app of the bundle (<see cref="Dispatch.Apps"/>) has its places in the repository (docs/axKit/SPEC.md 5.3 M1,
/// docs/axKit/adding-an-app.md): its folders, its three documents, its notes, its icon, its place in the issue forms,
/// the pages, the map and the spec, and its call in the program. Read off the files, so that a new app that is
/// missing somewhere fails here rather than at its release (axStart was missing from nine of these on 2026-10-23).
/// The installer, the icon script and run.ps1 read <see cref="Dispatch.Apps"/> itself since 2026-10-24
/// (docs/axKit/app-kit.md AK-4.1), so they have no place to miss; tools/new-app.ps1 writes the places it can.
/// </summary>
public sealed class InventoryTests
{
    /// <summary>axDown's pure code and tests live in AxKit.Core and AxKit.Tests since axJSON, the second editor (docs/axDown/SPEC.md §6).</summary>
    private static readonly IReadOnlySet<string> WithoutOwnCore = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "axDown" };

    [Fact]
    public void Every_app_has_its_places()
    {
        var root = LayoutTests.RepositoryRoot();
        var missing = new List<string>();
        void Expect(string app, bool present, string place)
        {
            if (!present)
            {
                missing.Add($"{app}: {place}");
            }
        }

        var texts = new Dictionary<string, string>();
        string Text(string relative) => texts.TryGetValue(relative, out var text) ? text : texts[relative] = File.ReadAllText(Path.Combine(root, relative));

        foreach (var app in Dispatch.Apps)
        {
            // The code's places by the project name, the texts by the name as shown (Dispatch.App).
            var name = app.Project;
            var shown = app.Name;
            Expect(name, Directory.Exists(Path.Combine(root, "src", name)), "src folder");
            Expect(name, File.Exists(Path.Combine(root, "src", name, "CLAUDE.md")), "src notes (CLAUDE.md)");
            Expect(name, File.Exists(Path.Combine(root, "src", name, "HelpText.cs")), "HelpText.cs");
            Expect(name, File.Exists(Path.Combine(root, "src", name, $"{name}Keys.cs")), "the key table");
            if (!WithoutOwnCore.Contains(name))
            {
                Expect(name, Directory.Exists(Path.Combine(root, "src", $"{name}.Core")), "Core project");
                Expect(name, File.Exists(Path.Combine(root, "src", $"{name}.Core", "CLAUDE.md")), "Core notes (CLAUDE.md)");
                Expect(name, Directory.Exists(Path.Combine(root, "tests", $"{name}.Tests")), "test project");
            }

            // The documents' folder and the run script carry the app's spoken name (AX-5.1, 5.6.0): docs/axDown, run-axDown.ps1.
            foreach (var document in new[] { "SPEC.md", "user-guide.md", "nvda-test-plan.md" })
            {
                Expect(name, File.Exists(Path.Combine(root, "docs", shown, document)), $"docs/{shown}/{document}");
            }

            Expect(name, File.Exists(Path.Combine(root, "src", "AxKit", $"{name}.ico")), "icon in src/AxKit");
            Expect(name, File.Exists(Path.Combine(root, $"run-{shown}.ps1")), $"run-{shown}.ps1");
            Expect(name, Text("src/AxKit/Program.cs").Contains($"{name}App.Run", StringComparison.Ordinal), "Program.cs's call");
            Expect(name, Text("src/AxKit/AxKit.csproj").Contains(shown, StringComparison.Ordinal), "AxKit.csproj's description");
            Expect(name, Text("docs/axKit/apps.md").Contains($"### {shown}", StringComparison.Ordinal), "docs/axKit/apps.md, the generated list of the apps (run tools/apps-page.ps1)");
            Expect(name, Text("README.md").Contains($"**{app.Title}**", StringComparison.Ordinal), "README.md, by its title (AX-11.2)");
            Expect(name, Text("docs/axKit/README.md").Contains($"**{app.Title}**", StringComparison.Ordinal), "docs/axKit/README.md, by its title (AX-11.2)");
            Expect(name, Text("docs/axKit/SPEC.md").Contains($"**{shown}**", StringComparison.Ordinal), "docs/axKit/SPEC.md section 3");
            Expect(name, Text("docs/axKit/SPEC.md").Contains($"`{app.Verb}`", StringComparison.Ordinal), "docs/axKit/SPEC.md AX-1.1's verb");
            Expect(name, Text(".github/ISSUE_TEMPLATE/bug.yml").Contains($"- {shown}", StringComparison.Ordinal), "the bug form's choices");
            Expect(name, Text(".github/ISSUE_TEMPLATE/idea.yml").Contains($"- {shown}", StringComparison.Ordinal), "the idea form's choices");
            Expect(name, Text("CHANGELOG.md").Contains($"{shown}:", StringComparison.Ordinal), "a CHANGELOG.md bullet");
        }

        Assert.True(missing.Count == 0, "Missing: " + string.Join("; ", missing));
    }
}
