namespace AxKit.Core;

/// <summary>
/// A file's size as people say it (from axFile's <c>FolderListing.FormatSize</c> on its second use, axPreview's
/// details and listings, 2026-10-24): "512 bytes", "12 KB", "1.2 MB", "3.5 GB", in units of 1024.
/// </summary>
public static class FileSize
{
    /// <summary>"512 bytes", "12 KB", "1.2 MB", "3.5 GB".</summary>
    public static string Text(long bytes)
    {
        const double kilo = 1024;
        if (bytes < 1024)
        {
            return bytes == 1 ? "1 byte" : $"{bytes} bytes";
        }

        // The unit is chosen by the number as it will be said, after its rounding: one byte under a megabyte is
        // "1 MB", never "1024 KB" (which it was until 2026-11-27, and "1024 MB" one under a gigabyte).
        if (Math.Round(bytes / kilo) < 1024)
        {
            return $"{Math.Round(bytes / kilo)} KB";
        }

        return Math.Round(bytes / (kilo * kilo), 1) < 1024
            ? $"{bytes / (kilo * kilo):0.#} MB"
            : $"{bytes / (kilo * kilo * kilo):0.#} GB";
    }
}
