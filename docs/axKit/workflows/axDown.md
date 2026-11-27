# axDown: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Open a text file in axDown

### Open axDown with an empty document

1. Open axDown.

### Open a file in axDown

1. Open axDown with the file plain.txt.

### Open a text file without naming an app

1. Open the file plain.txt.

### Start a new file by its name

1. Open axDown with the file missing.md.

## Read and write text in axDown

Starts with: Open axDown with the file long.md.

### Read the text line by line and word by word

1. Press Down twice.
2. Press Ctrl+Right twice.

### Move by paragraph

1. Press Ctrl+Home.
2. Press Ctrl+Down four times.

### Hear the line and column

1. Press F9 (Say the status; Window menu).
    You hear: Line 9, Column 1.

### Select text a paragraph at a time

1. Press Ctrl+Home, Ctrl+Shift+Down.
2. Press Ctrl+Shift+Up.

### Turn word wrap off and on

1. Press Ctrl+Home, Ctrl+Down, Ctrl+Down, Right, Right, Right.
2. Press F11 (Word wrap; Options menu, Text).
    You hear: Word wrap, not checked.
3. Press F11 (Word wrap; Options menu, Text).
    You hear: Word wrap, checked.

### Move by page

1. Open axDown with the file short.md.
2. Press Page Down.
3. Press Page Up.

### Type text, then undo and redo it

1. Open axDown with the file new.md.
2. Type "A sentence.".
3. Press Backspace.
4. Type "!".
5. Press Enter, Tab.
6. Type "Second line.".
7. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone.
8. Press Ctrl+Y (Redo; Actions menu).
    You hear: Redone.

### Undo and redo from the menu

1. Choose Redo in the Actions menu (key: Ctrl+Y).
    You hear: Redone.
2. Choose Undo in the Actions menu (key: Ctrl+Z).
    You hear: Undone.

## Hear why a file that is not text will not open

Starts with: Open axDown with the file image.png.

1. Press Escape.

## Open, save and switch files in axDown

### Save a change to a file

1. Open axDown with the file bom.md.
2. Press End.
3. Type "!".
4. Press Ctrl+S (Save; Session menu).
    You hear: Saved.

### Open a new empty window

1. Choose New window in the Session menu.
2. Go to the window that opened.
3. Close the window.
4. Go to the axDown window.

### Start a new document when the text has changes

1. Type "Added.".
2. Press Ctrl+N (New; Session menu).
3. Press Escape.
4. Press Ctrl+N (New; Session menu).
5. Press Enter.
    You hear: Saved.

### Reload a file that another program changed

1. Press Ctrl+S (Save; Session menu).
2. Press Tab twice.
3. Press Enter.
    You hear: Reloaded.

### Save over the changes of another program

1. Type "Y".
2. Press Ctrl+S (Save; Session menu).
3. Press Enter.
    You hear: Saved.

### Create a file by saving it

1. Open axDown with the file created.md.
2. Type "Created by Ctrl+S.".
3. Press Ctrl+S (Save; Session menu).
    You hear: Saved.

### Close a window without saving its changes

1. Go to the window that opened.
2. Type "More.".
3. Close the window.
4. Press Escape.
5. Close the window.
6. Press Tab twice.
7. Press Enter.

## Find text and go to a line

Starts with: Open axDown with the file find.md.

### Find a word in the text

1. Press Ctrl+F (Find; Navigation menu).
2. Type "apple".
3. Press Enter.

### Go to the next and the previous match

1. Press F3 (Find next; Navigation menu).
2. Press Shift+F3 (Find previous; Navigation menu).

### Go to a line by its number

1. Press F12 (Go to line; Navigation menu).
2. Type "5".
3. Press Enter.
    You hear: Line 5 of 7.

## Change the text size and the font

Starts with: Open axDown with the file size.md.

### Change the text size

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size 10.

## Find a command in the menus, the palette and the help

Starts with: Open axDown with the file a.md.

### Open the keyboard shortcuts

1. Press Down, Right, Right.
2. Choose Keyboard shortcuts in the Help menu.

### Move through the keyboard shortcuts by heading

1. Press H.
    You hear: In every window of axKit, heading level 2.

### Find a word in the keyboard shortcuts

1. Press Ctrl+F (Find; Navigation menu).
2. Type "wrap".
3. Press Enter.
4. Press F3 (Find next; Navigation menu).

### Close the keyboard shortcuts and go back to the text

1. Press Escape.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: heading level 2.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Read the licence, the attribution and the axKit guide

1. Choose Licence in the Help menu, About.
2. Press Escape.

### Read the attribution

1. Choose Attribution in the Help menu, About.
2. Press Escape.

### Read the axKit guide

1. Choose axKit guide in the Help menu, About.
2. Press Escape.

### Turn the update check at start on or off

1. Press F1 (Command palette; Help menu).
2. Type "check for updates at start".
    You hear: Check for updates at start, Options, 1 of 1.
3. Press Enter.
    You hear: Check for updates at start, checked.

### Move through the commands of the palette

1. Press F1 (Command palette; Help menu).
2. Press Down.
    You hear something with "A key alone moves and reads; with Shift, the other way, or select as you move., 2" in it.
3. Press Up.
    You hear something with "How the keys go, 1" in it.

### Run a command found by its name

1. Press Ctrl+A.
2. Type "wrap".
    You hear: Word wrap, Options, Text, F11, checked, 1 of 1.
3. Press Down.
    You hear the sound Edge.
    You hear: Word wrap, Options, Text, F11, checked, 1 of 1.
4. Press Enter.
    You hear: Word wrap, not checked.

## Check for updates and remove axKit

Starts with: Open axDown with the file a.md.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Check for updates

1. Choose the menu item Check for updates.
2. Press Escape.

## Move through a Markdown file by headings, lists and links

Starts with: Open axDown with the file long.md.

### Move to the next heading

1. Press Ctrl+H (Heading; Navigation menu, Next).
    You hear: Second heading, level 2, heading level 2.

### Move back to the previous heading

1. Press Ctrl+Shift+H (Heading; Navigation menu, Previous).

### Move to a heading of one level

1. Press Ctrl+Home.
2. Press Ctrl+2 (Heading level 2; Navigation menu, Next).
    You hear: Second heading, level 2, heading level 2.

### Move by heading from the menu

1. Press Ctrl+Home.
2. Choose Heading level 3 in the Navigation menu, Next (key: Ctrl+3).
    You hear: Third heading, level 3, heading level 3.

### Move to the next link

1. Press Ctrl+Home.
2. Press Ctrl+K (Link; Navigation menu, Next).
    You hear: GitHub, link.

### Move back to the previous link

1. Press Ctrl+Shift+K (Link; Navigation menu, Previous).
    You hear: https://www.example.com, link.
2. Press Right three times.
3. Press Ctrl+Shift+K (Link; Navigation menu, Previous).
    You hear: GitHub, link.

### Move by list and by list item

1. Press Ctrl+Home.
2. Press Ctrl+L (List; Navigation menu, Next).
    You hear: List with 3 items, one.
3. Press Ctrl+I (List item; Navigation menu, Next).
    You hear: two.

### Move to a block quote or a table

1. Press Ctrl+Home.
2. Press Ctrl+Q (Block quote; Navigation menu, Next).
    You hear: Block quote, A block quote, line one.

### Move to a table

1. Press Ctrl+Home.
2. Press Ctrl+T (Table; Navigation menu, Next).
    You hear: Table with 3 rows and 2 columns.

## Show the current file in another app

Starts with: Open axDown with the file sub\notes.md.

1. Choose File Explorer in the Session menu, Show current file in.
    You hear: File missing.

## Change the colours with the theme file

Starts with: Open axDown with the file notes.md.

### Make a theme file and open it

1. Choose Edit theme file in the Options menu, Theme.
2. Go to the window that opened.
3. Close the window.
4. Go back to the first window.

### Reload the theme

1. Choose Reload theme in the Options menu, Theme.
    You hear: Theme reloaded.

### Turn the theme off and on

1. Choose Theme on in the Options menu, Theme.
    You hear: Theme on, not checked.

## Ask Claude about the window you are in

Starts with: Open axDown with the file notes.md.

### Turn Claude on by agreeing to the reminder

1. Press Shift+F1 (Ask about this window; axKit menu, Claude).
2. Press Tab.
    You hear: I understand.
3. Press Enter.
    You hear: Check the box first.
4. Press Space.
    You hear: checked.
5. Press Enter.
    You hear: Claude on.
6. Press Escape.

### Turn Claude off and on

1. Choose Use Claude in axKit in the axKit menu, Claude.
    You hear: Claude off.

### Ask Claude about this window

1. Press Shift+F1 (Ask about this window; axKit menu, Claude).
2. Type "how do I turn word wrap off".
3. Press Enter.

### Ask more about an answer

1. Press Tab.
2. Type "and back on".
3. Press Enter.
4. Press Escape.

### See the folders Claude may read

1. Choose Folders Claude may read in the axKit menu, Claude.
2. Press Escape.

## Hear a line break as you type it

Starts with: Open axDown with the file notes.md.

### Start a new line and hear it said

1. Press Ctrl+End.
2. Press Enter.
    You hear: new line.
3. Type "Second line.".

### Split a line in two

1. Press Home.
2. Press Right six times.
3. Press Enter.
    You hear: new line.

### Turn the spoken new line off and on

1. Choose Say new line in the Options menu, Text.
    You hear: Say new line, not checked.

## Hear the heading sound as you move

Starts with: Open axDown with the file headings.md.

### Hear the sound when the caret reaches a heading

1. Press Down.
    You hear the sound Heading.

### Turn the heading sound off and on

1. Choose Sound on headings in the Options menu, Text.
    You hear: Sound on headings, not checked.

## Open a recent file or window from the command palette

Starts with: Open axDown with the file one.md. Open axDown with the file two.md.

### Open a recent file from the palette

1. Press F1 (Command palette; Help menu).
2. Type "one.md".
    You hear: Open recent file.
3. Press Enter.

### Find another open window from the palette

1. Press F1 (Command palette; Help menu).
2. Type "open windows".
    You hear something with "one.md, Window, Open windows" in it.
3. Press Escape.

### Reopen a window that was closed

1. Go back to the first window.
2. Close the window.
3. Go to the window that opened.
4. Press F1 (Command palette; Help menu).
5. Type "recently closed".
    You hear: 1. one.md.
    You hear: Window, Recently closed.
6. Press Enter.
7. Go to the window that opened.

## Find out whether a key arrives and what it does

Starts with: Open axDown with the file a.md.

### Start the key test

1. Press Down, Down, Right.
2. Choose Test a key in the Help menu, Diagnostics.
    You hear: Key test. Press any key to hear its name and what it does here. Press Escape to end.

### Hear what a key does without doing it

1. Press F9 (Say the status; Window menu).
    You hear: F9: Say the status.

### End the key test

1. Press Escape.
    You hear: Key test ended.

## Move through a file with single letters without changing it, then type again

Starts with: Open axDown with the file modes.md.

### Turn on navigation mode

1. Press Ctrl+Space.
    You hear: Navigation mode.

### Move by single letters

1. Press H.
    You hear the sound NavKey.
    You hear: Links, heading level 2.

### Go back to edit mode and type

1. Press Ctrl+Space.
    You hear: Edit mode.
2. Press Home.
3. Type "X".

## Give a line, a word and several lines their Markdown format, and take it away again

Starts with: Open axDown with the file format.md.

### Make a line a heading and change its level

1. Press Shift+Alt+Right (Heading deeper; Actions menu, Format).
    You hear: Heading 1.
2. Press Shift+Alt+Right (Heading deeper; Actions menu, Format).
    You hear: Heading 2.

### Set a heading's level from the menu

1. Choose Heading 4 in the Actions menu, Format.
    You hear: Heading 4.

### Make the word at the caret bold and take it off again

1. Press Ctrl+Down.
2. Press Ctrl+B (Bold; Actions menu, Format).
    You hear: Bold on.
3. Press Ctrl+B (Bold; Actions menu, Format).
    You hear: Bold off.

### Make the selected words italic

1. Press Home, Shift+End.
2. Choose Italic in the Actions menu, Format.
    You hear: Italic on.

### Make several lines list items and undo it in one step

1. Press Ctrl+Down.
2. Press Shift+Down, Shift+End.
3. Choose List item in the Actions menu, Format.
    You hear: List item on, 2 lines.
4. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone.

## Put a template in at the caret: a link around the selected words, a table, and a template of your own

Starts with: Open axDown with the file doc.md.

### Make the selected word a link and type its address

1. Press Shift+End.
2. Choose Link in the Actions menu, Insert template.
    You hear: Link inserted.
3. Type "https://example.org".

### Put in a table and type into its first cell

1. Press Ctrl+End.
2. Choose Table in the Actions menu, Insert template.
    You hear: Table inserted.
3. Type "Name".

### Put in a template of your own from the command palette, with the selected words carried into it

1. Press Ctrl+Home, Down, Down, Shift+End.
2. Press F1 (Command palette; Help menu).
3. Type "meeting".
    You hear: Actions, Insert template.
4. Press Enter.
    You hear: Meeting inserted.
5. Type "of Monday".

## Answer a notice and use the command palette

Starts with: Open axDown with the file notes.md.

### Read the keyboard shortcuts

1. Choose Keyboard shortcuts in the Help menu.
2. Press Escape.

### Find a command in the command palette

1. Press F1 (Command palette; Help menu).
2. Type "save as".
    You hear: Save as.
3. Press Escape.
