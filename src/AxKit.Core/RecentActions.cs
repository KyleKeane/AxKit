using System.Text.RegularExpressions;

namespace AxKit.Core;

/// <summary>What a window recorded: a command it ran, something it said, or a notice it showed.</summary>
public enum RecentActionKind
{
    Command,
    Said,
    Notice,
}

/// <summary>
/// The last actions of one window, for a report (docs/axKit/reports.md RP-4.1): every command run, by key, menu,
/// palette, script or AI tool, written by its name (<see cref="CommandText"/>), what the window announced, and the
/// notices it showed; twenty at most, the oldest dropped. Pure: the window adds each entry with the time, and asks for
/// the report's lines with the time now, so that the ages read "12 s ago".
/// </summary>
public sealed partial class RecentActions
{
    public const int Capacity = 20;

    // A label numbered as AxKitWindows.Label numbers it, plain: "2. notes.md (C:\notes)", "b. Tasks"; the number kept.
    [GeneratedRegex(@"^(\d+|[a-z])\. ")]
    private static partial Regex EntryRegex();

    /// <summary>
    /// How a command is written (RP-4.1; the maintainer's decision of 2026-11-25, docs/axKit/command-language.md
    /// section 8 (a) 3): its name and its key as the menu shows it, "Find, Ctrl+F", "Word wrap"; never what the user
    /// typed. An entry of a list of the user's own things, numbered as <see cref="AxKitWindows.Label"/> numbers it (a
    /// recent file, a window, a folder, "&amp;2. notes.md (C:\notes)"), is written by its menus and its number,
    /// "Session, Open recent file, entry 2", since its name may name the user's file (RP-4.2a); a menu above it that
    /// is such an entry is written the same way.
    /// </summary>
    public static string CommandText(string label, IReadOnlyList<string> menus, string? key)
    {
        var name = MenuText.Plain(label);
        var text = Entry(name) is null ? name : string.Join(", ", menus.Select(menu => Entry(MenuText.Plain(menu)) ?? MenuText.Plain(menu)).Append(Entry(name)));
        return string.IsNullOrWhiteSpace(key) ? text : $"{text}, {key.TrimEnd('.')}";

        static string? Entry(string plain) => EntryRegex().Match(plain) is { Success: true } match ? "entry " + match.Groups[1].Value : null;
    }

    private readonly object _gate = new();
    private readonly Queue<(DateTime At, RecentActionKind Kind, string Text)> _entries = new();

    public void Add(RecentActionKind kind, string text, DateTime at)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lock (_gate)
        {
            _entries.Enqueue((at, kind, text.Trim()));
            while (_entries.Count > Capacity)
            {
                _entries.Dequeue();
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>The report's lines, oldest first: "12 s ago: Find, Ctrl+F", "12 s ago, said: 3 matches", "2 min ago, notice: Save changes?".</summary>
    public IReadOnlyList<string> Lines(DateTime now)
    {
        lock (_gate)
        {
            return _entries.Select(entry => Line(entry.At, entry.Kind, entry.Text, now)).ToList();
        }
    }

    /// <summary>The entries themselves, oldest first, for a report that takes only some kinds (<c>Reports.ReportPrivacy.Commands</c>).</summary>
    public IReadOnlyList<(DateTime At, RecentActionKind Kind, string Text)> Entries()
    {
        lock (_gate)
        {
            return _entries.ToList();
        }
    }

    public static string Line(DateTime at, RecentActionKind kind, string text, DateTime now)
    {
        var age = Age(now - at);
        return kind switch
        {
            RecentActionKind.Said => $"{age}, said: {text}",
            RecentActionKind.Notice => $"{age}, notice: {text}",
            _ => $"{age}: {text}",
        };
    }

    /// <summary>"just now" under five seconds, then "12 s ago", "3 min ago", "2 h ago", "3 days ago".</summary>
    public static string Age(TimeSpan ago)
    {
        if (ago < TimeSpan.FromSeconds(5))
        {
            return "just now";
        }

        if (ago < TimeSpan.FromMinutes(1))
        {
            return $"{(int)ago.TotalSeconds} s ago";
        }

        if (ago < TimeSpan.FromHours(1))
        {
            return $"{(int)ago.TotalMinutes} min ago";
        }

        if (ago < TimeSpan.FromDays(1))
        {
            return $"{(int)ago.TotalHours} h ago";
        }

        var days = (int)ago.TotalDays;
        return days == 1 ? "1 day ago" : $"{days} days ago";
    }
}
