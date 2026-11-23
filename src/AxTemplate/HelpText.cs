using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxTemplate;

/// <summary>The texts axTemplate shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The browser's F1 text (SPEC.md TP-7.1): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then the browser's own, then the bundle's notices.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axTemplate",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find a template in the navigator. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: next or previous match",
        ]) + $$"""
        ## The navigator, on the left
          Up, Down, Home, End: move through the templates; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          Enter, Tab, {{BundleKeys.Text(BundleKeys.NextControl)}} or F6: to the template's fields
          {{BundleKeys.Text(ItemKeys.New)}}: a new template; a notice asks its name, and it is made with an empty text
          F2: rename the template; its file is renamed with it
          Delete: delete the template, or the selected ones, after a question
          {{BundleKeys.Text(BundleKeys.MainAction)}}: copy the template's text, its marks filled with nothing, to see what it makes
          {{BundleKeys.Text(AxTemplateKeys.OpenInJson)}}: the template's file in axJSON
          Space: select the template, or deselect it again; a selected template reads ", selected". {{BundleKeys.Text(SelectionKeys.SelectDown)}}, {{BundleKeys.Text(SelectionKeys.SelectUp)}}, {{BundleKeys.Text(SelectionKeys.SelectToStart)}}, {{BundleKeys.Text(SelectionKeys.SelectToEnd)}}: select as you move. {{BundleKeys.Text(SelectionKeys.SelectTo)}}: select from the nearest selected template to here. {{BundleKeys.Text(SelectionKeys.SelectAll)}}: all. {{BundleKeys.Text(SelectionKeys.SelectNone)}} or Escape: none
          {{BundleKeys.Text(ItemKeys.ReadAll)}}: read every field of the template
          {{BundleKeys.Text(ItemKeys.ItemMenu)}}, the Applications key or {{BundleKeys.Text(BundleKeys.ContextMenu)}}: the menu of the template

        ## The fields, on the right
          Title: the template's name. Text: what is put in. Then a check for each place inside axKit the template also shows in: Every axKit app, then one for each app
          In the text, {caret} is where the caret is left, {selection} is the text that was selected, and {clipboard} is the clipboard's text
          Up, Down: move through the fields
          Enter: change the field; the Text opens as a field of several lines, where Enter is a new line and {{BundleKeys.Text(BundleKeys.MainAction)}} keeps the text. Space: a check on or off
          Right and Left: on a check, Right checks and Left unchecks; the edge sound on any other field
          {{BundleKeys.Text(ItemKeys.ReadAll)}}: read the field's whole value
          Escape, Tab, {{BundleKeys.Text(ItemKeys.OtherPaneBack)}}, {{BundleKeys.Text(BundleKeys.NextControl)}}, {{BundleKeys.Text(BundleKeys.PreviousControl)}} or F6: back to the navigator

        ## Everywhere in axTemplate
          {{BundleKeys.Text(ItemKeys.Undo)}}: undo the last change: a field, a rename, a new template, a deletion
          F5: read the templates again
          {{BundleKeys.Text(BundleKeys.Save)}}: nothing to save here; a template is kept as you change it
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axTemplate", "the panes");

    /// <summary>The launcher's F1 text (SPEC.md TP-7.1), as axClip's: the bundle's section with what Alt+F4 does here, then the launcher's own keys.</summary>
    public static readonly string LauncherShortcuts = BundleHelp.Section(
        "axTemplate",
        [
            "Ctrl+F, F3 and Shift+F3: find in a notice's text",
        ],
        altF4: "hide the launcher (Quit axTemplate, in its first menu, ends it)") + """
        ## The launcher
          Ctrl+Shift+Semicolon, or the hotkey you chose: show the launcher from anywhere; again: hide it
          Type: only the templates that hold every word typed, in their name or in their text; names first
          Down, Up: the next or the previous template. Ctrl+Home, Ctrl+End: the first or the last. Page Down, Page Up: a screen
          Enter: put the template into the program you were in; the caret is left at {caret}, and what you had copied is on the clipboard again afterwards
          Ctrl+Enter: the same, around what is selected in that program, which is copied there first; not in a terminal, where Ctrl+C ends the running command
          Shift+Enter: put the template's text on the clipboard without putting it in
          Ctrl+Q: read the template's text
          Ctrl+B: open the browser, where templates are made and changed
          Escape: hide the launcher
        """ + BundleHelp.NoticesSection("axTemplate", "the list");

    /// <summary>docs/axTemplate/user-guide.md, embedded by AxTemplate.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTemplate/user-guide.md");
}
