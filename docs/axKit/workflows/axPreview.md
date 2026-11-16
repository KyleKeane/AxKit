# axPreview: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Preview a file that is almost text

Starts with: Open axPreview with the file nul-later.txt.

1. Press Escape.

## Preview a text file

Starts with: Open axPreview with the file README.md.

1. Press Down 2 times.
2. Type: xyz
3. Press Ctrl+V.
4. Press Shift+Down.
5. Press Ctrl+Home, Down.
6. Press L.
    You hear: Line 2 of 32.
7. Press Ctrl+G (Go to line; Navigation menu).
8. Type: 5
9. Press Enter.
    You hear: Line 5 of 32.
10. Press Ctrl+G (Go to line; Navigation menu).
11. Type: x
12. Press Enter.
    You hear: Not a line number.
13. Press Ctrl+G (Go to line; Navigation menu).
14. Press L.
15. Press Escape.
16. Press Ctrl+Home, Ctrl+H.
    You hear: Install, heading level 2.
17. Press Ctrl+Shift+H (Heading; Navigation menu, Previous).
    You hear: Readme for the preview tests, heading level 1.
18. Press Ctrl+2 2 times.
    You hear: Links, heading level 2.
19. Press Ctrl+Home, Ctrl+K.
    You hear: axKit on GitHub, link.
20. Press Ctrl+K (Link; Navigation menu, Next).
    You hear: https://www.example.com, link.
21. Press Ctrl+L (List; Navigation menu, Next).
    You hear: List with 3 items, first item of the list.
22. Press Ctrl+I (List item; Navigation menu, Next).
    You hear: second item of the list.
23. Press Ctrl+Q (Block quote; Navigation menu, Next).
    You hear: Block quote, A block quote, for Ctrl+Q.
24. Press Ctrl+Q (Block quote; Navigation menu, Next).
    You hear: No next block quote.
25. Press Ctrl+T (Table; Navigation menu, Next).
    You hear: Table with 3 rows and 2 columns.
26. Press Ctrl+T (Table; Navigation menu, Next).
    You hear: No next table.
27. Press Ctrl+Home, Ctrl+F.
28. Type: update
29. Press Enter.
30. Press F3 2 times (Find next; Navigation menu).
31. Press Shift+F3 (Find previous; Navigation menu).
32. Press Ctrl+Home, Page Down.
33. Press Ctrl+End, Page Up.
34. Press Ctrl+Home, Ctrl+Down.
35. Press Ctrl+Down.
36. Press Ctrl+Up.
37. Press Ctrl+G (Go to line; Navigation menu).
38. Type: 5
39. Press Enter.
    You hear: Line 5 of 32.
40. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap off.
41. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap on.
42. Press Ctrl+S.
    You hear: Nothing to save here.
43. Press F1 (Keyboard shortcuts; Help menu).
44. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
45. Press Escape.
46. Press F5 (Reload; Session menu).
    You hear: Reloaded.
47. Open axPreview with --new the file plain.txt.
48. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: No next heading.
49. Open axPreview with --new the file tool.exe.
50. Open axPreview with --new the file too-large.txt.
51. Press Enter.
52. Open axPreview with --new the file app.log.
53. Open axPreview with --new the file gone.txt.
54. Press Enter.
55. Press Ctrl+E (Edit in axDown; Session menu).
    You hear: No file yet.
56. Press Ctrl+W (File Explorer; Session menu, Show current file in).
    You hear: No file yet.
57. Press F5 (Reload; Session menu).
    You hear: No file yet.
58. Open axPreview with --new the file data.json.

## Preview a file from another app

1. Open axPreview with the file README.md.
2. Start axPreview again with the file plain.txt.
3. Open axPreview with --new the file README.md.
4. Go to the other axPreview window.

## Use axPreview's text, notices and axKit menu

Starts with: Open axPreview with the file README.md.

1. Press Down 2 times.
2. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size 10.
3. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size 9.
4. Choose User guide in the Help menu.
5. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
6. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
7. Press Escape.
8. Close the window.

## Read a file's details

Starts with: Open axPreview with the file notes.md.

1. Press Tab.
2. Press Down 3 times.
3. Press Enter.
    You hear: Enter opens the Folder line in axFile.
4. Type: wor
5. Press F6 2 times (Between the text and the details).
6. Open axPreview with --new the file report.docx.
7. Press Ctrl+End.
8. Press Ctrl+Shift+H (Heading; Navigation menu, Previous).
    You hear: Install, heading level 1.
9. Open axPreview with --new the file bundle.zip.
10. Press Tab.
11. Type: file
12. Open axPreview with --new the file big.zip.
13. Press Ctrl+End.
14. Press Tab.
15. Type: shown
16. Open axPreview with --new the file text.png.
    You hear: Text read: 2 lines.
17. Press Ctrl+Down.
18. Press Down.
19. Press Tab.
20. Type: text l
21. Press Ctrl+Shift+V (Read the clipboard's picture; Session menu).
    You hear: Text read: 2 lines.
22. Press Shift+Tab.
23. Open axPreview with --new the file song.mp3.
24. Open axPreview with --new the file tool.bin.
25. Press Down.

## Use axPreview's Context menu

Starts with: Open axPreview with the file README.md.

1. Choose Select all in the Context menu (key: Ctrl+A).
2. Press Right.
3. Press Ctrl+Shift+P (Command palette; Help menu).
4. Type: select all, context
    You hear: Select all, Context, Ctrl+A.
5. Press Enter.
6. Press Left.
