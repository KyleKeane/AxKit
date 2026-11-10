using AxKit.Core;

namespace AxDescribe.Core;

/// <summary>The list under axDescribe's field (SPEC.md DE-3): the earlier descriptions that match what is typed, and what is said of them.</summary>
public static class DescribeLines
{
    /// <summary>The list's one line while nothing was described yet.</summary>
    public const string None = "No descriptions yet: the hotkey describes the window in front, Ctrl+D a picture file";

    /// <summary>The list's one line when no earlier description matches the typed words.</summary>
    public static string NoMatch(string typed) => $"No earlier description matches {typed.Trim()}";

    /// <summary>"14:02 notes.md - axDown": the time and the window's title, or its program when the window had none.</summary>
    public static string Label(Description description) =>
        $"{description.When:HH:mm} {(description.Title.Length > 0 ? description.Title : description.Program ?? "a window without a title")}";

    /// <summary>The descriptions whose label holds every word typed, newest first; all of them for nothing typed (DE-3.2).</summary>
    public static IReadOnlyList<Description> Matches(IReadOnlyList<Description> descriptions, string typed)
    {
        var words = TextMatch.Words(typed);
        return words.Count == 0 ? descriptions : descriptions.Where(description => TextMatch.Rank(TextMatch.Fold(Label(description)), words) is not null).ToList();
    }

    /// <summary>"14:02 notes.md - axDown, 1 of 3": a match as the selection reaches it (DE-3.3).</summary>
    public static string Spoken(Description description, int index, int count) => $"{Label(description)}, {index + 1} of {count}";
}
