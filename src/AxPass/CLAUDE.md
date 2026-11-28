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
& '.\scripts to run-individual-apps\run-axPass.ps1' manual_tests\axPass\test.kdbx    # build AxKit and start axPass on it (run.ps1 -App pass)
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
- The selection (SPEC.md PW-6.4a, bundle AX-8.16, 2026-11-22; in `PassForm.Commands.cs`): `HasSelection`, `CanSelectRow` (an entry outside the Recycle Bin; never a group, never what is in the Recycle Bin), `LevelIdentity` (the database, the group and whether it is locked, so that the selection is given up when the group changes and when the window locks). Delete acts on the item window's `RowsInHand`, the selected entries whenever any is selected (two of one name refused first).
- The Recycle Bin (SPEC.md PW-6.4b, 2026-11-27): `_binOn` (the tool's `db-info` at each unlock and F5), `_binSeen` (the group a deleted entry was seen to arrive in) and `Bin` (`PassBin.Known`: that group and no other, never a group by its name, so the first delete inside a group after each unlock asks); F5 goes on when `db-info` fails (`Ran`, `Cli` without the failure's announcement); `DeleteRow` asks by `PassBin.Fate` (a question that says so where the end is not known, Cancel the default); `RemoveAsync` is the one removal: one `rm` or `rmdir` each through `Cli`, the database listed again, what became of each read from the two listings (`PassBin.Outcome`), the sentence that (`PassBin.Said`), and a `Step` only for the entries that are the one of their name in the bin. A `Call` of a step is `Sole` (run only while a listing read just before shows one entry of that path) or `Purge` (the undo of a new entry or group: removed, and deleted in the bin where it is the one of its name, `PurgeAsync`). Never write "Recycle Bin/<name>" into a run by hand.
- The review's third pass (2026-11-27): `ForgetDatabase` (with `ForgetDatabaseState`, the fields alone) is the one place that forgets a database, called by `Lock`, `OpenDatabase` and the made unlock (M6); `PassWindowTests` lists the window's fields, and since the fourth pass (L3) the item window's under it, as the database's or the window's, held against the classes by reflection, and makes a real window (on a thread of its own, never shown), gives it an open database's state, a selected entry and an entry's detail, and runs `ForgetDatabase` and then `Lock` on it: a new field is placed in one list or the other, and the test fails without the selection's clearing. `RemoveAsync` lists the database right before its first removal and takes a group for the bin by `PassBin.SeenBin` alone; F5 forgets the bin seen when `db-info` no longer says the bin is on (M7). `_unlocksAsked` and `_locks` decide with `PassLocks.DroppedByLock` whether a dropped unlock says the window was locked (L1).
- Nothing after the lock (PW-4.3, 2026-11-27): `_unlock` counts the unlocks asked for and made, the locks (Windows' own whatever the window's state) and the databases opened, and `Tool` is the one way the window waits for work off its thread (KeePassXC's tool, a backup's copy): it gives null when the count moved meanwhile. Every `KeePassCli.Run` and `Backups.Make` is written `await Tool(() => ...)`, and `Task.Run` stands in `Tool` alone; `PassWindowTests` fails on a wait that goes around it (the second pass found the unlock itself and the new database outside the count). `NotUnlocked` shows the unlock notice again after an unlock a lock dropped. Generate also puts its password only into the notice it was asked from. The Ctrl+Enter command's `When` only looks at the row (it is asked whenever the keys are listed; `SelectedEntry` speaks).
- `PassForm.Commands.cs`: `SaveFields` (`edit` with `EntryFields.EditOptions`; a change that would empty a field is refused first, and a value written into an empty field is kept as a `Step` that Ctrl+Z says it cannot take back, PW-6.1), `RenameItem`, `RenameTo`, `DeleteRow`
  (above; a group or the Recycle Bin's entries after a question), `NewItem`, `NewList`, `Undo` (the
  runs a `Step` keeps), the password's notices (`ChangeSpecial`, Generate, typed twice, `edit -p`), the copies (the
  user name by axPass, the password read with `show -s -a Password` and put on the clipboard marked private by `AxKit.Forms/PrivateClipboard`, cleared after 30 seconds, PW-5.4), the URL,
  the database's picker and `db-create`, the tool's place.
- `AxPassKeys`: the window's keys, `AxKit.Forms/ItemKeys.All`, the bundle's selection keys and its own; `All` feeds the collision test.
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
