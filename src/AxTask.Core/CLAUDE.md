# AxTask.Core, the pure code

Everything of axTask that needs no window, unit tested in `tests/AxTask.Tests` over temporary folders. The item
format is `docs/axKit/item-apps.md` (IA-n.m), shared with axPass; axTask's own rules are `docs/axTask/SPEC.md` (TK-n.m).
The window is `src/AxTask`, with its own `CLAUDE.md`.

## Commands

```
.\test.ps1 AxTask
```

## Where things are

- The item format itself is `AxKit.Core`'s, with its tests in `tests/AxKit.Tests`: the window's document in memory
  (`ItemDocument`, `ItemField`, `FieldTypes`, `Parsed`; since axPass, IA-D5) and, since 2026-11-21, the lists' files
  (`ListConfig`, `FlatItem`, `ItemView`) with the converter of the earlier shape (`Conversion/`), which the reports
  list uses too (docs/axTask/forms-framework.md sections 11 and 13; `src/AxKit.Core/notes/text-and-editors.md`).
  axTask reads and writes one shape: an item is its names and values with its link `"schema"`, read through its
  list's `list.json`.
- `ItemFolder` with `ItemFile`, `ListRead`, `FolderLevel`, `TopLevel` (IA-4, IA-D4, TK-3a, TK-D4, AF-11, AF-13): a
  list's items (`Read`; a folder inside a list, hidden and dot entries left out, what is not an item counted; the
  list's own configuration in `FolderLevel.List`, with `ConfigProblem` when it does not read, AF-11.7; `Earlier` for
  a list of the earlier format, which is not read, AF-13.6, with `IsEarlier`, `EarlierLists`, `EarlierSummary`), the
  top folder's lists and the Archive's (`ReadTop`), a list's section (`SectionOf`, the `SectionField` of its
  details; `NotesField`), `ReadItem` (the item read through the configuration its link names; with the text read,
  for `CheckFresh`), `ReadList` and `ReadConfig`, `NewItemFile` (a new item's file from the window's document),
  `StarterConfig` (the configuration a list without one gets, from what its items hold, AF-11.1), `NameOf`,
  `RefusedListName`, `AllItems` (the Archive left out), `ArchiveFolder`, `IsArchive`, `ListName`, `LinkTo` and
  `Resolve` (IA-D3). An `ItemFile` holds the file's own values (`Flat`), its `Config`, the `Document` the window
  shows (the Title, then the configuration's fields with the item's values), what is `Kept` (`KeptLine`),
  `ConfigProblem` (why it is shown as plain text and cannot be changed, AF-13.4) and `TextWith`, the file after a
  change to the document: every write of an item goes through it, so that only what changed is written. `FileStem`
  and `FreePath` (IA-4.5) are `AxKit.Core/ItemFiles` since their second use, the reports list (2026-11-07).
  A `.json` file that does not read as an item is in `FolderLevel.Unreadable` (TK-4.6, R-66), not in the count of
  files not shown; `ReadAll` gives a top folder's items and those files in one reading, `UnreadableRow` and
  `UnreadableSummary` are the two sentences.
- The changes on disk and their undo (`Changes`, `Step`, `FileChange`, IA-5.4) are `AxKit.Core`'s since 2026-11-23, on their second use, axTemplate's browser; this project and the window use them from there.
- `ListOrder` (SPEC.md TK-4.9, TK-4.10, 2026-11-24): a list's own order, the configuration's `order` (the items'
  file names without ".json") and `new items` (`top` or `bottom`): `Of`, `NewAtTop`, `Arrange` (named items in the
  order, the others by name after, or before for top), `Moved` and `MovedAmong` (a move among the items shown, hidden
  done tasks kept in place), `Renamed`, `Added`, `WithOrder`, `WithNewItems`, `Said`. `ItemFolder.Read` arranges by
  it. Tested in `ListOrderTests`.
- `Links` (IA-D3): `AfterMove` (the writes that keep every link pointing where it did when a file or a folder moves,
  both the links to it and the links it holds), `Spoken` ("missing: name"), `Remap`.
- The item's form rules, `ItemFormRules`, moved to `AxKit.Core` on 2026-11-23 (its second use, axTemplate's form; `src/AxKit.Core/notes/text-and-editors.md`).
- The actions of a list, pure (docs/axTask/forms-framework.md sections 3 to 7; 2026-11-21; the window's side is
  `TaskForm.Actions.cs`). `ActionProgram.Find`: the program a `run` action names, found on the search path or by a
  full path, never in the list's own folder, never off this computer, a program file only.
  `ListActions` with `ConfiguredAction` and `ActionField`: `Read` gives the entries of a configuration's `"actions"`
  in the file's order, each with its kind (`set`, `move`, `open`, `run`, `terminal`), its key of the eighteen
  (`ParseKey`, `KeyText`; a second action of a key gets `KeyProblem` and still runs from the menu), its form's fields,
  and `NotUnderstood`, why it is never run (a kind not known, a member of the wrong type, a program or a folder with
  a placeholder, a hidden field named outside the input); `Described` is the one sentence of what it does, `Label`
  the menu's words, `MarkText` what an acceptance is sealed over (the list's folder and the whole action as the file
  holds it). `ActionValues` with `ActionContext`, `FilledRun` and `FilledTerminal`: the names in braces (`Names`,
  `IsFixed`), `Fill`, and `Run`, `Terminal` and `Set`, which fill an action for an item. A value fills the one text it
  stands in and that text stays one argument; nothing here joins or splits a command line; a line break in an
  argument refuses the action. The terminal's command line is quoted by `AxKit.Core/ShellQuote`. Tested in
  `ListActionsTests`.
- `ManyItems` (TK-4.7, bundle AX-8.16, 2026-11-22): a command on several selected items, pure: `DoneNow` (Ctrl+Enter
  on several tasks marks them done while any is not, not done only when every one is), `DoneSaid` ("Done: 2 tasks. 1
  was done already. 1 selected item is not a task."), `Items` ("3 items"). The marks themselves and their words are
  the bundle's, `AxKit.Core/Marks`. Tested in `ManyItemsTests`.
- `ManyFields` (TK-4.7a, 2026-11-24): Set fields and Clear fields on several items: `Controls` (the list's fields but
  the Title, each starting as "leave": an empty box, `Leave` before a choice's options, a check as Leave, Checked,
  Not checked), `Set` (what was filled in, by field title, parsed as the item's form parses it; the first refusal
  with its control), `Clearable` and `Cleared` (a check unchecked, anything else an empty text). Tested in
  `ManyFieldsTests`.
- `TaskRules` (TK-4): Done, Start and Due, `DefaultTemplate`, `TemplateOf` (the fields of a list's configuration),
  `IsTask`, `IsDone`, `Due`, `Summary`, `TemplateWithDone` (a checklist or a plain list is the configuration's
  alone, AF-13.3: no item's file is touched), `DoneIndex`.
- `SmartList` (TK-5): All tasks and the `.filter.json` files: `InFolder`, `Read` (a file not understood is a smart
  list that shows nothing, with `NotUnderstood`), `Items` (of one top folder, or of `GatheredItem`s from several, each
  matched against its own top, TK-5.6), `Count`, `Matches`, `NewFileText`; its `order` keys are `OrderKey` (a name, or `field` with `by` options or text and `descending`; a choice by its options' order).
  The built-in lists have no `Path` (`BuiltIn`, `Key` "smart:" and the name, `Described` for the detail pane) and a
  rule of their own after the conditions: `AllTasks`, `DateLists` (`Overdue`, `DueToday`, `DueInSevenDays`,
  `NoDueDate`, TK-5.4), `BuiltIns`, and `Tagged(token)` for a tag (TK-5.5); `Grouped` (the user's smart lists by a shared first word, TK-5.2a). Tested in `SmartListTests`.
- `Tags` (TK-5.5, TK-D18, 2026-11-11): the tags of a text, any symbol after white space before a letter, the prose and
  Markdown symbols `Excluded` (`IsSymbol`, `In`, `Has`), an item's words (`TextOf`: Title and text fields), the tags of
  the tasks not done by symbol, @ and # first (`InTasks`), and a symbol's spoken name from a small table (`Spoken`, "@
  at sign", the code point otherwise). Tested in `SmartListTests`.
- `TaskSettings`: the top folder, the list open last, whether done tasks are hidden, the project folders (`ProjectFolders`, `RefusedProjectFolder`, `AddProjectFolder`; full paths once, TK-7b.1), and the action keys this install wrote or the user accepted (`AcceptedActionKeys`, TK-7a.3, bundle repairs R-72: each a seal of `ActionKeys.MarkText` under the key of the bundle's `SettingsSeal`, so a line written into the file accepts nothing; `ActionKeyMark`, `AcceptsActionKey`, `AcceptActionKey`, `MaxAcceptedActionKeys`).
- `Projects` with `TaskProject` (its `Root`, the project folder it was found under, TK-7b.3), `GatheredItem` (an item with its top and its place's name; `Where`, "Home" or "alpha / Home", TK-5.6) and `SearchHit` (TK-7b, 2026-11-11): `Find` (the folders of lists under the project folders, four folders down, hidden folders and `.git`, `node_modules`, `bin`, `obj` skipped, the top folder left out, names told apart by the folder above), `Gather` (every item of each place, the Archive left out), `GatherOne` (one file read again as a project's item, or null), `Inside`, `Words`, `Matches` (every word in the Title or a text field, case not minded) and `Search` (over places, or over items already gathered). Read-only; the window calls them off the UI thread. Tested in `ProjectsTests`.
- `TaskTools` with `ReachableList` (TK-8, docs/axKit/mcp.md MC-8, 2026-11-11): the AI tools' pure half: `Lists` (the top folder's lists by name, then each project's as "project / list"), `Find` (a list by its name, its own name when unique, or its full path when reached; a path elsewhere refused), `ItemOf` (an item as JSON: the fields its list's configuration names, a password or user name field "protected"), `NewItem` (`ItemDocument.NewItem` with the list's fields, then each typed value through `FieldTypes.Parse`; a field the list does not have is refused with the fields it has, since its value would be kept and shown to no one, AF-11.3; link, password, user name refused). Tested in `TaskToolsTests`.
- `ActionKeys` and `ListAction` (docs/axKit/reports.md RP-8, TK-7a; 2026-11-07, cut down on 2026-11-21 when a list's
  actions became `ListActions`): what the actions share with the action keys they grew from: `ApplySet` (fields set,
  a missing one added with the value's type) and `SpokenSet` ("Kind: Bug, Done: done"); `OpenRefusal`, which keeps an
  `open` of axClaude or axTerminal off a folder that is not on a drive of this computer (S-5's rule, 2026-11-09); and
  `MarkText` over a `ListAction`, the mark of an acceptance of a Ctrl+digit key as it was sealed before 2026-11-21
  (the list's folder, the digit and the action as the earlier file held it, `ListAction.ToJson`), which the window
  still honours, so that a key accepted before a list was converted is not asked about again. `TaskSettings` keeps
  the acceptances of both kinds in `AcceptedActionKeys` (`AcceptsAction`, `AcceptAction` for a whole action,
  AF-7.2). Tested in `ActionKeysTests`. The GitHub lists that came with the action keys (RP-7) were removed on
  2026-11-14, when axTask became local only: a `"github"` member in a `list.json` is kept as any member axTask does
  not know and means nothing (TK-7a.2).

- `Approvals` with `ApprovalEntry`, `ApprovalPublicKey` and `ApprovalException`, and `OutsideReports`
  (docs/axKit/outside-reports.md sections 5 and 6, 2026-11-21): the maintainer's signed approval of an outside report.
  `Approvals.Text` is the approval text of OR-5.3 (each field a line, the review as its SHA-256), `MakeKey` the key
  file (RSA 3072, the private half under AES-GCM with a key from the passphrase by PBKDF2, the public half as
  associated data), `Sign`, `Verify` (never throws), `ChangeText` for a change of key, `PinnedText` and `ReadPinned`
  for `docs/axKit/approval-key.pub`. `OutsideReports` is the list's side: `IsList`, `KeyFileOf`, `Field`, `EntryOf`,
  `Refusal` (with `Unheard`: an entry holding a character a screen reader does not speak is not signed, OR-5.6),
  `NoticeText`, `Signed`. All of them read the entry's own file (`FlatItem`), never the item as the list's
  configuration shows it: what an approval signs must not depend on which fields a configuration names.
  `tools/approvals.ps1` makes the same text and verifies in Windows PowerShell:
  a change to `Text` is a change to that script too, and `ApprovalScriptTests` fails when the two part. Tested in
  `ApprovalsTests` and `ApprovalScriptTests`.

## Gotchas

- `TextDocument.Read` gives CRLF text whatever the file holds, and `ToText` writes CRLF, so the text kept for the
  freshness check compares equal after a round trip.
- `NaturalOrder` is `StrCmpLogicalW`: the tests run on Windows only, as the whole bundle does.
