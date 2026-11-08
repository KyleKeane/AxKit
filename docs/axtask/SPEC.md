# axTask — Specification

axTask is an app of the axKit bundle (`docs/axkit/SPEC.md`): tasks and lists as a folder of small JSON files, for a
blind developer who uses the NVDA screen reader. It is an item app: its window, keys, field types and file format are
the shared ones of `docs/axkit/item-apps.md` (cited as IA-n.m and not repeated), and this document holds only what is
axTask's own. Written 2026-10-24 and built the same day on the branch `axtask`, not yet heard under NVDA; it took the
place of the first design, axList as Markdown lists with a Tasks view. axList is planned again beside it since
2026-11-06, for lists to remember and to arrange (`docs/axlist/SPEC.md`, not built); what is to do belongs here.
Requirements are numbered `TK-n.m` and decisions `TK-Dn`. In axTask's code and documents, "SPEC.md" means this file.

## 1. Summary

A folder of lists: `Home\`, `Shopping\`, `Course plan\`, each item a file such as `Home\Fix the tap.json`, one level
deep (TK-D4). The top level is in sections (TK-3a): **Smart lists** (All tasks, every task not done from every list
by due date, and the saved filters), **User lists**, the user's own sections, and **Archived lists**; Ctrl+Up and
Ctrl+Down move between them. Right or Enter goes into a list, Left back to the sections, Tab or Enter on an item to
its fields in the detail pane. Space marks a task done.

## 2. Goals and non-goals

- **G1 Tasks with dates.** Start and due dates as dates, done as a check, notes, priority, links to other items.
- **G2 One view of everything to do.** All tasks and the smart lists, read from the files every time.
- **G3 The files are the truth.** Every item can be opened in axJSON (Ctrl+J), changed by a script or an AI system,
  and synced by whatever syncs the user's files (bundle B-15).
- **G4 As simple as axFile.** The shape of IA-2, nothing more in the first version.
- **Non-goals for the first version.** Reminders that sound at a time, rules that act on their own (TK-6), recurring
  tasks, sharing. The format leaves room for them.

## 3. Scenarios

1. Start axTask on the folder: "Lists - axTask window", then "Lists, 4 lists list, Smart lists, section, 1 of 7".
   Down: "All tasks, 5 not done". Ctrl+Down: "User lists, section, 2 lists"; Down: "Home, list, 2 items".
2. Right: "Home, 2 items, Fix the tap, due 30 October". Tab: "Fields of Fix the tap, Title, text, Fix the tap". Down:
   "Done, check, not done", then "Due, date, 30 October 2026, Friday".
3. Enter on Due, type `+3`, Enter: "Due: 2 November 2026, Monday". Shift+Tab: "Fix the tap, due 2 November".
4. Space: "Done: Fix the tap". Ctrl+Z: "Undone: Done: Fix the tap".
5. Ctrl+N, type `Call the plumber`, Enter: "Added Call the plumber", with the list's new-item fields (Done, Due).
6. Left: "Lists, Home". Up: "All tasks, 5 not done". Right: the tasks of every list, the soonest due first, each read
   "Call the plumber, in Home".

## 3a. The top level in sections (asked 2026-10-24)

- **TK-3a.1 The sections**, in this order, each under a header row "Smart lists, section, 3": **Smart lists** (All
  tasks, then the smart lists, TK-5); **User lists**, the lists in no section of the user's own, by name; the user's
  own sections, by name, each with its lists; **Archived lists**, the lists in the Archive (IA-D4), then one row
  "Archived items, 4" when the Archive holds items of its own, which opens like a list. An empty section of the user's
  does not exist; User lists shows "User lists, section, none: Ctrl+Shift+N makes one" when there are none.
- **TK-3a.2 Ctrl+Up and Ctrl+Down** in the navigator go to the header of the section before or after the one the
  selection is in; past the first or the last, the edge sound. On a header, Right and Enter play the edge sound and
  say "A section: Down goes into it"; Down goes on to its first list. In the detail pane of a header: the number of
  its lists and how a list joins it. (In an edit control the same keys move by paragraph, AX-8.2; the navigator is a
  list, so they move by section there, the same idea.)
- **TK-3a.3 A list's section** is the field Section (`text`) of its details (`list.json`), always shown first in the
  detail pane of a list, empty for User lists. Enter on it changes it as any field; **List, Move to section**
  (Ctrl+M) chooses from a list of the sections there are, User lists, and "A new section", which asks its name. A new
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

- **TK-5.1 All tasks** is the first entry of the top level, always there and never a folder: every task in a list
  (the Archive left out) whose Done is false, the soonest Due first, tasks without a due date after them in title order. A row adds
  "in Home". Enter or Tab opens the task's fields as anywhere else; a change is written to the task's own file.
- **TK-5.2 A smart list** is a file `<name>.filter.json` in the top folder, shown after All tasks in the Smart lists
  section as "Due this week, smart list, 3". Its content is the conditions every shown item meets:

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
  as "Due this week, smart list, a condition not understood" and nothing in it.
- **TK-5.3 A new smart list** (List, New smart list) asks its name, writes `<name>.filter.json` with All tasks'
  conditions and opens it in axJSON to change them; Ctrl+J on a smart list opens it again. The detail pane of a smart
  list says what it shows, how many, and its file; the conditions are written in axJSON, not field by field, until
  the need is heard (a filter is written once and read many times).

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
| Ctrl+Up, Ctrl+Down | navigator | the section before or after (TK-3a.2) |
| Ctrl+M | navigator, on a list | move the list to a section (TK-3a.3) |

## 7a. Action keys and GitHub lists (2026-11-07)

The design is `docs/axkit/reports.md` sections 7 and 8 (RP-7, RP-8); the rules axTask keeps:

- **TK-7a.1 Action keys.** Ctrl+1 to Ctrl+9 run the action the list's `list.json` assigns to the digit under `"keys"`
  (RP-8.1), on the item selected in the navigator: `set` writes fields to values as one undo step (a field the item
  lacks is added, with the value's type), `move` moves the item to a list by a path relative to this one, the links
  following (IA-D3), `open` opens a field's web address in the browser, an app by its verb on the item's file
  (`axjson`, `axfile`), or `axclaude` on an issue (TK-7a.3). A digit without an action: the edge earcon and "No action
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
  "Issues, 14 items, 2 new"; a failure says "GitHub not read: no network; the list shows what was read last".
- **TK-7a.3 The GitHub actions** (Item → GitHub, and the action keys a new GitHub list comes with: Ctrl+1 to Ctrl+3
  the kinds, Ctrl+4 Close, Ctrl+5 Open on GitHub, Ctrl+6 Open in axClaude): Make it a bug, a feature request or
  feedback (the issue's labels), Close and Reopen (its state), Comment… (a notice with a field), Block this reporter
  and Block this version (the reports repository's `blocklist.json`, RP-6.4), Open on GitHub (the browser), Open in
  axClaude (axClaude started on the folder `"folder"` under `"github"` names, with `--message` putting "Issue 12:
  <title>" and the body into its message field, unsent; without the folder, it says to set it). A set of Kind or
  State in a GitHub list goes to GitHub first, as the user's own account, and the file changes only when GitHub took
  it; a refusal is said ("Not changed: GitHub refused (403)…") and nothing changes locally (RP-7.2).
- **TK-7a.4 The user's token.** List → GitHub account… takes a fine-grained token of the user's own GitHub account in
  a hidden field, kept protected for the Windows account in `%LOCALAPPDATA%\AxKit\github.token` (`UserSecret`); Forget
  GitHub account removes it. Reading a public repository's issues needs none; every action does, and says "Set your
  GitHub token in the List menu first" without one.

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

## 9. Open questions

- Reminders (TK-6) need a process that runs when the window is closed. axStart's is the candidate, which would make
  axTask's rules a second use of the background process.
- How a link is set (IA-4.2's search over the titles) across lists, and whether All tasks shows the items that wait
  on others.
