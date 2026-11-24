# axTask — Specification

axTask is an app of the axKit bundle (`docs/axKit/SPEC.md`): tasks and lists as a folder of small JSON files, for a
blind developer who uses the NVDA screen reader. It is an item app: its window, keys, field types and file format are
the shared ones of `docs/axKit/item-apps.md` (cited as IA-n.m and not repeated), and this document holds only what is
axTask's own. Written 2026-10-24 and built the same day on the branch `axTask`, not yet heard under NVDA; it took the
place of the first design, axList as Markdown lists with a Tasks view. axList is planned again beside it since
2026-11-06, for lists to remember and to arrange (`docs/axList/SPEC.md`, not built); what is to do belongs here.
Requirements are numbered `TK-n.m` and decisions `TK-Dn`. In axTask's code and documents, "SPEC.md" means this file.

## 1. Summary

A folder of lists: `Home\`, `Shopping\`, `Course plan\`, each item a file such as `Home\Fix the tap.json`, one level
deep (TK-D4). The top level is in sections (TK-3a): **Smart lists** (All tasks, every task not done from every list
by due date, and the saved filters), **User lists**, the user's own sections, and **Archived lists**; Ctrl+Up and
Ctrl+Down move between them. Right or Enter goes into a list, Left back to the sections, Tab or Enter on an item to
its fields in the detail pane (Ctrl+Tab, Ctrl+Shift+Tab and F6 switch the panes too, the bundle's keys, AX-8.2).
Ctrl+Enter marks a task done; Space selects an item, and Ctrl+Enter, Delete and a list's actions then act on all the
selected ones (TK-4.4, TK-4.7).

## 2. Goals and non-goals

- **G1 Tasks with dates.** Start and due dates as dates, done as a check, notes, priority, links to other items.
- **G2 One view of everything to do.** All tasks and the smart lists, read from the files every time.
- **G3 The files are the truth.** Every item can be opened in axJSON (Ctrl+J), changed by a script or an AI system,
  and synced by whatever syncs the user's files (bundle B-15).
- **G4 As simple as axFile.** The shape of IA-2, nothing more in the first version.
- **Non-goals for the first version.** Reminders that sound at a time, rules that act on their own (TK-6), recurring
  tasks, sharing. The format leaves room for them.

## 3. Scenarios

1. Start axTask on the folder: "Lists - axTask window", then "Lists, 4 lists list, Your lists, heading level 1, 2
   lists". Down: "Home, list, 2 items". Ctrl+1: "Smart lists, heading level 1, 1 smart list"; Down: "All tasks, 5 not
   done".
2. Right: "Home, 2 items, Fix the tap, due 30 October". Tab: "Fields of Fix the tap, Title, text, Fix the tap". Down:
   "Done, check, not done", then "Due, date, 30 October 2026, Friday".
3. Enter on Due, type `+3`, Enter: "Due: 2 November 2026, Monday". Shift+Tab: "Fix the tap, due 2 November".
4. Ctrl+Enter: "Done: Fix the tap". Ctrl+Z: "Undone: Done: Fix the tap".
5. Ctrl+N, type `Call the plumber`, Enter: "Added Call the plumber", with the list's new-item fields (Done, Due).
6. Left: "Lists, Home". Up: "All tasks, 5 not done". Right: the tasks of every list, the soonest due first, each read
   "Call the plumber, in Home".

## 3a. The top level under headings (asked 2026-10-24; two levels since 2026-11-11)

- **TK-3a.1 The headings**, in this order, each a row of one shape, "Smart lists, heading level 1, 3 smart lists"
  (TK-D13):
  - **Your lists**, level 1: the lists in no section of the user's own, by name; then a heading of **level 2 for each
    section of the user's own**, by name, each with its lists ("Jobs, heading level 2, 2 lists"). With no list at all:
    "Your lists, heading level 1, none: Ctrl+Shift+N makes one". An empty section of the user's does not exist.
  - **Projects**, level 1, while project folders are set and the user's own folder is shown ("Projects, heading level
    1, 2 project folders"): a heading of level 2 for each project folder, with the projects found under it (TK-7b.3).
  - **Smart lists**, level 1: All tasks, then the user's smart lists whose first word no other shares (TK-5); then a
    heading of level 2 for each first word two or more share, holding them (TK-5.2a); then **Tags**, level 2, only
    when the tasks hold any, with a heading of **level 3 for each symbol** (TK-5.5).
  - **Date lists**, level 1: Overdue, Due today, Due in the next 7 days, No due date (TK-5.4).
  - **Archived lists**, level 1, only when the Archive holds any: the lists in the Archive (IA-D4), then one row
    "Archived items, 4" when the Archive holds items of its own, which opens like a list.
- **TK-3a.2 Moving by heading** in the navigator: Ctrl+Up and Ctrl+Down go to the heading of any level before or
  after the selection; Ctrl+1, Ctrl+2 and Ctrl+3 to the next heading of level 1, 2 or 3, Ctrl+Shift+1, Ctrl+Shift+2
  and Ctrl+Shift+3 to the one before, at a top level only (in a list, Ctrl+1 to Ctrl+3 are the list's action keys,
  TK-7a.1; TK-D14, TK-D20). Ctrl+0 is All tasks (TK-D21). NVDA reads
  the heading's row as the selection moves; with no heading further, the edge sound. A typed letter finds a row by its
  name as in every list. On a heading, Right and Enter play the edge sound and say "A section: Down goes into it";
  Down goes on to its first row. In the detail pane of a heading: its row and what it holds. (In an edit control
  Ctrl+Up and Ctrl+Down move by paragraph, AX-8.2; the navigator is a list, so they move by heading there, the same
  idea.)
- **TK-3a.3 A list's section** is the field Section (`text`) of its details (`list.json`), always shown first in the
  detail pane of a list, empty for Your lists. Enter on it changes it as any field; **Context, Move to section**
  (Ctrl+M) opens one form (bundle AX-8.10), "Move <list>": the list "Choose the section" with Your lists, the sections
  there are and "A new section", then the field "Type the new section's name, when you chose A new section", then Move
  and Cancel. Enter or Ctrl+Enter moves the list; a new section without a name, or with a name a section has, is said
  with the focus in the name's field and the form stays (a list and then a second notice until 2026-11-14). A new
  list (Ctrl+Shift+N) joins the section of the row selected. A section is renamed by moving its lists.

## 4. axTask's part of the shared shape

- **TK-4.0 The files** (since 2026-11-21; `forms-framework.md` sections 11 and 13 are the specification, AF-n.m, and
  this section is axTask's part of it). A list's `list.json` is its configuration (`axkit-list 2`): its details, the
  fields of its items in their order, its actions. An item's file is its names and values with `"schema"`, the link to
  that configuration; everything of a field but its value is the configuration's. An item shows its Title and then
  the configuration's fields: a field its file does not hold is shown empty and written with its first value; a value
  the configuration does not name is kept, not shown among the fields, and named in the item's last line ("2 more
  fields kept: Sprint, Old priority", Enter reads them; AF-11.3, AF-11.4). The field commands that changed one item's
  fields (new, rename, remove, move) are not in axTask's Field menu, and their keys say "The fields are the list's
  configuration's: Ctrl+J on the list opens it in axJSON". Ctrl+J on a list is **Edit the list's configuration**: the
  file in axJSON, written first with the fields its items hold when the list has none (AF-11.1); back in the window,
  one line says what it holds now ("The list's configuration changed: 5 fields, 2 actions") or that it does not read.
  A configuration that does not read (AF-11.7) is said in the list's row ("its configuration does not read"), with the
  level ("The list's configuration does not read: it is not JSON at line 12. Ctrl+J opens it") and in the details;
  the list's items are then shown with every value as text and nothing in the list is changed, as for an item whose
  configuration is not there (AF-13.4). A list of the earlier format (`axkit-list 1`, items of `axkit-item 1`) is not
  read: its row says "earlier format: Enter converts it", the gathering lists leave it out and say so in their name,
  and Right or Enter asks once, with Convert, Convert all such lists and Cancel; the copy goes among the Archived
  lists as "<name>, before conversion", and the conversion is one step for Ctrl+Z (AF-13.6).
- **TK-4.0a The item's form** (since 2026-11-21; AF-14.1, AF-14.2, the maintainer's message 8). Enter on an item in
  the navigator opens "Fields of <item>", a form of the bundle's (AX-8.10): one standard control for each field of
  the item as its list's configuration gives it, the Title first, the focus in the title's box, with the form's sound and "Fields of <item>, form" (bundle AX-7.11); its explanation, read on Ctrl+Q, ends
  with the line of the fields the item keeps. A text and a number are a box of one line, a long text a box of
  several lines, a check a check box named by the field, a date and a date and time a box whose name says what it
  takes ("Type Due: a date such as 2026-11-30, today, tomorrow, +3 or a weekday; empty for none"), a choice a list of
  its options ("Choose Priority"; with "Not chosen" first while the field holds none of them), a link a box for the
  other item's title. Ctrl+Enter or Save, and Enter from a box of one line, a list or a check box, saves every field
  that was changed as one undo step, said once: "Saved" (the item and what changed are heard as the list reads the
  item the focus goes back to; the maintainer's choice of 2026-11-24, when the title and the changed fields said
  here were heard twice); nothing changed says "Nothing changed". A changed Title renames the item's file, the links following, as F2 does.
  A value a field does not take is said with its field's name and the focus in its box, and the form stays. A
  control left as it was changes nothing of its field, so a value the form cannot show as it is (a link whose item
  is gone, a value of the type the field had before, shown as text) is never rewritten. Escape leaves and nothing is
  changed. Enter on a field of an item in the detail pane opens the same form with the focus in that field, and the
  focus comes back to that field after it; the detail pane keeps its one-key changes (Space, Right and Left), and
  Enter on a list's own details still changes the one detail in a notice. Not in the search's level, where Enter
  opens the found item where it lives (TK-7b.4), and not on an item whose configuration is not there, which Enter
  shows in its fields as before. The rules are `AxTask.Core/ItemFormRules`; the window's side is
  `TaskForm.ItemForm.cs`.
- **TK-4.1 The default fields.** A list made in axTask (Ctrl+Shift+N) gets the fields of the list selected or shown,
  or Done (`check`), Start (`date`) and Due (`date`), empty. A folder with no `list.json` and no item gets these same
  fields, its configuration written in the same step as its first item.
- **TK-4.1a The day an item was made** (the maintainer's wish of the old feature list, item 25: "we should have a
  creation date for each item"; built 2026-11-23). A list whose configuration names a field titled Created (any case),
  of type `date` or `datetime`, gets today's date in it, or the moment, for every item added, by Ctrl+N or by an AI
  tool (TK-8.3); a value the tool gives is kept. Nothing fills it later, so items made before the field was named keep it
  empty, and a Created of another type is left alone. `TaskRules.WithCreated`. The record of every change, the
  item's second half, is not built.
- **TK-4.2 A task** is an item of a list whose configuration names a `check` field titled Done: a list of things that
  are not tasks is a list without it (the user's ask: plain lists and checklists, and an easy way between them). "Add
  Done to every item" and "Remove Done from every item" in Actions switch a whole list between the two, in its
  configuration alone (AF-13.3): no item's file is touched, a Done an item holds is kept while the list is plain and
  shows again when it is a checklist again ("Home is a plain list: Done is hidden, and kept in the items that have
  it"; "Home is a checklist already" when there is nothing to switch).
- **TK-4.3 The row summary** (IA-6): the Title, then "done" when Done is true, then "due" and the Due date when there is
  one: "Fix the tap, done, due 30 October". Overdue reads "overdue since 30 October".
- **TK-4.3a Ctrl+N on a list at the top level** (the maintainer's wish of the old feature list, item 30: "ctrl+n in
  axtask when focused on a list should insert the task into the list that is in focus"; built 2026-11-23). With a
  list selected among the sections, Ctrl+N asks for the title "In <the list>." and adds the item to that list, made
  as in the list itself; the focus stays on the list, and the window says "Added <title> to <the list>". Ctrl+Z takes
  it back. A list of the earlier format asks to be converted first, as Right does (AF-13.6). On a heading, a smart
  list, a project or the Archive, Ctrl+N still says "Items go in a list: Right on a list, then Ctrl+N".
- **TK-4.4 Ctrl+Enter marks a task done** (the maintainer's decision of 2026-11-22: "we should use space to mean
  select an item everywhere in axkit, so we can use ctrl+enter to mean mark as done in axtask"; Space did this
  until then). Ctrl+Enter, the bundle's main action key, toggles Done on a task, in the navigator and from the
  detail pane alike: "Done: Fix the tap", "Not done: Fix the tap"; on an item of a plain list, the edge earcon and
  "Not a task". Context, Done or not done, shows the key. With items selected it acts on them (TK-4.7).
- **TK-4.5 The start.** Without a path the window opens the list of the last run. That list is looked at during the
  start only on a drive of this computer (bundle AX-9.5a, 2026-11-16): elsewhere the top level shows and the window
  says "<path> is on the network and was not opened at start; Right on its list opens it." The top folder itself is
  read on the window's thread wherever it is (TK-D3).
- **TK-4.5a Another top folder, and the ones used lately** (the maintainer's wish of the old feature list, item 24:
  "we should add the open database and recent databases"; built 2026-11-23). Session → Top folder… chooses another
  folder of lists with Windows' folder picker; Session → Recent top folders ▸ lists the top folders used before, the
  newest first, "1. Tasks (D:\Lists\Tasks)" as every recent list of the bundle (AX-9.5), without the one in use
  and without one that is gone; "No recent top folders" when there is none. Choosing one opens it as Top folder…
  does: the top level, its undo emptied, "Folder <name>. <the list>". The list is kept in the settings file
  (`recentFolders`, at most ten, `RecentPaths`); a folder joins it when axTask leaves it for another and when it
  is opened, by either command or from the command line. Both commands are marked act for the AI tools, since they
  change the folder axTask works in (MC-D12).
- **TK-4.6 A damaged item is a row** (2026-11-16, `repairs.md` R-66). A `.json` file in a list that does not read as
  an item (not JSON, not an object, no list of fields, or a file that cannot be opened) was only counted with the
  files not shown, and the lists that gather items said nothing at all, so a due task whose file broke vanished from
  Due today without a word (confirmed by reading the code that day: `ItemFolder.AllItems` dropped it).
  - In its list it is a row after the items: "<file name>, not readable as an item, Enter opens it in axJSON"
    (`ItemFolder.UnreadableRow`), and the list's name says how many: "Home, 3 items, 1 not readable". Enter or Ctrl+J
    opens the file in axJSON, Ctrl+E shows it in axFile. F2 and Delete say "Not readable as an item: Enter opens it
    in axJSON, Ctrl+E shows it in axFile" and change nothing; Ctrl+Enter sounds the edge, and Space, which selects
    items, says the same sentence. The detail pane holds the row's
    words, what to do, and the file's path. A file that is not `.json` is still only counted (IA-4.6).
  - A list that gathers items (All tasks, a date list, a tag's list, a smart list of the user's) says in its name how
    many files it could not read, "Due today, 2 items, 1 not readable", and ends with one row that says where:
    "1 item not readable, in Home" or "3 items not readable, in Home, Work" (`ItemFolder.UnreadableSummary`), the
    lists in their order. Enter on it says the row again; its detail pane lists the files' paths. The files are
    found in the same reading as the items (`ItemFolder.ReadAll`), so nothing is read twice.
  - The limits: the counts at the top level's rows ("All tasks, 3 not done") do not say it; the Archive is not
    looked at; and the projects' items, read off the window's thread by the look for projects (TK-5.6), are
    gathered without their damaged files, which show in the project's own lists once it is entered.
  - Checked by `tests/scripts/axTask/10-damaged-item.txt` and `FolderTests`.
- **TK-4.7 Selecting several items, and what acts on them** (2026-11-22; the bundle's selection, AX-8.16, which
  states the keys and the words: this window keeps none of its own). Two words are kept apart from here on: the
  item **you are on**, which the arrow keys choose and NVDA reads, and the **selected** items, which Space marks.
  - **What is selected.** Items, in a list, in a smart list and in what a search found: **Space** selects the
    item you are on, or deselects it again; **Shift+Down, Shift+Up, Shift+Home, Shift+End** select as they move;
    **Shift+Space** selects from the nearest selected item to here; **Ctrl+A** selects every item shown, and
    Actions, Select all does; **Ctrl+Shift+A**, and **Escape** while any is selected, deselect all. A selected
    item's line ends in ", selected", and the status bar's first part says how many are: "Home, 5 items, 2
    selected". A list, a project, a heading and a file that does not read as an item are not selected: Space
    there plays the edge sound and says why ("Items are selected, not lists: Right goes into this one", "A
    heading: nothing to select"), and Ctrl+A at a top level says "No items here to select: go into a list,
    Right". The keys that select many pass over such rows.
  - **How long it lasts.** The selection is one level's: going into a list, back out, to a smart list or to a
    search starts with none. The same level filled again (a change written, F5, done tasks hidden) keeps the
    selected items that are still shown. Nothing of it is written anywhere, and it is not a step for Ctrl+Z.
  - **What acts on the selected items** when there are any, and on the item you are on alone otherwise:
    - **Ctrl+Enter**: the tasks among them done while any of those is not, and not done when every one is, so that
      one press never leaves them mixed and a second takes the first back. "Done: 2 tasks. 1 was done already."
      "Not done: 3 tasks". A selected item that is no task is passed over and counted: "1 selected item is not
      a task"; with no task among them, "None of the selected items is a task" and nothing changes.
    - **Delete**: each into its own folder of lists' Archive, the links to them kept: "Archived 3 items". Not
      from what a search found ("Open an item where it lives first: Enter. The search archives nothing") and not
      for items in the Archive already.
    - **An action of the list that sets fields or moves** (TK-7a.1a): each item filled from its own values and
      written, "Priority high: 3 items"; all of them moved, "Later: 3 items moved to Later". An action's form is
      asked once, and a move's first press says what it does once (TK-7a.3). One item that refuses (a value its
      field does not hold) stops the action before anything is written.
    - **Ctrl+M, Move to a list** (Context; the move key, which on a list with nothing selected is Move to
      section, TK-3a.3): one form (bundle AX-8.10) named "Move 3 items" or "Move Fix the tap", with the folder's
      lists as a list (the Archive and the items' own list left out, a list of the earlier format too) and "A new
      list" last, then a field for the new list's name, refused as a new list's name is (TK-D4). Enter or
      Ctrl+Enter moves them, the links following: "3 items moved to Later", "Fix the tap moved to Someday". A
      new list is made beside the others with the fields and the section of the list the first item is in, and
      is part of the same step: Ctrl+Z brings the items back and takes the list away again. An item that is in
      the chosen list already stays ("Already in Later" when all are). Not from what a search found ("Open an
      item where it lives first: Enter. The search moves nothing"), and not for a project's item gathered into
      the user's smart lists ("An item of a project is moved inside its project: Right on the project first").
      This is also how what a smart list or a tag's list shows becomes a list of its own: Ctrl+A, Ctrl+M, A new
      list.
    Each is one step for Ctrl+Z ("Undone: Archived 3 items"), and each checks every item against its file as it
    was shown before anything is written (IA-5.5). Several moves are made one after the other, each from the disk
    as the one before left it, so that an item linking to two of the moved items is written right; a failure
    takes back what was done before it and says so (`TaskForm.CommitMoves`).
  - **What stays on the item you are on**, whatever is selected: Enter (its form), F2, Ctrl+Q, Ctrl+J, Ctrl+E,
    the detail pane and its changes, and an action of the list that opens something, runs a program or fills
    axTerminal's field: those are about one thing.
  - **For an AI tool** (bundle `mcp.md` MC-7.3): while items are selected, the target of a command it asks to run
    is named with both, "Fix the tap, and 3 selected", and a change of the selection between the question and
    the run refuses the run, as a change of the item does.
  - The rules of Done on several and the words are pure code, `AxTask.Core/ManyItems`, tested; the window's part
    is `TaskForm.Targets`. Checked by `tests/scripts/axTask/20-selection.txt`.
  - Not built yet, each with its item in the priorities list: Set fields of the selected items in one form (a
    list's set action, with or without a form of its own, does it meanwhile), and selecting lists at the top
    level.
- **TK-4.8 Filter and sort** (2026-11-22; the navigator's, bundle AX-8.17). Options → Filter… shows only the rows of the level whose name contains a text (lists at the top level, items in a list), a heading staying while a row under it passes; Options → Sort by ▸ Name puts the rows in the order of their names, under each heading among themselves, and The list's own order puts them back. Neither changes a file: the order of a list on disk and its smart lists are untouched.
- **TK-4.9 A list's own order** (the maintainer's ask, "editable lists like axtask should have the ability to be
  reordered", the keys settled on 2026-11-21: Ctrl with an arrow moves the cursor, Ctrl+Shift with Up or Down moves an
  item; built 2026-11-24). In a list of the user's own, on an item: Ctrl+Shift+Up and Ctrl+Shift+Down move it one
  place, Ctrl+Shift+Home and Ctrl+Shift+End to the top and the bottom (the Item menu's Move up, Move down, Move to
  the top, Move to the bottom), each one step for Ctrl+Z: "Moved up: Fix the tap, 3 of 12"; at an end the edge sound
  and nothing else. The order is the list's configuration's member `order`, the items' file names without ".json"
  (`AxTask.Core/ListOrder`): the items it names come in its order, the others by name after them. A move among the
  items shown while done tasks are hidden keeps the hidden ones where they were. A rename keeps the item's place;
  an item archived or moved away leaves its name behind, which counts for nothing. Not at the top, in a smart list,
  the search or the Archive. A list without the member shows its items by name, as before.
- **TK-4.10 Where a new item lands** (the maintainer's ask: "a setting to let the user choose to add things at the
  top or bottom of a list"; built 2026-11-24): Options, New items at the top of this list, a check of the list
  shown, its configuration's member `new items`, `"bottom"` at first. At the top a new item (Ctrl+N, an AI tool's
  add) stands first, the order written with it; at the bottom it stands last in a list that has an order, and in one
  without, by its name as before. The configuration's file can say it too, and axJSON edits it (Ctrl+J).

## 5. All tasks and the smart lists

- **TK-5.1 All tasks** is the first entry under Smart lists, always there and never a folder: every task in a list
  (the Archive left out) whose Done is false, the soonest Due first, tasks without a due date after them in title order. A row adds
  "in Home" (TK-5.6). Enter or Tab opens the task's fields as anywhere else; a change is written to the task's own file.
- **TK-5.2 A smart list** is a file `<name>.filter.json` in the top folder, shown after All tasks under Smart lists
  as "Due this week, smart list, 3". Its content is the conditions every shown item meets:

```json
{
  "format": "axkit-filter 1",
  "match": "all",
  "conditions": [
    { "field": "Done", "is": false },
    { "field": "Due", "before": "+7" },
    { "list": "Work" }
  ],
  "order": [ "Due", "Title" ]
}
```

  The conditions in the first version: a field equals a value (`is`), a date before or after a date or a day count
  from today, the day itself included (`before`, `after`), a field is empty or not (`empty`), the item is in a list (`list`), a
  text field holds a word (`has`). `match` is `all` or `any`. A condition the app does not know shows the smart list
  as "Due this week, smart list, a condition not understood" and nothing in it. `order` is the fields to sort by, in turn (Due, then Title when it is left out); the items
  without a field, or with it empty, come after the rest. A key is a field's name, or an object that says how:
  `{ "field": "Priority", "by": "options", "descending": true }`. A choice sorts in its options' order by default
  (High, Normal, Low; change the order by changing the options), or alphabetically with `"by": "text"`;
  `descending` turns the order round (2026-11-10, the user: "fix the sort type on a field so that that is adjustable").
- **TK-5.2a Smart lists grouped** (asked 2026-11-11): the user's smart lists are grouped by the first word of their
  names. Every first word two or more share, case not minded, is a heading of level 2 under Smart lists, named as the
  first of them writes it ("App, heading level 2, 5 smart lists"), holding them by their full names ("App axDown,
  smart list, 3") in natural order; the groups in natural order after the lists whose first word no other shares.
  Nothing is renamed (TK-D17).
- **TK-5.3 A new smart list** (Actions, New smart list) asks its name, writes `<name>.filter.json` with All tasks'
  conditions and opens it in axJSON to change them; Ctrl+J on a smart list opens it again. The detail pane of a smart
  list says what it shows, how many, and its file; the conditions are written in axJSON, not field by field, until
  the need is heard (a filter is written once and read many times).
- **TK-5.4 The date lists** (asked 2026-11-11), built in like All tasks and never files, under the heading Date lists:
  **Overdue** (a Due before today), **Due today**, **Due in the next 7 days** (today to seven days on, both included)
  and **No due date**, each over the tasks not done, read "Overdue, 2". Their order is All tasks' (No due date by
  title). F2, Delete and Ctrl+J on one say it is built in.
- **TK-5.5 Tags** (asked 2026-11-11): a smart list for each tag in the Title or a text field (text, long text) of a
  task not done, read "#ui, 3", under the heading **Tags** (level 2, "Tags, heading level 2, 3 symbols"), left out
  while it would be empty. A tag is a symbol at the start of the text or after white space, then a letter, then
  letters, digits, "_" and "-": "@kyle", "#ui", "!urgent", "$budget", "%done", "&team". A symbol is any character that
  is not a letter, a digit or white space, except those that start ordinary prose or Markdown: ( [ { < " ' ` * _ ~ -
  / \ . , ; : = + | (TK-D18). So "(note", "-item", "$5" and an e-mail address are none, and "!" or "?" after a word
  never starts one. Under Tags, a heading of level 3 for each symbol, named so that NVDA says it: "@ at sign, heading
  level 3, 2 smart lists", "# number sign", "! exclamation mark", "? question mark", "$ dollar sign", "% percent
  sign", "& ampersand", "^ caret", "§ section sign", a symbol of no name in axTask's table by its code point ("※
  U+203B"); "@" then "#" first, the others in the order they first appear. Under each, its tags in natural order. Two
  tags told apart only by case are one, shown as first seen. "@" names the person a task is assigned to (TK-D19).
- **TK-5.6 Gathered from everywhere** (asked 2026-11-11): at the user's own top level, All tasks, the smart lists,
  the tags and the date lists take the items of the user's own lists and of every project found (TK-7b.2)
  together, so that "#ui" in two projects is one list. Each item's row says where it lives: "Fix the tap, in Home" for
  the user's own, "Fix the tap, in alpha / Home" for a project's, as the search says it. A smart list's `list`
  condition names a list of the item's own folder of lists. The item's fields, Done, F2, Delete and its links change
  its own file, in its own folder of lists (a project's item goes to that project's Archive), through the one change
  and its undo. The projects' items are read by the look for projects, off the UI thread (TK-7b.2), and kept; a file
  the window changes itself is read again at once, so that a task marked done leaves Overdue as the key is pressed.
  In a project shown as the top, they gather that project alone (TK-D15).

## 6. Later: rules (automation)

Recorded so the format has room; not built in the first version. A rule is a file `<name>.rule.json` in the top
folder: **when** (a date field reaches a day, an item is marked done, an item is added to a list), **if** (the same
conditions as a smart list), **then** (set a field, move the item to a list, make a new item from a template, sound a
reminder through the bundle's notices). Rules run only while axTask runs, or from axStart's background process when
reminders come; that choice waits for the build.

## 7. axTask's own keys

On top of IA-3:

| Key | Where | What |
| --- | --- | --- |
| Ctrl+Enter | navigator and detail pane | done or not done, the selected tasks when items are selected (TK-4.4, TK-4.7; Space until 2026-11-22) |
| Space, Shift+Space, Shift+Down, Shift+Up, Shift+Home, Shift+End, Ctrl+A, Ctrl+Shift+A, Escape | navigator | the bundle's selection keys (AX-8.16, TK-4.7) |
| Ctrl+J | anywhere | open the item, the list's `list.json` or the smart list in axJSON |
| Ctrl+E | anywhere | show the file or folder in axFile |
| Ctrl+Shift+D | navigator | hide or show done tasks in this list |
| Ctrl+0 | anywhere | to All tasks (Ctrl+1 until 2026-11-07, when the digits became the action keys, TK-7a.1) |
| Ctrl+1 to Ctrl+9 | anywhere, in a list | the list's action key on the item selected (TK-7a.1) |
| Ctrl+1, Ctrl+2, Ctrl+3 | navigator, at a top level | the next heading of level 1, 2 or 3 (TK-3a.2, TK-D14, TK-D20) |
| Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3 | navigator, at a top level | the heading of level 1, 2 or 3 before (TK-3a.2) |
| Ctrl+Up, Ctrl+Down | navigator | the heading before or after, of either level (TK-3a.2) |
| Ctrl+M | navigator, on a list | move the list to a section (TK-3a.3) |
| Ctrl+M | navigator, on an item or with items selected | move it, or the selected items, to a list chosen in a form, or to a new one (TK-4.7) |
| Ctrl+Shift+F | anywhere | find words in every list, the user's own and every project's (TK-7b.4) |
| Right, Enter, Escape | navigator, in the found level | open the item where it lives; back where the search started (TK-7b.4) |

## 7a. Action keys (2026-11-07; the GitHub lists removed 2026-11-14)

The design is `docs/axKit/reports.md` section 8 (RP-8); the rules axTask keeps:

- **TK-7a.1 Action keys.** Ctrl+1 to Ctrl+9 run the action the list's configuration gives the key among its
  `"actions"` (`{ "name": "Later", "key": "Ctrl+2", "does": "move", "to": "../Later" }`; under `"keys"` by digit in the
  earlier format, which the conversion turns into these, the same keys and the same actions, so that an acceptance
  of TK-7a.3 stands; RP-8.1), on the item selected in the navigator: `set` writes fields to values as one undo step
  (a value the item lacked is written; one the list's configuration does not name is kept, AF-11.3), `move` moves the item to a list by a path relative to this one, the links
  following (IA-D3), `open` opens a field's web address in the browser (the user's own act; axTask itself asks the network nothing) or
  an app by its verb on the item's file
  (`json`, `file`; `claude` and `terminal` only in a folder on a drive of this computer, RP-8.1). A key without an action: the edge earcon and "No action
  on Ctrl+3 in this list; its actions are written in its configuration, Ctrl+J on the list"; outside a list, "Action
  keys act in a list: open one first". All tasks moved to Ctrl+0 for them (RP-8.4).
- **TK-7a.1a The actions of a list** (since 2026-11-21; `forms-framework.md` sections 2 to 7 are the specification,
  AF-n.m, and this is what is built of it). A list's actions are the entries of its configuration's `"actions"`, and
  nothing else: the form Actions, Action keys went, and an action is written in the file, in axJSON (Ctrl+J on the
  list). Five kinds (`does`): `set`, `move`, `open` as above; `run`, a program with its arguments as a list; and
  `terminal`, a command handed to axTerminal's command field, never sent (below). Eighteen keys and no other:
  Ctrl+1 to Ctrl+9 and Ctrl+Shift+1 to Ctrl+Shift+9 (at a top level the first three of each are the heading keys);
  the first action of a key in the file has it. Context, Actions of this list, lists every action by its name with
  its key, made as the menu opens, then Read the last output; Actions has Stop the running action. An action that
  is not understood (a kind not known, a member of the wrong type, a second action of a name, a program or a folder
  with a placeholder, a hidden field named outside the input) is listed as "<name>, not understood: <why>", says so
  when chosen and is never run (AF-3.4). An action's texts may hold names in braces (AF-4.1): `{Title}` and any
  field's title, `{form:<a form field's title>}`, `{file}`, `{name}`, `{folder}`, `{today}`; a name the item or the
  form does not have refuses the action before anything happens. An action with `fields` opens its form first
  (AF-5): a control for each field, named by the field's title, the form's text saying what the action does;
  an empty needed field keeps the form open. A `run` action (AF-3.2, AF-4, AF-7): the program is fixed in the file,
  found on the search path or by its full path, never in the list's own folder and never off this computer
  (`ActionProgram`); each argument is one text after its names are filled, never joined into a command line, and a
  value with a line break is refused as an argument (`input` takes any text); before its first run on this
  computer, and after any change of the action in the file, a notice carries the bundle's AI caution (AX-11.12; the
  maintainer's decision of 2026-11-21, since an action may be an AI's): its name is "Run <name>? An AI may have
  written it: read the explanation before you run it", heard every time, and its text begins with "Read this
  first" and `AiCaution.Action` (who wrote the action is not known; it can be wrong; axKit checked none of it; an
  AI may have put it there because its own checks refused the command; running it is the user's own act). Under
  "The action" it then names the file the action is written in, what it does, the program by its full path, the
  folder, each argument as it is for the item on a line of
  its own with hidden characters written out, its input (not shown when it holds what was typed unseen) and what is
  set after; "axKit does not know what this program does or reaches. Running it is your own choice."; the answers
  Cancel, where Enter is, Run it once, and Run it, and do not ask again for this action, whose acceptance is the
  seal of TK-7a.3 over the list's folder and the whole action (`ListActions.MarkText`). A run asked for by an AI
  tool is asked about whatever was accepted, and an action with a field typed unseen is never offered to the AI
  tools (AF-7.5). The run is off the window's thread, one at a time, in a job that ends with the window: "Running
  <name>", then the done chime (`EventKind.TurnDone`, the sound of a finished command everywhere; the maintainer's
  choice of 2026-11-21) and "<name>: done" with what its `then` set, written as one undo step, or "<name> failed, exit code 1"
  with the failure sound and its output opened; "<name> was stopped after 60 seconds, its time limit"; Stop the
  running action: "<name> stopped". The output, both streams, its last 64 KB, is kept in memory for Read the last
  output and never logged; while a program runs the status bar's second part starts with "Running <name>."
  (AF-7.6). A `terminal` action (AF-4.5, AF-7.4): its `command`, a list of texts with the program first, is filled
  for the item and handed to a new axTerminal window in the action's folder, each part as an argument of that
  process; axTerminal makes the one line for its shell, each part quoted by the shell's rule, puts it into its
  command field and sends nothing (axTerminal's SPEC AT-1.5a). Asked about as a program is, before the first time
  and after any change of the action, and every time for an AI tool: "Put <name> into axTerminal?" with the same caution in its name and at the
  start of its text, then the folder
  and each part as it is for the item on a line of its own, "axTerminal puts the command into its command field and
  does not send it: you read it there and press Enter yourself. axKit does not know what the command does or
  reaches. Sending it is your own choice."; the answers Cancel, where Enter is, Put it there once, and Put it there,
  and do not ask again for this action. Then "<name> is in axTerminal's command field". Only in a folder on a drive
  of this computer. The rules are `AxTask.Core` (`ListActions`, `ActionValues`,
  `ActionProgram`), the window `TaskForm.Actions.cs`; tested in `ListActionsTests`, `ActionKeysTests` and
  `tests/scripts/axTask/19-list-actions.txt`.
- **TK-7a.3 A key that moves or opens is said first when the list's file was not this install's** (2026-11-16,
  bundle `repairs.md` R-72). A `list.json` can come from elsewhere (a shared folder, a sync, another program
  rewriting it), and its `keys` decide what Ctrl+1 to Ctrl+9 do. So an action that moves the item or opens something
  (a `set` is one undo step in the list and is never asked about) runs at the first press
  only when the user accepted it before (until 2026-11-21 also when this install had written it in the form Actions,
  Action keys, which went). Otherwise the first press
  does nothing and says what the key now does: "Ctrl+2 in this list now moves to ../Later. Press it again to do
  it." (for an action chosen in the menu, its name and "Choose it again"; the words are `ConfiguredAction.Described`); the same action run
  again, still the same in the file, does it and accepts it, so that it is not asked about again. Another key, or
  the same key after the file changed, starts over. The acceptances are kept in axTask's settings as
  `acceptedActionKeys`, the newest 500: each is the bundle's seal (AX-5.6, `SettingsSeal.Of`) over the list's
  folder and the whole action as the file holds it (`ListActions.MarkText`; for a Ctrl+digit key accepted before
  2026-11-21 the list's folder, the digit and the action as the earlier file held it, `ActionKeys.MarkText`, which
  still counts, so that the conversion of a list asks about nothing again), so that a line
  written into the settings file accepts nothing, the file says nothing of a list, and another computer's
  acceptances do not count on this one. Where the seal has no key, the second press still acts and nothing is
  kept. A list made before this version is asked about once for each such key. The item of Context, Actions of
  this list, does the same as the key. Tested in `ActionKeysTests`; the window in `tests/scripts/axTask/04-action-keys`.
- **TK-7a.2 axTask is local only; the GitHub lists are gone** (the user's decision, 2026-11-14; RP-7, RP-D10).
  axTask makes no network request and keeps no token. From 2026-11-07 to 2026-11-14 a list whose `list.json` had
  `"github": {"repository": "owner/name"}` filled itself from that repository's issues, and its items could change an
  issue with the user's token; all of it was removed: Actions → New GitHub list…, the reading when a list opens and on
  F5, Context → GitHub with its actions, Open on GitHub and Open in axClaude, Actions → GitHub account… and Forget
  GitHub account, and the `axClaude` target of an `open` key, which read the `"folder"` of that member. A list made
  as a GitHub list stays, as an ordinary local list: its items are files like any other, its `"github"` member is
  kept in `list.json` untouched and means nothing (as everything axTask does not know, IA-4), nothing is read,
  refused or reported, and its action keys work as in every list: a key that sets Kind or State writes the field in
  the file, a key that opens the Issue field opens the browser, and a key whose `open` is `axClaude` says "No field
  called axClaude, and no app called that". A token an earlier version kept stays in
  `%LOCALAPPDATA%\AxKit\github.token`, unused; no code reads or deletes it. A complex interaction such as GitHub gets
  an app of its own, an access layer over the `gh` CLI on the shared item window (`docs/axKit/plan.md`, Still to
  come: axGitHub).

## 7b. Project folders and the search across every list (2026-11-11)

The user, 2026-11-10: "run a script to find axtask databases and bring them into some aggregated list ... offer the
ability to check for items that are across many lists ... keep track of many lists across projects". Its first three
parts are built here: the folders of lists found, one place to reach them, and the search. Notifications of items due
are later (the priorities item of the request); the AI tools over the lists are section 7c.

- **TK-7b.1 Project folders.** Options → Project folders → Add a project folder… asks "Type the folder's path, or
  leave it empty for the folder picker": a typed path must be a whole path to a folder that is there, not one already
  and not the top folder or inside it ("No folder is there: …", "Projects is a project folder already", "That is your
  own top folder: its lists show at the top already", "That is inside your own top folder: …", the notice kept; the
  folder picker's choice is refused the same way, said); an empty field opens the
  Windows folder picker once the notice has closed (the notice says that in the picker Enter opens a folder and the
  Select Folder button chooses the one it is in). Either way the window goes to the user's own top level, the new
  folder's heading selected and the focus in the navigator, and says "Added Projects in Work, looking for lists",
  after the picker half a second later, once the window is in front again (TK-D12). Remove a project folder ▸ lists
  them as "1. Projects (C:\…)", or one item "No project folders yet"; a choice says "Removed Projects". When it removes
  the folder a project shown was found under, the window goes to the user's own top level and says so, "Removed
  Projects. Lists, 3 lists" (TK-D10). They are kept in axTask's settings as `projectFolders`, full paths; none by
  default (TK-D6).
- **TK-7b.2 What is found.** A folder of lists, a folder holding at least one list (a folder with `list.json` at its
  top, or whose Archive holds one when every list was archived, so that an Archive is never a project of its own),
  under a project folder, the project folder itself included, to four folders down; hidden folders, `.git`,
  `node_modules`, `bin` and `obj` are never looked into, nor a folder of lists already found, nor the top folder or
  anything in it (TK-D7). The look reads every item of every project found too (TK-5.6), in the same walk. It runs off
  the UI thread at the start, on F5 at the top, when Left leaves a project for the user's own top, and after a project
  folder is added or removed; a search does it again and keeps what it read. A list is recognised by its `list.json` alone: a
  folder of lists whose lists were all made by hand without one (axTask itself always writes it) is never found, and
  so cannot be reached in axTask to change a list's details; it needs a `list.json` in one of its lists, made by
  making one list in it with axTask (Session, Top folder on it, then Ctrl+Shift+N) or by adding the file. When it
  ends, the top level is read again only if what was found or any project's item changed, nothing said: the selection
  stays, and a row whose words changed is rewritten in place ("Projects in Work, heading level 2, looking for lists"
  becomes "…, 2 projects"); a level with other rows is filled again on the row selected. While a notice or a rename in
  place is open, the level is read again when the last notice closes.
- **TK-7b.3 Projects.** While project folders are set, the user's own top level has the heading **Projects** after
  Your lists, "Projects, heading level 1, 2 project folders", and under it a heading of level 2 for each project
  folder in the order they were added, "Projects in Work, heading level 2, 3 projects" ("looking for lists" until the
  first look ends, "none found" when it found nothing), each followed by the projects found under it: "tasks in alpha,
  project, 2 lists", by its folder's name, with the folder above it when two share a name ("tasks in alpha"), and
  that folder's path when two still share it ("tasks in alpha (D:\Work\alpha)"). A project under two project folders
  shows under the first. Right or Enter on a project opens it as a top folder of its own, "tasks in alpha, 2 lists,
  Your lists, heading level 1, 1 list": its headings, All tasks and smart lists, its Archive, every change and Ctrl+Z
  exactly as in the user's own folder (TK-D8). Left from a project's top goes back to the user's own top level on the
  project's row. On a project or a project folder's heading Ctrl+Shift+N, F2 and Delete make,
  rename and move nothing and say why (Ctrl+Shift+N: "A project's lists are made inside it: Right
  on a project first"; Delete: "A project stays: Options, Remove a project folder removes the folder it was found in"), and in
  the found level "Lists are made among the sections: Left first"; Ctrl+E shows a project's folder, or the project
  folder of its heading, in axFile. A project is never remembered as the list open last, nor a found item opened (TK-7b.4): the list
  open last of the user's own top folder stays as it was, so that the next start opens it, where before a project's
  list opened from the search cleared it and the next start opened at the top (TK-D10).
- **TK-7b.4 The search across every list.** Ctrl+Shift+F, anywhere in the window, asks "Type the words to find in
  every list" (the last words typed, again). Every list of the user's own top folder and of every project is read off
  the UI thread, the Archive left out as All tasks leaves it, and an item holding every word in its Title or a text
  field (text and long text), case not minded, is found. "Searching every list" is said as the notice closes. What a
  search finds is dropped, nothing said, when another search was started meanwhile or when a notice or a rename in
  place is open as it ends, so that the focus stays where the user took it; Ctrl+Shift+F asks again with the words
  kept. Nothing found: the edge earcon and "Nothing found for zebra",
  the level unchanged. Otherwise the level "Found tap, 3 items", one row per item, "Fix the tap, in tasks in alpha /
  Home", the user's own lists first, named by the top folder's name. Right or Enter opens the item's list where it
  lives (in its project, when it is in one) with the item selected: "Home, 1 item, Fix the tap"; Left, Backspace or
  Escape goes back where the search was started, the row selected then selected again. In the found level Tab shows
  the item's fields, which change as anywhere, and Ctrl+Enter marks a task done in its own file, the selection staying on
  it (done tasks stay in the found level, Hide done or not); Space selects what was found, for Ctrl+Enter on several, and
  Escape gives a selection up before it leaves the search (TK-4.7); F2, Delete and a link's change say "Open it where it
  lives first", Ctrl+N "Items are made in a list: open the item where it lives first, Enter", the action keys "Action keys act in a list: open one first" (TK-D9).
- **TK-7b.5 The top folder** is never moved or changed by any of this: a project is shown, entered and changed where
  it is, and the user's top folder stays the one in the settings.

## 7c. AI tools over the lists (2026-11-11)

The second part of the user's request of 2026-11-10, "everything in axkit should be very clearly operational via mcp
servers": an AI client (axHelp's conversation, Claude Code) reads the lists and adds an item by words ("remind me
next week to call the dentist") without driving keys. The tools are the bundle's (`docs/axKit/mcp.md` section 9,
MC-8); this section is what the axTask window does with them. Code: `TaskForm.Mcp.cs`, the pure half
`AxTask.Core/TaskTools`.

- **TK-8.1 What is read.** `axkit_task_lists` gives the lists the window reaches: its own top folder's (the one in
  axTask's settings, even while a project is shown as the top), by their own name ("Personal"), then the lists of each
  project found under the project folders (TK-7b.2) as "project / list" ("alpha / Home"; "tasks in alpha / Home" when two projects' folders are both named tasks and
  the project is told apart by the folder above, TK-7b), each with its
  place, its number of items and of tasks not done; the Archive and the smart lists never. While the first look for
  projects has not ended, the answer says so and gives the top folder's lists. `axkit_task_items` gives one list's
  items, each with its title, whether it is done (a task only) and its fields as title, type and value (a choice with
  its options); done items are left out and counted unless `done` is true. Reading changes nothing and says nothing.
- **TK-8.2 A list by its name.** The name as `axkit_task_lists` gives it, case not minded; a list's own name alone when
  exactly one list reached has it ("Home"); or its full path when that is one of the lists reached. A path anywhere
  else is refused: "… is not one of the axTask window's lists: AI tools reach only the lists of its top folder and of
  its project folders" (MC-D12). A name that names none or several says so with the names to choose from.
- **TK-8.3 An item added.** `axkit_task_add` makes the item as Ctrl+N does: the fields of the list's configuration
  (TK-4.0; the default Done, Start and Due for a folder that has none yet; nothing is added to a list of the earlier
  format or to one whose configuration does not read, and the answer says what the user does first), the Title given (one line of at most 200 characters, since the window says it
  whole: "A title is at most 200 characters: put the rest in Notes."; no control character, a tab or an escape, in the
  title or a field's name, since 2026-11-12: "A title holds no control characters."), into the list named (from 2026-11-12 to 2026-11-14 a GitHub list was refused; there is none now, TK-7a.2), at a free file name (`ItemFiles.FreePath`, so nothing is ever
  overwritten), then each value given typed into its field as the field's own change takes it: a date as 2026-11-18,
  today, tomorrow, +7 or a weekday; a choice as one of its options; a check as true or false. A field the list does
  not have is refused with the fields it has ("Notes is not a field of this list. Its fields: Done, Start, Due."),
  since its value would be kept in the file and shown to no one (MC-D13 as changed on 2026-11-21; added after the
  others until then). A link,
  a password or a user name, the Title among the fields and a value a field does not take are refused with the
  field's own reason ("Priority: Not one of the options: High, Normal, Low"), and nothing is written. It is one step of
  the window's undo, so Ctrl+Z takes it back ("Undone: Claude added Call the dentist to Personal").
- **TK-8.4 Heard.** The window says "Claude added Call the dentist to Personal" (the client's name as MC-7.5 gives it),
  and the panes are filled again: the new item is selected when its list is the one shown, otherwise the selection
  stays where the user left it (MC-D15). In a window behind, the chime and the taskbar flash of MC-7.5 instead.
- **TK-8.5 When it is refused.** An add needs Let AI tools run commands on (MC-7.2), and is refused while the window
  shows a notice or a Windows dialog, while a rename is open in it, and in the window in front within 3 s of the
  user's key (MC-7.8), each with its reason. It never changes or deletes an item (MC-D11).

## 7d. The maintainer's approval of outside reports (2026-11-21)

The process is `docs/axKit/outside-reports.md` (OR-n.m); axTask's part of it:

- **TK-9.1 Approve and Decline.** The Item menu (and so Context) has "Approve this report..." and "Decline this
  report...", for an entry of the list `outside-reports` that is `Awaiting approval` (OR-5.1, OR-5.2; `TaskForm.Approvals.cs`,
  `AxTask.Core/OutsideReports`). Anywhere else, or on another state, the refusal sound and why. The notice starts
  with the focus in its text, so that the entry is read first, and Tab goes to the passphrase field (OR-5.1); an entry
  with a character that is not spoken is not signed, and the refusal names the field and the character (OR-5.6).
- **TK-9.2 Approval passphrase.** Options, "Approval passphrase...", with the outside reports list open, sets up the
  key the first time and changes its passphrase after (OR-6.1, OR-6.3). Its notices say where the key file is kept,
  and for a change the fingerprint of the key in use; a change is refused while the last one is not pinned yet
  ("The last change of the approval passphrase is not pinned in the code repository yet").
- **TK-9.3 Never for the AI tools.** The three items are withheld from them (`CommandPalette.NeverForAiTools`) and
  marked act; the passphrase field is hidden, and nothing of it is kept (OR-5.4, OR-5.5). They are the first instance
  of the buttons and forms in items the maintainer asked for the same day (the priorities list, "a general framework
  of buttons and forms").

## 8. Decisions

- **TK-D1 The All tasks entry is not a file.** It is the one smart list nobody can delete or break; every other one is
  a file the user owns.
- **TK-D2 Done is a field by its title**, "Done", not a special key in the format: the format stays the shared one, and
  a script sets it as any other field. The cost: a user who renames Done in a list's configuration turns its tasks
  into plain items (the values of the old name are kept, AF-11.3); the list's line "Fields of its items" names the
  fields there are.
- **TK-D3 No index or cache.** All tasks and the smart lists read every file under the folder each time they open. A
  few thousand small files read in well under a second; an index waits until a real folder shows the need, and then
  it is a plain file too.
- **TK-D4 Lists hold items, not lists** (the user, 2026-10-24, over keeping folders inside folders). Sections group
  the lists, so a second level of lists is not needed to sort them, and Right and Left stay one step: into a list,
  back to the sections. A folder inside a list is not shown and is counted with the files not shown; so are items kept
  in the top folder itself.
- **TK-D5 A list's section is in its own details** (the user's choice of both ways in, 2026-10-24): no file of
  sections to keep in step; a list moved, synced or archived keeps its section; a script sets it as any field.
- **TK-D6 Project folders are named, never the whole disk** (taken for the user, who was away, 2026-11-11): the
  request said "not the whole disk", and a search of every drive would be slow and find folders the user never meant.
  One notice adds a folder, by a typed path or, with the field empty, by the Windows folder picker (the shape of
  axTerminal's Add a shell, AT-1.6a), so that one menu item serves both ways in.
- **TK-D7 A project is a folder holding lists** (2026-11-11): the request's "a folder with list.json at its top" is a
  list, and a list alone is not a folder axTask opens as a top; its parent is. So what is found is the folder above a
  `list.json` folder, which is what a top folder is, and it opens with everything a top folder has. Four folders down
  covers `Projects\client\repo\tasks`; the folders skipped are the ones that hold thousands of files and no lists.
- **TK-D8 One row, then the projects as top folders** (2026-11-11; the one row became the Projects heading, TK-D13, and
  the gathered All tasks was asked for the same day, TK-D15): the top level gains one row, not a section, so the
  sections stay as they were and a user without project folders sees no change. A project entered replaces the top
  folder for as long as it is shown, so the smart lists, the Archive, the links and the undo all work there unchanged;
  the window holds one folder of lists at a time. A gathered All tasks across every project waits until it is asked
  for (TK-D3's no index or cache stands; the search reads every project once per search).
- **TK-D9 The search is a level of its own, read-only for what moves** (2026-11-11): Ctrl+Shift+F is free in axTask
  and is axFile's filter key, the nearest meaning; Ctrl+F stays the bundle's find in the navigator. A found item's
  file is changed in place for its fields and Done, but a rename, an archive, a link and an action key need the
  item's own folder of lists (its Archive, the links to it), so they say to open it where it lives first. All the
  words must be in the item, as a search box usually means.
- **TK-D10 Leftovers of the project folders' review** (taken by the assistant on 2026-11-11, the user away, for the
  user to confirm): Use these fields for new items on a found item refused as F2 and Delete do, since it changed the
  item's list, a thing of where the item lives, not of the item (the command went on 2026-11-21, when the fields of a
  list's items became its configuration's, TK-4.0); New GitHub list refuses where New list does; the
  last project folder removed leaves a project shown for the user's own top level, since nothing would lead back to
  it; opening a list in a project or a found item leaves the list open last alone, as the task
  asked, a found item in the user's own folder included (the user's own place is the list open before the search);
  and a list without `list.json` is not looked for (TK-7b.2), since a
  folder of plain folders is not told from any other folder of folders without it.
- **TK-D11 The AI tools act through the open window** (taken by the assistant for the user, who was away,
  2026-11-11, to confirm; `docs/axKit/mcp.md` MC-D10 to MC-D16): the window already knows the folders, the template,
  the undo and the refresh, and shared code never references an app. Reading is free once paired; adding needs Let AI
  tools run commands and is said; changing and deleting wait for a later version with the question of MC-7.3.
- **TK-D12 The folder picker opens after the notice closes** (2026-11-11, from the user's pass by ear: the folder was
  added, but "Added" was never heard, the empty field was read again and the folder was nowhere to be found). The
  picker gives the focus back to the control it took it from, which was the notice's field while the notice was still
  closing; it now opens from the navigator, and the window goes to the folder's new heading. An announcement made as a
  Windows dialog closes is lost under NVDA's reading of the window coming back, so it is said half a second later.
- **TK-D13 Two levels of headings, one shape** (the user, 2026-11-11: "make this all a bit more efficient and
  consistent for the user"; supersedes the single Projects row and level of TK-D8): every heading row reads "name,
  heading level N, what it holds", the word NVDA uses for a document's headings, so that a level is heard the same way
  in axTask as on a page. Five headings of level 1, those of level 2 and 3 under them (TK-D20); a heading that would hold nothing is
  left out (Projects without project folders, Tags without any, Archived lists with an empty
  Archive) except Your lists, which says how to make one. User lists is now Your lists. A project shown as the top
  has no Projects heading: projects are reached from the user's own top.
- **TK-D14 Ctrl+1 and Ctrl+2 go by heading at a top level** (2026-11-11): the level keys of a browse mode, on the
  digits; there the list's action keys never act (they need a list), so each place gives the keys one meaning, as
  Ctrl+Up does in a list and in a field. Ctrl+Shift+1 and Ctrl+Shift+2 go back, as Shift goes back with NVDA's
  letters. Ctrl+0 stays All tasks; Ctrl+3 to Ctrl+9 at the top still say "Action keys act in a list".
- **TK-D15 The smart lists gather every project** (the user, 2026-11-11; what TK-D8 left for later): one place for
  "#ui" across projects. The user's own items are read on each fill, as before; the projects' are read off the UI
  thread by the look for projects, the same walk the search makes, and kept until the next look, so TK-D3's "no cache"
  now holds for the folder shown and not for the projects (a file the window changes itself is read again at once; a
  change made outside axTask shows after F5). A gathered item changes in its own folder of lists, rename, Archive and
  links included, where the found level still says "Open it where it lives first" (TK-D9): a smart list's rows are
  the user's working view, the search's a jump. The user's own items are read "in Home", without the top folder's
  name, a project's "in alpha / Home".
- **TK-D16 A mention or a hashtag is a word after its mark** (taken by the assistant, 2026-11-11; widened to any
  symbol the same day, TK-D18): the common reading of "@" and "#" in notes; never after a letter or a digit, so that
  an address and "C#" are none; the tasks not done only, as All tasks, so that a finished project's tags fall away by
  themselves.
- **TK-D17 Smart lists grouped by their first word** (the user, 2026-11-11): the user names filters "App axClaude",
  "App axDown", "Tag ai", "Tag bug", so the first word is already a group; a heading per shared word makes forty
  filters a handful of headings, Ctrl+2 between them, with no setting and no file renamed. A first word only one
  filter has is no group, since a heading of one would add a row and say nothing. On the user's own lists the group
  "Tag" falls just before Tags, by natural order: the neighbourhood the user wished for came by itself, and nothing
  places it there.
- **TK-D18 A tag is any symbol before a word** (the user, 2026-11-11, generalizing Mentions and Hashtags into one Tags
  heading): "@", "#", "!", "$", "%", "&" and any symbol the user picks start a tag, each symbol a heading of level 3,
  so one rule covers them all and a new symbol needs no change in axTask. The excluded symbols are those that open
  prose or Markdown ("(note", "-item", "*bold*", a quote); "!" and "?" are allowed, since after a word they never start
  one, and "!urgent" is a tag. A letter must follow the symbol, so "$5" and "#1" are none. Since NVDA may not read a
  bare symbol, a level-3 heading names it from a small table of names written in axTask (no lookup in the system's
  character data), falling back to the code point. A word that ends in its own symbol is a placeholder, not a tag:
  "%USERPROFILE%" and "$VAR$" are none (seen on the user's own lists, 2026-11-11, where a path made "%USERPROFILE" a
  tag).
- **TK-D19 "@" means a person the item is assigned to** (the user, 2026-11-11): "@kyle" in a task assigns it; the
  "@ at sign" heading lists the people with their tasks. Notifications per person, and anything else an assignee
  does, are later work (the priorities item "axTask assignees from @ with notifications per person"). The direction
  beyond it, also the user's: any symbol may later be tied to any kind of thing (a person, a place, a budget) and to
  handlers that act on its tags; nothing of that is built, and the rule of TK-D18 is the ground it stands on.
- **TK-D20 Three heading levels; Ctrl+3 by the third** (the user, 2026-11-11): the symbols under Tags are level 3,
  reached by Ctrl+3 and Ctrl+Shift+3 at a top level as Ctrl+1 and Ctrl+2 reach theirs (TK-D14), Ctrl+3 staying
  action key 3 in a list. Ctrl+Up and Ctrl+Down still go to the heading before or after, of any level. The names of
  the keys are axDown's ("Previous heading level 3"), so that the same key is the same thing in both windows.
- **TK-D21 Ctrl+0 for a level above 1, if one ever comes** (the user, 2026-11-11): no heading above level 1 exists,
  so Ctrl+0 stays All tasks. If a level above Your lists, Smart lists and the rest is ever added, Ctrl+0 at a top
  level goes between those headings, as Ctrl+1 does between its own, and All tasks keeps its menu item.

## 9. Open questions

- Reminders (TK-6) need a process that runs when the window is closed. axStart's is the candidate, which would make
  axTask's rules a second use of the background process.
- How a link is set (IA-4.2's search over the titles) across lists, and whether All tasks shows the items that wait
  on others.
