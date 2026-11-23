# axFile, the window

The WinForms side of axFile: one list with a folder's entries, the Actions list always under it ("No actions yet" until the first operation), a status bar, the bundle's notices. The pure code (the folder's entries, the operations, the Actions list's lines, the settings) is `src/AxFile.Core`, with its own `CLAUDE.md`. The spec is `docs/axFile/SPEC.md` ("SPEC.md" below); the guide `docs/axFile/user-guide.md` is embedded by `AxFile.csproj` for Help → User guide; the manual test plan is `docs/axFile/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`. The player and the preview pane this window carried until 2026-10-22 are axMedia and axPreview now (the split's inventory of 2026-10-22, in the git history); axFile hands over to them and plays and shows nothing itself.

## Commands

```
& '.\scripts to run-individual-apps\run-axFile.ps1' C:\Music\Dune    # build AxKit and start axFile on the folder (run.ps1 -App file); a file selects itself in its folder
& '.\scripts to run-individual-apps\run-axFile.ps1'                  # the last folder opened
dotnet build AxKit.sln
dotnet test tests/AxFile.Tests/AxFile.Tests.csproj
```

## Where things are

- `AxFileApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run` with the one folder or file of the command line, the settings and the window; the log (`axFile.log`), the crash handling, `--help` and `--version` are the host's. It moves the process's working directory to the program's folder (AF-1.2): a folder that is a process's current directory cannot be renamed or moved (Windows' "being used by another process"), and the apps started from here inherit the working directory.
- `FileForm`, in six partial files (`FileForm.Completion.cs`, the Go to folder field's completion, out of Browse, and `FileForm.Programs.cs`, the user's own programs and Windows' own ways, out of Operations, since 2026-10-24). It is a navigator, the bundle's item window (`AxKit.Forms/ItemForm`, its notes in `src/AxKit.Forms/notes/item.md`; SPEC.md AF-D14, docs/axKit/framework.md step 13, 2026-11-22; a window of its own on the window base until then): a folder is a level, each shown entry a row (a folder a list, a file an item, the row's key the entry's path, its `Tag` the entry), and the Actions list the second pane as a log.
  - `FileForm.cs`:
    - the controls are the item window's: `_list` is its `Navigator` (the folder list, the rename box a child of it, AF-4.2) and `_pane` its `DetailPane` (the Actions list); the split between them and the status bar are its own;
    - what the navigator asks for: `Pane` (a log), `LogLineMenu` and `LogName`, `HasSelection`, `RowNoun`, `EmptyLine` (the empty folder's line or the filter's), `AfterRowsLine` (the not-shown line), `StatusState` (the newest operation's line), `ReadLevel` (the rows from the shown entries, the list's name with the count, the title, the path), `LevelIdentity` (the folder, so that another folder starts without marks), `KeysWithoutUse`, `NothingToSelect`, and in Browse `EntryRefused` (Right on a folder: the read started, the level staying until it is in), `OpenItem` and `UpLevel` (Left);
    - the menus (AF-6.1):
      - the bundle's eight (`PlaceMenuBar`, AX-9.1, 2026-11-11): Session, Actions (Undo, New folder, New text file, Paste, Select all, Deselect all), Navigation (Favorites, Drives, Go to folder, Up one level, The Actions list), Context (the item window's `CreateContextMenu` over the entry's menu and the Actions list's line's, `_lineMenu`, by the list that last had the focus; on a line that is no entry it holds that line's words; AX-9.11), Options (Sort by and Filter first);
      - `FillItemMenu` fills the entry's menu, never on the bar, with one flat list in File Explorer's order, AF-4.6, and its Selection submenu;
      - the Selection submenu holds the Space toggle, Select to here and the operations on the marks, AF-3.7;
      - `FillOpenWith` in Programs fills Open with ▸ on each opening;
    - the hooks the window base asks for;
    - `WndProc` (the shell menu's messages forwarded to `Shell`);
    - the command list (docs/axKit/app-kit.md AK-2):
      - each menu item with a key is `Add(menu, new Command(...))`, a key with no item `AddKey`, its `When` where it acts, the item window's `InNavigator` or `InDetail`, its `Mark` for MCP; the item window adds its own keys after these (`PlaceControls`) and leaves a key to axFile where axFile has a command for it in the same place (`KeyTakenWhere`): Enter, Backspace, F2, Delete, find, F5, Ctrl+N and Ctrl+Shift+N are axFile's own, and the pane keys (Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab, F6), Right and Left, the selection's keys, Shift+F10 and the Applications key in either list, and Escape (deselect all in the folder list, back from the Actions list) are the item window's;
      - no `ProcessWindowKey` since 2026-10-24, the base running the list;
      - the Selection submenu's operations on the marks only show the entry's keys, the File Explorer item of Show current folder in only shows Ctrl+W, and Ctrl+D is a key of its own beside the Favorites submenu, refilled as it opens;
      - Enter, Shift+Enter, Backspace, Space, F2, Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Ctrl+Enter and Ctrl+Shift+Q only while the folder list has the focus;
      - in the Actions list Enter, Escape, Ctrl+Z and Ctrl+I act on its line;
      - Shift+F10, the Applications key and a right click open the bar's Context menu through the item window's `OpenPaneMenu`, AF-D13, AX-9.11;
    - `Failed` (one line as an announcement, more as a notice; the base's `Do` runs every task and turns an escaped exception into a notice);
    - `UpdateStatusBar` (the newest action's line and the folder's path, AF-2.3).
  - `FileForm.Browse.cs`:
    - `StartAsync`;
    - `ShowFolderAsync` (the read on a task, the list refilled with the selection by name, the title, the recent list; a folder gone gives the notice and the nearest parent that exists);
    - `Rearrange`, which puts the entries in order (`PutInOrder`) and has the item window fill the list (`Fill`, which asks `ReadLevel`):
      - `_entries` is the folder as read and `_sorted` the same entries in the chosen order, AF-3.8, which the level's rows are made from, one for one; the item window then shows the rows that pass its filter, so what the list holds is its `Rows`, each row's `Tag` its entry (`ShownEntries`), and an index into the list is an index into them, never into `_sorted`;
      - an empty folder gets one item (`EmptyLine`), a filter nothing passes the item window's line, and a read that left hidden or period-named entries out adds the last line "Not shown: …" (`AfterRowsLine`);
      - `CurrentEntry` (the selected row's entry) counts neither as an entry, and `SayNoEntry` reads the line the selection is on;
    - the filter and the sort (AF-3.8) are the item window's since 2026-11-22 (`AxKit.Forms/ItemForm.Arrange.cs`, docs/axKit/framework.md FW-4.10a): Options → Filter… and Sort by, Escape for the filter after the selection, the words. axFile names its orders (`SortNames`), keeps the chosen one in its settings (`SortChoice`), gives its rows in that order (`OrdersRowsItself`) and puts them in a new one when asked (`SortChanged`); the filter has no key (Ctrl+Shift+F until then);
    - `TryFolder`, `NoFolderHere` and `TryEntry` (the guards: the folder a command needs, or "No folder" and "Choose a drive first" at This PC said, AF-3.10; the entry under the selection, or the line it is on said; `RealFolder`, the folder shown when it is one on disk, which Ctrl+N and the restart pass on);
    - `FillFavorites` and `ToggleFavorite` (AF-3.9);
    - the Drives submenu of Folder (AF-3.11; a menu of its own until 2026-10-22);
    - the selection (AF-3.7, AF-D9):
      - `Selection` is the item window's, the bundle's `AxKit.Forms/ListSelection` over `AxKit.Core/Marks` (bundle AX-8.16, shared since 2026-11-22; the keys are `AxKit.Forms/SelectionKeys`), on its rows, the shown entries by path and name; it makes each item, rewrites the items whose mark flipped and says the words;
      - the keys (Space, Shift+Space, Ctrl+A, Ctrl+Shift+A, Escape, the four Shift keys of its `MoveCommands`) are the item window's commands, which say the line on a line that is no entry (`NothingToSelect`); the menu items only show them; a change of the marks is no line of the Actions list (it was one from 2026-10-22 to 2026-11-22, with its own undo; SPEC.md AF-D11), so nothing of the selection is axFile's own;
      - the item window keeps only the marks on the entries still shown as the list is filled, and drops them all for another folder (`LevelIdentity`);
      - `OnMarks` (the Selection submenu's operations, on the marks alone);
      - `SelectedEntries` and `Targets`, what an operation acts on;
    - `OpenEntry` (Enter: a folder here, a file through `OpenWithWindows`, AF-3.2);
    - `OpenInPreview` (Shift+Enter: axPreview on a file, a new axFile window on a folder);
    - `GoUp`, `EntryRefused` and `UpLevel` (Backspace; Right and Left, the item window's Into and Up a level, AF-D12: a folder or drive opened, up one level, the edge earcon on a file or at This PC);
    - the folder picker, the recent submenu;
    - Go to folder: its field's `KeyHook` is `OnPathKey`:
      - Down and Up run `CompleteAsync`, the folder read on a task through `AxFile.Core.PathCompletion`;
      - `_completed` the text the last completion put there so that repeated keys cycle and typing starts afresh;
      - Right at the end of a completed folder lists inside it;
    - refresh (F5, and silently on activation when the folder's write time moved);
    - find (the base's `ShowFindInList` and `FindInList` over the entries' names);
    - information, and `FolderAnnouncement`;
    - Type-ahead (AF-3.6) and the edge earcon are the bundle's list's, `AxKit.Forms/AxKitList`, since 2026-10-24; the folder list gives it the shown entries' names.
  - `FileForm.Operations.cs`:
    - `Undo` (the newest undoable line of the Actions list) and `UndoAction` (one line taken back, AF-4.14, AF-9.3: `FileOperations.CheckUndo` on a task, `_actions.NotUndone` with the reason when it refuses, then `Shell.Undelete` for a delete or `FileOperations.UndoAsync`, `_actions.Undone`, the folder shown with the entry selected and "Undone: …" said; the Actions menu's item is renamed from `ActionLog.MenuText` as the menu opens);
    - new folder (`NewFolder`, the base's `AskText` with `CheckNewName` as its refusal, so the notice stays on a bad name, AF-4.1; Add a program in `FileForm.Programs.cs` the same way), new text file (`NewFile`: the file made, shown and selected, then `Rename(wholeName: true)`, AF-4.1), move into new folder (AF-4.13);
    - the rename in place (the box is the bundle's, `AxKitList.BeginRename`, AX-8.7, since 2026-10-24, and its steps the item window's `RenameSelected` since 2026-11-22: an unchanged name, a refused name with the box kept open, the name taken): `Rename` says axFile's guards and opens it; `RenameSelectionLength` (a file's name before its extension, a folder's and a new file's whole name), `RefusedName` (`CheckNewName`) and `RenameTo` (the operation) are what axFile gives; the box, its keys and `EndRename` are `AxKitList`'s;
    - delete (on the marked entries in one shell operation, `NameAfter` choosing what to select next);
    - copy path, cut, copy and paste through the clipboard (`CopyOrCut`, `Paste`, AF-4.8);
    - the File Explorer and the other apps' windows (`NewWindow`, `ShowFolderInExplorer` and the base's `CreateShowInMenu` on the folder, `OpenInAxKit` on the entry, AF-4.9);
    - open in editor (`AxKit.Core/Dispatch.EditorOf`);
    - open with Windows (the Open with dialog when Windows has no program);
    - zip files (AF-4.12: `Compress` and `Extract` through `RunArchiveWork`);
    - the user's own programs (AF-4.11), and Windows' own ways with the entry (AF-4.7): `OpenWith`, `ShowProperties`, `ShowWindowsMenu`.
    - Every operation that changes the disk goes through one of two runners:
      - `RunOperation(folder, doing, waiting, failTitle, busy, work)` for one on a task (its line from `Begin` to `End` or `FailAction`, the `busy` paths held in `_inFlight` meanwhile, "<waiting>, please wait" after a second, the result through `ShowResultAsync`; `work` returns an `Outcome`, the line, the operation, the entry to select and what is said, or `Outcome.Failure(reason)` for a reason the shell gave; an exception is the failure too);
      - and `RunAtOnceAsync(folder, failTitle, work)` for one that ends at once (`Record`, then `ShowResultAsync`).
    - Every operation runs on a thread of its own and the window waits for none (AF-4.5):
      - `_inFlight` holds the paths the running pastes and moves are writing or moving away, so that `Paste` chooses its names around them (`FileOperations.Destinations` with the reserved paths) and a cut still on its way says "Already pasting", or pastes the files that are not and counts the ones that are;
      - the set is read on the UI thread, snapshotted for the task, and cleared in the runner's `finally`.
  - `FileForm.Actions.cs` (AF-9, 2026-10-22):
    - the Actions list, the item window's second pane as a log (`_pane`, its `DetailPane`), an `AxKitList` named "Actions" under the folder list from the start, "No actions yet" as its one line until the first operation (hidden only while a notice shows; the item window gives a notice's focus back to it), a third of the window (the item window's split);
    - `_actions`, the `AxFile.Core.ActionLog`;
    - `FillPane`:
      - the rows from `ActionLog.Rows`, each line changed in place while the count is the same, so that the selection stays and only the changed line moves under NVDA, the running line the user is on left alone while it is the same entry on the same row, `_paneRows`;
      - otherwise the whole list with the newest line selected while the focus is in the folder list, so that Tab lands on it;
      - the line the user is on is found in `_paneRows`, so the callers pass nothing;
    - Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 between the two lists are the item window's pane keys, named with `LogName`;
    - `ShowActions` (Navigation → The Actions list);
    - the line methods above (`Begin`, `Progress`, `End`, `FailAction`, `Record`);
    - the Actions list's sections and header lines come from `ActionLog.Lines`, and a line of the list is mapped to its entry with `EntryAt`/`IndexOf`, so that `FillPane` keeps the user's line by entry;
    - the list's keys:
      - `GoToAction` (Enter: the folder list on where the action left things, `ActionEntry.Location`, the focus moved before the folder is said);
      - `UndoSelectedAction` (Ctrl+Z on the line, with the reasons a line cannot be undone);
      - `SayActionInformation` (Ctrl+I);
    - and the hand-overs to axMedia (AF-5):
      - `PlayFolder` (Ctrl+Enter: axMedia on the folder);
      - and `AddToQueue` (Ctrl+Shift+Q: axMedia on the targets with `--add`, `MediaCount` on a task for "Added n to axMedia's queue").
  - `FileForm.Completion.cs`: the Go to folder field's completion, out of Browse (since 2026-10-24).
  - `FileForm.Programs.cs`: the user's own programs and Windows' own ways, out of Operations (since 2026-10-24).
- `AxFileKeys`: the window's keys (SPEC.md AF-6.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.
- The entry's menu has no context-menu copy (AF-D13, 2026-10-23): the item window's `OpenPaneMenu` (axFile's own opener in Programs until 2026-11-22) fills Context and is the window base's `OpenBarMenu` with C since 2026-11-11 (I until then), which waits until Shift, F10 and the Applications key are up, then posts `SC_KEYMENU` and the letter, as Alt and C typed, so that WinForms opens the bar's Context drop-down from the keyboard and NVDA reads its first item. The list's own `ContextMenuStrip` (the former `ItemMenu`) was silent the first time it opened however it was shown; a mnemonic inside the `SC_KEYMENU` message opened Folder; with the keys still down F10 closed the menu and the letter was Shift+I.
- `Shell` (SPEC.md AF-4.7, AF-D8): the things Windows does with an entry (its Open with dialog is `AxKit.Forms/WindowsOpenWith` since 2026-10-24, axPreview using it too), on the UI thread apart from `Undelete`, which the window runs through `FileOperations.OnShellThreadAsync`: `Properties` (`ShellExecuteExW` with the verb `properties`), `ShowMenu` (the shell's context menu: `SHParseDisplayName`, `SHBindToParent`, `IShellFolder.GetUIObjectOf`, `IContextMenu.QueryContextMenu` into a popup of `CreatePopupMenu`, `TrackPopupMenuEx` with the command returned, `InvokeCommand`), `Eject` (the same menu built for a drive, the item whose `GetCommandString` verb is `eject` invoked through `FindVerb` and `Invoke`, AF-3.11), `Undelete` (AF-4.14: the Recycle Bin bound as a shell folder from `SHGetKnownFolderIDList`, its items enumerated with `IEnumIDList`, each one's `$R` parsing name read through `GetDisplayNameOf` and `StrRetToBufW` and its record through `AxFile.Core.RecycleBin`, the items whose original path is wanted, the latest deletion of each, given to `GetUIObjectOf` together and the verb `undelete` invoked once), and `ForwardMenuMessage`, which the window's `WndProc` calls so that the shell fills its submenus (`IContextMenu2`/`3.HandleMenuMsg`). The interfaces are declared in vtable order with `PreserveSig`.

## Rules

1. Every control gets an `AccessibleName`; the folder list is named with its count alone, the folder being the title (AF-2.1); the Actions list is "Actions" and is always there, "No actions yet" until the first operation (AF-9.1); nothing speaks on a plain arrow key that moves, and a key that cannot move, or a typed name nothing starts with, sounds the bundle's edge earcon (AF-3.6, `AxKitList`, `Sounds.Edge`); a change of folder says the folder, its count and the entry (AF-3.1, `FolderAnnouncement`, which cuts short NVDA's own reading of the new selection); a command's result is one announcement (AF-7), on the list that has the focus, or on the notice while one shows. Nothing is spoken for a line of the Actions list arriving or changing.
2. The folder reads and the operations never run on the UI thread: `Task.Run` for the reads, the rename and the new folder, a thread of its own for the shell's delete, copy, move and Restore.
3. No second window (bundle AX-7.2): questions and errors are notices; the folder picker, File Explorer and the shell's own dialogs and menu are the system's own windows. Playing and reading are other apps' windows, started with the paths (AF-D2).
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a list (`AxKitList`, a `ListBox`) reaches `ProcessCmdKey` as `Keys.Return`; handled there with a `_list.Focused` check, and returning true keeps the list from dinging. Backspace, Space, F2 and Delete are the same; Shift+Enter arrives as `Keys.Shift | Keys.Return`.
- A read that comes back after the user has moved on is dropped (the item window's `BeginRead` and `EndRead`, 2026-11-23; a counter here until then; the status bar says "Reading…" meanwhile): a slow folder would otherwise overwrite a faster one chosen later.
- "The process cannot access the file because it is being used by another process" on renaming a folder is a process whose current directory the folder is (a terminal or an app started in it; probed 2026-10-22), not an open handle: an open enumeration of the folder's contents does not block a rename on Windows 11, and an open file inside gives "Access to the path is denied" instead. axFile holds nothing (every enumeration is a `foreach`, the shell objects are released at once, the file operation has no owner window); `FileOperations.Message` says what the two codes mean.
- The Actions list is a bottom-docked list, always visible (it was hidden until the first operation, which left Tab with nowhere to go); the docking order in `Controls.AddRange` puts it between the folder list and the status bar.
- The Actions list keeps the `ActionEntry` objects the operations were given back, so a running line is updated by object, not by index: lines may fall off the front of the list meanwhile.
- An array parameter of a COM interface method marshals as a SAFEARRAY by default (P/Invoke defaults to a C array): `GetUIObjectOf`'s list of item ids is `[MarshalAs(UnmanagedType.LPArray)]`, or the shell reads garbage and the process dies with an access violation, which the probe found.
- A `[ComImport]` interface that derives from another gets no vtable slots from it: `IContextMenu2` and `IContextMenu3` repeat `IContextMenu`'s three methods before their own. A cast of the object to the derived interface is the QueryInterface.
- The shell's menu is tracked after a `BeginInvoke`, so that the Item menu (a `ToolStripDropDown`) has closed and the list has the focus back; `TrackPopupMenuEx` wants its owner in the foreground. A `WM_KEYDOWN` of Down posted just before it selects the first item for the keyboard, as File Explorer's Shift+F10 does; otherwise a native popup opens with nothing selected and NVDA reads nothing until an arrow key.
- The keys of an open `ToolStripDropDown` go to the drop-down's `ProcessCmdKey` through WinForms' menu-mode message filter, never to the form's: a key that should work inside a menu needs a menu subclass, which a bar drop-down cannot be (AX-6.6). The menu's `Click` runs after the drop-down has closed; what needs the focus back on the list (the rename box, the shell's popup) is still posted with `BeginInvoke` to be safe.
- The clipboard's file list is `DataFormats.FileDrop` (`CF_HDROP`); whether File Explorer cut or copied is the four-byte "Preferred DropEffect" format (2 means move), read back as a `MemoryStream`. After a move the clipboard is cleared, as File Explorer does.
- The shell's extensions run in this process while its menu is up; an exception from one is caught and reported, a crash in one is not ours to prevent, which is why the menu is behind one item (AF-D8).
- A menu bar item's drop-down is the one WinForms makes, never a hand-made one (AX-6.6; `MenuTests` in the bundle's tests scans the sources for `DropDown = new`). The bar's Item menu was an `ItemMenu`, a `ContextMenuStrip`, for Ctrl+I inside it (AF-4.10): a drop-down that is not auto-generated has its `OwnerItem` set to null by `ToolStripDropDownItem.OnDropDownClosed`, so WinForms' Escape handling (`ToolStripDropDown.SelectPreviousToolStrip`) finds no owner strip and never resets the bar's `MenuAutoExpand`; from then on Left and Right open the neighbours' menus instead of moving along the bar (the user: "jumps into the items"). The context menu that kept the subclass is gone (AF-D13). Menu mode cannot be entered by posted messages in an off-screen probe (`ToolStripManager.ProcessMenuKey` wants the window in the foreground), so the keyboard walk of the bar is a step of the test plan, not a test.
- The selection is marks of the window's own written into the item texts (", selected"), not `SelectionMode.MultiSimple` (AF-D9): the list box's multiple selection separates the caret from the selection, so everything "current" would need `LB_GETCARETINDEX` and `LB_SETCARETINDEX`, and what NVDA says on the toggle was unknown. Setting `_list.Items[i]` to a new text deletes and reinserts the native item and selects it again; whether NVDA reads it a second time after Space is a step of the test plan.
