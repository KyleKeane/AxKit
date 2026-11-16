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

    /// <summary>Why a typed path cannot be a new project folder (TK-7b.1): blank, not a full path, not there, the top folder or inside it, or there already; null when it can.</summary>
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
        // TK-7b.5: the top folder is never a project, so a project folder in it would find nothing.
        if (Projects.Inside(full, TopFolder))
        {
            return full.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(TopFolder)), StringComparison.OrdinalIgnoreCase)
                ? "That is your own top folder: its lists show at the top already"
                : "That is inside your own top folder: its lists show at the top already";
        }

        return ProjectFolders.FirstOrDefault(folder => folder.Equals(full, StringComparison.OrdinalIgnoreCase)) is { } there ? PathName.Of(there) + " is a project folder already" : null;
    }

    /// <summary>The folder added as a full path without a trailing separator; the path added is returned.</summary>
    public string AddProjectFolder(string typed)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(typed.Trim().Trim('"')));
        ProjectFolders.Add(full);
        return full;
    }

    /// <summary>The most acceptances kept (TK-7a.3): the newest; beyond them the oldest goes, and its key asks once more.</summary>
    public const int MaxAcceptedActionKeys = 500;

    /// <summary>
    /// The action keys of the kinds that move or open (<see cref="ActionKeys.AsksFirst"/>) that this install wrote in
    /// Actions, Action keys or the user accepted by a second press (SPEC.md TK-7a.3; bundle repairs.md R-72), the
    /// newest last: each the seal of <see cref="ActionKeys.MarkText"/> under the key of the bundle's settings seal
    /// (<see cref="SettingsSeal.Of"/>, bundle AX-5.6), so that a line written into this file by hand or by another
    /// program accepts nothing. Nothing of a list is readable from it.
    /// </summary>
    public List<string> AcceptedActionKeys { get; set; } = [];

    /// <summary>The seal of one list's key and action for the settings file at <paramref name="settingsPath"/>; empty when no key of the seal can be had, and then nothing is ever accepted beyond the window's run.</summary>
    public static string ActionKeyMark(string settingsPath, string listFolder, int digit, ListAction action) =>
        SettingsSeal.Of(settingsPath, ActionKeys.MarkText(listFolder, digit, action));

    /// <summary>Whether the key's action in that list is one this install wrote or the user accepted.</summary>
    public bool AcceptsActionKey(string settingsPath, string listFolder, int digit, ListAction action) =>
        ActionKeyMark(settingsPath, listFolder, digit, action) is { Length: > 0 } mark && AcceptedActionKeys.Contains(mark);

    /// <summary>The key's action in that list is accepted from now on; false when nothing changed (it was already, or there is no seal).</summary>
    public bool AcceptActionKey(string settingsPath, string listFolder, int digit, ListAction action)
    {
        var mark = ActionKeyMark(settingsPath, listFolder, digit, action);
        if (mark.Length == 0 || AcceptedActionKeys.Contains(mark))
        {
            return false;
        }

        AcceptedActionKeys.Add(mark);
        if (AcceptedActionKeys.Count > MaxAcceptedActionKeys)
        {
            AcceptedActionKeys.RemoveRange(0, AcceptedActionKeys.Count - MaxAcceptedActionKeys);
        }

        return true;
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
        AcceptedActionKeys = [.. (AcceptedActionKeys ?? []).Where(mark => !string.IsNullOrWhiteSpace(mark)).Distinct().TakeLast(MaxAcceptedActionKeys)];
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
