# axMedia, the window

The WinForms side of axMedia: one list with the queue, a video area shown only while a video plays, a status bar, the bundle's notices. Since 2026-11-22 the window is a navigator, the bundle's item window (`AxKit.Forms/ItemForm`, `src/AxKit.Forms/notes/item.md`; `docs/axKit/framework.md` FW-4.9): one level, the queue, and no second pane. The list (`Navigator`), its selection, find, the Context menu and a navigator's keys are the item window's; this folder holds the queue's commands and the player. The pure code (the player seam, the queue, a folder's media files, the settings) is `src/AxMedia.Core`, with its own `CLAUDE.md`. The spec is `docs/axMedia/SPEC.md` ("SPEC.md" below); the guide `docs/axMedia/user-guide.md` is embedded by `AxMedia.csproj` for Help → User guide; the manual test plan is `docs/axMedia/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`. The window grew out of axFile's player and queue pane on 2026-10-22 (the split's inventory of 2026-10-22, in the git history).

## Commands

```
& '.\scripts to run-individual-apps\run-axMedia.ps1' C:\Music\Dune           # build AxKit and start axMedia on the folder's media files (run.ps1 -App media)
& '.\scripts to run-individual-apps\run-axMedia.ps1' C:\Music\Dune\03.mp3    # the file, with the folder's other media files after it
& '.\scripts to run-individual-apps\run-axMedia.ps1'                         # an empty queue
dotnet build AxKit.sln
dotnet test tests/AxMedia.Tests/AxMedia.Tests.csproj
```

## Where things are

- `AxMediaApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7): the paths, `--add` and `--new` read off the command line, then, with paths and an open axMedia window, one `AppMessage.Send` and no window of this process (SPEC.md AM-1.2); otherwise one call into `AxKit.Forms/AppHost.Run` with the settings and the window on a `HandOver`; the log (`axMedia.log`), the crash handling, `--help` and `--version` are the host's.
- `MediaForm`, in two partial files since 2026-10-24 (`MediaForm.Player.cs` is playing, AM-4: the engine, the transport, the position, the engine's events):
  - the controls (the video area, placed with the item window's `PlaceWith`; the list and the status bar are the item window's), the menus (AM-5.1; the bundle's eight through `PlaceMenuBar`, AX-9.1, 2026-11-11: Actions the player, Navigation the queue's moves and Go to time, Context (the item window's `CreateContextMenu(file)`, AX-9.11) the Play, Remove, Information and the two selection items of the file you are on, opened by Shift+F10, the Applications key and a right click; Options with Say each file as it starts first);
  - what the navigator asks for: `ReadLevel` (the queue as rows through `QueueRows`, the list's name with its count, the title, the status bar's second part), `Pane` none, `HasSelection` true, `RowNoun` "file", `EmptyLine` (the empty queue's one line), `LevelIdentity` (one level for ever, so the selection is kept), `OpenItem` (Enter plays), `DeleteRow` (Delete removes), `StatusState` (what plays, in the status bar's first part), `KeysWithoutUse` (F2, Ctrl+Z, F5 and Ctrl+Shift+N are no keys here);
  - the hooks the window base asks for (`TakeHandOver`: a hand-over message, answered by the base and queued through `Apply`, the queue drained in order by `DrainAsync`);
  - the menus and the keys as one command list (`Add` and `AddKey` of the window base, docs/axKit/app-kit.md AK-2; each key from `BundleKeys` and `AxMediaKeys`; Enter, Delete, the selection keys, find, the pane keys and the Context menu's keys are the item window's commands, registered by `PlaceWith` after the app's own, and the menu items for them (`Item`) only show the keys; pause and resume on `BundleKeys.MainAction`, Ctrl+Enter, with no `When`, AM-D7 as of 2026-11-22; no `ProcessWindowKey` of its own);
  - the base's `Do` runs every task (an escaped exception becomes a notice);
  - the queue:
    - `ApplyAsync` (a play replaces the queue through `MediaFolder.Queue` and plays with the playing file selected, an add appends through `MediaFolder.Expand` and says nothing, since axFile says it, AM-D3);
    - `FillList` (the item window's `Refill` while the same files are there, so that the row you are on stays and only the changed line moves under NVDA; its `Fill` otherwise, on the row of the same place or the one a play names; the list named with its count; the files as last filled are `_filledKeys`, since under the item window's filter the list shows fewer rows than the queue has), `QueueIndex` (a row carries its file's place in the queue as its `Tag`: a row's place in the list is not its file's place in the queue under a filter, so every command asks this), and the sort (`SortNames`, `OrdersRowsItself`, `SortChanged`: the queue is the order the files play in, so Name reorders the queue itself through `PlayQueue.OrderBy` and The order added puts it back, AM-3.6);
    - `AddPaths` (Open files, Open folder, a recent item: "Added n"), `FillRecent`;
    - `PlaySelected`, `RemoveSelected`, `ClearQueue`, `SayInformation`;
    - the selection of several files (AM-3.5, bundle AX-8.16, 2026-11-22): the item window's `Selection` over the rows `QueueRows` makes (a file queued twice is two rows: the key is the path and which occurrence it is), with its commands and its ", selected" in each line; `RemoveSelected` takes the selected files out, from the last to the first, when there are any; a play that replaces the queue clears the marks. In the notes "the file selected" of older lines means the file you are on;
    - `CurrentFile` (the playing file, or the selected one) for `ShowInExplorer` and the base's Show current file in submenu;
  - the player:
    - the engine made on first use with the video area's handle, `PlayCurrent`;
    - the events (`OnStarted`, `OnEnded`, `OnFailed`, `MoveOn`, `StopPlayback`);
    - the transport keys, the speed, Go to time, Ctrl+Q, the clock;
    - `UpdateTitle` with `TitleNow`, `UpdateStatusBar` with `StatusState` and `PlaceNow`, `FitVideo`.
- `AxMediaKeys`: the window's keys (SPEC.md AM-5.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys. Faster and Slower are Ctrl+Up and Ctrl+Down since 2026-11-14 (AM-D8), the bundle's paragraph keys taken on purpose (`KeyTests`' `Overrides`); Ctrl+Shift+Period and Ctrl+Shift+Comma are system-wide keys now (bundle AX-8.11) and no window's.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with its count ("Queue, 12 items"); nothing speaks on a plain arrow key; a command's result is one announcement (AM-6), on the list, or on the notice while one shows. "Playing <name>" when the queue moves on by itself is the one unprompted announcement, and a setting.
2. The player is driven from the UI thread and its events come back on it through the `BeginInvoke` the window hands `MediaPlayer` at creation; the folder reads of a hand-over or an add run on a task.
3. No second window (bundle AX-7.2): questions and errors are notices; the file and folder pickers and File Explorer are the system's own windows; the video area is a panel inside the window.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; handled there by the command list (the item window's commands, which act while the navigator has the focus), and returning true keeps the list from dinging. Delete and Space are the same.
- The video area's handle must exist before the engine is made (`_video.Handle` creates it, hidden or not); the engine needs it at creation and draws nothing in it for a sound file.
- The queue holds paths, not entries, and the window never indexes a list of its own: `PlayQueue.Playing` is the one index.
- A hand-over arrives through `SendMessage` from the starting process, which waits for the answer: the base's `WndProc` sets `m.Result` to 1 and posts the action `TakeHandOver` returns, so that nothing, not even the settings file, is touched before the answer; the folder is read on a task afterwards, and two hand-overs apply in the order they came (`_handOvers`, one queue since 2026-10-23). The starting process grants this one the right to come to the front (`AllowSetForegroundWindow` in `AppMessage.Send`) and the window activates itself once the queue is set, so that NVDA reads the new title and the playing file, not what was there before.
- The selection follows a play the user or a hand-over asked for (the playing file is selected), never the queue moving on by itself: the user may be arrowing through the list. "The file in hand" (`FileInHand`, for Ctrl+W, axFile, the restart and Recently closed) is the one playing or paused, else the one under the selection: after End of queue the queue's index still names the last file played, which is not in hand.
