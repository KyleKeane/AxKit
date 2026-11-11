using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// A smart list (docs/axTask/SPEC.md TK-5.2): a saved filter, a <c>.filter.json</c> file in the top folder, or one
/// built in: All tasks (TK-D1), the four by date (TK-5.4) and a mention or hashtag (TK-5.5). Its conditions are read
/// once; <see cref="Matches"/> asks them of one item and <see cref="Items"/> of every item under the folder, the
/// Archive left out.
/// </summary>
public sealed class SmartList
{
    public const string Format = "axkit-filter 1";
    public const string AllTasksName = "All tasks";

    private readonly JsonArray _conditions;
    private readonly bool _any;
    private readonly IReadOnlyList<OrderKey> _order;

    /// <summary>A built-in list's own rule, asked after the conditions (TK-5.4, TK-5.5); null for a filter file and All tasks.</summary>
    private readonly Func<ItemDocument, DateOnly, bool>? _rule;

    private SmartList(string name, string? path, JsonArray conditions, bool any, IReadOnlyList<OrderKey> order, string? notUnderstood, Func<ItemDocument, DateOnly, bool>? rule = null, string? described = null)
    {
        Name = name;
        Path = path;
        _conditions = conditions;
        _any = any;
        _order = order;
        NotUnderstood = notUnderstood;
        _rule = rule;
        Described = described;
    }

    public string Name { get; }

    /// <summary>The filter's file; null for a built-in list (All tasks, the four by date, a mention or a hashtag).</summary>
    public string? Path { get; }

    /// <summary>Whether the list is built in rather than a filter file of the user's.</summary>
    public bool BuiltIn => Path is null;

    /// <summary>What sets the list apart in the window: the filter's path, or "smart:" and a built-in list's name.</summary>
    public string Key => Path ?? "smart:" + Name;

    /// <summary>What a built-in list holds, one line for its details; null for a filter file.</summary>
    public string? Described { get; }

    /// <summary>What in the file was not understood (TK-5.2); null when every condition is known. Such a list shows nothing.</summary>
    public string? NotUnderstood { get; }

    private static JsonArray NotDone() => [new JsonObject { ["field"] = TaskRules.DoneField, ["is"] = false }];

    private static readonly IReadOnlyList<OrderKey> SoonestFirst = [new(TaskRules.DueField), new(ItemDocument.TitleField)];

    /// <summary>TK-5.1: every task not done.</summary>
    public static SmartList AllTasks { get; } = new(AllTasksName, null, NotDone(), any: false, SoonestFirst, null,
        described: "Every task not done, in every list but the Archive, the soonest due first");

    /// <summary>TK-5.4: the tasks not done whose Due date has passed.</summary>
    public static SmartList Overdue { get; } = new("Overdue", null, NotDone(), any: false, SoonestFirst, null,
        (item, today) => TaskRules.Due(item) is { } due && due < today, "Every task not done whose Due date has passed, the oldest first");

    /// <summary>TK-5.4: the tasks not done due today.</summary>
    public static SmartList DueToday { get; } = new("Due today", null, NotDone(), any: false, SoonestFirst, null,
        (item, today) => TaskRules.Due(item) == today, "Every task not done that is due today");

    /// <summary>TK-5.4: the tasks not done due from today to seven days on.</summary>
    public static SmartList DueInSevenDays { get; } = new("Due in the next 7 days", null, NotDone(), any: false, SoonestFirst, null,
        (item, today) => TaskRules.Due(item) is { } due && due >= today && due <= today.AddDays(7), "Every task not done due from today to seven days on, the soonest first");

    /// <summary>TK-5.4: the tasks not done without a Due date.</summary>
    public static SmartList NoDueDate { get; } = new("No due date", null, NotDone(), any: false, [new(ItemDocument.TitleField)], null,
        (item, _) => TaskRules.Due(item) is null, "Every task not done that has no Due date, by title");

    /// <summary>The lists by due date, the Date lists heading's, in order (TK-5.4).</summary>
    public static IReadOnlyList<SmartList> DateLists { get; } = [Overdue, DueToday, DueInSevenDays, NoDueDate];

    /// <summary>Every list built in with a name of its own: All tasks and the lists by due date (TK-5.1, TK-5.4).</summary>
    public static IReadOnlyList<SmartList> BuiltIns { get; } = [AllTasks, .. DateLists];

    /// <summary>TK-5.5: the tasks not done whose words hold a mention or a hashtag, "@Sam" or "#store".</summary>
    public static SmartList Tagged(string token) => new(token, null, NotDone(), any: false, SoonestFirst, null,
        (item, _) => Tags.Has(Tags.TextOf(item), token),
        $"Every task not done whose title or text holds {token}, the soonest due first");

    /// <summary>The text a new smart list starts from (TK-5.3): All tasks' conditions in a file of its own.</summary>
    public static string NewFileText() =>
        new JsonObject
        {
            ["format"] = Format,
            ["match"] = "all",
            ["conditions"] = new JsonArray(new JsonObject { ["field"] = TaskRules.DoneField, ["is"] = false }),
            ["order"] = new JsonArray(JsonValue.Create(TaskRules.DueField), JsonValue.Create(ItemDocument.TitleField)),
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\r\n") + "\r\n";

    /// <summary>The smart lists of the top folder, by name.</summary>
    public static IReadOnlyList<SmartList> InFolder(string top)
    {
        if (!Directory.Exists(top))
        {
            return [];
        }

        return Directory.GetFiles(top, "*" + ItemFolder.FilterExtension)
            .Select(Read)
            .OrderBy(list => list.Name, AxKit.Core.NaturalOrder.Instance)
            .ToList();
    }

    /// <summary>A filter file; one that cannot be read or understood is a smart list with <see cref="NotUnderstood"/> set.</summary>
    public static SmartList Read(string path)
    {
        var name = System.IO.Path.GetFileName(path)[..^ItemFolder.FilterExtension.Length];
        try
        {
            if (JsonNode.Parse(AxKit.Core.TextDocument.Read(path).Text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) is not JsonObject root
                || root["conditions"] is not JsonArray conditions)
            {
                return Broken(name, path, "no list of conditions");
            }

            var match = root["match"] is JsonValue m && m.TryGetValue<string>(out var text) ? text : "all";
            if (match is not ("all" or "any"))
            {
                return Broken(name, path, $"match is {match}, not all or any");
            }

            List<OrderKey> order = [new(TaskRules.DueField), new(ItemDocument.TitleField)];
            if (root["order"] is JsonArray keys)
            {
                order = [];
                foreach (var key in keys)
                {
                    if (OrderKey.Read(key) is not { } read)
                    {
                        return Broken(name, path, "an order that is not a field name, or a field with by options or by text");
                    }

                    order.Add(read);
                }
            }
            foreach (var condition in conditions)
            {
                if (Check(condition) is { } problem)
                {
                    return Broken(name, path, problem);
                }
            }

            return new SmartList(name, path, (JsonArray)conditions.DeepClone(), match == "any", order, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return Broken(name, path, "the file could not be read");
        }
    }

    /// <summary>The items under the top folder the conditions hold for, in the list's order.</summary>
    public IReadOnlyList<ItemFile> Items(string top, DateOnly today)
    {
        if (NotUnderstood is not null)
        {
            return [];
        }

        var items = ItemFolder.AllItems(top).Where(item => Matches(item, top, today));
        return Order(items, item => item);
    }

    /// <summary>
    /// The items read from several folders of lists the conditions hold for, together in the list's order (TK-5.6): a
    /// "list" condition is asked of each item against its own folder of lists.
    /// </summary>
    public IReadOnlyList<GatheredItem> Items(IEnumerable<GatheredItem> items, DateOnly today) =>
        NotUnderstood is not null ? [] : Order(items.Where(gathered => Matches(gathered.Item, gathered.Top, today)), gathered => gathered.Item);

    /// <summary>How many of the items read the conditions hold for (TK-5.6), for the list's row.</summary>
    public int Count(IEnumerable<GatheredItem> items, DateOnly today) =>
        NotUnderstood is not null ? 0 : items.Count(gathered => Matches(gathered.Item, gathered.Top, today));

    public bool Matches(ItemFile item, string top, DateOnly today)
    {
        if (_rule is not null && !_rule(item.Document, today))
        {
            return false;
        }

        if (_conditions.Count == 0)
        {
            return true;
        }

        var results = _conditions.Select(condition => Holds((JsonObject)condition!, item, top, today));
        return _any ? results.Any(held => held) : results.All(held => held);
    }

    private IReadOnlyList<T> Order<T>(IEnumerable<T> items, Func<T, ItemFile> fileOf)
    {
        IOrderedEnumerable<T>? ordered = null;
        foreach (var key in _order)
        {
            // An item without the field, or with it empty, goes after the rest in either direction. A date sorts by
            // the day; a choice by its options' order unless the key says by text; anything else by its text, as spoken.
            Func<T, int> missing = entry => fileOf(entry).Document is var document && document.IndexOf(key.Field) is var i && i >= 0 && Sortable(document.Fields[i], textOnly: true) is { Length: > 0 } ? 0 : 1;
            Func<T, string> value = entry => fileOf(entry).Document is var document && document.IndexOf(key.Field) is var i && i >= 0 ? Sortable(document.Fields[i], key.ByText) : string.Empty;
            ordered = ordered is null ? items.OrderBy(missing) : ordered.ThenBy(missing);
            ordered = key.Descending
                ? ordered.ThenByDescending(value, AxKit.Core.NaturalOrder.Instance)
                : ordered.ThenBy(value, AxKit.Core.NaturalOrder.Instance);
        }

        return (ordered ?? items.OrderBy(entry => ItemFolder.NameOf(fileOf(entry)), AxKit.Core.NaturalOrder.Instance)).ToList();
    }

    private static string Sortable(ItemField field, bool textOnly) => field.Type switch
    {
        FieldTypes.Check => FieldTypes.IsTrue(field.Value) ? "1" : "0",
        // A choice's place among its options, as a number; a value not among them after the rest.
        FieldTypes.Choice when !textOnly && FieldTypes.TextOf(field.Value) is { Length: > 0 } chosen => OptionPlace(field, chosen),
        _ => field.Value is JsonValue value && value.GetValueKind() == JsonValueKind.Number ? value.GetValue<double>().ToString("000000000000000.######", System.Globalization.CultureInfo.InvariantCulture) : FieldTypes.TextOf(field.Value),
    };

    private static string OptionPlace(ItemField field, string chosen)
    {
        var place = field.Options.ToList().FindIndex(option => string.Equals(option, chosen, StringComparison.OrdinalIgnoreCase));
        return (place < 0 ? field.Options.Count : place).ToString("000000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool Holds(JsonObject condition, ItemFile item, string top, DateOnly today)
    {
        if (condition["list"] is JsonValue listValue && listValue.TryGetValue<string>(out var list))
        {
            var folder = System.IO.Path.GetRelativePath(top, System.IO.Path.GetDirectoryName(item.Path)!).Replace('\\', '/');
            var wanted = list.Trim('/').Replace('\\', '/');
            return folder.Equals(wanted, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(wanted + "/", StringComparison.OrdinalIgnoreCase);
        }

        var name = condition["field"] is JsonValue f && f.TryGetValue<string>(out var title) ? title : string.Empty;
        var index = item.Document.IndexOf(name);
        var field = index >= 0 ? item.Document.Fields[index] : null;
        if (condition.ContainsKey("empty"))
        {
            var empty = field is null
                || (field.Type == FieldTypes.Check ? !FieldTypes.IsTrue(field.Value) : Sortable(field, textOnly: true).Length == 0);
            return empty == FieldTypes.IsTrue(condition["empty"]);
        }

        if (field is null)
        {
            return false;
        }

        if (condition["is"] is JsonNode wantedValue)
        {
            return field.Type == FieldTypes.Check
                ? FieldTypes.IsTrue(field.Value) == FieldTypes.IsTrue(wantedValue)
                : string.Equals(Sortable(field, textOnly: true), wantedValue is JsonValue w && w.TryGetValue<string>(out var s) ? s : wantedValue.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        }

        if (condition["has"] is JsonValue hasValue && hasValue.TryGetValue<string>(out var word))
        {
            return FieldTypes.TextOf(field.Value).Contains(word, StringComparison.OrdinalIgnoreCase);
        }

        var date = field.Type == FieldTypes.DateTime && FieldTypes.ReadDateTime(field.Value) is { } moment ? DateOnly.FromDateTime(moment) : FieldTypes.ReadDate(field.Value);
        if (condition["before"] is JsonValue before && before.TryGetValue<string>(out var beforeText))
        {
            return date is { } day && FieldTypes.ParseDate(beforeText, today) is { } limit && day <= limit;
        }

        if (condition["after"] is JsonValue after && after.TryGetValue<string>(out var afterText))
        {
            return date is { } day && FieldTypes.ParseDate(afterText, today) is { } limit && day >= limit;
        }

        return false;
    }

    /// <summary>Why a condition is not understood; null when it is.</summary>
    private static string? Check(JsonNode? node)
    {
        if (node is not JsonObject condition)
        {
            return "a condition that is not an object";
        }

        if (condition.ContainsKey("list"))
        {
            return condition["list"] is JsonValue v && v.TryGetValue<string>(out _) ? null : "a list that is not a name";
        }

        if (condition["field"] is not JsonValue f || !f.TryGetValue<string>(out _))
        {
            return "a condition without a field";
        }

        string[] known = ["is", "has", "before", "after", "empty"];
        var tests = condition.Select(pair => pair.Key).Where(key => key != "field").ToList();
        if (tests.Count != 1 || !known.Contains(tests[0]))
        {
            return $"a condition on {f} with {(tests.Count == 0 ? "nothing to test" : string.Join(" and ", tests))}";
        }

        if (tests[0] is "before" or "after" && (condition[tests[0]] is not JsonValue d || !d.TryGetValue<string>(out var text) || FieldTypes.ParseDate(text, DateOnly.FromDayNumber(0)) is null))
        {
            return $"a date that is not a date: {condition[tests[0]]?.ToJsonString()}";
        }

        return null;
    }

    private static SmartList Broken(string name, string path, string why) => new(name, path, [], any: false, [], why);
}
