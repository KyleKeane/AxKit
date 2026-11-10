# axHelp NVDA test plan

The manual checks of axHelp (`docs/axHelp/SPEC.md`, HE-n.m), run with NVDA before a release. Before starting: AI help
on in any axKit window (axKit menu, AI help, Let Claude help in the apps, the consent agreed), Claude Code installed
and logged in, `run-axHelp.ps1` (or quit a running axHelp first, axHelp, Quit axHelp). The automated part is
`tests/scripts/axHelp/01-start.txt`, which `nvda-pass.ps1 axHelp` runs with a fake Claude; the steps below with
"(real)" need the real one and are the user's by ear.

## 1. Bringing it up and putting it away (HE-1, HE-2.5, HE-4)

1. In Notepad, press Windows+Alt+H: NVDA says "axHelp, access layer for Claude Code window, Ask Claude, edit" and
   nothing else. The status bar (NVDA+End) says "0 questions kept".
2. Escape: axHelp is gone and Notepad has the focus again, on the same line.
3. Windows+Alt+H while the field shows: it hides. Alt+Tab to another program: it hides.
4. Start menu, axKit axHelp, while axHelp runs: the field comes up; no second axHelp starts.
5. Options, Hotkey: "Press the key that brings axHelp up"; press Ctrl+Alt+Shift+F9, Enter: "Hotkey
   Ctrl+Alt+Shift+F9"; the new key brings it up; set it back.
6. Tab: "Earlier questions, 0, list, No questions yet: type one and press Enter"; Shift+Tab: "Ask Claude, edit".
   Type "what does" slowly: nothing is said between the letters (HE-3.3).

## 2. A question (HE-2.3, HE-2.4; real)

1. Type "what does ctrl shift w do in axdown" and press Enter: "Asking Claude" is read as a notice; a few seconds
   later "Claude says" and the answer, which names Ctrl+Shift+W and word wrap. Down reads it line by line; h moves by
   heading if the answer has any.
2. Tab: "Ask more about this answer, edit". Type "and in axjson?" and Enter: "Asking Claude", then a new "Claude
   says" answering about axJSON, in the same conversation.
3. Tab to Copy, Enter: "Copied"; the answer is on the clipboard (Ctrl+V in Notepad later).
4. Escape: the field, empty, has the focus. The status bar says "1 question kept".
5. Enter with nothing typed on the kept question (Down first): "Claude says" with the first answer again; Escape.
6. Type a question and press Enter, then Escape at once during "Asking Claude": "Cancelled"; the field has the focus.

## 3. The earlier questions (HE-3)

1. Down: "what does ctrl shift w do in axdown, 1 of 1". Up: the low note and the same again.
2. Type "ctrl": Down says the question, 1 of 1. Ctrl+A, type "zzqq", Down: the low note, "No earlier question matches
   zzqq".
3. Before forgetting: Tab into the list: "Earlier questions, 1, list, what does ctrl shift w do in axdown, 1 of 1";
   Enter: "Claude says" and the answer; Escape: the list again; Shift+Tab: the field (HE-3.4).
4. axHelp, Forget the questions: "Forget the questions?", Enter: "Questions forgotten"; Down: the low note, "No
   questions yet: type one and press Enter".

## 4. Off, missing, consent (HE-7)

1. In another axKit window, axKit menu, AI help, Let Claude help in the apps (off): "AI help off". Windows+Alt+H,
   type a question, Enter: "AI help is off: axKit menu, AI help, Let Claude help in the apps"; the status bar says so
   too. Turn it on again: the consent is remembered, "AI help on: Shift+F1 asks Claude about this window; axHelp and
   axDescribe answer from anywhere".
2. F1: the keyboard shortcuts, with the hotkey and the field's keys; Escape.
3. axHelp, Quit axHelp: the process ends (Task Manager has no `AxKit.exe help`).
