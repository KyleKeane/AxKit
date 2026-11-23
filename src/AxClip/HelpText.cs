using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxClip;

/// <summary>The texts axClip shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md CP-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axClip",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}, {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: find in a notice's text",
        ],
        altF4: "hide axClip (Quit axClip, in its first menu, ends it)") + $"""
        ## axClip
          Ctrl+Shift+Apostrophe, or the hotkey you chose: show axClip from anywhere; again: hide it
          Type: only the items that hold every word typed
          Down, Up: the next or the previous item. {BundleKeys.Text(AxClipKeys.FirstMatch)}, {BundleKeys.Text(AxClipKeys.LastMatch)}: the first or the last. Page Down, Page Up: a screen
          Enter: paste the item into the program you were in
          {BundleKeys.Text(AxClipKeys.CopyOnly)}: put the item on the clipboard without pasting
          {BundleKeys.Text(AxClipKeys.ReadInFull)}: read the item in full
          Delete: remove the item. {BundleKeys.Text(AxClipKeys.ClearHistory)}: clear the history, after a question
          {BundleKeys.Text(AxClipKeys.PauseRecording)}: pause recording, or record again
          Escape: hide axClip
        """ + BundleHelp.NoticesSection("axClip", "the list");

    /// <summary>docs/axClip/user-guide.md, embedded by AxClip.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axClip/user-guide.md");
}
