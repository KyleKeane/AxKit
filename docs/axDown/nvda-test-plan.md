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
5a. New line said (AD-2.8, bundle AX-7.8; `tests/scripts/axDown/12-say-new-line.txt`). At the end of a line, type a
   word and press Enter: NVDA says the word, then "new line". Enter in the middle of a line: "new line". Options,
   Text, Say new line: "Say new line off"; Enter makes a line and nothing is said. Again: "Say new line on", and
   Enter says "new line" again. Arrow keys say nothing of it. In an item app's long text field (axTask, a list's
   Notes), Enter says "new line"; Tab, then Enter on Save keeps the text and says no "new line". In axTerminal,
   Shift+Enter in the command field says "new line" and Enter, which runs the command, does not.
5b. Heading sound (bundle AX-7.9; `tests/scripts/axDown/13-heading-sound.txt`). In a Markdown file, Down onto a
   heading's line: a short rising sound, then NVDA reads the heading; Down off it, and onto a plain line: no sound.
   Ctrl+Home onto a first-line heading and the heading jumps (Navigation, Next, Heading): the sound first. A `#` line in a
   fenced code block: no sound. A long heading wrapped over two rows sounds once. Options, Text, Sound on headings:
   "Sound on headings off", and Down onto a heading is silent; again: "Sound on headings on". In axPreview the same on
   a Markdown file, and in any notice's text (F1) h and Down onto a heading line. By ear: is the sound over before NVDA
   starts, and does it carry over speech at your rate?
6. Undo and redo (AD-2.5). Type a word, wait two seconds, type another: Ctrl+Z removes the second word only, Ctrl+Z
   again the first, a third Ctrl+Z says "Nothing to undo"; Ctrl+Y twice puts both back, a third says "Nothing to
   redo". Paste a line (Ctrl+V) and type after it: Ctrl+Z takes the typed text, then the pasted line. Hold Backspace
   over a few words: one Ctrl+Z restores them all, the caret after them. Type, Ctrl+S ("Saved"), Ctrl+Z: the typing
   is undone past the save and the title has its star again. Actions, Undo and Actions, Redo do the same as the keys. Alt+Backspace: "Undone" as Ctrl+Z (the undo of the window, not of the control); Shift+F10 in the text: the menu reads Undo, Redo, Cut, Copy, Paste, Select all.
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
10. Session, Open recent file lists `plain.txt` and the other files opened or saved in this window as "1. plain.txt
    (<path>)", newest first;
    choosing one opens it, after the unsaved-changes question when there are changes; before any file, "No recent files",
    which NVDA reads and which does nothing on Enter.
11. Session, Show current file in, File Explorer (Ctrl+W): a File Explorer window opens with the file selected and NVDA
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
   as in the editor, "No next table" when there is none; Help, About, Version names the version of axKit, the apps
   and the folders; About, Licence, Attribution and axKit guide open their notices, Escape closes each.
4. Alt+W: the Window menu with New window, Open windows, Recently closed, Close all windows, Say the status (F8: the status bar is said); Alt+K: the axKit menu with Claude, AI tools and Theme;
   Alt+H: Help with Command palette, Keyboard shortcuts, User guide, the resources, Send a report, Your reports, Diagnostics (Test a key, Copy diagnostics), Check
   for updates and About (bundle AX-9.1); Help, About, axKit on GitHub: the browser opens the repository page. With a second
   axDown open, Open windows reads "1. <name> - axDown, 1 of 1" and Enter brings that window to the front; Alt+W, N, D starts a new Untitled window. Alone, Open windows holds
   "No other open windows", which NVDA reads and which does nothing. Close a window that has `plain.txt` open: in another window Alt+W, R reads "1. plain.txt
   (<path>) - axDown" and Enter opens it in a new window; Session, Open on another file puts the one you left there too. Alt+W, C
   (Close all windows) with unsaved changes in one window: that window asks "Save changes?" and the others close.
5. Options, General, Check for updates at start: read as checked; Enter unchecks it. Help, Diagnostics, Copy diagnostics: "Copied diagnostics"; Options, Developer, Open settings file: a new axJSON
   window on `axDown.json`. Help, Markdown resources, Markdown basics: the browser opens commonmark.org; GitHub
   Flavored Markdown opens its page.
6. The command palette (bundle AX-10), with the caret in the middle of a line. Ctrl+Shift+P: "Command palette",
   the field "Search commands"; Down: "Undo, Actions, Ctrl+Z, 2 of …" or the like, each Down the next command with its
   menu and key, Up back; Ctrl+Home at the first match plays the low note and says the first match again. Type `wrap`: after a
   moment "Word wrap, …, 1 of 1"; Down: the low note, then "Word wrap, …, 1 of 1" again; Enter: the palette closes, NVDA is back in the editor at the same place, and
   "Word wrap, checked" (or "not checked") is said; Options shows the item changed. Ctrl+Shift+P, type `xyz`:
   "No commands match xyz"; Enter says it again and the palette stays; Escape: back in the editor, nothing run.
   Ctrl+Shift+P, `save as`, Enter: the Save as picker opens. Alt+H, P (Help, Command palette, the menu's first item) opens it too. Recent files and the
   Window menu's lists are not in it; Alt+W, New window, axDown is ("axDown, Window, New window").
7. The key test (bundle AX-8.12; script `15-key-test`), with the caret in the middle of a line. Alt+H, D, T (Help,
   Diagnostics, Test a key): "Key test. Press any key to hear its name and what it does here. Press Escape to end."
   F8: "F8: Say the status" and no status; F4: "F4: Send a report" and no form; Ctrl+G: "Ctrl+G: Go to line";
   Ctrl+H: "Ctrl+H: Next, Heading"; a letter: "A: types the letter" and the text unchanged; Down: "Down: moves in the
   text" and the caret where it was; Enter: "Enter: makes a new line" and no line; F9: "F9: no command of this
   window"; Alt alone: "Alt alone: opens the menu bar", the bar closed; Ctrl alone: "Ctrl alone: no command of this
   window"; Alt+F4: "Alt+F4: Exit", the window still there. Escape: "Key test ended" at once, Escape itself not
   named. The title has no star, the caret is where it was, and the same keys act
   again. Started again and left with Alt+Tab: "Key test ended" as the window is left. A key that is never said did
   not reach axKit: NVDA+1, NVDA's input help, says whether NVDA saw it.

## 7. Updates and uninstall

1. Help, "Check for updates (axKit <version>)..." names the version you have and reports in a notice either way; Help, About, Version names it too
   and "Latest release on GitHub: axKit <version>" with the releases page.
2. With an older build installed (set `<Version>` in `src/AxKit/AxKit.csproj` below the latest release, `.\publish.ps1`,
   set it back), open a file: the notice "Update to axKit <newest>" opens by itself with four buttons. Escape (Later):
   the Help menu has "Update to axKit <newest>..." below Check for updates, and Check for updates asks GitHub again and opens
   the notice once more. Enter (I accept all terms, install and restart): the notice "Updating to axKit <newest>" with a progress bar that
   NVDA reports by its progress bar settings; "Downloaded. Installing" at about 80 percent, then "Installed.
   Restarting"; axDown closes and comes back on the same file (or empty, as it was), Help, About, Version naming the newest
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
   last one: "No next heading level 2". Navigation, Next and Navigation, Previous each list Heading and Heading level 1 to
   6 with their keys, and the items do the same as the keys.
5. Ctrl+K: the caret on the next link's first character, "GitHub, link"; Ctrl+Shift+K from inside a link: the link
   before it; a bare https address and an `<https://…>` address count, an image does not.
6. Ctrl+L: "List with 3 items, one" at the first item; Ctrl+I: the next item's text, nested items included; a numbered
   list after a bulleted one is a second list.
7. Ctrl+Q: "Block quote, " and the first line; Ctrl+T: "Table with 3 rows and 2 columns" at the header line; nothing
   inside a fenced code block counts. Navigation, Next and Navigation, Previous list Link, List, List item, Block quote
   and Table with their keys.
8. Navigation mode (bundle AX-8.13, AD-5.3): Ctrl+Space: "Navigation mode", and NVDA+End (or F8) reads ", navigation mode"
   after the file's name. h, Shift+H, 2, k, l, i: the same jumps and words as Ctrl with the letter, each after a
   high glassy tick that is over before NVDA speaks (on a heading the heading sound follows it). h past the last heading:
   a low buzz and no tick, then "No next heading". A letter or a digit that is no jump, Space or a sign: a
   wooden bonk, the text and the title unchanged. Enter, Backspace, Delete, Ctrl+V and Ctrl+Z: the edge sound, the
   text and the title unchanged. By ear: the three sounds are told apart from each other and from the edge sound,
   and none of them is heard in edit mode.
   Actions, Undo: the edge sound and "Navigation mode: press Ctrl+Space to edit". Arrows and Shift+arrows move and select as ever.
   Ctrl+Space: "Edit mode"; a letter types. In Help, User guide (a read-only text), Ctrl+Space: "Read only: there is
   no edit mode here".

## 9. AI help: a question about this window (docs/axKit/ai-help.md AH-1, AH-6; 2026-11-09)

The automated part is `tests/scripts/axDown/11-ask-claude.txt`, against a fake Claude; the steps marked "(real)" need
Claude Code installed and logged in, and are the user's by ear.

1. With Claude off (axKit menu, Claude, Use Claude in axKit unchecked) and no agreement recorded (delete `consent.json`
   first, or read the step with the agreement remembered: Shift+F1 says "Claude on" and opens the question field):
   Shift+F1 opens the reminder "Before Claude helps in axKit" at once, read with its check box; Escape: "Claude stays
   off", the editor has the focus.
2. Shift+F1 again: the reminder; Tab to the box, Space: "checked"; Enter: "Claude on", then the notice "Ask Claude
   about this window" with its field.
3. Shift+F1: the notice "Ask Claude about this window" is read, then "Your question about this window, edit". Type
   "how do I turn word wrap off" and Enter: "Asking Claude" as a notice; then "Claude says" and the answer, the focus
   on its text so that NVDA reads it from the top; Down reads it line by line (real: the answer names Ctrl+Shift+W).
4. Tab: "Ask more about this answer, edit"; type "and back on?", Enter: "Asking Claude", then a new "Claude says" in
   the same conversation (real). Tab twice: the Copy button; Enter: "Copied"; the notice stays. Escape: the editor has
   the focus again, on the same line as before.
5. Shift+F1, then Escape: nothing asked, the editor has the focus. Ctrl+Slash: the same notice as Shift+F1 opens, and no character is typed into the text (bundle AX-8.15); Escape.
6. Alt+A, C, F: "Folders Claude may read", "No folders yet…", the text naming Claude's working folder; Tab to Add a
   folder, Enter: the folder picker; Escape it: the notice comes back; Escape closes it. Alt+A, C, W: axFile opens on
   `Documents\axKit Claude`; Alt+F4.
7. Alt+A, C, U: "Claude off"; Alt+A, C, U: "Claude on" (the agreement is remembered); Alt+A, C, U: "Claude off".

## 10. Format (SPEC.md AD-12, 2026-11-22)

On a copy of `manual_tests/axDown/_main.md`, or any Markdown file.

1. On a plain line, Shift+Alt+Right: "Heading 1", and the line starts with a number sign and a space; again:
   "Heading 2". Shift+Alt+Left twice: "Heading 1", then "No heading" and the line is plain again; once more: the
   edge sound and "No heading".
2. Alt+A, F: the Format menu reads Heading 1 to Heading 6, Heading deeper, Heading shallower, Bold, Italic, Code,
   List item, Block quote. Heading 3: "Heading 3"; the same again: "No heading".
3. With the caret in a word, Ctrl+B: "Bold on", two asterisks on each side of the word, the caret still in the
   word; Ctrl+B: "Bold off". Select three words, Ctrl+B: "Bold on", around all three. In an empty place, Ctrl+B,
   type a word: it stands between the asterisks.
4. Alt+A, F, I on a bold word: "Italic on", three asterisks; Ctrl+B there: "Bold off", one asterisk stays.
   Alt+A, F, C on a word: "Code on", a backtick on each side.
5. Select three lines, Alt+A, F, L: "List item on, 3 lines", each starts with a dash; again: "List item off, 3
   lines". Alt+A, F, Q on one line: "Block quote on"; again: "Block quote off".
6. Ctrl+Z after any of them: "Undone", the whole change back in one step.
7. Ctrl+Space, then Ctrl+B: the edge sound and "Navigation mode: press Ctrl+Space to edit"; nothing changed.
8. By ear, the maintainer's: whether "on" and "off" are the right words, and whether the three keys sit well.

## 11. Templates (SPEC.md AD-13, 2026-11-23)

1. Select a word, Alt+A, T, L: "Link inserted"; the word stands in square brackets, and what you type next goes
   between the round brackets. With nothing selected: "Link inserted", empty square brackets.
2. Alt+A, T, T: "Table inserted", two columns and two rows; what you type goes into the first cell.
3. Select two lines, Alt+A, T, C: "Code block inserted", the lines inside the fence.
4. Options, General, Templates folder: axFile opens on the folder. Put a file `Meeting.md` there with the text
   `# Meeting {caret}` and a second line `Present: {selection}`. Back in axDown, select a name, Alt+A, T: the menu
   ends with "1. Meeting"; Enter: "Meeting inserted", the caret after "# Meeting ", the name after "Present: ".
5. Ctrl+Shift+P, `insert template`: the first match "Link, Actions, Insert template, 1 of 4"; Down reads the others.
   Ctrl+Shift+P, `meeting`, Enter: "Meeting inserted".
6. Ctrl+Z after any of them: "Undone", the whole template gone in one step.
7. Ctrl+Space, then Alt+A, T, L: the edge sound and "Navigation mode: press Ctrl+Space to edit".
8. With no file in the folder, the menu's last item reads "No templates of your own yet" and does nothing.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. In the text with a word selected, Shift+F10: Context opens on "Cut Ctrl+X" and NVDA reads it, not the edit control's own menu; the Applications key and a right click the same; Escape: back in the text, the caret and the selection where they were. Alt, then Right along the bar: Session, Actions, Navigation, Context, Options, Window, axKit, Help.
