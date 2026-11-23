using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The editor shell, the templates (docs/axDown/SPEC.md AD-13, 2026-11-23): Actions, Insert template holds the
/// app's own templates and the files of the user's templates folder, each a command, so that the command palette
/// lists them all. A template is put in at the caret in the place of the selection, as one undo step, with its
/// marks filled (<see cref="TextTemplate"/>: the caret's place, the selection, the clipboard); in navigation mode
/// nothing is put in. Options, General, Templates folder opens the folder in axFile.
/// </summary>
public sealed partial class TextEditorForm
{
    /// <summary>A template file larger than this is not read: a template is a few lines, and a file put there by mistake is not poured into the text.</summary>
    private const int TemplateFileAtMost = 64 * 1024;

    /// <summary>The folder of this app's templates of the user's own, each file one template.</summary>
    private string TemplatesFolder => AppPaths.TemplatesFolder(_app.Name);

    /// <summary>Actions, Insert template: the app's own templates, then the user's files, made again as the menu opens or the palette reads it.</summary>
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
        return menu;
    }

    /// <summary>The user's files after the app's own templates, numbered as the recent files are; one line that says so when there are none.</summary>
    private void FillUserTemplates(ToolStripMenuItem menu, int kept)
    {
        while (menu.DropDownItems.Count > kept)
        {
            menu.DropDownItems.RemoveAt(kept);
        }

        var files = UserTemplateFiles();
        if (kept > 0)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
        }

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, TextTemplate.NameOf(file)), null, (_, _) => InsertTemplateFile(file)), CommandMark.Change));
        }

        if (files.Count == 0)
        {
            // Says only that there are none: no entry for the palette or the AI tools, which would run nothing.
            menu.DropDownItems.Add(CommandPalette.NoEntry(new ToolStripMenuItem("&No templates of your own yet")));
        }
    }

    /// <summary>The files of the templates folder, in the order of their names; none when the folder is not there or cannot be read.</summary>
    private IReadOnlyList<string> UserTemplateFiles()
    {
        try
        {
            if (!Directory.Exists(TemplatesFolder))
            {
                return [];
            }

            var files = Directory.GetFiles(TemplatesFolder);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The templates folder could not be read: " + ex.Message);
            return [];
        }
    }

    /// <summary>A template of the user's own: its file read off the UI thread, then put in; a file too large or not readable is said and nothing is put in.</summary>
    private void InsertTemplateFile(string path) => Do(async () =>
    {
        var name = TextTemplate.NameOf(path);
        string text;
        try
        {
            var read = await Task.Run(() => new FileInfo(path).Length > TemplateFileAtMost ? null : File.ReadAllText(path));
            if (read is null)
            {
                Announce($"{name} is too large for a template: more than {TemplateFileAtMost / 1024} KB", true);
                return;
            }

            text = read;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Announce($"{name} could not be read: {ex.Message}", true);
            return;
        }

        InsertTemplate(name, text);
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

    /// <summary>Options, General, Templates folder: the folder made when it is not there, and opened in axFile, where a template is a file like any other.</summary>
    private void OpenTemplatesFolder()
    {
        try
        {
            Directory.CreateDirectory(TemplatesFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Announce("The templates folder could not be made: " + ex.Message, true);
            return;
        }

        WindowMenu.Start(Dispatch.AxFile, TemplatesFolder);
    }
}
