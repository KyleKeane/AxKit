using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTemplate.Core;

/// <summary>
/// A template as the windows use it (SPEC.md TP-2.2): its name, the text that is put in, the places inside axKit it
/// also shows in (the titles of its checked fields), and its file.
/// </summary>
public sealed record Template(string Title, string Text, IReadOnlyList<string> ShowsIn, string Path)
{
    /// <summary>Whether the template shows in the command palette of the app of this name: checked for it, or for every axKit app (TP-5.1).</summary>
    public bool ShowsInApp(string app) =>
        ShowsIn.Contains(TemplateStore.EveryAppField, StringComparer.OrdinalIgnoreCase) || ShowsIn.Contains(app, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A file of the templates' folder as the browser holds it: its path, its text as it was read (null when it could
/// not be), and its item (null when the text is none: TP-2.4, "not a template").
/// </summary>
public sealed record TemplateFile(string Path, string? Text, FlatItem? Item)
{
    /// <summary>The template's name: the item's Title, or the file's name where it has none or is no item.</summary>
    public string Title => Item is { Title.Length: > 0 } item ? item.Title : System.IO.Path.GetFileNameWithoutExtension(Path);
}

/// <summary>The folder as the browser holds it: the configuration its items are read through, and its item files in the order of their names.</summary>
public sealed record TemplateList(ListConfig Config, IReadOnlyList<TemplateFile> Files);

/// <summary>What a folder of templates holds: the templates in the order of their names, and the names of the files that are none (TP-2.4).</summary>
public sealed record TemplateFolder(IReadOnlyList<Template> Templates, IReadOnlyList<string> NotTemplates);

/// <summary>
/// The templates' folder as a list of the bundle's item format (SPEC.md TP-2; docs/axKit/item-apps.md IA-4,
/// docs/axTask/forms-framework.md sections 11 and 13): its <c>list.json</c> names the fields of a template, and
/// every other <c>.json</c> file is one template, an item read through it (<see cref="ListConfig"/>,
/// <see cref="FlatItem"/>). A template's fields are its Title, its <see cref="TextField"/>, and one check for each
/// place it also shows inside axKit: <see cref="EveryAppField"/>, then one for each app, by the app's name. Nothing
/// here knows a window; the windows read and write through it.
/// </summary>
public static class TemplateStore
{
    /// <summary>The field that holds what is put in.</summary>
    public const string TextField = "Text";

    /// <summary>The check that shows a template in every axKit app's command palette.</summary>
    public const string EveryAppField = "Every axKit app";

    /// <summary>The path of the folder's configuration.</summary>
    public static string ConfigurationPath(string folder) => Path.Combine(folder, ListConfig.FileName);

    /// <summary>The fields a template has, in the form's order: the text, the check for every app, then one check for each app named.</summary>
    public static IReadOnlyList<ItemField> Fields(IEnumerable<string> apps) =>
    [
        new ItemField(TextField, FieldTypes.LongText, FieldTypes.Empty(FieldTypes.LongText), []),
        new ItemField(EveryAppField, FieldTypes.Check, FieldTypes.Empty(FieldTypes.Check), []),
        .. apps.Select(app => new ItemField(app, FieldTypes.Check, FieldTypes.Empty(FieldTypes.Check), [])),
    ];

    /// <summary>
    /// The folder's configuration as it should be written, or null when the one there has every field already
    /// (TP-2.1): made new when there is none; a field a newer version brings (a new app's check) added after the
    /// ones there; no field ever taken away, so that what the user added in axJSON stays. A configuration that does
    /// not read is left alone: null, and the templates are read as plain items until the user mends it.
    /// </summary>
    public static string? ConfigurationToWrite(string? existing, IEnumerable<string> apps)
    {
        var wanted = Fields(apps);
        if (existing is null)
        {
            return ListConfig.New(wanted).ToText();
        }

        ListConfig config;
        try
        {
            config = ListConfig.Parse(existing);
        }
        catch (InvalidDataException)
        {
            return null;
        }

        var missing = wanted.Where(field => config.IndexOf(field.Title) < 0).ToList();
        return missing.Count == 0 ? null : config.WithItemFields([.. config.ItemFields, .. missing]).ToText();
    }

    /// <summary>The configuration in a text, or the one a folder without a readable file gets: the fields for no app.</summary>
    public static ListConfig Configuration(string? text)
    {
        if (text is not null)
        {
            try
            {
                return ListConfig.Parse(text);
            }
            catch (InvalidDataException)
            {
                // Read as below: the Title and the Text still show.
            }
        }

        return ListConfig.New(Fields([]));
    }

    /// <summary>A template from its file's text, or null when the text is no item (TP-2.4).</summary>
    public static Template? Parse(string path, string text)
    {
        FlatItem item;
        try
        {
            item = FlatItem.Parse(text);
        }
        catch (InvalidDataException)
        {
            return null;
        }

        var title = item.Title.Length > 0 ? item.Title : System.IO.Path.GetFileNameWithoutExtension(path);
        var showsIn = item.Values
            .Where(pair => FieldTypes.IsTrue(pair.Value))
            .Select(pair => pair.Key)
            .ToList();
        return new Template(title, item.Text(TextField), showsIn, path);
    }

    /// <summary>
    /// Every item file of the folder as the browser shows it (TP-8.2), in the order of the names: the file's text
    /// and its item, or neither for a file that could not be read and no item for one that is none. Reads the
    /// disk: call it off the UI thread. A folder that is not there holds nothing.
    /// </summary>
    public static IReadOnlyList<TemplateFile> Files(string folder)
    {
        var files = new List<TemplateFile>();
        if (!Directory.Exists(folder))
        {
            return files;
        }

        foreach (var path in Directory.GetFiles(folder, "*.json"))
        {
            if (string.Equals(System.IO.Path.GetFileName(path), ListConfig.FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? text = null;
            FlatItem? item = null;
            try
            {
                text = TextDocument.Read(path).Text;
                item = FlatItem.Parse(text);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                // Listed as no template: the text when it was read, no item.
            }

            files.Add(new TemplateFile(path, text, item));
        }

        files.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Title, b.Title));
        return files;
    }

    /// <summary>
    /// Every template of the folder (TP-2.1), in the order of their names, and the files that are none. Reads the
    /// disk: call it off the UI thread.
    /// </summary>
    public static TemplateFolder Read(string folder)
    {
        var files = Files(folder);
        return new TemplateFolder(
            [.. files.Where(file => file.Item is not null).Select(file => Parse(file.Path, file.Text!)!)],
            [.. files.Where(file => file.Item is null).Select(file => file.Title)]);
    }

    /// <summary>
    /// The apps that get a check of their own (TP-2.2, TP-5.2), by name, in the bundle's order: the ones with a
    /// field to write in. A new one is added here; the folder's configuration gains its check the next time
    /// axTemplate reads it (<see cref="ConfigurationToWrite"/>).
    /// </summary>
    public static readonly IReadOnlyList<string> AppsWithField = ["axDown", "axJSON", "axTask", "axCalc", "axTerminal", "axPass", "axClaude", "axMCP"];

    /// <summary>
    /// The folder made ready and read (TP-2.1, TP-2.5): made when it is not there, its configuration written when
    /// there is none and given the fields a newer version brings, then every item file. Reads and writes the disk:
    /// call it off the UI thread.
    /// </summary>
    public static TemplateList Open(string folder)
    {
        Directory.CreateDirectory(folder);
        var path = ConfigurationPath(folder);
        var existing = Changes.ReadOrNull(path);
        if (ConfigurationToWrite(existing, AppsWithField) is { } text)
        {
            Changes.Apply([FileChange.Write(path, existing, text)]);
            existing = text;
        }

        return new TemplateList(Configuration(existing), Files(folder));
    }

    /// <summary>
    /// The files as they are after a change the browser made itself, or after its undo, without reading the disk
    /// again: a file written is read from its new text, one removed is gone, one moved has its new path.
    /// </summary>
    public static IReadOnlyList<TemplateFile> After(IReadOnlyList<TemplateFile> files, IReadOnlyList<FileChange> parts, bool undone = false)
    {
        var now = files.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var part in undone ? parts.Reverse() : parts)
        {
            if (part.IsMove)
            {
                var (from, to) = undone ? (part.To!, part.Path) : (part.Path, part.To!);
                if (now.Remove(from, out var moved))
                {
                    now[to] = moved with { Path = to };
                }

                continue;
            }

            var text = undone ? part.Before : part.After;
            if (text is null)
            {
                now.Remove(part.Path);
                continue;
            }

            FlatItem? item = null;
            try
            {
                item = FlatItem.Parse(text);
            }
            catch (InvalidDataException)
            {
                // No template, as a file read from the disk that is no item.
            }

            now[part.Path] = new TemplateFile(part.Path, text, item);
        }

        var list = now.Values.ToList();
        list.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Title, b.Title));
        return list;
    }

    /// <summary>A new template's file text: the name, an empty text and no check, read through the folder's configuration.</summary>
    public static string NewItemText(string title, ListConfig config) => FlatItem.New(title, config).ToText();

    /// <summary>
    /// Why a name cannot be a template's (TP-8.4): empty, or one a template of the folder has already, case
    /// ignored; null when it can. <paramref name="own"/> is the name of the template being renamed, which it may keep.
    /// </summary>
    public static string? NameRefused(string name, IEnumerable<string> names, string? own = null)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Type the template's name";
        }

        return !string.Equals(trimmed, own, StringComparison.OrdinalIgnoreCase) && names.Any(other => string.Equals(other, trimmed, StringComparison.OrdinalIgnoreCase))
            ? $"There is a template called {trimmed} already"
            : null;
    }

    /// <summary>
    /// How many Left keys set the caret after a paste in another program (TP-2.3, TP-D5): one for every character
    /// of the text after the caret's place, a line ending counting as one.
    /// </summary>
    public static int LeftKeys(TemplateText made)
    {
        var tail = made.Text[Math.Clamp(made.Caret, 0, made.Text.Length)..];
        return tail.ReplaceLineEndings("\n").Length;
    }

    /// <summary>What is said of a template whose text makes nothing to copy or put in: the clipboard takes no empty text (TP-4.7, TP-8.4).</summary>
    public static string NoTextYet(string title) => $"{title} has no text yet";

    /// <summary>The value of a check as the item's file holds it.</summary>
    public static JsonNode Checked(bool value) => JsonValue.Create(value);
}
