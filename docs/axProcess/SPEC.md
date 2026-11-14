# axProcess — Specification

axProcess is an app of the axKit bundle (`docs/axKit/SPEC.md`): what runs on the computer, as lists, for a blind
developer who uses the NVDA screen reader. Built 2026-10-23 on the branch `axProcess` (the long run of that day,
`docs/axKit/plan.md`), not yet heard under NVDA. The document is kept in step with the code (a behaviour change updates
it in the same commit). Requirements are numbered `PR-n.m` and decisions `PR-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axProcess's code and
documents, "SPEC.md" means this file.

## 1. Summary

Task Manager answers "what is using the memory", "what hangs", "what starts with Windows", but its lists are grids
that NVDA reads in part, and its columns move. axProcess is one window with one list and three views: the running
**processes** with their memory and processor use, the **services** with their state, and the programs that
**start** at sign-in. Arrows explore; Enter shows an item's details with what can be done to it; Delete ends a
process after a question, and in the Startup view removes a startup program after one; a startup program is turned
off and on again there as Task Manager does it (PR-5.4). Nothing refreshes by itself: F5 reads the computer again
(PR-D2).

## 2. Goals and non-goals

- **G1 The questions Task Manager answers.** Which process uses the most memory or processor time, end one that
  hangs, see which services run and start or stop one, see what starts at sign-in and turn it off, on again, or
  remove it.
- **G2 One list, the facts in the words** (axGIT AG-D3): "chrome, 350 MB, 2% CPU, process 1234". A list box, which
  NVDA reads best; no grid, no columns.
- **G3 Nothing moves under the reader** (AX-7.3): the list changes on the user's F5 or command, never on a timer.
- **G4 Windows' own APIs, nothing else** (B-10): the processes through .NET, the services through the Service Control
  Manager by P/Invoke, the startup programs from the registry's Run keys and the Startup folders.
- **Non-goals.** Performance graphs, per-process network and disk use, GPU, users, the details of threads and
  handles, changing a process's priority or affinity, adding a startup program or editing one's command, changing
  the startup programs of the whole computer (they need administrator rights, PR-5.5), the startup tasks of Store
  apps and scheduled tasks, anything that needs administrator rights to read. Several computers.

## 3. Scenarios

1. Start axProcess (Start menu, `axProcess`, the Window, axKit and Help menus): "This computer - axProcess window", then "Processes, 214
   list, chrome, 1.2 GB, 3% CPU, process 9120, 1 of 214": the list sorted by memory, the largest first.
2. Down to a program that hangs, Delete: the notice "End notepad (process 4410)? Anything unsaved in it is lost." with
   End process and Cancel; Enter: "Ended notepad"; the list is read again.
3. Ctrl+Q: "214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%".
4. Ctrl+2: "Services, 280 list, Windows Update (wuauserv), running, manual, 1 of 280". Enter on a stopped one: its
   details with Start, and a "needs administrator rights" notice when Windows refuses.
5. Ctrl+3: "Startup, 9 list, OneDrive, enabled, current user's Run key". Enter: the command and where it is set.
6. An installer set KeePassXC to start at sign-in, and its window gets in the screen reader's way. Ctrl+3, type `kee`,
   Shift+F10, Turn off: "KeePassXC turned off: it no longer starts with Windows"; the line reads "KeePassXC, disabled,
   current user's Run key". Or Delete: the question "Remove KeePassXC from the programs that start with Windows?",
   Cancel the default; Alt+R: "KeePassXC removed from the programs that start with Windows".

## 4. Functional requirements

### PR-1 Launch

- PR-1.1 `AxKit.exe process`; the console command `axProcess`; the Start menu entry "axKit axProcess". No File Explorer
  entry. Any argument is reported with the usage text (`AppHost`).
- PR-1.2 `AxProcessApp.Run(args)` is the entry point (AX-1.7): the host's log (`axProcess.log`), crash handling,
  `--help` and `--version`, the settings (`axProcess.json`), then `ProcessForm`.
- PR-1.4 Closing the window puts axProcess in the Window menu's Recently closed list, by its name, since it holds no file
  (AX-9.4); Enter there starts it again.
- PR-1.3 axProcess runs as the user, never elevated. What Windows refuses to a user (ending a system process,
  starting most services) is said as such (PR-3.3, PR-4.3).

### PR-2 The window

- PR-2.1 From the top: the menu bar; the list, named with its view and count ("Processes, 214", "Services, 280",
  "Startup, 9"); the status bar. A notice takes the list's place while it shows (AX-6.4). The focus starts in the list.
- PR-2.2 Views: Ctrl+1 Processes, Ctrl+2 Services, Ctrl+3 Startup (Navigation), and Tab and Shift+Tab to the next and the
  previous view, round from Startup to Processes (the window has one control; the user's ask, 2026-10-24). The key says the list's name and the
  item the selection is on, also when the list already shows the view (axClaude D26). A view is read the first time it
  shows and on F5; each keeps its selection by the item's key (the process ID, the service's name, the startup entry's
  name and place) across a refresh, as axGIT's views do (AG-2.2).
- PR-2.3 F5 reads the view shown again, on a background task (AX-7.5), and says "Refreshed: <the list's name>".
  While a read runs the status bar says "Reading…" and a second F5 says "Working…".
- PR-2.4 The title is "This computer - axProcess". The status bar's left label is the list's name, the right one the
  memory in use ("Memory 57% in use"). No AccessibleName, no Spring (`StatusLayout`).
- PR-2.5 Ctrl+Q says the summary: the number of processes, the memory in use of the total and its percentage, and the
  processor's use over the last read ("214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%").
- PR-2.6 Up or Home on the first item and Down or End on the last play the bundle's edge earcon (`Sounds.Edge`), since the
  key moves nothing (as axFile's AF-3.6; 2026-10-24). Ctrl+C copies the selected item's line ("Copied"); Ctrl+F finds an item by its text, F3 and Shift+F3 the next
  and the previous, wrapping once (the bundle's find in a list).

### PR-3 Processes

- PR-3.1 Each process is "<name>, <memory>, <n>% CPU, process <id>": the name without `.exe`, the working set
  ("350 MB", "350.4 MB", "1.2 GB", "900 KB": the bundle's `AxKit.Core/FileSize`, one decimal from megabytes up, since 2026-10-25, the format every app says sizes in; whole megabytes before), the processor use over one second, rounded to a whole percent of the whole computer
  ("0% CPU" for an idle one; left out for a process Windows does not let the user measure), and the process ID.
- PR-3.2 Sort by ▸ Memory (the default), Processor, Name (Options; the choice is kept, PR-7.1). Memory and processor
  sort the largest first; ties go by name.
- PR-3.3 Delete, or Context → End process, asks "End <name> (process <id>)? Anything unsaved in it is lost." with End
  process and Cancel; End runs `Process.Kill` on a background task, says "Ended <name>" and reads the list again. End
  process tree (Context → End process tree) ends its child processes too, with the question "End <name> and the
  processes it started?". A process that is already gone says "<name> has already ended"; one Windows refuses says
  "Could not end <name>: access is denied. It may need administrator rights". axProcess's own process is refused:
  "That is this window: close it with Alt+F4".
- PR-3.4 Enter, or Context → Details, shows a notice titled with the process's name: the process ID, the memory, the
  processor use, the program file, the start time and the number of threads, each on its own line, those Windows lets
  the user read; with the buttons End process, Show program in axFile and Close (the default and the cancel button).
- PR-3.5 Ctrl+E, or Context → Show program in axFile, opens axFile on the program file's folder with the file selected
  (`WindowMenu.Start`); "Windows does not say where <name>'s program is" when it cannot be read.

### PR-4 Services

- PR-4.1 Each service is "<display name> (<name>), <state>, <start type>", the name once when the display name holds it already ("ActiveX Installer (AxInstSV)") or is it: the states running, stopped, starting,
  stopping, paused; the start types automatic, automatic (delayed), manual, disabled. Sorted by display name.
- PR-4.2 Enter, or Context → Details, shows a notice titled with the display name: the name, the state, the start
  type, the process ID while it runs, with the buttons Start (while stopped), Stop (while running and it accepts a stop),
  Restart (while running), Close.
- PR-4.3 Start, Stop and Restart go to the Service Control Manager on a background task and wait up to 30 seconds for
  the new state: "Started <display name>", "Stopped <display name>", "Restarted <display name>", "<display name> is
  still starting" (or "still stopping" for a stop) when the wait runs out. A refusal is "Could not start <display name>: access is denied. Services
  need administrator rights to start and stop"; other errors are Windows' own message.

### PR-5 Startup

- PR-5.1 The list holds what starts at sign-in from the five usual places: the Run key of the current user, the Run
  key of the computer and its 32-bit twin, the user's Startup folder and the Startup folder of all users. Each is
  "<name>, <enabled or disabled>, <place>"; the place is "current user's Run key", "computer's Run key", "computer's
  Run key (32-bit)", "your Startup folder", "all users' Startup folder". Sorted by name.
- PR-5.2 Disabled is what Task Manager's Startup tab set: the entry's value under the matching `StartupApproved` key
  has an odd first byte. Without such a value the entry is enabled.
- PR-5.3 Enter, or Context → Details, shows a notice titled with the name: the command, the place, enabled or
  disabled, and the program file read from the command (the quoted path, or the text up to `.exe`); Show program in
  axFile (also Ctrl+E) opens it. The notice's buttons: Turn off (an enabled entry) or Turn on (a disabled one),
  Remove, Show program in axFile, Close (the default and the cancel button).
- PR-5.4 **Turn off and Turn on** (Context; the details' buttons; no key, as a service's Start and Stop) set the
  switch Task Manager's Startup apps tab sets and nothing else: the entry's value under its `StartupApproved` key,
  12 bytes, `02` and eleven zero bytes for on, `03`, three zero bytes and the time as a FILETIME for off
  (`StartupChange.ApprovedBytes`). The key is `...\Explorer\StartupApproved\Run` for a Run key, `Run32` for the
  computer's 32-bit Run key and `StartupFolder` for a Startup folder (the value named after the file, with its
  extension), in the hive of the entry itself: HKEY_CURRENT_USER for the user's entries, HKEY_LOCAL_MACHINE for the
  computer's. Nothing is deleted, the command is not touched, and each undoes the other. Seen on the developer's
  computer on 2026-11-14, read and not written: values of 12 bytes, `03 00 00 00` and a FILETIME, or zeros, in the
  user's key, `06` and zeros in the computer's, each in the hive of its entry. Microsoft does not document the key.
- PR-5.5 **The user's own entries act at once; the whole computer's are refused.** The current user's Run key and
  the user's Startup folder are the user's to change. The computer's Run keys and all users' Startup folder are set
  for every user of the computer (`StartupChange.NeedsAdministrator`), and their switch lies under
  HKEY_LOCAL_MACHINE: without administrator rights nothing is tried, no elevation is asked for (PR-1.3), and the
  window says "<name> is set for every user of this computer: turning it off needs administrator rights. Use Task
  Manager's Startup apps tab, or Settings, Apps, Startup" ("turning it on" for Turn on; for Remove, "removing it
  needs administrator rights. Task Manager's Startup apps tab, or Settings, Apps, Startup, turns it off"), with no
  question before it. An axProcess that was started as an administrator acts on them as on the user's own.
- PR-5.6 **Remove** (Delete in the Startup view, Context, the details' button) takes the entry out for good, after a
  question: the notice "Remove <name> from the programs that start with Windows?" with the lines "Command <command>",
  "Set in <place>", what is deleted ("The entry is deleted from the Run key for good. The program itself stays on
  the computer.", or for a Startup folder "The file in the Startup folder is deleted for good, not sent to the
  Recycle Bin. The program itself stays on the computer.") and "Turn off is the gentler way: it keeps the entry, and
  Turn on brings it back."; the buttons Remove (Alt+R) and Cancel, Cancel the default and Escape's (the window base's
  `Notice.Ask`, as axPass's deletions). Remove deletes the value of the Run key, or the file in the Startup folder (a
  file that is not in that folder is never deleted), and then the entry's `StartupApproved` value, so that an entry
  of the same name made later is not off from its first day. The file is deleted, not sent to the Recycle Bin: the
  bundle's one Recycle Bin operation is axFile's own (`AxFile.Core`), which another app may not reference (bundle
  5.3); when it moves to the shared code, Remove uses it and the question's line changes.
- PR-5.7 **One sentence per command, the list in place.** The change runs on a background task (the status bar says
  "Turning off <name>…" meanwhile, a second command "Working…"); then one announcement: "<name> turned off: it no
  longer starts with Windows", "<name> turned on: it starts with Windows again", "<name> removed from the programs
  that start with Windows"; "<name> is already turned off" (or "on") when there is nothing to do; "<name> is no longer
  among the programs that start with Windows. Nothing changed" when the entry went meanwhile; "Could not turn off
  <name>: access is denied. Nothing changed" (or "turn on", "remove") when Windows refuses, and "Could not … <name>:"
  with Windows' message for anything else. After a change, or an entry found gone, the view is read again without a
  word: the selection stays on the entry, whose line now reads enabled or disabled, and after a removal is on the
  entry that followed it (the one before it when it was the last). The words and the decisions are
  `AxProcess.Core/StartupChange`, pure and unit tested; the registry and the folders are `StartupList.Apply`.
- PR-5.8 **No check ever touches what starts with Windows** (the safety rule of this section). The five places are
  one record, `StartupPlaces`: Windows' own, or a stand-in. A window of a test run (`AppPaths.RunMark`: the run's
  secret and folder, `docs/axKit/automation.md`) reads and changes a stand-in of the run's own and never the
  computer's: the registry key `HKEY_CURRENT_USER\Software\AxKit\Runs\<the run's mark>\Startup`, under which the
  same key paths lie below `User` and `Computer`, and the folders `AxKit\startup\yours` and `AxKit\startup\everyone`
  of the run's folder. A stand-in never has administrator rights, so its computer-wide entries are refused as a
  user's are, and a stand-in outside `Software\AxKit` is refused when it is made. The check scripts seed the
  stand-in (a file for a folder entry; a PowerShell step the script writes for the registry, which finds the key from
  the run's folder as `AppPaths.MarkOf` does) and delete its key at their end; a script that fails before its end
  leaves its key under `Software\AxKit\Runs`, which starts nothing. The unit tests exercise the registry and the
  folders on a stand-in of their own (`Software\AxKit\Tests\Startup-<random>` and a temporary folder, deleted at
  the end) and never name the real places for a change: `StartupPlaces.Current`, the window's way to them, appears in
  no test source, and every `StartupList.Apply` of the tests is given the test's stand-in
  (`StartupChangeTests.No_test_names_the_real_places`). So the Startup view of a check shows the stand-in's entries
  and no other, which the scripts expect by their count.

### PR-6 Menus, keys, texts

- PR-6.1 Menus (AX-9.1, the bundle's eight since 2026-11-11): **Session (S):** Refresh F5 | ─ | Exit Alt+F4.
  **Actions (A):** Summary Ctrl+Q. **Navigation (N):** Processes Ctrl+1 | Services Ctrl+2 | Startup Ctrl+3.
  **Context (C)**, what acts on the line selected, by the view shown (AX-9.11): in Processes, Details Enter | End
  process… Delete | End process tree… | Show program in axFile Ctrl+E | Copy line Ctrl+C; in Services, Details Enter |
  Start | Stop | Restart | Copy line Ctrl+C; in Startup, Details Enter | Turn off | Turn on | Remove… Delete | Show
  program in axFile Ctrl+E | Copy line Ctrl+C; "Nothing selected" on an empty list. So a process command is never at hand among the services and the
  reverse (the user's ask, 2026-10-24; until 2026-11-11 three menus of which only the view's was enabled).
  **Options (O):** Sort processes by ▸ (Memory ✓ | Processor | Name) | Text ▸ | General ▸ | Developer ▸. **Window (W)**,
  **axKit (K)** (AX-9.1). **Help (H):** Keyboard shortcuts F1 | User guide | ─ | Windows resources ▸ (Task Manager |
  Services). Every item has a mnemonic (AX-7.4). Shift+F10, the Applications key and a right click in the list open
  Context, as Alt and C open it, so that NVDA reads its first item (the window base's `OpenBarMenu`, axFile's AF-D13;
  2026-10-24).
- PR-6.2 Keys (AX-8). Bundle-wide: F1, Ctrl+Shift+P, Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here. F5
  refreshes, Delete ends a process"), Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific (`AxProcessKeys`): Ctrl+1,
  Ctrl+2, Ctrl+3, Tab, Shift+Tab, F5, Enter, Delete, Ctrl+E, Ctrl+C, Ctrl+Q, Shift+F10 and the Applications key, and Up,
  Down, Home and End at the ends of the list. Enter and Delete act while the list has the focus. Delete
  is one key and two commands, by the view shown: Remove in the Startup view (PR-5.6), End process in the other two
  (which in the Services view says "Processes view first: Ctrl+1", as before). A
  command of one view asked for in another says which view first: "Processes view first: Ctrl+1", "Services view first:
  Ctrl+2", "Startup view first: Ctrl+3". End process, End process tree, Start, Stop, Restart, Turn off, Turn on, Remove
  and Copy line are marked act (`CommandMark.Act`): an AI tool asks the user before it runs one (`docs/axKit/mcp.md`
  MC-7.3).
- PR-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axProcess/user-guide.md` is embedded (Help, User
  guide); About is the bundle's (AX-9.9).

### PR-7 Settings, announcements, log

- PR-7.1 `%APPDATA%\AxKit\axProcess.json` (AX-5.3; `ProcessSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `window`, `sortBy` (`memory`, `processor` or `name`).
- PR-7.2 Announcements, one UI Automation notification each (AX-7.3): the view's name with the selected item;
  "Refreshed: <name>"; "Working…"; the summary; "Ended <name>", "<name> has already ended", the refusals of PR-3.3;
  "Started", "Stopped", "Restarted <display name>", "<display name> is still starting" and the refusals of PR-4.3;
  the sentences of PR-5.7 for a startup program turned off, turned on or removed and of PR-5.5 for one of the whole
  computer; "Sorted by memory", "Sorted by processor", "Sorted by name"; "Copied"; "Nothing selected"; "Processes view
  first: Ctrl+1", "Services view first: Ctrl+2", "Startup view first: Ctrl+3"; "Not found" (find); the bundle's text size and diagnostics announcements.
  Nothing is spoken on an arrow key.
- PR-7.3 The log is `%LOCALAPPDATA%\AxKit\logs\axProcess.log`: the start, the exit, every process ended, every
  service started or stopped and every startup program turned off, turned on or removed, by its name with what it
  came to, and errors.

## 5. Architecture

```
src/AxProcess.Core/    class library (net10.0): ProcessTable (the samples, the processor use between two of them,
                       the rows and their order), ProcessRow, SizeText, ServiceManager (the Service Control Manager
                       by P/Invoke: the list, start, stop, the wait) with ServiceRow, StartupList (the Run keys and
                       the Startup folders, read and changed) with StartupEntry, StartupPlaces (Windows' own places
                       or a test run's stand-in) and CommandLine (the program of a command), StartupChange (which
                       entries need administrator rights, the StartupApproved bytes, the question, the sentences),
                       SystemInfo (the memory, a process's program file and details, by P/Invoke) with
                       SystemTimes (the processors' busy share), ProcessSettings; references AxKit.Core
src/AxProcess/         class library (net10.0-windows): AxProcessApp (Run, Usage), ProcessForm (the window),
                       AxProcessKeys, HelpText; references AxProcess.Core, AxKit.Core, AxKit.Forms
tests/AxProcess.Tests/ ProcessTableTests, ServiceAndStartupTests (labels, order, the processor arithmetic, the
                       command parsing, the StartupApproved bytes, and a read of this computer's lists),
                       StartupChangeTests (the decisions, the bytes and the words from data; turn off, turn on and
                       remove on a stand-in of the test's own, PR-5.8)
docs/axProcess/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

## 6. Design decisions

- **PR-D1 One list with views**, as axGIT (AG-D3): one control to learn, the view's name said on its key.
- **PR-D2 No refresh on a timer.** Task Manager's lists change every second, which moves what a screen reader is
  reading; F5 reads again on the user's word, and every command that changes something reads again after it.
- **PR-D3 The processor use over one second.** One reading of a process's processor time says nothing; two, a
  second apart, on a background task, give the share of the last second, which is what Task Manager shows.
- **PR-D4 The startup list read-only at first; changed since 2026-11-14.** Turning an entry off writes the
  `StartupApproved` keys that Task Manager writes, and the computer's half needs administrator rights; reading was
  the need, and writing waited for the ask. The ask came on 2026-11-14, after an installer set a program to start at
  sign-in and its window blocked the screen reader: Turn off and Turn on (Task Manager's switch, nothing deleted) and
  Remove after a question (PR-5.4 to PR-5.7), for the user's own entries; the computer's are said to need
  administrator rights and are left to Windows' own places, since axProcess never asks for elevation (PR-1.3).
- **PR-D5 Turn off before Remove.** Turn off is reversible and is what Windows itself offers, so it has no question;
  Remove deletes, so it asks, Cancel is the default, and its question names Turn off as the gentler way. A program
  that writes its Run entry again at each start comes back after a removal and stays off after Turn off, another
  reason the question points there.
- **PR-D6 The checks' own start-up places.** A check that turned off a real entry, even one it made itself in the
  real Run key, would be one crash away from changing what the user's computer starts. So the places are data
  (`StartupPlaces`), a test run's are its own (PR-5.8), and the same registry code runs on both.

## 7. Open questions

- Grouping a program's processes into one item ("chrome, 12 processes, 1.2 GB").
- The startup programs Windows keeps elsewhere: the startup tasks of Store apps (Settings, Apps, Startup lists them)
  and scheduled tasks that run at sign-in.
- Remove through the Recycle Bin for a Startup folder's file, once axFile's operation is shared code (PR-5.6).
- A filter field for the service list, which is long.

## 8. Status

- 2026-10-23: built on the branch `axProcess` (PR-1 to PR-7) with its tests; not yet run under NVDA. The test plan is
  `docs/axProcess/nvda-test-plan.md`.
- 2026-11-14: Turn off, Turn on and Remove in the Startup view (PR-5.4 to PR-5.8), with unit tests and the check
  script `tests/scripts/axProcess/04-startup-turn-off-remove.txt`, run on the test desktop; not yet heard under NVDA
  (test plan section 4, steps 4 to 9).
