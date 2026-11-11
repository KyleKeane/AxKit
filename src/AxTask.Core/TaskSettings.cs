using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// Everything axTask remembers between runs (SPEC.md TK-7), in <c>%APPDATA%\AxKit\axTask.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, the top folder, the list open last, whether done tasks are hidden, and the project folders
/// (TK-7b.1).
/// </summary>
public sealed class TaskSettings : WindowSettings
{
    /// <summary>The top folder of lists; null for <see cref="ItemFolder.DefaultFolder"/>.</summary>
    public string? Folder { get; set; }

    /// <summary>The list open last, from the top folder ("Home", "Work\Course plan"), opened again at the next start; null for the top.</summary>
    public string? LastList { get; set; }

    public bool HideDone { get; set; }

    /// <summary>
    /// The folders axTask looks in for the user's other folders of lists (SPEC.md TK-7b.1, TK-D6): none by default; each a
    /// full path, kept in the order added.
    /// </summary>
    public List<string> ProjectFolders { get; set; } = [];

    /// <summary>Why a typed path cannot be a new project folder (TK-7b.1): blank, not a full path, not there, or there already; null when it can.</summary>
    public string? RefusedProjectFolder(string typed)
    {
        var path = typed.Trim().Trim('"');
        if (path.Length == 0)
        {
            return "Type the folder's path";
        }

        if (!Path.IsPathFullyQualified(path))
        {
            return "Type the whole path, from the drive, such as D:\\Projects";
        }

        if (!Directory.Exists(path))
        {
            return "No folder is there: " + path;
        }

        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return ProjectFolders.FirstOrDefault(folder => folder.Equals(full, StringComparison.OrdinalIgnoreCase)) is { } there ? PathName.Of(there) + " is a project folder already" : null;
    }

    /// <summary>The folder added as a full path without a trailing separator; the path added is returned.</summary>
    public string AddProjectFolder(string typed)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(typed.Trim().Trim('"')));
        ProjectFolders.Add(full);
        return full;
    }

    public static string DefaultPath => AppPaths.SettingsFile("axTask");

    /// <summary>The top folder in use: the chosen one, or the default.</summary>
    public string TopFolder => Folder ?? ItemFolder.DefaultFolder;

    public static TaskSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TaskSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    private void Sanitize()
    {
        SanitizeWindow();
        if (string.IsNullOrWhiteSpace(Folder))
        {
            Folder = null;
        }

        // TK-7b.1: a project folder is a full path, once; a blank, relative or repeated one is dropped.
        var folders = new List<string>();
        foreach (var folder in ProjectFolders ?? [])
        {
            if (!string.IsNullOrWhiteSpace(folder) && Path.IsPathFullyQualified(folder.Trim()) && !folders.Contains(folder.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(folder.Trim());
            }
        }

        ProjectFolders = folders;

        if (string.IsNullOrWhiteSpace(LastList) || LastList.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(LastList))
        {
            LastList = null;
        }
    }
}
