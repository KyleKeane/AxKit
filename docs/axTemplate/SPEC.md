# axTemplate — Specification

axTemplate is a planned app of the axKit bundle (`docs/axKit/SPEC.md`): the user's templates behind a hotkey, put in
wherever the caret was, in any program. **Specified on 2026-11-23 for the maintainer's reading; nothing of it is
built.** It is the wider form of the templates the editors got that day (`docs/axDown/SPEC.md` AD-13), which are
its scaffolding: the same files, the same marks, the same pure code. Requirements are numbered `TP-n.m` and
decisions `TP-Dn`; the bundle's rules are cited by number and not repeated; axClip's specification
(`docs/axClip/SPEC.md`, CP-n.m) is cited for what the two share. In axTemplate's code and documents, "SPEC.md" means
this file.

The maintainer's words, 2026-11-23: "we should abstract the axTemplate as it's own app that can inject things
anywhere on the system like autohotkey. and then every axkit app could have a special way to bring certain
templates, or probably axTemplate could have flags for including the template in the command palette for specific
or all axkit apps ... i want this wider axtemplate app to be available as a global gotkey with a simple launcher
like axclip that uses pattern matching for me to find templates by name or content and then inject it into wherver
the cursor was sitting in the prebious app. and then the option to load templates as available in the axkit command
palette could be considered. axautomate could likely be integrated with axtemplate in a celever way where an
automation step could be the insertion of a template and content into it."

## 1. Summary

A template is a file of text the user wrote once. axTemplate's hotkey brings up one search field over all of them.
Typing narrows the list by the words of a template's name or of its text; Down and Up move through it with the
caret kept in the field, as in axStart and axClip; Enter puts the template into the program that was in front, at
its caret. The same templates show in the command palette of the axKit apps the user names for them. Nothing is
spoken but the answer to a key. That is the launcher, as axClip is one. The app's other window is a browser, as
axTask is one: the templates in their places, the text of the one you are on beside it, and the commands that
make, rename, move and delete them.

## 2. Goals and non-goals

- **G1 Any program.** A template reaches every program that takes a paste: a browser's form, a mail, a terminal, an
  axKit window.
- **G2 axClip's way.** The same window, field and keys as the two launchers there are (AS-3, CP-2), so that one
  habit serves all three. It is the bundle's hotkey window (`HotkeyForm`), nothing of its own.
- **G3 Files, and nothing else to learn.** A template is a plain file in one folder, written in axDown or any
  editor, with the three marks of AD-13.2. Where a template shows is where its file lies. No database, no syntax
  beyond the marks.
- **G4 One store for the bundle.** The editors' templates of AD-13 and axTemplate's are the same files: what the
  launcher finds is what an editor's Insert template menu holds.
- **Non-goals, for now** (the maintainer paused them on 2026-11-23): placeholders to fill and a key that finds the
  next one; a form behind a template; a text expander that watches what is typed. Also: formatted text, pictures,
  typing a template key by key, sync between computers.

## 3. Scenarios

1. In a browser's comment field, the hotkey: "axTemplate window, Search templates, edit". Type `thanks`: after the
   pause, "Thanks for the report, 1 of 2". Enter: axTemplate hides, the browser is in front again, and the text
   stands in the field.
2. In a mail, copy a name, then the hotkey, `intro`, Enter: the template "Introduction" holds `{clipboard}` where
   the name goes and `{caret}` where the first sentence starts; the name is in, and the caret waits there.
3. The hotkey, type `sprint`: no template is named so, and one holds the word in its text: "Weekly notes, 1 of 1".
   Ctrl+Q reads the whole template in a notice before it is used.
4. In axDown, Ctrl+Shift+P, `insert template`: the editor's own three, the templates of its folder, and those the
   user keeps for every axKit app.

## 4. Functional requirements

### TP-1 Launch, the background, the hotkey

- TP-1.1 **The launcher** is as axClip (CP-1): a background process with one hidden window, one copy at a time,
  started at sign-in (`axKit template --hidden`) or with the first start of the app, brought up by its hotkey,
  hidden by Escape, Alt+F4 and the focus going to another program; only Quit ends it.
- TP-1.1a **A plain start** (`axKit template`, or Window, New window, axTemplate) shows the browser (TP-8), and
  starts the launcher in the background when it does not run yet, so that the hotkey works from then on.
- TP-1.2 The hotkey is the user's to change (Options, Hotkey). At first **Ctrl+Shift+Period** (TP-D1).
- TP-1.3 In the Store's copy axTemplate has no entry and no startup task of its own: the package's eight entries
  are taken (bundle AX-13.2). It is started from any axKit window, Window, New window, and then runs until Quit
  (as axHelp was until it took an entry). Open for the maintainer: section 7.

### TP-2 The templates

- TP-2.1 **One folder**: `%APPDATA%\AxKit\templates`, the folder AD-13.4 already uses. Every file in it and in its
  folders is one template, named by its file without the extension.
- TP-2.2 **Where a template shows is where its file lies** (TP-D2):
  - in the folder itself: in the launcher alone;
  - in `every axKit app`: also in the command palette of every axKit window that has a field to write in;
  - in a folder named as an axKit app (`axDown`, `axJSON`, `axClaude`, `axTerminal`): also in that app's command
    palette, and in its Insert template menu where it has one (AD-13).
  The launcher lists them all, whichever folder they lie in. To change where a template shows, move its file, in
  axFile or anywhere.
- TP-2.3 **The marks** are AD-13.2's, filled by the same code (`AxKit.Core/TextTemplate`): `{caret}`, `{selection}`,
  `{clipboard}`. In an axKit window all three mean what they mean in an editor. In another program:
  - `{clipboard}` is the clipboard's text at the moment the hotkey was pressed: copy first, then call the
    template. This is the way text is carried in ("templates can take clipboard contents as a passthrough").
  - `{selection}` is empty: axTemplate cannot read another program's selection without sending it a copy key,
    and that key ends a running command in a terminal (TP-D4).
  - `{caret}`: after the paste the caret is moved back to its place, by one Left key for every character that
    follows the mark, a line ending counting as one (TP-D5).
- TP-2.4 A file larger than 64 KB, or one that cannot be read, is no template: the launcher lists it with "too
  large" or "not readable" after its name and puts nothing in.
- TP-2.5 The folder is read off the UI thread as the window comes up, and again on F5; a file changed meanwhile
  is read again when it is used.

### TP-3 The window

- TP-3.1 The bundle's hotkey window: the field "Search templates" with the caret always in it, and under it the
  list "Templates, N", never focused (`AxKitList` in its mode under a field, framework FW-4.12).
- TP-3.2 **Finding.** Every word typed must be found, in any order and without regard to case, in the template's
  name or in its text. Templates whose name holds all the words come first, in the order of their names; then
  those that hold a word only in their text. An empty field lists every template, the ones used last first.
- TP-3.3 A line of the list is the template's name and, after a comma, where it shows when that is not the
  launcher alone: "Introduction", "Link to a ticket, axDown", "Signature, every axKit app". What is said when the
  selection reaches it is the line and its place, "Introduction, 1 of 12". After a pause in the typing the first
  match is said, as in axStart and axClip (the setting Say the first match while typing).
- TP-3.4 Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End move through the list from the field, with the edge
  sound at an end and the sound of nothing found when nothing matches (the shared list's).

### TP-4 Using a template

- TP-4.1 **Enter puts it in**, axClip's way (CP-4.1, CP-D3): the marks are filled, the text is put on the
  clipboard, the window hides and gives the front back, and when the program that was in front is in front again
  the paste key, Ctrl+V, is sent to it; then the Left keys of TP-2.3. When another window is in front by then,
  nothing is sent and the text stays on the clipboard.
- TP-4.2 **The clipboard afterwards.** The template's text is put on the clipboard marked as not for a clipboard
  history, the mark password managers use, so that axClip and Windows' own history do not keep it. When the
  clipboard held plain text before, that text is put back once the paste is done (TP-D6).
- TP-4.3 Shift+Enter puts the filled text on the clipboard without pasting: "Copied", and the window hides. For a
  program whose paste is another key.
- TP-4.4 Ctrl+Q shows the template's own text, marks and all, in a notice titled with its name and its lines, read
  with the arrows; Escape closes it.
- TP-4.5 Ctrl+B opens the browser (TP-8) on the selected template; the launcher hides. The launcher itself makes,
  changes and deletes nothing: it finds and puts in.
- TP-4.6 A command with no template says "No templates yet: Ctrl+B opens the browser, where one is made" or the
  no-match line.

### TP-5 In the axKit apps

- TP-5.1 **The command palette.** In an axKit window whose focus is in a field to write in, the palette lists,
  after the window's commands, the templates of `every axKit app` and of the window's own app folder, each as
  "Insert template <name>". Enter puts the template in at that field's caret in the place of its selection, with
  all three marks, as an editor does (AD-13.2), and says "<name> inserted". This is a command of the window, in a
  menu (Actions, Insert template, which every such window then has), since the palette holds commands only (bundle
  AX-10.5).
- TP-5.2 The editors keep what they have (AD-13): their own templates, then the files of their folder; they gain
  the templates of `every axKit app`.
- TP-5.3 Not in a field that sends on Enter without a look: in axTerminal's command field and axClaude's message
  field a template is put into the field and never sent, as any text put there is.

### TP-6 axAutomate

- TP-6.1 A step of a workflow, `template`: the template's name, and optionally the text for `{selection}` and for
  `{clipboard}`. The step fills the marks and types the text into the window the workflow drives, where its focus
  is, as a `type` step does with a text of its own. So a workflow can put a template in and fill what it carries.
- TP-6.2 A template named in a workflow and not found fails the step with its name, as a missing file does.

### TP-7 Menus, keys, texts, settings, log

- TP-7.1 The menu bar is the bundle's (AX-9.1); the keys are a table of its own (`AxTemplateKeys`) beside the
  bundle's; F1 gives the keys; the user guide is `docs/axTemplate/user-guide.md`.
- TP-7.2 Settings (`%APPDATA%\AxKit\axTemplate.json`): the hotkey, Start with Windows, Say the first match while
  typing (the hotkey windows' three), and the names of the templates used last.
- TP-7.3 The log holds that a template was put in, never its name or its text (bundle rule 6). axTemplate makes
  no network request (rule 8). Its outside calls are the two axClip has: the clipboard, and the paste key sent to
  the program in front; they join `docs/axKit/outside-calls.md` when it is built.

### TP-8 The browser

The maintainer, 2026-11-23: "axTemlate will have the simple injection interface like axclip. but we also need to
define the browser that will be like axtask". The launcher is for the moment of use; the browser is where the
templates are kept in order.

- TP-8.1 **A second window, of the navigator type** (`ItemForm`, the window axTask has; framework FW-4.6 to
  FW-4.10a), started by `axKit template` or the Window menu, New window, axTemplate, and by Ctrl+B in the launcher.
  It is a window like any other: it shows in the taskbar and the Window menu, Alt+F4 closes it, and it needs no
  background process. The launcher is the same app's other window, on the hotkey (TP-D8).
- TP-8.2 **Levels and rows.** The top level is the places a template can show: "Launcher only", "Every axKit
  app", then one row for each axKit app that has a folder, each with its count, "axDown, 4 templates". Right or
  Enter goes into a place; its rows are its templates, "Introduction". Left and Backspace go up. Filter and Sort
  by are the navigator's (bundle AX-8.17); Space selects (AX-8.16).
- TP-8.3 **The second pane is the template's text**, to read (the navigator's text pane, as axGIT's): the row's
  file as it is, marks and all. Tab goes to it and back.
- TP-8.4 **On a template**: Enter opens it in axDown to change it. Ctrl+N makes a new one in the place you are in:
  a notice asks its name, the file is made, and it opens in axDown. F2 renames it in place (bundle AX-8.7). Delete
  moves the selected templates to the Recycle Bin, with one Ctrl+Z to bring them back. Ctrl+M, **Show in…**,
  moves the selected templates to another place, chosen from a list: this is how where a template shows is
  changed, and it is the folder of TP-2.2 that changes. Ctrl+Enter puts the template on the clipboard, filled,
  "Copied", for a look at what it makes.
- TP-8.5 **On a place**: Ctrl+Shift+N adds a place for an axKit app that has none yet, chosen from the apps with a
  field to write in. Session, Show in axFile opens the folder.
- TP-8.6 The browser reads the folder off the UI thread (the navigator's read, framework FW-4.7 point 1) and again
  on F5 and on coming back to the window. For the AI tools, reading is free; making, renaming, moving and
  deleting a template are never run for an AI tool (TP-D7): a template is text that will be pasted into any
  program, a shell among them, so only the user writes one.

## 5. Architecture

- `src/AxTemplate.Core`: the store (the folder read into templates with where each shows, `TemplateStore`), the
  finding (`TemplateMatch`: words against name and text, the order), the count of Left keys after a paste; all pure
  and unit tested. The marks stay `AxKit.Core/TextTemplate`, which the editors use already.
- `src/AxTemplate`: two windows, each on a window type of the bundle and neither on a window of its own. The
  launcher on `HotkeyForm`, as axClip's: the field, the list, Enter and the paste (`ForegroundBefore`, the paste key
  through `SendInput`), the notices. The browser on `ItemForm`, as axTask's: the places as the top level, the
  templates as rows, the text pane, and its commands over the store.
- The window base gains the templates of TP-5.1 for every window with a field: the editor shell's
  `TextEditorForm.Templates.cs` is where that code is today, and it moves to the base on this second use (rule 4).
- axAutomate's step is one more line of its step table.

## 6. Design decisions

- **TP-D1 Ctrl+Shift+Period.** Every system-wide key of axKit is on Ctrl+Shift with a punctuation key or Space
  (bundle AX-8.11): Space is axStart's, Slash and Comma axHelp's, Semicolon axClip's. Period is the one kept free
  for a fourth, beside the others under the right hand.
- **TP-D2 A folder says where a template shows; no flags.** The maintainer thought of flags on a template for the
  apps whose palette shows it. A flag needs a place to be written (a line in the file, which would be one more
  thing to learn and would end up pasted by mistake, or a list beside the files, which goes stale when a file is
  renamed) and a window to set it in. A folder needs neither: the file manager moves a file, and the folder's name
  is read by ear. The price: a template for two apps and not all is two files. Open for him (section 7).
- **TP-D3 Paste, not typed keys.** As axClip (CP-D3): pasting is the program's own command and takes the whole text
  at once, whatever its length and its characters. Typing key by key is slow, breaks on a keyboard layout, and in a
  terminal runs every line as it is typed.
- **TP-D4 No copy key sent for `{selection}`.** Reading another program's selection means sending it Ctrl+C. In a
  terminal that ends the running command; in other programs it replaces what the user had on the clipboard. The
  user copies first, and `{clipboard}` carries it.
- **TP-D5 The caret by Left keys.** The one way that works in a plain field of any program. It is exact where the
  program puts in what was pasted and nothing more; a program that indents or closes brackets by itself can land
  the caret a little off, and the guide says so. A template without `{caret}` sends no Left key.
- **TP-D6 The clipboard is given back.** A template put in should not cost the user what they had copied, and
  should not fill a clipboard history with templates.
- **TP-D7 A template is the user's own text.** axTemplate reads no template from outside the folder, takes none
  from an AI tool, and runs nothing: it pastes. A template of several lines pasted into a shell runs them, as any
  paste of several lines does; the list's Ctrl+Q reads a template before it is used.
- **TP-D8 Two windows, one app.** The launcher and the browser do two jobs: one is called in the middle of other
  work and must be gone at once, the other is sat in. The bundle has both shapes already, and an app with one
  window of each is new: until now every app is one window type (framework section 5). They are not a second
  window in the sense of bundle AX-7.2, which rules out dialogs of an app's own: each is a whole window of a
  bundle type, as axHelp's small window and its conversation are. The alternative is no browser of axTemplate's
  own, with axFile on the templates folder in its place: less to build, and without the template's text beside
  the row, the places by their names and Show in. Open for him (section 7).

## 7. Open questions, for the maintainer

1. **Folders or flags** (TP-D2). My recommendation: folders.
2. **The hotkey** (TP-D1): Ctrl+Shift+Period, or another.
3. **The Store's copy** (TP-1.3): axTemplate without an entry of its own, started from the Window menu; or one of
   the eight entries given up for it.
4. **`{selection}` in other programs** (TP-D4): left empty, as specified; or a second key in the launcher that
   sends the copy key first, for the programs where that is safe.
5. **Which axKit fields take templates** (TP-5.1): every field to write in, as specified; or only the editors, the
   message field of axClaude and the command field of axTerminal.
6. **The browser** (TP-8, TP-D8): a navigator of axTemplate's own, as specified; or axFile on the templates
   folder. And in the browser: the template's text as the second pane, as specified; or fields, as an item of
   axTask has (its name, where it shows, its text), changed in a form.
7. **Which window a plain start shows**: the browser, as specified, with the launcher on the hotkey alone; or the
   launcher, as axClip does.

## 8. Steps, each a piece of work of its own once he has read this

1. The store and the finding, pure code with tests; the editors read the folder `every axKit app` too (TP-5.2).
2. The launcher: the window on the hotkey window, Enter with the paste, Shift+Enter, Ctrl+Q; its scripts, an NVDA
   pass.
3. The clipboard given back and the mark against histories (TP-4.2); the caret by Left keys (TP-2.3).
4. The browser on the navigator: places, templates, the text pane; new, rename, delete, Show in; its scripts, an
   NVDA pass; the user guide.
5. The templates in every axKit window's palette (TP-5.1), with the code moved from the editor shell to the base.
6. axAutomate's step (TP-6).

## 9. Status

Specified, not built. What exists: the marks and their code (`AxKit.Core/TextTemplate`), the templates folder
(`AppPaths.TemplatesFolder`), and the editors' Insert template menu over their own folder (AD-13), built
2026-11-23.
