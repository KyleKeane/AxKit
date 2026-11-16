# axDown: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Open a text file in axDown

### Open axDown with an empty document

1. Open axDown.

### Open a file in axDown

1. Open axDown with plain.txt.

### Open a text file without naming an app

1. Open plain.txt.

### Start a new file by its name

1. Open axDown with missing.md.

## Read and write text in axDown

Starts with: Open axDown with long.md.

### Read the text line by line and word by word

1. Press Down.
2. Press Down.
3. Press Ctrl+Right, Ctrl+Right.

### Move from paragraph to paragraph

1. Press Ctrl+Home.
2. Press Ctrl+Down.
3. Press Ctrl+Down.
4. Press Ctrl+Down.
5. Press Ctrl+Down.

### Hear the line and column

1. Press F8 (Say the status).
   You hear: Line 9, Column 1.

### Select text a paragraph at a time

1. Press Ctrl+Home, Ctrl+Shift+Down.
2. Press Ctrl+Shift+Up.

### Turn word wrap off and on

1. Press Ctrl+Home, Ctrl+Down, Ctrl+Down, Right, Right, Right.
2. Press Ctrl+Shift+W (Word wrap).
   You hear: Word wrap off.
3. Press Ctrl+Shift+W (Word wrap).
   You hear: Word wrap on.

### Move a page at a time

1. Open axDown with short.md.
2. Press Pagedown.
3. Press Pageup.

### Type text, then undo and redo it

1. Open axDown with new.md.
2. Type A sentence.
3. Press Backspace.
4. Type !
5. Press Enter, Tab.
6. Type Second line.
7. Press Ctrl+Z (Undo).
8. Press Ctrl+Y (Redo).

### Undo and redo from the menu

1. Choose Redo in the Actions menu (key: Ctrl+Y).
2. Choose Undo in the Actions menu (key: Ctrl+Z).

## Hear why a file that is not text will not open

Starts with: Open axDown with image.png.

1. Press Escape.

## Open, save and switch files in axDown

### Save a change to a file

1. Open axDown with bom.md.
2. Press End.
3. Type !
4. Press Ctrl+S (Save).

### Open a new empty window

1. Press Ctrl+Shift+N (New window).
2. Take the window the app opened.
3. Close the window.
4. Use window 4.

### Start a new document when the text has changes

1. Type Added.
2. Press Ctrl+N (New).
3. Press Escape.
4. Press Ctrl+N (New).
5. Press Enter.

### Reload a file that another program changed

1. Press Ctrl+S (Save).
2. Press Tab, Tab.
3. Press Enter.

### Save over the changes of another program

1. Type Y.
2. Press Ctrl+S (Save).
3. Press Enter.

### Create a file by saving it

1. Open axDown with created.md.
2. Type Created by Ctrl+S.
3. Press Ctrl+S (Save).

### Close a window without saving its changes

1. Use window 2.
2. Type More.
3. Close the window.
4. Press Escape.
5. Close the window.
6. Press Tab, Tab.
7. Press Enter.

## Find text and go to a line

Starts with: Open axDown with find.md.

### Find a word in the text

1. Press Ctrl+F (Find; Navigation menu).
2. Type apple.
3. Press Enter.
4. Press F3 (Find next; Navigation menu).
5. Press Shift+F3 (Find previous; Navigation menu).

### Go to a line by its number

1. Press Ctrl+G (Go to line; Navigation menu).
2. Type 5.
3. Press Enter.
   You hear: Line 5 of 7.

## Change the text size and the font

Starts with: Open axDown with size.md.

### Make the text larger or smaller

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
   You hear: Text size 10.
2. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
   You hear: Text size 9.
3. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
   You hear: Text size 10.
4. Close the window.

## Find a command in the menus, the palette and the help

Starts with: Open axDown with a.md.

### Open the keyboard shortcuts

1. Press Down, Right, Right.
2. Press F1 (Keyboard shortcuts).

### Move through the shortcuts by heading

1. Press H.
   You hear: In every window of axKit, heading level 2.
2. Press Ctrl+H (Heading, Navigation, Next).
   You hear: axDown, heading level 2.
3. Press Shift+H.
   You hear: In every window of axKit, heading level 2.
4. Press H, H.
   You hear: The text, heading level 2.

### Find a word in the shortcuts

1. Press Ctrl+F (Find).
2. Type wrap.
3. Press Enter.
4. Press F3 (Find next).
5. Press Ctrl+F (Find).
6. Press Escape.

### Close the shortcuts and return to the text

1. Press Escape.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press Ctrl+H (Heading, Navigation, Next).
   You hear: heading level 2.
3. Press Ctrl+T (Table, Navigation, Next).
   You hear: No next table.
4. Press Escape.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Read the licence, the attribution and the axKit guide

1. Choose Licence in the Help menu, About.
2. Press Escape.
3. Choose Attribution in the Help menu, About.
4. Press Escape.
5. Choose axKit guide in the Help menu, About.
6. Press Escape.

### Turn the update check at start on or off

1. Press Ctrl+Shift+P (Command palette).
2. Type check for updates at start.
   You hear: Check for updates at start, Options, General, 1 of 1.
3. Press Enter.
   You hear: Check for updates at start, checked.
4. Press Ctrl+Shift+P (Command palette).
5. Type check for updates at start.
   You hear: Check for updates at start, Options, General, checked, 1 of 1.
6. Press Enter.
   You hear: Check for updates at start, not checked.

### Move through the commands of the palette

1. Press Ctrl+Shift+P (Command palette).
2. Press Down.
   You hear: New window, Session, Ctrl+Shift+N, 2 of.
3. Press Down.
   You hear: Open, Session, Ctrl+O, 3 of.
4. Press Up.
   You hear: New window, Session, Ctrl+Shift+N, 2 of.

### Run a command found by its name

1. Type wrap.
   You hear: Word wrap, Options, Text, Ctrl+Shift+W, checked, 1 of 1.
2. Press Down.
   You hear the sound Edge.
   You hear: Word wrap, Options, Text, Ctrl+Shift+W, checked, 1 of 1.
3. Press Enter.
   You hear: Word wrap off.
4. Press Ctrl+Shift+W (Word wrap).
   You hear: Word wrap on.

## Check for updates and remove axKit

Starts with: Open axDown with a.md.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Check for updates

1. Choose the menu item Check for updates (axKit.
2. Press Escape.
3. Choose Version in the Help menu, About.
4. Press Escape.

## Move through a Markdown file by headings, lists and links

Starts with: Open axDown with long.md.

### Move to the next heading

1. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Second heading, level 2, heading level 2.
2. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Third heading, level 3, heading level 3.
3. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Lists, heading level 2.
4. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Quote and table, heading level 2.
5. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Code, heading level 2.
6. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Fourth level heading, heading level 4.
7. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Fifth level heading, heading level 5.
8. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Sixth level heading, heading level 6.
9. Press Ctrl+H (Heading, Navigation, Next).
   You hear: No next heading.

### Move back to the previous heading

1. Press Ctrl+Shift+H, Ctrl+Shift+H, Ctrl+Shift+H, Ctrl+Shift+H.
   You hear: Code, heading level 2.
2. Press Ctrl+Shift+H, Ctrl+Shift+H, Ctrl+Shift+H, Ctrl+Shift+H.
   You hear: Long Markdown file for navigation, heading level 1.
3. Press Ctrl+Shift+H (Heading, Navigation, Previous).
   You hear: No previous heading.

### Move to a heading of one level

1. Press Ctrl+Home, Ctrl+2.
   You hear: Second heading, level 2, heading level 2.
2. Press Ctrl+2 (Heading level 2, Navigation, Next).
   You hear: Lists, heading level 2.
3. Press Ctrl+2, Ctrl+2.
   You hear: Code, heading level 2.
4. Press Ctrl+2 (Heading level 2, Navigation, Next).
   You hear: No next heading level 2.
5. Press Ctrl+Shift+2 (Heading level 2, Navigation, Previous).
   You hear: Quote and table, heading level 2.
6. Press Ctrl+4 (Heading level 4, Navigation, Next).
   You hear: Fourth level heading, heading level 4.
7. Press Ctrl+Shift+4 (Heading level 4, Navigation, Previous).
   You hear: No previous heading level 4.

### Move by heading from the menu

1. Press Ctrl+Home.
2. Choose Heading level 3 in the Navigation menu, Next (key: Ctrl+3).
   You hear: Third heading, level 3, heading level 3.
3. Choose Heading level 1 in the Navigation menu, Previous (key: Ctrl+Shift+1).
   You hear: Long Markdown file for navigation, heading level 1.
4. Choose Heading in the Navigation menu, Next (key: Ctrl+H).
   You hear: Second heading, level 2, heading level 2.
5. Choose Heading in the Navigation menu, Previous (key: Ctrl+Shift+H).
   You hear: Long Markdown file for navigation, heading level 1.

### Move to the next link

1. Press Ctrl+Home, Ctrl+K.
   You hear: GitHub, link.
2. Press Ctrl+K (Link, Navigation, Next).
   You hear: https://www.example.com, link.
3. Press Ctrl+K (Link, Navigation, Next).
   You hear: https://example.org, link.
4. Press Ctrl+K (Link, Navigation, Next).
   You hear: No next link.

### Move back to the previous link

1. Press Ctrl+Shift+K (Link, Navigation, Previous).
   You hear: https://www.example.com, link.
2. Press Right, Right, Right.
3. Press Ctrl+Shift+K (Link, Navigation, Previous).
   You hear: GitHub, link.

### Move by list and by list item

1. Press Ctrl+Home, Ctrl+L.
   You hear: List with 3 items, one.
2. Press Ctrl+I (List item, Navigation, Next).
3. Press Ctrl+I (List item, Navigation, Next).
   You hear: two, nested.
4. Press Ctrl+I (List item, Navigation, Next).
5. Press Ctrl+I (List item, Navigation, Next).
   You hear: numbered one.
6. Press Ctrl+Home, Ctrl+L, Ctrl+L.
   You hear: List with 2 items, numbered one.
7. Press Ctrl+L (List, Navigation, Next).
   You hear: No next list.
8. Press Ctrl+Shift+L (List, Navigation, Previous).
   You hear: List with 3 items, one.

### Move to a block quote or a table

1. Press Ctrl+Home, Ctrl+Q.
   You hear: Block quote, A block quote, line one.
2. Press Ctrl+Q (Block quote, Navigation, Next).
   You hear: No next block quote.
3. Press Ctrl+Home, Ctrl+T.
   You hear: Table with 3 rows and 2 columns.
4. Press Ctrl+T (Table, Navigation, Next).
   You hear: No next table.

## Show the current file in another app

Starts with: Open axDown with sub\notes.md.

1. Press Ctrl+W (File Explorer).
   You hear: File missing.

## Change the colours with the theme file

Starts with: Open axDown with notes.md.

### Make a theme file and open it

1. Choose Edit theme file in the axKit menu, Theme.
2. Take the window the app opened.
3. Close the window.
4. Use window 1.

### Reload the theme

1. Choose Reload theme in the axKit menu, Theme.
   You hear: Theme reloaded.

### Turn the theme off and on

1. Choose Theme on in the axKit menu, Theme.
   You hear: Theme off.
2. Choose Theme on in the axKit menu, Theme.
   You hear: Theme on.

## Ask Claude about the window you are in

Starts with: Open axDown with notes.md.

### Turn Claude on by agreeing to the reminder

1. Press Shift+F1 (Ask about this window).
2. Press Tab.
   You hear: I understand.
3. Press Enter.
   You hear: Check the box first.
4. Press Space.
5. Press Enter.
   You hear: Claude on.
6. Press Escape.

### Turn Claude off and on

1. Choose Use Claude in axKit in the axKit menu, Claude.
   You hear: Claude off.
2. Choose Use Claude in axKit in the axKit menu, Claude.
   You hear: Claude on.

### Ask Claude about this window

1. Press Shift+F1 (Ask about this window).
2. Type how do I turn word wrap off.
3. Press Enter.

### Ask more about an answer

1. Press Tab.
2. Type and back on.
3. Press Enter.
4. Press Escape.

### See the folders Claude may read

1. Choose Folders Claude may read in the axKit menu, Claude.
2. Press Escape.

## Hear a line break as you type it

Starts with: Open axDown with notes.md.

### Start a new line and hear it said

1. Press Ctrl+End.
2. Press Enter.
   You hear: new line.
3. Type Second line.

### Split a line in two

1. Press Home.
2. Press Right, Right, Right, Right, Right, Right.
3. Press Enter.
   You hear: new line.

### Turn the spoken new line off and on

1. Choose Say new line in the Options menu, Text.
   You hear: Say new line off.
2. Press End, Enter.
3. Choose Say new line in the Options menu, Text.
   You hear: Say new line on.
4. Press Enter.
   You hear: new line.

## Hear the heading sound as you move

Starts with: Open axDown with headings.md.

### Hear the sound when the caret reaches a heading

1. Press Down.
   You hear the sound Heading.

### Turn the heading sound off and on

1. Choose Sound on headings in the Options menu, Text.
   You hear: Sound on headings off.
2. Press Down, Down, Down, Down.
3. Choose Sound on headings in the Options menu, Text.
   You hear: Sound on headings on.
4. Press Up.
5. Press Down.
   You hear the sound Heading.

## Open a recent file or window from the command palette

Starts with: Open axDown with one.md. Open axDown with two.md.

### Open a recent file from the palette

1. Press Ctrl+Shift+P (Command palette).
2. Type one.md.
   You hear: Open recent file.
3. Press Enter.

### Find another open window from the palette

1. Press Ctrl+Shift+P (Command palette).
2. Type open windows.
   You hear: one.md - axDown, Window, Open windows.
3. Press Escape.

### Reopen a window that was closed

1. Use window 1.
2. Close the window.
3. Use window 2.
4. Press Ctrl+Shift+P (Command palette).
5. Type recently closed.
   You hear: 1. one.md.
   You hear: Window, Recently closed.
6. Press Enter.
7. Take the window the app opened.

## Find out whether a key arrives and what it does

Starts with: Open axDown with a.md.

### Start testing keys

1. Press Down, Down, Right.
2. Choose Test a key in the Help menu, Diagnostics.
   You hear: Key test. Press any key to hear its name and what it does here. Press Escape to end.

### Hear what a key does without carrying it out

1. Press F8 (Say the status).
   You hear: F8: Say the status.
2. Press F4 (Send a report).
   You hear: F4: Send a report.
3. Press F1 (Keyboard shortcuts).
   You hear: F1: Keyboard shortcuts.
4. Press Ctrl+Shift+P (Command palette).
   You hear: Ctrl+Shift+P: Command palette.
5. Press Ctrl+G (Go to line).
   You hear: Ctrl+G: Go to line.
6. Press Ctrl+H (Heading, Navigation, Next).
   You hear: Ctrl+H: Next, Heading.
7. Press A.
   You hear: A: types the letter.
8. Press Down.
   You hear: Down: moves in the text.
9. Press Enter.
   You hear: Enter: makes a new line.
10. Press Ctrl+Down.
   You hear: Ctrl+Down: Paragraph down.
11. Press F9.
   You hear: F9: no command of this window.

### End the key test

1. Press Escape.
   You hear: Key test ended.

## Move through a file with single letters without changing it, then type again

Starts with: Open axDown with modes.md.

### Turn on navigation mode

1. Press Ctrl+Space.
   You hear: Navigation mode.

### Move by single letters

1. Press H.
   You hear: Links, heading level 2.
2. Press H.
   You hear: List, heading level 2.
3. Press H.
   You hear: No next heading.
4. Press Shift+H.
   You hear: Links, heading level 2.
5. Press Ctrl+Home, 2.
   You hear: Links, heading level 2.
6. Press K.
   You hear: GitHub, link.
7. Press L.
   You hear: List with 2 items, one.
8. Press I.
9. Press Shift+I.

### Return to edit mode and type

1. Press Ctrl+Space.
   You hear: Edit mode.
2. Press Home.
3. Type X.
4. Press Ctrl+Z (Undo).

## Answer a notice and use the command palette

Starts with: Open axDown with notes.md.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts).
2. Press Escape.

### Find a command in the command palette

1. Press Ctrl+Shift+P (Command palette).
2. Type save as.
   You hear: Save as.
3. Press Escape.
