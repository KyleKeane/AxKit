using System.Reflection;
using AxKit.Core;

namespace AxDescribe;

/// <summary>The texts axDescribe shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md DE-7): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axDescribe's own, then the bundle's Notices section. The first line, the bundle's section and the Notices section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axDescribe",
        [
            "Ctrl+F: find a line in the list. F3 and Shift+F3: next or previous match",
        ]) + """
        ## axDescribe
          Up and Down: the lines of the list
        """ + BundleHelp.NoticesSection("axDescribe", "the list");

    /// <summary>docs/axdescribe/user-guide.md, embedded by AxDescribe.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axdescribe/user-guide.md");
}
