using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxDown;

/// <summary>The texts axDown shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AD-5): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axDown's own, what the edit control does itself. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axDown",
        [],
        altF4: "close axDown (asks first when there are unsaved changes)",
        navigateMenu: true) + $$"""
        ## axDown
          The Go to line field also says where you are, like "Go to line (now line 12, column 4)".
          The Navigation menu's Next and Previous keys move by the Markdown of the text: a heading is a line starting with #, and it is said as "Install, heading level 2"; a heading level key goes to a heading of that level only. All of them work in the user guide and the other notices too.

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right move as in any text.
          Page Up and Page Down move one screen, or to the first or the last line.
          {{BundleKeys.Text(BundleKeys.ParagraphUp)}} and {{BundleKeys.Text(BundleKeys.ParagraphDown)}} go to the previous or the next paragraph, the first line after a blank line.
          Shift with any of them selects.
          Undo goes back as far as the file was opened here. {{BundleKeys.Text(EditorKeys.RedoAlso)}} redoes too.
          Enter makes a new line, and Tab types a tab character.

        ## Format (Actions, Format)
          Bold acts on the selection, or on the word the caret is in; again, it takes the bold off. It says "Bold on" or "Bold off".
          Heading deeper adds one number sign: a plain line becomes a heading of level 1, and it says "Heading 2" and the like.
          Heading shallower takes one away: a heading of level 1 becomes a plain line, said "No heading".
          The menu has, without a key: Heading 1 to Heading 6, Italic, Code, List item and Block quote. Each is a toggle: chosen again on the same text, it takes the format away.
          List item and Block quote act on every line of a selection.

        ## Templates (Actions, Insert template)
          Link puts square brackets around the selected words and round brackets after, with the caret between them for the address. Table makes two columns and two rows. Code block puts a fenced block around the selection.
          Every template of axTemplate checked for axDown or for every axKit app is one more; Options, General, Templates in axTemplate opens axTemplate, where you write them. In a template {caret} is where the caret lands, {selection} the text you had selected, {clipboard} the clipboard's text.
          They have no key: use the menu, or the command palette with "insert template" or the template's name. It says "Link inserted", and one Undo takes it back.

        ## Navigation mode
          {{BundleKeys.Text(BundleKeys.ToggleMode)}} says "Navigation mode", and again "Edit mode". The status bar shows "navigation mode" after the file's name. Every window starts in edit mode.
          The letters of the Navigation menu's Ctrl keys work without Ctrl: h and Shift+H heading, 1 to 6 and Shift with them a heading of that level, k link, l list, i list item, q block quote, t table, each the next one and with Shift the previous one.
          The arrows, Home, End, Page Up, Page Down and the paragraph keys move, and with Shift select, as in edit mode. Copy, Select all and the Ctrl jumps work too.
          Nothing changes the text: a letter that is no jump, a digit, Space, Enter, Tab, Backspace, Delete, Paste, Cut, Undo and Redo play the edge sound, and from the menus they also say "Navigation mode: press {{BundleKeys.Text(BundleKeys.ToggleMode)}} to edit".
        """ + BundleHelp.NoticesSection("axDown", "the text", navigateMenu: true);

    /// <summary>docs/axDown/user-guide.md, embedded in the executable at build time.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axDown/user-guide.md");
}
