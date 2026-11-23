using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The editor shell, the templates (docs/axDown/SPEC.md AD-13, 2026-11-23): Actions, Insert template is the window
/// base's menu (<see cref="AxKitForm.CreateInsertTemplateMenu"/>: the app's own templates, then axTemplate's checked
/// for this app or for every axKit app), and the editor puts a template into its text at the caret in the place of
/// the selection, as one undo step, with its marks filled (<see cref="TextTemplate"/>); in navigation mode nothing
/// is put in. Options, General, Templates in axTemplate opens axTemplate's browser, where templates are made.
/// </summary>
public sealed partial class TextEditorForm
{
    /// <summary>Actions, Insert template, with the app's own templates first.</summary>
    private ToolStripMenuItem CreateTemplatesMenu(IReadOnlyList<EditorTemplate> own) => CreateInsertTemplateMenu(own, InsertTemplate);

    /// <summary>
    /// The template put in at the caret in the place of the selection, its marks filled, as one undo step, the
    /// caret where the template says, and "Link inserted" said. The clipboard is read only for a template that
    /// names it. Nothing in navigation mode, as every command that edits.
    /// </summary>
    private void InsertTemplate(string name, string template)
    {
        if (RefusedInNavigation(_editor))
        {
            return;
        }

        var clipboard = template.Contains(TextTemplate.ClipboardMark, StringComparison.Ordinal) ? TemplateClipboardText() : string.Empty;
        var made = TextTemplate.Expand(template, _editor.SelectedText, clipboard);
        var start = _editor.SelectionStart;
        ApplyEdit(new TextEdit(start, _editor.SelectionLength, made.Text, start + made.Caret, $"{name} inserted"));
    }

    /// <summary>Options, General, Templates in axTemplate: axTemplate's browser, where templates are made, changed and checked for this app.</summary>
    private void OpenTemplates() => WindowMenu.Start(Dispatch.AxTemplate, null);
}
