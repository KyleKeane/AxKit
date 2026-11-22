using AxKit.Core;

namespace AxJSON.Core;

/// <summary>
/// Where the caret is in a document: the innermost value it stands in or on whose key it stands, the way there from
/// the root (each step a member's name, or null for an entry of a list), and the object or list that holds the value.
/// </summary>
public sealed record JsonPlace(JsonNode Node, IReadOnlyList<string?> Path, JsonNode? Parent);

/// <summary>One change to the text: what replaces which characters, where the caret stands after, and what is said.</summary>
public sealed record JsonEdit(int Start, int Length, string Text, int Caret, string Said);

/// <summary>
/// A list to choose from in place of typing: its name, its lines, the line it starts on, and the change each line
/// makes to the text.
/// </summary>
public sealed record JsonOffer(string Name, IReadOnlyList<string> Lines, int Selected, Func<int, JsonEdit> Choose);

/// <summary>
/// What axJSON offers for a file it has an options table for (SPEC.md AJ-5; docs/axTask/forms-framework.md section
/// 12): at a value that is one of a few, the values; in an object, the members it can have and does not have yet.
/// The table is the bundle's (<see cref="ListOptions"/>): a list's configuration by its <c>"format"</c>, an item of
/// a list by its <c>"schema"</c> link to that configuration. Nothing here validates: a file is never refused, and a
/// place the table does not know simply has nothing to offer. Pure; the window shows the list and applies the edit.
/// </summary>
public static class JsonOffers
{
    /// <summary>
    /// The table for a document, from the document itself: the list configuration's own for a root whose
    /// <c>"format"</c> is that format; for a root with a <c>"schema"</c> link, the table of an item, made from the
    /// configuration the link names (<paramref name="readLinked"/> gives that file's text, or null when it is not
    /// there; only a plain file name is followed, never a path). Null for any other document.
    /// </summary>
    public static OptionsTable? TableOf(JsonNode? root, Func<string, string?> readLinked)
    {
        if (root is not { Kind: TokenKind.OpenObject })
        {
            return null;
        }

        if (Member(root, ListConfig.FormatKey) is { Kind: TokenKind.String } format)
        {
            return format.Value == ListConfig.Format ? ListOptions.Table : null;
        }

        if (Member(root, FlatItem.SchemaKey) is { Kind: TokenKind.String } link
            && link.Value.Length > 0 && link.Value == Path.GetFileName(link.Value) && link.Value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && readLinked(link.Value) is { } text && ListConfig.FormatOf(text) == ListConfig.Format)
        {
            try
            {
                return ListOptions.ForItem(ListConfig.Parse(text));
            }
            catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or InvalidOperationException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>The innermost value at the caret, counting a member's key as the member's; null for an empty document.</summary>
    public static JsonPlace? At(JsonNode? root, int caret)
    {
        if (root is null)
        {
            return null;
        }

        var path = new List<string?>();
        var node = root;
        JsonNode? parent = null;
        while (node.IsContainer)
        {
            // The last child that holds the caret: after a value's last character still counts as that value.
            var inside = node.Children.LastOrDefault(child => caret >= child.KeyStart && caret <= child.End);
            if (inside is null)
            {
                break;
            }

            path.Add(node.Kind == TokenKind.OpenObject ? inside.Key : null);
            parent = node;
            node = inside;
        }

        return new JsonPlace(node, path, parent);
    }

    /// <summary>
    /// The values the member at the caret can take, to choose one in place of what stands there; or the one
    /// sentence that says why there is no such list here.
    /// </summary>
    public static (JsonOffer? Offer, string? Said) Values(string text, int caret, Func<string, string?> readLinked)
    {
        var (node, member, said) = MemberWithValues(text, caret, readLinked);
        if (node is null || member is null)
        {
            return (null, said);
        }

        var now = member.Values.ToList().FindIndex(value => value == node.Value);
        return (new JsonOffer($"{node.Key}: {member.What}", member.Values, Math.Max(0, now), chosen => Written(node, member, chosen, $"{node.Key}: {member.Values[chosen]}")), null);
    }

    /// <summary>
    /// The value of the member at the caret changed in line to the next of its values (<paramref name="direction"/>
    /// 1) or the one before (-1), said with its place among them ("type: date, 4 of 8"); a value that is none of
    /// them goes to the first, or from the other side to the last. At an end nothing changes: no edit, the value
    /// and that it is the last or the first, and <c>Edge</c> for the window's sound. Where there are no values, the
    /// sentence of <see cref="Values"/> and no edge.
    /// </summary>
    public static (JsonEdit? Edit, string Said, bool Edge) Step(string text, int caret, int direction, Func<string, string?> readLinked)
    {
        var (node, member, said) = MemberWithValues(text, caret, readLinked);
        if (node is null || member is null)
        {
            return (null, said ?? string.Empty, false);
        }

        var values = member.Values;
        var now = values.ToList().FindIndex(value => value == node.Value);
        var next = now < 0 ? (direction > 0 ? 0 : values.Count - 1) : now + direction;
        if (next < 0 || next >= values.Count)
        {
            return (null, $"{node.Key}: {values[now]}, the {(direction > 0 ? "last" : "first")} of {values.Count}", true);
        }

        return (Written(node, member, next, $"{node.Key}: {values[next]}, {next + 1} of {values.Count}"), string.Empty, false);
    }

    /// <summary>One of a member's values written in place of the value that stands there, quoted as it needs, the caret on it.</summary>
    private static JsonEdit Written(JsonNode node, OptionMember member, int chosen, string said)
    {
        var value = member.Values[chosen];
        return new JsonEdit(node.Start, node.End - node.Start, member.Quoted ? JsonScanner.Quote(value) : value, node.Start, said);
    }

    /// <summary>The member at the caret and its values, or the one sentence that says why there are none here.</summary>
    private static (JsonNode? Node, OptionMember? Member, string? Said) MemberWithValues(string text, int caret, Func<string, string?> readLinked)
    {
        var (tree, table, said) = Read(text, readLinked);
        if (table is null || tree.Root is null)
        {
            return (null, null, said);
        }

        var place = At(tree.Root, caret)!;
        var node = place.Node;
        if (node.IsContainer || node.Key is null || place.Parent is not { Kind: TokenKind.OpenObject })
        {
            return (null, null, "No list of values here: put the caret on a member that has one, such as type or does");
        }

        if (table.KindAt(place.Path.Take(place.Path.Count - 1).ToList())?.Member(node.Key) is not { } member)
        {
            return (null, null, $"{node.Key} is not a member axJSON has options for");
        }

        if (member.Values.Count == 0)
        {
            return (null, null, $"{node.Key} takes any value. {member.What}");
        }

        return (node, member, null);
    }

    /// <summary>
    /// The members the object at the caret can have and does not have yet, each with what it is, to add one with its
    /// first value at the object's end; or the one sentence that says why there is no such list here.
    /// </summary>
    public static (JsonOffer? Offer, string? Said) Members(string text, int caret, Func<string, string?> readLinked)
    {
        var (tree, table, said) = Read(text, readLinked);
        if (table is null || tree.Root is null)
        {
            return (null, said);
        }

        // The object the caret is in: the value itself when it is one, the object that holds it otherwise.
        var place = At(tree.Root, caret)!;
        var holder = place.Node;
        IReadOnlyList<string?> path = place.Path;
        if (holder.Kind != TokenKind.OpenObject)
        {
            if (place.Parent is not { Kind: TokenKind.OpenObject } parent)
            {
                return (null, "No members to add here: put the caret in an object");
            }

            holder = parent;
            path = place.Path.Take(place.Path.Count - 1).ToList();
        }

        if (table.KindAt(path) is not { } kind)
        {
            return (null, "axJSON has no list of members for this object");
        }

        var lacking = kind.Members.Where(member => Member(holder, member.Name) is null).ToList();
        if (lacking.Count == 0)
        {
            return (null, $"{kind.What} has every member it can have");
        }

        return (new JsonOffer($"Add a member to {Lower(kind.What)}", lacking.Select(member => $"{member.Name}: {member.What}").ToList(), 0,
            chosen => Added(text, holder, lacking[chosen])), null);
    }

    /// <summary>
    /// The member written as the object's last, on a line of its own, indented as the member before it (two spaces
    /// deeper than the object's line when it is the first), with the comma the member before it then needs; the
    /// caret goes to the new value.
    /// </summary>
    private static JsonEdit Added(string text, JsonNode holder, OptionMember member)
    {
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var close = holder.End - 1;
        var name = JsonScanner.Quote(member.Name);
        var written = $"{name}: {member.First}";

        // The edit, with the caret on the new member's value.
        JsonEdit Edit(int from, int length, string inserted) =>
            new(from, length, inserted, from + inserted.IndexOf(written, StringComparison.Ordinal) + name.Length + 2, Said(member));

        if (holder.Children.Count == 0)
        {
            var indent = IndentOf(text, holder.Start);
            return Edit(holder.Start, holder.End - holder.Start, "{" + newline + indent + "  " + written + newline + indent + "}");
        }

        // From the last member's end to the closing brace: white space, and a trailing comma if the text has one.
        var last = holder.Children[^1];
        var between = text[last.End..close];
        if (between.Trim() is not ("" or ","))
        {
            // A comment stands there: nothing is guessed about it.
            return new JsonEdit(last.End, 0, string.Empty, last.End, $"{member.Name} was not added: a comment stands before the object's closing brace. Add it by hand");
        }

        if (!text[holder.Start..holder.End].Contains('\n'))
        {
            // An object on one line stays on one line.
            return Edit(last.End, close - last.End, ", " + written + (between.EndsWith(' ') ? " " : string.Empty));
        }

        var lineIndent = between.Contains('\n') ? between[(between.LastIndexOf('\n') + 1)..] : string.Empty;
        return Edit(last.End, close - last.End, "," + newline + IndentOf(text, last.KeyStart) + written + newline + lineIndent);
    }

    private static string Said(OptionMember member) => $"Added {member.Name}: {member.What}";

    /// <summary>The white space a line starts with, for the line that holds a position.</summary>
    private static string IndentOf(string text, int position)
    {
        var start = text.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
        var end = start;
        while (end < text.Length && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text[start..end];
    }

    private static (JsonTree Tree, OptionsTable? Table, string? Said) Read(string text, Func<string, string?> readLinked)
    {
        var tree = JsonParser.Parse(text);
        if (tree.Problem is { } problem)
        {
            return (tree, null, $"Not valid JSON: line {JsonOutline.Line(text, problem.Start)}, {problem.Message}");
        }

        var table = TableOf(tree.Root, readLinked);
        return (tree, table, table is null ? "axJSON has no options for this file: it has them for a list's configuration and for an item of a list" : null);
    }

    private static JsonNode? Member(JsonNode holder, string name) => holder.Children.FirstOrDefault(child => child.Key == name);

    private static string Lower(string what) => what.Length == 0 ? what : char.ToLowerInvariant(what[0]) + what[1..];
}
