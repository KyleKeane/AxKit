# axGIT: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Open a repository in axGIT

### Open a repository

1. Open axGIT with the folder repo.

### Move between the list and the details

1. Press Down.
    It reads Untracked new.txt.
2. Press Tab.
    The focus moves to Details (Text).
3. Press Shift+Tab.
    The focus moves to Changes, 2 files (List): Untracked new.txt.
4. Press F6 (Switch between the list and the text).
    The focus moves to Details (Text).
5. Press Ctrl+Tab (Switch between the list and the text).
    The focus moves to Changes, 2 files (List): Untracked new.txt.

### Go up to the repository and into another list

1. Press Left.
    You hear: repo, 3 lists, Changes, 2 files.
2. Press Left.
    You hear the sound Edge.
3. Press Down.
4. Press Right.
    You hear: Log, 2 commits.
5. Press Backspace.
    You hear: repo, 3 lists, Log, 2 commits.
6. Press Up.
7. Press Enter.
    You hear: Changes, 2 files. Untracked new.txt.

### Switch between the lists by their keys

1. Press Alt+1 (Changes; Navigation menu).
    You hear: Changes, 2 files. Untracked new.txt.
2. Press Alt+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
    You hear: Test: second.
3. Press Alt+3 (Branches; Navigation menu).
    You hear: Branches, 2. main, current, no upstream.
4. Press Alt+1 (Changes; Navigation menu).
    You hear: Changes, 2 files. Untracked new.txt.

### Close the window

1. Close the window.

## Review and commit changes

Starts with: Open axGIT with the folder repo.

### Stage and unstage a file

1. Press Ctrl+Shift+S.
    You hear: Staged a.txt.
2. Press Ctrl+Shift+S.
    You hear: a.txt is already staged.
3. Press Down.
    It reads Untracked new.txt.
4. Press Up.
    It reads Modified a.txt, staged.
5. Press Ctrl+Shift+U.
    You hear: Unstaged a.txt.
6. Press Ctrl+Shift+U.
    You hear: a.txt is not staged.

### Select files with Space and stage them together

1. Press Space.
    You hear: Selected.
2. Press Shift+Down.
3. Press Ctrl+Shift+S.
    You hear: Staged 2 files.
4. Press Ctrl+Shift+S.
    You hear: Nothing to stage among the 2 selected files.
5. Press Ctrl+Shift+U.
    You hear: Unstaged 2 files.
6. Press Escape.
    You hear: None selected.
7. Press Home.

### Read the diff of a changed file

1. Press Enter.
    You hear: Diff of a.txt, 10 lines.
2. Press Down.
    It reads index.
3. Press Down four times.
    It reads hello.
4. Press Down.
    It reads -world.
5. Press Down.
    It reads  No newline at end of file.
6. Press Down.
    It reads +changed world.
7. Press Ctrl+Tab (Switch between the list and the text).
    The focus moves to Changes, 2 files (List): Modified a.txt, unstaged.

### Read an untracked file

1. Press Down.
    It reads Untracked new.txt.
2. Press Enter.
    You hear: Diff of new.txt, 4 lines.
3. Press Down.
    It reads one.
4. Press Ctrl+Tab (Switch between the list and the text).
    The focus moves to Changes, 2 files (List): Untracked new.txt.

### Stage all and unstage all

1. Choose Stage all in the Actions menu.
    You hear: Staged all.
2. Choose Unstage all in the Actions menu.
    You hear: Unstaged all.

### Commit the staged changes

1. Press Up.
    It reads Modified a.txt, unstaged.
2. Press Ctrl+Shift+S.
    You hear: Staged a.txt.
3. Press Ctrl+Enter (Commit; Actions menu).
4. Press Enter.
    You hear: Type the commit message.
5. Type "Test commit".
6. Press Enter.
    You hear: Committed.
7. Press Alt+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
8. Press Alt+1 (Changes; Navigation menu).
    You hear: Changes, 1 file.

### Discard the changes to a file

1. Press F5 (Refresh; Session menu).
    You hear: Refreshed: Changes, 2 files.
2. Press Home.
3. Press Delete.
4. Press Escape.
5. Press Delete.
6. Press Enter.
    You hear: Discarded a.txt.

### Discard the changes to the selected files

1. Press F5 (Refresh; Session menu).
    You hear: Refreshed: Changes, 2 files.
2. Press Ctrl+A.
    You hear: 2 selected.
3. Press Delete.
4. Press Enter.
5. Press Delete.
6. Press Alt+D.
    You hear: Discarded a.txt.

## Read the history and switch branches

Starts with: Open axGIT with the folder repo.

### Read a commit of the log

1. Press Alt+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
2. Press Enter.
    You hear something with "8 lines" in it.
3. Press Down.
    It reads Author: Test.
4. Press Down.
    It reads Date.
5. Press Down twice.
    You hear: second commit.
6. Press Down twice.
    You hear: a.txt.
7. Press Ctrl+Tab (Switch between the list and the text).
    The focus moves to Log, 2 commits (List).

### Switch branches

1. Press Alt+3 (Branches; Navigation menu).
    You hear: Branches, 2.
2. Press Enter.
    You hear: Already on main.
3. Press Down.
    It reads other.
4. Press Enter.
    You hear: Switched to other.
5. Press Alt+1 (Changes; Navigation menu).
    You hear: Changes, no changes.
6. Press Alt+3 (Branches; Navigation menu).
    You hear: Branches, 2.
7. Press Up.
    It reads main.
8. Press Enter.
    You hear: Switched to main.

### Make a new branch

1. Press Ctrl+N (New branch; Actions menu).
2. Type "scratch-test".
3. Press Enter.
    You hear: Switched to scratch-test.
4. Press Alt+3 (Branches; Navigation menu).
    You hear: Branches, 3.
5. Press Home.
6. Press Enter.
    You hear: Switched to main.

### Fetch, pull and push

1. Press Ctrl+R (Fetch; Actions menu).
    You hear: Fetched.
2. Press Ctrl+L (Pull; Actions menu).
    You hear: Pulled.
3. Press Ctrl+L (Pull; Actions menu).
    You hear: Already up to date.
4. Press F5 (Refresh; Session menu).
    You hear: Refreshed.
5. Press Ctrl+P (Push; Actions menu).
    You hear: Pushed.
6. Press Ctrl+P (Push; Actions menu).
    You hear: Pushed.

## Use axGIT's text, notices and axKit menu

Starts with: Open axGIT with the folder repo.

### Find a word in a diff

1. Press Enter.
    You hear: Diff of a.txt.
2. Press Ctrl+F.
3. Type "marker".
4. Press Enter.
    The focus moves to Diff of a.txt (Text): +first paragraph marker.
5. Press F3.
    It reads +second paragraph marker.
6. Press F3.
    It reads +first paragraph marker.

### Move down the details by page

The focus moves to Diff of a.txt (Text): +first paragraph marker.
1. Press Ctrl+Home.
    It reads diff --git.
2. Press Page Down.

### Move by paragraph in a commit's details

1. Press Ctrl+Tab (Switch between the list and the text).
2. Press Alt+2 (Log; Navigation menu).
    You hear: Log, 1 commit.
3. Press Enter.
    You hear: lines.
4. Press Ctrl+Down (Next section).
    It reads first.
5. Press Ctrl+Down (Next section).
    It reads a.txt.
6. Press Ctrl+Tab (Switch between the list and the text).
7. Press Alt+1 (Changes; Navigation menu).
    You hear: Changes, 1 file.

### Change the text size

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size.
2. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size.
3. Choose Windows text size in the Options menu, Text.
    You hear: Windows text size.

### Read the keyboard shortcuts and the user guide

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press Escape.
3. Choose User guide in the Help menu.
4. Press Escape.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Open the last repository again

1. Close the window.
2. Open axGIT.

## Agree to the reminder before the first repository opens

Starts with: Open axGIT with the folder repo.

### Agree to the reminder and open the repository

You hear: Before axGIT connects to Git.
1. Press Tab.
    You hear: I understand that axGIT.
2. Press Enter.
    You hear: Check the box first.
3. Press Space.
    You hear: checked.
4. Press Space.
    You hear: not checked.
5. Press Space.
    You hear: checked.
6. Press Enter.

### Read what was agreed to on this computer

1. Choose Licence in the Help menu, About.
2. Press Escape.
3. Close the window.

## Open a repository that runs programs of its own

### Open a repository with its own programs kept off

1. Open axGIT with the folder repo.
2. Press Enter.
3. Close the window.

### Trust a repository

1. Open axGIT with the folder repo.
2. Press Tab twice.
3. Press Space.
    You hear: Trusted.
4. Close the window.
