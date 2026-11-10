# axHelp NVDA test plan

Run with NVDA before a release, after `run-axhelp.ps1` (or `.\run.ps1 -App help`). Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axhelp/SPEC.md` (HE-n.m).

## 1. Start (HE-1, HE-2)

1. Start axHelp: NVDA reads the title ending in "- axHelp window", then "Help, list" and its line. Nothing else is spoken.
2. F1: the keyboard shortcuts as a notice; Escape closes it and the list has the focus again.
3. Alt+F4 closes the window without a question.
