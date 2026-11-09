# AI help: one shared, optional service

The user's direction since 2026-10-18 (bundle SPEC G6, B-15; `plan.md`, Still to come): AI editing and feedback are
not any app's own feature but one service every window can call, optional, so that each app keeps its full utility
offline. Specified 2026-11-09 (the night run), to be read by the user before it is built. Requirements are numbered
AH-n.m, decisions AH-Dn. It settles the four questions the plan left open: the database, how the service reaches
Claude, what it may change in a window, and the one switch.

## 1. Summary

AI help is Claude Code, the program the user already has, asked one question at a time from inside a window: the
window gathers what the user points at (a selection, the document, the folder in hand, a screenshot), the service
runs Claude Code in its print mode as a child process with read-only tools over the folders the user allowed, and the
answer comes back as a notice text the user reads with the bundle's keys, copies, or lets the window apply as one
undoable change. Off by default; one switch in the axKit menu turns it on for every window; off, every AI item says so
and does nothing. No API key, no network code of axKit's own, no database: Claude Code reads the files itself.

## 2. Goals and non-goals

- G1. One service, written once in the shared pieces, that any window calls with a prompt and gets a text back;
  AxAsk and AxSee (section 7) are its first two apps, axDown's and axJSON's AI items its first uses in a window.
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
- Non-goal: a conversation. One question, one answer; the next question starts fresh (axClaude is the conversation).

## 3. Scenarios

- In axDown, a paragraph selected, AI, Improve the selection: "Asking Claude…", then a notice "Claude suggests" with
  the rewritten paragraph; Enter replaces the selection with it, one Ctrl+Z step; Escape keeps the text as it was.
- In axJSON, AI, Explain this file: the answer as a notice text; Ctrl+C copies it; Escape closes it.
- From anywhere, AxAsk's hotkey: a field "Ask Claude about your files"; "where did I write down the DNS settings";
  the answer names the file and quotes it; Enter on a path it names opens the file in its app.
- From anywhere, AxSee's hotkey: a screenshot of the window in front goes to Claude with "Describe this screen for a
  blind user"; the description as a notice text.
- AI help off: AI, Improve the selection says "AI help is off: axKit menu, AI help, Let Claude help in the apps".
- Offline: "Claude Code could not reach Anthropic: …" as a notice, the document untouched.

## 4. Requirements

### AH-1 The switch and the consent

- AH-1.1 One setting for the bundle, in `%APPDATA%\AxKit\ai-help.json`: `enabled` (false by default), `folders` (the
  folders Claude may read, AH-2.3), `model` (empty for Claude Code's default). axKit menu → AI help ▸ → Let Claude help
  in the apps (a checked item), Folders Claude may read…, the same in every window (the axKit menu's shape, AX-9).
- AH-1.2 Turning it on shows the consent notice of AX-11.4 for Claude Code, recorded under the app "AI help" and the
  target Claude Code (one agreement for the service, however many windows use it); the notice says what goes to
  Anthropic: the question, what the window gathered, and the files Claude reads in the allowed folders, under the
  user's own account and Anthropic's terms.
- AH-1.3 Off: every AI item of every window stays in its menu, and says "AI help is off: axKit menu, AI help, Let
  Claude help in the apps" when chosen (AX-7.3: said, never a notice), so that the user learns where the switch is.
- AH-1.4 Claude Code missing: the item says so and offers Install it now, as axClaude does (AX-2.9).

### AH-2 How the service reaches Claude

- AH-2.1 Claude Code in print mode, one process per question (`ClaudeCodeProgram` finds it as axClaude does):
  `claude -p <prompt> --output-format json --allowedTools Read,Glob,Grep [--add-dir <folder>]…`, started without a
  window in the first allowed folder (or the user's profile folder when none), its standard output read to the end
  under a time limit (`ProcessRun`); the prompt goes on standard input, never on the command line (it may be long and
  may hold anything). `// Outside call, program, Claude Code` (B-17), the kind axClaude's calls already have.
- AH-2.2 The tools allowed are the three that read (`Read`, `Glob`, `Grep`); print mode cannot ask for permissions, so
  anything else is refused by Claude Code itself: the service cannot write a file, run a command or reach the web.
- AH-2.3 **Folders Claude may read**: the list of AH-1.1, chosen with the folder picker, each a line in a notice with
  Add and Remove; the service passes them as `--add-dir` and starts in the first. A window's own file or folder is
  added for that call alone when the user asked about it (axDown's document, AxSee's screenshot file), so that the
  answer can read what the user pointed at without the folder being allowed for good.
- AH-2.4 The answer is the JSON's `result` text; its `is_error` makes a notice "Claude could not answer" with the
  text; a process that does not end in 120 s is ended and said ("No answer in two minutes"); Escape in the waiting
  notice ends it at once.
- AH-2.5 The log holds the time, the window, the prompt's length, the folders and the outcome, never the prompt or
  the answer (they are the user's text).

### AH-3 What a window may do with an answer

- AH-3.1 **Ask**: the answer is a notice text, "Claude says", read with the bundle's keys (the heading letters,
  Ctrl+F), Ctrl+C copying it, Escape closing it. Every use starts here; this is all AxAsk and AxSee do.
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

- The pure parts in `AxKit.Core/Ai`: the settings file, the command line built from the folders and the model, the
  JSON answer read (`result`, `is_error`, a broken answer), the prompts with the user's text appended; `AiTests`.
- The window: the automation script of each use against a fake `claude` (a script in the run's folder that prints a
  canned JSON answer, as axMCP's fake server does), so that no run reaches Anthropic.
- NVDA: each window's plan gains the steps of its AI items; AxAsk and AxSee have plans of their own.

## 5. Architecture

- `AxKit.Core/Ai/AiSettings` (AH-1.1), `AiRequest` (the command line and the process, AH-2), `AiAnswer` (the JSON
  read), `AiPrompts` (AH-4).
- `AxKit.Forms/AxKitForm.Ai.cs`: `AskClaude(prompt, Action<string> shown)` with the waiting notice and the switch
  check, `OfferChange(prompt, Func<string, bool> apply)` for AH-3.2, the axKit menu's AI help submenu (AX-9).
- Each window's AI menu calls those with what it gathers; the apps keep no AI code of their own beyond the menu and
  the gathering.

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
- **AH-D4 One question, one answer.** A conversation is axClaude; the service is a question from inside a window,
  answered and done, so that nothing of it has to be shown, kept or resumed.
- **AH-D5 Off by default, one switch.** The bundle's rule for everything that reaches a service (the AI tools, the
  update check, the reports): the user turns it on once, in a place every window shows.

## 7. The first two apps

- **AxAsk** (asked 2026-10-23): on `HotkeyForm`, a hidden process from sign-in as axStart: its hotkey shows one
  field "Ask Claude about your files"; Enter asks with the folders of AH-2.3, the answer a notice text with Enter on
  a path opening it (AH-3.3); the last ten questions in a list under the field, as axStart's matches, Up and Down to
  reuse one. Its spec, `docs/axask/SPEC.md`, is written from this document when the build starts.
- **AxSee** (asked 2026-10-23): a hotkey that takes a screenshot of the window in front (`PrintWindow`, a PNG in
  `%LOCALAPPDATA%\AxKit\axsee`, deleted after the answer), adds that file's folder for the call and asks "Describe
  this screenshot for a blind user: what the window is, what it shows, what the controls are"; the answer a notice
  text. A second ask about the same picture is the field under the answer. Its spec the same way.

## 8. Plan

1. This document; `plan.md` points here; the user reads it and says yes, or changes it (the night of 2026-11-09 left
   the build for that reading: the service sends the user's text and files to Anthropic, which is the user's to agree
   to in the spec, not in the code).
2. `AxKit.Core/Ai` with its tests; the settings file; the switch and the consent in the axKit menu (AH-1).
3. `AxKitForm.Ai.cs` (AH-3.1, AH-3.4) and axDown's first AI item, Explain the selection, with its script and NVDA step.
4. Offer a change (AH-3.2) in axDown and axJSON.
5. AxAsk, then AxSee, each with `tools/new-app.ps1`, its spec, its scripts and its NVDA pass.
