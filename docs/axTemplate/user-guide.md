# axTemplate user guide

axTemplate keeps your templates: pieces of text you write once and put in again and again, made for screen reader users. This guide describes what is built today, the browser, where templates are made and changed. The launcher behind a hotkey, which puts a template into any program, is the next step and is not there yet.

## Install

axTemplate is part of axKit. Installing axKit (see the axKit guide in the Help, About) gives you a Start menu entry `axKit axTemplate` and a console command `axTemplate`. Any axKit window starts one too: Alt+A, New window, axTemplate.

## Start

The window ends its title in "- axTemplate", and the focus starts in the list of your templates, on the left. With no template yet, the list has one line: "No templates yet: Ctrl+N makes one".

## Use

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
- Then come checks, one for each place inside axKit where the template also shows: Every axKit app first, then one for each app. Space checks or unchecks. These are used by the steps that are built next; today they are only kept.

### The marks in a text

Three marks can stand in a template's text. Each is written with its curly brackets.

- `{caret}` is where the caret is left after the template is put in.
- `{selection}` is replaced by the text that was selected when the template was put in.
- `{clipboard}` is replaced by the clipboard's text.

A link's template, for example, is `[{selection}]({caret})`: the selected words become the link's text, and the caret waits where the address goes.

### Where the templates are

Each template is one small JSON file in the folder `templates` of axKit's settings folder. Session, Show the templates' folder in, opens it in File Explorer or axFile. A file there that is not a template is listed with "not a template" after its name; Ctrl+J opens it in axJSON to mend.

## Settings

Options, Filter and Sort by arrange the list. Options, Text sets the font and its size; Options, General the update check.

## Updates

The Help, Check for updates.

## If something goes wrong

- "The template changed on disk; it is read again": the file was changed outside this window, in axJSON for example, after the window read it. Nothing was changed; do what you wanted again.
- Options, Developer, Copy diagnostics copies what helps a bug report.

## About

The Help, About.

## Disclaimer

axTemplate comes with no warranty; see the licence in the Help, About.
