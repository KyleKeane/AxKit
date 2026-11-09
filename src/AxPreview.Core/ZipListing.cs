using System.IO.Compression;
using AxKit.Core;

namespace AxPreview.Core;

/// <summary>
/// A zip archive's listing (SPEC.md AP-7.2, 3): one entry to a line, its path, its unpacked size and its date
/// ("docs/SPEC.md, 41 KB, 2026-10-24 14:02"), in File Explorer's order of names; a folder entry as "docs/, folder".
/// The first <see cref="MaxEntries"/> entries, then one line "and N more"; the counts are of every entry. Only the
/// archive's directory is read, never what it packs. Tested in <c>tests/AxPreview.Tests/RenderersTests</c>.
/// </summary>
public static class ZipListing
{
    /// <summary>How many entries are listed: a listing is read line by line, and an archive of tens of thousands of entries would fill the window with lines nobody reads to the end (the user's choice of 2026-11-09).</summary>
    public const int MaxEntries = 1000;

    public static Rendered Render(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entries = zip.Entries.OrderBy(Name, NaturalOrder.Instance).ToList();
        var folders = entries.Count(IsFolder);
        var lines = entries
            .Take(MaxEntries)
            .Select(entry => IsFolder(entry)
                ? $"{Name(entry)}, folder"
                : $"{Name(entry)}, {FileSize.Text(entry.Length)}, {entry.LastWriteTime.LocalDateTime:yyyy-MM-dd HH:mm}")
            .ToList();
        if (entries.Count > MaxEntries)
        {
            lines.Add($"and {entries.Count - MaxEntries:N0} more");
        }

        if (lines.Count == 0)
        {
            lines.Add($"{Path.GetFileName(path)} is an empty zip archive.");
        }

        var contents = new List<string> { "Format: Zip archive", $"Entries: {entries.Count:N0}" };
        if (entries.Count > MaxEntries)
        {
            contents.Add($"Shown: the first {MaxEntries:N0} entries");
        }

        contents.AddRange(
        [
            $"Files: {entries.Count - folders:N0}",
            $"Folders: {folders:N0}",
            $"Packed: {FileDetails.Size(entries.Sum(entry => entry.CompressedLength))}",
            $"Unpacked: {FileDetails.Size(entries.Sum(entry => entry.Length))}",
        ]);
        return Rendered.Of(lines, "Zip archive", contents, WayOut.Windows);
    }

    /// <summary>An entry's path with forward slashes, as the format has them; Windows PowerShell 5.1 writes backslashes.</summary>
    private static string Name(ZipArchiveEntry entry) => entry.FullName.Replace('\\', '/');

    /// <summary>A folder entry: a name ending in a slash and nothing in it.</summary>
    private static bool IsFolder(ZipArchiveEntry entry) => Name(entry).EndsWith('/') && entry.Length == 0;
}
