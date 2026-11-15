# axHelp NVDA test plan

The manual checks of axHelp (`docs/axHelp/SPEC.md`, HE-n.m), run with NVDA before a release. Before starting: Claude
Code installed and logged in, axClaude's agreement given, `run-individual-apps\run-axHelp.ps1` (or quit a running axHelp first, axHelp,
Quit axHelp). The automated part is `tests/scripts/axHelp`, which `nvda-pass.ps1 axHelp` runs; the steps marked "(real)"
need a real reply and a real screen and are the user's by ear.

## 1. axHelp's window (HE-1, HE-2.2)

1. Start menu, axKit axHelp: NVDA says "axHelp hotkeys window", then "axHelp, edit, read only" and the first line
   "axHelp: your conversation with Claude, from any program." Down reads the hotkeys as set.
2. F1: the keyboard shortcuts, with the two hotkeys and Enter, Ctrl+D, Ctrl+O; Escape. Escape again: the window hides.
3. Alt: the menu bar, axHelp, Options, Window, Claude, Help; Options, Conversation model: Fast (Haiku) checked.

## 2. The conversation (HE-2.1, HE-2.3, HE-2.4)

1. In Notepad, press Ctrl+Shift+Slash: "axHelp window", then "Message to Claude, edit". The first time, axClaude's reminder
   and Claude Code's trust question come first; answer them. `Documents\axKit Claude` now has CLAUDE.md and
   `.claude\agents\agent.md`.
2. Type "what is the capital of France", Enter: the reply, spoken by axClaude's Speak replies setting (real). Ctrl+3:
   the reply. The status bar names the folder.
3. Alt+Tab to Notepad, Ctrl+Shift+Slash: the same window, the reply still there; no second window starts (Window menu,
   Open windows lists one "axHelp").
4. In the conversation, "/model": the model is Haiku (real). Options, Conversation model in axHelp's window, Balanced:
   "sonnet, from the conversation's next start ...". End the conversation (its Session, End Claude and close this
   window), Ctrl+Shift+Slash: the conversation continues on Sonnet (real). Set Fast again.
5. "Make a folder named Test on my desktop": Claude hands it to its agent or asks before creating it (real); answer.
6. The guides (HE-2.6). Actions, Claude's working folder in axHelp's window: axFile on the folder, whose list holds
   `documentation` and `screenshots` beside CLAUDE.md; Right into `documentation`: index.md, axKit.md and one file
   per app (axFile.md, axGIT.md), and axkit-version.txt. In the conversation, "in axFile, how do I select several
   files?": Claude reads the guide (a tool line names documentation and axFile.md) and answers Space (real). "what is
   axKit's phone number?": Claude says the guides do not cover it (real). Add a file `mine.md` to `documentation`,
   change a word in `axFile.md`, delete `axkit-version.txt`, press Ctrl+Shift+Slash: `axFile.md` is as axKit wrote
   it, `mine.md` is still there.

## 3. Screenshots and pictures (HE-3)

1. In Notepad with a line typed, Ctrl+Shift+Comma: the conversation comes up, its field holding "Screenshot of "Untitled -
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

1. Session, Quit axHelp: the process ends (Task Manager has no `AxKit.exe help`), and the conversation ends with it,
   on view or hidden (HE-1.3, section 6).

## 5. The keys taken over (HE-4.2, 2026-11-14)

1. With axHelp ended, set `"hotkey": "Windows+Alt+H"` and `"screenshotHotkey": "Windows+Alt+S"` in
   `%APPDATA%\AxKit\axHelp.json`, as 5.8.0 left them. Start axHelp: its text names Ctrl+Shift+Slash and
   Ctrl+Shift+Comma; Windows+Alt+H does nothing; Ctrl+Shift+Slash, the question mark's key, brings the conversation up.
2. Options, Hotkey, press Ctrl+Alt+J, Enter: "Hotkey Ctrl+Alt+J". Quit axHelp and start it again: Ctrl+Alt+J still
   brings the conversation up, and Ctrl+Shift+Slash does not. Set it back: Options, Hotkey, Ctrl+Shift+Slash, Enter.
3. In an axKit window (axDown, axMedia) with axHelp running, Ctrl+Shift+Period: nothing of axKit's happens.
4. In Word with axHelp running: Ctrl+Shift+Comma takes the screenshot, not the smaller font (expected, HE-D5).

## 6. The conversation hides and comes back (HE-1.3, HE-2.1, HE-2.7, 2026-11-15)

The automated part is `tests/scripts/axHelp/02-hide-and-show.txt` and `tests/scripts/axClaude/20-hide-on-close.txt`;
what the foreground and NVDA do on the real desktop is by ear.

1. With the conversation open and a few words typed in its message field, Alt+F4: no question, nothing said, the
   window gone and the program you were in before in front, its keys working. Alt+Tab does not list axHelp; Window,
   Open windows in another axKit window does not either.
2. Ctrl+Shift+Slash: NVDA says "axHelp" and "Message to Claude, edit" with your words, at once; nothing of the
   conversation is read again, no "Ready", no chime.
3. Ctrl+2 into the conversation, Up a few lines. Alt+Tab away and back: the caret is on the same line.
   Ctrl+Shift+Slash from another program: the focus is in the message field.
4. Send "count slowly to five", Alt+F4 at once: nothing is spoken, no tick or chime while it is hidden.
   Ctrl+Shift+Slash a little later: "Done" is said once, the reply is in the conversation (h, Ctrl+3).
5. With the conversation hidden, Ctrl+Shift+Comma in Notepad: the conversation comes up with the screenshot's line.
6. In the conversation, Session, End Claude and close this window: the window closes (Task Manager: its `AxKit.exe
   claude` and Claude Code are gone). Ctrl+Shift+Slash starts it afresh, the conversation continued.
7. With the conversation hidden, axHelp's window, Session, Quit axHelp: both processes end.
8. Ready before the first press (real, after `run.ps1` or a sign-in, axClaude's agreement given). As installed, the
   setting is off: after a start in the background Task Manager shows no `AxKit.exe claude` until the hotkey is
   pressed. Options, Keep the conversation ready in the background: checked, "On: from axHelp's next start in the
   background ...". Quit axHelp and start it hidden again: without pressing anything, Task Manager shows `AxKit.exe
   claude ... --hide-on-close --hidden`; nothing was said and the program in front kept the keyboard.
   Ctrl+Shift+Slash: the conversation at once, "Ready" not said again. Turn the setting off again.
9. With the setting on, in a folder Claude Code has not been trusted for (`workingFolder` in `ai-help.json` set to a
   new folder), start axHelp hidden: nothing is said. Ctrl+Shift+Slash: the trust question is asked now, with its chime and its list.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. Alt, then Right along the bar: Session, Actions, Navigation, Context, Options, Window, axKit, Help; Alt, A: Open the conversation first.
