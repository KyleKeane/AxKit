using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The notes beside the code name what is there (docs/axKit/repairs.json R-49, principle P8): every class or member a
/// <c>src/*/CLAUDE.md</c> names in backticks exists in the sources. A rename that leaves the notes behind fails here,
/// with the note and the name, instead of sending the next reader to something that is gone. Read off the sources, as
/// <see cref="ListTests"/> does: lint, and a good one, since the notes are what a reader with little context is given.
/// </summary>
public sealed partial class NotesTests
{
    /// <summary>
    /// Names the notes mention that are not axKit's: WinForms, .NET and project properties named in a gotcha, and
    /// names of the past that a note gives as such. A name is added here only when the note is right to hold it.
    /// </summary>
    private static readonly HashSet<string> NotOurs = new(StringComparer.Ordinal)
    {
        // WinForms and .NET, named in gotchas.
        "CanSelect", "CreateControl", "ListView", "MenuAutoExpand", "SelectionMode", "MultiSimple", "ToolStripDropDown",
        "SelectPreviousToolStrip", "ToolStripDropDownItem", "OnDropDownClosed", "ToolStripManager", "ProcessMenuKey",
        "SynchronizationContext", "Current",
        // Properties of a project file.
        "ApplicationHighDpiMode", "InvariantGlobalization",
        // The runner's project, tools/AxAutomate/AxAutomate.Runner.csproj.
        "Runner",
        // The Window menu's class until 5.7.0, named as its former name.
        "AxKitMenu",
    };

    [Fact]
    public void Every_name_the_notes_give_in_backticks_exists_in_the_sources()
    {
        var root = LayoutTests.RepositoryRoot();
        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (var folder in new[] { "src", "tools", "tests" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.EndsWith("NotesTests.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Match word in Identifier().Matches(File.ReadAllText(file)))
                {
                    words.Add(word.Value);
                }
            }
        }

        var gone = new List<string>();
        foreach (var notes in Directory.GetFiles(Path.Combine(root, "src"), "CLAUDE.md", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, notes);
            foreach (Match named in NamedInBackticks().Matches(File.ReadAllText(notes)))
            {
                foreach (var part in named.Groups[1].Value.Split('.'))
                {
                    if (!words.Contains(part) && !NotOurs.Contains(part))
                    {
                        gone.Add($"{relative}: {named.Groups[1].Value}");
                    }
                }
            }
        }

        Assert.True(gone.Count == 0, "The notes name what the sources no longer have (correct the note, or add a name that is not axKit's to NotesTests.NotOurs): " + string.Join("; ", gone.Distinct()));
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex Identifier();

    /// <summary>A name in backticks written as code names things: a capital, lower case, and a second capital further on (<c>TextDocument</c>, <c>Dispatch.Apps</c>, <c>RunCommand()</c>); a word alone (<c>Enter</c>) is not taken for one.</summary>
    [GeneratedRegex(@"`([A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]*)+(?:\.[A-Z][A-Za-z0-9]+)*)(?:\(\))?`")]
    private static partial Regex NamedInBackticks();
}
