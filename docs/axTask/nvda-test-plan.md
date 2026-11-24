# axTask NVDA test plan

Run with NVDA before a release, on the files of `manual_tests/axTask` (made and reset by
`.\manual_tests\prepare.ps1 axtask`), after `& '.\scripts to run-individual-apps\run-axTask.ps1' manual_tests\axTask\tasks`, never on the folder of your
real tasks. The folder holds the lists `Home` (`Fix the tap`, due 2026-10-30; `Water plants`, done, which also holds value `Sprint` that the list does not name), `Shopping` (`Washer`), `Jottings`, a plain list (`Milk`), `Old format`, list written in the earlier format, `No configuration`, a folder of one item without a `list.json`, and `Course plan`
(`Week one`) in the section Work. Each list's `list.json` names the fields of its items. Every step says what NVDA should read; a step
that reads differently is a defect. The specifications are `docs/axKit/item-apps.md` (IA-n.m) and
`docs/axTask/SPEC.md` (TK-n.m).

## 1. Start and moving (IA-2, IA-3)

$11a. Heading sound (bundle AX-7.9; `tests/scripts/axTask/07-heading-sound.txt`). Down from a list onto a heading such
   as "Smart lists, heading level 1": a short rising sound before NVDA reads it; Down onto a list: none. Ctrl+Up,
   Ctrl+Down, Ctrl+1 to Ctrl+3, Home and type-to-find onto a heading: the sound each time. Starting axTask, going into a
   list and back: no sound for the window's own fill. With Options, Text, Sound on headings off: none. The same in
   axPass, axMCP and axAutomate on a section header.
2. Right: "Home, 2 items, Fix the tap, due 30 October". Down: "Water plants, done".
3. Right on an item: the edge sound, nothing else. Left: "Lists, 6 lists, Home, list, 2 items". Left again: the edge
   sound.
4. Enter on Home: the same as Right. Enter on Fix the tap: the item's form (section 10); Escape. Tab on Fix the tap: the focus moves to "Fields of Fix the tap", "Title, text,
   Fix the tap". Down: "Done, check, not done", "Start, date, empty", "Due, date, 30 October 2026, Friday".
5. Escape: back to "Fix the tap, due 30 October". Tab: the fields again; Shift+Tab back. Ctrl+Tab: the fields; F6: back (the bundle's pane keys, as in every other window).
6. Up in the navigator on the first row: the edge sound. Type `w`: Water plants; Shift+W: back.
7. Ctrl+0 from anywhere: "All tasks, 3 items, 1 list in the earlier format is not read: Old format. Open one to convert it, …", the soonest due first, each "…, in Home".
8. Tab on a list: "Details of Home", "Section, text, empty", "Notes, long text, empty", then "Fields of its items:
   Done, Start, Due, Priority". Enter on that line: the notice "Fields of its items", which says that Ctrl+J on the list
   opens its configuration in axJSON; Escape.
9. On Shopping, Ctrl+M: the form "Move Shopping", the focus in the list "Choose the section" on Your lists, then the form's sound and "Move Shopping, form" (its explanation on Ctrl+Q, bundle AX-7.11); End, "A new section", Enter: the edge sound, "Type the section's name", the focus in the field "Type the new section's name, when you chose A new section", the form still open; type `Errands`, Enter: "Moved
   Shopping to Errands"; it is under the heading "Errands, heading level 2, 1 list". Ctrl+Shift+N there, type `Garden`: "Made
   Garden", in Errands too. Tab on Garden, Enter on Section, delete the text, Enter: back in Your lists.
10. Make a task in Home titled `Call @Sam about #garden !urgent`, Left: under Smart lists, "Tags, heading level 2, 3
   symbols"; Ctrl+3: "@ at sign, heading level 3, 1 smart list", Down: "@Sam, 1"; Ctrl+3: "# number sign, heading
   level 3, 1 smart list"; Ctrl+3: "! exclamation mark, heading level 3, 1 smart list"; Ctrl+3: the edge sound;
   Ctrl+Shift+3: back on "! exclamation mark" (TK-5.5, TK-D20). Listen that each symbol's name is said in full. Right
   on @Sam: "@Sam, 1 item, Call @Sam about #garden !urgent, in Home". Ctrl+Enter: "Done: …", and the row leaves the list.
   Ctrl+Z; then archive the task with Delete: Left, the heading Tags is gone.
11. Make two smart lists, Actions, New smart list, `App one` and `App two` (close axJSON each time), and a third, `Soon`:
   under Smart lists, after All tasks, "Soon, smart list, …", then "App, heading level 2, 2 smart lists" holding "App
   one" and "App two" by their full names (TK-5.2a). Ctrl+2 from Smart lists lands on App.

## 2. Changing (IA-5, TK-4)

1. On Fix the tap, Ctrl+Enter: "Done: Fix the tap"; Ctrl+Enter: "Not done: Fix the tap". Ctrl+J: axJSON shows the item as its values, `"Done": false` among them, and `"schema": "list.json"` first.
2. In its fields, Enter on Due: the item's form with the focus in Due's box; type `+3`, Enter: "Saved", and the focus is back on Due in the fields, read with the date three days from today. Enter again, type `30 Febuary`, Enter: "Due: Not a date: …"
   and the form stays, the focus in Due's box; Escape.
3. Ctrl+N in the fields: the knock of a key with no command, and no
   notice opens; F2 on Due and Delete knock the same way and change nothing (the fields of an item are its list's,
   `forms-framework.md` AF-13.3). Down to "Priority, choice, empty", a field of the list that this item's file does not
   hold yet. Enter on it: the item's form with the focus in the list "Choose Priority" on "Not chosen"; Down twice, Enter: "Saved", then Priority read with
   Low, written into the file with this first value. Left on it: "Priority: High", once; Left again: the edge sound, nothing said; Right: "Priority: Low"; Right
   again: the edge sound, it does not go round; Ctrl+Z: "Undone: Priority: Low", one step. On Done: Right: "Done:
   done"; Right again: the edge sound; Left: "Done: not done"; Left again: the edge sound. On Title or Due: Right and
   Left play the edge sound and change nothing; neither key leaves the pane (IA-5.6).
4. Enter on Notes (a list's details), type two lines, Tab, Enter on Save: "Notes: …"; Ctrl+Q reads both lines. Enter on
   Notes again, type a word, Ctrl+Enter: the text is kept as with Save (IA-5.7).
5. Hide, keep, restore (AF-11.3, AF-11.4). On Water plants, Tab, End: "1 more field kept: Sprint". Enter: the notice
   "Fields kept" with "Sprint: October" and what a kept field is; Escape. On Home, Ctrl+J: axJSON opens `list.json`.
   Add `{ "title": "Sprint", "type": "text", "value": "" }` to "item fields", save, and go back to axTask: "The list's
   configuration changed: 5 fields, 0 actions", said once after the window's own words. Water plants now shows
   "Sprint, text, October" and no kept line. Take Priority out of "item fields" in axJSON, save, back: "… 4 fields, 0
   actions"; Fix the tap shows no Priority, and its last line is "1 more field kept: Priority". Put Priority back: it
   shows "Low" again.
5a. A configuration that does not read (AF-11.7). In axJSON, put a comma too many into Home's `list.json`, save, back
   in axTask: "The list's configuration does not read: it is not JSON at line …. Ctrl+J opens it". Home's row reads
   "Home, list, 2 items, its configuration does not read"; an item's fields are all text, with the last line "Its
   configuration, list.json, does not read: …: every value is shown as text, and nothing of the item is changed
   here"; Enter on a field says the reason and opens nothing. Mend the file in axJSON, back: "The list's configuration
   changed: …", and everything is as before.
6. F2 on Title in the fields: the knock, as on every field. F2 in the navigator on Fix the tap, type `Fix the sink`: "Renamed
   Fix the sink"; Ctrl+E shows `Fix the sink.json` in axFile.
7. Ctrl+N in Shopping, type `Bread`: "Added Bread", with the list's fields, Done, Start, Due. Delete on Bread: "Archived Bread"; Ctrl+Z:
   "Undone: Archived Bread".
8. Actions, Remove Done from every item in Shopping: "Shopping is a plain list: Done is hidden, and kept in the items that have it"; Washer reads without "done" or a Done field, and its last line is "1 more field kept: Done". Ctrl+Z: "Undone: Shopping is a plain list", and Done shows again as it was.
9. Change Fix the sink in axJSON and save; in axTask, Ctrl+Enter on it: "The item changed on disk; it has been read again".
10. Ctrl+Shift+N at the top, type `Garden`: "Made Garden". Ctrl+Z: Garden is gone.

## 3. Links and smart lists (IA-D3, TK-5)

1. Ctrl+J on Home, add `{ "title": "Waits on", "type": "link", "value": "" }` to "item fields" in axJSON and save. On
   Fix the sink, Enter on "Waits on, link, empty" and type `Washer`: "Waits on: Washer". Rename
   Washer to `Washer, 15 mm`: the link reads the new title. Archive it: the link still reads it.
2. Actions, New smart list, `Soon`: axJSON opens `Soon.filter.json`. Change it to `"before": "+7"` on Due and save; F5 in
   axTask: "Soon, smart list, …".
3. A filter with a condition axTask does not know reads "…, a condition not understood".

## 4. The rest

- New line (bundle AX-7.8): in a long text field such as a list's Notes, Enter says "new line"; the steps are axDown's plan, step 2.5a.

1. Ctrl+Shift+D: "Done tasks hidden"; the list's name says "1 done hidden".
2. Shift+F10 in each pane: Context opens with the commands of the list, the item or the field the focus is on, and NVDA reads its first item. Ctrl+Period, then let go of both keys: the same menu, read the same way (bundle AX-8.15); in a text field of a notice, Ctrl+Period shows what Shift+F10 shows there.
3. F1: the keys; Help, User guide: this guide. Alt+H, About, Version names axTask.
4. Close and start again without a folder: the list you had.
5. A damaged item (TK-4.6): put a file `Broken.json` holding `{ nope` into Home. Right on Home: "Home, 2 items, 1 not readable, ..."; the last row reads "Broken.json, not readable as an item, Enter opens it in axJSON". F2 and Delete: "Not readable as an item: Enter opens it in axJSON, Ctrl+E shows it in axFile". Enter: axJSON opens the file. Ctrl+0: "All tasks, ..., 1 not readable"; End: "1 item not readable, in Home".

## 5. The actions of a list (TK-7a, forms-framework.md sections 2 to 7)

The runner's scripts are `tests/scripts/axTask/04-action-keys.txt` and `19-list-actions.txt`. For the steps by hand,
open Home's configuration (Ctrl+J on Home) and put these into `"actions"`, then save and go back to axTask:
`{ "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true } }`,
`{ "name": "Later", "key": "Ctrl+2", "does": "move", "to": "../Shopping" }`,
`{ "name": "Find it", "key": "Ctrl+Shift+4", "does": "run", "program": "findstr", "arguments": ["/c:tap"], "input": "{Title}" }`,
`{ "name": "Miss", "does": "run", "program": "findstr", "arguments": ["/c:no such words"], "input": "{Title}" }` and
`{ "name": "Broken", "does": "fly" }`.

1. In a list, Ctrl+3: the edge sound and "No action on Ctrl+3 in this list; its actions are written in its configuration, Ctrl+J on the list". At the top level, Ctrl+1: the next heading; Ctrl+4: the edge sound and "Action keys act in a list: open one first". Ctrl+0: "All tasks".
2. Back from axJSON after saving the actions: "The list's configuration changed: 4 fields, 5 actions".
3. On a task not done, Ctrl+1: "Done: Done: done" and the row reads done. Ctrl+Z takes it back. Context, Actions of this list: "Done Ctrl+1", "Later Ctrl+2", "Find it Ctrl+Shift+4", "Miss", "Broken, not understood: it does fly, which this version does not know", then "Read the last output". Enter on Broken: the refusal sound and the same words; nothing happens.
4. Ctrl+2 on an item: "Ctrl+2 in this list now moves to ../Shopping. Press it again to do it.", and the item stays. Ctrl+2 again: "Later: <item> moved to Shopping". Ctrl+Z, then Ctrl+2 once more: it moves at the first press.
5. Ctrl+Shift+4 on Fix the tap: the notice "Run Find it? An AI may have written it: read the explanation before you run it", the focus in its text. Read down: the heading "Read this first" and the AI caution (axKit does not know who wrote the action; it can be wrong; nothing of it is checked; running it is your own act), the heading "The action", the file it is written in, "Find it runs findstr with 1 argument: /c:tap; gives it {Title} as input", "The program:" and the full path of findstr.exe, the folder, the argument, the input "Fix the tap", and "axKit does not know what this program does or reaches. Running it is your own choice." Enter: the notice closes and nothing ran (Enter is Cancel).
6. Ctrl+Shift+4 again, Tab to "Run it once", Enter: "Running Find it", then the falling done chime and "Find it: done". Context, Actions of this list, Read the last output: "Output of Find it" with "Fix the tap". Ctrl+Shift+4 again: asked again, since it was run once only. This time Tab to "Run it, and do not ask again for this action", Enter; then Ctrl+Shift+4 once more: it runs at once.
7. Context, Actions of this list, Miss, Run it once: "Running Miss", the failure sound, "Miss failed, exit code 1", and "Output of Miss" opens with "It wrote nothing."
8. Change "/c:tap" to "/c:fix" in the configuration, save, back in axTask, Ctrl+Shift+4: asked again, with the new argument.
8a. A command for axTerminal: add `{ "name": "Look", "does": "terminal", "command": ["findstr", "/c:tap", "{file}"] }` to the actions. Context, Actions of this list, Look: the notice "Put Look into axTerminal? An AI may have written it: read the explanation before you run it", the AI caution first, then the three parts, one on a line, and "axTerminal puts the command into its command field and does not send it". Enter: nothing happened. Again, Tab twice to "Put it there once", Enter: "Look is in axTerminal's command field"; a new axTerminal window, whose field NVDA reads under its caution, "Command, put here by another program and not sent. axKit checked nothing of it: read it, and press Enter only if you want to run it, edit", with `findstr /c:tap '...\Fix the tap.json'` after it (docs/axTerminal/SPEC.md AT-1.5a). Nothing ran until Enter. While a program of an action runs (a long one, such as ping -n 30 127.0.0.1), NVDA+End: "Running" and the action's name before the folder.
9. With an AI tool paired and allowed to run commands, ask it to run Find it: the user is asked first, and the notice "Run Find it?" shows although the action was accepted.
10. axTask is local only (TK-7a.2): Actions has no New GitHub list and no GitHub account, and Context on an item has no GitHub submenu. A list whose list.json has a "github" member (a list made as a GitHub list, up to 5.8.0; `tasks\Issues` in the runner's script) opens as any list, "Issues, 1 item" and the item, with nothing said about GitHub; F5: "Refreshed: Issues, 1 item", nothing read from the network. Its key that sets Kind (Ctrl+1 there): "Make it a bug: Kind: Bug", and Ctrl+Z takes it back.

## 6. Project folders and the search across every list (TK-7b)

The project folder is `manual_tests\axTask\projects`, with `alpha\tasks\Home` (`Fix the leak`, Notes "A washer of 15
mm") and `beta\tasks\Bugs` (`Tap drips`). The runner's script is `tests/scripts/axTask/05-projects.txt`.

1. Without project folders, the top level has no Projects heading: Ctrl+1 from Your lists goes to Smart lists.
   Options, Remove a project folder: one item, "No project folders yet".
2. Options, Add a project folder: the notice "Add a project folder", the field "Type the folder's
   path, or leave it empty for the folder picker"; the notice's text says that in the picker Enter opens a folder and
   Select Folder chooses it. Type a path that is not there, Enter: "No folder is there: ...", the notice stays. Type
   the path of `manual_tests\axTask\tasks`, the top folder: "That is your own top folder: its lists show at the top
   already", the notice stays. Type the path of `manual_tests\axTask\projects`, Enter: "Added Projects in projects,
   looking for lists", the focus in the navigator on "Projects in projects, heading level 2, …", which becomes "2
   projects" quietly as the look ends. Add it again: "projects is a project folder already".
3. Remove it (Options, Remove a project folder, "1. projects"), then Add a project folder with the
   field empty, Enter: the Windows folder picker opens. Cancel: back in the navigator, nothing added. Again, choose
   `manual_tests\axTask\projects` with Select Folder: back in the navigator on its heading, and, half a second later,
   "Added Projects in projects, looking for lists" heard in full; never the empty field of the notice (TK-D12; the
   user's report of 2026-11-11).
4. Ctrl+Shift+1 to "Projects, heading level 1, 1 project folder"; Ctrl+2: "Projects in projects, heading level 2, 2
   projects". Down: "tasks in alpha, project, 1 list"; Down: "tasks in beta, project, 1 list". Ctrl+Shift+N:
   "A project's lists are made inside it: Right on a project first". Delete: "A project stays: Options, Remove
   a project folder removes the folder it was found in". F2: "A project's folder is renamed in axFile: Ctrl+E".
5. Up, Right on tasks in alpha: "tasks in alpha, 1 list, Your lists, heading level 1, 1 list". Down: "Home, list, 1
   item". Right: "Home, 1 item, Fix the leak". Ctrl+Enter: "Done: Fix the leak"; Ctrl+Z: "Undone: Done: Fix the leak".
6. Left: "tasks in alpha, 1 list, Home, list, 1 item". Left: "Lists, 6 lists, tasks in alpha, project, 1 list". Left:
   the edge sound.
6a. Ctrl+1 to Smart lists, Down: "All tasks, 5 not done", your own three tasks and the projects' two together (TK-5.6). Right:
   each row ends with where it lives; "Fix the leak, in tasks in alpha / Home". Ctrl+Enter on it: "Done: Fix the leak",
   and it leaves All tasks at once; the file in `projects\alpha\tasks\Home` holds the change. Ctrl+Z: back. Left,
   Ctrl+1 to Date lists: No due date counts the project's tasks too.
7. Ctrl+Shift+F: the notice "Find in every list", the field "Type the words to find in every list". Type `zebra`,
   Enter: "Searching every list", then the edge sound and "Nothing found for zebra".
8. Ctrl+Shift+F, type `tap`, Enter: "Found tap, 2 items, Fix the tap, in tasks / Home" (your own lists first). Down:
   "Tap drips, in tasks in beta / Bugs". Tab: "Fields of Tap drips" and its Title. Shift+Tab, Escape: "Lists, 6 lists",
   then the row the search was started on.
9. Right on one of your own lists, so that it is the list open last. Ctrl+Shift+F, type `washer 15`, Enter: "Found washer 15, 1 item, Fix the leak, in tasks in alpha / Home". F2: "Open
   it where it lives first: Enter". Ctrl+N: "Items are made in a list: open the item where it lives first, Enter".
   Enter: "Home, 1 item, Fix the leak".
   Left: "tasks in alpha, 1 list, Home, list, 1 item".
10. Right on Home again. Options, Remove a project folder: "1. projects (...)"; Enter: "Removed
    projects. Lists, 6 lists": with the project's folder removed, the window is back in your own lists and the Projects
    heading is gone. Close axTask and start it again without naming a folder (a start that names the folder opens its top level): it opens in that list of your own, the one open before the search, not in a project's Home.

## 7. AI tools over the lists (TK-8, docs/axKit/mcp.md MC-8)

With Claude Code installed, axKit menu, AI tools, Let AI tools read and open axKit on and Claude Code added (the
axKit guide), in a Claude session started in any folder. The runner's script `tests/scripts/mcp/04-task-items.txt`
checks the same steps with the runner as the client.

1. Ask Claude "which axTask lists do I have?": the pairing notice if Claude is not paired yet (Allow after the box);
   then Claude names the lists (Home, Shopping, Jottings, Course plan and the others) with their counts, and axTask says nothing.
2. Let AI tools run commands off. Ask Claude "add Call the dentist to Home, due next Monday": Claude says it is not
   allowed, and axTask says nothing.
3. axKit menu, AI tools, Let AI tools run commands: "AI tools may run commands: ...". With Home shown and your hands
   off the keys, ask the same: "Claude added Call the dentist to Home"; the navigator's selection is on "Call the
   dentist, due ..." and Tab reads its fields, Due the coming Monday. Ctrl+Z: "Undone: Claude added Call the dentist
   to Home".
4. Shopping shown, ask Claude to add "Buy soap" to Home: "Claude added Buy soap to Home", and the selection stays in
   Shopping where it was. Right into Home shows Buy soap. Ctrl+Z takes it back.
5. Switch to another program and ask again: a chime and axTask flashing on the taskbar, no words; back in axTask, the
   item is there.
6. Close axTask and ask Claude to list the lists: Claude says no axTask window is open and offers to open one.

## 8. Approving outside reports (TK-9, docs/axKit/outside-reports.md)

In a copy of the management folder (never the real one): its `outside-reports` list with one entry awaiting
approval, as `tools\outside-report.ps1` writes it.

1. Open the outside reports list, then Options, Approval passphrase: "New approval passphrase", the field read as
   protected. Type eleven characters, Enter: the refusal sound and "The passphrase needs 12 characters at least:
   nothing changed". Again with a long one, Enter: "Confirm the approval passphrase. Re-enter the new approval
   passphrase to confirm it" is said at once, the focus still in the field. The same once more: "Approval passphrase set. The key's fingerprint
   is" and four groups of four.
2. On the entry, Context, Approve this report: the focus is in the notice's text, at its top ("Approve:" and the
   entry's title); Down reads the report, the category, the item, the proposed priority, the scope, the harm review
   and the last sentence. Enter there: "Type your approval passphrase to approve", and the focus is in the field.
   Tab from the text reaches the same field, read as protected. A wrong passphrase, Enter: the refusal sound and
   "That is not the approval passphrase"; the entry's State is still Awaiting approval.
3. Again with the right passphrase: "Approved:" and the entry's title; Tab reads State Approved, Approved on today,
   Approval a long string. Approve on it once more: "This entry is Approved, not awaiting your approval".
4. On a priorities item, Approve this report: "Approve and Decline act on an entry of the outside reports list".
5. With an AI tool paired and allowed to run commands, ask it to approve the entry: it does not see the command.
6. Options, Approval passphrase with the key set up: the notice's text says the fingerprint of the key in use and
   where the key file is kept; Escape leaves and nothing changes.

## 9. A list of the earlier format, and a list without a configuration (forms-framework.md AF-13.6, AF-11.1, AF-13.4)

The runner's scripts are `tests/scripts/axTask/16-convert-earlier-format.txt` and `17-list-without-configuration.txt`.

1. At the top, the row "Old format, list, earlier format: Enter converts it". Tab: its details say "This list is
   written in axTask's earlier format. Enter in the navigator asks to convert it; a copy is kept among the Archived
   lists". Ctrl+0: All tasks' name says "1 list in the earlier format is not read: Old format. Open one to convert it".
2. Right on Old format: the notice "Convert Old format?", its text read once: what changes, that a copy is kept among
   the Archived lists, that Ctrl+Z takes the conversion back, and that an older copy of axKit does not read a
   converted list; the buttons Convert and Cancel (with more than one such list, also "Convert all 2 lists"). Escape:
   nothing changed, the row as before.
3. Enter, then Enter on Convert: "Converted Old format: 1 item. A copy is among the Archived lists". The row reads
   "Old format, list, 1 item". Right: "Paint the fence"; Tab: Title, "Done, check, not done", "Colour, text, Green",
   and no kept line. Ctrl+1: "Finish: Done: done" (the list's action key came through). Under Archived lists: "Old
   format, before conversion".
4. Ctrl+Z twice: "Undone: Finish: Done: done", then "Undone: Converted Old format: 1 item"; the row says "earlier
   format" again.
5. Right on No configuration: "No configuration, 1 item, Idea". Tab: "Title, text, Idea", "Done, text, false", "Notes,
   text, One line", then "Its configuration, list.json, is not there: every value is shown as text, and nothing of
   the item is changed here". Ctrl+Enter in the navigator: "Not a task". F2, type a name, Enter: "Its configuration,
   list.json, is not there: nothing of the item is changed until it is there and reads. Ctrl+J on the list opens it".
6. Left, Ctrl+J on No configuration: "Written the configuration of No configuration", and axJSON opens the new
   `list.json` with "item fields" Done and Notes and an empty list of actions. Back in axTask, Idea's fields read
   "Done, check, not done" and "Notes, text, One line", and Ctrl+Enter marks it done.

## 10. The item's form (TK-4.0a, forms-framework.md AF-14.1, AF-14.2)

The runner's script is `tests/scripts/axTask/18-item-form.txt`.

1. Enter on Fix the tap: the form's sound, the focus in an edit box named "Type the title" holding the title, then
   "Fields of Fix the tap, form". Listen that NVDA reads the box first and the words after it, not over it, and
   that the explanation is not read. Ctrl+Q: the explanation, the fields in the order of the list, Tab, Ctrl+Enter,
   Escape.
2. Tab through: "Done, check box, not checked"; "Type Start: a date such as …", an edit box; "Type Due: …"; "Choose
   Priority, list" on its option or on "Not chosen"; then Save and Cancel. Each is read by NVDA as the control it is.
   Shift+Tab goes back the same way.
3. In the list of Priority, Up and Down move and NVDA reads each option; nothing is saved by moving.
4. Change the title's box and press Escape: the form closes, the row reads as before.
5. Enter again; Space on Done, a new date in Due, Down in Priority; Ctrl+Enter: "Saved", once, and the row reads the new state (Done, Due,
   Priority). Ctrl+Z: "Undone: Saved", all three back.
6. Enter, type `30 Febuary` into Due, Ctrl+Enter: the refusal sound, "Due: Not a date: 30 Febuary. …", the focus in
   Due's box, the form still open. Escape.
7. Enter, Ctrl+Enter with nothing changed: "Nothing changed".
8. Enter, change the title, Enter: "Saved", and the new title read; Ctrl+E shows the file renamed.
9. Tab to the fields, Down to Due, Enter: the same form, the focus in Due's box at once. Save: the focus is back on
   Due in the fields, with its new value.
10. On Water plants, Enter: the explanation ends with "1 more field kept: Sprint: they are in the item's file and no
    field of its list".

## 11. Selecting several items (TK-4.7, bundle AX-8.16)

The runner's script is `tests/scripts/axTask/20-selection.txt`.

1. On the heading Your lists, Space: the edge sound, "A heading: nothing to select". On the list Home, Space: "Items
   are selected, not lists: Right goes into this one".
2. In Home, Space on Buy paint: "Selected"; Up and Down again onto it: NVDA reads "Buy paint, selected". Down twice,
   Space: "Selected, 2 selected". Space again: "Deselected, 1 selected". Listen that NVDA does not read the line a
   second time after the word.
3. Shift+Down: NVDA reads the next line with ", selected" and nothing else is said. Shift+Up: the line before,
   still selected, and the one left is not selected any more. Shift+Down on the last item: the edge sound.
4. End, Shift+Space: "Selected, n selected". Escape: "None selected", and the line reads without ", selected".
   Ctrl+A: "5 selected". Ctrl+Shift+A: "None selected". Say the status while two are selected: "Home, 5 items, 2
   selected".
5. Select two tasks not done and one done; Ctrl+Enter: "Done: 2 tasks. 1 was done already." Ctrl+Enter again: "Not
   done: 3 tasks". Ctrl+Z twice: each "Undone: …", and the three are still selected.
6. With the three selected, the list's set action (Ctrl+1): "Priority high: 3 items". Its move action (Ctrl+2),
   twice the first time: "Later: 3 items moved to Later"; the list shows the two left and nothing is selected.
   Ctrl+Z: all three back.
7. Select two, Delete: "Archived 2 items", the focus on the next item. Ctrl+Z: "Undone: Archived 2 items".
8. Select one, Left, Right: nothing is selected; Ctrl+Enter: "Done: <the item>".
9. With two selected, Enter: the form of the item you are on, not of the selected ones.
10. Select two, Ctrl+M: "Move 2 items", the focus in the list "Choose the list" on the first other list, then the
    form's sound and "Move 2 items, form"; Ctrl+Q reads the explanation. Enter: "2 items moved to Later". Ctrl+Z: "Undone: 2 items moved to Later".
11. On one item with nothing selected, Ctrl+M, End: "A new list". Enter without a name: the edge sound, "Type the
    list's name", the focus in the name's field. Type a name, Enter: "<item> moved to <name>"; the new list is among
    the lists with the same fields. Ctrl+Z: the item is back and the list is gone.
12. On a list at the top level, Ctrl+M: "Move <list>", the sections, as before.
13. Select two items, Context, Set fields (TK-4.7a): the form's sound and "Set fields, form"; NVDA reads the first
    control on "Leave as it is". Tab through: each choice and check on "Leave as it is", each box empty. Choose a
    value in one choice, type in one box, Ctrl+Enter: "Fields set: 2 items", and both items read with it. Ctrl+Z:
    "Undone: Fields set: 2 items".
14. Context, Clear fields: "Clear fields, form", the list "Check the fields to empty"; Ctrl+Enter at once: "Check at
    least one field to empty". Space on one field, Ctrl+Enter: "Fields cleared: 2 items". Ctrl+Z takes it back.

## 12. What opens says its kind (bundle AX-7.11)

The runner's script is `tests/scripts/axTask/21-what-opens.txt`. The sounds are bell notes: three quick beeps running
up, then the kind going down.

1. F1: the mark and one low long tone, then NVDA reads the text. Ctrl+Q: the text is read again from its start.
   Escape.
2. Ctrl+F: the mark and one long tone, higher than the text's, then NVDA reads the field. Escape. F2 on an item: the
   same sound, then "New name, edit". Escape.
3. A list to choose from, which axTask opens rarely since an item's choices are in its form: in axMCP, Enter on a
   tool's argument that is a choice ("Change size"): the mark and four quick beeps going down, then NVDA reads the
   list. Escape. Its script is `tests/scripts/axMCP/03-numbers-choices-restart-agreement.txt`.
4. Ctrl+M on an item: the mark, then beeps and tones, the longest of the four; NVDA reads "Choose the list, list" and
   its line; then "Move Fix the tap, form" and nothing more. Ctrl+Q: the explanation. Escape.
5. Listen across the four that they are told apart at once, that each is over before the words matter, and that none
   is mistaken for the heading sound, the edge, a question's sound or the sound of background work starting.
6. Options, Text, Entry sounds: "Entry sounds off"; F1, Ctrl+F, Ctrl+M and Tab between the panes are in
   silence, the form still saying its title and "form". Again: "on".
7. A notice that opens by itself (an update offer, an error at start) still plays the notice sound and not one of
   these.
