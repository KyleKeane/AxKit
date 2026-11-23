using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Actions, Insert template (docs/axTemplate/SPEC.md TP-5; from the editor shell on its second use, the conversation
/// windows, 2026-11-23): the window's own templates, then the user's, axTemplate's templates checked for this app or
/// for every axKit app (<see cref="TemplateStore"/>), each a command, so that the command palette lists them all. The
/// user's are read from their folder off the UI thread as the window comes up and each time it is active again, and
/// listed from what that read gave; a template's file is read again when it is chosen. Where it is put in is the
/// window's: the editor shell puts it into its text as one undo step, a window with a field to write in names the
/// field (<see cref="TemplateField"/>) and the base puts it in there. The fields of notices are reached by axTemplate's
/// launcher instead (Ctrl+Shift+Semicolon), since the palette never opens over a notice.
/// </summary>
public abstract partial class AxKitForm
{
    /// <summary>The user's templates for this app as the last read gave them, in the order of their names.</summary>
    private IReadOnlyList<Template> _userTemplates = [];

    /// <summary>The latest read of the templates: an earlier one that ends later is dropped.</summary>
    private int _templateReads;

    /// <summary>
    /// The field a template goes into, in a window that has one to write in and no text of its own (the conversation
    /// windows' input field: axClaude's message, axTerminal's command, axCalc's expression): the bar's Actions menu
    /// then holds Insert template (<see cref="PlaceMenuBar"/>). Null, the default, for none; the editor shell makes
    /// its own menu.
    /// </summary>
    protected virtual AxKitTextBox? TemplateField => null;

    /// <summary>
    /// Actions, Insert template: <paramref name="own"/>, the window's own templates, then the user's, made again as
    /// the menu opens or the palette reads it. <paramref name="insert"/> puts a template in, given its name and its text.
    /// </summary>
    protected ToolStripMenuItem CreateInsertTemplateMenu(IReadOnlyList<EditorTemplate> own, Action<string, string> insert)
    {
        var menu = new ToolStripMenuItem("Insert &template");
        foreach (var template in own)
        {
            Add(menu, new Command(template.Label, Keys.None, () => insert(MenuText.Plain(template.Label), template.Text)) { Mark = CommandMark.Change });
        }

        var kept = menu.DropDownItems.Count;
        CommandPalette.RefreshedOnOpening(menu, () => FillUserTemplates(menu, kept, insert));
        FillUserTemplates(menu, kept, insert);
        Activated += (_, _) => ReadUserTemplates(menu, kept, insert);
        ReadUserTemplates(menu, kept, insert);
        return menu;
    }

    /// <summary>The templates' folder read off the UI thread, then the menu filled again from what it gave.</summary>
    private void ReadUserTemplates(ToolStripMenuItem menu, int kept, Action<string, string> insert) => Do(async () =>
    {
        var read = ++_templateReads;
        var app = AppName;
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
        FillUserTemplates(menu, kept, insert);
    });

    /// <summary>The user's templates after the window's own, numbered as the recent files are; one line that says so when there are none.</summary>
    private void FillUserTemplates(ToolStripMenuItem menu, int kept, Action<string, string> insert)
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
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, template.Title), null, (_, _) => InsertUserTemplate(template, insert)), CommandMark.Change));
        }

        if (_userTemplates.Count == 0)
        {
            // Says only that there are none: no entry for the palette or the AI tools, which would run nothing.
            menu.DropDownItems.Add(CommandPalette.NoEntry(new ToolStripMenuItem($"&No templates of your own for {AppName} yet")));
        }
    }

    /// <summary>A template of the user's own: its file read again off the UI thread, so that a change made in axTemplate a moment ago is in, then put in.</summary>
    private void InsertUserTemplate(Template template, Action<string, string> insert) => Do(async () =>
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

        insert(template.Title, text);
    });

    /// <summary>
    /// A template put into <see cref="TemplateField"/> at its caret, in the place of its selection, with its marks
    /// filled (<see cref="TextTemplate"/>), the caret where the template says, the focus in the field, and "Thanks
    /// inserted" said. Never sent: the user reads it and sends it (TP-5.4). A field that is not there now (a question
    /// stands in its place) says so and nothing is put in.
    /// </summary>
    private void InsertIntoTemplateField(string name, string template)
    {
        if (TemplateField is not { } field || !field.Visible || !field.Enabled || field.ReadOnly)
        {
            Announce("No field to put a template into now", true);
            return;
        }

        var clipboard = template.Contains(TextTemplate.ClipboardMark, StringComparison.Ordinal) ? TemplateClipboardText() : string.Empty;
        var made = TextTemplate.Expand(template, field.SelectedText, clipboard);
        // The field's own line endings; the caret's place counted in them.
        var text = made.Text.ReplaceLineEndings("\r\n");
        var caret = made.Text[..made.Caret].ReplaceLineEndings("\r\n").Length;
        var start = field.SelectionStart;
        field.Focus();
        field.SelectedText = text;
        field.Select(Math.Min(start + caret, field.TextLength), 0);
        Announce($"{name} inserted", true);
    }

    /// <summary>The clipboard's text, empty when it holds none or another program holds the clipboard at this moment.</summary>
    protected static string TemplateClipboardText()
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
}
