using System.Text;

namespace AxKit.Core;

/// <summary>
/// A file written whole or not at all (docs/axKit/repairs.json R-22, principle P6): the bytes go to a temporary file of
/// its own name beside the target, and that file is put in the target's place in one step. A crash, a full disk or a
/// lost connection in the middle of a write leaves the file as it was, never half of the new text and none of the old.
/// Every file the user or the app depends on is written this way: a document an editor saves, an axTask item, a
/// settings file, the theme, a report, a key. A file made new for the user to fill (an export, a template) need not be.
/// Where the swap is not possible the file is written in place, as before: safer where it can be, never stricter.
/// </summary>
public static class SafeFile
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>The text as UTF-8 without a byte order mark.</summary>
    public static void WriteAllText(string path, string text) => WriteAllBytes(path, Utf8.GetBytes(text));

    public static void WriteAllText(string path, string text, Encoding encoding) =>
        WriteAllBytes(path, [.. encoding.GetPreamble(), .. encoding.GetBytes(text)]);

    public static void WriteAllBytes(string path, byte[] bytes)
    {
        var full = Path.GetFullPath(path);
        // Beside the target, so that the last step is a rename on one volume; a name of its own, so that two windows
        // saving the same file (the recently closed list, the AI tools' settings) never write one temporary file.
        var temporary = $"{full}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                // On the disk before the rename: the old file is given up only for a new one that is really there.
                stream.Flush(flushToDisk: true);
            }

            PutInPlace(temporary, full);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The temporary file could not be made (a folder that takes no new file) or not put in place (another
            // program holds the target open in a way that lets it be written but not replaced): the file is written
            // in place, as every write was before, so that no save that worked then fails now. A target that cannot
            // be written at all throws here, as it always did.
            TryDelete(temporary);
            File.WriteAllBytes(full, bytes);
        }
    }

    /// <summary>The temporary file becomes the target. An existing target is replaced with its attributes and its creation time kept (File.Replace); where the volume cannot do that (some network shares), by a move over it.</summary>
    private static void PutInPlace(string temporary, string target)
    {
        if (!File.Exists(target))
        {
            File.Move(temporary, target);
            return;
        }

        try
        {
            File.Replace(temporary, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException)
        {
            // A target that went away in between, or a volume without the replace operation.
            File.Move(temporary, target, overwrite: true);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The write's own failure is the one to report.
        }
    }
}
