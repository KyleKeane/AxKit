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
  fake stands in), `workingFolder` (Claude Code's working folder, AH-2.1; empty for `Documents\axKit\Claude`). axKit
  menu → Claude ▸ → Use Claude in axKit (a checked item, the switch), Ask about this window… (Shift+F1, AH-6), Folders
  Claude may read…, Claude's working folder (AH-2.1, opened in axFile), a separator, AI tools ▸ (`mcp.md` MC-6.1):
  everything of Claude in one submenu, the same in every window (the Window menu's shape, AX-9.1; until 5.6.0 an AI
  help submenu and the AI tools stood side by side, and the switch was "Let Claude help in the apps"). The file is
  read again at every use, so a switch in one window counts in every other at its next question.
- AH-1.1a **The settings that decide what is run and read are sealed** (bundle AX-5.6, 2026-11-16, `repairs.md`
  R-53): `enabled`, `folders`, `claudePath` and `workingFolder` are under the file's `seal`; `model` is not. A file
  whose seal does not check gives Claude off, no folders, the usual search for Claude Code and the default working
  folder, with the model kept. The next thing that needs the settings (the switch, a question, Folders Claude may
  read) saves them as they are then in use, shows the notice **AI help** with "AI help's settings were changed
  outside axKit and are not used: Claude is off, it may read no folder of yours, and Claude Code and its working
  folder are the usual ones. Turn Claude on and add the folders again from the axKit menu, Claude." and does not go
  on with what was asked (`AiHelpFlow.ChangedOutside`), so the notice is shown once. A hand change of a sealed file is
  refused as above; `claudePath` and `workingFolder` are set in axKit instead (AH-1.1b). Tested in `SettingsSealTests`.
- AH-1.1b **Claude Code's program and Claude's working folder, chosen in axKit** (2026-11-24): axKit, Claude, Choose
  Claude's working folder... (Windows' folder picker, opened on the folder in use) and Choose Claude Code's program...
  (Windows' file picker for claude.exe or claude.cmd, opened where the program in use is) write `workingFolder` and
  `claudePath` and seal the file anew; each says what is now in use ("Claude's working folder: C:\Work\Claude"), and
  Cancel says what stays. Neither is offered to an AI tool (AK-2.6, exception 3), as Folders Claude may read is not.
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
  the file's `claudePath`): `claude -p --output-format json --allowedTools Read,Glob,Grep --strict-mcp-config --settings <file> [--add-dir <folder>]…
  [--model <model>] [--resume <session>]`, started without a window in **Claude's working folder** (AH-D6, 5.6.0:
  `Documents\axKit\Claude` unless the file's `workingFolder` names another, made when first needed,
  `AiSettings.EnsureWorkingFolder`; axKit menu, Claude, Claude's working folder, and axHelp's first menu, open it in
  axFile; a CLAUDE.md the user writes there is Claude's standing instructions, and Claude Code's own memory, settings
  and sessions for the service live there; until 5.6.0 Claude Code started in the first allowed folder, or the user's
  profile, so nothing of its own had a home; since 2026-11-15 axKit's guides are there too, AH-7), every allowed folder as `--add-dir`, its standard output read to the
  end under a time limit (`ProcessRun`); the prompt goes on standard input, never on the command line (it may be long and may hold
  anything). `// Outside call, program, Claude Code` (B-17), the kind axClaude's calls already have.
- AH-2.2 The tools allowed are the three that read (`Read`, `Glob`, `Grep`); print mode cannot ask for permissions, so
  anything else is refused by Claude Code itself: the service cannot write a file, run a command or reach the web.
- AH-2.3 **Folders Claude may read**: the list of AH-1.1, chosen with the folder picker, each a line in a notice with
  Add and Remove, the notice naming Claude's working folder (AH-2.1) as the one it starts in; the service passes them
  as `--add-dir`. A window's own file or folder is
  added for that call alone when the user asked about it (axDown's document, AxSee's screenshot file), so that the
  answer can read what the user pointed at without the folder being allowed for good.
- AH-2.1a When Claude Code is npm's `claude.cmd`, a batch file Windows runs through cmd.exe, the command line is
  axClaude FR-1.2's (`ClaudeCodeProgram.LineFor`, `repairs.json` R-24): every argument in quotes, so that an allowed
  folder named `R&D` arrives whole; an allowed folder or a model holding a percent sign, an exclamation mark or a
  double quote, which cmd.exe acts on inside quotes too, is refused before anything runs, as the notice "Claude could
  not answer": "Claude Code was not run. claude.cmd is a batch program, which Windows runs through cmd.exe, and
  "<the folder>" holds a percent sign (%), which cmd.exe would act on even inside quotes: it is not started with it.
  Rename the folder or change the argument, or install Claude Code with its own installer… The folders Claude may
  read are in AI help's settings." `claude.exe` takes every character.
- AH-2.4 The answer is the JSON's `result` text; its `is_error` makes a notice "Claude could not answer" with the
  text; a process that does not end in 120 s is ended and said ("No answer in two minutes"); Escape in the waiting
  notice ends it at once.
- AH-2.5 The log holds the time, the window, the prompt's length, the folders and the outcome, never the prompt or
  the answer (they are the user's text).
- AH-2.6 **A question obeys nothing of the working folder that can run a command** (2026-11-16, `repairs.md` R-54).
  Print mode shows no trust question: Claude Code's documentation says it runs the hooks of a folder's
  `.claude\settings.json` and connects the servers of its `.mcp.json` even in a folder never trusted. So every
  question carries two of Claude Code's own options (code.claude.com/docs/en/cli-reference, checked against
  2.1.325): `--strict-mcp-config`, which loads only servers named with `--mcp-config`, and the question names none;
  and `--settings <file>`, where the file holds `{"disableAllHooks":true}`, which turns every hook off for the run
  and goes before the folder's own settings files. The file is `%LOCALAPPDATA%\AxKit\claude-no-hooks.json`
  (`AppPaths.ClaudeNoHooksFile`), written again before each question when it does not hold exactly that text, and
  when it cannot be written Claude Code is not run and the answer says so. A file and not the text on the command
  line: npm's `claude.cmd` is refused any argument with a double quote (AH-2.1a). `--setting-sources user` is not
  used: it would drop the folder's rules too, which is how axKit's two rule files reach Claude (AH-7.3). The words
  are in Claude Code's place in the bundle, `AxKit.Core/ClaudeCodeProgram` (`StrictMcpConfigOption`,
  `SettingsOption`, `NoHooksSettings`, `WriteNoHooksSettings`); `AiTests`. **Owed a live check**: a real question
  through a signed-in Claude Code with a hook and a `.mcp.json` in the working folder, to see that neither runs
  (`tools\upstream.ps1 -Verify` is the place for it; not run when this was built, since it needs the account and
  the network).

### AH-3 What a window may do with an answer

- AH-3.1 **Ask**: the answer is a notice, "Claude says" (its text ends with one line, "Written by Claude, an AI model. It can be wrong: check what matters before you rely on it.", bundle AX-11.12; Copy takes the answer alone): the text first, read with the bundle's keys (the heading
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
  empty one), the prompts with the user's text last; `AiTests`. The guides of AH-7 (every app's among the files,
  written where missing, again by another version, the user's own files kept); `AiDocumentationTests`.
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

### AH-7 axKit's guides in the working folder (2026-11-15)

The user, 2026-11-15: "axHelp should have a better developed RAG system by copying the relevant documentation for
axKit into the axHelp folder. Also screenshots should be placed into a folder so the top level axHelp folder stays
simple and manageable." Both uses of the working folder get it: Shift+F1 (AH-6) and axHelp's conversation
(`docs/axHelp/SPEC.md` HE-2.6).

- AH-7.1 **The folder.** `<working folder>\documentation` holds axKit's own guides as Markdown: `index.md` (one line
  per file saying what it is, and the version of axKit that wrote it), `axKit.md` (the bundle's guide,
  `docs/axKit/README.md`) and one file per app of `Dispatch.Apps` under the app's name as the user says it
  (`axFile.md`, `axGIT.md`: its `docs/ax<Name>/user-guide.md`); and, after the guides, the workflow pages
  (`docs/axKit/user-workflows.md` WF-8.1): `workflows.md` (`docs/axKit/workflows.md`, what a user can do in each
  app, as workflows and their goals), one `workflows-<page>.md` per group in the order of `Dispatch.Apps` and then
  `workflows-ai-tools.md` and `workflows-nvda-only.md` (`docs/axKit/workflows/<page>.md`, the steps of the group's
  goals with their keys), and `window-commands.md` (`docs/axKit/window-commands.md`, every command of every window
  with its menu and its key; never `commands.md`, which in the repository is the developer's commands), and, since
  2026-11-16, the two key pages: `key-groups.md` (`docs/axKit/key-groups.md`, how axKit's keys are grouped, to
  explain the patterns and to guess a key) and `key-map.md` (`docs/axKit/key-map.md`, every key and what it does in
  each app), each with its line in `index.md`; and, since 2026-11-17 (the user's ask of that day: "ensure we are
  feeding all the relevant docs to the built app so it has full information about what the program does and how to
  guide users"), the further pages (`AiDocumentation.FurtherPages`), each under the name it has in the repository:
  `families.md` (`docs/axKit/families.md`: what starts something, the menus' functions, every kind of event with
  its sound and words, every sound with what it means), `cases.md` (`docs/axKit/cases.md`: what axKit does and says
  in each unexpected situation), `outside-calls.md` (`docs/axKit/outside-calls.md`: every network request, program
  start and pipe), `privacy.md` (`PRIVACY.md`) and `changelog.md` (`CHANGELOG.md`: what changed in each version).
  What is not carried is a developer's: the specifications, designs, plans, records and audits, and the
  repository's own pages; `AiDocumentationTests` names each with its reason and fails on a new Markdown document of
  `docs/axKit` or of the repository's top that is neither carried nor named there. These files are axKit's, not the user's: each is
  written where it is missing, and all are written again when `documentation\axkit-version.txt`, the marker, does not
  hold this axKit's version, so that they follow each release (a development build writes them once in each process,
  since its guides change without its version). A changed copy is overwritten by the next version, which `index.md` and
  the rule file say; a file of the user's own in the folder is never read, changed or deleted, and that holds for a
  file with a name axKit uses too: a file that is there, that the marker does not name and that does not hold axKit's
  text is the user's, is left as it is and is not put on the marker (2026-11-21; until then a `changelog.md` of the
  user's own in a working folder of their choosing was written over). The marker's
  first line is the version; its other lines name every file axKit wrote. A file an earlier version wrote and this
  one no longer has (an app that left the bundle) is deleted on the next version's write, by that list alone and only
  when it names a Markdown file directly in `documentation` or the rule file (`AiDocumentation.IsOurs`): a marker is a
  file in the user's folder and is not believed about any other place. The write is seen to when a window asks for
  the working folder (`AiHelpFlow.AiWorkingFolder`: a question, the start of axHelp's conversation, a screenshot,
  Claude's working folder in the menu), at every such use (AH-7.7), and off the window's
  thread (bundle rule 3). Two cases wait for it: a folder that never had the guides (the index or a rule file is
  missing), and a rule file that no longer holds axKit's text (two small files read and compared), since Claude Code
  reads the rules as it starts moments later; in every other case the comparison and any repair of a guide run
  beside the start. A failure is logged and the ask goes on.
- AH-7.2 **Where the texts come from.** `AxKit.Core.csproj` embeds every app's guide once more, one line per app
  (`Guide.axFile.md`), beside the bundle's guide it embeds already. An app's own project keeps its copy for Help, User
  guide; the shared code may not reference an app (bundle 5.3 M2), so it embeds the files, which is no reference.
  `AiDocumentationTests` fails on an app of `Dispatch.Apps` whose guide is not among the files, or differs from the
  repository's; `tools/new-app.ps1` writes a new app's line. The cost is the guides' size once more in the program,
  about 180 KB before compression. The workflow pages are embedded the same way (WF-8.1), as `tools/workflows.ps1`
  wrote them when axKit is built: `docs/axKit/workflows.md` as `Workflows.md`, every page of `docs/axKit/workflows`
  by one line as `Workflows.<page>.md`, and `docs/axKit/window-commands.md` as `WindowCommands.md`.
  `AiDocumentation.WorkflowPages` finds a group's page by trying each name of `Dispatch.Apps` and the two other pages,
  never by listing the resources; `AiDocumentationTests` fails when a page differs from the repository's or a page of
  `docs/axKit/workflows` is not among the files (WF-8.2). They cost about 350 KB more before compression (2026-11-15).
  The key pages, as `tools/key-map.ps1` wrote them, are `KeyGroups.md` and `KeyMap.md`, held to their files by the
  same test; about 50 KB more (2026-11-16). The further pages are `Families.md`, `Cases.md`, `OutsideCalls.md`
  and `Changelog.md`, with `Privacy.md`, which About embeds already; held to their files by the same test; about
  340 KB more, most of it the changelog (2026-11-17).
- AH-7.3 **Claude is told by a rule file.** `<working folder>\.claude\rules\axkit-documentation.md`, written and
  kept current with the guides, says: for a question about axKit or one of its apps, read `documentation/index.md`
  and the fitting guide with the Read tool before answering, answer from the guide, search the folder with Grep when
  the guide is not obvious, and say when the guides do not cover the question. Since WF-8.3 it also says how to
  answer from the workflow pages, in that section's words: what the files are; what axKit or an app can do is
  answered from `workflows.md`, naming the workflows and goals that fit; how to do something is answered by finding
  the goal in the app's workflows file (by its title, with Grep) and giving its steps as written there, keys
  included; a file name or a word typed in the steps is the checks' example, said to be one and replaced by the
  user's own, and nothing else is changed; a key or a menu is never invented, and where a guide and
  `window-commands.md` differ, `window-commands.md` is right; when no goal covers the question, the app's guide and
  `window-commands.md`, saying when it is not sure. Since 2026-11-17 it names the further pages by the question
  each answers: what a sound means or what axKit says when something happens, `families.md`; what axKit does when
  something goes wrong, `cases.md`; what axKit contacts or runs outside itself, `privacy.md`, and `outside-calls.md`
  for the exact list; what changed in a version, `changelog.md`, searched with Grep; and that `families.md`,
  `cases.md` and `outside-calls.md` are written from axKit's source and name its files and tests, which stay out of
  an answer unless the user asks for them. **AH-7.3b** (the maintainer's decision of 2026-11-16, "Explain, then
  offer to do it"; built 2026-11-24, WF-D-24): after the steps, Claude may offer once, in one sentence, to carry them
  out in the open window through the AI tools, when the tools are there and a window of that app is open, saying
  `AiCaution.OfferToDo` word for word ("Claude, an AI, would run these steps for you through axKit's AI tools. It
  can be wrong; you hear each command as it runs, and a command that acts outside the window asks you first. Say yes
  only if you want it done."); it runs nothing before the user's yes, then each step's command with
  `axkit_window_run`, leaving a step that is a key alone to the user; without the run switch it says where to allow
  it instead of offering. What a run may do stays the AI tools' rule file's (AH-8), whose Use line for the run names
  the user's yes to such an offer. **AH-7.3a** (2026-11-16, from the user's own
  axHelp session, in which Claude remarked on the hour and on rest): the file ends with one paragraph
  (`AiDocumentation.Manner`): never remark on the user's rest, health or the time of day unless the user asks, no
  word that it is late or early, no suggestion of a break, sleep or stopping for the day; the computer's clock may
  not be the user's time. It is in this file, which axKit keeps at its text (AH-7.7), and not in the user's
  CLAUDE.md, which is the user's to change. Claude Code loads every Markdown file
  of a folder's `.claude\rules` at its start, with CLAUDE.md, in print mode and in a conversation alike
  (code.claude.com/docs/en/memory, "Organize rules with .claude/rules/"; read 2026-11-15). So the instruction also
  reaches a user whose CLAUDE.md exists already: that file is theirs, written once, and is never read or changed for
  this. A new folder's CLAUDE.md names the guides as well (`AxHelp.Core/WorkingFolderFiles.Instructions`).
- AH-7.4 **Nothing else at the top of the folder.** What axKit puts into the working folder: `documentation`,
  `screenshots` (axHelp, HE-3.1), `CLAUDE.md` and `.claude` (the agent, the two rules, AH-7.3 and AH-8; Claude Code's own settings). No
  picture, log or export of axKit's lands loose there (checked 2026-11-15: the screenshots were in their folder
  already; the conversation's Export proposes the folder in the save dialog and the user chooses).
- AH-7.5 **Local first** (bundle G6, B-15; AH-D2 stands): no index, no embeddings, no service and no network request
  of axKit's. The retrieval is Claude Code's own read-only tools (Read, Grep, Glob) over plain files. A guide's text
  goes to Anthropic only as any file does that Claude reads for an answer; the guides are public text of axKit's
  repository and hold nothing of the user's.
- AH-7.7 **The files repair themselves** (the user's ask of 2026-11-16: the folder must not be able to break axHelp
  when a user deletes or damages it, and the repair must be code alone, with no network and no AI reasoning). At
  every use of the folder each file of AH-7.1 and the two rule files are compared with the text this version of axKit
  holds built in (`AiDocumentation.Write`; the rules before Claude Code starts, `AiDocumentation.RulesNeedRepair`): a
  file that is missing, emptied or changed in any character is written again, whoever changed it, the user, another
  program, or Claude, which may write in its working folder. The whole folder deleted is the same case: everything
  of axKit's is there again at the next use. Nothing but axKit's own files is read or written for it: `CLAUDE.md`
  and the agent file stay the user's (written only when missing, so deleting one resets it), and so does every file
  the user adds. What decides what axKit itself does (the AI tools' switch and pairing, the consent, `ai-help.json`)
  was never in this folder: `mcp.json`, `consent.json` and `ai-help.json` are under `%APPDATA%\AxKit`
  (`AppPaths.McpFile`, `ConsentFile`, `AiHelpFile`), and the keys their seals are made with under
  `%LOCALAPPDATA%\AxKit`. `AiDocumentationTests`.
- AH-7.8 **Files Claude Code obeys that axKit did not write are asked about** (2026-11-16, `repairs.md` R-54; the
  maintainer's requirement: mechanical, plain code, no network and no AI reasoning, no interaction that is not
  needed). The self-repair of AH-7.7 covers axKit's own files; Claude Code obeys others in the folder, which the
  user, another program or Claude itself may have written. `AxKit.Core/Ai/AiFolderCheck` lists them by name and
  place alone, each with a SHA-256:
  - `.mcp.json`;
  - `.claude\settings.json` and `.claude\settings.local.json`, only when they hold a `hooks` member that names
    anything, or are no JSON at all. The hash is of the `hooks` member's text, so that an "always allow" answer
    Claude Code writes beside it changes nothing; their other members are the user's and are never asked about;
  - every file under `.claude\skills`, `.claude\commands`, `.claude\output-styles`, `.claude\plugins`;
  - every file under `.claude\agents`, but `agent.md` while it holds exactly the text axHelp writes;
  - every file under `.claude\rules`, but axKit's two, which AH-7.7 keeps at axKit's text.

  Not on the list, by decision: `CLAUDE.md` and `CLAUDE.local.md` (the user's standing instructions; words, not
  commands) and anything outside `.claude` but `.mcp.json`. The files the user accepted are kept, by working folder,
  path and hash, in `%APPDATA%\AxKit\ai-folder-files.json` under the settings' seal (bundle AX-5.6, `SettingsSeal`):
  a list changed by hand or by another program is not used, and every file is asked about again. The check returns
  the files that are new or changed against that list. Before axHelp starts its conversation in the folder (axHelp
  HE-2.9) and the check returns anything, axHelp's window shows the notice **"Claude's working folder holds files
  that can run commands"** before Claude Code starts: how many, their names one a line (the first twenty), that
  axKit did not write them, that Claude Code would obey them from its start, and that they may be the user's own;
  **Open the folder** (the notice stays), **Start anyway and remember these files**, which writes them onto the
  list as they were named and starts, and **Cancel**, the answer to Enter and Escape, which starts nothing. When
  the check returns nothing, nothing is said and nothing is asked. A conversation kept ready in the background
  (HE-2.7) is not started while the check returns anything: the start waits for the user's next ask, which shows
  the notice. The notice is axKit's own text, not an AI's, so AX-11.12 asks nothing of it. Shift+F1's questions
  need no notice: they obey neither hooks nor servers (AH-2.6), and print mode allows the three reading tools
  alone. Limits, stated: an axClaude window the user starts in the folder themselves is Claude Code's own trust
  question's; a conversation that is already running is not checked again when it is brought up; settings members
  other than `hooks` that name a command (`statusLine`, `apiKeyHelper`) are not on the list.
  `AiFolderCheckTests`, `HelpTests`; the notice in the window is owed a window check and a hearing.
- AH-7.6 The cautions are unchanged (bundle AX-11.12): an answer of Shift+F1 still ends with `AiCaution.Written`,
  and the conversation is axClaude's with its own.

### AH-8 The rule file of axKit's AI tools (2026-11-15)

The user, 2026-11-15: "make axhelp load the mcp automatically and provide lots of instructions along with some
language rules to help it know when to use which tool". How the conversation gets the tools is axHelp's
(`docs/axHelp/SPEC.md` HE-2.8) and the AI tools' (`mcp.md` MC-6.9); the instructions are here, since they are a
file of the working folder.

- AH-8.1 **The file.** `<working folder>\.claude\rules\axkit-tools.md`, a second rule file of axKit's own beside
  AH-7.3's, loaded by Claude Code at every start as that one is. It is one of the files of AH-7.1
  (`AiDocumentation.Files`): written where missing, written again by each new version of axKit, named in the marker,
  deleted by the marker alone should axKit ever stop writing it; a file of the user's in `.claude\rules` is never
  touched. A folder that has the guides and lacks this file (one an axKit before it wrote) counts as missing
  (`AiDocumentation.IsMissing`), so the file is there before Claude Code starts. It is written whether the AI tools
  are on or off: off, it is how Claude knows to say where the user turns them on.
- AH-8.2 **What it says** (`AxKit.Core/Ai/AiToolsRule.Text`), plain Markdown for a model, about 150 lines:
  1. what the tools are for; that the user is blind and hears every action, so one short sentence before an action
     and one after; reading before acting; that every call is in the user's log; that the first call may make an
     axKit window ask the user to allow Claude Code; and (`AiToolsRule.ListedNotLoaded`, 2026-11-16, from the user's
     own axHelp session: Claude Code may list a server's tools by name and load each at its first use, and Claude
     told the user they were not loaded and asked whether to load them) that tools listed and not yet loaded are
     loaded as needed without asking the user and without saying that they were not loaded, and that, asked whether
     axKit's tools are there, the answer is yes;
  2. **the language rules**: what the user is likely to say and what answers it. "What is on my screen", "what is
     open": `axkit_windows`, then `axkit_window_state`, then `axkit_window_text` or `axkit_window_list`. "What did
     it just say": `axkit_window_said`. "Open ...": `axkit_apps`, `axkit_open`; axClaude, axTerminal and axTask on a folder are asked of the user first (mcp.md MC-6.5a).
     "Press, run, turn on, do X in that window": `axkit_window_commands` for the label, the command said first,
     then `axkit_window_run`, only while the user allows commands to run. "Add a task", "remind me":
     `axkit_task_lists`, `axkit_task_add`. "What are my tasks": `axkit_task_lists`, `axkit_task_items`. "How do
     I ... in axFile": the workflows and the guides of AH-7, not a tool (one entry on two lines, WF-8.4, since a
     hand-written line holds at most 112 characters). A command the user must run themselves:
     `axkit_offer_command`. A screenshot's message: the picture read first;
  3. **every tool** of `McpTools.All`, in its order, each with its own description as the tool gives it, then
     three lines written by hand (`AiToolsRule.Tools`): when to use it, when not, what to say;
  4. the caution of an offered command, quoted from `AiCaution.Short` and `AiCaution.Command` (bundle AX-11.12,
     root rule 11: never written afresh), and that a No is the answer;
  5. the limits: axPass and axClip never readable (`McpTools.NeverRead`), nothing answers a question a window asks
     the user, a command that acts outside its window asks first, a refused call is not retried another way, no
     keys and no typing;
  6. when the tools are not there: say so, and where the user turns them on (axKit menu, AI tools, Let AI tools read
     and open axKit; the run switch beside it, asked for only when the task needs it).
- AH-8.3 **It cannot drift.** The tools' names and descriptions come from the one table (`McpTools.All`), and
  `AiToolsRuleTests` fails when a tool has no guidance, when the file lacks a tool, or when the file names an
  `axkit_` tool that does not exist.
- AH-8.4 The file changes nothing about what Claude may do: every gate is the tools' own (`mcp.md`), and the file
  says only how to use them well. Shift+F1's print mode reads the same folder and so loads the file, but has no
  AI tools (AH-2.2: only `Read`, `Glob` and `Grep` are allowed there); the file's last section tells Claude what to
  say then.

## 5. Architecture

- `AxKit.Core/Ai/AiDocumentation` (AH-7): `Files(version)`, the guides, the workflow pages (`WorkflowsName`,
  `WorkflowPages`, `WorkflowPageName`, `WindowCommandsName`; `user-workflows.md` WF-8), the index and the rule with
  their paths, and `Write(workingFolder, version, rewrite)`; tested in `AiDocumentationTests`. `AxKit.Core/Ai/AiToolsRule` (AH-8):
  the AI tools' rule file, `Text` made from `McpTools.All` and the hand-written `Tools`; `AiToolsRuleTests`.

- `AxKit.Core/Ai/AiSettings` (AH-1.1), `AiRequest` (the command line and the process, AH-2), `AiAnswer` (the JSON
  read, with the session), `AiPrompts` with `WindowContext` (AH-4, AH-6.2).
- `AxKit.Forms/AxKitForm.Ai.cs` (the menu and the members a window calls) and `AxKit.Forms/AiHelpFlow.cs` (the work, a plain class the window holds since 2026-11-14): `CreateClaudeMenu` (axKit, Claude, AX-9.1, beside the AI tools), `TurnOnAi` (AH-1.3), `AskAboutThisWindow` (Shift+F1,
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
  in the first allowed folder, or the user's profile, scattered that; one folder, `Documents\axKit\Claude`, made when
  first needed and opened from the menu, gathers it and gives the user one place to write Claude standing
  instructions. The allowed folders stay what they were, `--add-dir` each. It was `Documents\axKit Claude` until
  2026-11-23, when the maintainer chose to move it into axKit's folder of what the user keeps (bundle AX-5.11); an
  update moves an existing `Documents\axKit Claude` in at its first start, unless the settings name it
  (`AppPaths.TakeOverUserFolders`, his choice, "Move both on update"); while it cannot be moved it stays in use
  (`AiSettings.WorkingFolderFor`). Claude Code finds its sessions by the folder's path, so axHelp starts a new
  conversation after the move; the folder's CLAUDE.md and the permissions given with "always" move with it.
- **AH-D7 The guides as files, the instruction as a rule file** (2026-11-15, AH-7). Three ways to tell Claude about
  the guides were weighed. A line in the working folder's CLAUDE.md reaches only new folders: the file is the user's
  and is written once. Claude Code's `--append-system-prompt` works in print mode and in a conversation, but Claude
  Code records the system prompt with a conversation's first request and reuses it after `--continue` and `--resume`
  until the conversation is compacted (code.claude.com/docs/en/cli-reference, "System prompt flags in resumed
  conversations"), so it would not reach axHelp's conversation, which goes on; it would also put a long sentence on
  the command line of npm's `claude.cmd`, where cmd.exe refuses several characters (AH-2.1a), and need a new option
  of axClaude's. A file of axKit's own in `.claude\rules` is loaded at every start in both modes, needs no option and
  no change to a launcher, is plain text the user can read, and is kept current by the same code that writes the
  guides. That is the one mechanism. If Claude Code stops reading `.claude\rules`, this one file's place changes
  (root rule 12). The retrieval stays Claude Code's own reading (AH-D2): the guides are small and few, and a search
  by name and by Grep finds what a question needs.

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
  are started from any window's Access layers and then keep running; which eight the Store shows is the user's to change
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
  folders, the working folder and the AI tools (AX-9.1); the Claude apps (axClaude, axMCP, axHelp, axDescribe) stood
  as the last group of every list of apps until 2026-11-23, when axClaude became the AI group and axHelp and axMCP
  the access layers' utilities (`AppGroup.Ai`, `AppGroup.Utilities`, AX-9.2). The switch is AI help's: axClaude
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
  HE-D1): Ctrl+Shift+Slash (Windows+Alt+H until 2026-11-14, bundle AX-8.11) brings it up on Claude's working folder (AH-2.1), titled "axHelp", ongoing until the user
  clears it; Ctrl+Shift+Comma (Windows+Alt+S until then; Ctrl+Shift+Period since 2026-11-23, when Comma went to axClip) puts a picture of the window in front into its message field. axDescribe leaves the bundle;
  its verb still starts axHelp.
- So axHelp is no longer a use of this service's print mode: the conversation is Claude Code in its interactive mode,
  with axClaude's agreement and its permission questions, not read-only. Shift+F1 in every window (AH-6) stays this
  service's, as section 4 has it.
- The model is axHelp's own, given at the start (`--model`, fast by default), and a subagent on the strongest model in
  the working folder takes the work of several steps (HE-2.3, HE-2.4); `/model` inside the conversation would change
  Claude Code's saved default everywhere, which the guide says.
- AiPrompts lost `Anywhere`, `Screen` and `Picture`, which only the two apps used; the working folder (AH-D6) is what
  axHelp still takes from here.
- Since 2026-11-15 the conversation hides on closing and the hotkey brings the same window back, and, only when the user turned on
  axHelp's setting Keep the conversation ready in the background (off at first), axHelp started in the background
  starts it hidden, Claude Code loaded, once the user has agreed to Claude Code (`docs/axHelp/SPEC.md` HE-1.3,
  HE-2.1, HE-2.7, HE-D9; axClaude FR-9.6). That start is the one moment Claude Code runs without the user's key, and
  it exists only with that setting on; this service's own questions (AH-2) start Claude Code only when asked.
