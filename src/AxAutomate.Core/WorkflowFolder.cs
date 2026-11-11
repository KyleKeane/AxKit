using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>One entry of a folder of workflows: a folder inside it, or a workflow file.</summary>
public sealed record FolderEntry(string Path, string Name, bool IsFolder);

/// <summary>
/// A folder of workflows (SPEC.md AU-4.1, AU-4.2): its folders, then its workflows, by name in the natural order
/// (02 before 10); a workflow is a <c>.workflow.json</c> file or a script of the runner (<c>.txt</c>, AU-5.2). Hidden
/// and dot entries are left out, as axTask leaves them.
/// </summary>
public static class WorkflowFolder
{
    public static IReadOnlyList<FolderEntry> Read(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var info = new DirectoryInfo(folder);
        var folders = info.EnumerateDirectories()
            .Where(Shown)
            .Select(directory => new FolderEntry(directory.FullName, directory.Name, IsFolder: true));
        var files = info.EnumerateFiles()
            .Where(file => Shown(file) && IsWorkflow(file.Name))
            .Select(file => new FolderEntry(file.FullName, NameOf(file.Name), IsFolder: false));
        return folders.OrderBy(entry => entry.Name, NaturalOrder.Instance)
            .Concat(files.OrderBy(entry => entry.Name, NaturalOrder.Instance))
            .ToList();
    }

    public static bool IsWorkflow(string name) =>
        name.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) || name.EndsWith(ScriptReader.Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>A workflow file's name without its extension.</summary>
    public static string NameOf(string fileName) =>
        fileName.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) ? fileName[..^WorkflowFile.Extension.Length] : System.IO.Path.GetFileNameWithoutExtension(fileName);

    /// <summary>A workflow file read in the reader of its kind.</summary>
    public static Workflow ReadWorkflow(string path) =>
        path.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) ? WorkflowFile.Read(path) : ScriptReader.Read(path);

    /// <summary>How many workflows a folder holds, its folders' included.</summary>
    public static int CountWorkflows(string folder) =>
        Read(folder).Sum(entry => entry.IsFolder ? CountWorkflows(entry.Path) : 1);

    private static bool Shown(FileSystemInfo entry) =>
        !entry.Name.StartsWith('.') && (entry.Attributes & FileAttributes.Hidden) == 0;
}
