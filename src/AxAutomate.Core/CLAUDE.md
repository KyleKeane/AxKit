# AxAutomate.Core, the pure code and the engine

Everything of axAutomate that needs no window: the workflow format, and the runner's engine, which plays a workflow on a desktop of its own (SPEC.md AU-5.1). Unit tested in `tests/AxAutomate.Tests`. The spec is `docs/axAutomate/SPEC.md` ("SPEC.md" below); the window is `src/AxAutomate`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`. The command line over the engine is `tools/AxAutomate` (project `AxAutomate.Runner`, program `AxAutomate.exe`), which `nvda-pass.ps1` and the assistant run; its header lists every step word.

## Commands

```
.\test.ps1 AxAutomate
dotnet build tools\AxAutomate\AxAutomate.Runner.csproj -c Release -o <scratch>   # the command line, then <scratch>\AxAutomate.exe <script> --exe <AxKit.exe>
```

## Where things are

- `Workflow`, `WorkflowStep`, `StepValue` (AU-3): a workflow and its steps as records; a note alone is a step with an empty `Do`.
- `ScriptReader` (AU-5.2): a runner's `.txt` script read as a workflow: the header comments are `Covers`, later comment runs are notes, `try said` is `expect said` with `on failure` continue, the `iffound` lines its `Then`. A test reads every script of `tests/scripts` without a problem.
- `WorkflowFile` (AU-3.1): a `.workflow.json` file; every key of a step other than do, on failure, report, note, then and otherwise is a value.
- `StepText` (AU-4.2, AU-D5): a step's sentence, its marks, the rows in run order with a block's steps after their step ("5.1 then: ..."), a step's detail lines.
- `WorkflowFolder` (AU-4.1): a folder's folders then its workflows, natural order, hidden and dot entries left out.
- `AutomateSettings`: the window base's settings and the folder opened last.
- The engine (AU-5.1, AU-D7), moved from `tools/AxAutomate`:
  - `ScriptLines`: a workflow as the lines the engine plays: a script's own lines, or a workflow file's steps written as lines; why a workflow cannot run (`Prepare`), and the operations the engine knows (`Known`).
  - `ScriptRunner.Play`: one workflow's lines played until one fails, every window closed, the report's log; a callback per line, and a stop asked between lines (Escape in the window).
  - `Run`: one step (`Step`), the expectations, the end (`Finish`) and what it ends (`Sweep`); `hosted` for a run inside axAutomate's window.
  - `Window`: one AxKit.exe of the run on its desktop (`Create`, with the run's variables in its own environment), its pipe and its timeline; `Event` and `Taken` in `RunEvent.cs`.
  - `RunSentences` (AU-6b): what the window says of a run: a failing step, a workflow's end, a folder's end, and the outcome its rows carry.
  - `Speech`: NVDA's capture log (the NVDA tier); `Broker`: the run as an MCP client; `Native`: the P/Invoke the engine needs.

## Gotchas

- A new step word of the runner needs its value key in `ScriptReader.Values`, its sentence in `StepText.Sentence` and its name in `ScriptLines.Known`; without the last the window will not run a workflow that uses it.
- The run's secret and folder are never set on this process's environment (`Window.Create` hands them in the child's own block): the window that runs a workflow also starts the user's own windows, which must never get them (S-11).
- Every process start and pipe here carries its `Outside call, ...` comment (`tools/outside-calls.ps1`).
