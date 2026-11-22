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
  - on a list, the list's own details, and a line naming the fields of its items;
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

- On a list, Ctrl+M (Context, Move to section) opens a small form: a list of the sections (Your lists, each of your sections, and A new section, the last) and, after it, a field for a new section's name. Up and Down choose, Enter moves the list there. For a new section, choose A new section, Tab, type its name, Enter.
- The same is the Section field, first in a list's details: Tab on the list, Enter on Section, type the name.
- A new list (Ctrl+Shift+N) joins the section of the line selected.
- A section exists while a list is in it; to rename one, move its lists.

## Tasks and plain lists

- A task is an item of a list whose fields have a **Done** check. Space marks it done or not done, in the navigator or
  on the Done field. An item that was never marked is not done.
- A list without Done is a plain list, for things that are not tasks. Actions, Add Done to every item makes a plain list
  a checklist; Actions, Remove Done from every item makes it plain again. Neither touches an item's file: a task marked
  done stays marked while the list is plain, and shows so again when the list is a checklist again.
- A row reads its title and, for a task, "done", "due 30 October" or "overdue since 22 October".
- Ctrl+Shift+D hides the done tasks of a list, and shows them again.

## Changing things

In the navigator:

- Ctrl+N adds an item to the list shown. It gets the fields of its list: Done, Start and Due in a list made by
  axTask, or the ones you wrote into the list's configuration ("The fields of a list", below).
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
  - a long text is a field of several lines: Enter makes a new line, Ctrl+Enter keeps it, and so does Tab and then Enter on Save;
  - a link takes the title of the item it points to, in any list.
- Right and Left change the field you are on without opening anything. On a choice, Right is the next option and Left the previous; at the first and the last you hear the edge sound, and it never goes round. On a check, Right checks and Left unchecks; when it is so already, the edge sound. You hear the field and its new value once, "Priority: Low", and Ctrl+Z takes it back. On any other field the two keys do nothing but the edge sound. They never leave the detail pane: Escape or Shift+Tab goes back to the navigator.
- Which fields an item has is not changed here: the fields are its list's, the same for every item of the list. Ctrl+N,
  F2, Delete, Alt+Up and Alt+Down in the fields say so, and where it is done: Ctrl+J on the list.

Every change is written to the file at once, and Ctrl+Z takes back the last ones, one at a time. If another program
changed the file since axTask showed it, axTask reads it again and says so instead of writing over it.

## The fields of a list

A list says once which fields its items have, in which order and of which type. That is the list's configuration,
the file `list.json` in its folder. Ctrl+J on a list, or Context, Edit the list's configuration, opens it in axJSON.
A list that has no such file yet gets one written first, with the fields its items hold, so that there is something
to edit. When you come back to axTask, one line says what the configuration holds now, "The list's configuration
changed: 5 fields, 2 actions".

```json
{
  "format": "axkit-list 2",
  "details": { "Section": "Home", "Notes": "Things to do at home." },
  "item fields": [
    { "title": "Done", "type": "check", "value": false },
    { "title": "Due", "type": "date", "value": "" },
    { "title": "Priority", "type": "choice", "value": "Normal", "options": ["High", "Normal", "Low"] },
    { "title": "Notes", "type": "longtext", "value": "" }
  ],
  "actions": []
}
```

- `details` are the list's own values, shown in the detail pane when the list is selected: its Section, its Notes.
- `item fields` are the fields of its items, in the order they are shown after the Title. Each has a `title`, a
  `type` (`text`, `longtext`, `number`, `date`, `datetime`, `check`, `choice` or `link`), the `value` a new item
  starts with, and for a choice its `options`.
- `actions` are the list's action keys, further down.

What happens when you change the fields:

- A field you add shows in every item at once, empty. Nothing is written into an item's file until you give the
  field a value there.
- A field you take out is hidden, and its values stay in the items' files. An item that holds such a value says so
  in the last line of its fields, "2 more fields kept: Sprint, Old priority"; Enter on that line reads them. Put the
  field back under the same name and every item shows the value it had.
- A field you rename is a new field: the old name's values are kept as above, and the new one starts empty.
- A field whose type you change keeps its values: one that the new type cannot hold is read as text with what the
  field is now, "next week, kept as text: the field is now a date", until you give the field a new value.
- Actions, Add Done to every item makes the list a checklist, and Remove Done from every item a plain list, whose
  items are no tasks. Both change the configuration only.

If the file does not read after an edit (a comma too many), nothing is lost and nothing is changed: the list's line
says "its configuration does not read", the list says where, "The list's configuration does not read: it is not JSON
at line 12. Ctrl+J opens it", its items are shown with every value as plain text, and axTask writes nothing in that
list until the file reads again.

## A list from an earlier version

Before this version an item's file held the title, the type and the value of every field. axTask now reads one
shape only, the one above. A list written the earlier way says "earlier format: Enter converts it" in its line and is
not opened, and All tasks and the other gathering lists leave it out and say so. Enter or Right on it asks once:
Convert, Convert all such lists, or Cancel. Convert keeps a copy first, among the Archived lists, named after the
list with ", before conversion", then writes the configuration and every item anew. Nothing an item holds is dropped;
the list's fields are the ones its items had, so every item shows what it showed; the action keys stay on their keys.
Ctrl+Z takes the conversion back. A copy of axKit older than this version does not read a converted list.

## Smart lists

All tasks is every task not done, in every list but the Archive, the soonest due first. Actions, New smart list makes
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

Each item is a JSON file named after its title: its names and values, and `schema`, the link to the list's
configuration it is read through.

```json
{
  "schema": "list.json",
  "Title": "Fix the tap",
  "Done": false,
  "Due": "2026-10-30"
}
```

A list is a folder in the top folder; its configuration is `list.json` in it ("The fields of a list", above). Ctrl+J
opens a list's configuration, an item's file or a smart list in axJSON; Ctrl+E shows it in axFile. Anything axTask
does not know in a file is kept as it is. An item in a folder that has no `list.json`, made by hand or by a script,
is shown with every value as plain text and is not changed until Ctrl+J on the list has written the configuration.

## Action keys: Ctrl+1 to Ctrl+9

Every list can give the number keys actions of its own. Actions, Action keys opens one form. It starts on the list of
the nine keys; Up and Down choose one, and the fields after it show what that key does. Tab goes on to the action's
name (empty removes the key), to a list of what the key does, and to a box for its value, where Enter makes a new
line; Ctrl+Enter or the Keep button keeps it. The three things a key can do: set fields to values ("Kind: Bug", "Done: done"), move the item to another
list, or open something (a field holding a web address, or an app such as axJSON for the item's file). From then on
Ctrl+1 to Ctrl+9 do that to the item selected, and Context's Action keys submenu on an item names them. Ctrl+Z takes a set
or a move back. All tasks is Ctrl+0.

The keys are written among the `actions` of the list's configuration, each with its name, its key and what it does:
`{ "name": "Later", "key": "Ctrl+2", "does": "move", "to": "../Later" }`.

A key that moves an item or opens something acts at once when you set it in this form on this computer. When the
list's keys came another way (a list someone shared, a folder that syncs, a `list.json` edited outside axTask), the
first press does nothing and says what the key now does: "Ctrl+2 in this list now moves to ../Later. Press it again to
do it." Press it again and it does it; from then on it acts at the first press, until the key's action changes. A key
that only sets fields is never asked about: Ctrl+Z takes it back.

## Local only

axTask works on the files of your computer and asks the network nothing. An action key that opens a field's web
address hands the address to your browser, which is your own step.

Until version 5.8.0 axTask had GitHub lists, which read a repository's issues. They were removed: a complex thing
such as GitHub will get an app of its own. A list you made as a GitHub list stays, as an ordinary list with its
items; it is no longer refreshed, and its action keys work as in every list. A GitHub token an earlier version kept
is no longer used; its file is `%LOCALAPPDATA%\AxKit\github.token`, yours to delete.

## Lists in other projects

Your other folders of lists, such as a `tasks` folder in each code project, can be reached from this window without
moving them.

- Options, Add a project folder: type the path of a folder that holds your projects, such as
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
  archive or link it, open it where it lives first.

## AI tools and your lists

An AI tool such as Claude Code, or your conversation in axHelp, can read your lists and add to them while an axTask
window is open, once you have turned the AI tools on and allowed it (axKit menu, AI tools; the axKit guide says how).

- Reading: it can list your lists, your own top folder's and your projects', with how many items each has, and read
  one list's items with their fields. Nothing is said while it reads.
- Adding: only while axKit menu, AI tools, Let AI tools run commands is on. Ask it, for instance, "remind me next week
  to call the dentist": it adds the item to the list it names, made from that list's fields as Ctrl+N makes one, with
  a Due date, a Priority from the list's choices or Notes if it gives them and the list has those fields; a field the
  list does not have is refused, and the tool is told which fields there are. The window says "Claude added Call the
  dentist to Personal"; if that list is the one shown, the new item is selected, otherwise you stay where you are.
  Ctrl+Z takes it back as any change. In a window behind you hear a chime and the window flashes on the taskbar.
- It never changes or deletes an item, never reaches a folder of lists that is not your top folder or one of your
  projects, and never adds while a notice or a rename is open, or while you are typing in the window.

## Approving outside reports

Reports that people send with F4 are never worked on until you approve them. Each one has an entry in the list
"outside-reports": the assistant's review of it for harm, what kind of matter it is, and exactly what it proposes to
do. Open an entry, read it, then choose Approve this report or Decline this report from the Context menu (Shift+F10).
The notice opens on its text: read down through everything your approval covers, then press Tab for the passphrase
field, type your approval passphrase and press Enter. Your passphrase signs exactly what you read; if anything of it
changes afterwards, the approval no longer counts, and the work stops. If an entry holds a character that a screen
reader does not speak, axTask signs nothing and says which field holds it: have the entry written again.

The first time, set up your passphrase: open the outside reports list, then Options, Approval passphrase. Type a
passphrase of at least 12 characters, twice. The notice says where the key file is kept. axKit then says the key's
fingerprint, sixteen letters and digits in groups of four. axKit never keeps your passphrase and cannot show it
again. Make it long, use it nowhere else, and tell it to nobody and no program: it is what protects the key file,
which is backed up with your lists.

The same item later changes it: the current passphrase first, then the new one twice. Its first notice says the
fingerprint of the key in use, which is how you hear your own key at any time (Escape leaves without a change). A
change makes a new key. Approvals you gave before and that are still in force are offered to you again; what is
finished stays as you signed it.

## Keys

F1 lists every key. Shift+F10 or the Applications key opens Context, what acts on the selection.

## If something goes wrong

- "The item changed on disk; it has been read again": another program changed the file. Look at it again and repeat
  the change.
- A file that is not an item is not shown; the list's name counts such files.
- An item whose file is damaged (a `.json` file that is no longer an item, after a sync that went wrong or an edit by
  hand) is a row at the end of its list: "Broken.json, not readable as an item, Enter opens it in axJSON". Enter or
  Ctrl+J opens the file in axJSON to mend, Ctrl+E shows it in axFile; F2 and Delete leave it alone. The list's name
  says how many there are ("Home, 3 items, 1 not readable"). All tasks, the date lists, the tags and your smart
  lists cannot show such an item, so their name says how many they could not read and their last row says where:
  "1 item not readable, in Home". Go to that list to find the file.
- Help, Diagnostics, Copy diagnostics puts what is needed for a bug report on the clipboard.

## About

axTask is part of axKit by Dr. Kyle Keane, www.kylekeane.com. Help, About has the licence and the version.

## Disclaimer

axTask is provided as it is, without warranty of any kind. Keep a copy of files that matter.
