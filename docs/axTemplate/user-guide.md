# axTemplate user guide

axTemplate keeps your templates: pieces of text you write once and put in again and again, made for screen reader users. It has two windows. The browser is where templates are made and changed. The launcher comes up over any program when you press Ctrl+Shift+Semicolon: you type a few letters, press Enter, and the template is put in where your caret was.

## Install

axTemplate is part of axKit. Installing axKit (see the axKit guide in the Help, About) gives you a Start menu entry `axKit axTemplate` and a console command `axTemplate`. Any axKit window starts one too: Alt+K, Apps, axTemplate.

## Start

Starting axTemplate shows the browser. Its window ends its title in "- axTemplate", and the focus starts in the list of your templates, on the left. With no template yet, the list has one line: "No templates yet: Ctrl+N makes one".

The launcher needs no start of yours. It runs in the background from the moment axKit is installed or updated, and again each time you sign in to Windows, so Ctrl+Shift+Semicolon (the semicolon key, beside L) brings it up from any program. To have it not start by itself, turn off Options, General, Start with Windows in the launcher's window. Starting axTemplate also starts the launcher when it is not running.

## Use

### The launcher, from any program

- Ctrl+Shift+Semicolon shows the launcher: one field, "Search templates", with your templates listed under it. The key again, or Escape, hides it, and you are back where you were.
- Type words of a template's name or of its text. Only the templates that hold every word stay in the list; those that hold them in their name come first. With nothing typed, the templates you used last come first.
- Down and Up move through the list and say each template with its place, "Thanks, 1 of 3". The caret stays in the field. Ctrl+Home and Ctrl+End go to the first and the last, Page Down and Page Up by a screen.
- Enter puts the template into the program you came from: the launcher hides, and the text is pasted where your caret was, with that program's own paste, Ctrl+V. When the template has a `{caret}` mark, the caret is then moved back to it, so you can type on at once.
- Ctrl+Enter does the same around what you had selected in that program: the selection is copied there first (the launcher presses Ctrl+C for you), stands where the template says `{selection}`, and the template takes its place. Do not use it in a terminal, where Ctrl+C ends the running command; and an editor that copies the whole line when nothing is selected gives that line.
- Afterwards the clipboard holds what it held before, when that was text: a template does not cost you what you had copied. The template itself is kept out of clipboard histories, axClip's and Windows' own.
- Shift+Enter only puts the text on the clipboard: the launcher hides and you are back in your program, which is the sign that it worked. Use it where pasting is another key, and in a program that runs as administrator, where Windows lets no other program press keys.
- Ctrl+Q reads the template's text before you use it. In a terminal, a template of several lines runs each line as it is pasted, as any paste of several lines does.
- Ctrl+B opens the browser.
- Options, Hotkey changes the key. Session, Quit axTemplate ends the launcher; closing its window only hides it.
- Options, Wait before the clipboard comes back: what you had copied is put back on the clipboard after a template goes in. Where the launcher cannot see the program's text, it waits 0.6 seconds after pasting first; if a slow program pastes your old clipboard in place of the template, choose 1, 2 or 4 seconds.

How the caret gets to its place: where the program shows its text to screen readers (Word, Notepad, a field in a browser), the template is pasted with the mark `{caret}` still in it, and the launcher finds that mark back from the caret, selects it and deletes it, so the caret is exactly there, however slowly the program pastes. Where it does not (a terminal, some older programs), the caret is moved back with Left keys, one for each character after the mark; a program that changes what is pasted, by indenting it or closing a bracket by itself, can leave it a little off there. If you ever find `{caret}` left in your text, the program did not let the launcher find it in time: delete it yourself, and tell the maintainer with F4. The launcher waits until you have let go of Ctrl and Shift before it sends anything.

### The templates, on the left

- Up, Down, Home and End move through the templates. Typing the start of a name jumps to it.
- Ctrl+N makes a new template: its form opens, empty. Type the name, Tab to the Text and write it, check where it shows, then Ctrl+Enter, or Enter from the name, adds it.
- Enter opens the template's form: every field in one place, the name first, then the Text, then the checks. Tab or Ctrl+Down goes to the next field, Shift+Tab or Ctrl+Up to the one before. Ctrl+Enter saves everything as one change, which Ctrl+Z takes back; Escape leaves and changes nothing. A new name renames the file too.
- F2 renames the template. Its file is renamed with it.
- Delete (Template, Archive) moves the template into the Archive folder beside your templates, after a question. With several selected (Space selects), it moves those, and the question says how many. Nothing is lost: the Archive folder is in your templates' folder, which Session, Show the templates' folder in opens, and a file moved back out of it is a template again.
- Ctrl+Z takes back the last change, whichever it was: a field, a rename, a new template, an archiving. It works for as long as the window is open.
- Ctrl+Enter copies the template's text to the clipboard, with its marks filled with nothing, so that you can paste it somewhere and see what it makes.
- Ctrl+J opens the template's file in axJSON.
- Ctrl+Q reads every field of the template. F5 reads the templates again.

### The fields, on the right, to read

- Title is the template's name.
- Text is what is put in. In the form it is a box of several lines: there Enter is a new line.
- Then come checks, one for each axKit app where the template also shows. Space checks or unchecks one right here. In the form they are one list: Down and Up move through the apps, Space checks or unchecks one, Ctrl+A checks them all, and the Check all and Uncheck all buttons just before the list check or uncheck them all. Enter on any field opens the form with the focus in that field. A template checked for an app is in that app's Actions, Insert template and in its command palette: axDown, axJSON, axCalc, axTerminal and axClaude. The checks for axTask, axPass and axMCP are for their forms, which are still being finished; there, and in any field, the launcher's hotkey puts a template in. The launcher lists every template whatever its checks.

### The marks in a text

Three marks can stand in a template's text. Each is written with its curly brackets.

- `{caret}` is where the caret is left after the template is put in.
- `{selection}` is replaced by the text that was selected when the template was put in. With Enter the launcher reads it without pressing any key, in programs that show their text to screen readers (Word, Notepad, a browser's field); in another program it is empty with Enter, and Ctrl+Enter takes it by pressing Ctrl+C there first.
- `{clipboard}` is replaced by the clipboard's text, as it was when the launcher came up.

A link's template, for example, is `[{selection}]({caret})`: the selected words become the link's text, and the caret waits where the address goes.

### Where the templates are

Each template is one small JSON file in the folder `axKit\Templates` of your Documents. The files are yours: copy them to another computer, back them up, or delete them; axKit needs none of them to run, and writes the folder's `list.json` again when it is missing. Session, Show the templates' folder in, opens it in File Explorer or axFile. A file there that is not a template is listed with "not a template" after its name; Ctrl+J opens it in axJSON to mend.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); Hide (Escape); Open the browser (Ctrl+B); Quit axTemplate; Refresh (F5); Show the templates' folder in (axClaude, access layer for Claude Code, axFile, axGIT, access layer for Git, axTerminal, access layer for your shell, File Explorer).
- **Actions**: Deselect all (Ctrl+Shift+A); New template (Ctrl+N); No actions here yet; Select all (Ctrl+A); Undo (Ctrl+Z).
- **Navigation**: Nothing to move to here yet.
- **Context**: Copy without putting in (Shift+Enter); No templates yet: Ctrl+N makes one; Put in (Enter); Put in, with that program's selection (Ctrl+Enter); Read in full (Ctrl+Q).
- **Options**: Filter; Hotkey; Developer (Open settings file); General (Say the first match while typing, Start with Windows); Sort by (Name, The list's own order); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size); Wait before the clipboard comes back (0.3 seconds, 0.6 seconds, the default, 1 second, 2 seconds, 4 seconds).
- Keys without a menu item: Enter (Change the field); Space (Check the field); Escape (Clear the filter); Delete (Delete); Ctrl+F (Find in the list); F3 (Find next); Shift+F3 (Find previous); Ctrl+Home (First match); Right (Into the list); Ctrl+End (Last match); Ctrl+N (New item); Down (Next match); Enter (Open); Page Down (Page down); Page Up (Page up); Up (Previous match); Ctrl+Q (Read in full); F2 (Rename); Space (Select or deselect); Shift+Space (Select to here); Shift+Home (Select to the first template); Shift+End (Select to the last template); Shift+Down (Select to the next template); Shift+Up (Select to the previous template); Ctrl+Tab (Switch between the navigator and the detail pane); Shift+F10 (The Context menu); Shift+F10 (The field's menu); Right (The next value of the field); Left (The previous value of the field); Tab (To the detail pane); Tab (To the navigator); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## Settings

In the browser, Options, Filter and Sort by arrange the list. In the launcher, Options, Hotkey sets the key that brings it up, and Options, General has Start with Windows and Say the first match while typing. In both, Options, Text sets the font and its size, and Options, General the update check.

## Updates

The Help, Check for updates.

## If something goes wrong

- "The template changed on disk; it is read again": the file was changed outside this window, in axJSON for example, after the window read it. Nothing was changed; do what you wanted again.
- The hotkey does nothing: the launcher is not running (after Quit, or with Start with Windows turned off), or another program has the key. Start axTemplate once; if the key is taken, the launcher shows itself and says so when it starts, and Options, Hotkey in its window chooses another.
- Enter pasted nothing: when another window came to the front in between, nothing is pasted into it, and the text waits on the clipboard for Ctrl+V. A program that runs as administrator takes no keys from the launcher: paste there yourself.
- Options, Developer, Copy diagnostics copies what helps a bug report.

## About

The Help, About.

## Disclaimer

axTemplate comes with no warranty; see the licence in the Help, About.
