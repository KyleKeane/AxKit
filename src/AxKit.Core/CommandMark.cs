namespace AxKit.Core;

/// <summary>
/// What a command does beyond reading its window, for the control of AI clients (docs/axKit/mcp.md MC-3.1). Here, and
/// no longer beside <c>AxKit.Forms/Command.cs</c>, since 2026-11-16: <see cref="CommandGate"/>, which is pure, decides
/// by it.
/// </summary>
public enum CommandMark
{
    /// <summary>Says or shows something; changes nothing.</summary>
    Read,

    /// <summary>Opens a window or a file, moves in the window.</summary>
    Open,

    /// <summary>Changes the window's own state or the file it holds.</summary>
    Change,

    /// <summary>Acts outside the window: deletes, moves, runs, sends, installs. An AI client never runs one on its own word.</summary>
    Act,
}
