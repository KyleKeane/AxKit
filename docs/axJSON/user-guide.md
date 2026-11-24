# axJSON user guide

axJSON is a small editor for JSON files, part of axKit, made for working with a screen reader: one plain text field with the whole file in it, keys that jump through the file by its structure, and a Format command that lays the file out and repairs the small things that make JSON invalid. It opens one file in one window. The structured view with a field per value is planned for a later version.

## Install

axKit installs all its apps at once; see the axKit guide (Help, About, axKit guide). axJSON is "axKit axJSON" in the Start menu, `axJSON` in a console, and "Open with" for `.json` files in File Explorer.

## Start

- Start menu: axKit axJSON opens an empty document.
- Console: `axjson settings.json` opens that file; so does `axkit settings.json`, since a `.json` file means axJSON.
- File Explorer: right-click a `.json` file, Open with axKit, axJSON; or Windows' own Open with, axJSON.

## Write and read

The window is one text field named after the file, so NVDA reads it as any text: the arrow keys move, Ctrl+Home and Ctrl+End go to the ends, Page Up and Page Down move a screen, Ctrl+Up and Ctrl+Down move by paragraph. Nothing is spoken that you did not ask for.

Long lines wrap at the window's edge, so Down Arrow reads one screen line at a time. Options, Text, Word wrap (Ctrl+Shift+W) turns that off, and each line of the file is then one line in the window. The setting is one for every editor of axKit, kept in the theme file (`%APPDATA%\AxKit\theme.json`, `display.wordWrap`); axDown and axPreview follow it the next time their window is active.

## Files

Ctrl+S saves and axJSON says "Saved". A new document asks for a name first, with the Windows file dialog; Ctrl+Shift+S, Save as, asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first if you have unsaved changes; Ctrl+Shift+N starts an empty document in a new axJSON window and leaves this one as it is, as does closing the window: a notice says "Save changes to settings.json?" with Save, Don't save and Cancel; Enter saves, Escape cancels. Session, Open recent file lists the last ten files you opened or saved; Session, Show current file in, File Explorer (Ctrl+W) opens File Explorer with the file selected.

When another program changes the file while it is open here, for example Claude editing it while you are in axClaude, axJSON notices when you come back to its window: with nothing unsaved it reloads the file, keeps your line, and says "Reloaded, settings.json changed on disk"; with unsaved changes it says "settings.json changed on disk; Save will ask", and Ctrl+S then offers Write over, Reload and lose mine, or Cancel.

axJSON writes a file back the way it found it: the same encoding, with or without a byte order mark, and the same kind of line ending. The status bar shows what it found, for example "UTF-8 · CRLF". If the file changed on disk while you had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*settings.json - axJSON".

## Find and go to a line

Ctrl+F opens a Find field inside the window; Enter finds the next match, selects it and reads its line; F3 and Shift+F3 repeat forwards and backwards. Ctrl+G opens a Go to line field, named with where you are, "Go to line (now line 12, column 4)"; type a number and press Enter, or Escape to stay.

## Move by structure

The keys follow axDown's: Ctrl and a letter go to the next thing of that kind, Ctrl+Shift and the letter to the previous one, and each jump speaks what it landed on. The caret goes to the key or the value, so Enter and typing act right there.

- Ctrl+K: the next key, spoken with its value, "model: opus", or with its size when the value is an object or an array, "permissions, object with 2 keys".
- Ctrl+J: the next object, "permissions, object with 2 keys"; the whole file is the first one.
- Ctrl+L: the next array, "allow, array with 2 items".
- Ctrl+I: the next item of an array, "Item 2 of 2, Read".
- Ctrl+E: the place where the file stops being JSON, "Error, line 12: expected a comma or a closing brace after model, found a string". When the file is valid you hear "No next error", which is the quickest way to check a file.

Past the last one of a kind you hear "No next key" and the caret stays. The same keys work in the user guide and the other notices. The Navigation menu lists all of them under Next and Previous.

## Choose instead of typing

For two kinds of file axJSON knows what can be written, and lets you choose it: the configuration of an axTask list (`list.json`, which axTask opens here with Ctrl+J on a list) and an item of such a list. The keys are Alt with an arrow, and they follow the arrows as you know them from the lists of axKit: Right goes in, Up and Down change, Left steps out.

- **Alt+Right, the list of values** (Actions, Choose a value). Put the caret on a member whose value is one of a few, such as `"type"` of a field or `"does"` of an action; the caret may be on the key, in the value or just after it. A list opens under the text, named with the member and what it is, "type: What the field holds", and starts on the value that stands there. Up and Down move through the values; Enter writes the one you are on in place of the old value and says it, "type: date"; Escape leaves and nothing changed.
- **Alt+Down and Alt+Up, the next and the previous value** (Actions, Next value and Previous value). The value changes in line, without a list, and you hear it with its place: "type: date, 4 of 8". At the last or the first value nothing changes; you hear the edge sound and "type: link, the last of 8".
- **Alt+Left, add a member** (Actions, Add a member). Put the caret anywhere in an object. The list holds what that object can have and does not have yet, each line the name and what it is, "key: The key that runs it; without one it runs from the menu". Enter adds the member at the end of the object with a first value and puts the caret on that value, so that Alt+Down or Alt+Right can follow at once.

On a member that takes any value you hear what it is, "title takes any value. The field's name, as it is shown and said".

So an action is written by choosing: Alt+Left in the action, Enter on "key", then Alt+Down until you hear the key you want, or Alt+Right to pick it from the eighteen.

In an item's file the choices come from the item's list: axJSON follows the item's `"schema": "list.json"` to the configuration beside it, and offers a choice field's options and true or false for a check.

Each change is one step for Ctrl+Z. Nothing is checked or refused: you can still type anything, and for any other file you hear "axJSON has no options for this file".

## Format

Actions, Format (Ctrl+Shift+F) writes the file out again with two spaces per level and one key or item per line, keeping the order of the keys and the file's line endings, and says "Formatted". Small mistakes are repaired on the way and named: a comma after the last item, a missing comma between two items, single quotes, a key without quotes, and comments, so "Formatted, repaired: 1 trailing comma, 1 unquoted key". Anything else leaves the file as it is and names the line: "Not valid JSON: line 12, expected a value, found '}'". Format is one step for Undo (Ctrl+Z), and Redo (Ctrl+Y) formats again. The history reaches back to when the file was opened in this window, a run of typing being one step: "Undone", "Redone", "Nothing to undo", "Nothing to redo".

## Text size and fonts

Ctrl+Plus and Ctrl+Minus make the text larger and smaller, and axJSON says the new size. Options, Text, Font opens the Windows font dialog. Both are remembered.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); New (Ctrl+N); New window (Ctrl+Shift+N); Open (Ctrl+O); Save (Ctrl+S); Save as (Ctrl+Shift+S); Show current file in (axClaude, access layer for Claude Code, axFile, axGIT, access layer for Git, axTerminal, access layer for your shell, File Explorer (Ctrl+W)).
- **Actions**: Add a member (Alt+Left); Choose a value (Alt+Right); Format (Ctrl+Shift+F); Next value (Alt+Down); Previous value (Alt+Up); Redo (Ctrl+Y); Undo (Ctrl+Z).
- **Navigation**: Find (Ctrl+F); Find first; Find next (F3); Find previous (Shift+F3); Go to line (Ctrl+G); Next (Array (Ctrl+L), Error (Ctrl+E), Item (Ctrl+I), Key (Ctrl+K), Object (Ctrl+J)); Previous (Array (Ctrl+Shift+L), Error (Ctrl+Shift+E), Item (Ctrl+Shift+I), Key (Ctrl+Shift+K), Object (Ctrl+Shift+J)).
- **Context**: Copy (Ctrl+C); Cut (Ctrl+X); Paste (Ctrl+V); Select all (Ctrl+A).
- **Options**: Developer (Open settings file); General (Check for updates at start, Templates in axTemplate); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size, Word wrap (Ctrl+Shift+W)).
- Keys without a menu item: Ctrl+Tab (Between the text and the details).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## Settings

axJSON remembers the font, the text size, the recent files and the window's place in `%APPDATA%\AxKit\axJSON.json`. Options, Developer, Open settings file opens it in a new axJSON window. Word wrap is in the theme file instead, the same for every editor of axKit.

## Updates

New versions of axKit are published on GitHub, and every app of axKit updates together. When axJSON starts, it asks GitHub once whether there is a newer one; if there is, a notice opens with four buttons. I accept all terms, install and restart downloads and installs the new version, then closes axJSON (asking first about unsaved changes) and starts the new version on the same file; I accept all terms, install and close installs it and closes without starting it again. Later keeps the version you have, and Help, Update to axKit n offers it until you take it. Help, Check for updates asks at any time. Help, About, Version names the version you have and the latest release on GitHub.

## If something goes wrong

- A file that cannot be opened, is not text, or is larger than 32 MB, is reported in a notice; the window stays open, empty and untitled, so that saving cannot write over the file.
- An error from axJSON is a notice inside the window. Escape closes it. Help, Diagnostics, Copy diagnostics puts the version, the paths and the last log lines on the clipboard for a bug report; the log is in `%LOCALAPPDATA%\AxKit\logs\axJSON.log`.

## About axJSON

axJSON is part of axKit, made by Dr. Kyle Keane, www.kylekeane.com. It is free software under the GNU AGPL 3 (the `LICENSE` file next to the program), with the terms in `NOTICE.md` (Help, About, Licence): use it, study it, change it and share it; a changed version that is shared gives its source under the same licence and carries a name of its own.

## Disclaimer

axJSON is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use axJSON at your own risk.
