# The documents

Where each thing is written. The apps and their folders are in `apps.md`; this page is the bundle's own documents, in
`docs/axKit/` unless a path is given. Read the one that covers what you are changing; none needs the others.

## Start here

- The root `CLAUDE.md`: the rules of the bundle, the constraints and the environment's gotchas.
- `apps.md` (generated): the apps, each with its command, its code, its tests, its documents and its checks.
- `commands.md`: every command and script, and which check holds which rule (its section Test).
- An app's own `docs/<app>/SPEC.md`, `user-guide.md` and `nvda-test-plan.md`, and the `CLAUDE.md` notes beside its code. The shared window code's notes are an index, `src/AxKit.Forms/CLAUDE.md`, over one file per kind of window in `src/AxKit.Forms/notes`, which that index lists: read the file of the kind you change. The pure shared code's notes are `src/AxKit.Core/CLAUDE.md`, over `src/AxKit.Core/notes`, the same way. The notes of axClaude's window, `src/AxClaude/CLAUDE.md`, likewise point to two files in `src/AxClaude/notes`: a window that hides on closing, and the machinery beside the screen (the event core, the session file's reader, the hooks). The notes of its pure code, `src/AxClaude.Core/CLAUDE.md`, point to `src/AxClaude.Core/notes/stream.md` for the stream.

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
- `docs/axClaude/stream-mode.md` (SM-n.m; a specification, 2026-11-17; built, behind the setting `claudeSource`): Claude
  Code's non-interactive stream as a second source of axClaude's window behind a switch: the seam (one model with two
  sources, `ClaudeModel`), the stream's model, host and relay, what the window sends, what the stream does not have
  and what its documentation does not promise (SM-6.7), the folder's trust and the questions, sessions, the switch,
  the checks with what the maintainer's two hours of use should try (SM-10.5), the path in eight stages, and what is
  the maintainer's to decide. The measurements are `tools/ClaudeProbe stream` (its README, section 9);
  `stream-shape.json` beside the spec (written by the probe, never by hand) holds the keys of every kind of line seen,
  without a value. The code that knows the stream's words is `src/AxClaude.Core/Streaming/ClaudeCodeStream.cs`.
- `docs/axChat/spin-out.md` (a proposal for the maintainer and the sitting with Fable, 2026-11-26): whether axChat
  should be spun out of axClaude's stream mode, what the screen and the stream share, the two shapes axChat could
  take, the order of moving the shared pieces, the risks and the questions for the sitting.
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
- `outside-reports.md` (OR-n): what is done with a report that comes from outside: its entry in the outside reports
  list with the harm review and a proposed scope, the maintainer's signed approval in axTask, and the checks that hold
  it (rule 9). The approval key's public half is `approval-key.pub` beside it, written by `tools/pin-approval-key.ps1`.
- `axkit-list 2.options.json`: the options of the list format, for other programs that write axTask's lists; held to
  `ListOptions` by `ListOptionsTests`.
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
  and every family of keys must be named there; since rule 13 (2026-11-17) `EventKindTests` holds `EventKind` to it, so
  a new kind of event is written there first. The key families stay in `key-families.json` and the sounds' design in `earcons.md`.
- `events.md` (EV-n.m; a proposal of 2026-11-17, its step 2 built: `EventKind`, the door `AxKit.Forms/EventDoor` and `EventPresenter`, rule 13): one event schema of
  axKit's own, the kinds of `families.json`; each outside program behind an adapter that may only raise events of it;
  one door through which a window raises an event and by which each kind gets its sound and its words; the tests
  that would hold the boundary; the path in four steps; what is his to decide.
- `framework.md` (FW-n.m; a proposal of 2026-11-22 at the maintainer's direction, built: every app is on one of the types, `components.json` and `ComponentsTests` hold it): the
  smallest set of window types (the conversation window, the editor, the navigator, the hotkey window; a graph much
  later), the canonical components each is made of with their entry events, every app mapped onto a type and what
  goes for it, the tests that would hold it, the work in sixteen small pieces, and the decisions taken and open.
- `components.md` (generated by `tools/components.ps1` from `components.json`; framework FW-7.3): the canonical
  pieces a window is made of, each with what is built into it, its sound and its keys by family; the window types
  with their panes; every app with its type today and the type it is to be. `ComponentsTests` holds the sources to
  the file.
- `owed.md` (generated by `tools/owed.ps1` from `cases.json`, `repairs.json` and the Owed sections of
  `nvda-test-record.md`): everything tested in code and not yet heard or tried for real, by the kind of check it
  waits for. What is owed is written in those three places and read here.
- `key-families.json` (by hand; `"status": "settled"`, the maintainer settled every family on 2026-11-26 and the
  keys moved for them the same day, AX-8.18): what each kind of key is for (Ctrl with a letter, Ctrl with a digit,
  the F-keys and so on) and the exceptions kept on purpose, each with its reason. No test enforces the families.
- `workflow-graph.md` (by hand, a proposal of 2026-11-26, its decisions settled the same day, steps 2 and 3 built): the
  workflow graph, every place a node and every action an edge, the two bounds of G7 checked over it, the scripts as
  walks and the map held against each change.
- `workflow-graph.json` (written by the runner, never by hand; `workflow-graph.md` WG-2): the graph, by app every
  place of the windows with what is over it and every action from it with where it lands, the words said and the
  kind of event; `tools/AxAutomate --inventory write` writes it from a crawl of each window on the hidden desktop, and
  it says which apps it covers. `PlacesTests` reads its controls of no place, `WorkflowGraphTests` its form.
- `map.md` (generated by `tools/map.ps1` from `workflow-graph.json`; WG-2.5): the map for people, by app each
  waypoint and each other place, how it is reached in one action, and every key that does something there.
- `over-the-bounds.md` (generated by `tools/graph-check.ps1` from the scripts of `tests/scripts`, the command
  inventory and `workflow-graph.json`; WG-3.4): by app the goals over four actions with their count and steps, those
  at four, and the nodes more than two actions from a waypoint; the list the shortenings are decided from.
- `shortenings.md` (by hand, 2026-11-26, a working page): for the goals still over four actions or at four after the
  scripts were re-marked, one app a section, each goal's count and steps and the assistant's proposal to bring it to
  three or four, for the maintainer's sittings; deleted once each app's sitting has decided.
- `for-your-return.md` (by hand, from 2026-11-27, a working page): the record of the three days the assistant worked
  alone at the maintainer's word: where things stand, what he decided before leaving, what the assistant decided for
  him to turn back, what is owed his ear and what only he can do; deleted once it has been gone through together.
- `goal-counts.json` (written by `tools/graph-check.ps1`, never by hand; WG-3.3, WG-3.4): the baseline, each goal's
  workflow, title, script and count of actions. The check holds the scripts to it: a goal may get shorter, never
  longer; `tools/graph-check.ps1 -Write` takes a longer one in, at the maintainer's word. `GoalCountsTests` holds the
  counting rule.
- `command-exceptions.json` (by hand, 2026-11-25; `command-language.md` CL-7.3): the command names several apps share
  that do not mean one thing among them, each with what differs and its reason. `CommandMeaningsTests` holds the
  inventory to it: a new difference is made one or added here on purpose, and one made one is taken off.
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

- `plan.md`: the direction, the order of the work in hand, and what has been asked for and is not built yet.
- The priorities list of the private management repository (`..\axkit-management\priorities`) is the one place for
  open work (root `CLAUDE.md`, rule 9).
- Dated records. A finished review, inventory, audit, plan or report is deleted once its results are in the code, a
  specification or the priorities list; the git history is the archive (the maintainer's word, 2026-11-17). Gone
  that day, each last present at commit ab8cf1b1 (`git show ab8cf1b1:docs/axKit/<name>`): `review-2026-10-23.md`,
  `inventory-2026-10-24.md`, `inventory-2026-11-09.md`, `cleanup-run.md` and `cleanup-run-report.md`,
  `security-audit-2026-10-24.md` (the code still names its findings by number, S-n), `axfile-split.md` (how axMedia
  and axPreview came out of axFile), `code-signing.md` (the Windows signing set up and retired for the Store's
  signature), and the entries of the NVDA record before 2026-11-15. Gone on 2026-11-26, each last present at commit
  fc3109e6: `docs/axClaude/stream.md` (the stream measured, its decision taken) and
  `docs/axClaude/stream-equivalence.md` (what the stream loses, its nine items built), both now in `stream-mode.md`;
  `key-moves.md` (every key family settled and moved); the planned axList's `docs/axList/` (parked, `plan.md`); and
  `docs/feature_requests/README.md` (a pointer to the reports repository). A new dated record says in its first lines
  what ends it, and goes when that has happened.

## The repository's pages

- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `GOVERNANCE.md`, `CODE_OF_CONDUCT.md`, `THIRD-PARTY-NOTICES.md`,
  `CHANGELOG.md` at the root; `NOTICE.md`, what axKit does, what each program under an access layer does and what is the
  user's, embedded in the app.
- The Menus section of each app's `user-guide.md` is written from the command inventory by `tools/guide-menus.ps1`, never
  by hand; `tools/pages.ps1` writes it with every other generated page.
- `PRIVACY.md`: everything axKit contacts. It changes in the same commit as any new network contact or program axKit
  starts (rule 8).
- `.github/`: the workflows, the issue forms and the pull request checklist.
- `.claude/skills/accessibility-review` (run on any change to a window before committing) and
  `.claude/skills/record-fixture` (axClaude's recordings).
