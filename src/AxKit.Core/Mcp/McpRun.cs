namespace AxKit.Core.Mcp;

/// <summary>One command of a window as <c>axkit_window_run</c> matches it: its label with its letter, and the menus that hold its item ("Navigate, Next"), empty for a key without an item.</summary>
public sealed record McpCommandName(string Label, string Menu);

/// <summary>
/// The pure rules of <c>axkit_window_run</c> (docs/axKit/mcp.md MC-7): which windows never run a command for an AI tool,
/// the words of a refusal, how a command's name is matched, and what the window says when a command ran. The window
/// does the running (<c>AxKit.Forms/AxKitForm.Mcp.cs</c>); the switch is <see cref="McpSettings.RunCommands"/>. Tested
/// in <c>tests/AxKit.Tests/McpTests</c>.
/// </summary>
public static class McpRun
{
    /// <summary>The apps whose commands an AI tool never runs (MC-7.4): the password database, and the clipboard history, which serves no pipe at all.</summary>
    public static readonly IReadOnlyList<string> NeverRun = [Dispatch.AxPass, Dispatch.AxClip];

    /// <summary>The menu item of the switch (MC-7.2), in the axKit menu's AI tools submenu.</summary>
    public const string SwitchLabel = "Let AI tools run commands";

    /// <summary>What the tool answers while the switch is off (MC-7.2), the way to allow it included.</summary>
    public const string SwitchOff = "The user has not allowed AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu, AI tools, Let AI tools run commands.";

    /// <summary>The refusal for an app that never runs a command for an AI tool, or null when it may.</summary>
    public static string? AppRefusal(string appVerb) =>
        NeverRun.Contains(appVerb, StringComparer.OrdinalIgnoreCase)
            ? appVerb.Equals(Dispatch.AxPass, StringComparison.OrdinalIgnoreCase)
                ? "axPass's commands are never run by AI tools: its windows hold passwords. The user runs them."
                : "axClip's commands are never run by AI tools: its window holds the clipboard's history. The user runs them."
            : null;

    /// <summary>A label or a menu path as it is matched: the letter's marker and the trailing dots gone, the spaces trimmed.</summary>
    public static string Plain(string text) => MenuText.Plain(text).Trim();

    /// <summary>
    /// The command a name gives (MC-7.1): the label as <c>axkit_window_commands</c> lists it or as the palette shows it
    /// (its letter's marker and dots do not matter, nor case); <paramref name="menu"/>, when given, the menus that hold
    /// it ("Navigate, Next"), which tells apart two commands of one name. The index in <paramref name="commands"/>, or
    /// the refusal that says why none.
    /// </summary>
    public static (int Index, string? Refusal) Find(IReadOnlyList<McpCommandName> commands, string? name, string? menu)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (-1, "Name the command, as axkit_window_commands lists it.");
        }

        var wanted = Plain(name);
        var named = Enumerable.Range(0, commands.Count)
            .Where(i => string.Equals(Plain(commands[i].Label), wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!string.IsNullOrWhiteSpace(menu))
        {
            var place = Plain(menu);
            named = [.. named.Where(i => string.Equals(Plain(commands[i].Menu), place, StringComparison.OrdinalIgnoreCase))];
        }

        if (named.Count == 0)
        {
            return (-1, string.IsNullOrWhiteSpace(menu)
                ? $"The window has no command {wanted}: axkit_window_commands lists them."
                : $"The window has no command {wanted} in the menu {Plain(menu)}: axkit_window_commands lists the commands with their menus.");
        }

        if (named.Count > 1)
        {
            var places = string.Join("; ", named.Select(i => commands[i].Menu.Length > 0 ? commands[i].Menu : "no menu"));
            return (-1, $"The window has several commands named {wanted}: give the menu too, one of: {places}.");
        }

        return (named[0], null);
    }

    /// <summary>Who ran it, as the user hears it (MC-7.5): "Claude" for Claude Code under any spelling of its name, the client's own name otherwise, its hidden characters made visible and at most 40 characters.</summary>
    public static string Speaker(string client)
    {
        var name = DisplayText.Visible(client.Trim());
        if (name.StartsWith("claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        return name.Length == 0 ? "An AI tool" : name.Length > 40 ? name[..40] : name;
    }

    /// <summary>What the window says as a command runs for an AI tool (MC-7.5): "Claude ran Word wrap".</summary>
    public static string Said(string client, string label) => $"{Speaker(client)} ran {Plain(label)}";

    /// <summary>The title of the question an act command asks first (MC-7.3): "Claude asks to run Delete".</summary>
    public static string AskTitle(string client, string label) => $"{Speaker(client)} asks to run {Plain(label)}";
}
