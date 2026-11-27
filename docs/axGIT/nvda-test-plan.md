# axGIT NVDA test plan

The files for this plan are in `manual_tests/axGIT`, made and reset by `.\manual_tests\prepare.ps1 axgit`; its `_main.md` says which folder or file each section uses.

Run with NVDA before a release, on a repository with a remote (the axKit repository itself does), after `scripts to run-individual-apps\run-axGIT.ps1 C:\path\to\repo` (or `.\run.ps1 -App git <repo>`). Make a scratch change first: edit a file, create a new one. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axGIT/SPEC.md` (AG-n.m).

## 1. Start (AG-1, AG-2)

1. Start axGIT on the repository: NVDA reads "<folder name> - axGIT window", then "Changes, 2 files list, Modified <file>, unstaged, 1 of 2" (or similar). Nothing else is spoken.
2. Down Arrow: the next item, nothing else. Tab: "Details, edit, read only", empty. Shift+Tab: the list, the same item as before. F6: the details again; Ctrl+Tab: the list.
3. NVDA+End: the status bar: "main · no upstream · 2 changed, 0 staged" in a repository without a remote (or the branch's state: "up to date", "2 ahead"), then the path.
4. Alt+1 while the Changes list shows: "Changes, 2 files". Alt+2: "Log, 200 commits list, <hash> <date> <author>: <subject>, 1 of 200". Alt+3: "Branches, N list, <branch>, current, …". Alt+1: back to Changes on the item you had. No menu opens and Windows makes no sound for Alt and the number; the menu bar stays closed when Alt is let go.
4a. The menus (bundle AX-9.1, AX-9.11): Alt, then Right along the bar: Session, Actions, Navigation, Context, Options, Window, axKit, Help. In Changes on a file, Shift+F10: Context opens on "Show diff Enter" and NVDA reads it; Escape. Alt+2, Shift+F10: "Show the commit Enter"; Alt+3, Shift+F10: "Switch to branch Enter". The Applications key does the same.
4a. In the Log (Alt+2), Space: the edge sound, since only files are selected; Delete: "A commit is not removed here"; Ctrl+Shift+S: "For the files of Changes: Alt+1"; the list stays.
4b. The levels (AG-2.2, since 2026-11-22): in Changes, Left: "<repository>, 3 lists, Changes, N files"; Left again: the edge sound. Down, Right: "Log, N commits. <commit>". Backspace: "<repository>, 3 lists, Log, N commits". Up, Enter: "Changes, N files. <the file it was left on>". Right on a file: the edge sound.
5. Start axGIT on a folder that is not a repository: "Not a git repository" notice naming the folder; Enter closes it; the title is "axGIT"; Ctrl+O opens the folder picker. With a repository open, Ctrl+O on a folder outside any repository: the same notice, and the repository stays open, its title unchanged.
6. Alt+F4 closes the window without a question.
7. A repository with a hook of its own (`manual_tests/axGIT/scratch-repository-with-a-hook`, made by `prepare.ps1 axgit`): opening it shows the notice "This repository runs programs of its own" naming "hook post-checkout"; Enter keeps them off and the Changes list comes. Open it again and choose Trust this repository: "Trusted"; the next opening shows no notice.

## 2. From and to the other windows (AG-1.4)

1. In axClaude on the repository, Alt+S, I: "Show current folder in submenu", Down: "File Explorer" (no key since 2026-11-26), Down: "axTerminal", Down: "axGIT"; Enter: a new axGIT window on the repository, focused.
2. Alt+W, O: the open windows, the axClaude window first; Enter goes back to it. Alt+W, O again, the axGIT window first now.
3. In axDown on a file of the repository, Alt+S, I, G: axGIT on the repository that holds the file.
4. In axGIT, Alt+S, I: "File Explorer" and "axClaude"; Enter on axClaude: a new axClaude on the repository.
5. In the Changes list on a `.md` file, Ctrl+E: axDown opens it; on a `.json` file, axJSON.

## 3. Changes (AG-3)

1. On an unstaged file, Ctrl+Shift+S: "Staged <path>"; Up or Down and back: the row reads "…, staged". Ctrl+Shift+S again: "<path> is already staged". Ctrl+Shift+U: "Unstaged <path>"; again: "<path> is not staged". NVDA+End: the staged count changed.
1a. Space on a file: "Selected", the row reads "…, selected"; Shift+Down takes the next; NVDA+End: "2 selected" after the counts. Ctrl+Shift+S: "Staged 2 files", both rows read "…, staged, selected"; again: "Nothing to stage among the 2 selected files". Ctrl+Shift+U: "Unstaged 2 files". Escape: "None selected".
2. Enter on a changed file: "Diff of <path>, N lines" and the focus in the details on its first line ("diff --git …"); Down Arrow reads the lines; a line with a plus is an addition. Ctrl+Tab: the list, the same item.
3. Enter on the untracked file: the details hold "Untracked file, N lines" and the file's text.
4. Actions, Stage all: "Staged all"; every row reads "…, staged". Actions, Unstage all: "Unstaged all".
5. Ctrl+Enter with nothing staged: "Nothing staged. Ctrl+Shift+S stages the selected files", no notice. Ctrl+Shift+S on a file, Ctrl+Enter: the notice "Commit 1 staged file" with the field "Message"; Enter on the empty field: the notice stays and says "Type the commit message"; Ctrl+Shift+S then Ctrl+Enter at once: "Working…" while the stage runs; type "Test commit", Enter: "Committed <hash>"; the Changes list no longer has the file; Alt+2: the commit is first in the log.
6. Edit a tracked file, F5: "Refreshed: Changes, 1 file"; on it, Delete: the notice "Discard the changes to <path>?" with Discard and Cancel; Escape: nothing happens; Delete, Enter: "Discarded <path>", the item is gone. On the untracked file, Delete: "Untracked file: delete it in File Explorer".
6a. Edit the tracked file again, F5, Ctrl+A: "2 selected". Delete: the notice "Discard the changes to 1 file?" with the path and "Left alone: 1 untracked file, which File Explorer deletes."; Enter: nothing is discarded, since Cancel is the default; Delete, Alt+D: "Discarded <path>", and nothing is selected.
7. Ctrl+S anywhere: "Nothing to save here. Ctrl+Shift+S stages, Ctrl+Enter commits".

## 4. Log and branches (AG-4, AG-5)

1. Alt+2, Enter on the first commit: "Commit <hash>, N lines", the focus in the details on "commit <full hash>"; Down Arrow reads the author, the date, the message and the files. Ctrl+Tab: the list.
2. Alt+3: the branches; the current one reads "…, current". Enter on it: "Already on <name>". Enter on another: "Switched to <name>"; NVDA+End: the status bar names it; Alt+1: the Changes of that branch. Switch back.
3. Ctrl+N: the notice "New branch" with the field "Name"; type "scratch-test", Enter: "Switched to scratch-test"; Alt+3 lists it as current. Switch back to the branch you had (Enter on it) and delete the test branch in a terminal (`git branch -d scratch-test`).
4. Ctrl+R: the status bar says "Working: fetch…" (F9), then "Fetched". Ctrl+L: "Pulled" or "Already up to date". Ctrl+P on a branch with nothing to push: "Pushed" or git's "Everything up-to-date" read as "Pushed". With the network off, Ctrl+R: the notice "Fetch failed" with git's message.
5. Press Ctrl+R and at once Ctrl+L: "Working…" for the second; the first finishes on its own.

## 5. Text, notices, the Window, axKit and Help menus (AG-2.4, AG-6)

1. In the details after a diff, Ctrl+F: the Find notice; type a word from the diff, Enter: the match selected in the details and read; F3: the next, and after the last match the first again; a word with no match: "Not found". Ctrl+Home, Page Down: a screen down; Ctrl+Down: the next paragraph.
2. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back. Options, Text, Windows text size: "Windows text size".
3. Help, Keyboard shortcuts: the notice; Escape closes it. F1: the command palette, Down reads the rules of the keys;
   Escape. Help, User guide: this guide; Escape.
4. Alt+W: the Window menu with Open windows, Recently closed, Close all windows, Say the status (F9: the status bar is said); Alt+H: Help with Command palette, Keyboard shortcuts, User guide, the resources, Send a report, Your reports, Diagnostics (Test a key and Copy diagnostics), Check for updates and About; Alt+K: the axKit menu with Apps, Access layers, Claude and AI tools; Alt+O: Options, ending after a separator with Theme, Open the sounds folder, Edit app keys file, Check for updates at start and Keep a log of events. About, Version: "axKit <version>" and axGIT named among the apps.
5. Help, Diagnostics, Copy diagnostics: "Copied diagnostics"; paste somewhere: the versions, the repository and the log's end.
6. Close the window and start axGIT without a folder: it opens on the same repository (the last one); Session, Open recent repository lists it as "1. <name> (<path>)".

## 6. The review of 2026-11-27 (AG-1.6a, AG-5.4a)

The runner's script for 1 is `tests/scripts/axGIT/07-own-programs.txt`.

1. A repository with `git config --worktree core.sshCommand x` (after `git config extensions.worktreeConfig true`) and a repository inside it with a hook: the notice "This repository runs programs of its own" names "core.sshcommand" and "submodule <path>: hook <name>"; Enter keeps them off.
2. While a large repository opens, F5 at once: "Working…", and the Changes list comes after the notice or by itself; nothing ran in between.
3. Enter on an untracked file of several megabytes: the details come at once, "Untracked file, the first N lines", and end with "(cut after 200 KB)".
4. With a remote whose push address is on another server (`git remote set-url --push origin <address>`): Push asks "Before axGIT connects to <that server>", not the fetch address's.

## 7. The review's second pass of 2026-11-27 (AG-1.6b, AG-1.6c, AG-5.4a, AG-5.4b)

The runner's script for 1 to 4 is `tests/scripts/axGIT/08-settings-and-servers.txt`.

1. A repository with a setting axGIT does not know and no program (`git config http.sslVerify false`): the notice is "This repository has settings axGIT does not know" and names "http.sslverify"; Enter keeps them off, and the Changes list comes. An ordinary clone opens with no notice.
2. In a trusted repository, add a hook file in a console while the window is open, then F5: the notice comes again, starting "What axGIT finds in this repository differs from what you trusted", before "Refreshed" is said. F5 once more: no notice a second time for the same hook.
3. With `git config branch.<branch>.pushRemote https://example.invalid/x.git`: Ctrl+P shows the notice "Push not started", which says Git could not say where the remote leads, and ends "Nothing was sent and nothing was fetched."; Enter closes it.
4. With HEAD on no branch (`git checkout --detach`): Ctrl+P: "Push not started", "HEAD is on no branch (detached), and axGIT pushes a branch."
5. By ear, with a real remote: Ctrl+R the first time says "Connecting to <server>; if Git asks you to sign in…" and then "Fetched"; in a repository that is not trusted and on a private server, the fetch fails with git's own words about the sign-in, since no credential helper runs.
