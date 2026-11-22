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
4. Enter on Home: the same as Right. Enter on Fix the tap: the focus moves to "Fields of Fix the tap", "Title, text,
   Fix the tap". Down: "Done, check, not done", "Start, date, empty", "Due, date, 30 October 2026, Friday".
5. Escape: back to "Fix the tap, due 30 October". Tab: the fields again; Shift+Tab back. Ctrl+Tab: the fields; F6: back (the bundle's pane keys, as in every other window).
6. Up in the navigator on the first row: the edge sound. Type `w`: Water plants; Shift+W: back.
7. Ctrl+0 from anywhere: "All tasks, 3 items, 1 list in the earlier format is not read: Old format. Open one to convert it, …", the soonest due first, each "…, in Home".
8. Tab on a list: "Details of Home", "Section, text, empty", "Notes, long text, empty", then "Fields of its items:
   Done, Start, Due, Priority". Enter on that line: the notice "Fields of its items", which says that Ctrl+J on the list
   opens its configuration in axJSON; Escape.
9. On Shopping, Ctrl+M: the form "Move Shopping", the focus in the list "Choose the section" on Your lists, then its explanation said once; End, "A new section", Enter: the edge sound, "Type the section's name", the focus in the field "Type the new section's name, when you chose A new section", the form still open; type `Errands`, Enter: "Moved
   Shopping to Errands"; it is under the heading "Errands, heading level 2, 1 list". Ctrl+Shift+N there, type `Garden`: "Made
   Garden", in Errands too. Tab on Garden, Enter on Section, delete the text, Enter: back in Your lists.
10. Make a task in Home titled `Call @Sam about #garden !urgent`, Left: under Smart lists, "Tags, heading level 2, 3
   symbols"; Ctrl+3: "@ at sign, heading level 3, 1 smart list", Down: "@Sam, 1"; Ctrl+3: "# number sign, heading
   level 3, 1 smart list"; Ctrl+3: "! exclamation mark, heading level 3, 1 smart list"; Ctrl+3: the edge sound;
   Ctrl+Shift+3: back on "! exclamation mark" (TK-5.5, TK-D20). Listen that each symbol's name is said in full. Right
   on @Sam: "@Sam, 1 item, Call @Sam about #garden !urgent, in Home". Space: "Done: …", and the row leaves the list.
   Ctrl+Z; then archive the task with Delete: Left, the heading Tags is gone.
11. Make two smart lists, Actions, New smart list, `App one` and `App two` (close axJSON each time), and a third, `Soon`:
   under Smart lists, after All tasks, "Soon, smart list, …", then "App, heading level 2, 2 smart lists" holding "App
   one" and "App two" by their full names (TK-5.2a). Ctrl+2 from Smart lists lands on App.

## 2. Changing (IA-5, TK-4)

1. On Fix the tap, Space: "Done: Fix the tap"; Space: "Not done: Fix the tap". Ctrl+J: axJSON shows the item as its values, `"Done": false` among them, and `"schema": "list.json"` first.
2. In its fields, Enter on Due, type `+3`, Enter: "Due: …, " three days from today. Type `30 Febuary`: "Not a date: …"
   and the notice stays; Escape.
3. Ctrl+N in the fields: "The fields are the list's configuration's: Ctrl+J on the list opens it in axJSON", and no
   notice opens; F2 on Due, Delete and Alt+Up say the same and change nothing (the fields of an item are its list's,
   `forms-framework.md` AF-13.3). Down to "Priority, choice, empty", a field of the list that this item's file does not
   hold yet. Enter on it: the list "Priority" on High; Down, Enter: "Priority:
   Low", written into the file with this first value. Left on it: "Priority: High", once; Left again: the edge sound, nothing said; Right: "Priority: Low"; Right
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
6. F2 on Title: "The Title field keeps its name …". F2 in the navigator on Fix the tap, type `Fix the sink`: "Renamed
   Fix the sink"; Ctrl+E shows `Fix the sink.json` in axFile.
7. Ctrl+N in Shopping, type `Bread`: "Added Bread", with the list's fields, Done, Start, Due. Delete on Bread: "Archived Bread"; Ctrl+Z:
   "Undone: Archived Bread".
8. Actions, Remove Done from every item in Shopping: "Shopping is a plain list: Done is hidden, and kept in the items that have it"; Washer reads without "done" or a Done field, and its last line is "1 more field kept: Done". Ctrl+Z: "Undone: Shopping is a plain list", and Done shows again as it was.
9. Change Fix the sink in axJSON and save; in axTask, Space on it: "The item changed on disk; it has been read again".
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

## 5. Action keys (TK-7a, docs/axKit/reports.md RP-8)

1. In a list, Ctrl+3: the edge sound and "No action on Ctrl+3 in this list; Actions, Action keys sets one". At the top level, Ctrl+1: the edge sound and "Action keys act in a list: open one first". Ctrl+0: "All tasks" (Ctrl+1 until 5.1.0).
2. Actions, Action keys: one form, the focus in the list "Choose the key" with nine lines, "Ctrl+1. not set" to "Ctrl+9. not set", the full stop a pause after the digit, then its explanation said once. Tab: "Type the action's name, or leave it empty to remove the key"; type "Done"; Tab: the list "Choose what the key does" with "Set fields to values" first; Ctrl+Enter now: the edge sound, "Type at least one line as Field: value", the focus in the last box; type "Done: done", Enter: "new line", nothing kept yet; Ctrl+Enter: "Ctrl+1: Done, sets Done: done". Open the form again: Down through the keys changes what the three fields after the list hold; Escape keeps nothing. Ctrl+Z: "Undone: Ctrl+1: Done, sets Done: done".
3. Set Ctrl+1 again as in step 2, then on a task not done, Ctrl+1: "Done: Done: done" and the row reads done. Ctrl+Z takes it back. Context, Action keys: the first item reads "Done Ctrl+1." and the others "Not set Ctrl+2." to "Not set Ctrl+9.".
4. Actions, Action keys, Down to Ctrl+2, Tab, name "Later", Tab, "Move the item to a list", Tab, the list's name of another list, Ctrl+Enter: Ctrl+2 on an item: "Later: <item> moved to <list>", the item gone from this list and in the other; Ctrl+Z brings it back.
5. Actions, Action keys, Down to Ctrl+5, Tab, name "Web", Tab, "Open a field's web address, or an app", Tab, the value "Notes", Ctrl+Enter: "Ctrl+5: Web, opens Notes". Ctrl+5 on an item with empty Notes: "Notes is empty"; with a web address in its Notes: the address opens in the browser.
5a. A key the list's file brought (TK-7a.3, 2026-11-16): add `{ "name": "Away", "key": "Ctrl+7", "does": "move", "to": "../<another list>" }` to the `"actions"` of a list's `list.json` by hand (Ctrl+J on the list opens it in axJSON). In that list, Ctrl+7 on an item: "Ctrl+7 in this list now moves to ../<another list>. Press it again to do it.", and the item stays. Ctrl+7 again: "Away: <item> moved to <another list>". Ctrl+Z, then Ctrl+7 once more: it moves at the first press. The keys set in steps 4 and 5 acted at their first press.
6. axTask is local only (TK-7a.2): Actions has no New GitHub list and no GitHub account, and Context on an item has no GitHub submenu. A list whose list.json has a "github" member (a list made as a GitHub list, up to 5.8.0; `tasks\Issues` in the runner's script) opens as any list, "Issues, 1 item" and the item, with nothing said about GitHub; F5: "Refreshed: Issues, 1 item", nothing read from the network. Its key that sets Kind (Ctrl+1 there): "Make it a bug: Kind: Bug", and Ctrl+Z takes it back.

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
   item". Right: "Home, 1 item, Fix the leak". Space: "Done: Fix the leak"; Ctrl+Z: "Undone: Done: Fix the leak".
6. Left: "tasks in alpha, 1 list, Home, list, 1 item". Left: "Lists, 6 lists, tasks in alpha, project, 1 list". Left:
   the edge sound.
6a. Ctrl+1 to Smart lists, Down: "All tasks, 5 not done", your own three tasks and the projects' two together (TK-5.6). Right:
   each row ends with where it lives; "Fix the leak, in tasks in alpha / Home". Space on it: "Done: Fix the leak",
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
   the item is changed here". Space in the navigator: "Not a task". F2, type a name, Enter: "Its configuration,
   list.json, is not there: nothing of the item is changed until it is there and reads. Ctrl+J on the list opens it".
6. Left, Ctrl+J on No configuration: "Written the configuration of No configuration", and axJSON opens the new
   `list.json` with "item fields" Done and Notes and an empty list of actions. Back in axTask, Idea's fields read
   "Done, check, not done" and "Notes, text, One line", and Space marks it done.
