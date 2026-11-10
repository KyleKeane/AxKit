# AxHelp.Core, the pure code

Everything of axHelp that needs no window. Unit tested in `tests/AxHelp.Tests`. The spec is `docs/axhelp/SPEC.md` ("SPEC.md" below); the window is `src/AxHelp`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxHelp
```

## Where things are

- `HelpLines` (SPEC.md HE-2.1): the list's lines and the window's title.
- `HelpSettings` (SPEC.md HE-9): the settings file, the window base's settings and nothing else yet.

## Gotchas

- Written by `tools/new-app.ps1` on 2026-11-09; replace this line with the first gotcha found.
