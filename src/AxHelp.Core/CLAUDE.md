# AxHelp.Core, the pure code

Everything of axHelp that needs no window: the settings, the conversation's command line, the files axHelp writes into Claude's working folder, and what counts as a picture file. Unit tested in `tests/AxHelp.Tests`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the window is `src/AxHelp`, with its own `CLAUDE.md`. AI help's own pure code (the working folder among it) is the bundle's, `src/AxKit.Core/Ai`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxHelp
```

## Where things are

- `HelpSettings` (SPEC.md HE-2.3, HE-4, HE-5): on the bundle's `HotkeySettings` (the conversation's hotkey, Windows+Alt+H by default, Start with Windows); `ScreenshotHotkeyText` with `ScreenshotKey` (Windows+Alt+S), `Model` (`FastModel`, haiku, by default) with the choices `Models`, `ScreenshotsKept`; `ConversationArguments` (the axClaude command line: the folder, `--title axHelp`, `--message`, then `-- --continue --model`).
- `WorkingFolderFiles` (SPEC.md HE-2.4): `CLAUDE.md` and `.claude\agents\agent.md` (a subagent on opus), written into the working folder only where missing.
- `PictureFile` (SPEC.md HE-3.3; axDescribe's until 5.7.0): `Kinds`, `Filter` for the Windows file picker, `IsPicture`, `Refusal`.

## Gotchas

- A settings file of 5.6.0 holds `exchanges`; System.Text.Json leaves an unknown property alone, so it still reads.

