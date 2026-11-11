# AxTask.Core, the pure code

Everything of axTask that needs no window, unit tested in `tests/AxTask.Tests` over temporary folders. The item
format is `docs/axKit/item-apps.md` (IA-n.m), shared with axPass; axTask's own rules are `docs/axTask/SPEC.md` (TK-n.m).
The window is `src/AxTask`, with its own `CLAUDE.md`.

## Commands

```
.\test.ps1 AxTask
```

## Where things are

- The item format itself (`ItemDocument`, `ItemField`, `FieldTypes`, `Parsed`: an item or a list's `list.json` as JSON
  kept whole, every change a new document; the types read out and typed) is `AxKit.Core`'s since axPass, its second
  use (IA-D5), with its tests in `tests/AxKit.Tests`.
- `ItemFolder` and `ItemFile`, `FolderLevel`, `TopLevel` (IA-4, IA-D4, TK-3a, TK-D4): a list's items (`Read`; a
  folder inside a list, hidden and dot entries left out, what is not an item counted), the top folder's lists and the
  Archive's (`ReadTop`), a list's section (`SectionOf`, from the `SectionField` of its details), `ReadItem` (with the text read, for
  `CheckFresh`), `ReadList`, `NameOf`, `RefusedListName`, `AllItems` (the Archive
  left out), `ArchiveFolder`, `IsArchive`, `ListName`, `LinkTo` and `Resolve` (IA-D3). `FileStem` and `FreePath`
  (IA-4.5) are `AxKit.Core/ItemFiles` since their second use, the reports list (2026-11-07).
- `Changes`, `Step` and `FileChange` (IA-5.4): a change as parts, writes and moves, `Apply`, `CheckUndo` (every path
  as the step left it), `Undo` (last part first; a new list's folder goes with its `list.json`), `ReadOrNull`.
- `Links` (IA-D3): `AfterMove` (the writes that keep every link pointing where it did when a file or a folder moves,
  both the links to it and the links it holds), `Spoken` ("missing: name"), `Remap`.
- `TaskRules` (TK-4): Done, Start and Due, `DefaultTemplate`, `TemplateOf`, `IsTask`, `IsDone`, `Due`, `Summary`,
  `WithDone`, `WithoutDone`, `TemplateWithDone`, `DoneIndex`.
- `SmartList` (TK-5): All tasks and the `.filter.json` files: `InFolder`, `Read` (a file not understood is a smart
  list that shows nothing, with `NotUnderstood`), `Items`, `Matches`, `NewFileText`; its `order` keys are `OrderKey` (a name, or `field` with `by` options or text and `descending`; a choice by its options' order).
- `TaskSettings`: the top folder, the list open last, whether done tasks are hidden.
- `ActionKeys` and `ListAction` (docs/axKit/reports.md RP-8, TK-7a.1; 2026-11-07): a list's Ctrl+number actions under
  `"keys"` in its `list.json` (`ItemDocument.Extra`): `Read`, `With`, `Spoken` ("Ctrl+1. Make it a bug"), `ApplySet`
  (fields set, a missing one added with the value's type), `SpokenSet`, `ParseSetLines` ("Field: value" lines); an
  action is `set`, `move` or `open`, with `Described`; `OpenRefusal` keeps an `open` of axClaude or axTerminal, and Open
  in axClaude, off a folder that is not on a drive of this computer (S-5's rule, 2026-11-09). Tested in `ActionKeysTests`.
- `GitHubList` and `GitHubListSettings` (RP-7.1, TK-7a.2): a list whose `list.json` has `"github"`: `Of`, `IsRepository`,
  `NewList` (the template, the settings, the default action keys), `Template`, `ToItem` (an issue as an item, the
  reporter id and the version from the body's last line), `NumberOf`, `FileStem` ("12 title"), `Refresh` (the
  `FileChange`s that bring the folder up to the issues, Notes kept, the gone archived), `KindName`, `LabelOf`,
  `SpokenRefresh`. The issues come from `AxKit.Core.Reports.GitHubIssues`. Tested in `GitHubListTests`.

## Gotchas

- `TextDocument.Read` gives CRLF text whatever the file holds, and `ToText` writes CRLF, so the text kept for the
  freshness check compares equal after a round trip.
- `NaturalOrder` is `StrCmpLogicalW`: the tests run on Windows only, as the whole bundle does.
