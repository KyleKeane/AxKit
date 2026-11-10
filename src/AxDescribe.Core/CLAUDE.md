# AxDescribe.Core, the pure code

Everything of axDescribe that needs no window. Unit tested in `tests/AxDescribe.Tests`. The spec is `docs/axdescribe/SPEC.md` ("SPEC.md" below); the window is `src/AxDescribe`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxDescribe
```

## Where things are

- `DescribeLines` (SPEC.md DE-2.1): the list's lines and the window's title.
- `DescribeSettings` (SPEC.md DE-9): the settings file, the window base's settings and nothing else yet.

## Gotchas

- Written by `tools/new-app.ps1` on 2026-11-09; replace this line with the first gotcha found.
