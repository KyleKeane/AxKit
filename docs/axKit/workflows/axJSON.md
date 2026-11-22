# axJSON: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Open a JSON file in axJSON

### Open axJSON with an empty document

1. Open axJSON.

### Open a JSON file

1. Open axJSON with the file settings.json.
2. Open the file settings.json.

### Reload a file that another program changed

1. Go to the axJSON window.
2. Press Down.
3. Type: X
4. Press Ctrl+S (Save; Session menu).
5. Press Tab 2 times.
6. Press Enter.
    You hear: Reloaded.

## Move through a JSON file by its structure

Starts with: Open axJSON with the file settings.json.

### Move from key to key

1. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: model: opus.
2. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: permissions, object with 2 keys.
3. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: allow, array with 2 items.
4. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: deny, empty array.
5. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: count: 3.
6. Press Ctrl+K (Key; Navigation menu, Next).
    You hear: on: true.

### Move back from key to key

1. Press Ctrl+Shift+K (Key; Navigation menu, Previous).
    You hear: count: 3.
2. Press Ctrl+Shift+K (Key; Navigation menu, Previous).
    You hear: deny, empty array.
3. Press Ctrl+Shift+K (Key; Navigation menu, Previous).
    You hear: allow, array with 2 items.
4. Press Ctrl+Shift+K (Key; Navigation menu, Previous).
    You hear: permissions, object with 2 keys.
5. Press Ctrl+Shift+K (Key; Navigation menu, Previous).
    You hear: model: opus.

### Move to the next object and back to the whole file

1. Press Ctrl+Home, Ctrl+J.
    You hear: permissions, object with 2 keys.
2. Press Ctrl+J (Object; Navigation menu, Next).
    You hear: No next object.
3. Press Ctrl+Shift+J (Object; Navigation menu, Previous).
    You hear: object with 4 keys.

### Move through arrays and their items

1. Press Ctrl+Home, Ctrl+L.
    You hear: allow, array with 2 items.
2. Press Ctrl+L (Array; Navigation menu, Next).
    You hear: deny, empty array.
3. Press Ctrl+Home, Ctrl+I.
    You hear: Item 1 of 2, Bash.
4. Press Ctrl+I (Item; Navigation menu, Next).
    You hear: Item 2 of 2, Read.
5. Press Ctrl+I (Item; Navigation menu, Next).
    You hear: No next item.

### Move by the structure from the Navigation menu

1. Press Ctrl+Home.
2. Choose Key in the Navigation menu, Next (key: Ctrl+K).
    You hear: model: opus.
3. Choose Object in the Navigation menu, Next (key: Ctrl+J).
    You hear: permissions, object with 2 keys.
4. Choose Array in the Navigation menu, Next (key: Ctrl+L).
    You hear: allow, array with 2 items.
5. Choose Item in the Navigation menu, Next (key: Ctrl+I).
    You hear: Item 1 of 2, Bash.
6. Choose Item in the Navigation menu, Previous (key: Ctrl+Shift+I).
    You hear: No previous item.
7. Choose Array in the Navigation menu, Previous (key: Ctrl+Shift+L).
    You hear: allow, array with 2 items.
8. Choose Object in the Navigation menu, Previous (key: Ctrl+Shift+J).
    You hear: permissions, object with 2 keys.
9. Choose Object in the Navigation menu, Previous (key: Ctrl+Shift+J).
    You hear: object with 4 keys.
10. Choose Key in the Navigation menu, Previous (key: Ctrl+Shift+K).
    You hear: No previous key.
11. Choose Error in the Navigation menu, Next (key: Ctrl+E).
    You hear: No next error.
12. Choose Error in the Navigation menu, Previous (key: Ctrl+Shift+E).
    You hear: No previous error.

### Go to the next and the previous error

1. Open axJSON with the file broken.json.
2. Press Ctrl+Home, Ctrl+E.
    You hear: No next error.
3. Open axJSON with the file missing-value.json.
4. Press Ctrl+Home, Ctrl+E.
    You hear: Error, line 4: expected a value, found ','.
5. Press Ctrl+End, Ctrl+Shift+E.
    You hear: Error, line 4: expected a value, found ','.

## Format a JSON file

### Format a file written on one line

1. Open axJSON with the file one-line.json.
2. Press Ctrl+Shift+F (Format; Actions menu).
    You hear: Formatted.
3. Press Ctrl+Home.
4. Press Down.

### Format and repair a broken file

1. Open axJSON with the file broken.json.
2. Press Ctrl+Shift+F (Format; Actions menu).
    You hear: Formatted, repaired: 1 trailing comma, 1 missing comma.
3. Press Ctrl+E (Error; Navigation menu, Next).
    You hear: No next error.

### Repair a single-quoted value

1. Press Ctrl+Home, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K.
    You hear: on: true.
2. Press End.
3. Type: , "q": 'x'
4. Press Ctrl+Shift+F (Format; Actions menu).
    You hear: Formatted, repaired: 1 single-quoted string.

### Undo and redo a format

1. Go to the other axJSON window.
2. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone.
3. Press Ctrl+Y (Redo; Actions menu).
    You hear: Redone.

## Use axJSON's axKit menu and About

Starts with: Open axJSON with the file settings.json.

### Hear the version of axKit

1. Choose Version in the Help menu, About.
2. Press Escape.

## Use axJSON's Context menu

Starts with: Open axJSON with the file settings.json.

### Select all the text

1. Choose Select all in the Context menu (key: Ctrl+A).
2. Press Right.

### Select all the text from the command palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: select all, context
    You hear: Select all, Context, Ctrl+A.
3. Press Enter.
4. Press Left.

## Save, find, go to a line and wrap in axJSON

Starts with: Open axJSON with the file settings.json.

### Find text in a JSON file

1. Press Ctrl+F (Find; Navigation menu).
2. Type: apple
3. Press Enter.
4. Press F3 (Find next; Navigation menu).
5. Press Shift+F3 (Find previous; Navigation menu).

### Go to a line of a JSON file by its number

1. Press Ctrl+G (Go to line; Navigation menu).
2. Type: 5
3. Press Enter.
    You hear: Line 5 of 8.

### Turn word wrap off and on in axJSON

1. Press Ctrl+G (Go to line; Navigation menu).
2. Type: 6
3. Press Enter.
    You hear: Line 6 of 8.
4. Press Right 3 times.
5. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap off.
6. Press Ctrl+Shift+W (Word wrap; Options menu, Text).
    You hear: Word wrap on.

### Save a change to a JSON file

1. Press Ctrl+G (Go to line; Navigation menu).
2. Type: 4
3. Press Enter.
4. Press End, Left.
5. Type: 0
6. Press Ctrl+S (Save; Session menu).
    You hear: Saved.

### Close axJSON when the file has changes

1. Press End.
2. Type: ,
3. Close the window.
4. Press Escape.
5. Close the window.
6. Press Tab 2 times.
7. Press Enter.

## Write a list's configuration by choosing

Starts with: Open axJSON with the file lists\Home\list.json.

### Choose a value from the values a member can take

1. Press Ctrl+Home, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K.
    You hear: type: check.
2. Press Alt+Right (Choose a value; Actions menu).
3. Press Down.
4. Press Enter.
    You hear: type: choice.

### Change a value in line to the next and the previous

1. Press Alt+Down (Next value; Actions menu).
    You hear: type: link, 8 of 8.
2. Press Alt+Down (Next value; Actions menu).
    You hear the sound Edge.
    You hear: type: link, the last of 8.
3. Press Alt+Up (Previous value; Actions menu).
    You hear: type: choice, 7 of 8.

### Add a member an object can have

1. Press Alt+Left (Add a member; Actions menu).
2. Press Enter.
    You hear: Added options: The values to choose from, for the type choice.

### Add a key to an action and choose it

1. Press Ctrl+Home, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K.
    You hear: name: Later.
2. Press Alt+Left (Add a member; Actions menu).
3. Press Enter.
    You hear: Added key: The key that runs it; without one it runs from the menu.
4. Press Alt+Right (Choose a value; Actions menu).
5. Press End.
6. Press Enter.
    You hear: key: Ctrl+Shift+9.

### Choose an item's value through its link to the list's configuration

1. Open axJSON with the file lists\Home\tap.json.
2. Press Ctrl+Home, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K.
    You hear: Priority: Normal.
3. Press Alt+Right (Choose a value; Actions menu).
4. Press Up.
5. Press Enter.
    You hear: Priority: High.
