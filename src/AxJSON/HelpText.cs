using System.Reflection;
using AxKit.Core;

namespace AxJSON;

/// <summary>The texts axJSON shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AJ-2.2, AJ-2.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the editor shell's and axJSON's own, what the edit control does itself. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axJSON",
        [
            "Ctrl+F: find text. F3 and Shift+F3: next or previous match",
            "Ctrl+S: save the file",
        ],
        altF4: "close axJSON (asks first when there are unsaved changes)") + """
        ## axJSON
          Ctrl+N: new, empty document
          Ctrl+Shift+N: a new, empty document in a new axJSON window; this one stays as it is
          Ctrl+O: open a file
          Ctrl+Shift+S: save under a new name
          Ctrl+W: show the current file in File Explorer
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          Ctrl+K and Ctrl+Shift+K: next and previous key, spoken with its value ("model: opus") or its size ("permissions, object with 2 keys")
          Ctrl+J and Ctrl+Shift+J: next and previous object, spoken with its key and its number of keys
          Ctrl+L and Ctrl+Shift+L: next and previous array, spoken with its key and its number of items
          Ctrl+I and Ctrl+Shift+I: next and previous item of an array, "Item 2 of 5, Read"
          Ctrl+E and Ctrl+Shift+E: the place where the file stops being JSON, with the line and what was expected there; "No next error" means the file is valid
          Ctrl+Shift+F: format: two spaces per level, one key per line; a trailing comma, a missing comma, single quotes, an unquoted key and a comment are repaired on the way and named
          Ctrl+Shift+W: word wrap on or off

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C, Ctrl+X, Ctrl+V: copy, cut, paste
          Ctrl+Z: undo the last change, as far back as the file was opened here. Ctrl+Y or Ctrl+Shift+Z: redo
          Enter: a new line. Tab: a tab character
        """ + BundleHelp.NoticesSection("axJSON", "the text");

    /// <summary>docs/axJSON/user-guide.md, embedded by AxJSON.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axJSON/user-guide.md");
}
