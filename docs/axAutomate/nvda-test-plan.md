# axAutomate NVDA test plan

Run with NVDA before a release, after `run-axAutomate.ps1` (or `.\run.ps1 -App automate`), on the repository's
`tests\scripts` folder. Every step says what NVDA should read; a step that reads differently is a defect. The
specification is `docs/axAutomate/SPEC.md` (AU-n.m). `tests/scripts/axAutomate/01-start.txt` runs sections 1 to 3 on
a small folder of its own.

## 1. The folder (AU-4.1, AU-4.2, AU-6a.1)

1. `axAutomate <repository>\tests\scripts`: the title "scripts - axAutomate window", then "scripts, 0 workflows, 18
   folders" and the first row, "axAutomate, folder, 1 workflow". Nothing else is spoken.
2. Down and Up move through the folders; type "axd": "axDown, folder, ...".
3. F1: the keyboard shortcuts as a notice; Escape closes it and the navigator has the focus again.

## 2. A workflow (AU-4.2, AU-D5)

1. Right on axHelp: "axHelp, 1 workflow, 0 folders, 01-start, workflow, N steps".
2. Enter: "01-start, N steps, 1. Make the file home\AxKit\axHelp.json".
3. Down through the rows: notes read "Note: ...", steps "N. Press Enter", the trust step "Expect said: trust, may not
   come, then 2 steps", then "N.1 then: Wait 1.5 seconds" and "N.2 then: Press Enter".
4. Left: back in axHelp's folder, 01-start selected. Left again: the top folder, axHelp selected.

## 3. A step's details and the refusals (AU-4.3, AU-6a.2)

1. On a step, Tab: "Step N" and its lines: Does, its values, On failure, Report, the line of the file.
2. Ctrl+Q reads every line. Escape: back to the navigator, the step selected.
3. Delete, Ctrl+N, F2: "This version views and runs the workflows; changing them comes later".
4. Ctrl+J on a workflow: axDown opens the script.

## 4. Another folder (AU-6a.1)

1. Ctrl+O: Windows' folder picker; choose another folder: its name and first row are read.
2. Alt+F4, start axAutomate with no folder: the folder chosen last opens.

## 5. Running (AU-4.4, AU-4.5, AU-5.3)

`tests/scripts/axAutomate/02-run.txt` runs steps 1 to 4 on a small folder of its own, inside the runner's own
desktop; the window's run starts a further desktop of its own there, which works (checked 2026-11-11).

1. In a folder of workflows, F5: "Running N workflows; Escape stops the run". Nothing is said while steps pass; a
   failing step as it ends, "Step 2 failed: Expect the focus: ... Not within 10 s; seen: ..."; one line at each
   workflow's end, "01-passes passed, 4 steps", "02-fails failed at step 2", "Later cannot run: ..."; then "Run
   finished: 3 of 5 workflows passed, 1 failed, 1 could not run". The keyboard stays in axAutomate: no window comes
   to the front, and arrows move through the rows during the run.
2. The rows read "..., passed", "..., failed at step 2", "..., cannot run"; Tab on the failed one: "Last run: failed
   at step 2. ..." with what the runner saw.
3. Right into the failed workflow: its step reads "2. Expect the focus: ..., failed"; Tab, Down: "Last run: failed.
   ...".
4. Inside a workflow, F5: "Running 03-waits; Escape stops it"; Escape at once: "Stopping after the current step",
   then "03-waits stopped". On a workflow that cannot run, F5: "This workflow cannot run: ...". The Operations menu:
   Run unavailable on it; Stop the run unavailable when nothing runs.
5. By ear (the script cannot): during a folder run of `tests\scripts\axDown`, work in another program: nothing of the
   run appears, takes the focus or plays a sound; Alt+F4 on axAutomate during a run closes the window, and Task
   Manager shows the run's AxKit.exe processes gone within half a minute (checked by a scratch script 2026-11-11); no
   window of a run takes the focus from you, even one that opens a page or Explorer (none is handed the foreground).
