# axTemplate, the windows

The WinForms side of axTemplate: two windows, each a start of its own and each on a window type of the bundle (SPEC.md TP-D8): the browser on the item window, the launcher on the hotkey window. The pure code is `src/AxTemplate.Core`, with its own `CLAUDE.md`. The spec is `docs/axTemplate/SPEC.md` ("SPEC.md" below); the guide `docs/axTemplate/user-guide.md` is embedded by `AxTemplate.csproj` for Help, User guide; the manual test plan is `docs/axTemplate/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axTemplate.ps1'                  # build axKit and start axTemplate (run.ps1 -App template)
.\test.ps1 AxTemplate                 # the tests in Release, as CI runs them
dotnet run --project tools\AxAutomate -c Release -- tests\scripts\axTemplate --build --record   # the window checks
```

## Where things are

- `AxTemplateApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`. A plain start shows the browser and starts the launcher hidden when none runs (TP-1.1); `--launcher` shows the launcher, `--hidden` runs it hidden, one copy at a time (`AppHost.OneCopy`, as axClip's).
- `LauncherForm` (TP-1.2, TP-3, TP-4): the field and the matches of `AxKit.Forms/HotkeyForm`, as axClip's window. It reads the templates off the UI thread each time it shows, takes the clipboard's text at that moment for `{clipboard}`, finds through `TemplateMatch`, and on Enter copies the filled text, hides, and sends the paste key through `AxKit.Forms/KeySender` once the window it came from is in front again. Its keys are `AxTemplateKeys.Launcher`, its F1 text `HelpText.LauncherShortcuts`.
- `BrowserForm` (TP-8): the templates as one level of `AxKit.Forms/ItemForm`, the pane of a template's fields beside it. The navigator's keys, selection, filter, sort, Context menu and field forms are the item window's. This class holds the folder as it was read (`_files`, `_config`), gives the rows (`ReadLevel`) and a row's document (`DetailOf`), and writes every change as a `Step` of `AxKit.Core/Changes` through `Commit`, which Ctrl+Z (`Undo`) takes back.
- `AxTemplateKeys`: the keys of both windows (bundle AX-8), the one place a key is changed: `All` is the browser's table, `Launcher` the launcher's; each feeds the collision test, and each window's F1 text names its own.
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
- The launcher's window is found by its title, the app's name alone (`HotkeyForm.FindRunning`); the browser's title ends in the name and is never taken for it. Both windows share one settings file; a save writes only what that window changed (`SettingsFile`'s merge), so the launcher's hotkey and the browser's place do not overwrite each other.
- A script of the browser gives the settings a hotkey nobody uses, since the browser starts the launcher, and a system-wide hotkey is shared with the user's own session. The script runner ends that launcher with the browser's process tree.
- `docs/axKit/components.json` names the launcher as the app's second window (`second window`, `second type`); `ComponentsTests` fails on a window class the file does not name.
- The installer and `run.ps1` tell the launcher from the browser by its arguments (`template --hidden` or `--launcher`): only the launcher is stopped for an installation or a build.
- The hotkey is on for everyone after an installation and an update (TP-1.4, the maintainer's word of 2026-11-23). The copy from GitHub: `install.ps1` starts the launcher with the other background apps, and its first run writes the Run value (`HotkeyForm`, `SignInStart`). The Store's copy has no startup task for it (no entry): `Dispatch`'s `InBackground = true` has `AxKit.Core/StoreFirstStart.ToStart` start it with the first start after an install or an update and with the sign-in's startup tasks, unless its own Start with Windows is off (bundle AX-13.4b). None of this runs in a test run.
- Enter in the launcher pastes into another window, which the hidden desktop cannot show: `tests/nvda-scripts/axTemplate-put-in.txt` checks it in the NVDA pass.
