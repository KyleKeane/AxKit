using AxFile.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>
/// The history of the window's file operations (SPEC.md AF-9.2, AF-9.3): every operation is an entry of
/// <see cref="ActionLog"/>, which the operations (Operations) begin and end; the status bar shows the newest line
/// (<see cref="StatusState"/>), Ctrl+Z takes back the newest that can be (Operations' Undo), and Actions → Recent
/// operations lists them newest first in a notice, where Enter goes to where the chosen one left things. Also here:
/// the hand-overs to axMedia, Play folder and Add to queue (AF-5).
/// </summary>
public sealed partial class FileForm
{
    /// <summary>What Recent operations says before the first operation (AF-9.3).</summary>
    private const string NoOperationsText = "No operations yet";

    private readonly ActionLog _actions = new();

    // ---- the entries (AF-9.2): what the operations call; each changes the log and the status bar follows ----

    /// <summary>A running operation's entry, "Copying 3 items", in the folder shown.</summary>
    private ActionEntry Begin(string text)
    {
        var entry = _actions.Begin(text, _folder ?? string.Empty);
        UpdateStatusBar();
        return entry;
    }

    /// <summary>A running entry's count, "Compressing, 40 of 120 files".</summary>
    private void Progress(ActionEntry entry, string text)
    {
        _actions.Progress(entry, text);
        UpdateStatusBar();
    }

    /// <summary>The operation done: its line says what it did and, with an operation, can be undone and leads to its result.</summary>
    private void End(ActionEntry entry, string text, Operation? operation)
    {
        _actions.End(entry, text, operation);
        UpdateStatusBar();
    }

    /// <summary>The operation failed: "Failed: <reason>" in the history, and the failure said as every failure is (AF-4.5).</summary>
    private void FailAction(ActionEntry entry, string title, string reason)
    {
        _actions.Fail(entry, reason);
        UpdateStatusBar();
        Failed(title, reason);
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its entry, done.</summary>
    private void Record(string text, Operation operation)
    {
        _actions.Add(text, operation);
        UpdateStatusBar();
    }

    // ---- Recent operations (AF-9.3) ----

    /// <summary>
    /// Actions → Recent operations…: the window's operations, newest first, each "Copied 3 items, 21:04" (a running
    /// one "…, running"), in a notice with the bundle's list; Enter on one goes to where it left things, Escape leaves
    /// it. "No operations yet" before the first. The list is what the history held when it opened.
    /// </summary>
    private void ShowRecentOperations()
    {
        var recent = _actions.Recent();
        if (recent.Count == 0)
        {
            Announce(NoOperationsText, true);
            return;
        }

        ChooseFromList("Recent operations", $"The file operations of this window, newest first. Choose one to go to where it left things; {BundleKeys.Text(AxFileKeys.Undo).TrimEnd('.')} in the folder list undoes the newest.",
            "Recent operations", [.. recent.Select(entry => entry.HistoryLine)], 0, index => GoToOperation(recent[index]));
    }

    /// <summary>
    /// An operation chosen in Recent operations: the folder list shows where it left things, the entry it made or moved
    /// selected (<see cref="ShowInList"/>); a running one says it is still running.
    /// </summary>
    private void GoToOperation(ActionEntry entry)
    {
        if (entry.State == ActionState.Running)
        {
            Announce("Still running", true);
            return;
        }

        var (folder, select) = entry.Location;
        if (folder.Length == 0)
        {
            Announce("Folder not found", true);
            return;
        }

        // What the operation made may be gone since (renamed, undone): in the folder shown, the entry the user is on
        // stays selected rather than the list jumping to its first entry.
        if (select is not null && string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase)
            && !ShownEntries().Any(shown => string.Equals(shown.Name, select, StringComparison.OrdinalIgnoreCase)))
        {
            select = CurrentEntry()?.Name;
        }

        ShowInList(folder, select, remember: false);
    }

    /// <summary>
    /// The folder list on <paramref name="folder"/> with <paramref name="select"/> selected, for Recent operations and
    /// for Enter in the Favorites list: the list takes the focus, which moves before the folder is said, so that NVDA's
    /// reading of the list does not cut the announcement off (AF-3.1), and the folder is said when it is another than
    /// the one shown. A folder that is gone is ShowFolderAsync's "Folder not found" notice and the nearest parent (AF-3.5).
    /// </summary>
    private void ShowInList(string folder, string? select, bool remember)
    {
        var changes = !string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase);
        Do(async () =>
        {
            if (!await ShowFolderAsync(folder, select, remember, announce: false))
            {
                return;
            }

            _list.Focus();
            if (changes)
            {
                Announce(FolderAnnouncement(), true);
            }
        });
    }

    // ---- the hand-overs to axMedia (AF-5): axFile starts the app on the paths and says nothing but the add's count ----

    /// <summary>Context → Play folder in axMedia (no key; Ctrl+Enter until 2026-11-26): axMedia on the folder shown, its media files from the first; "No sound or video files here" without any.</summary>
    private void PlayFolder()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        if (!ShownEntries().Any(entry => !entry.IsFolder && MediaFiles.IsMedia(entry.Path)))
        {
            Refuse("No sound or video files here");
            return;
        }

        WindowMenu.Start(Dispatch.AxMedia, folder);
    }

    /// <summary>
    /// Item → Add to axMedia's queue (Ctrl+Shift+Q): the entry, or the marked entries (AF-3.7), to the end of axMedia's
    /// queue, a folder as its media files; "Added 12 to axMedia's queue", the focus staying here (AM-D3). The count is
    /// this window's own reading of the folders, on a task.
    /// </summary>
    private void AddToQueue()
    {
        if (NoFolderHere())
        {
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        Do(async () =>
        {
            var count = await Task.Run(() => MediaCount(targets));
            if (count == 0)
            {
                Refuse("No sound or video files to add");
                return;
            }

            WindowMenu.StartWith(Dispatch.AxMedia, [.. targets.Select(target => target.Path), "--add"]);
            Announce($"Added {count} to axMedia's queue", true);
        });
    }

    /// <summary>How many media files the entries hold: a media file itself, a folder's media files (the hidden ones as the settings say), anything else none.</summary>
    private int MediaCount(List<Entry> targets)
    {
        var count = 0;
        foreach (var target in targets)
        {
            if (!target.IsFolder)
            {
                count += MediaFiles.IsMedia(target.Path) ? 1 : 0;
                continue;
            }

            try
            {
                count += FolderListing.Read(target.Path, _settings.ShowHidden, _settings.ShowDotFiles).Entries.Count(entry => !entry.IsFolder && MediaFiles.IsMedia(entry.Path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("A folder could not be read for the queue", ex);
            }
        }

        return count;
    }
}
