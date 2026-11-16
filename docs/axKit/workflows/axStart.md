# axStart: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Find and start a program

Starts with: Open axStart.

### Refresh the list of programs

1. Press F5 (Refresh programs; Session menu).
    You hear: Programs refreshed.

### Move through the matches

1. Type: windows
2. Press Down.
    You hear words such as: 2 of.
3. Press Up.
    You hear words such as: 1 of.
4. Press Up.
    You hear the sound Edge.
    You hear words such as: 1 of.

### Jump to the first or last match, or by a page

1. Press Ctrl+End (Last match).
2. Press Down.
    You hear the sound Edge.
3. Press Ctrl+Home (First match).
    You hear words such as: 1 of.
4. Press Page Down.
5. Press Page Up.
    You hear words such as: 1 of.

### Hear the selected match's kind and path

1. Press Ctrl+I (Information; Context menu).
    You hear: Windows.

### Hide the window and show it again

1. Press Escape.
2. Start axStart again.

### Quit axStart

1. Choose Quit axStart in the Session menu.

## Search programs as you type and change axStart's hotkey

Starts with: Open axStart.

### Hear the first match as you type

1. Type: windows
    You hear words such as: 1 of.

### Hear the new first match after deleting a letter

1. Press Down.
    You hear words such as: 2 of.
2. Press Backspace.
    You hear words such as: 1 of.

### Search by several words

1. Press Ctrl+A.
2. Type: win power
    You hear: PowerShell.

### Change the hotkey

1. Choose Hotkey in the Options menu.
2. Press A.
    You hear: Use Ctrl, Alt or the Windows key with it.
3. Press Ctrl+Alt+Shift+F9.
4. Press Enter.
    You hear: Hotkey Ctrl+Alt+Shift+F9.

### Quit axStart

1. Choose Quit axStart in the Session menu.

## Hear that nothing matches and turn the spoken match off

Starts with: Open axStart.

### Turn the spoken first match off and on

1. Choose Say the first match while typing in the Options menu, General.
    You hear: Say the first match while typing, not checked.
2. Press Ctrl+A.
3. Type: calculato
4. Press Ctrl+I (Information; Context menu).
    You hear: Calculator, Store app.
5. Choose Say the first match while typing in the Options menu, General.
    You hear: Say the first match while typing, checked.
6. Press Backspace.
    You hear words such as: Calculator, 1.

### Quit axStart

1. Choose Quit axStart in the Session menu.

## Find out whether a key arrives and what it does, in a hotkey window

Starts with: Open axStart.

### Test what a key does

1. Choose Test a key in the Help menu, Diagnostics.
    You hear: Key test. Press any key to hear its name and what it does here. Press Escape to end.
2. Press F8 (Say the status; Window menu).
    You hear: F8: Say the status.
3. Press F5 (Refresh programs; Session menu).
    You hear: F5: Refresh programs.
4. Press Ctrl+I (Information; Context menu).
    You hear: Ctrl+I: Information.
5. Press Down.
    You hear: Down: Next match.
6. Press Enter.
    You hear: Enter: Start.
7. Press A.
    You hear: A: types the letter.
8. Press F9.
    You hear: F9: no command of this window.
