# axProcess, the window

The WinForms side of axProcess: one list, a status bar, the bundle's notices. Since 2026-11-22 the window is a navigator, the bundle's item window (`AxKit.Forms/ItemForm`, `src/AxKit.Forms/notes/item.md`; `docs/axKit/framework.md` FW-4.6): the top level, "This computer", holds three lists (Processes, Services, Startup), the window starts inside Processes, and there is no second pane. Until then it was a window of its own with three views of one list. The pure code (the process table, the services, the startup list, the system's memory and times, the settings) is `src/AxProcess.Core`, with its own `CLAUDE.md`. The spec is `docs/axProcess/SPEC.md` ("SPEC.md" below); the guide `docs/axProcess/user-guide.md` is embedded by `AxProcess.csproj`; the manual test plan is `docs/axProcess/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axProcess.ps1'               # build AxKit and start axProcess (run.ps1 -App process)
.\test.ps1 AxProcess              # the tests in Release, as CI runs them
```

## Where things are

- `AxProcessApp`: the entry point `Run` (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`; no arguments.
- `ProcessForm`: the window (SPEC.md PR-2) on `AxKit.Forms/ItemForm`. The level is `_level` (one of the three lists, or null at This computer); `ReadLevel` gives its rows from what was read last (`LevelRows`: a row's key is the process ID, the service's name, the startup entry's place and name; its `Tag` is the `ProcessRow`, `ServiceRow` or `StartupEntry`, which every command reads its kind from; `TopRow` for a list at the top, with its count once read), `_loaded` says which list has been read and `_kept` the row each was left on. Going into a list is the app's own, off the UI thread (`EntryRefused` returns an empty reason and starts `ShowListAsync`, which reads on the first visit, sets the level only once the read is in, fills on the kept row and says the name with the row; Alt+1, 2 and 3 call it too); `LeaveList` goes up. `LoadAsync` reads one list on a background task with `_busy` set ("Reading…" in the status bar through `StatusState`, "Working…" for a second command); `ReadProcesses` takes two samples a second apart (PR-D3); `RefreshAsync` reads the list shown, or all three at the top, and fills on the row you are on. What the navigator asks for besides: `Pane` none, `HasSelection` with `CanSelectRow` (a process, a startup program), `LevelIdentity` (the level's name, since the status bar's second part is the memory and changes), `OpenItem` (Enter: the details), `DeleteRow` (Delete by the kind of row, on all the selected when there are several), `MenuOf` (Context's menu by the kind of row), `KeysWithoutUse`. The commands: `ActAsync` (the details notice of the row), `EndProcess`, `EndProcesses` (several, one question) and `Kill` (the question, then `Process.Kill` off the UI thread, the ID checked against the name since Windows reuses IDs; whether it ended and what to say), `ShowProgram` (axFile on the program file), `CopyLine`, `SaySummary`, `ShowServiceDetails` and `ControlService` (start, stop, restart through `ServiceManager` with its wait), `ChangeSelectedStartup`, `ChangeStartup` and `ApplyStartup` (SPEC.md PR-5.4 to PR-5.7: Turn off, Turn on, Remove; what is refused is said first, a removal asks with Cancel the default, the change runs off the UI thread through `StartupList.Apply`, one sentence from `StartupChange.Said`, then the list read again on the same entry or, after a removal, its neighbour), `RemoveStartups` (several, one question, Cancel the default), the item window's Sort by with the processes' three orders (`SortNames`, `SortChoice` kept in the settings, `OrdersRowsItself`, and `SortChanged`, which puts the rows in order again without a read). The menus and keys are one command list (`docs/axKit/app-kit.md` AK-2): axProcess's own keys (Alt+1 to 3, F5, Ctrl+E, Ctrl+C, Ctrl+Q) each one `Add(menu, new Command(...))`; Enter, Delete, Right, Left, the selection keys, find and Shift+F10 are the item window's commands, registered by `PlaceControls` after these, and the menu items for them (`Item`) only show the keys; the window has no `ProcessWindowKey` of its own.
- `AxProcessKeys`: the window's keys (SPEC.md PR-6.2; bundle AX-8); `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with what it shows and its count; nothing speaks on an arrow key; nothing refreshes on a timer (SPEC.md PR-D2).
2. Every read of the system and every end, start and stop runs on a background task; the window awaits it.
3. No second window (bundle AX-7.2): the details and the questions are notices.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The list is empty for the second the first read takes, while it already has the focus; `RefreshAsync` therefore says the list's name and first item once after that read, not interrupting.
- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; it is the item window's Open, which calls `OpenItem` on a row and goes into a list at the top.
- Delete is the item window's one command; `DeleteRow` chooses by the row's `Tag`. A new kind of row needs its case there, in `MenuOf` and in `CanSelectRow`.
- The level changes only after a list's read is in (`ShowListAsync`): the rows shown are always the level's, so a command never reads a process out of a row of services.
- The startup places are always `StartupPlaces.Current`, never built by hand: in a test run it is the run's stand-in (SPEC.md PR-5.8), so a check script never reads or changes what starts with Windows. A new command on a startup entry goes through `StartupList.Apply` with it.
