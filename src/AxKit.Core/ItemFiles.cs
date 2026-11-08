namespace AxKit.Core;

/// <summary>
/// The file an item of an item app is (docs/axkit/item-apps.md IA-4.5): its name from its Title, and a free name beside
/// the taken ones. From AxTask.Core's <c>ItemFolder</c> on its second use, the reports list (2026-11-07), unchanged.
/// </summary>
public static class ItemFiles
{
    /// <summary>The file name a Title gives (IA-4.5): the characters Windows forbids as spaces, no full stop at the end, "Untitled" for nothing.</summary>
    public static string FileStem(string title)
    {
        var chars = title.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsControl(c) ? ' ' : c).ToArray();
        var stem = string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.', ' ');
        if (stem.Length > 120)
        {
            stem = stem[..120].TrimEnd('.', ' ');
        }

        return stem.Length == 0 ? "Untitled" : stem;
    }

    /// <summary>"name.json", or "name - 2.json" when taken (IA-4.5); <paramref name="own"/> is the item's own file, which never counts as taken.</summary>
    public static string FreePath(string folder, string stem, string extension, string? own = null)
    {
        for (var n = 1; ; n++)
        {
            var path = Path.Combine(folder, n == 1 ? stem + extension : $"{stem} - {n}{extension}");
            if (own is not null && path.Equals(own, StringComparison.OrdinalIgnoreCase))
            {
                return own;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }
        }
    }
}
