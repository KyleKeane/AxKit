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
its fields in the detail pane (Ctrl+Tab, Ctrl+Shift+Tab and F6 switch the panes too, the bundle's keys, AX-8.2). Space
marks a task done.

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
4. Space: "Done: Fix the tap". Ctrl+Z: "Undone: Done: Fix the tap".
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
  detail pane of a list, empty for Your lists. Enter on it changes it as any field; **List, Move to section**
  (Ctrl+M) chooses from a list of the sections there are, Your lists, and "A new section", which asks its name. A new
  list (Ctrl+Shift+N) joins the section of the row selected. A section is renamed by moving its lists.

## 4. axTask's part of the shared shape

- **TK-4.1 The default template.** A list with no `list.json` gives a new item the fields Done (`check`), Start
  (`date`) and Due (`date`), empty.
- **TK-4.2 A task** is an item with a `check` field titled Done. An item without one is a plain item: a list of
  things that are not tasks is a list whose template has no Done (the user's ask: plain lists and checklists, and an
  easy way between them). Changing a list's template does not change its items; "Add Done to every item" and "Remove
  Done from every item" in the List menu switch a whole list between the two.
- **TK-4.3 The row summary** (IA-6): the Title, then "done" when Done is true, then "due" and the Due date when there is
  one: "Fix the tap, done, due 30 October". Overdue reads "overdue since 30 October".
- **TK-4.4 Space in the navigator** toggles Done on a task; on a plain item, the edge earcon and "Not a task".

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
- **TK-5.3 A new smart list** (List, New smart list) asks its name, writes `<name>.filter.json` with All tasks'
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
| Space | navigator | done or not done (TK-4.4) |
| Ctrl+J | anywhere | open the item, the list's `list.json` or the smart list in axJSON |
| Ctrl+E | anywhere | show the file or folder in axFile |
| Ctrl+Shift+D | navigator | hide or show done tasks in this list |
| Ctrl+0 | anywhere | to All tasks (Ctrl+1 until 2026-11-07, when the digits became the action keys, TK-7a.1) |
| Ctrl+1 to Ctrl+9 | anywhere, in a list | the list's action key on the item selected (TK-7a.1) |
| Ctrl+1, Ctrl+2, Ctrl+3 | navigator, at a top level | the next heading of level 1, 2 or 3 (TK-3a.2, TK-D14, TK-D20) |
| Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3 | navigator, at a top level | the heading of level 1, 2 or 3 before (TK-3a.2) |
| Ctrl+Up, Ctrl+Down | navigator | the heading before or after, of either level (TK-3a.2) |
| Ctrl+M | navigator, on a list | move the list to a section (TK-3a.3) |
| Ctrl+Shift+F | anywhere | find words in every list, the user's own and every project's (TK-7b.4) |
| Right, Enter, Escape | navigator, in the found level | open the item where it lives; back where the search started (TK-7b.4) |

## 7a. Action keys and GitHub lists (2026-11-07)

The design is `docs/axKit/reports.md` sections 7 and 8 (RP-7, RP-8); the rules axTask keeps:

- **TK-7a.1 Action keys.** Ctrl+1 to Ctrl+9 run the action the list's `list.json` assigns to the digit under `"keys"`
  (RP-8.1), on the item selected in the navigator: `set` writes fields to values as one undo step (a field the item
  lacks is added, with the value's type), `move` moves the item to a list by a path relative to this one, the links
  following (IA-D3), `open` opens a field's web address in the browser, an app by its verb on the item's file
  (`json`, `file`; `claude` and `terminal` only in a folder on a drive of this computer, RP-8.1), or `axClaude` on an
  issue (TK-7a.3). A digit without an action: the edge earcon and "No action
  on Ctrl+3 in this list; List menu, Action keys sets one"; outside a list, "Action keys act in a list: open one
  first". List → Action keys… lists the nine keys ("Ctrl+1. Make it a bug", "Ctrl+3. not set"); Enter on one asks the
  name (empty removes the key), what it does (set, move, open) and the value, and writes `list.json` as one undo step
  (RP-8.2). The Item menu's Action keys submenu shows the nine with the names the list gives them, so the palette and
  F1 list them (RP-8.3). All tasks moved to Ctrl+0 for them (RP-8.4).
- **TK-7a.2 A GitHub list** is a list whose `list.json` has `"github": {"repository": "owner/name", "state": "open"}`
  (`GitHubList.Of`), made by List → New GitHub list… in the top folder as "<name> issues" (RP-7.1). When it opens,
  once per visit, and on F5, the repository's issues in that state are read from GitHub without an account
  (`GitHubIssues.ListAsync`, off the UI thread), and the folder is brought up to them (`GitHubList.Refresh`): one
  item per issue, named "<number> <title>.json", with Number, Kind (the label among feedback, bug and feature, else
  "unlabelled"), State, Opened, Reporter (the reporter id of a report sent from axKit, or the GitHub login), Version,
  Updated, Issue (the web address), Body and Notes, which is the user's and stays across refreshes; an item whose
  issue GitHub no longer returns goes to the Archive. The refresh is a mirror, not a step: nothing to undo. It says
  "Issues, 14 items, 2 new"; a failure says "GitHub not read: no network; the list shows what was read last". An item
  made by Ctrl+N in the list starts as "unlabelled" and "open", the template's values (IA-4.3; a list made before
  2026-11-09 carries empty values in its `list.json`, which Ctrl+J puts right).
- **TK-7a.3 The GitHub actions** (Item → GitHub, and the action keys a new GitHub list comes with: Ctrl+1 to Ctrl+3
  the kinds, Ctrl+4 Close, Ctrl+5 Open on GitHub, Ctrl+6 Open in axClaude): Make it a bug, a feature request or
  feedback (the issue's labels), Close and Reopen (its state), Comment… (a notice with a field), Block this reporter
  and Block this version (the reports repository's `blocklist.json`, RP-6.4), Open on GitHub (the browser), Open in
  axClaude (axClaude started on the folder `"folder"` under `"github"` names, with `--message` putting "Issue 12:
  <title>" and the body into its message field, unsent; without the folder, it says to set it; a folder off this
  computer, a share or a mapped drive, is refused before it is touched, RP-8.1). A set of Kind or
  State in a GitHub list goes to GitHub first, as the user's own account, and the file changes only when GitHub took
  it; a refusal is said ("Not changed: GitHub refused (403)…") and nothing changes locally (RP-7.2).
- **TK-7a.4 The user's token.** List → GitHub account… takes a fine-grained token of the user's own GitHub account in
  a hidden field, kept protected for the Windows account in `%LOCALAPPDATA%\AxKit\github.token` (`UserSecret`); Forget
  GitHub account removes it. Reading a public repository's issues needs none; every action does, and says "Set your
  GitHub token in the List menu first" without one.

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
  project's row. On a project or a project folder's heading Ctrl+Shift+N, List → New GitHub list, F2 and Delete make,
  rename and move nothing and say why (Ctrl+Shift+N and New GitHub list: "A project's lists are made inside it: Right
  on a project first"; Delete: "A project stays: Options, Project folders removes the folder it was found in"), and in
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
  the item's fields, which change as anywhere, and Space marks a task done in its own file, the selection staying on
  it (done tasks stay in the found level, Hide done or not); F2, Delete, a link's change and Item → Use these fields
  for new items say "Open it where it lives first", Ctrl+N "Items are made in a list: open the item where it lives first, Enter", the action keys "Action keys act in a list: open one first" (TK-D9).
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
- **TK-8.3 An item added.** `axkit_task_add` makes the item as Ctrl+N does: the list's template (TK-4.1, the default
  Done, Start and Due without one), the Title given (one line of at most 200 characters, since the window says it
  whole: "A title is at most 200 characters: put the rest in Notes."), at a free file name (`ItemFiles.FreePath`, so nothing is ever
  overwritten), then each value given typed into its field as the field's own change takes it: a date as 2026-11-18,
  today, tomorrow, +7 or a weekday; a choice as one of its options; a check as true or false. A field the template
  lacks is added after the others, long text for Notes or a value of several lines, text otherwise (MC-D13). A link,
  a password or a user name, the Title among the fields and a value a field does not take are refused with the
  field's own reason ("Priority: Not one of the options: High, Normal, Low"), and nothing is written. It is one step of
  the window's undo, so Ctrl+Z takes it back ("Undone: Claude added Call the dentist to Personal").
- **TK-8.4 Heard.** The window says "Claude added Call the dentist to Personal" (the client's name as MC-7.5 gives it),
  and the panes are filled again: the new item is selected when its list is the one shown, otherwise the selection
  stays where the user left it (MC-D15). In a window behind, the chime and the taskbar flash of MC-7.5 instead.
- **TK-8.5 When it is refused.** An add needs Let AI tools run commands on (MC-7.2), and is refused while the window
  shows a notice or a Windows dialog, while a rename is open in it, and in the window in front within 3 s of the
  user's key (MC-7.8), each with its reason. It never changes or deletes an item (MC-D11).

## 8. Decisions

- **TK-D1 The All tasks entry is not a file.** It is the one smart list nobody can delete or break; every other one is
  a file the user owns.
- **TK-D2 Done is a field by its title**, "Done", not a special key in the format: the format stays the shared one, and
  a script sets it as any other field. The cost: a user who renames Done turns tasks into plain items; the Fields
  of a new item line says so when the template has no Done.
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
  user to confirm): Use these fields for new items on a found item refuses as F2 and Delete do, since it changes the
  item's list, a thing of where the item lives, not of the item; New GitHub list refuses where New list does; the
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
  character data), falling back to the code point. Seen on the user's own lists: "%USERPROFILE" from a path written
  as "%USERPROFILE%" is read as a tag; a later rule may leave out a word that ends in its own symbol.
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
