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
        // This test, whose own file is not among the sources read (its list of names would vouch for every one).
        "NotesTests",
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
        // The notes are each folder's CLAUDE.md and, where one was split by kind of window, the files of its notes folder.
        var files = Directory.GetFiles(Path.Combine(root, "src"), "CLAUDE.md", SearchOption.AllDirectories)
            .Concat(Directory.GetDirectories(Path.Combine(root, "src"), "notes", SearchOption.AllDirectories).SelectMany(folder => Directory.GetFiles(folder, "*.md")));
        foreach (var notes in files)
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

    /// <summary>The most a file of notes may hold, in bytes: what is loaded with every file of its folder stays readable in one go.</summary>
    private const int NotesAtMost = 32 * 1024;

    /// <summary>The most the root instructions may hold: they are loaded into every session.</summary>
    private const int RootAtMost = 20 * 1024;

    /// <summary>
    /// The notes stay small enough to hold (docs/axKit/repairs.json R-45): the root instructions, loaded into every
    /// session, and each folder's notes, loaded with every file read there. One that outgrows its limit is split by
    /// kind into a notes folder under an index, as <c>src/AxKit.Forms</c> is, or loses what a document already says.
    /// </summary>
    [Fact]
    public void The_notes_stay_small_enough_to_hold()
    {
        var root = LayoutTests.RepositoryRoot();
        var large = new List<string>();
        var rootNotes = new FileInfo(Path.Combine(root, "CLAUDE.md"));
        if (rootNotes.Length > RootAtMost)
        {
            large.Add($"CLAUDE.md is {rootNotes.Length} bytes, at most {RootAtMost}: move what is not a rule or a gotcha to docs/axKit (commands.md, documents.md)");
        }

        var files = Directory.GetFiles(Path.Combine(root, "src"), "CLAUDE.md", SearchOption.AllDirectories)
            .Concat(Directory.GetDirectories(Path.Combine(root, "src"), "notes", SearchOption.AllDirectories).SelectMany(folder => Directory.GetFiles(folder, "*.md")));
        foreach (var file in files)
        {
            var length = new FileInfo(file).Length;
            if (length > NotesAtMost)
            {
                large.Add($"{Path.GetRelativePath(root, file)} is {length} bytes, at most {NotesAtMost}: split it by kind into a notes folder under a short index, as src/AxKit.Forms is");
            }
        }

        Assert.True(large.Count == 0, string.Join("; ", large));
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex Identifier();

    /// <summary>A name in backticks written as code names things: a capital, lower case, and a second capital further on (<c>TextDocument</c>, <c>Dispatch.Apps</c>, <c>RunCommand()</c>); a word alone (<c>Enter</c>) is not taken for one.</summary>
    [GeneratedRegex(@"`([A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]*)+(?:\.[A-Z][A-Za-z0-9]+)*)(?:\(\))?`")]
    private static partial Regex NamedInBackticks();
}
