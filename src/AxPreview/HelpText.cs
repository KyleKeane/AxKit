using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxPreview;

/// <summary>The texts axPreview shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AP-3.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axPreview's own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axPreview",
        [
            $"Tab, {BundleKeys.Text(BundleKeys.NextControl)} or F6: between the file's text and the Details list under it",
            $"{BundleKeys.Text(BundleKeys.Find)}: find text. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ],
        navigateMenu: true) + $"""
        ## axPreview
          {BundleKeys.Text(EditorKeys.Open)}: open any file to read. {BundleKeys.Text(EditorKeys.NewDocument)}: a new axPreview window
          F5: read the file again; coming back to the window after another program changed it does the same by itself
          {BundleKeys.Text(EditorKeys.ClipboardPicture)}: read the picture on the clipboard: it is saved beside axPreview's logs and its text read by Windows' own reader. Alt+PrintScreen puts the window in front on the clipboard, PrintScreen the whole screen
          {BundleKeys.Text(EditorKeys.Edit)}: go on with the file: a text in its editor, axDown, or axJSON for a .json file; a sound or video in axMedia; anything else in the program Windows has for it
          Session, Open with: the axKit apps for the file, Windows' program, or Windows' own list of programs, whose Always makes one the default
          {BundleKeys.Text(EditorKeys.ShowInExplorer)}: show the file in File Explorer
          {BundleKeys.Text(EditorKeys.GoToLine)}: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          l: say the line, "Line 12 of 40"
          {BundleKeys.Text(MarkdownKeys.NextHeading)} and {BundleKeys.Text(MarkdownKeys.PreviousHeading)}: next and previous Markdown heading (a line starting with #), spoken as "Install, heading level 2"
          Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+4, Ctrl+5, Ctrl+6: the next heading of that level only; Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3, Ctrl+Shift+4, Ctrl+Shift+5, Ctrl+Shift+6: the previous one
          {BundleKeys.Text(MarkdownKeys.NextLink)} and {BundleKeys.Text(MarkdownKeys.PreviousLink)}: next and previous link
          {BundleKeys.Text(MarkdownKeys.NextList)} and {BundleKeys.Text(MarkdownKeys.PreviousList)}: next and previous list
          {BundleKeys.Text(MarkdownKeys.NextListItem)} and {BundleKeys.Text(MarkdownKeys.PreviousListItem)}: next and previous list item
          {BundleKeys.Text(MarkdownKeys.NextBlockQuote)} and {BundleKeys.Text(MarkdownKeys.PreviousBlockQuote)}: next and previous block quote
          {BundleKeys.Text(MarkdownKeys.NextTable)} and {BundleKeys.Text(MarkdownKeys.PreviousTable)}: next and previous table
            All of them work in the user guide and the other notices too
          {BundleKeys.Text(EditorKeys.WordWrap)}: word wrap on or off
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; axPreview says so

        ## The text
          What any file shows: a text file its text; a Word document, a PowerPoint presentation or an OpenDocument text its text, with its headings and slides as headings; a zip its list of files; a picture the text Windows' own reader finds in it, a second or two after it opens; a sound or video what it is; any other file the readable text inside it
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          {BundleKeys.Text(BundleKeys.ParagraphUp)}, {BundleKeys.Text(BundleKeys.ParagraphDown)}: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C: copy. Typing changes nothing

        ## The Details list
          Everything known about the file, one line each: its name, folder, size, type and dates; what is in it, like the encoding and the number of words, or the files in a zip; and every detail Windows has, like a picture's dimensions or a song's length
          Arrow keys and letters: move through the lines
          Ctrl+C: copy the line's value, without its name
          Enter on the Folder line: the folder in axFile, the file selected
        """ + BundleHelp.NoticesSection("axPreview", "the text", navigateMenu: true);

    /// <summary>docs/axPreview/user-guide.md, embedded by AxPreview.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axPreview/user-guide.md");
}
