# axTask user guide

axTask keeps tasks and lists as a folder of small files, one file per item, and shows them the way axFile shows a
folder. It is part of axKit, a bundle of apps for people who use a screen reader; it was built for and with NVDA.

## Start

- Start menu: `axKit axTask`. Console: `axTask`, or `axtask C:\my\tasks` for a folder of your own.
- The first time, axTask uses the folder `axKit\Tasks` in your Documents folder, beside what the other axKit apps keep for you there. If your lists were in `Tasks` in your Documents, from a version before this, the update moves them into `axKit\Tasks` the first time any axKit app starts; a folder you chose yourself is never moved. Session, Top folder chooses another.
- Session, Recent top folders lists the folders of lists you used before, the newest first; choose one to open it. The command palette finds them too: Ctrl+Shift+P and part of the folder's name.
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
- Enter opens: on a list it shows the list's items; on an item it opens the item's form ("The item's form", below).
  Tab moves you to the item's fields to read them.
- Tab, Ctrl+Tab or F6 go to the detail pane. Escape, Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6 come back to the navigator.
- Ctrl+0 goes to All tasks from anywhere.
- At either end of a pane, Up or Down plays the edge sound.

## Sections of your own

- On a list, Ctrl+M (Context, Move to section) opens a small form: a list of the sections (Your lists, each of your sections, and A new section, the last) and, after it, a field for a new section's name. Up and Down choose, Enter moves the list there. For a new section, choose A new section, Tab, type its name, Enter.
- The same is the Section field, first in a list's details: Tab on the list, Enter on Section, type the name.
- A new list (Ctrl+Shift+N) joins the section of the line selected.
- A section exists while a list is in it; to rename one, move its lists.

## Tasks and plain lists

- A task is an item of a list whose fields have a **Done** check. **Ctrl+Enter** marks it done or not done, from the
  navigator or the detail pane; on the Done field itself, Space checks and unchecks it. An item that was never marked
  is not done. (Until 5.10.0 Space marked a task done in the navigator; Space selects now, as below.)
- A list without Done is a plain list, for things that are not tasks. Actions, Add Done to every item makes a plain list
  a checklist; Actions, Remove Done from every item makes it plain again. Neither touches an item's file: a task marked
  done stays marked while the list is plain, and shows so again when the list is a checklist again.
- A row reads its title and, for a task, "done", "due 30 October" or "overdue since 22 October".
- Ctrl+Shift+D hides the done tasks of a list, and shows them again.

## Selecting several items

The arrow keys choose the item you are on, as always. To act on several items at once, select them first. The keys
are the same in every axKit window that has a selection, axFile among them.

- **Space** selects the item you are on, or deselects it again: "Selected", "Deselected", with the count when more
  than one is selected, "Selected, 3 selected". A selected item's line ends in ", selected", so you hear it as you
  arrow over it.
- **Shift+Down** and **Shift+Up** select as you move; **Shift+Home** and **Shift+End** select to the first and the
  last item. Moving back with Shift takes items out of the range again.
- **Shift+Space** selects every item from the nearest selected one to the item you are on.
- **Ctrl+A** selects every item shown. **Ctrl+Shift+A** deselects all, and so does **Escape**: "None selected".
- The status bar says how many are selected, "Home, 5 items, 2 selected"; Say the status reads it.
- Items are selected, not lists or headings: Space on one of those says so. Going into another list, or back out,
  starts with nothing selected.

With items selected, these act on all of them, each as one step that Ctrl+Z takes back:

- **Ctrl+Enter**: done or not done. All the selected tasks are marked done while any of them is not; when every one
  is done, all are marked not done. "Done: 2 tasks. 1 was done already."
- **Delete**: all of them to the Archive, "Archived 3 items".
- **An action of the list that sets fields or moves the item** (Ctrl+1 to Ctrl+9, see Actions): "Priority high: 3
  items", "Later: 3 items moved to Later".
- **Ctrl+M**, Move to a list: a form with your lists; Up and Down choose one, Enter moves the items there, "3 items
  moved to Later". The last line, A new list, makes one: Tab to the name's field and type it. The new list gets the
  same fields as the one the items came from. With nothing selected, Ctrl+M moves the item you are on. (On a list,
  Ctrl+M moves the list to a section, as before.)
- **Context, Set fields**: a form of the list's fields, all starting as "leave": an empty box and a choice left on
  "Leave as it is" keep each item's own value, so only what you fill in is written into every item, "Fields set: 3
  items". A check is a list of three lines there: Leave as it is, Checked, Not checked.
- **Context, Clear fields**: a list of the fields; check the ones to empty with Space and press Ctrl+Enter, "Fields
  cleared: 3 items".

Both work on items of one list, and Ctrl+Z takes each back in one step.

To make a list out of what a smart list or a tag shows: go into it, Ctrl+A, Ctrl+M, A new list.

### Putting a list in your own order

In a list, Ctrl+Shift+Up and Ctrl+Shift+Down move the item you are on one place up or down, and Ctrl+Shift+Home and
Ctrl+Shift+End move it to the top or the bottom: "Moved up: Fix the tap, 3 of 12". The list keeps that order from
then on, and Ctrl+Z takes a move back. Options, New items at the top of this list puts every new item first instead
of last. A list you never reorder shows its items by name, as before.

A list can keep a history of its changes: Options, Keep a history of changes in this list. It is off until you turn
it on, and it stays on your computer, in the file `history.jsonl` beside the list's `list.json`. From then on, every
change axTask makes to the list's items is a line there with its time and what the field went from and to, and so is
every Ctrl+Z. Context, History on an item reads its changes, newest first: "2026-11-24 18:30, Done: from not checked
to checked". Changes made outside axTask, in axJSON or by a sync, are not in it.

Everything else stays on the one item you are on, whatever is selected: Enter and its form, F2, Ctrl+Q, Ctrl+J,
Ctrl+E, the detail pane, and an action that opens something or runs a program.

## Changing things

In the navigator:

- Ctrl+N adds an item to the list shown: a row "New item" opens where it will stand, with a box to type its title
  in, as F2 renames; Enter adds it, Escape drops it. It gets the fields of its list: Done, Start and Due in a list made by
  axTask, or the ones you wrote into the list's configuration ("The fields of a list", below).
- On a list among your lists, Ctrl+N adds the item to that list without going into it: type the title and press
  Enter; axTask says "Added" and the title "to" the list, and you stay on the list.
- Ctrl+Shift+N makes a new list.
- F2 renames the item or the list in place: a box over the line holds the name; type the new one and press Enter,
  or Escape to keep the old. Links to it from other items are kept.
- Delete moves the item or the list into the Archive: an archived list is under **Archived lists**, archived items
  under its line "Archived items". Ctrl+Z brings it back.
  To delete something for good, show it in axFile with Ctrl+E and delete it there.

The item's form:

- Enter on an item opens its form: one control for each of the item's fields, in the order of its list, the focus in
  the title's box. Tab and Shift+Tab move between them, and NVDA reads each as what it is:
  - a text is a box of one line; a long text a box of several lines, where Enter makes a new line;
  - a check is a check box, which Space checks and unchecks;
  - a date is a box that takes 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday such
    as friday; a date and time takes a date, a space and a time: tomorrow 14:00;
  - a choice is a list of its options: Up and Down move, and nothing is chosen until you leave the list. While the
    field holds none of them the list starts on "Not chosen";
  - a link is a box that takes the title of the item it points to, in any list.
- Ctrl+Enter, or the Save button, saves everything you changed as one step, and says "Saved"; NVDA then reads the item
  where the focus lands, with what changed. Ctrl+Z takes the whole step back. Enter saves too, from a box of one
  line, a list or a check box. A title you changed renames the item and its file, and the links to it follow.
- Escape leaves the form and nothing is changed. A value a field does not take ("30 Febuary") is said with the focus
  in its box, and the form stays open.
- What the item keeps that its list does not name is in the form's explanation, which Ctrl+Q reads.

In the detail pane, which stays for reading:

- Enter on a field of an item opens the same form with the focus in that field. On a list's own details, Enter
  changes the one detail in a notice.
- Right and Left change the field you are on without opening anything. On a choice, Right is the next option and Left the previous; at the first and the last you hear the edge sound, and it never goes round. On a check, Right checks and Left unchecks; when it is so already, the edge sound. You hear the field and its new value once, "Priority: Low", and Ctrl+Z takes it back. On any other field the two keys do nothing but the edge sound. They never leave the detail pane: Escape or Shift+Tab goes back to the navigator.
- Which fields an item has is not changed here: the fields are its list's, the same for every item of the list. Ctrl+N,
  F2 and Delete in the fields say so, and where it is done: Ctrl+J on the list.

Every change is written to the file at once, and Ctrl+Z takes back the last ones, one at a time. If another program
changed the file since axTask showed it, axTask reads it again and says so instead of writing over it.

## The fields of a list

A list says once which fields its items have, in which order and of which type. That is the list's configuration,
the file `list.json` in its folder. Ctrl+J on a list, or Context, Edit the list's configuration, opens it in axJSON.
A list that has no such file yet gets one written first, with the fields its items hold, so that there is something
to edit. When you come back to axTask, one line says what the configuration holds now, "The list's configuration
changed: 5 fields, 2 actions".

In axJSON you can choose instead of typing, with Alt and an arrow. Alt+Right on a value that is one of a few, such
as a field's `type` or an action's `does`, lists the values; Alt+Down and Alt+Up change it in line to the next and
the previous one; Alt+Left in a field or an action lists the members it can have and does not have yet, each with
what it is, and adds the one you choose (axJSON's guide, "Choose instead of typing").

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
- A field titled `Created`, of type `date` or `datetime`, keeps the day each item was made: an item you add, with
  Ctrl+N or through an AI tool, gets today's date there, or the moment for a date and time. Items made before the field
  was there keep it empty.
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

## Actions: Ctrl+1 to Ctrl+9, and with Shift

Every list can have actions of its own, which act on the item selected. They are written in the list's
configuration, under `actions`: Ctrl+J on the list opens it in axJSON. Each action has a name, may have a key, and
does one thing:

```json
"actions": [
  { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true, "Closed": "{today}" } },
  { "name": "Later", "key": "Ctrl+2", "does": "move", "to": "../Later" },
  { "name": "Web", "does": "open", "open": "Address" },
  { "name": "Answer the report", "key": "Ctrl+Shift+1", "does": "run",
    "fields": [ { "title": "Type your answer", "type": "longtext" } ],
    "program": "gh",
    "arguments": ["issue", "comment", "{Number}", "--repo", "owner/name", "--body-file", "-"],
    "input": "{form:Type your answer}",
    "then": { "set": { "Answered": "{today}" } } }
]
```

- `set` writes fields; `move` moves the item to another list; `open` opens a field that holds a web address, or an
  app such as axJSON on the item's file; `run` starts a program; `terminal` puts a command into axTerminal's command
  field for you to read and send (`"does": "terminal", "command": ["gh", "issue", "view", "{Number}"]`).
- A key is one of eighteen: Ctrl+1 to Ctrl+9 and Ctrl+Shift+1 to Ctrl+Shift+9. An action without a key is run from
  the menu. Context, Actions of this list, names every action of the list with its key. All tasks is Ctrl+0.
- A text of an action may hold names in braces, filled in from the item when the action runs: `{Title}` or any
  field's title, `{file}` for the item's file, `{name}` for its name, `{folder}` for the list's folder, `{today}` for
  the date, and `{form:...}` for what you typed in the action's form.
- An action with `fields` opens a small form first, with a box for each field; the field's title is what NVDA reads
  as the box's name, so write it as an instruction ("Type your answer").
- An action axTask does not understand is in the menu as "Name, not understood:" and why, and never runs.

A program an action runs:

- gets each argument as one piece, whatever it holds. A title with quotes or a semicolon in it is that title, and can
  never become a second command. A text of several lines goes to the program as its `input`, not as an argument.
- is asked about before its first run: "Run Answer the report? An AI may have written it: read the explanation
  before you run it". The text starts with "Read this first": axKit does not know who wrote the action, you, another
  program or an AI; it can be wrong, nothing of it is checked, and running it is your own act. Then, under "The
  action", where it is written, what it does, the
  program by its full path and every argument as it is for the item you are on. Enter there is Cancel; Tab goes to
  Run it once, and to Run it, and do not ask again for this action. Change anything of the action in the file and
  you are asked again.
- is heard: "Running Answer the report" as it starts; the falling chime of a finished command and "Answer the report: done" with what it set, or "Answer the
  report failed, exit code 1", and then its output opens to read. Context, Actions of this list, Read the last output
  shows what the last program wrote. Actions, Stop the running action ends it.
- shows in the status bar while it runs: "Running Answer the report" stands before the folder (NVDA+End).
- is never taken from the list's own folder, and runs only on this computer. axTask does not know what a program
  does or reaches: that is the program's doing, and your choice to run it.

A command an action puts into axTerminal is never sent for you. You are asked first, "Put Look at the report into
axTerminal?", with each part of the command as it is for the item; then a new axTerminal window opens in the list's
folder with the command in its field, and says that it is there and not sent. You read it, change it if you like,
and press Enter yourself. Each part is quoted for the shell of that window, so a value from the item stays one text.

A key that moves an item or opens something says first what it does, the first time: "Ctrl+2 in this list now moves
to ../Later. Press it again to do it." Press it again and it does it; from then on it acts at the first press, until
the action changes. A key that only sets fields is never asked about: Ctrl+Z takes it back.

## Local only

axTask works on the files of your computer and asks the network nothing. An action that opens a field's web
address hands the address to your browser, which is your own step. An action that runs a program starts a program
of your choosing, which you were asked about; what that program reaches, a server among it, is its own doing, as
if you had run it in a terminal.

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
  where you searched from. Tab shows the found item's fields, and Ctrl+Enter marks a task done, there and then; to rename,
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

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); Refresh (F5); Top folder.
- **Actions**: Add Done to every item; Deselect all (Ctrl+Shift+A); New item (Ctrl+N); New list (Ctrl+Shift+N); New smart list; Remove Done from every item; Select all (Ctrl+A); Stop the running action; Undo (Ctrl+Z).
- **Navigation**: All tasks (Ctrl+0); Find in every list (Ctrl+Shift+F); Next section (Ctrl+Down); Previous section (Ctrl+Up).
- **Context**: A heading: nothing to act on; Approve this report; Archive (Delete); Clear fields; Decline this report; Done or not done (Ctrl+Enter); Edit the list's configuration (Ctrl+J); History; Move down in the list (Ctrl+Shift+Down); Move to a list (Ctrl+M); Move to section (Ctrl+M); Move to the bottom of the list (Ctrl+Shift+End); Move to the top of the list (Ctrl+Shift+Home); Move up in the list (Ctrl+Shift+Up); Open in axJSON (Ctrl+J); Open its form (Enter); Read in full (Ctrl+Q); Rename (F2); Select or deselect (Space); Select to here (Shift+Space); Set fields; Show in axFile (Ctrl+E); Actions of this list (Later (Ctrl+2), No actions in this list, Priority high (Ctrl+1), Read the last output).
- **Options**: Add a project folder; Approval passphrase; Filter; Hide done tasks (Ctrl+Shift+D); Keep a history of changes in this list; New items at the top of this list; Developer (Open settings file); Sort by (Name, The list's own order); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size).
- Keys without a menu item: Ctrl+1 (Action key 1); Ctrl+2 (Action key 2); Ctrl+3 (Action key 3); Ctrl+4 (Action key 4); Ctrl+5 (Action key 5); Ctrl+6 (Action key 6); Ctrl+7 (Action key 7); Ctrl+8 (Action key 8); Ctrl+9 (Action key 9); Ctrl+Shift+1 (Action key Shift+1); Ctrl+Shift+2 (Action key Shift+2); Ctrl+Shift+3 (Action key Shift+3); Ctrl+Shift+4 (Action key Shift+4); Ctrl+Shift+5 (Action key Shift+5); Ctrl+Shift+6 (Action key Shift+6); Ctrl+Shift+7 (Action key Shift+7); Ctrl+Shift+8 (Action key Shift+8); Ctrl+Shift+9 (Action key Shift+9); Escape (Back from the search); Enter (Change the field); Space (Check the field); Escape (Clear the filter); Delete (Delete); Ctrl+F (Find in the list); F3 (Find next); Shift+F3 (Find previous); Right (Into the list); Ctrl+1 (Next heading level 1); Ctrl+2 (Next heading level 2); Ctrl+3 (Next heading level 3); Enter (Open); Right (Open it where it lives); Enter (Open the file in axJSON); Ctrl+Shift+1 (Previous heading level 1); Ctrl+Shift+2 (Previous heading level 2); Ctrl+Shift+3 (Previous heading level 3); Ctrl+Q (Read in full); F2 (Rename); Space (Select or deselect); Shift+Space (Select to here); Shift+Home (Select to the first item); Shift+End (Select to the last item); Shift+Down (Select to the next item); Shift+Up (Select to the previous item); Ctrl+Tab (Switch between the navigator and the detail pane); Shift+F10 (The Context menu); Shift+F10 (The field's menu); Right (The next value of the field); Left (The previous value of the field); Tab (To the detail pane); Tab (To the navigator); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

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

## Filter and sort

Options, Filter (Alt+O, then F) shows only what has a text in its name: type part of a name, Enter, and the list holds only those rows and says so in its name, "Home, 4 items, filter tap, 1 shown". An empty text clears the filter, and so does Escape in the list once nothing is selected; going into another list drops it. Options, Sort by puts the list in the order of its names, or back in its own order. Both are the same in every list of axKit's navigators, and neither changes anything that is stored.

## About

axTask is part of axKit by Dr. Kyle Keane, www.kylekeane.com. Help, About has the licence and the version.

## Disclaimer

axTask is provided as it is, without warranty of any kind. Keep a copy of files that matter.
