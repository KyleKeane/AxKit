# axAutomate specification

axAutomate, workflows of steps that drive axKit's windows: a window where the user opens a workflow, hears its steps
one per line, changes them as fields, runs it and hears each step passed or failed. Its first workflows are the
automated checks of every app, the scripts of `tests/scripts` that the runner `tools/AxAutomate` plays today
(`docs/axKit/automation.md`), so that the user can open, review and change the tests themselves. Asked by the user on
2026-11-10 ("i would like axautomate workflows to be the framework that you are using for the automated testing so
that i could open and review those workflows and potentially even tweak them"; then "each step has a main operation
and some branching and reporting outputs"); this document was written the same day, before any code, for the user to
read. Built from 2026-11-11 in steps, at the user's word ("the first goal on this is that i can view the existing
workflows and navigate them"): the first version is a viewer (section 6a); running from the window followed the same day (section 6b), and
changing comes after it. Requirements are numbered AU-n.m and decisions AU-Dn. In axAutomate's code and
documents, "SPEC.md" means this file.

## 1. Summary

A workflow is one JSON file, `<name>.workflow.json`, in a folder of workflows; the checks of axDown are
`tests/workflows/axDown/02-palette.workflow.json` and so on. The window is the item apps' (`docs/axKit/item-apps.md`):
the navigator lists the folder's workflows, Right goes into one, and its steps are rows read as a sentence: "3. Press
Ctrl+Shift+P", "4. Expect said: Command palette", "5. Choose the menu item Close all windows". Tab goes to the detail
pane, where a step's parts are fields: what it does, its text, what happens when it fails, the message it reports.
Later, F5 runs the workflow on a desktop of its own, so the run never takes the user's keyboard; each row then ends with
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
- AU-4.2 The item apps' window (`AxKit.Forms/ItemForm`; the user's choice of 2026-11-11, "full window with all the
  features since the list name will give a simple overview"): the navigator lists the folder's folders, then its
  workflows by title, with "N steps" and, after a run, "passed" or "failed at step 7"; Right or Enter goes into a
  folder or a workflow; a workflow's rows are its steps in the order they run, each a sentence (AU-3.3) with its
  marks after it ("may not come" or "goes on if it fails", "reports", "then 2 steps", "otherwise 1 step"), a note a
  row of its own, "Note: ..."; the steps of a block follow their step, numbered under it and named by their block,
  "5.1 then: Wait 1.5 seconds" (AU-D5); Tab or Enter on a step goes to its details.
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
  the command line over the same core, for `nvda-pass.ps1`, the release checks and the assistant. Done 2026-11-11:
  `Run` (the steps), `Window` (a window of the run and its timeline), `Speech` (NVDA's capture), `Broker` (the run as
  an MCP client) and `ScriptRunner` (one workflow played to its end, the lines of the report) are in the core, moved
  unchanged but for three things a run inside a window needs: the run's secret and folder go to its windows in their
  own environment and are never set on the process that runs it (S-11: the window's own Ctrl+J must not hand them to
  the user's axDown); the secret is the run's, not the process's; and a run inside the window ends at its end only the
  processes it started and their descendants, never the window's other children. The command line keeps its
  arguments, its output and its report; it runs a `.workflow.json` file too (AU-5.2). Windows' voice for the NVDA
  tier stays in the command line, which alone has that tier.
- AU-5.2 Both formats are read while the tests move: a `.txt` script and a `.workflow.json` workflow run through the
  same steps. A converter writes each script as a workflow, its `#` lines as notes; a test run of the converted
  workflows must pass exactly as the scripts did, script by script, before the scripts are deleted.
- AU-5.3 The automation channel stays as the security audit left it (S-11): on only in a run, only for the runner's
  own windows, with the run's secret. The window starts the runs; it never opens the channel of the user's own
  windows. As built (2026-11-11): the run's windows are this very program file (`Environment.ProcessPath`), started
  on a desktop of the run's own with the run's secret and folder in their own environment block, never set on the
  window's process, so that the window's other children (Ctrl+J's axDown, Window, New window) never get them; the
  run's end ends only the processes the run started and their descendants (`Run`, `hosted`).

## 6b. Running from the window (2026-11-11)

- AU-6b.1 F5 runs the workflow in hand, read again from its file first; at a folder's level, every workflow of that
  folder, one after another, each with fresh windows and a fresh `{temp}` under `%TEMP%\axkit-automate`. The folders
  inside it are not run (AU-D9). Session, Refresh keeps its menu item, without a key (AU-D8).
- AU-6b.2 The run is on a thread of its own: the window stays usable, the arrows move and nothing is said while
  steps pass. A failing step is said as it ends, "Step 2 failed: Expect the focus: Nothing like this. Not within
  10 s; seen: ...", its own `report` in place of its sentence when it has one, what the runner saw cut at 240
  characters; each workflow's end is one line, "01-passes passed, 4 steps", "02-fails failed at step 2", "... failed
  after its steps: ..." (a window that ended by itself, an ERROR in a run's log), "... stopped", "... cannot run:
  ..."; a folder's run ends with "Run finished: 3 of 5 workflows passed, 1 failed, 1 could not run", or, stopped,
  with the workflow it stopped in and the count it never reached, "Run stopped at 02-x: 1 passed, 3 not run". None of
  them interrupts.
- AU-6b.3 After a run the workflow's row ends with "passed", "failed at step N", "stopped" or "cannot run", and the
  failed step's row with "failed"; the detail pane gives "Last run: ..." with what the runner saw, on the workflow
  and on the failed step. The outcomes are the window's, kept until it closes.
- AU-6b.4 Escape during a run stops it after the step going on ("Stopping after the current step"); outside a run,
  Escape keeps its item-app meaning. Closing the window during a run stops it the same way: the process waits up to
  15 s for the run to close its windows, then ends the processes the run started (a step that waits up to 60 s, a
  window that hangs), and ends; it never stays on without a window (checked 2026-11-11: gone within 35 s of closing
  during a 40 s wait). The run's thread never ends the process: anything a step throws fails that workflow.
- AU-6b.5 A workflow that cannot run (AU-3.3.2, AU-D7) says why on F5, "This workflow cannot run: ...", and its
  details say "Cannot run: ..."; Operations, Run is unavailable on it (in the command palette too), and Stop the run
  when nothing runs. A step whose number is not one (`wait 2s`, `use first`, a `silence` without its length, a
  `wait` of a workflow file without its milliseconds) makes the workflow unrunnable with the step said, as an unknown
  operation does; whether a workflow can run is read from what the window holds, and every file a run reads is read
  on the run's thread.
- AU-6b.7 The run's windows never get the right to take the foreground: the window that runs is the user's
  foreground app, and what a run's window hands to the shell (Explorer, a browser, Open with) lands on the user's own
  desktop. The command line's NVDA tier, on the user's desktop, keeps giving it.
- AU-6b.6 The report of AU-4.5 (Ctrl+R, every step with what was said and focused) is not built: the detail pane's
  "Last run" line holds what the runner saw at the failure.
- AU-6b.8 What a run from the window reaches (2026-11-11, at review): its axKit windows are on the run's own desktop
  with settings of their own, and its `file`, `mkdir` and `silence` steps write in the run's `{temp}` folder alone: a
  path that resolves outside it (a full path elsewhere, one from the drive's root, `..` out of it, after `{temp}` and
  `\uXXXX` are filled in) makes the workflow unrunnable before anything runs, "step 3 writes outside the run's folder:
  ..\x.md" (`ScriptLines.CannotRun` with `confined`). A `run` step's command line is not confined: it runs through
  Windows' `cmd.exe` by its full path (S-1), in the run's folder but on the user's own desktop, with the user's files
  and rights, so it is asked for first (AU-D11). The command line (`tools/AxAutomate`) is unchanged: it runs its
  scripts as they are, without the confinement and without the question.

## 6a. The first version: a viewer (2026-11-11)

- AU-6a.1 `AxKit.exe automate [<folder>]` opens a folder of workflows; Ctrl+O opens another with Windows' folder
  picker, and the folder is opened again at the next start. The repository's `tests\scripts` is the folder of the
  existing checks: its scripts are read as workflows as they are (AU-5.2), with no second copy to drift from them.
- AU-6a.2 Every change is refused with one sentence, "This version views and runs the workflows; changing them comes
  later" (until the runs of section 6b, "only views", and nothing ran). Ctrl+J opens the file as text: a script in
  axDown, a workflow file in axJSON.
- AU-6a.3 A file not understood still shows what was read, with "not all understood" in its row and what in the
  detail pane.

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

- AU-D5 A block's steps are rows right after the step that holds them, numbered under it (5.1, 5.2) and named by
  their block, not a level to go into: the arrows go through a workflow in the order it can run, and the marks on
  the holding step say what follows. Managing blocks when steps can be changed (adding a step into a block, moving one
  out) is to be specified with the editing (the user, 2026-11-11: "needs some thought about how to make that simple
  to manage").
- AU-D6 The converted workflows will live in `tests/workflows/<app>`, beside `tests/scripts` while the runner reads
  both, then in the scripts' place (AU-5.2); until the runner reads JSON the viewer reads the scripts themselves.
- AU-D7 The script interpreter is the engine (taken by the assistant on 2026-11-11, the user away, at the word "use
  your best judgement"): a `.txt` script plays its own lines exactly as the runner always read them, each tied to the
  step the reader made of it, and a `.workflow.json` file is written as the same lines (`ScriptLines`): an `expect
  said` that goes on when it fails is `try said`, its `then` steps `iffound` lines; a stopping step's `then` steps
  follow it. Playing the parsed steps directly would have needed a second interpreter of the scripts' quoting and
  escapes, which every check of the bundle depends on. What the lines cannot say yet (an `otherwise` block, any other
  step that goes on when it fails, a block in a block) makes a workflow file unrunnable with the step said, as an
  unknown operation does (AU-3.3.2); the NVDA tier's steps (`expect spoken`, `refuse spoken`, `spoken`) run only from
  the command line's `--nvda`. To reverse: a walker over `Workflow.Steps` that calls `Run.Step` per step. A
  workflow file's values go into the lines as they are, so the line's own conventions apply to them: `{temp}` is the
  run's folder and `\uXXXX` a character in any value, and in a `file` step's text a backslash followed by n is a new
  line, so a text that holds one literally (`C:\notes`) cannot be written by a workflow file in this version; the
  two formats are not yet equivalent there.
- AU-D8 F5 runs (AU-4.4), so it is not the item apps' Refresh here: Refresh stays in the Session menu without a key,
  and a run reads its workflow's file again by itself (the assistant, 2026-11-11). Escape also stops a run, before its
  detail pane meaning, only while one goes on. To reverse: a key of its own for Refresh in `AxAutomateKeys`.
- AU-D9 F5 at a folder's level runs that folder's own workflows, not those of the folders inside it (the assistant,
  2026-11-11): at `tests\scripts`, whose workflows are all in folders, a stray F5 would otherwise start an hour of
  checks. The command line keeps running a folder whole. To reverse: `WorkflowFolder` read recursively in
  `RunCommand`.
- AU-D10 The outcome of a run lives in the window until it closes, not in a file: the files are the user's workflows,
  and a run's record beside them would be a second thing to keep (the assistant, 2026-11-11).
- AU-D11 From the window, a workflow with a `run` step plays only after a notice has named its command lines: "This
  workflow runs commands on your computer: git init. Run it?" (for a folder, "These workflows run ... Run them?"),
  with Run and Cancel, Cancel the default, so that Enter and Escape both decline; Cancel says "Not run" and nothing of
  the run starts (taken by the assistant at review, 2026-11-11, for the user to confirm). The commands act on the
  user's own desktop and files, which the run's own desktop does not cover, and a workflow file may come from anyone;
  the question is asked once per F5, on the run's thread after the files are read, so it names what will really run.
  To reverse: drop `CommandsAllowed` in `AutomateForm.Run.cs`.

## 7. Later

- The user's own workflows beyond the tests, and what they may drive (AU-D4), are for later (the user, 2026-11-11).
- Changing steps (AU-4.3), with the blocks' editing (AU-D5).
- The run's report as a notice text, Ctrl+R (AU-4.5, AU-6b.6).
- Playing `otherwise` blocks and steps other than `expect said` that go on when they fail (AU-D7).
