# axDown NVDA test plan

The files for this plan are in `manual_tests/axDown`, made and reset by `.\manual_tests\prepare.ps1 axdown`; its `_main.md` says which folder or file each section uses.

Manual checks with NVDA running, against `docs/axDown/SPEC.md`. Run the sections a change touches before a release;
run all of them before a release that changes the window. Prepare a folder with `plain.txt` (UTF-8, no
mark, CRLF), `bom.md` (UTF-8 with a mark, LF), `wide.txt` (UTF-16 LE), `ansi.txt` (Windows-1252 with an accented
letter), and a `long.md` with headings and lines longer than the window.

## 1. Install and start

1. `.\publish.ps1` installs axKit. Press the Windows key, type `axd`: "axKit axDown" is offered; Enter opens an
   empty window. NVDA reads the title "Untitled - axDown" and then "Untitled edit multi line".
2. Right-click `plain.txt` in File Explorer: the "Open with axKit" submenu lists axDown, and not axJSON; Enter on
   axDown opens the file. "Open with"
   lists axDown for `.md`, `.markdown` and `.txt`.
3. In a console: `axdown plain.txt` opens it; `axkit plain.txt` opens it in axDown, `axkit <folder>` opens axClaude;
   `axdown missing.md` opens an empty document named `missing.md`.
4. Two files from Explorer give two windows; Alt+Tab names both.

## 2. Reading and writing

1. In `long.md`, Down Arrow reads one line at a time; Ctrl+Right reads words; NVDA's say-all reads on. Nothing is
   spoken by axDown on these keys.
2. Type a sentence; Backspace and Delete behave as in Notepad; Enter makes a line; Tab types a tab (the focus does
   not leave the editor). Ctrl+Z: "Undone", the whole sentence is gone in one step; Ctrl+Y: "Redone", it is back.
3. Page Down moves a screen and reaches the last line; Page Up the first. In a short file both do something.
3a. In the tutorial file, Ctrl+Down moves to the first line of the next paragraph (a line with text after a blank line), one paragraph per press however many rows it wraps to, a heading with blank lines around it being a paragraph of its own; Ctrl+Up goes to the start of the current paragraph, then to the one before; from the last paragraph Ctrl+Down goes to the end of the file. Ctrl+Shift+Down: NVDA says the text selected; Ctrl+Shift+Up: unselected. The status bar's line number follows.
4. Ctrl+Shift+W: "Word wrap off"; a long line is now one line; the caret stays on the same character. Again:
   "Word wrap on"; the caret stays.
5. Ctrl+M, Ctrl+H, Ctrl+I, Ctrl+J insert nothing.
6. Undo and redo (AD-2.5). Type a word, wait two seconds, type another: Ctrl+Z removes the second word only, Ctrl+Z
   again the first, a third Ctrl+Z says "Nothing to undo"; Ctrl+Y twice puts both back, a third says "Nothing to
   redo". Paste a line (Ctrl+V) and type after it: Ctrl+Z takes the typed text, then the pasted line. Hold Backspace
   over a few words: one Ctrl+Z restores them all, the caret after them. Type, Ctrl+S ("Saved"), Ctrl+Z: the typing
   is undone past the save and the title has its star again. Edit, Undo and Edit, Redo do the same as the keys. Alt+Backspace: "Undone" as Ctrl+Z (the undo of the window, not of the control); Shift+F10 in the text: the menu reads Undo, Redo, Cut, Copy, Paste, Select all.
   Open another file: Ctrl+Z says "Nothing to undo". Nothing but the announcement is spoken on any of these, and
   NVDA's arrow keys read the text as restored.

## 3. Files

1. Open `bom.md`, change a letter, Ctrl+S: "Saved". A hex view shows the mark kept and LF endings only.
2. Open `wide.txt`, edit, save: still UTF-16 LE with its mark. `ansi.txt`: the accented letter survives a save.
3. Open `plain.txt`, Ctrl+S without a change: "Saved", the file unchanged byte for byte.
4a. Ctrl+Shift+N: a second axDown window opens on Untitled, empty, and takes the focus; this window is unchanged. Close the new one.
4. Ctrl+N with unsaved changes: the notice "Save changes to plain.txt?" with Save, Don't save, Cancel; Escape
   cancels and the text is still there; Enter saves and the document is empty, "Untitled".
5. Ctrl+S on Untitled opens the Windows Save dialog; cancelling returns to the editor with the focus where it was.
6. Ctrl+Shift+S saves under a new name; the title, the editor's name and the status bar show the new name.
7. With `plain.txt` open and unchanged, go to another window and append a line to the file there (in axClaude: "add
   a line to plain.txt"; or in a console: `echo more>> plain.txt`), then Alt+Tab back: "Reloaded, plain.txt changed
   on disk", the caret on the line it was on, and Ctrl+End reads the new line. Now type a character, go away, change
   the file again, come back: "plain.txt changed on disk; Save will ask", your character still there; Alt+Tab away
   and back again: nothing is said twice. Ctrl+S: the notice "The file changed on disk" with Write over, Reload and
   lose mine, Cancel; Escape keeps both; Ctrl+S, Down to "Reload and lose mine", Enter: "Reloaded", your character
   gone and the file's new line there; make a change, Ctrl+S, Enter on Write over: "Saved".
8. Close with Alt+F4 with unsaved changes: the notice; Don't save closes; Cancel stays.
9. Open a locked file, or one that is not text (a `.png`): a notice names the problem; Escape closes it; the window
   stays open on an empty "Untitled" document, and Ctrl+S opens the Save dialog instead of writing over the file. A
   missing file opens as an empty document of that name, which Ctrl+S creates.
10. File, Open recent file lists `plain.txt` and the other files opened or saved in this window as "1. plain.txt
    (<path>)", newest first;
    choosing one opens it, after the unsaved-changes question when there are changes; before any file, "No recent files",
    which NVDA reads and which does nothing on Enter.
11. File, Show current file in, File Explorer (Ctrl+W): a File Explorer window opens with the file selected and NVDA
    reads its title; Alt+F4 closes it and the focus is back in axDown. On Untitled: "No file yet".

## 4. Find and go to line

1. Ctrl+F: the notice with "Text to find"; type a word, Enter: the caret lands on the match, NVDA reads the line
   with the selection; F3 the next; Shift+F3 back; a word that is not there: "Not found".
2. Ctrl+G: the field "Go to line (now line n, column m)" is read with the correct numbers; type a number, Enter:
   "Line n of m" and the caret is at the start of that line; a number past the end lands on the last line; a word
   instead of a number: "Not a line number" and the caret has not moved; Escape returns to the editor where it was.
3. NVDA+End reads the status bar: the name, "Line n, Column m", the encoding and the line ending.

## 5. Text size and font

1. Ctrl+Plus: "Text size n"; the text is larger; Ctrl+Minus back. Close and reopen: the size is kept.
2. Options, Text, Font opens the Windows font dialog; a chosen font is used and kept.

## 6. Menus and help

1. Alt opens the menu; every item is read with its shortcut; Escape closes without changing the text.
2. F1 opens the shortcuts notice, its first line naming the text's own keys; Page Down and Page Up move a screen of
   its text, Ctrl+Down and Ctrl+Up a paragraph; h and Ctrl+H alike: "In every window of axKit, heading level 2" (the first section),
   Shift+H back; Ctrl+F, type `wrap`, Enter: the shortcuts text is back with "wrap" selected and NVDA reads the
   line, F3: the next line with it, Ctrl+F then Escape: the text is back where it was; Escape closes the notice and
   the focus is back in the editor at the same place.
3. Help, User guide shows this app's guide, and Ctrl+H, Ctrl+K, Ctrl+L and the other navigation keys move through it, spoken
   as in the editor, "No next table" when there is none; axKit, About, Version names the version of axKit, the nine apps
   and the folders; About, Licence, Attribution and axKit guide open their notices, Escape closes each.
4. Alt+A: the axKit menu with New window, Recently closed, Open windows, Open windows by type, Close all windows, Check
   for updates and About (bundle AX-9); About, axKit on GitHub: the browser opens the repository page. With a second
   axDown open, Open windows reads "1. <name> - axDown, 1 of 1" and Enter brings that window to the front; Alt+A, B, D there
   lists both windows, the current one checked; Alt+A, N, D starts a new Untitled window. Alone, Open windows holds
   "No other open windows", which NVDA reads and which does nothing. Close a window that has `plain.txt` open: in another window Alt+A, R reads "1. plain.txt
   (<path>) - axDown" and Enter opens it in a new window; File, Open on another file puts the one you left there too. Alt+A, C
   (Close all windows) with unsaved changes in one window: that window asks "Save changes?" and the others close.
5. Options, General, Check for updates at start: read as checked; Enter unchecks it. Options, Developer, Copy diagnostics: "Copied diagnostics"; Options, Developer, Open settings file: a new axJSON
   window on `axDown.json`. Help, Markdown resources, Markdown basics: the browser opens commonmark.org; GitHub
   Flavored Markdown opens its page.
6. The command palette (bundle AX-10), with the caret in the middle of a line. Ctrl+Shift+P: "Command palette",
   the field "Search commands"; Down: "Undo, Edit, Ctrl+Z, 2 of …" or the like, each Down the next command with its
   menu and key, Up back; Ctrl+Home at the first match plays the low note and says the first match again. Type `wrap`: after a
   moment "Word wrap, …, 1 of 1"; Down: the low note, then "Word wrap, …, 1 of 1" again; Enter: the palette closes, NVDA is back in the editor at the same place, and
   "Word wrap, checked" (or "not checked") is said; Options shows the item changed. Ctrl+Shift+P, type `xyz`:
   "No commands match xyz"; Enter says it again and the palette stays; Escape: back in the editor, nothing run.
   Ctrl+Shift+P, `save as`, Enter: the Save as picker opens. Alt+A, P (axKit, Command palette, the menu's first item) opens it too. Recent files and the
   axKit menu's window lists are not in it; Alt+A, New window, axDown is ("axDown, axKit, New window").

## 7. Updates and uninstall

1. axKit, "Check for updates (axKit <version>)..." names the version you have and reports in a notice either way; axKit, About, Version names it too
   and "Latest release on GitHub: axKit <version>" with the releases page.
2. With an older build installed (set `<Version>` in `src/AxKit/AxKit.csproj` below the latest release, `.\publish.ps1`,
   set it back), open a file: the notice "Update to axKit <newest>" opens by itself with four buttons. Escape (Later):
   the axKit menu has "Update to axKit <newest>..." below Check for updates, and Check for updates asks GitHub again and opens
   the notice once more. Enter (I accept all terms, install and restart): the notice "Updating to axKit <newest>" with a progress bar that
   NVDA reports by its progress bar settings; "Downloaded. Installing" at about 80 percent, then "Installed.
   Restarting"; axDown closes and comes back on the same file (or empty, as it was), axKit, About, Version naming the newest
   version. Again with the older build: Escape during the download says "Update cancelled" and keeps the version;
   I accept all terms, install and close says "Installed. Closing" and axDown does not come back; the Start menu entry is the newest
   version. With unsaved changes, I accept all terms, install and restart: the "Save changes?" question; Escape (Cancel): "Update
   installed. It starts with the next axDown", the window stays; a later Exit with Don't save closes without
   starting axDown again.
3. Settings, Apps, axKit, Uninstall removes axDown's Start entry, its file menu entries, Open with, and `axDown.cmd`
   together with everything else of axKit; `%APPDATA%\AxKit\axDown.json` stays.

## 8. Navigation (from the version with section 9 of the spec)

1. In `long.md`, Ctrl+H: the caret moves to the next heading and its line is spoken; Ctrl+Shift+H back; at the last
   heading: "No next heading", at the first: "No previous heading".
2. A heading-looking line inside a fenced code block is skipped.
3. Plain arrow keys still speak nothing of axDown's own.
4. Ctrl+2: the next level-2 heading, other levels skipped, spoken "…, heading level 2"; Ctrl+Shift+2 back; past the
   last one: "No next heading level 2". Navigate, Next and Navigate, Previous each list Heading and Heading level 1 to
   6 with their keys, and the items do the same as the keys.
5. Ctrl+K: the caret on the next link's first character, "GitHub, link"; Ctrl+Shift+K from inside a link: the link
   before it; a bare https address and an `<https://…>` address count, an image does not.
6. Ctrl+L: "List with 3 items, one" at the first item; Ctrl+I: the next item's text, nested items included; a numbered
   list after a bulleted one is a second list.
7. Ctrl+Q: "Block quote, " and the first line; Ctrl+T: "Table with 3 rows and 2 columns" at the header line; nothing
   inside a fenced code block counts. Navigate, Next and Navigate, Previous list Link, List, List item, Block quote
   and Table with their keys.

## 9. AI help: a question about this window (docs/axKit/ai-help.md AH-1, AH-6; 2026-11-09)

The automated part is `tests/scripts/axDown/11-ask-claude.txt`, against a fake Claude; the steps marked "(real)" need
Claude Code installed and logged in, and are the user's by ear.

1. With AI help off (axKit menu, AI help, Let Claude help in the apps unchecked): Shift+F1 says "AI help is off: axKit
   menu, AI help, Let Claude help in the apps" and opens nothing.
2. Alt+A, H, L: the first time, the consent notice "Before Claude helps in the apps" is read, with the check box; Space
   on the box says "checked"; Enter: "AI help on: Shift+F1 asks Claude about this window; axHelp and axDescribe answer
   from anywhere".
3. Shift+F1: the notice "Ask Claude about this window" is read, then "Your question about this window, edit". Type
   "how do I turn word wrap off" and Enter: "Asking Claude" as a notice; then "Claude says" and the answer, the focus
   on its text so that NVDA reads it from the top; Down reads it line by line (real: the answer names Ctrl+Shift+W).
4. Tab: "Ask more about this answer, edit"; type "and back on?", Enter: "Asking Claude", then a new "Claude says" in
   the same conversation (real). Tab twice: the Copy button; Enter: "Copied"; the notice stays. Escape: the editor has
   the focus again, on the same line as before.
5. Shift+F1, then Escape: nothing asked, the editor has the focus.
6. Alt+A, H, F: "Folders Claude may read", "No folders yet…"; Tab to Add a folder, Enter: the folder picker; Escape it:
   the notice comes back; Escape closes it.
7. Alt+A, H, L: "AI help off".
