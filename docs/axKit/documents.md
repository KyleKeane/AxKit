# The documents

Where each thing is written. The apps and their folders are in `apps.md`; this page is the bundle's own documents, in
`docs/axKit/` unless a path is given. Read the one that covers what you are changing; none needs the others.

## Start here

- The root `CLAUDE.md`: the rules of the bundle, the constraints and the environment's gotchas.
- `apps.md` (generated): the apps, each with its command, its code, its tests, its documents and its checks.
- `commands.md`: every command and script, and which check holds which rule (its section Test).
- An app's own `docs/<app>/SPEC.md`, `user-guide.md` and `nvda-test-plan.md`, and the `CLAUDE.md` notes beside its code. The shared window code's notes are an index, `src/AxKit.Forms/CLAUDE.md`, over one file per kind of window in `src/AxKit.Forms/notes` (the window base, the controls, the console window, the editor shell, the item window, the hotkey window, AI and automation): read the file of the kind you change. The notes of axClaude's window, `src/AxClaude/CLAUDE.md`, likewise point to two files in `src/AxClaude/notes`: a window that hides on closing, and the machinery beside the screen (the event core, the session file's reader, the hooks). The notes of its pure code, `src/AxClaude.Core/CLAUDE.md`, point to `src/AxClaude.Core/notes/stream.md` for the stream.

## Specifications

- `SPEC.md`: the bundle. Launch, install, update, release, the shared pieces, the accessibility rules (AX-7), the
  keys (AX-8), the menus (AX-9), the command palette (AX-10), the access layers (AX-11), the Store's copy (AX-13), the
  modularity rules.
- `app-kit.md` (AK-n): the shape of an app's window and commands; one source for the menus, the palette and the AI
  tools (AK-2.5, AK-2.6).
- `command-language.md` (CL-n): the commands as one language, stated from the code: what a command is, where an
  action can come from (the key, the menu, the palette, a script's `do`, an AI tool), the table of who may run a
  command of which mark and what the user hears (section 3), the path each origin takes, how a name finds a command,
  the boundary of what axKit promises, and what is not built yet.
- `item-apps.md` (IA-n): the shape axTask, axPass, axMCP and axAutomate share, the navigator and the detail pane.
- `mcp.md` (MC-n): the AI tools, what an AI client may read and run in axKit's windows and how the user stays in
  control.
- `ai-help.md` (AH-n): AI help, Claude Code in print mode with read-only tools, Shift+F1 in every window, and axHelp.
- `access-layers.md`: the programs under the access layers (Claude Code, Git, KeePassXC's tool, the shells, MCP
  servers), where each one's texts, options and protocol are written, what is read from it and how, what guards it,
  and what is still outside that one place (root `CLAUDE.md`, rule 12).
- `docs/axClaude/stream.md` (a report, 2026-11-17, deciding nothing): Claude Code's non-interactive stream as a third
  way for axClaude to learn what Claude does, beside the screen and the session file with the hooks: what the
  documentation says, what `tools/ClaudeProbe stream` measured in twenty-four cases, the stream in the kinds of event of
  `families.json`, what a window over it would and would not need, and the ways to go. `stream-shape.json` beside it
  (written by the probe, never by hand) holds the keys of every kind of line seen, without a value. The code that
  knows the stream's words is `src/AxClaude.Core/Streaming/ClaudeCodeStream.cs`; no window uses it yet.
- `docs/axClaude/stream-mode.md` (SM-n.m; a specification, 2026-11-17, nothing of it built): the stream as a second
  source of axClaude's window behind a switch: the seam (one model with two sources, `ClaudeModel`), the stream's
  model, host and relay, what the window sends, what the stream does not have, the folder's trust and the
  questions, sessions, the switch, the checks, the path in eight stages, and what is the maintainer's to decide.
- `docs/axClaude/events-and-grounding.md` (EG-n.m, EG-D-n): how axClaude learns what happens in a Claude Code session
  and how the conversation is tied to Claude Code's own record; it amends `docs/axClaude/SPEC.md` (FR-15). Where each
  piece is written: the marks of a turn, EG-1; Claude Code's events and the settings file with the hooks, EG-2 and
  its sections 6.1 and 6.2 (code: `src/AxClaude.Core/Hooks`); the session file's reader, "the record", EG-3 and 6.3
  (`src/AxClaude.Core/Record`); the core that takes all of it, EG-4 to EG-7, 6.4 and 6.5 (`src/AxClaude.Core/Flow`,
  and `MainForm.Flow.cs` in the window); the build plan, section 10. The fixtures: `tests/fixtures/timelines` (a
  recorded session's console stream, events and session file entries on one clock, made and masked by
  `tools/ClaudeProbe`) and `tests/fixtures/records` (session file lines written by hand), both under the rules of
  `tests/fixtures/README.md`.
- `reports.md` (RP-n): the reports from every window (F4), encrypted on the user's computer and sent to GitHub as
  issues of the private reports repository; the envelope; axTask's action keys, and why its GitHub lists were
  removed.
- `theme.md` (TH-n): the theme file, every colour from one JSON file, and the display settings.
- `earcons.md`: the classes of sound, a proposal of 2026-11-14 not built: each class of interaction (a tick, a move,
  a turn, an ask whose number of knocks says how much is asked, code that asks to run) with the pattern that names it and the
  variants inside it, the sounds that exist and the ones proposed.
- `adding-an-app.md`: how a new app is made and every place it is registered.

## Checks and quality

- `automation.md`: the automated checks of the windows, the runner, and the NVDA pass.
- `workflows.md` and `workflows/` (generated by `tools/workflows.ps1` from the scripts and the command inventory):
  the workflow pages (`user-workflows.md` WF-7). `workflows.md` is the index, by app every workflow the automated
  checks keep working and under it its goals; `workflows/<app>.md` gives each goal's steps, with the keys as they are
  now, and what is heard.
- `user-workflows.md` (WF-n): workflows as one source for the checks, the how-to, axHelp and the AI tools: goals, the
  step `do`, the command inventory, the workflow record.
- `window-commands.json` (written by the runner, never by hand): the command inventory, every window's commands by
  app as the windows list them; `tools/AxAutomate --inventory write` writes it, a `--convert` run merges the rows it used into it, every window run checks it
  (`automation.md`, "The command inventory").
- `window-commands.md` (generated by `tools/workflows.ps1` from `window-commands.json`): every window's commands, one
  line a command, with its menu, its key and its mark.
- `key-map.md` (generated by `tools/key-map.ps1` from `window-commands.json`, `src/AxKit.Forms/BundleKeys.cs` and
  `key-families.json`): every key, whether it is a bundle key or a window's, and what it does in each app; first the
  keys with one meaning everywhere, then the keys whose meaning differs, most spread first; last the key families.
- `key-groups.md` (generated by the same script): the same keys the other way round, for learning them: by context
  (from anywhere in Windows, the same in every axKit window, inside one app's window) and under each by group, with
  the sentence a user is told, the keys that fit, Windows' own keys and the exceptions. `key-families.json` is the
  one place the groups, their contexts and their exceptions are written.
- `families.md` (generated by `tools/families.ps1` from `families.json`): the four families side by side, for the
  maintainer and the assistant to name the same things by the same short names: what starts something (your hands,
  the program underneath, an AI, axKit itself, Windows), the functions (the commands by menu, with their marks), the
  events (every kind of thing that can happen, with its sound or none and its words) and the sounds; then each family
  of keys with the sounds that can follow one, and the events with no sound. `families.json` is written by hand and
  held to the code by the script: every sound of `Sounds.cs`, every file that plays one, every menu of the inventory
  and every family of keys must be named there. `"status": "proposed"`; it describes what the code does and changes
  nothing. The key families stay in `key-families.json` and the sounds' design in `earcons.md`.
- `events.md` (EV-n.m; a proposal of 2026-11-17 for the maintainer to read, nothing built): one event schema of
  axKit's own, the kinds of `families.json`; each outside program behind an adapter that may only raise events of it;
  one door through which a window raises an event and by which each kind gets its sound and its words; the tests
  that would hold the boundary; the path in four steps; what is his to decide.
- `owed.md` (generated by `tools/owed.ps1` from `cases.json`, `repairs.json` and the Owed sections of
  `nvda-test-record.md`): everything tested in code and not yet heard or tried for real, by the kind of check it
  waits for. What is owed is written in those three places and read here.
- `key-families.json` (by hand; `"status": "proposed"` until the maintainer settles it): what each kind of key is
  for (Ctrl with a letter, Ctrl with a digit, the F-keys and so on) and the keys that do not fit today. No test
  enforces the families, and no key moves because of them.
- `tests/traces/<folder>/<script>.trace.txt` (written by the runner, never by hand): the kept traces, for each
  section a script marks `# Stable` every input and what the windows said under it; `tools/AxAutomate --traces write`
  writes a script's file, and every window run on the hidden desktop fails a stable section whose trace differs
  (`automation.md`, "The traces"; `user-workflows.md` WF-5). No section is marked yet, so the folder does not exist.
- `upstream.json`: the outside programs (Claude Code, Git, KeePassXC's tool, the shells), each with the version the
  checks last passed with; read and written by `tools/upstream.ps1` (`automation.md`, "When an outside program
  changes").
- `workflow-record.json` (written by runs of the automated checks with `--record`, never by hand): when each goal
  and check last ran and last passed, on the checks' own desktop, under NVDA and through the AI tools.
  `tools/window-passes.ps1` holds it against the commits of each app's window code, and `release.ps1` refuses while
  a window changed after its last recorded pass.
- `nvda-test-record.md`: what every NVDA pass found, newest first; when each goal last passed is in
  `workflow-record.json`.
- `repairs.md` (generated from `repairs.json`): what the audits found and what became of each finding.
- `cases.json` (C-n; by hand, one row per case): how each unexpected situation is handled, a file that is missing,
  cannot be opened, was changed outside axKit and so on: what axKit does, the exact words it says, and the test or
  window script that proves it. `CasesTests` holds every proof and every sentence to the code (SPEC AX-5.10).
  `cases.md` (generated by `tools/cases.ps1`) is the same table to read, by thing, then by situation.
- `outside-calls.md` (generated): everything axKit reaches outside its process (SPEC B-17).
- `longevity.md`: the practices that keep the code maintainable for decades, and the yearly check.

## Release and distribution

- `README.md`: the bundle's guide, shipped in the zip.
- `store-distribution.md`: the plan for the Microsoft Store and winget, what the Store's copy changes, the steps and
  their state.
- `store-listing.md`: the Store listing's texts.
- `store-setup.md`: the Store account's runbook.
- `reports-setup.md`: the reports repository, the reporter account, its token and the release secret; the
  maintainer's key for the sealed reports, its backup in the private `KyleKeane/AxKit-keys`, and how reports are read
  (`tools/ReportKey`).
- `development-environment.md`: every tool of the development computer and how to set a new one up; the four
  folders side by side; where the keys and their backups are (section 2: the release key and the reports key, both
  backed up in `..\axkit-keys`); the second development computer's checklist (section 6). The release key's commands:
  `commands.md` and the header of `tools/release-key.ps1`.

## Plans and history

- `plan.md`: how the repository became the bundle, and the apps asked for and not built yet.
- The priorities list of the private management repository (`..\axkit-management\priorities`) is the one place for
  open work (root `CLAUDE.md`, rule 9).
- Dated records. A finished review, inventory, audit, plan or report is deleted once its results are in the code, a
  specification or the priorities list; the git history is the archive (the maintainer's word, 2026-11-17). Gone
  that day, each last present at commit ab8cf1b1 (`git show ab8cf1b1:docs/axKit/<name>`): `review-2026-10-23.md`,
  `inventory-2026-10-24.md`, `inventory-2026-11-09.md`, `cleanup-run.md` and `cleanup-run-report.md`,
  `security-audit-2026-10-24.md` (the code still names its findings by number, S-n), `axfile-split.md` (how axMedia
  and axPreview came out of axFile), `code-signing.md` (the Windows signing set up and retired for the Store's
  signature), and the entries of the NVDA record before 2026-11-15. A new dated record says in its first lines what
  ends it, and goes when that has happened.

## The repository's pages

- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `GOVERNANCE.md`, `CODE_OF_CONDUCT.md`, `THIRD-PARTY-NOTICES.md`,
  `CHANGELOG.md` at the root.
- `PRIVACY.md`: everything axKit contacts. It changes in the same commit as any new network contact or program axKit
  starts (rule 8).
- `docs/feature_requests/README.md`: points to the reports repository `KyleKeane/AxKit-reports`, where feature
  requests and bugs are issues.
- `.github/`: the workflows, the issue forms and the pull request checklist.
- `.claude/skills/accessibility-review` (run on any change to a window before committing) and
  `.claude/skills/record-fixture` (axClaude's recordings).
