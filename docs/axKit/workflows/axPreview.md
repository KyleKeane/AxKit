# axPreview: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Preview a file that is almost text

## Preview a text file

### Open a text file in axPreview

1. Open axPreview with the file README.md.

### Read the text line by line

1. Press Down 2 times.

### Go to a line by its number

1. Press Ctrl+Home, Down.
2. Press L.
    You hear: Line 2 of 32.
3. Press Ctrl+G (Go to line; Navigation menu).
4. Type: 5
5. Press Enter.
    You hear: Line 5 of 32.

### Move by headings, links, lists, quotes and tables

1. Press Ctrl+Home, Ctrl+H.
    You hear: Install, heading level 2.
2. Press Ctrl+Shift+H (Heading; Navigation menu, Previous).
    You hear: Readme for the preview tests, heading level 1.
3. Press Ctrl+2 2 times.
    You hear: Links, heading level 2.
4. Press Ctrl+Home, Ctrl+K.
    You hear: axKit on GitHub, link.
5. Press Ctrl+K (Link; Navigation menu, Next).
    You hear: https://www.example.com, link.
6. Press Ctrl+L (List; Navigation menu, Next).
    You hear: List with 3 items, first item of the list.
7. Press Ctrl+I (List item; Navigation menu, Next).
    You hear: second item of the list.
8. Press Ctrl+Q (Block quote; Navigation menu, Next).
    You hear: Block quote, A block quote, for Ctrl+Q.
9. Press Ctrl+Q (Block quote; Navigation menu, Next).
    You hear: No next block quote.
10. Press Ctrl+T (Table; Navigation menu, Next).
    You hear: Table with 3 rows and 2 columns.
11. Press Ctrl+T (Table; Navigation menu, Next).
    You hear: No next table.

### Find a word in the text

1. Press Ctrl+Home, Ctrl+F.
2. Type: update
3. Press Enter.
4. Press F3 2 times (Find next; Navigation menu).
5. Press Shift+F3 (Find previous; Navigation menu).

### Move by page and by paragraph

1. Press Ctrl+Home, Page Down.
2. Press Ctrl+End, Page Up.
3. Press Ctrl+Home, Ctrl+Down.
4. Press Ctrl+Down.
5. Press Ctrl+Up.

### Turn word wrap off and on

1. Press Ctrl+G (Go to line; Navigation menu).
2. Type: 5
3. Press Enter.
    You hear: Line 5 of 32.
4. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap off.
5. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap on.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
3. Press Escape.

### Reload the file after a change

1. Press F5 (Reload; Session menu).
    You hear: Reloaded.

## Preview a file from another app

### Show a file in the open window

1. Open axPreview with the file README.md.
2. Start axPreview again with the file plain.txt.

### Open a second window

1. Open axPreview with --new the file README.md.
2. Go to the other axPreview window.

## Use axPreview's text, notices and axKit menu

Starts with: Open axPreview with the file README.md.

### Change the text size

1. Press Down 2 times.
2. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size 10.
3. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size 9.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
3. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.
4. Press Escape.

### Close the window

1. Close the window.

## Read a file's details

### Read a file's details

1. Press Tab.
2. Press Down 3 times.

### Find a line of the details by typing its first letters

1. Type: wor

### Move between the text and the details

1. Press F6 2 times (Between the text and the details).

### Read a Word document

1. Open axPreview with --new the file report.docx.
2. Press Ctrl+End.
3. Press Ctrl+Shift+H (Heading; Navigation menu, Previous).
    You hear: Install, heading level 1.

### Read the contents of a zip

1. Open axPreview with --new the file bundle.zip.
2. Press Tab.
3. Type: file

### Read the text in a picture

1. Open axPreview with --new the file text.png.
    You hear: Text read: 2 lines.
2. Press Ctrl+Down.
3. Press Down.
4. Press Tab.
5. Type: text l

### Read the text of a picture on the clipboard

1. Press Ctrl+Shift+V (Read the clipboard's picture; Session menu).
    You hear: Text read: 2 lines.
2. Press Shift+Tab.

### Preview a sound file

1. Open axPreview with --new the file song.mp3.

### Read the text inside any other file

1. Open axPreview with --new the file tool.bin.
2. Press Down.

## Use axPreview's Context menu

Starts with: Open axPreview with the file README.md.

### Select all the text

1. Choose Select all in the Context menu (key: Ctrl+A).
2. Press Right.

### Select all the text from the command palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: select all, context
    You hear: Select all, Context, Ctrl+A.
3. Press Enter.
4. Press Left.

## Open a recent file in axPreview from the command palette

Starts with: Open axPreview with the file orchard.md. Start axPreview again with the file cellar.txt.

### Open a recent file from the palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: orch
    You hear words such as: 2. orchard.md.
    You hear: Session, Open recent.
3. Press Enter.
