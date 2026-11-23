using System.Reflection;
using AxKit.Core;

namespace AxDown;

/// <summary>The texts axDown shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AD-5): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axDown's own, what the edit control does itself. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axDown",
        [
            "Ctrl+F: find text. F3 and Shift+F3: next or previous match",
            "Ctrl+S: save the file",
        ],
        altF4: "close axDown (asks first when there are unsaved changes)",
        navigateMenu: true) + """
        ## axDown
          Ctrl+N: new, empty document
          Ctrl+Shift+N: a new, empty document in a new axDown window; this one stays as it is
          Ctrl+O: open a file
          Ctrl+Shift+S: save under a new name
          Ctrl+W: show the current file in File Explorer
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          Ctrl+H and Ctrl+Shift+H: next and previous Markdown heading (a line starting with #), spoken as "Install, heading level 2"
          Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+4, Ctrl+5, Ctrl+6: the next heading of that level only; Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3, Ctrl+Shift+4, Ctrl+Shift+5, Ctrl+Shift+6: the previous one
          Ctrl+K and Ctrl+Shift+K: next and previous link
          Ctrl+L and Ctrl+Shift+L: next and previous list
          Ctrl+I and Ctrl+Shift+I: next and previous list item
          Ctrl+Q and Ctrl+Shift+Q: next and previous block quote
          Ctrl+T and Ctrl+Shift+T: next and previous table
            All of them work in the user guide and the other notices too
          Ctrl+Shift+W: word wrap on or off

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C, Ctrl+X, Ctrl+V: copy, cut, paste
          Ctrl+Z: undo the last change, as far back as the file was opened here. Ctrl+Y or Ctrl+Shift+Z: redo
          Enter: a new line. Tab: a tab character

        ## Format (Actions, Format)
          Ctrl+B: bold on the selection, or on the word the caret is in; again takes it off. Said "Bold on", "Bold off"
          Shift+Alt+Right: heading deeper, one number sign more: a plain line becomes a heading of level 1. Said "Heading 2"
          Shift+Alt+Left: heading shallower, one number sign fewer: a heading of level 1 becomes a plain line, said "No heading"
          In the menu, without a key: Heading 1 to Heading 6, Italic, Code, List item, Block quote. Each is a toggle: chosen again on the same text, it takes the format away
          List item and Block quote act on every line of a selection

        ## Templates (Actions, Insert template)
          Link: square brackets around the selected words, round brackets after, the caret between them for the address. Table: two columns, two rows. Code block: a fenced block around the selection
          Your own: every template of axTemplate checked for axDown or for every axKit app is one more; Options, General, Templates in axTemplate opens axTemplate, where you write them. In a template {caret} is where the caret lands, {selection} the text you had selected, {clipboard} the clipboard's text
          No key: the menu, or the command palette, Ctrl+Shift+P, with "insert template" or the template's name. Said "Link inserted"; one Ctrl+Z takes it back

        ## Navigation mode
          Ctrl+Space: navigation mode, said "Navigation mode"; again: edit mode, said "Edit mode". The status bar shows "navigation mode" after the file's name. Every window starts in edit mode
          The letters of the Ctrl keys above, without Ctrl: h and Shift+H heading, 1 to 6 and Shift with them a heading of that level, k link, l list, i list item, q block quote, t table, each the next one and with Shift the previous one
          The arrows, Home, End, Page Up, Page Down and the paragraph keys move, and with Shift select, as in edit mode. Ctrl+C copies, Ctrl+A selects all, and the Ctrl jumps above work too
          Nothing changes the text: a letter that is no jump, a digit, Space, Enter, Tab, Backspace, Delete, Ctrl+V, Ctrl+X, Ctrl+Z and Ctrl+Y play the edge sound; Undo, Redo, Cut and Paste from the menus play it too and say "Navigation mode: press Ctrl+Space to edit"
        """ + BundleHelp.NoticesSection("axDown", "the text", navigateMenu: true);

    /// <summary>docs/axDown/user-guide.md, embedded in the executable at build time.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axDown/user-guide.md");
}
