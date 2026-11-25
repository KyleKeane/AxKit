using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The history of a list's items (SPEC.md TK-4.11; the maintainer's ask, "keep a record of all edits with a time and the
/// field that was edited and what it went from and what it changed to", built 2026-11-24): kept only for a list whose
/// configuration says so (<see cref="Key"/>, off by default: a record of what the user does stays on the computer and
/// is off until he turns it on), in one file beside list.json (<see cref="FileName"/>), one JSON object a line, added to
/// and never rewritten. A line is a change of one field of one item, an item made, removed or moved, with its local
/// time; an undo is recorded as the change it makes. The item is named by its title at the time, and a change of the
/// title is a line of its own, so that an item's history is followed back through its renames
/// (<see cref="Of"/>). The shape is plain on purpose: the long-term item of ids and history can read it as it is.
/// Pure, tested in <c>tests/AxTask.Tests/ItemHistoryTests</c>.
/// </summary>
public static class ItemHistory
{
    /// <summary>The configuration's member: <c>true</c> keeps the history; absent or anything else keeps none.</summary>
    public const string Key = "keep history";

    /// <summary>The file beside list.json; its extension keeps it out of the list's items.</summary>
    public const string FileName = "history.jsonl";

    public const string Made = "made";
    public const string Removed = "removed";
    public const string MovedTo = "moved to";
    public const string MovedFrom = "moved from";

    /// <summary>One line of the history: when, which item by its title then, and either a field with its two values or what happened to the item.</summary>
    public sealed record Entry(string Time, string Item, string? Field, JsonNode? From, JsonNode? To, string? Happened, string? Where);

    public static bool On(ListConfig? config) => config?.Extra(Key) is JsonValue value && value.TryGetValue<bool>(out var on) && on;

    /// <summary>The configuration with the history kept or not; not kept takes the member away.</summary>
    public static ListConfig With(ListConfig config, bool on) => config.WithExtra(Key, on ? JsonValue.Create(true) : null);

    /// <summary>
    /// The lines a step's parts add, each with the folder of the list it belongs to, for the lists <paramref name="keeps"/>
    /// says keep a history (it is asked with a list's folder). Only items count: a file of a list's folder that ends in
    /// ".json" and is not list.json. A write gives a line for each field whose value changed, or "made" when the file
    /// was made and "removed" when it went; a move between two lists gives "moved to" in the one and "moved from" in
    /// the other, a move inside one list nothing (a rename's line is its Title's).
    /// </summary>
    public static List<(string Folder, string Line)> Lines(IReadOnlyList<FileChange> parts, DateTime now, Func<string, bool> keeps)
    {
        var time = now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var lines = new List<(string, string)>();
        foreach (var part in parts)
        {
            if (part.IsMove)
            {
                if (!IsItem(part.Path) || !IsItem(part.To!))
                {
                    continue;
                }

                var from = Path.GetDirectoryName(part.Path)!;
                var to = Path.GetDirectoryName(part.To!)!;
                if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var title = Path.GetFileNameWithoutExtension(part.To!);
                if (keeps(from))
                {
                    lines.Add((from, Line(time, title, null, null, null, MovedTo, Path.GetFileName(to))));
                }

                if (keeps(to))
                {
                    lines.Add((to, Line(time, title, null, null, null, MovedFrom, Path.GetFileName(from))));
                }

                continue;
            }

            var folder = Path.GetDirectoryName(part.Path);
            if (folder is null || !IsItem(part.Path) || !keeps(folder))
            {
                continue;
            }

            var before = Fields(part.Before);
            var after = Fields(part.After);
            var name = TitleOf(after) ?? TitleOf(before) ?? Path.GetFileNameWithoutExtension(part.Path);
            if (before is null && after is not null)
            {
                lines.Add((folder, Line(time, name, null, null, null, Made, null)));
                continue;
            }

            if (after is null || before is null)
            {
                lines.Add((folder, Line(time, TitleOf(before) ?? name, null, null, null, Removed, null)));
                continue;
            }

            // The fields in the order the item has them now, then those it no longer has.
            foreach (var field in after.Select(pair => pair.Key).Concat(before.Select(pair => pair.Key).Where(key => !after.ContainsKey(key))))
            {
                if (field == FlatItem.SchemaKey)
                {
                    continue;
                }

                var old = before.TryGetPropertyValue(field, out var o) ? o : null;
                var current = after.TryGetPropertyValue(field, out var n) ? n : null;
                if (!JsonNode.DeepEquals(old, current))
                {
                    // A changed title is recorded under the old one, which is the name the lines before it use.
                    lines.Add((folder, Line(time, field == ItemDocument.TitleField ? TitleOf(before) ?? name : name, field, old, current, null, null)));
                }
            }
        }

        return lines;
    }

    /// <summary>The parts of an undo, as the change the undo makes: each part taken back, in the reverse order.</summary>
    public static List<FileChange> Undone(IReadOnlyList<FileChange> parts) =>
        [.. parts.Reverse().Select(part => part.IsMove ? FileChange.Move(part.To!, part.Path) : new FileChange(part.Path, part.After, part.Before))];

    /// <summary>
    /// The lines of one item, newest first, from the history file's text: those under its title, and, through each
    /// change of the title, those under the title it had before. A line that does not read is passed over.
    /// </summary>
    public static List<Entry> Of(string text, string title)
    {
        var entries = text.Split('\n').Select(Parse).OfType<Entry>().ToList();
        var found = new List<Entry>();
        var name = title;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (!entry.Item.Equals(name, StringComparison.OrdinalIgnoreCase)
                && !(entry.Field == ItemDocument.TitleField && TextOf(entry.To).Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            found.Add(entry);
            if (entry.Field == ItemDocument.TitleField && TextOf(entry.To).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                name = TextOf(entry.From);
            }
        }

        return found;
    }

    /// <summary>One line said: "2026-11-24 18:30, Done: from not done to done"; "2026-11-24 18:31, moved to Home".</summary>
    public static string Said(Entry entry)
    {
        var when = entry.Time.Replace('T', ' ');
        when = when.Length > 16 ? when[..16] : when;
        return entry.Field is { } field
            ? $"{when}, {field}: from {Spoken(entry.From)} to {Spoken(entry.To)}"
            : $"{when}, {entry.Happened}{(entry.Where is { } where ? " " + where : string.Empty)}";
    }

    private static bool IsItem(string path) =>
        path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        && !Path.GetFileName(path).Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase);

    private static JsonObject? Fields(string? text)
    {
        if (text is null)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TitleOf(JsonObject? fields) =>
        fields is not null && fields.TryGetPropertyValue(ItemDocument.TitleField, out var title) && title is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string Line(string time, string item, string? field, JsonNode? from, JsonNode? to, string? happened, string? where)
    {
        var line = new JsonObject { ["time"] = time, ["item"] = item };
        if (field is not null)
        {
            line["field"] = field;
            line["from"] = from?.DeepClone();
            line["to"] = to?.DeepClone();
        }
        else
        {
            line["happened"] = happened;
            if (where is not null)
            {
                line["where"] = where;
            }
        }

        return line.ToJsonString();
    }

    private static Entry? Parse(string line)
    {
        try
        {
            if (JsonNode.Parse(line.Trim()) is not JsonObject o || o["time"] is not JsonValue time || o["item"] is not JsonValue item)
            {
                return null;
            }

            return new Entry(time.ToString(), item.ToString(), (o["field"] as JsonValue)?.ToString(), o["from"], o["to"], (o["happened"] as JsonValue)?.ToString(), (o["where"] as JsonValue)?.ToString());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string TextOf(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    private static string Spoken(JsonNode? value) => value switch
    {
        null => "empty",
        JsonValue v when v.TryGetValue<bool>(out var flag) => flag ? "checked" : "not checked",
        JsonValue v when v.TryGetValue<string>(out var text) => text.Length == 0 ? "empty" : text.Replace("\n", " "),
        _ => value.ToJsonString(),
    };
}
