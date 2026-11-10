using System.Reflection;
using AxKit.Core;

namespace AxHelp;

/// <summary>The texts axHelp shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md HE-6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axHelp's own, then the bundle's Notices section.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axHelp",
        [
            "Ctrl+S: nothing to save here; axHelp says so",
        ],
        altF4: "hide axHelp (axHelp, Quit axHelp ends it)") + """
        ## Bringing axHelp up
          The hotkey, Windows+Alt+H unless you chose another in Options, Hotkey: from any program, the question field
          The same hotkey while axHelp shows hides it again
          AI help must be on: axKit menu, AI help, Let Claude help in the apps, once, in any axKit window

        ## In the question field
          The caret stays here: type your question and press Enter. Claude answers in a notice; the arrows and the heading letters read it
          In the answer: Tab to the field under it, type more and press Enter to ask about the same answer; Escape closes it
          Down, Up: your earlier questions, newest first, said with their place; type part of a question to find it. Page Down, Page Up: a page of them
          Ctrl+Home, Ctrl+End: the first or the last. At the first or the last, a key that cannot move plays one short low note and says the question again
          Enter with nothing typed: the selected earlier question's answer again, and more can be asked about it
          Escape: hide axHelp at once and return where you were; the field is empty the next time
        """ + BundleHelp.NoticesSection("axHelp", "the field");

    /// <summary>docs/axHelp/user-guide.md, embedded by AxHelp.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axHelp/user-guide.md");
}
