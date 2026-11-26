using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxJSON;

/// <summary>The texts axJSON shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AJ-2.2, AJ-2.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the editor shell's and axJSON's own, what the edit control does itself. The first line and the bundle's section come from <c>BundleHelp</c>; every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axJSON",
        [],
        altF4: "close axJSON (asks first when there are unsaved changes)") + $$"""
        ## axJSON
          The Go to line field also says where you are, like "Go to line (now line 12, column 4)".
          The Navigation menu's Next and Previous keys move by the JSON of the text. A key is said with its value, "model: opus", or its size, "permissions, object with 2 keys". An object is said with its key and its number of keys, an array with its key and its number of items, and an item of an array with its place, "Item 2 of 5, Read".
          An error is the place where the file stops being JSON, said with the line and what was expected there; "No next error" means the file is valid.
          Format puts two spaces per level and one key per line; a trailing comma, a missing comma, single quotes, an unquoted key and a comment are repaired on the way and named.
          Alt with an arrow chooses instead of typing, in a list's configuration of axTask (list.json) and in an item of a list.
            Choose a value lists the values the member at the caret can take: "type", "does", "key", a choice's options, true or false. Up and Down move in the list, Enter writes the one chosen, and Escape leaves.
            Next value and Previous value write the value in line and say it with its place, "type: date, 4 of 8".
            Add a member lists the members the object at the caret can have and does not have yet, each with what it is; Enter adds the one chosen with a first value and puts the caret on it.
          Actions, Insert template puts in, at the caret, every template of axTemplate checked for axJSON or for every axKit app; Options, General, Templates in axTemplate opens axTemplate, where you write them. In a template {caret} is where the caret lands, {selection} the text you had selected, {clipboard} the clipboard's text.

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right move as in any text.
          Page Up and Page Down move one screen, or to the first or the last line.
          {{BundleKeys.Text(BundleKeys.ParagraphUp)}} and {{BundleKeys.Text(BundleKeys.ParagraphDown)}} go to the previous or the next paragraph, the first line after a blank line.
          Shift with any of them selects.
          Undo goes back as far as the file was opened here. {{BundleKeys.Text(EditorKeys.RedoAlso)}} redoes too.
          Enter makes a new line, and Tab types a tab character. {{BundleKeys.Text(BundleKeys.MainAction)}} saves, as {{BundleKeys.Text(BundleKeys.Save)}} does.
        """ + BundleHelp.NoticesSection("axJSON", "the text");

    /// <summary>docs/axJSON/user-guide.md, embedded by AxJSON.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axJSON/user-guide.md");
}
