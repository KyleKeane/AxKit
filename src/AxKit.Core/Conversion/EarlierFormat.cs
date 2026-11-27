using System.Text.Json;
using System.Text.Json.Nodes;
namespace AxKit.Core.Conversion;

/// <summary>One item's file as the conversion writes it: its name in the list's folder and its new text.</summary>
public sealed record ConvertedItem(string FileName, string Text);

/// <summary>
/// A list converted: the new text of its <c>list.json</c>, each item's new text, and what the conversion has to say
/// (a file left as it was and why, a name it had to change), a sentence each.
/// </summary>
public sealed record ConvertedList(string Config, IReadOnlyList<ConvertedItem> Items, IReadOnlyList<string> Notes);

/// <summary>
/// The conversion of a list from axTask's earlier shape to the one the app reads (docs/axTask/forms-framework.md
/// AF-13.6; the maintainer's decision of 2026-11-21: "we should just convert any items or configs that are not
/// updated ... so that we do not try to maintain code to render both ... then all the converter code is modular and
/// separated from the core code of the app").
///
/// The earlier shape (<c>axkit-list 1</c>, <c>axkit-item 1</c>): an item is a list of fields, each an object with
/// its title, its type and its value; a list's file holds its own fields the same way, the template of a new item
/// under <c>"new item"</c>, and the action keys under <c>"keys"</c>. The shape now: an item is its names and values
/// with a link to the list's file (<see cref="FlatItem"/>), and the list's file is the schema
/// (<see cref="ListConfig"/>).
///
/// This file reads the earlier shape itself and is the only place that knows it: the apps' own code calls it where
/// it meets such a list (through <see cref="ListConversion"/>, the folder's side) and nowhere reads the earlier
/// shape. Pure: texts in, texts out. Nothing is ever dropped: a value that has no place of its own is kept under a
/// name the notes give. It sits beside the item format in <c>AxKit.Core</c> because two things meet earlier lists:
/// axTask, and the report form's own list (<c>Reports/ReportFiles</c>), whose waiting reports must still be sent.
/// </summary>
public static class EarlierFormat
{
    public const string ListFormat = "axkit-list 1";
    public const string ItemFormat = "axkit-item 1";

    /// <summary>Whether a list's file is of the earlier shape.</summary>
    public static bool IsEarlierList(string text) => ListConfig.FormatOf(text) == ListFormat;

    /// <summary>Whether an item's file is of the earlier shape: an object of that format with a list of fields.</summary>
    public static bool IsEarlierItem(string text) => ReadObject(text) is { } root && FormatOf(root) == ItemFormat && root["fields"] is JsonArray;

    /// <summary>
    /// The list converted. <paramref name="listText"/> is the earlier <c>list.json</c>, or null for a folder that had
    /// none; <paramref name="items"/> are the folder's item files by name. The configuration's item fields are the
    /// template's, in its order, and after them every field that only some items hold, so that nothing an item shows
    /// today is hidden by the conversion (AF-13.6). An item that is not of the earlier shape is left out of the
    /// result and its file stays as it is: without a word when it is an item of the shape now (a list half
    /// converted, or one an earlier version of axKit wrote a file into), with a note otherwise. A list's file that
    /// is of the shape now already is kept, and only gains the fields the earlier items bring.
    /// </summary>
    public static ConvertedList Convert(string? listText, IReadOnlyList<(string FileName, string Text)> items)
    {
        var notes = new List<string>();
        var existing = listText is not null && ListConfig.FormatOf(listText) == ListConfig.Format ? ListConfig.Parse(listText) : null;
        var list = listText is null || existing is not null ? null : ReadObject(listText);
        if (listText is not null && existing is null && list is null)
        {
            notes.Add("The list's own file did not read as JSON; the new one is made from the items alone.");
        }

        // The fields of the list's items: the template first, then what the items themselves hold.
        var fields = new List<ItemField>();
        foreach (var field in existing?.ItemFields ?? Fields(list?["new item"]))
        {
            AddField(fields, field, startValue: field.Value);
        }

        var read = new List<(string FileName, JsonObject Root, List<ItemField> Named)>();
        foreach (var (name, text) in items)
        {
            var root = ReadObject(text);
            if (FlatItem.IsItem(root))
            {
                continue;
            }

            if (root is null || FormatOf(root) != ItemFormat || root["fields"] is not JsonArray)
            {
                notes.Add($"{name} is not an item of the earlier format and is left as it is.");
                continue;
            }

            // The item's fields under the names they keep: a field renamed here is a field of the list too, so
            // that it is still shown after the conversion.
            var named = Named(name, root, notes);
            read.Add((name, root, named));
            foreach (var field in named)
            {
                // A field only items hold starts empty in a new item: an item's own value is no template's.
                AddField(fields, field, startValue: null);
            }
        }

        // A configuration of the shape now is rewritten only where the earlier items bring a field it lacks.
        var config = existing is null ? ListConfig.New(fields)
            : fields.Count == existing.ItemFields.Count ? existing
            : existing.WithItemFields(fields);
        foreach (var detail in Fields(list?["fields"]))
        {
            config = config.WithDetail(detail.Title, detail.Value);
        }

        if (Actions(list?["keys"]) is { Count: > 0 } actions)
        {
            config = config.WithExtra(ListConfig.ActionsKey, actions);
        }

        if (list is not null)
        {
            // Whatever else the list's file held is kept as it was (IA-4).
            foreach (var (key, value) in list)
            {
                if (key is not ("format" or "fields" or "new item" or "keys") && config.Extra(key) is null)
                {
                    config = config.WithExtra(key, value);
                }
            }
        }

        var converted = new List<ConvertedItem>();
        foreach (var (name, root, named) in read)
        {
            converted.Add(new ConvertedItem(name, Item(name, root, named, config, notes).ToText()));
        }

        // A configuration that gained nothing keeps its text to the letter, so that its file is not written at all.
        return new ConvertedList(ReferenceEquals(config, existing) ? listText! : config.ToText(), converted, notes);
    }

    /// <summary>
    /// An item's fields under the names they keep. Two names cannot stay: the link's own, which a field cannot have
    /// any more, and a second field of a title, since a value is known by its name now. Each is kept under another
    /// name, and said.
    /// </summary>
    private static List<ItemField> Named(string name, JsonObject root, List<string> notes)
    {
        var named = new List<ItemField>();
        foreach (var field in Fields(root["fields"]))
        {
            var title = field.Title;
            if (title.Length == 0)
            {
                // A value is known by its name now, and this one had none.
                title = "untitled field";
                notes.Add($"{name}: a field without a title is kept as \"{title}\".");
            }
            else if (title.Equals(FlatItem.SchemaKey, StringComparison.OrdinalIgnoreCase))
            {
                title = "schema (field)";
                notes.Add($"{name}: the field named schema is kept as \"{title}\", since that name is the item's link now.");
            }

            if (named.Any(known => known.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
            {
                var second = Free([.. named.Select(known => (known.Title, known.Value))], title);
                notes.Add($"{name}: a second field named {title} is kept as \"{second}\".");
                title = second;
            }

            named.Add(field with { Title = title });
        }

        return named;
    }

    /// <summary>One item as its values: the link, the Title, the configuration's fields the item holds in the configuration's order, then whatever else it held.</summary>
    private static FlatItem Item(string name, JsonObject root, List<ItemField> named, ListConfig config, List<string> notes)
    {
        var values = named.Select(field => (field.Title, field.Value)).ToList();

        var item = FlatItem.Parse(new JsonObject { [FlatItem.SchemaKey] = ListConfig.FileName }.ToJsonString());
        var titled = values.FirstOrDefault(value => value.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase));
        item = item.With(ItemDocument.TitleField, titled.Value?.DeepClone() ?? JsonValue.Create(string.Empty));
        foreach (var field in config.ItemFields)
        {
            var held = values.FirstOrDefault(value => value.Title.Equals(field.Title, StringComparison.OrdinalIgnoreCase));
            if (held.Title is not null)
            {
                item = item.With(field.Title, held.Value);
            }
        }

        foreach (var (title, value) in values)
        {
            if (!item.Has(title))
            {
                item = item.With(title, value);
            }
        }

        // What the item's file held beside its fields is kept as values of its own, after them.
        foreach (var (key, value) in root)
        {
            if (key is "format" or "fields")
            {
                continue;
            }

            var kept = item.Has(key) || key.Equals(FlatItem.SchemaKey, StringComparison.OrdinalIgnoreCase) ? Free(item.Values.Select(pair => (pair.Key, pair.Value)).ToList(), key + " (file)") : key;
            if (kept != key)
            {
                notes.Add($"{name}: the file's own member {key} is kept as \"{kept}\".");
            }

            item = item.With(kept, value);
        }

        return item;
    }

    /// <summary>A name no value has yet: the name itself, or with a number after it.</summary>
    private static string Free(List<(string Title, JsonNode? Value)> values, string title)
    {
        var free = title;
        for (var n = 2; values.Any(known => known.Title.Equals(free, StringComparison.OrdinalIgnoreCase)); n++)
        {
            free = $"{title} {n}";
        }

        return free;
    }

    /// <summary>A field joins the configuration's item fields once, by its title: the first that names it gives its type and its options. Never the Title, which every item has, nor the link's name.</summary>
    private static void AddField(List<ItemField> fields, ItemField field, JsonNode? startValue)
    {
        if (field.Title.Length == 0
            || field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase)
            || field.Title.Equals(FlatItem.SchemaKey, StringComparison.OrdinalIgnoreCase)
            || fields.Any(known => known.Title.Equals(field.Title, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var type = field.Type.Length == 0 ? FieldTypes.Text : field.Type;
        fields.Add(new ItemField(field.Title, type, startValue?.DeepClone() ?? FieldTypes.Empty(type), field.Options));
    }

    /// <summary>
    /// The action keys as actions (AF-3.3): the action of digit n is an action with the key Ctrl+n, its kind named,
    /// and what it sets, where it moves to or what it opens as it was.
    /// </summary>
    private static JsonArray Actions(JsonNode? keys)
    {
        var actions = new JsonArray();
        if (keys is not JsonObject root)
        {
            return actions;
        }

        for (var digit = 1; digit <= 9; digit++)
        {
            if (root[digit.ToString(System.Globalization.CultureInfo.InvariantCulture)] is not JsonObject key || FieldTypes.TextOf(key["name"]).Trim() is not { Length: > 0 } name)
            {
                continue;
            }

            var action = new JsonObject { ["name"] = name, ["key"] = $"Ctrl+{digit}" };
            if (key["set"] is JsonObject set)
            {
                action["does"] = "set";
                action["set"] = set.DeepClone();
            }
            else if (FieldTypes.TextOf(key["move"]).Trim() is { Length: > 0 } to)
            {
                action["does"] = "move";
                action["to"] = to;
            }
            else if (FieldTypes.TextOf(key["open"]).Trim() is { Length: > 0 } open)
            {
                action["does"] = "open";
                action["open"] = open;
            }
            else
            {
                continue;
            }

            actions.Add(action);
        }

        return actions;
    }

    /// <summary>The fields of the earlier shape: a list of objects, each with a title, a type, a value and a choice's options.</summary>
    private static List<ItemField> Fields(JsonNode? node)
    {
        var fields = new List<ItemField>();
        if (node is not JsonArray array)
        {
            return fields;
        }

        foreach (var entry in array)
        {
            if (entry is not JsonObject field)
            {
                continue;
            }

            var options = field["options"] is JsonArray given
                ? given.Select(option => option is JsonValue value && value.TryGetValue<string>(out var text) ? text : null).OfType<string>().ToList()
                : [];
            fields.Add(new ItemField(FieldTypes.TextOf(field["title"]).Trim(), FieldTypes.TextOf(field["type"]).Trim(), field["value"]?.DeepClone(), options));
        }

        return fields;
    }

    private static string? FormatOf(JsonObject root) => root["format"] is JsonValue value && value.TryGetValue<string>(out var format) ? format : null;

    private static JsonObject? ReadObject(string text)
    {
        try
        {
            // A file that names a member twice is no item of the earlier shape either (JsonText): it is left as it is.
            return JsonText.Parse(text, lenient: true) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
