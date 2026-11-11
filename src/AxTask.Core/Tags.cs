using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The mentions and hashtags in the tasks' words (SPEC.md TK-5.5, asked 2026-11-11): "@name" and "#tag" in an item's
/// Title or a text field, each a smart list of its own under Mentions or Hashtags on the top level. A mark starts a
/// token only where no letter or digit stands before it, so that an e-mail address is no mention; the token is the
/// letters, digits, "_" and "-" after it. Told apart without case, shown as first seen.
/// </summary>
public static class Tags
{
    public const char Mention = '@';
    public const char Hashtag = '#';

    /// <summary>The tokens with this mark in a text, "@Sam", in order, each once (case not minded).</summary>
    public static IReadOnlyList<string> In(string text, char mark)
    {
        var found = new List<string>();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != mark || (i > 0 && char.IsLetterOrDigit(text[i - 1])))
            {
                continue;
            }

            var end = i + 1;
            while (end < text.Length && IsTokenChar(text[end]))
            {
                end++;
            }

            if (end > i + 1)
            {
                var token = text[i..end];
                if (!found.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    found.Add(token);
                }
            }

            i = end - 1;
        }

        return found;
    }

    /// <summary>Whether the text holds this token ("@Sam"), case not minded, as a whole token.</summary>
    public static bool Has(string text, string token) =>
        token.Length > 1 && In(text, token[0]).Contains(token, StringComparer.OrdinalIgnoreCase);

    /// <summary>An item's words a tag is looked for in: its Title and its text fields (TK-5.5), as the search reads them.</summary>
    public static string TextOf(ItemDocument item) =>
        string.Join("\n", item.Fields.Where(field => field.Type is FieldTypes.Text or FieldTypes.LongText && !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
            .Select(field => FieldTypes.TextOf(field.Value)).Prepend(item.Title));

    /// <summary>
    /// The tokens with this mark in the tasks not done (TK-5.5), each once as first seen, in natural order; the items
    /// are those All tasks reads.
    /// </summary>
    public static IReadOnlyList<string> InTasks(IEnumerable<ItemFile> items, char mark)
    {
        var found = new List<string>();
        foreach (var item in items)
        {
            if (!TaskRules.IsTask(item.Document) || TaskRules.IsDone(item.Document))
            {
                continue;
            }

            foreach (var token in In(TextOf(item.Document), mark))
            {
                if (!found.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    found.Add(token);
                }
            }
        }

        found.Sort(NaturalOrder.Instance);
        return found;
    }

    private static bool IsTokenChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '-';
}
