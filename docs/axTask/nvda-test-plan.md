# axTask NVDA test plan

Run with NVDA before a release, on the files of `manual_tests/axTask` (made and reset by
`.\manual_tests\prepare.ps1 axtask`), after `.\run-axTask.ps1 manual_tests\axTask\tasks`, never on the folder of your
real tasks. The folder holds the lists `Home` (`Fix the tap`, due 2026-10-30; `Water plants`, done) and `Shopping`
(`Milk`, a plain item; `Washer`), and `Course plan` (`Week one`) in the section Work. Every step says what NVDA should read; a step
that reads differently is a defect. The specifications are `docs/axKit/item-apps.md` (IA-n.m) and
`docs/axTask/SPEC.md` (TK-n.m).

## 1. Start and moving (IA-2, IA-3)

1. Start axTask on the folder: "Lists - axTask window", then "Lists, 3 lists", then "Smart lists, section, 1 smart
   list". Down: "All tasks, 3 not done". Ctrl+Down: "User lists, section, 2 lists"; Ctrl+Down: "Work, section, 1
   list"; Ctrl+Down: the edge sound; Ctrl+Up twice: back on User lists. Right on a header: the edge sound and "A
   section: Down goes into it". Down: "Home, list, 2 items".
1a. Heading sound (bundle AX-7.9; `tests/scripts/axTask/06-heading-sound.txt`). Down from All tasks onto "User lists,
   section": a short rising sound before NVDA reads it; Down onto a list: none. Ctrl+Up and Ctrl+Down, Home and
   type-to-find onto a header: the sound each time. Starting axTask, going into a list and back: no sound for the
   window's own fill. With Options, Text, Sound on headings off: none. The same in axPass, axMCP and axAutomate on a
   section header.
2. Right: "Home, 2 items, Fix the tap, due 30 October". Down: "Water plants, done".
3. Right on an item: the edge sound, nothing else. Left: "Lists, 3 lists, Home, list, 2 items". Left again: the edge
   sound.
4. Enter on Home: the same as Right. Enter on Fix the tap: the focus moves to "Fields of Fix the tap", "Title, text,
   Fix the tap". Down: "Done, check, not done", "Start, date, empty", "Due, date, 30 October 2026, Friday".
5. Escape: back to "Fix the tap, due 30 October". Tab: the fields again; Shift+Tab back. Ctrl+Tab: the fields; F6: back (the bundle's pane keys, as in every other window).
6. Up in the navigator on the first row: the edge sound. Type `w`: Water plants; Shift+W: back.
7. Ctrl+0 from anywhere: "All tasks, 3 items, …", the soonest due first, each "…, in Home".
8. Tab on a list: "Details of Home", "Section, text, empty", "Notes, long text, empty", then "Fields of a new item:
   Done, Start, Due".
9. On Shopping, Ctrl+M: the list "Section" on User lists; End, "A new section", Enter, type `Errands`, Enter: "Moved
   Shopping to Errands"; it is under the header "Errands, section, 1 list". Ctrl+Shift+N there, type `Garden`: "Made
   Garden", in Errands too. Tab on Garden, Enter on Section, delete the text, Enter: back in User lists.

## 2. Changing (IA-5, TK-4)

1. On Fix the tap, Space: "Done: Fix the tap"; Space: "Not done: Fix the tap". Ctrl+J: axJSON shows `"value": false`.
2. In its fields, Enter on Due, type `+3`, Enter: "Due: …, " three days from today. Type `30 Febuary`: "Not a date: …"
   and the notice stays; Escape.
3. Ctrl+N in the fields, type `Priority`, Enter: the list "Type", on "text"; type `c` twice to "choice", Enter, type
   `High, Low`, Enter: "Added Priority, choice of 2". Enter on it: the list "Priority" on High; Down, Enter: "Priority:
   Low".
4. Enter on Notes (a list's details), type two lines, Tab, Enter on Save: "Notes: …"; Ctrl+Q reads both lines.
5. Alt+Up on Priority: "Moved up: Priority"; at the top: the edge sound and "Top of the fields".
6. F2 on Title: "The Title field keeps its name …". F2 in the navigator on Fix the tap, type `Fix the sink`: "Renamed
   Fix the sink"; Ctrl+E shows `Fix the sink.json` in axFile.
7. Ctrl+N in Shopping, type `Bread`: "Added Bread", with Done, Start, Due. Delete on Bread: "Archived Bread"; Ctrl+Z:
   "Undone: Archived Bread".
8. List, Remove Done from every item in Shopping: "Shopping is a plain list: Done removed from …". Ctrl+Z.
9. Change Fix the sink in axJSON and save; in axTask, Space on it: "The item changed on disk; it has been read again".
10. Ctrl+Shift+N at the top, type `Garden`: "Made Garden". Ctrl+Z: Garden is gone.

## 3. Links and smart lists (IA-D3, TK-5)

1. On Fix the sink, Ctrl+N, `Waits on`, type link, then Enter on it and type `Washer`: "Waits on: Washer". Rename
   Washer to `Washer, 15 mm`: the link reads the new title. Archive it: the link still reads it.
2. List, New smart list, `Soon`: axJSON opens `Soon.filter.json`. Change it to `"before": "+7"` on Due and save; F5 in
   axTask: "Soon, smart list, …".
3. A filter with a condition axTask does not know reads "…, a condition not understood".

## 4. The rest

- New line (bundle AX-7.8): in a long text field such as a list's Notes, Enter says "new line"; the steps are axDown's plan, step 2.5a.

1. Ctrl+Shift+D: "Done tasks hidden"; the list's name says "1 done hidden".
2. Shift+F10 in each pane: its menu (List, Item or Field) opens and NVDA reads its first item.
3. F1: the keys; Help, User guide: this guide. Alt+H, About, Version names axTask.
4. Close and start again without a folder: the list you had.

## 5. Action keys and GitHub lists (TK-7a, docs/axKit/reports.md RP-7, RP-8)

1. In a list, Ctrl+3: the edge sound and "No action on Ctrl+3 in this list; List menu, Action keys sets one". At the top level, Ctrl+1: the edge sound and "Action keys act in a list: open one first". Ctrl+0: "All tasks" (Ctrl+1 until 5.1.0).
2. List menu, Action keys: the list "Keys" with nine lines, "Ctrl+1. not set" to "Ctrl+9. not set", the full stop a pause after the digit. Enter on the first: "Ctrl+1" and a Name field; type "Done", Enter; the list "What" with "Set fields to values" first; Enter; the Fields field; type "Done: done", Enter: "Ctrl+1: Done, sets Done: done". Ctrl+Z: "Undone: Ctrl+1: Done, sets Done: done".
3. Set Ctrl+1 again as in step 2, then on a task not done, Ctrl+1: "Done: Done: done" and the row reads done. Ctrl+Z takes it back. Item menu, Action keys: the first item reads "Done Ctrl+1." and the others "Not set Ctrl+2." to "Not set Ctrl+9.".
4. List menu, Action keys, Enter on Ctrl+2, name "Later", "Move the item to a list", the list's name of another list: Ctrl+2 on an item: "Later: <item> moved to <list>", the item gone from this list and in the other; Ctrl+Z brings it back.
5. List menu, New GitHub list: "Repository:"; type "KyleKeane" and Enter: "Type the repository as owner/name"; type "KyleKeane/AxKit-reports", Enter: "Made AxKit-reports issues", the list selected. Right: the list opens, "AxKit-reports issues, empty: Ctrl+N adds an item" at once, then, when GitHub has answered, "AxKit-reports issues, N items, N new" and the items by "<title>"; with the network off instead: "GitHub not read: no network; the list shows what was read last".
6. In the GitHub list, Tab: the fields Number, Kind, State, Opened, Reporter, Version, Updated, Issue, Body, Notes. Item menu, GitHub, Make it a bug without a token: "Set your GitHub token in the List menu first: GitHub account". List menu, GitHub account: the notice with a protected field "Paste your GitHub token"; Escape. Ctrl+5 (Open on GitHub, a default key of the list): the issue opens in the browser.
7. Item menu, GitHub, Open in axClaude without a folder in list.json: "Set the code repository's folder first". With `"folder"` set in list.json (Ctrl+J, axJSON): a new axClaude window on that folder, its message field holding "Issue N: <title>" and the body, not sent.

## 6. Project folders and the search across every list (TK-7b)

The project folder is `manual_tests\axTask\projects`, with `alpha\tasks\Home` (`Fix the leak`, Notes "A washer of 15
mm") and `beta\tasks\Bugs` (`Tap drips`). The runner's script is `tests/scripts/axTask/05-projects.txt`.

1. Without project folders, the top level starts at "Smart lists, section, 1 smart list", as before. Options, Project
   folders, Remove a project folder: one item, "No project folders yet".
2. Options, Project folders, Add a project folder: the notice "Add a project folder", the field "Type the folder's
   path, or leave it empty for the folder picker". Type a path that is not there, Enter: "No folder is there: ...", the
   notice stays. Type the path of `manual_tests\axTask\projects`, Enter: "Added projects, looking for lists"; the first
   line of the top level reads "Projects, 2 lists", and NVDA does not read that line again as the look ends. Add it
   again: "projects is a project folder already". Type the path of `manual_tests\axTask\tasks`, the top folder:
   "That is your own top folder: its lists show at the top already", the notice stays.
3. Add a project folder again with the field empty, Enter: the Windows folder picker opens; Cancel: back in the window,
   nothing added.
4. Right on Projects: "Projects, 2 projects, tasks in alpha, 1 list". Down: "tasks in beta, 1 list". Ctrl+Shift+N:
   "A project's lists are made inside it: Right on a project first". Delete: "A project stays: Options, Project
   folders removes the folder it was found in". F2: "A project's folder is renamed in axFile: Ctrl+E". Ctrl+N: "A
   project's items are made in its lists: Right on a project, then on a list". List, New GitHub list: "A project's
   lists are made inside it: Right on a project first", no notice. F5: "Refreshed: Projects, 2 projects", and the Projects level
   is not read a second time.
5. Up, Right on tasks in alpha: "tasks in alpha, 1 list, Smart lists, section, 1 smart list". Down: "All tasks, 1 not
   done". Down twice, Right: "Home, 1 item, Fix the leak". Space: "Done: Fix the leak"; Ctrl+Z: "Undone: Done: Fix the
   leak".
6. Left: "tasks in alpha, 1 list, Home, list, 1 item". Left: "Projects, 2 projects, tasks in alpha, 1 list". Left:
   "Lists, 3 lists, Projects, 2 lists". Left: the edge sound.
7. Ctrl+Shift+F: the notice "Find in every list", the field "Type the words to find in every list". Type `zebra`,
   Enter: "Searching every list", then the edge sound and "Nothing found for zebra".
8. Ctrl+Shift+F, type `tap`, Enter: "Found tap, 2 items, Fix the tap, in tasks / Home" (your own lists first). Down:
   "Tap drips, in tasks in beta / Bugs". Tab: "Fields of Tap drips" and its Title. Shift+Tab, Escape: "Lists, 3 lists,
   Projects, 2 lists", where the search was started.
9. Right on one of your own lists, so that it is the list open last. Ctrl+Shift+F, type `washer 15`, Enter: "Found washer 15, 1 item, Fix the leak, in tasks in alpha / Home". F2: "Open
   it where it lives first: Enter". Ctrl+N: "Items are made in a list: open the item where it lives first, Enter".
   Item, Use these fields for new items: "Open it where it lives first: Enter". Enter: "Home, 1 item, Fix the leak".
   Left: "tasks in alpha, 1 list, Home, list, 1 item".
10. Right on Home again. Options, Project folders, Remove a project folder: "1. projects (...)"; Enter: "Removed
    projects. Lists, 3 lists": with no project folder left, the window is back in your own lists and the Projects line
    is gone. Close axTask and start it again on the folder: it opens in that list of your own, the one open before the search, not in a project's Home.

## 7. AI tools over the lists (TK-8, docs/axKit/mcp.md MC-8)

With Claude Code installed, axKit menu, AI tools, Let AI tools read and open axKit on and Claude Code added (the
axKit guide), in a Claude session started in any folder. The runner's script `tests/scripts/mcp/04-task-items.txt`
checks the same steps with the runner as the client.

1. Ask Claude "which axTask lists do I have?": the pairing notice if Claude is not paired yet (Allow after the box);
   then Claude names Home, Shopping and Course plan with their counts, and axTask says nothing.
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
