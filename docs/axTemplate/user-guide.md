# axTemplate user guide

axTemplate keeps your templates: pieces of text you write once and put in again and again, made for screen reader users. It has two windows. The browser is where templates are made and changed. The launcher comes up over any program when you press Ctrl+Shift+Semicolon: you type a few letters, press Enter, and the template is put in where your caret was.

## Install

axTemplate is part of axKit. Installing axKit (see the axKit guide in the Help, About) gives you a Start menu entry `axKit axTemplate` and a console command `axTemplate`. Any axKit window starts one too: Alt+W, Apps, axTemplate.

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

How the caret gets to its place: where the program shows its text to screen readers (Word, Notepad, a field in a browser), the template is pasted with the mark `{caret}` still in it, and the launcher finds that mark back from the caret, selects it and deletes it, so the caret is exactly there, however slowly the program pastes. Where it does not (a terminal, some older programs), the caret is moved back with Left keys, one for each character after the mark; a program that changes what is pasted, by indenting it or closing a bracket by itself, can leave it a little off there. If you ever find `{caret}` left in your text, the program did not let the launcher find it in time: delete it yourself, and tell the maintainer with F4. The launcher waits until you have let go of Ctrl and Shift before it sends anything.

### The templates, on the left

- Up, Down, Home and End move through the templates. Typing the start of a name jumps to it.
- Ctrl+N makes a new template: a notice asks its name. Enter adds it, with an empty text.
- Enter or Tab goes to the template's fields, on the right. Escape or Tab comes back.
- F2 renames the template. Its file is renamed with it.
- Delete deletes the template after a question. With several selected (Space selects), it deletes those, and the question says how many.
- Ctrl+Z takes back the last change, whichever it was: a field, a rename, a new template, a deletion. It works for as long as the window is open; a template deleted before the window was closed is gone.
- Ctrl+Enter copies the template's text to the clipboard, with its marks filled with nothing, so that you can paste it somewhere and see what it makes.
- Ctrl+J opens the template's file in axJSON.
- Ctrl+Q reads every field of the template. F5 reads the templates again.

### The fields, on the right

- Title is the template's name.
- Text is what is put in. Enter opens it as a field of several lines: there Enter is a new line, and Ctrl+Enter keeps the text.
- Then come checks, one for each place inside axKit where the template also shows: Every axKit app first, then one for each app. Space checks or unchecks. These are for a step that is built later, the templates in the command palette of the axKit apps; today they are only kept. The launcher lists every template whatever its checks.

### The marks in a text

Three marks can stand in a template's text. Each is written with its curly brackets.

- `{caret}` is where the caret is left after the template is put in.
- `{selection}` is replaced by the text that was selected when the template was put in: from the launcher with Ctrl+Enter, and empty with Enter.
- `{clipboard}` is replaced by the clipboard's text, as it was when the launcher came up.

A link's template, for example, is `[{selection}]({caret})`: the selected words become the link's text, and the caret waits where the address goes.

### Where the templates are

Each template is one small JSON file in the folder `axKit\Templates` of your Documents. The files are yours: copy them to another computer, back them up, or delete them; axKit needs none of them to run, and writes the folder's `list.json` again when it is missing. Session, Show the templates' folder in, opens it in File Explorer or axFile. A file there that is not a template is listed with "not a template" after its name; Ctrl+J opens it in axJSON to mend.

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
