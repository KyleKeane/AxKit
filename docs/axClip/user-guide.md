# axClip user guide

axClip keeps a history of the text you copy, made for screen reader users. It runs in the background; one key from anywhere, Ctrl+Shift+Apostrophe unless you choose another, brings up a search field over everything you copied, the newest first. Enter pastes the item into the program you were in.

## Install

axClip is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry `axKit axClip` and a console command `axClip`, and starts axClip in the background; it then starts with Windows, unless you turn that off in its Options, General.

## Using it

1. Copy text anywhere, as you always do (Ctrl+C). axClip keeps it quietly; nothing is said.
2. Press Ctrl+Shift+Apostrophe. axClip comes up with the caret in its search field and the newest item selected.
3. Down and Up move through the items; each is read with its place, "git status, 2 of 12". Ctrl+Home and Ctrl+End go to the first and the last, Page Down and Page Up a screen at a time.
4. Type part of what you want: only the items with every word you typed stay, and after a short pause the first is read.
5. **Enter** pastes the item into the program you were in. axClip hides, that program comes back to the front, and the text is pasted.

Other keys while axClip shows:

- **Shift+Enter** puts the item on the clipboard without pasting, for a program whose paste is another key.
- **Ctrl+Q** shows the item's whole text in a notice, to read with the arrow keys. Escape closes it.
- **Delete** removes the item; **Ctrl+Shift+Delete** clears the whole history, after a question.
- **Ctrl+P** pauses recording, for when you are about to copy something private; press it again to record again.
- **Escape**, or the hotkey again, hides axClip.

An item reads as its first line, cut at 120 characters, and says how many lines it has when there are more.

## Privacy

The history is kept in memory only: it is gone when axClip ends, unless you turn on Options, Keep history between runs, which keeps it in axClip's settings file, encrypted so that only your Windows account can read it back. Password managers that mark a copy as private (most do) are respected: such a copy is never kept. axClip keeps only text, never pictures or files, and its log never holds what you copied.

A kept history can be read only by the Windows account that wrote it. After a move to another computer, or a password reset by an administrator, axClip says once, the first time you bring it up: "The kept clipboard history could not be read on this computer and starts empty." Nothing else changes: Keep history between runs stays on and the new history is kept from then on. The settings file as it was is kept once beside itself, with .bad at the end of its name, the old history in it still encrypted; Actions, Clear history deletes that copy too. The kept history holds 8 MB of text at most: beyond that the oldest items are left out of the file.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Hide (Escape); Quit axClip.
- **Actions**: Clear history (Ctrl+Shift+Delete).
- **Navigation**: Nothing to move to here yet.
- **Context**: Copy without pasting (Shift+Enter); Paste (Enter); Read in full (Ctrl+Q); Remove (Delete).
- **Options**: Hotkey; Keep history between runs; Pause recording (Ctrl+P); Developer (Open settings file); General (Say the first match while typing, Start with Windows); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size).
- Keys without a menu item: Ctrl+Home (First match); Ctrl+End (Last match); Down (Next match); Page Down (Page down); Page Up (Page up); Up (Previous match).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## The hotkey

Options, Hotkey: press the new key with Ctrl, Alt or the Windows key, then Enter. When another program already holds the key, axClip says so and keeps the one before.

Up to version 5.8.0 the key was Windows+Alt+V. If you never changed it, axClip moves you to Ctrl+Shift+Apostrophe by itself; a key you chose stays yours.

## Settings

axClip keeps the font, the text size, the update check, the hotkey, Start with Windows, Say the first match while typing and Keep history between runs in `%APPDATA%\AxKit\axClip.json`, and the history there too while you keep it. Options, Developer, Open settings file opens it in axJSON.

## Updates

axClip checks for a newer axKit when it starts and offers it in a notice; the Help menu has Check for updates at any time. One update brings every app of axKit up to date; axClip is started again after it, and its history starts empty unless you keep it.

## If something goes wrong

If the hotkey does nothing, start axClip again from the Start menu. When the running axClip has stopped answering, the new start asks "axClip is running but does not answer. End it and start again?". No is the default and changes nothing; Yes ends that axClip alone and starts it again, and a history you do not keep on disk starts empty.

axClip's log is `%LOCALAPPDATA%\AxKit\logs\axClip.log`. If Enter does not paste, the program you were in did not come back to the front in time, or it runs as administrator, where Windows does not let axClip press keys: the text is on the clipboard, so press Ctrl+V there yourself.

## About axClip

axClip is one app of axKit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Help menu's About names the version and the licence.

## Disclaimer

A clipboard history holds whatever you copy. Pause recording before you copy something private that no password manager marks, and leave Keep history between runs off if others use this computer.
