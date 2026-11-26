using System.Text.Json;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// A command name that several apps share and that does not mean one thing among them (docs/axKit/command-language.md
/// section 8 (c), CL-7.3): <paramref name="Differs"/> is "mark", "key" or "mark and key", and
/// <paramref name="Details"/> says for each mark or chord which apps have it ("act: axFile", "ctrl+o: axDown, axJSON").
/// </summary>
public sealed record NameDifference(string Name, string Differs, IReadOnlyList<string> Details);

/// <summary>A difference the maintainer let stand, from <c>docs/axKit/command-exceptions.json</c>: the name, what differs and why.</summary>
public sealed record CommandException(string Name, string Differs, string Reason);

/// <summary>
/// One command name means one thing across apps (docs/axKit/command-language.md CL-7.3; the maintainer's decision of
/// 2026-11-25): the commands of the inventory (<see cref="CommandInventory"/>) that several apps call by the same
/// canonical name have one mark and, where they have a chord, one chord; or the name is a listed exception, with its
/// reason. Pure; <c>tests/AxAutomate.Tests/CommandMeaningsTests</c> holds the inventory to the list. Which family a
/// chord belongs to is not asked here: it waits on the families (CL-7.2).
/// </summary>
public static class CommandMeanings
{
    public const string ExceptionsFormat = "axkit-command-exceptions 1";

    /// <summary>
    /// Every name shared by two apps or more whose marks differ, or whose chords differ among the rows that have one.
    /// A name is the command's canonical name in its app (<see cref="CommandNames.CanonicalOf"/>: the label, or the
    /// label with its menus where two commands of the app share it, "Heading, Navigation, Next"), compared without
    /// regard to case; a row of keys alone whose label a menu row has is that row's command and is passed over. In the
    /// order of the names.
    /// </summary>
    public static IReadOnlyList<NameDifference> Differences(IReadOnlyList<InventoryApp> apps)
    {
        var named = new List<(string Name, string App, ListedCommand Row)>();
        foreach (var app in apps)
        {
            var rows = app.Commands.Where(row => !CommandInventory.PassedOver(app.Commands, row)).ToList();
            var names = rows.Select(row => new CommandName(row.Label, row.Menu)).ToList();
            for (var i = 0; i < rows.Count; i++)
            {
                named.Add((CommandNames.CanonicalOf(names, i), app.App, rows[i]));
            }
        }

        var differences = new List<NameDifference>();
        foreach (var shared in named.GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (shared.Select(entry => entry.App).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
            {
                continue;
            }

            var marks = shared.GroupBy(entry => entry.Row.Mark, StringComparer.OrdinalIgnoreCase).ToList();
            var keys = shared.Where(entry => entry.Row.Key.Length > 0).GroupBy(entry => entry.Row.Key, StringComparer.OrdinalIgnoreCase).ToList();
            var markDiffers = marks.Count > 1;
            var keyDiffers = keys.Count > 1;
            if (!markDiffers && !keyDiffers)
            {
                continue;
            }

            var details = new List<string>();
            if (markDiffers)
            {
                details.AddRange(marks.Select(group => $"{group.Key}: {Apps(group)}"));
            }

            if (keyDiffers)
            {
                details.AddRange(keys.Select(group => $"{group.Key}: {Apps(group)}"));
            }

            differences.Add(new NameDifference(shared.First().Name, markDiffers && keyDiffers ? "mark and key" : markDiffers ? "mark" : "key", details));
        }

        return differences;

        static string Apps(IEnumerable<(string Name, string App, ListedCommand Row)> group) =>
            string.Join(", ", group.Select(entry => entry.App).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>The exceptions file read: its format checked, each entry with a name, what differs and a reason; a fault throws <see cref="FormatException"/>.</summary>
    public static IReadOnlyList<CommandException> ReadExceptions(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (JsonElements.GetString(root, "format") != ExceptionsFormat)
        {
            throw new FormatException($"the exceptions file is not of the format {ExceptionsFormat}");
        }

        var exceptions = new List<CommandException>();
        if (!root.TryGetProperty("exceptions", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("the exceptions file has no list of exceptions");
        }

        foreach (var entry in list.EnumerateArray())
        {
            exceptions.Add(new CommandException(
                JsonElements.GetString(entry, "name") ?? string.Empty,
                JsonElements.GetString(entry, "differs") ?? string.Empty,
                JsonElements.GetString(entry, "reason") ?? string.Empty));
        }

        return exceptions;
    }

    /// <summary>
    /// What is wrong, one line each: a name that does not mean one thing and is not listed as it differs, a listed
    /// exception that no longer differs as listed (take it off the list: the name was made one), an entry without its
    /// reason, and a name listed twice. Empty when the inventory and the list agree.
    /// </summary>
    public static IReadOnlyList<string> Problems(IReadOnlyList<NameDifference> found, IReadOnlyList<CommandException> listed)
    {
        var problems = new List<string>();
        foreach (var difference in found)
        {
            var exception = listed.FirstOrDefault(entry => Same(entry.Name, difference.Name));
            if (exception is null || !Same(exception.Differs, difference.Differs))
            {
                problems.Add($"{difference.Name} does not mean one thing across apps, its {difference.Differs} differs ({string.Join("; ", difference.Details)}): "
                    + "make it one by a name of its own, or list it in docs/axKit/command-exceptions.json with what differs and why");
            }
        }

        foreach (var exception in listed)
        {
            // One that differs otherwise than listed is named by the loop above.
            if (!found.Any(difference => Same(difference.Name, exception.Name)))
            {
                problems.Add($"{exception.Name} is listed as an exception and now means one thing across apps: take it off docs/axKit/command-exceptions.json");
            }

            if (string.IsNullOrWhiteSpace(exception.Reason))
            {
                problems.Add($"{exception.Name} is listed as an exception without its reason");
            }

            if (listed.Count(other => Same(other.Name, exception.Name)) > 1)
            {
                problems.Add($"{exception.Name} is listed more than once");
            }
        }

        return problems.Distinct().ToList();

        static bool Same(string one, string other) => string.Equals(one.Trim(), other.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
