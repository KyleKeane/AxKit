using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxPass;

/// <summary>The texts axPass shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axPass",
        [],
        altF4: "close axPass, which locks it") + $"""
        ## The navigator, on the left
          Up, Down, Home and End move, and the edge sound plays at an end. Type the start of a name to jump to it; Shift with a letter goes backwards.
          Right goes into a group; on an entry it does nothing and plays the edge sound. Left or Backspace goes up a level.
          A database has no sections, so the keys for the next and the previous section play the edge sound.
          Enter opens: a group shows what is in it, and an entry moves to its fields. On a locked window, Enter unlocks.
          F2 renames the entry in place, in a box over the line: Enter keeps the new title, Escape the old. Delete moves it to the Recycle Bin, and Undo brings it back.
          Only entries are selected, never a group or what is in the Recycle Bin; a selected entry reads ", selected". Escape deselects all. Delete then moves all the selected entries to the Recycle Bin, and one Undo brings them all back.
          {BundleKeys.Text(AxPassKeys.MoveEntry)} moves the entry to another group, chosen from a list.
          {BundleKeys.Text(ItemKeys.ReadAll)} reads every field of the entry; the password is never read.

        ## The detail pane, on the right
          It shows the fields of the entry you are on in the navigator: Title, UserName, Password (read as hidden), URL and Notes.
          Up and Down move through the fields.
          Enter changes the field. On Password, type a new one twice, or Generate (Alt+G) makes one.
          Right and Left change nothing here and play the edge sound: an entry has no choice and no check, and the user name and the password never change on an arrow.
          Escape, Tab or F6 goes back to the navigator.

        ## Everywhere in axPass
          {BundleKeys.Text(AxPassKeys.CopyUserName)} copies the user name.
          {BundleKeys.Text(AxPassKeys.CopyPassword)} copies the password, and the clipboard clears after 30 seconds.
          {BundleKeys.Text(AxPassKeys.OpenUrl)} opens the URL in the browser.
          Lock forgets the master password until you type it again.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here: every change is written to the database at once, by KeePassXC's tool.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axPass", "the panes");

    /// <summary>docs/axPass/user-guide.md, embedded by AxPass.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axPass/user-guide.md");
}
