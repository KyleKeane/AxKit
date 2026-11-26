namespace AxKit.Core;

/// <summary>
/// One line of the command palette (docs/axKit/SPEC.md AX-10.2): what the list shows and NVDA says, "Save as, File,
/// Ctrl+Shift+S", and what Enter does with it. A line of the keys' help (<paramref name="IsCommand"/> false, AX-10.2,
/// 2026-11-26) is read and runs nothing: Enter says it again and the palette stays.
/// </summary>
public sealed record PaletteEntry(string Text, Action Run, bool IsCommand = true);

/// <summary>
/// The command palette's text and search (AX-10.2, AX-10.3): an entry's line from its menu item (its name being
/// <see cref="MenuText.Plain"/> of the item's text), the matches of the typed text in order, and what is said of
/// them. The window side, which reads the menus and shows the notice, is <c>AxKit.Forms.CommandPalette</c> and the
/// window base.
/// </summary>
public static class Palette
{
    /// <summary>
    /// An entry's line: the name, the menus it is in, the key when it has one, "checked" when it is:
    /// "Word wrap, Options, Text, F11, checked".
    /// </summary>
    public static string Text(string name, IEnumerable<string> path, string? key, bool isChecked)
    {
        var parts = new List<string> { name };
        parts.AddRange(path);
        if (!string.IsNullOrEmpty(key))
        {
            parts.Add(key);
        }

        if (isChecked)
        {
            parts.Add("checked");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// The entries holding every word of the text, the ones whose line starts with its first word first, then those
    /// with a word starting with it, then the rest; within each, in the order given, which is the menus' (AX-10.3).
    /// An empty text gives every entry.
    /// </summary>
    public static IReadOnlyList<PaletteEntry> Find(IReadOnlyList<PaletteEntry> entries, string text)
    {
        var words = TextMatch.Words(text);
        var matches = new List<(PaletteEntry Entry, int Rank)>();
        foreach (var entry in entries)
        {
            if (TextMatch.Rank(TextMatch.Fold(entry.Text), words) is { } rank)
            {
                matches.Add((entry, rank));
            }
        }

        // OrderBy is stable, so the menus' order stays within a rank.
        return matches.OrderBy(match => match.Rank).Select(match => match.Entry).ToList();
    }

    /// <summary>The line before the commands in the list with nothing typed (AX-10.2).</summary>
    public const string CommandsLine = "Every command of this window, by menu: Enter runs the one you are on";

    /// <summary>
    /// The palette's list with nothing typed (AX-10.2, the maintainer's decision of 2026-11-26, palette and help in
    /// one): every line of the window's F1 text <paramref name="help"/> that holds a word, its indent and a heading's
    /// marks taken off, each read and running nothing; then <see cref="CommandsLine"/> and the
    /// <paramref name="commands"/>, which run; then <paramref name="keysAlone"/>, the keys with no menu item, read.
    /// </summary>
    public static List<PaletteEntry> WithNothingTyped(string help, IReadOnlyList<PaletteEntry> commands, IEnumerable<string> keysAlone)
    {
        var lines = ReadLines(help);
        lines.Add(new PaletteEntry(CommandsLine, () => { }, IsCommand: false));
        lines.AddRange(commands);
        lines.AddRange(ReadLines(string.Join("\n", keysAlone)));
        return lines;
    }

    /// <summary>Each line of a text that holds a word as a line that is read: "## How the keys go" as "How the keys go".</summary>
    private static List<PaletteEntry> ReadLines(string text) =>
        [.. text.Split('\n').Select(line => line.Trim().TrimStart('#').Trim()).Where(line => line.Length > 0).Select(line => new PaletteEntry(line, () => { }, IsCommand: false))];

    /// <summary>What one match says (AX-10.4): "Save as, Session, Ctrl+Shift+S, 1 of 3".</summary>
    public static string Spoken(PaletteEntry entry, int index, int count) => $"{entry.Text}, {index + 1} of {count}";

    /// <summary>What a text nothing matches says: "No commands match xyz".</summary>
    public static string NoMatch(string text) => $"No commands match {text.Trim()}";
}
