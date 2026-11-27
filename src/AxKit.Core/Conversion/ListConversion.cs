using System.Text;

namespace AxKit.Core.Conversion;

/// <summary>One file the conversion writes: where, the text it holds now (null for a file that is not there) and the text it gets.</summary>
public sealed record ConvertedFile(string Path, string? Before, string After);

/// <summary>A folder's conversion as it would be done: the files to write, and what the conversion has to say, a sentence each.</summary>
public sealed record ConvertedFolder(IReadOnlyList<ConvertedFile> Files, IReadOnlyList<string> Notes);

/// <summary>
/// The folder's side of the conversion (docs/axTask/forms-framework.md AF-13.6): whether a list on disk is of the
/// earlier shape, what converting it would write (<see cref="EarlierFormat.Convert"/> over the folder's files), the
/// copy kept before anything is written, and the writing itself. A caller that keeps undo steps (axTask) takes the
/// plan and writes it its own way; one that does not (the report form's list) calls <see cref="Apply"/>.
/// </summary>
public static class ListConversion
{
    private static readonly UTF8Encoding Utf8 = new(false);

    /// <summary>
    /// Whether the folder is a list of the earlier shape: its <c>list.json</c> is, or one of its item files is (a
    /// list without a file of its own, or one that an earlier version of axKit wrote an item into). A folder that
    /// cannot be read is none, and a file that cannot be read is no file of the earlier shape: the others still
    /// count (until 2026-11-27 one unreadable file hid the whole list's shape).
    /// </summary>
    public static bool IsEarlier(string folder)
    {
        try
        {
            return Directory.Exists(folder) && JsonFiles(folder).Any(IsEarlierFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsEarlierFile(string path)
    {
        try
        {
            var text = TextDocument.Read(path).Text;
            return IsListFile(path) ? EarlierFormat.IsEarlierList(text) : EarlierFormat.IsEarlierItem(text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// What converting the folder writes: its <c>list.json</c> (when the conversion changes or makes it) and every
    /// item of the earlier shape, each with the text it holds now. Nothing is written here.
    /// </summary>
    public static ConvertedFolder Plan(string folder)
    {
        var listFile = Path.Combine(folder, ListConfig.FileName);
        var listText = File.Exists(listFile) ? TextDocument.Read(listFile).Text : null;
        var items = JsonFiles(folder).Where(path => !IsListFile(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => (FileName: Path.GetFileName(path), TextDocument.Read(path).Text))
            .ToList();
        var converted = EarlierFormat.Convert(listText, items);

        var files = new List<ConvertedFile>();
        if (converted.Config != listText)
        {
            files.Add(new ConvertedFile(listFile, listText, converted.Config));
        }

        foreach (var item in converted.Items)
        {
            files.Add(new ConvertedFile(Path.Combine(folder, item.FileName), items.First(read => read.FileName == item.FileName).Text, item.Text));
        }

        return new ConvertedFolder(files, converted.Notes);
    }

    /// <summary>
    /// The copy kept before a conversion (AF-13.6): the folder's files copied into <paramref name="into"/> under the
    /// folder's name and ", before conversion" (a number after it when that name is taken); the copy's path. Only the
    /// folder's own files: a list holds no lists.
    /// </summary>
    public static string KeepCopy(string folder, string into)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) + ", before conversion";
        var copy = Path.Combine(into, name);
        for (var n = 2; Directory.Exists(copy) || File.Exists(copy); n++)
        {
            copy = Path.Combine(into, $"{name} {n}");
        }

        Directory.CreateDirectory(copy);
        try
        {
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                File.Copy(file, Path.Combine(copy, Path.GetFileName(file)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Half a copy is no copy: it is taken away again, and the failure thrown on.
            RemoveCopies([copy]);
            throw;
        }

        return copy;
    }

    /// <summary>
    /// Several folders made ready for their conversion, whole or not at all: every folder is planned before the
    /// first copy is made, and a copy that cannot be made takes the copies before it away again, so that a
    /// conversion that fails here has changed nothing, as its message says (until 2026-11-27 each folder's copy was
    /// made before the next folder was planned, and a failure left the copies in the Archive). The folders with
    /// nothing to convert are left out. <paramref name="archiveOf"/> gives the folder a list's copy goes into; it
    /// is made when it is not there.
    /// </summary>
    public static (IReadOnlyList<(string Folder, ConvertedFolder Plan)> Plans, IReadOnlyList<string> Copies) Prepare(IReadOnlyList<string> folders, Func<string, string> archiveOf)
    {
        var plans = folders.Select(folder => (Folder: folder, Plan: Plan(folder))).Where(planned => planned.Plan.Files.Count > 0).ToList();
        var copies = new List<string>();
        try
        {
            foreach (var (folder, _) in plans)
            {
                var archive = archiveOf(folder);
                Directory.CreateDirectory(archive);
                copies.Add(KeepCopy(folder, archive));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RemoveCopies(copies);
            throw;
        }

        return (plans, copies);
    }

    /// <summary>The copies of a conversion that did not happen taken away again; one that cannot be removed stays.</summary>
    public static void RemoveCopies(IReadOnlyList<string> copies)
    {
        foreach (var copy in copies)
        {
            try
            {
                if (Directory.Exists(copy))
                {
                    Directory.Delete(copy, recursive: true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // It stays among the Archived lists, a copy more than needed.
            }
        }
    }

    /// <summary>The plan written: each file whole, through a file beside it and a move, so that a failure leaves a file as it was.</summary>
    public static void Apply(ConvertedFolder plan)
    {
        foreach (var file in plan.Files)
        {
            SafeFile.WriteAllText(file.Path, file.After, Utf8);
        }
    }

    private static bool IsListFile(string path) => Path.GetFileName(path).Equals(ListConfig.FileName, StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder's JSON files that could be a list's: no smart list's filter, nothing hidden.</summary>
    private static IEnumerable<string> JsonFiles(string folder) =>
        new DirectoryInfo(folder).EnumerateFiles("*.json")
            .Where(file => !file.Name.StartsWith('.') && (file.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0
                && !file.Name.EndsWith(".filter.json", StringComparison.OrdinalIgnoreCase))
            .Select(file => file.FullName);
}
