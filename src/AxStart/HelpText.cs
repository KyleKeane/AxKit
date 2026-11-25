using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxStart;

/// <summary>The texts axStart shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AS-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axStart's own. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context and the hotkey, which no command carries.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axStart",
        [
            $"{BundleKeys.Text(BundleKeys.Save)} has nothing to save here, and axStart says so.",
        ],
        altF4: "hide axStart (Session, Quit axStart ends it)") + """
        ## Bringing axStart up
          The hotkey, Ctrl+Shift+Space unless you chose another in Options, Hotkey, shows the search field from any program.
          The same hotkey while axStart shows hides it again.

        ## In the search field
          The caret stays here: type and delete at any time.
          Type part of a program's name; after a short pause the first match is said, "Calculator, 1 of 2".
          What you closed lately in axKit's windows is among the matches too, first when nothing is typed: Enter opens it again in its app.
          Each match is said with its place. At the first or the last match, a key that cannot move plays one short low note and says the match again.
          Start as administrator has Windows ask first.
          Information says the match, its kind and where its program file is.
          Open file location shows the program file's folder in axFile, with the file selected.
          Escape hides axStart at once and returns where you were; the field is empty the next time.
        """ + BundleHelp.NoticesSection("axStart", "the field");

    /// <summary>docs/axStart/user-guide.md, embedded by AxStart.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axStart/user-guide.md");
}
