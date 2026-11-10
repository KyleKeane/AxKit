# axHelp, access layer for Claude Code: specification

Status: built 2026-11-09 on the branch `ai-help`, from `docs/axkit/ai-help.md` (AH-n.m, the service it is an app of)
and the user's decisions of that evening (ai-help.md section 9). Requirements are numbered HE-n.m. The bundle's
rules are `docs/axkit/SPEC.md` (AX-7 accessibility, AX-8 keys, AX-11 access layers); "SPEC.md" in axHelp's code means
this file.

## 1. Summary

axHelp is a question to Claude from anywhere: a hotkey from any program brings up one field, Enter asks Claude Code
in its print mode (through the bundle's AI help, ai-help.md AH-2), and the answer comes as a notice the user reads,
asks more about and closes. The last ten questions with their answers stay in a list under the field. `AxKit.exe
help [--hidden]`, Start entry `axKit axHelp`, command `axhelp`; a background process from sign-in, as axStart and
axClip (AX-13.4 in the Store's copy). It is an access layer (AX-11.1): Claude Code, Anthropic's program, does the
work under the user's own account and Anthropic's terms; AI help must be on (axKit menu, AI help) and the consent
given once (AH-1.2) before it answers anything.

## 2. Goals and non-goals

- G1. One key from anywhere, one field, one answer: nothing to open, find or close beyond Escape.
- G2. Nothing of AI help lives here: the switch, the folders, the program, the consent, the request and the answer
  notice are the window base's (ai-help.md section 5); axHelp adds the hotkey, the field, the list and the prompt's
  context.
- G3. The last questions are a memory, not a transcript: ten, newest first, each reopened with its answer and its
  session for more questions (AH-D4).
- Non-goal: a chat window. Non-goal: reading the program in front (axDescribe reads its picture; axHelp names it).

## 3. Scenarios

- In Word, Windows+Alt+H: "axHelp window, Ask Claude, edit". "how do I make a table of contents in word", Enter.
  "Asking Claude". Then "Claude says" and the answer, read with Down; Tab, "and update it later?", Enter: the same
  session answers. Escape, Escape: back in Word on the same line.
- Windows+Alt+H, Down: "how do I make a table of contents in word, 1 of 1"; Enter: the answer again; Tab, a new
  follow-up in the same session.
- AI help off: Enter says "AI help is off: axKit menu, AI help, Let Claude help in the apps" (AH-1.3).

## 4. Requirements

### HE-1 Launch, the background process, the hand-over

- HE-1.1 `AxKit.exe help` shows the field in front and keeps running; `--hidden` runs it hidden until the hotkey, as at
  sign-in (`HotkeyForm`, axStart AS-1.1, AS-1.3). Start with Windows (Options, General) writes the Run key. In the
  Store's copy axHelp has no startup task (bundle AX-13.2: a task hangs on one of the eight entries, which are taken):
  it is started from any window's New window and then keeps running; its Start with Windows item says so.
- HE-1.2 One axHelp at a time (a named mutex): a second start hands "show" to the running one's window (`AppMessage`)
  and ends; with `--hidden` it just ends (axStart AS-1.2).
- HE-1.3 Alt+F4 and Close all windows hide the window; axHelp, Quit axHelp ends the process (axStart AS-1.4).

### HE-2 The window

- HE-2.1 One field, "Ask Claude", with the caret always in it (axStart AS-D2), the list of earlier questions under
  it, which never takes the focus (HE-3), a menu bar and a status bar; the title is the app's title (AX-11.2), by
  which a second start finds it.
- HE-2.2 The status bar: "N questions kept" ("1 question kept"), or "AI help is off: axKit menu, AI help" while the
  switch is off.
- HE-2.3 **Enter** asks the typed question (AH-6.4: the notice "Asking Claude" with Cancel, then "Claude says",
  AH-3.1, with the field for more, AH-6.3); the question and the answer join the list (HE-3.1) and the field is
  emptied. With nothing typed, Enter shows the selected earlier question's answer again, with its session for more;
  with nothing kept, "Type a question first".
- HE-2.4 The prompt (`AiPrompts.Anywhere`) names the program and the window title that were in front when the hotkey
  was pressed (`HotkeyForm.ForegroundBefore`, `WindowOwner`), so that "how do I do this here" has a here; the folders
  Claude may read are the bundle's (AH-2.3), reachable from axHelp's first menu as from the axKit menu.
- HE-2.5 Escape hides the window at once, whatever the field holds, and the field is empty the next time (axStart
  AS-3.4); the focus going to another program hides it too (AS-3.5), unless a notice is open.
- HE-2.6 axHelp, Forget the questions: a yes-or-keep notice, then the list emptied; nothing changes at Anthropic.

### HE-3 The earlier questions

- HE-3.1 The last ten questions with their answers and sessions, newest first, in `axhelp.json`; a question asked
  again moves to the front with its new answer (`HelpSettings.Remember`).
- HE-3.2 Typing in the field narrows the list to the questions holding every typed word (`TextMatch`, as axStart's
  programs); with nothing typed the list shows them all; "No earlier question matches xyz" or "No questions yet:
  type one and press Enter" as the list's one line when there is nothing.
- HE-3.3 Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End move the selection with the caret kept in the field
  (`HotkeyForm.AddMatchKeys`), each match said as "the question, n of m"; at an end the edge earcon and the match
  again (AS-3.3). Say the first match while typing (Options, General) says the first match after a pause.

### HE-4 The hotkey

- HE-4.1 Windows+Alt+H by default (ai-help.md section 9: never Ctrl+H, which every program uses), changed in Options,
  Hotkey (axStart AS-4.2) and kept in the settings; the hotkey while the window shows hides it.
- HE-4.2 A hotkey another program holds: the window shows with the notice that it is taken (AS-4.3).

### HE-5 Settings

- HE-5.1 `%APPDATA%\AxKit\axhelp.json` (`HelpSettings` on `HotkeySettings`): `hotkey`, `startWithWindows`,
  `sayFirstMatch`, the font and the update check, and `exchanges` (question, answer, sessionId, when; ten).
- HE-5.2 AI help's own settings are the bundle's `ai-help.json` (AH-1.1), never here.

### HE-6 Keys

Window-specific (`AxHelpKeys`): Down and Up the next and previous question; Page Down and Page Up a page (the
bundle's keys, on the list here); Ctrl+Home and Ctrl+End the first and last; Enter ask; Escape hide. Bundle-wide
(AX-8.2): F1, Ctrl+Shift+P, F4, Shift+F1 (a question about axHelp's own window), Ctrl+F in a notice's text, the
text size keys, Alt and Alt+F4. The F1 text names them all (`HelpText.Shortcuts`).

### HE-7 Notices and announcements

"Asking Claude" with Cancel; "Claude says" with the answer, the field "Ask more about this answer", Ask more, Copy
and Close; "Claude could not answer" with Claude Code's words; "AI help is off: axKit menu, AI help, Let Claude help
in the apps"; "Claude Code was not found" with Install Claude Code now; the consent notice (AX-11.4) under the app
"AI help"; "the question, n of m" and the edge earcon on the list; "Type a question first"; "Cancelled";
"Forget the questions?"; "Questions forgotten"; "Hotkey …" and "… is taken by another program" (AS-4).

## 5. Architecture

`src/AxHelp`: `HelpForm` on `AxKit.Forms/HotkeyForm` (the field, the list, the match keys, the hotkey, the showing and
hiding, the foreground rule are the base's; `AskClaude`, `ShowAnswer` and `ShowAiFolders` are `AxKitForm.Ai.cs`),
`AxHelpApp` (the mutex and `--hidden`), `AxHelpKeys`, `HelpText`. `src/AxHelp.Core`: `HelpSettings` with
`HelpExchange`, `HelpLines` (the matches and what is said); tested in `tests/AxHelp.Tests`.

## 6. Decisions

- **HE-D1 A HotkeyForm app, not a window of its own.** The field-over-a-list shape, the hotkey and the foreground
  rule exist and are checked with NVDA; axHelp adds a prompt and a list of exchanges.
- **HE-D2 The exchanges in the settings file.** Ten short texts; a file of their own would be a second place to look.
- **HE-D3 The program in front named, not read.** What was in front gives the question its place ("here" means
  Word); reading its screen is axDescribe's, with a picture, and reading its text would be a second access layer.

## 7. Testing

`tests/AxHelp.Tests` (the exchanges, the matches, the settings); `tests/scripts/axhelp/01-start.txt` with the fake
Claude (ai-help.md AH-5); the NVDA test plan `docs/axhelp/nvda-test-plan.md`.
