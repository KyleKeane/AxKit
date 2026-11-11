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
3. Delete, Ctrl+N, F2: "This version only views the workflows; changing them comes later".
4. Ctrl+J on a workflow: axDown opens the script.

## 4. Another folder (AU-6a.1)

1. Ctrl+O: Windows' folder picker; choose another folder: its name and first row are read.
2. Alt+F4, start axAutomate with no folder: the folder chosen last opens.
