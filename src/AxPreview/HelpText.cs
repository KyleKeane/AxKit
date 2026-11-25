using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxPreview;

/// <summary>The texts axPreview shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AP-3.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axPreview's own. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axPreview",
        [],
        navigateMenu: true) + $"""
        ## axPreview
          Reload reads the file again; coming back to the window after another program changed it does the same by itself.
          Read the clipboard's picture saves the picture on the clipboard beside axPreview's logs and has its text read by Windows' own reader. Alt+PrintScreen puts the window in front on the clipboard, PrintScreen the whole screen.
          {BundleKeys.Text(EditorKeys.Edit)} goes on with the file: a text in its editor, axDown, or axJSON for a .json file; a sound or video in axMedia; anything else in the program Windows has for it.
          Session, Open with has the axKit apps for the file, Windows' program, and Windows' own list of programs, whose Always makes one the default.
          The Go to line field also says where you are, like "Go to line (now line 12, column 4)".
          l says the line, "Line 12 of 40".
          The Navigation menu's Next and Previous keys move by the Markdown of the text: a heading is a line starting with #, and it is said as "Install, heading level 2"; a heading level key goes to a heading of that level only. All of them work in the user guide and the other notices too.
          {BundleKeys.Text(BundleKeys.Save)} has nothing to save here, and axPreview says so.

        ## The text
          What any file shows: a text file its text; a Word document, a PowerPoint presentation or an OpenDocument text its text, with its headings and slides as headings; a zip its list of files; a picture the text Windows' own reader finds in it, a second or two after it opens; a sound or video what it is; any other file the readable text inside it.
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right move as in any text.
          Page Up and Page Down move one screen, or to the first or the last line.
          {BundleKeys.Text(BundleKeys.ParagraphUp)} and {BundleKeys.Text(BundleKeys.ParagraphDown)} go to the previous or the next paragraph, the first line after a blank line.
          Shift with any of them selects. Typing changes nothing.

        ## The Details list
          Tab or F6 moves between the file's text and the Details list under it.
          The list has everything known about the file, one line each: its name, folder, size, type and dates; what is in it, like the encoding and the number of words, or the files in a zip; and every detail Windows has, like a picture's dimensions or a song's length.
          Arrow keys and letters move through the lines.
          Copy the detail's value copies it without its name.
          Enter on the Folder line shows the folder in axFile, with the file selected.
        """ + BundleHelp.NoticesSection("axPreview", "the text", navigateMenu: true);

    /// <summary>docs/axPreview/user-guide.md, embedded by AxPreview.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axPreview/user-guide.md");
}
