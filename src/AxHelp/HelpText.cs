using System.Reflection;
using AxKit.Core;

namespace AxHelp;

/// <summary>The texts axHelp shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md HE-7): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axHelp's own, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axHelp",
        [
            "Ctrl+F: find a line in the list. F3 and Shift+F3: next or previous match",
        ]) + """
        ## axHelp
          Up and Down: the lines of the list
        """ + BundleHelp.NoticesSection("axHelp", "the list");

    /// <summary>docs/axhelp/user-guide.md, embedded by AxHelp.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axhelp/user-guide.md");
}
