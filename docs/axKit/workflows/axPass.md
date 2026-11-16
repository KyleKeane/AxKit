# axPass: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Unlock a password database and change an entry

Starts with: Open axPass with the file test.kdbx.

1. Type: wrong
2. Press Enter.
    You hear: Unlocking test.
3. Type: test-master-1
4. Press Enter.
    You hear: test, 3 items, Email, group, 1 item.
5. Press F5 (Refresh; Session menu).
    You hear: Refreshed: test, 3 items.
6. Press Right.
    You hear: Email, 1 item, Gmail.
7. Press Right.
    You hear the sound Edge.
8. Press Left.
    You hear: test, 3 items, Email, group, 1 item.
9. Press Right.
10. Press Enter.
11. Press Down 3 times.
12. Press Escape.
13. Press Ctrl+Tab (Switch between the navigator and the detail pane).
14. Press Ctrl+Shift+Tab (Switch between the navigator and the detail pane).
15. Press F6 2 times (Switch between the navigator and the detail pane).
16. Press Tab.
17. Press Home.
18. Press Down.
19. Press Enter.
20. Type: sam2
21. Press Enter.
    You hear: UserName: sam2.
22. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: UserName: sam2.
23. Press Ctrl+N.
    You hear: An entry has KeePass's own fields here.
24. Press Escape.
25. Press F2.
26. Type: a/b
27. Press Enter.
    You hear: A name cannot hold a slash.
28. Press Escape.
29. Press F2.
30. Type: Gmail personal
31. Press Enter.
    You hear: Renamed Gmail personal.
32. Press Ctrl+N.
33. Type: Bank
34. Press Enter.
    You hear: Added Bank.
35. Press Delete.
    You hear: Moved Bank to the Recycle Bin.
36. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Moved Bank to the Recycle Bin.
37. Press Up.
38. Press Tab.
39. Press Home.
40. Press Down 2 times.
41. Press Enter.
42. Press Alt+G.
    You hear: Generated a password of 20 characters.
43. Press Enter.
    You hear: Password set for Gmail personal.
44. Press Enter.
45. Type: one
46. Press Enter.
47. Type: two
48. Press Enter.
    You hear: The two passwords differ: nothing changed.
49. Press Escape.
50. Type: b
51. Press Left.
52. Type: w
53. Press Delete.
54. Press Enter.
55. Press Home.
56. Press Right.
57. Type: g
58. Press Ctrl+M.
59. Press Down.
60. Press Enter.
    You hear: Moved Gmail personal to Work.
61. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Moved Gmail personal to Work.
62. Press Left.
63. Press Ctrl+L (Lock; Actions menu).
    You hear: Locked.
64. Press Escape.

## Agree to the reminder before the first unlock

Starts with: Open axPass with the file test.kdbx.

You hear: Before axPass connects to KeePassXC.
1. Press Tab.
    You hear: I understand that axPass.
2. Press Enter.
    You hear: Check the box first.
3. Press Space.
    You hear: checked.
4. Press Enter.
5. Type: test-master-1
6. Press Enter.
    You hear: test, 1 item, Email, group, 0 items.
7. Close the window.
8. Open axPass with the file test.kdbx.

## Read an entry in full and come back to the same database

Starts with: Open axPass with the file test.kdbx.

1. Type: test-master-1
2. Press Enter.
    You hear: test, 1 item, Email, group, 1 item.
3. Press Right.
    You hear: Email, 1 item, Gmail.
4. Press Enter.
5. Press Down 2 times.
6. Press Ctrl+Q (Read in full).
    You hear: Password, password, hidden.
7. Press Escape.
8. Press Ctrl+Q (Read in full).
    You hear words such as: Gmail. UserName: sam. Password: hidden. URL: https://mail.example.com. Notes.
9. Press Ctrl+L (Lock; Actions menu).
    You hear: Locked.
10. Press Escape.
11. Choose 5 minutes in the Options menu, Lock after.
    You hear: axPass locks after 5 minutes without a key.
12. Choose Never in the Options menu, Lock after.
    You hear: axPass locks only on Ctrl+L.
13. Close the window.
14. Open axPass.
15. Type: test-master-1
16. Press Enter.
    You hear: test, 1 item, Email, group, 1 item.
