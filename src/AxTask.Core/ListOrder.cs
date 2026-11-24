using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// A list's own order (SPEC.md TK-4.9; the maintainer's asks: "editable lists like axtask should have the ability to be
/// reordered", settled on 2026-11-21 as Ctrl+Shift+Up and Ctrl+Shift+Down, and "a setting to let the user choose to
/// add things at the top or bottom of a list"). The configuration's member <see cref="OrderKey"/> names the items' files
/// (their names without ".json") in the list's order; <see cref="NewItemsKey"/> says where an item it does not name
/// goes, <c>"bottom"</c> (the default) or <c>"top"</c>. A list without the member shows its items by name, as before.
/// Pure, tested in <c>tests/AxTask.Tests/ListOrderTests</c>.
/// </summary>
public static class ListOrder
{
    public const string OrderKey = "order";
    public const string NewItemsKey = "new items";
    public const string Top = "top";
    public const string Bottom = "bottom";

    /// <summary>The names the configuration gives in order; empty without the member, a name not a string passed over.</summary>
    public static IReadOnlyList<string> Of(ListConfig? config) =>
        config?.Extra(OrderKey) is JsonArray names
            ? [.. names.OfType<JsonValue>().Select(value => value.TryGetValue<string>(out var name) ? name : null).OfType<string>()]
            : [];

    /// <summary>Whether a new item goes to the top: the member says <see cref="Top"/>.</summary>
    public static bool NewAtTop(ListConfig? config) =>
        config?.Extra(NewItemsKey) is JsonValue value && value.TryGetValue<string>(out var where) && where.Equals(Top, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The items in the list's order: those the order names in its order, and the others, in the order given (by name),
    /// after them, or before them when new items go to the top. Names are matched without case; a name the order gives
    /// twice counts where it first stands.
    /// </summary>
    public static List<T> Arrange<T>(IReadOnlyList<T> items, Func<T, string> nameOf, IReadOnlyList<string> order, bool newAtTop)
    {
        if (order.Count == 0)
        {
            return [.. items];
        }

        var place = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in order)
        {
            place.TryAdd(name, place.Count);
        }

        var named = items.Where(item => place.ContainsKey(nameOf(item))).OrderBy(item => place[nameOf(item)]).ToList();
        var others = items.Where(item => !place.ContainsKey(nameOf(item))).ToList();
        return newAtTop ? [.. others, .. named] : [.. named, .. others];
    }

    /// <summary>
    /// The names after a move of the one at <paramref name="from"/> (TK-4.9): <paramref name="by"/> places up (negative)
    /// or down, or to the top (<see cref="int.MinValue"/>) or the bottom (<see cref="int.MaxValue"/>); null when it
    /// cannot move that way (already first or last). The new place is the second value.
    /// </summary>
    public static (List<string> Names, int At)? Moved(IReadOnlyList<string> shown, int from, int by)
    {
        if (from < 0 || from >= shown.Count)
        {
            return null;
        }

        var to = by switch
        {
            int.MinValue => 0,
            int.MaxValue => shown.Count - 1,
            _ => Math.Clamp(from + by, 0, shown.Count - 1),
        };
        if (to == from)
        {
            return null;
        }

        var names = shown.ToList();
        var name = names[from];
        names.RemoveAt(from);
        names.Insert(to, name);
        return (names, to);
    }

    /// <summary>
    /// A move among the items shown when some are not (done tasks hidden): the item moves among <paramref name="shown"/>
    /// as <see cref="Moved"/> says, and in <paramref name="all"/> it goes right after the shown item now before it, or
    /// right before the one now after it at the top. The whole order, and the place among those shown; null as for
    /// <see cref="Moved"/>.
    /// </summary>
    public static (List<string> Names, int At)? MovedAmong(IReadOnlyList<string> all, IReadOnlyList<string> shown, int from, int by)
    {
        if (Moved(shown, from, by) is not { } moved)
        {
            return null;
        }

        var name = shown[from];
        var names = all.Where(other => !other.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        int IndexOf(string wanted) => names.FindIndex(other => other.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        var at = moved.At > 0 ? IndexOf(moved.Names[moved.At - 1]) + 1 : Math.Max(0, IndexOf(moved.Names[1]));
        names.Insert(at, name);
        return (names, moved.At);
    }

    /// <summary>The configuration with the order written.</summary>
    public static ListConfig WithOrder(ListConfig config, IReadOnlyList<string> names) =>
        config.WithExtra(OrderKey, new JsonArray([.. names.Select(name => (JsonNode)JsonValue.Create(name))]));

    /// <summary>The configuration with where new items go written.</summary>
    public static ListConfig WithNewItems(ListConfig config, bool top) =>
        config.WithExtra(NewItemsKey, JsonValue.Create(top ? Top : Bottom));

    /// <summary>
    /// The order after an item's file was renamed (TK-4.9): the old name replaced where it stood, so that a renamed item
    /// keeps its place; null when the order does not name it (nothing to write).
    /// </summary>
    public static List<string>? Renamed(IReadOnlyList<string> order, string oldName, string newName)
    {
        var at = order.ToList().FindIndex(name => name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
        if (at < 0)
        {
            return null;
        }

        var names = order.ToList();
        names[at] = newName;
        return names;
    }

    /// <summary>
    /// The order with a new item's name at the end where new items go (TK-4.10): for a list without an order, null when
    /// new items go to the bottom (the item is shown by its name with the rest, as before), and the names as shown with
    /// the new one first when they go to the top, so that it stands there.
    /// </summary>
    public static List<string>? Added(IReadOnlyList<string> order, IReadOnlyList<string> shown, string name, bool atTop) =>
        order.Count == 0
            ? atTop ? [name, .. shown] : null
            : atTop ? [name, .. order] : [.. order, name];

    /// <summary>The words of a move: "Moved up: Fix the tap, 3 of 12", "Moved to the top: ...".</summary>
    public static string Said(int by, string title, int at, int count) =>
        $"{(by switch { int.MinValue => "Moved to the top", int.MaxValue => "Moved to the bottom", < 0 => "Moved up", _ => "Moved down" })}: {title}, {at + 1} of {count}";
}
