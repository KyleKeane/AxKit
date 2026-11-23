using AxKit.Core;

namespace AxTemplate.Core;

/// <summary>
/// Finding a template by what was typed (SPEC.md TP-3.2): every word must be found, in any order and without regard
/// to case, in the template's name or in its text. Templates whose name holds all the words come first, in the
/// order of their names; then those that need their text for a word. An empty field lists every template, the ones
/// used last first, then the others by name.
/// </summary>
public static class TemplateMatch
{
    /// <summary>The templates that match <paramref name="typed"/>, in the order the list shows them. <paramref name="recent"/> holds the names used last, newest first.</summary>
    public static IReadOnlyList<Template> Find(IReadOnlyList<Template> templates, string typed, IReadOnlyList<string> recent)
    {
        var words = typed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            var used = recent
                .Select(name => templates.FirstOrDefault(template => string.Equals(template.Title, name, StringComparison.OrdinalIgnoreCase)))
                .Where(template => template is not null)
                .Select(template => template!)
                .Distinct()
                .ToList();
            return [.. used, .. templates.Where(template => !used.Contains(template))];
        }

        var byName = templates.Where(template => words.All(word => Holds(template.Title, word))).ToList();
        var byText = templates
            .Where(template => !byName.Contains(template) && words.All(word => Holds(template.Title, word) || Holds(template.Text, word)))
            .ToList();
        return [.. byName, .. byText];
    }

    /// <summary>What the list's one line says when nothing matches, or when there is no template at all (TP-4.7).</summary>
    public static string Nothing(string typed, int templates) =>
        templates == 0 ? "No templates yet: Ctrl+B opens the browser, where one is made"
        : $"No template matches {typed.Trim()}";

    /// <summary>What is said when the selection reaches a template: its name and its place, "Introduction, 1 of 12" (TP-3.3).</summary>
    public static string Spoken(Template template, int index, int count) => $"{template.Title}, {index + 1} of {count}";

    /// <summary>The names used last with <paramref name="name"/> the newest, at most <paramref name="keep"/> of them.</summary>
    public static List<string> Used(IEnumerable<string> recent, string name, int keep = 20) =>
        [name, .. recent.Where(other => !string.Equals(other, name, StringComparison.OrdinalIgnoreCase)).Take(keep - 1)];

    private static bool Holds(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
