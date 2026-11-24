using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxTask;

/// <summary>The texts axTask shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axTask",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find an entry in the navigator. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ]) + $"""
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          The top level is under headings of three levels. Level 1: Your lists, Projects, Smart lists, Date lists, Archived lists. Level 2: your own sections under Your lists, a project folder's projects under Projects, under Smart lists the first word your smart lists share (App for App axDown and App axClaude) and Tags. Level 3: under Tags, one heading per symbol, such as @ at sign
          Smart lists, Tags and Date lists gather your lists and every project's; each item says where it lives, and a change is made to its own file
          {BundleKeys.Text(ItemKeys.PreviousSection)}, {BundleKeys.Text(ItemKeys.NextSection)}: the heading before or after, of either level; the edge sound past the first or the last
          Ctrl+1, Ctrl+2, Ctrl+3: the next heading of level 1, 2 or 3, at a top level. Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3: the one before. In a list these are action keys
          Right: into a list or a smart list. On an item or a section's header, nothing: the edge sound
          Left or Backspace: back to the sections
          Enter: open: a list shows its items; an item opens its form, one control for each of its fields: Tab moves between them, {BundleKeys.Text(BundleKeys.MainAction)} saves them all as one step, Escape leaves and nothing is changed
          Tab, {BundleKeys.Text(BundleKeys.NextControl)} or F6: to the detail pane
          {BundleKeys.Text(BundleKeys.MainAction)}: done or not done, on a task
          Space: select the item you are on, or deselect it again; a selected item reads ", selected". {BundleKeys.Text(SelectionKeys.SelectDown)}, {BundleKeys.Text(SelectionKeys.SelectUp)}, {BundleKeys.Text(SelectionKeys.SelectToStart)}, {BundleKeys.Text(SelectionKeys.SelectToEnd)}: select as you move. {BundleKeys.Text(SelectionKeys.SelectTo)}: select from the nearest selected item to this one. {BundleKeys.Text(SelectionKeys.SelectAll)}: select every item shown. {BundleKeys.Text(SelectionKeys.SelectNone)}, or Escape: deselect all. Items are selected, not lists; another list starts with none
          With items selected, {BundleKeys.Text(BundleKeys.MainAction)}, Delete, {BundleKeys.Text(AxTaskKeys.MoveToSection)} and the list's actions that set fields or move act on all of them, as one step for {BundleKeys.Text(ItemKeys.Undo)}: "Done: 3 tasks", "Archived 3 items". {BundleKeys.Text(BundleKeys.MainAction)} marks them all done while any is not, and all not done when every one is. Enter, F2 and the ways to open stay on the item you are on
          {BundleKeys.Text(ItemKeys.New)}: a new item in this list. {BundleKeys.Text(ItemKeys.NewList)}: a new list, in the section of the line you are on
          {BundleKeys.Text(AxTaskKeys.MoveToSection)} on a list: move it to a section, or to a new one; its Section field in the detail pane does the same
          {BundleKeys.Text(AxTaskKeys.MoveToSection)} on an item, or with items selected: move it, or all of them, to a list you choose in a form, or to a new list you name there, as one step for {BundleKeys.Text(ItemKeys.Undo)}: "3 items moved to Later"
          {BundleKeys.Text(AxTaskKeys.MoveItemUp)}, {BundleKeys.Text(AxTaskKeys.MoveItemDown)}: move the item you are on up or down in its list, which keeps that order from then on: "Moved up: Fix the tap, 3 of 12". {BundleKeys.Text(AxTaskKeys.MoveItemToTop)}, {BundleKeys.Text(AxTaskKeys.MoveItemToBottom)}: to the top or the bottom. Options, New items at the top of this list puts each new item there
          F2: rename in place: a box over the line, Enter keeps the new name, Escape the old. Delete: move to the Archive; {BundleKeys.Text(ItemKeys.Undo)} brings it back
          {BundleKeys.Text(ItemKeys.ReadAll)}: read every field of the item
          Ctrl+0: All tasks, every task not done
          Tags: a smart list for each word your tasks not done start with a symbol, @kyle for the person a task is assigned to, #ui, !urgent, $budget or %done, in their titles and text fields
          Date lists: Overdue, Due today, Due in the next 7 days, No due date
          {BundleKeys.Text(AxTaskKeys.FindEverywhere)}: find words in every list, your own and every project's. "Searching every list" is said, then what was found; another notice opened meanwhile drops the result. In what was found, Right or Enter opens the item where it lives, Left or Escape goes back
          Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+4, Ctrl+5, Ctrl+6, Ctrl+7, Ctrl+8, Ctrl+9, and Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3, Ctrl+Shift+4, Ctrl+Shift+5, Ctrl+Shift+6, Ctrl+Shift+7, Ctrl+Shift+8, Ctrl+Shift+9: the list's own actions on the item you are on. An action sets fields, moves the item to a list, opens something, runs a program with what the item holds as its arguments, or puts a command into axTerminal's command field for you to read and send. They are written in the list's configuration, {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list; Context, Actions of this list, names them all, with or without a key
          An action that runs a program is asked about before its first run, with the program and every argument written out; Enter there is Cancel. "Running" and its name is said as it starts, "done" or "failed" with its exit code as it ends; Context, Actions of this list, Read the last output shows what it wrote. Actions, Stop the running action ends it
          {BundleKeys.Text(AxTaskKeys.HideDone)}: hide or show the done tasks of a list
          {BundleKeys.Text(ItemKeys.ItemMenu)}, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the Context menu, what acts on the item or the list you are on

        ## The detail pane, on the right
          It shows the fields of the item you are on in the navigator, or the details of the list
          Up, Down: move through the fields
          Enter: on a field of an item, the item's form with the focus in that field; on a list's detail, change it. Space: done or not done, on a check
          Right and Left: on a choice, the next and the previous value; on a check, Right checks and Left unchecks. Said once, one step for {BundleKeys.Text(ItemKeys.Undo)}; the edge sound at an end and on any other field
          Which fields a list's items have is written once, in the list's configuration: {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list opens it in axJSON, where a field is added, renamed, taken out or moved for every item at once. A field taken out is hidden and its values are kept; put back, they show again. {BundleKeys.Text(ItemKeys.New)}, F2, Delete, Alt+Up and Alt+Down in the fields say where this is done
          A last line such as "2 more fields kept: Sprint, Old priority" names what an item holds that its list does not name: Enter reads those values
          {BundleKeys.Text(ItemKeys.ReadAll)}: read the field's whole value
          Escape, Tab, {BundleKeys.Text(ItemKeys.OtherPaneBack)}, {BundleKeys.Text(BundleKeys.NextControl)}, {BundleKeys.Text(BundleKeys.PreviousControl)} or F6: back to the navigator

        ## Projects
          Options, Add a project folder: a folder axTask looks in, four folders down, for your other folders of lists, never your own top folder or one inside it; Remove a project folder takes one off
          Under the Projects heading, a heading of level 2 for each project folder, "Projects in" and the folder's name, with each folder of lists found under it and its count of lists
          Right on a project: its lists, smart lists and Archive, changed in place as your own. Left from its top goes back to your lists, on the project

        ## AI tools
          While this window is open, an AI tool you allowed reads your lists, and adds an item while axKit menu, AI tools, Let AI tools run commands is on: "Claude added Call the dentist to Personal". {BundleKeys.Text(ItemKeys.Undo)} takes it back

        ## Everywhere in axTask
          {BundleKeys.Text(ItemKeys.Undo)}: undo the last change
          F5: read the folder again
          {BundleKeys.Text(AxTaskKeys.OpenInAxJson)}: on a list, edit its configuration in axJSON (its details, the fields of its items, its actions; written first when the list has none); on an item or a smart list, open its file there. Back in axTask, one line says what the configuration holds now, or that it does not read and at which line
          A list written by an earlier version of axTask says "earlier format" and is not opened: Enter on it asks to convert it, and a copy is kept among the Archived lists
          {BundleKeys.Text(AxTaskKeys.ShowInAxFile)}: show it in axFile
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; every change is written at once
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axTask", "the panes");

    /// <summary>docs/axTask/user-guide.md, embedded by AxTask.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTask/user-guide.md");
}
