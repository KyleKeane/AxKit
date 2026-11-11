using System.Reflection;
using AxKit.Core;

namespace AxTask;

/// <summary>The texts axTask shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axTask",
        [
            "Ctrl+F: find an entry in the navigator. F3 and Shift+F3: next or previous match",
        ]) + """
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          The top level is in sections: Smart lists, User lists, your own sections, Archived lists, each under a header line
          Ctrl+Up, Ctrl+Down: the section before or after; the edge sound past the first or the last
          Right: into a list or a smart list. On an item or a section's header, nothing: the edge sound
          Left or Backspace: back to the sections
          Enter: open: a list shows its items, an item moves to its fields
          Tab, Ctrl+Tab or F6: to the detail pane
          Space: done or not done, on a task
          Ctrl+N: a new item in this list. Ctrl+Shift+N: a new list, in the section of the line selected
          Ctrl+M on a list: move it to a section, or to a new one; its Section field in the detail pane does the same
          F2: rename in place: a box over the line, Enter keeps the new name, Escape the old. Delete: move to the Archive; Ctrl+Z brings it back
          Ctrl+Q: read every field of the item
          Ctrl+0: All tasks, every task not done
          Ctrl+Shift+F: find words in every list, your own and every project's. In what was found, Right or Enter opens the item where it lives, Left or Escape goes back
          Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+4, Ctrl+5, Ctrl+6, Ctrl+7, Ctrl+8, Ctrl+9: the list's own action keys on the item selected, set in List, Action keys: fields set, the item moved to a list, or something opened; the Item menu's Action keys submenu names them
          Ctrl+Shift+D: hide or show the done tasks of a list
          Shift+F10 or the Applications key: the menu of the selection

        ## The detail pane, on the right
          It shows the fields of the item selected in the navigator, or the details of the list
          Up, Down: move through the fields
          Enter: change the field. Space: done or not done, on a check
          Ctrl+N: a new field. F2: rename the field in place. Delete: remove it. Alt+Up, Alt+Down: move it
          Ctrl+Q: read the field's whole value
          Escape, Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6: back to the navigator

        ## Projects
          Options, Project folders, Add a project folder: a folder axTask looks in, four folders down, for your other folders of lists; Remove a project folder takes one off
          While project folders are set, the top level starts with Projects, the number of lists found. Right shows the projects, each with its count of lists
          Right on a project: its lists, smart lists and Archive, changed in place as your own. Left from its top goes back to Projects, and Left again to your lists

        ## GitHub lists
          List, New GitHub list makes a list of a repository's open issues, read from GitHub when it opens and on F5; your Notes on each issue stay
          Item, GitHub: make it a bug, a feature request or feedback, close, reopen, comment, block the reporter or the version, open on GitHub, open in axClaude, as your own GitHub account (List, GitHub account, a token pasted once)

        ## Everywhere in axTask
          Ctrl+Z: undo the last change
          F5: read the folder again, and a GitHub list from GitHub
          Ctrl+J: open the item, the list's details or the smart list in axJSON
          Ctrl+E: show it in axFile
          Ctrl+S: nothing to save here; every change is written at once
        """ + BundleHelp.NoticesSection("axTask", "the panes");

    /// <summary>docs/axTask/user-guide.md, embedded by AxTask.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTask/user-guide.md");
}
