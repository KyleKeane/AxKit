namespace AxKit.Core;

/// <summary>A top-level window of one of the bundle's processes: its handle and its title.</summary>
public sealed record AxKitWindow(nint Handle, string Title)
{
    /// <summary>The app the title names ("notes.md, axDown, C:\work\notes.md" is axDown's); the whole title when it is one part.</summary>
    public string App => AxKitWindows.AppOf(Title);
}

/// <summary>
/// What a window holds open, for its title (docs/axKit/SPEC.md AX-9.6): its name ("notes.md", "myproject", "Home")
/// and its full path when it has one; a path that is its own name (a drive's root) is said once.
/// </summary>
public sealed record Opened(string Name, string? Path = null)
{
    /// <summary>A file or folder by its full path: its name (<see cref="PathName.Of"/>) and the path.</summary>
    public static Opened At(string path) => new(PathName.Of(path), path);
}

/// <summary>
/// The window lists of the Window menu (docs/axKit/SPEC.md AX-9), pure: which windows go under Open windows and under
/// each app, and what an item says. The windows come in the order Windows keeps its top-level windows, the most recently used
/// first, which is Alt+Tab's order, so recency needs nothing stored. Tested in <c>tests/AxKit.Tests</c>.
/// </summary>
public static class AxKitWindows
{
    /// <summary>The parts of a title, a comma and a space between them, which NVDA reads with a pause.</summary>
    public const string Separator = ", ";

    /// <summary>
    /// A window's title (AX-9.6, the maintainer's word of 2026-11-26): what it holds open first, then the app's name
    /// alone, then the full path, "notes.md, axDown, C:\work\notes.md", "myproject, axClaude, C:\src\myproject"; the
    /// app's name alone for a window with nothing open; the path left out when there is none or it is the name.
    /// </summary>
    public static string Title(string app, Opened? opened)
    {
        if (opened is null || opened.Name.Length == 0)
        {
            return app;
        }

        var title = opened.Name + Separator + app;
        return opened.Path is { Length: > 0 } path && path != opened.Name ? title + Separator + path : title;
    }

    /// <summary>
    /// The app a window's title names (<see cref="Title"/>): the whole title when it is one part; otherwise the last
    /// part that is an app's name and stands before the path (the first part after the second that is a whole path,
    /// a mark of unsaved changes before it left out), or the last such part of all in a title without a path. So a
    /// name with a comma in it ("Smith, John.docx") still finds its app, and so does one that holds an app's name
    /// as a part of its own (a folder "ideas, axTask" open in axFile, which was named axTask until 2026-11-27). The
    /// second part when no part is an app's name.
    /// </summary>
    public static string AppOf(string title)
    {
        var parts = title.Split(Separator);
        if (parts.Length == 1)
        {
            return title;
        }

        // The path comes after the name and the app: never one of the first two parts ("C:\, axFile" is a name).
        var path = Array.FindIndex(parts, 2, part => Path.IsPathFullyQualified(part.TrimStart('*')));
        var before = path < 0 ? parts.Length : path;
        return parts.Take(before).Skip(1).LastOrDefault(part => Dispatch.ByName(part) is not null) ?? parts[1];
    }

    /// <summary>The Open windows list: the windows of the bundle's apps other than the current one, most recently used first.</summary>
    public static IReadOnlyList<AxKitWindow> Others(IReadOnlyList<AxKitWindow> windows, nint current) =>
        windows.Where(w => w.Handle != current && IsApp(w)).ToList();

    /// <summary>The app's windows, the current one included, most recently used first.</summary>
    public static IReadOnlyList<AxKitWindow> Of(IReadOnlyList<AxKitWindow> windows, App app) =>
        windows.Where(w => string.Equals(w.App, app.Name, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// "&amp;1. notes.md, axDown, C:\work\notes.md": the item's number as its mnemonic, 1 to 9 and then a to z, a full stop, which the
    /// synthesizer takes as a pause before the name (AX-8.5), and the title; an ampersand in a title is doubled, as a
    /// menu needs. The same shape serves the apps' own recent lists (FR-8.5, AD-4.1).
    /// </summary>
    public static string Label(int index, string title)
    {
        var text = title.Replace("&", "&&");
        var mnemonic = index < 9 ? (char)('1' + index) : index < 35 ? (char)('a' + index - 9) : '\0';
        return mnemonic == '\0' ? text : $"&{mnemonic}. {text}";
    }

    /// <summary>"ax&amp;Claude, access layer for Claude Code": the app's title (AX-11.2) with the letter after ax as the mnemonic.</summary>
    public static string AppLabel(App app) =>
        app.Title.StartsWith("ax", StringComparison.OrdinalIgnoreCase) ? app.Title[..2] + "&" + app.Title[2..] : "&" + app.Title;

    private static bool IsApp(AxKitWindow window) => Dispatch.Apps.Any(a => string.Equals(a.Name, window.App, StringComparison.OrdinalIgnoreCase));
}
