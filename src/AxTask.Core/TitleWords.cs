using System.Globalization;
using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// Plain words at the end of a new item's title that fill its fields (SPEC.md TK-4.3c; the maintainer's ask of the
/// old feature list, item 21, and his choice of 2026-11-25): a date fills Due, written as the date fields take one
/// (2026-12-01, today, tomorrow, +3, friday), and a word after an exclamation mark fills Priority with the choice that
/// starts with it (!high). Read from the end, one word at a time, each kind once; a word is taken only when the list has
/// the field and the word names a value of it, and the title never ends up empty. The words taken leave the title.
/// </summary>
public static class TitleWords
{
    public const string PriorityField = "Priority";

    /// <summary>The title without the words taken, the values they give by field title, and what is said of them: ", due 26 November, priority High".</summary>
    public sealed record Taken(string Title, IReadOnlyList<KeyValuePair<string, JsonNode?>> Values, string Said);

    public static Taken Read(string title, IReadOnlyList<ItemField> fields, DateOnly today)
    {
        var due = fields.FirstOrDefault(field => field.Title.Equals(TaskRules.DueField, StringComparison.OrdinalIgnoreCase)
            && field.Type is FieldTypes.Date or FieldTypes.DateTime);
        var priority = fields.FirstOrDefault(field => field.Title.Equals(PriorityField, StringComparison.OrdinalIgnoreCase)
            && field.Type == FieldTypes.Choice && field.Options.Count > 0);

        var words = title.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var values = new List<KeyValuePair<string, JsonNode?>>();
        var said = new List<string>();
        var dateTaken = false;
        var priorityTaken = false;
        while (words.Count > 1)
        {
            var word = words[^1];
            if (!priorityTaken && priority is not null && word.Length > 1 && word[0] == '!'
                && priority.Options.FirstOrDefault(option => option.StartsWith(word[1..], StringComparison.OrdinalIgnoreCase)) is { } chosen)
            {
                values.Insert(0, new(priority.Title, JsonValue.Create(chosen)));
                said.Insert(0, $"priority {chosen}");
                priorityTaken = true;
            }
            else if (!dateTaken && due is not null && word[0] != '!' && FieldTypes.ParseDate(word, today) is { } date)
            {
                var value = due.Type == FieldTypes.DateTime
                    ? FieldTypes.DateTimeText(date.ToDateTime(TimeOnly.MinValue))
                    : FieldTypes.DateText(date);
                values.Insert(0, new(due.Title, JsonValue.Create(value)));
                said.Insert(0, "due " + date.ToString(date.Year == today.Year ? "d MMMM" : "d MMMM yyyy", CultureInfo.InvariantCulture));
                dateTaken = true;
            }
            else
            {
                break;
            }

            words.RemoveAt(words.Count - 1);
        }

        return new Taken(values.Count == 0 ? title : string.Join(' ', words), values, string.Concat(said.Select(part => ", " + part)));
    }
}
