# axPass: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Unlock a password database and change an entry

Starts with: Open axPass with the file test.kdbx.

### Unlock a password database

1. Type "test-master-1".
2. Press Enter.
    You hear: test, 3 items, Email, group, 1 item.

### Refresh the database

1. Press F5 (Refresh; Session menu).
    You hear: Refreshed: test, 3 items.

### Open a group and go back up

1. Press Right.
    You hear: Email, 1 item, Gmail.
2. Press Right.
    You hear the sound Edge.
3. Press Left.
    You hear: test, 3 items, Email, group, 1 item.
4. Press Right.

### Read an entry's fields

1. Press Enter.
2. Press Down three times.

### Move between the entries and the fields

1. Press Ctrl+Tab (Switch between the navigator and the detail pane).
2. Press Ctrl+Shift+Tab (Switch between the navigator and the detail pane).
3. Press F6 twice (Switch between the navigator and the detail pane).

### Change a user name

1. Press Tab.
2. Press Home.
3. Press Down.
4. Press Enter.
5. Type "sam2".
6. Press Enter.
    You hear: UserName: sam2.

### Undo a change

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: UserName: sam2.

### Rename an entry

1. Press Escape.
2. Press F2 (Rename).
3. Type "Gmail personal".
4. Press Enter.
    You hear: Renamed Gmail personal.

### Add an entry

1. Press Ctrl+N.
2. Type "Bank".
3. Press Enter.
    You hear: Added Bank.

### Move an entry to the Recycle Bin and undo it

1. Press Delete.
    You hear: Moved Bank to the Recycle Bin.
2. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Moved Bank to the Recycle Bin.

### Move several entries to the Recycle Bin at once and undo it

1. Press Ctrl+A.
    You hear: 2 selected.
2. Press Delete.
    You hear: Moved 2 entries to the Recycle Bin; Ctrl+Z brings them back.
3. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Moved 2 entries to the Recycle Bin.
4. Press End.

### Set a generated password

1. Press Up.
2. Press Tab.
3. Press Home.
4. Press Down twice.
5. Press Enter.
6. Press Alt+G.
    You hear: Generated a password of 20 characters.
7. Press Enter.
    You hear: Password set for Gmail personal.

### Move an entry to another group

1. Press Home.
2. Press Right.
3. Type "g".
4. Press Ctrl+M.
5. Press Down.
6. Press Enter.
    You hear: Moved Gmail personal to Work.
7. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Moved Gmail personal to Work.
8. Press Left.

### Lock the database

1. Press Ctrl+L (Lock; Actions menu).
    You hear: Locked.
2. Press Escape.

## Agree to the reminder before the first unlock

Starts with: Open axPass with the file test.kdbx.

### Agree to the reminder and unlock the database

You hear: Before axPass connects to KeePassXC.
1. Press Tab.
    You hear: I understand that axPass.
2. Press Enter.
    You hear: Check the box first.
3. Press Space.
    You hear: checked.
4. Press Enter.
5. Type "test-master-1".
6. Press Enter.
    You hear: test, 1 item, Email, group, 0 items.
7. Close the window.

## Read an entry in full and come back to the same database

Starts with: Open axPass with the file test.kdbx.

### Hear one field of an entry

1. Press Enter.
2. Press Down twice.
3. Press Ctrl+Q (Read in full).
    You hear: Password, password, hidden.

### Hear every field of an entry at once

1. Press Escape.
2. Press Ctrl+Q (Read in full).
    You hear: Gmail. UserName: sam. Password: hidden. URL: https://mail.example.com. Notes: empty.

### Choose when axPass locks by itself

1. Press Ctrl+L (Lock; Actions menu).
    You hear: Locked.
2. Press Escape.
3. Choose 5 minutes in the Options menu, Lock after.
    You hear: axPass locks after 5 minutes without a key.
4. Choose Never in the Options menu, Lock after.
    You hear: axPass locks only on Ctrl+L.
5. Close the window.

### Open the last database again without naming it

1. Open axPass.
2. Type "test-master-1".
3. Press Enter.
    You hear: test, 1 item, Email, group, 1 item.

## Change an entry's fields, keep a backup and delete for good

Starts with: Open axPass with the file test.kdbx.

### Make a backup now

1. Choose Back up now in the Session menu.
    You hear: Backed up: test.
    You hear something with "in test backups" in it.

### Show the backups in axFile

1. Choose Show backups in axFile in the Session menu.
2. Go to the window that opened.
3. Close the window.
4. Go back to the first window.

### Open a group with Enter and go back up with Backspace

1. Press Enter.
    You hear: Email, 2 items, Gmail.
2. Press Backspace.
    You hear: test, 2 items, Email, group, 2 items.
3. Press Enter.
    You hear: Email, 2 items, Gmail.

### Write an entry's notes over two lines

1. Press Enter.
2. Press End.
3. Press Enter.
4. Type "First line of the notes".
5. Press Enter.
    You hear: new line.
6. Type "Second line".
7. Press Tab.
8. Press Enter.
    You hear: Notes: First line of the notes, and 1 more line.

### Change an entry's URL

1. Press Up.
2. Press Enter.
3. Type "https://example.org/login".
4. Press Enter.
    You hear: URL: https://example.org/login.

### Set a password typed twice

1. Press Up.
2. Press Enter.
3. Type "made-up-pass-1".
4. Press Enter.
5. Type "made-up-pass-1".
6. Press Enter.
    You hear: Password set for Gmail.

### Change an entry's title in its fields

1. Press Home.
2. Press Enter.
3. Type "Gmail work".
4. Press Enter.
    You hear: Renamed Gmail work.

### Move an entry to the Recycle Bin

1. Press Delete.
    You hear: Moved Share to the Recycle Bin.

### Make a new group

1. Press Backspace.
    You hear: test, 3 items, Email, group, 1 item.
2. Press Ctrl+Shift+N.
3. Type "Archive".
4. Press Enter.
    You hear: Made Archive.

### Move a group to the Recycle Bin

1. Type "w".
2. Press Delete.
3. Press Alt+M.
    You hear: Moved Work to the Recycle Bin.

### Delete an entry for good from the Recycle Bin

1. Press Delete.
2. Press Alt+D.
    You hear: Deleted Share.

## Find, filter, sort and select entries of a password database

Starts with: Open axPass with the file test.kdbx.

### Find an entry and go to the next and the previous match

1. Press Ctrl+F (Find in the list).
2. Type "bank".
3. Press Enter.
4. Press F3 twice (Find next).
5. Press Shift+F3 (Find previous).

### Show only the entries whose name contains a text

1. Choose Filter in the Options menu.
2. Type "bank".
3. Press Enter.
    You hear: filter bank, 2 shown.
4. Press Escape.
    You hear: Filter cleared.

### Put the entries in the order of their names

1. Choose Name in the Options menu, Sort by.
    You hear: Sorted by name.
2. Choose The list's own order in the Options menu, Sort by.
    You hear: Sorted by the list's own order.

### Select entries as you move and deselect them all

1. Type "bank".
2. Press Space.
3. Press Shift+Down.
4. Press Ctrl+Shift+A (Deselect all; Actions menu).
    You hear: None selected.
