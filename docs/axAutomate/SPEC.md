# axAutomate specification

axAutomate, workflows of steps that drive axKit's windows: a window where the user opens a workflow, hears its steps
one per line, changes them as fields, runs it and hears each step passed or failed. Its first workflows are the
automated checks of every app, the scripts of `tests/scripts` that the runner `tools/AxAutomate` plays today
(`docs/axKit/automation.md`), so that the user can open, review and change the tests themselves. Asked by the user on
2026-11-10 ("i would like axautomate workflows to be the framework that you are using for the automated testing so
that i could open and review those workflows and potentially even tweak them"; then "each step has a main operation
and some branching and reporting outputs"); this document was written the same day, before any code, for the user to
read. **Nothing of it is built.** Requirements are numbered AU-n.m and decisions AU-Dn. In axAutomate's code and
documents, "SPEC.md" means this file.

## 1. Summary

A workflow is one JSON file, `<name>.workflow.json`, in a folder of workflows; the checks of axDown are
`tests/workflows/axDown/02-palette.workflow.json` and so on. The window is the item apps' (`docs/axKit/item-apps.md`):
the navigator lists the folder's workflows, Right goes into one, and its steps are rows read as a sentence: "3. Press
Ctrl+Shift+P", "4. Expect said: Command palette", "5. Choose the menu item Close all windows". Tab goes to the detail
pane, where a step's parts are fields: what it does, its text, what happens when it fails, the message it reports.
F5 runs the workflow on a desktop of its own, so the run never takes the user's keyboard; each row then ends with
"passed", "failed: <what happened>" or "skipped", and the report opens as a notice text. Ctrl+J opens the file in
axJSON, as in axTask.

## 2. Goals and non-goals

- G1. A workflow is read and changed by ear: one row per step, a sentence each, the parts of a step as the item apps'
  typed fields; no syntax to type.
- G2. The window shows exactly what the file holds (the user's words: "the UI clean and simple and directly matches
  the data format"): one row per step in the file, one field per key of the step, the same names in both.
- G3. Branching and reporting without a programming language: a step can be allowed to fail, can hold the steps that
  run only when it passed, or only when it failed, and can say what to report when it fails (AU-D2).
- G4. One engine: the window and the command line run a workflow through the same code, so that the checks the
  assistant runs before a commit are the ones the user hears in the window.
- G5. Local first (bundle G6): no network; the windows a workflow drives are axKit's own, on the run's own desktop,
  with their own settings folder, as the runner's are today.
- Non-goals of the first version: driving programs other than axKit's windows, or axKit's windows on the user's own
  desktop (AU-D4); recording a workflow from what the user does; variables and values from the system beyond the
  placeholders of AU-3.4 (a recorded feature request, Low); loops and jumps to a named step (AU-D2).

## 3. The format

### 3.1 A workflow

```json
{
  "format": "axkit-workflow 1",
  "title": "axDown: the command palette",
  "covers": "docs/axDown/nvda-test-plan.md section 2",
  "steps": [
    { "do": "file", "path": "notes.md", "text": "# Notes\nOne line" },
    { "do": "start", "app": "down", "path": "{temp}\\notes.md" },
    { "do": "keys", "keys": "ctrl+shift+p" },
    { "do": "expect said", "text": "Command palette" },
    { "do": "expect said", "text": "trust", "on failure": "continue",
      "then": [ { "do": "wait", "ms": 1500 }, { "do": "keys", "keys": "enter" } ] },
    { "do": "menu", "name": "Close all windows",
      "on failure": "stop", "report": "The palette did not run Close all windows" }
  ]
}
```

- AU-3.1.1 `format` is `axkit-workflow 1`; a file of another format opens read-only with what was not understood
  said, as a smart list does (TK-5.2).
- AU-3.1.2 `title` is what the navigator reads; `covers` (optional) names the plan or the document section the
  workflow checks, read in the detail pane.
- AU-3.1.3 `steps` is the steps in order.

### 3.2 A step

Every step has `do`, the operation, and the keys that operation takes. Three keys any step may have:

- AU-3.2.1 `on failure`: `stop` (the default: the workflow fails here, the steps after it are skipped) or `continue`
  (the failure is reported, and the run goes on; with `then`, the failure is expected and nothing is reported: the
  script's `try` and `iffound` of today).
- AU-3.2.2 `report`: the sentence the report gives when the step fails, before what the runner saw ("The palette did
  not run Close all windows. Said: ..."). Without it, the runner's own words.
- AU-3.2.3 `then` and `otherwise`: steps that run only when this step passed, and only when it failed. They nest: a
  step inside `then` can have its own. In the window, Right on a step with them goes into them, as into a list.
- AU-3.2.4 `note`: a sentence for the reader, the plan step the step checks or why a step is missing (the script's
  `#` lines of today). A note alone, without `do`, is a row of its own that is never run.

### 3.3 The operations

The operations are the runner's commands of today (the header of `tools/AxAutomate/Program.cs`), each with named
keys in place of its words on a line:

| `do` | Keys | The row reads |
| --- | --- | --- |
| `start` | `app`, `path` (optional), `arguments` (optional) | Start axDown on notes.md |
| `launch`, `adopt`, `use` | `app`, `path`; none; `window` | Launch ..., Adopt the new window, Use window 2 |
| `keys` | `keys` | Press Ctrl+Shift+P |
| `type` | `text` | Type hello |
| `menu` | `name` | Choose the menu item Close all windows |
| `expect said`, `expect focus`, `expect state`, `expect list`, `expect text` | `text` | Expect said: Command palette |
| `refuse said` and the other refusals | `text` | Must not say: Error |
| `expect closed`, `expect quiet` | none; `ms` | Expect the window closed |
| `file`, `mkdir`, `silence` | `path`, `text`; `path`; `path`, `ms` | Make the file notes.md |
| `run` | `command` | Run git init |
| `wait` | `ms` | Wait 1.5 seconds |
| `close` | none | Close the window |
| `check commands`, `allow error` | none; `text` | Check the commands |
| the `mcp` steps | `tool`, `arguments` (JSON), `text` | Call the tool axkit_windows |

- AU-3.3.1 `try said` and `iffound` are not operations of their own: they are `expect said` with
  `"on failure": "continue"` and `then` (AU-3.2.1).
- AU-3.3.2 An operation the engine does not know makes the workflow read-only and unrunnable, with the step said.

### 3.4 Placeholders

- AU-3.4.1 `{temp}`, the run's own folder, as today; `{window}`, the id the last MCP answer named. A value from the
  system such as `{clipboard}` is a later feature (its priorities item), never a variable that steps assign.

## 4. The window

- AU-4.1 `AxKit.exe automate [<folder or workflow>]`, Start entry `axKit axAutomate`, command `axAutomate`. Without a
  path, the folder used last, or `tests\workflows` of the repository the user names once in Options.
- AU-4.2 The item apps' window (`AxKit.Forms/ItemForm`): the navigator lists the folder's workflows by title, with
  "N steps" and, after a run, "passed" or "failed at step 7"; Right or Enter goes into a workflow; its rows are the
  steps of AU-3.3, numbered, with a step's `note` after its sentence; Tab or Enter on a step goes to its fields.
- AU-4.3 A step's fields: Do (a choice of the operations), one field per key of the operation (text, a number for
  `ms`, a long text for `text`), On failure (a choice: stop, continue), Report (text), Note (long text). Changing Do
  keeps the keys the new operation shares and drops the others, with Ctrl+Z to bring them back (IA-5.4).
- AU-4.4 Keys of the window: F5 runs the workflow in hand (in the navigator's top level: every workflow of the
  folder, one after another); Escape during a run stops it after the current step; Ctrl+N a new step after the one
  selected (a new workflow at the top level); Delete removes a step, Ctrl+Z brings it back; Alt+Up and Alt+Down move a
  step; Ctrl+J the file in axJSON. The rest are the item apps' (IA-3).
- AU-4.5 During a run, each step's row is said as it ends only when it fails ("Step 7 failed: expected said Command
  palette"), and once at the end: "axDown: the command palette passed, 42 steps" or "failed at step 7". The report,
  every step with what was said and focused, opens with Ctrl+R as a notice text with the bundle's reading keys.
- AU-4.6 The window's menus follow AX-9 (Session, Operations, Options, Window, axKit, Help).

## 5. The engine and the move of the tests

- AU-5.1 The runner's code moves from `tools/AxAutomate` into `src/AxAutomate.Core` (the steps, the reading of a
  workflow, the run on the test desktop, the report) and `src/AxAutomate` (the window). `tools/AxAutomate` stays as
  the command line over the same core, for `nvda-pass.ps1`, the release checks and the assistant.
- AU-5.2 Both formats are read while the tests move: a `.txt` script and a `.workflow.json` workflow run through the
  same steps. A converter writes each script as a workflow, its `#` lines as notes; a test run of the converted
  workflows must pass exactly as the scripts did, script by script, before the scripts are deleted.
- AU-5.3 The automation channel stays as the security audit left it (S-11): on only in a run, only for the runner's
  own windows, with the run's secret. The window starts the runs; it never opens the channel of the user's own
  windows.

## 6. Decisions

- AU-D1 JSON, one file per workflow (the user's decision of 2026-11-10: JSON for anything structured that may need
  links or conditions). One file per step, as axTask's items are, would make a script of forty steps forty files;
  the window gives the per-step view the item apps give.
- AU-D2 Branching as nested `then` and `otherwise` blocks, failure as `on failure`, reporting as `report`; no jump to
  a named step and no loops. Blocks read in order and always end; a jump can make a workflow that never ends, and
  nothing the tests do today needs one. Revisited if a real workflow does.
- AU-D3 The operations keep today's names, so that the scripts, the documents and the reports keep speaking of
  `expect said` and `keys`; the window reads them as sentences.
- AU-D4 Only axKit's windows, on the run's own desktop, in the first version: the channel is the only way in that is
  safe and leaves the user's keyboard alone. Driving other programs, or the user's own windows, is a new kind of
  access, to be specified with its own safeguards if it is asked for.

## 7. Open questions for the user

- AU-Q1 The window as an item app (AU-4.2) or a simpler one: one list of steps with the step's fields below it,
  without the navigator's levels. The item app gives nesting and the fields for nothing; the simpler one is quicker to
  move through.
- AU-Q2 Where the workflows of the tests live: `tests/workflows/<app>` beside the scripts during the move, then in
  their place.
- AU-Q3 Whether the user's own workflows, beyond the tests, come in the first version (what they would drive is
  AU-D4's question).
