using System.Reflection;
using AxKit.Core;

namespace AxDescribe;

/// <summary>The texts axDescribe shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md DE-6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axDescribe's own, then the bundle's Notices section.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axDescribe",
        [
            "Ctrl+S: nothing to save here; axDescribe says so",
        ],
        altF4: "hide axDescribe (axDescribe, Quit axDescribe ends it)") + """
        ## Describing the window in front
          The hotkey, Windows+Alt+S unless you chose another in Options, Hotkey: from any program, a picture of the window in front goes to Claude, and its description comes as a notice, which the arrows and the heading letters read
          In the description: Tab to the field under it, type a question about the picture and press Enter; Escape closes it
          The same hotkey while axDescribe shows hides it again
          AI help must be on: axKit menu, AI help, Let Claude help in the apps, once, in any axKit window

        ## In the field
          The caret stays here: type a question about the selected description's picture and press Enter
          Down, Up: your earlier descriptions, newest first, each said with its time and the window's title; type part of a title to find one. Page Down, Page Up: a page of them
          Ctrl+Home, Ctrl+End: the first or the last. At the first or the last, a key that cannot move plays one short low note and says the description again
          Enter with nothing typed: the selected description again, and more can be asked about it
          Ctrl+O: the last picture in axPreview, which reads the text in it with Windows' own reader
          Escape: hide axDescribe at once and return where you were
        """ + BundleHelp.NoticesSection("axDescribe", "the field");

    /// <summary>docs/axDescribe/user-guide.md, embedded by AxDescribe.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axDescribe/user-guide.md");
}
