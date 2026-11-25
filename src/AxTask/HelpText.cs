using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxTask;

/// <summary>The texts axTask shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axTask", []) + $"""
        ## The navigator, on the left
          Up, Down, Home and End move, and the edge sound plays at an end. Type the start of a name to jump to it; Shift with a letter goes backwards.
          The top level is under headings of three levels. Level 1: Your lists, Projects, Smart lists, Date lists, Archived lists. Level 2: your own sections under Your lists, a project folder's projects under Projects, under Smart lists the first word your smart lists share (App for App axDown and App axClaude) and Tags. Level 3: under Tags, one heading per symbol, such as @ at sign.
          Smart lists, Tags and Date lists gather your lists and every project's; each item says where it lives, and a change is made to its own file.
          The keys for the next and the previous section go to the heading before or after, of either level; the edge sound plays past the first or the last.
          At a top level, Ctrl with 1, 2 or 3 goes to the next heading of that level, and Ctrl with Shift to the one before. In a list these keys are action keys.
          Right goes into a list or a smart list; on an item or a section's header it does nothing and plays the edge sound. Left or Backspace goes back to the sections.
          Enter opens: a list shows its items, and an item opens its form, one control for each of its fields. Tab moves between them, {BundleKeys.Text(BundleKeys.MainAction)} saves them all as one step, and Escape leaves with nothing changed.
          Only items are selected, never lists, and another list starts with none; a selected item reads ", selected". Escape deselects all.
          With items selected, Done or not done, Delete, Move to a list and the list's actions that set fields or move act on all of them, as one step for Undo: "Done: 3 tasks", "Archived 3 items". Done or not done marks them all done while any is not, and all not done when every one is. Enter, F2 and the ways to open stay on the item you are on.
          New list makes the list in the section of the line you are on.
          {BundleKeys.Text(AxTaskKeys.MoveToSection)} on a list moves it to a section, or to a new one; its Section field in the detail pane does the same.
          {BundleKeys.Text(AxTaskKeys.MoveToSection)} on an item, or with items selected, moves it, or all of them, to a list you choose in a form, or to a new list you name there, as one step for Undo: "3 items moved to Later".
          Moving an item up or down in its list makes the list keep that order from then on: "Moved up: Fix the tap, 3 of 12". Options, New items at the top of this list puts each new item there.
          F2 renames in place, in a box over the line: Enter keeps the new name, Escape the old. Delete moves to the Archive, and Undo brings it back.
          All tasks lists every task not done.
          Tags: a smart list for each word your tasks not done start with a symbol, @kyle for the person a task is assigned to, #ui, !urgent, $budget or %done, in their titles and text fields.
          Date lists: Overdue, Due today, Due in the next 7 days, No due date.
          Find in every list finds words in every list, your own and every project's. "Searching every list" is said, then what was found; another notice opened meanwhile drops the result. In what was found, Right or Enter opens the item where it lives, and Left or Escape goes back.
          The action keys, Ctrl with a digit and Ctrl with Shift and a digit, run the list's own actions on the item you are on. An action sets fields, moves the item to a list, opens something, runs a program with what the item holds as its arguments, or puts a command into axTerminal's command field for you to read and send. They are written in the list's configuration, {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list; Context, Actions of this list names them all, with or without a key.
          An action that runs a program is asked about before its first run, with the program and every argument written out; Enter there is Cancel. "Running" and its name is said as it starts, and "done" or "failed" with its exit code as it ends; Context, Actions of this list, Read the last output shows what it wrote. Actions, Stop the running action ends it.

        ## The detail pane, on the right
          It shows the fields of the item you are on in the navigator, or the details of the list.
          Up and Down move through the fields.
          Enter on a field of an item opens the item's form with the focus in that field; on a list's detail, it changes it. Space on a check marks done or not done.
          Right and Left go to the next and the previous value of a choice; on a check, Right checks and Left unchecks. Each is said once and is one step for Undo; the edge sound plays at an end and on any other field.
          Which fields a list's items have is written once, in the list's configuration: {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list opens it in axJSON, where a field is added, renamed, taken out or moved for every item at once. A field taken out is hidden and its values are kept; put back, they show again. New item, F2, Delete, Alt+Up and Alt+Down in the fields say where this is done.
          A last line such as "2 more fields kept: Sprint, Old priority" names what an item holds that its list does not name: Enter reads those values.
          Escape, Tab or F6 goes back to the navigator.

        ## Projects
          Options, Add a project folder adds a folder axTask looks in, four folders down, for your other folders of lists, never your own top folder or one inside it; Remove a project folder takes one off.
          Under the Projects heading, a heading of level 2 for each project folder, "Projects in" and the folder's name, has each folder of lists found under it and its count of lists.
          Right on a project shows its lists, smart lists and Archive, changed in place as your own. Left from its top goes back to your lists, on the project.

        ## AI tools
          While this window is open, an AI tool you allowed reads your lists, and adds an item while axKit menu, AI tools, Let AI tools run commands is on: "Claude added Call the dentist to Personal". Undo takes it back.

        ## Everywhere in axTask
          {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on a list edits its configuration in axJSON: its details, the fields of its items and its actions, written first when the list has none. On an item or a smart list, it opens its file there. Back in axTask, one line says what the configuration holds now, or that it does not read and at which line.
          A list written by an earlier version of axTask says "earlier format" and is not opened: Enter on it asks to convert it, and a copy is kept among the Archived lists.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here: every change is written at once.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axTask", "the panes");

    /// <summary>docs/axTask/user-guide.md, embedded by AxTask.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTask/user-guide.md");
}
