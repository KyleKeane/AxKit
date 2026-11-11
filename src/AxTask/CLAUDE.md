# axTask, the window

The WinForms side of axTask: the navigator and the detail pane, each the bundle's `AxKitList`, a status bar, the
bundle's notices. The pure code (the item format, the field values, the folder, the changes with their undo, the links,
the tasks and the smart lists, the settings) is `src/AxTask.Core`, with its own `CLAUDE.md`. The shared shape of the
item apps is `docs/axKit/item-apps.md` (IA-n.m); axTask's own spec is `docs/axTask/SPEC.md` ("SPEC.md" below); the guide
`docs/axTask/user-guide.md` is embedded by `AxTask.csproj`; the manual test plan is `docs/axTask/nvda-test-plan.md`.
The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axTask.ps1 manual_tests\axTask\tasks   # build AxKit and start axTask on the test folder (run.ps1 -App task)
.\test.ps1 AxTask                            # the tests in Release, as CI runs them
```

## Where things are

- `AxTaskApp`: the entry point `Run`, one call into `AxKit.Forms/AppHost.Run` with the one folder or item file.
- The panes, the moves between levels, the shared keys and every field command are the item apps' shared window,
  `AxKit.Forms/ItemForm` (since axPass, its second use); `TaskForm` derives from it and fills in what is axTask's.
- `TaskForm.cs`: the menus (Folder; List, Item and Field, only the one for the selection enabled by `ItemForm` from `SelectionMenus`;
  View, Options, axKit, Help) with axTask's own keys as commands of the window's one command list (`Add`, each key's`r
  command at its first menu item; docs/axKit/app-kit.md AK-2), the levels of the folder (`Start`, `EnterList`,
  `LeaveList`, which goes straight back to the top, `ShowAllTasks`, `ReadLevel` with the rows' `Tag`: an `ItemFile`, a
  `SmartList`, a `SectionTag` on a header, `ArchivedItemsTag`, or nothing for a list; the top level in sections is
  `ReadSections`, TK-3a) and the detail (`DetailOf`, with a `FileSource` to save to; a link read by `SpokenValue`).
- `TaskForm.Commands.cs`: the commands and the one way a file changes: `Commit` (a `Step` applied, kept for Ctrl+Z,
  the panes filled, its words said), `CheckFresh` (the file still as shown, IA-5.5), `SaveFields` (the shared field
  commands' change, written), `MoveToSection` and `SetSection` (TK-3a.3), `NewList` in the selected row's section, the link's notice (`ChangeSpecial`). Rename and Archive add `Links.AfterMove`'s writes,
  so links follow what moved.
- `TaskForm.GitHub.cs` (docs/axKit/reports.md RP-7, RP-8; SPEC.md TK-7a, 2026-11-07): the action keys (`RunActionKey` for Ctrl+1 to Ctrl+9 from the list's `list.json`: `SetFields` through `Commit`, in a GitHub list after GitHub took the Kind or State; `MoveItem`; `OpenTarget`, with `OpenInAxClaude` starting axClaude on the list's folder with `--message`, both through `ActionKeys.OpenRefusal`, which keeps a shell or Claude Code off a folder that is not on a drive of this computer, S-5), the Item menu's Action keys submenu with the names refreshed as it opens and the GitHub submenu (`PlaceGitHubMenus`), the GitHub list's refresh (`RefreshGitHubOnEnter` from `EnterList`, `RefreshGitHubAgain` from `BeforeRefresh`, `RefreshGitHub` off the UI thread through `Do`, the changes applied as a mirror, not a step), `NewGitHubList`, the GitHub actions (`SetKind`, `SetState`, `CommentOnIssue`, `Block`, each through `WithGitHubItem`, `WithGitHubToken` and `TryGitHubAsync`), the user's token (`UserToken`, `SetGitHubAccount` with a hidden field, `ForgetGitHubAccount`) and List → Action keys (`EditActionKeys`, `EditActionKey`, `WriteActionKey`).
- `TaskForm.Projects.cs` (SPEC.md TK-7b, 2026-11-11): the project folders (Options, Project folders: `AddProjectFolder`, one `AskText` whose empty field opens the folder picker, and Remove a project folder made again as it opens, `FillProjectFolderItems`), `FindProjects` off the UI thread through `Do` (`_findRun` drops an older look's result; the level read again through `ItemForm.Refill` only when the projects changed, held in `_refillHeld` while a notice or a rename is open and done in `OnNoticeClosed`), the Projects row and level (`ProjectsRow`, `ReadProjects`; `_atProjects`), a project entered as the top (`_project`: `TopFolder` is the project while it is shown, so everything else works there unchanged), and the search (Ctrl+Shift+F: `FindEverywhere`, `RunSearch` ("Searching every list" at once; `_searchRun` and `Busy` drop a result that comes late), the level `_search` with its `FoundItem` rows read again on every fill, `OpenFound` on Right and Enter and `LeaveSearch` on Left and Escape, those keys registered before the shared ones). `Gathered` is the Projects or the found level, where nothing is made or moved.
- `TaskForm.Mcp.cs` (SPEC.md TK-8, docs/axKit/mcp.md MC-8, 2026-11-11): the AI tools' `axkit_task_lists`, `axkit_task_items` and `axkit_task_add`, the override of the window base's `McpAppRequest` (the base has refused axPass, the switch, a notice, a dialog and the user typing first); the lists reached are `_settings.TopFolder`'s and `_projects`' (never a project shown as the top), an add goes through `Commit` with the words "Claude added X to List", selected only in the list shown, then `ChimeForMcp`; a rename open refuses it.
- `AxTaskKeys`: the window's keys, `AxKit.Forms/ItemKeys.All` and its own (`ActionKey(digit)` for Ctrl+1 to Ctrl+9; All tasks is Ctrl+0); `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every change goes through `Commit` with a `Step`; nothing writes a file another way.
2. Never write a file without checking it is as it was shown (`CheckFresh`): axJSON or a sync may have changed it.
3. The detail pane says nothing when it fills; NVDA reads it when the focus goes there.
4. The window, the field notices and the format are shared with axPass (IA-D5): a change to them changes both apps;
   keep what is axTask's own (Done, Due, All tasks, smart lists, links, files) in `TaskForm`.
5. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The reads and writes run on the UI thread: items are small local files. All tasks reads every file under the top
  folder on each fill (TK-D3); a slow share would stall the window.
- Enter, Space, Delete, Escape, Backspace, Tab and the arrows reach `ProcessCmdKey` while a list has the focus; they are
  `ItemForm`'s commands with a `When` of the pane focused, and taking them keeps the list box from its own move and its
  ding. `ItemForm` registers the shared keys after the app's (`PlaceControls`), so that an app's key comes first; a
  command of axTask that acts anywhere has `When = NotRenaming`, so that the rename box keeps its keys.
