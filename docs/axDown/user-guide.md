# axDown user guide

axDown is a plain editor for Markdown and text files, made for NVDA and for anyone who reads with a screen reader. It is one app of axKit, one program that holds several apps for screen reader users; the others have their own guides. axDown opens one file in one window: a menu bar, the text, and a status bar. Nothing is spoken unless you ask for it or a command finishes.

## Install

axDown comes with axKit. The README in the axKit zip, or on the releases page (https://github.com/KyleKeane/AxKit/releases), explains the installation: download `install.cmd` and double-click it. This puts axKit in your Programs folder and adds a Start menu entry, "axKit axDown" (press the Windows key and type `axd`), axDown in the "Open with axKit" submenu of the right-click menu of files, axDown under "Open with" for `.md`, `.markdown` and `.txt` files, and the `axDown` command for consoles. To make axDown the program that opens `.md` files when you press Enter on one, use Settings, Apps, Default apps, and choose axDown for `.md`; Windows does not let a program set that for itself.

## Start

- Start menu: axKit axDown opens an empty document called Untitled.
- File Explorer: right-click a file, open "Open with axKit" and choose axDown; or choose axDown under Windows' own "Open with".
- Console: `axdown notes.md`, or `axkit notes.md`, since a file means axDown. `axdown new.md` opens an empty document with that name, and the file is created when you save.

Each file opens in its own window; Alt+Tab moves between them.

## Write and read

The text is an ordinary Windows edit field, so NVDA's own keys read it: arrows for characters and lines, Ctrl+Left and Ctrl+Right for words, NVDA's say-all to read on from the caret, and so on. Type to write; Enter starts a new line, and axDown says "new line", which NVDA would not (Options, Text, Say new line turns that off, in every axKit window at once); Tab types a tab. Ctrl+Z undoes the last change and Ctrl+Y does it again, as far back as the file was opened in this window: a run of typing is one step, a deletion the same, a paste or a cut one step each. axDown says "Undone" or "Redone", and "Nothing to undo" or "Nothing to redo" at the ends. Saving keeps the history; opening another file or a new document ends it. Page Up and Page Down move a screen at a time and reach the first and last line. Ctrl+Up and Ctrl+Down move to the previous and next paragraph, the first line after a blank line, and with Shift they select to there, as in Word.

Long lines wrap at the window's edge, so Down Arrow reads one screen line at a time. Options, Text, Word wrap (Ctrl+Shift+W) turns that off: each line of the file is then one line in the window, however long, and axDown says "Word wrap off". Your place in the text is kept either way. The setting is one for every editor of axKit: axJSON and axPreview follow it the next time their window is active, and it is kept in the theme file, `%APPDATA%\AxKit\theme.json`, as `display.wordWrap`.

## Files

Ctrl+S saves and axDown says "Saved". A new document asks for a name first, with the Windows file dialog; Ctrl+Shift+S, Save as, asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first if you have unsaved changes; Ctrl+Shift+N starts an empty document in a new axDown window and leaves this one as it is, as does closing the window: a notice says "Save changes to notes.md?" with Save, Don't save and Cancel; Enter saves, Escape cancels. Session, Open recent file lists the last ten files you opened or saved; Session, Show current file in, File Explorer (Ctrl+W) opens File Explorer with the file selected.

When another program changes the file while it is open here, for example Claude editing it while you are in axClaude, axDown notices when you come back to its window: with nothing unsaved it reloads the file, keeps your line, and says "Reloaded, notes.md changed on disk"; with unsaved changes it says "notes.md changed on disk; Save will ask", and Ctrl+S then offers Write over, Reload and lose mine, or Cancel.

axDown writes a file back the way it found it: the same encoding, with or without a byte order mark, and the same kind of line ending. The status bar shows what it found, for example "UTF-8 · CRLF". A new document is UTF-8 without a mark. If the file changed on disk while you had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*notes.md - axDown".

## Find and go to a line

Ctrl+F opens Find in the window: type the text, press Enter, and the caret lands on the next match after it, which NVDA reads; "Not found" is spoken when there is none. F3 finds the next match again and Shift+F3 the previous one, without opening Find.

Ctrl+G asks for a line number; the field is named with where you are, for example "Go to line (now line 12, column 4)", so Ctrl+G is also how you ask where you are. Type a number and press Enter, or press Escape to stay. The status bar carries the line, the column and the encoding too; NVDA+End reads it.

## Move by structure

In a Markdown file, the keys NVDA uses on a web page work with Ctrl: Ctrl+H moves to the next heading (a line that starts with one or more `#`) and Ctrl+Shift+H to the previous one; Ctrl+1 to Ctrl+6 to the next heading of that level only; Ctrl+K to the next link, Ctrl+L to the next list, Ctrl+I to the next list item, Ctrl+Q to the next block quote and Ctrl+T to the next table, each with Shift added for the previous one. The caret lands at the start of what was found and axDown says what it is, as NVDA would: "Install, heading level 2", "GitHub, link", "List with 3 items, one", "Block quote, …", "Table with 3 rows and 2 columns". When there is none, axDown says so ("No next link") and the caret stays. Lines inside fenced code blocks are skipped. The Navigation menu lists all of them under Next and Previous. They work in the user guide and the other help texts too.

To move by structure without holding Ctrl, press Ctrl+Space: axDown says "Navigation mode", and the status bar adds "navigation mode" after the file's name. Now the same letters work alone, as in NVDA's browse mode and in axClaude's conversation: h and Shift+H for headings, 1 to 6 for a level, k for links, l for lists, i for list items, q for block quotes, t for tables. The arrows, Page Up, Page Down and the paragraph keys still move, and with Shift select; Ctrl+C copies and the Ctrl keys still jump. Each letter has its sound, heard before anything is read: a small click when it moves you, one flat low tone when there is nothing of that kind to go to, and a wooden bonk for a letter, a digit or a sign that has no command. Nothing can change the file in this mode: Enter, Backspace, Delete and a paste play the low edge sound, and Undo, Cut and Paste from the menus play the same sound and say "Navigation mode: press Ctrl+Space to edit". Press Ctrl+Space again to hear "Edit mode" and type as before. Every window starts in edit mode. axJSON works the same way with its own letters (k a key, j an object, l an array, i an item, e an error).

## Format

Actions, Format (Alt+A, F) writes Markdown's marks for you. Every command there acts where you are, and every one is a toggle: chosen again on the same text, it takes the format away. Each change is one step for Ctrl+Z, and axDown says in a word or two what it did.

- **Headings.** Shift+Alt+Right makes the line you are on a heading, or a heading one level deeper: it adds a number sign, and axDown says "Heading 1", "Heading 2". Shift+Alt+Left takes one away, down to a plain line, "No heading". The menu's Heading 1 to Heading 6 set a level at once; on a heading of that very level they take the number signs away.
- **Bold, Italic, Code.** Ctrl+B puts two asterisks on each side of the selection, or, with nothing selected, of the word the caret is in: "Bold on". Again: "Bold off". With the caret in no word it writes the empty marks with the caret between them, to type into. Italic (one asterisk) and Code (a backtick) work the same way, from the menu.
- **List item, Block quote.** On the line you are on, or on every line of a selection: a dash and a space, or a greater-than sign and a space, before each line; again takes them away. For several lines axDown says how many, "List item on, 3 lines".

In navigation mode nothing changes the text, these commands included.

## Templates

Actions, Insert template (Alt+A, T) puts a ready-made piece of text in at the caret. axDown has three of its own, and every file you keep in your templates folder is one more.

- **Link.** Select the words that should be the link's text, then Insert template, Link: axDown writes the square brackets around them and the round brackets after, and the caret waits between the round brackets for the address. With nothing selected the square brackets are empty.
- **Table.** A table of two columns and two rows, with the caret in the first cell.
- **Code block.** A fenced block; what you had selected is inside it.
- **Your own.** Your templates are written in axTemplate (Options, General, Templates in axTemplate opens it). A template shows here when its check for axDown, or for every axKit app, is on; it is listed by its name after axDown's own.

In a template of your own, three marks have a meaning, and everything else is put in as you wrote it: `{caret}` is where the caret lands (after the text when you leave it out), `{selection}` is the text you had selected when you chose the template, and `{clipboard}` is the text on the clipboard at that moment.

Every template is a command, so the command palette finds it: Ctrl+Shift+P, type "insert template" to hear them all, or the template's name. axDown says "Link inserted", and one Ctrl+Z takes a template back. In navigation mode nothing is put in.

## Text size and fonts

Ctrl+Plus and Ctrl+Minus make the text larger and smaller, and axDown says the new size. Options, Text, Font opens the Windows font dialog. Both are remembered.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); New (Ctrl+N); New window (Ctrl+Shift+N); Open (Ctrl+O); Save (Ctrl+S); Save as (Ctrl+Shift+S); Show current file in (axClaude, access layer for Claude Code, axFile, axGIT, access layer for Git, axTerminal, access layer for your shell, File Explorer (Ctrl+W)).
- **Actions**: Redo (Ctrl+Y); Undo (Ctrl+Z); Format (Block quote, Bold (Ctrl+B), Code, Heading 1, Heading 2, Heading 3, Heading 4, Heading 5, Heading 6, Heading deeper (Shift+Alt+Right), Heading shallower (Shift+Alt+Left), Italic, List item); Insert template (Code block, Link, Table).
- **Navigation**: Find (Ctrl+F); Find first; Find next (F3); Find previous (Shift+F3); Go to line (Ctrl+G); Next (Block quote (Ctrl+Q), Heading (Ctrl+H), Heading level 1 (Ctrl+1), Heading level 2 (Ctrl+2), Heading level 3 (Ctrl+3), Heading level 4 (Ctrl+4), Heading level 5 (Ctrl+5), Heading level 6 (Ctrl+6), Link (Ctrl+K), List (Ctrl+L), List item (Ctrl+I), Table (Ctrl+T)); Previous (Block quote (Ctrl+Shift+Q), Heading (Ctrl+Shift+H), Heading level 1 (Ctrl+Shift+1), Heading level 2 (Ctrl+Shift+2), Heading level 3 (Ctrl+Shift+3), Heading level 4 (Ctrl+Shift+4), Heading level 5 (Ctrl+Shift+5), Heading level 6 (Ctrl+Shift+6), Link (Ctrl+Shift+K), List (Ctrl+Shift+L), List item (Ctrl+Shift+I), Table (Ctrl+Shift+T)).
- **Context**: Copy (Ctrl+C); Cut (Ctrl+X); Paste (Ctrl+V); Select all (Ctrl+A).
- **Options**: Developer (Open settings file); General (Check for updates at start, Templates in axTemplate); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size, Word wrap (Ctrl+Shift+W)).
- Keys without a menu item: Ctrl+Tab (Between the text and the details).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## Settings

Everything you change is saved in `%APPDATA%\AxKit\axDown.json`: the font, the text size, the recent files, the window's place and size, and whether axDown checks for updates when it starts. Word wrap is in the theme file instead, the same for every editor of axKit.

## Updates

New versions of axKit are published on GitHub, and every app of axKit updates together. When axDown starts, it asks GitHub once whether there is a newer one; if there is, a notice opens with four buttons. I accept all terms, install and restart (Enter) downloads and installs the new version while axDown stays open, then closes axDown (asking first about unsaved changes) and starts the new version on the same file; I accept all terms, install and close installs it and closes without starting it again. One progress bar covers the download and the installation, and NVDA reports it the way it reports any progress bar (beeps or percentages, by your NVDA settings). Escape during the download cancels it and keeps the version you have; once the installation has begun it runs to the end, a few seconds. Open release page opens it in the browser; Later keeps the version you have, and Help, "Update to axKit n" offers it until you take it. Help, "Check for updates (axKit n)" names the version you have, asks at any time and is always there, so after skipping a version you can look again. Help, About, Version names the version you have, the latest release on GitHub and where the settings and logs are.

## If something goes wrong

- A file that cannot be opened, is not text, or is larger than 32 MB, is reported in a notice; the window stays open, empty and untitled, so that saving cannot write over the file.
- An error from axDown is a notice inside the window. Escape closes it. Help, Diagnostics, Copy diagnostics puts the version, the paths and the last log lines on the clipboard for a bug report; the log is in `%LOCALAPPDATA%\AxKit\logs\axDown.log`.

## About axDown

axDown is part of axKit, made by Dr. Kyle Keane, www.kylekeane.com. It is free software under the GNU AGPL 3 (the `LICENSE` file next to the program), with the terms in `NOTICE.md` (Help, About, Licence): use it, study it, change it and share it; a changed version that is shared gives its source under the same licence and carries a name of its own.

## Disclaimer

axDown is free software offered under the GNU AGPL 3, with the terms and the full disclaimer in `NOTICE.md` (Help, About, Licence). It is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use axDown at your own risk.
