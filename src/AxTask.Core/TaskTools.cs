using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>A list an AI tool may reach (docs/axTask/SPEC.md TK-8.1): its name for the tools, the place it is in, its folder.</summary>
public sealed record ReachableList(string Name, string Place, string Folder);

/// <summary>
/// The pure half of axTask's AI tools (docs/axKit/mcp.md MC-8, docs/axTask/SPEC.md TK-8, 2026-11-11): the lists the
/// window reaches, a list found by its name, an item as the tools give it, and a new item made from a list's template
/// with the values a client typed. The window answers the tools with these (<c>AxTask/TaskForm.Mcp.cs</c>).
/// </summary>
public static class TaskTools
{
    /// <summary>The place of the top folder's own lists.</summary>
    public const string TopPlace = "top folder";

    /// <summary>
    /// TK-8.1, MC-D12: the lists of the top folder by their own name ("Personal"), then each project's as
    /// "project / list" ("alpha / Home"); the Archive and the smart lists are never among them.
    /// </summary>
    public static IReadOnlyList<ReachableList> Lists(string top, IEnumerable<TaskProject> projects)
    {
        var lists = ItemFolder.ReadTop(top).Lists.Select(folder => new ReachableList(Path.GetFileName(folder), TopPlace, folder)).ToList();
        foreach (var project in projects)
        {
            lists.AddRange(ItemFolder.ReadTop(project.Path).Lists.Select(folder => new ReachableList($"{project.Name} / {Path.GetFileName(folder)}", project.Name, folder)));
        }

        return lists;
    }

    /// <summary>
    /// The list a client named (TK-8.2): its name as <see cref="Lists"/> gives it, case not minded; a list's own last part
    /// when exactly one list has it ("Home" for "alpha / Home" when the top folder has none); or its full path when that
    /// is one of the lists. A path elsewhere is refused (MC-D12): the tools reach only the window's own lists.
    /// </summary>
    public static (ReachableList? List, string? Refused) Find(IReadOnlyList<ReachableList> lists, string? named)
    {
        var name = named?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return (null, "Name the list: axkit_task_lists gives the names.");
        }

        if (Path.IsPathRooted(name) || name.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var full = Path.TrimEndingDirectorySeparator(name);
            return lists.FirstOrDefault(list => list.Folder.Equals(full, StringComparison.OrdinalIgnoreCase)) is { } there
                ? (there, null)
                : (null, $"{name} is not one of the axTask window's lists: AI tools reach only the lists of its top folder and of its project folders.");
        }

        if (lists.FirstOrDefault(list => list.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } exact)
        {
            return (exact, null);
        }

        var last = lists.Where(list => Path.GetFileName(list.Folder).Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        return last.Count switch
        {
            1 => (last[0], null),
            0 => (null, $"No list is named {name}: axkit_task_lists gives the names."),
            _ => (null, $"Several lists are named {name}: give one of {string.Join("; ", last.Select(list => list.Name))}."),
        };
    }

    /// <summary>
    /// An item as the tools give it (TK-8.1): its title, whether it is done (a task only), and its fields with their
    /// type and value; a password or a user name field (axPass's types, never axTask's own) as "protected" (MC-4.3).
    /// </summary>
    public static JsonObject ItemOf(ItemFile item)
    {
        var fields = new JsonArray();
        foreach (var field in item.Document.Fields)
        {
            var value = field.Type is FieldTypes.Password or FieldTypes.UserName ? JsonValue.Create("protected") : field.Value?.DeepClone();
            var entry = new JsonObject { ["title"] = field.Title, ["type"] = field.Type, ["value"] = value };
            if (field.Options.Count > 0)
            {
                entry["options"] = new JsonArray([.. field.Options.Select(option => (JsonNode)option)]);
            }

            fields.Add(entry);
        }

        var result = new JsonObject { ["title"] = ItemFolder.NameOf(item) };
        if (TaskRules.IsTask(item.Document))
        {
            result["done"] = TaskRules.IsDone(item.Document);
        }

        result["fields"] = fields;
        return result;
    }

    /// <summary>
    /// TK-8.3: a new item as the window's New item makes it (<see cref="ItemDocument.NewItem"/> with the list's template),
    /// then each value typed into its field as the field's own change would take it (<see cref="FieldTypes.Parse"/>: a
    /// date as 2026-11-18, today, +7 or a weekday; a choice as one of its options), a check as true or false. A field the
    /// template lacks is added at the end (MC-D13): long text for Notes or a value of several lines, text otherwise. A
    /// link, a password and a user name are refused, as is the Title among the fields. The first refusal is the answer.
    /// </summary>
    public static (ItemDocument? Item, string? Refused) NewItem(string? title, IReadOnlyList<ItemField> template, IReadOnlyList<KeyValuePair<string, string>> values, DateOnly today)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return (null, "Give the item's title.");
        }

        if (trimmed.Contains('\n') || trimmed.Contains('\r'))
        {
            return (null, "A title is one line.");
        }

        var item = ItemDocument.NewItem(trimmed, template);
        foreach (var (name, text) in values)
        {
            var field = name.Trim();
            if (field.Length == 0)
            {
                return (null, "A field needs its title.");
            }

            if (field.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            {
                return (null, "The title is given as title, not among the fields.");
            }

            var index = item.IndexOf(field);
            if (index < 0)
            {
                var type = field.Equals(NotesField, StringComparison.OrdinalIgnoreCase) || text.Contains('\n') ? FieldTypes.LongText : FieldTypes.Text;
                item = item.WithField(-1, field, type, []);
                index = item.Fields.Count - 1;
            }

            var shown = item.Fields[index];
            var (value, refused) = ValueOf(shown, text, today);
            if (refused is not null)
            {
                return (null, $"{shown.Title}: {refused}");
            }

            item = item.WithValue(index, value);
        }

        return (item, null);
    }

    /// <summary>The field a new item's notes go in when its list's template has none.</summary>
    public const string NotesField = "Notes";

    private static (JsonNode? Value, string? Refused) ValueOf(ItemField field, string text, DateOnly today)
    {
        switch (field.Type)
        {
            case FieldTypes.Check:
                return text.Trim().ToLowerInvariant() switch
                {
                    "true" or "yes" or "checked" => (JsonValue.Create(true), null),
                    "false" or "no" or "not checked" or "" => (JsonValue.Create(false), null),
                    _ => (null, "a check is true or false"),
                };
            case FieldTypes.Link:
                return (null, "a link is made by the user in the axTask window");
            case FieldTypes.Password or FieldTypes.UserName:
                return (null, "AI tools never write a password or a user name");
        }

        var parsed = FieldTypes.Parse(field.Type, text, field.Options, today);
        return (parsed.Value, parsed.Refused);
    }
}
