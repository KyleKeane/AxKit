namespace AxKit.Core;

/// <summary>
/// The file an item of an item app is (docs/axKit/item-apps.md IA-4.5): its name from its Title, and a free name beside
/// the taken ones. From AxTask.Core's <c>ItemFolder</c> on its second use, the reports list (2026-11-07), unchanged.
/// </summary>
public static class ItemFiles
{
    /// <summary>The file name a Title gives (IA-4.5): the characters Windows forbids as spaces, no full stop at the end, "Untitled" for nothing, an underscore before a device name (S-55).</summary>
    public static string FileStem(string title)
    {
        var chars = title.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsControl(c) ? ' ' : c).ToArray();
        var stem = string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.', ' ');
        if (stem.Length > 120)
        {
            stem = stem[..120].TrimEnd('.', ' ');
        }

        // Windows maps a name whose first part is a device's (con, nul, com1) to the device, whatever follows the dot:
        // the file could not be written and every refresh failed on it (security audit S-55). An underscore in front.
        if (Devices.Contains(stem.Split('.')[0].TrimEnd()))
        {
            stem = "_" + stem;
        }

        return stem.Length == 0 ? "Untitled" : stem;
    }

    private static readonly HashSet<string> Devices = new(["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// "name.json", or "name - 2.json", "name - 3.json" when taken (IA-4.5; axFile's AF-4.8 and axPass's backups follow
    /// the same rule): the first path in the folder that nothing has, nor <paramref name="taken"/> holds (the names an
    /// operation under way will write); <paramref name="own"/> is the item's own file, which never counts as taken.
    /// </summary>
    public static string FreePath(string folder, string stem, string extension, string? own = null, IEnumerable<string>? taken = null)
    {
        for (var n = 1; ; n++)
        {
            var path = Path.Combine(folder, n == 1 ? stem + extension : $"{stem} - {n}{extension}");
            if (own is not null && path.Equals(own, StringComparison.OrdinalIgnoreCase))
            {
                return own;
            }

            if (!File.Exists(path) && !Directory.Exists(path) && !(taken?.Contains(path, StringComparer.OrdinalIgnoreCase) ?? false))
            {
                return path;
            }
        }
    }
}
