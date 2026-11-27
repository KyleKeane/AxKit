using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxTemplate;

/// <summary>The texts axTemplate shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The browser's F1 text (SPEC.md TP-7.1): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then the browser's own, then the bundle's notices. Every key with its command follows from the window's own list (AX-7.2a), so this part says only what a key does in context.</summary>
    public static readonly string Shortcuts = BundleHelp.Section("axTemplate", []) + $$"""
        ## The navigator, on the left
          Up, Down, Home and End move through the templates, and the edge sound plays at an end. Type the start of a name to jump to it; Shift with a letter goes backwards.
          Enter, Tab or F6 moves to the template's fields.
          New template asks for its name in a notice, and the template is made with an empty text.
          F2 renames the template, and its file is renamed with it.
          Delete moves the template, or the selected ones, into the Archive folder, after a question.
          {{BundleKeys.Text(BundleKeys.MainAction)}} copies the template's text, its marks filled with nothing, so that you can see what it makes.
          {{BundleKeys.Text(AxTemplateKeys.OpenInJson)}} opens the template's file in axJSON.
          A selected template reads ", selected". Escape deselects all.

        ## The fields, on the right
          Title is the template's name, and Text is what is put in. Then comes a check for each place inside axKit the template also shows in: Every axKit app, then one for each app.
          In the text, {caret} is where the caret is left, {selection} is the text that was selected, and {clipboard} is the clipboard's text.
          Up and Down move through the fields.
          Enter changes the field; the Text opens as a field of several lines, where Enter is a new line and {{BundleKeys.Text(BundleKeys.MainAction)}} keeps the text. Space turns a check on or off.
          On a check, Right checks and Left unchecks; the edge sound plays on any other field.
          Escape, Tab or F6 goes back to the navigator.

        ## Everywhere in axTemplate
          Undo takes back the last change: a field, a rename, a new template or a deletion.
          {{BundleKeys.Text(BundleKeys.Save)}} has nothing to save here: a template is kept as you change it.
        """ + BundleHelp.FilterAndSortSection() + BundleHelp.NoticesSection("axTemplate", "the panes");

    /// <summary>The launcher's F1 text (SPEC.md TP-7.1), as axClip's: the bundle's section with what Alt+F4 does here, then what the launcher's keys do in context; its keys with their commands follow from its own list (AX-7.2a).</summary>
    public static readonly string LauncherShortcuts = BundleHelp.Section(
        "axTemplate",
        [],
        altF4: "hide the launcher (Quit axTemplate, in its first menu, ends it)") + """
        ## The launcher
          Ctrl+Shift+Semicolon, or the hotkey you chose, shows the launcher from any program; pressed again, it hides the launcher.
          Typing lists only the templates that hold every word typed, in their name or in their text; names come first.
          Enter puts the template into the program you were in; the caret is left at {caret}, and the text you had copied is on the clipboard again afterwards; a picture, files, or a password copied as private are not put back.
          Ctrl+Enter does the same around what is selected in that program, which is copied there first; not in a terminal, where Ctrl+C ends the running command.
          Open the browser shows the window where templates are made and changed.
        """ + BundleHelp.NoticesSection("axTemplate", "the list");

    /// <summary>docs/axTemplate/user-guide.md, embedded by AxTemplate.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axTemplate/user-guide.md");
}
