using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// One field of an action's form (docs/axTask/forms-framework.md AF-5.2): its title, said as an instruction; its
/// type, one of text, longtext, choice and date; the value it starts with, which may hold placeholders; a choice's
/// options; whether it must be filled; and whether it is typed unseen (AF-5.3).
/// </summary>
public sealed record ActionField(string Title, string Type, string Value, IReadOnlyList<string> Options, bool Required, bool Hidden);

/// <summary>
/// One action of a list as its configuration holds it (AF-3): its name, what it does, and the kind's own members.
/// <see cref="NotUnderstood"/> says why an action is never run (AF-3.4); such an action is still listed, and kept in
/// the file as it is. <see cref="KeyProblem"/> says why an action has no key though the file names one (AF-6.3): it
/// still runs from the menu. <see cref="Source"/> is the action as the file holds it, which an acceptance is sealed
/// over (AF-7.2), so that any change to it asks again.
/// </summary>
public sealed record ConfiguredAction(string Name, string Does, JsonNode Source)
{
    /// <summary>The key, 1 to 18: Ctrl+1 to Ctrl+9, then Ctrl+Shift+1 to Ctrl+Shift+9; null for an action run from the menu alone.</summary>
    public int? Key { get; init; }

    public string? KeyProblem { get; init; }

    public string? NotUnderstood { get; init; }

    public IReadOnlyList<ActionField> Fields { get; init; } = [];

    /// <summary>A <c>set</c> action's fields and values.</summary>
    public IReadOnlyDictionary<string, JsonNode?> Set { get; init; } = new Dictionary<string, JsonNode?>();

    /// <summary>A <c>move</c> action's list, a path from this one.</summary>
    public string To { get; init; } = string.Empty;

    /// <summary>An <c>open</c> action's field or app.</summary>
    public string Open { get; init; } = string.Empty;

    /// <summary>A <c>run</c> action's program, fixed in the file (AF-4.3).</summary>
    public string Program { get; init; } = string.Empty;

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string? Input { get; init; }

    /// <summary>Where a <c>run</c> or a <c>terminal</c> action runs: null for the list's folder, or a path.</summary>
    public string? Folder { get; init; }

    public int Seconds { get; init; } = ListActions.DefaultSeconds;

    /// <summary>What a <c>run</c> action sets once its program ended with exit code 0.</summary>
    public IReadOnlyDictionary<string, JsonNode?> Then { get; init; } = new Dictionary<string, JsonNode?>();

    /// <summary>A <c>terminal</c> action's command, the program first.</summary>
    public IReadOnlyList<string> Command { get; init; } = [];

    /// <summary>Whether the action starts a program or hands a command to a shell's window: asked about before its first run (AF-7.2).</summary>
    public bool StartsProgram => Does is ListActions.RunKind or ListActions.TerminalKind;

    /// <summary>
    /// Whether the action is said first and done at its second run, until the user has accepted it (SPEC.md TK-7a.3;
    /// bundle repairs R-72): one that moves the item or opens something. A set is one undo step in the list and is
    /// never asked about; one that starts a program has its own question (<see cref="StartsProgram"/>).
    /// </summary>
    public bool AsksFirst => Does is ListActions.MoveKind or ListActions.OpenKind;

    /// <summary>Whether the action's form has a field typed unseen: never offered to the AI tools (AF-7.5).</summary>
    public bool HasHiddenField => Fields.Any(asked => asked.Hidden);

    /// <summary>"Answer the report on GitHub", or "Answer the report on GitHub, not understood: gh is no program's name" (AF-3.4, AF-6.3).</summary>
    public string Label => NotUnderstood is { } why ? $"{Name}, not understood: {why}"
        : KeyProblem is { } key ? $"{Name}, not understood: {key}"
        : Name;

    /// <summary>
    /// One sentence that says what the action does, made from the file alone (AF-7.1): "runs gh with 3 arguments:
    /// issue, view, {Number}; gives it {form:Answer} as input; then sets Answered: {today}".
    /// </summary>
    public string Described => Does switch
    {
        ListActions.SetKind => "sets " + ActionKeys.SpokenSet(Set),
        ListActions.MoveKind => $"moves to {To}",
        ListActions.OpenKind => $"opens {Open}",
        ListActions.RunKind => $"runs {Program}"
            + (Arguments.Count == 0 ? " without arguments" : $" with {Count(Arguments.Count, "argument")}: {string.Join(", ", Arguments)}")
            + (Input is null ? string.Empty : $"; gives it {Input} as input")
            + (Folder is null ? string.Empty : $"; in the folder {Folder}")
            + (Seconds == ListActions.DefaultSeconds ? string.Empty : $"; for {Seconds} seconds at most")
            + (Then.Count == 0 ? string.Empty : "; then sets " + ActionKeys.SpokenSet(Then)),
        ListActions.TerminalKind => $"puts a command of {Count(Command.Count, "part")} into axTerminal's command field, for you to send: {string.Join(", ", Command)}"
            + (Folder is null ? string.Empty : $"; in the folder {Folder}"),
        _ => "does nothing",
    };

    private static string Count(int count, string noun) => $"{count} {(count == 1 ? noun : noun + "s")}";
}

/// <summary>
/// The actions of a list (AF-3, AF-6): the entries of its configuration's <c>"actions"</c>, read with every rule
/// that keeps an action safe to run. Five kinds: <c>set</c>, <c>move</c>, <c>open</c>, <c>run</c> and
/// <c>terminal</c>. Eighteen keys and no other. What is not understood is listed and never run, and one bad action
/// leaves the others working. Pure; the window runs what this gives it.
/// </summary>
public static class ListActions
{
    public const string SetKind = "set";
    public const string MoveKind = "move";
    public const string OpenKind = "open";
    public const string RunKind = "run";
    public const string TerminalKind = "terminal";

    public static readonly IReadOnlyList<string> Kinds = [SetKind, MoveKind, OpenKind, RunKind, TerminalKind];

    /// <summary>The value of <c>"folder"</c> that names the list's own folder, the default.</summary>
    public const string ListFolder = "list";

    public const int MaxName = 60;
    public const int KeyCount = 18;
    public const int DefaultSeconds = 60;
    public const int MaxSeconds = 600;

    /// <summary>The types a form's field may have (AF-5.2).</summary>
    public static readonly IReadOnlyList<string> FieldTypesOfAForm = [FieldTypes.Text, FieldTypes.LongText, FieldTypes.Choice, FieldTypes.Date];

    /// <summary>"Ctrl+1" to "Ctrl+9" for 1 to 9, "Ctrl+Shift+1" to "Ctrl+Shift+9" for 10 to 18.</summary>
    public static string KeyText(int key) => key <= 9 ? $"Ctrl+{key}" : $"Ctrl+Shift+{key - 9}";

    /// <summary>The key a text names, 1 to 18, case and spaces not minded; null for anything else (AF-6.1).</summary>
    public static int? ParseKey(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 3 || !parts[0].Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
            || parts[^1].Length != 1 || parts[^1][0] is < '1' or > '9')
        {
            return null;
        }

        var digit = parts[^1][0] - '0';
        return parts.Length == 2 ? digit
            : parts[1].Equals("Shift", StringComparison.OrdinalIgnoreCase) ? digit + 9
            : null;
    }

    /// <summary>
    /// What an acceptance of an action is for (AF-7.2), as one text for the seal: the list's folder whatever its
    /// case, and the whole action as the file holds it. Another list or any change to the action is another text.
    /// </summary>
    public static string MarkText(string listFolder, ConfiguredAction action) =>
        SettingsSeal.Text([Path.TrimEndingDirectorySeparator(Path.GetFullPath(listFolder)).ToLowerInvariant(), "action", action.Source.ToJsonString()]);

    /// <summary>The list's actions in the file's order; none for a list without a configuration or without actions.</summary>
    public static IReadOnlyList<ConfiguredAction> Read(ListConfig? config)
    {
        var actions = new List<ConfiguredAction>();
        foreach (var entry in config?.Actions ?? [])
        {
            if (entry is not JsonObject source)
            {
                actions.Add(new ConfiguredAction("An action", string.Empty, entry?.DeepClone() ?? JsonValue.Create(string.Empty)!) { NotUnderstood = "it is not an object of names and values" });
                continue;
            }

            var action = ReadOne(source);
            // One of its name in the list: a second of a name is listed and never run.
            if (action.NotUnderstood is null && actions.Any(known => known.Name.Equals(action.Name, StringComparison.OrdinalIgnoreCase)))
            {
                action = action with { NotUnderstood = "another action of this list has this name", Key = null };
            }

            // The first in the file has the key; a second of a key keeps its place in the menu, without a key.
            if (action.Key is { } key && actions.FirstOrDefault(known => known.Key == key) is { } holder)
            {
                action = action with { Key = null, KeyProblem = $"{KeyText(key)} is {holder.Name}'s" };
            }

            actions.Add(action);
        }

        return actions;
    }

    private static ConfiguredAction ReadOne(JsonObject source)
    {
        var name = Text(source["name"])?.Trim() ?? string.Empty;
        var does = Text(source["does"])?.Trim().ToLowerInvariant() ?? string.Empty;
        var action = new ConfiguredAction(name.Length == 0 ? "An action without a name" : name, does, source.DeepClone());
        ConfiguredAction Not(string why) => action with { NotUnderstood = why, Key = null };

        if (name.Length == 0)
        {
            return Not("it has no name");
        }

        if (name.Length > MaxName || name.Any(char.IsControl))
        {
            return Not(name.Length > MaxName ? $"its name is longer than {MaxName} characters" : "its name holds a control character");
        }

        if (source["key"] is { } keyNode)
        {
            if (Text(keyNode) is { } keyText && ParseKey(keyText) is { } key)
            {
                action = action with { Key = key };
            }
            else
            {
                action = action with { KeyProblem = "its key is none of Ctrl+1 to Ctrl+9 and Ctrl+Shift+1 to Ctrl+Shift+9" };
            }
        }

        if (!Kinds.Contains(does))
        {
            return Not(does.Length == 0 ? "it does not say what it does" : $"it does {does}, which this version does not know");
        }

        var (fields, fieldsWrong) = ReadFields(source["fields"], does);
        if (fieldsWrong is not null)
        {
            return Not(fieldsWrong);
        }

        action = action with { Fields = fields };
        string? wrong;
        switch (does)
        {
            case SetKind:
                if (ReadSet(source[SetKind], "set", out var set) is { } setWrong)
                {
                    return Not(setWrong);
                }

                if (set.Count == 0)
                {
                    return Not("it sets nothing");
                }

                action = action with { Set = set };
                wrong = Texts(action, set.Values.Select(Text).OfType<string>(), argument: false, hidden: false);
                break;
            case MoveKind:
                if (Text(source["to"])?.Trim() is not { Length: > 0 } to)
                {
                    return Not("it does not say which list it moves to (\"to\")");
                }

                action = action with { To = to };
                wrong = ActionValues.IsFixed(to) ? null : "the list it moves to holds a placeholder";
                break;
            case OpenKind:
                if (Text(source[OpenKind])?.Trim() is not { Length: > 0 } open)
                {
                    return Not("it does not say what it opens (\"open\")");
                }

                action = action with { Open = open };
                wrong = ActionValues.IsFixed(open) ? null : "what it opens holds a placeholder";
                break;
            case RunKind:
                return ReadRun(action, source, Not);
            default:
                return ReadTerminal(action, source, Not);
        }

        return wrong is null ? action : Not(wrong);
    }

    private static ConfiguredAction ReadRun(ConfiguredAction action, JsonObject source, Func<string, ConfiguredAction> not)
    {
        if (Text(source["program"])?.Trim() is not { Length: > 0 } program)
        {
            return not("it does not name its program (\"program\")");
        }

        // AF-4.3: what an action starts is read in the file; only what it is given comes from the item.
        if (!ActionValues.IsFixed(program) || program.Any(char.IsControl))
        {
            return not("its program holds a placeholder or a control character: the program is fixed in the file");
        }

        if (ReadTexts(source["arguments"], "arguments", out var arguments) is { } argumentsWrong)
        {
            return not(argumentsWrong);
        }

        string? input = null;
        if (source["input"] is { } inputNode)
        {
            if (Text(inputNode) is not { } inputText)
            {
                return not("its input is not a text");
            }

            input = inputText;
        }

        if (ReadFolder(source["folder"], out var folder) is { } folderWrong)
        {
            return not(folderWrong);
        }

        var seconds = DefaultSeconds;
        if (source["seconds"] is { } secondsNode)
        {
            if (secondsNode is not JsonValue number || !number.TryGetValue<int>(out seconds) || seconds < 1 || seconds > MaxSeconds)
            {
                return not($"its seconds are not a number from 1 to {MaxSeconds}");
            }
        }

        IReadOnlyDictionary<string, JsonNode?> then = new Dictionary<string, JsonNode?>();
        if (source["then"] is { } thenNode)
        {
            if (thenNode is not JsonObject thenObject)
            {
                return not("its \"then\" is not an object");
            }

            if (ReadSet(thenObject[SetKind], "then, set", out then) is { } thenWrong)
            {
                return not(thenWrong);
            }
        }

        action = action with { Program = program, Arguments = arguments, Input = input, Folder = folder, Seconds = seconds, Then = then };
        var wrong = Texts(action, arguments, argument: true, hidden: false)
            ?? Texts(action, then.Values.Select(Text).OfType<string>(), argument: false, hidden: false)
            ?? (input is null ? null : Texts(action, [input], argument: false, hidden: true));
        return wrong is null ? action : not(wrong);
    }

    private static ConfiguredAction ReadTerminal(ConfiguredAction action, JsonObject source, Func<string, ConfiguredAction> not)
    {
        if (ReadTexts(source["command"], "command", out var command) is { } commandWrong)
        {
            return not(commandWrong);
        }

        if (command.Count == 0 || command[0].Trim().Length == 0)
        {
            return not("its command names no program");
        }

        if (!ActionValues.IsFixed(command[0]))
        {
            return not("the program of its command holds a placeholder: the program is fixed in the file");
        }

        if (ReadFolder(source["folder"], out var folder) is { } folderWrong)
        {
            return not(folderWrong);
        }

        action = action with { Command = command, Folder = folder };
        return Texts(action, command, argument: true, hidden: false) is { } wrong ? not(wrong) : action;
    }

    /// <summary>
    /// What is wrong with the texts of an action that hold placeholders, or null: braces that do not pair, a form
    /// field the form does not have, a control character written into an argument, and a hidden field named anywhere
    /// but in the input (<paramref name="hidden"/>; AF-4.4, AF-5.3: a program's arguments can be read by other
    /// programs, a terminal's command line is shown, and a value set is written into the item's file).
    /// </summary>
    private static string? Texts(ConfiguredAction action, IEnumerable<string> texts, bool argument, bool hidden)
    {
        foreach (var text in texts)
        {
            if (argument && text.Any(char.IsControl))
            {
                return "one of its arguments holds a line break or another control character";
            }

            if (ActionValues.Names(text) is not { } names)
            {
                return "one of its texts holds a brace without its pair";
            }

            foreach (var name in names)
            {
                if (ActionValues.FormFieldOf(name) is not { } formField)
                {
                    continue;
                }

                var field = action.Fields.FirstOrDefault(known => known.Title.Equals(formField, StringComparison.OrdinalIgnoreCase));
                if (field is null)
                {
                    return $"it names {{{name}}}, and its form has no field {formField}";
                }

                if (field.Hidden && !hidden)
                {
                    return $"the hidden field {field.Title} may fill the input alone";
                }
            }
        }

        return null;
    }

    private static (IReadOnlyList<ActionField> Fields, string? Wrong) ReadFields(JsonNode? node, string does)
    {
        var fields = new List<ActionField>();
        if (node is null)
        {
            return (fields, null);
        }

        if (node is not JsonArray array)
        {
            return (fields, "its fields are not a list");
        }

        foreach (var entry in array)
        {
            if (entry is not JsonObject field || Text(field["title"])?.Trim() is not { Length: > 0 } title)
            {
                return (fields, "a field of its form has no title");
            }

            if (title.Any(char.IsControl) || fields.Any(known => known.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
            {
                return (fields, title.Any(char.IsControl) ? "a field of its form has a control character in its title" : $"its form has two fields named {title}");
            }

            var type = Text(field["type"])?.Trim() ?? FieldTypes.Text;
            if (!FieldTypesOfAForm.Contains(type))
            {
                return (fields, $"the field {title} of its form has the type {type}; a form takes text, longtext, choice and date");
            }

            var value = string.Empty;
            if (field["value"] is { } valueNode)
            {
                if (Text(valueNode) is not { } valueText || ActionValues.Names(valueText) is null)
                {
                    return (fields, $"the first value of the field {title} is not a text, or holds a brace without its pair");
                }

                value = valueText;
            }

            var options = field["options"] is JsonArray given ? given.Select(Text).OfType<string>().ToList() : [];
            if (type == FieldTypes.Choice && options.Count == 0)
            {
                return (fields, $"the choice {title} of its form has no options");
            }

            if (Flag(field["required"], true) is not { } required || Flag(field["hidden"], false) is not { } hidden)
            {
                return (fields, $"\"required\" and \"hidden\" of the field {title} are true or false");
            }

            if (hidden && (does != RunKind || type is not (FieldTypes.Text or FieldTypes.LongText)))
            {
                return (fields, $"the hidden field {title} is for a run action's input alone, as a text");
            }

            fields.Add(new ActionField(title, type, value, options, required, hidden));
        }

        return (fields, null);
    }

    private static string? ReadSet(JsonNode? node, string member, out IReadOnlyDictionary<string, JsonNode?> set)
    {
        var read = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        set = read;
        if (node is null)
        {
            return null;
        }

        if (node is not JsonObject fields)
        {
            return $"its \"{member}\" is not an object of fields and values";
        }

        foreach (var (field, value) in fields)
        {
            if (field.Trim().Length == 0 || field.Equals(FlatItem.SchemaKey, StringComparison.OrdinalIgnoreCase) || value is JsonObject or JsonArray)
            {
                return $"its \"{member}\" names a field that cannot be set, or a value that is a list or an object";
            }

            read[field] = value?.DeepClone();
        }

        return null;
    }

    private static string? ReadTexts(JsonNode? node, string member, out IReadOnlyList<string> texts)
    {
        var read = new List<string>();
        texts = read;
        if (node is null)
        {
            return null;
        }

        if (node is not JsonArray array)
        {
            return $"its {member} are not a list of texts";
        }

        foreach (var entry in array)
        {
            if (Text(entry) is not { } text)
            {
                return $"its {member} are not a list of texts";
            }

            read.Add(text);
        }

        return null;
    }

    private static string? ReadFolder(JsonNode? node, out string? folder)
    {
        folder = null;
        if (node is null)
        {
            return null;
        }

        if (Text(node)?.Trim() is not { Length: > 0 } text || !ActionValues.IsFixed(text) || text.Any(char.IsControl))
        {
            return "its folder is not a path written in the file";
        }

        folder = text.Equals(ListFolder, StringComparison.OrdinalIgnoreCase) ? null : text;
        return null;
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static bool? Flag(JsonNode? node, bool otherwise) =>
        node is null ? otherwise : node is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;
}
