# AxAutomate.Core, the pure code

Everything of axAutomate that needs no window. Unit tested in `tests/AxAutomate.Tests`. The spec is `docs/axAutomate/SPEC.md` ("SPEC.md" below); the window is `src/AxAutomate`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`. The runner that plays the scripts today is `tools/AxAutomate` (project `AxAutomate.Runner`, program `AxAutomate.exe`); its code moves here when the window learns to run (AU-5.1).

## Commands

```
.\test.ps1 AxAutomate
```

## Where things are

- `Workflow`, `WorkflowStep`, `StepValue` (AU-3): a workflow and its steps as records; a note alone is a step with an empty `Do`.
- `ScriptReader` (AU-5.2): a runner's `.txt` script read as a workflow: the header comments are `Covers`, later comment runs are notes, `try said` is `expect said` with `on failure` continue, the `iffound` lines its `Then`. A test reads every script of `tests/scripts` without a problem.
- `WorkflowFile` (AU-3.1): a `.workflow.json` file; every key of a step other than do, on failure, report, note, then and otherwise is a value.
- `StepText` (AU-4.2, AU-D5): a step's sentence, its marks, the rows in run order with a block's steps after their step ("5.1 then: ..."), a step's detail lines.
- `WorkflowFolder` (AU-4.1): a folder's folders then its workflows, natural order, hidden and dot entries left out.
- `AutomateSettings`: the window base's settings and the folder opened last.

## Gotchas

- A new step word of the runner needs its value key in `ScriptReader.Values` and its sentence in `StepText.Sentence`; without them it still reads, as its words.
