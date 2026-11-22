using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>
/// One member an object of a kind can have (docs/axTask/forms-framework.md AF-12.1): its name, one line that says
/// what it is, the values it can take where they are a few (<see cref="Values"/>, written in quotes when
/// <see cref="Quoted"/>), the JSON text a new member starts with, and the kind of the object it holds
/// (<see cref="Kind"/>) or of each entry of the list it holds (<see cref="Each"/>).
/// </summary>
public sealed record OptionMember(string Name, string What, string First, IReadOnlyList<string>? Choices = null, bool Quoted = true, string? Kind = null, string? Each = null)
{
    public IReadOnlyList<string> Values => Choices ?? [];
}

/// <summary>A kind of object and the members it can have, in the order they are written.</summary>
public sealed record OptionKind(string Name, string What, IReadOnlyList<OptionMember> Members)
{
    public OptionMember? Member(string name) => Members.FirstOrDefault(member => member.Name.Equals(name, StringComparison.Ordinal));
}

/// <summary>
/// The options of a file format: every kind of object the file can hold, the root's first. A list of names and
/// choices and nothing that validates (JSON Schema stays a non-goal of axJSON, its SPEC G3).
/// </summary>
public sealed record OptionsTable(string Format, IReadOnlyList<OptionKind> Kinds)
{
    public OptionKind Root => Kinds[0];

    public OptionKind? Kind(string? name) => name is null ? null : Kinds.FirstOrDefault(kind => kind.Name.Equals(name, StringComparison.Ordinal));

    /// <summary>
    /// The kind of the object at a path from the root, each step a member's name or, for an entry of a list, null;
    /// null where the table knows no kind (a member it does not name, a value that is no object).
    /// </summary>
    public OptionKind? KindAt(IReadOnlyList<string?> path)
    {
        var kind = Root;
        for (var i = 0; i < path.Count; i++)
        {
            if (path[i] is not { } name || kind.Member(name) is not { } member)
            {
                return null;
            }

            if (member.Each is not null)
            {
                // A list's entries: the next step must be the entry itself.
                if (i + 1 >= path.Count || path[i + 1] is not null || Kind(member.Each) is not { } each)
                {
                    return null;
                }

                kind = each;
                i++;
            }
            else if (Kind(member.Kind) is { } held)
            {
                kind = held;
            }
            else
            {
                return null;
            }
        }

        return kind;
    }

    /// <summary>The table as the small JSON file of AF-12.2, for a program that is not axKit: the kinds by name, each with its members in order.</summary>
    public string ToJson()
    {
        var kinds = new JsonObject();
        foreach (var kind in Kinds)
        {
            var members = new JsonArray();
            foreach (var member in kind.Members)
            {
                var entry = new JsonObject { ["name"] = member.Name, ["what"] = member.What, ["first"] = JsonNode.Parse(member.First) };
                if (member.Values.Count > 0)
                {
                    entry["values"] = new JsonArray([.. member.Values.Select(value => member.Quoted ? JsonValue.Create(value) : JsonNode.Parse(value))]);
                }

                if (member.Kind is not null)
                {
                    entry["kind"] = member.Kind;
                }

                if (member.Each is not null)
                {
                    entry["each"] = member.Each;
                }

                members.Add(entry);
            }

            kinds[kind.Name] = new JsonObject { ["what"] = kind.What, ["members"] = members };
        }

        var root = new JsonObject { ["format"] = "axkit-options 1", ["for"] = Format, ["root"] = Root.Name, ["kinds"] = kinds };
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).ReplaceLineEndings("\r\n") + "\r\n";
    }
}

/// <summary>
/// The options of a list's configuration, <c>list.json</c> of the format <c>axkit-list 2</c>
/// (docs/axTask/forms-framework.md section 12): every member the configuration, a field of the list's items, an
/// action and a field of an action's form can have, with what each is and, where a value is one of a few, the
/// values. One table, written once: axJSON offers a configuration's members and values from it, the file
/// <see cref="FileName"/> under <c>docs/axKit</c> is written from it for other programs, and tests hold it to what
/// the readers take (<see cref="ListConfig"/> here, the actions' reader in axTask).
/// </summary>
public static class ListOptions
{
    /// <summary>The file the table is written out as (AF-12.2).</summary>
    public const string FileName = "axkit-list 2.options.json";

    private static readonly string[] Flags = ["true", "false"];

    /// <summary>The eighteen keys an action can have (AF-6.1), as the file writes them.</summary>
    public static IReadOnlyList<string> ActionKeys { get; } =
        [.. Enumerable.Range(1, 9).Select(digit => $"Ctrl+{digit}"), .. Enumerable.Range(1, 9).Select(digit => $"Ctrl+Shift+{digit}")];

    /// <summary>What an action can do (AF-3.2).</summary>
    public static IReadOnlyList<string> ActionKinds { get; } = ["set", "move", "open", "run", "terminal"];

    /// <summary>The types a field of an action's form can have (AF-5.2).</summary>
    public static IReadOnlyList<string> FormFieldTypes { get; } = [FieldTypes.Text, FieldTypes.LongText, FieldTypes.Choice, FieldTypes.Date];

    /// <summary>
    /// The options of an item of a list (AF-12.4): the link, the Title and the fields its list's configuration
    /// names, each with its type said, the value a new item starts with, and its choices where the type has them
    /// (a choice's options; done or not for a check). The same link axTask reads an item through.
    /// </summary>
    public static OptionsTable ForItem(ListConfig config)
    {
        var members = new List<OptionMember>
        {
            new(FlatItem.SchemaKey, "The link to the list's configuration, which says what the item's fields are", "\"" + ListConfig.FileName + "\""),
            new(ItemDocument.TitleField, "The item's title", "\"\""),
        };
        foreach (var field in config.ItemFields)
        {
            var first = field.Value?.ToJsonString() ?? "\"\"";
            members.Add(field.Type switch
            {
                FieldTypes.Choice => new OptionMember(field.Title, "A choice of the list's options", first, field.Options),
                FieldTypes.Check => new OptionMember(field.Title, "A check: true or false", first, Flags, Quoted: false),
                _ => new OptionMember(field.Title, $"A field of the type {field.Type}", first),
            });
        }

        return new OptionsTable("an item of a list", [new OptionKind("item", "An item of a list", members)]);
    }

    public static OptionsTable Table { get; } = new(ListConfig.Format,
    [
        new OptionKind("list", "The configuration of a list",
        [
            new OptionMember("format", "Says that this file is a list's configuration, and of which version", "\"" + ListConfig.Format + "\"", [ListConfig.Format]),
            new OptionMember("details", "The list's own values: its notes and its section", "{}", Kind: "details"),
            new OptionMember("item fields", "The fields every item of the list has, in the order they are shown", "[]", Each: "field"),
            new OptionMember("actions", "What can be done with an item of the list, by a key or from the menu", "[]", Each: "action"),
        ]),
        new OptionKind("details", "The list's own values",
        [
            new OptionMember("Notes", "Notes about the list", "\"\""),
            new OptionMember("Section", "The section of the top level the list stands in", "\"\""),
        ]),
        new OptionKind("field", "A field of the list's items",
        [
            new OptionMember("title", "The field's name, as it is shown and said", "\"\""),
            new OptionMember("type", "What the field holds", "\"text\"", FieldTypes.All),
            new OptionMember("value", "The value a new item starts with", "\"\""),
            new OptionMember("options", "The values to choose from, for the type choice", "[]"),
        ]),
        new OptionKind("action", "An action of the list",
        [
            new OptionMember("name", "The action's name, as the menu shows it", "\"\""),
            new OptionMember("key", "The key that runs it; without one it runs from the menu", "\"Ctrl+1\"", ActionKeys),
            new OptionMember("does", "What the action does", "\"set\"", ActionKinds),
            new OptionMember("fields", "A form that asks for values before the action runs", "[]", Each: "form field"),
            new OptionMember("set", "For does set: the fields it writes, each with its value", "{}"),
            new OptionMember("to", "For does move: the list the item moves to, as a path from this list", "\"../Later\""),
            new OptionMember("open", "For does open: a field that holds a web address, or an app such as json", "\"\""),
            new OptionMember("program", "For does run: the program, by its name or its full path", "\"\""),
            new OptionMember("arguments", "For does run: the program's arguments, each one text", "[]"),
            new OptionMember("input", "For does run: a text the program reads as its input", "\"\""),
            new OptionMember("command", "For does terminal: the command for axTerminal's field, the program first, each part one text", "[]"),
            new OptionMember("folder", "For does run and terminal: where it runs: list, the list's folder, or a path", "\"list\"", ["list"]),
            new OptionMember("seconds", "For does run: how long the program may take, 1 to 600", "60", Quoted: false),
            new OptionMember("then", "For does run: what happens when the program ends without an error", "{}", Kind: "then"),
        ]),
        new OptionKind("form field", "A field of an action's form",
        [
            new OptionMember("title", "The field's name, written as an instruction: it is what is read as the box's name", "\"\""),
            new OptionMember("type", "The kind of box", "\"text\"", FormFieldTypes),
            new OptionMember("value", "What the box starts with", "\"\""),
            new OptionMember("options", "The values to choose from, for the type choice", "[]"),
            new OptionMember("required", "Whether the action waits until the field holds something", "true", Flags, Quoted: false),
            new OptionMember("hidden", "Whether the field is typed unseen; such a field may fill the input alone", "false", Flags, Quoted: false),
        ]),
        new OptionKind("then", "What follows a program that ended without an error",
        [
            new OptionMember("set", "The fields it writes, each with its value", "{}"),
        ]),
    ]);
}
