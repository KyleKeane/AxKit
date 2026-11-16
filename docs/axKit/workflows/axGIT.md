# axGIT: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Open a repository in axGIT

Starts with: Open axGIT with repo.

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
6. Press Ctrl+1 (Changes).
   You hear: Changes, 2 files. Untracked new.txt.
7. Press Ctrl+2 (Log).
   You hear: Log, 2 commits.
   You hear: Test: second.
8. Press Ctrl+3 (Branches).
   You hear: Branches, 2. main, current, no upstream.
9. Press Ctrl+1 (Changes).
   You hear: Changes, 2 files. Untracked new.txt.
10. Press Ctrl+2 (Log).
   You hear: Log, 2 commits.
11. Press Space.
   You hear: Changes view first: Ctrl+1.
12. Press Delete.
   You hear: Changes view first: Ctrl+1.
13. Press Ctrl+1 (Changes).
   You hear: Changes, 2 files.
14. Close the window.
15. Open axGIT with plain.
16. Press Enter.

## Open axGIT from another app

Starts with: Open axGIT with repo.

1. Press Ctrl+E (Open file in editor).
   You hear: No file selected.
2. Open axDown with repo\notes.md.
3. Close the window.
4. Open axClaude with repo -- --model haiku.

## Review and commit changes

Starts with: Open axGIT with repo.

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
7. Press Down, Down, Down.
   It reads @@.
8. Press Down.
   It reads hello.
9. Press Down.
   It reads -world.
10. Press Down.
   It reads \ No newline at end of file.
11. Press Down.
   It reads +changed world.
12. Press Ctrl+Tab (Switch between the list and the details).
   The focus moves to Changes, 2 files (List): Modified a.txt, unstaged.
13. Press Down.
   It reads Untracked new.txt.
14. Press Enter.
   You hear: Diff of new.txt, 4 lines.
15. Press Down.
   It reads one.
16. Press Ctrl+Tab (Switch between the list and the details).
   The focus moves to Changes, 2 files (List): Untracked new.txt.
17. Press Ctrl+Shift+A (Stage all).
   You hear: Staged all.
18. Press Ctrl+Shift+U (Unstage all).
   You hear: Unstaged all.
19. Press Ctrl+Enter (Commit).
   You hear: Nothing staged. Space stages a file.
20. Press Up.
   It reads Modified a.txt, unstaged.
21. Press Space.
   You hear: Staged a.txt.
22. Press Ctrl+Enter (Commit).
23. Press Enter.
   You hear: Type the commit message.
24. Type Test commit.
25. Press Enter.
26. Press Ctrl+2 (Log).
   You hear: Log, 2 commits.
27. Press Ctrl+1 (Changes).
   You hear: Changes, 1 file.
28. Press F5 (Refresh).
   You hear: Refreshed: Changes, 2 files.
29. Press Home.
30. Press Delete.
31. Press Escape.
32. Press Delete.
33. Press Enter.
   You hear: Discarded a.txt.
34. Press Delete.
   You hear: Untracked file: delete it in File Explorer.
35. Press Ctrl+S.
   You hear: Nothing to save here. Space stages, Ctrl+Enter commits.
36. Press F5 (Refresh).
   You hear: Refreshed: Changes, 2 files.
37. Press Home.
38. Press Space, Ctrl+Enter.

## Read the history and switch branches

Starts with: Open axGIT with repo.

1. Press Ctrl+2 (Log).
   You hear: Log, 2 commits.
2. Press Enter.
   You hear: 8 lines.
3. Press Down.
   It reads Author: Test.
4. Press Down.
   It reads Date.
5. Press Down, Down.
   You hear: second commit.
6. Press Down, Down.
7. Press Ctrl+Tab (Switch between the list and the details).
   The focus moves to Log, 2 commits (List).
8. Press Ctrl+3 (Branches).
   You hear: Branches, 2.
9. Press Enter.
   You hear: Already on main.
10. Press Down.
   It reads other.
11. Press Enter.
   You hear: Switched to other.
12. Press Ctrl+1 (Changes).
   You hear: Changes, no changes.
13. Press Ctrl+3 (Branches).
   You hear: Branches, 2.
14. Press Up.
   It reads main.
15. Press Enter.
   You hear: Switched to main.
16. Press Ctrl+N (New branch).
17. Type scratch-test.
18. Press Enter.
   You hear: Switched to scratch-test.
19. Press Ctrl+3 (Branches).
   You hear: Branches, 3.
20. Press Home.
21. Press Enter.
   You hear: Switched to main.
22. Press Ctrl+Shift+F (Fetch).
23. Press Ctrl+Shift+L (Pull).
24. Press Ctrl+Shift+L (Pull).
   You hear: Already up to date.
25. Press F5 (Refresh).
26. Press Ctrl+Shift+H (Push).
27. Press Ctrl+Shift+H (Push).
28. Press Ctrl+Shift+F, Ctrl+Shift+L.
29. Press Ctrl+Shift+F (Fetch).
30. Press Escape.

## Use axGIT's text, notices and axKit menu

Starts with: Open axGIT with repo.

1. Press Enter.
   You hear: Diff of a.txt.
2. Press Ctrl+F.
3. Type marker.
4. Press Enter.
   The focus moves to Diff of a.txt (Text): +first paragraph marker.
5. Press F3.
   It reads +second paragraph marker.
6. Press F3.
   It reads +first paragraph marker.
7. Press Ctrl+F.
8. Press Ctrl+A.
9. Type absent.
10. Press Enter.
   You hear: Not found.
   The focus moves to Diff of a.txt (Text): +first paragraph marker.
11. Press Ctrl+Home.
   It reads diff --git.
12. Press Pagedown.
13. Press Ctrl+Tab (Switch between the list and the details).
14. Press Ctrl+2 (Log).
   You hear: Log, 1 commit.
15. Press Enter.
16. Press Ctrl+Down.
   It reads first.
17. Press Ctrl+Down.
   It reads a.txt.
18. Press Ctrl+Tab (Switch between the list and the details).
19. Press Ctrl+1 (Changes).
   You hear: Changes, 1 file.
20. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
   You hear: Text size.
21. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
   You hear: Text size.
22. Choose Windows text size in the Options menu, Text.
   You hear: Windows text size.
23. Press F1 (Keyboard shortcuts).
24. Press Escape.
25. Choose User guide in the Help menu.
26. Press Escape.
27. Choose Version in the Help menu, About.
28. Press Escape.
29. Close the window.
30. Open axGIT.

## Agree to the reminder before the first repository opens

Starts with: Open axGIT with repo.

   You hear: Before axGIT connects to Git.
1. Press Tab.
   You hear: I understand that axGIT.
2. Press Enter.
   You hear: Check the box first.
3. Press Space.
4. Press Space.
   You hear: not checked.
5. Press Space.
6. Press Enter.
7. Choose Licence in the Help menu, About.
8. Press Escape.
9. Close the window.
10. Open axGIT with repo.

## Open a repository that runs programs of its own

Starts with: Open axGIT with repo.

1. Press Enter.
2. Close the window.
3. Open axGIT with repo.
4. Press Tab.
5. Press Tab.
6. Press Space.
7. Close the window.
8. Open axGIT with repo.
