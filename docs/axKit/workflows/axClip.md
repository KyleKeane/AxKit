# axClip: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Find and paste something you copied earlier

Starts with: Open axClip.

### Move through the copied items

1. Type: axkitclip
2. Press Down.
    You hear: axkitclip beta, 2 of 3.
3. Press Up.
    You hear: axkitclip gamma, 1 of 3.
4. Press Up.
    You hear the sound Edge.
    You hear: axkitclip gamma, 1 of 3.

### Jump to the newest or oldest item, or by a page

1. Press Ctrl+End (Last match).
    You hear: axkitclip alpha, 3 of 3.
2. Press Down.
    You hear the sound Edge.
3. Press Ctrl+Home (First match).
    You hear: axkitclip gamma, 1 of 3.
4. Press Page Down.
    You hear: axkitclip alpha, 3 of 3.
5. Press Page Up.
    You hear: axkitclip gamma, 1 of 3.

### Read an item's whole text

1. Press Ctrl+Q (Read in full; Context menu).
2. Press Escape.

### Pause the recording and start it again

1. Press Ctrl+P (Pause recording; Options menu).
    You hear: Recording paused.
2. Press Ctrl+P (Pause recording; Options menu).

### Remove an item

1. Press Delete.
    You hear: Removed.
2. Press Ctrl+End (Last match).
    You hear words such as: 2 of 2.

### Clear the clipboard history

1. Press Ctrl+Shift+Delete (Clear history; Actions menu).
2. Press Enter.
    You hear: Clipboard history cleared.
3. Press Down.
    You hear the sound Edge.

### Hide the window and show it again

1. Press Escape.
2. Start axClip again.

### Quit axClip

1. Choose Quit axClip in the Session menu.

## Copy again, read a long copy and keep the history between runs

Starts with: Open axClip.

### Read a copy of several lines in full

1. Press Ctrl+Home (First match).
    You hear: axkitkeep first, 3 lines, 1 of 3.
2. Press Ctrl+Q (Read in full; Context menu).
3. Press Escape.

### Keep the history between runs

1. Choose Keep history between runs in the Options menu.
2. Choose Quit axClip in the Session menu.
3. Open axClip.
4. Type: axkitkeep
5. Press Down.
    You hear: axkitkeep one, 2 of 3.
6. Press Ctrl+End (Last match).
    You hear: axkitkeep two, 3 of 3.

## Start with a kept history that can no longer be read
