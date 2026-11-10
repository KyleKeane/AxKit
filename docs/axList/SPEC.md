# axList — Specification

axList is a planned app of the axKit bundle (`docs/axKit/SPEC.md`): lists of things to remember, kept as plain
Markdown files, for a blind developer who uses the NVDA screen reader. It is the list side of the pair the plan calls
axList and axTask (`docs/axKit/plan.md`, Still to come). The pair was first built as one app, axList with a Tasks view,
on the branch `axList` (2026-10-23 and 24, LI-1 to LI-8 with their tests, never heard under NVDA); on 2026-10-24
axTask was designed anew as an item app, one JSON file of typed fields per task (`docs/axTask/SPEC.md`,
`docs/axKit/item-apps.md` IA-D1), built and merged, and the Markdown design was set aside. On 2026-11-06 the user asked
to keep axList and axTask in view as two separate apps, so this document came into `main` from the branch, brought up
to the bundle as it is today, and the branch's code was kept as the tag `archive/axlist-draft` (section 11). **Nothing
of it is built on `main`.** Requirements are numbered `LI-n.m` and decisions `LI-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axList's code and
documents, "SPEC.md" means this file. Before the build, section 10 is to be settled with the user, and every citation
of the bundle spec checked again.

## 1. Summary

A list is a Markdown file in a folder of lists: `Shopping.md`, `Ideas.md`, `Course plan.md`. axList shows the folder's
lists and a list's items one per line of a list box: bullet, numbered and checkbox items, nested as deep as wanted.
Enter changes an item's text, Ctrl+N adds one, Alt and the arrows move and indent it, Space checks a checkbox item off.
Every change is written to the file at once, as Markdown that any editor, script or AI system reads and changes as
easily as axList does (LI-D2): the user's rule is "plain text files on disk with a clear, documented format; no
database the user cannot open in axDown".

What sets it apart from axTask: axTask is for things to do, each a file of typed fields (done, due date, priority,
notes), with All tasks and smart lists across every list. axList is for things to remember and to arrange: outlines,
ideas, plans, reading lists, where the order and the nesting carry the meaning and the whole list is one readable text.
A checkbox in a list is a convenience of Markdown, not a task manager (LI-D1).

## 2. Goals and non-goals

- **G1 Lists to remember.** Bullet, numbered and checkbox items, nested, in as many lists as wanted.
- **G2 The file is the truth** (LI-D2). The format is section 5; a line axList does not understand is kept as it is;
  a file changed by another program is read again before every change.
- **G3 The bundle's way.** One list box at a time, the state in the words ("Buy milk, task, not done"), nothing spoken
  on an arrow key, every change one announcement (AX-7).
- **G4 Room for links** (LI-D3). Links between items, for mind maps, curriculum planning and recall in plain language,
  are the later step the plan names; the format leaves them room.
- **Non-goals.** Due dates, reminders, recurring tasks, smart lists and priorities: axTask's. Sync and sharing: the
  user's own sync client's, as every file of the bundle (B-15).

## 3. Scenarios

1. Start axList: "Lists - axList window", then "Lists, 3 lists, Shopping, 6 items, 1 of 3". Enter: "Shopping, 6 items
   list, Heading: Shopping, 1 of 6". Down: "Milk, task, not done".
2. Space: "Done: Milk". Space again: "Not done: Milk".
3. Ctrl+N, type `Bread`, Enter: "Added Bread" after the item the selection was on, a task like it.
4. Alt+Down: "Moved down: Bread". Alt+Right: "Indented: Bread, level 2".
5. In axDown, the user adds `- Eggs` to `Shopping.md` and saves; back in axList, F5 or the next change reads the file
   again, and Eggs is there.

## 4. Functional requirements

### LI-1 Launch and the folder of lists

- LI-1.1 `AxKit.exe list [<file or folder>]`; the console command `axList`; the Start menu entry "axKit axList";
  axList under "Open with axKit" on `.md` files (AX-2.3). A `.md` file opens its folder as the folder of lists and the
  list itself; a folder becomes the folder of lists; without a path, the folder of the last run.
- LI-1.2 The folder of lists is `Documents\Lists` at first, made when axList first writes a list there. Options,
  Lists folder… (the Windows folder picker) changes it; the choice is kept (LI-8.1).
- LI-1.3 `AxListApp.Run(args)` is the entry point (AX-1.7): the host's log (`axList.log`), crash handling, `--help`
  and `--version`, the settings (`axList.json`), then `ListForm`.

### LI-2 The window and its views

- LI-2.1 From the top: the menu bar; one list box, the bundle's `AxKitList`, named with its view and count, with
  type-to-find (Shift with a letter backwards) and the edge earcon at its ends built in (AX-8.6); the status bar. A
  notice takes the list box's place while it shows (AX-6.4).
- LI-2.2 Views (View menu): Ctrl+1 **Lists**, the folder's lists; Ctrl+2 **Items**, the open list's items; Ctrl+3
  **Tasks**, the open checkbox items of every list, if section 10 keeps the view. The key says the view's name and the
  selected item, also when the view already shows (axClaude D26); Ctrl+2 with no list open says "No list open: Ctrl+1,
  then Enter". Tab and Shift+Tab go to the next and the previous view, passing over the Items view while no list is
  open (axProcess's way, 2026-10-24).
- LI-2.3 The title is "<list name> - axList" while a list is open and "Lists - axList" without one. The status bar's
  left label is the view's name and count; the right one the folder of lists. No AccessibleName, no Spring.
- LI-2.4 F5 reads the folder and the open list again and says "Refreshed: <the list box's name>", keeping the
  selection on the same item (by its line and text, LI-6.2).
- LI-2.5 Escape and Backspace in the Items view go back to the Lists view, on the list that was open.

### LI-3 The Lists view

- LI-3.1 One item per `.md` file of the folder, by name in File Explorer's order (`NaturalOrder`), "<name>, <n> items"
  ("<name>, <n> items, <m> open tasks" when it has checkbox items not done; "<name>, empty").
- LI-3.2 Enter opens the list in the Items view. Ctrl+N asks for a name ("New list", the field "Name of the new list")
  and makes `<name>.md` with the line `# <name>` and opens it: "Made <name>". A name with a character Windows does not
  allow in a file name, or the name of a list that exists, is refused with the reason.
- LI-3.3 Ctrl+E shows the list's file in axFile, selected (deleting and renaming lists is axFile's in the first
  version); List, Open in axDown opens it there.

### LI-4 The Items view

- LI-4.1 One item per line of the file that is not blank, in the file's order: an item is "<text>, task, not done",
  "<text>, task, done", "<number>. <text>" (numbered), or "<text>" (a bullet); a heading is "Heading: <text>"; any
  other line is its text. A nested item ends in ", level <n>" (level 1 is not said).
- LI-4.2 Space on a checkbox item checks it ("Done: <text>") or unchecks it ("Not done: <text>"); on another item it
  says "Not a task: Ctrl+T makes it one".
- LI-4.3 Enter on an item asks for its text ("Change item", the field "New text of the item" with the text in it);
  Enter in the field writes it ("Changed <text>"), an empty text is refused ("Type the item's text"). On a heading or a
  text line, Enter changes that line's text the same way.
- LI-4.4 Ctrl+N asks for a new item's text ("New item", the field "Text of the new item") and adds it after the
  selected item and its nested items, at the selected item's level and of its kind (a checkbox when it is one,
  numbered when numbered), or as a bullet at the end of an empty list: "Added <text>". Numbered items are numbered
  again from 1 at their level after every change.
- LI-4.5 Delete removes the item and its nested items: "Removed <text>" ("Removed <text> and 2 nested items").
- LI-4.6 Alt+Up and Alt+Down move the item with its nested items past the item before or after it at the same level:
  "Moved up: <text>", "Moved down: <text>", "Top of the list" or "Bottom of the list" at the ends (with the edge sound,
  `Sounds.Edge`). Alt+Right indents an item one level under the item before it ("Indented: <text>, level 2"), never
  more than one level deeper than that item; Alt+Left takes it out one level ("Outdented: <text>"). Headings and text
  lines do not move or indent ("Only list items move").
- LI-4.7 Ctrl+T turns a bullet or numbered item into a checkbox item (not done) and a checkbox item into a bullet:
  "<text> is a task", "<text> is not a task".
- LI-4.8 Ctrl+Shift+D hides the done checkbox items of the Items view, and shows them again: "Done tasks hidden",
  "Done tasks shown"; the choice is kept (LI-8.1). The list box's name says "N items, M done hidden".
- LI-4.9 Ctrl+Z undoes the last change to the open list, one step at a time, ten steps back at most, while the window
  is open: "Undone: <what>" ("Undone: Done: Milk"); "Nothing to undo" otherwise. Undo writes the file as it was before
  the step, if the file has not changed on disk since; otherwise it says "The list changed on disk, so the last step
  cannot be undone". To check at the build: whether the editors' `EditHistory` (AD-2.5) serves here unchanged.
- LI-4.10 Ctrl+C copies the item's text ("Copied"); Ctrl+F finds an item by its text, F3 and Shift+F3 the next and the
  previous (the bundle's find in a list).

### LI-5 The Tasks view (open, section 10)

- LI-5.1 Every checkbox item that is not done, of every list of the folder, in the order of the lists and of their
  files: "<text>, in <list name>". The list box's name is "Tasks, N open" ("Tasks, none open").
- LI-5.2 Space checks the item off in its file ("Done: <text>"); the item leaves the view, and the selection goes to
  the next one. Enter opens its list in the Items view on that item. Ctrl+Z undoes as in the Items view, for the list
  the last step changed.

### LI-6 The file, read and written

- LI-6.1 A list is read in full from its file when it opens, on F5, and before every change; the change is made to
  what was read, written to the file at once (`TextDocument`, the editors' reader and writer, keeping the encoding,
  the byte order mark and the line ends), and the view is shown again from it. The reads and writes are small and run
  on the UI thread's turn of the command, never while a key is held.
- LI-6.2 A change acts on the line it was asked for: when that line's text is no longer what the window showed (the
  file changed on disk), nothing is written and the window says "The list changed on disk; it has been read again"
  (axTask says the same of an item, IA-5).
- LI-6.3 A file that cannot be read or written is a notice with Windows' message; the window keeps what it showed.

## 5. The file format (LI-D2)

A list is a UTF-8 Markdown file (another encoding read is kept on writing). axList reads each line as one of these,
and writes the lines it changes in the same shape:

```
# Shopping                      a heading: any line starting with #
- Milk                          a bullet item: "-", "*" or "+", a space, the text
- [ ] Bread                     a checkbox item, not done: a bullet, "[ ]", the text
- [x] Eggs                      a checkbox item, done: "[x]" or "[X]"
1. Preheat the oven             a numbered item: a number, "." or ")", a space, the text
1. [ ] Call the plumber         a numbered checkbox item
  - [ ] Wholemeal               a nested item: two spaces (or a tab) more than the item it is under
Anything else                   a text line, kept and shown as it is
```

The marker a line has is kept when its text or state changes; a new item takes the marker of the item it follows,
or `- ` in an empty list. Indentation is written as two spaces a level. Blank lines are kept and not shown. Nothing
else is added to the file: no identifiers, no hidden metadata, so that a person, a script or an AI system can edit it
in any editor and axList reads it the same way. `---` (a rule), `#tag` and `1.5 kg` are text lines, not items or
headings: an item wants a space after its marker, a heading a space after its # signs.

## 6. Menus, keys, texts

- LI-6.4 Menus (AX-6): **Folder (F):** Lists folder… | Refresh F5 | ─ | Exit Alt+F4. **List (L):** Open Enter | New
  list… Ctrl+N | Show in axFile Ctrl+E | Open in axDown | Copy name Ctrl+C. **Item (I):** Check or uncheck Space |
  Change… Enter | New item… Ctrl+N | Make a task or not Ctrl+T | Remove Delete | ─ | Move up Alt+Up | Move down
  Alt+Down | Indent Alt+Right | Outdent Alt+Left | ─ | Undo Ctrl+Z | Copy text Ctrl+C | Show in axFile Ctrl+E | Edit in
  axDown. **Task (T):** Check off Space | Go to its list Enter | ─ | Undo Ctrl+Z | Copy text Ctrl+C. Only the menu of
  the view shown is enabled, the other two not, so that an item's command is never at hand among the lists (axProcess's
  way, 2026-10-24); Shift+F10, the Applications key and a right click in the list open it (the window base's
  `OpenBarMenu`). **View (V):** Lists Ctrl+1 | Items Ctrl+2 | Tasks Ctrl+3 | ─ | Hide done tasks Ctrl+Shift+D ✓.
  **Options (O):** Text ▸ | General ▸ | Developer ▸. **Window (W)** (before Options), **axKit (A)** (AX-9.1). **Help (H):** Command palette… Ctrl+Shift+P | Keyboard shortcuts F1 | User
  guide | ─ | List resources ▸ (Markdown task lists). Every item has a mnemonic (AX-7.4). New list and New item are one
  key, Ctrl+N, by the view (the Lists view makes a list).
- LI-6.5 Keys (AX-8). Bundle-wide: F1, Ctrl+Shift+P, Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here: every
  change is written at once"), Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific (`AxListKeys`): Ctrl+1, Ctrl+2, Ctrl+3,
  Enter, Space, Ctrl+N, Delete, Alt+Up, Alt+Down, Alt+Right, Alt+Left, Ctrl+T, Ctrl+Shift+D, Ctrl+Z, Ctrl+C, Ctrl+E,
  F5, Escape and Backspace, Tab and Shift+Tab, Shift+F10 and the Applications key. Alt+Up is axFile's Up a folder too,
  the same gesture with this window's meaning (AX-8.4's list of keys used differently). An item command in the Lists
  view says "Open a list first: Enter".
- LI-6.6 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axList/user-guide.md` is embedded (Help, User
  guide); About is the bundle's (AX-9.9).

## 7. Settings, announcements, log

- LI-7.1 Announcements, one UI Automation notification each (AX-7.3): the texts of LI-2 to LI-5; "Refreshed: <name>";
  "Nothing selected"; "Open a list first: Enter"; "No list open: Ctrl+1, then Enter"; "The list changed on disk; it
  has been read again"; "Copied"; "Not found" (find); the bundle's text size and diagnostics announcements. Nothing is
  spoken on an arrow key.
- LI-8.1 `%APPDATA%\AxKit\axList.json` (AX-5.3; `ListSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `window`, `folder` (the folder of lists; null for `Documents\Lists`), `lastList` (the file name
  open last, opened again at the next start), `hideDone` (false).
- LI-8.2 The log is `%LOCALAPPDATA%\AxKit\logs\axList.log`: the start, the exit, errors; never an item's text.

## 8. Architecture

```
src/AxList.Core/    class library (net10.0): ListLine and LineKind (one line of a list file read), ListDocument (the
                    lines, the changes of LI-4 as pure operations returning the new document, the numbering, the
                    text written back), ListFolder (the folder's lists and the open checkbox items of all),
                    ListSettings; references AxKit.Core (TextDocument, NaturalOrder, SettingsFile)
src/AxList/         class library (net10.0-windows): AxListApp (Run, Usage), ListForm (the window), AxListKeys,
                    HelpText; references AxList.Core, AxKit.Core, AxKit.Forms
tests/AxList.Tests/ ListDocumentTests (the reading, every change, the round trip of a file it does not change),
                    ListFolderTests (a temporary folder), ListSettingsTests
docs/axList/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

The build follows `docs/axKit/adding-an-app.md` (`tools/new-app.ps1 List list …`), which registers the app in
`Dispatch.Apps`, the installer's table, the bundle spec's AX-1.1, AX-1.6, AX-2.3, AX-2.4 and AX-8.3 lists, `run.ps1`,
`manual_tests/prepare.ps1` and the pages. The archived draft (section 11) had `ListDocument` as pure operations with
`Holds` for LI-6.2 and `SubtreeEnd` for what "nested" means (the following lines wider than the item, up to a blank
line), `ListLine.Raw` kept so that a line nothing changed is written back exactly as read, and every change of the
window through one `Change` method: read the file again, check the line, apply, write, keep the step for Ctrl+Z, fill
the view, announce. Gotcha recorded there: `TextDocument.Text` has `\r\n` line ends whatever the file's, so the
document splits and joins on them and `TextDocument.ToBytes` writes the file's own; a file's last line end is kept or
left off as it was.

## 9. Design decisions

- **LI-D1 Two apps** (the user, 2026-11-06; until then one app first, 2026-10-23). axList and axTask were to be one
  app, the Tasks view the task manager's first step, split when reminders and smart lists gave axTask a window of its
  own. axTask was then built on its own design (JSON items with typed fields, IA-D1), and the user wants both kept in
  view as separate apps: axTask for things to do, axList for things to remember and arrange. What a checkbox item in a
  list means beside axTask is section 10.
- **LI-D2 Markdown files, one per list.** The user's rule (2026-10-23): plain text on disk with a documented format
  (section 5), so that an AI system, a script, axDown or any editor reads and changes a list as easily as axList does.
  No database, no identifiers in the file; a line is found again by its place and text. IA-D1 chose JSON for axTask
  because typed fields need a format that says each field's type; a list of lines needs none.
- **LI-D3 Room to grow.** Tags (`#home`) and links (`[[Other list]]`, `[[Other list#Item]]`) are plain text on the
  item's line today and can become meaning later without changing the files: links between items are the plan's later
  step for mind maps, curriculum planning and recall in plain language.
- **LI-D4 Written at once.** There is no Save: every change goes to the file when it is made, so that nothing is lost
  and another program sees it at once; Ctrl+Z is the way back.
- **LI-D5 Read before every change.** The file may be open in axDown or changed by a script; reading it again before a
  change, and refusing a change whose line moved, is cheaper than watching the folder and never writes over the other
  program's work. axTask took the same rule (IA-5).

## 10. Open questions, to settle with the user before the build

- **Checkbox items beside axTask.** Keep them as Markdown's checkbox (Space toggles, nothing more), keep the Tasks view
  across lists (LI-5), or drop the view and send things to do to axTask. The draft had the view; the words "task" and
  "Done:" in the announcements follow the answer.
- **Where axList's items and axTask's meet.** Whether an item of a list can become a task of axTask (one key, the
  item's text as the task's title and a link back), or the two stay apart.
- **Links between items** (LI-D3): the first step that makes `[[Other list]]` a thing to follow (Enter opens the list),
  and what a mind map is in one list box.
- Renaming and deleting lists here rather than in axFile; several folders of lists; a Today view, if the Tasks view
  stays.

## 11. Status

- 2026-10-23 and 24: built on the branch `axList` (LI-1 to LI-8, with `tests/AxList.Tests`), under the bundle's former
  name and before the bundle's list control and the window base changes of 2026-10-24 and after; never run under NVDA.
- 2026-10-24: axTask designed and built as an item app in its place; the branch kept as a record.
- 2026-11-06: the user keeps axList and axTask in view as two apps; this spec, the guide and the test plan brought into
  `main` with the bundle's current names, nothing built; the branch's last commit kept as the tag `archive/axlist-draft`
  (`git show archive/axlist-draft:src/AxList/ListForm.cs` reads the window's draft) and the branch deleted. A build
  starts from this spec and `docs/axKit/adding-an-app.md`, not from the draft's code, and lifts from the draft what
  still fits.
