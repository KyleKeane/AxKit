# axAutomate, the window

The WinForms side of axAutomate: the item apps' window (`AxKit.Forms/ItemForm`) over a folder of workflows, a viewer in its first version (SPEC.md AU-6a). The pure code is `src/AxAutomate.Core`, with its own `CLAUDE.md`. The spec is `docs/axAutomate/SPEC.md` ("SPEC.md" below); the guide `docs/axAutomate/user-guide.md` is embedded by `AxAutomate.csproj` for Help, User guide; the manual test plan is `docs/axAutomate/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axAutomate.ps1                  # build axKit and start axAutomate (run.ps1 -App automate)
.\test.ps1 AxAutomate                 # the tests in Release, as CI runs them
```

## Where things are

- `AxAutomateApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`.
- `AutomateForm`: the window on `ItemForm`: the levels (a stack of folders, then one workflow), the rows from `StepText.Rows`, the details from `StepText.Details`; every change refused with `ViewOnly`; Ctrl+O and Ctrl+J its own.
- `AutomateForm.Run.cs` (SPEC.md AU-6b): F5 and Escape; the run on a background thread of its own (the files read there) through `ScriptRunner.Play(..., hosted: true)` with this program file as the run's exe; failing steps and each end posted to the window (`Post`) and said without interrupting (`RunSentences`); the outcomes by path, which the rows and the details read.
- `AxAutomateKeys`: the window's keys (bundle AX-8), `ItemKeys.All` without F5 and Escape, which are Run and Stop the run here, and its own; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key.
2. No second window (bundle AX-7.2): questions are notices.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- `check commands` in the automation fails on a shared item key the F1 text does not name, even one this version only refuses: the refused keys have their line in the F1 text.
- A run inside the runner's own hidden desktop starts its windows on a further desktop of its own; that works (`tests/scripts/axAutomate/02-run.txt`). In a script's `file` step, write `{temp}` for the inner run's `{temp}` and never a file name starting with n after a backslash (`\n` is a new line there).
- The run's thread is a background thread: a window closed during a run starts a foreground thread that waits up to 15 s for the run, then ends the run's own processes (`Run.Abandon`), so the process never stays invisible. Every exception on the run's thread is caught: one escaping it would end the user's window.
- Whether a workflow can run is `ScriptLines.CannotRun`, which reads no file; `Prepare` (a script's lines from its file) runs on the run's thread only.
