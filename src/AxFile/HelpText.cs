using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>The texts axFile shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>A key of the tables as the text names it (the key documents item, 2026-11-24: no key typed by hand here).</summary>
    private static string K(Keys key) => BundleKeys.Text(key).TrimEnd('.');

    /// <summary>The F1 text (SPEC.md AF-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axFile's own by what they do. The first line and the bundle's section come from <c>BundleHelp</c>; every key from <see cref="AxFileKeys"/>, <see cref="BundleKeys"/> and <see cref="SelectionKeys"/>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axFile",
        [
            $"Tab, Shift+Tab, {K(BundleKeys.NextControl)}, {K(BundleKeys.PreviousControl)} or {K(BundleKeys.NextControlAlso)}: between the folder list and the Favorites list",
            $"{K(BundleKeys.Find)}: find an entry whose name contains the text. {K(BundleKeys.FindNext)} and {K(BundleKeys.FindPrevious)}: the next or the previous match",
        ]) + $"""
        ## Moving around
          Arrow keys, Home, End, Page Up, Page Down: through the entries. At the first or the last entry, a key that cannot move plays one short low note
          Type the start of a name, the letters within a second of each other: the first name starting with it, as in File Explorer; the same letter again, the next such name; Shift with a letter, the same backwards; the low note when no name starts with it
          {K(AxFileKeys.Open)} or Right Arrow on a folder: into it; Right on a file plays the low note and never opens it. {K(AxFileKeys.Up)}, Left Arrow or {K(AxFileKeys.UpAlso)}: the folder above, the one you left selected; from a drive's top, This PC with the drives. Each says the folder, its count and the entry you land on: "docs, 5 items, plan.md"
          {K(AxFileKeys.GoToFolder)} or {K(AxFileKeys.GoToFolderAlso)}: go to a typed path, %USERPROFILE%\Downloads for instance; in its field, Down and Up complete the path from what you typed, "Documents, folder, 2 of 5", and Right at the end of a completed folder goes into it. {K(AxFileKeys.OpenFolder)}: the Windows folder picker. Session, Open recent folder and Navigation, Favorites: by number. {K(AxFileKeys.Favorite)}: add this folder to Favorites, or remove it
          {K(AxFileKeys.NewWindow)}: a new axFile window on this folder. {K(AxFileKeys.Refresh)}: read the folder again
          Navigation, Drives: the drives by number, This PC, Eject this drive, Drive properties

        ## Opening
          {K(AxFileKeys.Open)} on a file: the program Windows has for it (the one you made its default: axDown, axMedia, axPreview or any other), or the Windows dialog that picks one when there is none
          {K(AxFileKeys.Preview)}: read the file in axPreview; a folder opens in a new axFile window
          {K(BundleKeys.MainAction)}: play the folder's sound and video files in axMedia. {K(AxFileKeys.AddToQueue)}: add the entry, or the selected entries, to axMedia's queue, staying here
          Alt+C, {K(AxFileKeys.ContextMenu)}, the Applications key or {K(BundleKeys.ContextMenu)}: the Context menu, the entry's, one list in File Explorer's order, with Open with (Windows default, Choose a program, your own programs), Open with axKit apps (A), Open with axKit access layers (S) and Windows menu, the full Windows menu for the entry.
          {K(AxFileKeys.Properties)}: the entry's Properties, the Windows sheet

        ## Selecting
          {K(SelectionKeys.Select)}: select the entry, or deselect it; it reads "selected" after its name while it is
          {K(SelectionKeys.SelectDown)}, {K(SelectionKeys.SelectUp)}, {K(SelectionKeys.SelectToStart)}, {K(SelectionKeys.SelectToEnd)}: select from where you started to there, as in File Explorer, keeping what was selected before
          {K(SelectionKeys.SelectTo)}: select from the nearest selected entry to the one you are on, above or below; with no other selected, as {K(SelectionKeys.Select)}
          {K(SelectionKeys.SelectAll)}: select all. {K(SelectionKeys.SelectNone)}, or {K(AxFileKeys.Clear)}: deselect all. {K(AxFileKeys.Information)} says how many are selected
          Cut, Copy, Delete, Copy path, Move to folder, Compress to zip and Add to axMedia's queue act on the selected entries, or on the entry you are on when none is selected
          Context, Selection: the selected entries from anywhere: select this entry, select to here, then Cut, Copy, Delete, Move to folder, Compress to zip, Add to axMedia's queue and Copy paths on them alone; "None selected" when there are none. Select all and Deselect all are in Actions
          {K(SelectionKeys.Select)} on a selected entry, or {K(AxFileKeys.Clear)} for all, takes a selection back; {K(AxFileKeys.Undo)} never does. The status bar says how many are selected

        ## File operations
          {K(AxFileKeys.NewFolder)}: a new folder, after its name. {K(AxFileKeys.NewFile)}: a new empty text file, with the rename box open on its whole name: type the name with its extension, Enter
          {K(AxFileKeys.Rename)}: rename in place, in a box over the entry with the name before the extension selected; Enter renames, Escape keeps the name. {K(AxFileKeys.Delete)}: to the Recycle Bin, after a question
          {K(AxFileKeys.Cut)}, {K(AxFileKeys.Copy)}, {K(AxFileKeys.Paste)}: cut, copy or paste as files, to and from File Explorer too; a pasted name already in the folder gets a number. {K(AxFileKeys.CopyPath)}: copy the entry's full path, or every selected entry's, one per line
          In the entry's menu: Move to folder, Compress to zip, Extract here
          {K(AxFileKeys.Undo)}: undo the newest operation that can be undone, one per press, the last twenty: a rename renamed back, a new folder or file removed while still empty, a move moved back, a paste's copies or a zip sent to the Recycle Bin, a delete restored from it. Actions, Undo names what it would undo
          Every operation runs on its own: a second paste, delete or zip can start while the first runs; the status bar shows the newest operation's line, "Compressing, 40 of 120 files" while it runs
          Actions, Recent operations: the operations of this window, newest first, "Copied 3 items, 21:04", "…, undone" once taken back; Enter on one goes to where it left things, Escape closes the list. Selecting writes none

        ## Information
          {K(AxFileKeys.Information)}: the entry's size and date; for a drive, the free space
          {K(AxFileKeys.ShowItemInExplorer)}: show the entry in File Explorer. {K(AxFileKeys.ShowInExplorer)}: show the folder in File Explorer. Session, Show current folder in: axClaude, axTerminal, axGIT
          NVDA+End: the status bar: the newest operation and how many entries are selected on the left, the folder's full path on the right
          {K(BundleKeys.Save)}: nothing to save here; axFile says so

        ## The Favorites list
          Under the folder list, always; "No favorites yet" until the first. Tab, Shift+Tab, {K(BundleKeys.NextControl)}, {K(BundleKeys.PreviousControl)} or {K(BundleKeys.NextControlAlso)} moves between the two lists, {K(AxFileKeys.Clear)} goes back to the folder list, Navigation, The Favorites list goes there
          One line per favorite, in the order added: its name, then its full path, "Music (C:\Music)". {K(AxFileKeys.Favorite)} in any folder adds it, and selects it here
          {K(AxFileKeys.Open)}: the folder in the folder list, the focus there. {K(AxFileKeys.Delete)}: take it out of the favorites; the folder itself stays. {K(AxFileKeys.Information)}: its full path and place, "C:\Music, 2 of 3". {K(AxFileKeys.ContextMenu)} or the Applications key: the Context menu with these for the line
        """ + BundleHelp.FilterAndSortSection("Name, Date or Size, folders first") + BundleHelp.NoticesSection("axFile", "the lists");

    /// <summary>docs/axFile/user-guide.md, embedded by AxFile.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axFile/user-guide.md");
}
