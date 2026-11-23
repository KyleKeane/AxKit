using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxPass;

/// <summary>The texts axPass shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axPass",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find an entry in the navigator. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ],
        altF4: "close axPass, which locks it") + $"""
        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          Right: into a group. On an entry, nothing: the edge sound
          Left or Backspace: up a level
          {BundleKeys.Text(ItemKeys.PreviousSection)}, {BundleKeys.Text(ItemKeys.NextSection)}: the section before or after, on a level in sections (the other item apps'); here the edge sound
          Enter: open: a group shows what is in it, an entry moves to its fields. On a locked window, Enter unlocks
          Tab, {BundleKeys.Text(BundleKeys.NextControl)} or F6: to the detail pane
          {BundleKeys.Text(ItemKeys.New)}: a new entry here. {BundleKeys.Text(ItemKeys.NewList)}: a new group here
          F2: rename the entry in place: a box over the line, Enter keeps the new title, Escape the old. Delete: move it to the Recycle Bin; {BundleKeys.Text(ItemKeys.Undo)} brings it back
          Space: select the entry you are on, or deselect it again; a selected entry reads ", selected". {BundleKeys.Text(SelectionKeys.SelectDown)}, {BundleKeys.Text(SelectionKeys.SelectUp)}, {BundleKeys.Text(SelectionKeys.SelectToStart)}, {BundleKeys.Text(SelectionKeys.SelectToEnd)}: select as you move. {BundleKeys.Text(SelectionKeys.SelectTo)}: select from the nearest selected entry to this one. {BundleKeys.Text(SelectionKeys.SelectAll)}: select every entry of the group. {BundleKeys.Text(SelectionKeys.SelectNone)}, or Escape: deselect all. Delete then moves all the selected entries to the Recycle Bin, and one {BundleKeys.Text(ItemKeys.Undo)} brings them all back. Groups, and what is in the Recycle Bin, are not selected
          {BundleKeys.Text(AxPassKeys.MoveEntry)}: move the entry to another group, chosen from a list
          {BundleKeys.Text(ItemKeys.ReadAll)}: read every field of the entry; the password is never read
          {BundleKeys.Text(ItemKeys.ItemMenu)}, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the Context menu, what acts on the selection

        ## The detail pane, on the right
          It shows the fields of the entry selected in the navigator: Title, UserName, Password (read as hidden), URL, Notes
          Up, Down: move through the fields
          Enter: change the field. On Password: type a new one twice, or Generate (Alt+G) makes one
          Right and Left change nothing here and play the edge sound: an entry has no choice and no check, and the user name and the password never change on an arrow
          {BundleKeys.Text(ItemKeys.ReadAll)}: read the field's whole value
          Escape, Tab, {BundleKeys.Text(ItemKeys.OtherPaneBack)}, {BundleKeys.Text(BundleKeys.NextControl)}, {BundleKeys.Text(BundleKeys.PreviousControl)} or F6: back to the navigator

        ## Everywhere in axPass
          {BundleKeys.Text(AxPassKeys.CopyUserName)}: copy the user name
          {BundleKeys.Text(AxPassKeys.CopyPassword)}: copy the password; the clipboard clears after 30 seconds
          {BundleKeys.Text(AxPassKeys.OpenUrl)}: open the URL in the browser
          {BundleKeys.Text(AxPassKeys.Lock)}: lock: the master password is forgotten until you type it again
          {BundleKeys.Text(AxPassKeys.OpenDatabase)}: open another database
          {BundleKeys.Text(ItemKeys.Undo)}: undo the last change. F5: read the database again
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; every change is written to the database at once, by KeePassXC's tool
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axPass", "the panes");

    /// <summary>docs/axPass/user-guide.md, embedded by AxPass.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axPass/user-guide.md");
}
