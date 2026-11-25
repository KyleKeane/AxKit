using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxClip;

/// <summary>The texts axClip shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md CP-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axClip",
        [],
        altF4: "hide axClip (Quit axClip, in its first menu, ends it)") + """
        ## axClip
          Ctrl+Shift+Comma, or the hotkey you chose, shows axClip from any program; pressed again, it hides axClip.
          Typing lists only the items that hold every word typed. The caret stays in the field, and the keys that move go through the items under it.
          Enter pastes the item into the program you were in.
          Clear history asks first.
          Pause recording, pressed again, records again.
        """ + BundleHelp.NoticesSection("axClip", "the list");

    /// <summary>docs/axClip/user-guide.md, embedded by AxClip.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axClip/user-guide.md");
}
