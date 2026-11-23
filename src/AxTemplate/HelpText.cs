using System.Reflection;
using AxKit.Core;

namespace AxTemplate;

/// <summary>The texts axTemplate shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The browser's F1 text (SPEC.md TP-7.1): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then the browser's own, then the bundle's notices.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axTemplate",
        [
            "Ctrl+F: find a template in the navigator. F3 and Shift+F3: next or previous match",
        ]) + """
        ## The navigator, on the left
          Up, Down, Home, End: move through the templates; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          Enter, Tab, Ctrl+Tab or F6: to the template's fields
          Ctrl+N: a new template; a notice asks its name, and it is made with an empty text
          F2: rename the template; its file is renamed with it
          Delete: delete the template, or the selected ones, after a question
          Ctrl+Enter: copy the template's text, its marks filled with nothing, to see what it makes
          Ctrl+J: the template's file in axJSON
          Space: select the template, or deselect it again; a selected template reads ", selected". Shift+Down, Shift+Up, Shift+Home, Shift+End: select as you move. Shift+Space: select from the nearest selected template to here. Ctrl+A: all. Ctrl+Shift+A or Escape: none
          Ctrl+Q: read every field of the template
          Shift+F10, the Applications key or Ctrl+Period: the menu of the template

        ## The fields, on the right
          Title: the template's name. Text: what is put in. Then a check for each place inside axKit the template also shows in: Every axKit app, then one for each app
          In the text, {caret} is where the caret is left, {selection} is the text that was selected, and {clipboard} is the clipboard's text
          Up, Down: move through the fields
          Enter: change the field; the Text opens as a field of several lines, where Enter is a new line and Ctrl+Enter keeps the text. Space: a check on or off
          Right and Left: on a check, Right checks and Left unchecks; the edge sound on any other field
          Ctrl+Q: read the field's whole value
          Escape, Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6: back to the navigator

        ## Everywhere in axTemplate
          Ctrl+Z: undo the last change: a field, a rename, a new template, a deletion
          F5: read the templates again
          Ctrl+S: nothing to save here; a template is kept as you change it
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axTemplate", "the panes");

    /// <summary>docs/axTemplate/user-guide.md, embedded by AxTemplate.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTemplate/user-guide.md");
}
