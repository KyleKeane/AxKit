# axDescribe NVDA test plan

Run with NVDA before a release, after `run-axdescribe.ps1` (or `.\run.ps1 -App describe`). Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axdescribe/SPEC.md` (DE-n.m).

## 1. Start (DE-1, DE-2)

1. Start axDescribe: NVDA reads the title ending in "- axDescribe window", then "Describe, list" and its line. Nothing else is spoken.
2. F1: the keyboard shortcuts as a notice; Escape closes it and the list has the focus again.
3. Alt+F4 closes the window without a question.
