using AxGit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>axGIT's window, the commands of the three views: changes (AG-3), history (AG-4) and branches (AG-5).</summary>
public sealed partial class GitForm
{
    // ---- changes (AG-3) ----

    /// <summary>
    /// Actions → Stage, Unstage (AG-3.2; Space staged or unstaged the one file until 2026-11-22): the selected
    /// files, or the file you are on when none is selected. Stage takes the files with anything unstaged, Unstage
    /// the files with anything staged; a conflict is never staged from here. One sentence: "Staged src/Foo.cs",
    /// "Staged 3 files", or why nothing was done. The selection stays, so that Commit can follow.
    /// </summary>
    private async Task StageFilesAsync(bool stage)
    {
        if (FilesInHand() is not { } files)
        {
            return;
        }

        var able = files.Where(file => stage ? file.Unstaged && file.CanStage : file.Staged).ToList();
        if (able.Count == 0)
        {
            Announce(NothingToStage(files, stage), true);
            return;
        }

        var paths = able.Select(file => file.Path).ToList();
        for (var at = 0; at < paths.Count; at += GitTexts.PathsPerCall)
        {
            var some = paths.Skip(at).Take(GitTexts.PathsPerCall);
            var result = await RunGitAsync(stage ? GitTexts.StageArguments(some) : GitTexts.UnstageArguments(some), GitRunner.Short, stage ? "stage" : "unstage");
            if (result is null)
            {
                return;
            }

            if (!result.Ok)
            {
                Failed(stage ? "Stage failed" : "Unstage failed", result);
                await RefreshAsync(announce: false);
                return;
            }
        }

        var verb = stage ? "Staged" : "Unstaged";
        Announce(able.Count == 1 ? $"{verb} {able[0].Path}" : $"{verb} {able.Count} files", true);
        await RefreshAsync(announce: false);
    }

    /// <summary>Why Stage or Unstage did nothing: a conflict, a file already staged or not staged, or none among the selected.</summary>
    private static string NothingToStage(IReadOnlyList<ChangedFile> files, bool stage)
    {
        if (files.Count > 1)
        {
            return stage ? $"Nothing to stage among the {files.Count} selected files" : $"Nothing staged among the {files.Count} selected files";
        }

        var file = files[0];
        return stage && !file.CanStage ? "A conflict: resolve it in the editor or the terminal first"
            : stage ? $"{file.Path} is already staged"
            : $"{file.Path} is not staged";
    }

    private async Task StageAllAsync(bool stage)
    {
        var result = await RunGitAsync(stage ? GitTexts.StageAllArguments : GitTexts.UnstageAllArguments, GitRunner.Short, stage ? "stage all" : "unstage all");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed(stage ? "Stage failed" : "Unstage failed", result);
            return;
        }

        Announce(stage ? "Staged all" : "Unstaged all", true);
        await RefreshAsync(announce: false);
    }

    /// <summary>Enter on a file (AG-3.3): its diff in the details, with the focus on the first line; an untracked file shows itself.</summary>
    private async Task ShowDiffAsync()
    {
        if (_root is null || CurrentFile() is not { } file)
        {
            Announce(_root is null ? "No repository" : "No file selected", true);
            return;
        }

        if (file.Kind == ChangeKind.Untracked)
        {
            var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
            // Its first 200 KB, and no more of it read (AxGit.Core/UntrackedFile).
            string text;
            bool cut;
            try
            {
                (text, cut) = await UntrackedFile.ReadAsync(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail($"{file.Path} could not be read: {ex.Message}");
                return;
            }

            ShowText($"Diff of {file.Path}", UntrackedFile.Details(text, cut), focus: true);
            return;
        }

        var result = await RunGitAsync(file.DiffArguments, GitRunner.Short, "diff");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Diff failed", result);
            return;
        }

        if (result.Output.Trim().Length == 0)
        {
            Announce("No changes", true);
            return;
        }

        ShowText($"Diff of {file.Path}", result.Output, focus: true);
    }

    /// <summary>
    /// Context → Discard changes… (Delete, AG-3.4): a question first; an untracked file is not deleted from here.
    /// With files selected, the changes to all of them after one question (<see cref="DiscardSeveral"/>).
    /// </summary>
    private void Discard()
    {
        if (FilesInHand() is not { } files)
        {
            return;
        }

        if (files.Count > 1)
        {
            DiscardSeveral(files);
            return;
        }

        var file = files[0];
        if (file.Kind == ChangeKind.Untracked)
        {
            Announce("Untracked file: delete it in File Explorer", true);
            return;
        }

        ShowNotice(Notice.Ask($"Discard the changes to {file.Path}?", "They cannot be brought back.", "&Discard", () => Do(async () =>
        {
            var result = await RunGitAsync(file.DiscardArguments, GitRunner.Short, "discard");
            if (result is null)
            {
                return;
            }

            if (!result.Ok)
            {
                Failed("Discard failed", result);
                return;
            }

            Announce($"Discarded {file.Path}", true);
            await RefreshAsync(announce: false);
        })));
    }

    /// <summary>
    /// Delete with several files selected (AG-3.4a, bundle AX-8.16): one question that names them, Cancel the
    /// default, since what is discarded cannot be brought back; the untracked files among them are named as left
    /// alone, and with only such files selected nothing is asked. One sentence, "Discarded 3 files"; the selection
    /// given up and the list read again.
    /// </summary>
    private void DiscardSeveral(IReadOnlyList<ChangedFile> files)
    {
        var tracked = files.Where(file => file.Kind != ChangeKind.Untracked).ToList();
        var untracked = files.Count - tracked.Count;
        if (tracked.Count == 0)
        {
            Announce("Untracked files: delete them in File Explorer", true);
            return;
        }

        var text = "They cannot be brought back.\n" + string.Join("\n", tracked.Select(file => file.Path));
        if (untracked > 0)
        {
            text += $"\nLeft alone: {untracked} untracked {(untracked == 1 ? "file" : "files")}, which File Explorer deletes.";
        }

        ShowNotice(Notice.Ask($"Discard the changes to {tracked.Count} {(tracked.Count == 1 ? "file" : "files")}?", text, "&Discard", () => Do(async () =>
        {
            var paths = tracked.Select(file => file.Path).ToList();
            for (var at = 0; at < paths.Count; at += GitTexts.PathsPerCall)
            {
                var result = await RunGitAsync(GitTexts.DiscardArguments(paths.Skip(at).Take(GitTexts.PathsPerCall)), GitRunner.Short, "discard");
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    Failed("Discard failed", result);
                    await RefreshAsync(announce: false);
                    return;
                }
            }

            Announce(tracked.Count == 1 ? $"Discarded {tracked[0].Path}" : $"Discarded {tracked.Count} files", true);
            Selection.Clear();
            await RefreshAsync(announce: false);
        }), cancelIsDefault: true));
    }

    /// <summary>Changes → Commit… (Ctrl+Enter, AG-3.5): the message in a notice, then git commit.</summary>
    private void Commit()
    {
        if (_root is null)
        {
            Refuse("No repository");
            return;
        }

        if (RefusedWhileWorking())
        {
            // A stage or its refresh still runs: the count below is the one before it (found by tools/AxAutomate,
            // 2026-10-23: the stage key then Ctrl+Enter said "Nothing staged").
            return;
        }

        var staged = _repo.StagedCount;
        if (staged == 0)
        {
            Refuse($"Nothing staged. {BundleKeys.Text(AxGitKeys.Stage).TrimEnd('.')} stages the selected files");
            return;
        }

        AskText($"Commit {staged} staged {(staged == 1 ? "file" : "files")}", string.Empty, "Message:", "Message", string.Empty, "&Commit", typed =>
        {
            var message = typed.Trim();
            Do(async () =>
            {
                var result = await RunGitAsync(GitTexts.CommitArguments(message), GitRunner.Short, "commit");
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    Failed("Commit failed", result);
                    return;
                }

                Announce(GitTexts.CommittedHash(result.Output) is { } hash ? $"Committed {hash}" : "Committed", true);
                Stale(View.Log, View.Branches);
                await RefreshAsync(announce: false);
            });
        }, emptyMessage: "Type the commit message", cancel: "C&ancel");
    }

    // ---- history (AG-4) ----

    private async Task ShowCommitAsync()
    {
        if (CurrentCommit() is not { } commit)
        {
            Refuse("No commit selected");
            return;
        }

        var result = await RunGitAsync(commit.ShowArguments, GitRunner.Short, "show");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Show failed", result);
            return;
        }

        ShowText($"Commit {commit.Hash}", result.Output, focus: true);
    }

    // ---- branches (AG-5) ----

    private async Task SwitchBranchAsync()
    {
        if (CurrentBranch() is not { } branch)
        {
            Refuse("No branch selected");
            return;
        }

        if (branch.Current)
        {
            Announce($"Already on {branch.Name}", true);
            return;
        }

        var result = await RunGitAsync(branch.SwitchArguments, GitRunner.Short, "switch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Switch failed", result);
            return;
        }

        Announce($"Switched to {branch.Name}", true);
        Stale(View.Log, View.Branches);
        await RefreshAsync(announce: false);
    }

    /// <summary>Branches → New branch… (Ctrl+N, AG-5.3): the name in a notice, then git switch -c.</summary>
    private void NewBranch()
    {
        if (_root is null)
        {
            Refuse("No repository");
            return;
        }

        AskText("New branch", "A new branch from the current commit, checked out at once.", "Name:", "Name", string.Empty, "C&reate", typed =>
        {
            var name = typed.Trim();
            Do(async () =>
            {
                var result = await RunGitAsync(GitBranches.CreateArguments(name), GitRunner.Short, "new branch");
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    Failed("New branch failed", result);
                    return;
                }

                Announce($"Switched to {name}", true);
                Stale(View.Log, View.Branches);
                await RefreshAsync(announce: false);
            });
        }, emptyMessage: "Type the branch name");
    }

    private async Task FetchAsync()
    {
        var result = await RunGitAsync(GitTexts.FetchArguments, GitRunner.Long, "fetch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Fetch failed", result);
            return;
        }

        Announce("Fetched", true);
        Stale(View.Branches);
        await RefreshAsync(announce: false);
    }

    private async Task PullAsync()
    {
        var result = await RunGitAsync(GitTexts.PullArguments, GitRunner.Long, "pull");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Pull failed", result);
            return;
        }

        Announce(GitTexts.SaysUpToDate(result.Output) ? "Already up to date" : "Pulled", true);
        Stale(View.Log, View.Branches);
        await RefreshAsync(announce: false);
    }

    private async Task PushAsync()
    {
        var result = await RunGitAsync(GitTexts.PushArguments(_repo.Upstream, _repo.Branch), GitRunner.Long, "push");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Push failed", result);
            return;
        }

        Announce("Pushed", true);
        Stale(View.Branches);
        await RefreshAsync(announce: false);
    }
}
