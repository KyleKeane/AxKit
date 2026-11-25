using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// Every list the user moves through is the bundle's <c>AxKit.Forms/AxKitList</c> (docs/axKit/SPEC.md AX-8.6), which has
/// type-to-find with Shift backwards and the edge earcon built in, so that a new app gets them by making the list and
/// nothing else. Read off the sources: a plain <c>ListBox</c> made in an app fails here, unless it is one of the lists
/// below that never take a typed letter, each with its reason.
/// </summary>
public sealed class ListTests
{
    /// <summary>The plain list boxes allowed, by file, with why.</summary>
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // The lists under a field that keeps the caret (the hotkey windows' matches, the command palette's) were
        // here until 2026-11-22: they are the bundle's list too now, in its mode for that (AxKitList.Field,
        // framework FW-4.12).
        // A question's answers: their letters are the screen's own keys (axClaude D33), not a search.
        [Path.Combine("src", "AxKit.Forms", "ControlArea.cs")] = "letters are keys",
    };

    [Fact]
    public void Every_list_a_user_types_in_is_the_bundles_list()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || Allowed.ContainsKey(relative))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                // "ListBox _list = new()", "ListBox list = new()", "ListBox Matches { get; } = new()", "new ListBox()",
                // "new CheckedListBox { … }": a plain list box made.
                if (Regex.IsMatch(line, @"\b(?:Checked)?ListBox\s+\w+\s*(?:\{\s*get;\s*\}\s*)?=\s*new\b|\bnew\s+(?:Checked)?ListBox\s*[({]"))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A plain ListBox where the bundle's AxKitList belongs (type-to-find and the edge earcon, AX-8.6): " + string.Join("; ", offenders));
    }
}
