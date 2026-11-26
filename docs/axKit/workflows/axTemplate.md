# axTemplate: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Make, write, rename and delete a template in axTemplate's browser

Starts with: Open axTemplate.

### Make a template

1. Press Ctrl+N.
2. Type "Thanks".
3. Press Enter.
    You hear: Added Thanks.

### Write the text of a template in its form

1. Press Enter.
2. Press Tab.
3. Type "Thank you, {clipboard}.{caret}".
4. Press Ctrl+Enter (Put in, with that program's selection; Context menu).
    You hear: Saved.

### Rename a template

1. Press F2 (Rename).
2. Type "Thank you note".
3. Press Enter.
    You hear: Renamed Thank you note.

### Archive a template and bring it back

1. Press Delete.
2. Press Enter.
    You hear: Archived Thank you note.
3. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Archived Thank you note.

### Rename a template from its Title field

1. Press Up.
2. Press Enter.
3. Press Ctrl+A (Select all; Actions menu).
4. Type "Greeting".
5. Press Ctrl+Enter (Put in, with that program's selection; Context menu).
    You hear: Saved.
6. Press Escape.

## Delete several templates, and meet a file that is no template, in axTemplate's browser

Starts with: Open axTemplate.

### Archive several templates at once

1. Press Home.
2. Press Space.
3. Press Down.
4. Press Space.
5. Press Delete.
6. Press Enter.
    You hear: Archived 2 templates.
7. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: Archived 2 templates.

## Find a template in the launcher and use it

Starts with: Open axTemplate's launcher.

### Move through the templates

1. Press Down.
    You hear: Sprint notes, 2 of 3.
2. Press Down.
    You hear: Thanks, 3 of 3.
3. Press Down.
    You hear the sound Edge.
4. Press Ctrl+Home (First match).
    You hear: Greeting, 1 of 3.

### Find a template by a word of its name or its text

1. Type "sprint".
2. Press Down.
    You hear: Greeting, 2 of 2.
3. Press Up.
    You hear: Sprint notes, 1 of 2.
4. Press Ctrl+A (Select all; Actions menu).
5. Type "zzz".
6. Press Down.
    You hear: No template matches zzz.

### Read a template before using it

1. Press Ctrl+A (Select all; Actions menu).
2. Type "greet".
3. Press Ctrl+Q (Read in full; Context menu).
4. Press Escape.

### Copy a template without putting it in

1. Press Ctrl+A (Select all; Actions menu).
2. Type "thanks".
3. Press Shift+Enter (Copy without putting in; Context menu).
4. Open axTemplate's launcher again.
5. Press Down.
    You hear: Greeting, 2 of 3.
6. Press Up.
    You hear: Thanks, 1 of 3.

### Open the browser from the launcher

1. Press Ctrl+B (Open the browser; Session menu).
2. Go to the window that opened.
3. Close the window.
4. Go back to the first window.

### Quit the launcher

1. Open axTemplate's launcher again.
2. Choose Quit axTemplate in the Session menu.
