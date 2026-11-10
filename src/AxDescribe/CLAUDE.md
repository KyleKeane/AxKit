# axDescribe, the window

The WinForms side of axDescribe: one list and the bundle's notices, on the window base. The pure code is `src/AxDescribe.Core`, with its own `CLAUDE.md`. The spec is `docs/axdescribe/SPEC.md` ("SPEC.md" below); the guide `docs/axdescribe/user-guide.md` is embedded by `AxDescribe.csproj` for Help, User guide; the manual test plan is `docs/axdescribe/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axdescribe.ps1                  # build axKit and start axDescribe (run.ps1 -App describe)
.\test.ps1 AxDescribe                 # the tests in Release, as CI runs them
```

## Where things are

- `AxDescribeApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`.
- `DescribeForm`: the window (SPEC.md DE-2) on the bundle's window base (`AxKit.Forms/AxKitForm`); its commands are `Add` and `AddKey` calls (docs/axkit/app-kit.md AK-2).
- `AxDescribeKeys`: the window's keys (bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key.
2. No second window (bundle AX-7.2): questions are notices.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- Written by `tools/new-app.ps1` on 2026-11-09; replace this line with the first gotcha found.
