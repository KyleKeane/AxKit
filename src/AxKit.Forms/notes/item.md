# AxKit.Forms: the item window (axTask, axPass, axMCP, axAutomate) and the list views

Part of the notes of `src/AxKit.Forms` (the index is `src/AxKit.Forms/CLAUDE.md`). One bullet per piece.

- `ContextBarMenu` (AX-9.11, B-19, 2026-11-11): the bar's Context menu over a window's menus of what acts on each kind of thing (the item apps' list, item and field; axProcess's three views), none of them on the bar and each disabled: `Fill` moves the items of the one the window's function names into Context and puts the ones shown before back, as Context opens, as the palette reads it and as an AI tool asks (`RefreshedOnOpening` on Context; the held menus stay off the bar and disabled, and every reader reads the bar alone, `CommandPalette.Entries`); with nothing to act on, one enabled item saying why; `Letter` is C.
- `ListViewState` (docs/axGIT/SPEC.md AG-2.2; from axGIT on its second use, axProcess, 2026-10-24): one view of a window's one list, its name, labels and keys, whether it is read, and the selected key kept across refreshes and view switches; `FillInto` fills the list on the kept key, `Remember` keeps the selected one.
- `ItemForm` and `ItemKeys`, with the records `ItemRow`, `ItemDetail` and `ItemDetailLine` (docs/axKit/item-apps.md; from axTask on its second use, axPass, 2026-10-24): the item apps' window on `AxKitForm`.
  - `ItemForm.cs`:
    - the navigator and the detail pane (`Navigator`, `DetailPane`, each an `AxKitList`) in a `SplitContainer`, the status bar;
    - `PlaceControls` with the app's menu bar, the commands of `ItemKeys` registered after the app's own (`AddSharedKeys`, called by `PlaceControls`; the bundle's Ctrl+Tab, Ctrl+Shift+Tab and F6 switch the panes from either one, AX-8.2, 2026-11-09);
    - the levels (`Into`, `OpenSelected`, `UpLevel`, `AnnounceLevel`, `Refresh`);
    - `Fill` (the one place the navigator, its name, the title and the status bar are filled, from the app's `ReadLevel`), and `Refill` for a change off the user's command (axTask's projects found, 2026-11-11: the rows rewritten in place and the selection kept while the same keys show, the detail pane made again only when the selected row's words changed and, while it has the focus, once the focus reaches the navigator (`_detailStale`), `Fill` otherwise; `LevelIdentity` names the level for `McpTarget`, the place by default);
    - and `FillDetail` (silently, on every selection change, from the app's `DetailOf`; the field selected kept while the same `ItemDetail.Key` shows).
  - `ItemForm.Fields.cs`:
    - every field command (`CheckField`, `ChangeField` by type, with the app's `ChangeSpecial` for its own types and the Title's change through `RenameItem`; `NewField`, `RenameField`, `RemoveField`, `MoveField`, refused with the app's `FieldsFixed`; `ReadAll`), each a new `ItemDocument` handed to the app's `SaveFields`;
    - and `ChooseFromList`, the list notice both apps use (`AskText`, the one-field notice, is the base's since 2026-11-09, `AxKitForm.Notices.cs`).
  - The app fills in:
    - the levels (`ReadLevel`, `EnterList`, `LeaveList`; `EntryRefused` for a list Right and Enter do not go into, its reason said, axMCP's remote server);
    - the detail (`DetailOf`, `SpokenValue`);
    - the saving;
    - the navigator's commands (`NewItem`, `NewList`, `RenameItem`, `RenameTo` (an item's rename by default), `RenameRefused`, `DeleteRow`, `Undo`, `CheckInNavigator`);
    - and its three menus of a list, an item and a field, never on the bar, handed to `CreateContextMenu`, which makes the bar's Context from them (AX-9.11, 2026-11-11: the field's in the detail pane, the item's or the list's in the navigator by the pane that last had the focus, `_detailLast`; "Nothing selected" or "A heading: nothing to act on" otherwise; `SelectionMenusAvailable` and `NothingWhileUnavailable` for axPass's lock); Shift+F10 opens it.
  - `ItemKeys.All` is in each app's key table.
