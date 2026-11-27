# axFile: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Browse folders and files

### Open a folder in axFile

1. Open axFile with the folder nav.

### Move through the list of files

1. Press Down.
2. Press End.
3. Press Home.

### Find a file by typing its name

1. Type "ban".
    It reads banana.txt.
2. Type "c".
3. Type "c".

### Move between the list and the Favorites list

1. Press Tab.
2. Press Shift+Tab.

### Hear the status bar

1. Press F9 (Say the status; Window menu).
    You hear something with "nav" in it.

### Open a folder in the list

1. Press Home.
2. Press Down.
3. Press Enter.
    You hear: level-1-enter-me, 2 items, level-2-enter-me, folder.

### Go back to the folder above

1. Press Backspace.
    You hear: nav, 13 items, level-1-enter-me, folder.
2. Press Enter.
    You hear: level-1-enter-me, 2 items.
3. Press Backspace.
    You hear: nav, 13 items, level-1-enter-me, folder.

### Go to a folder by its path

1. Press F12 (Go to folder; Navigation menu).
2. Type "nav\level-1-enter-me\level-2-enter-me".
3. Press Enter.
    You hear: level-2-enter-me, 2 items, level-3-enter-me, folder.

### Go to the drives of This PC

1. Press F12 (Go to folder; Navigation menu).
2. Type "C:\".
3. Press Enter.
    You hear something with "C:\" in it.
4. Press Backspace.
    You hear: (C:), drive.

### Open a drive

1. Press Enter.
    You hear something with "C:\" in it.
2. Press Left.
    You hear something with "This PC" in it.
3. Press Right.
    You hear something with "C:\" in it.
4. Press Backspace.
    You hear something with "This PC" in it.

### Move down and up the folder tree

1. Press F12 (Go to folder; Navigation menu).
2. Type "nav".
3. Press Enter.
    You hear: nav, 13 items.
4. Press Down.
5. Press Right.
    You hear: level-1-enter-me, 2 items, level-2-enter-me, folder.
6. Press Right.
    You hear: level-2-enter-me, 2 items, level-3-enter-me, folder.
7. Press Right.
    You hear: level-3-enter-me, 1 item, you-reached-the-bottom.txt.
8. Press Right.
    You hear the sound Edge.
9. Press Left.
    You hear: level-2-enter-me, 2 items, level-3-enter-me, folder.
10. Press Left.
    You hear: level-1-enter-me, 2 items, level-2-enter-me, folder.
11. Press Left.
    You hear: nav, 13 items, level-1-enter-me, folder.

### Hear the details of a folder or a file

1. Press F8 (Information; Context menu).
    You hear: level-1-enter-me, folder, modified.
2. Press End.
3. Press F8 (Information; Context menu).
    You hear: fig.txt, 13 bytes, modified.

### Go to a folder named by an environment variable

1. Type "%SystemRoot%".
2. Press Enter.
    You hear something with "Windows" in it.

### Go to a file by its path

1. Press F12 (Go to folder; Navigation menu).
2. Type "nav\level-1-enter-me\level-1-file.txt".
3. Press Enter.
    You hear: level-1-enter-me, 2 items, level-1-file.txt.

### Complete a path from its first letters

1. Press F12 (Go to folder; Navigation menu).
2. Type "nav\lev".
3. Press Down.
    You hear: level-1-enter-me, folder, 1 of 1.

### Open a completed path

1. Press F12 (Go to folder; Navigation menu).
2. Type "nav\level-1-enter-me\level-2".
3. Press Down.
    You hear: level-2-enter-me, folder, 1 of 1.
4. Press Enter.
    You hear: level-2-enter-me, 2 items, level-3-enter-me, folder.

### Add a folder to the favorites and remove it

1. Press Ctrl+D.
    You hear: Added to Favorites.

### Remove the folder from the favorites

1. Press Ctrl+D.
    You hear: Removed from Favorites.

### Refresh the folder after a change from outside

1. Press End.
2. Press F5 (Refresh; Session menu).
    You hear: Refreshed.

### Show hidden files and names that start with a period

1. Press F12 (Go to folder; Navigation menu).
2. Type "hid".
3. Press Enter.
    You hear: hid, 1 item, visible-file.txt.
4. Press End.
5. Press Enter.
    You hear: Not shown: 1 hidden, 1 starting with a period.
6. Press Up.
7. Choose Show files and folders starting with a period in the Options menu, General.
8. Choose Show hidden files and folders in the Options menu, General.

### Find a file by part of its name

1. Press F12 (Go to folder; Navigation menu).
2. Type "fs".
3. Press Enter.
    You hear: fs, 9 items, chapter-01-notes.txt.
4. Press Ctrl+F (Find).
5. Type "find".
6. Press Enter.

### Go to the next and the previous match

1. Press Home.
2. Press Ctrl+F (Find).
3. Type "notes".
4. Press Enter.
5. Press F3 (Find next).

### Go back to the previous match

1. Press Shift+F3 (Find previous).

### Show only the entries whose names contain a word

1. Press Home.
2. Choose Filter in the Options menu.
3. Type "chap".
4. Press Enter.
    You hear: 9 items, filter chap, 5 shown, chapter-01-notes.txt.

### Clear the filter

1. Press Ctrl+Delete (Clear the filter).
    You hear: Filter cleared.

### Sort the list by date, size or name

1. Choose Date in the Options menu, Sort by.
    You hear: Sorted by date.
2. Press Home.
3. Press Down.

### Select several entries

1. Press F12 (Go to folder; Navigation menu).
2. Type "sel".
3. Press Enter.
    You hear: sel, 14 items, select-folder-a, folder.
4. Press Down twice.
5. Press Space.
    You hear: Selected.
6. Press Down.
7. Press Up.
8. Press Down, Space.
    You hear: Selected, 2 selected.

### Select all the entries

1. Press Ctrl+A (Select all; Actions menu).
    You hear: 14 selected.
2. Press Down.

### Clear the selection

1. Press Space, Down, Space.
    You hear: Selected, 2 selected.
2. Press Escape.
    You hear: None selected.
3. Press Up.

### Select a run of entries in a row

1. Press Shift+Down three times.
2. Press Shift+Up.

### Select every entry from here to the last

1. Press Down.
2. Press Shift+End.
3. Press F8 (Information; Context menu).
    You hear something with "12 selected" in it.
4. Press Ctrl+Shift+A (Deselect all; Actions menu).
    You hear: None selected.

## Open a folder in axFile from another app

Starts with: Open axFile with the folder here.

### Show the file of an axDown window in axFile

1. Open axDown with the file here\b.txt.
2. Choose axFile in the Session menu, Show current file in.
3. Go to the window that opened.

## Copy, move, rename and delete files

Starts with: Open axFile with the folder ops.

### Make a new folder

1. Press Ctrl+Shift+N (New folder; Actions menu).
2. Press Enter.
    You hear: Type a name.
3. Type "a?b".
4. Press Enter.
    You hear: A name cannot contain.
5. Press Backspace three times.
6. Type "notes".
7. Press Enter.
    You hear: Created notes.

### Make a new text file

1. Press Ctrl+T (New text file; Actions menu).
2. Type "todo.md".
3. Press Enter.
    You hear: Renamed to todo.md.

### Move selected files into a new folder

1. Press Space.
    You hear: Selected.
2. Press Down three times.
3. Press Space.
    You hear: Selected, 2 selected.
4. Choose the menu item Move to folder, Context.
5. Type "bundle".
6. Press Enter.
    You hear: Moved 2 items into bundle.

### Rename a folder

1. Press Home.
2. Press Down twice.
3. Press F2 (Rename; Context menu).
4. Type "Notes".
5. Press Enter.
    You hear: Renamed to Notes.

### Delete a folder to the Recycle Bin

1. Press Home.
2. Press Down twice.
3. Press Delete.
4. Press Escape.
5. Press Delete.
6. Press Enter.
    You hear: Deleted Notes.

### Compress a folder to a zip file

1. Press Down.
2. Choose the menu item Compress to zip, Context.
    You hear: Compressed to docs.zip.

### Compress selected files into one zip file

1. Press Ctrl+F (Find).
2. Type "move-into".
3. Press Enter.
4. Press Space, Shift+Down.
5. Choose the menu item Compress to zip, Context.
    You hear: Compressed to ops.zip.

### Extract a zip file here

1. Press Ctrl+F (Find).
2. Type "docs.zip".
3. Press Enter.
4. Choose Extract here in the Context menu.
    You hear: Extracted to docs - 2.

### Undo a rename

1. Press Ctrl+F (Find).
2. Type "rename-then".
3. Press Enter.
4. Press F2 (Rename; Context menu).
5. Type "renamed".
6. Press Enter.
    You hear: Renamed to renamed.txt.
7. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: renamed back to rename-then-undo.txt.

### Undo a new folder

1. Press Ctrl+Shift+N (New folder; Actions menu).
2. Type "temp".
3. Press Enter.
    You hear: Created temp.
4. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: temp removed.

### Undo a move to a folder

1. Press Ctrl+F (Find).
2. Type "move-two".
3. Press Enter.
4. Press Space, Shift+Down.
5. Choose the menu item Move to folder, Context.
6. Type "bundle".
7. Press Enter.
    You hear: Moved 2 items into bundle.
8. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: moved back.

### Undo a compress

1. Press Home.
2. Choose the menu item Compress to zip, Context.
    You hear: Compressed to compress-then-undo.zip.
3. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: compress-then-undo.zip sent to the Recycle Bin.

### Undo a delete

1. Press Ctrl+F (Find).
2. Type "delete-then".
3. Press Enter.
4. Press Delete.
5. Press Enter.
    You hear: Deleted delete-then-undo.txt.
6. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: delete-then-undo.txt restored.

### Undo the delete of several entries

1. Press Ctrl+F (Find).
2. Type "delete-two".
3. Press Enter.
4. Press Space, Shift+Down.
5. Press Delete.
6. Press Enter.
    You hear: Deleted 2 items.
7. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: 2 items restored.

## Play or read a file from axFile

Starts with: Open axFile with the folder hand.

### Read a file in axPreview

1. Press End.
2. Press Shift+Enter.
3. Go to the window that opened.

### Read another file in the same axPreview window

1. Go back to the first window.
2. Press Up.
3. Press Ctrl+Enter (Preview; Context menu).
4. Go to the window that opened.

### Open a folder in a new axFile window

1. Go back to the first window.
2. Press Home.
3. Press Shift+Enter.
4. Go to the window that opened.

## Use axFile's text, notices and axKit menu

Starts with: Open axFile with the folder pal.

### Change the text size

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press Escape.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

### Run a command from the command palette

1. Press F1 (Command palette; Help menu).
2. Type "oth".
    You hear: No commands match oth.
3. Press Escape.
4. Press F1 (Command palette; Help menu).
5. Type "refresh".
    You hear: Refresh, Session, F5, 1 of 1.
6. Press Enter.
    You hear: Refreshed.

### Open axFile on the last folder

1. Close the window.
2. Open axFile.

## Keep favorite folders and go back to recent operations

Starts with: Open axFile with the folder act.

### Move to the Favorites list from the menu

1. Choose The Favorites list in the Navigation menu.
2. Press Shift+Tab.

### Add a folder to the Favorites list

1. Press Enter.
    You hear: favorite-me, 1 item, readme.txt.
2. Press Ctrl+D.
    You hear: Added to Favorites.

### Open a favorite from the Favorites list

1. Press Tab.
2. Press Home.
3. Press Enter.
    You hear: favorite-me, 1 item, readme.txt.

### Remove a favorite from the Favorites list

1. Press Delete.
    You hear: Removed from Favorites.
2. Press Escape.
3. Press Backspace.
    You hear: act, 8 items, favorite-me, folder.

### See the last operation in the status bar

1. Press Ctrl+Shift+N (New folder; Actions menu).
2. Type "temp".
3. Press Enter.
    You hear: Created temp.

### Read the recent operations again

1. Press F2 (Rename; Context menu).
2. Type "notes".
3. Press Enter.
    You hear: Renamed to notes.
4. Choose Recent operations in the Actions menu.
5. Press Down.
6. Press Escape.

### Go to the folder of an operation from Recent operations

1. Choose Recent operations in the Actions menu.
2. Press Down.
3. Press Enter.

### Undo the newest operation from either list

1. Press Tab.
2. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: renamed back to temp.
3. Press Escape.

### Go to an operation done in another folder

1. Press Ctrl+F (Find).
2. Type "subfolder".
3. Press Enter twice.
    You hear: subfolder-for-inner, 1 item, readme.txt.
4. Press Ctrl+Shift+N (New folder; Actions menu).
5. Type "inner".
6. Press Enter.
    You hear: Created inner.
7. Press Backspace.
    You hear: act, 9 items, subfolder-for-inner, folder.
8. Choose Recent operations in the Actions menu.
9. Press Enter.
    You hear: subfolder-for-inner, 2 items, inner, folder.

### Undo the newest operation from the folder list

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: 2 items restored.

### Select entries without an operation

1. Press Ctrl+F (Find).
2. Type "start-here".
3. Press Enter.
4. Press Space.
    You hear: Selected.
5. Choose Recent operations in the Actions menu.
6. Press Escape.

### Select every entry from the marked one to here

1. Press Home.
2. Press Down.
3. Press Space.
    You hear: Selected.
4. Press Down three times.
5. Press Shift+Space.
    You hear: Selected, 4 selected.

## Move files to a folder you type

Starts with: Open axFile with the folder ops.

### Move a file into a folder by typing its name

1. Choose the menu item Move to folder, Context.
2. Type "do".
3. Press Down.
    You hear: docs, folder, 1 of 1.
4. Press Up.
    You hear the sound Edge.
    You hear: docs, folder, 1 of 1.
5. Press Enter.
    You hear: Moved a.txt into docs.

### Undo the move

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: moved back.

## Open a recent folder in axFile from the command palette

Starts with: Open axFile with the folder orchard.

### Open a recent folder from the command palette

1. Press F1 (Command palette; Help menu).
2. Type "orch".
    You hear something with "2. orchard" in it.
    You hear: Session, Open recent folder.
3. Press Enter.
    You hear: orchard, 2 items, inner, folder.

## Find out whether a key arrives and what it does, in a list

Starts with: Open axFile with the folder orchard.

### Start the key test

1. Choose Test a key in the Help menu, Diagnostics.
    You hear: Key test. Press any key to hear its name and what it does here. Press Escape to end.

### Hear what a key does without doing it

1. Press F9 (Say the status; Window menu).
    You hear: F9: Say the status.

### End the key test

1. Press Escape.
    You hear: Key test ended.

## Change settings in two windows of one app

Starts with: Open axFile with the folder one. Open axFile with the folder two.

### Change one setting in each of two windows

1. Go back to the first window.
2. Choose Show hidden files and folders in the Options menu, General.
    You hear: Show hidden files and folders.
3. Go to the window that opened.
4. Choose Show files and folders starting with a period in the Options menu, General.
    You hear: Show files and folders starting with a period.

## Take back a new text file

Starts with: Open axFile with the folder u.

### Undo a new text file

1. Press Ctrl+T (New text file; Actions menu).
2. Press Escape.
3. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: New file.txt removed.

### Make a new Markdown file and a new JSON file

1. Choose New Markdown file in the Actions menu.
2. Press Escape.

### Make a new JSON file

1. Choose New JSON file in the Actions menu.
2. Press Escape.

## Hear why a folder a program runs in cannot be renamed

Starts with: Open axFile with the folder r.

### Rename the folder once the program has ended

1. Press F2 (Rename; Context menu).
2. Type "kept".
3. Press Enter.
    You hear: Renamed to kept.
