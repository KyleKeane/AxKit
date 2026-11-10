namespace AxHelp.Core;

/// <summary>What axHelp's list shows (SPEC.md HE-2.1): a first draft, the path it was started on or a line that says there is none.</summary>
public static class HelpLines
{
    public const string NothingOpen = "Nothing open";

    /// <summary>The list's lines: never empty, so that the list always has a line to read (AX-8.6).</summary>
    public static IReadOnlyList<string> For(string? path) => [path ?? NothingOpen];

    /// <summary>The window's title before " - axHelp": the file's or folder's name, or "Help".</summary>
    public static string Title(string? path) =>
        path is null ? "Help" : Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) is { Length: > 0 } name ? name : path;
}
