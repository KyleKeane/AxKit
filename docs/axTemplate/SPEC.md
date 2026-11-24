# axTemplate — Specification

axTemplate is an app of the axKit bundle (`docs/axKit/SPEC.md`): the user's templates, put in wherever the caret
is, in any program. It has two windows: a launcher behind a system-wide hotkey, as axClip is one, and a browser
where the templates are made and changed, as axTask is one. Asked by the maintainer on 2026-11-23, specified that
day, and rewritten the same day on his answers to its open questions (section 7); built in the steps of section 8,
and section 9 says how far. Requirements are numbered `TP-n.m` and decisions `TP-Dn`; the bundle's rules are cited
by number and not repeated; axClip's specification (`docs/axClip/SPEC.md`, CP-n.m) and the item apps' page
(`docs/axKit/item-apps.md`, IA-n) are cited for what axTemplate shares with them. In axTemplate's code and
documents, "SPEC.md" means this file.

The maintainer's words, 2026-11-23: "we should abstract the axTemplate as it's own app that can inject things
anywhere on the system like autohotkey ... i want this wider axtemplate app to be available as a global gotkey with
a simple launcher like axclip that uses pattern matching for me to find templates by name or content and then
inject it into wherver the cursor was sitting in the prebious app. and then the option to load templates as
available in the axkit command palette could be considered. axautomate could likely be integrated with axtemplate in
a celever way where an automation step could be the insertion of a template and content into it." And: "axTemlate
will have the simple injection interface like axclip. but we also need to define the browser that will be like
axtask." And, on how a template is kept: "i do want the template to be bery simple, but i also think we should
usejson to keep things just like axtask ... i want you to be able to reuse as much as you can".

## 1. Summary

A template is a small item the user wrote once: a name, a text, and where it shows. The hotkey brings up one search
field over all of them. Typing narrows the list by the words of a template's name or of its text; Down and Up move
through it with the caret kept in the field, as in axStart and axClip; Enter puts the template into the program
that was in front, at its caret. That is the launcher. The browser is the app's other window: the templates as a
list, the fields of the one you are on beside it, and the commands that make, rename, change and delete them. The
same templates show in the command palette of the axKit apps the user checks for them. Nothing is spoken but the
answer to a key.

## 2. Goals and non-goals

- **G1 Any program.** A template reaches every program that takes a paste: a browser's form, a mail, a terminal, an
  axKit window.
- **G2 The bundle's two shapes, nothing of its own.** The launcher is the hotkey window (`HotkeyForm`), with the
  field and the keys of axStart and axClip. The browser is the item window (`ItemForm`), with the list, the fields
  and the keys of axTask and axPass.
- **G3 The item format, reused.** A template is a JSON item of the bundle's one item format (IA-4,
  `docs/axTask/forms-framework.md` sections 11 and 13): a file of names and values, read through its folder's
  `list.json`. The browser changes it in the item window's own ways; axJSON opens it for whoever wants the file;
  axTask could open the folder as a list.
- **G4 One store for the bundle.** The editors' templates of `docs/axDown/SPEC.md` AD-13 are these items.
- **Non-goals, for now** (the maintainer paused them on 2026-11-23): placeholders to fill and a key that finds the
  next one; a form behind a template; a text expander that watches what is typed. Also: formatted text, pictures,
  typing a template key by key, sync between computers.

## 3. Scenarios

1. In a browser's comment field, Ctrl+Shift+Semicolon: "axTemplate window, Search templates, edit". Type
   `thanks`: after the pause, "Thanks for the report, 1 of 2". Enter: axTemplate hides, the browser is in front
   again, and the text stands in the field.
2. In a mail, select a name, then the hotkey, `intro`, Ctrl+Enter: axTemplate copies the selected name from the
   mail, and the template "Introduction", which holds `{selection}` where the name goes and `{caret}` where the
   first sentence starts, replaces it; the caret waits there.
3. The hotkey, type `sprint`: no template is named so, and one holds the word in its text: "Weekly notes, 1 of 1".
   Ctrl+Q reads the whole template in a notice before it is used.
4. Window menu, New window, axTemplate: the browser, "Templates, 12". Ctrl+N, a name, Enter: the new template is
   in the list. Tab to its fields, Enter on Text: write it. Space on "axDown": the template is in axDown's command
   palette from now on.

## 4. Functional requirements

### TP-1 Launch, the background, the hotkey

- TP-1.1 **A plain start** (`axKit template`, axKit, Apps, axTemplate, or its key) shows the browser (TP-8), and
  starts the launcher in the background when it does not run yet, so that the hotkey works from then on (his
  answer: "The browser").
- TP-1.2 **The launcher** is as axClip (CP-1): a background process with one hidden window, one copy at a time,
  brought up by its hotkey, hidden by Escape, Alt+F4 and the focus going to another program; only Quit ends it.
  `axKit template --launcher` shows its field (a second start hands "show" to the running one);
  `axKit template --hidden` runs it hidden, which is what a sign-in start and the browser's start of it are.
- TP-1.3 The hotkey is the user's to change (Options, Hotkey). At first **Ctrl+Shift+Semicolon** (TP-D1).
- TP-1.4 **The hotkey is on for everyone after an installation and after an update** (the maintainer's word of
  2026-11-23: "you should automentically enable the axtemplate global hotkey for all users on installation and
  update"), with nothing for the user to start first. Options, General, Start with Windows, in the launcher's
  window, is the one switch that says no; it is on at first.
  - The copy from GitHub: the installer starts the launcher, hidden, after every installation and update, as it
    starts axStart, axClip and axHelp; the launcher's first run writes its value under the Run key, so Windows
    starts it at every sign-in from then on.
  - The Store's copy: axTemplate has no entry of its own (his answer: "No entry of its own": the package's eight
    are taken, bundle AX-13.2), and so no startup task. axKit starts the launcher itself there (bundle AX-13.4b):
    with the first start of any app after the installation, with the first start of any app after an update, and
    at every sign-in, when the startup task of axStart, axClip or axHelp starts its app.
  - Quit ends the launcher until the next of those starts, or until axTemplate is started (TP-1.1).

### TP-2 The templates

- TP-2.1 **One folder**, `Documents\axKit\Templates` (the user's Documents; bundle AX-5.11, the maintainer's
  decision of 2026-11-23: what the user writes is where the user finds it, and where an uninstall of the Store's
  copy does not take it; first built under AppData, never released there), a list in the item format: its `list.json` (`axkit-list 2`)
  names the fields, and every other `.json` file in it is one template, an item that names `list.json` as its
  configuration. axTemplate writes `list.json` when it is not there and adds a field to it that a newer version
  brings; it never takes one away.
- TP-2.2 **A template's fields**: `Title`, its name and its file's name; `Text` (a long text), what is put in;
  then one check for each place it also shows inside axKit (TP-5), one for each app
  whose window has a field or a text that Insert template puts a template into (TP-5.2), by the app's name (his
  answers: "A check per app", "Every app with a field"): axDown, axJSON, axCalc, axTerminal and axClaude, in the
  bundle's order (`TemplateStore.AppsWithField`). Built first with axTask, axPass and axMCP too; their fields are
  the fields of notices, which the palette does not reach, so their checks would have shown a template nowhere. The launcher lists every template whatever its checks. A value in a template's file that no field
  names is kept as it is, and the pane says so in a line after the fields.
  The check for every axKit app that came first until 2026-11-23 is retired (the maintainer: "every axkit app should be
  a button that checks all items in the list of checkboxes"): in the form the checks are one list with Check all
  before it (bundle AX-8.10 rule 19); a template whose file still has it checked shows in every app, and the
  folder's configuration drops the field (`TemplateStore.ConfigurationToWrite`).
- TP-2.3 **The marks** in a template's text are `docs/axDown/SPEC.md` AD-13.2's, filled by the same code
  (`AxKit.Core/TextTemplate`): `{caret}`, `{selection}`, `{clipboard}` (his answer: "Yes, keep all three"). In an
  axKit window all three mean what they mean in an editor. From the launcher, in another program:
  - `{clipboard}` is the clipboard's text at the moment the launcher came up.
  - `{selection}` is the text selected in that program. With Enter it is read through UI Automation, with no key sent, where the field gives its text (the maintainer, 2026-11-23: "{selection} is not working as expected"), and is empty elsewhere; with Ctrl+Enter it is copied with the copy key first (TP-4.2), for a program that gives no text that way. Either way the paste then stands in the selection's place.
  - `{caret}`: the caret is put in its place after the paste (TP-D10). Where the field gives its text to UI
    Automation, the channel a screen reader reads by (Word, Notepad, a browser's field), the text is pasted with
    the mark `{caret}` left in, the mark nearest before the caret is searched for until the paste has put it
    there, selected, and deleted with the Delete key, which leaves the caret where it was. Elsewhere (a terminal,
    a program that gives no text), the caret is moved back by one Left key for every character that follows the
    mark, a line ending counting as one (TP-D5). Before any key is sent, the launcher waits until Ctrl, Shift, Alt
    and the Windows keys are up, 1.5 seconds at most.
- TP-2.4 A file that is not an item, or cannot be read, is no template: the browser lists it with "not a
  template" after its name and the launcher leaves it out.
- TP-2.5 The folder is read off the UI thread, as each window comes up and on F5; the launcher reads a template's
  file again when it is used, so that a change made in the browser a moment ago is in.

### TP-3 The launcher's window

- TP-3.1 The bundle's hotkey window: the field "Search templates" with the caret always in it, and under it the
  list "Templates, N", never focused (`AxKitList` in its mode under a field, framework FW-4.12).
- TP-3.2 **Finding.** Every word typed must be found, in any order and without regard to case, in the template's
  name or in its text. Templates whose name holds all the words come first, in the order of their names; then
  those that hold a word only in their text. An empty field lists every template, the ones used last first.
- TP-3.3 A line of the list is the template's name. What is said when the selection reaches it is the name and its
  place, "Introduction, 1 of 12". After a pause in the typing the first match is said, as in axStart and axClip
  (the setting Say the first match while typing).
- TP-3.4 Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End move through the list from the field, with the edge
  sound at an end and the sound of nothing found when nothing matches (the shared list's).

### TP-4 Using a template from the launcher

- TP-4.1 **Enter puts it in**, axClip's way (CP-4.1, CP-D3): the marks are filled, the text is put on the
  clipboard, the window hides and gives the front back, and when the program that was in front is in front again
  the paste key, Ctrl+V, is sent to it; then the Left keys of TP-2.3. When another window is in front by then,
  nothing is sent and the text stays on the clipboard.
- TP-4.2 **Ctrl+Enter takes the selection first** (his answer: "A second key for it"): the window hides and gives
  the front back, the copy key, Ctrl+C, is sent to the program that was in front, what it puts on the clipboard is
  the template's `{selection}`, and then the template is put in as with Enter, in the selection's place. Not for
  a terminal, where Ctrl+C ends the running command: the choice is the user's, each time (TP-D4).
- TP-4.3 **The clipboard afterwards.** The template's text is put on the clipboard marked as not for a clipboard
  history, the mark password managers use, so that axClip and Windows' own history do not keep it. When the
  clipboard held plain text before, that text is put back once the paste is done (TP-D6). Where the caret's mark
  cannot be found, "done" is a wait after the paste key that the user chooses: the launcher's Options, Wait
  before the clipboard comes back, 0.3, 0.6 (the default), 1, 2 or 4 seconds, saved as `giveBackMs` (the
  maintainer's answer of 2026-11-24, "a setting for it"); a choice is said, "The clipboard comes back 2 seconds
  after a template is put in".
- TP-4.4 Shift+Enter puts the filled text on the clipboard without pasting, and the window hides; nothing is
  said, as in axClip (CP-4.2, the maintainer's choice of 2026-11-23: a word said as the window hides is never
  heard, and coming back is the sign). For a
  program whose paste is another key.
- TP-4.5 Ctrl+Q shows the template's own text, marks and all, in a notice titled with its name and its lines, read
  with the arrows; Escape closes it.
- TP-4.6 Ctrl+B opens the browser (TP-8); the launcher hides. The launcher itself makes, changes and deletes
  nothing: it finds and puts in.
- TP-4.7 A command with no template says "No templates yet: Ctrl+B opens the browser, where one is made" or the
  no-match line.

### TP-5 In the axKit apps

- TP-5.1 **The command palette.** In a window of an axKit app that has a field to write in, the palette lists the
  templates checked for that app or for every axKit app, each as a command "Insert template" with its name, in a
  menu (Actions, Insert template), since the palette holds commands only (bundle AX-10.5). Enter puts the template
  in at the caret of the window's field, in the place of its selection, with all three marks, as an editor does
  (AD-13.2), the focus in the field, and says "<name> inserted". The templates are read off the UI thread as the
  window comes up and each time it is active again; a template's file is read again when it is chosen. A field that
  is not there at that moment (a question of the program stands in its place) says "No field to put a template
  into now" (`AxKitForm.Templates.cs`, `TemplateField`).
- TP-5.2 **Which apps** (his answer: "Every app with a field"): the editors, axDown and axJSON, in their text;
  axClaude in its message field and axTerminal in its command field; axCalc in its expression field (the
  conversation windows' field at the bottom); and the forms of axTask, axPass and axMCP, where text is written
  (TP-5.5). Each has its check in a template's form (`TemplateStore.AppsWithField`). The fields of other notices
  are reached by the launcher, Ctrl+Shift+Semicolon, which puts a template into whatever field had the focus,
  axKit's own too.
- TP-5.5 **In a form** (the maintainer's answer of 2026-11-24, "Insert template in forms too"; until then the
  launcher alone). A form is a notice, where the palette does not open and no command of the bar is offered (bundle
  AX-10.1, AX-8.12), so the templates are on the field itself: in a form of an app of TP-5.2, Shift+F10, the
  Applications key or a right click in a text field opens a menu of the templates checked for that app, numbered
  as Insert template numbers them, and the one chosen is put in at the caret, the selection and the clipboard
  filled as TP-5.1 fills them, "Thanks inserted" said. With none, the one line "No templates of your own for axTask
  yet: axTemplate makes them". This menu stands in the place of Windows' edit menu of the field, whose commands are
  all keys; a field only read, or one that holds a password, keeps Windows' menu and gets no templates. The folder
  is read again off the UI thread as the form opens (`AxKitForm.OfferTemplatesInForm`). Checked in the NVDA pass,
  `tests/nvda-scripts/axTask-template-in-a-form.txt`, since a context menu opens only in the window in front.
- TP-5.3 The editors keep their own templates (AD-13.3: axDown's Link, Table and Code block) before the user's.
- TP-5.4 A template is put into a field and never sent: in axTerminal's command field and axClaude's message
  field the user reads it and presses Enter, as for any text put there.

### TP-6 axAutomate

- TP-6.1 A step of a workflow, `template <name> [| <selection> [| <clipboard>]]`: the template's name, and
  optionally the text for `{selection}` and for `{clipboard}`. The step fills the marks and types the text into the
  window the workflow drives, where its focus is, as `type` steps do with a text of their own, its lines with Enter
  between them, then the Left keys that put the caret at `{caret}`. So a workflow can put a template in and fill
  what it carries (`AxAutomate.Core/TemplateStep`, docs/axKit/automation.md). The templates are those of the run's
  home, `home\Documents\axKit\Templates`, which a script's `file` steps write, as every file a run uses is its own.
- TP-6.2 A template named in a workflow and not found fails the step with its name ("No template is called
  Thanks"), as a missing file does, and nothing is typed.

### TP-7 Menus, keys, texts, settings, log

- TP-7.1 Each window's menu bar is the bundle's (AX-9.1). The keys are one table (`AxTemplateKeys`) beside the
  bundle's, the launcher's and the browser's in it; F1 gives the keys of the window it is pressed in; the user
  guide is `docs/axTemplate/user-guide.md`.
- TP-7.2 Settings (`%APPDATA%\AxKit\axTemplate.json`): the hotkey, Start with Windows, Say the first match while
  typing (the hotkey windows' three), the names of the templates used last, and the browser's window place.
- TP-7.3 The log holds that a template was put in, never its name or its text (bundle rule 6). axTemplate makes
  no network request (rule 8). What it does outside its windows is what axClip does: the clipboard, and keys sent
  to the program in front (the paste key, the Left keys, and with Ctrl+Enter the copy key), through the one
  place that sends keys (`AxKit.Forms/KeySender`). Its program starts are the bundle's own, of another axKit
  window (the launcher by the browser, the browser by the launcher, axJSON on a template's file), which
  `docs/axKit/outside-calls.md` lists under the Window menu's start.

### TP-8 The browser

- TP-8.1 **A window of the navigator type** (`ItemForm`; framework FW-4.6 to FW-4.10a; his answer: "Own window,
  same format"), a window like any other: it shows in the taskbar and the Window menu, and Alt+F4 closes it.
- TP-8.2 **One level**: the templates, "Templates, 12 templates", each row a template's name, in the order of the
  names; a file that is no template (TP-2.4) is among them by its file's name. An empty folder shows the one line
  "No templates yet: Ctrl+N makes one". Filter and Sort by are the navigator's (bundle AX-8.17); Space selects
  (AX-8.16); type-to-find and the edge sound are the list's.
- TP-8.3 **The second pane is the template's fields** (the navigator's pane of fields, IA-2.3): Title, Text, and
  the checks of TP-2.2, for reading. Tab goes to it and back. Enter on a field opens the template's form in that
  field (TP-8.3a); a check also changes with Space, Right and Left in place.
- TP-8.3a **The template's form** (the maintainer, 2026-11-23: "axtemplate should work like axtask in that enter on
  any template should open a form with all fields. axtemplate should have a read only preview in the details pane
  like axtask. enter on an item in the details view should just open the form and can place the focus on that
  element in the list"): Enter on a template opens one form with every field, the item window's form, the same as
  axTask's (bundle AX-8.10 rule 17): the name, the Text in a box of several lines, a check box for each place.
  Ctrl+Enter or Save writes every change as one step, which Ctrl+Z takes back; a new name renames the file, and a
  name another template has is refused in the form, the focus in the name. Escape leaves, nothing changed.
- TP-8.4 **On a template**: Enter opens its form (TP-8.3a). Ctrl+N opens the same form, empty (the maintainer, 2026-11-23: "adding a new template should open the full form
  rather than the smaller naming dialog"): Add makes the template with everything typed in it and selects it; a
  name another template has, whatever its capitals, is refused in the form, the focus in the name. F2 renames it in place (bundle AX-8.7), and so does its form's name; its file
  is renamed with it. Delete (Template, Archive) moves the template, or the selected ones, into the folder's
  `Archive` folder after a question that names it or their count (TP-D9); a name the Archive has already gets a
  number. Neither the browser nor the launcher reads the Archive; axFile reaches it, and a file moved back is a
  template again. Ctrl+Z takes back the last change, whichever it was: a field, a rename, a new template, an
  archiving; up to fifty, for as long as the window is open, and never over a file that changed on disk since
  (item-apps IA-5.4). Ctrl+Enter puts the template's text on the clipboard, marks filled with nothing, "Copied",
  for a look at what it makes. Ctrl+J opens the template's file in axJSON, and Session, Show the templates'
  folder in, opens the folder in File Explorer or axFile.
- TP-8.5 The browser reads the folder off the UI thread (the navigator's read, framework FW-4.7 point 1), again on
  F5 and when the window is active again, where the list is shown again only when the folder changed. A change
  is made only to a file that is still as it was read: otherwise "The template changed on disk; it is read
  again", and nothing is changed (IA-5.5). For the AI tools, reading is free; making, renaming, changing and
  deleting a template are never run for an AI tool (TP-D7): their menu items are withheld from the AI tools.

## 5. Architecture

- `src/AxTemplate.Core` and `src/AxKit.Core`: the store (`AxKit.Core/TemplateStore`, shared since step 4: the folder as a list of the item format, its `list.json` with
  the fields of TP-2.2, a template read from and written to its item through `AxKit.Core`'s `ListConfig`,
  `FlatItem` and `ItemView`), the finding (`TemplateMatch`: words against name and text, the order), the count of
  Left keys after a paste, and the settings; all pure and unit tested. The marks stay `AxKit.Core/TextTemplate`.
- `src/AxTemplate`: two windows, each on a window type of the bundle. The launcher on `HotkeyForm`, as axClip's:
  the field, the list, Enter and the paste (`ForegroundBefore`, keys through `SendInput`), the notices. The browser
  on `ItemForm`, as the item apps': `ReadLevel` gives the templates, `DetailOf` an item's document, `SaveFields`
  writes it.
- The window base holds the templates of TP-5.1 (`AxKitForm.Templates.cs`, moved from the editor shell on its
  second use, rule 4): a window names its field (`TemplateField`), the editor shell puts a template into its text.
- axAutomate's step is one more line of its step table.

## 6. Design decisions

- **TP-D1 Ctrl+Shift+Semicolon** (the maintainer's choice and swap, 2026-11-23). Every system-wide key of axKit is
  on Ctrl+Shift with a punctuation key or Space (bundle AX-8.11): Space is axStart's, Slash and Comma axHelp's.
  He first chose Apostrophe, the key to the right of axClip's Semicolon; the same day, after using it, he swapped
  the two: "i think templates should be ctrl+shift+; since it will be used more". axTemplate has Semicolon, axClip
  Apostrophe; a settings file of either that holds its old default is taken over to the new one as it is read.
  Ctrl+Shift+Period stays kept and unused.
- **TP-D2 A template is an item; where it shows is its checks.** First specified as plain files, with the folder a
  file lies in saying where it shows. The maintainer wanted JSON, as axTask, and the most reuse: an item brings
  the browser's fields, forms, undo and file handling with it, and a check per app says where it shows, in the
  file itself, with nothing to go stale. The price: a template's text is a JSON string, with its line endings
  written as `\n`, for whoever opens the file in an editor; the browser's Text field hides that.
- **TP-D3 Paste, not typed keys.** As axClip (CP-D3): pasting is the program's own command and takes the whole text
  at once, whatever its length and its characters. Typing key by key is slow, breaks on a keyboard layout, and in a
  terminal runs every line as it is typed.
- **TP-D4 The copy key only on Ctrl+Enter.** Reading another program's selection means sending it Ctrl+C. In a
  terminal that ends the running command, and everywhere it replaces what was on the clipboard for a moment. So
  Enter never sends it, and Ctrl+Enter is the user's word that it is wanted here.
- **TP-D5 The caret by Left keys.** The one way that works in a plain field of any program. It is exact where the
  program puts in what was pasted and nothing more; a program that indents or closes brackets by itself can land
  the caret a little off, and the guide says so. A template without `{caret}` sends no Left key.
- **TP-D10 The caret found by its mark** (the maintainer, 2026-11-23: the caret was "unreliable", then "can you
  instead insert the full template and then search for each pattern to select it and replace it with the
  clipboard or place the caret there by deleting the content that is there?"). The Left keys of TP-D5 went wrong
  two ways: a key the user still held (Ctrl from the hotkey or from Ctrl+Enter) made every Left a Ctrl+Left, a
  word, and a program that pastes slowly took the Left keys before the paste. A program's own Find cannot be driven
  reliably, each has its own; UI Automation's text of the focused field can be searched in every program that
  gives it, which is the one NVDA reads, so the mark is found there (`AxTemplate/FocusedText`, the client's
  interfaces declared in code, no package). The mark's arrival also tells that the paste is in, so the clipboard
  goes back then. `{clipboard}` and `{selection}` are filled before the paste, as they were; a mark in the text
  after the caret (from the clipboard) sends the Left keys instead, and a mark not found within three seconds stays
  in the text, where the user finds it, and is logged.
- **TP-D6 The clipboard is given back.** A template put in should not cost the user what they had copied, and
  should not fill a clipboard history with templates. What is given back is the text the clipboard held when the
  launcher came up, so the copy that Ctrl+Enter makes in the other program is undone with it; a clipboard that
  held a picture or files then is not given back (the launcher takes only its text). The other program is asked
  to paste and cannot say when it has, so the text goes back after a wait, 0.6 seconds after the paste key unless
  the user chose another (TP-4.3): a program slower than that pastes the old text, and the guide names Shift+Enter for such a one. The
  wait is the same every time, not a guess at the program (the maintainer's rule of no hidden thresholds is
  about behaviour the user sees differing by a measure; this is one fixed step). Open to his ear and his use.
- **TP-D7 A template is the user's own text.** axTemplate reads no template from outside its folder, takes none
  from an AI tool, and runs nothing: it pastes. A template of several lines pasted into a shell runs them, as any
  paste of several lines does; Ctrl+Q reads a template before it is used.
- **TP-D8 Two windows, one app.** The launcher and the browser do two jobs: one is called in the middle of other
  work and must be gone at once, the other is sat in. The bundle has both shapes, and an app with one window of
  each is new: until now every app is one window type (framework section 5). They are no second window in the
  sense of bundle AX-7.2, which rules out dialogs of an app's own: each is a whole window of a bundle type, as
  axHelp's small window and its conversation are.
- **TP-D9 Delete archives, and Ctrl+Z brings back** (the maintainer's answer of 2026-11-24: "make an archive
  directory just like we do with axtask so old items can be recovered"). The specification first said the Recycle
  Bin; the first build (2026-11-23) deleted the file, with Ctrl+Z for as long as the window was open. Now the file
  is moved into `Archive` beside the templates, one move of `AxKit.Core/Changes`, so that the same Ctrl+Z takes it
  back with the check that nothing changed on disk since, and after the window closed the file is still there to
  move back in axFile. The undo of a move out of the folder reads the folder again (`TemplateStore.MovesAcrossFolders`).

## 7. The maintainer's answers, 2026-11-23

Asked one at a time through the question tool, after he had the first specification.

1. Folders or flags for where a template shows: "i think we should usejson to keep things just like axtask"; then,
   on how far the reuse goes for the browser: "Own window, same format".
2. How the field for where it shows works: "A check per app".
3. Which apps have a check: "Every app with a field".
4. The hotkey: "Ctrl+Shift+Apostrophe"; swapped with axClip's Semicolon the same day, at his word (TP-D1).
5. The selection of another program: "A second key for it".
6. The Store's copy: "No entry of its own".
7. Which window a plain start shows: "The browser".
8. The marks: "Yes, keep all three".
9. What next: "Rewrite the spec, then build".

## 8. Steps

1. The store and the finding, pure code with tests; the browser on the item window: the list, the fields, new,
   rename, delete; its scripts, an NVDA pass.
2. The launcher on the hotkey window: finding, Enter with the paste, Shift+Enter, Ctrl+Q, Ctrl+B; the plain start
   that starts it; its scripts, an NVDA pass.
3. Ctrl+Enter with the selection; the clipboard given back and the mark against histories; the caret by Left keys.
4. The editors read the items (AD-13.4 changes from plain files to these).
5. The templates in every axKit window with a field (TP-5), with the code moved from the editor shell to the base.
6. axAutomate's step (TP-6).

## 9. Status

All six steps are built (2026-11-23). Step 1: the store, the finding and the settings, with their tests (the store
is `AxKit.Core/TemplateStore` since step 4), and the browser, `BrowserForm`, which a plain start shows. Step 2: the launcher, `LauncherForm`,
behind Ctrl+Shift+Semicolon: the finding, Enter with the paste, Shift+Enter, Ctrl+Q, Ctrl+B, and the plain start
that starts it hidden, with the hotkey on for everyone after an installation and an update (TP-1.4). Step 3:
Ctrl+Enter with the other program's selection, the template on the clipboard marked against histories
(`AxKit.Forms/PrivateClipboard`) and the clipboard's text of before given back 550 ms after the paste key, when
nothing else was copied since, and the caret moved back to `{caret}` by Left keys 150 ms after the paste key
(`AxKit.Forms/KeySender`). The window checks are `tests/scripts/axTemplate`; what only a real front window shows
(the paste, the caret, the clipboard, the selection) is `tests/nvda-scripts/axTemplate-put-in.txt`. Step 4: the
editors' Insert template (axDown, axJSON; docs/axDown/SPEC.md AD-13.4) lists the templates checked for the app or
for every axKit app, in place of the plain files they read before, and Options, General, Templates in axTemplate
opens the browser. Step 5: the conversation windows (axClaude, axTerminal, axCalc) have Actions, Insert template
for their field, the code moved from the editor shell to the window base (`AxKitForm.Templates.cs`); axCalc's
script `05-templates.txt` checks it. Step 6: axAutomate's step `template <name> [| <selection> [| <clipboard>]]`
(`AxAutomate.Core/TemplateStep`), which the same script uses. The marks and their code are `AxKit.Core/TextTemplate`, shared with the editors.
