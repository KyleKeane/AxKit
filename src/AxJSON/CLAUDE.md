# axJSON, the app

The window side of axJSON: an `EditorApp` record for the bundle's text editor shell (`src/AxKit.Forms/TextEditorForm`), the key table and the texts. The pure code (the JSON outline and the formatter) is `src/AxJSON.Core`, with its own `CLAUDE.md`. The spec is `docs/axJSON/SPEC.md` ("SPEC.md" below); the guide `docs/axJSON/user-guide.md` is embedded by `AxJSON.csproj`; the manual test plan is `docs/axJSON/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axJSON.ps1' C:\path\to\settings.json   # build AxKit and start axJSON on the file (run.ps1 -App json; close a running AxKit first)
& '.\scripts to run-individual-apps\run-axJSON.ps1'                            # axJSON with an empty document
dotnet build AxKit.sln
dotnet test tests/AxJSON.Tests/AxJSON.Tests.csproj
```

## Where things are

- `AxJSONApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms.EditorHost.Run` with `App`, axJSON's `EditorApp` record: name, verb, settings file (`axJSON.json`), file filter (`.json`, `.jsonc`), F1 text, guide, the JSON key pairs, the JSON resources pages, and the Format command (`AxJSON.Core.JsonFormat.Apply` as an `EditorCommand`).
- `AxJSONKeys`: the window's keys (SPEC.md AJ-2.2, AJ-2.3): the shell's through `EditorKeys.All`, then Ctrl+K key, Ctrl+J object, Ctrl+L array, Ctrl+I item, Ctrl+E error (Ctrl+Shift for the previous one), Ctrl+Shift+F format; `Jumps` pairs each kind with `JsonOutline`'s next and previous functions, for the editor and every notice text.
- The options (SPEC.md AJ-5, 2026-11-21): `App.Offers` holds four `EditorOffer`s on Alt with an arrow (the maintainer's keys, AJ-5.1a): Choose a value (`AxJSONKeys.ChooseValue`, Alt+Right), Next value and Previous value (`NextValue`, `PreviousValue`, Alt+Down and Alt+Up, the value changed in line), Add a member (`AxJSONKeys.AddMember`, Alt+Left), each `AxJSON.Core.JsonOffers` as the shell shows it (`Shown`, `Stepped`); `ReadLinked` reads the file an item's `"schema"` names, beside the document, on a drive of this computer only and at most 1 MB. The list, the edit as one undo step and the words are the shell's (`TextEditorForm.RunOffer`).
- `HelpText`: the F1 text and the embedded guide; About is the bundle's (`AxKit.Core.BundleAbout`).
- The window, the menus, the files, find, go to line, the notices and the update flow are the shell's: see `src/AxKit.Forms/CLAUDE.md` and `src/AxDown/CLAUDE.md`; axJSON adds nothing to them but data.

Structured mode (SPEC.md AJ-3, a form of real fields from templates) is not built; raw mode is version one (AJ-D3).
