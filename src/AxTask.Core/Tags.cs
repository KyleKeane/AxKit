using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The smart tags in the tasks' words (SPEC.md TK-5.5, asked 2026-11-11, generalized the same day): "@kyle", "#ui",
/// "$budget", any symbol at the start of a word followed by a letter, each tag a smart list of its own under Tags,
/// one heading per symbol. A tag starts at the start of the text or after white space; its symbol is any character that
/// is not a letter, a digit or white space, except those that start ordinary prose or Markdown (<see cref="Excluded"/>);
/// a letter must follow the symbol, then letters, digits, "_" and "-". So an e-mail address, "(note", "-item" and "$5"
/// are none (TK-D18). Told apart without case, shown as first seen.
/// </summary>
public static class Tags
{
    /// <summary>The symbols that never start a tag: they start prose, quotes, lists and Markdown. "!" and "?" are tags' symbols ("!urgent"): after a word they never start one.</summary>
    public const string Excluded = "([{<\"'`*_~-/\\.,;:=+|";

    /// <summary>The symbol of a person the item is assigned to (TK-D19).</summary>
    public const char Assignee = '@';

    /// <summary>Whether a character can be a tag's symbol.</summary>
    public static bool IsSymbol(char c) =>
        !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && !char.IsSurrogate(c) && !char.IsControl(c) && !Excluded.Contains(c);

    /// <summary>The tags of a text, "@Sam", in order, each once (case not minded).</summary>
    public static IReadOnlyList<string> In(string text)
    {
        var found = new List<string>();
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsSymbol(text[i]) || (i > 0 && !char.IsWhiteSpace(text[i - 1])) || i + 1 >= text.Length || !char.IsLetter(text[i + 1]))
            {
                continue;
            }

            var end = i + 2;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] is '_' or '-'))
            {
                end++;
            }

            var token = text[i..end];
            // A word that ends in its own symbol is a placeholder, not a tag: %USERPROFILE%, $VAR$ (TK-D18).
            if (end < text.Length && text[end] == text[i])
            {
                i = end;
                continue;
            }

            if (!found.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                found.Add(token);
            }

            i = end - 1;
        }

        return found;
    }

    /// <summary>Whether the text holds this tag ("@Sam"), case not minded, as a whole tag.</summary>
    public static bool Has(string text, string token) => In(text).Contains(token, StringComparer.OrdinalIgnoreCase);

    /// <summary>An item's words a tag is looked for in: its Title and its text fields (TK-5.5), as the search reads them.</summary>
    public static string TextOf(ItemDocument item) =>
        string.Join("\n", item.Fields.Where(field => field.Type is FieldTypes.Text or FieldTypes.LongText && !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            .Select(field => FieldTypes.TextOf(field.Value)).Prepend(item.Title));

    /// <summary>
    /// The tags of the tasks not done, by symbol (TK-5.5): "@" then "#" when there are any, then the other symbols in the
    /// order they first appear; each symbol's tags once, as first seen, in natural order.
    /// </summary>
    public static IReadOnlyList<(char Symbol, IReadOnlyList<string> Tags)> InTasks(IEnumerable<ItemFile> items)
    {
        var found = new List<string>();
        foreach (var item in items)
        {
            if (!TaskRules.IsTask(item.Document) || TaskRules.IsDone(item.Document))
            {
                continue;
            }

            foreach (var token in In(TextOf(item.Document)))
            {
                if (!found.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    found.Add(token);
                }
            }
        }

        var symbols = found.Select(token => token[0]).Distinct().ToList();
        var order = new[] { '@', '#' }.Where(symbols.Contains).Concat(symbols.Where(symbol => symbol is not ('@' or '#'))).ToList();
        return order.Select(symbol => (symbol, (IReadOnlyList<string>)found.Where(token => token[0] == symbol).OrderBy(token => token, NaturalOrder.Instance).ToList())).ToList();
    }

    /// <summary>
    /// A symbol as NVDA should hear it (TK-5.5), since a bare symbol may not be spoken: "@ at sign", "§ section sign";
    /// one not in the table by its code point, "※ U+203B".
    /// </summary>
    public static string Spoken(char symbol) => $"{symbol} {(Names.TryGetValue(symbol, out var name) ? name : $"U+{(int)symbol:X4}")}";

    private static readonly Dictionary<char, string> Names = new()
    {
        ['@'] = "at sign",
        ['!'] = "exclamation mark",
        ['?'] = "question mark",
        ['#'] = "number sign",
        ['$'] = "dollar sign",
        ['%'] = "percent sign",
        ['&'] = "ampersand",
        ['^'] = "caret",
        ['§'] = "section sign",
        ['>'] = "greater-than sign",
        [']'] = "right square bracket",
        [')'] = "right parenthesis",
        ['}'] = "right curly bracket",
        ['¶'] = "pilcrow sign",
        ['€'] = "euro sign",
        ['£'] = "pound sign",
        ['¥'] = "yen sign",
        ['¢'] = "cent sign",
        ['©'] = "copyright sign",
        ['®'] = "registered sign",
        ['°'] = "degree sign",
        ['±'] = "plus-minus sign",
        ['×'] = "multiplication sign",
        ['÷'] = "division sign",
        ['•'] = "bullet",
        ['†'] = "dagger",
        ['‡'] = "double dagger",
        ['→'] = "rightwards arrow",
        ['←'] = "leftwards arrow",
        ['★'] = "black star",
        ['☆'] = "white star",
        ['✓'] = "check mark",
        ['¤'] = "currency sign",
        ['µ'] = "micro sign",
        ['¬'] = "not sign",
        ['¦'] = "broken bar",
        ['¿'] = "inverted question mark",
        ['¡'] = "inverted exclamation mark",
    };
}
