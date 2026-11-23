using AxKit.Core;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- project folder ----

    /// <summary>Session → Show current folder in (FR-8.2): the project folder, or "No project folder" said.</summary>
    private string? FolderOrSay()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
        }

        return _folder;
    }

    /// <summary>Session → Show current folder in → File Explorer, Ctrl+W (FR-8.2): the folder in File Explorer, one of the system's own windows (D23); "Folder missing" is the base's (AX-9.10).</summary>
    private void ShowInExplorer()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
            return;
        }

        OpenInExplorer(_folder);
    }

    /// <summary>What the bundle's folder picker (one of the few separate windows, D23; the console window's PickFolder) says it is for.</summary>
    protected override string FolderPickerText => "Choose the project folder for Claude";

    /// <summary>Makes the folder the project folder: the title and the status bar follow, and the default arguments apply there again (FR-9.1).</summary>
    protected override void SetFolder(string folder)
    {
        if (_folder is { } old && !string.Equals(old, folder, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the folder left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember(AppName, old);
        }

        _folder = folder;
        _nothingToContinue = false;
        UpdateFolderUi();
    }

    /// <summary>Session → Open folder… (FR-8.3): the picker, then Claude restarts in the chosen folder.</summary>
    private void OpenFolder()
    {
        if (PickFolder(_folder) is { } folder)
        {
            SwitchFolder(folder);
        }
    }

    /// <summary>Session → Open folder in new window… (FR-8.3): the picker, then Claude in that folder in a window of its own; this session goes on.</summary>
    private void OpenFolderInNewWindow()
    {
        if (PickFolder(_folder) is { } folder)
        {
            OpenInNewWindow(folder);
        }
    }


    /// <summary>The project folder, for the bundle's Recently closed list (AX-9.4).</summary>
    protected override string? PathInHand => _folder;

    /// <summary>
    /// Claude again in another folder (Open folder, Open recent folder; the console window's SwitchFolder, which
    /// checks the folder, sets it and says "Folder name" first), with a system line. Another folder is another
    /// conversation (FR-8.3): the window is cleared, and what Claude prints there, a replay with its blocks when
    /// the folder has one, arrives as at the first start of the app.
    /// </summary>
    protected override void StartInFolder(string folder) =>
        Relaunch($"Project folder changed to {folder}", clearConversation: true);
}
