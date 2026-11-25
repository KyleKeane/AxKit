using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxMedia;

/// <summary>The texts axMedia shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AM-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axMedia's own. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axMedia", []) + $"""
        ## The queue
          Up, Down, Home, End, Page Up and Page Down move through the files; type the start of a name to jump to it, Shift with a letter backwards. The one playing reads "(playing)" after its name.
          Right, Left and Backspace play the edge sound: a queue has no list to go into and no level above.
          Enter plays this file, and the rest of the queue follows in order.
          Delete takes this file out of the queue; the one playing goes on to the next. With files selected, it takes all of them: "Removed 3 files".
          A selected file reads ", selected". Escape deselects all.
          {BundleKeys.Text(AxMediaKeys.Information)} says the file's place, "3 of 12", its position while it plays, and how many are selected.
          Open files adds files with the Windows file picker, several at once, and Open folder adds a folder's sound and video files. Session, Open recent lists the files and folders added lately by number.
          Session, Clear queue empties the queue and stops the sound.
          Session, Show current file in, axFile shows the file in its folder in axFile.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here, and axMedia says so.

        ## Playing
          {BundleKeys.Text(BundleKeys.MainAction)}, from anywhere in the window, plays the file you are on when nothing plays, pauses, and resumes.
          Go to time takes 12:40, 1:02:10 or seconds.
          Faster and Slower make it faster or slower by a quarter, the voice's pitch kept; Actions, Normal speed goes back to 1.
          What is playing says what is playing, where it is, and the speed.
          Options, Say each file as it starts says the next file's name when the queue moves on, the one thing axMedia says on its own.
          The volume is Windows' own: your keyboard's volume keys.
        """ + BundleHelp.FilterAndSortSection("Name puts the queue itself in the order of the names, so that the files play in that order; The order added puts it back") + BundleHelp.NoticesSection("axMedia", "the list");

    /// <summary>docs/axMedia/user-guide.md, embedded by AxMedia.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axMedia/user-guide.md");
}
