# axPreview user guide

axPreview is a viewer for one text file, made for screen reader users: the file's text in a plain read-only field, to read and not to change, with every reading key axDown has: the arrows, Page Up and Page Down, Ctrl+Up and Ctrl+Down by paragraph, find, Go to line, `l` for the line, and for a Markdown file the keys that jump between headings, links, lists, list items, block quotes and tables. Ctrl+E opens the same file in its editor for the one time in ten you want to change something.

## Install

axPreview is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry `axKit axPreview`, a console command `axPreview`, axPreview in the `Open with axKit` submenu when you right-click any file in File Explorer, and axPreview under Windows' own `Open with` for every kind of file, so that you can make it the program that opens a kind when you press Enter on one: Settings, Apps, Default apps, axPreview, or right-click a file, Open with, Choose another app, axPreview, Always. Windows lets only you set that, never a program.

## Start

- From axFile: Shift+Enter on a file, or the entry's menu (Shift+F10), Preview. The axPreview window opens on the file and comes to the front; Alt+W, O, 1 goes back to axFile. Enter on a file in axFile opens it in the program you have made its default in Windows, which can be axPreview.
- File Explorer: right-click a file, open `Open with axKit` and choose axPreview; or double-click it once axPreview is its default program.
- Start menu: `axKit axPreview`. Press the Windows key and type `axp`. The window opens empty, "Untitled"; Ctrl+O opens a file.
- Console: `axpreview C:\notes\README.md`.

axPreview keeps one window: a file opened from axFile, File Explorer or the console while an axPreview window is open goes into that window rather than opening another; Session, Open recent lists the last ten. A second window is the axKit menu's Apps, axPreview, Ctrl+N, or `--new` on the console.

The window is named after the file, then axPreview, then the file's full path: "README.md, axPreview, C:\work\README.md". NVDA reads the title, then "README.md, read only, edit, multi line" and the first line. The status bar (NVDA+End) has the file's name on the left and "Line 12, Column 4 · UTF-8 · CRLF" on the right.

## Reading

The text is the one control. Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right move as in any text; Shift with any of them selects; Ctrl+A selects all and Ctrl+C copies. Typing changes nothing. Nothing is spoken until you ask.

- **Page Up** and **Page Down** move one screen, or to the first or last line. **Ctrl+Up** and **Ctrl+Down** go to the previous or next paragraph.
- **l** says where you are, "Line 12 of 40". **F12** goes to a line: the field under the text says where you are now, type the number, Enter.
- **Ctrl+F** finds text; F3 and Shift+F3 the next and the previous match, the match selected and read.
- **Ctrl+H** and **Ctrl+Shift+H** go to the next and the previous Markdown heading, spoken as "Install, heading level 2"; **Ctrl+1** to **Ctrl+6** to the next heading of that level only, Ctrl+Shift with the number back. **Ctrl+K** links, **Ctrl+L** lists, **Ctrl+I** list items, **Ctrl+Q** block quotes, **Ctrl+T** tables, each with Shift for the previous one; "No next heading" when there is none. They work in a plain text file too, where there are none, and in every notice text. Navigation, Next and Previous list them.
- **F5** reads the file again, the caret kept on its line, "Reloaded". Coming back to the window after another program changed the file does the same by itself: "Reloaded, README.md changed on disk".
- **Ctrl+E** opens the file in axDown, or in axJSON for a `.json` file, a new editor window; Session, Edit in axDown is the same item, named after the file's kind.
- **Session, Show current file in, File Explorer** shows the file in File Explorer; the same submenu also offers axClaude and axTerminal (in its folder), axGIT (the repository that holds it) and axFile (the file selected in its folder).
- **Ctrl+O** opens another file with the Windows file picker; Session, Open recent lists the last ten, "1. README.md (C:\notes\README.md)".
- **F11** turns word wrap off and on (Options, Text); the caret stays where it was. The setting is one for every editor of axKit, kept in the theme file.
- **Ctrl+S** says "Nothing to save here": nothing is ever changed, so nothing is ever lost.

Every file opens. A text file, up to 32 MB, shows its text: the editors' kinds always, a log, a script, a file without an extension. A Word document, a PowerPoint presentation or an OpenDocument text shows its text, its headings and slides as headings, so that Ctrl+H moves through them. A zip shows its list of files, the first thousand of a huge one and then how many more. A picture shows the text Windows' own reader finds in it, a second or two after it opens, one line per line of the picture (nothing of it leaves your computer); Session, Read the clipboard's picture (Ctrl+Shift+V) does the same for a picture on the clipboard, which Alt+PrintScreen puts there from the window in front and PrintScreen from the whole screen, so a dialog that reads badly is read as text. A sound or a video says what it is. Any other file shows the readable text inside it. A text file over 32 MB gives "Too large".

## The Details list

Under the text is the Details list: Tab or F6 moves there and back. It has everything known about the file, one line each: its name, folder, size, type and dates; what is in it, like the encoding and the number of words, or the files in a zip; and every detail Windows has, like a picture's dimensions, a song's length and artist, or a document's author. Letters jump to a line. Ctrl+C copies the line's value, without its name. Enter on the Folder line opens the folder in axFile with the file selected.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Edit in axDown (Ctrl+E); Exit (Alt+F4); New window (Ctrl+N); Open (Ctrl+O); Read the clipboard's picture (Ctrl+Shift+V); Reload (F5); Show current file in (axClaude, access layer for Claude Code, axFile, axGIT, access layer for Git, axTerminal, access layer for your shell, File Explorer).
- **Actions**: No actions here yet.
- **Navigation**: Find (Ctrl+F); Find first; Find next (F3); Find previous (Shift+F3); Go to line (F12); Next (Block quote (Ctrl+Q), Heading (Ctrl+H), Heading level 1 (Ctrl+1), Heading level 2 (Ctrl+2), Heading level 3 (Ctrl+3), Heading level 4 (Ctrl+4), Heading level 5 (Ctrl+5), Heading level 6 (Ctrl+6), Link (Ctrl+K), List (Ctrl+L), List item (Ctrl+I), Table (Ctrl+T)); Previous (Block quote (Ctrl+Shift+Q), Heading (Ctrl+Shift+H), Heading level 1 (Ctrl+Shift+1), Heading level 2 (Ctrl+Shift+2), Heading level 3 (Ctrl+Shift+3), Heading level 4 (Ctrl+Shift+4), Heading level 5 (Ctrl+Shift+5), Heading level 6 (Ctrl+Shift+6), Link (Ctrl+Shift+K), List (Ctrl+Shift+L), List item (Ctrl+Shift+I), Table (Ctrl+Shift+T)).
- **Context**: Copy (Ctrl+C); Select all (Ctrl+A).
- **Options**: Check for updates at start; Edit app keys file; Keep a log of events; Open the sounds folder; Developer (Open settings file); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size, Word wrap (F11)); Theme (Edit theme file, Reload theme, Theme on).
- Keys without a menu item: Ctrl+Tab (Between the text and the details); Ctrl+C (Copy the detail's value); Enter (Open the detail); L (Say the line).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1, the command palette, lists every key and command. Options ends with the settings of all of axKit.
<!-- end of the menus -->

## Settings

Everything you change is kept in `%APPDATA%\AxKit\axPreview.json`: the recent files, the font and text size, the update check. Options, Developer, Open settings file opens it in axJSON. Word wrap is in the theme file instead (`%APPDATA%\AxKit\theme.json`), the same for every editor of axKit.

## Updates

axPreview checks for a newer axKit when it starts and offers it in a notice; the Help menu has Check for updates at any time. One update brings every app of axKit up to date.

## If something goes wrong

axPreview's log is `%LOCALAPPDATA%\AxKit\logs\axPreview.log`; Help, Diagnostics, Copy diagnostics copies its end with the versions, for a bug report. A file that could not be read is reported in a notice with Windows' message.

## About axPreview

axPreview is one app of axKit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Help menu's About names the version and the licence.

## Disclaimer

axPreview reads your files and never writes them.
