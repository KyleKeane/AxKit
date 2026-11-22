using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// What an action's placeholders are filled from (docs/axTask/forms-framework.md AF-4.1): the item selected, as its
/// file holds it, with its list's configuration; the item's file and the list's folder; what was typed into the
/// action's form, by the form fields' titles; and the day.
/// </summary>
public sealed record ActionContext(FlatItem Item, ListConfig? Config, string File, string ListFolder, IReadOnlyDictionary<string, string> Form, DateOnly Today);

/// <summary>A <c>run</c> action filled for an item: the program as the file names it, each argument as one text, the input, the folder it runs in, its time limit, and what is set after it ended well.</summary>
public sealed record FilledRun(string Program, IReadOnlyList<string> Arguments, string? Input, string Folder, int Seconds, IReadOnlyDictionary<string, JsonNode?> Then);

/// <summary>A <c>terminal</c> action filled for an item: the command's elements, the program first, and the folder.</summary>
public sealed record FilledTerminal(IReadOnlyList<string> Command, string Folder);

/// <summary>
/// The values of an action (AF-4): the names in braces of its texts, filled when the action runs. <c>{Title}</c> and
/// <c>{&lt;a field's title&gt;}</c> are the selected item's values as text (<c>{field:&lt;title&gt;}</c> says the
/// same for a field whose title is one of the four names below); <c>{form:&lt;title&gt;}</c> is what was typed in
/// the action's form; <c>{file}</c>, <c>{name}</c>, <c>{folder}</c> and <c>{today}</c> are the item's file, its
/// name without <c>.json</c>, the list's folder and the date. <c>{{</c> and <c>}}</c> are braces themselves.
///
/// The first safety rule of the actions is kept here (AF-4.2): a value fills the one text it stands in, and that
/// text stays one argument. Nothing here joins texts into a command line or splits one. A value with a line break
/// or another control character is refused where the text is an argument (AF-4.4); the input takes any text. A name
/// neither the item nor the form has refuses the whole action before anything happens. Pure.
/// </summary>
public static class ActionValues
{
    /// <summary>The names every item has, whatever its fields: written in lower case, exactly.</summary>
    private static readonly string[] BuiltIn = ["file", "name", "folder", "today"];

    private const string FormPrefix = "form:";
    private const string FieldPrefix = "field:";

    /// <summary>
    /// The names in braces of a text, in their order, as written ("Number", "form:Answer"); null for a text whose
    /// braces do not pair (a single <c>{</c> or <c>}</c>, a name that is empty or holds a brace), which no action
    /// may hold.
    /// </summary>
    public static IReadOnlyList<string>? Names(string text)
    {
        var names = new List<string>();
        return Walk(text, name => { names.Add(name); return string.Empty; }, out _) is null ? null : names;
    }

    /// <summary>Whether a text holds no placeholder at all and pairs its braces: what a program's name must be (AF-4.3).</summary>
    public static bool IsFixed(string text) => Names(text) is { Count: 0 };

    /// <summary>The form field a name asks for, <c>{form:Answer}</c> gives Answer; null for a name that is no form field's.</summary>
    public static string? FormFieldOf(string name) =>
        name.StartsWith(FormPrefix, StringComparison.OrdinalIgnoreCase) ? name[FormPrefix.Length..].Trim() : null;

    /// <summary>
    /// A text with its names filled. <paramref name="argument"/>: the text is an argument or a part of a command,
    /// where a value with a control character is refused. The refusal is a whole sentence for the user.
    /// </summary>
    public static (string? Text, string? Refused) Fill(string text, ActionContext context, string actionName, bool argument)
    {
        string? refused = null;
        var filled = Walk(text, name =>
        {
            var (value, why) = ValueOf(name, context, actionName);
            if (why is not null)
            {
                refused ??= why;
                return string.Empty;
            }

            var filledIn = value ?? string.Empty;
            if (argument && filledIn.Any(char.IsControl))
            {
                var what = filledIn.First(char.IsControl) is '\r' or '\n' ? "a line break" : "a control character";
                refused ??= FormFieldOf(name) is { } typed
                    ? $"What you typed for {typed} holds {what}, which an argument cannot; an action gives such a text as its input"
                    : $"The field {Bare(name)} holds {what}, which an argument cannot; an action gives such a text as its input";
            }

            return filledIn;
        }, out var unpaired);
        if (unpaired)
        {
            return (null, $"{actionName} holds a brace without its pair");
        }

        return refused is null ? (filled, null) : (null, refused);
    }

    /// <summary>A <c>run</c> action filled for the item; or why it cannot be, and nothing is started.</summary>
    public static (FilledRun? Run, string? Refused) Run(ConfiguredAction action, ActionContext context)
    {
        var arguments = new List<string>();
        foreach (var argument in action.Arguments)
        {
            var (text, refused) = Fill(argument, context, action.Name, argument: true);
            if (refused is not null)
            {
                return (null, refused);
            }

            arguments.Add(text!);
        }

        string? input = null;
        if (action.Input is not null)
        {
            var (text, refused) = Fill(action.Input, context, action.Name, argument: false);
            if (refused is not null)
            {
                return (null, refused);
            }

            input = text;
        }

        var (then, thenRefused) = Set(action.Then, context, action.Name);
        return thenRefused is not null
            ? (null, thenRefused)
            : (new FilledRun(action.Program, arguments, input, FolderOf(action.Folder, context), action.Seconds, then!), null);
    }

    /// <summary>A <c>terminal</c> action filled for the item; or why it cannot be.</summary>
    public static (FilledTerminal? Terminal, string? Refused) Terminal(ConfiguredAction action, ActionContext context)
    {
        var command = new List<string>();
        foreach (var element in action.Command)
        {
            var (text, refused) = Fill(element, context, action.Name, argument: true);
            if (refused is not null)
            {
                return (null, refused);
            }

            command.Add(text!);
        }

        return (new FilledTerminal(command, FolderOf(action.Folder, context)), null);
    }

    /// <summary>The fields an action sets, each text value filled; a value that is no text (a check's true) as it is.</summary>
    public static (IReadOnlyDictionary<string, JsonNode?>? Set, string? Refused) Set(IReadOnlyDictionary<string, JsonNode?> set, ActionContext context, string actionName)
    {
        var filled = new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (field, value) in set)
        {
            if (value is JsonValue known && known.TryGetValue<string>(out var text))
            {
                var (now, refused) = Fill(text, context, actionName, argument: false);
                if (refused is not null)
                {
                    return (null, refused);
                }

                filled[field] = JsonValue.Create(now);
            }
            else
            {
                filled[field] = value?.DeepClone();
            }
        }

        return (filled, null);
    }

    /// <summary>Where an action runs: the list's folder, or the path the action names, from the list's folder when it is not a full one.</summary>
    public static string FolderOf(string? folder, ActionContext context) =>
        folder is null || folder.Trim().Length == 0 || folder.Trim().Equals(ListActions.ListFolder, StringComparison.OrdinalIgnoreCase)
            ? context.ListFolder
            : Path.GetFullPath(Path.Combine(context.ListFolder, folder.Trim()));

    private static (string? Value, string? Refused) ValueOf(string name, ActionContext context, string actionName)
    {
        if (FormFieldOf(name) is { } formField)
        {
            var typed = context.Form.FirstOrDefault(pair => pair.Key.Equals(formField, StringComparison.OrdinalIgnoreCase));
            return typed.Key is null
                ? (null, $"{actionName} names {{{name}}}, which its form does not have")
                : (typed.Value, null);
        }

        if (Array.IndexOf(BuiltIn, name) >= 0)
        {
            return (name switch
            {
                "file" => context.File,
                "name" => Path.GetFileNameWithoutExtension(context.File),
                "folder" => context.ListFolder,
                _ => FieldTypes.DateText(context.Today),
            }, null);
        }

        var title = Bare(name);
        if (context.Item.Has(title))
        {
            return (FieldTypes.PlainText(context.Item.Value(title)), null);
        }

        // A field of the list that this item's file does not hold yet is the item's, and empty (AF-11.3).
        return context.Config is { } config && config.IndexOf(title) >= 0
            ? (string.Empty, null)
            : (null, $"{actionName} names {{{name}}}, which this item does not have");
    }

    /// <summary>A field's title as a name gives it: <c>field:name</c> is the field titled name.</summary>
    private static string Bare(string name) =>
        name.StartsWith(FieldPrefix, StringComparison.OrdinalIgnoreCase) ? name[FieldPrefix.Length..].Trim() : name;

    /// <summary>
    /// The text with every name replaced by what <paramref name="value"/> gives for it, and <c>{{</c>, <c>}}</c> made
    /// single braces; null, with <paramref name="unpaired"/> set, when its braces do not pair.
    /// </summary>
    private static string? Walk(string text, Func<string, string> value, out bool unpaired)
    {
        unpaired = false;
        var result = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '{' or '}' && i + 1 < text.Length && text[i + 1] == c)
            {
                result.Append(c);
                i++;
                continue;
            }

            if (c == '}')
            {
                unpaired = true;
                return null;
            }

            if (c != '{')
            {
                result.Append(c);
                continue;
            }

            var end = text.IndexOf('}', i + 1);
            var name = end < 0 ? string.Empty : text[(i + 1)..end].Trim();
            if (end < 0 || name.Length == 0 || name.Contains('{'))
            {
                unpaired = true;
                return null;
            }

            result.Append(value(name));
            i = end;
        }

        return result.ToString();
    }
}
