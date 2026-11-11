# AI help: one shared, optional service

The user's direction since 2026-10-18 (bundle SPEC G6, B-15; `plan.md`, Still to come): AI editing and feedback are
not any app's own feature but one service every window can call, optional, so that each app keeps its full utility
offline. Specified 2026-11-09 (the night run), read and cleared by the user the same evening ("you are clear to do
the ai assistant work"), with the names, the keys and the shape of the answers settled in section 9, and built that
night on the branch `ai-help`. Requirements are numbered AH-n.m, decisions AH-Dn. It settles the four questions the
plan left open: the database, how the service reaches Claude, what it may change in a window, and the one switch.

## 1. Summary

AI help is Claude Code, the program the user already has, asked one question at a time from inside a window: the
window gathers what the user points at (a selection, the document, the folder in hand, a screenshot), the service
runs Claude Code in its print mode as a child process with read-only tools over the folders the user allowed, and the
answer comes back as a notice text the user reads with the bundle's keys, copies, or lets the window apply as one
undoable change. Off by default; one switch in the axKit menu, Claude, turns it on for every window; off, the first ask opens the reminder
and does nothing. No API key, no network code of axKit's own, no database: Claude Code reads the files itself.

## 2. Goals and non-goals

- G1. One service, written once in the shared pieces, that any window calls with a prompt and gets a text back;
  Shift+F1 in every window (AH-6) is its first use, axHelp and axDescribe (section 7) its first two apps; axDown's
  and axJSON's AI items (AH-3.2) come later.
- G2. Optional and local first (bundle G6): nothing of any app depends on it; with it off, or offline, every window is
  what it is today.
- G3. The user's account and consent, through the program they already run: Claude Code, under Anthropic's terms,
  the consent of AX-11.4 asked once for the service as for axClaude.
- G4. The service never changes a file. It answers; a window may offer the answer as a change to its own document,
  which the window applies and the window's undo takes back.
- Non-goal: Anthropic's API called by axKit (a key to keep, a second account path, network code of axKit's own to
  state; AH-D1).
- Non-goal: an index or database of the user's files (AH-D2). Claude Code's own tools read and search the folders
  allowed; a database waits for a use that needs one.
- Non-goal: a conversation window. One question, one answer as a notice; a follow-up typed under the answer continues
  the same Claude session (AH-6.3), but nothing is shown as a transcript (axClaude is the conversation).

## 3. Scenarios

- In axDown, a paragraph selected, AI, Improve the selection: "Asking Claude…", then a notice "Claude suggests" with
  the rewritten paragraph; Enter replaces the selection with it, one Ctrl+Z step; Escape keeps the text as it was.
- In axJSON, AI, Explain this file: the answer as a notice text; Ctrl+C copies it; Escape closes it.
- In any axKit window, Shift+F1: "Ask Claude about this window"; "how do I turn word wrap off?"; the window sends its
  name, its keyboard shortcuts, its guide and the user's last actions, and the answer names Ctrl+Shift+W; Tab, "and in
  axJSON?", Enter: the same session answers again.
- From anywhere, axHelp's hotkey (Windows+Alt+H): a field "Ask Claude"; "where did I write down the DNS settings"; the
  answer names the file, found in the folders Claude may read; Down later brings the question back with its answer.
- From anywhere, axDescribe's hotkey (Windows+Alt+S): a picture of the window in front goes to Claude; the
  description comes as a notice text; "is anything selected?" under it asks about the same picture.
- AI help off: AI, Improve the selection says "AI help is off: axKit menu, Claude, Use Claude in axKit".
- Offline: "Claude Code could not reach Anthropic: …" as a notice, the document untouched.

## 4. Requirements

### AH-1 The switch and the consent

- AH-1.1 One setting for the bundle, in `%APPDATA%\AxKit\ai-help.json`: `enabled` (false by default), `folders` (the
  folders Claude may read, AH-2.3), `model` (empty for Claude Code's default), `claudePath` (Claude Code's program
  when it is not where axKit looks; a relative path is taken from the settings folder, which is how the automation's
  fake stands in), `workingFolder` (Claude Code's working folder, AH-2.1; empty for `Documents\axKit Claude`). axKit
  menu → Claude ▸ → Use Claude in axKit (a checked item, the switch), Ask about this window… (Shift+F1, AH-6), Folders
  Claude may read…, Claude's working folder (AH-2.1, opened in axFile), a separator, AI tools ▸ (`mcp.md` MC-6.1):
  everything of Claude in one submenu, the same in every window (the Window menu's shape, AX-9.1; until 5.6.0 an AI
  help submenu and the AI tools stood side by side, and the switch was "Let Claude help in the apps"). The file is
  read again at every use, so a switch in one window counts in every other at its next question.
- AH-1.2 Turning it on shows the consent notice of AX-11.4 for Claude Code, recorded under the app "AI help" and the
  target Claude Code (one agreement for the service, however many windows use it); the notice says what goes to
  Anthropic: the question, what the window gathered, and the files Claude reads in the allowed folders, under the
  user's own account and Anthropic's terms.
- AH-1.3 Off: every AI item of every window stays in its menu, and **whatever asks for Claude while it is off turns it
  on** (5.6.0; the user, 2026-11-09: "the ai help toggle process is confusing and should immediately open the consent
  form"): Shift+F1, Enter in axHelp, axDescribe's hotkey or the switch itself opens the reminder of AH-1.2 at once
  when the agreement is not recorded yet, and its Turn on records it, turns the switch on, says "Claude on" and goes on
  with the ask that was in hand (the question field, the picture); with the agreement recorded, the switch goes on and
  the ask goes on, "Claude on" said. Not now (Escape) leaves Claude off, "Claude stays off", nothing asked. Off from the
  switch is "Claude off" (`AxKitForm.TurnOnAi`, `ToggleAiHelp`, `AiReady`). axHelp's and axDescribe's status bars say
  "Claude is off; asking turns it on" while it is off. Until 5.6.0 every item said "AI help is off: axKit menu, AI help,
  Let Claude help in the apps" and did nothing, which sent the user to a menu to find a switch to find a notice.
- AH-1.4 Claude Code missing: the item says so and offers Install it now, as axClaude does (AX-2.9).

### AH-2 How the service reaches Claude

- AH-2.1 Claude Code in print mode, one process per question (`ClaudeCodeProgram` finds it as axClaude does, after
  the file's `claudePath`): `claude -p --output-format json --allowedTools Read,Glob,Grep [--add-dir <folder>]…
  [--model <model>] [--resume <session>]`, started without a window in **Claude's working folder** (AH-D6, 5.6.0:
  `Documents\axKit Claude` unless the file's `workingFolder` names another, made when first needed,
  `AiSettings.EnsureWorkingFolder`; axKit menu, Claude, Claude's working folder, and axHelp's first menu, open it in
  axFile; a CLAUDE.md the user writes there is Claude's standing instructions, and Claude Code's own memory, settings
  and sessions for the service live there; until 5.6.0 Claude Code started in the first allowed folder, or the user's
  profile, so nothing of its own had a home), every allowed folder as `--add-dir`, its standard output read to the
  end under a time limit (`ProcessRun`); the prompt goes on standard input, never on the command line (it may be long and may hold
  anything). `// Outside call, program, Claude Code` (B-17), the kind axClaude's calls already have.
- AH-2.2 The tools allowed are the three that read (`Read`, `Glob`, `Grep`); print mode cannot ask for permissions, so
  anything else is refused by Claude Code itself: the service cannot write a file, run a command or reach the web.
- AH-2.3 **Folders Claude may read**: the list of AH-1.1, chosen with the folder picker, each a line in a notice with
  Add and Remove, the notice naming Claude's working folder (AH-2.1) as the one it starts in; the service passes them
  as `--add-dir`. A window's own file or folder is
  added for that call alone when the user asked about it (axDown's document, AxSee's screenshot file), so that the
  answer can read what the user pointed at without the folder being allowed for good.
- AH-2.4 The answer is the JSON's `result` text; its `is_error` makes a notice "Claude could not answer" with the
  text; a process that does not end in 120 s is ended and said ("No answer in two minutes"); Escape in the waiting
  notice ends it at once.
- AH-2.5 The log holds the time, the window, the prompt's length, the folders and the outcome, never the prompt or
  the answer (they are the user's text).

### AH-3 What a window may do with an answer

- AH-3.1 **Ask**: the answer is a notice, "Claude says": the text first, read with the bundle's keys (the heading
  letters, Ctrl+F), then a field "Ask more about this answer" (AH-6.3), then the buttons Ask more (Enter), Copy
  (the whole answer to the clipboard, the notice kept) and Close (Escape). An error is "Claude could not answer"
  with Claude Code's own words. Every use starts here; this is all axHelp and axDescribe do.
- AH-3.2 **Offer a change**: an editor may ask for a rewrite of the selection or the document ("Improve", "Fix the
  spelling", "Explain" asks only). The answer is a notice "Claude suggests" with the text and the buttons Replace and
  Keep mine; Replace puts it in place of the selection (or the document) as one step of the window's own undo
  (axDown AD-2.5), and says "Replaced, Ctrl+Z takes it back"; Keep mine (Escape) changes nothing. The window applies
  the change, never the service; nothing is written to disk until the user saves.
- AH-3.3 **Open what it names**: a path in an answer that exists on this computer is opened with Enter on its line
  (AxAsk's use), through the bundle's dispatch, in the app for that kind of file; a folder on a network share is
  refused as elsewhere (security audit S-5).
- AH-3.4 While a question runs the window shows a notice "Asking Claude…" with Cancel, the keys of the window
  blocked as under any notice; the window's own work (axClaude's conversation, axTerminal's shell) goes on behind it.
- AH-3.5 Every AI item lives in one menu named AI, after Edit (or the window's second menu), the same words in every
  window that has it; a window without a use for it has no AI menu. F1 names its items.

### AH-4 Prompts

- AH-4.1 Each use has one prompt text in the code (`AiPrompts`), in plain English, with the user's text appended after
  a line that says where it starts; the prompt says the reader is blind and uses a screen reader, so that the answer
  is text that reads well aloud (no tables, no decoration, short paragraphs).
- AH-4.2 The user's own question, when there is one (AxAsk), is the last paragraph of the prompt; nothing of the
  user's text is reworded by axKit.

### AH-5 Tests

- The pure parts in `AxKit.Core/Ai`: the settings file (the relative `claudePath`), the command line built from the
  folders, the model and the session, the JSON answer read (`result`, `is_error`, `session_id`, a broken answer, an
  empty one), the prompts with the user's text last; `AiTests`.
- The window: the automation script of each use against a fake `claude` (a cmd script in the run's own settings
  folder, named by `claudePath`, that swallows the prompt and prints one canned JSON answer, as axMCP's fake server
  does), so that no run reaches Anthropic: `tests/scripts/axHelp/01-start.txt`, `tests/scripts/axDescribe/01-start.txt`,
  and axDown's `11-ask-claude.txt` for Shift+F1.
- NVDA: axDown's plan gains the Shift+F1 steps; axHelp and axDescribe have plans of their own.

### AH-6 A question about this window (Shift+F1)

- AH-6.1 **Shift+F1** in every window (`BundleKeys.AskClaude`, AX-8.2; Windows' own key for help about the thing in
  front), and axKit menu → Claude → Ask about this window…: a notice with one field, "Your question about this
  window", whose text says what Claude gets; Enter asks, Escape asks nothing. Off, missing and consent as AH-1 (off:
  the reminder first, then the field, AH-1.3).
- AH-6.2 **What the window sends** (`WindowContext`): the app's title (AX-11.2), the window's title, the focused
  control's name and place as the reports describe it (RP-4.2: never its text), the last actions of the window (the
  reports' list, redacted as a report is), the F1 text and the app's user guide (the one Help, User guide shows), so
  that Claude answers from the app's own documents. Never the text of a document, a conversation or a field, and
  never a file of the user's unless the user allowed its folder (AH-2.3).
- AH-6.3 **Follow-ups**: the field under the answer asks again in the same Claude Code session (`--resume` with the
  `session_id` of the answer), so that "say more about the second point" works; the answer replaces the one before.
  Nothing is kept of the exchange by the window itself (axHelp and axDescribe keep their last ten, section 7).
- AH-6.4 While Claude works, the notice "Asking Claude" with Cancel (Escape ends the process at once, "Cancelled");
  the window's own work goes on behind it (AH-3.4). The answer notice opens in its place with the focus on the text,
  so that NVDA reads it from the top; Tab reaches the field.

## 5. Architecture

- `AxKit.Core/Ai/AiSettings` (AH-1.1), `AiRequest` (the command line and the process, AH-2), `AiAnswer` (the JSON
  read, with the session), `AiPrompts` with `WindowContext` (AH-4, AH-6.2).
- `AxKit.Forms/AxKitForm.Ai.cs`: `CreateClaudeMenu` (axKit, Claude, AX-9.1, beside the AI tools), `TurnOnAi` (AH-1.3), `AskAboutThisWindow` (Shift+F1,
  AH-6), `AskClaude(prompt, extraFolders, resume, then)` with the switch, the program and the consent checks and the
  waiting notice, `ShowAnswer(answer, extraFolders, closed)` with the follow-up field (AH-3.1, AH-6.3), the folders
  notice (AH-2.3); `ConsentTarget.AiHelp` and `HasConsent`'s app name for the one agreement (AH-1.2); `Notice.FocusText`
  for an answer read before its field; `OfferChange` for AH-3.2 when the editors' AI items come.
- `AxKit.Forms/WindowOwner`: the program and the title of the window in front, for the two apps' prompts.
- The apps (`AxHelp`, `AxDescribe`) and the windows call those with what they gather; no AI code of their own.

## 6. Decisions

- **AH-D1 Claude Code, not the API.** The user has Claude Code, its account, its consent and its sign-in already;
  axKit's rule is that an access layer reaches the network only through its program (AX-11.11), and the API would
  make axKit itself a network client with a key to keep. Print mode gives one answer per process, which is all the
  service needs.
- **AH-D2 No database.** The plan foresaw a shared database for the index of the user's files. Claude Code reads and
  searches folders itself, well enough for a question at a time; an index would be a second copy of the user's files
  to keep right. If a use comes that needs one (a knowledge base across sessions), it is specified then, and axSQL
  (`plan.md`) is where it would live.
- **AH-D3 Read-only tools.** The service never changes anything: the window does, as one undo step, from what the
  user read first. This keeps every answer safe to ask and every change the user's.
- **AH-D4 One question, one answer, and follow-ups in the same session; no transcript.** A conversation is axClaude;
  the service is a question from inside a window, answered as one notice. The user asked on 2026-11-09 whether it
  would be "a chat like interface": the answer is a field under every answer that continues the same Claude Code
  session (`--resume`), which costs nothing to keep (Claude Code keeps the session) and reads as a chat to the user,
  without a transcript window to build, read or save; axHelp and axDescribe keep their last ten exchanges as a list
  to reopen, which is the memory a user needs.
- **AH-D5 Off by default, one switch.** The bundle's rule for everything that reaches a service (the AI tools, the
  update check, the reports): the user turns it on once, in a place every window shows.
- **AH-D6 A working folder of Claude's own** (5.6.0; the user, 2026-11-09: axHelp "should be a simple chat helper
  with its own default location for working directory to use all the claude code benefits"). Claude Code keeps what
  it learns per working folder: a CLAUDE.md there is read into every question, its memory, its project settings and
  its sessions live under that folder's name, and `--resume` finds a session from the folder it was made in. Starting
  in the first allowed folder, or the user's profile, scattered that; one folder, `Documents\axKit Claude`, made when
  first needed and opened from the menu, gathers it and gives the user one place to write Claude standing
  instructions. The allowed folders stay what they were, `--add-dir` each.

## 7. The first two apps (built 2026-11-09; rebuilt as one in 5.7.0, section 10)

- **axHelp**, access layer for Claude Code (asked 2026-10-23 as AxAsk; `docs/axHelp/SPEC.md`): on `HotkeyForm`, a
  hidden process from sign-in as axStart, hotkey Windows+Alt+H by default: one field "Ask Claude"; Enter asks with
  the folders of AH-2.3 and the name and title of the program that was in front when the hotkey was pressed, the
  answer as AH-3.1 with its follow-up field; the last ten questions with their answers in the list under the field,
  found by typing, Down and Up through them, Enter with nothing typed showing one's answer again with its session.
- **axDescribe**, access layer for Claude Code (asked 2026-10-23 as AxSee; `docs/axDescribe/SPEC.md`): the same window,
  hotkey Windows+Alt+S by default: before its window shows it takes a picture of the window in front
  (`Graphics.CopyFromScreen` of the window's rectangle, one PNG under the local AxKit folder, written over by the next
  one and deleted when axDescribe ends), asks Claude to read the file and describe it for a blind user, and shows the
  description as AH-3.1; the field under it asks about the same picture in the same session; Ctrl+O opens the picture
  in axPreview, whose reader gives the text in it; the last ten descriptions in the list, by time and window title.
- AH-3.3 (a path in an answer opened with Enter) waits: the answers name files, and Ctrl+C with axFile's Go to folder
  does the rest for now.

## 8. Plan

1. This document; `plan.md` points here; the user reads it and says yes, or changes it (the night of 2026-11-09 left
   the build for that reading: the service sends the user's text and files to Anthropic, which is the user's to agree
   to in the spec, not in the code). Done: cleared the evening of 2026-11-09, section 9.
2. `AxKit.Core/Ai` with its tests; the settings file; the switch and the consent in the axKit menu (AH-1). Done.
3. `AxKitForm.Ai.cs` (AH-3.1, AH-3.4) and Shift+F1 in every window (AH-6), with axDown's script. Done.
4. axHelp, then axDescribe, each with `tools/new-app.ps1`, its spec, its script and its NVDA pass. Done.
5. Later: axDown's and axJSON's AI items with Offer a change (AH-3.2); Enter on a path in an answer (AH-3.3).

## 9. The user's decisions of 2026-11-09 and what was decided for them

- **Names**: "i think axdescribe and axHelp are better names": axHelp for AxAsk, axDescribe for AxSee.
- **The key.** The user asked for Ctrl+H as the global hotkey and as the key in every axKit window. Not taken, for a
  reason the user could not see from the description: a global hotkey takes its key from every program on the
  computer, and Ctrl+H is Replace in Word, Notepad and most editors, History in every browser, and axDown's and
  axPreview's own next heading; inside an edit control it also arrives as Backspace (axDown AD-5.1). The defaults
  are Windows+Alt+H for axHelp and Windows+Alt+S for axDescribe, in the family of axClip's Windows+Alt+V, each
  changeable in Options, Hotkey as the other two are; and inside an axKit window Shift+F1, Windows' own key for help
  about the thing in front, free in every window of the bundle (`KeyTests`). The user changes any of the three in
  one place if the choice is wrong.
- **"with the specific window and program context loaded"**: AH-6.2, gathered by the window base from what every
  window already has for a report (RP-3), plus its F1 text and guide; never the user's text.
- **"is this going to be a chat like interface?"**: AH-D4: follow-ups in the same session from a field under every
  answer, a list of the last ten exchanges in the two apps, no transcript window.
- **The Store's copy.** The Store allows eight app entries and a startup task hangs on an entry (bundle AX-13.2); the
  eight are taken (axClaude, axDown, axFile, axTerminal, axGIT, axTask, axStart, axClip). axHelp and axDescribe start
  at sign-in in the GitHub install (the Run key, as axStart) and have no sign-in start in the Store's copy, where they
  are started from any window's New window and then keep running; which eight the Store shows is the user's to change
  in `Dispatch.Apps`. The user answered on 2026-11-11: axHelp gets an entry, in axGIT's place, so from the next Store
  package it has its startup task there too (axDescribe merged into it in 5.7.0).

Later the same evening, after the first use (5.6.0):

- **"when axhelp opens it should just let me type a question and get a response. presently it says no questions,
  please ask one and tab does nothing"**: the list's one line, "No questions yet", was said 300 ms into typing a
  question (Say the first match while typing, on by default from axStart); axHelp and axDescribe now say nothing while
  typing (their `sayFirstMatch` is off by default, with no menu item; axStart and axClip keep theirs), and Tab reaches
  the list of earlier questions, where NVDA reads each one and Enter shows its answer (`HotkeyForm.MatchesTakeFocus`).
- **"its own default location for working directory to use all the claude code benefits"**: AH-D6.
- **"the ai help toggle process is confusing and should immediately open the consent form to activate the service"**:
  AH-1.3: any ask while Claude is off opens the reminder at once and goes on after Turn on; the switch is "Use Claude
  in axKit", said as "Claude on" and "Claude off".
- **"group the claude related apps in the axkit menu so that a user can either enable or disable the entire claude
  suite and still use the axkit apps"**: one Claude submenu in the axKit menu holds the switch, the question, the
  folders, the working folder and the AI tools (AX-9.1); the Claude apps (axClaude, axMCP, axHelp, axDescribe) stand
  as the last group of New window and every list of apps (`AppGroup.Claude`, AX-9.2). The switch is AI help's: axClaude
  itself, axMCP and the AI tools keep their own agreements and switches, since each is its own access layer and the
  user turns each on knowingly; every other app of axKit works with all of them off (G2).
- **"simplify the app menus and especially the axkit menu which is now hard to find the new window"**: the axKit menu's
  order of AX-9.1 (New window first, Open windows by type gone); axHelp's and axDescribe's menus lost the first-match
  item (HE-3.3). The other apps' menus are untouched here; a pass over them is a priorities item.

## 10. axHelp as a conversation in axClaude (5.7.0, 2026-11-10)

The user, after the first use: "make the ask claude interface simpler so that it is just like the axclaude without any
of the buttons ... a seamless conversation ... leave it ongoing and then let the user clear it ... give that axhelp
permissions to do things on the user's computer ... a global hotkey to take a screenshot ... drop it into the axhelp
with a single keypress"; then, asked whether axDescribe should stay an app of its own, "merge axDescribe into axHelp";
and on the model, "a very fast chat like experience ... that only spins up a better model if needed for agentic work".

- axHelp is now two hotkeys into one conversation, and the conversation is an axClaude window (`docs/axHelp/SPEC.md`
  HE-D1): Windows+Alt+H brings it up on Claude's working folder (AH-2.1), titled "axHelp", ongoing until the user
  clears it; Windows+Alt+S puts a picture of the window in front into its message field. axDescribe leaves the bundle;
  its verb still starts axHelp.
- So axHelp is no longer a use of this service's print mode: the conversation is Claude Code in its interactive mode,
  with axClaude's agreement and its permission questions, not read-only. Shift+F1 in every window (AH-6) stays this
  service's, as section 4 has it.
- The model is axHelp's own, given at the start (`--model`, fast by default), and a subagent on the strongest model in
  the working folder takes the work of several steps (HE-2.3, HE-2.4); `/model` inside the conversation would change
  Claude Code's saved default everywhere, which the guide says.
- AiPrompts lost `Anywhere`, `Screen` and `Picture`, which only the two apps used; the working folder (AH-D6) is what
  axHelp still takes from here.
