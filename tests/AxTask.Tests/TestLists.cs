using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// Lists and items on disk for the tests, in the one shape the app reads (docs/axTask/forms-framework.md section 13):
/// a list's <c>list.json</c> with the fields of its items, and each item as its values.
/// </summary>
internal static class TestLists
{
    /// <summary>The list's configuration written with these fields (a new list's own by default), and a section when one is given; the configuration.</summary>
    public static ListConfig List(string folder, IReadOnlyList<ItemField>? fields = null, string? section = null)
    {
        Directory.CreateDirectory(folder);
        var config = ListConfig.New(fields ?? TaskRules.DefaultTemplate);
        if (section is not null)
        {
            config = config.WithDetail(ItemFolder.SectionField, JsonValue.Create(section));
        }

        File.WriteAllText(Path.Combine(folder, ItemDocument.ListFile), config.ToText());
        return config;
    }

    /// <summary>
    /// An item written as its file, from the document a test made of it. Its list's configuration names every field
    /// of the document: written when the list has none, and given the fields it lacks, so that a test's item shows
    /// what the test put in it.
    /// </summary>
    public static string Write(string path, ItemDocument document)
    {
        var folder = Path.GetDirectoryName(path)!;
        var own = document.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            .Select(field => field with { Value = FieldTypes.Empty(field.Type) }).ToList();
        var config = ItemFolder.ReadList(folder);
        var missing = own.Where(field => config is null || config.IndexOf(field.Title) < 0).ToList();
        if (config is null)
        {
            config = List(folder, own);
        }
        else if (missing.Count > 0)
        {
            config = config.WithItemFields([.. config.ItemFields, .. missing]);
            File.WriteAllText(Path.Combine(folder, ItemDocument.ListFile), config.ToText());
        }

        File.WriteAllText(path, ItemFolder.NewItemFile(document, config).ToText());
        return path;
    }

    /// <summary>An item of a list's configuration, with these values set, written under its title; its path.</summary>
    public static string Item(string folder, string title, params (string Name, JsonNode? Value)[] values)
    {
        var config = ItemFolder.ReadList(folder) ?? List(folder);
        var flat = FlatItem.New(title, config);
        foreach (var (name, value) in values)
        {
            flat = flat.With(name, value, config);
        }

        var path = Path.Combine(folder, title + ".json");
        File.WriteAllText(path, flat.ToText());
        return path;
    }

    /// <summary>The text of an item's file after a change to the document a test read of it: what the window writes.</summary>
    public static string TextOf(string path, Func<ItemDocument, ItemDocument> change)
    {
        var item = ItemFolder.ReadItem(path)!;
        return item.TextWith(change(item.Document));
    }
}
