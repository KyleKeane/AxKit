# axGIT: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Open a repository in axGIT

Starts with: Open axGIT with the folder repo.

1. Press Down.
    It reads Untracked new.txt.
2. Press Tab.
    The focus moves to Details (Text).
3. Press Shift+Tab.
    The focus moves to Changes, 2 files (List): Untracked new.txt.
4. Press F6 (Switch between the list and the details).
    The focus moves to Details (Text).
5. Press Ctrl+Tab (Switch between the list and the details).
    The focus moves to Changes, 2 files (List): Untracked new.txt.
6. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, 2 files. Untracked new.txt.
7. Press Ctrl+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
    You hear: Test: second.
8. Press Ctrl+3 (Branches; Navigation menu).
    You hear: Branches, 2. main, current, no upstream.
9. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, 2 files. Untracked new.txt.
10. Press Ctrl+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
11. Press Space.
    You hear: Changes view first: Ctrl+1.
12. Press Delete.
    You hear: Changes view first: Ctrl+1.
13. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, 2 files.
14. Close the window.
15. Open axGIT with the folder plain.
16. Press Enter.

## Open axGIT from another app

Starts with: Open axGIT with the folder repo.

1. Press Ctrl+E (Open file in editor; Context menu).
    You hear: No file selected.
2. Open axDown with the file repo\notes.md.
3. Close the window.
4. Open axClaude with the folder repo.

## Review and commit changes

Starts with: Open axGIT with the folder repo.

1. Press Space.
    You hear: Staged a.txt.
2. Press Down.
    It reads Untracked new.txt.
3. Press Up.
    It reads Modified a.txt, staged.
4. Press Space.
    You hear: Unstaged a.txt.
5. Press Enter.
    You hear: Diff of a.txt, 10 lines.
6. Press Down.
    It reads index.
7. Press Down 4 times.
    It reads hello.
8. Press Down.
    It reads -world.
9. Press Down.
    It reads  No newline at end of file.
10. Press Down.
    It reads +changed world.
11. Press Ctrl+Tab (Switch between the list and the details).
    The focus moves to Changes, 2 files (List): Modified a.txt, unstaged.
12. Press Down.
    It reads Untracked new.txt.
13. Press Enter.
    You hear: Diff of new.txt, 4 lines.
14. Press Down.
    It reads one.
15. Press Ctrl+Tab (Switch between the list and the details).
    The focus moves to Changes, 2 files (List): Untracked new.txt.
16. Press Ctrl+Shift+A (Stage all; Actions menu).
    You hear: Staged all.
17. Press Ctrl+Shift+U (Unstage all; Actions menu).
    You hear: Unstaged all.
18. Press Ctrl+Enter (Commit; Actions menu).
    You hear: Nothing staged. Space stages a file.
19. Press Up.
    It reads Modified a.txt, unstaged.
20. Press Space.
    You hear: Staged a.txt.
21. Press Ctrl+Enter (Commit; Actions menu).
22. Press Enter.
    You hear: Type the commit message.
23. Type: Test commit
24. Press Enter.
    You hear: Committed.
25. Press Ctrl+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
26. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, 1 file.
27. Press F5 (Refresh; Session menu).
    You hear: Refreshed: Changes, 2 files.
28. Press Home.
29. Press Delete.
30. Press Escape.
31. Press Delete.
32. Press Enter.
    You hear: Discarded a.txt.
33. Press Delete.
    You hear: Untracked file: delete it in File Explorer.
34. Press Ctrl+S.
    You hear: Nothing to save here. Space stages, Ctrl+Enter commits.
35. Press F5 (Refresh; Session menu).
    You hear: Refreshed: Changes, 2 files.
36. Press Home.
37. Press Space, Ctrl+Enter.

## Read the history and switch branches

Starts with: Open axGIT with the folder repo.

1. Press Ctrl+2 (Log; Navigation menu).
    You hear: Log, 2 commits.
2. Press Enter.
    You hear words such as: 8 lines.
3. Press Down.
    It reads Author: Test.
4. Press Down.
    It reads Date.
5. Press Down 2 times.
    You hear: second commit.
6. Press Down 2 times.
    You hear: a.txt.
7. Press Ctrl+Tab (Switch between the list and the details).
    The focus moves to Log, 2 commits (List).
8. Press Ctrl+3 (Branches; Navigation menu).
    You hear: Branches, 2.
9. Press Enter.
    You hear: Already on main.
10. Press Down.
    It reads other.
11. Press Enter.
    You hear: Switched to other.
12. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, no changes.
13. Press Ctrl+3 (Branches; Navigation menu).
    You hear: Branches, 2.
14. Press Up.
    It reads main.
15. Press Enter.
    You hear: Switched to main.
16. Press Ctrl+N (New branch; Actions menu).
17. Type: scratch-test
18. Press Enter.
    You hear: Switched to scratch-test.
19. Press Ctrl+3 (Branches; Navigation menu).
    You hear: Branches, 3.
20. Press Home.
21. Press Enter.
    You hear: Switched to main.
22. Press Ctrl+Shift+F (Fetch; Actions menu).
    You hear: Fetched.
23. Press Ctrl+Shift+L (Pull; Actions menu).
    You hear: Pulled.
24. Press Ctrl+Shift+L (Pull; Actions menu).
    You hear: Already up to date.
25. Press F5 (Refresh; Session menu).
    You hear: Refreshed.
26. Press Ctrl+Shift+H (Push; Actions menu).
    You hear: Pushed.
27. Press Ctrl+Shift+H (Push; Actions menu).
    You hear: Pushed.
28. Press Ctrl+Shift+F, Ctrl+Shift+L.
    You hear: Working…
    You hear: Fetched.
29. Press Ctrl+Shift+F (Fetch; Actions menu).
30. Press Escape.

## Use axGIT's text, notices and axKit menu

Starts with: Open axGIT with the folder repo.

1. Press Enter.
    You hear: Diff of a.txt.
2. Press Ctrl+F.
3. Type: marker
4. Press Enter.
    The focus moves to Diff of a.txt (Text): +first paragraph marker.
5. Press F3.
    It reads +second paragraph marker.
6. Press F3.
    It reads +first paragraph marker.
7. Press Ctrl+F.
8. Press Ctrl+A.
9. Type: absent
10. Press Enter.
    You hear: Not found.
    The focus moves to Diff of a.txt (Text): +first paragraph marker.
11. Press Ctrl+Home.
    It reads diff --git.
12. Press Page Down.
13. Press Ctrl+Tab (Switch between the list and the details).
14. Press Ctrl+2 (Log; Navigation menu).
    You hear: Log, 1 commit.
15. Press Enter.
    You hear: lines.
16. Press Ctrl+Down.
    It reads first.
17. Press Ctrl+Down.
    It reads a.txt.
18. Press Ctrl+Tab (Switch between the list and the details).
19. Press Ctrl+1 (Changes; Navigation menu).
    You hear: Changes, 1 file.
20. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size.
21. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size.
22. Choose Windows text size in the Options menu, Text.
    You hear: Windows text size.
23. Press F1 (Keyboard shortcuts; Help menu).
24. Press Escape.
25. Choose User guide in the Help menu.
26. Press Escape.
27. Choose Version in the Help menu, About.
28. Press Escape.
29. Close the window.
30. Open axGIT.

## Agree to the reminder before the first repository opens

Starts with: Open axGIT with the folder repo.

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
7. Choose Licence in the Help menu, About.
8. Press Escape.
9. Close the window.
10. Open axGIT with the folder repo.

## Open a repository that runs programs of its own

Starts with: Open axGIT with the folder repo.

1. Press Enter.
2. Close the window.
3. Open axGIT with the folder repo.
4. Press Tab 2 times.
5. Press Space.
    You hear: Trusted.
6. Close the window.
7. Open axGIT with the folder repo.
