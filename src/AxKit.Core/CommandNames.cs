namespace AxKit.Core;

/// <summary>
/// One command of a window as a name finds it (docs/axKit/user-workflows.md WF-1): its label as its menu item shows it
/// (the letter's marker and the dots may be there), and the menus that hold its item from the bar down joined by a
/// comma and a space ("Navigation, Next"); empty for a key with no menu item.
/// </summary>
public sealed record CommandName(string Label, string Menu);

/// <summary>What a name came to (WF-1.3): one command; no name given; no command of that name; none of that name in the menu given; several.</summary>
public enum CommandMatch
{
    Found,
    NoName,
    Unknown,
    UnknownInMenu,
    Ambiguous,
}

/// <summary>
/// The answer of <see cref="CommandNames.Resolve"/>: the match; the command's index (-1 unless found); the name and the
/// menu as they were compared (plain, the menu empty when none was given); and the candidates as indexes, in the
/// list's order (WF-1.4): for <see cref="CommandMatch.Ambiguous"/> the commands that share the name, for
/// <see cref="CommandMatch.Unknown"/> at most five whose label holds the name or is held by it, for
/// <see cref="CommandMatch.UnknownInMenu"/> the commands of that label in other menus; none otherwise.
/// </summary>
public sealed record CommandResolution(CommandMatch Match, int Index, string Wanted, string WantedMenu, IReadOnlyList<int> Candidates);

/// <summary>
/// The one resolver of a command's canonical name (docs/axKit/user-workflows.md WF-1): its label, plain, and where two
/// commands of a window share the label, the label with its menus as the palette lists it without its key ("Heading,
/// Navigation, Next"). Every caller that names a command finds it here (<c>axkit_window_run</c> through
/// <c>McpRun.Find</c>, and the automated checks); whole text against whole text, without regard to case, never a
/// search: the palette's own search is <see cref="Palette.Find"/> (WF-1.6). Tested in <c>tests/AxKit.Tests/CommandNamesTests</c>.
/// </summary>
public static class CommandNames
{
    /// <summary>How many commands an unknown name names at most as the nearest (WF-1.4).</summary>
    private const int Nearest = 5;

    /// <summary>A label, a menu path or a name as it is compared: the letter's marker and the trailing dots gone, the spaces trimmed.</summary>
    public static string Plain(string text) => MenuText.Plain(text).Trim();

    /// <summary>
    /// The command a name gives (WF-1.3): an empty name is refused; with <paramref name="menu"/> given, the commands of
    /// that label in those menus; without it, the commands of that label, and only when there is none, the commands
    /// whose whole line (<see cref="Line"/>) is the name, so that a label always wins over a line. One left is found,
    /// none is unknown, several are ambiguous, also when their menus are the same (WF-1.5).
    /// </summary>
    public static CommandResolution Resolve(IReadOnlyList<CommandName> commands, string? name, string? menu = null)
    {
        var place = string.IsNullOrWhiteSpace(menu) ? string.Empty : Plain(menu);
        if (string.IsNullOrWhiteSpace(name))
        {
            return new CommandResolution(CommandMatch.NoName, -1, string.Empty, place, []);
        }

        var wanted = Plain(name);
        var labelled = Where(commands, command => Same(Plain(command.Label), wanted));
        List<int> named;
        if (place.Length > 0)
        {
            named = [.. labelled.Where(i => Same(Plain(commands[i].Menu), place))];
            if (named.Count == 0)
            {
                return new CommandResolution(CommandMatch.UnknownInMenu, -1, wanted, place, labelled);
            }
        }
        else
        {
            named = labelled.Count > 0 ? labelled : Where(commands, command => Same(Line(command), wanted));
            if (named.Count == 0)
            {
                var nearest = Where(commands, command => Plain(command.Label) is { Length: > 0 } label
                    && (label.Contains(wanted, StringComparison.OrdinalIgnoreCase) || wanted.Contains(label, StringComparison.OrdinalIgnoreCase)));
                return new CommandResolution(CommandMatch.Unknown, -1, wanted, place, [.. nearest.Take(Nearest)]);
            }
        }

        return named.Count == 1
            ? new CommandResolution(CommandMatch.Found, named[0], wanted, place, [])
            : new CommandResolution(CommandMatch.Ambiguous, -1, wanted, place, named);
    }

    /// <summary>A command's whole line (WF-1.1): its label and its menus, "Heading, Navigation, Next"; the label alone for a command without menus.</summary>
    public static string Line(CommandName command)
    {
        var label = Plain(command.Label);
        var menus = Plain(command.Menu);
        return menus.Length > 0 ? $"{label}, {menus}" : label;
    }

    /// <summary>The canonical name of the command at <paramref name="index"/> (WF-1.1): its label when no other command of the list has that label, else its whole line.</summary>
    public static string CanonicalOf(IReadOnlyList<CommandName> commands, int index)
    {
        var label = Plain(commands[index].Label);
        var shared = Where(commands, command => Same(Plain(command.Label), label)).Count > 1;
        return shared ? Line(commands[index]) : label;
    }

    /// <summary>
    /// What an unknown or ambiguous name is refused with, for the step <c>do</c> and the checks (WF-1.4): what was named
    /// and what to write instead; empty for a command found. <c>McpRun.Find</c> keeps its own sentences.
    /// </summary>
    public static string Refusal(CommandResolution resolution, IReadOnlyList<CommandName> commands)
    {
        var lines = resolution.Candidates.Select(i => Line(commands[i])).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return resolution.Match switch
        {
            CommandMatch.NoName => "no command is named",
            CommandMatch.Unknown => lines.Count > 0
                ? $"no command \"{resolution.Wanted}\"; nearest: {string.Join("; ", lines)}"
                : $"no command \"{resolution.Wanted}\"",
            CommandMatch.UnknownInMenu => $"no command \"{resolution.Wanted}\" in the menu {resolution.WantedMenu}",
            CommandMatch.Ambiguous => lines.Count == 1
                ? $"several commands are named \"{lines[0]}\" and the name cannot tell them apart"
                : $"several commands are named \"{resolution.Wanted}\": write one of: {string.Join("; ", lines)}",
            _ => string.Empty,
        };
    }

    private static bool Same(string one, string other) => string.Equals(one, other, StringComparison.OrdinalIgnoreCase);

    private static List<int> Where(IReadOnlyList<CommandName> commands, Func<CommandName, bool> holds) =>
        [.. Enumerable.Range(0, commands.Count).Where(i => holds(commands[i]))];
}
