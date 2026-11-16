using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// One command of a window (docs/axKit/app-kit.md AK-2): its menu label with its letter, its key from the window's key
/// table (<see cref="Keys.None"/> for a command that has none), what runs, and where the key acts (<see cref="When"/>,
/// null for anywhere in the window). The window makes its menu item and its key from this one record
/// (<see cref="AxKitForm.Add"/>, <see cref="AxKitForm.AddKey"/>), so that the menu, the key and the command palette
/// cannot disagree, and the AI tools read each command's <see cref="Mark"/> off its menu item (<see cref="CommandPalette.MarkOf"/>).
/// </summary>
public sealed record Command(string Label, Keys Key, Action Run)
{
    /// <summary>Whether the key acts now (the list has the focus, the field has it); the menu item always runs.</summary>
    public Func<bool>? When { get; init; }

    /// <summary>What the command does beyond reading, for MCP; a change by default, but every command made in <c>src</c> states it (<c>CommandMarkTests</c>, docs/axKit/mcp.md MC-D7), since only act is asked of the user before an AI tool runs it.</summary>
    public CommandMark Mark { get; init; } = CommandMark.Change;
}
