using System.Reflection;
using AxKit.Core;

namespace AxGit;

/// <summary>The texts axGIT shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AG-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axGIT's own, the controls' own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axGIT",
        [
            "Tab, Shift+Tab, Ctrl+Tab or F6: switch between the list and the details",
            "Shift+F10, the Applications key or Ctrl+Period: the Context menu, what acts on the row you are on",
            "Ctrl+F: find in the list while the list has the focus, in the details otherwise. F3 and Shift+F3: next or previous match",
        ]) + """
        ## axGIT
          Type the start of an item to jump to it, the same letter again to the next; Shift with a letter backwards
          Ctrl+O: open a repository (the Windows folder picker; any folder inside it will do)
          F5: refresh the list and the status bar
          Ctrl+W: show the repository's folder in File Explorer
          The window starts in Changes, one of three lists of the repository: Changes, Log and Branches
          Left or Backspace: up to the repository, where the three lists are rows. Right or Enter on one: into it
          Alt+1, Alt+2, Alt+3: straight to Changes, Log or Branches from anywhere; pressed in the list already shown, the key says its name. Ctrl+1, Ctrl+2 and Ctrl+3, their keys until now, still work
          In a list
            Enter: in Changes, the file's diff in the details; in Log, the commit's details; in Branches, switch to the branch
            Space: select the file you are on in Changes, or deselect it again; a selected file reads ", selected". Shift+Down, Shift+Up, Shift+Home, Shift+End: select as you move. Shift+Space: select from the nearest selected file to this one. Ctrl+A: select every file. Ctrl+Shift+A, or Escape: deselect all
            Delete: discard the changes to the file, after a question; with files selected, to all of them after one question whose default is Cancel
            Right on a row that is no list, Left at the repository: the edge sound
          Ctrl+Shift+S: stage the selected files, or the file you are on. Ctrl+Shift+U: unstage them. Actions, Stage all and Unstage all do every file
          Ctrl+E: open the file in axDown, or axJSON for a .json file
          Ctrl+Enter: commit what is staged; a notice asks for the message
          Ctrl+N: a new branch, after its name; axGIT switches to it
          Ctrl+Shift+F, Ctrl+Shift+L, Ctrl+Shift+H: fetch, pull, push
          Ctrl+S: nothing to save here; axGIT says so

        ## The details
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C: copy
        """ + BundleHelp.NoticesSection("axGIT", "the list and the details");

    /// <summary>docs/axGIT/user-guide.md, embedded by AxGit.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axGIT/user-guide.md");
}
