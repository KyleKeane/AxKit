# axTemplate, the windows

The WinForms side of axTemplate. Today one window, the browser, on the bundle's item window; the launcher on the hotkey window is the next step (SPEC.md section 8). The pure code is `src/AxTemplate.Core`, with its own `CLAUDE.md`. The spec is `docs/axTemplate/SPEC.md` ("SPEC.md" below); the guide `docs/axTemplate/user-guide.md` is embedded by `AxTemplate.csproj` for Help, User guide; the manual test plan is `docs/axTemplate/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axTemplate.ps1'                  # build axKit and start axTemplate (run.ps1 -App template)
.\test.ps1 AxTemplate                 # the tests in Release, as CI runs them
dotnet run --project tools\AxAutomate -c Release -- tests\scripts\axTemplate --build --record   # the window checks
```

## Where things are

- `AxTemplateApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`. A plain start shows the browser (TP-1.1).
- `BrowserForm` (TP-8): the templates as one level of `AxKit.Forms/ItemForm`, the pane of a template's fields beside it. The navigator's keys, selection, filter, sort, Context menu and field forms are the item window's. This class holds the folder as it was read (`_files`, `_config`), gives the rows (`ReadLevel`) and a row's document (`DetailOf`), and writes every change as a `Step` of `AxKit.Core/Changes` through `Commit`, which Ctrl+Z (`Undo`) takes back.
- `AxTemplateKeys`: the browser's keys (bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Everything of the list and the fields comes from `ItemForm`; nothing here makes a control of its own.
2. No second window of the bundle's kind (AX-7.2): questions are notices. The browser and the launcher are each a whole window of a bundle type (TP-D8).
3. Making, renaming, changing and deleting a template are never run for an AI tool (TP-D7): each of those menu items is made with `CommandPalette.NeverForAiTools`. A new command that changes a template gets the same.
4. The log never holds a template's name or text (TP-7.3).
5. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The folder is read off the UI thread (`ReadFolderAsync`, the navigator's `BeginRead` and `EndRead`); `ReadLevel` only gives what was read. After a change of the window's own the files are not read again: `TemplateStore.After` works out what they are now from the step's parts. A change that goes around `Commit` leaves the list stale.
- Before a change the file is compared with the text it was read as (`Fresh`, item-apps IA-5.5); a change of a file that an edit in axJSON touched since is refused and the folder read again.
- A rename that changes capitals alone writes the Title and keeps the file: `Changes.CheckUndo` takes a move onto the same file, under Windows' names without case, for a file "again where it was".
- F5 is this window's own command, registered before `PlaceControls`, so that the item window's Refresh (which only fills again) is not registered.
- In a script's `file` step an item names its configuration with `"schema": "list.json"`, not `$schema`.
