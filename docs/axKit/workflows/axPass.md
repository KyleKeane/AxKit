# axPass: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Unlock a password database and change an entry

Starts with: Open axPass with test.kdbx.

1. Type wrong.
2. Press Enter.
   You hear: Unlocking test.
3. Type test-master-1.
4. Press Enter.
   You hear: test, 3 items, Email, group, 1 item.
5. Press F5 (Refresh).
   You hear: Refreshed: test, 3 items.
6. Press Right.
   You hear: Email, 1 item, Gmail.
7. Press Right.
   You hear the sound Edge.
8. Press Left.
   You hear: test, 3 items, Email, group, 1 item.
9. Press Right.
10. Press Enter.
11. Press Down.
12. Press Down.
13. Press Down.
14. Press Escape.
15. Press Ctrl+Tab (Switch between the navigator and the detail pane).
16. Press Ctrl+Shift+Tab (Switch between the navigator and the detail pane).
17. Press F6 (Switch between the navigator and the detail pane).
18. Press F6 (Switch between the navigator and the detail pane).
19. Press Tab.
20. Press Home.
21. Press Down.
22. Press Enter.
23. Type sam2.
24. Press Enter.
   You hear: UserName: sam2.
25. Press Ctrl+Z (Undo).
   You hear: Undone: UserName: sam2.
26. Press Ctrl+N.
   You hear: An entry has KeePass's own fields here.
27. Press Escape.
28. Press F2.
29. Type a/b.
30. Press Enter.
   You hear: A name cannot hold a slash.
31. Press Escape.
32. Press F2.
33. Type Gmail personal.
34. Press Enter.
   You hear: Renamed Gmail personal.
35. Press Ctrl+N.
36. Type Bank.
37. Press Enter.
   You hear: Added Bank.
38. Press Delete.
   You hear: Moved Bank to the Recycle Bin.
39. Press Ctrl+Z (Undo).
   You hear: Undone: Moved Bank to the Recycle Bin.
40. Press Up.
41. Press Tab.
42. Press Home.
43. Press Down.
44. Press Down.
45. Press Enter.
46. Press Alt+G.
   You hear: Generated a password of 20 characters.
47. Press Enter.
   You hear: Password set for Gmail personal.
48. Press Enter.
49. Type one.
50. Press Enter.
51. Type two.
52. Press Enter.
   You hear: The two passwords differ: nothing changed.
53. Press Escape.
54. Type b.
55. Press Left.
56. Type w.
57. Press Delete.
58. Press Enter.
59. Press Home.
60. Press Right.
61. Type g.
62. Press Ctrl+M.
63. Press Down.
64. Press Enter.
   You hear: Moved Gmail personal to Work.
65. Press Ctrl+Z (Undo).
   You hear: Undone: Moved Gmail personal to Work.
66. Press Left.
67. Press Ctrl+L (Lock).
68. Press Escape.

## Agree to the reminder before the first unlock

Starts with: Open axPass with test.kdbx.

   You hear: Before axPass connects to KeePassXC.
1. Press Tab.
   You hear: I understand that axPass.
2. Press Enter.
   You hear: Check the box first.
3. Press Space.
4. Press Enter.
5. Type test-master-1.
6. Press Enter.
   You hear: test, 1 item, Email, group, 0 items.
7. Close the window.
8. Open axPass with test.kdbx.

## Read an entry in full and come back to the same database

Starts with: Open axPass with test.kdbx.

1. Type test-master-1.
2. Press Enter.
   You hear: test, 1 item, Email, group, 1 item.
3. Press Right.
   You hear: Email, 1 item, Gmail.
4. Press Enter.
5. Press Down, Down.
6. Press Ctrl+Q (Read in full).
   You hear: Password, password, hidden.
7. Press Escape.
8. Press Ctrl+Q (Read in full).
   You hear: Gmail. UserName: sam. Password: hidden. URL: https://mail.example.com. Notes.
9. Press Ctrl+L (Lock).
10. Press Escape.
11. Choose 5 minutes in the Options menu, Lock after.
   You hear: axPass locks after 5 minutes without a key.
12. Choose Never in the Options menu, Lock after.
   You hear: axPass locks only on Ctrl+L.
13. Close the window.
14. Open axPass.
15. Type test-master-1.
16. Press Enter.
   You hear: test, 1 item, Email, group, 1 item.
