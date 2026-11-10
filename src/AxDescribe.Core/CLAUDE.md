# AxDescribe.Core, the pure code

Everything of axDescribe that needs no window: the descriptions kept and the matches of the list. Unit tested in `tests/AxDescribe.Tests`. The spec is `docs/axDescribe/SPEC.md` ("SPEC.md" below); the window is `src/AxDescribe`, with its own `CLAUDE.md`. AI help's own pure code is the bundle's, `src/AxKit.Core/Ai`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxDescribe
```

## Where things are

- `DescribeSettings` with `Description` (SPEC.md DE-3.1, DE-5): on the bundle's `HotkeySettings` (the hotkey, Windows+Alt+S by default); `Descriptions`, the last ten, newest first, each with its `Picture` path for a described file (DE-2.7) and null for a window's; `Remember`.
- `PictureFile` (SPEC.md DE-2.7): `Kinds` (the extensions Claude Code reads as pictures), `Filter` for the Windows file picker, `IsPicture`, `Refusal` (why a path cannot be described, or null), `Program` ("picture file", the program a file's description is kept under).
- `DescribeLines` (SPEC.md DE-3.2, DE-3.3): `Label` (the time and the window's title, or its program), `Matches` through the bundle's `TextMatch`, `Spoken`, the list's one line for nothing kept and for no match.

## Gotchas

- A `Description` is a positional record filled by System.Text.Json through its constructor; its `Title` may be empty (a window without one) but never null after `Sanitize`; `Picture` has a default of null, so a file written before 5.6.0 reads without it.
