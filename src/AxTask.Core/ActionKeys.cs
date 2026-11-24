using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// An action of an action key as it was until 2026-11-21 (docs/axKit/reports.md RP-8.1): fields set to values, the
/// item moved to a list, or something opened, under a name. A list's actions are <see cref="ListActions"/> now;
/// this shape is kept for one thing, the mark of an acceptance given then (<see cref="ActionKeys.MarkText"/>), so
/// that a key the user accepted before the lists were converted is not asked about again.
/// </summary>
public sealed record ListAction(string Name, IReadOnlyDictionary<string, JsonNode?>? Set = null, string? Move = null, string? Open = null)
{
    /// <summary>The action as the earlier list file held it under its digit, which the mark is made over.</summary>
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

            node["set"] = set;
        }
        else if (Move is not null)
        {
            node["move"] = Move;
        }
        else if (Open is not null)
        {
            node["open"] = Open;
        }

        return node;
    }
}

/// <summary>
/// What the actions of a list share with the action keys they grew from (RP-8, SPEC.md TK-7a): a set of fields
/// applied to an item and said, the rule that keeps an open off a folder that is not on this computer, and the mark
/// of an acceptance of a Ctrl+digit key that moves or opens, as it was sealed before 2026-11-21.
/// </summary>
public static class ActionKeys
{
    /// <summary>
    /// What an acceptance of an action key was for (TK-7a.3), as one text for the seal: the list's folder whatever
    /// its case, the digit and the action as the earlier file held it, name and all.
    /// </summary>
    public static string MarkText(string listFolder, int digit, ListAction action) =>
        SettingsSeal.Text([Path.TrimEndingDirectorySeparator(Path.GetFullPath(listFolder)).ToLowerInvariant(), digit.ToString(System.Globalization.CultureInfo.InvariantCulture), action.ToJson().ToJsonString()]);

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
    /// mapped drive, is refused as S-5 refuses links there; the AI tools start the two only after the user's yes in a window (McpTools.AsksFirst), the
    /// user's own key may, on this computer (the user's decision of 2026-11-09). Asking the drive's kind touches no network.
    /// </summary>
    public static string? OpenRefusal(string verb, string path) =>
        verb is Dispatch.AxClaude or Dispatch.AxTerminal && !LocalPaths.IsLocal(path)
            ? $"Not started: {Dispatch.Of(verb).Name} would run in a folder that is not on a drive of this computer"
            : null;
}
