using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// One action a Ctrl+number key runs on an item of a list (docs/axKit/reports.md RP-8.1): fields set to values, the
/// item moved to a list by a path relative to this one, or something opened (a field's link, or an app's verb on the
/// item's file). One kind per key; the name is the user's.
/// </summary>
public sealed record ListAction(string Name, IReadOnlyDictionary<string, JsonNode?>? Set = null, string? Move = null, string? Open = null)
{
    public const string SetKind = "set";
    public const string MoveKind = "move";
    public const string OpenKind = "open";

    public string Kind => Set is not null ? SetKind : Move is not null ? MoveKind : Open is not null ? OpenKind : "nothing";

    /// <summary>"sets Kind to Bug", "moves to Later", "opens Issue".</summary>
    public string Described => Set is not null
        ? "sets " + ActionKeys.SpokenSet(Set)
        : Move is not null ? $"moves to {Move}" : Open is not null ? $"opens {Open}" : "does nothing";

    public JsonObject ToJson()
    {
        var node = new JsonObject { ["name"] = Name };
        if (Set is not null)
        {
            var set = new JsonObject();
            foreach (var (field, value) in Set)
            {
                set[field] = value?.DeepClone();
            }

            node[SetKind] = set;
        }
        else if (Move is not null)
        {
            node[MoveKind] = Move;
        }
        else if (Open is not null)
        {
            node[OpenKind] = Open;
        }

        return node;
    }

    /// <summary>The action a key's JSON names; null for one that is not an object with a name.</summary>
    public static ListAction? Parse(JsonNode? node)
    {
        if (node is not JsonObject obj || obj["name"] is not JsonValue nameValue || !nameValue.TryGetValue<string>(out var name) || name.Trim().Length == 0)
        {
            return null;
        }

        if (obj[SetKind] is JsonObject set)
        {
            var fields = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (field, value) in set)
            {
                fields[field] = value?.DeepClone();
            }

            return new ListAction(name.Trim(), Set: fields);
        }

        if (obj[MoveKind] is JsonValue move && move.TryGetValue<string>(out var to) && to.Trim().Length > 0)
        {
            return new ListAction(name.Trim(), Move: to.Trim());
        }

        if (obj[OpenKind] is JsonValue open && open.TryGetValue<string>(out var what) && what.Trim().Length > 0)
        {
            return new ListAction(name.Trim(), Open: what.Trim());
        }

        return null;
    }
}

/// <summary>The action keys of a list, under <c>"keys"</c> in its <c>list.json</c>, one entry per digit 1 to 9 (RP-8).</summary>
public static class ActionKeys
{
    public const string RootKey = "keys";
    public const int First = 1;
    public const int Last = 9;

    /// <summary>The digits that have an action, with it; a list without <c>list.json</c> has none.</summary>
    public static IReadOnlyDictionary<int, ListAction> Read(ItemDocument? list)
    {
        var keys = new Dictionary<int, ListAction>();
        if (list?.Extra(RootKey) is JsonObject root)
        {
            for (var digit = First; digit <= Last; digit++)
            {
                if (ListAction.Parse(root[digit.ToString()]) is { } action)
                {
                    keys[digit] = action;
                }
            }
        }

        return keys;
    }

    /// <summary>The list's details with the digit's action set, or removed with null (the whole key gone when none is left).</summary>
    public static ItemDocument With(ItemDocument list, int digit, ListAction? action)
    {
        var root = list.Extra(RootKey) is JsonObject existing ? (JsonObject)existing.DeepClone() : new JsonObject();
        if (action is null)
        {
            root.Remove(digit.ToString());
        }
        else
        {
            root[digit.ToString()] = action.ToJson();
        }

        return list.WithExtra(RootKey, root.Count == 0 ? null : root);
    }

    /// <summary>"Ctrl+1. Make it a bug", or "Ctrl+3. not set"; the full stop is NVDA's pause after the digit (AX-8.5).</summary>
    public static string Spoken(int digit, ListAction? action) => $"Ctrl+{digit}. {action?.Name ?? "not set"}";

    /// <summary>The item with the fields set (RP-8.1): a field the item lacks is added at the end, with the value's type.</summary>
    public static ItemDocument ApplySet(ItemDocument item, IReadOnlyDictionary<string, JsonNode?> set)
    {
        foreach (var (title, value) in set)
        {
            var index = item.IndexOf(title);
            if (index < 0)
            {
                item = item.WithField(-1, title, TypeFor(value), []);
                index = item.IndexOf(title);
            }

            item = item.WithValue(index, value);
        }

        return item;
    }

    /// <summary>"Kind: Bug", "Done: done", several joined with commas.</summary>
    public static string SpokenSet(IReadOnlyDictionary<string, JsonNode?> set) =>
        string.Join(", ", set.Select(pair => $"{pair.Key}: {SpokenValue(pair.Value)}"));

    /// <summary>
    /// The fields of a Set action as typed, one per line, "Field: value"; "done", "not done", "true" and "false" are
    /// checks, a number is a number, anything else text. Null for a text with no such line.
    /// </summary>
    public static IReadOnlyDictionary<string, JsonNode?>? ParseSetLines(string text)
    {
        var set = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var title = line[..colon].Trim();
            var typed = line[(colon + 1)..].Trim();
            set[title] = typed.ToLowerInvariant() switch
            {
                "done" or "true" or "checked" or "yes" => JsonValue.Create(true),
                "not done" or "false" or "not checked" or "no" => JsonValue.Create(false),
                _ when double.TryParse(typed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) && typed.Length > 0 => JsonValue.Create(number),
                _ => JsonValue.Create(typed),
            };
        }

        return set.Count == 0 ? null : set;
    }

    private static string TypeFor(JsonNode? value) => value switch
    {
        JsonValue v when v.TryGetValue<bool>(out _) => FieldTypes.Check,
        JsonValue v when v.GetValueKind() == JsonValueKind.Number => FieldTypes.Number,
        _ => FieldTypes.Text,
    };

    private static string SpokenValue(JsonNode? value) => value switch
    {
        JsonValue v when v.TryGetValue<bool>(out var flag) => flag ? "done" : "not done",
        null => "empty",
        _ => value.ToJsonString().Trim('"'),
    };

    /// <summary>
    /// Why an open action must not start the app <paramref name="verb"/> on <paramref name="path"/>; null when it may.
    /// axClaude and axTerminal start a program there (Claude Code, a shell), and a list.json may come from a list the
    /// user did not write (a shared or synced folder, a cloned repository), so a folder off this computer, a share or a
    /// mapped drive, is refused as S-5 refuses links there; the AI tools never start the two at all (McpTools), the
    /// user's own key may, on this computer (the user's decision of 2026-11-09). Asking the drive's kind touches no network.
    /// </summary>
    public static string? OpenRefusal(string verb, string path) =>
        verb is Dispatch.AxClaude or Dispatch.AxTerminal && !LocalPaths.IsLocal(path)
            ? $"Not started: {Dispatch.Of(verb).Name} would run in a folder that is not on a drive of this computer"
            : null;
}
