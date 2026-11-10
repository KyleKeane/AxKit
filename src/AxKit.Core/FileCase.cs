namespace AxKit.Core;

/// <summary>
/// A file's name in the case the bundle writes it (docs/axKit/SPEC.md AX-5.1, 2026-11-10): the settings and log
/// files carry the app's spoken name, <c>axDown.json</c>, since NVDA reads an all-lower-case name oddly; a file an
/// earlier version left as <c>axdown.json</c> is renamed to the case asked for the first time it is read, so that what
/// the user hears in axFile or Explorer is the app's name. Windows finds the file either way; only its name changes.
/// </summary>
public static class FileCase
{
    /// <summary>Renames the file at <paramref name="path"/> to that path's own spelling when it exists under another case; nothing otherwise, and nothing when the rename fails.</summary>
    public static void Match(string path)
    {
        try
        {
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folder) || name.Length == 0 || !Directory.Exists(folder))
            {
                return;
            }

            // On Windows the search is case-insensitive and gives the name as it is on disk.
            var actual = Directory.EnumerateFiles(folder, name).FirstOrDefault();
            if (actual is null)
            {
                return;
            }

            var actualName = Path.GetFileName(actual);
            if (!string.Equals(actualName, name, StringComparison.Ordinal) && string.Equals(actualName, name, StringComparison.OrdinalIgnoreCase))
            {
                File.Move(actual, path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The old name stays; the file is still read.
        }
    }
}
