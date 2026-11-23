using AxKit.Core;
using AxKit.Forms;

namespace AxDown;

/// <summary>
/// axDown's entry point inside axKit (docs/axKit/SPEC.md AX-1.7; SPEC.md AD-1.4): <c>AxKit.exe down [&lt;file&gt;]</c>
/// calls <see cref="Run"/>, which hands the editor shell (<c>AxKit.Forms.EditorHost</c>, <c>TextEditorForm</c>) what
/// axDown is: its name and verb, its settings file, its file types, its texts, its Markdown keys and its resources.
/// </summary>
public static class AxDownApp
{
    public const string Usage = """
        Usage: axKit down [<file>]
               axdown [<file>]  the same, as the installed console command

          <file>            the file to open; a path that does not exist yet is a new document with that name
          --help, --version this text, or the version
        """;

    /// <summary>axDown as the editor shell sees it (bundle AX-6.2, AJ-D4).</summary>
    public static readonly EditorApp App = new(
        Name: "axDown",
        Verb: Dispatch.AxDown,
        SettingsName: "axDown",
        FileFilter: "Markdown and text files (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|All files (*.*)|*.*",
        DefaultExtension: "md",
        Shortcuts: HelpText.Shortcuts,
        UserGuide: HelpText.UserGuide,
        Jumps: AxDownKeys.Jumps,
        ResourcesLabel: "Markdown &resources",
        Resources:
        [
            ("Markdown &basics", "https://commonmark.org/help/"),
            ("&GitHub Flavored Markdown", "https://github.github.com/gfm/"),
        ],
        Commands: [],
        Format: FormatCommands(),
        Templates:
        [
            // SPEC.md AD-13.3: the selected words become the link's text, and the caret waits where the address goes.
            new EditorTemplate("&Link", "[{selection}]({caret})"),
            // Two columns and two rows, the caret in the first cell.
            new EditorTemplate("&Table", "| {caret} |  |\n| --- | --- |\n|  |  |"),
            // A fenced block with the selection inside it and the caret after it.
            new EditorTemplate("&Code block", "```\n{selection}{caret}\n```"),
        ]);

    /// <summary>
    /// Actions, Format (SPEC.md AD-12): each item one function of <see cref="MarkdownEdit"/> on the caret's line
    /// or the selection, a toggle; three have a key (<see cref="AxDownKeys"/>).
    /// </summary>
    private static IReadOnlyList<EditorChange> FormatCommands()
    {
        var items = new List<EditorChange>();
        for (var level = 1; level <= MarkdownEdit.DeepestHeading; level++)
        {
            var heading = level;
            items.Add(new EditorChange($"Heading &{level}", Keys.None, (text, start, _) => Edit(MarkdownEdit.Heading(text, start, heading))));
        }

        items.Add(new EditorChange("Heading &deeper", AxDownKeys.HeadingDeeper, (text, start, _) => Edit(MarkdownEdit.HeadingDeeper(text, start))));
        items.Add(new EditorChange("Heading &shallower", AxDownKeys.HeadingShallower, (text, start, _) => Edit(MarkdownEdit.HeadingShallower(text, start))));
        items.Add(new EditorChange("&Bold", AxDownKeys.Bold, (text, start, length) => Edit(MarkdownEdit.Bold(text, start, length))));
        items.Add(new EditorChange("&Italic", Keys.None, (text, start, length) => Edit(MarkdownEdit.Italic(text, start, length))));
        items.Add(new EditorChange("&Code", Keys.None, (text, start, length) => Edit(MarkdownEdit.Code(text, start, length))));
        items.Add(new EditorChange("&List item", Keys.None, (text, start, length) => Edit(MarkdownEdit.ListItem(text, start, length))));
        items.Add(new EditorChange("Block &quote", Keys.None, (text, start, length) => Edit(MarkdownEdit.BlockQuote(text, start, length))));
        return items;
    }

    private static TextEdit Edit(MarkdownChange change) => new(change.Start, change.Length, change.Text, change.Caret, change.Said, change.Edge);

    public static void Run(string[] args) => EditorHost.Run(App, Usage, args);
}
