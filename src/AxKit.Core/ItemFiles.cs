namespace AxKit.Core;

/// <summary>
/// The file an item of an item app is (docs/axKit/item-apps.md IA-4.5): its name from its Title, and a free name beside
/// the taken ones. From AxTask.Core's <c>ItemFolder</c> on its second use, the reports list (2026-11-07), unchanged.
/// </summary>
public static class ItemFiles
{
    /// <summary>
    /// The file name a Title gives (IA-4.5): the characters Windows forbids as spaces, no full stop at the end,
    /// "Untitled" for nothing, an underscore before a device name (S-55), before a full stop the name would start
    /// with and before "list" (the configuration's own name), at most <see cref="MaxStem"/> characters and never
    /// half of a character of two.
    /// </summary>
    public static string FileStem(string title)
    {
        var chars = title.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsControl(c) ? ' ' : c).ToArray();
        var stem = string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.', ' ');
        if (stem.Length > MaxStem)
        {
            // Never between the two units of one character (an emoji): Windows refuses a name that ends in half of one.
            var cut = char.IsHighSurrogate(stem[MaxStem - 1]) ? MaxStem - 1 : MaxStem;
            stem = stem[..cut].TrimEnd('.', ' ');
        }

        // Windows maps a name whose first part is a device's (con, nul, com1) to the device, whatever follows the dot:
        // the file could not be written and every refresh failed on it (security audit S-55). An underscore in front.
        // The same before a full stop at the start: a list does not show a file whose name starts with one (IA-4.6),
        // so an item titled ".NET upgrade" was written and never seen again (until 2026-11-27).
        // And before the one name an item's file cannot have, its list's configuration's: a new item titled "list"
        // in a list that had no list.json yet was written over the configuration made in the same step.
        if (Devices.Contains(stem.Split('.')[0].TrimEnd()) || stem.StartsWith('.') || stem.Equals(ConfigurationStem, StringComparison.OrdinalIgnoreCase))
        {
            stem = "_" + stem;
        }

        return stem.Length == 0 ? "Untitled" : stem;
    }

    /// <summary>The longest name a Title gives, in characters as .NET counts them.</summary>
    public const int MaxStem = 120;

    /// <summary>The name of a list's configuration without ".json" (<see cref="ListConfig.FileName"/>): never an item's.</summary>
    private static readonly string ConfigurationStem = Path.GetFileNameWithoutExtension(ListConfig.FileName);

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
