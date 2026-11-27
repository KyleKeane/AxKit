namespace AxGit.Core;

/// <summary>
/// An untracked file as the details show it (SPEC.md AG-3.3): git has no diff for it, so the file shows itself, its
/// first 200 KB. Only that much is read (the review of 2026-11-27): until then the whole file was read into memory
/// and cut afterwards, so that Enter on an untracked recording or disk image of some gigabytes filled the memory
/// before a line was shown.
/// </summary>
public static class UntrackedFile
{
    /// <summary>How many characters of the file the details show.</summary>
    public const int Limit = 200 * 1024;

    /// <summary>
    /// The start of the file as text, at most <paramref name="limit"/> characters, and whether the file goes on
    /// after them; UTF-8 unless the file starts with another encoding's mark. Throws as a file read does.
    /// </summary>
    public static async Task<(string Text, bool Cut)> ReadAsync(string path, int limit = Limit, CancellationToken cancellation = default)
    {
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        // One character more than is shown, to know whether there is more.
        var buffer = new char[limit + 1];
        var count = await reader.ReadBlockAsync(buffer.AsMemory(), cancellation).ConfigureAwait(false);
        return (new string(buffer, 0, Math.Min(count, limit)), count > limit);
    }

    /// <summary>The details' text: "Untracked file, 3 lines" as the first line, then the text, and a last line that says so when it was cut.</summary>
    public static string Details(string text, bool cut)
    {
        var lines = text.Length == 0 ? 0 : text.Split('\n').Length;
        var shown = cut ? text + "\n(cut after 200 KB)" : text;
        return cut ? $"Untracked file, the first {lines} lines\n{shown}" : $"Untracked file, {lines} lines\n{shown}";
    }
}
