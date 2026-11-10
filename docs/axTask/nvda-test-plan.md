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
