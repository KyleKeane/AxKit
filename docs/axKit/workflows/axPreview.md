# axPreview: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Preview a file that is almost text

Starts with: Open axPreview with nul-later.txt.

1. Press Escape.

## Preview a text file

Starts with: Open axPreview with README.md.

1. Press Down.
2. Press Down.
3. Type xyz.
4. Press Ctrl+V.
5. Press Shift+Down.
6. Press Ctrl+Home, Down.
7. Press L.
   You hear: Line 2 of 32.
8. Press Ctrl+G (Go to line).
9. Type 5.
10. Press Enter.
   You hear: Line 5 of 32.
11. Press Ctrl+G (Go to line).
12. Type x.
13. Press Enter.
   You hear: Not a line number.
14. Press Ctrl+G (Go to line).
15. Press L.
16. Press Escape.
17. Press Ctrl+Home, Ctrl+H.
   You hear: Install, heading level 2.
18. Press Ctrl+Shift+H (Heading, Navigation, Previous).
   You hear: Readme for the preview tests, heading level 1.
19. Press Ctrl+2, Ctrl+2.
   You hear: Links, heading level 2.
20. Press Ctrl+Home, Ctrl+K.
   You hear: axKit on GitHub, link.
21. Press Ctrl+K (Link, Navigation, Next).
   You hear: https://www.example.com, link.
22. Press Ctrl+L (List, Navigation, Next).
   You hear: List with 3 items, first item of the list.
23. Press Ctrl+I (List item, Navigation, Next).
   You hear: second item of the list.
24. Press Ctrl+Q (Block quote, Navigation, Next).
   You hear: Block quote, A block quote, for Ctrl+Q.
25. Press Ctrl+Q (Block quote, Navigation, Next).
   You hear: No next block quote.
26. Press Ctrl+T (Table, Navigation, Next).
   You hear: Table with 3 rows and 2 columns.
27. Press Ctrl+T (Table, Navigation, Next).
   You hear: No next table.
28. Press Ctrl+Home, Ctrl+F.
29. Type update.
30. Press Enter.
31. Press F3 (Find next).
32. Press F3 (Find next).
33. Press Shift+F3 (Find previous).
34. Press Ctrl+Home, Pagedown.
35. Press Ctrl+End, Pageup.
36. Press Ctrl+Home, Ctrl+Down.
37. Press Ctrl+Down.
38. Press Ctrl+Up.
39. Press Ctrl+G (Go to line).
40. Type 5.
41. Press Enter.
   You hear: Line 5 of 32.
42. Press Ctrl+Shift+W (Word wrap).
   You hear: Word wrap off.
43. Press Ctrl+Shift+W (Word wrap).
   You hear: Word wrap on.
44. Press Ctrl+S.
   You hear: Nothing to save here.
45. Press F1 (Keyboard shortcuts).
46. Press Ctrl+H (Heading, Navigation, Next).
   You hear: heading level 2.
47. Press Escape.
48. Press F5 (Reload).
49. Open axPreview with --new plain.txt.
50. Press Ctrl+H (Heading, Navigation, Next).
   You hear: No next heading.
51. Open axPreview with --new tool.exe.
52. Open axPreview with --new too-large.txt.
53. Press Enter.
54. Open axPreview with --new app.log.
55. Open axPreview with --new gone.txt.
56. Press Enter.
57. Press Ctrl+E (Edit in axDown).
   You hear: No file yet.
58. Press Ctrl+W (File Explorer).
   You hear: No file yet.
59. Press F5 (Reload).
   You hear: No file yet.
60. Open axPreview with --new data.json.

## Preview a file from another app

1. Open axPreview with README.md.
2. Launch preview plain.txt.
3. Open axPreview with --new README.md.
4. Use window 1.

## Use axPreview's text, notices and axKit menu

Starts with: Open axPreview with README.md.

1. Press Down, Down.
2. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
   You hear: Text size 10.
3. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
   You hear: Text size 9.
4. Choose User guide in the Help menu.
5. Press Ctrl+H (Heading, Navigation, Next).
   You hear: heading level 2.
6. Press Ctrl+H (Heading, Navigation, Next).
   You hear: heading level 2.
7. Press Escape.
8. Close the window.

## Read a file's details

Starts with: Open axPreview with notes.md.

1. Press Tab.
2. Press Down.
3. Press Down, Down.
4. Press Enter.
   You hear: Enter opens the Folder line in axFile.
5. Type wor.
6. Press F6 (Between the text and the details).
7. Press F6 (Between the text and the details).
8. Open axPreview with --new report.docx.
9. Press Ctrl+End.
10. Press Ctrl+Shift+H (Heading, Navigation, Previous).
   You hear: Install, heading level 1.
11. Open axPreview with --new bundle.zip.
12. Press Tab.
13. Type file.
14. Open axPreview with --new big.zip.
15. Press Ctrl+End.
16. Press Tab.
17. Type shown.
18. Open axPreview with --new text.png.
   You hear: Text read: 2 lines.
19. Press Ctrl+Down.
20. Press Down.
21. Press Tab.
22. Type text l.
23. Press Ctrl+Shift+V (Read the clipboard's picture).
   You hear: Text read: 2 lines.
24. Press Shift+Tab.
25. Open axPreview with --new song.mp3.
26. Open axPreview with --new tool.bin.
27. Press Down.

## Use axPreview's Context menu

Starts with: Open axPreview with README.md.

1. Choose Select all in the Context menu (key: Ctrl+A).
2. Press Right.
3. Press Ctrl+Shift+P (Command palette).
4. Type select all, context.
   You hear: Select all, Context, Ctrl+A.
5. Press Enter.
6. Press Left.
