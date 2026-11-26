using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>The texts axFile shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>A key of the tables as the text names it (the key documents item, 2026-11-24: no key typed by hand here).</summary>
    private static string K(Keys key) => BundleKeys.Text(key).TrimEnd('.');

    /// <summary>The F1 text (SPEC.md AF-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axFile's own by what they do. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context, each key it names from <see cref="AxFileKeys"/>, <see cref="BundleKeys"/> and <see cref="SelectionKeys"/>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axFile", []) + $"""
        ## Moving around
          Arrow keys, Home, End, Page Up and Page Down move through the entries. At the first or the last entry, a key that cannot move plays one short low note.
          Type the start of a name, the letters within a second of each other, to go to the first name starting with it, as in File Explorer; the same letter again goes to the next such name, and Shift with a letter backwards. The low note plays when no name starts with it.
          {K(AxFileKeys.Open)} or Right Arrow on a folder goes into it; Right on a file plays the low note and never opens it. {K(AxFileKeys.Up)} or Left Arrow goes to the folder above, with the one you left selected; from a drive's top, to This PC with the drives. Each says the folder, its count and the entry you land on: "docs, 5 items, plan.md".
          Go to folder takes a typed path, %USERPROFILE%\Downloads for instance. In its field, Down and Up complete the path from what you typed, "Documents, folder, 2 of 5", and Right at the end of a completed folder goes into it. Session, Open recent folder and Navigation, Favorites list the folders by number.
          Navigation, Drives has the drives by number, This PC, Eject this drive and Drive properties.

        ## Opening
          {K(AxFileKeys.Open)} on a file opens it in the program Windows has for it, the one you made its default (axDown, axMedia, axPreview or any other), or in the Windows dialog that picks one when there is none.
          Preview reads a file in axPreview; on a folder, it opens a new axFile window.
          Add to axMedia's queue adds the entry, or the selected entries, and the focus stays here.
          The Context menu of an entry is one list in File Explorer's order, with Open with (Windows default, Choose a program, your own programs), Open with axKit apps (A), Open with axKit access layers (S) and Windows menu, the full Windows menu for the entry.

        ## Selecting
          {K(SelectionKeys.Select)} selects the entry, or deselects it; it reads "selected" after its name while it is.
          {K(SelectionKeys.SelectDown)}, {K(SelectionKeys.SelectUp)}, {K(SelectionKeys.SelectToStart)} and {K(SelectionKeys.SelectToEnd)} select from where you started to there, as in File Explorer, keeping what was selected before.
          {K(SelectionKeys.SelectTo)} selects from the nearest selected entry to the one you are on, above or below; with no other selected, it does as {K(SelectionKeys.Select)}.
          {K(AxFileKeys.Clear)} deselects all too, and {K(AxFileKeys.Information)} says how many are selected. The status bar says it as well.
          Cut, Copy, Delete, Copy path, Move to folder, Compress to zip and Add to axMedia's queue act on the selected entries, or on the entry you are on when none is selected.
          Context, Selection reaches the selected entries from anywhere: select this entry, select to here, then Cut, Copy, Delete, Move to folder, Compress to zip, Add to axMedia's queue and Copy paths on them alone; it says "None selected" when there are none. Select all and Deselect all are in Actions.
          {K(SelectionKeys.Select)} on a selected entry, or {K(AxFileKeys.Clear)} for all, takes a selection back; Undo never does.

        ## File operations
          New folder asks for its name. New text file makes an empty file and opens the rename box on its whole name: type the name with its extension, then Enter.
          Rename works in place, in a box over the entry with the name before the extension selected; Enter renames, Escape keeps the name. Delete sends to the Recycle Bin, after a question.
          Cut, Copy and Paste work as files, to and from File Explorer too; a pasted name already in the folder gets a number. Copy path copies the entry's full path, or every selected entry's, one per line.
          The entry's menu has Move to folder, Compress to zip and Extract here.
          Undo takes back the newest operation that can be undone, one per press, the last twenty: a rename renamed back, a new folder or file removed while still empty, a move moved back, a paste's copies or a zip sent to the Recycle Bin, a delete restored from it. Actions, Undo names what it would undo.
          Every operation runs on its own: a second paste, delete or zip can start while the first runs. The status bar shows the newest operation's line, "Compressing, 40 of 120 files", while it runs.
          Actions, Recent operations lists the operations of this window, newest first, "Copied 3 items, 21:04", or "…, undone" once taken back. Enter on one goes to where it left things, and Escape closes the list. Selecting writes none.

        ## Information
          Information says the entry's size and date, and for a drive the free space.
          Session, Show current folder in has axClaude, axTerminal and axGIT too.
          NVDA+End reads the status bar: the newest operation and how many entries are selected on the left, the folder's full path on the right.
          {K(BundleKeys.Save)} has nothing to save here, and axFile says so.

        ## The Favorites list
          It is under the folder list, always, and says "No favorites yet" until the first. Tab moves between the two lists, {K(AxFileKeys.Clear)} goes back to the folder list, and Navigation, The Favorites list goes there.
          Each favorite is one line, in the order added: its name, then its full path, "Music (C:\Music)". Adding a folder to the favorites selects it here.
          {K(AxFileKeys.Open)} shows the folder in the folder list, with the focus there. {K(AxFileKeys.Delete)} takes it out of the favorites, and the folder itself stays. {K(AxFileKeys.Information)} says its full path and place, "C:\Music, 2 of 3".
        """ + BundleHelp.FilterAndSortSection("Name, Date or Size, folders first") + BundleHelp.NoticesSection("axFile", "the lists");

    /// <summary>docs/axFile/user-guide.md, embedded by AxFile.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axFile/user-guide.md");
}
