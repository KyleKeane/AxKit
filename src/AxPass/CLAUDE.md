# axPass, the window

The WinForms side of axPass: one KeePass database in the item apps' shared window, `AxKit.Forms/ItemForm` (the
navigator and the detail pane, the moves, the shared keys, every field command). The pure code (KeePassXC's tool as a
process, its output read into groups, entries and fields, a field's change into the tool's options, the settings) is
`src/AxPass.Core`, with its own `CLAUDE.md`. The shared shape is `docs/axKit/item-apps.md` (IA-n.m); axPass's own spec
is `docs/axPass/SPEC.md` ("SPEC.md" below), with the format and what the tool does in `docs/axPass/kdbx-notes.md`; the
guide `docs/axPass/user-guide.md` is embedded by `AxPass.csproj`; the manual test plan is `docs/axPass/nvda-test-plan.md`.

## Commands

```
.\manual_tests\prepare.ps1 axpass                 # the test database, password test-master-1 (needs KeePassXC)
.\run-axPass.ps1 manual_tests\axPass\test.kdbx    # build AxKit and start axPass on it (run.ps1 -App pass)
.\test.ps1 AxPass                                 # the tests in Release, as CI runs them
```

## Where things are

- `AxPassApp`: the entry point `Run`, one call into `AxKit.Forms/AppHost.Run` with the one database of the command line.
- `PassForm.cs`: the window on `ItemForm`: the menus (the bundle's eight through `PlaceMenuBar`, AX-9.1: Session; Actions with New entry, New group, Lock and Undo; Context over the Group, Entry and Field menus, never on the bar, from `ItemForm.CreateContextMenu`, "Locked: Enter unlocks the database" while `SelectionMenusAvailable` is false; Options with Lock after and the tool's place), axPass's own keys
  as commands of the window's one command list (`Add`, `AddKey`; docs/axKit/app-kit.md AK-2; each before
  `ItemForm`'s shared keys, so that Enter unlocks a locked window before it opens; a command that acts anywhere has
  `When = NotRenaming`, so that the rename box keeps its keys), the levels from the `PassTree` (`ReadLevel`, `EnterList`,
  `LeaveList`; a group's key ends in a slash, so that a group and an entry of one name differ), the detail (`DetailOf`;
  an entry's fields read with `show` when first selected, `ReadEntry`, kept in `_entries` until the window locks),
  unlocking and locking (`ShowUnlock`, `UnlockAsync`, `Lock`, `LockWhenIdle` from `GetLastInputInfo`), `ReloadAsync`,
  and `Cli`, the one way a command of the tool runs: off the UI thread, the master password first on its input, one
  at a time (`_busy`), its failure said, and before the first change after unlocking a backup (`_backedUp`,
  `Backups.Make`; no change without it; a command and an option are named by `KeePassCliTexts`' constants, never
  written as a string here, root `CLAUDE.md` rule 12, and `KeePassCliTextsTests` fails on one); `BackUpNowAsync` and `ShowBackups` for the Session menu.
- `PassForm.Commands.cs`: `SaveFields` (`edit` with `EntryFields.EditOptions`), `RenameItem`, `RenameTo`, `DeleteRow`
  (`rm` with its `mv` back; a group or the Recycle Bin's entries after a question), `NewItem`, `NewList`, `Undo` (the
  runs a `Step` keeps), the password's notices (`ChangeSpecial`, Generate, typed twice, `edit -p`), the copies (the
  user name by axPass, the password read with `show -s -a Password` and put on the clipboard marked private by `AxKit.Forms/PrivateClipboard`, cleared after 30 seconds, PW-5.4), the URL,
  the database's picker and `db-create`, the tool's place.
- `AxPassKeys`: the window's keys, `AxKit.Forms/ItemKeys.All` and its own; `All` feeds the collision test.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. The master password and an entry's password never reach a log, the diagnostics, a file, a command line or an
   announcement; they go to the tool on standard input only, and the notice's field is emptied as soon as it is read.
2. Every command of the tool goes through `Cli` (or `KeePassCli` from a `Task.Run`), never on the UI thread.
3. axPass never decrypts the database itself (PW-D1); a need the tool cannot meet is said, not worked around.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The tool's paths have no escape for a slash, so a title or a group name with one is refused, and `mv` moves entries
  only: a group in the Recycle Bin comes back in KeePassXC's own window.

- A notice holds the window's own keys, so Generate is its button's Alt+G, not a window key.
