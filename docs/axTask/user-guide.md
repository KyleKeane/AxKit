# axTask user guide

axTask keeps tasks and lists as a folder of small files, one file per item, and shows them the way axFile shows a
folder. It is part of axKit, a bundle of apps for people who use a screen reader; it was built for and with NVDA.

## Start

- Start menu: `axKit axTask`. Console: `axTask`, or `axtask C:\my\tasks` for a folder of your own.
- The first time, axTask uses the folder `Tasks` in your Documents folder. Session, Top folder chooses another.
- axTask opens on the list you had open last.

## The window

Two panes side by side, and a status bar.

- **The navigator**, on the left, where the focus starts. Its top level is under headings of three levels, each a
  line such as "Smart lists, heading level 1, 3 smart lists":
  - **Your lists**, with a heading of level 2 for each of your own sections;
  - **Projects**, when you have project folders, with a heading of level 2 for each project folder;
  - **Smart lists**: All tasks and your saved filters, those that share a first word under a heading of level 2 of
    it (App for App axDown and App axClaude), then **Tags** when your tasks hold any, with a heading of level 3 for
    each symbol;
  - **Date lists**: Overdue, Due today, Due in the next 7 days and No due date;
  - **Archived lists**, when you have archived anything.

  Ctrl+Up and Ctrl+Down go to the heading before or after. Ctrl+1, Ctrl+2 and Ctrl+3 go to the next heading of
  level 1, 2 or 3, and with Shift to the one before; in a list these keys are its action keys instead. Up and Down move; type the
  start of a name to jump to it.
- **The detail pane**, on the right. It shows what the navigator's selection holds, without saying anything until
  you go there with Tab:
  - on an item, its fields: "Due, date, 30 October 2026, Friday";
  - on a list, the list's own details, and a line naming the fields a new item gets;
  - on a smart list, what it shows.

## Moving around

- Right goes into a list. Left or Backspace goes back to the sections, with the list you came from selected. A list
  holds items, not other lists: your own sections group the lists instead.
- Right on an item does nothing; you hear the edge sound, as in axFile on a file.
- Enter opens: on a list it shows the list's items; on an item it moves you to the item's fields, as Tab does.
- Tab, Ctrl+Tab or F6 go to the detail pane. Escape, Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6 come back to the navigator.
- Ctrl+0 goes to All tasks from anywhere.
- At either end of a pane, Up or Down plays the edge sound.

## Sections of your own

- On a list, Ctrl+M (List, Move to section) chooses its section: Your lists, one of your sections, or a new one.
- The same is the Section field, first in a list's details: Tab on the list, Enter on Section, type the name.
- A new list (Ctrl+Shift+N) joins the section of the line selected.
- A section exists while a list is in it; to rename one, move its lists.

## Tasks and plain lists

- A task is an item with a **Done** check. Space marks it done or not done, in the navigator or on the Done field.
- A list without Done is a plain list, for things that are not tasks. List, Add Done to every item makes a plain list
  a checklist; List, Remove Done from every item makes it plain again.
- A row reads its title and, for a task, "done", "due 30 October" or "overdue since 22 October".
- Ctrl+Shift+D hides the done tasks of a list, and shows them again.

## Changing things

In the navigator:

- Ctrl+N adds an item to the list shown. It gets the fields the list gives new items: Done, Start and Due unless
  you choose others.
- Ctrl+Shift+N makes a new list.
- F2 renames the item or the list in place: a box over the line holds the name; type the new one and press Enter,
  or Escape to keep the old. Links to it from other items are kept.
- Delete moves the item or the list into the Archive: an archived list is under **Archived lists**, archived items
  under its line "Archived items". Ctrl+Z brings it back.
  To delete something for good, show it in axFile with Ctrl+E and delete it there.

In the detail pane:

- Enter changes the field. A notice opens with the value:
  - a date takes 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday such as friday;
  - a date and time takes a date, a space and a time: tomorrow 14:00;
  - a choice shows a list of its options; Up and Down move, Enter chooses;
  - a long text is a field of several lines: Enter makes a new line, Tab and then Enter on Save keeps it;
  - a link takes the title of the item it points to, in any list.
- Ctrl+N adds a field after the selected one: its title, then its type from a list; a choice then asks its options.
- F2 renames the field in place, Delete removes it, Alt+Up and Alt+Down move it. The Title field stays.
- Item, Use these fields for new items gives the list's new items the fields of the item selected, emptied.

Every change is written to the file at once, and Ctrl+Z takes back the last ones, one at a time. If another program
changed the file since axTask showed it, axTask reads it again and says so instead of writing over it.

## Smart lists

All tasks is every task not done, in every list but the Archive, the soonest due first. List, New smart list makes
one of your own: a small file whose conditions start as All tasks' and open in axJSON to change. For example, the
tasks of the Work list due within a week:

```json
{
  "format": "axkit-filter 1",
  "match": "all",
  "conditions": [
    { "field": "Done", "is": false },
    { "list": "Work" },
    { "field": "Due", "before": "+7" }
  ],
  "order": [ "Due", "Title" ]
}
```

A condition is one of: a field `is` a value; a date field `before` or `after` a date or a count of days from today
(the day itself included); a field `empty` or not; the item in a `list` or the lists inside it; a text field `has` a
word. `match` is `all` or `any`.

Smart lists whose names start with the same word, such as "App axDown" and "App axClaude", are grouped under a
heading of that word, "App"; the file names stay as they are.

Under Smart lists, the heading **Tags** holds a list for each tag written in the title or a text field of a task not
done: a symbol at the start of a word, then the word, such as "@Sam", "#garden", "!urgent", "$budget" or "%done".
Each symbol has a heading of level 3 of its own, read by name, "@ at sign" or "# number sign". "@" stands for the
person a task is assigned to. Symbols that start ordinary writing never make a tag: brackets, quotes, the Markdown
marks, "-", "/", ".", ",", ";", ":", "=", "+" and "|"; a symbol before a digit ("$5") or after a letter (an e-mail
address) makes none either. Write "Call @Sam about #garden" and both appear.

Under Date lists, Overdue, Due today, Due in the next 7 days and No due date sort the tasks not done by their Due date.

When you have project folders, every one of these lists takes the tasks of your own lists and of every project
together: "#ui" in two projects is one list. Each line says where the item lives, "Fix the tap, in Home" for yours,
"Fix the tap, in alpha / Home" for a project's, and whatever you do to it there is done in its own file and its own
folder of lists. A change made outside axTask to a project's file shows after F5.

## The files

Each item is a JSON file named after its title, a list of fields each with a title, a type and a value:

```json
{
  "format": "axkit-item 1",
  "fields": [
    { "title": "Title", "type": "text", "value": "Fix the tap" },
    { "title": "Done", "type": "check", "value": false },
    { "title": "Due", "type": "date", "value": "2026-10-30" }
  ]
}
```

A list is a folder in the top folder; its details are `list.json` in it, its section among them. Ctrl+J opens the item, the list's details or a smart list in
axJSON; Ctrl+E shows it in axFile. Anything axTask does not know in a file is kept as it is.

## Action keys: Ctrl+1 to Ctrl+9

Every list can give the number keys actions of its own. List, Action keys lists the nine keys; Enter on one asks a
name, what the key does and its value: set fields to values ("Kind: Bug", "Done: done"), move the item to another
list, or open something (a field holding a web address, or an app such as axJSON for the item's file). From then on
Ctrl+1 to Ctrl+9 do that to the item selected, and the Item menu's Action keys submenu names them. Ctrl+Z takes a set
or a move back. All tasks is Ctrl+0.

## GitHub lists

List, New GitHub list asks for a repository as owner/name and makes a list of its open issues, read from GitHub when
the list opens and on F5, with no account needed. Each issue is an item: its number, kind, state, who reported it, the
axKit version when it came from axKit, the address, the body, and Notes, which are yours and stay. The Item menu's
GitHub submenu changes an issue as your own GitHub account: make it a bug, a feature request or feedback, close or
reopen it, comment, block a reporter or a version of axKit that sends nonsense, open it on GitHub, or open it in
axClaude with the issue in the message field. For that, List, GitHub account takes a token of your GitHub account
once (a fine-grained token with Issues and Contents on the reports repository), kept protected on this computer. A new
GitHub list comes with action keys: Ctrl+1 to Ctrl+3 the kinds, Ctrl+4 Close, Ctrl+5 Open on GitHub, Ctrl+6 Open in
axClaude. axKit's own reports arrive in the maintainer's reports repository this way; `docs/axKit/reports.md` tells
the whole story.

## Lists in other projects

Your other folders of lists, such as a `tasks` folder in each code project, can be reached from this window without
moving them.

- Options, Project folders, Add a project folder: type the path of a folder that holds your projects, such as
  `D:\Projects`, and press Enter; or leave the field empty and press Enter to choose it with the Windows
  folder picker: there, Enter opens a folder and the Select Folder button chooses the one you are in. axTask then
  shows your own lists with the new folder's heading selected, "Added Projects in Projects, looking for lists".
  axTask looks four folders down in it for every folder that holds a list. A list is known by its
  details file, `list.json`, which axTask writes for every list it makes; a project whose
  lists were all made by hand without one is not found: make one list in it with axTask (Session, Top folder on its folder of lists, then
  Ctrl+Shift+N), or add a `list.json` to one of
  its lists. Remove a project folder takes one off; when you remove the one a project you are in was found under,
  you are taken back to your own lists. There are none until you add one. Your own top folder, or a folder inside it, is refused: its lists
  show at the top already.
- While project folders are set, your top level has the heading **Projects** after Your lists, and under it a heading
  of level 2 for each project folder, "Projects in Work". It says "looking for lists" at first and changes quietly
  when the look ends; under it are the projects found, each by its folder's name and its number of lists ("tasks in
  alpha" when two folders share a name, with that folder's path when that is shared too). A folder whose lists were
  all archived is shown as a project with 0 lists.
- Right on a project opens it the way your own folder is shown: its headings, All tasks, smart lists and Archive.
  Everything you change there is changed in that project's files, and Ctrl+Z works as usual. Left from its top
  goes back to your own lists, on the project. Lists are made inside a project, not on its line. axTask starts again in the list of your own you had open last, never in a project's, even after you open an item the search found.
- Ctrl+Shift+F finds words in every list, your own and every project's: type the words and press Enter. An item is
  found when its title or a text field holds every word, in capitals or not. axTask says "Searching every list" and
  then what it found; if you open another notice before the search ends, its result is dropped and the focus stays
  where you are: press Ctrl+Shift+F again, your words are kept. Each line says where the item lives:
  "Fix the tap, in tasks in alpha / Home". Right or Enter opens the item in its own list; Left or Escape goes back to
  where you searched from. Tab shows the found item's fields, and Space marks a task done, there and then; to rename,
  archive or link it, or to use its fields for new items, open it where it lives first.

## AI tools and your lists

An AI tool such as Claude Code, or your conversation in axHelp, can read your lists and add to them while an axTask
window is open, once you have turned the AI tools on and allowed it (axKit menu, AI tools; the axKit guide says how).

- Reading: it can list your lists, your own top folder's and your projects', with how many items each has, and read
  one list's items with their fields. Nothing is said while it reads.
- Adding: only while axKit menu, AI tools, Let AI tools run commands is on. Ask it, for instance, "remind me next week
  to call the dentist": it adds the item to the list it names, made from that list's fields as Ctrl+N makes one, with
  a Due date, a Priority from the list's choices or Notes if it gives them. The window says "Claude added Call the
  dentist to Personal"; if that list is the one shown, the new item is selected, otherwise you stay where you are.
  Ctrl+Z takes it back as any change. In a window behind you hear a chime and the window flashes on the taskbar.
- It never changes or deletes an item, never reaches a folder of lists that is not your top folder or one of your
  projects, and never adds while a notice or a rename is open, or while you are typing in the window.

## Keys

F1 lists every key. Shift+F10 or the Applications key opens the menu of the selection.

## If something goes wrong

- "The item changed on disk; it has been read again": another program changed the file. Look at it again and repeat
  the change.
- A file that is not an item is not shown; the list's name counts such files.
- Options, Developer, Copy diagnostics puts what is needed for a bug report on the clipboard.

## About

axTask is part of axKit by Dr. Kyle Keane, www.kylekeane.com. axKit, About has the licence and the version.

## Disclaimer

axTask is provided as it is, without warranty of any kind. Keep a copy of files that matter.
