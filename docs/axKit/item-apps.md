# Item apps: one shape for axTask and axPass

Written 2026-10-24 at the user's direction, before either app is built. axTask (tasks, `docs/axTask/SPEC.md`) and
axPass (passwords, `docs/axPass/SPEC.md`, with the format in `docs/axPass/kdbx-notes.md`) are both an *item app*: lists of items, each
item a set of typed fields. This document is the part they share, so that the two stay alike and the places they
differ are written down in one table (section 6). Requirements are numbered `IA-n.m` and decisions `IA-Dn`; each app's
spec cites them and does not repeat them. It took the place of the Markdown design of axList for tasks (IA-D1); axList
itself is planned again beside axTask since 2026-11-06, for lists to remember (`docs/axList/SPEC.md`, not built).

## 1. Summary

The window is axFile's, made smaller: a navigator on the left and a detail pane on the right. The navigator is lists
and items, moved through as axFile moves through folders and files. The detail pane shows what the navigator's
selection holds: an item's fields on an item, the list's own details on a list. Every field has a title, a type and a
value ("Due, date, 30 October 2026"), and each type has its own simple way to set it. The same fields are a small JSON
file that axJSON opens, so the app's editor is a convenience, never the only way in (axTask; axPass excepted, IA-6).

## 2. The window

- **IA-2.1 Two panes.** The navigator and the detail pane, each the bundle's `AxKitList` (type-to-find, the edge
  earcon, AX-8.6), side by side; a status bar below. Nothing else takes the focus.
- **IA-2.2 The navigator.** One level at a time, as axFile: the top level is the lists (and the app's own entries
  above them, such as axTask's All tasks), a list's level is its sub-lists, then its items. A row reads its name and
  kind: "Home, list, 4 items", "Fix the tap, due 30 October" (the app's summary, IA-6). The list box is named after the
  level ("Lists", "Home") with its count.
- **IA-2.3 The detail pane.** Filled silently whenever the navigator's selection changes: on an item, its fields; on a
  list, the list's details (section 4.3). Nothing is spoken until the user goes there. It is named "Fields of Fix the
  tap" or "Details of Home".
- **IA-2.3a Sections.** A level may be in sections, each under a header row ("User lists, section, 2"); Ctrl+Up and
  Ctrl+Down go from header to header. A header is no list: Right and Enter on it play the edge sound.
- **IA-2.4 One announcement per move between levels**, axFile's words: "Home, 4 items, Fix the tap".

## 3. Keys

Window-specific keys, in one table per app (AX-8); the ones here are the same in every item app.

| Key | In the navigator | In the detail pane |
| --- | --- | --- |
| Up, Down, Home, End | move; the edge earcon at an end | move through the fields |
| Ctrl+Up, Ctrl+Down | the section before or after, on a level in sections (axTask's top level, TK-3a); the edge earcon past the ends | — |
| Right | on a list: open it. On an item: nothing, the edge earcon (axFile's Right on a file) | on a choice the next value, on a check checked (IA-5.6); the edge earcon on any other field |
| Left | up a level | on a choice the previous value, on a check unchecked (IA-5.6); the edge earcon on any other field |
| Backspace | up a level | nothing |
| Enter | open: a list opens its items; an item moves to its fields, as Tab (IA-D2) | change the field (section 5) |
| Tab, Shift+Tab | to the detail pane | back to the navigator |
| Ctrl+Tab, Ctrl+Shift+Tab, F6 | to the detail pane (the bundle's pane keys, AX-8.2, since 2026-11-09) | back to the navigator |
| Escape | — | back to the navigator |
| Ctrl+N | new item in this list | said: where the fields are changed (IA-5.3) |
| Ctrl+Shift+N | new list here | — |
| F2 | rename in place (the Title field; bundle AX-8.7) | said: where the fields are changed (IA-5.3) |
| Delete | delete the item or list (IA-D4) | said: where the fields are changed (IA-5.3) |
| Alt+Up, Alt+Down | — | said: where the fields are changed (IA-5.3) |
| Ctrl+Z | undo the last change | the same |
| Ctrl+Q | read the selection in full: every field, one after another | read the field in full |
| F5 | read the folder again | — |
| Shift+F10, the Applications key | the bar's Context menu: the list's or the item's commands (bundle AX-9.11) | Context: the field's commands |

- **IA-3.0 The menus** (bundle AX-9.1, B-19, 2026-11-11): every item app's bar is Session, Actions, Navigation,
  Context, Options, Window, axKit, Help. What acts on the whole (a new item, a new list, Undo, Lock) is in Actions; the
  moves (sections, All tasks, the search) in Navigation; and what acts on the row or field the focus is on in Context,
  which `ItemForm.CreateContextMenu` fills from the app's three menus of a list, an item and a field (none of them on
  the bar), saying "Nothing selected" or "A heading: nothing to act on" when there is nothing.

- **IA-3.1** Entering the detail pane reads its name and the field: "Fields of Fix the tap, Due, date, 30 October
  2026". Leaving it says the navigator's selection.
- **IA-3.2** A level shows its sub-lists first, then its items, each by name in natural order, as axFile shows folders and files (the Archive last at the top, IA-D4). An item's fields are in the order its list's configuration gives them (IA-4.3), the Title first; nothing else sorts behind the user's back.

## 4. The format

The files are axTask's (the reports list of `reports.md` is one of its lists); axPass makes the same fields in
memory from a KeePass entry and has no file of this shape. Since 2026-11-21 the format is the one of
`docs/axTask/forms-framework.md` sections 11 and 13 (AF-n.m), which this section sums up: an item holds values, and
its list's configuration says once what the fields are. The shape before it (`axkit-item 1`, `axkit-list 1`: an item
as a list of objects with a title, a type and a value each) is read by the converter alone
(`AxKit.Core/Conversion`, AF-13.6), never beside this one.

### 4.1 An item

A JSON file of names and values, with `"schema"`, the link to the configuration it is read through. The Title is
first.

```json
{
  "schema": "list.json",
  "Title": "Fix the tap",
  "Done": false,
  "Start": "2026-10-24",
  "Due": "2026-10-30",
  "Priority": "High",
  "Notes": "Washer under the sink.",
  "Waits on": "../Shopping/Washer.json"
}
```

- **IA-4.1** A value whose name the list's configuration does not have is kept as it is when the file is written
  again, not shown among the fields, and named in the item's last line ("1 more field kept: Sprint"; AF-11.3,
  AF-11.4). A field of a type the app does not know reads as text. In the window the item is an `ItemDocument`, its
  Title and the configuration's fields with the item's values (`AxKit.Core/ItemView`).
- **IA-4.2** A field the file does not hold is shown empty (`""`, or not done for a check) and is written with its
  first value; a new item is written with every field of its list, each with the value the configuration starts it
  with.

### 4.2 The types

| Type | Value in the file | Read as | Set by (section 5) |
| --- | --- | --- | --- |
| `text` | a string, one line | the text | a text field |
| `longtext` | a string with line breaks | the first line and the count of the others, Ctrl+Q for all | a field of several lines: Enter makes a new line, Ctrl+Enter keeps it, as Tab then Enter on Save does |
| `number` | a JSON number | the number | a text field that takes only a number |
| `date` | `"YYYY-MM-DD"` | "30 October 2026, Friday" | a text field: a date, `today`, `tomorrow`, `+3` (days) |
| `datetime` | `"YYYY-MM-DDTHH:MM"` | "30 October 2026, 14:00" | the same, a time after the date |
| `check` | `true` or `false` | "done" or "not done" | Space or Enter toggles it, Right checks and Left unchecks; no field |
| `choice` | one of `options` | the option | a list of the options, the current one selected; Right and Left step through them without the list |
| `link` | a path to another item, relative to this file | the other item's title, or "missing: name" | the other item's title, typed |

axPass adds `username` and `password` (section 6). A value its field's type does not hold (the type was changed in
the list's configuration) is read as its text with what the field is now, "next week, kept as text: the field is now
a date", and stays as it is until the field is given a new value (AF-11.6; `FieldTypes.Holds`).

### 4.3 A list

A folder. Its configuration is `list.json` in it, never shown in the navigator:

```json
{
  "format": "axkit-list 2",
  "details": { "Notes": "" },
  "item fields": [
    { "title": "Done", "type": "check", "value": false },
    { "title": "Due", "type": "date", "value": "" },
    { "title": "Priority", "type": "choice", "value": "Normal", "options": ["High", "Normal", "Low"] }
  ],
  "actions": []
}
```

- **IA-4.3** `item fields` are the fields of the list's items, in the order they are shown after the Title, each
  with its `title`, its `type`, the `value` a new item starts with and a choice's `options`; `details` are the list's
  own values by name; `actions` are its actions (axTask's action keys, its spec). The detail pane on a list shows
  the details as fields, then one line "Fields of its items: Done, Due, Priority", on which Enter says how they are
  changed: in this file, opened in axJSON from the list (Ctrl+J in axTask). A change there holds for every item at
  once and touches no item's file: a field added shows empty, a field taken out is hidden and its values kept, and
  it shows them again when it is named again (hide, keep, restore; AF-11.3). Anything else in the file is kept.
- **IA-4.4** A folder without `list.json` shows its details as they would be written; the file is written the first
  time the user changes a detail, adds an item or asks to edit the configuration, with the fields the folder's items
  hold (AF-11.1). Until then an item of the folder, whose link names a file that is not there, is shown with every
  value as text and is not changed (AF-13.4), as is an item whose configuration does not read (AF-11.7).

### 4.4 File names

- **IA-4.5** An item's file is named after its Title, with the characters Windows forbids replaced by a space, and
  `.json`: `Fix the tap.json`. Two items with one title are `Fix the tap.json` and `Fix the tap - 2.json`, axFile's
  rule. A Title that is a Windows device name (`con`, `nul`, `com1` and the like, whatever follows a dot) gets an
  underscore in front, `_con.json`, since Windows would send the file to the device (security audit S-55,
  2026-11-09). Changing the Title renames the file, and the links to it are changed with it (IA-D3). The rule is
  `AxKit.Core/ItemFiles` (`FileStem`, `FreePath`) since the bundle's reports list used it too (2026-11-07,
  docs/axKit/reports.md RP-5.1).
- **IA-4.6** A file that is not JSON, or not an item, is not shown; the status bar counts them ("2 files not shown").
  In axTask a `.json` file that does not read as an item is a damaged item and is shown as a row (docs/axTask/SPEC.md
  TK-4.6, 2026-11-16); only the other files are counted.

## 5. Changing a field

- **IA-5.1** Enter on a field opens a notice in the window (bundle rule 2) titled with the field, its value in an edit
  field and selected (a date as 2026-10-30, a link as the other item's title). Enter keeps it, Escape leaves it as it
  was. A value the type refuses ("30 Febuary") says why and the notice stays.
- **IA-5.2** A check changes with Space or Enter, at once: "Done: done".
- **IA-5.3** Which fields an item has is not changed in the detail pane (since 2026-11-21): they are the list's
  configuration's in axTask (IA-4.3), KeePass's in axPass, the server's schema in axMCP. Ctrl+N, F2, Delete, Alt+Up
  and Alt+Down in the fields say where it is done, in the app's own words ("The fields are the list's
  configuration's: Ctrl+J on the list opens it in axJSON"), and change nothing. Until then axTask added, renamed,
  removed and moved a field in one item's file; the shared window still holds that code behind `FieldsFixed`, which
  every app now gives, and it is to be removed (the priorities list).
- **IA-5.4** Every change is one announcement ("Due: 30 October 2026, Friday") and one step for Ctrl+Z.
- **IA-5.5** Before every write the file is read again; if it changed since it was shown (axJSON, a script, a sync),
  the change is refused with "The item changed on disk; it has been read again" (axList's LI-D5).
- **IA-5.6** Right and Left change the field in focus (bundle AX-8.10 rule 4; the user's decision, 2026-11-14). On a
  choice, Right is the next option and Left the previous, stopping at the first and the last with the edge earcon,
  never wrapping; a choice that is empty takes the first option on Right. On a check, Right checks and Left unchecks;
  already so, the edge earcon. The change is saved at once, said once ("Priority: Low", "Done: done") and is one step
  for Ctrl+Z, as every change (IA-5.4). On any other field, and on a line that is no field, both keys play the edge
  earcon and change nothing; never on axPass's user name and password. The rule is `AxKit.Core/FieldStep`. Until
  2026-11-14 both keys played the edge earcon on every field: neither ever left the pane, and they still do not;
  Escape, Shift+Tab and the pane keys go back to the navigator.
- **IA-5.7** A long text's box: Enter makes a new line; Ctrl+Enter keeps the text (bundle AX-8.10 rule 9), as Tab,
  then Enter on Save does; Escape leaves the text as it was.

## 6. What differs between the apps

| | axTask | axPass |
| --- | --- | --- |
| Where the items are | a folder of JSON files; a list is a folder (IA-4) | one encrypted KeePass file (`.kdbx`), through KeePassXC's command-line tool; a list is a KeePass group |
| The fields | read from the item's file through its list's configuration (IA-4) | made in memory from the entry; never written to disk |
| Open in axJSON | yes, Ctrl+J on an item or a list | no: a password in a plain file defeats the encryption |
| Extra field types | none | `username`, `password` (read as "password, hidden"; Ctrl+Q never reads it) |
| Extra navigator entries | All tasks, the smart lists (saved filters) | none; search by title (Ctrl+F) |
| Row summary | the Title, "done" and the Due date | the Title and the user name |
| Space in the navigator | marks the item done or not done | nothing |
| Its own keys | (its spec) | copy user name, copy password with the clipboard cleared after 30 seconds, generate a password, lock |
| Opens with | a folder | a `.kdbx` file and the master password, asked in a notice whose field hides what is typed |
| Which fields an item has | the list's configuration, `list.json`'s `item fields`, edited in axJSON (IA-4.3) | KeePass's standard fields, custom attributes read only (PW-D3) |
| A new item's fields | the configuration's, each with the value it starts with | KeePass's standard fields (Title, UserName, Password, URL, Notes) plus the user's own |
| Delete | to the top folder's Archive list; Ctrl+Z brings it back | to KeePass's own Recycle Bin group |

Everything not in this table is the same in both apps, and a change to one of those things changes this document and
both apps in the same commit.

## 7. Decisions

- **IA-D1 One item per JSON file, not Markdown lists** (the user, 2026-10-24). axList kept lists as Markdown, and the
  user found it more than was needed and too close to axFile and axDown. Typed fields (a start and a due date as
  dates) need a format that says each field's type; JSON says it, axJSON already edits it, and an AI system or a
  script reads it as easily. One file per item keeps the navigator the same shape as axFile's and lets axJSON open one
  item at a time (the user's choice over one file per list). The Markdown lists stay the right format for a list of
  lines whose order and nesting carry the meaning, which is axList's own ground (`docs/axList/SPEC.md` LI-D2,
  2026-11-06).
- **IA-D2 Enter opens** (the user, 2026-10-24). In axFile, Enter on a folder opens it and Enter on a file opens the
  file; here Enter on a list opens it and Enter on an item opens its fields. The worry on record: Enter does two
  things. It is the same word, open, in both places, and the announcement says which happened ("Fields of Fix the
  tap" against "Home, 4 items"). Right stays the key that only goes into lists.
- **IA-D3 Links are paths** relative to the item's file, so that a folder moved or synced whole keeps them. The app
  changes them when it renames or moves an item; a link changed by another program to a file that is not there reads
  "Waits on, link, missing: Washer".
- **IA-D4 Delete archives.** Delete moves an item or a list into the app's archive: for axTask the list `Archive` in
  the top folder (made by the first Delete, shown last at the top, left out of All tasks and the smart lists), for
  axPass KeePass's Recycle Bin group. Ctrl+Z brings it back; in the archive, Delete says that axFile (Ctrl+E) deletes
  for good. A task done stays where it is, marked done, until the user archives it (the user's thought of
  2026-10-24: "delete could just remove them and add them into an archive"). The Recycle Bin was the first draft;
  the archive is a list like the others, read by ear and by axFile, with no shell call behind it.
- **IA-D5 Built once, shared on the second use** (bundle rule 4). axTask is built first with the window and the format
  in its own projects. When axPass is built, the window, the field editors and the format move unchanged to
  `AxKit.Core` and `AxKit.Forms`, one piece per commit, and the two apps keep only what section 6 lists.

## 8. Open questions

- A field in the detail pane that the app needs (axTask's Done and Due) may be removed by the user; the app then treats
  the item as having none. Whether such fields should be protected is left until it is heard in use.
- Whether a list's sub-lists come before or after its items in the navigator (axFile puts folders first; the
  first build does the same).
