# axHelp NVDA test plan

The manual checks of axHelp (`docs/axHelp/SPEC.md`, HE-n.m), run with NVDA before a release. Before starting: Claude
Code installed and logged in, axClaude's agreement given, `run-axHelp.ps1` (or quit a running axHelp first, axHelp,
Quit axHelp). The automated part is `tests/scripts/axHelp`, which `nvda-pass.ps1 axHelp` runs; the steps marked "(real)"
need a real reply and a real screen and are the user's by ear.

## 1. axHelp's window (HE-1, HE-2.2)

1. Start menu, axKit axHelp: NVDA says "axHelp hotkeys window", then "axHelp, edit, read only" and the first line
   "axHelp: your conversation with Claude, from any program." Down reads the hotkeys as set.
2. F1: the keyboard shortcuts, with the two hotkeys and Enter, Ctrl+D, Ctrl+O; Escape. Escape again: the window hides.
3. Alt: the menu bar, axHelp, Options, Window, Claude, Help; Options, Conversation model: Fast (Haiku) checked.

## 2. The conversation (HE-2.1, HE-2.3, HE-2.4)

1. In Notepad, press Windows+Alt+H: "axHelp window", then "Message to Claude, edit". The first time, axClaude's reminder
   and Claude Code's trust question come first; answer them. `Documents\axKit Claude` now has CLAUDE.md and
   `.claude\agents\agent.md`.
2. Type "what is the capital of France", Enter: the reply, spoken by axClaude's Speak replies setting (real). Ctrl+3:
   the reply. The status bar names the folder.
3. Alt+Tab to Notepad, Windows+Alt+H: the same window, the reply still there; no second window starts (Window menu,
   Open windows lists one "axHelp").
4. In the conversation, "/model": the model is Haiku (real). Options, Conversation model in axHelp's window, Balanced:
   "sonnet, from the conversation's next start". Close the conversation (Alt+F4), Windows+Alt+H: the conversation
   continues on Sonnet (real). Set Fast again.
5. "Make a folder named Test on my desktop": Claude hands it to its agent or asks before creating it (real); answer.

## 3. Screenshots and pictures (HE-3)

1. In Notepad with a line typed, Windows+Alt+S: the conversation comes up, its field holding "Screenshot of "Untitled -
   Notepad": <path> " with the caret at the end; Ctrl+V in Notepad later pastes the same path.
2. Type "what does it say?", Enter: the reply reads the line (real).
3. In axHelp's window, Ctrl+D: the Windows file picker, "Choose a picture to put into the conversation"; type the path of
   `tests\fixtures\pictures\dialog.png`, Enter: the conversation's field holds "Picture dialog.png: <path> "; type "which
   button is the default?", Enter (real: Yes).
4. In a terminal, `axHelp <repository>\tests\fixtures\pictures\chart.png`: the conversation's field gets the chart's
   path. `axHelp <repository>\README.md`: a message box "README.md is not a picture axHelp can pass to Claude".
5. axHelp's window, Ctrl+O: axPreview opens on the newest screenshot and reads its text; Alt+F4.
6. `axDescribe` in a terminal: axHelp's window comes up (the former verb, HE-1.4).

## 4. Quit

1. Session, Quit axHelp: the process ends (Task Manager has no `AxKit.exe help`); the conversation window stays open, an
   axClaude window of its own.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. Alt, then Right along the bar: Session, Actions, Navigation, Context, Options, Window, axKit, Help; Alt, A: Open the conversation first.
