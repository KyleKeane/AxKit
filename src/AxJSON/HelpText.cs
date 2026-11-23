using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxJSON;

/// <summary>The texts axJSON shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AJ-2.2, AJ-2.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, the editor shell's and axJSON's own, what the edit control does itself. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axJSON",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find text. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
            $"{BundleKeys.Text(BundleKeys.Save)}: save the file",
        ],
        altF4: "close axJSON (asks first when there are unsaved changes)") + $$"""
        ## axJSON
          {{BundleKeys.Text(EditorKeys.NewDocument)}}: new, empty document
          {{BundleKeys.Text(EditorKeys.NewWindow)}}: a new, empty document in a new axJSON window; this one stays as it is
          {{BundleKeys.Text(EditorKeys.Open)}}: open a file
          {{BundleKeys.Text(EditorKeys.SaveAs)}}: save under a new name
          {{BundleKeys.Text(EditorKeys.ShowInExplorer)}}: show the current file in File Explorer
          {{BundleKeys.Text(EditorKeys.GoToLine)}}: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          {{BundleKeys.Text(AxJSONKeys.NextKey)}} and {{BundleKeys.Text(AxJSONKeys.PreviousKey)}}: next and previous key, spoken with its value ("model: opus") or its size ("permissions, object with 2 keys")
          {{BundleKeys.Text(AxJSONKeys.NextObject)}} and {{BundleKeys.Text(AxJSONKeys.PreviousObject)}}: next and previous object, spoken with its key and its number of keys
          {{BundleKeys.Text(AxJSONKeys.NextArray)}} and {{BundleKeys.Text(AxJSONKeys.PreviousArray)}}: next and previous array, spoken with its key and its number of items
          {{BundleKeys.Text(AxJSONKeys.NextItem)}} and {{BundleKeys.Text(AxJSONKeys.PreviousItem)}}: next and previous item of an array, "Item 2 of 5, Read"
          {{BundleKeys.Text(AxJSONKeys.NextError)}} and {{BundleKeys.Text(AxJSONKeys.PreviousError)}}: the place where the file stops being JSON, with the line and what was expected there; "No next error" means the file is valid
          {{BundleKeys.Text(AxJSONKeys.Format)}}: format: two spaces per level, one key per line; a trailing comma, a missing comma, single quotes, an unquoted key and a comment are repaired on the way and named
          Alt with an arrow: choosing instead of typing, in a list's configuration of axTask (list.json) and in an item of a list
          {{BundleKeys.Text(AxJSONKeys.ChooseValue)}}: the values the member at the caret can take, as a list; Up and Down move in it, Enter writes the one chosen, Escape leaves. "type", "does", "key", a choice's options, true or false
          {{BundleKeys.Text(AxJSONKeys.NextValue)}} and {{BundleKeys.Text(AxJSONKeys.PreviousValue)}}: the next and the previous value, written in line and said with its place, "type: date, 4 of 8"
          {{BundleKeys.Text(AxJSONKeys.AddMember)}}: add a member: the members the object at the caret can have and does not have yet, each with what it is; Enter adds the one chosen with a first value and puts the caret on it
          Actions, Insert template: every template of axTemplate checked for axJSON or for every axKit app, put in at the caret; Options, General, Templates in axTemplate opens axTemplate, where you write them. In a template {caret} is where the caret lands, {selection} the text you had selected, {clipboard} the clipboard's text
          {{BundleKeys.Text(EditorKeys.WordWrap)}}: word wrap on or off

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          {{BundleKeys.Text(BundleKeys.ParagraphUp)}}, {{BundleKeys.Text(BundleKeys.ParagraphDown)}}: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C, Ctrl+X, Ctrl+V: copy, cut, paste
          {{BundleKeys.Text(EditorKeys.Undo)}}: undo the last change, as far back as the file was opened here. {{BundleKeys.Text(EditorKeys.Redo)}} or {{BundleKeys.Text(EditorKeys.RedoAlso)}}: redo
          Enter: a new line. Tab: a tab character
        """ + BundleHelp.NoticesSection("axJSON", "the text");

    /// <summary>docs/axJSON/user-guide.md, embedded by AxJSON.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axJSON/user-guide.md");
}
