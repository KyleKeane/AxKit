using AxGit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>axGIT's window, the three lists of a repository and their reads (AG-2.2), and the status bar (AG-2.3).</summary>
public sealed partial class GitForm
{
    // ---- the lists (AG-2.2): three levels under the repository, each read through git off the UI thread ----

    /// <summary>
    /// F5, and every command's end: the status read again, and the list shown with it; at the repository's level
    /// all three, so that each row has its count. The list is filled on the row you are on.
    /// </summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        // The status first: the status bar and the commit need it whatever the list shown.
        if (!await LoadAsync(View.Changes))
        {
            return;
        }

        foreach (var view in _level switch { null => new[] { View.Log, View.Branches }, View.Changes => [], var one => [one.Value] })
        {
            await LoadAsync(view);
        }

        Fill(SelectedKey());
        UpdateStatusBar();
        if (announce)
        {
            Announce("Refreshed: " + Navigator.AccessibleName, true);
        }
    }

    /// <summary>
    /// Reads one list from git into its records: the status for Changes (<see cref="_repo"/>, which the status bar
    /// and the commit need too), the log, the branches. False when git did not run or failed, which is said.
    /// </summary>
    private async Task<bool> LoadAsync(View view)
    {
        var (arguments, verb, title) = view switch
        {
            View.Changes => (GitStatus.Arguments, "status", "Status failed"),
            View.Log => (GitLog.Arguments, "log", "Log failed"),
            _ => (GitBranches.Arguments, "branches", "Branches failed"),
        };
        var result = await RunGitAsync(arguments, GitRunner.Short, verb);
        if (result is null)
        {
            return false;
        }

        if (!result.Ok)
        {
            Failed(title, result);
            return false;
        }

        switch (view)
        {
            case View.Changes:
                _repo = GitStatus.Parse(result.Output);
                break;
            case View.Log:
                _commits = GitLog.Parse(result.Output);
                break;
            default:
                _branches = GitBranches.Parse(result.Output);
                break;
        }

        _loaded[(int)view] = true;
        return true;
    }

    /// <summary>The lists to read again before they show next: a commit, a switch, a fetch changed what they hold.</summary>
    private void Stale(params View[] views)
    {
        foreach (var view in views)
        {
            _loaded[(int)view] = false;
        }
    }

    /// <summary>
    /// Into a list: Right or Enter on its row at the repository's level, or Alt+1, 2 and 3 from anywhere. Read on
    /// the first visit or when stale, then shown on the row it was left on, and its name said with that row
    /// (axClaude D26). The level changes only once the read is in, so that the rows shown are always the level's.
    /// </summary>
    private async Task ShowListAsync(View view)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (_busy is not null)
        {
            // The list's first read would be refused.
            Announce("Working…", true);
            return;
        }

        if (!_loaded[(int)view] && !await LoadAsync(view))
        {
            return;
        }

        if (_level is { } from)
        {
            _kept[(int)from] = SelectedKey();
        }

        _level = view;
        Fill(_kept[(int)view]);
        var name = Navigator.AccessibleName ?? string.Empty;
        Announce(Navigator.SelectedItem is string item ? $"{name}. {item}" : name, true);
    }

    private ChangedFile? CurrentFile() => SelectedRow()?.Tag as ChangedFile;

    private Commit? CurrentCommit() => SelectedRow()?.Tag as Commit;

    private Branch? CurrentBranch() => SelectedRow()?.Tag as Branch;

    /// <summary>
    /// The files a command of Changes acts on (AG-3.2, bundle AX-8.16): the selected files, or the file you are on
    /// when none is selected. Null, and said, when the list shown is not Changes or holds no file.
    /// </summary>
    private IReadOnlyList<ChangedFile>? FilesInHand()
    {
        if (_level != View.Changes)
        {
            Announce("For the files of Changes: " + BundleKeys.Text(AxGitKeys.ViewChanges).TrimEnd('.'), true);
            return null;
        }

        IReadOnlyList<ChangedFile> files = SelectedRows() is { Count: > 0 } rows
            ? [.. rows.Select(row => row.Tag).OfType<ChangedFile>()]
            : CurrentFile() is { } file ? [file] : [];
        if (files.Count == 0)
        {
            Announce("No file selected", true);
            return null;
        }

        return files;
    }

    // ---- the status bar (AG-2.3) ----

    /// <summary>The status bar shown again: what runs or the branch and the counts on the left (<see cref="StatusState"/>), the repository's path on the right.</summary>
    private void UpdateStatusBar()
    {
        SetPlace(_root ?? string.Empty);
        ShowStatus();
    }

    /// <summary>An item that runs git: disabled while a command runs and when git is missing.</summary>
    private ToolStripMenuItem Git(ToolStripMenuItem item)
    {
        _gitItems.Add(item);
        return item;
    }
}
