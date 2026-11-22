# axMedia: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Build and move through a play queue

### Open axMedia with an empty queue

1. Open axMedia.

### Play a folder of sound files

1. Open axMedia with the folder ch.
    You hear: Playing alpha.wav.
2. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear words such as: Paused at.

### Move through the queue

1. Press Down.
    It reads beta.wav.
2. Press End.
    It reads gamma.wav.
3. Press Home.
    It reads alpha.wav (playing).
4. Type: g
    It reads gamma.wav.

### Find a file by part of its name

1. Press Ctrl+F (Find in the list).
2. Type: elt
3. Press Enter.
4. Press Ctrl+F (Find in the list).
5. Type: ta.w
6. Press Enter.
7. Press F3 2 times (Find next).
8. Press Ctrl+F (Find in the list).
9. Type: a.wav
10. Press Enter.
11. Press Shift+F3 2 times (Find previous).
12. Press F3 (Find next).

### Play another file of the queue

1. Press Home.
2. Press Down.
3. Press Enter.
    You hear: Playing beta.wav.
4. Press Down.
5. Press Up.

### Hear the information of a file

1. Press Ctrl+I.
    You hear: beta.wav, 2 of 4, playing, 0:0.
2. Press Down.
3. Press Ctrl+I.
    You hear: delta.wav, 3 of 4.

### Remove files and clear the queue

1. Press Delete.
    You hear: Removed delta.wav.
2. Press Up.
3. Press Delete.
    You hear: Removed beta.wav.
    You hear: Playing gamma.wav.
4. Choose Clear queue in the Session menu.
    You hear: Queue cleared.
5. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.

## Add files to the queue from another app

### Add a folder to the queue with no window open

1. Open axMedia with --add the folder ch.
2. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.
3. Press Ctrl+I.
    You hear: alpha.wav, 1 of 3.
4. Close the window.

### Play a file with its folder around it

1. Open axMedia with the file ch\beta.wav.
    You hear: Playing beta.wav.

### Play a file in the open window

1. Start axMedia again with the file ch\alpha.wav.
    You hear: Playing alpha.wav.

### Add a folder to the open window's queue

1. Start axMedia again with --add the folder ch.

### Open a second window on a file

1. Open axMedia with --new the file ch\gamma.wav.
    You hear: Playing gamma.wav.
2. Go to the other axMedia window.

## Play, pause and seek

### Play, pause and resume

1. Open axMedia with the folder p.
    You hear: Playing first.wav.
2. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Paused at 0:0.
3. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Playing.
4. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Paused at 0:0.
5. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Playing.

### Move forward, back and to a time

1. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Paused at 0:0.
2. Press Ctrl+Right (Forward 15 seconds; Actions menu).
    You hear: 0:1.
3. Press Ctrl+Left (Back 15 seconds; Actions menu).
    You hear: 0:0.
4. Press Ctrl+T (Go to time; Navigation menu).
5. Type: 0:30
6. Press Enter.
    You hear: 0:30.
7. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Playing.

### Change the playing speed

1. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.25.
2. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.5.
3. Choose Faster in the Actions menu (key: Ctrl+Up).
    You hear: Speed 1.75.
4. Press Ctrl+Down (Slower; Actions menu).
    You hear: Speed 1.5.
5. Choose Slower in the Actions menu (key: Ctrl+Down).
    You hear: Speed 1.25.
6. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.5.
7. Choose Normal speed in the Actions menu.
    You hear: Speed 1.
8. Choose Faster in the Actions menu (key: Ctrl+Up).
9. Choose Faster in the Actions menu (key: Ctrl+Up).
    You hear: Speed 1.5.

### Hear what is playing and go to the next or the previous file

1. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Playing first.wav, 0:3.
2. Press Ctrl+Q (What is playing; Actions menu).
    You hear: of 1:00, speed 1.5.
3. Press Ctrl+Shift+Right (Next file; Navigation menu).
    You hear: Playing second.wav.
4. Press Ctrl+Shift+Left (Previous file; Navigation menu).
    You hear: Playing first.wav.

### Stop saying each file as it starts

You hear: Playing s2.wav.
1. Choose Say each file as it starts in the Options menu.

## Hear why a file cannot be played

### Hear that a file cannot be played, and the next one plays

1. Open axMedia with the folder c.
    You hear words such as: Cannot play a-broken.mp3.
    You hear: Playing b-good.wav.

## Use axMedia's text, notices and axKit menu

Starts with: Open axMedia with the folder ch.

### Change the text size

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size.
2. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size.
3. Choose Windows text size in the Options menu, Text.
    You hear: Windows text size.

### Read the keyboard shortcuts and the user guide

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press Escape.
3. Choose User guide in the Help menu.
4. Press Escape.

## Use axMedia's Context menu

### Hear a file's information from the Context menu

1. Choose the menu item Information, Context.
    You hear: alpha.wav, 1 of 2, playing, 0:0.
2. Press Down.
3. Choose the menu item Information, Context.
    You hear: beta.wav, 2 of 2.

### Play the selected file from the Context menu

1. Choose the menu item Play, Context.
    You hear: Playing beta.wav.

### Remove a file found in the command palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: remove
    You hear: Remove, Context, Delete.
3. Press Enter.
    You hear: Removed beta.wav.

## Add a recent folder to the queue from the command palette

Starts with: Open axMedia with the folder chapters.

### Add a recent folder to the queue from the palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: chapt
    You hear words such as: 1. chapters.
    You hear: Session, Open recent.
3. Press Enter.
    You hear: Added 2.

## Select files in the queue and take several out

Starts with: Open axMedia with the folder ch.

### Pause and resume with Ctrl+Enter

1. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear words such as: Paused at.
2. Press Ctrl+Enter (Pause or resume; Actions menu).
    You hear: Playing.

### Select files with Space and with Shift and an arrow

1. Press Down.
2. Press Space.
    You hear: Selected.
3. Press Shift+Down.
4. Press Ctrl+I.
    You hear: 2 selected.

### Take the selected files out of the queue

1. Press Delete.
    You hear: Removed 2 files.

### Select every file, and give the selection up

1. Press Ctrl+A (Select all; Actions menu).
    You hear: 2 selected.
2. Press Escape.
    You hear: None selected.
3. Press Shift+Space.
    You hear: Selected.
4. Press Ctrl+Shift+A (Deselect all; Actions menu).
    You hear: None selected.
