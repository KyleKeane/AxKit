using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The editor shell, the templates (docs/axDown/SPEC.md AD-13, 2026-11-23): Actions, Insert template holds the
/// app's own templates and the user's, each a command, so that the command palette lists them all. The user's are
/// axTemplate's templates checked for this app or for every axKit app (docs/axTemplate/SPEC.md TP-5.1, step 4),
/// read from their folder off the UI thread (<see cref="TemplateStore"/>) as the window comes up and each time it is
/// active again, and listed from what that read gave. A template is put in at the caret in the place of the
/// selection, as one undo step, with its marks filled (<see cref="TextTemplate"/>); in navigation mode nothing is
/// put in. Options, General, Templates opens axTemplate's browser, where templates are made and changed.
/// </summary>
public sealed partial class TextEditorForm
{
    /// <summary>The user's templates for this app as the last read gave them, in the order of their names.</summary>
    private IReadOnlyList<Template> _userTemplates = [];

    /// <summary>The latest read of the templates: an earlier one that ends later is dropped.</summary>
    private int _templateReads;

    /// <summary>Actions, Insert template: the app's own templates, then the user's, made again as the menu opens or the palette reads it.</summary>
    private ToolStripMenuItem CreateTemplatesMenu(IReadOnlyList<EditorTemplate> own)
    {
        var menu = new ToolStripMenuItem("Insert &template");
        foreach (var template in own)
        {
            Add(menu, new Command(template.Label, Keys.None, () => InsertTemplate(MenuText.Plain(template.Label), template.Text)) { Mark = CommandMark.Change });
        }

        var kept = menu.DropDownItems.Count;
        CommandPalette.RefreshedOnOpening(menu, () => FillUserTemplates(menu, kept));
        FillUserTemplates(menu, kept);
        Activated += (_, _) => ReadUserTemplates(menu, kept);
        ReadUserTemplates(menu, kept);
        return menu;
    }

    /// <summary>The templates' folder read off the UI thread, then the menu filled again from what it gave.</summary>
    private void ReadUserTemplates(ToolStripMenuItem menu, int kept) => Do(async () =>
    {
        var read = ++_templateReads;
        var app = _app.Name;
        IReadOnlyList<Template> templates;
        try
        {
            templates = await Task.Run(() => TemplateStore.Read(AppPaths.TemplatesRoot).Templates.Where(template => template.ShowsInApp(app)).ToList());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // TP-7.3: that it failed, never a name.
            Log.Error("The templates' folder could not be read: " + ex.GetType().Name);
            return;
        }

        if (read != _templateReads || IsDisposed)
        {
            return;
        }

        _userTemplates = templates;
        FillUserTemplates(menu, kept);
    });

    /// <summary>The user's templates after the app's own, numbered as the recent files are; one line that says so when there are none.</summary>
    private void FillUserTemplates(ToolStripMenuItem menu, int kept)
    {
        while (menu.DropDownItems.Count > kept)
        {
            menu.DropDownItems.RemoveAt(kept);
        }

        if (kept > 0)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
        }

        for (var i = 0; i < _userTemplates.Count; i++)
        {
            var template = _userTemplates[i];
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, template.Title), null, (_, _) => InsertUserTemplate(template)), CommandMark.Change));
        }

        if (_userTemplates.Count == 0)
        {
            // Says only that there are none: no entry for the palette or the AI tools, which would run nothing.
            menu.DropDownItems.Add(CommandPalette.NoEntry(new ToolStripMenuItem($"&No templates of your own for {_app.Name} yet")));
        }
    }

    /// <summary>A template of the user's own: its file read again off the UI thread, so that a change made in axTemplate a moment ago is in, then put in.</summary>
    private void InsertUserTemplate(Template template) => Do(async () =>
    {
        var text = template.Text;
        try
        {
            text = await Task.Run(() =>
                Changes.ReadOrNull(template.Path) is { } now && TemplateStore.Parse(template.Path, now) is { } fresh ? fresh.Text : template.Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The text as the folder's read gave it.
        }

        InsertTemplate(template.Title, text);
    });

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

        var clipboard = template.Contains(TextTemplate.ClipboardMark, StringComparison.Ordinal) ? ClipboardText() : string.Empty;
        var made = TextTemplate.Expand(template, _editor.SelectedText, clipboard);
        var start = _editor.SelectionStart;
        ApplyEdit(new TextEdit(start, _editor.SelectionLength, made.Text, start + made.Caret, $"{name} inserted"));
    }

    /// <summary>The clipboard's text, empty when it holds none or another program holds the clipboard at this moment.</summary>
    private static string ClipboardText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            return string.Empty;
        }
    }

    /// <summary>Options, General, Templates: axTemplate's browser, where templates are made, changed and checked for this app.</summary>
    private void OpenTemplates() => WindowMenu.Start(Dispatch.AxTemplate, null);
}
