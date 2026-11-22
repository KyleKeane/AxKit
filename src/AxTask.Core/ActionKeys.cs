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

    /// <summary>
    /// The action as a list's configuration holds it (docs/axTask/forms-framework.md section 3): its name, its key,
    /// what it does, and what it sets, where it moves to or what it opens.
    /// </summary>
    public JsonObject ToAction(int digit)
    {
        var node = new JsonObject { ["name"] = Name, ["key"] = $"Ctrl+{digit}", ["does"] = Kind };
        if (Set is not null)
        {
            node[SetKind] = ToJson()[SetKind]!.DeepClone();
        }
        else if (Move is not null)
        {
            node["to"] = Move;
        }
        else if (Open is not null)
        {
            node[OpenKind] = Open;
        }

        return node;
    }

    /// <summary>
    /// The action of a Ctrl+number key that an entry of a configuration's <c>"actions"</c> names, with its digit;
    /// null for an entry that is not one: another key, another kind (the kinds that run a program come with the
    /// actions' own code), or a kind without what it needs.
    /// </summary>
    public static (int Digit, ListAction Action)? FromAction(JsonNode? node)
    {
        if (node is not JsonObject obj || FieldTypes.TextOf(obj["name"]).Trim() is not { Length: > 0 } name
            || FieldTypes.TextOf(obj["key"]).Trim() is not { Length: 6 } key || !key.StartsWith("Ctrl+", StringComparison.OrdinalIgnoreCase)
            || key[5] is < '1' or > '9')
        {
            return null;
        }

        var digit = key[5] - '0';
        return FieldTypes.TextOf(obj["does"]).Trim().ToLowerInvariant() switch
        {
            SetKind when obj[SetKind] is JsonObject => (digit, Parse(new JsonObject { ["name"] = name, [SetKind] = obj[SetKind]!.DeepClone() })!),
            MoveKind when FieldTypes.TextOf(obj["to"]).Trim() is { Length: > 0 } to => (digit, new ListAction(name, Move: to)),
            OpenKind when FieldTypes.TextOf(obj[OpenKind]).Trim() is { Length: > 0 } open => (digit, new ListAction(name, Open: open)),
            _ => null,
        };
    }

    /// <summary>The action in the shape an acceptance is sealed over (<see cref="ActionKeys.MarkText"/>), the action keys' own until 2026-11-21; null for one that is not an object with a name.</summary>
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

/// <summary>
/// The action keys of a list (RP-8): the entries of its configuration's <c>"actions"</c> that are on Ctrl+1 to
/// Ctrl+9 and set fields, move the item or open something (docs/axTask/forms-framework.md section 3; under
/// <c>"keys"</c> in the earlier format, which the conversion turns into these). The other kinds of action and the
/// keys with Shift come with the actions' own code.
/// </summary>
public static class ActionKeys
{
    public const int First = 1;
    public const int Last = 9;

    /// <summary>The digits that have an action, with it, the first entry of a key counting; a list without a configuration has none.</summary>
    public static IReadOnlyDictionary<int, ListAction> Read(ListConfig? list)
    {
        var keys = new Dictionary<int, ListAction>();
        foreach (var entry in list?.Actions ?? [])
        {
            if (ListAction.FromAction(entry) is { } read)
            {
                keys.TryAdd(read.Digit, read.Action);
            }
        }

        return keys;
    }

    /// <summary>
    /// The configuration with the digit's action set, or removed with null: in the place of the entry the key had,
    /// at the end for a key that had none. Every other entry of <c>"actions"</c> stays as it is.
    /// </summary>
    public static ListConfig With(ListConfig list, int digit, ListAction? action)
    {
        var actions = new JsonArray();
        var placed = false;
        foreach (var entry in list.Actions ?? [])
        {
            if (ListAction.FromAction(entry) is { } read && read.Digit == digit)
            {
                if (action is not null && !placed)
                {
                    actions.Add(action.ToAction(digit));
                    placed = true;
                }

                continue;
            }

            actions.Add(entry?.DeepClone());
        }

        if (action is not null && !placed)
        {
            actions.Add(action.ToAction(digit));
        }

        return list.WithExtra(ListConfig.ActionsKey, actions);
    }

    /// <summary>
    /// Whether an action is of the kinds that are asked about before their first run on this install (TK-7a.3; bundle
    /// repairs.md R-72): one that moves the item or opens something. A set of fields is one undo step in the list and
    /// is never asked about.
    /// </summary>
    public static bool AsksFirst(ListAction action) => action.Move is not null || action.Open is not null;

    /// <summary>What the window says at the first press of such a key whose action this install neither wrote nor accepted (TK-7a.3): "Ctrl+2 in this list now moves to Later. Press it again to do it."</summary>
    public static string AskText(int digit, ListAction action) => $"Ctrl+{digit} in this list now {action.Described}. Press it again to do it.";

    /// <summary>
    /// What an acceptance is for (TK-7a.3), as one text for the seal: the list's folder whatever its case, the digit
    /// and the action as the file holds it, name and all. Another list, another key or another action is another text.
    /// </summary>
    public static string MarkText(string listFolder, int digit, ListAction action) =>
        SettingsSeal.Text([Path.TrimEndingDirectorySeparator(Path.GetFullPath(listFolder)).ToLowerInvariant(), digit.ToString(System.Globalization.CultureInfo.InvariantCulture), action.ToJson().ToJsonString()]);

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

    // ---- the form of one key (RP-8.2; one form since 2026-11-14, bundle AX-8.10) ----

    /// <summary>What a key can do, as the form's list shows it, in the order of <see cref="KindIndex"/>.</summary>
    public static readonly IReadOnlyList<string> KindNames = ["Set fields to values", "Move the item to a list", "Open a field's web address, or an app"];

    /// <summary>The line of <see cref="KindNames"/> an action is; the first for a key not set.</summary>
    public static int KindIndex(ListAction? action) => action?.Kind switch { ListAction.MoveKind => 1, ListAction.OpenKind => 2, _ => 0 };

    /// <summary>What the form's last field holds for an action: its fields as "Field: value" lines, the list it moves to, or what it opens; empty for a key not set.</summary>
    public static string ValueText(ListAction? action) =>
        action?.Set is { } set ? string.Join("\n", set.Select(pair => pair.Key + ": " + (FieldTypes.TextOf(pair.Value) is { Length: > 0 } text ? text : SpokenValue(pair.Value))))
        : action?.Move is { } move ? move.Replace("../", string.Empty, StringComparison.Ordinal)
        : action?.Open ?? string.Empty;

    /// <summary>
    /// What the form's fields make: the action, or none for an empty name, which removes the key; or why they are
    /// refused, and whether that is about the last field (the name's otherwise). <paramref name="listExists"/> says
    /// whether the top folder holds a list of a name.
    /// </summary>
    public static (ListAction? Action, string? Refused, bool InValue) FromForm(string name, int kind, string value, Func<string, bool> listExists)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            return (null, null, false);
        }

        switch (kind)
        {
            case 0:
                return ParseSetLines(value) is { } set
                    ? (new ListAction(name, Set: set), null, false)
                    : (null, "Type at least one line as Field: value", true);
            case 1:
                var list = value.Trim();
                return list.Length == 0 ? (null, "Type the name of a list in the top folder", true)
                    : list.Contains('\n') || !listExists(list) ? (null, $"No list called {list.ReplaceLineEndings(" ")} in the top folder", true)
                    : (new ListAction(name, Move: "../" + list), null, false);
            default:
                var open = value.Trim();
                return open.Length == 0 || open.Contains('\n')
                    ? (null, "Type one field's title or one app", true)
                    : (new ListAction(name, Open: open), null, false);
        }
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
