using AxJSON.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxJSON;

/// <summary>
/// axJSON's entry point inside axKit (docs/axKit/SPEC.md AX-1.7; SPEC.md AJ-1): <c>AxKit.exe json [&lt;file&gt;]</c>
/// calls <see cref="Run"/>, which hands the editor shell (<c>AxKit.Forms.EditorHost</c>, <c>TextEditorForm</c>) what
/// axJSON is: its name and verb, its settings file, its file types, its texts, its JSON keys, its resources and its
/// Format command (AJ-D4).
/// </summary>
public static class AxJSONApp
{
    public const string Usage = """
        Usage: axKit json [<file>]
               axjson [<file>]  the same, as the installed console command

          <file>            the file to open; a path that does not exist yet is a new document with that name
          --help, --version this text, or the version
        """;

    /// <summary>axJSON as the editor shell sees it (bundle AX-6.2, AJ-D4).</summary>
    public static readonly EditorApp App = new(
        Name: "axJSON",
        Verb: Dispatch.AxJSON,
        SettingsName: "axJSON",
        FileFilter: "JSON files (*.json;*.jsonc)|*.json;*.jsonc|All files (*.*)|*.*",
        DefaultExtension: "json",
        Shortcuts: HelpText.Shortcuts,
        UserGuide: HelpText.UserGuide,
        Jumps: AxJSONKeys.Jumps,
        ResourcesLabel: "JSON &resources",
        Resources:
        [
            ("&JSON syntax", "https://www.json.org/json-en.html"),
            ("Claude Code &settings", "https://code.claude.com/docs/en/settings"),
        ],
        Commands:
        [
            new EditorCommand("&Format", AxJSONKeys.Format, text =>
            {
                var (formatted, message) = JsonFormat.Apply(text);
                return new Rewrite(formatted, message);
            }),
        ],
        Offers:
        [
            new EditorOffer("&Choose a value...", AxJSONKeys.ChooseValue, (text, caret, path) => Shown(JsonOffers.Values(text, caret, name => ReadLinked(path, name)))),
            new EditorOffer("Add a &member...", AxJSONKeys.AddMember, (text, caret, path) => Shown(JsonOffers.Members(text, caret, name => ReadLinked(path, name)))),
        ]);

    public static void Run(string[] args) => EditorHost.Run(App, Usage, args);

    /// <summary>The longest file a link is followed to (AJ-5.4): a list's configuration is a few thousand characters.</summary>
    private const long MaxLinkedFile = 1024 * 1024;

    /// <summary>A pure offer as the editor shell shows it.</summary>
    private static Offered Shown((JsonOffer? Offer, string? Said) result) => result.Offer is { } offer
        ? new Offered(null, offer.Name, offer.Lines, offer.Selected, chosen =>
        {
            var edit = offer.Choose(chosen);
            return new TextEdit(edit.Start, edit.Length, edit.Text, edit.Caret, edit.Said);
        })
        : new Offered(result.Said);

    /// <summary>
    /// The text of the file a document links to (AJ-5.4): a file of that name beside the document, read only on a
    /// drive of this computer (so that the window never waits on a network), only when it is small, and never
    /// anywhere else: the name is a plain file name by the time it comes here. Null when there is no such file.
    /// </summary>
    private static string? ReadLinked(string? path, string name)
    {
        if (path is null || System.IO.Path.GetDirectoryName(path) is not { Length: > 0 } folder || !LocalPaths.IsLocal(folder))
        {
            return null;
        }

        try
        {
            var linked = new FileInfo(System.IO.Path.Combine(folder, name));
            return linked.Exists && linked.Length <= MaxLinkedFile ? File.ReadAllText(linked.FullName) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            return null;
        }
    }
}
