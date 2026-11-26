using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>The texts axGIT shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AG-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axGIT's own, the controls' own. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axGIT", []) + $"""
        ## axGIT
          Find searches the list while the list has the focus, and the details otherwise.
          Type the start of an item to jump to it, the same letter again to the next; Shift with a letter goes backwards.
          Open repository shows the Windows folder picker; any folder inside the repository will do.
          The window starts in Changes, one of three lists of the repository: Changes, Log and Branches.
          Left or Backspace goes up to the repository, where the three lists are rows. Right or Enter on one goes into it.
          Alt+1, Alt+2 and Alt+3 go straight to Changes, Log or Branches from anywhere; pressed in the list already shown, the key says its name.
          In a list
            Enter shows the file's diff in the details in Changes, the commit's details in Log, and in Branches switches to the branch.
            Only the files of Changes are selected; a selected file reads ", selected". Escape deselects all.
            Delete discards the changes to the file, after a question; with files selected, to all of them after one question whose default is Cancel.
            Right on a row that is no list, and Left at the repository, play the edge sound.
          Stage and Unstage act on the selected files, or on the file you are on. Actions, Stage all and Unstage all do every file.
          Open file in editor opens the file in axDown, or in axJSON for a .json file.
          Commit asks for the message in a notice and commits what is staged.
          New branch asks for its name, and axGIT switches to it.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here, and axGIT says so.

        ## The details
          Arrow keys, Home, End, Ctrl+Home and Ctrl+End move as in any text.
          Page Up and Page Down move one screen, or to the first or the last line.
          {BundleKeys.Text(BundleKeys.ParagraphUp)} and {BundleKeys.Text(BundleKeys.ParagraphDown)} go to the previous or the next paragraph, the first line after a blank line.
          Shift with any of them, or Ctrl+A, selects, and Ctrl+C copies.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axGIT", "the list and the details");

    /// <summary>docs/axGIT/user-guide.md, embedded by AxGit.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axGIT/user-guide.md");
}
